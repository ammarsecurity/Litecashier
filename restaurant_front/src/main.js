import Vue from 'vue';
import App from './App.vue';
import router from './router';
import { BootstrapVue, IconsPlugin, BootstrapVueIcons } from 'bootstrap-vue';
import 'vue-toast-notification/dist/theme-sugar.css';
import 'bootstrap/dist/css/bootstrap.css';
import 'bootstrap-vue/dist/bootstrap-vue.css';
import './assets/css/main.css';
import Toast from "vue-toastification";
import "vue-toastification/dist/index.css";
import LottieAnimation from "lottie-vuejs"; 
import VueI18n from 'vue-i18n';
import messages from './lang';
import FlagIcon from 'vue-flag-icon'; 

Vue.use(BootstrapVue);
Vue.use(IconsPlugin);
Vue.use(BootstrapVueIcons);
Vue.component('LottieAnimation', LottieAnimation); 
Vue.use(FlagIcon);
Vue.use(VueI18n);

export const i18n = new VueI18n({
  locale: 'ar',
  fallbackLocale: 'ar',
  messages,
});

Vue.use(Toast, {
  transition: "Vue-Toastification__slideBlurred",
  maxToasts: 3,
  newestOnTop: true,
  position: "top-right",
  timeout: 3000,
  closeOnClick: true,
  pauseOnFocusLoss: false,
  pauseOnHover: true,
  draggable: false,
  hideProgressBar: true,
  icon: true,
  rtl: i18n.locale === 'ar',
  closeButton: false,
});



Vue.config.productionTip = false;

// Create a new Vue instance
new Vue({
  i18n,
  router,
  render: (h) => h(App),
  beforeMount() {
    const currentLang = this.$i18n.locale;
    document.body.dir = currentLang === 'en' ? 'ltr' : 'rtl';
  },
}).$mount('#app');
