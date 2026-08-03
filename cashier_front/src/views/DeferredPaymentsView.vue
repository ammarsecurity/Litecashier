<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content deferred-payments-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="wallet2" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("deferredPaymentsTitle") || "الدفع اللاحق" }}</h1>
                  <p class="header-subtitle">{{ $t("deferredPaymentsDescription") || "حسابات الآجل للعملاء — عرض الدين والتسديد" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="loadOverview" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="exclamation-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ formatMoney(overview.totalPendingDebt) }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("totalPendingDebt") || "إجمالي الدين المعلق" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ formatMoney(overview.totalPaidAmount) }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("totalPaidCredit") || "إجمالي المسدّد" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="people-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ overview.accountsWithPendingDebt || 0 }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("accountsWithDebt") || "حسابات عليها دين" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="receipt-cutoff"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ totalPendingInvoices }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("pendingInvoices") || "فواتير معلقة" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="wallet2"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("deferredAccountsList") || "حسابات الآجل" }}</h3>
                  <p class="app-section-subtitle">{{ $t("deferredAccountsListHint") || "عرض الديون وتسديد الفواتير الآجلة" }}</p>
                </div>
              </div>
            </div>

            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                    <p>{{ $t("deferredFiltersHint") || "بحث في حسابات الآجل بالاسم أو الهاتف" }}</p>
                  </div>
                </div>
                <div class="app-filters-panel-actions" v-if="searchQuery">
                  <button
                    type="button"
                    class="users-filter-clear-btn app-filters-clear-btn"
                    @click="searchQuery = ''"
                  >
                    <b-icon icon="x-circle" class="me-1"></b-icon>
                    {{ $t("clearFilters") || "مسح الفلاتر" }}
                  </button>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--2">
                <label class="app-filter-field app-filter-field--grow">
                  <span class="app-filter-label">{{ $t("search") || "بحث" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="search" class="search-icon"></b-icon>
                    <input
                      v-model="searchQuery"
                      type="search"
                      class="users-search-input"
                      :placeholder="$t('searchDeferredAccounts') || 'بحث بالاسم أو الهاتف...'"
                      autocomplete="off"
                    />
                  </div>
                </label>
              </div>
            </div>

            <div class="app-section-body deferred-table-body">
              <div v-if="loading && !filteredAccounts.length" class="loading-state">
                <b-spinner small></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="filteredAccounts.length > 0" class="report-table-container">
                <b-table
                  :items="filteredAccounts"
                  :fields="accountTableFields"
                  striped
                  hover
                  responsive
                  class="reports-table deferred-accounts-table"
                  :empty-text="$t('noData') || 'لا توجد بيانات'"
                >
                  <template #cell(name)="row">
                    <div class="deferred-account-cell">
                      <span class="deferred-account-avatar" :class="accountAvatarClass(row.item)">
                        <b-icon icon="person-circle"></b-icon>
                      </span>
                      <div>
                        <span class="deferred-account-name">{{ row.item.name }}</span>
                        <span v-if="row.item.pendingAmount > 0" class="deferred-account-debt-tag">
                          {{ $t("hasDebt") || "عليه دين" }}
                        </span>
                      </div>
                    </div>
                  </template>
                  <template #cell(phone)="row">
                    <span class="deferred-phone">{{ row.item.phone || "—" }}</span>
                  </template>
                  <template #cell(totalCharged)="row">
                    <span class="deferred-amount">{{ formatMoney(row.item.totalCharged) }}</span>
                  </template>
                  <template #cell(paidAmount)="row">
                    <span class="deferred-amount deferred-amount--paid">{{ formatMoney(row.item.paidAmount) }}</span>
                  </template>
                  <template #cell(pendingAmount)="row">
                    <span class="deferred-amount" :class="{ 'deferred-amount--debt': row.item.pendingAmount > 0 }">
                      {{ formatMoney(row.item.pendingAmount) }}
                    </span>
                  </template>
                  <template #cell(progress)="row">
                    <div class="deferred-progress-wrap">
                      <div class="deferred-progress-bar">
                        <div
                          class="deferred-progress-fill"
                          :style="{ width: paidPercent(row.item) + '%' }"
                        ></div>
                      </div>
                      <span class="deferred-progress-label">{{ paidPercent(row.item) }}%</span>
                    </div>
                  </template>
                  <template #cell(pendingOrderCount)="row">
                    <span class="deferred-pending-count" :class="{ 'deferred-pending-count--active': row.item.pendingOrderCount > 0 }">
                      {{ row.item.pendingOrderCount }}
                    </span>
                  </template>
                  <template #cell(actions)="row">
                    <div class="actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--view"
                        @click="openAccountDetail(row.item)"
                        :title="$t('viewDetails') || 'تفاصيل'"
                        :aria-label="$t('viewDetails') || 'تفاصيل'"
                      >
                        <b-icon icon="eye" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </template>
                </b-table>
              </div>
              <div v-else class="empty-state">
                <b-icon icon="wallet2" class="empty-icon"></b-icon>
                <p>{{ searchQuery ? ($t("noResults") || "لا توجد نتائج") : ($t("noDeferredAccounts") || "لا توجد حسابات آجلة") }}</p>
                <button v-if="searchQuery" type="button" class="empty-state-btn" @click="searchQuery = ''">
                  <b-icon icon="x-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("clearFilters") || "مسح البحث" }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Account detail modal -->
    <b-modal
      v-model="showDetailModal"
      hide-header
      hide-footer
      class="users-modal deferred-detail-modal"
      centered
      size="lg"
      @hidden="onDetailModalHidden"
    >
      <div v-if="detailLoading" class="deferred-modal-loading">
        <b-spinner></b-spinner>
        <span>{{ $t("loading") }}</span>
      </div>
      <div v-else-if="accountDetail && selectedAccount" class="deferred-modal">
        <div class="deferred-modal-hero">
          <button type="button" class="deferred-modal-close" @click="showDetailModal = false" :aria-label="$t('close')">
            <b-icon icon="x-lg"></b-icon>
          </button>
          <div class="deferred-modal-account">
            <span class="deferred-account-avatar deferred-account-avatar--lg" :class="accountAvatarClass(selectedAccount)">
              <b-icon icon="person-circle"></b-icon>
            </span>
            <div class="deferred-modal-account-text">
              <h2 class="deferred-modal-title">{{ selectedAccount.name }}</h2>
              <p class="deferred-modal-phone">
                <b-icon icon="telephone-fill"></b-icon>
                {{ selectedAccount.phone || "—" }}
              </p>
              <span
                class="deferred-modal-status-chip"
                :class="selectedAccount.pendingAmount > 0 ? 'deferred-modal-status-chip--debt' : 'deferred-modal-status-chip--clear'"
              >
                <b-icon :icon="selectedAccount.pendingAmount > 0 ? 'exclamation-circle-fill' : 'check-circle-fill'"></b-icon>
                {{ selectedAccount.pendingAmount > 0 ? ($t('hasDebt') || 'عليه دين') : ($t('fullySettled') || 'مسدّد بالكامل') }}
              </span>
            </div>
          </div>
        </div>

        <div class="deferred-detail-stats">
          <div class="deferred-detail-stat deferred-detail-stat--primary">
            <span class="deferred-detail-stat-icon"><b-icon icon="wallet2"></b-icon></span>
            <div>
              <span class="deferred-detail-stat-label">{{ $t("totalCharged") || "إجمالي الآجل" }}</span>
              <span class="deferred-detail-stat-value">{{ formatMoney(accountDetail.summary.totalCharged) }}</span>
              <span class="deferred-detail-stat-currency">{{ $t("currency") || "د.ع" }}</span>
            </div>
          </div>
          <div class="deferred-detail-stat deferred-detail-stat--success">
            <span class="deferred-detail-stat-icon"><b-icon icon="check-circle-fill"></b-icon></span>
            <div>
              <span class="deferred-detail-stat-label">{{ $t("paidAmount") || "مسدّد" }}</span>
              <span class="deferred-detail-stat-value">{{ formatMoney(accountDetail.summary.paidAmount) }}</span>
              <span class="deferred-detail-stat-currency">{{ $t("currency") || "د.ع" }}</span>
            </div>
          </div>
          <div class="deferred-detail-stat deferred-detail-stat--warning">
            <span class="deferred-detail-stat-icon"><b-icon icon="hourglass-split"></b-icon></span>
            <div>
              <span class="deferred-detail-stat-label">{{ $t("pendingAmount") || "متبقي" }}</span>
              <span class="deferred-detail-stat-value">{{ formatMoney(accountDetail.summary.pendingAmount) }}</span>
              <span class="deferred-detail-stat-currency">{{ $t("currency") || "د.ع" }}</span>
            </div>
          </div>
        </div>

        <div v-if="accountDetail.summary.totalCharged > 0" class="deferred-modal-progress">
          <div class="deferred-modal-progress-head">
            <span>{{ $t("paymentProgress") || "نسبة التسديد" }}</span>
            <strong>{{ detailPaidPercent }}%</strong>
          </div>
          <div class="deferred-progress-bar deferred-progress-bar--lg">
            <div class="deferred-progress-fill" :style="{ width: detailPaidPercent + '%' }"></div>
          </div>
        </div>

        <div class="deferred-orders-panel">
          <div class="deferred-orders-panel-head">
            <div>
              <h3 class="deferred-orders-panel-title">{{ $t("deferredOrdersHistory") || "سجل الفواتير الآجلة" }}</h3>
              <p class="deferred-orders-panel-sub">{{ accountDetail.orders.length }} {{ $t("deferredInvoiceUnit") || "فاتورة" }}</p>
            </div>
          </div>

          <div class="deferred-order-filters">
            <button
              v-for="opt in orderFilterOptions"
              :key="opt.value"
              type="button"
              class="deferred-filter-chip"
              :class="[
                opt.chipClass,
                { 'deferred-filter-chip--active': orderStatusFilter === opt.value },
              ]"
              @click="setOrderFilter(opt.value)"
            >
              <b-icon :icon="opt.icon"></b-icon>
              <span>{{ opt.label }}</span>
              <span v-if="opt.count != null" class="deferred-filter-count">{{ opt.count }}</span>
            </button>
          </div>

          <div v-if="accountDetail.orders.length" class="deferred-orders-list">
            <div
              v-for="order in accountDetail.orders"
              :key="order.orderId"
              class="deferred-order-card"
              :class="isPendingStatus(order.paymentStatus) ? 'deferred-order-card--pending' : 'deferred-order-card--paid'"
            >
              <div class="deferred-order-card-main">
                <div class="deferred-order-card-top">
                  <span class="deferred-order-code">{{ order.orderCode }}</span>
                  <span class="deferred-status-badge" :class="paymentStatusClass(order.paymentStatus)">
                    {{ paymentStatusLabel(order.paymentStatus) }}
                  </span>
                </div>
                <div class="deferred-order-card-meta">
                  <span class="deferred-order-meta-item">
                    <b-icon icon="calendar3"></b-icon>
                    {{ formatDate(order.insertDate) }}
                  </span>
                  <span v-if="order.settledAt" class="deferred-order-meta-item">
                    <b-icon icon="check2-circle"></b-icon>
                    {{ $t("settledAt") || "تاريخ التسديد" }}: {{ formatDate(order.settledAt) }}
                  </span>
                  <span v-if="order.settlementPaymentMethod && !isPendingStatus(order.paymentStatus)" class="deferred-order-meta-item">
                    <b-icon :icon="methodIcon(order.settlementPaymentMethod)"></b-icon>
                    {{ settlementMethodLabel(order.settlementPaymentMethod) }}
                  </span>
                </div>
              </div>
              <div class="deferred-order-card-side">
                <span class="deferred-order-amount">{{ formatMoney(order.amount) }}</span>
                <span class="deferred-order-currency">{{ $t("currency") || "د.ع" }}</span>
                <button
                  v-if="isPendingStatus(order.paymentStatus)"
                  type="button"
                  class="deferred-settle-btn"
                  :disabled="settlingOrderId === order.orderId"
                  @click="openSettleModal(order)"
                >
                  <b-spinner small v-if="settlingOrderId === order.orderId"></b-spinner>
                  <template v-else>
                    <b-icon icon="cash-coin"></b-icon>
                    {{ $t("settleOrder") || "تسديد" }}
                  </template>
                </button>
              </div>
            </div>
          </div>
          <div v-else class="deferred-orders-empty">
            <b-icon icon="inbox"></b-icon>
            <span>{{ $t("noOrdersInFilter") || "لا توجد فواتير في هذا التصفية" }}</span>
          </div>
        </div>

        <div class="deferred-modal-footer">
          <button type="button" class="deferred-modal-close-btn" @click="showDetailModal = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </div>
    </b-modal>

    <!-- Settle modal -->
    <b-modal
      v-model="showSettleModal"
      hide-header
      hide-footer
      class="users-modal deferred-settle-modal"
      centered
      @hidden="orderToSettle = null"
    >
      <div v-if="orderToSettle" class="deferred-settle-modal-inner">
        <div class="deferred-settle-modal-head">
          <span class="deferred-settle-modal-icon"><b-icon icon="cash-coin"></b-icon></span>
          <h2 class="deferred-settle-modal-title">{{ $t("settleInvoiceTitle") || "تسديد الفاتورة" }}</h2>
          <p class="deferred-settle-modal-sub">{{ $t("settlementPaymentMethod") || "اختر طريقة التسديد" }}</p>
        </div>

        <div class="deferred-settle-summary">
          <div class="deferred-settle-row">
            <span class="deferred-settle-label">{{ $t("orderCode") || "رمز الفاتورة" }}</span>
            <span class="deferred-settle-value deferred-order-code">{{ orderToSettle.orderCode }}</span>
          </div>
          <div class="deferred-settle-row">
            <span class="deferred-settle-label">{{ $t("amount") || "المبلغ" }}</span>
            <span class="deferred-settle-value deferred-settle-amount">
              {{ formatMoney(orderToSettle.amount) }}
              <small>{{ $t("currency") || "د.ع" }}</small>
            </span>
          </div>
        </div>

        <div class="deferred-method-grid">
          <button
            v-for="opt in settlementMethodOptions"
            :key="opt.value"
            type="button"
            class="deferred-method-btn"
            :class="{ 'deferred-method-btn--active': settlementMethod === opt.value }"
            @click="settlementMethod = opt.value"
          >
            <b-icon :icon="methodIcon(opt.value)"></b-icon>
            <span>{{ opt.text }}</span>
          </button>
        </div>

        <div class="deferred-settle-actions">
          <button type="button" class="deferred-modal-close-btn" @click="showSettleModal = false">
            {{ $t("cancel") || "إلغاء" }}
          </button>
          <button
            type="button"
            class="deferred-settle-confirm-btn"
            :disabled="settlingOrderId != null"
            @click="confirmSettle()"
          >
            <b-spinner small v-if="settlingOrderId != null" class="me-1"></b-spinner>
            <b-icon v-else icon="check-lg"></b-icon>
            {{ $t("confirmSettle") || "تأكيد التسديد" }}
          </button>
        </div>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";

export default {
  name: "DeferredPaymentsView",
  components: { AppHeader },
  data() {
    return {
      loading: false,
      detailLoading: false,
      searchQuery: "",
      overview: {
        totalPendingDebt: 0,
        totalPaidAmount: 0,
        accountsWithPendingDebt: 0,
        customers: [],
      },
      showDetailModal: false,
      showSettleModal: false,
      selectedAccount: null,
      accountDetail: null,
      orderStatusFilter: "all",
      orderToSettle: null,
      settlementMethod: "Cash",
      settlingOrderId: null,
    };
  },
  computed: {
    settlementMethodOptions() {
      return [
        { value: "Cash", text: this.$t("cash") || "نقد" },
        { value: "Card", text: this.$t("card") || "بطاقة" },
        { value: "BankTransfer", text: this.$t("bankTransfer") || "تحويل" },
      ];
    },
    currentAccounts() {
      return this.overview.customers || [];
    },
    filteredAccounts() {
      const q = (this.searchQuery || "").trim().toLowerCase();
      if (!q) return this.currentAccounts;
      return this.currentAccounts.filter((a) => {
        const name = (a.name || "").toLowerCase();
        const phone = (a.phone || "").toLowerCase();
        return name.includes(q) || phone.includes(q);
      });
    },
    customersWithDebtCount() {
      return (this.overview.customers || []).filter((c) => c.pendingAmount > 0).length;
    },
    totalPendingInvoices() {
      return (this.overview.customers || []).reduce((sum, a) => sum + (a.pendingOrderCount || 0), 0);
    },
    accountTableFields() {
      return [
        { key: "name", label: this.$t("name") || "الاسم", sortable: true },
        { key: "phone", label: this.$t("phone") || "الهاتف" },
        { key: "totalCharged", label: this.$t("totalCharged") || "إجمالي الآجل", sortable: true },
        { key: "paidAmount", label: this.$t("paidAmount") || "مسدّد", sortable: true },
        { key: "pendingAmount", label: this.$t("pendingAmount") || "متبقي", sortable: true },
        { key: "progress", label: this.$t("paymentProgress") || "نسبة التسديد" },
        { key: "pendingOrderCount", label: this.$t("pendingInvoices") || "فواتير معلقة", sortable: true },
        { key: "actions", label: "", class: "deferred-actions-col" },
      ];
    },
    detailPaidPercent() {
      if (!this.accountDetail?.summary) return 0;
      const total = Number(this.accountDetail.summary.totalCharged || 0);
      if (total <= 0) return 100;
      return Math.min(
        100,
        Math.round((Number(this.accountDetail.summary.paidAmount || 0) / total) * 100)
      );
    },
    orderFilterOptions() {
      const total = this.selectedAccount?.totalOrderCount || 0;
      const pending = this.selectedAccount?.pendingOrderCount || 0;
      const paid = Math.max(0, total - pending);
      const t = (key, fallback) => this.$t(key) || fallback;
      return [
        { value: "all", label: t("all", "الكل"), icon: "list-ul", chipClass: "", count: total },
        {
          value: "pending",
          label: t("pending", "معلق"),
          icon: "hourglass-split",
          chipClass: "deferred-filter-chip--pending",
          count: pending,
        },
        {
          value: "paid",
          label: t("paid", "مسدّد"),
          icon: "check-circle-fill",
          chipClass: "deferred-filter-chip--paid",
          count: paid,
        },
      ];
    },
  },
  mounted() {
    this.loadOverview().then(() => this.tryOpenFromQuery());
  },
  methods: {
    formatMoney(value) {
      const n = Number(value || 0);
      return n.toLocaleString(this.$i18n?.locale === "en" ? "en" : "ar-IQ");
    },
    formatDate(value) {
      if (!value) return "—";
      try {
        return new Date(value).toLocaleString(this.$i18n?.locale === "en" ? "en" : "ar-IQ", {
          year: "numeric",
          month: "short",
          day: "numeric",
          hour: "2-digit",
          minute: "2-digit",
        });
      } catch (e) {
        return String(value);
      }
    },
    paidPercent(account) {
      const total = Number(account.totalCharged || 0);
      if (total <= 0) return 100;
      return Math.min(100, Math.round((Number(account.paidAmount || 0) / total) * 100));
    },
    accountAvatarClass(account) {
      if (account.pendingAmount > 0) return "deferred-account-avatar--debt";
      if (account.totalCharged > 0) return "deferred-account-avatar--clear";
      return "";
    },
    methodIcon(method) {
      const icons = { Cash: "cash-stack", Card: "credit-card-fill", BankTransfer: "bank" };
      return icons[method] || "wallet2";
    },
    isPendingStatus(status) {
      return String(status || "").toLowerCase() !== "paid" && String(status || "").toLowerCase() !== "refunded";
    },
    paymentStatusClass(status) {
      return this.isPendingStatus(status) ? "deferred-status-badge--pending" : "deferred-status-badge--paid";
    },
    paymentStatusLabel(status) {
      if (this.isPendingStatus(status)) return this.$t("pending") || "معلق";
      return this.$t("paid") || "مسدّد";
    },
    settlementMethodLabel(method) {
      if (!method) return "—";
      const m = {
        Cash: this.$t("cash") || "نقد",
        Card: this.$t("card") || "بطاقة",
        BankTransfer: this.$t("bankTransfer") || "تحويل",
      };
      return m[method] || method;
    },
    async loadOverview() {
      this.loading = true;
      try {
        const res = await HTTP.get("CreditAccounts/summary");
        const data = res?.data?.data || {};
        this.overview = {
          totalPendingDebt: data.totalPendingDebt || 0,
          totalPaidAmount: data.totalPaidAmount || 0,
          accountsWithPendingDebt: data.accountsWithPendingDebt || 0,
          customers: data.customers || [],
        };
      } catch (e) {
        console.error(e);
        this.$toast.error(this.$t("deferredLoadFailed") || "تعذر تحميل حسابات الدفع اللاحق", {
          position: "top-right",
          timeout: 4000,
        });
      } finally {
        this.loading = false;
      }
    },
    tryOpenFromQuery() {
      const customerId = this.$route?.query?.customerId;
      if (!customerId) return;
      const id = Number(customerId);
      const row = (this.overview.customers || []).find((c) => c.accountId === id);
      if (row) {
        this.openAccountDetail(row);
      }
    },
    async openAccountDetail(row) {
      this.selectedAccount = row;
      this.orderStatusFilter = "all";
      this.showDetailModal = true;
      await this.reloadAccountDetail();
    },
    setOrderFilter(status) {
      this.orderStatusFilter = status;
      this.reloadAccountDetail();
    },
    async reloadAccountDetail() {
      if (!this.selectedAccount) return;
      this.detailLoading = true;
      try {
        const type = this.selectedAccount.accountType.toLowerCase();
        const res = await HTTP.get(
          `CreditAccounts/${type}/${this.selectedAccount.accountId}/orders`,
          { params: { status: this.orderStatusFilter } }
        );
        this.accountDetail = res?.data?.data || null;
      } catch (e) {
        console.error(e);
        this.$toast.error(this.$t("deferredLoadFailed") || "تعذر تحميل التفاصيل", {
          position: "top-right",
          timeout: 4000,
        });
      } finally {
        this.detailLoading = false;
      }
    },
    onDetailModalHidden() {
      this.selectedAccount = null;
      this.accountDetail = null;
      this.orderStatusFilter = "all";
    },
    openSettleModal(order) {
      this.orderToSettle = order;
      this.settlementMethod = "Cash";
      this.showSettleModal = true;
    },
    async confirmSettle() {
      if (!this.orderToSettle) return;
      this.settlingOrderId = this.orderToSettle.orderId;
      try {
        const res = await HTTP.post("CreditAccounts/settle", {
          orderId: this.orderToSettle.orderId,
          settlementPaymentMethod: this.settlementMethod,
        });
        if (res?.data?.errorStatus) {
          this.$toast.error(this.mapSettleError(res?.data?.message), { position: "top-right", timeout: 4000 });
          return;
        }
        this.$toast.success(this.$t("settleSuccess") || "تم التسديد بنجاح", { position: "top-right", timeout: 3000 });
        this.showSettleModal = false;
        this.orderToSettle = null;
        await this.reloadAccountDetail();
        await this.loadOverview();
        if (this.selectedAccount) {
          const updated = (this.overview.customers || []).find(
            (a) => a.accountId === this.selectedAccount.accountId
          );
          if (updated) this.selectedAccount = updated;
        }
      } catch (e) {
        console.error(e);
        this.$toast.error(this.mapSettleError(e?.response?.data?.message), { position: "top-right", timeout: 4000 });
      } finally {
        this.settlingOrderId = null;
      }
    },
    mapSettleError(code) {
      const key = code || "error";
      if (this.$te(key)) return this.$t(key);
      return this.$t("settleFailed") || "فشل التسديد";
    },
  },
};
</script>

<style scoped>
.deferred-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
}

