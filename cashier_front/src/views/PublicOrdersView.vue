<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content public-orders-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="phone" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("publicOrders") || "طلبات المنيو" }}</h1>
                  <p class="header-subtitle">{{ $t("publicOrdersHint") || "طلبات الزبائن من المنيو الإلكتروني" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="copyMenuLink">
                  <b-icon icon="link-45deg" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("copyLink") || "نسخ الرابط" }}</span>
                </button>
                <button type="button" class="btn-refresh" @click="loadOrders" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="hourglass-split"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading && !orders.length"></b-spinner>
                  <template v-else>{{ pendingCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("pending") || "بانتظار الموافقة" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ approvedCount }}</div>
                <div class="app-overview-stat-label">{{ $t("approved") || "موافق عليها" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="receipt-cutoff"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ orders.length }}</div>
                <div class="app-overview-stat-label">{{ $t("all") || "الكل" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="list-ul"></b-icon>
                </div>
                <div>
                  <h2 class="app-section-title">{{ $t("publicOrdersList") || "قائمة الطلبات" }}</h2>
                  <p class="app-section-subtitle">{{ $t("publicOrdersListHint") || "الموافقة تحول الطلب إلى فاتورة وتطبعه" }}</p>
                </div>
              </div>
            </div>

            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                    <p>{{ $t("publicOrdersListHint") }}</p>
                  </div>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--2">
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("status") || "الحالة" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="filter" class="search-icon"></b-icon>
                    <select v-model="statusFilter" class="users-search-input reports-filter-select" @change="loadOrders">
                      <option value="">{{ $t("all") || "الكل" }}</option>
                      <option value="Pending">{{ $t("pending") || "بانتظار" }}</option>
                      <option value="Approved">{{ $t("approved") || "موافق" }}</option>
                      <option value="Cancelled">{{ $t("cancelled") || "ملغي" }}</option>
                    </select>
                  </div>
                </label>
              </div>
            </div>

            <div class="app-section-body app-section-body--no-padding">
              <div v-if="loading" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="error" class="empty-state">
                <b-icon icon="exclamation-triangle-fill" class="empty-icon"></b-icon>
                <p>{{ error }}</p>
              </div>
              <div v-else-if="!orders.length" class="empty-state">
                <b-icon icon="phone" class="empty-icon"></b-icon>
                <p>{{ $t("noPublicOrders") || "لا توجد طلبات بعد." }}</p>
              </div>
              <div v-else class="report-table-container">
                <div class="table-responsive">
                  <table class="table reports-table users-table">
                    <thead>
                      <tr>
                        <th>{{ $t("orderCode") || "الكود" }}</th>
                        <th>{{ $t("customerName") || "الزبون" }}</th>
                        <th>{{ $t("phoneNumber") || "الهاتف" }}</th>
                        <th>{{ $t("total") || "المجموع" }}</th>
                        <th>{{ $t("date") || "التاريخ" }}</th>
                        <th>{{ $t("status") || "الحالة" }}</th>
                        <th>{{ $t("actions") || "العمليات" }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="order in orders" :key="order.id">
                        <td class="public-order-code">{{ order.orderCode }}</td>
                        <td>{{ order.customerName || "—" }}</td>
                        <td dir="ltr">{{ order.customerPhone || "—" }}</td>
                        <td>{{ formatPrice(order.orderTotalAfterDiscount) }} {{ $t("currency") }}</td>
                        <td>{{ formatDate(order.insertDate) }}</td>
                        <td>
                          <span
                            class="public-order-pill"
                            :class="statusPillClass(order.orderStatus)"
                          >
                            <b-icon :icon="statusIcon(order.orderStatus)"></b-icon>
                            {{ statusLabel(order.orderStatus) }}
                          </span>
                        </td>
                        <td>
                          <div class="actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                            <button
                              type="button"
                              class="action-btn action-btn--icon action-btn--view"
                              :title="$t('details') || 'التفاصيل'"
                              :aria-label="$t('details') || 'التفاصيل'"
                              @click="openDetails(order)"
                            >
                              <b-icon icon="eye-fill" class="action-icon"></b-icon>
                            </button>
                            <button
                              v-if="order.orderStatus === 'Pending'"
                              type="button"
                              class="action-btn action-btn--icon action-btn--success"
                              :disabled="busyId === order.id"
                              :title="$t('approve') || 'موافقة'"
                              :aria-label="$t('approve') || 'موافقة'"
                              @click="approve(order)"
                            >
                              <b-icon icon="check-circle-fill" class="action-icon"></b-icon>
                            </button>
                            <button
                              v-if="order.orderStatus === 'Pending'"
                              type="button"
                              class="action-btn action-btn--icon action-btn--delete"
                              :disabled="busyId === order.id"
                              :title="$t('reject') || 'رفض'"
                              :aria-label="$t('reject') || 'رفض'"
                              @click="cancel(order)"
                            >
                              <b-icon icon="x-circle-fill" class="action-icon"></b-icon>
                            </button>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal
      v-model="showDetails"
      :title="$t('orderDetails') || 'تفاصيل الطلب'"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
      @hidden="selected = null"
    >
      <div class="modal-content-wrapper" v-if="selected">
        <h2 class="modal-title">{{ $t("orderDetails") || "تفاصيل الطلب" }} — {{ selected.orderCode }}</h2>
        <div class="invoice-details-content">
          <div class="invoice-details-grid">
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("customerName") || "الزبون" }}</label>
              <span class="invoice-detail-value">{{ selected.customerName || "—" }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("phoneNumber") || "الهاتف" }}</label>
              <span class="invoice-detail-value" dir="ltr">{{ selected.customerPhone || "—" }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("date") || "التاريخ" }}</label>
              <span class="invoice-detail-value">{{ formatDate(selected.insertDate) }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("status") || "الحالة" }}</label>
              <span class="invoice-detail-value">{{ statusLabel(selected.orderStatus) }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("total") || "المجموع" }}</label>
              <span class="invoice-detail-value invoice-total">
                {{ formatPrice(selected.orderTotalAfterDiscount) }} {{ $t("currency") }}
              </span>
            </div>
            <div class="invoice-detail-item" v-if="selected.notes">
              <label class="invoice-detail-label">{{ $t("notes") || "ملاحظات" }}</label>
              <span class="invoice-detail-value">{{ selected.notes }}</span>
            </div>
          </div>

          <div v-if="selected.items && selected.items.length" class="invoice-items-section">
            <h3 class="invoice-items-title">{{ $t("orderItems") || "عناصر الطلب" }}</h3>
            <table class="invoice-items-table">
              <thead>
                <tr>
                  <th>{{ $t("itemName") || "المنتج" }}</th>
                  <th>{{ $t("quantity") || "الكمية" }}</th>
                  <th>{{ $t("price") || "السعر" }}</th>
                  <th>{{ $t("total") || "المجموع" }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in selected.items" :key="line.id">
                  <td>{{ line.name }}</td>
                  <td>{{ line.quantity }}</td>
                  <td>{{ formatPrice(line.sellingPrice) }} {{ $t("currency") }}</td>
                  <td>{{ formatPrice(line.total) }} {{ $t("currency") }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <div class="users-form-actions">
          <button
            v-if="selected.orderStatus === 'Pending'"
            type="button"
            class="users-form-submit-button"
            :disabled="busyId === selected.id"
            @click="approve(selected)"
          >
            <b-icon icon="check-circle-fill" class="me-2"></b-icon>
            {{ $t("approve") || "موافقة" }}
          </button>
          <button
            v-if="selected.orderStatus === 'Pending'"
            type="button"
            class="users-form-cancel-button"
            :disabled="busyId === selected.id"
            @click="cancel(selected)"
          >
            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
            {{ $t("reject") || "رفض" }}
          </button>
          <button type="button" class="users-form-cancel-button" @click="showDetails = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";
import signalRService from "@/services/signalr.js";
import { formatBusinessDateTime } from "@/utils/formatBusinessDateTime.js";
import { publicMenuUrl, resolveCommercialUserId } from "@/utils/publicMenu.js";
import { printApprovedPublicOrder } from "@/utils/publicMenuPrint.js";

export default {
  name: "PublicOrdersView",
  components: { AppHeader },
  data() {
    return {
      loading: false,
      error: "",
      orders: [],
      pendingCount: 0,
      statusFilter: "Pending",
      busyId: null,
      showDetails: false,
      selected: null,
      commercialUserInfo: {},
    };
  },
  computed: {
    commercialUserId() {
      return resolveCommercialUserId();
    },
    approvedCount() {
      return this.orders.filter((o) => o.orderStatus === "Approved").length;
    },
  },
  async mounted() {
    await this.loadCommercialInfo();
    await this.loadOrders();
    this.bindRealtime();
  },
  beforeDestroy() {
    this.unbindRealtime();
  },
  methods: {
    formatPrice(value) {
      return (Number(value) || 0).toLocaleString("en-US");
    },
    formatDate(iso) {
      return formatBusinessDateTime(iso);
    },
    statusLabel(status) {
      if (status === "Approved") return this.$t("approved") || "موافق";
      if (status === "Cancelled") return this.$t("cancelled") || "ملغي";
      return this.$t("pending") || "بانتظار";
    },
    statusIcon(status) {
      if (status === "Approved") return "check-circle-fill";
      if (status === "Cancelled") return "x-circle-fill";
      return "hourglass-split";
    },
    statusPillClass(status) {
      if (status === "Approved") return "public-order-pill--ok";
      if (status === "Cancelled") return "public-order-pill--off";
      return "public-order-pill--pending";
    },
    normalizeOrder(raw) {
      return {
        id: raw.id ?? raw.Id,
        orderCode: raw.orderCode ?? raw.OrderCode,
        orderStatus: raw.orderStatus ?? raw.OrderStatus,
        paymentStatus: raw.paymentStatus ?? raw.PaymentStatus,
        customerName: raw.customerName ?? raw.CustomerName,
        customerPhone: raw.customerPhone ?? raw.CustomerPhone,
        notes: raw.notes ?? raw.Notes,
        orderTotalAfterDiscount: raw.orderTotalAfterDiscount ?? raw.OrderTotalAfterDiscount,
        insertDate: raw.insertDate ?? raw.InsertDate,
        items: (raw.items || raw.Items || []).map((line) => ({
          id: line.id ?? line.Id,
          name: line.name ?? line.Name,
          quantity: line.quantity ?? line.Quantity,
          sellingPrice: line.sellingPrice ?? line.SellingPrice,
          total: line.total ?? line.Total,
        })),
      };
    },
    async loadCommercialInfo() {
      try {
        const res = await HTTP.get("Admin/CommercialUserInfo");
        this.commercialUserInfo = res?.data?.data || {};
      } catch (_) {
        this.commercialUserInfo = {};
      }
    },
    async loadOrders() {
      if (!this.commercialUserId) {
        this.error = this.$t("invalidCommercialId") || "تعذر تحديد الحساب";
        return;
      }
      this.loading = true;
      this.error = "";
      try {
        const params = new URLSearchParams();
        if (this.statusFilter) params.set("status", this.statusFilter);
        params.set("pageSize", "100");
        const res = await HTTP.get(`PublicMenu/${this.commercialUserId}/orders?${params}`);
        const data = res.data?.data || {};
        this.orders = (data.items || []).map(this.normalizeOrder);
        this.pendingCount = data.pendingCount ?? this.orders.filter((o) => o.orderStatus === "Pending").length;
      } catch (err) {
        this.error = this.$t("errorFetchingOrders") || "تعذر تحميل الطلبات";
      } finally {
        this.loading = false;
      }
    },
    openDetails(order) {
      this.selected = order;
      this.showDetails = true;
    },
    async approve(order) {
      const ok = await this.$confirm({
        title: this.$t("approve") || "موافقة",
        message: this.$t("confirmApprovePublicOrder") || "الموافقة تحول الطلب إلى فاتورة وتطبعه. متابعة؟",
        variant: "warning",
        icon: "check-circle-fill",
        confirmText: this.$t("approve") || "موافقة",
      });
      if (!ok) return;
      this.busyId = order.id;
      try {
        const res = await HTTP.put(`PublicMenu/${this.commercialUserId}/orders/${order.id}/approve`);
        if (res.data?.errorStatus) throw new Error(res.data.message);
        const updated = this.normalizeOrder(res.data.data || order);
        this.showDetails = false;
        try {
          await printApprovedPublicOrder(updated, this.commercialUserInfo, (k) => this.$t(k));
        } catch (_) {
          this.$notify?.error?.(this.$t("printFailed") || "تمت الموافقة لكن فشلت الطباعة");
        }
        await this.loadOrders();
      } catch (err) {
        const msg = err?.response?.data?.message || err.message || "";
        if (String(msg).startsWith("insufficientInventory|")) {
          const parts = String(msg).split("|");
          this.$notify?.error?.(`${this.$t("insufficientInventory") || "المخزون غير كافٍ"}: ${parts[1] || ""}`);
        } else {
          this.$notify?.error?.(this.$t("approveFailed") || "تعذر الموافقة على الطلب");
        }
      } finally {
        this.busyId = null;
      }
    },
    async cancel(order) {
      const ok = await this.$confirm({
        title: this.$t("reject") || "رفض",
        message: this.$t("confirmCancelPublicOrder") || "رفض هذا الطلب؟",
        variant: "danger",
        confirmText: this.$t("reject") || "رفض",
      });
      if (!ok) return;
      this.busyId = order.id;
      try {
        const res = await HTTP.put(`PublicMenu/${this.commercialUserId}/orders/${order.id}/cancel`);
        if (res.data?.errorStatus) throw new Error(res.data.message);
        this.showDetails = false;
        await this.loadOrders();
      } catch (_) {
        this.$notify?.error?.(this.$t("cancelFailed") || "تعذر رفض الطلب");
      } finally {
        this.busyId = null;
      }
    },
    async copyMenuLink() {
      const url = publicMenuUrl(this.commercialUserId);
      try {
        await navigator.clipboard.writeText(url);
        this.$notify?.success?.(this.$t("linkCopied") || "تم نسخ الرابط");
      } catch (_) {
        window.prompt(this.$t("copyLink") || "نسخ الرابط", url);
      }
    },
    onRealtime(payload) {
      const id = Number(payload?.commercialUserId ?? payload?.CommercialUserId);
      if (id && this.commercialUserId && id !== Number(this.commercialUserId)) return;
      this.loadOrders();
    },
    async bindRealtime() {
      try {
        await signalRService.startConnection();
        signalRService.on("PublicOrderAdded", this.onRealtime);
        signalRService.on("PublicOrderUpdated", this.onRealtime);
      } catch (_) {
        /* ignore */
      }
    },
    unbindRealtime() {
      signalRService.off("PublicOrderAdded", this.onRealtime);
      signalRService.off("PublicOrderUpdated", this.onRealtime);
    },
  },
};
</script>

<style scoped>
.public-order-code {
  font-family: ui-monospace, monospace;
  font-weight: 700;
}

.public-order-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 700;
}

.public-order-pill--pending {
  background: rgba(245, 158, 11, 0.16);
  color: #d97706;
}

.public-order-pill--ok {
  background: rgba(34, 197, 94, 0.16);
  color: #16a34a;
}

.public-order-pill--off {
  background: var(--bg-secondary, rgba(148, 163, 184, 0.2));
  color: var(--text-secondary, #64748b);
}

.invoice-details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.invoice-detail-item {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.invoice-detail-label {
  font-size: 13px;
  color: var(--text-secondary, #64748b);
  margin: 0;
}

.invoice-detail-value {
  font-weight: 700;
  color: var(--text-primary, #0f172a);
}

.invoice-total {
  color: var(--primary, #2563eb);
}

.invoice-items-title {
  font-size: 16px;
  font-weight: 700;
  margin: 0 0 0.75rem;
}

.invoice-items-table {
  width: 100%;
  border-collapse: collapse;
}

.invoice-items-table th,
.invoice-items-table td {
  padding: 0.75rem;
  border-bottom: 1px solid var(--border-color);
  text-align: start;
}

.invoice-items-table th {
  background: var(--bg-secondary, #f1f5f9);
  font-weight: 700;
}
</style>
