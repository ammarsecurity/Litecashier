<template>
  <div v-if="visible" class="license-gate-overlay" role="dialog" aria-modal="true">
    <div class="license-gate-card">
      <button
        v-if="canDismiss"
        type="button"
        class="license-gate-close"
        :aria-label="$t('cancelButtonLabel') || 'إغلاق'"
        :disabled="busy"
        @click="dismiss"
      >
        <b-icon icon="x-lg"></b-icon>
      </button>
      <div class="license-gate-icon">
        <b-icon icon="key-fill"></b-icon>
      </div>
      <h2 class="license-gate-title">
        {{ changeMode ? ($t("licenseChangeTitle") || $t("licenseActivationTitle")) : $t("licenseActivationTitle") }}
      </h2>
      <p class="license-gate-subtitle">{{ statusMessage }}</p>

      <div v-if="status && status.enforcementEnabled" class="license-gate-meta">
        <div v-if="status.code" class="license-gate-current">
          {{ $t("licenseCurrentCode") || "الكود الحالي" }}:
          <code>{{ status.code }}</code>
        </div>
        <div v-if="status.isLifetime && status.isActive">
          {{ $t("licenseLifetime") }}
        </div>
        <div v-else-if="status.daysRemaining != null">
          {{ $t("licenseDaysRemaining", { days: status.daysRemaining }) }}
        </div>
        <div class="license-gate-machine">
          {{ $t("licenseMachineId") }}: <code>{{ status.machineId }}</code>
        </div>
      </div>

      <form class="license-gate-form" @submit.prevent="activate">
        <label class="license-gate-label" for="licenseCodeInput">{{ $t("licenseCodeLabel") }}</label>
        <input
          id="licenseCodeInput"
          v-model="code"
          type="text"
          class="license-gate-input"
          :placeholder="$t('licenseCodePlaceholder')"
          autocomplete="off"
          :disabled="busy"
          required
        />
        <p v-if="error" class="license-gate-error">{{ error }}</p>
        <button type="submit" class="license-gate-submit" :disabled="busy || !code.trim()">
          {{
            busy
              ? ($t("pleaseWait") || "...")
              : changeMode
                ? ($t("licenseChangeActivateButton") || $t("licenseActivateButton"))
                : $t("licenseActivateButton")
          }}
        </button>
      </form>
    </div>
  </div>
</template>

<script>
import { HTTP } from "@/http/api.js";
import { registerLicenseGateHandler } from "@/utils/licenseGateBus.js";

export default {
  name: "LicenseGate",
  data() {
    return {
      visible: false,
      forced: false,
      changeMode: false,
      busy: false,
      code: "",
      error: "",
      status: null,
    };
  },
  computed: {
    canDismiss() {
      return this.changeMode && this.status?.isActive;
    },
    statusMessage() {
      if (!this.status) return this.$t("licenseChecking");
      if (!this.status.enforcementEnabled) return "";
      if (this.changeMode && this.status.isActive) {
        return this.$t("licenseChangeHint") || this.$t("licenseActiveHint");
      }
      if (this.status.isActive) return this.$t("licenseActiveHint");
      if (this.status.message === "expired") return this.$t("licenseExpiredMessage");
      return this.$t("licenseRequiredMessage");
    },
  },
  mounted() {
    registerLicenseGateHandler((payload) => {
      this.forced = true;
      this.changeMode = !!(payload && (payload.allowChange || payload.changeMode));
      if (payload && payload.status) this.status = payload.status;
      this.visible = true;
      this.error = "";
      this.code = "";
      this.refreshStatus({ keepOpenForChange: this.changeMode });
    });
    this.refreshStatus();
  },
  beforeDestroy() {
    registerLicenseGateHandler(null);
  },
  methods: {
    dismiss() {
      if (!this.canDismiss || this.busy) return;
      this.visible = false;
      this.forced = false;
      this.changeMode = false;
      this.error = "";
      this.code = "";
    },
    async refreshStatus({ keepOpenForChange = false } = {}) {
      try {
        const res = await HTTP.get("License/status");
        this.status = res.data;
        if (!this.status?.enforcementEnabled) {
          this.visible = false;
          this.forced = false;
          this.changeMode = false;
          return;
        }
        if (this.status.isActive) {
          if (keepOpenForChange || this.changeMode) {
            this.visible = true;
            return;
          }
          this.forced = false;
          this.visible = false;
          return;
        }
        this.changeMode = false;
        this.visible = true;
      } catch (e) {
        // If license endpoint missing (old server), do not block UI
        if (e?.response?.status === 404) {
          this.visible = false;
          this.changeMode = false;
        }
      }
    },
    async activate() {
      this.busy = true;
      this.error = "";
      try {
        const res = await HTTP.post("License/activate", { code: this.code.trim() });
        this.status = res.data;
        if (this.status?.isActive) {
          this.visible = false;
          this.forced = false;
          this.changeMode = false;
          this.code = "";
          if (this.$notify?.success) {
            this.$notify.success(this.$t("licenseActivatedSuccess"));
          } else if (this.$toast?.success) {
            this.$toast.success(this.$t("licenseActivatedSuccess"));
          }
          // Reload to retry blocked API calls cleanly
          window.location.reload();
        } else {
          this.error = this.$t("licenseActivationFailed");
        }
      } catch (e) {
        const msg = e?.response?.data?.message || "activationFailed";
        this.error = this.$t(msg) !== msg ? this.$t(msg) : this.$t("licenseActivationFailed");
        if (e?.response?.data?.status) this.status = e.response.data.status;
      } finally {
        this.busy = false;
      }
    },
  },
};
</script>

