<template>
  <div v-if="visible && alert" class="po-alert" role="alertdialog" aria-live="assertive" aria-modal="true">
    <div class="po-alert-card">
      <div class="po-alert-icon">
        <b-icon icon="bell-fill"></b-icon>
      </div>
      <div class="po-alert-copy">
        <p class="po-alert-kicker">{{ $t("newPublicOrderAlert") || "طلب جديد من المنيو" }}</p>
        <h2 class="po-alert-title">{{ alert.orderCode || "—" }}</h2>
        <p class="po-alert-meta">
          {{ alert.customerName || ($t("customerName") || "الزبون") }}
          <span v-if="alert.customerPhone"> · <b dir="ltr">{{ alert.customerPhone }}</b></span>
        </p>
        <p class="po-alert-hint">{{ $t("newPublicOrderAlertHint") || "راجع الطلب ووافق عليه ليصبح فاتورة." }}</p>
      </div>
      <div class="po-alert-actions">
        <button type="button" class="users-form-submit-button" @click="openOrders">
          {{ $t("viewPublicOrder") || "عرض الطلب" }}
        </button>
        <button type="button" class="users-form-cancel-button" @click="dismiss">
          {{ $t("close") || "إغلاق" }}
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import signalRService from "@/services/signalr.js";
import publicOrderAlertSound from "@/utils/publicOrderAlertSound.js";
import { resolveCommercialUserId } from "@/utils/publicMenu.js";

const TITLE_FLASH_MS = 900;

export default {
  name: "PublicOrderIncomingAlert",
  data() {
    return {
      visible: false,
      alert: null,
      originalTitle: "",
      titleTimer: null,
      titleOn: true,
      bound: false,
    };
  },
  watch: {
    "$route"() {
      this.ensureBound();
    },
  },
  mounted() {
    this.ensureBound();
  },
  beforeDestroy() {
    this.unbindRealtime();
    this.stopAttention();
  },
  methods: {
    ensureBound() {
      if (this.bound || !localStorage.getItem("token")) return;
      this.bound = true;
      publicOrderAlertSound.unlock();
      this.bindRealtime();
      if (typeof Notification !== "undefined" && Notification.permission === "default") {
        document.addEventListener("click", this.requestNotifyPermission, { once: true, capture: true });
      }
    },
    requestNotifyPermission() {
      if (typeof Notification === "undefined") return;
      Notification.requestPermission().catch(() => {});
    },
    belongsToStore(payload) {
      const id = Number(payload?.commercialUserId ?? payload?.CommercialUserId);
      const mine = resolveCommercialUserId();
      if (!id || !mine) return true;
      return id === Number(mine);
    },
    onAdded(payload) {
      if (!this.belongsToStore(payload)) return;
      this.alert = {
        orderId: payload?.orderId ?? payload?.OrderId,
        orderCode: payload?.orderCode ?? payload?.OrderCode,
        customerName: payload?.customerName ?? payload?.CustomerName,
        customerPhone: payload?.customerPhone ?? payload?.CustomerPhone,
      };
      this.visible = true;
      this.startAttention();
    },
    startAttention() {
      publicOrderAlertSound.unlock();
      publicOrderAlertSound.startLoop();
      if (navigator.vibrate) {
        navigator.vibrate([220, 80, 220, 80, 420]);
      }
      this.flashTitle();
      this.showDesktopNotification();
    },
    stopAttention() {
      publicOrderAlertSound.stopLoop();
      if (this.titleTimer) {
        clearInterval(this.titleTimer);
        this.titleTimer = null;
      }
      if (this.originalTitle) {
        document.title = this.originalTitle;
        this.originalTitle = "";
      }
    },
    flashTitle() {
      if (!this.originalTitle) this.originalTitle = document.title;
      const alertTitle = this.$t("newPublicOrderAlert") || "طلب جديد من المنيو";
      this.titleOn = true;
      if (this.titleTimer) clearInterval(this.titleTimer);
      document.title = `🔔 ${alertTitle}`;
      this.titleTimer = setInterval(() => {
        this.titleOn = !this.titleOn;
        document.title = this.titleOn ? `🔔 ${alertTitle}` : this.originalTitle;
      }, TITLE_FLASH_MS);
    },
    showDesktopNotification() {
      if (typeof Notification === "undefined" || Notification.permission !== "granted") return;
      try {
        const title = this.$t("newPublicOrderAlert") || "طلب جديد من المنيو";
        const body = [this.alert?.orderCode, this.alert?.customerName].filter(Boolean).join(" — ");
        const n = new Notification(title, {
          body,
          requireInteraction: true,
          tag: "public-menu-order",
        });
        n.onclick = () => {
          window.focus();
          this.openOrders();
          n.close();
        };
      } catch (_) {
        /* ignore */
      }
    },
    openOrders() {
      this.dismiss();
      if (this.$route.path !== "/public-orders") {
        this.$router.push("/public-orders");
      }
    },
    dismiss() {
      this.visible = false;
      this.stopAttention();
    },
    async bindRealtime() {
      try {
        await signalRService.startConnection();
        signalRService.on("PublicOrderAdded", this.onAdded);
      } catch (_) {
        /* ignore */
      }
    },
    unbindRealtime() {
      signalRService.off("PublicOrderAdded", this.onAdded);
    },
  },
};
</script>

<style scoped>
.po-alert {
  position: fixed;
  inset: 0;
  z-index: 10040;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 16px;
  background: rgba(15, 23, 42, 0.55);
}
.po-alert-card {
  width: 100%;
  max-width: 560px;
  margin-top: 48px;
  padding: 24px;
  border-radius: 16px;
  background: #fff7ed;
  border: 2px solid #f59e0b;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.16);
  text-align: center;
  animation: po-pulse 1s ease-in-out infinite;
}
.po-alert-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto 12px;
  border-radius: 999px;
  background: #f59e0b;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
}
.po-alert-kicker {
  margin: 0 0 4px;
  font-weight: 800;
  font-size: 18px;
  color: #b45309;
}
.po-alert-title {
  margin: 0 0 8px;
  font-size: 32px;
  letter-spacing: 1px;
  color: #0f172a;
  font-family: ui-monospace, monospace;
}
.po-alert-meta,
.po-alert-hint {
  margin: 0 0 8px;
  color: #475569;
  font-size: 15px;
}
.po-alert-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  justify-content: center;
  margin-top: 16px;
}
@keyframes po-pulse {
  0%,
  100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.015);
  }
}
</style>
