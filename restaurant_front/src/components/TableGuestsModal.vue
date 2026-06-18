<template>
  <b-modal
    id="modal-floor-table-guests"
    hide-header
    hide-footer
    class="users-modal"
    modal-class="users-modal table-guests-modal-root"
    body-class="table-guests-modal-body"
    centered
    @shown="onShown"
  >
    <div class="guests-modal-content">
      <div class="guests-modal-header">
        <div class="guests-modal-icon-wrap">
          <b-icon icon="people-fill" />
        </div>
        <h3 class="guests-modal-title">{{ $t("numberOfGuests") }}</h3>
        <p class="guests-modal-subtitle">{{ $t("guestsModalSubtitle") }}</p>
      </div>

      <div v-if="tableNumber" class="guests-table-chip">
        <b-icon icon="grid-3x3-gap-fill" class="guests-table-chip-icon" />
        <span class="guests-table-chip-label">{{ $t("tableNumber") }}</span>
        <strong class="guests-table-chip-value">{{ tableNumber }}</strong>
      </div>

      <div class="guests-counter-card">
        <label class="guests-counter-label">{{ $t("numberOfGuests") }}</label>
        <div class="guests-counter-row">
          <button
            type="button"
            class="guests-counter-btn"
            :disabled="localCount <= 1"
            @click="adjustCount(-1)"
          >
            <b-icon icon="dash-lg" />
          </button>
          <input
            v-model.number="localCount"
            type="number"
            min="1"
            max="99"
            class="guests-counter-input"
            @change="normalizeCount"
          />
          <button type="button" class="guests-counter-btn" @click="adjustCount(1)">
            <b-icon icon="plus-lg" />
          </button>
        </div>
      </div>

      <div class="guests-presets">
        <span class="guests-presets-label">{{ $t("guestsQuickSelect") }}</span>
        <div class="guests-presets-row">
          <button
            v-for="preset in guestPresets"
            :key="preset"
            type="button"
            class="guests-preset-btn"
            :class="{ 'guests-preset-btn--active': localCount === preset }"
            @click="setCount(preset)"
          >
            {{ preset }}
          </button>
        </div>
      </div>

      <div class="guests-modal-actions">
        <button type="button" class="order-notes-confirm-button" @click="confirm">
          <b-icon icon="check-circle-fill" class="me-2" />
          {{ $t("save") }}
        </button>
        <button type="button" class="order-notes-cancel-button" @click="cancel">
          <b-icon icon="x-circle-fill" class="me-2" />
          {{ $t("cancelButton") }}
        </button>
      </div>
    </div>
  </b-modal>
</template>

<script>
export default {
  name: "TableGuestsModal",
  props: {
    tableNumber: {
      type: String,
      default: "",
    },
    count: {
      type: Number,
      default: 1,
    },
  },
  data() {
    return {
      localCount: 1,
      guestPresets: [1, 2, 3, 4, 5, 6, 8, 10],
    };
  },
  watch: {
    count: {
      immediate: true,
      handler(value) {
        this.localCount = this.clampCount(value);
      },
    },
  },
  methods: {
    clampCount(value) {
      const n = Number(value);
      if (!Number.isFinite(n) || n < 1) return 1;
      return Math.min(Math.round(n), 99);
    },
    onShown() {
      this.localCount = this.clampCount(this.count);
    },
    syncCount() {
      const next = this.clampCount(this.localCount);
      this.localCount = next;
      this.$emit("update:count", next);
    },
    normalizeCount() {
      this.syncCount();
    },
    adjustCount(delta) {
      this.localCount = this.clampCount(this.localCount + delta);
      this.syncCount();
    },
    setCount(value) {
      this.localCount = this.clampCount(value);
      this.syncCount();
    },
    confirm() {
      this.syncCount();
      this.$emit("confirm", this.localCount);
    },
    cancel() {
      this.$emit("cancel");
    },
  },
};
</script>

<style>
.table-guests-modal-root .table-guests-modal-body {
  padding: 0 !important;
  max-height: none !important;
  overflow: hidden !important;
}
</style>

