import Vue from 'vue';
import AppConfirmDialog from '@/components/common/AppConfirmDialog.vue';
import { confirm, setConfirmInstance } from '@/utils/confirm';

export default {
  install(Vue, { i18n } = {}) {
    const ConfirmConstructor = Vue.extend(AppConfirmDialog);
    const instance = new ConfirmConstructor({ i18n });
    instance.$mount();
    document.body.appendChild(instance.$el);
    setConfirmInstance(instance);
    Vue.prototype.$confirm = confirm;
  },
};
