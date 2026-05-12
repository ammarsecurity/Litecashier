<template>
  <b-overlay :show="loading" spinner-variant="primary" spinner-type="grow" spinner-small rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <div class="users-header-section">
            <div class="users-header-content">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="calendar2-check-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("endOfDayReportTitle") || "تقرير نهاية اليوم" }}</h1>
                  <p class="header-subtitle">{{ $t("endOfDayReportSubtitle") || "ملخص شامل لتقارير اليوم الحالي" }}</p>
                </div>
              </div>
            </div>
          </div>

          <div class="users-search-section end-day-actions">
            <button class="users-form-submit-button" @click="fetchReport" :disabled="loading">
              <b-icon icon="search" class="me-1"></b-icon>
              {{ $t("generateTodayReport") || "استخراج تقرير اليوم" }}
            </button>
            <button class="export-excel-btn" @click="downloadExcel" :disabled="!report || downloading">
              <b-spinner small v-if="downloading" class="me-1"></b-spinner>
              <b-icon v-else icon="file-earmark-excel" class="me-1"></b-icon>
              {{ $t("downloadExcel") || "تحميل Excel" }}
            </button>
          </div>

          <div v-if="!report && !loading" class="end-day-empty-state">
            <b-icon icon="bar-chart-line-fill" class="end-day-empty-icon"></b-icon>
            <p>{{ $t("generateTodayReport") || "استخراج تقرير اليوم" }}</p>
          </div>

          <div v-if="report" class="report-stats-grid">
            <div class="report-stat-card report-stat-primary">
              <p class="report-stat-label">{{ $t("orders") || "الفواتير" }}</p>
              <h3 class="report-stat-value">{{ report.totals.ordersCount || 0 }}</h3>
            </div>
            <div class="report-stat-card report-stat-success">
              <p class="report-stat-label">{{ $t("netSales") || "صافي المبيعات" }}</p>
              <h3 class="report-stat-value">{{ formatPrice(report.totals.netSales || 0) }} {{ $t("currency") }}</h3>
            </div>
            <div class="report-stat-card report-stat-danger">
              <p class="report-stat-label">{{ $t("profitReport") || "الربح" }}</p>
              <h3 class="report-stat-value">{{ formatPrice(report.totals.profit || 0) }} {{ $t("currency") }}</h3>
            </div>
            <div class="report-stat-card report-stat-info">
              <p class="report-stat-label">{{ $t("returnedItemsReport") || "المواد المسترجعة" }}</p>
              <h3 class="report-stat-value">{{ report.totals.returnedCount || 0 }}</h3>
            </div>
          </div>

          <div v-if="report" class="report-table-container">
            <h4 class="report-section-title">{{ $t("tablesStatusSummary") || "ملخص حالات الطاولات" }}</h4>
            <b-table :items="[report.tableStatus]" :fields="tableStatusFields" small responsive class="reports-table" />
          </div>

          <div v-if="report" class="report-table-container">
            <h4 class="report-section-title">{{ $t("paymentMethod") || "طريقة الدفع" }}</h4>
            <b-table :items="report.paymentBreakdown || []" :fields="paymentFields" small responsive class="reports-table" />
          </div>

          <div v-if="report" class="report-table-container">
            <h4 class="report-section-title">{{ $t("invoicesByTable") || "عدد الفواتير لكل طاولة" }}</h4>
            <b-table :items="report.invoicesByTable || []" :fields="invoicesByTableFields" small responsive class="reports-table" />
          </div>

          <div v-if="report" class="report-table-container">
            <h4 class="report-section-title">{{ $t("topSellingItems") || "الأكثر مبيعاً" }}</h4>
            <b-table :items="report.topItems || []" :fields="topItemsFields" small responsive class="reports-table" />
          </div>

          <div v-if="report" class="report-table-container">
            <h4 class="report-section-title">{{ $t("returnedItemsReport") || "المواد المسترجعة" }}</h4>
            <b-table :items="report.returnedItems || []" :fields="returnedFields" small responsive class="reports-table" />
          </div>
        </div>
      </div>
    </div>
  </b-overlay>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../http/api.js";

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
    formatPrice(value) {
      const n = Number(value || 0);
      return Number.isFinite(n) ? n.toLocaleString("en-EG") : "0";
    },
    async fetchReport() {
      try {
        this.loading = true;
        const response = await HTTP.get("Admin/GetEndOfDaySummary");
        this.report = response?.data?.data || null;
      } catch (error) {
        this.report = null;
        this.$toast.error(
          error?.response?.data?.message ||
            (this.$t("endOfDayReportBlocked") || "لا يمكن استخراج تقرير نهاية اليوم"),
          { timeout: 3000, maxToasts: 1 }
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
      } catch (error) {
        this.$toast.error(
          error?.response?.data?.message ||
            (this.$t("endOfDayExportError") || "فشل تنزيل تقرير نهاية اليوم"),
          { timeout: 3000, maxToasts: 1 }
        );
      } finally {
        this.downloading = false;
      }
    },
  },
};
</script>

<style scoped>
.end-day-actions {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
}

.end-day-empty-state {
  margin-top: 1rem;
  border: 1px dashed var(--border-color, #dbeafe);
  border-radius: 1rem;
  background: color-mix(in srgb, var(--bg-secondary, #f8fafc) 85%, transparent);
  min-height: 180px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.65rem;
  color: var(--text-secondary, #64748b);
}

.end-day-empty-icon {
  font-size: 2rem;
  color: var(--primary-color, #6366f1);
}

.report-table-container {
  margin-top: 0.9rem;
}

/* Align with ReporstView design system */
.export-excel-btn {
  display: inline-flex;
  align-items: center;
  padding: 0.5rem 1rem;
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
  opacity: 0.6;
  cursor: not-allowed;
}

.report-section-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 1rem 0;
  padding: 0.75rem;
  border-bottom: 2px solid var(--border-color);
  border-inline-start: 4px solid var(--primary-color);
  line-height: 1.4;
  font-family: inherit;
}

.reports-table {
  background: var(--bg-primary, #ffffff);
  border-radius: 0.5rem;
  overflow: hidden;
}

.reports-table ::v-deep .table {
  margin-bottom: 0;
}

.reports-table ::v-deep thead th {
  background: var(--bg-secondary, #f8f9fa);
  color: var(--text-primary, #212529);
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  padding: 1rem;
  border-bottom: 2px solid var(--border-color, #dee2e6);
}

.reports-table ::v-deep tbody td {
  padding: 1rem;
  vertical-align: middle;
  border-bottom: 1px solid var(--border-color, #e9ecef);
}

.reports-table ::v-deep tbody tr:hover {
  background: var(--bg-secondary, #f8f9fa);
}

@media (max-width: 767px) {
  .end-day-actions {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
