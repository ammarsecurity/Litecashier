<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content card-payments-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="credit-card-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("cardPaymentTransactions") || "معاملات البطاقة" }}</h1>
                  <p class="header-subtitle">
                    {{ $t("cardPaymentTransactionsDescription") || "سجل عمليات الدفع عبر جهاز PAX" }}
                  </p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="loadTransactions" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="list-ul"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ total }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("cardPaymentsTotal") || "إجمالي المعاملات" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ successCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("success") || "ناجحة" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="x-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ failedCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("failed") || "فاشلة" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="cash-stack"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ formatAmount(successAmountTotal) }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("cardPaymentsSuccessAmount") || "مبالغ ناجحة (د.ع)" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card card-payments-link-section">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="link-45deg"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("cardPaymentLinkResults") || "نتائج الربط" }}</h3>
                  <p class="app-section-subtitle">
                    {{ $t("cardPaymentLinkResultsHint") || "مطابقة معاملات البطاقة مع الطلبات وإعادة الفحص" }}
                  </p>
                </div>
              </div>
              <button
                type="button"
                class="users-add-button users-add-button--compact"
                @click="recheckNotMatched"
                :disabled="rechecking || loading"
              >
                <b-spinner small v-if="rechecking" class="me-1"></b-spinner>
                <b-icon v-else icon="arrow-repeat" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("recheckUnmatched") || "إعادة فحص غير المطابقة" }}</span>
              </button>
            </div>
            <div class="card-payments-link-stats">
              <div class="card-payments-link-stat card-payments-link-stat--matched">
                <span class="link-stat-value">{{ matchedLinkCount }}</span>
                <span class="link-stat-label">{{ $t("linkStatusMatched") || "مطابق" }}</span>
              </div>
              <div class="card-payments-link-stat card-payments-link-stat--unmatched">
                <span class="link-stat-value">{{ unmatchedLinkCount }}</span>
                <span class="link-stat-label">{{ $t("linkStatusUnmatched") || "غير مطابق" }}</span>
              </div>
              <div class="card-payments-link-stat card-payments-link-stat--failed">
                <span class="link-stat-value">{{ failedLinkCount }}</span>
                <span class="link-stat-label">{{ $t("linkStatusFailed") || "فاشل" }}</span>
              </div>
              <div class="card-payments-link-stat card-payments-link-stat--pending">
                <span class="link-stat-value">{{ pendingLinkCount }}</span>
                <span class="link-stat-label">{{ $t("linkStatusPending") || "معلق" }}</span>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="receipt-cutoff"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("cardPaymentsList") || "سجل المعاملات" }}</h3>
                  <p class="app-section-subtitle">
                    {{ $t("cardPaymentsListHint") || "تصفية وعرض عمليات الدفع بالبطاقة وربطها بالفواتير" }}
                  </p>
                </div>
              </div>
            </div>

            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                    <p>{{ $t("cardPaymentsFiltersHint") || "تصفية معاملات البطاقة بالتاريخ والحالة والربط" }}</p>
                  </div>
                </div>
                <div class="app-filters-panel-actions">
                  <button type="button" class="btn-refresh" @click="applyFilters">
                    <b-icon icon="search" class="button-icon"></b-icon>
                    <span class="button-text">{{ $t("filter") || "تصفية" }}</span>
                  </button>
                  <button
                    v-if="hasActiveFilters"
                    type="button"
                    class="users-filter-clear-btn app-filters-clear-btn"
                    @click="clearFilters"
                  >
                    <b-icon icon="x-circle" class="me-1"></b-icon>
                    {{ $t("clearFilters") || "مسح الفلاتر" }}
                  </button>
                </div>
              </div>
              <div class="app-filters-fields">
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("startDate") || "من تاريخ" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="calendar" class="search-icon"></b-icon>
                    <input v-model="filters.startDate" type="date" class="users-search-input" />
                  </div>
                </label>
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("endDate") || "إلى تاريخ" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="calendar-check" class="search-icon"></b-icon>
                    <input v-model="filters.endDate" type="date" class="users-search-input" />
                  </div>
                </label>
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("status") || "الحالة" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="filter" class="search-icon"></b-icon>
                    <select v-model="filters.status" class="users-search-input reports-filter-select">
                      <option value="">{{ $t("all") || "الكل" }}</option>
                      <option value="Success">{{ $t("success") || "نجاح" }}</option>
                      <option value="Failed">{{ $t("failed") || "فشل" }}</option>
                      <option value="Pending">{{ $t("pending") || "معلق" }}</option>
                    </select>
                  </div>
                </label>
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("linkStatus") || "حالة الربط" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="link-45deg" class="search-icon"></b-icon>
                    <select v-model="filters.linkStatus" class="users-search-input reports-filter-select">
                      <option value="">{{ $t("all") || "الكل" }}</option>
                      <option value="Matched">{{ $t("linkStatusMatched") || "مطابق" }}</option>
                      <option value="Unmatched">{{ $t("linkStatusUnmatched") || "غير مطابق" }}</option>
                      <option value="Failed">{{ $t("linkStatusFailed") || "فاشل" }}</option>
                      <option value="Pending">{{ $t("linkStatusPending") || "معلق" }}</option>
                      <option value="NotMatched">{{ $t("linkStatusNotMatched") || "ليس مطابقاً" }}</option>
                    </select>
                  </div>
                </label>
              </div>
            </div>

            <div class="app-section-body card-payments-table-body">
              <div v-if="loading" class="loading-state">
                <b-spinner small></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="items.length > 0" class="report-table-container">
                <b-table
                  :items="items"
                  :fields="tableFields"
                  striped
                  hover
                  responsive
                  class="reports-table card-payments-table"
                  :empty-text="$t('noData') || 'لا توجد بيانات'"
                >
                  <template #cell(insertDate)="row">
                    <span class="card-payments-date">{{ formatDate(row.item.insertDate) }}</span>
                  </template>
                  <template #cell(orderNumber)="row">
                    <span v-if="formatOrderNumber(row.item)" class="card-payments-order" :title="formatOrderNumber(row.item)">
                      {{ formatOrderNumber(row.item) }}
                    </span>
                    <span v-else class="card-payments-unlinked">{{ $t("notLinkedToOrder") || "غير مربوط بطلب" }}</span>
                  </template>
                  <template #cell(amount)="row">
                    <span class="card-payments-amount">
                      {{ formatAmount(row.item.amount) }}
                      <small>{{ row.item.currencyCode || "IQD" }}</small>
                    </span>
                  </template>
                  <template #cell(cardNo)="row">
                    <span class="card-payments-mono">{{ row.item.cardNo || "—" }}</span>
                  </template>
                  <template #cell(authCode)="row">
                    <span class="card-payments-mono">{{ row.item.authCode || "—" }}</span>
                  </template>
                  <template #cell(refNo)="row">
                    <span class="card-payments-mono">{{ row.item.refNo || "—" }}</span>
                  </template>
                  <template #cell(deviceName)="row">
                    <span>{{ row.item.deviceName || "—" }}</span>
                  </template>
                  <template #cell(status)="row">
                    <span class="card-payments-status" :class="statusClass(row.item.status)">
                      {{ statusLabel(row.item.status) }}
                    </span>
                  </template>
                  <template #cell(linkStatus)="row">
                    <span class="card-payments-link-badge" :class="linkStatusClass(row.item.linkStatus)">
                      {{ linkStatusLabel(row.item.linkStatus) }}
                    </span>
                  </template>
                  <template #cell(actions)="row">
                    <div class="actions-cell card-payments-row-actions" role="group" :aria-label="$t('actions') || 'العمليات'">
                      <button
                        v-if="row.item.linkStatus !== 'Matched'"
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        @click="recheckSingle(row.item.id)"
                        :disabled="recheckingId === row.item.id"
                        :title="$t('recheck') || 'إعادة فحص'"
                        :aria-label="$t('recheck') || 'إعادة فحص'"
                      >
                        <b-spinner small v-if="recheckingId === row.item.id"></b-spinner>
                        <b-icon v-else icon="arrow-repeat" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--view"
                        @click="openDetail(row.item.id)"
                        :title="$t('viewDetails') || 'عرض التفاصيل'"
                        :aria-label="$t('viewDetails') || 'عرض التفاصيل'"
                      >
                        <b-icon icon="eye" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </template>
                </b-table>

                <div v-if="total > pageSize" class="card-payments-pagination">
                  <span class="card-payments-pagination-info">
                    {{ $t("showing") || "عرض" }} {{ items.length }} {{ $t("of") || "من" }} {{ total }}
                  </span>
                  <div class="card-payments-pagination-btns">
                    <button
                      type="button"
                      class="payment-device-btn payment-device-btn--outline"
                      :disabled="pageNumber <= 1"
                      @click="goToPage(pageNumber - 1)"
                    >
                      {{ $t("previous") || "السابق" }}
                    </button>
                    <span class="card-payments-page-num">{{ pageNumber }}</span>
                    <button
                      type="button"
                      class="payment-device-btn payment-device-btn--outline"
                      :disabled="pageNumber >= totalPages"
                      @click="goToPage(pageNumber + 1)"
                    >
                      {{ $t("next") || "التالي" }}
                    </button>
                  </div>
                </div>
              </div>
              <div v-else class="empty-state">
                <b-icon icon="credit-card" class="empty-icon"></b-icon>
                <p>{{ hasActiveFilters ? ($t("noResults") || "لا توجد نتائج") : ($t("noCardPayments") || "لا توجد معاملات بطاقة") }}</p>
                <button v-if="hasActiveFilters" type="button" class="empty-state-btn" @click="clearFilters">
                  <b-icon icon="x-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("clearFilters") || "مسح الفلاتر" }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal
      v-model="showDetailModal"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
      @hidden="selectedTx = null"
    >
      <div v-if="selectedTx" class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("transactionDetails") || "تفاصيل المعاملة" }}</h2>

        <div class="card-payments-detail-header">
          <span class="card-payments-status card-payments-status--lg" :class="statusClass(selectedTx.status)">
            {{ statusLabel(selectedTx.status) }}
          </span>
          <span class="card-payments-detail-id">#{{ selectedTx.id }}</span>
        </div>

        <div class="card-payments-detail-grid">
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("date") || "التاريخ" }}</span>
            <span class="detail-value">{{ formatDate(selectedTx.insertDate) }}</span>
          </div>
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("amount") || "المبلغ" }}</span>
            <span class="detail-value detail-value--amount">
              {{ formatAmount(selectedTx.amount) }} {{ selectedTx.currencyCode || "IQD" }}
            </span>
          </div>
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("authCode") || "رمز الموافقة" }}</span>
            <span class="detail-value card-payments-mono">{{ selectedTx.authCode || "—" }}</span>
          </div>
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("refNo") || "رقم المرجع" }}</span>
            <span class="detail-value card-payments-mono">{{ selectedTx.refNo || "—" }}</span>
          </div>
          <div class="card-payments-detail-item card-payments-detail-item--highlight">
            <span class="detail-label">{{ $t("orderNumber") || "رقم الطلب" }}</span>
            <span class="detail-value detail-value--order">
              {{ formatOrderNumberFromTx(selectedTx) || ($t("notLinkedToOrder") || "غير مربوط بطلب") }}
            </span>
          </div>
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("orderCode") || "رمز الفاتورة" }}</span>
            <span class="detail-value card-payments-mono">
              {{ getOrderCodeFromTx(selectedTx) || "—" }}
            </span>
          </div>
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("card") || "البطاقة" }}</span>
            <span class="detail-value card-payments-mono">{{ selectedTx.cardNo || "—" }}</span>
          </div>
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("terminalId") || "رقم الجهاز" }}</span>
            <span class="detail-value card-payments-mono">{{ selectedTx.terminalId || "—" }}</span>
          </div>
          <div class="card-payments-detail-item">
            <span class="detail-label">{{ $t("merchantName") || "اسم التاجر" }}</span>
            <span class="detail-value">{{ selectedTx.merchantName || "—" }}</span>
          </div>
          <div v-if="selectedTx.message" class="card-payments-detail-item card-payments-detail-item--full">
            <span class="detail-label">{{ $t("message") || "الرسالة" }}</span>
            <span class="detail-value">{{ selectedTx.message }}</span>
          </div>
        </div>

        <div v-if="selectedTx.rawResponse" class="card-payments-raw-block">
          <button type="button" class="card-payments-raw-toggle" @click="showRawResponse = !showRawResponse">
            <b-icon :icon="showRawResponse ? 'chevron-up' : 'chevron-down'"></b-icon>
            {{ $t("rawResponse") || "الاستجابة الخام" }}
          </button>
          <pre v-if="showRawResponse" class="card-payments-raw-response">{{ formatRawResponse(selectedTx.rawResponse) }}</pre>
        </div>

        <div class="users-form-actions">
          <button type="button" class="users-form-cancel-button" @click="showDetailModal = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </div>
      <div v-else class="modal-content-wrapper loading-state">
        <b-spinner small></b-spinner>
        <span>{{ $t("loading") }}</span>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";