.deferred-tabs {
  display: inline-flex;
  padding: 0.25rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
  gap: 0.2rem;
}

.deferred-tab {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.45rem 0.85rem;
  border: none;
  border-radius: 0.5rem;
  background: transparent;
  color: var(--text-secondary);
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}

.deferred-tab--active {
  background: var(--primary-color);
  color: #fff;
  box-shadow: 0 2px 8px color-mix(in srgb, var(--primary-color) 35%, transparent);
}

.deferred-tab-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.25rem;
  height: 1.25rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  background: rgba(239, 68, 68, 0.15);
  color: #dc2626;
  font-size: 0.68rem;
  font-weight: 800;
}

.deferred-tab--active .deferred-tab-badge {
  background: rgba(255, 255, 255, 0.25);
  color: #fff;
}

.deferred-search {
  min-width: 220px;
  flex: 1;
  max-width: 320px;
}

.deferred-table-body {
  padding-top: 0.25rem;
}

.deferred-account-cell {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}

.deferred-account-avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2.1rem;
  height: 2.1rem;
  border-radius: 50%;
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--primary-color);
  font-size: 1.1rem;
  flex-shrink: 0;
}

.deferred-account-avatar--lg {
  width: 3rem;
  height: 3rem;
  font-size: 1.5rem;
}

