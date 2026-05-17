import AppToast from '@/components/common/AppToast.vue';

const LEGACY_OPTION_KEYS = new Set([
  'position',
  'timeout',
  'rtl',
  'draggable',
  'draggablePercent',
  'hideProgressBar',
  'closeButton',
  'icon',
  'maxToasts',
  'closeOnClick',
  'pauseOnFocusLoss',
  'pauseOnHover',
  'showCloseButtonOnHover',
]);

let vueRef = null;
let i18nRef = null;
let rawToastRef = null;
let currentLocale = 'ar';

export function getToastPosition(locale) {
  return locale === 'ar' ? 'top-left' : 'top-right';
}

export function buildNotifyDefaults(locale = currentLocale) {
  return {
    position: getToastPosition(locale),
    rtl: locale === 'ar',
    timeout: 3200,
    maxToasts: 4,
    newestOnTop: true,
    closeOnClick: true,
    pauseOnFocusLoss: false,
    pauseOnHover: true,
    draggable: false,
    hideProgressBar: false,
    icon: false,
    closeButton: false,
    toastClassName: 'app-toast',
    bodyClassName: 'app-toast-body',
    containerClassName: 'app-toast-container',
  };
}

function stripLegacyOptions(options = {}) {
  const cleaned = { ...options };
  LEGACY_OPTION_KEYS.forEach((key) => {
    delete cleaned[key];
  });
  return cleaned;
}

function extractMessage(content) {
  if (typeof content === 'string' || typeof content === 'number') {
    return String(content);
  }
  if (content && typeof content === 'object') {
    if (content.component === AppToast) {
      return content.props?.message || '';
    }
    if (typeof content.props?.message === 'string') {
      return content.props.message;
    }
  }
  return '';
}

const TITLE_KEYS = {
  success: 'notifySuccess',
  error: 'notifyError',
  warning: 'notifyWarning',
  info: 'notifyInfo',
  default: 'notifyInfo',
};

function getToastLabels(type) {
  const toastType = ['success', 'error', 'warning', 'info'].includes(type) ? type : 'default';
  const titleKey = TITLE_KEYS[toastType] || TITLE_KEYS.default;
  if (i18nRef) {
    return {
      title: i18nRef.t(titleKey),
      closeLabel: i18nRef.t('close'),
    };
  }
  return { title: titleKey, closeLabel: 'Close' };
}

function wrapContent(toast) {
  const message = extractMessage(toast.content);
  const toastType = toast.type || 'default';
  const labels = getToastLabels(toastType);

  if (!message && toast.content && typeof toast.content === 'object' && toast.content.component) {
    const existing = toast.content.props || {};
    return {
      ...toast.content,
      props: {
        ...existing,
        title: existing.title || labels.title,
        closeLabel: existing.closeLabel || labels.closeLabel,
      },
    };
  }

  return {
    component: AppToast,
    props: {
      message,
      type: toastType,
      title: labels.title,
      closeLabel: labels.closeLabel,
    },
  };
}

export function createFilterBeforeCreate() {
  return (toast, toasts) => {
    if (toast === false) return false;

    const defaults = buildNotifyDefaults(currentLocale);
    const merged = {
      ...defaults,
      ...stripLegacyOptions(toast),
      position: defaults.position,
      rtl: defaults.rtl,
    };

    merged.content = wrapContent(toast);
    return merged;
  };
}

function getToast() {
  if (!rawToastRef) {
    throw new Error('Notify plugin is not installed');
  }
  return rawToastRef;
}

export function setRawToast(toastApi) {
  rawToastRef = toastApi;
}

function showToast(type, message, options = {}) {
  const toast = getToast();
  const fn = toast[type] || toast;
  if (typeof fn === 'function') {
    return fn.call(toast, message, stripLegacyOptions(options));
  }
  return toast(message, { ...stripLegacyOptions(options), type });
}

export function syncNotifyLocale(locale) {
  if (locale) {
    currentLocale = locale;
  }
  if (rawToastRef && typeof rawToastRef.updateDefaults === 'function') {
    rawToastRef.updateDefaults(buildNotifyDefaults(currentLocale));
  }
}

export function initNotify(Vue, i18n) {
  vueRef = Vue;
  i18nRef = i18n;
  if (i18n?.locale) {
    currentLocale = i18n.locale;
  }
}

function resolveApiMessage(error, fallbackKey) {
  const apiMessage =
    error?.response?.data?.message ||
    error?.response?.data?.Message ||
    error?.message;
  if (apiMessage) return apiMessage;
  if (i18nRef && fallbackKey) {
    return i18nRef.t(fallbackKey);
  }
  return i18nRef?.t('somethingWrong') || 'Something went wrong';
}

const notify = {
  success(message, options) {
    return showToast('success', message, options);
  },
  error(message, options) {
    return showToast('error', message, options);
  },
  warning(message, options) {
    return showToast('warning', message, options);
  },
  info(message, options) {
    return showToast('info', message, options);
  },
  show(message, options) {
    return showToast('default', message, options);
  },
  apiError(error, fallbackKey = 'somethingWrong') {
    return notify.error(resolveApiMessage(error, fallbackKey));
  },
  clear() {
    return getToast().clear();
  },
  dismiss(id) {
    return getToast().dismiss(id);
  },
};

export default notify;
