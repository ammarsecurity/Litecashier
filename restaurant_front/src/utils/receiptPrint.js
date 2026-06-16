/**
 * Shared receipt print styles and HTML document builder.
 * Used for browser print and Print Server (localhost:5000) so output matches the POS preview.
 */

/** Axios timeout for direct Print Server fetch (WebView2 cold start can be slow). */
export const PRINT_SERVER_FETCH_TIMEOUT_MS = 90000;

/** Axios timeout for api/Printers/{id}/print (API returns quickly after queueing). */
export const PRINT_API_TIMEOUT_MS = 60000;

/** Local Cairo font for print HTML (Print Server serves /fonts on :5000; file:// receipts cannot use relative /fonts). */
export const RECEIPT_PRINT_CAIRO_FONT_HTML = '<link rel="stylesheet" href="http://127.0.0.1:5000/fonts/cairo.css">';

export const RECEIPT_PRINT_STYLES_HTML = `
    <style>
      @page {
        size: 72mm auto;
        margin: 2mm 4mm;
      }

      * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
      }

      body {
        font-family: 'Cairo', 'Arial', sans-serif;
        direction: rtl;
        font-size: 11px;
        line-height: 1.35;
        color: #000;
        background: #fff;
        /* extra padding-left = physical left margin (non-printable zone on thermal) */
        padding: 3mm 3mm 3mm 5mm;
        width: 72mm;
        max-width: 72mm;
        margin: 0 auto;
      }

      .bill-container {
        width: 100%;
        max-width: 100%;
        margin: 0 auto;
        padding: 0 2mm 0 3mm;
      }

      .bill-header {
        text-align: center;
        margin-bottom: 8px;
        padding-bottom: 8px;
        border-bottom: 1px dashed #000;
      }

      .bill-logo-img {
        max-width: 50px;
        height: auto;
        margin-bottom: 4px;
      }

      .bill-store-name {
        font-size: 16px;
        font-weight: 800;
        margin: 4px 0 2px 0;
        color: #000;
      }

      .bill-store-subtitle {
        font-size: 9px;
        color: #666;
        margin: 0;
      }

      .bill-info-section {
        margin: 8px 0;
        padding: 0 1mm;
        font-size: 10px;
      }

      .bill-info-row {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
        align-items: flex-start;
        gap: 6px;
        margin-bottom: 4px;
        padding: 0 1px;
      }

      .bill-info-label {
        flex: 0 0 44%;
        max-width: 44%;
        font-weight: 600;
        line-height: 1.35;
      }

      .bill-info-value {
        flex: 1 1 auto;
        min-width: 0;
        font-weight: 400;
        text-align: right;
        padding-left: 2mm;
        word-break: break-word;
        overflow-wrap: anywhere;
        line-height: 1.35;
      }

      .bill-barcode-section {
        text-align: center;
        margin: 8px 0;
        padding: 4px 0;
      }

      .bill-barcode-img {
        max-width: 100%;
        height: auto;
        display: block;
        margin: 0 auto;
      }

      .bill-divider {
        border-top: 1px dashed #000;
        margin: 8px 0;
      }

      .bill-items-section {
        margin: 8px 0;
        padding: 0 1mm;
        overflow: hidden;
      }

      .bill-items-table {
        width: 100%;
        table-layout: fixed;
        border-collapse: collapse;
        font-size: 9px;
      }

      .bill-items-table thead {
        border-bottom: 1px solid #000;
      }

      .bill-items-table th {
        padding: 4px 3px;
        text-align: right;
        font-weight: 700;
        font-size: 8px;
        line-height: 1.2;
        word-break: break-word;
      }

      .bill-item-name-col {
        width: 32%;
      }

      .bill-item-qty-col {
        width: 13%;
        text-align: center;
      }

      .bill-item-price-col {
        width: 22%;
        text-align: center;
      }

      .bill-item-total-col {
        width: 33%;
        text-align: right;
        padding-left: 2mm;
      }

      .bill-items-table td {
        padding: 4px 3px;
        vertical-align: top;
        line-height: 1.25;
      }

      .bill-item-name {
        font-weight: 500;
        word-break: break-word;
      }

      .bill-discount-badge {
        display: block;
        font-size: 7px;
        color: #dc2626;
        font-weight: 600;
        margin-top: 2px;
      }

      .bill-item-qty {
        text-align: center;
        font-weight: 600;
      }

      .bill-item-price {
        text-align: center;
        font-size: 8px;
        word-break: break-word;
      }

      .bill-price-discounted {
        display: block;
      }

      .bill-original-price {
        display: block;
        text-decoration: line-through;
        color: #999;
        font-size: 8px;
      }

      .bill-discount-price {
        display: block;
        color: #dc2626;
        font-weight: 600;
      }

      .bill-item-total {
        text-align: right;
        font-weight: 700;
        font-size: 8px;
        padding-left: 2mm;
        word-break: break-word;
        overflow-wrap: anywhere;
      }

      .bill-items-table--kitchen .bill-item-name-col {
        width: 72%;
      }

      .bill-items-table--kitchen .bill-item-qty-col {
        width: 28%;
        text-align: center;
      }

      .bill-item-line-note {
        display: block;
        margin-top: 3px;
        font-size: 8px;
        font-weight: 600;
        color: #333;
        line-height: 1.3;
        word-break: break-word;
      }

      .bill-summary-section {
        margin: 8px 0;
        padding: 0 1mm;
        font-size: 11px;
      }

      .bill-summary-row {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
        align-items: flex-start;
        gap: 6px;
        margin-bottom: 4px;
        padding: 0 1px;
      }

      .bill-summary-label {
        flex: 0 0 44%;
        max-width: 44%;
        font-weight: 600;
        line-height: 1.35;
      }

      .bill-summary-value {
        flex: 1 1 auto;
        min-width: 0;
        font-weight: 400;
        text-align: right;
        padding-left: 2mm;
        word-break: break-word;
        overflow-wrap: anywhere;
        line-height: 1.35;
      }

      .bill-summary-total {
        border-top: 1px solid #000;
        padding-top: 4px;
        margin-top: 4px;
        font-size: 12px;
      }

      .bill-summary-total .bill-summary-label {
        font-weight: 700;
        font-size: 13px;
      }

      .bill-summary-total .bill-summary-value {
        font-weight: 800;
        font-size: 13px;
      }

      .bill-notes-section {
        margin-top: 12px;
        padding-top: 8px;
      }

      .bill-notes-content {
        margin-bottom: 8px;
        padding: 6px 0;
      }

      .bill-notes-label {
        font-weight: 600;
        font-size: 10px;
        margin-bottom: 4px;
        color: #000;
      }

      .bill-notes-text {
        font-size: 10px;
        color: #333;
        line-height: 1.4;
        word-wrap: break-word;
      }

      .bill-footer {
        text-align: center;
        margin-top: 12px;
        padding-top: 8px;
        border-top: 1px dashed #000;
      }

      .bill-footer-text {
        font-size: 9px;
        margin: 2px 0;
        color: #666;
      }

      @media print {
        body {
          width: 72mm !important;
          max-width: 72mm !important;
          padding: 3mm 3mm 3mm 5mm !important;
        }

        .bill-container {
          width: 100% !important;
          max-width: 100% !important;
          padding: 0 2mm 0 3mm !important;
        }

        .bill-info-section,
        .bill-items-section,
        .bill-summary-section {
          padding: 0 1mm 0 2mm !important;
        }
      }
    </style>
`;

