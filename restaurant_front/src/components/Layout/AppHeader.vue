<template>
  <header class="app-top-header">
    <div class="app-top-header-inner">
      <router-link to="/sections" class="app-top-header-sections-link">
        <b-icon icon="grid-3x3-gap-fill" class="app-top-header-sections-icon"></b-icon>
        <span class="app-top-header-sections-text">{{ $t("systemModules") }}</span>
      </router-link>

      <div class="app-top-header-actions">
        <button
          v-if="showPosFullscreenButton"
          type="button"
          class="app-top-header-fullscreen-btn"
          @click="onTogglePosFullscreen"
          :title="
            posFullscreenActive
              ? $t('exitFullscreen') || 'الخروج من الوضع الكامل'
              : $t('enterFullscreen') || 'عرض كامل'
          "
        >
          <b-icon
            :icon="posFullscreenActive ? 'fullscreen-exit' : 'fullscreen'"
          ></b-icon>
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

        <select
          v-model="$i18n.locale"
          @change="changeLanguage"
          class="app-top-header-lang"
        >
          <option value="en">English</option>
          <option value="ar">عربي</option>
        </select>

        <router-link to="/logout" class="app-top-header-logout">
          <b-icon icon="box-arrow-right" class="me-1 app-top-header-logout-icon"></b-icon>
          <span class="app-top-header-logout-text">{{ $t("Logout") }}</span>
        </router-link>
      </div>
    </div>
  </header>
</template>

<script>
export default {
  name: "AppHeader",
  props: {
    showPosFullscreenButton: {
      type: Boolean,
      default: false,
    },
    posFullscreenActive: {
      type: Boolean,
      default: false,
    },
  },
  data() {
    return {
      currentTheme: "dark",
    };
  },
  methods: {
    changeLanguage(event) {
      const lang = event.target.value;
      localStorage.setItem("language", lang);
      this.$i18n.locale = lang;
      document.body.dir = lang === "en" ? "ltr" : "rtl";
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
    onTogglePosFullscreen() {
      this.$emit("toggle-pos-fullscreen");
    },
  },
  mounted() {
    this.initializeTheme();
    const lang = localStorage.getItem("language") || "ar";
    document.body.dir = lang === "en" ? "ltr" : "rtl";
  },
};
</script>
