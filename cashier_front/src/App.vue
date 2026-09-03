<template>
  <div id="app">
    <router-view />
    <LicenseGate v-if="!isPublicMenu" />
    <DevicePausedGate v-if="!isPublicMenu" />
  </div>
</template>

<script>
import { syncNotifyLocale } from '@/plugins/notifyPlugin';
import LicenseGate from '@/components/LicenseGate.vue';
import DevicePausedGate from '@/components/DevicePausedGate.vue';
import { HTTP } from '@/http/api.js';
import { applyCommercialBranding } from '@/utils/posBranding.js';

export default {
  name: 'App',
  components: { LicenseGate, DevicePausedGate },
  computed: {
    isPublicMenu() {
      const path = this.$route?.path || '';
      return this.$route?.name === 'publicMenu' || path === '/menu' || path.startsWith('/menu/');
    },
  },
  watch: {
    '$i18n.locale'(locale) {
      syncNotifyLocale(locale);
    },
  },
  mounted() {
    const savedTheme = localStorage.getItem('theme') || 'dark';
    const root = document.documentElement;
    root.classList.remove('light-theme', 'dark-theme');
    root.classList.add(`${savedTheme}-theme`);
    syncNotifyLocale(this.$i18n.locale);
    this.syncPosBranding();
  },
  methods: {
    async syncPosBranding() {
      if (this.isPublicMenu || !localStorage.getItem('token')) return;
      try {
        const res = await HTTP.get('Admin/CommercialUserInfo');
        applyCommercialBranding(res?.data?.data);
      } catch (_) {
        /* ignore — POS/settings will retry */
      }
    },
  },
};
</script>