/**
 * @param {string} innerHtml - Receipt body HTML (#print innerHTML or fragment)
 * @param {string} [title='Receipt']
 * @returns {string} Full HTML document for Print Server / browser print
 */
export function buildReceiptPrintDocument(innerHtml, title = 'Receipt') {
  const body = innerHtml || '';
  const trimmed = body.trim();
  if (/^<!DOCTYPE/i.test(trimmed) || /^<html/i.test(trimmed)) {
    if (!/<style[\s>]/i.test(body)) {
      return body.replace(/<head([^>]*)>/i, `<head$1>${RECEIPT_PRINT_CAIRO_FONT_HTML}${RECEIPT_PRINT_STYLES_HTML}`);
    }
    return body;
  }

  const safeTitle = String(title)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');

  return `<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>${safeTitle}</title>
  ${RECEIPT_PRINT_CAIRO_FONT_HTML}
  ${RECEIPT_PRINT_STYLES_HTML}
</head>
<body>
${body}
</body>
</html>`;
}

/**
 * @param {HTMLElement|null|undefined} printElement
 * @param {string} [title]
 * @returns {string}
 */
export function getReceiptHtmlFromElement(printElement, title = 'Receipt') {
  if (!printElement) return '';
  return buildReceiptPrintDocument(printElement.innerHTML, title);
}

