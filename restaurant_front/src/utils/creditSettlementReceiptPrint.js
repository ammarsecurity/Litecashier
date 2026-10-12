import { HTTP } from "@/http/api.js";
import { resolveApiBaseUrl, resolvePrintServerUrl } from "@/utils/apiBase.js";
import { buildReceiptPrintDocument } from "@/utils/receiptPrint.js";

function resolveAbsoluteAssetUrl(url) {
  if (url == null || url === "") return null;
  const raw = String(url).trim();
  if (raw.startsWith("data:")) return raw;
  if (raw.startsWith("http://") || raw.startsWith("https://")) return raw;
  const base = resolveApiBaseUrl().replace(/\/$/, "");
  if (raw.startsWith("/")) return `${base}${raw}`;
  return `${base}/${raw}`;
}

function money(v) {
  const n = Number(v);
  if (!Number.isFinite(n)) return "0";
  return n.toLocaleString("en-US", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function formatDateTime(value, locale = "ar") {
  if (!value) return "—";
  try {
    const loc = locale === "en" ? "en-GB" : "ar-IQ";
    return new Intl.DateTimeFormat(loc, {
      numberingSystem: "latn",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    }).format(new Date(value));
  } catch (_) {
    return String(value).slice(0, 19).replace("T", " ");
  }
}

function readStoreInfo() {
  try {
    const raw = localStorage.getItem("info");
    if (!raw) return {};
    const d = JSON.parse(raw);
    return {
      storeName: d.storeName || d.StoreName || "LiteRestaurant",
      logo: d.logo || d.Logo || null,
      footerCreditText: d.footerCreditText || d.FooterCreditText || null,
      footerCreditPhone: d.footerCreditPhone || d.FooterCreditPhone || null,
    };
  } catch (_) {
    return { storeName: "LiteRestaurant" };
  }
}

function methodLabel(method, locale = "ar") {
  const mapAr = { Cash: "نقد", Card: "بطاقة", BankTransfer: "تحويل بنكي" };
  const mapEn = { Cash: "Cash", Card: "Card", BankTransfer: "Bank transfer" };
  const map = locale === "en" ? mapEn : mapAr;
  return map[method] || method || "—";
}

/**
 * إيصال تسديد آجل بطراز POS حراري (72mm).
 */
export function buildCreditSettlementReceiptHtml({
  account,
  order,
  settlement,
  locale = "ar",
  storeInfo,
} = {}) {
  const store = storeInfo || readStoreInfo();
  const rtl = locale !== "en";
  const t = rtl
    ? {
        title: "إيصال تسديد آجل",
        subtitle: "سند قبض",
        customer: "العميل",
        phone: "الهاتف",
        invoice: "رقم الفاتورة",
        invoiceDate: "تاريخ الفاتورة",
        invoiceTotal: "إجمالي الفاتورة",
        paidBefore: "المسدّد سابقاً",
        paidNow: "المبلغ المستلم",
        remaining: "المتبقي",
        method: "طريقة الدفع",
        settledAt: "تاريخ التسديد",
        status: "الحالة",
        paid: "مسدّد بالكامل",
        partial: "تسديد جزئي",
        thanks: "شكراً لتسديدك",
        footerNote: "احتفظ بالإيصال للمراجعة",
      }
    : {
        title: "Credit Settlement Receipt",
        subtitle: "Payment receipt",
        customer: "Customer",
        phone: "Phone",
        invoice: "Invoice no.",
        invoiceDate: "Invoice date",
        invoiceTotal: "Invoice total",
        paidBefore: "Previously paid",
        paidNow: "Amount received",
        remaining: "Remaining",
        method: "Payment method",
        settledAt: "Settlement date",
        status: "Status",
        paid: "Fully paid",
        partial: "Partial payment",
        thanks: "Thank you",
        footerNote: "Keep this receipt for your records",
      };

  const orderAmount = Number(order?.amount ?? order?.Amount ?? 0);
  const applied = Number(
    settlement?.appliedAmount ??
      settlement?.AppliedAmount ??
      settlement?.amount ??
      settlement?.Amount ??
      0
  );
  const settledTotal = Number(
    settlement?.settledAmount ?? settlement?.SettledAmount ?? order?.settledAmount ?? 0
  );
  const remaining = Number(
    settlement?.remainingAmount ??
      settlement?.RemainingAmount ??
      Math.max(0, orderAmount - settledTotal)
  );
  const paidBefore = Math.max(0, settledTotal - applied);
  const method =
    settlement?.settlementPaymentMethod ||
    settlement?.SettlementPaymentMethod ||
    settlement?.paymentMethod ||
    settlement?.PaymentMethod ||
    order?.settlementPaymentMethod ||
    "Cash";
  const settledAt =
    settlement?.settledAt ||
    settlement?.SettledAt ||
    settlement?.insertDate ||
    settlement?.InsertDate ||
    new Date().toISOString();
  const statusText = remaining <= 0 ? t.paid : t.partial;
  const logoUrl = resolveAbsoluteAssetUrl(store.logo);
  const customerName = account?.name || account?.Name || "—";
  const customerPhone = account?.phone || account?.Phone || "";
  const orderCode = order?.orderCode || order?.OrderCode || "—";
  const orderDate = order?.insertDate || order?.InsertDate;

  const infoRow = (label, value) => `
    <div class="bill-info-row">
      <span class="bill-info-label">${escapeHtml(label)}</span>
      <span class="bill-info-value">${escapeHtml(value)}</span>
    </div>`;

  const summaryRow = (label, value, extraClass = "") => `
    <div class="bill-summary-row ${extraClass}">
      <span class="bill-summary-label">${escapeHtml(label)}</span>
      <span class="bill-summary-value">${escapeHtml(value)}</span>
    </div>`;

  const inner = `
    <div class="bill-container">
      <div class="bill-header">
        ${logoUrl ? `<img class="bill-logo-img" src="${escapeHtml(logoUrl)}" alt="" />` : ""}
        <h2 class="bill-store-name">${escapeHtml(store.storeName || "LiteRestaurant")}</h2>
        <p class="bill-store-subtitle">${escapeHtml(t.subtitle)}</p>
        <div style="margin-top:6px;font-size:13px;font-weight:800;">${escapeHtml(t.title)}</div>
      </div>

      <div class="bill-info-section">
        ${infoRow(t.customer, customerName)}
        ${customerPhone ? infoRow(t.phone, customerPhone) : ""}
        ${infoRow(t.invoice, orderCode)}
        ${orderDate ? infoRow(t.invoiceDate, formatDateTime(orderDate, locale)) : ""}
        ${infoRow(t.settledAt, formatDateTime(settledAt, locale))}
        ${infoRow(t.method, methodLabel(method, locale))}
        ${infoRow(t.status, statusText)}
      </div>

      <div class="bill-summary-section" style="margin-top:8px;">
        ${summaryRow(t.invoiceTotal, `${money(orderAmount)} د.ع`)}
        ${paidBefore > 0 ? summaryRow(t.paidBefore, `${money(paidBefore)} د.ع`) : ""}
        ${summaryRow(t.paidNow, `${money(applied)} د.ع`, "bill-total-row")}
        ${summaryRow(t.remaining, `${money(remaining)} د.ع`)}
      </div>

      <div class="bill-footer" style="text-align:center;margin-top:12px;padding-top:8px;border-top:1px dashed #000;">
        <div style="font-weight:700;margin-bottom:4px;">${escapeHtml(t.thanks)}</div>
        <div style="font-size:9px;color:#555;">${escapeHtml(t.footerNote)}</div>
        ${
          store.footerCreditText
            ? `<div style="font-size:9px;margin-top:8px;">${escapeHtml(store.footerCreditText)}</div>`
            : ""
        }
        ${
          store.footerCreditPhone
            ? `<div style="font-size:9px;">${escapeHtml(store.footerCreditPhone)}</div>`
            : ""
        }
      </div>
    </div>
  `;

  return buildReceiptPrintDocument(inner, t.title);
}

async function resolveMainPrinterId() {
  const res = await HTTP.get("Printers");
  const list = res.data?.data || res.data || [];
  const arr = Array.isArray(list) ? list : list.items || [];
  const main =
    arr.find((p) => (p.isMain ?? p.IsMain) && (p.isActive ?? p.IsActive) !== false) ||
    arr.find((p) => (p.isActive ?? p.IsActive) !== false);
  return main?.id ?? main?.Id ?? null;
}

export async function printCreditSettlementReceipt(payload) {
  const locale = payload?.locale || "ar";
  const htmlContent = buildCreditSettlementReceiptHtml({
    ...payload,
    locale,
  });

  const printerId = await resolveMainPrinterId().catch(() => null);
  if (printerId) {
    try {
      const response = await HTTP.post(`Printers/${printerId}/print`, {
        htmlContent,
        copies: 1,
      });
      if (response.data && !response.data.errorStatus) {
        return { ok: true, via: "api" };
      }
    } catch (_) {
      /* fall through */
    }
  }

  const printServer = resolvePrintServerUrl();
  try {
    const response = await fetch(`${printServer}/print`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ htmlContent, copies: 1 }),
    });
    if (response.ok) return { ok: true, via: "printServer" };
  } catch (_) {
    /* fall through */
  }

  const w = window.open("", "_blank", "width=420,height=720");
  if (w) {
    w.document.write(htmlContent);
    w.document.close();
    w.focus();
    setTimeout(() => {
      w.print();
      setTimeout(() => w.close(), 400);
    }, 350);
    return { ok: true, via: "browser" };
  }
  return { ok: false };
}
