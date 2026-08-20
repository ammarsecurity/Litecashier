<template>
  <div id="app">
    <router-view />
    <LicenseGate />
    <DevicePausedGate />
  </div>
</template>

<script>
import { syncNotifyLocale } from '@/plugins/notifyPlugin';
import LicenseGate from '@/components/LicenseGate.vue';
import DevicePausedGate from '@/components/DevicePausedGate.vue';

export default {
  name: 'App',
  components: { LicenseGate, DevicePausedGate },
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
  },
};
</script>