export default {
  name: "CardPaymentsView",
  components: { AppHeader },
  data() {
    return {
      items: [],
      total: 0,
      loading: false,
      rechecking: false,
      recheckingId: null,
      selectedTx: null,
      showDetailModal: false,
      showRawResponse: false,
      pageNumber: 1,
      pageSize: 50,
      filters: {
        startDate: "",
        endDate: "",
        status: "",
        linkStatus: "",
      },
    };
  },
  computed: {
    tableFields() {
      return [
        { key: "insertDate", label: this.$t("date") || "التاريخ", sortable: true },
        { key: "orderNumber", label: this.$t("orderNumber") || "رقم الطلب" },
        { key: "amount", label: this.$t("amount") || "المبلغ" },
        { key: "cardNo", label: this.$t("card") || "البطاقة" },
        { key: "authCode", label: this.$t("authCode") || "رمز الموافقة" },
        { key: "refNo", label: this.$t("refNo") || "المرجع" },
        { key: "deviceName", label: this.$t("deviceName") || "الجهاز" },
        { key: "status", label: this.$t("status") || "الحالة" },
        { key: "linkStatus", label: this.$t("linkStatus") || "حالة الربط" },
        { key: "actions", label: "", class: "text-center" },
      ];
    },
    successCount() {
      return this.items.filter((x) => x.status === "Success").length;
    },
    failedCount() {
      return this.items.filter((x) => x.status === "Failed").length;
    },
    successAmountTotal() {
      return this.items
        .filter((x) => x.status === "Success")
        .reduce((sum, x) => sum + (Number(x.amount) || 0), 0);
    },
    matchedLinkCount() {
      return this.items.filter((x) => this.resolveLinkStatus(x) === "Matched").length;
    },
    unmatchedLinkCount() {
      return this.items.filter((x) => this.resolveLinkStatus(x) === "Unmatched").length;
    },
    failedLinkCount() {
      return this.items.filter((x) => this.resolveLinkStatus(x) === "Failed").length;
    },
    pendingLinkCount() {
      return this.items.filter((x) => this.resolveLinkStatus(x) === "Pending").length;
    },
    totalPages() {
      return Math.max(1, Math.ceil(this.total / this.pageSize));
    },
    hasActiveFilters() {
      return !!(
        this.filters.startDate ||
        this.filters.endDate ||
        this.filters.status ||
        this.filters.linkStatus
      );
    },
  },
  mounted() {
    this.loadTransactions();
  },
  methods: {
    async loadTransactions() {
      this.loading = true;
      try {
        const params = {
          pageNumber: this.pageNumber,
          pageSize: this.pageSize,
        };
        if (this.filters.startDate) params.startDate = this.filters.startDate;
        if (this.filters.endDate) params.endDate = this.filters.endDate;
        if (this.filters.status) params.status = this.filters.status;
        if (this.filters.linkStatus) params.linkStatus = this.filters.linkStatus;
        const res = await HTTP.get("CardPayments", { params });
        const data = res?.data?.data || {};
        this.items = data.items || [];
        this.total = data.total || 0;
      } catch (e) {
        console.error(e);
        this.$toast.error(this.$t("loadFailed") || "فشل التحميل");
      } finally {
        this.loading = false;
      }
    },
    applyFilters() {
      this.pageNumber = 1;
      this.loadTransactions();
    },
    clearFilters() {
      this.filters = { startDate: "", endDate: "", status: "", linkStatus: "" };
      this.pageNumber = 1;
      this.loadTransactions();
    },
    buildRecheckParams() {
      const params = { onlyNotMatched: true };
      if (this.filters.startDate) params.startDate = this.filters.startDate;
      if (this.filters.endDate) params.endDate = this.filters.endDate;
      if (this.filters.status) params.status = this.filters.status;
      return params;
    },
    async recheckNotMatched() {
      this.rechecking = true;
      try {
        await HTTP.post("CardPayments/recheck", null, {
          params: this.buildRecheckParams(),
        });
        this.$toast.success(this.$t("cardPaymentRecheckCompleted") || "تم إعادة الفحص", {
          timeout: 4000,
        });
        await this.loadTransactions();
      } catch (e) {
        console.error(e);
        this.$toast.error(this.$t("cardPaymentRecheckFailed") || "فشل إعادة الفحص");
      } finally {
        this.rechecking = false;
      }
    },
    async recheckSingle(id) {
      this.recheckingId = id;
      try {
        const res = await HTTP.post(`CardPayments/${id}/recheck`);
        const data = res?.data?.data || {};
        if (data.linkStatus === "Matched") {
          this.$toast.success(this.$t("cardPaymentLinkMatched") || "تم ربط المعاملة بالطلب");
        } else if (data.changed) {
          this.$toast.info(this.$t("cardPaymentRecheckCompleted") || "تم إعادة الفحص");
        } else {
          this.$toast.info(this.$t("cardPaymentNoChanges") || "لا تغييرات");
        }
        await this.loadTransactions();
      } catch (e) {
        console.error(e);
        this.$toast.error(this.$t("cardPaymentRecheckFailed") || "فشل إعادة الفحص");
      } finally {
        this.recheckingId = null;
      }
    },
    goToPage(page) {
      const next = Math.min(Math.max(page, 1), this.totalPages);
      if (next === this.pageNumber) return;
      this.pageNumber = next;
      this.loadTransactions();
    },
    async openDetail(id) {
      this.showDetailModal = true;
      this.showRawResponse = false;
      this.selectedTx = null;
      try {
        const res = await HTTP.get(`CardPayments/${id}`);
        this.selectedTx = res?.data?.data || null;
      } catch (e) {
        this.showDetailModal = false;
        this.$toast.error(this.$t("loadFailed") || "فشل التحميل");
      }
    },
    getOrderFromTx(tx) {
      if (!tx) return null;
      return tx.customerOrder || tx.CustomerOrder || null;
    },
    getOrderCodeFromTx(tx) {
      const order = this.getOrderFromTx(tx);
      if (order?.orderCode) return order.orderCode;
      if (order?.OrderCode) return order.OrderCode;
      if (tx?.orderCode) return tx.orderCode;
      return null;
    },
    formatOrderNumber(item) {
      if (!item) return null;
      const code = item.orderCode || null;
      if (code) return code;
      if (item.customerOrderId) return `${this.$t("order") || "طلب"} #${item.customerOrderId}`;
      return null;
    },
    formatOrderNumberFromTx(tx) {
      if (!tx) return null;
      return this.formatOrderNumber({
        orderCode: this.getOrderCodeFromTx(tx),
        customerOrderId: tx.customerOrderId ?? tx.CustomerOrderId,
      });
    },
    resolveLinkStatus(item) {
      if (item?.linkStatus) return item.linkStatus;
      if (item?.status === "Success" && item?.customerOrderId) return "Matched";
      if (item?.status === "Success") return "Unmatched";
      if (item?.status === "Failed") return "Failed";
      return "Pending";
    },
    linkStatusClass(linkStatus) {
      const key = String(linkStatus || "").toLowerCase();
      return `card-payments-link-badge--${key}`;
    },
    linkStatusLabel(linkStatus) {
      const map = {
        Matched: this.$t("linkStatusMatched") || "مطابق",
        Unmatched: this.$t("linkStatusUnmatched") || "غير مطابق",
        Failed: this.$t("linkStatusFailed") || "فاشل",
        Pending: this.$t("linkStatusPending") || "معلق",
      };
      return map[linkStatus] || linkStatus || "—";
    },
    statusClass(status) {
      const key = String(status || "").toLowerCase();
      return `card-payments-status--${key}`;
    },
    statusLabel(status) {
      const map = {
        Success: this.$t("success") || "نجاح",
        Failed: this.$t("failed") || "فشل",
        Pending: this.$t("pending") || "معلق",
      };
      return map[status] || status || "—";
    },
    formatDate(value) {
      if (!value) return "—";
      return new Date(value).toLocaleString("ar-EG");
    },
    formatAmount(value) {
      const n = Number(value);
      if (Number.isNaN(n)) return "0";
      return n.toLocaleString("en-EG");
    },
    formatRawResponse(raw) {
      if (!raw) return "—";
      try {
        return JSON.stringify(JSON.parse(raw), null, 2);
      } catch (e) {
        return String(raw);
      }
    },
  },
};
</script>

