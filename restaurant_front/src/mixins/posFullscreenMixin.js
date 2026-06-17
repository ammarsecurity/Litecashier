import {
  isBrowserFullscreen,
  toggleBrowserFullscreen,
} from "@/utils/browserFullscreen.js";

/**
 * POS / Waiter browser fullscreen (F11-like) synced with header toggle button.
 */
export default {
  data() {
    return {
      isFullscreen: false,
    };
  },
  mounted() {
    this._onBrowserFullscreenChange = () => {
      this.syncPosFullscreenState();
    };
    document.addEventListener("fullscreenchange", this._onBrowserFullscreenChange);
    document.addEventListener(
      "webkitfullscreenchange",
      this._onBrowserFullscreenChange
    );
    this.syncPosFullscreenState();
  },
  beforeDestroy() {
    if (this._onBrowserFullscreenChange) {
      document.removeEventListener(
        "fullscreenchange",
        this._onBrowserFullscreenChange
      );
      document.removeEventListener(
        "webkitfullscreenchange",
        this._onBrowserFullscreenChange
      );
    }
  },
  methods: {
    syncPosFullscreenState() {
      this.isFullscreen = isBrowserFullscreen();
      try {
        localStorage.setItem("posFullscreen", String(this.isFullscreen));
      } catch {
        /* ignore */
      }
    },
    getFullscreenToastPosition() {
      return document.documentElement.dir === "rtl" ? "top-right" : "top-left";
    },
    async toggleFullscreen() {
      const toastPosition = this.getFullscreenToastPosition();
      try {
        await toggleBrowserFullscreen();
        this.syncPosFullscreenState();
        const message = this.isFullscreen
          ? this.$i18n.t("fullscreenEnabled") || "تم تفعيل الوضع الكامل"
          : this.$i18n.t("fullscreenDisabled") || "تم إلغاء الوضع الكامل";
        this.$toast.info(message, {
          position: toastPosition,
          timeout: 2000,
          maxToasts: 1,
        });
      } catch (error) {
        console.warn("Fullscreen toggle failed:", error);
        this.syncPosFullscreenState();
        this.$toast.warning(
          this.$i18n.t("fullscreenUnavailable") ||
            "تعذّر تفعيل الوضع الكامل — قد يمنع المتصفح ذلك",
          { position: toastPosition, timeout: 2500, maxToasts: 1 }
        );
      }
    },
  },
};
