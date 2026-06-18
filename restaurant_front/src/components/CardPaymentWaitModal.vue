<template>
  <b-modal
    id="modal-card-payment-wait"
    :visible="visible"
    hide-header
    hide-footer
    class="users-modal"
    modal-class="users-modal card-payment-wait-modal-root"
    content-class="cpw-modal-content"
    body-class="cpw-modal-body"
    :no-close-on-backdrop="isWaiting"
    :no-close-on-esc="isWaiting"
    :hide-header-close="isWaiting"
    @change="onVisibilityChange"
  >
    <div
      class="cpw-shell"
      :class="{
        'cpw-shell--success': isSuccess,
        'cpw-shell--failed': isFailed,
        'cpw-shell--waiting': isWaiting,
      }"
    >
      <div class="cpw-top">
        <div class="cpw-icon-stage">
          <div
            class="cpw-icon-circle"
            :class="{
              'cpw-icon-circle--success': isSuccess,
              'cpw-icon-circle--failed': isFailed,
              'cpw-icon-circle--waiting': isWaiting,
            }"
          >
            <b-spinner v-if="isWaiting" class="cpw-spinner" />
            <b-icon
              v-else-if="isSuccess"
              icon="check-circle-fill"
              class="cpw-result-icon cpw-result-icon--success"
            />
            <b-icon
              v-else-if="isFailed"
              icon="x-circle-fill"
              class="cpw-result-icon cpw-result-icon--failed"
            />
            <b-icon v-else icon="credit-card-2-front-fill" class="cpw-card-icon" />
          </div>
        </div>

        <div class="cpw-title-block">
          <span class="cpw-badge">{{ $t("cardPaymentWaitTitle") }}</span>
          <h3 class="cpw-title">{{ modalTitle }}</h3>
          <p class="cpw-subtitle">{{ statusMessage }}</p>
        </div>
      </div>

      <div class="cpw-amount-card">
        <span class="cpw-amount-label">{{ $t("cardPaymentAmountLabel") }}</span>
        <div class="cpw-amount-value">
          <span class="cpw-amount-number">{{ formattedAmount }}</span>
          <span class="cpw-amount-currency">{{ currencyLabel }}</span>
        </div>
        <div v-if="deviceName" class="cpw-device-chip">
          <b-icon icon="hdd-network" class="cpw-device-icon" />
          <span>{{ deviceName }}</span>
        </div>
      </div>

      <div v-if="isWaiting" class="cpw-steps-row">
        <div
          v-for="step in timelineSteps"
          :key="step.id"
          class="cpw-step-pill"
          :class="`cpw-step-pill--${step.state}`"
        >
          <span class="cpw-step-pill-icon">
            <b-icon v-if="step.state === 'done'" icon="check" />
            <b-spinner v-else-if="step.state === 'active'" small />
            <b-icon v-else :icon="step.icon" />
          </span>
          <span class="cpw-step-pill-label">{{ step.title }}</span>
        </div>
      </div>

      <div v-if="isSuccess && (authCode || refNo)" class="cpw-result-details">
        <div v-if="authCode" class="cpw-result-row">
          <span>{{ $t("authCode") }}</span>
          <strong>{{ authCode }}</strong>
        </div>
        <div v-if="refNo" class="cpw-result-row">
          <span>{{ $t("refNo") }}</span>
          <strong>{{ refNo }}</strong>
        </div>
      </div>

      <div v-if="isFailed && errorMessage" class="cpw-error-box">
        <b-icon icon="exclamation-triangle-fill" class="cpw-error-icon" />
        <span>{{ errorMessage }}</span>
      </div>

      <p v-if="isWaiting" class="cpw-wait-hint">
        <b-icon icon="info-circle" class="cpw-hint-icon" />
        {{ $t("cardPaymentWaitHint") }}
      </p>

      <div class="cpw-actions">
        <button
          v-if="isWaiting && canCancel"
          type="button"
          class="cpw-btn cpw-btn--ghost"
          :disabled="cancelling"
          @click="$emit('cancel')"
        >
          <b-spinner v-if="cancelling" small class="cpw-btn-icon" />
          <b-icon v-else icon="x-circle" class="cpw-btn-icon" />
          {{ $t("cardPaymentCancel") }}
        </button>
        <button
          v-if="isTerminal"
          type="button"
          class="cpw-btn"
          :class="isSuccess ? 'cpw-btn--success' : 'cpw-btn--primary'"
          @click="close"
        >
          <b-icon :icon="isSuccess ? 'check-circle-fill' : 'arrow-left-circle'" class="cpw-btn-icon" />
          {{ $t("close") }}
        </button>
      </div>
    </div>
  </b-modal>
</template>