.deferred-account-avatar--debt {
  background: rgba(245, 158, 11, 0.15);
  color: #d97706;
}

.deferred-account-avatar--clear {
  background: rgba(34, 197, 94, 0.12);
  color: #16a34a;
}

.deferred-account-name {
  display: block;
  font-weight: 700;
  color: var(--text-primary);
}

.deferred-account-debt-tag {
  display: inline-block;
  margin-top: 0.15rem;
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
  background: rgba(245, 158, 11, 0.12);
  color: #b45309;
  font-size: 0.68rem;
  font-weight: 700;
}

.deferred-phone {
  font-family: ui-monospace, monospace;
  font-size: 0.85rem;
  color: var(--text-secondary);
}

.deferred-amount {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--text-primary);
}

.deferred-amount--paid {
  color: #16a34a;
}

.deferred-amount--debt {
  color: #dc2626;
  font-weight: 700;
}

.deferred-amount--bold {
  font-weight: 700;
  color: var(--primary-color);
}

.deferred-progress-wrap {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 100px;
}

.deferred-progress-bar {
  flex: 1;
  height: 6px;
  background: var(--bg-tertiary);
  border-radius: 999px;
  overflow: hidden;
}

.deferred-progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #22c55e, #16a34a);
  border-radius: 999px;
  transition: width 0.3s ease;
}

