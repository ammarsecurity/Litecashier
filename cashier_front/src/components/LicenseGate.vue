<template>
  <div v-if="visible" class="license-gate-overlay" role="dialog" aria-modal="true">
    <div class="license-gate-card">
      <div class="license-gate-icon">
        <b-icon icon="key-fill"></b-icon>
      </div>
      <h2 class="license-gate-title">{{ $t("licenseActivationTitle") }}</h2>
      <p class="license-gate-subtitle">{{ statusMessage }}</p>

      <div v-if="status && status.enforcementEnabled" class="license-gate-meta">
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
          {{ busy ? ($t("pleaseWait") || "...") : $t("licenseActivateButton") }}
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
      busy: false,
      code: "",
      error: "",
      status: null,
    };
  },
  computed: {
    statusMessage() {
      if (!this.status) return this.$t("licenseChecking");
      if (!this.status.enforcementEnabled) return "";
      if (this.status.isActive) return this.$t("licenseActiveHint");
      if (this.status.message === "expired") return this.$t("licenseExpiredMessage");
      return this.$t("licenseRequiredMessage");
    },
  },
  mounted() {
    registerLicenseGateHandler((payload) => {
      this.forced = true;
      if (payload && payload.status) this.status = payload.status;
      this.visible = true;
      this.error = "";
      this.refreshStatus();
    });
    this.refreshStatus();
  },
  beforeDestroy() {
    registerLicenseGateHandler(null);
  },
  methods: {
    async refreshStatus() {
      try {
        const res = await HTTP.get("License/status");
        this.status = res.data;
        if (!this.status?.enforcementEnabled) {
          this.visible = false;
          this.forced = false;
          return;
        }
        if (this.status.isActive) {
          this.forced = false;
          this.visible = false;
          return;
        }
        this.visible = true;
      } catch (e) {
        // If license endpoint missing (old server), do not block UI
        if (e?.response?.status === 404) {
          this.visible = false;
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
  background: rgba(0, 20, 30, 0.72);
  backdrop-filter: blur(6px);
}

.license-gate-card {
  width: min(440px, 100%);
  background: var(--bg-secondary, #0f2430);
  border: 1px solid color-mix(in srgb, var(--primary-color, #3db4d0) 35%, transparent);
  border-radius: 1rem;
  padding: 1.75rem 1.5rem;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.35);
  color: var(--text-primary, #fff);
}

.license-gate-icon {
  width: 3rem;
  height: 3rem;
  border-radius: 0.85rem;
  display: grid;
  place-items: center;
  margin: 0 auto 1rem;
  background: color-mix(in srgb, var(--primary-color, #3db4d0) 18%, transparent);
  color: var(--primary-color, #3db4d0);
  font-size: 1.35rem;
}

.license-gate-title {
  margin: 0 0 0.4rem;
  text-align: center;
  font-size: 1.35rem;
  font-weight: 700;
}

.license-gate-subtitle {
  margin: 0 0 1rem;
  text-align: center;
  color: var(--text-secondary, #94a3b8);
  font-size: 0.95rem;
}

.license-gate-meta {
  margin-bottom: 1rem;
  padding: 0.75rem;
  border-radius: 0.65rem;
  background: color-mix(in srgb, var(--primary-color, #3db4d0) 8%, transparent);
  font-size: 0.85rem;
  text-align: center;
}

.license-gate-machine {
  margin-top: 0.35rem;
  word-break: break-all;
  opacity: 0.85;
}

.license-gate-machine code {
  font-family: ui-monospace, monospace;
}

.license-gate-label {
  display: block;
  margin-bottom: 0.4rem;
  font-weight: 600;
  font-size: 0.9rem;
}

.license-gate-input {
  width: 100%;
  padding: 0.75rem 0.9rem;
  border-radius: 0.65rem;
  border: 1px solid var(--border-color, #334155);
  background: var(--bg-primary, #0b1a22);
  color: inherit;
  font-size: 1rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.license-gate-input:focus {
  outline: none;
  border-color: var(--primary-color, #3db4d0);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color, #3db4d0) 22%, transparent);
}

.license-gate-error {
  margin: 0.55rem 0 0;
  color: var(--danger-color, #f87171);
  font-size: 0.88rem;
}

.license-gate-submit {
  width: 100%;
  margin-top: 1rem;
  padding: 0.85rem 1rem;
  border: none;
  border-radius: 0.65rem;
  background: var(--primary-color, #3db4d0);
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}

.license-gate-submit:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
</style>