<style scoped>
.guests-modal-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.35rem 1.5rem 1.25rem;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.guests-modal-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.4rem;
}

.guests-modal-icon-wrap {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.35rem;
  color: var(--primary-light);
  background: rgba(129, 140, 248, 0.15);
  border: 1px solid var(--border-color);
}

.guests-modal-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 800;
  color: var(--text-primary);
}

.guests-modal-subtitle {
  margin: 0;
  font-size: 0.86rem;
  color: var(--text-secondary);
  line-height: 1.45;
  max-width: 280px;
}

.guests-table-chip {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  padding: 0.55rem 0.85rem;
  border-radius: 999px;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  align-self: center;
}

.guests-table-chip-icon {
  color: var(--primary-color);
  font-size: 0.9rem;
}

.guests-table-chip-label {
  font-size: 0.78rem;
  color: var(--text-muted);
  font-weight: 600;
}

.guests-table-chip-value {
  font-size: 0.95rem;
  color: var(--text-primary);
  font-weight: 800;
}

.guests-counter-card {
  padding: 0.9rem 1rem;
  border-radius: 0.8rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
}

.guests-counter-label {
  display: block;
  margin-bottom: 0.65rem;
  font-size: 0.84rem;
  font-weight: 700;
  color: var(--text-primary);
  text-align: center;
}

.guests-counter-row {
  display: grid;
  grid-template-columns: 44px 1fr 44px;
  gap: 0.6rem;
  align-items: center;
}

.guests-counter-btn {
  width: 44px;
  height: 44px;
  border-radius: 0.65rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}

.guests-counter-btn:hover:not(:disabled) {
  border-color: var(--primary-color);
  color: var(--primary-color);
  background: rgba(129, 140, 248, 0.1);
}

.guests-counter-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.guests-counter-input {
  width: 100%;
  text-align: center;
  font-size: 1.75rem;
  font-weight: 800;
  font-variant-numeric: tabular-nums;
  padding: 0.45rem 0.5rem;
  border: 2px solid var(--border-color);
  border-radius: 0.7rem;
  background: var(--bg-primary);
  color: var(--text-primary);
  -moz-appearance: textfield;
}

.guests-counter-input:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.12);
}

.guests-counter-input::-webkit-outer-spin-button,
.guests-counter-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.guests-presets {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.guests-presets-label {
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--text-muted);
  text-align: center;
}

.guests-presets-row {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.45rem;
}

.guests-preset-btn {
  min-width: 40px;
  height: 36px;
  padding: 0 0.65rem;
  border-radius: 0.55rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.guests-preset-btn:hover {
  border-color: var(--primary-color);
  color: var(--primary-light);
}

.guests-preset-btn--active {
  border-color: var(--primary-color);
  background: rgba(129, 140, 248, 0.12);
  color: var(--primary-light);
  box-shadow: 0 0 0 2px rgba(129, 140, 248, 0.1);
}

.guests-modal-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  margin-top: 0.25rem;
  padding-top: 0.25rem;
}

.guests-modal-actions .order-notes-confirm-button,
.guests-modal-actions .order-notes-cancel-button {
  display: inline-flex;
  align-items: center;
  padding: 0.7rem 1.25rem;
  border: none;
  border-radius: 0.55rem;
  font-size: 0.92rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.guests-modal-actions .order-notes-confirm-button {
  background: var(--primary-color);
  color: #fff;
}

.guests-modal-actions .order-notes-confirm-button:hover {
  background: var(--primary-hover);
}

.guests-modal-actions .order-notes-cancel-button {
  background: transparent;
  border: 1px solid var(--border-color);
  color: var(--text-secondary);
}

.guests-modal-actions .order-notes-cancel-button:hover {
  background: var(--bg-tertiary);
  color: var(--text-primary);
}

@media (max-width: 575px) {
  .guests-modal-actions {
    flex-direction: column-reverse;
  }

  .guests-modal-actions .order-notes-confirm-button,
  .guests-modal-actions .order-notes-cancel-button {
    width: 100%;
    justify-content: center;
  }
}
</style>
