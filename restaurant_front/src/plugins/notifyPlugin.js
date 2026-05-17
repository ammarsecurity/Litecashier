import notify, { initNotify, setRawToast, syncNotifyLocale } from '@/utils/notify';

function wrapToastInterface(rawToast, notifyApi) {
  const types = ['success', 'error', 'warning', 'info', 'default'];
  const wrapped = (message, options) => notifyApi.show(message, options);

  types.forEach((type) => {
    wrapped[type] = (message, options) => notifyApi[type](message, options);
  });

  wrapped.clear = (...args) => rawToast.clear(...args);
  wrapped.dismiss = (...args) => rawToast.dismiss(...args);
  wrapped.update = (...args) => rawToast.update(...args);
  wrapped.updateDefaults = (...args) => rawToast.updateDefaults(...args);

  return wrapped;
}

export default {
  install(Vue, { i18n } = {}) {
    initNotify(Vue, i18n);
    const rawToast = Vue.prototype.$toast;
    setRawToast(rawToast);
    Vue.prototype.$notify = notify;
    Vue.prototype.$toast = wrapToastInterface(rawToast, notify);
    syncNotifyLocale(i18n?.locale);
  },
};

export { syncNotifyLocale };
