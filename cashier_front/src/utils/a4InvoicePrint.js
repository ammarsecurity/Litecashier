import { RECEIPT_PRINT_CAIRO_FONT_HTML } from "@/utils/receiptPrint.js";

/**
 * Resolve invoice status badge for A4 print.
 * @returns {{ key: string, label: string }}
 */
export function resolveInvoiceStatus(data, t = (k) => k) {
  const method = String(data?.paymentMethod || "").toLowerCase();
  const status = String(data?.paymentStatus || "").toLowerCase();

  if (status === "paid" || status === "refunded") {
    return {
      key: status,
      label:
        status === "refunded"
          ? t("invoiceStatusRefunded") || "مسترجعة"
          : t("invoiceStatusPaid") || "مدفوعة",
    };
  }
  if (method === "credit") {
    return {
      key: "credit",
      label: t("invoiceStatusCredit") || "آجل",
    };
  }
  if (data?.isCheckout) {
    return {
      key: "paid",
      label: t("invoiceStatusPaid") || "مدفوعة",
    };
  }
  return {
    key: "pending",
    label: t("invoiceStatusPending") || "قيد الانتظار",
  };
}

function esc(text) {
  return String(text ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function formatMoney(value) {
  const n = Number(value) || 0;
  return n.toLocaleString("en-US", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

/**
 * Build a professional A4 invoice HTML document.
 * @param {object} data
 */
export function buildA4InvoicePrintDocument(data = {}) {
  const t = typeof data.t === "function" ? data.t : (k) => k;
  const status = resolveInvoiceStatus(data, t);
  const storeName = esc(data.storeName || "LiteCashier");
  const logoHtml = data.logoUrl
    ? `<img class="a4-logo" src="${esc(data.logoUrl)}" alt="logo" />`
    : `<div class="a4-logo-fallback">${storeName.slice(0, 1)}</div>`;

  const lines = Array.isArray(data.lines) ? data.lines : [];
  const rowsHtml = lines
    .map((line, idx) => {
      const qty = Number(line.quantity) || 0;
      const unit = Number(line.unitPrice) || 0;
      const total = unit * qty;
      return `
        <tr>
          <td class="col-idx">${idx + 1}</td>
          <td class="col-name">
            ${esc(line.name)}
            ${line.hasDiscount ? `<span class="disc-tag">${esc(t("discountLabel") || "خصم")}</span>` : ""}
          </td>
          <td class="col-qty">${qty}</td>
          <td class="col-price">${formatMoney(unit)}</td>
          <td class="col-total">${formatMoney(total)}</td>
        </tr>`;
    })
    .join("");

  const creditRow =
    String(data.paymentMethod || "").toLowerCase() === "credit" && data.creditCustomerName
      ? `<div class="meta-row"><span>${esc(t("creditAccount") || "حساب آجل")}</span><strong>${esc(data.creditCustomerName)}</strong></div>`
      : "";

  const discountRow =
    Number(data.discountAmount) > 0
      ? `<div class="totals-row"><span>${esc(t("discountLabel") || "الخصم")}</span><strong>− ${formatMoney(data.discountAmount)} ${esc(data.currency || "")}</strong></div>`
      : "";

  const notesBlock = data.notes
    ? `<section class="a4-notes"><h3>${esc(t("notesLabel") || "ملاحظات")}</h3><p>${esc(data.notes)}</p></section>`
    : "";

  const title = `${t("invoice_number") || "فاتورة"} - ${data.orderCode || ""}`.trim();

  return `<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
  <meta charset="utf-8" />
  <title>${esc(title)}</title>
  ${RECEIPT_PRINT_CAIRO_FONT_HTML}
  <style>
    @page { size: A4; margin: 14mm 12mm; }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: 'Cairo', 'Segoe UI', Tahoma, sans-serif;
      color: #1a1f24;
      background: #fff;
      font-size: 12px;
      line-height: 1.45;
      direction: rtl;
    }
    .sheet { width: 100%; max-width: 186mm; margin: 0 auto; }
    .a4-top {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 16px;
      padding-bottom: 14px;
      border-bottom: 2px solid #0f6e6e;
      margin-bottom: 16px;
    }
    .brand { display: flex; gap: 12px; align-items: center; }
    .a4-logo { width: 64px; height: 64px; object-fit: contain; border-radius: 10px; }
    .a4-logo-fallback {
      width: 64px; height: 64px; border-radius: 10px;
      background: linear-gradient(145deg, #0f6e6e, #14919b);
      color: #fff; display: flex; align-items: center; justify-content: center;
      font-size: 28px; font-weight: 800;
    }
    .brand h1 { font-size: 22px; font-weight: 800; color: #0f6e6e; margin-bottom: 2px; }
    .brand p { font-size: 11px; color: #667085; }
    .status-box { text-align: left; min-width: 150px; }
    .status-chip {
      display: inline-block; padding: 6px 14px; border-radius: 999px;
      font-weight: 800; font-size: 12px; letter-spacing: 0.2px;
      border: 1px solid transparent;
    }
    .status-chip.paid { background: #e8f8ef; color: #0f7a3c; border-color: #b7ebc9; }
    .status-chip.credit { background: #fff7e6; color: #ad6800; border-color: #ffe58f; }
    .status-chip.pending { background: #f2f4f7; color: #475467; border-color: #d0d5dd; }
    .status-chip.refunded { background: #fef3f2; color: #b42318; border-color: #fecdca; }
    .invoice-title {
      margin-top: 10px; font-size: 13px; color: #475467; font-weight: 600;
    }
    .invoice-code {
      font-size: 18px; font-weight: 800; color: #101828; margin-top: 2px;
    }
    .meta-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px 24px;
      margin: 16px 0 18px;
      padding: 12px 14px;
      background: #f8fafc;
      border: 1px solid #e4e7ec;
      border-radius: 10px;
    }
    .meta-row {
      display: flex; justify-content: space-between; gap: 10px;
      font-size: 12px; padding: 2px 0;
    }
    .meta-row span { color: #667085; }
    .meta-row strong { color: #101828; font-weight: 700; }
    table.items {
      width: 100%; border-collapse: collapse; margin-bottom: 16px;
    }
    table.items thead th {
      background: #0f6e6e; color: #fff; font-weight: 700;
      padding: 9px 8px; font-size: 11px; text-align: center;
    }
    table.items thead th.col-name { text-align: right; }
    table.items tbody td {
      padding: 9px 8px; border-bottom: 1px solid #eaecf0;
      text-align: center; vertical-align: middle;
    }
    table.items tbody td.col-name { text-align: right; font-weight: 600; }
    table.items tbody tr:nth-child(even) { background: #f9fafb; }
    .disc-tag {
      display: inline-block; margin-inline-start: 6px;
      font-size: 10px; padding: 1px 6px; border-radius: 6px;
      background: #fff1f0; color: #cf1322; font-weight: 700;
    }
    .col-idx { width: 36px; color: #98a2b3; }
    .col-qty { width: 60px; }
    .col-price, .col-total { width: 90px; }
    .bottom {
      display: flex; justify-content: space-between; gap: 20px;
      align-items: flex-start; margin-top: 8px;
    }
    .totals {
      min-width: 260px; margin-inline-start: auto;
      border: 1px solid #e4e7ec; border-radius: 10px; overflow: hidden;
    }
    .totals-row {
      display: flex; justify-content: space-between;
      padding: 9px 12px; border-bottom: 1px solid #f2f4f7;
      font-size: 12px;
    }
    .totals-row:last-child { border-bottom: 0; }
    .totals-row.grand {
      background: #0f6e6e; color: #fff; font-size: 14px; font-weight: 800;
    }
    .a4-notes {
      margin-top: 16px; padding: 12px; border-radius: 10px;
      background: #fffbeb; border: 1px solid #fde68a;
    }
    .a4-notes h3 { font-size: 12px; margin-bottom: 4px; color: #92400e; }
    .a4-notes p { color: #78350f; white-space: pre-wrap; }
    .a4-footer {
      margin-top: 28px; padding-top: 12px;
      border-top: 1px dashed #d0d5dd; text-align: center; color: #98a2b3; font-size: 10px;
    }
    .a4-footer .thanks { color: #0f6e6e; font-weight: 700; font-size: 12px; margin-bottom: 4px; }
    @media print {
      body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    }
  </style>
</head>
<body>
  <div class="sheet">
    <header class="a4-top">
      <div class="brand">
        ${logoHtml}
        <div>
          <h1>${storeName}</h1>
          <p>${esc(data.appName || t("app-name") || "LiteCashier")}</p>
        </div>
      </div>
      <div class="status-box">
        <div class="status-chip ${esc(status.key)}">${esc(status.label)}</div>
        <div class="invoice-title">${esc(t("taxInvoice") || "فاتورة مبيعات")}</div>
        <div class="invoice-code">#${esc(data.orderCode || "---")}</div>
      </div>
    </header>

    <section class="meta-grid">
      <div class="meta-row"><span>${esc(t("from_date") || "التاريخ")}</span><strong>${esc(data.dateTime || "")}</strong></div>
      <div class="meta-row"><span>${esc(t("employeeLabel") || "الموظف")}</span><strong>${esc(data.employeeName || "---")}</strong></div>
      <div class="meta-row"><span>${esc(t("paymentMethod") || "طريقة الدفع")}</span><strong>${esc(data.paymentMethodLabel || data.paymentMethod || "---")}</strong></div>
      <div class="meta-row"><span>${esc(t("priceModeLabel") || "نوع السعر")}</span><strong>${esc(data.priceModeLabel || "---")}</strong></div>
      ${creditRow}
      ${data.warehouseName ? `<div class="meta-row"><span>${esc(t("selectWarehouse") || "المخزن")}</span><strong>${esc(data.warehouseName)}</strong></div>` : ""}
    </section>

    <table class="items">
      <thead>
        <tr>
          <th class="col-idx">#</th>
          <th class="col-name">${esc(t("item_name_label") || "المادة")}</th>
          <th class="col-qty">${esc(t("quantity_label") || "الكمية")}</th>
          <th class="col-price">${esc(t("selling_price_label") || "السعر")}</th>
          <th class="col-total">${esc(t("total_label") || "الإجمالي")}</th>
        </tr>
      </thead>
      <tbody>
        ${rowsHtml || `<tr><td colspan="5">${esc(t("emptyCart") || "لا توجد مواد")}</td></tr>`}
      </tbody>
    </table>

    <div class="bottom">
      <div></div>
      <div class="totals">
        <div class="totals-row"><span>${esc(t("countLabel") || "عدد المواد")}</span><strong>${esc(data.itemsCount ?? lines.length)} ${esc(t("itemLabel") || "")}</strong></div>
        ${discountRow}
        <div class="totals-row grand"><span>${esc(t("totalLabel") || "المجموع")}</span><strong>${formatMoney(data.grandTotal)} ${esc(data.currency || "")}</strong></div>
      </div>
    </div>

    ${notesBlock}

    <footer class="a4-footer">
      <p class="thanks">${esc(t("thankYouMessage") || "شكراً لتعاملكم معنا")}</p>
      ${data.footerLine ? `<p>${esc(data.footerLine)}</p>` : ""}
      ${data.footerCreditText ? `<p>${esc(data.footerCreditText)}</p>` : ""}
      ${data.footerCreditPhone ? `<p>${esc(data.footerCreditPhone)}</p>` : ""}
    </footer>
  </div>
</body>
</html>`;
}
