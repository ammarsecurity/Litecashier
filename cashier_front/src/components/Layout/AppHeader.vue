<template>
  <header class="app-top-header">
    <div class="app-top-header-inner">
      <div class="app-top-header-start">
        <slot name="header-start">
          <router-link
            to="/sections"
            class="app-top-header-sections-link"
            :title="$t('systemModules') || 'أقسام النظام'"
          >
            <b-icon icon="grid-3x3-gap-fill" class="app-top-header-sections-icon"></b-icon>
          </router-link>
        </slot>
      </div>

      <div v-if="$slots['pos-center']" class="app-top-header-center">
        <slot name="pos-center"></slot>
      </div>

      <div class="app-top-header-actions">
        <div v-if="$slots['pos-actions']" class="app-top-header-pos-slot">
          <slot name="pos-actions"></slot>
        </div>

        <button
          type="button"
          class="app-top-header-fullscreen-btn"
          :class="{ 'app-top-header-fullscreen-btn--active': isBrowserFullscreen }"
          @click="toggleBrowserFullscreen"
          :title="
            isBrowserFullscreen
              ? $t('exitFullscreen') || 'الخروج من الوضع الكامل'
              : $t('enterFullscreen') || 'عرض كامل'
          "
        >
          <b-icon
            :icon="isBrowserFullscreen ? 'fullscreen-exit' : 'fullscreen'"
          ></b-icon>
        </button>

        <button
          type="button"
          class="app-top-header-icon-btn"
          :disabled="!canZoomIn"
          @click="zoomIn"
          :title="$t('zoomInScreen') || 'تكبير الشاشة'"
        >
          <b-icon icon="zoom-in" class="app-top-header-icon"></b-icon>
        </button>

        <button
          type="button"
          class="app-top-header-icon-btn"
          :disabled="!canZoomOut"
          @click="zoomOut"
          :title="$t('zoomOutScreen') || 'تصغير الشاشة'"
        >
          <b-icon icon="zoom-out" class="app-top-header-icon"></b-icon>
        </button>

        <button
          type="button"
          @click="toggleTheme"
          class="app-top-header-icon-btn"
          :title="currentTheme === 'dark' ? ($t('switchToLightMode') || '') : ($t('switchToDarkMode') || '')"
        >
          <b-icon
            :icon="currentTheme === 'dark' ? 'sun-fill' : 'moon-fill'"
            class="app-top-header-icon"
          ></b-icon>
        </button>

        <button
          type="button"
          class="app-top-header-lang"
          @click="toggleLanguage"
          :title="$t('changeLanguage') || 'تغيير اللغة'"
        >
          <b-icon icon="translate" class="app-top-header-icon"></b-icon>
        </button>

        <router-link
          to="/logout"
          class="app-top-header-logout"
          :title="$t('Logout') || 'تسجيل الخروج'"
        >
          <b-icon icon="box-arrow-right" class="app-top-header-logout-icon"></b-icon>
        </router-link>
      </div>
    </div>
  </header>
</template>

<script>
import { syncNotifyLocale } from '@/plugins/notifyPlugin';

const ZOOM_STORAGE_KEY = "appUiZoom";
const ZOOM_MIN = 0.8;
const ZOOM_MAX = 1.5;
const ZOOM_STEP = 0.1;

