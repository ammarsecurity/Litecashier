<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content settings-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="gear-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("settingsTitle") }}</h1>
                  <p class="header-subtitle">{{ $t("settingsSubtitle") }}</p>
                </div>
              </div>
            </div>
          </div>

          <div class="app-section-card settings-danger-zone">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap settings-danger-zone__icon">
                  <b-icon icon="exclamation-triangle-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("settingsDangerZoneTitle") }}</h3>
                  <p class="app-section-subtitle">{{ $t("settingsDangerZoneSubtitle") }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <p class="settings-danger-zone__intro">{{ $t("clearCatalogWarning") }}</p>
              <ul class="settings-danger-zone__list">
                <li>{{ $t("clearCatalogTags") }}</li>
                <li>{{ $t("clearCatalogItems") }}</li>
                <li>{{ $t("clearCatalogOrders") }}</li>
                <li>{{ $t("clearCatalogStockMovements") }}</li>
                <li>{{ $t("clearCatalogSuppliers") }}</li>
              </ul>
              <p class="settings-danger-zone__hint">{{ $t("clearCatalogNotClearedHint") }}</p>
              <div class="settings-danger-zone__actions">
                <button
                  type="button"
                  class="catalog-clear-btn"
                  v-b-modal.modal-clearCatalog
                >
                  <b-icon icon="trash-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("clearCatalogData") }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal id="modal-clearCatalog" hide-header hide-footer class="users-modal">
      <div class="modal-content-wrapper">
        <div class="delete-confirmation-content">
          <div class="delete-icon-wrapper">
            <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
          </div>
          <h3 class="delete-confirmation-title">{{ $t("clearCatalogTitle") }}</h3>
          <p class="delete-confirmation-text">{{ $t("clearCatalogWarning") }}</p>
          <ul class="settings-danger-zone__list settings-danger-zone__list--modal">
            <li>{{ $t("clearCatalogTags") }}</li>
            <li>{{ $t("clearCatalogItems") }}</li>
            <li>{{ $t("clearCatalogOrders") }}</li>
            <li>{{ $t("clearCatalogStockMovements") }}</li>
            <li>{{ $t("clearCatalogSuppliers") }}</li>
          </ul>
          <p class="settings-danger-zone__hint settings-danger-zone__hint--modal">
            {{ $t("clearCatalogNotClearedHint") }}
          </p>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("clearCatalogPasswordLabel") }}</label>
            <input
              v-model="clearCatalogPassword"
              type="password"
              class="users-form-input"
              :placeholder="$t('clearCatalogPasswordPlaceholder')"
              autocomplete="current-password"
              @keyup.enter="executeClearCatalog"
            />
          </div>
          <div v-if="clearCatalogResult" class="import-items-summary">
            <div class="import-items-summary-row">
              <span>{{ $t("clearCatalogTags") }}</span>
              <strong>{{ clearCatalogResult.tagsCleared }}</strong>
            </div>
            <div class="import-items-summary-row">
              <span>{{ $t("clearCatalogItems") }}</span>
              <strong>{{ clearCatalogResult.itemsCleared }}</strong>
            </div>
            <div class="import-items-summary-row">
              <span>{{ $t("clearCatalogOrders") }}</span>
              <strong>{{ clearCatalogResult.ordersCleared }}</strong>
            </div>
            <div class="import-items-summary-row">
              <span>{{ $t("clearCatalogStockMovements") }}</span>
              <strong>{{ clearCatalogResult.stockMovementsCleared }}</strong>
            </div>
            <div class="import-items-summary-row">
              <span>{{ $t("clearCatalogSuppliers") }}</span>
              <strong>{{ clearCatalogResult.suppliersCleared }}</strong>
            </div>
          </div>
          <div class="delete-confirmation-actions">
            <button
              type="button"
              class="delete-confirm-button"
              :disabled="clearCatalogLoading || !clearCatalogPassword"
              @click="executeClearCatalog"
            >
              <b-spinner small v-if="clearCatalogLoading" class="me-2"></b-spinner>
              <b-icon v-else icon="trash-fill" class="me-2"></b-icon>
              {{ $t("clearCatalogConfirm") }}
            </button>
            <button
              type="button"
              class="delete-cancel-button"
              :disabled="clearCatalogLoading"
              @click="closeClearCatalogModal"
            >
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancelButtonLabel") }}
            </button>
          </div>
        </div>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";

export default {
  name: "SettingsView",
  components: { AppHeader },
  data() {
    return {
      clearCatalogPassword: "",
      clearCatalogLoading: false,
      clearCatalogResult: null,
    };
  },
  methods: {
    closeClearCatalogModal() {
      this.$bvModal.hide("modal-clearCatalog");
      this.clearCatalogPassword = "";
      this.clearCatalogResult = null;
      this.clearCatalogLoading = false;
    },
    async executeClearCatalog() {
      if (!this.clearCatalogPassword || this.clearCatalogLoading) return;

      this.clearCatalogLoading = true;
      this.clearCatalogResult = null;

      try {
        const response = await HTTP.post("Admin/ClearCatalog", {
          password: this.clearCatalogPassword,
        });
        const payload = response?.data;
        this.clearCatalogResult = payload?.data || null;

        this.$notify.success(
          this.$te(payload?.message) ? this.$t(payload.message) : this.$t("catalogClearSuccess"),
          { position: "top-right", timeout: 4500, maxToasts: 1 }
        );
      } catch (error) {
        const msg = error?.response?.data?.message;
        const text =
          msg && this.$te(msg) ? this.$t(msg) : this.$t("catalogClearFailed");
        this.$notify.error(text, {
          position: "top-right",
          timeout: 4000,
          maxToasts: 1,
        });
      } finally {
        this.clearCatalogLoading = false;
      }
    },
  },
};
</script>

<style scoped>
.settings-danger-zone {
  border-color: rgba(239, 68, 68, 0.35);
  background: linear-gradient(
    135deg,
    rgba(239, 68, 68, 0.06) 0%,
    rgba(239, 68, 68, 0.02) 100%
  );
}

.settings-danger-zone__icon {
  background: rgba(239, 68, 68, 0.15);
  color: #ef4444;
}

.settings-danger-zone__intro {
  margin: 0 0 0.75rem;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.6;
}

.settings-danger-zone__list {
  margin: 0 0 1rem;
  padding-inline-start: 1.25rem;
  color: var(--text-primary, #e2e8f0);
}

.settings-danger-zone__list--modal {
  text-align: start;
  margin-bottom: 0.75rem;
}

.settings-danger-zone__hint {
  margin: 0 0 1.25rem;
  font-size: 0.9rem;
  color: var(--text-secondary, #94a3b8);
}

.settings-danger-zone__hint--modal {
  margin-bottom: 1rem;
}

.settings-danger-zone__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}
</style>
