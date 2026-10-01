<template>
  <b-modal
    id="modal-pos-sync-queue"
    :visible.sync="localVisible"
    hide-header
    hide-footer
    centered
    size="lg"
    modal-class="users-modal pos-ui-modal pos-sync-queue-modal"
    content-class="pos-ui-modal-content"
    body-class="pos-ui-modal-body"
    @shown="onShown"
    @hidden="onHidden"
  >
    <div class="modal-content-wrapper pos-ui-modal-wrapper">
      <div class="pos-ui-modal-hero">
        <div class="pos-ui-modal-hero-icon" aria-hidden="true">
          <b-icon icon="arrow-repeat"></b-icon>
        </div>
        <div class="pos-ui-modal-hero-text">
          <h3 class="pos-ui-modal-title">
            {{ $t("posSyncQueueTitle") || "فواتير المزامنة" }}
          </h3>
          <p class="pos-ui-modal-subtitle">
            {{ $t("posSyncQueueSubtitle") || "عرض الفواتير المعلقة وإعادة المزامنة أو إلغاؤها" }}
          </p>
        </div>
        <button
          type="button"
          class="pos-ui-modal-close"
          :aria-label="$t('close') || 'إغلاق'"
          @click="close"
        >
          <b-icon icon="x-lg"></b-icon>
        </button>
      </div>

      <div class="pos-ui-modal-body-content pos-sync-queue-body">
        <div class="pos-sync-queue-toolbar">
          <div class="pos-sync-queue-filters" role="tablist">
            <button
              type="button"
              class="pos-sync-queue-filter"
              :class="{ 'is-active': filter === 'all' }"
              @click="setFilter('all')"
            >
              {{ $t("posSyncQueueFilterAll") || "الكل" }}
              <span class="pos-sync-queue-filter-count">{{ allCount }}</span>
            </button>
            <button
              type="button"
              class="pos-sync-queue-filter"
              :class="{ 'is-active': filter === 'pending' }"
              @click="setFilter('pending')"
            >
              {{ $t("posSyncQueueFilterPending") || "بانتظار المزامنة" }}
              <span class="pos-sync-queue-filter-count">{{ pendingFilterCount }}</span>
            </button>
            <button
              type="button"
              class="pos-sync-queue-filter"
              :class="{ 'is-active': filter === 'failed' }"
              @click="setFilter('failed')"
            >
              {{ $t("posSyncQueueFilterFailed") || "فشل" }}
              <span class="pos-sync-queue-filter-count">{{ failedFilterCount }}</span>
            </button>
          </div>

          <div class="pos-sync-queue-actions">
            <button
              type="button"
              class="pos-sync-queue-btn pos-sync-queue-btn--ghost"
              :disabled="!orders.length || loading"
              @click="toggleSelectAll"
            >
              {{ allSelected
                ? ($t("deselectAll") || "إلغاء التحديد")
                : ($t("selectAll") || "تحديد الكل") }}
            </button>
            <button
              type="button"
              class="pos-sync-queue-btn pos-sync-queue-btn--primary"
              :disabled="!selectedIds.length || busy"
              @click="retrySelected"
            >
              <b-icon icon="arrow-clockwise"></b-icon>
              {{ $t("posSyncQueueRetrySelected") || "مزامنة المحدد" }}
            </button>
            <button
              type="button"
              class="pos-sync-queue-btn pos-sync-queue-btn--danger"
              :disabled="!selectedIds.length || busy"
              @click="clearSelected"
            >
              <b-icon icon="trash"></b-icon>
              {{ $t("posSyncQueueClearSelected") || "إلغاء المحدد" }}
            </button>
            <button
              type="button"
              class="pos-sync-queue-btn pos-sync-queue-btn--danger-outline"
              :disabled="!orders.length || busy"
              @click="clearAll"
            >
              {{ $t("posSyncQueueClearAll") || "إلغاء الكل" }}
            </button>
          </div>
        </div>

        <div v-if="loading" class="pos-sync-queue-empty">
          <b-spinner small></b-spinner>
          <span>{{ $t("loading") || "جاري التحميل..." }}</span>
        </div>

        <div v-else-if="!orders.length" class="pos-sync-queue-empty">
          <b-icon icon="check2-circle" class="pos-sync-queue-empty-icon"></b-icon>
          <span>{{ $t("posSyncQueueEmpty") || "لا توجد فواتير معلقة للمزامنة" }}</span>
        </div>

        <ul v-else class="pos-sync-queue-list">
          <li
            v-for="order in orders"
            :key="order.clientOrderId"
            class="pos-sync-queue-card"
            :class="'pos-sync-queue-card--' + order.status"
          >
            <div class="pos-sync-queue-card-head">
              <label class="pos-sync-queue-check">
                <input
                  type="checkbox"
                  :checked="isSelected(order.clientOrderId)"
                  :disabled="order.status === 'syncing'"
                  @change="toggleSelect(order.clientOrderId)"
                />
              </label>
              <button
                type="button"
                class="pos-sync-queue-card-main"
                @click="toggleExpand(order.clientOrderId)"
              >
                <div class="pos-sync-queue-card-title-row">
                  <strong class="pos-sync-queue-code">#{{ order.orderCode }}</strong>
                  <span
                    class="pos-sync-queue-status"
                    :class="'pos-sync-queue-status--' + order.status"
                  >
                    {{ statusLabel(order.status) }}
                  </span>
                </div>
                <div class="pos-sync-queue-card-meta">
                  <span>{{ formatWhen(order.soldAt || order.createdAt) }}</span>
                  <span>{{ paymentLabel(order.paymentMethod) }}</span>
                  <span>
                    {{ order.lineCount }}
                    {{ $t("posSyncQueueLines") || "أصناف" }}
                    ·
                    {{ formatMoney(order.itemQty) }}
                    {{ $t("quantity") || "كمية" }}
                  </span>
                  <span class="pos-sync-queue-total">
                    {{ formatMoney(order.orderTotal) }}
                    {{ $t("currency") || "" }}
                  </span>
                </div>
                <p v-if="order.lastError" class="pos-sync-queue-error">
                  {{ order.lastError }}
                </p>
              </button>
              <div class="pos-sync-queue-card-side">
                <button
                  type="button"
                  class="pos-sync-queue-icon-btn"
                  :title="$t('posSyncQueueRetryOne') || 'مزامنة'"
                  :disabled="busy || order.status === 'syncing'"
                  @click="retryOne(order)"
                >
                  <b-icon icon="arrow-clockwise"></b-icon>
                </button>
                <button
                  type="button"
                  class="pos-sync-queue-icon-btn pos-sync-queue-icon-btn--danger"
                  :title="$t('posSyncQueueClearOne') || 'إلغاء المزامنة'"
                  :disabled="busy || order.status === 'syncing'"
                  @click="clearOne(order)"
                >
                  <b-icon icon="x-lg"></b-icon>
                </button>
                <b-icon
                  :icon="expandedId === order.clientOrderId ? 'chevron-up' : 'chevron-down'"
                  class="pos-sync-queue-chevron"
                  @click="toggleExpand(order.clientOrderId)"
                ></b-icon>
              </div>
            </div>

            <div
              v-if="expandedId === order.clientOrderId"
              class="pos-sync-queue-details"
            >
              <div class="pos-sync-queue-detail-grid">
                <div>
                  <span class="pos-sync-queue-detail-label">{{ $t("posSyncQueueClientId") || "معرّف محلي" }}</span>
                  <code>{{ order.clientOrderId }}</code>
                </div>
                <div>
                  <span class="pos-sync-queue-detail-label">{{ $t("attempts") || "المحاولات" }}</span>
                  <strong>{{ formatMoney(order.attempts) }}</strong>
                </div>
                <div v-if="order.isWholesale">
                  <span class="pos-sync-queue-detail-label">{{ $t("wholesale") || "جملة" }}</span>
                  <strong>{{ $t("yes") || "نعم" }}</strong>
                </div>
                <div v-if="order.discountAmount > 0">
                  <span class="pos-sync-queue-detail-label">{{ $t("discountLabel") || "الخصم" }}</span>
                  <strong>− {{ formatMoney(order.discountAmount) }}</strong>
                </div>
                <div v-if="order.notes">
                  <span class="pos-sync-queue-detail-label">{{ $t("notes") || "ملاحظات" }}</span>
                  <strong>{{ order.notes }}</strong>
                </div>
              </div>

              <table class="pos-sync-queue-lines-table">
                <thead>
                  <tr>
                    <th>{{ $t("item") || "الصنف" }}</th>
                    <th>{{ $t("quantity") || "الكمية" }}</th>
                    <th>{{ $t("price") || "السعر" }}</th>
                    <th>{{ $t("total") || "الإجمالي" }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(line, idx) in order.lines" :key="idx">
                    <td>
                      <div class="pos-sync-queue-line-name">{{ line.name }}</div>
                      <div v-if="line.code" class="pos-sync-queue-line-code">{{ line.code }}</div>
                      <div v-if="line.notes" class="pos-sync-queue-line-note">{{ line.notes }}</div>
                    </td>
                    <td>{{ formatMoney(line.quantity) }}</td>
                    <td>{{ formatMoney(line.unitPrice) }}</td>
                    <td>{{ formatMoney(line.lineTotal) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </li>
        </ul>
      </div>

      <div class="pos-ui-modal-actions pos-ui-modal-actions--single">
        <button
          type="button"
          class="pos-ui-modal-btn pos-ui-modal-btn--primary"
          @click="close"
        >
          {{ $t("close") || "إغلاق" }}
        </button>
      </div>
    </div>
  </b-modal>
</template>

<script>
import {
  clearQueuedOrders,
  listQueuedOrders,
  retryQueuedOrders,
  subscribePosSync,
} from "@/utils/posSync.js";
import { formatDateTimeLatn, formatMoney } from "@/utils/formatMoney.js";

export default {
  name: "PosSyncQueueModal",
  props: {
    visible: { type: Boolean, default: false },
  },
  data() {
    return {
      localVisible: false,
      filter: "all",
      orders: [],
      allCount: 0,
      pendingFilterCount: 0,
      failedFilterCount: 0,
      selectedIds: [],
      expandedId: null,
      loading: false,
      busy: false,
      _unsub: null,
      _refreshTimer: null,
    };
  },
  computed: {
    allSelected() {
      const selectable = this.orders.filter((o) => o.status !== "syncing");
      return (
        selectable.length > 0 &&
        selectable.every((o) => this.selectedIds.includes(o.clientOrderId))
      );
    },
  },
  watch: {
    visible: {
      immediate: true,
      handler(v) {
        this.localVisible = !!v;
        if (v) this.refresh();
      },
    },
    localVisible(v) {
      if (v !== this.visible) this.$emit("update:visible", v);
    },
  },
  beforeDestroy() {
    this.teardown();
  },
  methods: {
    formatMoney,
    close() {
      this.localVisible = false;
    },
    onShown() {
      this._unsub = subscribePosSync(() => {
        clearTimeout(this._refreshTimer);
        this._refreshTimer = setTimeout(() => this.refresh({ silent: true }), 400);
      });
      this.refresh();
    },
    onHidden() {
      this.teardown();
      this.selectedIds = [];
      this.expandedId = null;
      this.$emit("update:visible", false);
    },
    teardown() {
      if (this._unsub) {
        this._unsub();
        this._unsub = null;
      }
      clearTimeout(this._refreshTimer);
    },
    setFilter(next) {
      if (this.filter === next) return;
      this.filter = next;
      this.selectedIds = [];
      this.expandedId = null;
      this.refresh();
    },
    async refresh({ silent = false } = {}) {
      if (!silent) this.loading = true;
      try {
        const [filtered, all] = await Promise.all([
          listQueuedOrders({ filter: this.filter }),
          listQueuedOrders({ filter: "all" }),
        ]);
        this.orders = filtered;
        this.allCount = all.length;
        this.pendingFilterCount = all.filter(
          (o) => o.status === "pending" || o.status === "syncing"
        ).length;
        this.failedFilterCount = all.filter((o) => o.status === "failed").length;
        const alive = new Set(filtered.map((o) => o.clientOrderId));
        this.selectedIds = this.selectedIds.filter((id) => alive.has(id));
      } catch (err) {
        console.warn("pos sync queue refresh failed", err);
      } finally {
        this.loading = false;
      }
    },
    isSelected(id) {
      return this.selectedIds.includes(id);
    },
    toggleSelect(id) {
      if (this.selectedIds.includes(id)) {
        this.selectedIds = this.selectedIds.filter((x) => x !== id);
      } else {
        this.selectedIds = this.selectedIds.concat(id);
      }
    },
    toggleSelectAll() {
      if (this.allSelected) {
        this.selectedIds = [];
        return;
      }
      this.selectedIds = this.orders
        .filter((o) => o.status !== "syncing")
        .map((o) => o.clientOrderId);
    },
    toggleExpand(id) {
      this.expandedId = this.expandedId === id ? null : id;
    },
    statusLabel(status) {
      if (status === "failed") return this.$t("posSyncQueueStatusFailed") || "فشل";
      if (status === "syncing") return this.$t("posSyncQueueStatusSyncing") || "جاري الإرسال";
      return this.$t("posSyncQueueStatusPending") || "بانتظار الإرسال";
    },
    paymentLabel(method) {
      const m = String(method || "Cash");
      if (m === "Card") return this.$t("card") || "بطاقة";
      if (m === "Credit") return this.$t("credit") || "آجل";
      if (m === "Transfer") return this.$t("transfer") || "تحويل";
      return this.$t("cash") || "نقد";
    },
    formatWhen(value) {
      if (!value) return "—";
      const locale = this.$i18n?.locale === "en" ? "en" : "ar";
      if (typeof value === "number") {
        return formatDateTimeLatn(new Date(value), locale);
      }
      return formatDateTimeLatn(value, locale);
    },
    async retryOne(order) {
      this.busy = true;
      try {
        await retryQueuedOrders([order.clientOrderId]);
        await this.refresh({ silent: true });
        this.$emit("synced");
      } finally {
        this.busy = false;
      }
    },
    async retrySelected() {
      if (!this.selectedIds.length) return;
      this.busy = true;
      try {
        await retryQueuedOrders(this.selectedIds.slice());
        this.selectedIds = [];
        await this.refresh({ silent: true });
        this.$emit("synced");
      } finally {
        this.busy = false;
      }
    },
    async confirmClear(message, count) {
      if (!this.$confirm) return window.confirm(message);
      return this.$confirm({
        title: this.$t("posSyncQueueClearTitle") || "إلغاء المزامنة",
        message,
        confirmText: this.$t("posSyncQueueClearConfirm") || "إلغاء المزامنة",
        cancelText: this.$t("cancelButton") || "تراجع",
        variant: "danger",
        icon: "trash-fill",
      });
    },
    async clearOne(order) {
      const msg =
        this.$t("posSyncQueueClearOneMsg", { code: `#${order.orderCode}` }) ||
        `إلغاء مزامنة الفاتورة #${order.orderCode}؟ لن تُرسل للخادم.`;
      const ok = await this.confirmClear(msg, 1);
      if (!ok) return;
      this.busy = true;
      try {
        await clearQueuedOrders([order.clientOrderId]);
        await this.refresh({ silent: true });
        this.$emit("cleared");
      } finally {
        this.busy = false;
      }
    },
    async clearSelected() {
      if (!this.selectedIds.length) return;
      const n = this.selectedIds.length;
      const msg =
        this.$t("posSyncQueueClearSelectedMsg", { n }) ||
        `إلغاء مزامنة ${n} فاتورة محددة؟ لن تُرسل للخادم.`;
      const ok = await this.confirmClear(msg, n);
      if (!ok) return;
      this.busy = true;
      try {
        await clearQueuedOrders(this.selectedIds.slice());
        this.selectedIds = [];
        await this.refresh({ silent: true });
        this.$emit("cleared");
      } finally {
        this.busy = false;
      }
    },
    async clearAll() {
      const n = this.orders.length;
      if (!n) return;
      const msg =
        this.filter === "all"
          ? this.$t("posSyncQueueClearAllMsg", { n }) ||
            `إلغاء مزامنة كل الفواتير المعلقة (${n})؟ لن تُرسل للخادم.`
          : this.$t("posSyncQueueClearPendingMsg", { n }) ||
            `إلغاء مزامنة الفواتير بانتظار الإرسال (${n})؟ لن تُرسل للخادم.`;
      const ok = await this.confirmClear(msg, n);
      if (!ok) return;
      this.busy = true;
      try {
        await clearQueuedOrders(null, { filter: this.filter });
        this.selectedIds = [];
        await this.refresh({ silent: true });
        this.$emit("cleared");
      } finally {
        this.busy = false;
      }
    },
  },
};
</script>

<style scoped>
.pos-sync-queue-body {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  max-height: min(62vh, 560px);
  overflow: auto;
}

.pos-sync-queue-toolbar {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  position: sticky;
  top: 0;
  z-index: 2;
  background: var(--bg-primary, #fff);
  padding-bottom: 0.25rem;
}

.pos-sync-queue-filters {
  display: inline-flex;
  gap: 0.4rem;
  flex-wrap: wrap;
}

.pos-sync-queue-filter {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  border: 1px solid var(--border-color, #d7dee8);
  background: var(--bg-tertiary, #f4f7fb);
  color: inherit;
  border-radius: 999px;
  padding: 0.35rem 0.75rem;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
}

.pos-sync-queue-filter.is-active {
  background: #02265b;
  border-color: #02265b;
  color: #fff;
}

.pos-sync-queue-filter-count {
  min-width: 1.35rem;
  height: 1.35rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  background: rgba(0, 0, 0, 0.08);
}

.pos-sync-queue-filter.is-active .pos-sync-queue-filter-count {
  background: rgba(255, 255, 255, 0.2);
}

.pos-sync-queue-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.pos-sync-queue-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  border-radius: 0.55rem;
  border: 1px solid transparent;
  padding: 0.4rem 0.7rem;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
}

.pos-sync-queue-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.pos-sync-queue-btn--ghost {
  background: transparent;
  border-color: var(--border-color, #d7dee8);
  color: inherit;
}

.pos-sync-queue-btn--primary {
  background: #0056f3;
  color: #fff;
}

.pos-sync-queue-btn--danger {
  background: #dc3545;
  color: #fff;
}

.pos-sync-queue-btn--danger-outline {
  background: transparent;
  border-color: #dc3545;
  color: #dc3545;
}

.pos-sync-queue-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  padding: 2.5rem 1rem;
  color: var(--text-secondary, #6b778c);
  text-align: center;
}

.pos-sync-queue-empty-icon {
  font-size: 2rem;
  color: #198754;
}

.pos-sync-queue-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.pos-sync-queue-card {
  border: 1px solid var(--border-color, #d7dee8);
  border-radius: 0.85rem;
  background: var(--bg-secondary, #fff);
  overflow: hidden;
}

.pos-sync-queue-card--failed {
  border-color: rgba(220, 53, 69, 0.35);
}

.pos-sync-queue-card--syncing {
  border-color: rgba(0, 86, 243, 0.35);
}

.pos-sync-queue-card-head {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 0.75rem;
}

.pos-sync-queue-check {
  padding-top: 0.2rem;
  margin: 0;
}

.pos-sync-queue-card-main {
  flex: 1;
  border: none;
  background: transparent;
  text-align: start;
  padding: 0;
  color: inherit;
  cursor: pointer;
  min-width: 0;
}

.pos-sync-queue-card-title-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  margin-bottom: 0.25rem;
}

.pos-sync-queue-code {
  font-size: 0.95rem;
}

.pos-sync-queue-status {
  font-size: 0.72rem;
  font-weight: 700;
  border-radius: 999px;
  padding: 0.15rem 0.5rem;
  background: #fff3cd;
  color: #856404;
}

.pos-sync-queue-status--failed {
  background: #f8d7da;
  color: #842029;
}

.pos-sync-queue-status--syncing {
  background: #cfe2ff;
  color: #084298;
}

.pos-sync-queue-status--pending {
  background: #fff3cd;
  color: #856404;
}

.pos-sync-queue-card-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem 0.75rem;
  font-size: 0.8rem;
  color: var(--text-secondary, #6b778c);
}

.pos-sync-queue-total {
  font-weight: 700;
  color: inherit;
}

.pos-sync-queue-error {
  margin: 0.4rem 0 0;
  font-size: 0.78rem;
  color: #b02a37;
  word-break: break-word;
}

.pos-sync-queue-card-side {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  flex-shrink: 0;
}

.pos-sync-queue-icon-btn {
  width: 2rem;
  height: 2rem;
  border-radius: 0.5rem;
  border: 1px solid var(--border-color, #d7dee8);
  background: var(--bg-tertiary, #f4f7fb);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: inherit;
}

.pos-sync-queue-icon-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.pos-sync-queue-icon-btn--danger {
  color: #dc3545;
}

.pos-sync-queue-chevron {
  cursor: pointer;
  opacity: 0.7;
}

.pos-sync-queue-details {
  border-top: 1px solid var(--border-color, #e6ebf2);
  padding: 0.75rem;
  background: var(--bg-tertiary, #f7f9fc);
}

.pos-sync-queue-detail-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 0.55rem;
  margin-bottom: 0.75rem;
  font-size: 0.82rem;
}

.pos-sync-queue-detail-label {
  display: block;
  color: var(--text-secondary, #6b778c);
  margin-bottom: 0.15rem;
  font-size: 0.72rem;
}

.pos-sync-queue-lines-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.8rem;
}

.pos-sync-queue-lines-table th,
.pos-sync-queue-lines-table td {
  padding: 0.4rem 0.35rem;
  border-bottom: 1px solid var(--border-color, #e6ebf2);
  text-align: start;
}

.pos-sync-queue-lines-table th {
  color: var(--text-secondary, #6b778c);
  font-weight: 600;
}

.pos-sync-queue-line-name {
  font-weight: 600;
}

.pos-sync-queue-line-code,
.pos-sync-queue-line-note {
  font-size: 0.72rem;
  color: var(--text-secondary, #6b778c);
}

@media (max-width: 700px) {
  .pos-sync-queue-card-head {
    flex-wrap: wrap;
  }

  .pos-sync-queue-card-side {
    width: 100%;
    justify-content: flex-end;
  }
}
</style>
