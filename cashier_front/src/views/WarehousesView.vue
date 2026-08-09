<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content warehouses-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="building" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("warehousesTitle") }}</h1>
                  <p class="header-subtitle">{{ $t("warehousesSubtitle") }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button
                  type="button"
                  class="btn-refresh"
                  :disabled="loading"
                  @click="loadAll"
                >
                  <b-icon
                    icon="arrow-clockwise"
                    class="button-icon"
                    :class="{ spinning: loading }"
                  ></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
                <button type="button" class="users-add-button" @click="openCreate">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addWarehouse") }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="building"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ warehouses.length }}</template>
                </div>
                <div class="app-overview-stat-label">
                  {{ $t("warehousesOverviewTotal") || "إجمالي المخازن" }}
                </div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ activeCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("active") || "نشط" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                <b-icon icon="x-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ inactiveCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("inactive") || "غير نشط" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="star-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value warehouses-default-stat">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ defaultWarehouseName }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("defaultWarehouse") }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="building"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">
                    {{ $t("warehousesListTitle") || "قائمة المخازن" }}
                  </h3>
                  <p class="app-section-subtitle">
                    {{
                      $t("warehousesListHint") ||
                      "إدارة المخازن وتعيين المخزن الافتراضي للمبيعات"
                    }}
                  </p>
                </div>
              </div>
            </div>

            <div class="app-section-body">
              <div v-if="loading" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>

              <div v-else-if="warehouses.length" class="warehouses-cards-grid">
                <div
                  v-for="wh in warehouses"
                  :key="wh.id"
                  class="warehouse-card"
                  :class="{
                    'warehouse-card--default': wh.isDefault,
                    'warehouse-card--inactive': !wh.isActive,
                  }"
                >
                  <div class="warehouse-card__header">
                    <div class="warehouse-card__title">
                      <span class="warehouse-card__icon">
                        <b-icon icon="building"></b-icon>
                      </span>
                      <div>
                        <h4 class="warehouse-card__name" :title="wh.name">{{ wh.name }}</h4>
                        <p class="warehouse-card__meta">
                          #{{ wh.id }}
                        </p>
                      </div>
                    </div>
                    <div
                      class="actions-cell"
                      role="group"
                      :aria-label="$t('actions') || 'العمليات'"
                    >
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        :title="$t('edit') || 'تعديل'"
                        :aria-label="$t('edit') || 'تعديل'"
                        @click="openEdit(wh)"
                      >
                        <b-icon icon="pencil-square" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--delete"
                        :disabled="wh.isDefault"
                        :title="
                          wh.isDefault
                            ? $t('cannotDeleteDefaultWarehouse')
                            : $t('delete') || 'حذف'
                        "
                        :aria-label="$t('delete') || 'حذف'"
                        @click="removeWarehouse(wh)"
                      >
                        <b-icon icon="trash" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </div>

                  <div class="warehouse-card__badges">
                    <span
                      v-if="wh.isDefault"
                      class="warehouse-status-pill warehouse-status-pill--default"
                    >
                      <b-icon icon="star-fill"></b-icon>
                      {{ $t("defaultWarehouse") }}
                    </span>
                    <span
                      class="warehouse-status-pill"
                      :class="
                        wh.isActive
                          ? 'warehouse-status-pill--active'
                          : 'warehouse-status-pill--inactive'
                      "
                    >
                      <b-icon
                        :icon="wh.isActive ? 'check-circle-fill' : 'x-circle-fill'"
                      ></b-icon>
                      {{ wh.isActive ? $t("active") : $t("inactive") }}
                    </span>
                  </div>
                </div>
              </div>

              <div v-else class="empty-state">
                <b-icon icon="building" class="empty-icon"></b-icon>
                <p>{{ $t("noWarehousesYet") }}</p>
                <button type="button" class="empty-state-btn" @click="openCreate">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addWarehouse") }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="arrow-left-right"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("transferStockTitle") }}</h3>
                  <p class="app-section-subtitle">
                    {{
                      $t("transferStockHint") ||
                      "نقل كمية منتج من مخزن إلى آخر"
                    }}
                  </p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <form class="users-form warehouses-transfer-form" @submit.prevent="transferStock">
                <div class="modal-form-grid warehouses-transfer-grid">
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="box-seam" class="form-label-icon"></b-icon>
                      {{ $t("Items") || $t("selectItem") }}
                      <span class="required">*</span>
                    </label>
                    <select
                      v-model.number="transfer.itemId"
                      class="users-form-input"
                      required
                    >
                      <option :value="0" disabled>{{ $t("selectItem") }}</option>
                      <option v-for="it in items" :key="it.id" :value="it.id">
                        {{ it.name }} ({{ it.quantity }})
                      </option>
                    </select>
                  </div>
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="box-arrow-up" class="form-label-icon"></b-icon>
                      {{ $t("fromWarehouse") }}
                      <span class="required">*</span>
                    </label>
                    <select
                      v-model.number="transfer.fromWarehouseId"
                      class="users-form-input"
                      required
                    >
                      <option
                        v-for="w in activeWarehouses"
                        :key="'f' + w.id"
                        :value="w.id"
                      >
                        {{ w.name }}
                      </option>
                    </select>
                  </div>
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="box-arrow-in-down" class="form-label-icon"></b-icon>
                      {{ $t("toWarehouse") }}
                      <span class="required">*</span>
                    </label>
                    <select
                      v-model.number="transfer.toWarehouseId"
                      class="users-form-input"
                      required
                    >
                      <option
                        v-for="w in activeWarehouses"
                        :key="'t' + w.id"
                        :value="w.id"
                      >
                        {{ w.name }}
                      </option>
                    </select>
                  </div>
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="123" class="form-label-icon"></b-icon>
                      {{ $t("quantityLabel") || $t("quantity_label") || "الكمية" }}
                      <span class="required">*</span>
                    </label>
                    <input
                      v-model.number="transfer.quantity"
                      type="number"
                      min="1"
                      class="users-form-input"
                      required
                    />
                  </div>
                </div>
                <div class="users-form-actions warehouses-transfer-actions">
                  <button
                    type="submit"
                    class="users-form-submit-button"
                    :disabled="transferring || !canTransfer"
                  >
                    <b-spinner small v-if="transferring" class="me-2"></b-spinner>
                    <b-icon v-else icon="arrow-left-right" class="me-2"></b-icon>
                    {{ $t("transferStock") }}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal
      id="modal-warehouse"
      hide-header
      hide-footer
      class="users-modal"
      centered
      @hidden="resetForm"
    >
      <div class="modal-content-wrapper">
        <div class="modal-title-row">
          <span class="modal-title-icon">
            <b-icon :icon="editing ? 'pencil-square' : 'plus-circle-fill'"></b-icon>
          </span>
          <h2 class="modal-title">
            {{ editing ? $t("editWarehouse") : $t("addWarehouse") }}
          </h2>
        </div>
        <form class="users-form" @submit.prevent="saveWarehouse">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="building" class="form-label-icon"></b-icon>
              {{ $t("warehouseName") }}
              <span class="required">*</span>
            </label>
            <input
              v-model="form.name"
              type="text"
              class="users-form-input"
              required
              :placeholder="$t('warehouseName')"
            />
          </div>

          <div class="form-toggle-cards form-toggle-cards--stack">
            <label
              class="form-toggle-card"
              :class="{ 'form-toggle-card--on': form.isDefault }"
            >
              <input
                v-model="form.isDefault"
                type="checkbox"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--warning">
                  <b-icon icon="star-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("defaultWarehouse") }}</span>
                  <span class="form-toggle-card-desc">
                    {{
                      $t("defaultWarehouseHint") ||
                      "يُستخدم تلقائياً عند البيع إن لم يُختر مخزن"
                    }}
                  </span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card"
              :class="{ 'form-toggle-card--on': form.isActive }"
            >
              <input
                v-model="form.isActive"
                type="checkbox"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--success">
                  <b-icon icon="check-circle-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("active") }}</span>
                  <span class="form-toggle-card-desc">
                    {{
                      $t("warehouseActiveHint") ||
                      "المخازن غير النشطة لا تظهر في نقطة البيع"
                    }}
                  </span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
          </div>

          <div class="users-form-actions">
            <button
              type="button"
              class="users-form-cancel-button"
              :disabled="saving"
              @click="$bvModal.hide('modal-warehouse')"
            >
              {{ $t("closeButton") || $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="saving">
              <b-spinner small v-if="saving" class="me-2"></b-spinner>
              {{ $t("save") || $t("addButton") || "حفظ" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api";

export default {
  name: "WarehousesView",
  components: { AppHeader },
  data() {
    return {
      loading: false,
      saving: false,
      transferring: false,
      warehouses: [],
      items: [],
      editing: false,
      editId: null,
      form: { name: "", isDefault: false, isActive: true },
      transfer: {
        itemId: 0,
        fromWarehouseId: 0,
        toWarehouseId: 0,
        quantity: 1,
      },
    };
  },
  computed: {
    activeWarehouses() {
      return (this.warehouses || []).filter((w) => w.isActive);
    },
    activeCount() {
      return this.activeWarehouses.length;
    },
    inactiveCount() {
      return (this.warehouses || []).filter((w) => !w.isActive).length;
    },
    defaultWarehouseName() {
      const def = (this.warehouses || []).find((w) => w.isDefault);
      return def?.name || "—";
    },
    canTransfer() {
      return (
        !!this.transfer.itemId &&
        !!this.transfer.fromWarehouseId &&
        !!this.transfer.toWarehouseId &&
        this.transfer.fromWarehouseId !== this.transfer.toWarehouseId &&
        Number(this.transfer.quantity) > 0
      );
    },
  },
  mounted() {
    this.loadAll();
  },
  methods: {
    resetForm() {
      this.editing = false;
      this.editId = null;
      this.form = { name: "", isDefault: false, isActive: true };
    },
    async loadAll() {
      this.loading = true;
      try {
        await Promise.all([this.loadWarehouses(), this.loadItems()]);
      } finally {
        this.loading = false;
      }
    },
    async loadWarehouses() {
      const res = await HTTP.get("Warehouses");
      this.warehouses = res.data?.data || [];
      if (this.activeWarehouses.length) {
        if (!this.transfer.fromWarehouseId) {
          this.transfer.fromWarehouseId = this.activeWarehouses[0].id;
        }
        if (!this.transfer.toWarehouseId) {
          this.transfer.toWarehouseId =
            this.activeWarehouses[1]?.id || this.activeWarehouses[0].id;
        }
      }
    },
    async loadItems() {
      const res = await HTTP.get("Admin/GetItems", {
        params: { pageNumber: 0, pageSize: 500, info: "" },
      });
      this.items = res.data?.data?.items || [];
    },
    openCreate() {
      this.editing = false;
      this.editId = null;
      this.form = { name: "", isDefault: false, isActive: true };
      this.$bvModal.show("modal-warehouse");
    },
    openEdit(item) {
      this.editing = true;
      this.editId = item.id;
      this.form = {
        name: item.name,
        isDefault: !!item.isDefault,
        isActive: !!item.isActive,
      };
      this.$bvModal.show("modal-warehouse");
    },
    async saveWarehouse() {
      this.saving = true;
      try {
        if (this.editing) {
          await HTTP.put(`Warehouses/${this.editId}`, this.form);
        } else {
          await HTTP.post("Warehouses", this.form);
        }
        this.$bvModal.hide("modal-warehouse");
        await this.loadWarehouses();
        this.$notify.success(this.$t("savedSuccessfully") || "OK", {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
      } catch (e) {
        this.$notify.error(
          e?.response?.data?.message || this.$t("error") || "Error",
          { position: "top-right", timeout: 3500, maxToasts: 1 }
        );
      } finally {
        this.saving = false;
      }
    },
    async removeWarehouse(item) {
      if (item.isDefault) return;
      if (!confirm(this.$t("confirmDeleteWarehouse"))) return;
      try {
        await HTTP.delete(`Warehouses/${item.id}`);
        await this.loadWarehouses();
        this.$notify.success(this.$t("deletedSuccessfully") || "OK", {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
      } catch (e) {
        this.$notify.error(
          e?.response?.data?.message || this.$t("error") || "Error",
          { position: "top-right", timeout: 3500, maxToasts: 1 }
        );
      }
    },
    async transferStock() {
      if (!this.canTransfer) return;
      this.transferring = true;
      try {
        await HTTP.post("Warehouses/Transfer", {
          itemId: this.transfer.itemId,
          fromWarehouseId: this.transfer.fromWarehouseId,
          toWarehouseId: this.transfer.toWarehouseId,
          quantity: this.transfer.quantity,
        });
        this.$notify.success(this.$t("transferSuccess"), {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        await this.loadItems();
      } catch (e) {
        this.$notify.error(
          e?.response?.data?.message || this.$t("error") || "Error",
          { position: "top-right", timeout: 3500, maxToasts: 1 }
        );
      } finally {
        this.transferring = false;
      }
    },
  },
};
</script>

<style scoped>
.warehouses-page .app-section-card + .app-section-card {
  margin-top: 1.25rem;
}

.warehouses-default-stat {
  font-size: 1.05rem;
  line-height: 1.3;
  word-break: break-word;
}

.warehouses-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

.warehouse-card {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  padding: 1rem 1.1rem;
  border-radius: 14px;
  border: 1px solid var(--border-color, rgba(148, 163, 184, 0.28));
  background: linear-gradient(
    160deg,
    rgba(148, 163, 184, 0.08) 0%,
    rgba(148, 163, 184, 0.02) 100%
  );
  transition: border-color 0.15s ease, transform 0.15s ease;
}

.warehouse-card:hover {
  border-color: rgba(15, 110, 110, 0.45);
}

.warehouse-card--default {
  border-color: rgba(15, 110, 110, 0.55);
  background: linear-gradient(
    160deg,
    rgba(15, 110, 110, 0.14) 0%,
    rgba(15, 110, 110, 0.03) 100%
  );
}

.warehouse-card--inactive {
  opacity: 0.78;
}

.warehouse-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
}

.warehouse-card__title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
}

.warehouse-card__icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: rgba(15, 110, 110, 0.15);
  color: #0f6e6e;
  flex-shrink: 0;
  font-size: 1.15rem;
}

.warehouse-card__name {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text-primary, #e2e8f0);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 180px;
}

.warehouse-card__meta {
  margin: 0.15rem 0 0;
  font-size: 0.8rem;
  color: var(--text-secondary, #94a3b8);
}

.warehouse-card__badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
}

.warehouse-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.28rem 0.65rem;
  border-radius: 999px;
  font-size: 0.78rem;
  font-weight: 700;
}

.warehouse-status-pill--default {
  background: rgba(15, 110, 110, 0.18);
  color: #14b8a6;
}

.warehouse-status-pill--active {
  background: rgba(34, 197, 94, 0.16);
  color: #4ade80;
}

.warehouse-status-pill--inactive {
  background: rgba(148, 163, 184, 0.18);
  color: #94a3b8;
}

.warehouses-transfer-grid {
  margin-bottom: 0.25rem;
}

.warehouses-transfer-actions {
  justify-content: flex-start;
  margin-top: 0.5rem;
}

@media (max-width: 640px) {
  .warehouse-card__name {
    max-width: 140px;
  }
}
</style>