.deferred-progress-label {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--text-secondary);
  min-width: 2rem;
  text-align: left;
}

.deferred-pending-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.75rem;
  height: 1.75rem;
  border-radius: 0.45rem;
  background: var(--bg-secondary);
  font-weight: 700;
  font-size: 0.85rem;
  color: var(--text-secondary);
}

.deferred-pending-count--active {
  background: rgba(239, 68, 68, 0.1);
  color: #dc2626;
}

/* Detail modal */
.deferred-modal {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.deferred-modal-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 3rem 1rem;
  color: var(--text-secondary);
}

.deferred-modal-hero {
  position: relative;
  margin: -1.25rem -1.25rem 0;
  padding: 1.25rem 1.25rem 1rem;
  background: linear-gradient(135deg, color-mix(in srgb, var(--primary-color) 10%, transparent), color-mix(in srgb, var(--primary-color) 4%, transparent));
  border-bottom: 1px solid var(--border-color);
  border-radius: 0.75rem 0.75rem 0 0;
}

.deferred-modal-account {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding-inline-end: 2.5rem;
}

.deferred-modal-account-text {
  min-width: 0;
}

.deferred-modal-title {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--text-primary);
  line-height: 1.3;
}

.deferred-modal-phone {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  margin: 0.3rem 0 0.55rem;
  font-size: 0.85rem;
  color: var(--text-secondary);
  direction: ltr;
  justify-content: flex-end;
}

