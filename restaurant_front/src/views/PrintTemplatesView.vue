<template>
  <div class="main-content-wrapper">
    <AppHeader />
    <div class="pt-page-container">
      <div class="pt-page-content">
        <div class="users-header-section">
          <div class="users-header-content">
            <div class="header-title-wrapper">
              <div class="header-icon-wrapper">
                <b-icon icon="printer-fill" class="header-icon"></b-icon>
              </div>
              <div>
                <h1 class="users-page-title">
                  {{ $t("printTemplates") || "نماذج الطباعة" }}
                </h1>
                <p class="header-subtitle">
                  {{
                    $t("printTemplatesDescription") ||
                    "معاينة نماذج الإيصال كما تُطبَع من POS والتقارير والويتر"
                  }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div class="pt-templates-grid">
          <article
            v-for="tpl in templateCards"
            :key="tpl.id"
            class="pt-template-card app-section-card"
          >
            <header class="pt-template-head">
              <span class="pt-template-icon" :class="`pt-template-icon--${tpl.id}`">
                <b-icon :icon="tpl.icon"></b-icon>
              </span>
              <div class="pt-template-head-text">
                <h2 class="pt-template-title">{{ tpl.title }}</h2>
                <p class="pt-template-desc">{{ tpl.description }}</p>
              </div>
            </header>

            <div class="pt-preview-shell">
              <div
                class="pt-receipt-paper"
                :id="`${tpl.id}-print-preview`"
                v-html="tpl.innerHtml"
              ></div>
            </div>

            <footer class="pt-template-footer">
              <button
                type="button"
                class="action-btn action-btn--print pt-print-btn"
                @click="printTemplate(tpl)"
              >
                <b-icon icon="printer-fill" class="me-2"></b-icon>
                {{ $t("print") || "طباعة" }}
              </button>
            </footer>
          </article>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import {
  buildStandaloneOrderReceiptHtml,
  RECEIPT_PRINT_CAIRO_FONT_HTML,
  RECEIPT_PRINT_STYLES_HTML,
} from "@/utils/receiptPrint.js";
import logoUrl from "@/assets/logoarabic.png";

const SAMPLE_STORE = "مطعم لايت كاشير";

function sampleItems(variant) {
  if (variant === "kitchen") {
    return [
      { name: "شاورما دجاج", quantity: 2, price: 12000, total: 24000, tags: "مطبخ", notes: "بدون بصل" },
      { name: "بطاطس مقلية", quantity: 1, price: 5000, total: 5000, tags: "مطبخ", notes: "" },
    ];
  }
  if (variant === "waiter") {
    return [
      { name: "سلطة خضار", quantity: 1, price: 8000, total: 8000, tags: "بار", notes: "" },
      { name: "شاي", quantity: 2, price: 1500, total: 3000, tags: "بار", notes: "سكر خفيف" },
    ];
  }
  if (variant === "reports") {
    return [
      { name: "برجر لحم", quantity: 2, price: 18000, total: 36000, tags: "مطبخ", notes: "" },
      { name: "بطاطس مقلية", quantity: 2, price: 5000, total: 10000, tags: "مطبخ", notes: "" },
    ];
  }
  return [
    { name: "شاورما دجاج", quantity: 2, price: 12000, total: 24000, tags: "مطبخ", notes: "" },
    { name: "بيتزا كبيرة", quantity: 1, price: 25000, total: 25000, tags: "مطبخ", notes: "" },
      { name: "كولا", quantity: 3, price: 2000, total: 6000, tags: "بار", notes: "" },
  ];
}

function sampleOrder(variant) {
  const base = {
    orderCode: "ORD-2024-001",
    dailySequenceNumber: 42,
    orderSubTotal: 0,
    discountAmount: 0,
    orderTotalAfterDiscount: 0,
    notes: "",
  };

  if (variant === "kitchen") {
    return {
      ...base,
      orderCode: "ORD-KIT-007",
      dailySequenceNumber: 7,
      orderType: "DineIn",
      paymentMethod: "Cash",
      orderSubTotal: 29000,
      orderTotalAfterDiscount: 29000,
    };
  }
  if (variant === "waiter") {
    return {
      ...base,
      orderCode: "ORD-W-055",
      dailySequenceNumber: 55,
      orderType: "DineIn",
      paymentMethod: "Cash",
      orderSubTotal: 11000,
      orderTotalAfterDiscount: 11000,
    };
  }
  if (variant === "reports") {
    return {
      ...base,
      orderCode: "RPT-987654",
      dailySequenceNumber: 18,
      orderType: "DineIn",
      paymentMethod: "Card",
      orderSubTotal: 46000,
      discountAmount: 2000,
      orderTotalAfterDiscount: 44000,
    };
  }
  return {
    ...base,
    orderType: "Takeaway",
    paymentMethod: "Cash",
    orderSubTotal: 55000,
    discountAmount: 5000,
    orderTotalAfterDiscount: 50000,
  };
}

export default {
  name: "PrintTemplatesView",
  components: { AppHeader },
  computed: {
    printLabels() {
      const t = (k, fb) => this.$t(k) || fb;
      return {
        invoiceNumber: t("invoiceNumber", "رقم الفاتورة"),
        orderNumber: t("orderNumber", "رقم الطلب"),
        orderType: t("orderType", "نوع الطلب"),
        paymentMethod: t("paymentMethod", "طريقة الدفع"),
        date: t("from_date", "التاريخ"),
        customerName: t("customerName", "اسم العميل"),
        phoneNumber: t("phoneNumber", "رقم الهاتف"),
        address: t("address", "العنوان"),
        notes: t("notes", "ملاحظات"),
        itemName: t("item_name_label", "طبق/مشروب"),
        quantity: t("quantity_label", "العدد"),
        price: t("selling_price_label", "السعر"),
        total: t("total", "المجموع"),
        discountLabel: t("discountLabel", "الخصم"),
        currency: t("currency", "د.ع"),
        thankYou: t("thankYouVisit", "شكراً لزيارتكم"),
        storeFallback: t("restaurant", "المطعم"),
        dineIn: t("dineIn", "داخل المطعم"),
        takeaway: t("takeaway", "خارجي"),
        delivery: t("delivery", "توصيل"),
        cash: t("cash", "نقدي"),
        card: t("card", "بطاقة"),
        credit: t("credit", "آجل"),
      };
    },
    templateCards() {
      const t = (k, fb) => this.$t(k) || fb;
      const defs = [
        {
          id: "pos",
          icon: "cash-stack",
          title: t("posPrintTemplate", "نموذج طباعة POS"),
          description: t("posPrintTemplateDesc", "فاتورة كاملة مع الأسعار — نقطة البيع"),
          variant: "pos",
          hidePrices: false,
          tagName: null,
        },
        {
          id: "reports",
          icon: "file-earmark-bar-graph-fill",
          title: t("reportsPrintTemplate", "نموذج طباعة التقارير"),
          description: t("reportsPrintTemplateDesc", "فاتورة مع خصم — كما في التقارير"),
          variant: "reports",
          hidePrices: false,
          tagName: null,
        },
        {
          id: "waiter",
          icon: "person-badge-fill",
          title: t("waiterPrintTemplate", "نموذج طباعة الويتر"),
          description: t("waiterPrintTemplateDesc", "طلب داخل المطعم — صفحة الويتر"),
          variant: "waiter",
          hidePrices: false,
          tagName: null,
        },
        {
          id: "kitchen",
          icon: "egg-fried",
          title: t("kitchenPrintTemplate", "نموذج طباعة المطبخ"),
          description: t("kitchenPrintTemplateDesc", "تذكرة قسم بدون أسعار — طابعات الأقسام"),
          variant: "kitchen",
          hidePrices: true,
          tagName: t("kitchenSection", "مطبخ"),
        },
      ];

      return defs.map((def) => {
        const items = sampleItems(def.variant);
        const order = sampleOrder(def.variant);
        const built = buildStandaloneOrderReceiptHtml({
          storeName: SAMPLE_STORE,
          logoUrl,
          order,
          items,
          hidePrices: def.hidePrices,
          tagName: def.tagName,
          labels: this.printLabels,
        });
        return { ...def, innerHtml: built.innerHtml, documentHtml: built.documentHtml };
      });
    },
  },
  methods: {
    printTemplate(tpl) {
      const preview = document.getElementById(`${tpl.id}-print-preview`);
      if (!preview) return;

      const bodyHtml = tpl.documentHtml || preview.innerHTML;
      const printWindow = window.open(
        "",
        "",
        "left=0,top=0,width=420,height=640,toolbar=0,scrollbars=0,status=0"
      );
      if (!printWindow) return;

      printWindow.document.write(`<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
  <meta charset="UTF-8">
  ${RECEIPT_PRINT_CAIRO_FONT_HTML}
  ${RECEIPT_PRINT_STYLES_HTML}
  <title>${tpl.title}</title>
</head>
<body>
  ${tpl.innerHtml}
</body>
</html>`);
      printWindow.document.close();
      printWindow.focus();
      setTimeout(() => {
        printWindow.print();
        setTimeout(() => printWindow.close(), 150);
      }, 300);
    },
  },
};
</script>

<style scoped>
.pt-page-container {
  padding: 1rem 1rem 2.5rem;
  max-width: 1400px;
  margin: 0 auto;
}

.pt-page-content {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.pt-templates-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 320px), 1fr));
  gap: 1.25rem;
}

