import {
  PRINT_API_TIMEOUT_MS,
  buildStandaloneOrderReceiptHtml,
} from "./receiptPrint.js";
import { groupItemsForDepartmentPrinting } from "./tagHierarchy.js";

let cachedResources = null;
let cachedCommercialUserId = null;

function formatPrice(value) {
  const n = Number(value || 0);
  return Number.isFinite(n) ? n.toLocaleString("en-EG") : "0";
}

function isApiSuccess(response) {
  const body = response?.data;
  if (!body) return false;
  if (body.errorStatus === true || body.ErrorStatus === true) return false;
  const status = response.status ?? 0;
  return status >= 200 && status < 300;
}

export function mapPublicOrderItems(order) {
  const raw =
    order?.customerOrderItem ||
    order?.CustomerOrderItem ||
    order?.items ||
    order?.Items ||
    [];
  return raw.map((item) => {
    const qty = Number(item.quantity ?? item.Quantity ?? 0);
    const price = Number(item.sellingPrice ?? item.SellingPrice ?? 0);
    const total =
      Number(item.total ?? item.Total ?? 0) || price * qty;
    const note = item.notes ?? item.Notes ?? "";
    return {
      name: item.itemName ?? item.ItemName ?? item.name ?? item.Name ?? "",
      quantity: qty,
      price,
      disCountPrice: 0,
      total,
      tags: item.tags ?? item.Tags ?? "",
      notes: note,
      lineNote: note,
    };
  });
}

function findMainPrinter(managedPrinters) {
  const list = (managedPrinters || []).filter(
    (p) => (p.isActive ?? p.IsActive) !== false
  );
  const main = list.find((p) => p.isMain ?? p.IsMain);
  if (main) return main;
  return list[0] || null;
}

function findPublicOrderPrinter(managedPrinters) {
  const list = (managedPrinters || []).filter(
    (p) => (p.isActive ?? p.IsActive) !== false
  );
  return list.find((p) => p.isPublicOrderPrinter ?? p.IsPublicOrderPrinter) || null;
}

function findPrinterById(managedPrinters, tagPrinters, printerId) {
  if (printerId == null) return null;
  const id = String(printerId);
  const fromManaged = (managedPrinters || []).find(
    (p) => String(p.id ?? p.Id) === id
  );
  if (fromManaged) return fromManaged;
  const link = (tagPrinters || []).find((tp) => {
    const pid =
      tp.printer?.id ??
      tp.printer?.Id ??
      tp.printerId ??
      tp.PrinterId;
    return String(pid) === id;
  });
  return link?.printer ?? link?.Printer ?? null;
}

async function checkPythonServerHealth() {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 3000);
    const response = await fetch("http://localhost:5000/health", {
      method: "GET",
      signal: controller.signal,
    });
    clearTimeout(timeoutId);
    if (!response.ok) return false;
    const health = await response.json();
    return health.status === "ok";
  } catch {
    return false;
  }
}

async function printViaPythonServer(printData) {
  try {
    const serverAvailable = await checkPythonServerHealth();
    if (!serverAvailable) return false;

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000);
    const response = await fetch("http://localhost:5000/print", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(printData),
      signal: controller.signal,
    });
    clearTimeout(timeoutId);
    if (!response.ok) return false;
    const result = await response.json();
    return !!result.success;
  } catch {
    return false;
  }
}

async function printHtmlToPrinter(http, printerId, documentHtml) {
  if (!printerId || !documentHtml) return false;
  try {
    const response = await http.post(
      `Printers/${printerId}/print`,
      { htmlContent: documentHtml, copies: 1 },
      { timeout: PRINT_API_TIMEOUT_MS }
    );
    if (isApiSuccess(response)) {
      return true;
    }
    console.warn("[orderPrint] API print rejected:", response.data?.message);
  } catch (error) {
    console.warn(
      "[orderPrint] API print failed:",
      error.response?.data?.message || error.message
    );
  }
  return false;
}

async function printHtmlWithFallback(
  http,
  printerId,
  documentHtml,
  printData
) {
  const apiOk = await printHtmlToPrinter(http, printerId, documentHtml);
  if (apiOk) return true;

  const printer = printData?.printerRecord;
  if (printer) {
    const payload = {
      ...printData.fallbackPayload,
      htmlContent: documentHtml,
      printerName: printer.printerName ?? printer.PrinterName,
      printerType: printer.printerType ?? printer.PrinterType ?? "windows",
    };
    return printViaPythonServer(payload);
  }
  return printViaPythonServer({
    ...printData?.fallbackPayload,
    htmlContent: documentHtml,
  });
}