.deferred-modal-status-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
}

.deferred-modal-status-chip--debt {
  background: rgba(245, 158, 11, 0.14);
  color: #b45309;
}

.deferred-modal-status-chip--clear {
  background: rgba(34, 197, 94, 0.14);
  color: #15803d;
}

.deferred-modal-close {
  position: absolute;
  top: 1rem;
  inset-inline-end: 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  background: var(--bg-primary);
  color: var(--text-secondary);
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}

.deferred-modal-close:hover {
  background: var(--bg-secondary);
  color: var(--text-primary);
}

.deferred-modal-progress {
  padding: 0.85rem 1rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
}

.deferred-modal-progress-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
  font-size: 0.82rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.deferred-modal-progress-head strong {
  color: #16a34a;
  font-size: 0.95rem;
}

.deferred-progress-bar--lg {
  height: 8px;
}

.deferred-orders-panel {
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
  overflow: hidden;
  background: var(--bg-primary);
}

.deferred-orders-panel-head {
  padding: 0.85rem 1rem;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.deferred-orders-panel-title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 800;
  color: var(--text-primary);
}

.deferred-orders-panel-sub {
  margin: 0.2rem 0 0;
  font-size: 0.78rem;
  color: var(--text-secondary);
}

.deferred-orders-list {
  max-height: 320px;
  overflow-y: auto;
  padding: 0.65rem;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}