<script>
export default {
  name: "CardPaymentWaitModal",
  props: {
    visible: { type: Boolean, default: false },
    status: { type: String, default: "Starting" },
    amount: { type: Number, default: 0 },
    currencyCode: { type: String, default: "IQD" },
    deviceName: { type: String, default: "" },
    message: { type: String, default: "" },
    authCode: { type: String, default: "" },
    refNo: { type: String, default: "" },
    errorMessage: { type: String, default: "" },
    canCancel: { type: Boolean, default: true },
    cancelling: { type: Boolean, default: false },
  },
  computed: {
    normalizedStatus() {
      return String(this.status || "Starting");
    },
    isWaiting() {
      return ["Starting", "Pending", "Processing"].includes(this.normalizedStatus);
    },
    isSuccess() {
      return this.normalizedStatus === "Success";
    },
    isFailed() {
      return this.normalizedStatus === "Failed";
    },
    isTerminal() {
      return this.isSuccess || this.isFailed;
    },
    modalTitle() {
      if (this.isSuccess) return this.$t("cardPaymentSuccessTitle");
      if (this.isFailed) return this.$t("cardPaymentFailedTitle");
      if (this.normalizedStatus === "Processing") {
        return this.$t("cardPaymentProcessingTitle");
      }
      if (this.normalizedStatus === "Starting") {
        return this.$t("cardPaymentConnectingTitle");
      }
      return this.$t("cardPaymentPlaceCard");
    },
    statusMessage() {
      if (this.message) {
        const key = String(this.message).trim();
        if (this.$te(key)) return this.$t(key);
        return this.message;
      }
      if (this.isSuccess) return this.$t("cardPaymentSuccess");
      if (this.isFailed) return this.$t("cardPaymentFailed");
      if (this.normalizedStatus === "Processing") {
        return this.$t("cardPaymentProcessingSub");
      }
      if (this.normalizedStatus === "Starting") {
        return this.$t("cardPaymentConnectingSub");
      }
      return this.$t("cardPaymentPlaceCardSub");
    },
    formattedAmount() {
      const value = Number(this.amount) || 0;
      try {
        return value.toLocaleString();
      } catch {
        return String(value);
      }
    },
    currencyLabel() {
      if (this.currencyCode === "IQD") return this.$t("currency");
      return this.currencyCode;
    },
    timelineSteps() {
      const s = this.normalizedStatus;
      const stepState = (doneWhen, activeWhen) => {
        if (doneWhen) return "done";
        if (activeWhen) return "active";
        return "idle";
      };

      return [
        {
          id: "connect",
          icon: "wifi",
          title: this.$t("cardPaymentStepConnect"),
          state: stepState(s !== "Starting", s === "Starting"),
        },
        {
          id: "tap",
          icon: "credit-card",
          title: this.$t("cardPaymentStepCard"),
          state: stepState(s === "Processing" || this.isTerminal, s === "Pending"),
        },
        {
          id: "process",
          icon: "hourglass-split",
          title: this.$t("cardPaymentStepProcessing"),
          state: stepState(this.isTerminal, s === "Processing"),
        },
      ];
    },
  },
  methods: {
    onVisibilityChange(visible) {
      if (!visible) {
        this.$emit("update:visible", false);
        this.$emit("close");
      }
    },
    close() {
      this.$emit("update:visible", false);
      this.$emit("close");
    },
  },
};
</script>

<style>
.card-payment-wait-modal-root .cpw-modal-body {
  padding: 0 !important;
  max-height: none !important;
  overflow: hidden !important;
}

.card-payment-wait-modal-root .cpw-modal-content {
  overflow: hidden;
  border-radius: 1rem;
}

.card-payment-wait-modal-root .modal-content-wrapper {
  padding: 0;
}
</style>

<style scoped>
.cpw-shell {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  padding: 1.25rem 1.35rem 1.1rem;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.cpw-top {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.65rem;
}

.cpw-icon-stage {
  width: 56px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.cpw-icon-circle {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  border: 2px solid var(--border-color);
}

.cpw-icon-circle--waiting {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.22), rgba(129, 140, 248, 0.12));
  border-color: var(--primary-color);
}

.cpw-icon-circle--success {
  background: var(--success-light);
  border-color: var(--success-color);
}

.cpw-icon-circle--failed {
  background: var(--danger-light);
  border-color: var(--danger-color);
}

.cpw-spinner {
  width: 1.4rem;
  height: 1.4rem;
  color: var(--primary-color);
}

.cpw-card-icon {
  font-size: 1.35rem;
  color: var(--primary-light);
}

.cpw-result-icon {
  font-size: 1.6rem;
}

.cpw-result-icon--success { color: var(--success-color); }
.cpw-result-icon--failed { color: var(--danger-color); }

.cpw-title-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
}