/**
 * Totals for a kitchen/department print group (proportional order discount).
 */
export function computeGroupPrintTotals(items, orderSubtotal, orderDiscountAmount) {
  const subtotal = (items || []).reduce(
    (sum, item) => sum + (Number(item.total) || 0),
    0
  );
  const orderSub = Number(orderSubtotal) || 0;
  const orderDisc = Number(orderDiscountAmount) || 0;
  let groupDiscount = 0;
  if (orderDisc > 0 && orderSub > 0 && subtotal > 0) {
    groupDiscount = Math.round(orderDisc * (subtotal / orderSub));
  }
  const groupTotal = Math.max(0, subtotal - groupDiscount);
  const totalItems = (items || []).reduce(
    (sum, item) => sum + (Number(item.quantity) || 0),
    0
  );
  return { subtotal, groupDiscount, groupTotal, totalItems };
}

function receiptLineUnitPrice(item) {
  const price = Number(item?.price || 0);
  const discount = Number(item?.disCountPrice || 0);
  return discount > 0 && discount !== price ? discount : price;
}

function receiptLineNote(item) {
  const raw = item?.lineNote ?? item?.notes ?? item?.Notes;
  if (raw == null) return "";
  return String(raw).trim();
}

function formatReceiptItemNameCell(item, escapeHtml, includeLineNote) {
  const name = escapeHtml(item?.name || "");
  if (!includeLineNote) return name;
  const note = receiptLineNote(item);
  if (!note) return name;
  return `${name}<div class="bill-item-line-note">${escapeHtml(note)}</div>`;
}

/**
 * Build items table HTML for receipt / kitchen department prints.
 */
export function buildReceiptItemsTableHtml({
  items = [],
  labels = {},
  escapeHtml = (t) => String(t ?? ""),
  formatPrice = (n) => String(n),
  hidePrices = false,
} = {}) {
  const itemName = labels.itemName || "طبق/مشروب";
  const quantity = labels.quantity || "العدد";
  const priceLabel = labels.price || "السعر";
  const totalLabel = labels.total || "المجموع";
  const tableClass = hidePrices
    ? "bill-items-table bill-items-table--kitchen"
    : "bill-items-table";

  let html = `
        <table class="${tableClass}">
          <thead>
            <tr>
              <th class="bill-item-name-col">${itemName}</th>
              <th class="bill-item-qty-col">${quantity}</th>`;

  if (!hidePrices) {
    html += `
              <th class="bill-item-price-col">${priceLabel}</th>
              <th class="bill-item-total-col">${totalLabel}</th>`;
  }

  html += `
            </tr>
          </thead>
          <tbody>
      `;

  for (const item of items) {
    const unitPrice = receiptLineUnitPrice(item);
    const nameCell = formatReceiptItemNameCell(item, escapeHtml, hidePrices);
    html += `
          <tr>
            <td class="bill-item-name">${nameCell}</td>
            <td class="bill-item-qty">${item.quantity || 0}</td>`;
    if (!hidePrices) {
      html += `
            <td class="bill-item-price">${unitPrice ? formatPrice(unitPrice) : "0"}</td>
            <td class="bill-item-total">${item.total ? formatPrice(item.total) : "0"}</td>`;
    }
    html += `
          </tr>
        `;
  }

  html += `
          </tbody>
        </table>
      `;
  return html;
}

/**
 * Build summary block HTML for receipt / kitchen department prints.
 */
