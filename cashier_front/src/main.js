import Vue from 'vue';
import App from './App.vue';
import router from './router';
import { BootstrapVue, IconsPlugin, BootstrapVueIcons } from 'bootstrap-vue';
import 'bootstrap/dist/css/bootstrap.css';
import 'bootstrap-vue/dist/bootstrap-vue.css';
import './assets/css/main.css';
import './assets/css/pos-v2.css';
import Toast from "vue-toastification";
import "vue-toastification/dist/index.css";
import LottieAnimation from "lottie-vuejs"; 
import VueI18n from 'vue-i18n';
import messages from './lang';
import FlagIcon from 'vue-flag-icon';
import notifyPlugin from './plugins/notifyPlugin';
import confirmPlugin from './plugins/confirmPlugin';
import { createFilterBeforeCreate, buildNotifyDefaults, syncNotifyLocale } from './utils/notify';

Vue.use(BootstrapVue, {
  BTable: {
    labelSortAsc: '',
    labelSortDesc: '',
    labelSortClear: '',
  },
});
Vue.use(IconsPlugin);
Vue.use(BootstrapVueIcons);
Vue.component('LottieAnimation', LottieAnimation); 
Vue.use(FlagIcon);
Vue.use(VueI18n);

const savedLang = localStorage.getItem('language') || 'ar';

export const i18n = new VueI18n({
  locale: savedLang,
  fallbackLocale: 'ar',
  messages,
});

const notifyDefaults = buildNotifyDefaults(savedLang);

Vue.use(Toast, {
  ...notifyDefaults,
  transition: "Vue-Toastification__fade",
  filterBeforeCreate: createFilterBeforeCreate(),
});

Vue.use(notifyPlugin, { i18n });
Vue.use(confirmPlugin, { i18n });
syncNotifyLocale(savedLang);

Vue.config.productionTip = false;

new Vue({
  i18n,
  router,
  render: (h) => h(App),
  beforeMount() {
    const currentLang = this.$i18n.locale;
    document.body.dir = currentLang === 'en' ? 'ltr' : 'rtl';
    syncNotifyLocale(currentLang);
  },
}).$mount('#app');