.pt-template-card {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid var(--border-color);
  background: var(--bg-tertiary);
  border-radius: var(--radius-xl, 0.75rem);
  box-shadow: var(--shadow-md);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.pt-template-card:hover {
  border-color: color-mix(in srgb, var(--primary-color) 45%, var(--border-color));
  box-shadow: var(--shadow-lg);
}

.pt-template-head {
  display: flex;
  align-items: flex-start;
  gap: 0.875rem;
  padding: 1rem 1.15rem;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.pt-template-icon {
  flex-shrink: 0;
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--primary-color);
}

.pt-template-icon--pos {
  color: var(--primary-color);
}

.pt-template-icon--reports {
  color: var(--info-color, #3b82f6);
}

.pt-template-icon--waiter {
  color: var(--success-color, #10b981);
}

.pt-template-icon--kitchen {
  color: #f59e0b;
}

.pt-template-head-text {
  min-width: 0;
}

.pt-template-title {
  margin: 0 0 0.25rem;
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.35;
}

.pt-template-desc {
  margin: 0;
  font-size: 0.8125rem;
  color: var(--text-secondary);
  line-height: 1.45;
}

.pt-preview-shell {
  padding: 1rem;
  background: color-mix(in srgb, var(--bg-primary) 88%, #000 12%);
  border-bottom: 1px solid var(--border-color);
  display: flex;
  justify-content: center;
  max-height: min(58vh, 520px);
  overflow: auto;
}

.pt-receipt-paper {
  width: 72mm;
  max-width: 100%;
  background: #fff;
  color: #000;
  border-radius: 4px;
  box-shadow: 0 4px 18px rgba(0, 0, 0, 0.22);
  padding: 3mm 3mm 3mm 5mm;
  font-family: "Cairo", "Arial", sans-serif;
  direction: rtl;
  font-size: 11px;
  line-height: 1.35;
}

.pt-template-footer {
  padding: 1rem 1.15rem;
  display: flex;
  justify-content: center;
  background: var(--bg-tertiary);
}

.pt-print-btn {
  min-width: 9rem;
  justify-content: center;
}

/* Thermal receipt look inside preview (matches receiptPrint.js) */
.pt-receipt-paper :deep(.bill-container) {
  width: 100%;
  max-width: 100%;
  margin: 0;
  padding: 0 2mm 0 3mm;
}

.pt-receipt-paper :deep(.bill-header) {
  text-align: center;
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px dashed #000;
}

.pt-receipt-paper :deep(.bill-logo-img) {
  max-width: 50px;
  height: auto;
  margin-bottom: 4px;
}

.pt-receipt-paper :deep(.bill-store-name) {
  font-size: 16px;
  font-weight: 800;
  margin: 4px 0 2px;
  color: #000;
}

.pt-receipt-paper :deep(.bill-info-section) {
  margin: 8px 0;
  padding: 0 1mm;
  font-size: 10px;
  background: transparent;
  border: none;
  border-radius: 0;
}

.pt-receipt-paper :deep(.bill-info-row) {
  display: flex;
  justify-content: space-between;
  gap: 6px;
  margin-bottom: 4px;
  padding: 0 1px;
  border-bottom: none;
}

.pt-receipt-paper :deep(.bill-info-label) {
  flex: 0 0 44%;
  font-weight: 600;
  color: #000;
}

.pt-receipt-paper :deep(.bill-info-value) {
  flex: 1;
  text-align: right;
  font-weight: 400;
  color: #000;
  word-break: break-word;
}

.pt-receipt-paper :deep(.bill-divider) {
  border-top: 1px dashed #000;
  margin: 8px 0;
}

.pt-receipt-paper :deep(.bill-items-section) {
  margin: 8px 0;
  padding: 0 1mm;
  background: transparent;
  border: none;
  box-shadow: none;
  border-radius: 0;
  overflow: hidden;
}

.pt-receipt-paper :deep(.bill-items-table) {
  width: 100%;
  border-collapse: collapse;
  font-size: 10px;
}

.pt-receipt-paper :deep(.bill-items-table thead) {
  border-bottom: 1px solid #000;
  background: transparent;
  color: #000;
  box-shadow: none;
}

.pt-receipt-paper :deep(.bill-items-table th) {
  padding: 4px 2px;
  text-align: right;
  font-weight: 700;
  font-size: 9px;
  color: #000;
  border-bottom: none;
  text-shadow: none;
}

.pt-receipt-paper :deep(.bill-items-table td) {
  padding: 3px 2px;
  vertical-align: top;
  background: #fff !important;
  color: #000 !important;
  border-bottom: 1px dotted #ccc;
}

.pt-receipt-paper :deep(.bill-items-table tbody tr:hover),
.pt-receipt-paper :deep(.bill-items-table tbody tr:nth-child(even)) {
  background: transparent !important;
}

.pt-receipt-paper :deep(.bill-item-qty-col) {
  text-align: center;
}

.pt-receipt-paper :deep(.bill-item-price-col),
.pt-receipt-paper :deep(.bill-item-total-col) {
  text-align: left;
}

.pt-receipt-paper :deep(.bill-item-line-note) {
  font-size: 8px;
  color: #444;
  margin-top: 2px;
}

.pt-receipt-paper :deep(.bill-summary-section) {
  margin: 8px 0;
  font-size: 11px;
  background: transparent;
  border: none;
  padding: 0;
}

.pt-receipt-paper :deep(.bill-summary-row) {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
}

.pt-receipt-paper :deep(.bill-total-row) {
  border-top: 1px solid #000;
  padding-top: 4px;
  margin-top: 4px;
  font-weight: 800;
}

.pt-receipt-paper :deep(.bill-footer) {
  text-align: center;
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px dashed #000;
}

.pt-receipt-paper :deep(.bill-footer p) {
  font-size: 9px;
  margin: 2px 0;
  color: #666;
}

@media (max-width: 640px) {
  .pt-page-container {
    padding-inline: 0.75rem;
  }

  .pt-preview-shell {
    max-height: 420px;
  }
}
</style>