export function buildReceiptSummaryHtml({
  totalItems = 0,
  tagName = null,
  hidePrices = false,
  groupDiscount = 0,
  groupTotal = 0,
  labels = {},
  formatPrice = (n) => String(n),
  escapeHtml = (t) => String(t ?? ""),
} = {}) {
  const countLabel = labels.countLabel || "العدد:";
  const countSuffix = labels.countSuffix || " طبق/مشروب";
  const sectionLabel = labels.sectionLabel || "القسم:";
  const discountLabel = labels.discountLabel || "الخصم";
  const totalLabel = labels.totalLabel || "المجموع:";
  const currency = labels.currency || "";

  const showTag =
    tagName && tagName !== "default" && tagName !== "unmapped";

  let html = `
        <div data-v-f8758d62="" class="bill-summary-section">
          <div data-v-f8758d62="" class="bill-summary-row">
            <span data-v-f8758d62="" class="bill-summary-label">${countLabel}</span>
            <span data-v-f8758d62="" class="bill-summary-value">${totalItems}${countSuffix}</span>
          </div>`;

  if (showTag) {
    html += `
          <div data-v-f8758d62="" class="bill-summary-row">
            <span data-v-f8758d62="" class="bill-summary-label">${sectionLabel}</span>
            <span data-v-f8758d62="" class="bill-summary-value">${escapeHtml(tagName)}</span>
          </div>`;
  }

  if (!hidePrices) {
    if (groupDiscount > 0) {
      html += `
          <div data-v-f8758d62="" class="bill-summary-row">
            <span data-v-f8758d62="" class="bill-summary-label">${discountLabel}:</span>
            <span data-v-f8758d62="" class="bill-summary-value">- ${formatPrice(groupDiscount)} ${currency}</span>
          </div>`;
    }
    html += `
          <div data-v-f8758d62="" class="bill-summary-row bill-total-row">
            <span data-v-f8758d62="" class="bill-summary-label">${totalLabel}</span>
            <span data-v-f8758d62="" class="bill-summary-value bill-total-amount">${formatPrice(groupTotal)} ${currency}</span>
          </div>`;
  }

  html += `
        </div>
      `;
  return html;
}

/**
 * Replace the full bill-summary-section block (nested div-safe).
 */
export function replaceReceiptSummarySection(htmlContent, summaryHTML) {
  if (!htmlContent || !summaryHTML) return htmlContent;

  const classPattern = /class="[^"]*\bbill-summary-section\b[^"]*"/i;
  const match = classPattern.exec(htmlContent);
  if (!match) return htmlContent;

  let start = htmlContent.lastIndexOf("<div", match.index);
  if (start === -1) return htmlContent;

  let depth = 0;
  let i = start;
  const len = htmlContent.length;

  while (i < len) {
    const openDiv = htmlContent.indexOf("<div", i);
    const closeDiv = htmlContent.indexOf("</div>", i);
    if (closeDiv === -1) break;

    if (openDiv !== -1 && openDiv < closeDiv) {
      depth += 1;
      i = openDiv + 4;
    } else {
      depth -= 1;
      i = closeDiv + 6;
      if (depth === 0) {
        return (
          htmlContent.slice(0, start) + summaryHTML + htmlContent.slice(i)
        );
      }
    }
  }

  return htmlContent;
}

/**
 * Remove leftover price/total rows from kitchen department receipt HTML.
 */
export function stripKitchenFinancialFromReceiptHtml(htmlContent) {
  if (!htmlContent) return htmlContent;

  let html = htmlContent;

  html = html.replace(
    /<div[^>]*class="[^"]*\bbill-summary-total\b[^"]*"[^>]*>[\s\S]*?<\/div>\s*/gi,
    ""
  );
  html = html.replace(
    /<div[^>]*class="[^"]*\bbill-total-row\b[^"]*"[^>]*>[\s\S]*?<\/div>\s*/gi,
    ""
  );
  html = html.replace(
    /<div[^>]*class="bill-summary-row"[^>]*>[\s\S]*?(?:discountLabel|الخصم|discount)[\s\S]*?<\/div>\s*/gi,
    ""
  );
  html = html.replace(
    /<th[^>]*class="[^"]*\bbill-item-price-col\b[^"]*"[^>]*>[\s\S]*?<\/th>\s*/gi,
    ""
  );
  html = html.replace(
    /<th[^>]*class="[^"]*\bbill-item-total-col\b[^"]*"[^>]*>[\s\S]*?<\/th>\s*/gi,
    ""
  );
  html = html.replace(
    /<td[^>]*class="[^"]*\bbill-item-price\b[^"]*"[^>]*>[\s\S]*?<\/td>\s*/gi,
    ""
  );
  html = html.replace(
    /<td[^>]*class="[^"]*\bbill-item-total\b[^"]*"[^>]*>[\s\S]*?<\/td>\s*/gi,
    ""
  );

  return html;
}

