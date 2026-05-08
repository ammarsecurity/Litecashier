import Vue from 'vue';
import App from './App.vue';
import router from './router';
import { BootstrapVue, IconsPlugin, BootstrapVueIcons } from 'bootstrap-vue';
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
  transition: "Vue-Toastification__fade",
  maxToasts: 3,
  newestOnTop: true,
  position: "bottom-center",
  timeout: 3500,
  closeOnClick: true,
  pauseOnFocusLoss: false,
  pauseOnHover: true,
  draggable: true,
  draggablePercent: 0.65,
  hideProgressBar: false,
  icon: true,
  rtl: i18n.locale === 'ar',
  closeButton: "button",
  toastClassName: "app-toast",
  bodyClassName: "app-toast-body",
  containerClassName: "app-toast-container",
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