.deferred-order-card {
  display: flex;
  align-items: stretch;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.75rem 0.85rem;
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
  background: var(--bg-primary);
  transition: border-color 0.15s, box-shadow 0.15s;
}

.deferred-order-card--pending {
  border-inline-start: 3px solid #d97706;
}

.deferred-order-card--paid {
  border-inline-start: 3px solid #16a34a;
}

.deferred-order-card-main {
  flex: 1;
  min-width: 0;
}

.deferred-order-card-top {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 0.45rem;
}

.deferred-order-card-meta {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.deferred-order-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.76rem;
  color: var(--text-secondary);
}

.deferred-order-card-side {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  justify-content: center;
  gap: 0.35rem;
  flex-shrink: 0;
}

.deferred-order-amount {
  font-size: 1rem;
  font-weight: 800;
  color: var(--primary-color);
  font-variant-numeric: tabular-nums;
}

.deferred-order-currency {
  font-size: 0.68rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin-top: -0.2rem;
}

.deferred-modal-footer {
  display: flex;
  justify-content: flex-end;
  padding-top: 0.25rem;
}

.deferred-modal-close-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 6rem;
  padding: 0.55rem 1.1rem;
  border: 1px solid var(--border-color);
  border-radius: 0.55rem;
  background: var(--bg-secondary);
  color: var(--text-primary);
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s;
}