async function fetchJson(http, url, fallback = null) {
  try {
    const response = await http.get(url);
    if (isApiSuccess(response)) {
      return { ok: true, data: response.data?.data ?? fallback, message: null };
    }
    return {
      ok: false,
      data: fallback,
      message: response.data?.message || "Request failed",
    };
  } catch (error) {
    return {
      ok: false,
      data: fallback,
      message: error.response?.data?.message || error.message || "Request failed",
    };
  }
}

export async function loadPrintResources(http, commercialUserId, force = false) {
  if (
    !force &&
    cachedResources &&
    cachedCommercialUserId === commercialUserId &&
    cachedResources.printersLoadedOk !== false
  ) {
    return cachedResources;
  }

  const [printersRes, tagPrintersRes, tagsRes, menuRes] = await Promise.all([
    fetchJson(http, "Printers", []),
    fetchJson(http, "TagPrinters", []),
    fetchJson(http, "Admin/GetTags?pageNumber=0&pageSize=10000", { items: [] }),
    commercialUserId
      ? fetchJson(http, `PublicMenu/${commercialUserId}`, {})
      : Promise.resolve({ ok: true, data: {}, message: null }),
  ]);

  const managedPrinters = Array.isArray(printersRes.data) ? printersRes.data : [];
  const tagPrinters = Array.isArray(tagPrintersRes.data) ? tagPrintersRes.data : [];
  const tagsPayload = tagsRes.data;
  const tags = Array.isArray(tagsPayload)
    ? tagsPayload
    : tagsPayload?.items || tagsPayload?.Items || [];
  const menu = menuRes.data || {};

  cachedResources = {
    managedPrinters,
    tagPrinters,
    tags,
    restaurantInfo: {
      restaurantName: menu.restaurantName || menu.RestaurantName || "",
      logo: menu.logo || menu.Logo || null,
    },
    mainPrinter: findMainPrinter(managedPrinters),
    publicOrderPrinter: findPublicOrderPrinter(managedPrinters),
    printersLoadedOk: printersRes.ok,
    printersLoadMessage: printersRes.ok ? null : printersRes.message,
  };
  cachedCommercialUserId = commercialUserId;
  return cachedResources;
}

function buildPrintLabels(t) {
  const tr = typeof t === "function" ? t : (key) => key;
  return {
    invoiceNumber: tr("invoiceNumber") || "رقم الفاتورة",
    orderNumber: tr("orderNumber") || "رقم الطلب",
    orderType: tr("orderType") || "نوع الطلب",
    paymentMethod: tr("paymentMethod") || "طريقة الدفع",
    date: tr("from_date") || "التاريخ",
    customerName: tr("customerName") || "اسم العميل",
    phoneNumber: tr("phoneNumber") || "رقم الهاتف",
    address: tr("address") || "العنوان",
    notes: tr("notes") || "ملاحظات",
    itemName: tr("item_name_label") || "طبق/مشروب",
    quantity: tr("quantity_label") || "العدد",
    price: tr("selling_price_label") || "السعر",
    total: tr("total") || "المجموع",
    discountLabel: tr("discountLabel") || "الخصم",
    currency: tr("currency") || "د.ع",
    thankYou: tr("thankYouVisit") || "شكراً لزيارتكم",
    storeFallback: tr("restaurant") || "المطعم",
    dineIn: tr("dineIn") || "داخل المطعم",
    takeaway: tr("takeaway") || "خارجي",
    delivery: tr("delivery") || "توصيل",
    cash: tr("cash") || "نقدي",
    card: tr("card") || "بطاقة",
    credit: tr("credit") || "آجل",
    paymentStatus: tr("paymentStatus") || "حالة الدفع",
    paymentStatusPaid: tr("paymentStatusPaid") || "مدفوع",
    paymentStatusPending: tr("paymentStatusPending") || "غير مدفوع",
    paymentStatusRefunded: tr("paymentStatusRefunded") || "مسترد",
  };
}

