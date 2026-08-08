<template>
  <div id="app">
    <router-view />
    <SystemSectionsFab v-if="showSectionsFab" />
    <LicenseGate />
  </div>
</template>

<script>
import { syncNotifyLocale } from '@/plugins/notifyPlugin';
import SystemSectionsFab from '@/components/Layout/SystemSectionsFab.vue';
import LicenseGate from '@/components/LicenseGate.vue';
import pendingOrderAlertSound from '@/utils/pendingOrderAlertSound.js';

export default {
  name: 'App',
  components: { SystemSectionsFab, LicenseGate },
  computed: {
    showSectionsFab() {
      const token = localStorage.getItem('token');
      if (!token) return false;
      const route = this.$route;
      if (route.meta && route.meta.requiresAuth === false) return false;
      if (route.path === '/login' || route.path === '/register' || route.path === '/logout') return false;
      return true;
    },
  },
  watch: {
    '$i18n.locale'(locale) {
      syncNotifyLocale(locale);
    },
  },
  mounted() {
    pendingOrderAlertSound.unlock();
    const savedTheme = localStorage.getItem('theme') || 'dark';
    const root = document.documentElement;
    root.classList.remove('light-theme', 'dark-theme');
    root.classList.add(`${savedTheme}-theme`);
    syncNotifyLocale(this.$i18n.locale);
  },
};
</script>