.deferred-modal-close-btn:hover {
  background: var(--bg-tertiary);
}

.deferred-settle-modal-inner {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.deferred-settle-modal-head {
  text-align: center;
  padding-bottom: 0.25rem;
}

.deferred-settle-modal-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 3rem;
  height: 3rem;
  margin-bottom: 0.65rem;
  border-radius: 50%;
  background: rgba(34, 197, 94, 0.12);
  color: #16a34a;
  font-size: 1.35rem;
}

.deferred-settle-modal-title {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--text-primary);
}

.deferred-settle-modal-sub {
  margin: 0.35rem 0 0;
  font-size: 0.82rem;
  color: var(--text-secondary);
}

.deferred-settle-actions {
  display: flex;
  gap: 0.65rem;
  justify-content: flex-end;
}

.deferred-settle-confirm-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.55rem 1.1rem;
  border: none;
  border-radius: 0.55rem;
  background: linear-gradient(135deg, #22c55e, #16a34a);
  color: #fff;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  transition: opacity 0.15s, transform 0.1s;
}

.deferred-settle-confirm-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(34, 197, 94, 0.35);
}

.deferred-settle-confirm-btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.deferred-settle-amount small {
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin-inline-start: 0.25rem;
}

.deferred-detail-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.65rem;
}

.deferred-detail-stat {
  display: flex;
  align-items: flex-start;
  gap: 0.65rem;
  padding: 0.85rem 0.75rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
}

.deferred-detail-stat-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border-radius: 0.5rem;
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--primary-color);
  font-size: 0.95rem;
  flex-shrink: 0;
}

.deferred-detail-stat--success .deferred-detail-stat-icon {
  background: rgba(34, 197, 94, 0.12);
  color: #16a34a;
}

.deferred-detail-stat--warning .deferred-detail-stat-icon {
  background: rgba(245, 158, 11, 0.12);
  color: #d97706;
}