/**
 * Ensure table number appears in cloned receipt HTML (for department prints).
 */
export function ensurePrintTableNumberInHtml(
  htmlContent,
  tableNumber,
  tableNumberLabel,
  escapeHtmlFn
) {
  if (!tableNumber || !htmlContent) return htmlContent;
  const label = String(tableNumberLabel || "رقم الطاولة");
  const escapedLabel = label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  if (new RegExp(escapedLabel, "i").test(htmlContent)) {
    return htmlContent;
  }
  const safeValue = escapeHtmlFn
    ? escapeHtmlFn(String(tableNumber))
    : String(tableNumber);
  const row = `
          <div class="bill-info-row">
            <span class="bill-info-label">${label}:</span>
            <span class="bill-info-value">${safeValue}</span>
          </div>
  `;
  const afterEmployee =
    /(<div[^>]*class="bill-info-row"[^>]*>[\s\S]*?employeeLabel[\s\S]*?<\/div>\s*)/i;
  if (afterEmployee.test(htmlContent)) {
    return htmlContent.replace(afterEmployee, `$1${row}`);
  }
  return htmlContent;
}

/**
 * Ensure invoice/order code appears in cloned receipt HTML (fixes empty "---" on print-only).
 */
export function ensurePrintOrderCodeInHtml(htmlContent, orderCode, escapeHtmlFn) {
  if (!orderCode || !htmlContent) return htmlContent;
  const safeCode = escapeHtmlFn
    ? escapeHtmlFn(String(orderCode))
    : String(orderCode);
  const trimmed = String(orderCode).trim();
  if (!trimmed) return htmlContent;

  let updated = htmlContent.replace(
    /(<div[^>]*class="bill-info-row"[^>]*>[\s\S]*?class="bill-info-value"[^>]*>)\s*---\s*(<\/span>)/i,
    `$1${safeCode}$2`
  );

  const emptyValueAfterFirstRow =
    /(<div[^>]*class="bill-info-row"[^>]*>[\s\S]*?class="bill-info-value"[^>]*>)\s*(<\/span>)/i;
  if (updated === htmlContent && emptyValueAfterFirstRow.test(htmlContent)) {
    updated = htmlContent.replace(
      emptyValueAfterFirstRow,
      `$1${safeCode}$2`
    );
  }

  return updated;
}

