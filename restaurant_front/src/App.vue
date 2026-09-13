<template>
  <div id="app">
    <router-view />
    <SystemSectionsFab v-if="showSectionsFab" />
    <LicenseGate />
    <DevicePausedGate />
  </div>
</template>


<script>
import { syncNotifyLocale } from '@/plugins/notifyPlugin';
import SystemSectionsFab from '@/components/Layout/SystemSectionsFab.vue';
import LicenseGate from '@/components/LicenseGate.vue';
import DevicePausedGate from '@/components/DevicePausedGate.vue';
import pendingOrderAlertSound from '@/utils/pendingOrderAlertSound.js';
import { onAuthSessionChanged } from '@/utils/authSessionBus.js';

export default {
  name: 'App',
  components: { SystemSectionsFab, LicenseGate, DevicePausedGate },
  data() {
    return {
      sessionTick: 0,
      unbindAuthSession: null,
    };
  },
  computed: {
    showSectionsFab() {
      void this.sessionTick;
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
    '$route'() {
      this.bumpSession();
    },
  },
  mounted() {
    pendingOrderAlertSound.unlock();
    const savedTheme = localStorage.getItem('theme') || 'dark';
    const root = document.documentElement;
    root.classList.remove('light-theme', 'dark-theme');
    root.classList.add(`${savedTheme}-theme`);
    syncNotifyLocale(this.$i18n.locale);
    this.bumpSession();
    this.unbindAuthSession = onAuthSessionChanged(this.bumpSession);
  },
  beforeDestroy() {
    if (typeof this.unbindAuthSession === 'function') {
      this.unbindAuthSession();
      this.unbindAuthSession = null;
    }
  },
  methods: {
    bumpSession() {
      this.sessionTick += 1;
    },
  },
};
</script>
