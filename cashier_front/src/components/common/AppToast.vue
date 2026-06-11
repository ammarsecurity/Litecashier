<template>
  <div class="app-notify" :class="typeClass" role="alert">
    <div class="app-notify__icon-wrap" aria-hidden="true">
      <b-icon :icon="iconName" />
    </div>
    <div class="app-notify__body">
      <p class="app-notify__title">{{ displayTitle }}</p>
      <p v-if="message" class="app-notify__message">{{ message }}</p>
    </div>
    <button
      type="button"
      class="app-notify__close"
      :aria-label="displayCloseLabel"
      @click="close"
    >
      <b-icon icon="x" />
    </button>
  </div>
</template>

<script>
const ICONS = {
  success: 'check-circle-fill',
  error: 'exclamation-circle-fill',
  warning: 'exclamation-triangle-fill',
  info: 'info-circle-fill',
  default: 'bell-fill',
};

export default {
  name: 'AppToast',
  props: {
    message: {
      type: String,
      default: '',
    },
    type: {
      type: String,
      default: 'default',
    },
    title: {
      type: String,
      default: '',
    },
    closeLabel: {
      type: String,
      default: '',
    },
  },
  computed: {
    toastType() {
      const t = (this.type || 'default').toLowerCase();
      return ['success', 'error', 'warning', 'info'].includes(t) ? t : 'default';
    },
    typeClass() {
      return `app-notify--${this.toastType}`;
    },
    displayTitle() {
      return this.title || this.toastType;
    },
    iconName() {
      return ICONS[this.toastType] || ICONS.default;
    },
    displayCloseLabel() {
      return this.closeLabel || 'Close';
    },
  },
  methods: {
    close() {
      this.$emit('close-toast');
    },
  },
};
</script>
