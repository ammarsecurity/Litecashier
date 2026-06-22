<template>
  <div class="main-content-wrapper" :dir="direction">
    <AppHeader />
    <b-overlay :show="loading" spinner-variant="primary" spinner-type="grow" spinner-small rounded="sm">
      <div class="eod-page-container">
        <div class="eod-page-content">
          <!-- Header -->
          <div class="users-header-section">
            <div class="users-header-content eod-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="calendar2-check-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("endOfDayReportTitle") || "تقرير نهاية اليوم" }}</h1>
                  <p class="header-subtitle">{{ $t("endOfDayReportSubtitle") || "ملخص شامل لتقارير اليوم الحالي" }}</p>
                </div>
              </div>
              <div class="eod-header-actions">
                <button
                  type="button"
                  class="users-form-submit-button"
                  @click="fetchReport"
                  :disabled="loading"
                >
                  <b-icon icon="search" class="me-1"></b-icon>
                  {{ $t("generateTodayReport") || "استخراج تقرير اليوم" }}
                </button>
                <button
                  type="button"
                  class="export-excel-btn"
                  @click="downloadExcel"
                  :disabled="!report || downloading"
                >
                  <b-spinner small v-if="downloading" class="me-1"></b-spinner>
                  <b-icon v-else icon="file-earmark-excel" class="me-1"></b-icon>
                  {{ $t("downloadExcel") || "تحميل Excel" }}
                </button>
              </div>
            </div>
          </div>

          <!-- Hint before generate -->
          <div v-if="!report && !loading" class="eod-hint-card">
            <b-icon icon="info-circle-fill" class="eod-hint-icon"></b-icon>
            <p>{{ $t("endOfDayHint") || "يجب إغلاق جميع الطاولات المشغولة قبل استخراج تقرير نهاية اليوم." }}</p>
          </div>

          <!-- Empty -->
          <div v-if="!report && !loading" class="eod-empty-state">
            <b-icon icon="bar-chart-line-fill" class="eod-empty-icon"></b-icon>
            <h3>{{ $t("endOfDayEmptyTitle") || "لم يُستخرج التقرير بعد" }}</h3>
            <p>{{ $t("endOfDayEmptyText") || "اضغط «استخراج تقرير اليوم» لعرض ملخص اليوم." }}</p>
            <button type="button" class="eod-empty-btn" @click="fetchReport">
              <b-icon icon="search"></b-icon>
              {{ $t("generateTodayReport") || "استخراج تقرير اليوم" }}
            </button>
          </div>

          <template v-if="report">
            <!-- Period -->
            <div class="eod-period-banner">
              <b-icon icon="clock-history" class="eod-period-icon"></b-icon>
              <div>
                <span class="eod-period-label">{{ $t("endOfDayPeriod") || "فترة التقرير" }}</span>
                <strong class="eod-period-value">{{ reportPeriodLabel }}</strong>
              </div>
            </div>

            <!-- KPIs -->
            <div class="report-stats-grid eod-stats-grid">
              <div class="report-stat-card report-stat-primary">
                <div class="report-stat-icon">
                  <b-icon icon="receipt-cutoff"></b-icon>
                </div>
                <div class="report-stat-content">
                  <h3 class="report-stat-value">{{ report.totals.ordersCount || 0 }}</h3>
                  <p class="report-stat-label">{{ $t("orders") || "الفواتير" }}</p>
                </div>
              </div>
              <div class="report-stat-card report-stat-info">
                <div class="report-stat-icon">
                  <b-icon icon="currency-dollar"></b-icon>
                </div>
                <div class="report-stat-content">
                  <h3 class="report-stat-value">{{ formatPrice(report.totals.grossSales) }}</h3>
                  <p class="report-stat-label">{{ $t("grossSales") || "إجمالي المبيعات" }}</p>
                  <p class="report-stat-detail" v-if="report.totals.discountAmount">
                    {{ $t("discountLabel") || "الخصم" }}: {{ formatPrice(report.totals.discountAmount) }}
                  </p>
                </div>
              </div>
              <div class="report-stat-card report-stat-success">
                <div class="report-stat-icon">
                  <b-icon icon="cash-stack"></b-icon>
                </div>
                <div class="report-stat-content">
                  <h3 class="report-stat-value">{{ formatPrice(report.totals.netSales) }}</h3>
                  <p class="report-stat-label">{{ $t("netSales") || "صافي المبيعات" }}</p>
                </div>
              </div>
              <div class="report-stat-card report-stat-success">
                <div class="report-stat-icon">
                  <b-icon icon="graph-up-arrow"></b-icon>
                </div>
                <div class="report-stat-content">
                  <h3 class="report-stat-value">{{ formatPrice(report.totals.profit) }}</h3>
                  <p class="report-stat-label">{{ $t("profitReport") || "الربح" }}</p>
                  <p class="report-stat-detail" v-if="profitMargin">
                    {{ $t("profitMargin") || "هامش الربح" }}: {{ profitMargin }}%
                  </p>
                </div>
              </div>
              <div class="report-stat-card report-stat-danger">
                <div class="report-stat-icon">
                  <b-icon icon="arrow-counterclockwise"></b-icon>
                </div>
                <div class="report-stat-content">
                  <h3 class="report-stat-value">{{ report.totals.returnedCount || 0 }}</h3>
                  <p class="report-stat-label">{{ $t("returnedItemsReport") || "المواد المسترجعة" }}</p>
                  <p class="report-stat-detail" v-if="report.totals.returnedAmount">
                    {{ formatPrice(report.totals.returnedAmount) }} {{ $t("currency") }}
                  </p>
                </div>
              </div>
              <div class="report-stat-card report-stat-primary">
                <div class="report-stat-icon">
                  <b-icon icon="box-seam"></b-icon>
                </div>
                <div class="report-stat-content">
                  <h3 class="report-stat-value">{{ report.totals.itemsQuantity || 0 }}</h3>
                  <p class="report-stat-label">{{ $t("itemsSoldToday") || "المواد المباعة" }}</p>
                  <p class="report-stat-detail">
                    {{ report.totals.itemsCount || 0 }} {{ $t("uniqueItems") || "صنف مختلف" }}
                  </p>
                </div>
              </div>
            </div>

            <!-- Tables status -->
            <div class="eod-section-card">
              <div class="eod-section-header">
                <div class="eod-section-title-wrap">
                  <div class="eod-section-icon-wrap">
                    <b-icon icon="grid-3x3-gap-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="eod-section-title">{{ $t("tablesStatusSummary") || "ملخص حالات الطاولات" }}</h3>
                  </div>
                </div>
              </div>
              <div class="eod-section-body">
                <div class="eod-table-status-grid">
                  <div
                    v-for="chip in tableStatusChips"
                    :key="chip.key"
                    class="eod-table-status-chip"
                    :class="`eod-table-status-chip--${chip.key}`"
                  >
                    <span class="eod-table-status-value">{{ chip.value }}</span>
                    <span class="eod-table-status-label">{{ chip.label }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Payment breakdown -->
            <div class="eod-section-card">
              <div class="eod-section-header">
                <div class="eod-section-title-wrap">
                  <div class="eod-section-icon-wrap eod-section-icon-wrap--payment">
                    <b-icon icon="credit-card-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="eod-section-title">{{ $t("paymentMethod") || "طريقة الدفع" }}</h3>
                  </div>
                </div>
              </div>
              <div class="eod-section-body">
                <b-table
                  v-if="(report.paymentBreakdown || []).length"
                  :items="paymentRows"
                  :fields="paymentFields"
                  small
                  responsive
                  class="reports-table"
                  striped
                  hover
                />
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>

            <!-- Orders by type -->
            <div class="eod-section-card">
              <div class="eod-section-header">
                <div class="eod-section-title-wrap">
                  <div class="eod-section-icon-wrap eod-section-icon-wrap--top">
                    <b-icon icon="diagram-3-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="eod-section-title">{{ $t("ordersByType") || "الفواتير حسب نوع الطلب" }}</h3>
                  </div>
                </div>
              </div>
              <div class="eod-section-body">
                <b-table
                  v-if="(report.ordersByType || []).length"
                  :items="ordersByTypeRows"
                  :fields="ordersByTypeFields"
                  small
                  responsive
                  class="reports-table"
                  striped
                  hover
                />
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>

            <!-- Invoices by table -->
            <div class="eod-section-card">
              <div class="eod-section-header">
                <div class="eod-section-title-wrap">
                  <div class="eod-section-icon-wrap eod-section-icon-wrap--tables">
                    <b-icon icon="table"></b-icon>
                  </div>
                  <div>
                    <h3 class="eod-section-title">{{ $t("invoicesByTable") || "عدد الفواتير لكل طاولة" }}</h3>
                  </div>
                </div>
              </div>
              <div class="eod-section-body">
                <b-table
                  v-if="(report.invoicesByTable || []).length"
                  :items="invoicesByTableRows"
                  :fields="invoicesByTableFields"
                  small
                  responsive
                  class="reports-table"
                  striped
                  hover
                />
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>

            <!-- Top items -->
            <div class="eod-section-card">
              <div class="eod-section-header">
                <div class="eod-section-title-wrap">
                  <div class="eod-section-icon-wrap eod-section-icon-wrap--top">
                    <b-icon icon="trophy-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="eod-section-title">{{ $t("topSellingItems") || "الأكثر مبيعاً" }}</h3>
                  </div>
                </div>
              </div>
              <div class="eod-section-body">
                <b-table
                  v-if="(report.topItems || []).length"
                  :items="topItemsRows"
                  :fields="topItemsFields"
                  small
                  responsive
                  class="reports-table"
                  striped
                  hover
                />
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>

            <!-- Returned items -->
            <div class="eod-section-card">
              <div class="eod-section-header">
                <div class="eod-section-title-wrap">
                  <div class="eod-section-icon-wrap eod-section-icon-wrap--return">
                    <b-icon icon="arrow-counterclockwise"></b-icon>
                  </div>
                  <div>
                    <h3 class="eod-section-title">{{ $t("returnedItemsReport") || "المواد المسترجعة" }}</h3>
                  </div>
                </div>
              </div>
              <div class="eod-section-body">
                <b-table
                  v-if="(report.returnedItems || []).length"
                  :items="returnedRows"
                  :fields="returnedFields"
                  small
                  responsive
                  class="reports-table"
                  striped
                  hover
                />
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>
          </template>
        </div>
      </div>
    </b-overlay>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../http/api.js";
import { formatBusinessDate } from "../utils/formatBusinessDateTime.js";

export default {
  name: "EndOfDayReportView",
  components: { AppHeader },
  data() {
    return {
      loading: false,
      downloading: false,
      report: null,
    };
  },
  computed: {
    direction() {
      return this.$i18n.locale === "ar" ? "rtl" : "ltr";
    },
    reportPeriodLabel() {
      if (!this.report) return "";
      return formatBusinessDate(this.report.dayStart);
    },
    profitMargin() {
      const net = Number(this.report?.totals?.netSales || 0);
      const profit = Number(this.report?.totals?.profit || 0);
      if (!net || !Number.isFinite(net)) return null;
      return ((profit / net) * 100).toFixed(1);
    },
    tableStatusChips() {
      const s = this.report?.tableStatus || {};
      return [
        { key: "total", label: this.$t("tables") || "الطاولات", value: s.totalTables || 0 },
        { key: "available", label: this.$t("available") || "متاحة", value: s.availableTables || 0 },
        { key: "occupied", label: this.$t("occupied") || "مشغولة", value: s.occupiedTables || 0 },
        { key: "reserved", label: this.$t("reserved") || "محجوزة", value: s.reservedTables || 0 },
        { key: "out", label: this.$t("outOfService") || "خارج الخدمة", value: s.outOfServiceTables || 0 },
      ];
    },
    paymentRows() {
      return (this.report?.paymentBreakdown || []).map((row) => ({
        ...row,
        amount: this.formatPrice(row.amount),
      }));
    },
    invoicesByTableRows() {
      return (this.report?.invoicesByTable || []).map((row) => ({
        ...row,
        tableNumber: this.formatInvoiceTableLabel(row.tableNumber),
        totalAmount: this.formatPrice(row.totalAmount),
      }));
    },
    ordersByTypeRows() {
      return (this.report?.ordersByType || []).map((row) => ({
        ...row,
        orderType: this.getOrderTypeText(row.orderType),
        totalAmount: this.formatPrice(row.totalAmount),
      }));
    },
    topItemsRows() {
      return (this.report?.topItems || []).map((row) => ({
        ...row,
        salesAmount: this.formatPrice(row.salesAmount),
      }));
    },
    returnedRows() {
      return (this.report?.returnedItems || []).map((row) => ({
        ...row,
        lineTotal: this.formatPrice(row.lineTotal),
      }));
    },
    tableStatusFields() {
      return [
        { key: "totalTables", label: this.$t("tables") || "الطاولات" },
        { key: "availableTables", label: this.$t("available") || "متاحة" },
        { key: "occupiedTables", label: this.$t("occupied") || "مشغولة" },
        { key: "reservedTables", label: this.$t("reserved") || "محجوزة" },
        { key: "outOfServiceTables", label: this.$t("outOfService") || "خارج الخدمة" },
      ];
    },
    paymentFields() {
      return [
        { key: "method", label: this.$t("paymentMethod") || "طريقة الدفع" },
        { key: "ordersCount", label: this.$t("orders") || "الفواتير" },
        { key: "amount", label: this.$t("totalAmount") || "المبلغ" },
      ];
    },
    ordersByTypeFields() {
      return [
        { key: "orderType", label: this.$t("orderType") || "نوع الطلب" },
        { key: "ordersCount", label: this.$t("orders") || "الفواتير" },
        { key: "totalAmount", label: this.$t("totalAmount") || "المبلغ" },
      ];
    },
    invoicesByTableFields() {
      return [
        { key: "tableNumber", label: this.$t("table") || "الطاولة" },
        { key: "invoicesCount", label: this.$t("orders") || "الفواتير" },
        { key: "totalAmount", label: this.$t("totalAmount") || "المبلغ" },
      ];
    },
    topItemsFields() {
      return [
        { key: "itemName", label: this.$t("itemName") || "المادة" },
        { key: "quantity", label: this.$t("quantity") || "الكمية" },
        { key: "salesAmount", label: this.$t("totalSales") || "المبيعات" },
      ];
    },
    returnedFields() {
      return [
        { key: "orderCode", label: this.$t("invoiceNumber") || "الفاتورة" },
        { key: "itemName", label: this.$t("itemName") || "المادة" },
        { key: "quantity", label: this.$t("quantity") || "الكمية" },
        { key: "lineTotal", label: this.$t("lineTotal") || "المجموع" },
        { key: "deletedByUsername", label: this.$t("deletedBy") || "حذف بواسطة" },
      ];
    },
  },
  methods: {
    getOrderTypeText(type) {
      const texts = {
        DineIn: this.$t("dineIn") || "داخل المطعم",
        Takeaway: this.$t("takeaway") || "خارجي",
        Delivery: this.$t("delivery") || "توصيل",
      };
      return texts[type] || type || "—";
    },
    formatInvoiceTableLabel(value) {
      if (!value || value === "-") return "—";
      if (value === "Takeaway" || value === "Delivery" || value === "DineIn") {
        return this.getOrderTypeText(value);
      }
      return value;
    },
    formatPrice(value) {
      const n = Number(value || 0);
      return Number.isFinite(n) ? n.toLocaleString("en-EG") : "0";
    },
    formatDateTime(value) {
      if (!value) return "—";
      try {
        const d = new Date(value);
        return d.toLocaleString(this.$i18n.locale === "ar" ? "ar-EG" : "en-GB", {
          dateStyle: "medium",
          timeStyle: "short",
        });
      } catch {
        return String(value);
      }
    },
    async fetchReport() {
      try {
        this.loading = true;
        const response = await HTTP.get("Admin/GetEndOfDaySummary");
        this.report = response?.data?.data || null;
        if (this.report) {
          this.$toast.success(this.$t("endOfDayReportReady") || "تم استخراج التقرير بنجاح", {
            timeout: 2500,
            maxToasts: 1,
          });
        }
      } catch (error) {
        this.report = null;
        this.$toast.error(
          error?.response?.data?.message ||
            (this.$t("endOfDayReportBlocked") || "لا يمكن استخراج تقرير نهاية اليوم"),
          { timeout: 4000, maxToasts: 1 }
        );
      } finally {
        this.loading = false;
      }
    },
    async downloadExcel() {
      if (!this.report) return;
      try {
        this.downloading = true;
        const response = await HTTP.get("Admin/ExportEndOfDaySummary", { responseType: "blob" });
        const contentType =
          response?.headers?.["content-type"] ||
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        const blob = new Blob([response.data], { type: contentType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        const disposition = response?.headers?.["content-disposition"] || "";
        const matchedName = disposition.match(/filename\*?=(?:UTF-8''|")?([^\";]+)/i);
        const fallbackName = `end_of_day_${new Date().toISOString().split("T")[0]}.xlsx`;
        link.download = decodeURIComponent((matchedName?.[1] || fallbackName).replace(/"/g, ""));
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        this.$toast.success(this.$t("endOfDayExportSuccess") || "تم تنزيل الملف بنجاح", {
          timeout: 2500,
          maxToasts: 1,
        });
      } catch (error) {
        this.$toast.error(
          error?.response?.data?.message ||
            (this.$t("endOfDayExportError") || "فشل تنزيل تقرير نهاية اليوم"),
          { timeout: 4000, maxToasts: 1 }
        );
      } finally {
        this.downloading = false;
      }
    },
  },
};
</script>

<style scoped>
.eod-page-container {
  padding: 2rem;
  min-height: calc(100vh - 4rem);
}

.eod-page-content {
  max-width: 1400px;
  margin: 0 auto;
}

.eod-header-row {
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 1rem;
}

.eod-header-actions {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  flex-wrap: wrap;
}

.eod-hint-card {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.9rem 1.1rem;
  margin-bottom: 1rem;
  border-radius: 0.85rem;
  border: 1px solid rgba(245, 158, 11, 0.35);
  background: linear-gradient(135deg, rgba(251, 191, 36, 0.12) 0%, rgba(245, 158, 11, 0.06) 100%);
  color: var(--text-primary);
  font-size: 0.88rem;
  line-height: 1.5;
}

.eod-hint-card p {
  margin: 0;
}

.eod-hint-icon {
  color: #d97706;
  font-size: 1.25rem;
  flex-shrink: 0;
  margin-top: 0.1rem;
}

.eod-empty-state {
  margin-top: 0.5rem;
  padding: 2.5rem 1.5rem;
  border: 1px dashed var(--border-color);
  border-radius: 1rem;
  background: var(--bg-secondary);
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.5rem;
}

.eod-empty-icon {
  font-size: 2.5rem;
  color: var(--primary-color);
  margin-bottom: 0.25rem;
}

.eod-empty-state h3 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 700;
  color: var(--text-primary);
}

.eod-empty-state p {
  margin: 0 0 0.75rem;
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.eod-empty-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.65rem 1.2rem;
  border: none;
  border-radius: var(--radius-md);
  background: var(--primary-color);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.eod-empty-btn:hover {
  background: var(--primary-dark, #4338ca);
  transform: translateY(-1px);
}

.eod-period-banner {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 1rem 1.25rem;
  margin-bottom: 1.25rem;
  border-radius: 0.85rem;
  border: 1px solid rgba(99, 102, 241, 0.3);
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.1) 0%, rgba(79, 70, 229, 0.05) 100%);
}

.eod-period-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.eod-period-label {
  display: block;
  font-size: 0.75rem;
  color: var(--text-secondary);
  margin-bottom: 0.15rem;
}

.eod-period-value {
  font-size: 0.95rem;
  color: var(--text-primary);
}

.eod-stats-grid {
  margin-bottom: 1.5rem;
}

.eod-section-card {
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 1rem;
  margin-bottom: 1.25rem;
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.eod-section-header {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.eod-section-title-wrap {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.eod-section-icon-wrap {
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.65rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.14) 0%, rgba(79, 70, 229, 0.08) 100%);
  color: var(--primary-color);
  font-size: 1.1rem;
}

.eod-section-icon-wrap--payment {
  background: linear-gradient(135deg, rgba(16, 185, 129, 0.14) 0%, rgba(5, 150, 105, 0.08) 100%);
  color: #059669;
}

.eod-section-icon-wrap--tables {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.14) 0%, rgba(37, 99, 235, 0.08) 100%);
  color: #2563eb;
}

.eod-section-icon-wrap--top {
  background: linear-gradient(135deg, rgba(245, 158, 11, 0.16) 0%, rgba(217, 119, 6, 0.08) 100%);
  color: #d97706;
}

.eod-section-icon-wrap--return {
  background: linear-gradient(135deg, rgba(239, 68, 68, 0.14) 0%, rgba(220, 38, 38, 0.08) 100%);
  color: #dc2626;
}

.eod-section-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text-primary);
}

.eod-section-body {
  padding: 1rem 1.25rem 1.25rem;
}

.eod-section-empty {
  margin: 0;
  text-align: center;
  color: var(--text-secondary);
  font-size: 0.88rem;
  padding: 1.5rem 0;
}

.eod-table-status-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 0.75rem;
}

.eod-table-status-chip {
  text-align: center;
  padding: 0.85rem 0.5rem;
  border-radius: 0.75rem;
  border: 1.5px solid var(--border-color);
  background: var(--bg-secondary);
}

.eod-table-status-value {
  display: block;
  font-size: 1.35rem;
  font-weight: 800;
  line-height: 1.2;
  color: var(--text-primary);
}

.eod-table-status-label {
  display: block;
  margin-top: 0.2rem;
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.eod-table-status-chip--available {
  border-color: rgba(16, 185, 129, 0.4);
  background: rgba(16, 185, 129, 0.08);
}

.eod-table-status-chip--occupied {
  border-color: rgba(239, 68, 68, 0.4);
  background: rgba(239, 68, 68, 0.08);
}

.eod-table-status-chip--reserved {
  border-color: rgba(124, 58, 237, 0.45);
  background: rgba(124, 58, 237, 0.1);
}

.eod-table-status-chip--out {
  border-color: rgba(148, 163, 184, 0.5);
  background: rgba(148, 163, 184, 0.12);
}

.export-excel-btn {
  display: inline-flex;
  align-items: center;
  padding: 0.55rem 1rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: #0d6e2f;
  background: rgba(13, 110, 47, 0.12);
  border: 1px solid rgba(13, 110, 47, 0.3);
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.export-excel-btn:hover:not(:disabled) {
  background: #0d6e2f;
  color: #fff;
  border-color: #0d6e2f;
}

.export-excel-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.reports-table ::v-deep .table {
  margin-bottom: 0;
}

.reports-table ::v-deep thead th {
  background: var(--bg-secondary);
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.8rem;
  padding: 0.85rem 1rem;
  border-bottom: 2px solid var(--border-color);
  white-space: nowrap;
}

.reports-table ::v-deep tbody td {
  padding: 0.85rem 1rem;
  vertical-align: middle;
  border-bottom: 1px solid var(--border-color);
  font-size: 0.88rem;
}

.reports-table ::v-deep tbody tr:hover {
  background: var(--bg-secondary);
}

@media (max-width: 992px) {
  .eod-table-status-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 768px) {
  .eod-page-container {
    padding: 1rem;
  }

  .eod-header-actions {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
  }

  .eod-header-actions .users-form-submit-button,
  .eod-header-actions .export-excel-btn {
    width: 100%;
    justify-content: center;
  }

  .eod-table-status-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
