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
                  <p class="header-subtitle">{{ $t("deferredPaymentsDescription") || "حسابات الآجل للعملاء والموظفين" }}</p>
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
              <div class="deferred-toolbar">
                <div class="deferred-tabs" role="tablist">
                  <button
                    type="button"
                    role="tab"
                    class="deferred-tab"
                    :class="{ 'deferred-tab--active': activeTab === 'customers' }"
                    @click="activeTab = 'customers'"
                  >
                    <b-icon icon="person-lines-fill"></b-icon>
                    {{ $t("customers") || "العملاء" }}
                    <span v-if="customersWithDebtCount" class="deferred-tab-badge">{{ customersWithDebtCount }}</span>
                  </button>
                  <button
                    type="button"
                    role="tab"
                    class="deferred-tab"
                    :class="{ 'deferred-tab--active': activeTab === 'employees' }"
                    @click="activeTab = 'employees'"
                  >
                    <b-icon icon="person-badge-fill"></b-icon>
                    {{ $t("employees") || "الموظفين" }}
                    <span v-if="employeesWithDebtCount" class="deferred-tab-badge">{{ employeesWithDebtCount }}</span>
                  </button>
                </div>
                <div class="app-search-wrap deferred-search">
                  <b-icon icon="search" class="app-search-icon"></b-icon>
                  <input
                    v-model="searchQuery"
                    type="search"
                    class="app-search-input"
                    :placeholder="$t('searchDeferredAccounts') || 'بحث بالاسم أو الهاتف...'"
                    autocomplete="off"
                  />
                </div>
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
                        <b-icon :icon="row.item.accountType === 'Employee' ? 'person-badge-fill' : 'person-circle'"></b-icon>
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
                    <button
                      type="button"
                      class="action-btn action-btn--icon action-btn--view"
                      @click="openAccountDetail(row.item)"
                      :title="$t('viewDetails') || 'تفاصيل'"
                    >
                      <b-icon icon="eye-fill" class="action-icon"></b-icon>
                    </button>
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
      <div v-if="detailLoading" class="modal-content-wrapper loading-state">
        <b-spinner></b-spinner>
        <span>{{ $t("loading") }}</span>
      </div>
      <div v-else-if="accountDetail && selectedAccount" class="modal-content-wrapper">
        <div class="deferred-modal-header">
          <div class="deferred-modal-account">
            <span class="deferred-account-avatar deferred-account-avatar--lg" :class="accountAvatarClass(selectedAccount)">
              <b-icon :icon="selectedAccount.accountType === 'Employee' ? 'person-badge-fill' : 'person-circle'"></b-icon>
            </span>
            <div>
              <h2 class="modal-title">{{ selectedAccount.name }}</h2>
              <p class="deferred-modal-subtitle">
                <b-icon icon="telephone-fill"></b-icon>
                {{ selectedAccount.phone || "—" }}
              </p>
            </div>
          </div>
          <button type="button" class="deferred-modal-close" @click="showDetailModal = false">
            <b-icon icon="x-lg"></b-icon>
          </button>
        </div>

        <div class="deferred-detail-stats">
          <div class="deferred-detail-stat">
            <span class="deferred-detail-stat-label">{{ $t("totalCharged") || "إجمالي الآجل" }}</span>
            <span class="deferred-detail-stat-value">{{ formatMoney(accountDetail.summary.totalCharged) }}</span>
          </div>
          <div class="deferred-detail-stat deferred-detail-stat--success">
            <span class="deferred-detail-stat-label">{{ $t("paidAmount") || "مسدّد" }}</span>
            <span class="deferred-detail-stat-value">{{ formatMoney(accountDetail.summary.paidAmount) }}</span>
          </div>
          <div class="deferred-detail-stat deferred-detail-stat--warning">
            <span class="deferred-detail-stat-label">{{ $t("pendingAmount") || "متبقي" }}</span>
            <span class="deferred-detail-stat-value">{{ formatMoney(accountDetail.summary.pendingAmount) }}</span>
          </div>
        </div>

        <div class="deferred-order-filters">
          <span class="deferred-order-filters-label">{{ $t("filter") || "فلتر" }}:</span>
          <button
            type="button"
            class="deferred-filter-chip"
            :class="{ 'deferred-filter-chip--active': orderStatusFilter === 'all' }"
            @click="setOrderFilter('all')"
          >
            {{ $t("all") || "الكل" }}
          </button>
          <button
            type="button"
            class="deferred-filter-chip deferred-filter-chip--pending"
            :class="{ 'deferred-filter-chip--active': orderStatusFilter === 'pending' }"
            @click="setOrderFilter('pending')"
          >
            {{ $t("pending") || "معلق" }}
          </button>
          <button
            type="button"
            class="deferred-filter-chip deferred-filter-chip--paid"
            :class="{ 'deferred-filter-chip--active': orderStatusFilter === 'paid' }"
            @click="setOrderFilter('paid')"
          >
            {{ $t("paid") || "مسدّد" }}
          </button>
        </div>

        <div class="deferred-orders-table-wrap">
          <b-table
            v-if="accountDetail.orders.length"
            :items="accountDetail.orders"
            :fields="orderTableFields"
            small
            striped
            hover
            responsive
            class="reports-table deferred-orders-table"
          >
            <template #cell(insertDate)="row">
              <span class="deferred-date">{{ formatDate(row.item.insertDate) }}</span>
            </template>
            <template #cell(orderCode)="row">
              <span class="deferred-order-code">{{ row.item.orderCode }}</span>
            </template>
            <template #cell(amount)="row">
              <span class="deferred-amount deferred-amount--bold">{{ formatMoney(row.item.amount) }}</span>
            </template>
            <template #cell(paymentStatus)="row">
              <span class="deferred-status-badge" :class="paymentStatusClass(row.item.paymentStatus)">
                {{ paymentStatusLabel(row.item.paymentStatus) }}
              </span>
            </template>
            <template #cell(settlementPaymentMethod)="row">
              <span class="deferred-settlement-method">{{ settlementMethodLabel(row.item.settlementPaymentMethod) }}</span>
            </template>
            <template #cell(actions)="row">
              <button
                v-if="isPendingStatus(row.item.paymentStatus)"
                type="button"
                class="deferred-settle-btn"
                :disabled="settlingOrderId === row.item.orderId"
                @click="openSettleModal(row.item)"
              >
                <b-spinner small v-if="settlingOrderId === row.item.orderId"></b-spinner>
                <template v-else>
                  <b-icon icon="cash-coin"></b-icon>
                  {{ $t("settleOrder") || "تسديد" }}
                </template>
              </button>
              <span v-else class="deferred-settled-mark">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
            </template>
          </b-table>
          <div v-else class="deferred-orders-empty">
            <b-icon icon="inbox"></b-icon>
            <span>{{ $t("noData") || "لا توجد بيانات" }}</span>
          </div>
        </div>

        <div class="users-form-actions">
          <button type="button" class="users-form-cancel-button" @click="showDetailModal = false">
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
      class="users-modal"
      centered
      @hidden="orderToSettle = null"
    >
      <div v-if="orderToSettle" class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("settleOrder") || "تسديد الفاتورة" }}</h2>
        <div class="deferred-settle-summary">
          <div class="deferred-settle-row">
            <span class="deferred-settle-label">{{ $t("orderCode") || "رمز الفاتورة" }}</span>
            <span class="deferred-settle-value deferred-order-code">{{ orderToSettle.orderCode }}</span>
          </div>
          <div class="deferred-settle-row">
            <span class="deferred-settle-label">{{ $t("amount") || "المبلغ" }}</span>
            <span class="deferred-settle-value deferred-settle-amount">{{ formatMoney(orderToSettle.amount) }}</span>
          </div>
        </div>

        <p class="deferred-settle-hint">{{ $t("settlementPaymentMethod") || "طريقة التسديد" }}</p>
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

        <div class="users-form-actions">
          <button type="button" class="users-form-cancel-button" @click="showSettleModal = false">
            {{ $t("cancel") || "إلغاء" }}
          </button>
          <button
            type="button"
            class="users-form-submit-button"
            :disabled="settlingOrderId != null"
            @click="confirmSettle()"
          >
            <b-spinner small v-if="settlingOrderId != null" class="me-1"></b-spinner>
            {{ $t("confirm") || "تأكيد" }}
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
      activeTab: "customers",
      searchQuery: "",
      overview: {
        totalPendingDebt: 0,
        totalPaidAmount: 0,
        accountsWithPendingDebt: 0,
        customers: [],
        employees: [],
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
      return this.activeTab === "customers"
        ? (this.overview.customers || [])
        : (this.overview.employees || []);
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
    employeesWithDebtCount() {
      return (this.overview.employees || []).filter((e) => e.pendingAmount > 0).length;
    },
    totalPendingInvoices() {
      const all = [...(this.overview.customers || []), ...(this.overview.employees || [])];
      return all.reduce((sum, a) => sum + (a.pendingOrderCount || 0), 0);
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
    orderTableFields() {
      return [
        { key: "insertDate", label: this.$t("date") || "التاريخ" },
        { key: "orderCode", label: this.$t("orderCode") || "رمز الفاتورة" },
        { key: "amount", label: this.$t("amount") || "المبلغ" },
        { key: "paymentStatus", label: this.$t("status") || "الحالة" },
        { key: "settlementPaymentMethod", label: this.$t("settlementPaymentMethod") || "طريقة التسديد" },
        { key: "actions", label: "", class: "deferred-actions-col" },
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
          employees: data.employees || [],
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
        this.activeTab = "customers";
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
          const list = this.selectedAccount.accountType === "Customer"
            ? this.overview.customers
            : this.overview.employees;
          const updated = list.find((a) => a.accountId === this.selectedAccount.accountId);
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
  background: var(--primary-color, #6366f1);
  color: #fff;
  box-shadow: 0 2px 8px rgba(99, 102, 241, 0.35);
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
  background: rgba(99, 102, 241, 0.12);
  color: #6366f1;
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
  color: #4f46e5;
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

.deferred-modal-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--border-color);
}

.deferred-modal-account {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.deferred-modal-subtitle {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  margin: 0.25rem 0 0;
  font-size: 0.85rem;
  color: var(--text-secondary);
}

.deferred-modal-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  background: var(--bg-secondary);
  color: var(--text-secondary);
  cursor: pointer;
}

.deferred-detail-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.75rem;
  margin-bottom: 1.25rem;
}

.deferred-detail-stat {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 0.85rem 1rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
}

.deferred-detail-stat--success {
  border-color: rgba(34, 197, 94, 0.35);
  background: rgba(34, 197, 94, 0.06);
}

.deferred-detail-stat--warning {
  border-color: rgba(245, 158, 11, 0.35);
  background: rgba(245, 158, 11, 0.06);
}

.deferred-detail-stat-label {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.deferred-detail-stat-value {
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
}

.deferred-order-filters {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.deferred-order-filters-label {
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--text-secondary);
}

.deferred-filter-chip {
  padding: 0.35rem 0.75rem;
  border-radius: 999px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
}

.deferred-filter-chip--active {
  background: var(--primary-color, #6366f1);
  border-color: var(--primary-color, #6366f1);
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

.deferred-orders-table-wrap {
  max-height: 340px;
  overflow-y: auto;
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
}

.deferred-orders-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2.5rem 1rem;
  color: var(--text-secondary);
  font-size: 0.9rem;
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
  margin: 1rem 0 1.25rem;
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
  color: #4f46e5;
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
  margin-bottom: 1.25rem;
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
  border-color: var(--primary-color, #6366f1);
  background: rgba(99, 102, 241, 0.08);
  color: var(--primary-color, #6366f1);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
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

  .deferred-method-grid {
    grid-template-columns: 1fr;
  }

  .app-section-header--toolbar {
    flex-direction: column;
    align-items: stretch !important;
  }
}
</style>
