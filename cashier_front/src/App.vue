<template>
  <div id="app">
    <router-view />
    <LicenseGate />
  </div>
</template>

<script>
import { syncNotifyLocale } from '@/plugins/notifyPlugin';
import LicenseGate from '@/components/LicenseGate.vue';

export default {
  name: 'App',
  components: { LicenseGate },
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
