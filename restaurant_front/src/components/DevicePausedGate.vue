<template>
  <div v-if="visible" class="device-paused-overlay" role="dialog" aria-modal="true">
    <div class="device-paused-card">
      <div class="device-paused-icon">
        <b-icon icon="pause-circle-fill"></b-icon>
      </div>
      <h2 class="device-paused-title">{{ $t("devicePausedTitle") || "تم إيقاف هذا الجهاز" }}</h2>
      <p class="device-paused-subtitle">
        {{ pauseReason || $t("devicePausedDefaultReason") || "الجهاز متوقف من لوحة الإدارة المركزية. سيستأنف تلقائياً بعد السماح ومزامنة الاتصال." }}
      </p>
      <p v-if="machineId" class="device-paused-machine">
        {{ $t("licenseMachineId") || "معرف الجهاز" }}:
        <code>{{ machineId }}</code>
      </p>
      <p v-if="error" class="device-paused-error">{{ error }}</p>
      <button type="button" class="device-paused-btn" :disabled="busy" @click="retrySync">
        {{ busy ? ($t("pleaseWait") || "جاري التحقق...") : ($t("devicePausedRetry") || "إعادة المزامنة") }}
      </button>
    </div>
  </div>
</template>

<script>
import { HTTP } from "@/http/api.js";
import { registerDevicePausedHandler } from "@/utils/devicePausedGateBus.js";

export default {
  name: "DevicePausedGate",
  data() {
    return {
      visible: false,
      busy: false,
      pauseReason: "",
      machineId: "",
      error: "",
      pollTimer: null,
    };
  },
  mounted() {
    registerDevicePausedHandler((payload) => this.open(payload));
    this.checkStatus();
    window.addEventListener("online", this.onOnline);
  },
  beforeDestroy() {
    registerDevicePausedHandler(null);
    window.removeEventListener("online", this.onOnline);
    this.clearPoll();
  },
  methods: {
    onOnline() {
      this.retrySync();
    },
    open(payload = {}) {
      const device = payload.deviceStatus || payload;
      this.pauseReason = payload.pauseReason || device.pauseReason || "";
      this.machineId = device.machineId || "";
      this.visible = true;
      this.error = "";
      this.startPoll();
    },
    clearPoll() {
      if (this.pollTimer) {
        clearInterval(this.pollTimer);
        this.pollTimer = null;
      }
    },
    startPoll() {
      this.clearPoll();
      this.pollTimer = setInterval(() => this.retrySync(true), 60000);
    },
    async checkStatus() {
      try {
        const { data } = await HTTP.get("License/device-status");
        if (data?.isPaused) this.open({ deviceStatus: data, pauseReason: data.pauseReason });
      } catch {
        /* ignore */
      }
    },
    async retrySync(silent = false) {
      if (this.busy) return;
      this.busy = true;
      if (!silent) this.error = "";
      try {
        const { data } = await HTTP.post("License/device-sync");
        if (data?.isPaused) {
          this.pauseReason = data.pauseReason || this.pauseReason;
          this.machineId = data.machineId || this.machineId;
          this.visible = true;
          if (!silent && data.syncOnline === false) {
            this.error = this.$t("devicePausedOffline") || "لا يوجد اتصال للمزامنة حالياً";
          }
        } else {
          this.visible = false;
          this.clearPoll();
        }
      } catch (e) {
        if (!silent) {
          this.error =
            e?.response?.data?.message ||
            this.$t("devicePausedOffline") ||
            "تعذر التحقق من الحالة";
        }
      } finally {
        this.busy = false;
      }
    },
  },
};
</script>

<style scoped>
.device-paused-overlay {
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
.device-paused-card {
  width: min(420px, 100%);
  background: var(--bg-primary);
  color: var(--text-primary);
  border: none;
  border-radius: 16px;
  padding: 32px 24px 24px;
  text-align: center;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.16);
}
.device-paused-icon {
  width: 52px;
  height: 52px;
  margin: 0 auto 16px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  background: color-mix(in srgb, var(--warning-color) 16%, transparent);
  color: var(--warning-color);
  font-size: 1.45rem;
}
.device-paused-title {
  margin: 0 0 8px;
  font-size: 28px;
  font-weight: 800;
  letter-spacing: -0.03em;
}
.device-paused-subtitle {
  margin: 0 0 16px;
  color: var(--text-secondary);
  font-size: 15px;
  font-weight: 500;
  line-height: 1.6;
}
.device-paused-machine {
  margin: 0 0 16px;
  font-size: 13px;
  color: var(--text-muted);
}
.device-paused-machine code {
  font-size: 0.8rem;
  background: var(--bg-tertiary);
  padding: 0.15rem 0.4rem;
  border-radius: 8px;
}
.device-paused-error {
  color: var(--danger-color);
  margin: 0 0 12px;
  font-size: 0.9rem;
}
.device-paused-btn {
  width: 100%;
  min-height: 48px;
  border: none;
  border-radius: 12px;
  padding: 0.75rem 1rem;
  background: var(--primary-color);
  color: #fff;
  font-weight: 700;
  cursor: pointer;
  box-shadow: none;
}
.device-paused-btn:disabled {
  opacity: 0.6;
  cursor: wait;
}
</style>
