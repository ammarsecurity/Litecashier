<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <b-overlay
        :show="show"
        spinner-variant="primary"
        spinner-type="border"
        rounded="sm"
      >
        <div class="app-page-container">
          <div class="app-page-content dashboard-page">
            <div class="users-header-section">
              <div class="users-header-content app-header-row">
                <div class="header-title-wrapper">
                  <div class="header-icon-wrapper">
                    <b-icon icon="speedometer2" class="header-icon"></b-icon>
                  </div>
                  <div>
                    <h1 class="users-page-title">{{ $t("welcomeToDashboard") || "لوحة التحكم" }}</h1>
                    <p class="header-subtitle">{{ $t("dashboardSubtitle") || "نظرة شاملة على إحصائيات متجرك" }}</p>
                  </div>
                </div>
                <div class="app-header-actions">
                  <button type="button" class="btn-refresh" @click="refreshPage" :disabled="show">
                    <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                    <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                  </button>
                  <router-link to="/sections" class="users-add-button dashboard-sections-link">
                    <b-icon icon="grid-3x3-gap-fill" class="button-icon"></b-icon>
                    <span class="button-text">{{ $t("systemModules") || "أقسام النظام" }}</span>
                  </router-link>
                </div>
              </div>
            </div>

            <div class="app-overview-grid">
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                  <b-icon icon="receipt-cutoff"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ stats.orders?.total || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("all_sales") }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--success">
                  <b-icon icon="currency-dollar"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value app-overview-stat-value--text">
                    {{ formattedNumber(stats.salesAmount?.total || 0) }} {{ $t("currency") }}
                  </div>
                  <div class="app-overview-stat-label">{{ $t("salesAmountStatisticsLabel") }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--info">
                  <b-icon icon="box"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ stats.products?.total || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("Items") }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                  <b-icon icon="people"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ stats.users?.total || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("all_accounts") }}</div>
                </div>
              </div>
            </div>

            <!-- Invoice Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="receipt-cutoff" class="section-title-icon"></b-icon>
                  {{ $t("invoiceStatisticsLabel") }}
                </h2>
              </div>
              <div class="app-overview-grid stats-grid">
                <StatCard
                  color="primary"
                  :value="stats.orders?.total || 0"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="receipt-cutoff"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="stats.orders?.today || 0"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.orders?.thisWeek || 0"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="stats.orders?.thisMonth || 0"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Items Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="box" class="section-title-icon"></b-icon>
                  {{ $t("itemsStatisticsLabel") }}
                </h2>
              </div>
              <div class="app-overview-grid stats-grid">
                <StatCard
                  color="primary"
                  :value="stats.items?.total || 0"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="box"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="stats.items?.today || 0"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.items?.thisWeek || 0"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="stats.items?.thisMonth || 0"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Sales Amount Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="currency-dollar" class="section-title-icon"></b-icon>
                  {{ $t("salesAmountStatisticsLabel") }}
                </h2>
              </div>
              <div class="app-overview-grid stats-grid">
                <StatCard
                  color="primary"
                  :value="formattedNumber(stats.salesAmount?.total || 0) + ' ' + $t('currency')"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="currency-dollar"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="formattedNumber(stats.salesAmount?.today || 0) + ' ' + $t('currency')"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="formattedNumber(stats.salesAmount?.thisWeek || 0) + ' ' + $t('currency')"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="formattedNumber(stats.salesAmount?.thisMonth || 0) + ' ' + $t('currency')"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Additional Statistics -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="graph-up" class="section-title-icon"></b-icon>
                  {{ $t("additionalStats") || "إحصائيات إضافية" }}
                </h2>
              </div>
              <div class="app-overview-grid stats-grid">
                <StatCard
                  color="info"
                  :value="stats.products?.total || 0"
                  :label="$t('Items') + ' (' + $t('totalLabel') + ')'"
                >
                  <template #icon>
                    <b-icon icon="box"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="warning"
                  :value="stats.users?.total || 0"
                  :label="$t('all_accounts')"
                >
                  <template #icon>
                    <b-icon icon="people-fill"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.categories?.total || 0"
                  :label="$t('all_categories')"
                >
                  <template #icon>
                    <b-icon icon="tags-fill"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Recent Invoices -->
            <div class="app-section-card dashboard-invoices-card">
              <div class="app-section-header app-section-header--toolbar">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap">
                    <b-icon icon="receipt-cutoff"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("invoiceListTitle") || "قائمة الفواتير" }}</h3>
                    <p class="app-section-subtitle">{{ $t("invoiceListHint") || "بحث وعرض تفاصيل الفواتير" }}</p>
                  </div>
                </div>
                <button
                  type="button"
                  class="users-form-cancel-button section-view-details-btn"
                  @click="showInvoiceDetails = !showInvoiceDetails"
                >
                  <b-icon :icon="showInvoiceDetails ? 'chevron-up' : 'chevron-down'"></b-icon>
                  {{ showInvoiceDetails ? ($t("hideDetails") || "إخفاء") : ($t("viewDetails") || "عرض الفواتير") }}
                </button>
              </div>
              <div v-if="showInvoiceDetails" class="app-section-body dashboard-invoices-body">
                <div class="invoice-details-section">
                  <div class="app-filters-panel app-filters-panel--inset">
                    <div class="app-filters-panel-head">
                      <div class="app-filters-panel-title">
                        <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                        <div>
                          <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                          <p>{{ $t("dashboardInvoiceFiltersHint") || "تصفية فواتير لوحة التحكم بالتاريخ أو رقم الطلب" }}</p>
                        </div>
                      </div>
                      <div
                        class="app-filters-panel-actions"
                        v-if="invoiceFilters.startDate || invoiceFilters.endDate || invoiceFilters.search"
                      >
                        <button
                          type="button"
                          class="users-filter-clear-btn app-filters-clear-btn"
                          @click="clearInvoiceFilters"
                        >
                          <b-icon icon="x-circle" class="me-1"></b-icon>
                          {{ $t("clearFilters") || "مسح الفلاتر" }}
                        </button>
                      </div>
                    </div>
                    <div class="app-filters-fields app-filters-fields--3">
                      <label class="app-filter-field">
                        <span class="app-filter-label">{{ $t("from_date") || "من تاريخ" }}</span>
                        <div class="users-search-container">
                          <b-icon icon="calendar" class="search-icon"></b-icon>
                          <input
                            v-model="invoiceFilters.startDate"
                            type="date"
                            class="users-search-input"
                            @change="loadInvoices"
                          />
                        </div>
                      </label>
                      <label class="app-filter-field">
                        <span class="app-filter-label">{{ $t("to_date") || "إلى تاريخ" }}</span>
                        <div class="users-search-container">
                          <b-icon icon="calendar-check" class="search-icon"></b-icon>
                          <input
                            v-model="invoiceFilters.endDate"
                            type="date"
                            class="users-search-input"
                            @change="loadInvoices"
                          />
                        </div>
                      </label>
                      <label class="app-filter-field app-filter-field--grow">
                        <span class="app-filter-label">{{ $t("search") || "بحث" }}</span>
                        <div class="users-search-container">
                          <b-icon icon="search" class="search-icon"></b-icon>
                          <input
                            v-model="invoiceFilters.search"
                            type="search"
                            class="users-search-input"
                            :placeholder="$t('searchByOrderCode') || 'ابحث برقم الطلب'"
                            @input="debounceInvoiceSearch"
                          />
                        </div>
                      </label>
                    </div>
                  </div>

                  <div class="invoice-table-section">
                    <div v-if="loadingInvoices" class="loading-state">
                      <b-spinner small></b-spinner>
                      <span>{{ $t("loading") || "جاري التحميل..." }}</span>
                    </div>
                    <div v-else-if="invoices.length > 0" class="invoice-table-wrapper">
                      <table class="invoice-table">
                        <thead>
                          <tr>
                            <th>{{ $t("orderCode") || "رقم الطلب" }}</th>
                            <th>{{ $t("date") || "التاريخ" }}</th>
                            <th>{{ $t("paymentMethod") || "طريقة الدفع" }}</th>
                            <th>{{ $t("total") || "المجموع" }}</th>
                            <th>{{ $t("actions") || "الإجراءات" }}</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr v-for="invoice in invoices" :key="invoice.id">
                            <td>{{ invoice.orderCode || "-" }}</td>
                            <td>{{ formatDate(invoice.createdAt || invoice.insertDate) }}</td>
                            <td>{{ getPaymentMethodText(invoice.paymentMethod) }}
                              <span v-if="invoice.isWholesale" class="report-wholesale-badge">{{ $t("wholesalePriceMode") || "جملة" }}</span>
                            </td>
                            <td>{{ formatPrice(invoice.orderTotalAfterDiscount ?? invoice.total ?? invoice.orderPrice ?? 0) }} {{ $t("currency") }}</td>
                            <td>
                              <button
                                class="invoice-action-btn"
                                @click="viewInvoiceDetails(invoice)"
                                :title="$t('viewDetails') || 'عرض التفاصيل'"
                              >
                                <b-icon icon="eye"></b-icon>
                              </button>
                            </td>
                          </tr>
                        </tbody>
                      </table>

                      <div class="invoice-pagination">
                        <button
                          class="pagination-btn"
                          @click="previousInvoicePage"
                          :disabled="invoicePageNumber === 1"
                        >
                          <b-icon icon="chevron-right"></b-icon>
                          {{ $t("previous") || "السابق" }}
                        </button>
                        <span class="pagination-info">
                          {{ $t("page") || "صفحة" }} {{ invoicePageNumber }} {{ $t("of") || "من" }} {{ totalInvoicePages }}
                        </span>
                        <button
                          class="pagination-btn"
                          @click="nextInvoicePage"
                          :disabled="invoicePageNumber >= totalInvoicePages"
                        >
                          {{ $t("next") || "التالي" }}
                          <b-icon icon="chevron-left"></b-icon>
                        </button>
                      </div>
                    </div>
                    <div v-else class="empty-invoices-state">
                      <b-icon icon="receipt" class="empty-icon"></b-icon>
                      <p>{{ $t("noInvoicesFound") || "لا توجد فواتير" }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </b-overlay>
    </div>

    <b-modal
      v-model="showInvoiceModal"
      :title="$t('invoiceDetails') || 'تفاصيل الفاتورة'"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
      @hidden="selectedInvoice = null"
    >
      <div class="modal-content-wrapper" v-if="selectedInvoice">
        <h2 class="modal-title">{{ $t("invoiceDetails") || "تفاصيل الفاتورة" }}</h2>
        <div class="invoice-details-content">
          <div class="invoice-details-grid">
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("orderCode") || "رقم الطلب" }}</label>
              <span class="invoice-detail-value">{{ selectedInvoice.orderCode || "-" }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("date") || "التاريخ" }}</label>
              <span class="invoice-detail-value">{{ formatDate(selectedInvoice.createdAt || selectedInvoice.insertDate) }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("paymentMethod") || "طريقة الدفع" }}</label>
              <span class="invoice-detail-value">{{ getPaymentMethodText(selectedInvoice.paymentMethod) }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("priceModeLabel") || "نوع السعر" }}</label>
              <span class="invoice-detail-value">{{ selectedInvoice.isWholesale ? ($t("wholesalePriceMode") || "جملة") : ($t("retailPriceMode") || "مفرد") }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("total") || "المجموع" }}</label>
              <span class="invoice-detail-value invoice-total">{{ formatPrice(selectedInvoice.orderTotalAfterDiscount ?? selectedInvoice.total ?? selectedInvoice.orderPrice ?? 0) }} {{ $t("currency") }}</span>
            </div>
            <div class="invoice-detail-item" v-if="Number(selectedInvoice.discountAmount || 0) > 0">
              <label class="invoice-detail-label">{{ $t("discountLabel") || "الخصم" }}</label>
              <span class="invoice-detail-value">- {{ formatPrice(selectedInvoice.discountAmount || 0) }} {{ $t("currency") }}</span>
            </div>
          </div>

          <div v-if="activeInvoiceItems.length > 0" class="invoice-items-section">
            <h3 class="invoice-items-title">{{ $t("orderItems") || "عناصر الطلب" }}</h3>
            <table class="invoice-items-table">
              <thead>
                <tr>
                  <th>{{ $t("itemName") || "اسم المنتج" }}</th>
                  <th>{{ $t("quantity") || "الكمية" }}</th>
                  <th>{{ $t("price") || "السعر" }}</th>
                  <th>{{ $t("total") || "المجموع" }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in activeInvoiceItems" :key="index">
                  <td>{{ item.item?.name || "-" }}</td>
                  <td>{{ item.quantity || 0 }}</td>
                  <td>{{ formatPrice(item.sellingPrice || 0) }} {{ $t("currency") }}</td>
                  <td>{{ formatPrice((item.sellingPrice || 0) * (item.quantity || 0)) }} {{ $t("currency") }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <div class="users-form-actions">
          <button type="button" class="users-form-cancel-button" @click="showInvoiceModal = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../http/api.js";
import StatCard from "@/components/StatCard.vue";
import { formatBusinessDateTime } from "@/utils/formatBusinessDateTime.js";

export default {
  name: "DashboardView",
  components: {
    AppHeader,
    StatCard,
  },
  data() {
    return {
      stats: {
        orders: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        items: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        salesAmount: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        products: {
          total: 0,
          active: 0,
        },
        users: {
          total: 0,
          active: 0,
        },
        categories: {
          total: 0,
          active: 0,
        },
      },
      show: false,
      showInvoiceDetails: false,
      loadingInvoices: false,
      invoices: [],
      totalInvoices: 0,
      invoicePageNumber: 1,
      invoicePageSize: 10,
      invoiceFilters: {
        startDate: "",
        endDate: "",
        search: "",
      },
      invoiceSearchTimer: null,
      selectedInvoice: null,
      showInvoiceModal: false,
    };
  },
  computed: {
    role() {
      return localStorage.getItem("role");
    },
    totalInvoicePages() {
      return Math.max(1, Math.ceil(this.totalInvoices / this.invoicePageSize));
    },
    activeInvoiceItems() {
      if (!this.selectedInvoice?.customerOrderItem) return [];
      return this.selectedInvoice.customerOrderItem.filter((item) => !item.isDeleted);
    },
  },
  watch: {
    showInvoiceDetails(newVal) {
      if (newVal && this.invoices.length === 0) {
        this.loadInvoices();
      }
    },
  },
  mounted() {
    this.getDashboardStats();
  },
  methods: {
    refreshPage() {
      this.getDashboardStats();
    },
    formattedNumber(info) {
      if (typeof info === 'number') {
        return info.toLocaleString("en-EG");
      }
      return info || "0";
    },
    getDashboardStats() {
      this.show = true;
      HTTP.get(`Admin/GetDashboardStats`)
        .then((response) => {
          if (response.data && response.data.data) {
            this.stats = response.data.data;
          }
          this.show = false;
        })
        .catch((error) => {
          console.error("Error fetching dashboard stats:", error);
          this.show = false;
        });
    },
    async loadInvoices() {
      try {
        this.loadingInvoices = true;
        const params = new URLSearchParams({
          pageNumber: (this.invoicePageNumber - 1).toString(),
          pageSize: this.invoicePageSize.toString(),
        });

        if (this.invoiceFilters.startDate) {
          params.append("startDate", this.invoiceFilters.startDate);
        }
        if (this.invoiceFilters.endDate) {
          params.append("endDate", this.invoiceFilters.endDate);
        }
        if (this.invoiceFilters.search) {
          params.append("info", this.invoiceFilters.search);
        }

        const response = await HTTP.get(`Admin/GetOrders?${params.toString()}`);
        if (response.data && response.data.data) {
          this.invoices = response.data.data.items || [];
          this.totalInvoices = response.data.data.totalItems || 0;
        } else {
          this.invoices = [];
          this.totalInvoices = 0;
        }
      } catch (error) {
        console.error("Error loading invoices:", error);
        this.invoices = [];
        this.totalInvoices = 0;
        this.$notify.error(this.$i18n.t("errorLoadingInvoices") || "حدث خطأ أثناء تحميل الفواتير", {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.loadingInvoices = false;
      }
    },
    debounceInvoiceSearch() {
      clearTimeout(this.invoiceSearchTimer);
      this.invoiceSearchTimer = setTimeout(() => {
        this.invoicePageNumber = 1;
        this.loadInvoices();
      }, 500);
    },
    clearInvoiceFilters() {
      this.invoiceFilters = { startDate: "", endDate: "", search: "" };
      this.invoicePageNumber = 1;
      this.loadInvoices();
    },
    previousInvoicePage() {
      if (this.invoicePageNumber > 1) {
        this.invoicePageNumber--;
        this.loadInvoices();
      }
    },
    nextInvoicePage() {
      if (this.invoicePageNumber < this.totalInvoicePages) {
        this.invoicePageNumber++;
        this.loadInvoices();
      }
    },
    formatDate(dateString) {
      if (!dateString) return "-";
      return formatBusinessDateTime(dateString);
    },
    formatPrice(price) {
      if (price !== null && price !== undefined && !isNaN(price)) {
        return parseFloat(price).toLocaleString("en-EG");
      }
      return "0";
    },
    getPaymentMethodText(method) {
      const methods = {
        Cash: this.$t("cash") || "نقدي",
        Card: this.$t("card") || "بطاقة",
        Credit: this.$t("credit") || "آجل",
      };
      return methods[method] || method || "-";
    },
    viewInvoiceDetails(invoice) {
      this.selectedInvoice = invoice;
      this.showInvoiceModal = true;
    },
  },
};
</script>

<style scoped>
.dashboard-invoices-body {
  padding-top: 0.5rem;
}

.invoice-filters-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.invoice-filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.invoice-filter-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
}

.invoice-filter-input {
  padding: 0.75rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.9375rem;
}

.invoice-filter-clear-btn {
  padding: 0.75rem 1.5rem;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  color: var(--text-primary);
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: 1.5rem;
}

.invoice-table-wrapper {
  overflow-x: auto;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
}

.invoice-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9375rem;
}

.invoice-table thead {
  background: var(--bg-secondary);
  border-bottom: 2px solid var(--border-color);
}

.invoice-table th,
.invoice-table td {
  padding: 1rem;
  text-align: right;
  border-bottom: 1px solid var(--border-color);
}

.invoice-action-btn {
  padding: 0.5rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  color: var(--primary-color);
  cursor: pointer;
}

.invoice-pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-top: 1.5rem;
  padding: 1rem;
}

.pagination-btn {
  padding: 0.5rem 1rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  cursor: pointer;
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.empty-invoices-state {
  text-align: center;
  padding: 3rem 1rem;
  color: var(--text-secondary);
}

.loading-state {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem;
  justify-content: center;
}

.invoice-details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.invoice-items-table {
  width: 100%;
  border-collapse: collapse;
}

.invoice-items-table th,
.invoice-items-table td {
  padding: 0.75rem;
  border-bottom: 1px solid var(--border-color);
  text-align: right;
}
</style>

