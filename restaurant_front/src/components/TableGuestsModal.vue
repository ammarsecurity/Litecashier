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
      <div class="guests-modal-hero">
        <div class="guests-modal-icon-wrap" aria-hidden="true">
          <b-icon icon="people-fill" />
        </div>
        <h3 class="guests-modal-title">{{ $t("numberOfGuests") }}</h3>
        <p class="guests-modal-subtitle">{{ $t("guestsModalSubtitle") }}</p>
        <div v-if="tableNumber" class="guests-table-chip">
          <b-icon icon="grid-3x3-gap-fill" class="guests-table-chip-icon" />
          <span class="guests-table-chip-label">{{ $t("tableNumber") }}</span>
          <strong class="guests-table-chip-value">{{ tableNumber }}</strong>
        </div>
      </div>

      <div class="guests-counter-card">
        <div class="guests-counter-row">
          <button
            type="button"
            class="guests-counter-btn guests-counter-btn--minus"
            :disabled="localCount <= 1"
            :aria-label="$t('decrease') || 'إنقاص'"
            @click="adjustCount(-1)"
          >
            <b-icon icon="dash-lg" />
          </button>

          <div class="guests-counter-value-wrap">
            <input
              v-model.number="localCount"
              type="number"
              min="1"
              max="99"
              class="guests-counter-input"
              :aria-label="$t('numberOfGuests')"
              @change="normalizeCount"
            />
            <span class="guests-counter-hint">{{ $t("numberOfGuests") }}</span>
          </div>

          <button
            type="button"
            class="guests-counter-btn guests-counter-btn--plus"
            :aria-label="$t('increase') || 'زيادة'"
            @click="adjustCount(1)"
          >
            <b-icon icon="plus-lg" />
          </button>
        </div>
      </div>

      <div class="guests-presets">
        <div class="guests-presets-head">
          <b-icon icon="lightning-charge-fill" class="guests-presets-icon" />
          <span class="guests-presets-label">{{ $t("guestsQuickSelect") }}</span>
        </div>
        <div class="guests-presets-row" role="group" :aria-label="$t('guestsQuickSelect')">
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
        <button type="button" class="guests-action-btn guests-action-btn--primary" @click="confirm">
          <b-icon icon="check-lg" />
          <span>{{ $t("save") }}</span>
        </button>
        <button type="button" class="guests-action-btn guests-action-btn--ghost" @click="cancel">
          <b-icon icon="x-lg" />
          <span>{{ $t("cancelButton") }}</span>
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
.table-guests-modal-root .modal-dialog {
  max-width: 420px;
}

.table-guests-modal-root .table-guests-modal-body {
  padding: 0 !important;
  max-height: none !important;
  overflow: hidden !important;
}

.table-guests-modal-root .modal-content {
  border: none;
  border-radius: 1.15rem;
  overflow: hidden;
  background: transparent;
  box-shadow:
    0 24px 48px color-mix(in srgb, #0f172a 18%, transparent),
    0 0 0 1px color-mix(in srgb, var(--primary-color) 12%, transparent);
}
</style>

<style scoped>
.guests-modal-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 0;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.guests-modal-hero {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.45rem;
  padding: 1.35rem 1.35rem 1.1rem;
  background:
    radial-gradient(
      ellipse 90% 80% at 50% 0%,
      color-mix(in srgb, var(--primary-color) 16%, transparent),
      transparent 70%
    ),
    linear-gradient(
      180deg,
      color-mix(in srgb, var(--primary-color) 6%, var(--bg-primary)),
      var(--bg-primary)
    );
  border-bottom: 1px solid color-mix(in srgb, var(--primary-color) 12%, var(--border-color));
}

.guests-modal-icon-wrap {
  width: 3.35rem;
  height: 3.35rem;
  border-radius: 1rem;
  display: grid;
  place-items: center;
  font-size: 1.4rem;
  color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 14%, var(--bg-primary));
  border: 1px solid color-mix(in srgb, var(--primary-color) 28%, transparent);
  box-shadow: 0 8px 18px color-mix(in srgb, var(--primary-color) 16%, transparent);
  margin-bottom: 0.15rem;
}

.guests-modal-title {
  margin: 0;
  font-size: 1.28rem;
  font-weight: 800;
  color: var(--text-primary);
  letter-spacing: -0.01em;
}

.guests-modal-subtitle {
  margin: 0;
  font-size: 0.86rem;
  color: var(--text-secondary);
  line-height: 1.45;
  max-width: 280px;
}

.guests-table-chip {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  margin-top: 0.35rem;
  padding: 0.4rem 0.85rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary-color) 10%, var(--bg-primary));
  border: 1px solid color-mix(in srgb, var(--primary-color) 24%, var(--border-color));
}

.guests-table-chip-icon {
  color: var(--primary-color);
  font-size: 0.88rem;
}

.guests-table-chip-label {
  font-size: 0.78rem;
  color: var(--text-muted);
  font-weight: 600;
}