export default {
  name: "AppHeader",
  data() {
    return {
      currentTheme: "dark",
      isBrowserFullscreen: false,
      uiZoom: 1,
    };
  },
  computed: {
    canZoomIn() {
      return this.uiZoom < ZOOM_MAX - 0.001;
    },
    canZoomOut() {
      return this.uiZoom > ZOOM_MIN + 0.001;
    },
  },
  methods: {
    changeLanguage(event) {
      const lang = event.target.value;
      localStorage.setItem("language", lang);
      this.$i18n.locale = lang;
      document.body.dir = lang === "en" ? "ltr" : "rtl";
      syncNotifyLocale(lang);
    },
    toggleLanguage() {
      const currentLang = this.$i18n.locale || localStorage.getItem("language") || "ar";
      const nextLang = currentLang === "ar" ? "en" : "ar";
      this.changeLanguage({ target: { value: nextLang } });
    },
    toggleTheme() {
      this.currentTheme = this.currentTheme === "dark" ? "light" : "dark";
      this.applyTheme(this.currentTheme);
      localStorage.setItem("theme", this.currentTheme);
    },
    applyTheme(theme) {
      const root = document.documentElement;
      root.classList.remove("light-theme", "dark-theme");
      root.classList.add(`${theme}-theme`);
    },
    initializeTheme() {
      const savedTheme = localStorage.getItem("theme") || "dark";
      this.currentTheme = savedTheme;
      this.applyTheme(savedTheme);
    },
    clampZoom(value) {
      const n = Number(value);
      if (!Number.isFinite(n)) return 1;
      return Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, Math.round(n * 10) / 10));
    },
    applyUiZoom(zoom) {
      const next = this.clampZoom(zoom);
      this.uiZoom = next;
      const root = document.documentElement;
      root.style.zoom = String(next);
      root.style.setProperty("--app-ui-zoom", String(next));
      localStorage.setItem(ZOOM_STORAGE_KEY, String(next));
    },
    initializeUiZoom() {
      const saved = localStorage.getItem(ZOOM_STORAGE_KEY);
      this.applyUiZoom(saved != null ? saved : 1);
    },
    zoomIn() {
      if (!this.canZoomIn) return;
      this.applyUiZoom(this.uiZoom + ZOOM_STEP);
    },
    zoomOut() {
      if (!this.canZoomOut) return;
      this.applyUiZoom(this.uiZoom - ZOOM_STEP);
    },
    getFullscreenElement() {
      return (
        document.fullscreenElement ||
        document.webkitFullscreenElement ||
        document.msFullscreenElement ||
        null
      );
    },
    syncBrowserFullscreenState() {
      this.isBrowserFullscreen = !!this.getFullscreenElement();
      if (typeof document !== "undefined") {
        document.documentElement.classList.toggle(
          "app-browser-fullscreen",
          this.isBrowserFullscreen
        );
      }
    },
    async requestBrowserFullscreen() {
      const element = document.documentElement;
      if (element.requestFullscreen) {
        await element.requestFullscreen();
        return;
      }
      if (element.webkitRequestFullscreen) {
        await element.webkitRequestFullscreen();
        return;
      }
      if (element.msRequestFullscreen) {
        await element.msRequestFullscreen();
      }
    },
    async exitBrowserFullscreen() {
      if (document.exitFullscreen) {
        await document.exitFullscreen();
        return;
      }
      if (document.webkitExitFullscreen) {
        await document.webkitExitFullscreen();
        return;
      }
      if (document.msExitFullscreen) {
        await document.msExitFullscreen();
      }
    },
    async toggleBrowserFullscreen() {
      try {
        if (this.getFullscreenElement()) {
          await this.exitBrowserFullscreen();
        } else {
          await this.requestBrowserFullscreen();
        }
      } catch (error) {
        this.$notify.error(this.$t("fullscreenUnavailable") || "الوضع الكامل غير مدعوم", {
          position: "top-right",
          timeout: 2500,
        });
      }
    },
    onFullscreenChange() {
      this.syncBrowserFullscreenState();
    },
  },
  mounted() {
    this.initializeTheme();
    this.initializeUiZoom();
    const lang = localStorage.getItem("language") || "ar";
    document.body.dir = lang === "en" ? "ltr" : "rtl";
    this.syncBrowserFullscreenState();
    document.addEventListener("fullscreenchange", this.onFullscreenChange);
    document.addEventListener("webkitfullscreenchange", this.onFullscreenChange);
    document.addEventListener("MSFullscreenChange", this.onFullscreenChange);
  },
  beforeDestroy() {
    document.removeEventListener("fullscreenchange", this.onFullscreenChange);
    document.removeEventListener("webkitfullscreenchange", this.onFullscreenChange);
    document.removeEventListener("MSFullscreenChange", this.onFullscreenChange);
    if (!this.getFullscreenElement()) {
      document.documentElement.classList.remove("app-browser-fullscreen");
    }
  },
};
</script>