<style scoped>
.license-gate-overlay {
  position: fixed;
  inset: 0;
  z-index: 10050;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.25rem;
  background: color-mix(in srgb, var(--bg-dark, #041015) 72%, transparent);
  backdrop-filter: blur(16px);
}

.license-gate-card {
  position: relative;
  width: min(420px, 100%);
  background: var(--bg-primary);
  border: none;
  border-radius: 16px;
  padding: 32px 24px 24px;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.16);
  color: var(--text-primary);
}

.license-gate-close {
  position: absolute;
  top: 0.75rem;
  inset-inline-end: 0.75rem;
  width: 40px;
  height: 40px;
  border: none;
  border-radius: 12px;
  display: grid;
  place-items: center;
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  cursor: pointer;
}

.license-gate-close:hover {
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--primary-color);
}

.license-gate-icon {
  width: 52px;
  height: 52px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  margin: 0 auto 16px;
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--primary-color);
  font-size: 1.35rem;
}

.license-gate-title {
  margin: 0 0 8px;
  text-align: center;
  font-size: 28px;
  font-weight: 800;
  letter-spacing: -0.03em;
}

.license-gate-subtitle {
  margin: 0 0 16px;
  text-align: center;
  color: var(--text-secondary);
  font-size: 15px;
  font-weight: 500;
}

.license-gate-meta {
  margin-bottom: 16px;
  padding: 12px;
  border-radius: 12px;
  background: var(--bg-tertiary);
  font-size: 13px;
  text-align: center;
}

.license-gate-current {
  margin-bottom: 0.35rem;
}

.license-gate-machine {
  margin-top: 0.35rem;
  word-break: break-all;
  color: var(--text-muted);
}

.license-gate-machine code,
.license-gate-current code {
  font-family: ui-monospace, monospace;
}

.license-gate-label {
  display: block;
  margin-bottom: 8px;
  font-weight: 600;
  font-size: 0.9375rem;
}

.license-gate-input {
  width: 100%;
  min-height: 44px;
  padding: 0.75rem 0.9rem;
  border-radius: 12px;
  border: 1px solid var(--border-light);
  background: var(--bg-tertiary);
  color: inherit;
  font-size: 1rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  box-shadow: none;
}

.license-gate-input:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 16%, transparent);
}

.license-gate-error {
  margin: 0.55rem 0 0;
  color: var(--danger-color, #f87171);
  font-size: 0.88rem;
}

.license-gate-submit {
  width: 100%;
  margin-top: 16px;
  min-height: 48px;
  padding: 0.85rem 1rem;
  border: none;
  border-radius: 12px;
  background: var(--primary-color);
  color: #fff;
  font-weight: 700;
  cursor: pointer;
  box-shadow: none;
}

.license-gate-submit:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
</style>