<style scoped>
.card-payments-link-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 0.75rem;
  margin-top: 1rem;
  padding: 0 1rem 1rem;
}

.card-payments-link-stat {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  padding: 0.75rem;
  border-radius: 0.65rem;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.link-stat-value {
  font-size: 1.35rem;
  font-weight: 800;
  line-height: 1.1;
}

.link-stat-label {
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.card-payments-link-stat--matched .link-stat-value { color: #15803d; }
.card-payments-link-stat--unmatched .link-stat-value { color: #b45309; }
.card-payments-link-stat--failed .link-stat-value { color: #b91c1c; }
.card-payments-link-stat--pending .link-stat-value { color: #64748b; }

.card-payments-link-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
  white-space: nowrap;
}

.card-payments-link-badge--matched {
  background: rgba(34, 197, 94, 0.15);
  color: #15803d;
}

.card-payments-link-badge--unmatched {
  background: rgba(245, 158, 11, 0.15);
  color: #b45309;
}

.card-payments-link-badge--failed {
  background: rgba(239, 68, 68, 0.12);
  color: #b91c1c;
}

.card-payments-link-badge--pending {
  background: rgba(148, 163, 184, 0.2);
  color: #64748b;
}

.card-payments-row-actions {
  display: inline-flex;
  gap: 0.35rem;
  align-items: center;
  justify-content: center;
}

.card-payments-filters {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.75rem 1rem;
  padding: 0 1rem 1rem;
  border-bottom: 1px solid var(--border-color);
  align-items: end;
}

.card-payments-filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 0;
}

.card-payments-filter-label {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-secondary);
}

.card-payments-filter-label .filter-icon {
  color: var(--primary-color);
}

.card-payments-filter-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.card-payments-filter-btn {
  min-height: 2.5rem;
  padding: 0.5rem 1rem;
  white-space: nowrap;
}

.card-payments-table-body {
  padding-top: 0;
}

.report-table-container {
  overflow-x: auto;
}

.card-payments-table ::v-deep thead th {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-secondary);
  white-space: nowrap;
  border-bottom-width: 1.5px;
}

.card-payments-table ::v-deep tbody td {
  vertical-align: middle;
  font-size: 0.85rem;
}

.card-payments-date {
  white-space: nowrap;
  font-size: 0.8rem;
  color: var(--text-secondary);
}

.card-payments-amount {
  font-weight: 700;
  color: var(--text-primary);
}

.card-payments-amount small {
  font-weight: 500;
  color: var(--text-secondary);
  margin-inline-start: 0.25rem;
}

.card-payments-mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 0.8rem;
  direction: ltr;
  display: inline-block;
}

.card-payments-order {
  font-weight: 700;
  color: var(--primary-color);
  white-space: nowrap;
}

.card-payments-unlinked {
  font-size: 0.78rem;
  color: var(--text-secondary);
  font-style: italic;
}

.card-payments-detail-item--highlight {
  grid-column: 1 / -1;
  background: rgba(99, 102, 241, 0.08);
  border-color: rgba(99, 102, 241, 0.25);
}

.detail-value--order {
  color: #4f46e5;
  font-size: 1.05rem;
}

.card-payments-status {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
  white-space: nowrap;
}

.card-payments-status--lg {
  font-size: 0.82rem;
  padding: 0.3rem 0.75rem;
}

.card-payments-status--success {
  background: rgba(34, 197, 94, 0.15);
  color: #15803d;
}

.card-payments-status--failed {
  background: rgba(239, 68, 68, 0.12);
  color: #b91c1c;
}

.card-payments-status--pending {
  background: rgba(245, 158, 11, 0.15);
  color: #b45309;
}

.card-payments-pagination {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 1rem 0 0.25rem;
  border-top: 1px solid var(--border-color);
  margin-top: 0.75rem;
}

.card-payments-pagination-info {
  font-size: 0.82rem;
  color: var(--text-secondary);
}

.card-payments-pagination-btns {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.card-payments-page-num {
  min-width: 2rem;
  text-align: center;
  font-weight: 700;
  color: var(--text-primary);
}

.payment-device-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.45rem 0.85rem;
  border-radius: 0.55rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}

.payment-device-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.payment-device-btn--outline:hover:not(:disabled) {
  border-color: rgba(99, 102, 241, 0.45);
  background: var(--bg-secondary);
}

.card-payments-detail-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin: 1rem 0;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.card-payments-detail-id {
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--text-secondary);
}