function defaultEscapeHtml(text) {
  return String(text ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function defaultFormatPrice(value) {
  const n = Number(value || 0);
  return Number.isFinite(n) ? n.toLocaleString("en-EG") : "0";
}

function paymentMethodLabel(method, labels = {}) {
  if (!method) return "—";
  const map = {
    Cash: labels.cash || "نقدي",
    Card: labels.card || "بطاقة",
    Credit: labels.credit || "آجل",
  };
  return map[method] || method;
}

function orderTypeLabel(type, labels = {}) {
  if (!type) return "—";
  const map = {
    DineIn: labels.dineIn || "داخل المطعم",
    Takeaway: labels.takeaway || "خارجي",
    Delivery: labels.delivery || "توصيل",
  };
  return map[type] || type;
}

/**
 * Build a full receipt body (bill-container) without relying on #print DOM.
 */
export function buildStandaloneOrderReceiptHtml({
  storeName = "",
  logoUrl = null,
  order = {},
  items = [],
  hidePrices = false,
  tagName = null,
  labels = {},
  escapeHtml = defaultEscapeHtml,
  formatPrice = defaultFormatPrice,
} = {}) {
  const currency = labels.currency || "د.ع";
  const orderSubtotal =
    Number(order.orderSubTotal) ||
    items.reduce((sum, item) => sum + (Number(item.total) || 0), 0);
  const orderDiscount = Number(order.discountAmount || order.DiscountAmount || 0);
  const orderTotal =
    Number(order.orderTotalAfterDiscount) ||
    Math.max(0, orderSubtotal - orderDiscount);

  const { groupDiscount, groupTotal, totalItems } = computeGroupPrintTotals(
    items,
    orderSubtotal,
    orderDiscount
  );

  const receiptLabels = {
    itemName: labels.itemName || "طبق/مشروب",
    quantity: labels.quantity || "العدد",
    price: labels.price || "السعر",
    total: labels.total || "المجموع",
    countLabel: labels.countLabel || "العدد:",
    countSuffix: labels.countSuffix || " طبق/مشروب",
    sectionLabel: labels.sectionLabel || "القسم:",
    discountLabel: labels.discountLabel || "الخصم",
    totalLabel: labels.totalLabel || `${labels.total || "المجموع"}:`,
    currency,
  };

  const now = new Date();
  const dateTime = now.toLocaleString("ar-IQ", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });

  const infoRows = [];
  const pushRow = (label, value) => {
    if (value == null || value === "") return;
    infoRows.push(`
          <div class="bill-info-row">
            <span class="bill-info-label">${escapeHtml(label)}:</span>
            <span class="bill-info-value">${escapeHtml(String(value))}</span>
          </div>`);
  };

  pushRow(labels.invoiceNumber || "رقم الفاتورة", order.orderCode);
  if (order.dailySequenceNumber) {
    pushRow(labels.orderNumber || "رقم الطلب", `#${order.dailySequenceNumber}`);
  }
  pushRow(labels.orderType || "نوع الطلب", orderTypeLabel(order.orderType, labels));
  pushRow(labels.paymentMethod || "طريقة الدفع", paymentMethodLabel(order.paymentMethod, labels));
  pushRow(labels.date || "التاريخ", dateTime);

  if (order.orderType === "Delivery") {
    pushRow(labels.customerName || "اسم العميل", order.deliveryCustomerName);
    pushRow(labels.phoneNumber || "رقم الهاتف", order.deliveryPhoneNumber);
    pushRow(labels.address || "العنوان", order.deliveryAddress);
  }

  if (order.notes) {
    pushRow(labels.notes || "ملاحظات", order.notes);
  }

  const logoHtml = logoUrl
    ? `<img src="${escapeHtml(logoUrl)}" alt="" class="bill-logo-img" />`
    : "";

  const itemsTableHtml = buildReceiptItemsTableHtml({
    items,
    labels: receiptLabels,
    escapeHtml,
    formatPrice,
    hidePrices,
  });

  const summaryTotal = hidePrices ? 0 : tagName ? groupTotal : orderTotal;
  const summaryDiscount = hidePrices ? 0 : tagName ? groupDiscount : orderDiscount;

  const summaryHtml = buildReceiptSummaryHtml({
    totalItems,
    tagName,
    hidePrices,
    groupDiscount: summaryDiscount,
    groupTotal: summaryTotal,
    labels: receiptLabels,
    formatPrice,
    escapeHtml,
  });

  const titleSuffix = tagName ? ` - ${tagName}` : "";
  const docTitle = `${labels.invoiceNumber || "فاتورة"}${titleSuffix}`;

  const innerHtml = `
  <div class="bill-container">
    <div class="bill-header">
      ${logoHtml}
      <h2 class="bill-store-name">${escapeHtml(storeName || labels.storeFallback || "المطعم")}</h2>
    </div>
    <div class="bill-info-section">
      ${infoRows.join("")}
    </div>
    <div class="bill-divider"></div>
    <div class="bill-items-section">
      ${itemsTableHtml}
    </div>
    <div class="bill-divider"></div>
    ${summaryHtml}
    <div class="bill-footer">
      <p>${escapeHtml(labels.thankYou || "شكراً لزيارتكم")}</p>
    </div>
  </div>`;

  return {
    innerHtml,
    documentHtml: buildReceiptPrintDocument(innerHtml, docTitle),
    title: docTitle,
  };
}
