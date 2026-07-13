<template>
  <div class="main-content-wrapper" :dir="direction">
    <AppHeader />
    <b-overlay :show="loading" spinner-variant="primary" spinner-type="grow" spinner-small rounded="sm">
      <div class="app-page-container">
        <div class="app-page-content reports-page-content eod-page-content">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="calendar2-check-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("endOfDayReportTitle") || "تقرير نهاية اليوم" }}</h1>
                  <p class="header-subtitle">{{ $t("endOfDayReportSubtitle") || "ملخص شامل لتقارير اليوم الحالي" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="fetchReport" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("generateTodayReport") || "استخراج تقرير اليوم" }}</span>
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

          <div v-if="!report && !loading" class="eod-empty-state">
            <div class="eod-empty-icon-wrap">
              <b-icon icon="calendar2-check" class="eod-empty-icon"></b-icon>
            </div>
            <h3>{{ $t("endOfDayEmptyTitle") || "لم يُستخرج التقرير بعد" }}</h3>
            <p class="eod-empty-text">{{ $t("endOfDayEmptyText") || "اضغط «استخراج تقرير اليوم» لعرض ملخص اليوم." }}</p>
            <div class="report-info-banner eod-empty-hint">
              <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
              <span>{{ $t("endOfDayHint") }}</span>
            </div>
            <button type="button" class="eod-empty-btn" @click="fetchReport">
              <b-icon icon="search"></b-icon>
              {{ $t("generateTodayReport") || "استخراج تقرير اليوم" }}
            </button>
          </div>

          <template v-if="report">
            <div class="report-info-banner eod-period-banner">
              <b-icon icon="clock-history" class="banner-icon"></b-icon>
              <div>
                <span class="eod-period-label">{{ $t("endOfDayPeriod") || "فترة التقرير" }}</span>
                <strong class="eod-period-value">{{ reportPeriodLabel }}</strong>
              </div>
            </div>

            <div class="app-overview-grid eod-overview-grid">
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                  <b-icon icon="receipt-cutoff"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ report.totals.ordersCount || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("orders") || "الفواتير" }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--info">
                  <b-icon icon="currency-dollar"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value app-overview-stat-value--text">
                    {{ formatPrice(report.totals.grossSales) }} {{ $t("currency") }}
                  </div>
                  <div class="app-overview-stat-label">{{ $t("grossSales") || "إجمالي المبيعات" }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                  <b-icon icon="percent"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value app-overview-stat-value--text">
                    {{ formatPrice(report.totals.discountAmount) }} {{ $t("currency") }}
                  </div>
                  <div class="app-overview-stat-label">{{ $t("discountLabel") || "الخصم" }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--success">
                  <b-icon icon="cash-stack"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value app-overview-stat-value--text">
                    {{ formatPrice(report.totals.netSales) }} {{ $t("currency") }}
                  </div>
                  <div class="app-overview-stat-label">{{ $t("netSales") || "صافي المبيعات" }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--success">
                  <b-icon icon="graph-up"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value app-overview-stat-value--text">
                    {{ formatPrice(report.totals.profit) }} {{ $t("currency") }}
                  </div>
                  <div class="app-overview-stat-label">
                    {{ $t("profitReport") || "الربح" }}
                    <span v-if="profitMargin" class="eod-stat-extra">({{ profitMargin }}%)</span>
                  </div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                  <b-icon icon="arrow-counterclockwise"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ report.totals.returnedCount || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("returnedItemsReport") || "المواد المسترجعة" }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                  <b-icon icon="box-seam"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ report.totals.itemsQuantity || 0 }}</div>
                  <div class="app-overview-stat-label">
                    {{ $t("itemsSoldToday") || "المواد المباعة" }}
                    <span class="eod-stat-extra">· {{ report.totals.itemsCount || 0 }} {{ $t("uniqueItems") || "صنف" }}</span>
                  </div>
                </div>
              </div>
            </div>

            <div class="app-section-card" v-if="showTableSections">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap">
                    <b-icon icon="grid-3x3-gap-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("tablesStatusSummary") || "ملخص حالات الطاولات" }}</h3>
                  </div>
                </div>
              </div>
              <div class="app-section-body">
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

            <div class="app-section-card">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap eod-icon--payment">
                    <b-icon icon="credit-card-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("paymentMethod") || "طريقة الدفع" }}</h3>
                    <p class="app-section-subtitle">{{ $t("paymentBreakdownHint") || "توزيع المبيعات حسب طريقة الدفع" }}</p>
                  </div>
                </div>
              </div>
              <div class="app-section-body">
                <div v-if="(report.paymentBreakdown || []).length" class="eod-payment-grid">
                  <div
                    v-for="row in report.paymentBreakdown"
                    :key="row.method"
                    class="eod-payment-card"
                    :class="`eod-payment-card--${paymentMethodKey(row.method)}`"
                  >
                    <span class="eod-payment-card-icon">
                      <b-icon :icon="paymentMethodIcon(row.method)"></b-icon>
                    </span>
                    <span class="eod-payment-card-method">{{ paymentMethodLabel(row.method) }}</span>
                    <span class="eod-payment-card-amount">{{ formatPrice(row.amount) }} {{ $t("currency") }}</span>
                    <span class="eod-payment-card-count">
                      {{ row.ordersCount || 0 }} {{ $t("orders") || "فاتورة" }}
                    </span>
                  </div>
                </div>
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>

            <div class="app-section-card" v-if="showTableSections">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap eod-icon--tables">
                    <b-icon icon="table"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("invoicesByTable") || "عدد الفواتير لكل طاولة" }}</h3>
                  </div>
                </div>
              </div>
              <div class="app-section-body">
                <div v-if="(report.invoicesByTable || []).length" class="report-table-container">
                  <table class="report-table">
                    <thead>
                      <tr>
                        <th>{{ $t("table") || "الطاولة" }}</th>
                        <th>{{ $t("orders") || "الفواتير" }}</th>
                        <th>{{ $t("totalAmount") || "المبلغ" }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="row in report.invoicesByTable" :key="row.tableNumber">
                        <td><span class="report-item-code">{{ row.tableNumber }}</span></td>
                        <td><span class="quantity-badge">{{ row.invoicesCount }}</span></td>
                        <td><span class="report-item-price">{{ formatPrice(row.totalAmount) }} {{ $t("currency") }}</span></td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>

            <div class="app-section-card">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap eod-icon--top">
                    <b-icon icon="trophy-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("topSellingItems") || "الأكثر مبيعاً" }}</h3>
                    <p class="app-section-subtitle">{{ $t("topSellingItemsDescription") || "أفضل المنتجات مبيعاً اليوم" }}</p>
                  </div>
                </div>
              </div>
              <div class="app-section-body">
                <div v-if="(report.topItems || []).length" class="report-table-container">
                  <table class="report-table">
                    <thead>
                      <tr>
                        <th class="report-item-rank">#</th>
                        <th>{{ $t("itemName") || "المادة" }}</th>
                        <th>{{ $t("quantity") || "الكمية" }}</th>
                        <th>{{ $t("totalSales") || "المبيعات" }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(item, index) in report.topItems" :key="item.itemId || index">
                        <td class="report-item-rank">
                          <span class="rank-badge" :class="getRankClass(index)">{{ index + 1 }}</span>
                        </td>
                        <td><span class="report-item-name">{{ item.itemName }}</span></td>
                        <td><span class="quantity-badge">{{ item.quantity }}</span></td>
                        <td><span class="report-item-price">{{ formatPrice(item.salesAmount) }} {{ $t("currency") }}</span></td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <p v-else class="eod-section-empty">{{ $t("noDataForSection") || "لا توجد بيانات" }}</p>
              </div>
            </div>

            <div class="app-section-card">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap eod-icon--return">
                    <b-icon icon="arrow-counterclockwise"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("returnedItemsReport") || "المواد المسترجعة" }}</h3>
                  </div>
                </div>
              </div>
              <div class="app-section-body">
                <div v-if="(report.returnedItems || []).length" class="report-table-container">
                  <table class="report-table">
                    <thead>
                      <tr>
                        <th>{{ $t("invoiceNumber") || "الفاتورة" }}</th>
                        <th>{{ $t("itemName") || "المادة" }}</th>
                        <th>{{ $t("quantity") || "الكمية" }}</th>
                        <th>{{ $t("lineTotal") || "المجموع" }}</th>
                        <th>{{ $t("deletedBy") || "حذف بواسطة" }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(row, index) in report.returnedItems" :key="index">
                        <td><span class="report-item-code">{{ row.orderCode }}</span></td>
                        <td><span class="report-item-name">{{ row.itemName }}</span></td>
                        <td><span class="quantity-badge">{{ row.quantity }}</span></td>
                        <td><span class="report-item-price">{{ formatPrice(row.lineTotal) }} {{ $t("currency") }}</span></td>
                        <td>{{ row.deletedByUsername || "—" }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <p v-else class="eod-section-empty">{{ $t("eodNoReturnsToday") || "لا توجد مواد مسترجعة اليوم" }}</p>
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
      const start = this.formatDateTime(this.report.dayStart);
      const end = this.formatDateTime(this.report.dayEnd);
      return `${start} — ${end}`;
    },
    profitMargin() {
      const net = Number(this.report?.totals?.netSales || 0);
      const profit = Number(this.report?.totals?.profit || 0);
      if (!net || !Number.isFinite(net)) return null;
      return ((profit / net) * 100).toFixed(1);
    },
    showTableSections() {
      const status = this.report?.tableStatus;
      const hasTables = status && Number(status.totalTables || 0) > 0;
      const hasInvoicesByTable = (this.report?.invoicesByTable || []).length > 0;
      return hasTables || hasInvoicesByTable;
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
  },
  methods: {
    formatPrice(value) {
      const n = Number(value || 0);
      const locale = this.$i18n?.locale === "en" ? "en" : "ar-IQ";
      return Number.isFinite(n) ? n.toLocaleString(locale) : "0";
    },
    paymentMethodLabel(method) {
      const labels = {
        Cash: this.$t("cash") || "نقد",
        Card: this.$t("card") || "بطاقة",
        Credit: this.$t("credit") || "دفع لاحق",
        BankTransfer: this.$t("bankTransfer") || "تحويل",
      };
      return labels[method] || method || "—";
    },
    paymentMethodKey(method) {
      return String(method || "other").toLowerCase();
    },
    paymentMethodIcon(method) {
      const icons = {
        Cash: "cash-stack",
        Card: "credit-card-fill",
        Credit: "wallet2",
        BankTransfer: "bank",
      };
      return icons[method] || "cash-coin";
    },
    getRankClass(index) {
      if (index === 0) return "rank-gold";
      if (index === 1) return "rank-silver";
      if (index === 2) return "rank-bronze";
      return "";
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
          this.$notify.success(this.$t("endOfDayReportReady") || "تم استخراج التقرير بنجاح", {
            timeout: 2500,
            maxToasts: 1,
          });
        }
      } catch (error) {
        this.report = null;
        this.$notify.error(
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
        const matchedName = disposition.match(/filename.?=(?:UTF-8''|")?([^.;]+)/i);
        const fallbackName = `end_of_day_${new Date().toISOString().split("T")[0]}.xlsx`;
        link.download = decodeURIComponent((matchedName?.[1] || fallbackName).replace(/"/g, ""));
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        this.$notify.success(this.$t("endOfDayExportSuccess") || "تم تنزيل الملف بنجاح", {
          timeout: 2500,
          maxToasts: 1,
        });
      } catch (error) {
        this.$notify.error(
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
  gap: 0.65rem;
}

.eod-empty-icon-wrap {
  width: 4rem;
  height: 4rem;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--primary-color) 12%, var(--bg-primary));
  margin-bottom: 0.25rem;
}

.eod-empty-icon {
  font-size: 1.75rem;
  color: var(--primary-color);
}

.eod-empty-state h3 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 700;
  color: var(--text-primary);
}

.eod-empty-text {
  margin: 0;
  color: var(--text-secondary);
  font-size: 0.9rem;
  max-width: 28rem;
}

.eod-empty-hint {
  max-width: 32rem;
  text-align: start;
  margin: 0.25rem 0 0.5rem;
}

.eod-empty-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.65rem 1.2rem;
  border: none;
  border-radius: var(--radius-md, 0.55rem);
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
  margin-bottom: 1rem;
}

.eod-period-label {
  display: block;
  font-size: 0.72rem;
  color: var(--text-secondary);
  font-weight: 600;
  margin-bottom: 0.15rem;
}

.eod-period-value {
  display: block;
  font-size: 0.92rem;
  color: var(--text-primary);
  font-weight: 700;
}

.eod-overview-grid {
  margin-bottom: 1.25rem;
}

.eod-stat-extra {
  font-weight: 500;
  color: var(--text-secondary);
  font-size: 0.68rem;
}

.eod-icon--payment {
  background: rgba(16, 185, 129, 0.14);
  color: #059669;
}

.eod-icon--tables {
  background: rgba(59, 130, 246, 0.14);
  color: #2563eb;
}

.eod-icon--top {
  background: rgba(245, 158, 11, 0.16);
  color: #d97706;
}

.eod-icon--return {
  background: rgba(239, 68, 68, 0.12);
  color: #dc2626;
}

.eod-section-empty {
  margin: 0;
  text-align: center;
  color: var(--text-secondary);
  font-size: 0.88rem;
  padding: 1.5rem 0;
}

.eod-payment-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.75rem;
}

.eod-payment-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.35rem;
  padding: 1rem 0.75rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.eod-payment-card-icon {
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.55rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 1rem;
  background: rgba(99, 102, 241, 0.12);
  color: #4f46e5;
}

.eod-payment-card--cash .eod-payment-card-icon {
  background: rgba(16, 185, 129, 0.14);
  color: #059669;
}

.eod-payment-card--card .eod-payment-card-icon {
  background: rgba(59, 130, 246, 0.14);
  color: #2563eb;
}

.eod-payment-card--credit .eod-payment-card-icon {
  background: rgba(245, 158, 11, 0.14);
  color: #d97706;
}

.eod-payment-card-method {
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--text-primary);
}

.eod-payment-card-amount {
  font-size: 1rem;
  font-weight: 800;
  color: var(--primary-color);
  font-variant-numeric: tabular-nums;
}

.eod-payment-card-count {
  font-size: 0.72rem;
  color: var(--text-secondary);
  font-weight: 600;
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
  border: 1px solid var(--border-color);
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
  border-color: rgba(16, 185, 129, 0.35);
  background: rgba(16, 185, 129, 0.06);
}

.eod-table-status-chip--occupied {
  border-color: rgba(239, 68, 68, 0.35);
  background: rgba(239, 68, 68, 0.06);
}

.eod-table-status-chip--reserved {
  border-color: rgba(245, 158, 11, 0.4);
  background: rgba(245, 158, 11, 0.08);
}

.eod-table-status-chip--out {
  border-color: rgba(148, 163, 184, 0.45);
  background: rgba(148, 163, 184, 0.08);
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

.spinning {
  animation: eod-spin 1s linear infinite;
}

@keyframes eod-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 992px) {
  .eod-table-status-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 768px) {
  .eod-table-status-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .eod-payment-grid {
    grid-template-columns: 1fr 1fr;
  }
}
</style>