function buildFallbackPayload(order, items, storeName, hidePrices) {
  const subtotal = items.reduce(
    (sum, item) => sum + (Number(item.total) || 0),
    0
  );
  const discount = Number(order.discountAmount || order.DiscountAmount || 0);
  const total =
    Number(order.orderTotalAfterDiscount ?? order.OrderTotalAfterDiscount) ||
    Math.max(0, subtotal - discount);

  return {
    storeName: storeName || "المطعم",
    storeAddress: "",
    storePhone: "",
    orderCode: order.orderCode || order.OrderCode || "",
    date: new Date().toLocaleDateString("ar-EG"),
    time: new Date().toLocaleTimeString("ar-EG"),
    tableNumber: null,
    employeeName: "",
    items: items.map((item) => ({
      name: item.name || "",
      quantity: item.quantity || 0,
      price: hidePrices ? "0" : formatPrice(item.price),
      total: hidePrices ? "0" : formatPrice(item.total),
    })),
    subtotal: hidePrices ? "0" : formatPrice(subtotal),
    discount: hidePrices ? "0" : formatPrice(discount),
    tax: "0",
    total: hidePrices ? "0" : formatPrice(total),
    paymentMethod:
      order.paymentMethod === "Cash" || order.PaymentMethod === "Cash"
        ? "نقدي"
        : order.paymentMethod === "Card" || order.PaymentMethod === "Card"
          ? "بطاقة"
          : order.paymentMethod || order.PaymentMethod || "نقدي",
  };
}

export function resolvePrintFailureMessage(result, t) {
  const tr = typeof t === "function" ? t : (key) => key;
  const errors = result?.errors || [];

  if (errors.includes("no_items")) {
    return tr("printNoOrderItems") || "لا توجد عناصر في الطلب للطباعة";
  }
  if (errors.includes("printers_load_failed")) {
    return (
      result?.printersLoadMessage ||
      tr("printPrintersLoadFailed") ||
      "تعذّر تحميل الطابعات — تحقق من الصلاحيات أو سجّل الدخول مجدداً"
    );
  }
  if (errors.includes("no_main_printer")) {
    return (
      tr("printNoMainPrinter") ||
      "لا توجد طابعة مفعّلة — أضف طابعة وحدّد الطابعة الرئيسية من إعدادات الطباعة"
    );
  }
  if (errors.includes("no_public_printer")) {
    return (
      tr("printNoPublicOrderPrinter") ||
      "لم تُعيَّن طابعة للطلبات العامة — أضفها من إعدادات الطباعة"
    );
  }
  if (errors.includes("main_printer_failed")) {
    return (
      tr("printMainPrinterFailed") ||
      "تعذّر إرسال الفاتورة للطابعة الرئيسية — تحقق من Print Server"
    );
  }
  if (errors.includes("public_printer_failed")) {
    return (
      tr("printPublicPrinterFailed") ||
      "تعذّر إرسال الإيصال لطابعة الطلبات العامة — تحقق من Print Server"
    );
  }

  return tr("printError") || "حدث خطأ أثناء الطباعة";
}

export function notifyPrintOrderResult(result, notify, t, options = {}) {
  const silent = options.silent === true;
  if (silent) return;

  if (result.ok && result.mainPrinted) {
    if (result.usedMainPrinterFallback) {
      notify.warning(
        t("printNoPublicOrderPrinter") ||
          "لم تُعيَّن طابعة للطلبات العامة — تم استخدام الطابعة الرئيسية",
        { timeout: 3500, maxToasts: 1 }
      );
      return;
    }
    notify.success(t("printOrderSuccess") || "تم إرسال الطلب للطباعة", {
      timeout: 2500,
      maxToasts: 1,
    });
    return;
  }

  if (result.ok && result.tagsPrinted > 0) {
    notify.warning(t("printOrderPartial") || "تمت الطباعة جزئياً — تحقق من الطابعات", {
      timeout: 3500,
      maxToasts: 1,
    });
    return;
  }

  notify.error(resolvePrintFailureMessage(result, t), {
    timeout: 4500,
    maxToasts: 1,
  });
}

/**
 * Print public order: customer receipt on public-order printer + kitchen tickets by tag.
 */
