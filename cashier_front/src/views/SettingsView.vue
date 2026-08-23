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

          <div
            v-if="licenseStatus && licenseStatus.enforcementEnabled"
            class="app-section-card settings-license-zone"
          >
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap settings-license-zone__icon">
                  <b-icon icon="key-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("settingsLicenseTitle") || "الترخيص" }}</h3>
                  <p class="app-section-subtitle">
                    {{ $t("settingsLicenseSubtitle") || "عرض حالة الترخيص واستبدال كود التفعيل" }}
                  </p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <div v-if="licenseConnectivityLoading" class="settings-license-zone__intro">
                <b-spinner small></b-spinner>
              </div>
              <template v-else-if="!licenseOnline">
                <div class="settings-license-offline">
                  <b-icon icon="wifi-off" class="settings-license-offline__icon"></b-icon>
                  <p class="settings-license-offline__title">
                    {{ $t("settingsLicenseOfflineTitle") || "اتصل بالإنترنت أولاً" }}
                  </p>
                  <p class="settings-license-offline__text">
                    {{
                      $t("settingsLicenseOfflineMessage") ||
                      "لتغيير كود الترخيص أو عرض حالة التفعيل يلزم اتصال بالإنترنت."
                    }}
                  </p>
                  <button
                    type="button"
                    class="users-add-button"
                    :disabled="licenseConnectivityLoading"
                    @click="checkLicenseConnectivity"
                  >
                    <b-icon icon="arrow-clockwise" class="button-icon"></b-icon>
                    <span class="button-text">{{ $t("retry") || "إعادة المحاولة" }}</span>
                  </button>
                </div>
              </template>
              <template v-else>
                <p class="settings-license-zone__intro">
                  {{ $t("settingsLicenseHint") || "إذا حصلت على كود ترخيص جديد يمكنك استبدال الكود الحالي من هنا." }}
                </p>
                <div class="settings-license-meta" v-if="!licenseStatusLoading">
                  <div class="settings-license-meta__row">
                    <span>{{ $t("licenseCurrentCode") || "الكود الحالي" }}</span>
                    <strong><code>{{ licenseStatus.code || "—" }}</code></strong>
                  </div>
                  <div class="settings-license-meta__row">
                    <span>{{ $t("status") || "الحالة" }}</span>
                    <strong>
                      {{
                        licenseStatus.isActive
                          ? ($t("licenseActiveHint") || "نشط")
                          : ($t("licenseExpiredMessage") || "غير نشط")
                      }}
                    </strong>
                  </div>
                  <div v-if="licenseStatus.isLifetime && licenseStatus.isActive" class="settings-license-meta__row">
                    <span>{{ $t("licenseLifetime") }}</span>
                  </div>
                  <div
                    v-else-if="licenseStatus.daysRemaining != null"
                    class="settings-license-meta__row"
                  >
                    <span>{{ $t("licenseDaysRemaining", { days: licenseStatus.daysRemaining }) }}</span>
                  </div>
                  <div class="settings-license-meta__row">
                    <span>{{ $t("licenseMachineId") }}</span>
                    <strong><code>{{ licenseStatus.machineId }}</code></strong>
                  </div>
                </div>
                <div v-else class="settings-license-zone__intro">
                  <b-spinner small></b-spinner>
                </div>
                <div class="settings-danger-zone__actions">
                  <button
                    type="button"
                    class="users-add-button"
                    :disabled="licenseStatusLoading || !licenseOnline"
                    @click="openChangeLicense"
                  >
                    <b-icon icon="arrow-repeat" class="button-icon"></b-icon>
                    <span class="button-text">
                      {{ $t("settingsLicenseChangeButton") || "تغيير كود الترخيص" }}
                    </span>
                  </button>
                </div>
              </template>
            </div>
          </div>

          <div class="app-section-card settings-print-zone">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap settings-print-zone__icon">
                  <b-icon icon="printer-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("settingsPrintTitle") }}</h3>
                  <p class="app-section-subtitle">{{ $t("settingsPrintSubtitle") }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <p class="settings-print-zone__intro">{{ $t("settingsPrintHint") }}</p>
              <div class="settings-print-options" role="radiogroup" :aria-label="$t('settingsPrintTitle')">
                <label
                  class="settings-print-option"
                  :class="{ 'settings-print-option--active': printInvoiceFormat === 'Pos' }"
                >
                  <input
                    v-model="printInvoiceFormat"
                    type="radio"
                    value="Pos"
                    class="settings-print-option__input"
                    :disabled="printSettingsLoading || printSettingsSaving"
                  />
                  <span class="settings-print-option__body">
                    <strong>{{ $t("printFormatPos") }}</strong>
                    <span>{{ $t("printFormatPosHint") }}</span>
                  </span>
                </label>
                <label
                  class="settings-print-option"
                  :class="{ 'settings-print-option--active': printInvoiceFormat === 'A4' }"
                >
                  <input
                    v-model="printInvoiceFormat"
                    type="radio"
                    value="A4"
                    class="settings-print-option__input"
                    :disabled="printSettingsLoading || printSettingsSaving"
                  />
                  <span class="settings-print-option__body">
                    <strong>{{ $t("printFormatA4") }}</strong>
                    <span>{{ $t("printFormatA4Hint") }}</span>
                  </span>
                </label>
              </div>
              <div class="settings-danger-zone__actions">
                <button
                  type="button"
                  class="users-add-button"
                  :disabled="printSettingsLoading || printSettingsSaving || !printFormatDirty"
                  @click="savePrintSettings"
                >
                  <b-spinner small v-if="printSettingsSaving" class="button-icon"></b-spinner>
                  <b-icon v-else icon="check2-circle" class="button-icon"></b-icon>
                  <span class="button-text">
                    {{
                      printSettingsSaving
                        ? $t("settingsPrintSaving")
                        : $t("settingsPrintSave")
                    }}
                  </span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-section-card settings-branding-zone">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap settings-branding-zone__icon">
                  <b-icon icon="palette-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("settingsPosBrandingTitle") }}</h3>
                  <p class="app-section-subtitle">{{ $t("settingsPosBrandingSubtitle") }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <p class="settings-branding-zone__intro">{{ $t("settingsPosBrandingHint") }}</p>
              <div class="settings-branding-grid">
                <div class="settings-branding-card">
                  <div class="settings-branding-card__head">
                    <strong>{{ $t("settingsCartWatermarkTitle") }}</strong>
                    <span>{{ $t("settingsCartWatermarkHint") }}</span>
                  </div>
                  <div
                    class="settings-watermark-preview"
                    :style="{ '--pos-cart-watermark-opacity': String(cartWatermarkOpacity / 100) }"
                  >
                    <img
                      v-if="watermarkPreviewSrc"
                      :src="watermarkPreviewSrc"
                      alt=""
                    />
                    <span v-else class="settings-branding-empty">
                      {{ $t("settingsCartWatermarkEmpty") }}
                    </span>
                  </div>
                  <label class="settings-opacity-field">
                    <span>
                      {{ $t("settingsCartWatermarkOpacity") }}
                      <strong>{{ cartWatermarkOpacity }}%</strong>
                    </span>
                    <input
                      v-model.number="cartWatermarkOpacity"
                      type="range"
                      min="20"
                      max="100"
                      step="1"
                      :disabled="brandingLoading || brandingSaving"
                    />
                  </label>
                  <div class="settings-branding-actions">
                    <button
                      type="button"
                      class="logo-upload-btn"
                      :disabled="brandingLoading || brandingSaving"
                      @click="$refs.watermarkInput.click()"
                    >
                      <b-icon icon="cloud-upload-fill" class="me-2"></b-icon>
                      {{ $t("settingsCartWatermarkUpload") }}
                    </button>
                    <button
                      v-if="watermarkPreviewSrc"
                      type="button"
                      class="settings-branding-clear"
                      :disabled="brandingLoading || brandingSaving"
                      @click="clearWatermark"
                    >
                      <b-icon icon="trash"></b-icon>
                      {{ $t("settingsCartWatermarkRemove") }}
                    </button>
                  </div>
                  <input
                    ref="watermarkInput"
                    type="file"
                    accept="image/png,image/jpeg,image/gif"
                    hidden
                    @change="onWatermarkFileChange"
                  />
                </div>

                <div class="settings-branding-card">
                  <div class="settings-branding-card__head">
                    <strong>{{ $t("settingsDefaultProductImageTitle") }}</strong>
                    <span>{{ $t("settingsDefaultProductImageHint") }}</span>
                  </div>
                  <div class="settings-product-preview">
                    <img :src="defaultProductPreviewSrc" alt="" />
                  </div>
                  <div class="settings-branding-actions">
                    <button
                      type="button"
                      class="logo-upload-btn"
                      :disabled="brandingLoading || brandingSaving"
                      @click="$refs.defaultProductInput.click()"
                    >
                      <b-icon icon="cloud-upload-fill" class="me-2"></b-icon>
                      {{ $t("settingsDefaultProductImageUpload") }}
                    </button>
                    <button
                      v-if="hasCustomDefaultProduct"
                      type="button"
                      class="settings-branding-clear"
                      :disabled="brandingLoading || brandingSaving"
                      @click="clearDefaultProduct"
                    >
                      <b-icon icon="arrow-counterclockwise"></b-icon>
                      {{ $t("settingsDefaultProductImageReset") }}
                    </button>
                  </div>
                  <input
                    ref="defaultProductInput"
                    type="file"
                    accept="image/png,image/jpeg,image/gif"
                    hidden
                    @change="onDefaultProductFileChange"
                  />
                </div>
              </div>
              <div class="settings-danger-zone__actions">
                <button
                  type="button"
                  class="users-add-button"
                  :disabled="brandingLoading || brandingSaving || !brandingDirty"
                  @click="savePosBranding"
                >
                  <b-spinner small v-if="brandingSaving" class="button-icon"></b-spinner>
                  <b-icon v-else icon="check2-circle" class="button-icon"></b-icon>
                  <span class="button-text">
                    {{
                      brandingSaving
                        ? $t("settingsPosBrandingSaving")
                        : $t("settingsPosBrandingSave")
                    }}
                  </span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-section-card settings-backup-zone">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap settings-backup-zone__icon">
                  <b-icon icon="cloud-download-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("settingsBackupTitle") }}</h3>
                  <p class="app-section-subtitle">{{ $t("settingsBackupSubtitle") }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <p class="settings-backup-zone__intro">{{ $t("settingsBackupHint") }}</p>
              <div class="settings-danger-zone__actions">
                <button
                  type="button"
                  class="users-add-button"
                  :disabled="backupLoading"
                  @click="downloadDatabaseBackup"
                >
                  <b-spinner small v-if="backupLoading" class="button-icon"></b-spinner>
                  <b-icon v-else icon="download" class="button-icon"></b-icon>
                  <span class="button-text">
                    {{
                      backupLoading
                        ? $t("settingsBackupDownloading")
                        : $t("settingsBackupDownload")
                    }}
                  </span>
                </button>
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
import { openLicenseGate } from "@/utils/licenseGateBus.js";
import { applyCommercialBranding, clampWatermarkOpacity } from "@/utils/posBranding.js";
import { BUILTIN_DEFAULT_PRODUCT_IMAGE } from "@/utils/productImage.js";

export default {
  name: "SettingsView",
  components: { AppHeader },
  data() {
    return {
      clearCatalogPassword: "",
      clearCatalogLoading: false,
      clearCatalogResult: null,
      backupLoading: false,
      printInvoiceFormat: "Pos",
      savedPrintInvoiceFormat: "Pos",
      printSettingsLoading: false,
      printSettingsSaving: false,
      brandingLoading: false,
      brandingSaving: false,
      cartWatermarkOpacity: 18,
      savedCartWatermarkOpacity: 18,
      savedWatermarkLogo: null,
      savedDefaultProductImage: null,
      watermarkFile: null,
      watermarkPreview: null,
      defaultProductFile: null,
      defaultProductPreview: null,
      clearWatermarkPending: false,
      clearDefaultProductPending: false,
      licenseStatus: null,
      licenseStatusLoading: false,
      licenseOnline: false,
      licenseConnectivityLoading: false,
    };
  },
  computed: {
    printFormatDirty() {
      return this.printInvoiceFormat !== this.savedPrintInvoiceFormat;
    },
    watermarkPreviewSrc() {
      if (this.clearWatermarkPending) return null;
      return this.watermarkPreview || this.savedWatermarkLogo;
    },
    defaultProductPreviewSrc() {
      if (this.clearDefaultProductPending) return BUILTIN_DEFAULT_PRODUCT_IMAGE;
      return this.defaultProductPreview || this.savedDefaultProductImage || BUILTIN_DEFAULT_PRODUCT_IMAGE;
    },
    hasCustomDefaultProduct() {
      return !!(this.defaultProductFile || (!this.clearDefaultProductPending && this.savedDefaultProductImage));
    },
    brandingDirty() {
      return (
        !!this.watermarkFile ||
        !!this.defaultProductFile ||
        this.clearWatermarkPending ||
        this.clearDefaultProductPending ||
        clampWatermarkOpacity(this.cartWatermarkOpacity) !== this.savedCartWatermarkOpacity
      );
    },
  },
  mounted() {
    this.loadPrintSettings();
    this.loadPosBranding();
    this.loadLicenseStatus();
    this.checkLicenseConnectivity();
    window.addEventListener("online", this.onBrowserOnline);
    window.addEventListener("offline", this.onBrowserOffline);
  },
  beforeDestroy() {
    window.removeEventListener("online", this.onBrowserOnline);
    window.removeEventListener("offline", this.onBrowserOffline);
    this.revokeBrandingPreviews();
  },
  methods: {
    onBrowserOnline() {
      this.checkLicenseConnectivity();
    },
    onBrowserOffline() {
      this.licenseOnline = false;
      this.licenseConnectivityLoading = false;
    },
    async checkLicenseConnectivity() {
      if (typeof navigator !== "undefined" && navigator.onLine === false) {
        this.licenseOnline = false;
        this.licenseConnectivityLoading = false;
        return;
      }
      this.licenseConnectivityLoading = true;
      try {
        const res = await HTTP.get("License/connectivity", { timeout: 12000 });
        const data = res.data || {};
        this.licenseOnline = !!(data.online ?? data.Online);
        // Browser reports online but probe failed: still allow UI (activate shows server errors).
        if (!this.licenseOnline && typeof navigator !== "undefined" && navigator.onLine) {
          this.licenseOnline = true;
        }
      } catch (_) {
        this.licenseOnline =
          typeof navigator === "undefined" ? true : navigator.onLine !== false;
      } finally {
        this.licenseConnectivityLoading = false;
      }
    },
    async loadLicenseStatus() {
      this.licenseStatusLoading = true;
      try {
        const res = await HTTP.get("License/status");
        this.licenseStatus = res.data || null;
      } catch (_) {
        this.licenseStatus = null;
      } finally {
        this.licenseStatusLoading = false;
      }
    },
    openChangeLicense() {
      if (!this.licenseOnline) {
        this.checkLicenseConnectivity();
        return;
      }
      openLicenseGate({ allowChange: true, status: this.licenseStatus });
    },
    async loadPrintSettings() {
      this.printSettingsLoading = true;
      try {
        const response = await HTTP.get("Admin/CommercialUserInfo");
        const d = response?.data?.data;
        if (d) {
          const format =
            String(d.printInvoiceFormat || d.PrintInvoiceFormat || "Pos").toUpperCase() ===
            "A4"
              ? "A4"
              : "Pos";
          this.printInvoiceFormat = format;
          this.savedPrintInvoiceFormat = format;
          localStorage.setItem("printInvoiceFormat", format);
        }
      } catch (error) {
        console.error("Error loading print settings:", error);
        const cached = localStorage.getItem("printInvoiceFormat") === "A4" ? "A4" : "Pos";
        this.printInvoiceFormat = cached;
        this.savedPrintInvoiceFormat = cached;
      } finally {
        this.printSettingsLoading = false;
      }
    },
    async savePrintSettings() {
      if (this.printSettingsSaving || !this.printFormatDirty) return;
      this.printSettingsSaving = true;
      try {
        const response = await HTTP.post("Admin/UpdatePrintSettings", {
          printInvoiceFormat: this.printInvoiceFormat,
        });
        if (response?.data?.errorStatus) {
          throw new Error(response.data.message || "saveFailed");
        }
        const d = response?.data?.data;
        const format =
          String(d?.printInvoiceFormat || d?.PrintInvoiceFormat || this.printInvoiceFormat)
            .toUpperCase() === "A4"
            ? "A4"
            : "Pos";
        this.printInvoiceFormat = format;
        this.savedPrintInvoiceFormat = format;
        localStorage.setItem("printInvoiceFormat", format);
        this.$notify.success(this.$t("settingsPrintSaveSuccess"), {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } catch (error) {
        const msg = error?.response?.data?.message || error?.message;
        this.$notify.error(
          msg && this.$te(msg) ? this.$t(msg) : this.$t("settingsPrintSaveFailed"),
          { position: "top-right", timeout: 4000, maxToasts: 1 }
        );
      } finally {
        this.printSettingsSaving = false;
      }
    },
    applyBrandingPayload(d) {
      const branding = applyCommercialBranding(d);
      this.savedWatermarkLogo = branding.cartWatermarkLogo;
      this.savedDefaultProductImage = branding.defaultProductImage;
      this.cartWatermarkOpacity = branding.cartWatermarkOpacity;
      this.savedCartWatermarkOpacity = branding.cartWatermarkOpacity;
      this.clearWatermarkPending = false;
      this.clearDefaultProductPending = false;
      this.revokeBrandingPreviews();
      this.watermarkFile = null;
      this.watermarkPreview = null;
      this.defaultProductFile = null;
      this.defaultProductPreview = null;
    },
    revokeBrandingPreviews() {
      if (this.watermarkPreview) URL.revokeObjectURL(this.watermarkPreview);
      if (this.defaultProductPreview) URL.revokeObjectURL(this.defaultProductPreview);
    },
    async loadPosBranding() {
      this.brandingLoading = true;
      try {
        const response = await HTTP.get("Admin/CommercialUserInfo");
        this.applyBrandingPayload(response?.data?.data);
      } catch (error) {
        console.error("Error loading POS branding:", error);
      } finally {
        this.brandingLoading = false;
      }
    },
    onWatermarkFileChange(event) {
      const file = event?.target?.files?.[0];
      if (this.$refs.watermarkInput) this.$refs.watermarkInput.value = "";
      if (!file) return;
      if (this.watermarkPreview) URL.revokeObjectURL(this.watermarkPreview);
      this.watermarkFile = file;
      this.watermarkPreview = URL.createObjectURL(file);
      this.clearWatermarkPending = false;
    },
    clearWatermark() {
      if (this.watermarkPreview) URL.revokeObjectURL(this.watermarkPreview);
      this.watermarkFile = null;
      this.watermarkPreview = null;
      this.clearWatermarkPending = !!this.savedWatermarkLogo;
    },
    onDefaultProductFileChange(event) {
      const file = event?.target?.files?.[0];
      if (this.$refs.defaultProductInput) this.$refs.defaultProductInput.value = "";
      if (!file) return;
      if (this.defaultProductPreview) URL.revokeObjectURL(this.defaultProductPreview);
      this.defaultProductFile = file;
      this.defaultProductPreview = URL.createObjectURL(file);
      this.clearDefaultProductPending = false;
    },
    clearDefaultProduct() {
      if (this.defaultProductPreview) URL.revokeObjectURL(this.defaultProductPreview);
      this.defaultProductFile = null;
      this.defaultProductPreview = null;
      this.clearDefaultProductPending = !!this.savedDefaultProductImage;
    },
    async savePosBranding() {
      if (this.brandingSaving || !this.brandingDirty) return;
      this.brandingSaving = true;
      try {
        const formData = new FormData();
        formData.append("CartWatermarkOpacity", String(clampWatermarkOpacity(this.cartWatermarkOpacity)));
        if (this.watermarkFile) formData.append("CartWatermarkLogo", this.watermarkFile);
        if (this.clearWatermarkPending) formData.append("ClearCartWatermark", "true");
        if (this.defaultProductFile) formData.append("DefaultProductImage", this.defaultProductFile);
        if (this.clearDefaultProductPending) formData.append("ClearDefaultProductImage", "true");

        const response = await HTTP.post("Admin/UpdatePosBranding", formData);
        if (response?.data?.errorStatus) {
          throw new Error(response.data.message || "saveFailed");
        }
        this.applyBrandingPayload(response?.data?.data);
        this.$notify.success(this.$t("settingsPosBrandingSaveSuccess"), {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } catch (error) {
        const msg = error?.response?.data?.message || error?.message;
        this.$notify.error(
          msg && this.$te(msg) ? this.$t(msg) : this.$t("settingsPosBrandingSaveFailed"),
          { position: "top-right", timeout: 4000, maxToasts: 1 }
        );
      } finally {
        this.brandingSaving = false;
      }
    },
    async downloadDatabaseBackup() {
      if (this.backupLoading) return;
      this.backupLoading = true;
      try {
        const response = await HTTP.get("Admin/DownloadDatabaseBackup", {
          responseType: "blob",
          timeout: 300000,
        });

        const contentType = response.headers["content-type"] || "";
        if (contentType.includes("application/json")) {
          const text = await response.data.text();
          const payload = JSON.parse(text);
          const msg = payload?.message;
          throw new Error(msg || "backupFailed");
        }

        const disposition = response.headers["content-disposition"] || "";
        const matchedName = /filename\*?=(?:UTF-8'')?["']?([^"';]+)/i.exec(disposition);
        const fallbackName = `litecashier-backup-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, "-")}.sql`;
        const fileName = decodeURIComponent(
          (matchedName?.[1] || fallbackName).replace(/"/g, "")
        );

        const blob = new Blob([response.data], { type: "application/sql" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);

        this.$notify.success(this.$t("settingsBackupSuccess"), {
          position: "top-right",
          timeout: 4000,
          maxToasts: 1,
        });
      } catch (error) {
        let msg = error?.message;
        if (error?.response?.data instanceof Blob) {
          try {
            const text = await error.response.data.text();
            const payload = JSON.parse(text);
            msg = payload?.message || msg;
          } catch (_) {
            /* ignore */
          }
        } else {
          msg = error?.response?.data?.message || msg;
        }
        const text =
          msg && this.$te(msg) ? this.$t(msg) : this.$t("settingsBackupFailed");
        this.$notify.error(text, {
          position: "top-right",
          timeout: 4500,
          maxToasts: 1,
        });
      } finally {
        this.backupLoading = false;
      }
    },
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
.settings-license-zone {
  margin-bottom: 1.25rem;
}

.settings-license-zone__icon {
  background: rgba(245, 158, 11, 0.18);
  color: #f59e0b;
}

.settings-license-zone__intro {
  margin: 0 0 1rem;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.6;
}

.settings-license-offline {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.5rem;
  padding: 1rem 0.5rem 0.25rem;
}

.settings-license-offline__icon {
  font-size: 1.75rem;
  color: #f59e0b;
  margin-bottom: 0.25rem;
}

.settings-license-offline__title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text-primary, #e2e8f0);
}

.settings-license-offline__text {
  margin: 0 0 0.75rem;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.55;
  max-width: 36rem;
}

.settings-license-meta {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  margin-bottom: 1.25rem;
  padding: 0.9rem 1rem;
  border-radius: 12px;
  border: 1px solid rgba(148, 163, 184, 0.28);
  background: rgba(148, 163, 184, 0.06);
}

.settings-license-meta__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  color: var(--text-secondary, #94a3b8);
  font-size: 0.9rem;
}

.settings-license-meta__row strong {
  color: var(--text-primary, #e2e8f0);
  font-weight: 700;
}

.settings-license-meta__row code {
  font-family: ui-monospace, monospace;
  word-break: break-all;
}

.settings-print-zone {
  margin-bottom: 1.25rem;
}

.settings-print-zone__icon {
  background: rgba(15, 110, 110, 0.15);
  color: #0f6e6e;
}

.settings-print-zone__intro {
  margin: 0 0 1rem;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.6;
}

.settings-print-options {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 0.75rem;
  margin-bottom: 1.25rem;
}

.settings-print-option {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.9rem 1rem;
  border: 1px solid var(--border-color, rgba(148, 163, 184, 0.35));
  border-radius: 12px;
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
  background: rgba(148, 163, 184, 0.06);
}

.settings-print-option--active {
  border-color: #0f6e6e;
  background: rgba(15, 110, 110, 0.1);
}

.settings-print-option__input {
  margin-top: 0.2rem;
  accent-color: #0f6e6e;
}

.settings-print-option__body {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.settings-print-option__body strong {
  color: var(--text-primary, #e2e8f0);
  font-size: 0.98rem;
}

.settings-print-option__body span {
  color: var(--text-secondary, #94a3b8);
  font-size: 0.85rem;
  line-height: 1.45;
}

.settings-branding-zone {
  margin-bottom: 1.25rem;
}

.settings-branding-zone__icon {
  background: rgba(14, 116, 144, 0.16);
  color: #0e7490;
}

.settings-branding-zone__intro {
  margin: 0 0 1.25rem;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.6;
}

.settings-branding-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
  margin-bottom: 1.25rem;
}

.settings-branding-card {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  padding: 1rem;
  border-radius: 1rem;
  background: rgba(15, 23, 42, 0.28);
  border: 1px solid rgba(148, 163, 184, 0.12);
}

.settings-branding-card__head {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.settings-branding-card__head strong {
  color: var(--text-primary, #e2e8f0);
  font-size: 0.98rem;
}

.settings-branding-card__head span {
  color: var(--text-secondary, #94a3b8);
  font-size: 0.85rem;
  line-height: 1.45;
}

.settings-watermark-preview,
.settings-product-preview {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 220px;
  border-radius: 0.85rem;
  overflow: hidden;
}

.settings-watermark-preview {
  background:
    linear-gradient(180deg, #f8fafb 0%, #eef2f5 100%);
}

.settings-watermark-preview img {
  max-width: 78%;
  max-height: 180px;
  object-fit: contain;
  opacity: var(--pos-cart-watermark-opacity, 0.18);
}

.settings-product-preview {
  background: #fff;
}

.settings-product-preview img {
  width: 200px;
  height: 140px;
  object-fit: contain;
}

.settings-branding-empty {
  color: #64748b;
  font-size: 0.85rem;
}

.settings-opacity-field {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  color: var(--text-secondary, #94a3b8);
  font-size: 0.88rem;
}

.settings-opacity-field strong {
  color: var(--text-primary, #e2e8f0);
  margin-inline-start: 0.35rem;
}

.settings-opacity-field input[type="range"] {
  width: 100%;
  accent-color: #0e7490;
}

.settings-branding-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
  align-items: center;
}

.settings-branding-clear {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  border: none;
  background: transparent;
  color: #f87171;
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
  padding: 0.35rem 0;
}

.settings-branding-clear:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

@media (max-width: 900px) {
  .settings-branding-grid {
    grid-template-columns: 1fr;
  }
}

.settings-backup-zone {
  margin-bottom: 1.25rem;
}

.settings-backup-zone__icon {
  background: rgba(59, 130, 246, 0.15);
  color: #3b82f6;
}

.settings-backup-zone__intro {
  margin: 0 0 1.25rem;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.6;
}

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