.guests-table-chip-value {
  font-size: 0.95rem;
  color: var(--primary-color);
  font-weight: 800;
}

.guests-counter-card {
  margin: 0 1.15rem;
  padding: 1rem 1rem 1.05rem;
  border-radius: 1rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
}

.guests-counter-row {
  display: grid;
  grid-template-columns: 3.1rem 1fr 3.1rem;
  gap: 0.75rem;
  align-items: center;
}

.guests-counter-btn {
  width: 3.1rem;
  height: 3.1rem;
  border-radius: 0.9rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  display: grid;
  place-items: center;
  font-size: 1.05rem;
  cursor: pointer;
  transition:
    background 0.18s ease,
    color 0.18s ease,
    border-color 0.18s ease,
    box-shadow 0.18s ease,
    transform 0.18s ease;
}

.guests-counter-btn--plus {
  border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
  background: color-mix(in srgb, var(--primary-color) 10%, var(--bg-primary));
  color: var(--primary-color);
}

.guests-counter-btn:hover:not(:disabled) {
  border-color: var(--primary-color);
  color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 12%, var(--bg-primary));
  box-shadow: 0 6px 14px color-mix(in srgb, var(--primary-color) 16%, transparent);
  transform: translateY(-1px);
}

.guests-counter-btn--plus:hover:not(:disabled) {
  background: var(--primary-color);
  color: #fff;
}

.guests-counter-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.guests-counter-value-wrap {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.2rem;
  min-width: 0;
}

.guests-counter-input {
  width: 100%;
  text-align: center;
  font-size: 2.35rem;
  font-weight: 800;
  font-variant-numeric: tabular-nums;
  line-height: 1.1;
  padding: 0.55rem 0.5rem;
  border: 2px solid color-mix(in srgb, var(--primary-color) 22%, var(--border-color));
  border-radius: 0.9rem;
  background: var(--bg-primary);
  color: var(--text-primary);
  -moz-appearance: textfield;
}

.guests-counter-input:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 18%, transparent);
}

.guests-counter-hint {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-muted);
}

.guests-counter-input::-webkit-outer-spin-button,
.guests-counter-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.guests-presets {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  margin: 0 1.15rem;
}

.guests-presets-head {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
}

.guests-presets-icon {
  color: var(--primary-color);
  font-size: 0.85rem;
}

.guests-presets-label {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-secondary);
}

.guests-presets-row {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.45rem;
}

.guests-preset-btn {
  min-height: 2.45rem;
  padding: 0.35rem 0.4rem;
  border-radius: 0.7rem;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
  color: var(--text-secondary);
  font-size: 0.95rem;
  font-weight: 800;
  font-variant-numeric: tabular-nums;
  cursor: pointer;
  transition:
    background 0.18s ease,
    color 0.18s ease,
    border-color 0.18s ease,
    box-shadow 0.18s ease,
    transform 0.18s ease;
}

.guests-preset-btn:hover {
  border-color: color-mix(in srgb, var(--primary-color) 40%, var(--border-color));
  color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 8%, var(--bg-primary));
}

.guests-preset-btn--active {
  border-color: var(--primary-color);
  background: var(--primary-color);
  color: #fff;
  box-shadow: 0 6px 14px color-mix(in srgb, var(--primary-color) 28%, transparent);
}

.guests-modal-actions {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 0.65rem;
  margin-top: 0.15rem;
  padding: 0.95rem 1.15rem 1.15rem;
  border-top: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--bg-secondary) 70%, var(--bg-primary));
}

.guests-action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  min-height: 2.7rem;
  padding: 0.7rem 1rem;
  border-radius: 0.8rem;
  font-size: 0.95rem;
  font-weight: 700;
  cursor: pointer;
  border: 1px solid transparent;
  transition:
    background 0.18s ease,
    color 0.18s ease,
    border-color 0.18s ease,
    box-shadow 0.18s ease,
    transform 0.18s ease;
}

.guests-action-btn--primary {
  background: var(--primary-color);
  color: #fff;
  box-shadow: 0 8px 18px color-mix(in srgb, var(--primary-color) 28%, transparent);
}

.guests-action-btn--primary:hover {
  background: var(--primary-hover, var(--primary-dark, var(--primary-color)));
  transform: translateY(-1px);
}

.guests-action-btn--ghost {
  background: var(--bg-primary);
  border-color: var(--border-color);
  color: var(--text-secondary);
}

.guests-action-btn--ghost:hover {
  background: var(--bg-tertiary, var(--bg-secondary));
  color: var(--text-primary);
  border-color: color-mix(in srgb, var(--text-primary) 18%, var(--border-color));
}

@media (max-width: 575px) {
  .guests-presets-row {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }

  .guests-modal-actions {
    grid-template-columns: 1fr;
  }

  .guests-action-btn--ghost {
    order: 2;
  }

  .guests-action-btn--primary {
    order: 1;
  }
}
</style>
