import { HTTP } from "@/http/api.js";
import { resolveAbsoluteAssetUrl } from "@/utils/apiBase.js";
import {
  PRINT_API_TIMEOUT_MS,
  buildReceiptPrintDocument,
} from "@/utils/receiptPrint.js";

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function formatPrice(value) {
  return (Number(value) || 0).toLocaleString("en-US", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

function orderLines(order) {
  return (order?.items || order?.Items || []).map((line) => ({
    name: line.name || line.Name || "—",
    quantity: Number(line.quantity ?? line.Quantity ?? 0),
    sellingPrice: Number(line.sellingPrice ?? line.SellingPrice ?? 0),
    total: Number(line.total ?? line.Total ?? 0),
  }));
}

export function buildPublicOrderReceiptHtml(order, commercialUserInfo = {}, t = (k) => k) {
  const storeName =
    commercialUserInfo.storeName || commercialUserInfo.StoreName || "LiteCashier";
  const logo = resolveAbsoluteAssetUrl(
    commercialUserInfo.logo || commercialUserInfo.Logo
  );
  const footerText =
    commercialUserInfo.footerCreditText || commercialUserInfo.FooterCreditText || "";
  const footerPhone =
    commercialUserInfo.footerCreditPhone || commercialUserInfo.FooterCreditPhone || "";
  const lines = orderLines(order);
  const total =
    Number(order.orderTotalAfterDiscount ?? order.OrderTotalAfterDiscount) ||
    lines.reduce((sum, l) => sum + (l.total || l.sellingPrice * l.quantity), 0);
  const code = order.orderCode || order.OrderCode || "";
  const customerName = order.customerName || order.CustomerName || "";
  const customerPhone = order.customerPhone || order.CustomerPhone || "";
  const notes = String(order.notes || order.Notes || "").trim();
  const when = order.insertDate || order.InsertDate || "";
  const dateText = when
    ? new Date(when).toLocaleString("en-GB", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "";

  const rows = lines
    .map(
      (line) => `
        <tr>
          <td class="bill-item-name">${escapeHtml(line.name)}</td>
          <td class="bill-item-qty">${line.quantity}</td>
          <td class="bill-item-price">${formatPrice(line.sellingPrice)}</td>
          <td class="bill-item-total">${formatPrice(line.total || line.sellingPrice * line.quantity)}</td>
        </tr>`
    )
    .join("");

  const inner = `
    <div class="bill-container">
      <header class="bill-header">
        ${logo ? `<img class="bill-logo-img" src="${escapeHtml(logo)}" alt="" />` : ""}
        <h2 class="bill-store-name">${escapeHtml(storeName)}</h2>
        <p class="bill-store-subtitle">${escapeHtml(t("publicMenuInvoice") || "فاتورة منيو إلكتروني")}</p>
      </header>
      <section class="bill-info-section">
        <div>${escapeHtml(t("orderCode") || "رقم الطلب")}: <strong>${escapeHtml(code)}</strong></div>
        <div>${escapeHtml(dateText)}</div>
        ${customerName ? `<div>${escapeHtml(t("customerName") || "الزبون")}: ${escapeHtml(customerName)}</div>` : ""}
        ${customerPhone ? `<div>${escapeHtml(t("phone") || "الهاتف")}: ${escapeHtml(customerPhone)}</div>` : ""}
        ${
          notes
            ? `<div class="bill-notes"><strong>${escapeHtml(
                t("publicMenuNotes") || "الملاحظات والعنوان"
              )}:</strong><div>${escapeHtml(notes)}</div></div>`
            : ""
        }
      </section>
      <table class="bill-items-table">
        <thead>
          <tr>
            <th class="bill-item-name-col">${escapeHtml(t("itemName") || "الصنف")}</th>
            <th class="bill-item-qty-col">${escapeHtml(t("quantity") || "العدد")}</th>
            <th class="bill-item-price-col">${escapeHtml(t("price") || "السعر")}</th>
            <th class="bill-item-total-col">${escapeHtml(t("total") || "المجموع")}</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
      <div class="bill-summary">
        <div class="bill-summary-row">
          <span>${escapeHtml(t("total") || "المجموع")}</span>
          <strong>${formatPrice(total)} ${escapeHtml(t("currency") || "")}</strong>
        </div>
        <div class="bill-summary-row">
          <span>${escapeHtml(t("paymentMethod") || "الدفع")}</span>
          <span>${escapeHtml(t("cash") || "نقدي")}</span>
        </div>
      </div>
      ${
        footerText || footerPhone
          ? `<footer class="bill-footer">${escapeHtml(footerText)}${
              footerPhone ? `<div>${escapeHtml(footerPhone)}</div>` : ""
            }</footer>`
          : ""
      }
    </div>`;

  return buildReceiptPrintDocument(inner, code || "Receipt");
}

export async function printApprovedPublicOrder(order, commercialUserInfo, t) {
  const html = buildPublicOrderReceiptHtml(order, commercialUserInfo, t);
  let printerId = null;
  try {
    const def = await HTTP.get("Printers/my-default");
    printerId = def?.data?.data?.id ?? def?.data?.data?.Id ?? null;
  } catch (_) {
    /* ignore */
  }
  if (!printerId) {
    try {
      const list = await HTTP.get("Printers");
      const printers = list?.data?.data || list?.data || [];
      const arr = Array.isArray(printers) ? printers : printers.items || [];
      const main =
        arr.find((p) => (p.isMain ?? p.IsMain) && (p.isActive ?? p.IsActive) !== false) ||
        arr.find((p) => (p.isActive ?? p.IsActive) !== false) ||
        arr[0];
      printerId = main?.id ?? main?.Id ?? null;
    } catch (_) {
      /* ignore */
    }
  }
  if (printerId) {
    const response = await HTTP.post(
      `Printers/${printerId}/print`,
      { htmlContent: html, copies: 1 },
      { timeout: PRINT_API_TIMEOUT_MS }
    );
    if (response.data && !response.data.errorStatus) return true;
  }

  const win = window.open("", "_blank", "width=420,height=720");
  if (!win) return false;
  win.document.write(html);
  win.document.close();
  setTimeout(() => {
    win.focus();
    win.print();
    setTimeout(() => win.close(), 400);
  }, 350);
  return true;
}
