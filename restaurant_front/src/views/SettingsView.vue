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
                  <h1 class="users-page-title">{{ $t("settingsTitle") || "الإعدادات" }}</h1>
                  <p class="header-subtitle">
                    {{ $t("settingsSubtitle") || "إدارة إعدادات النظام والترخيص" }}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div
            v-if="licenseStatusLoading || (licenseStatus && licenseStatus.enforcementEnabled)"
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
              <div
                v-if="licenseStatusLoading || licenseConnectivityLoading"
                class="settings-license-zone__intro"
              >
                <b-spinner small></b-spinner>
              </div>
              <template v-else-if="licenseStatus && licenseStatus.enforcementEnabled && !licenseOnline">
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
              <template v-else-if="licenseStatus && licenseStatus.enforcementEnabled">
                <p class="settings-license-zone__intro">
                  {{
                    $t("settingsLicenseHint") ||
                    "إذا حصلت على كود ترخيص جديد يمكنك استبدال الكود الحالي من هنا."
                  }}
                </p>
                <div class="settings-license-meta">
                  <div class="settings-license-meta__row">
                    <span>{{ $t("licenseCurrentCode") || "الكود الحالي" }}</span>
                    <strong><code>{{ licenseStatus.code || "—" }}</code></strong>
                  </div>
                  <div class="settings-license-meta__row">
                    <span>{{ $t("status") || "الحالة" }}</span>
                    <strong>
                      {{
                        licenseStatus.isActive
                          ? $t("licenseActiveHint") || "نشط"
                          : $t("licenseExpiredMessage") || "غير نشط"
                      }}
                    </strong>
                  </div>
                  <div
                    v-if="licenseStatus.isLifetime && licenseStatus.isActive"
                    class="settings-license-meta__row"
                  >
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
                <div class="settings-license-actions">
                  <button
                    type="button"
                    class="users-add-button"
                    :disabled="!licenseOnline"
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

          <div
            v-else-if="!licenseStatusLoading"
            class="app-section-card settings-license-zone"
          >
            <div class="app-section-body">
              <p class="settings-license-zone__intro">
                {{ $t("licenseEnforcementDisabled") || "التحقق من الترخيص غير مفعّل على هذا الجهاز." }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";
import { openLicenseGate } from "@/utils/licenseGateBus.js";

export default {
  name: "SettingsView",
  components: { AppHeader },
  data() {
    return {
      licenseStatus: null,
      licenseStatusLoading: false,
      licenseOnline: false,
      licenseConnectivityLoading: false,
    };
  },
  mounted() {
    this.loadLicenseStatus();
    this.checkLicenseConnectivity();
    window.addEventListener("online", this.onBrowserOnline);
    window.addEventListener("offline", this.onBrowserOffline);
  },
  beforeDestroy() {
    window.removeEventListener("online", this.onBrowserOnline);
    window.removeEventListener("offline", this.onBrowserOffline);
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

.settings-license-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}
</style>
