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
    if (!this.isPosRoute() && !this.isPublicMenuRoute()) {
      this.checkStatus();
    }
    window.addEventListener("online", this.onOnline);
  },
  beforeDestroy() {
    registerDevicePausedHandler(null);
    window.removeEventListener("online", this.onOnline);
    this.clearPoll();
  },
  methods: {
    isPosRoute() {
      const path = this.$route?.path || "";
      return path === "/pos" || path.startsWith("/pos/");
    },
    isPublicMenuRoute() {
      const path = this.$route?.path || "";
      return this.$route?.name === "publicMenu" || this.$route?.name === "publicMenuTrack" || path === "/menu" || path.startsWith("/menu/");
    },
    onOnline() {
      if (this.isPosRoute() || this.isPublicMenuRoute()) return;
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
  background: rgba(15, 23, 42, 0.55);
  backdrop-filter: blur(4px);
}
.device-paused-card {
  width: min(440px, 100%);
  background: #fff;
  color: #0f172a;
  border-radius: 16px;
  padding: 1.75rem 1.5rem;
  text-align: center;
  box-shadow: 0 20px 50px rgba(15, 23, 42, 0.25);
}
.device-paused-icon {
  width: 56px;
  height: 56px;
  margin: 0 auto 1rem;
  border-radius: 50%;
  display: grid;
  place-items: center;
  background: #fef3c7;
  color: #a16207;
  font-size: 1.6rem;
}
.device-paused-title {
  margin: 0 0 0.5rem;
  font-size: 1.25rem;
  font-weight: 800;
}
.device-paused-subtitle {
  margin: 0 0 1rem;
  color: #64748b;
  line-height: 1.6;
}
.device-paused-machine {
  margin: 0 0 1rem;
  font-size: 0.85rem;
  color: #64748b;
}
.device-paused-machine code {
  font-size: 0.8rem;
  background: #f1f5f9;
  padding: 0.15rem 0.4rem;
  border-radius: 6px;
}
.device-paused-error {
  color: #b91c1c;
  margin: 0 0 0.75rem;
  font-size: 0.9rem;
}
.device-paused-btn {
  width: 100%;
  border: none;
  border-radius: 10px;
  padding: 0.75rem 1rem;
  background: #002536;
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}
.device-paused-btn:disabled {
  opacity: 0.6;
  cursor: wait;
}
</style>