.cpw-badge {
  display: inline-flex;
  padding: 0.15rem 0.65rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--primary-light);
  background: rgba(129, 140, 248, 0.14);
  border: 1px solid var(--border-color);
}

.cpw-shell--success .cpw-badge {
  color: var(--success-color);
  background: var(--success-light);
}

.cpw-shell--failed .cpw-badge {
  color: var(--danger-color);
  background: var(--danger-light);
}

.cpw-title {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--text-primary);
  line-height: 1.35;
}

.cpw-subtitle {
  margin: 0;
  font-size: 0.84rem;
  color: var(--text-secondary);
  line-height: 1.45;
  max-width: 300px;
}

.cpw-amount-card {
  text-align: center;
  padding: 0.75rem 1rem;
  border-radius: 0.75rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
}

.cpw-amount-label {
  display: block;
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--text-muted);
  margin-bottom: 0.2rem;
}

.cpw-amount-value {
  display: flex;
  align-items: baseline;
  justify-content: center;
  gap: 0.35rem;
}

.cpw-amount-number {
  font-size: 1.65rem;
  font-weight: 800;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
  line-height: 1.1;
}

.cpw-amount-currency {
  font-size: 0.9rem;
  font-weight: 700;
  color: var(--primary-light);
}

.cpw-device-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  margin-top: 0.5rem;
  padding: 0.25rem 0.65rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-secondary);
  background: var(--bg-tertiary);
  border: 1px solid var(--border-light);
}

.cpw-device-icon {
  font-size: 0.85rem;
}

.cpw-steps-row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.45rem;
}

.cpw-step-pill {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.3rem;
  padding: 0.45rem 0.25rem;
  border-radius: 0.6rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-light);
  min-height: 58px;
}

.cpw-step-pill-icon {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.7rem;
  color: var(--text-muted);
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
}

.cpw-step-pill-label {
  font-size: 0.68rem;
  font-weight: 600;
  color: var(--text-muted);
  text-align: center;
  line-height: 1.25;
}

.cpw-step-pill--done {
  border-color: rgba(52, 194, 94, 0.35);
  background: rgba(52, 194, 94, 0.08);
}

.cpw-step-pill--done .cpw-step-pill-icon {
  color: var(--success-color);
  border-color: var(--success-color);
  background: var(--success-light);
}

.cpw-step-pill--done .cpw-step-pill-label {
  color: var(--text-secondary);
}

.cpw-step-pill--active {
  border-color: var(--primary-color);
  background: rgba(129, 140, 248, 0.1);
}

.cpw-step-pill--active .cpw-step-pill-icon {
  color: var(--primary-color);
  border-color: var(--primary-color);
  background: rgba(129, 140, 248, 0.15);
}

.cpw-step-pill--active .cpw-step-pill-label {
  color: var(--primary-light);
  font-weight: 700;
}

.cpw-result-details {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.65rem 0.85rem;
  border-radius: 0.65rem;
  background: var(--success-light);
  border: 1px solid rgba(52, 194, 94, 0.35);
}

.cpw-result-row {
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
  font-size: 0.82rem;
  color: var(--text-secondary);
}

.cpw-result-row strong {
  color: var(--text-primary);
}

.cpw-error-box {
  display: flex;
  align-items: flex-start;
  gap: 0.4rem;
  padding: 0.65rem 0.85rem;
  border-radius: 0.65rem;
  background: var(--danger-light);
  border: 1px solid rgba(239, 68, 68, 0.35);
  color: var(--text-primary);
  font-size: 0.82rem;
  line-height: 1.45;
}

.cpw-error-icon {
  color: var(--danger-color);
  flex-shrink: 0;
  margin-top: 0.1rem;
}

.cpw-wait-hint {
  margin: 0;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  gap: 0.35rem;
  text-align: center;
  font-size: 0.75rem;
  color: var(--text-muted);
  line-height: 1.4;
}

.cpw-hint-icon {
  flex-shrink: 0;
  margin-top: 0.1rem;
}

.cpw-actions {
  display: flex;
  justify-content: center;
  padding-top: 0.15rem;
}

.cpw-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  padding: 0.6rem 1.15rem;
  border: none;
  border-radius: 0.55rem;
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
  min-width: 130px;
}

.cpw-btn-icon {
  font-size: 1rem;
}

.cpw-btn--primary {
  background: var(--primary-color);
  color: #fff;
}

.cpw-btn--success {
  background: var(--success-color);
  color: #fff;
}

.cpw-btn--ghost {
  background: transparent;
  color: var(--text-secondary);
  border: 1px solid var(--border-color);
}

.cpw-btn--ghost:hover:not(:disabled) {
  background: var(--bg-tertiary);
  color: var(--text-primary);
}

.cpw-btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}
</style>