export async function printPublicOrderLikePos(order, options = {}) {
  const { http, commercialUserId, t } = options;
  if (!http || !order) {
    return {
      ok: false,
      mainPrinted: false,
      tagsPrinted: 0,
      errors: ["missing_params"],
    };
  }

  const items = mapPublicOrderItems(order);
  if (!items.length) {
    return { ok: false, mainPrinted: false, tagsPrinted: 0, errors: ["no_items"] };
  }

  const resources = await loadPrintResources(http, commercialUserId, true);
  const errors = [];

  if (resources.printersLoadedOk === false) {
    errors.push("printers_load_failed");
    return {
      ok: false,
      mainPrinted: false,
      tagsPrinted: 0,
      errors,
      printersLoadMessage: resources.printersLoadMessage,
    };
  }

  const labels = buildPrintLabels(t);
  const storeName = resources.restaurantInfo.restaurantName;
  const logoUrl = resources.restaurantInfo.logo;
  let mainPrinted = false;
  let tagsPrinted = 0;

  const publicOrderPrinter = resources.publicOrderPrinter;
  const customerPrinter = publicOrderPrinter || resources.mainPrinter;
  const usedMainPrinterFallback = !publicOrderPrinter && !!resources.mainPrinter;
  const customerPrinterId = customerPrinter?.id ?? customerPrinter?.Id ?? null;

  if (customerPrinterId) {
    const { documentHtml } = buildStandaloneOrderReceiptHtml({
      storeName,
      logoUrl,
      order,
      items,
      hidePrices: false,
      labels,
      formatPrice,
    });

    const fallbackPayload = buildFallbackPayload(order, items, storeName, false);
    mainPrinted = await printHtmlWithFallback(
      http,
      customerPrinterId,
      documentHtml,
      {
        printerRecord: customerPrinter,
        fallbackPayload,
      }
    );
    if (!mainPrinted) {
      errors.push(publicOrderPrinter ? "public_printer_failed" : "main_printer_failed");
    }
  } else {
    errors.push(publicOrderPrinter ? "no_public_printer" : "no_main_printer");
  }

  const grouped = groupItemsForDepartmentPrinting(
    items,
    resources.tagPrinters,
    resources.tags
  );

  for (const [groupKey, group] of Object.entries(grouped)) {
    if (groupKey === "unmapped" || !group.printerId || !group.items?.length) {
      continue;
    }

    const { documentHtml } = buildStandaloneOrderReceiptHtml({
      storeName,
      logoUrl,
      order,
      items: group.items,
      hidePrices: true,
      tagName: group.tagName,
      labels,
      formatPrice,
    });

    const printer = findPrinterById(
      resources.managedPrinters,
      resources.tagPrinters,
      group.printerId
    );

    const fallbackPayload = buildFallbackPayload(
      order,
      group.items,
      storeName,
      true
    );

    const ok = await printHtmlWithFallback(http, group.printerId, documentHtml, {
      printerRecord: printer,
      fallbackPayload,
    });

    if (ok) {
      tagsPrinted += 1;
    } else {
      errors.push(`tag_printer_${group.tagName}`);
    }
  }

  const ok = mainPrinted || tagsPrinted > 0;

  return {
    ok,
    mainPrinted,
    tagsPrinted,
    errors,
    usedMainPrinterFallback,
    printersLoadMessage: resources.printersLoadMessage,
  };
}

export async function fetchPublicOrderById(http, commercialUserId, orderId) {
  if (!http || !commercialUserId || !orderId) return null;
  try {
    const response = await http.get(
      `PublicMenu/${commercialUserId}/orders/${orderId}`
    );
    if (isApiSuccess(response)) {
      return response.data?.data ?? null;
    }
  } catch (error) {
    console.warn("[orderPrint] fetch public order failed:", error);
  }
  return null;
}

export function shouldAutoPrintPublicCardOrder(data) {
  if (!data) return false;
  const shouldPrint = data.ShouldAutoPrint ?? data.shouldAutoPrint;
  const status = data.OrderStatus ?? data.orderStatus;
  return !!shouldPrint && status === "Processing";
}

export function canPrintOrderStatus(status) {
  return ["Processing", "Ready", "Completed"].includes(status);
}

export function shouldAutoPrintOnStatusChange(previousStatus, nextStatus) {
  return previousStatus !== "Processing" && nextStatus === "Processing";
}
