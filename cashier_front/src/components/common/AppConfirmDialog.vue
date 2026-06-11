<template>
  <b-modal
    id="app-global-confirm-modal"
    v-model="visible"
    hide-header
    hide-footer
    centered
    class="users-modal app-confirm-modal"
    content-class="app-confirm-modal-content"
    @hidden="onHidden"
  >
    <div class="modal-content-wrapper">
      <div class="delete-confirmation-content">
        <div class="delete-icon-wrapper">
          <b-icon :icon="icon" class="delete-warning-icon" :class="iconClass"></b-icon>
        </div>
        <h3 class="delete-confirmation-title">{{ title }}</h3>
        <p class="delete-confirmation-text">{{ message }}</p>
        <div class="delete-confirmation-actions">
          <button
            type="button"
            class="delete-confirm-button"
            :class="confirmButtonClass"
            @click="onConfirm"
          >
            <b-icon icon="check-circle-fill" class="me-2"></b-icon>
            {{ confirmText }}
          </button>
          <button type="button" class="delete-cancel-button" @click="onCancel">
            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
            {{ cancelText }}
          </button>
        </div>
      </div>
    </div>
  </b-modal>
</template>

<script>
import { resolveConfirm } from '@/utils/confirm';

export default {
  name: 'AppConfirmDialog',
  data() {
    return {
      visible: false,
      title: '',
      message: '',
      confirmText: '',
      cancelText: '',
      variant: 'danger',
      icon: 'exclamation-triangle-fill',
      settled: false,
    };
  },
  computed: {
    iconClass() {
      return `app-confirm-icon--${this.variant}`;
    },
    confirmButtonClass() {
      return this.variant !== 'danger' ? `delete-confirm-button--${this.variant}` : '';
    },
  },
  methods: {
    open(options = {}) {
      const i18n = this.$i18n;
      this.settled = false;
      this.variant = options.variant || 'danger';
      this.icon = options.icon || this.defaultIcon(this.variant);
      this.title = options.title || i18n.t('confirm_delete');
      this.message = options.message || '';
      this.confirmText =
        options.confirmText ||
        (this.variant === 'danger' ? i18n.t('deleteButtonLabel') : i18n.t('confirm'));
      this.cancelText = options.cancelText || i18n.t('cancelButtonLabel');
      this.visible = true;
    },
    defaultIcon(variant) {
      if (variant === 'warning') return 'exclamation-circle-fill';
      if (variant === 'info') return 'info-circle-fill';
      return 'exclamation-triangle-fill';
    },
    finish(result) {
      if (this.settled) return;
      this.settled = true;
      this.visible = false;
      resolveConfirm(result);
    },
    onConfirm() {
      this.finish(true);
    },
    onCancel() {
      this.finish(false);
    },
    onHidden() {
      this.finish(false);
    },
  },
};
</script>