.deferred-detail-stat--success {
  border-color: rgba(34, 197, 94, 0.3);
}

.deferred-detail-stat--warning {
  border-color: rgba(245, 158, 11, 0.3);
}

.deferred-detail-stat-label {
  display: block;
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin-bottom: 0.15rem;
}

.deferred-detail-stat-value {
  display: block;
  font-size: 1rem;
  font-weight: 800;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
  line-height: 1.2;
}

.deferred-detail-stat-currency {
  display: block;
  font-size: 0.68rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin-top: 0.1rem;
}

.deferred-order-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
  padding: 0.65rem 0.75rem;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-primary);
}

.deferred-order-filters-label {
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--text-secondary);
}

.deferred-filter-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.75rem;
  border-radius: 999px;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
  color: var(--text-secondary);
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
}

.deferred-filter-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.2rem;
  height: 1.2rem;
  padding: 0 0.3rem;
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.06);
  font-size: 0.68rem;
  font-weight: 800;
}

.deferred-filter-chip--active .deferred-filter-count {
  background: rgba(255, 255, 255, 0.25);
  color: inherit;
}

.deferred-filter-chip--active {
  background: var(--primary-color);
  border-color: var(--primary-color);
  color: #fff;
}

.deferred-filter-chip--pending.deferred-filter-chip--active {
  background: #d97706;
  border-color: #d97706;
}

.deferred-filter-chip--paid.deferred-filter-chip--active {
  background: #16a34a;
  border-color: #16a34a;
}

.deferred-orders-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem 1rem;
  color: var(--text-secondary);
  font-size: 0.88rem;
}

.deferred-orders-empty .b-icon {
  font-size: 2rem;
  opacity: 0.5;
}

.deferred-date {
  font-size: 0.82rem;
  color: var(--text-secondary);
  white-space: nowrap;
}

.deferred-order-code {
  font-family: ui-monospace, monospace;
  font-weight: 700;
  font-size: 0.85rem;
  color: var(--text-primary);
}

.deferred-status-badge {
  display: inline-block;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
  white-space: nowrap;
}

.deferred-status-badge--pending {
  background: rgba(245, 158, 11, 0.15);
  color: #b45309;
}

.deferred-status-badge--paid {
  background: rgba(34, 197, 94, 0.15);
  color: #15803d;
}

.deferred-settlement-method {
  font-size: 0.82rem;
  color: var(--text-secondary);
}

.deferred-settle-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.35rem 0.7rem;
  border-radius: 0.5rem;
  border: none;
  background: linear-gradient(135deg, #22c55e, #16a34a);
  color: #fff;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
  transition: opacity 0.15s, transform 0.1s;
}

.deferred-settle-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(34, 197, 94, 0.35);
}

.deferred-settle-btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.deferred-settled-mark {
  color: #16a34a;
  font-size: 1.1rem;
}

.deferred-settle-summary {
  margin: 0;
  padding: 1rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
}

.deferred-settle-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.35rem 0;
}

.deferred-settle-row + .deferred-settle-row {
  margin-top: 0.5rem;
  padding-top: 0.65rem;
  border-top: 1px dashed var(--border-color);
}

.deferred-settle-label {
  font-size: 0.82rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.deferred-settle-value {
  font-weight: 700;
  color: var(--text-primary);
}

.deferred-settle-amount {
  font-size: 1.15rem;
  color: var(--primary-color);
}

.deferred-settle-hint {
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin-bottom: 0.75rem;
}

.deferred-method-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.65rem;
}

.deferred-method-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  padding: 0.85rem 0.5rem;
  border: 2px solid var(--border-color);
  border-radius: 0.65rem;
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s;
}

.deferred-method-btn .b-icon {
  font-size: 1.35rem;
}

.deferred-method-btn--active {
  border-color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 8%, transparent);
  color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 15%, transparent);
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
  opacity: 0.45;
}

.empty-state p {
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.spinning {
  animation: deferred-spin 1s linear infinite;
}

@keyframes deferred-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 768px) {
  .deferred-toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .deferred-search {
    max-width: none;
  }

  .deferred-detail-stats {
    grid-template-columns: 1fr;
  }

  .deferred-order-card {
    flex-direction: column;
  }

  .deferred-order-card-side {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding-top: 0.5rem;
    border-top: 1px dashed var(--border-color);
  }

  .deferred-method-grid {
    grid-template-columns: 1fr;
  }

  .deferred-settle-actions {
    flex-direction: column-reverse;
  }

  .deferred-settle-actions .deferred-modal-close-btn,
  .deferred-settle-actions .deferred-settle-confirm-btn {
    width: 100%;
  }

  .app-section-header--toolbar {
    flex-direction: column;
    align-items: stretch !important;
  }
}
</style>
