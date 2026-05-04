<template>
  <header class="app-top-header">
    <div class="app-top-header-inner">
      <router-link
        to="/sections"
        class="app-top-header-sections-link"
        :title="$t('systemModules') || 'أقسام النظام'"
      >
        <b-icon icon="grid-3x3-gap-fill" class="app-top-header-sections-icon"></b-icon>
      </router-link>

      <div v-if="$slots['pos-center']" class="app-top-header-center">
        <slot name="pos-center"></slot>
      </div>

      <div class="app-top-header-actions">
        <div v-if="$slots['pos-actions']" class="app-top-header-pos-slot">
          <slot name="pos-actions"></slot>
        </div>

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