.card-payments-detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.75rem 1rem;
}

.card-payments-detail-item {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  padding: 0.65rem 0.75rem;
  background: var(--bg-secondary);
  border-radius: 0.55rem;
  border: 1px solid var(--border-color);
}

.card-payments-detail-item--full {
  grid-column: 1 / -1;
}

.detail-label {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.detail-value {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-primary);
  word-break: break-word;
}

.detail-value--amount {
  color: #4f46e5;
  font-size: 1rem;
}

.card-payments-raw-block {
  margin-top: 1rem;
}

.card-payments-raw-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.65rem;
  border: 1px dashed var(--border-color);
  border-radius: 0.5rem;
  background: transparent;
  color: var(--text-secondary);
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}

.card-payments-raw-response {
  margin-top: 0.5rem;
  max-height: 220px;
  overflow: auto;
  background: var(--bg-tertiary, #1e1e2e);
  color: #e2e8f0;
  padding: 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.72rem;
  white-space: pre-wrap;
  word-break: break-all;
  direction: ltr;
  text-align: left;
}

.loading-state,
.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.empty-icon {
  font-size: 4rem;
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.empty-state p {
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.spinning {
  animation: card-payments-spin 1s linear infinite;
}

@keyframes card-payments-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .card-payments-detail-grid {
    grid-template-columns: 1fr;
  }

  .card-payments-filter-actions {
    grid-column: 1 / -1;
  }
}
</style>
