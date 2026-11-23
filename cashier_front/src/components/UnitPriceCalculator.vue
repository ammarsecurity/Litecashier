<template>
  <div class="unit-price-calc" :class="{ 'is-open': open }">
    <button type="button" class="unit-price-calc__toggle" @click="open = !open">
      <span class="unit-price-calc__toggle-icon" aria-hidden="true">÷</span>
      <span class="unit-price-calc__toggle-text">{{ displayTitle }}</span>
      <span class="unit-price-calc__chevron" :class="{ 'is-open': open }" aria-hidden="true"></span>
    </button>

    <div v-show="open" class="unit-price-calc__body">
      <div class="unit-price-calc__row">
        <label class="unit-price-calc__field">
          <span>{{ $t("unitPriceCalcPieces") || "عدد القطع الكلي" }}</span>
          <input
            v-model="pieces"
            type="number"
            min="0"
            step="1"
            inputmode="numeric"
            class="unit-price-calc__input"
            :placeholder="$t('unitPriceCalcPiecesShort') || 'العدد'"
          />
        </label>
        <label class="unit-price-calc__field">
          <span>{{ $t("unitPriceCalcTotal") || "المبلغ الكلي" }}</span>
          <input
            v-model="totalAmount"
            type="number"
            min="0"
            step="any"
            inputmode="decimal"
            class="unit-price-calc__input"
            :placeholder="$t('unitPriceCalcTotalShort') || 'المبلغ'"
          />
        </label>
        <div class="unit-price-calc__result" :class="{ 'has-value': unitPrice != null }">
          <span class="unit-price-calc__result-label">
            {{ $t("unitPriceCalcResult") || "سعر القطعة" }}
          </span>
          <strong class="unit-price-calc__result-value">
            {{ unitPrice != null ? formatResult(unitPrice) : "—" }}
          </strong>
        </div>
      </div>
      <button
        v-if="pieces || totalAmount"
        type="button"
        class="unit-price-calc__clear"
        @click="clear"
      >
        {{ $t("unitPriceCalcClear") || "مسح" }}
      </button>
    </div>
  </div>
</template>

<script>
export default {
  name: "UnitPriceCalculator",
  props: {
    label: {
      type: String,
      default: "",
    },
  },
  data() {
    return {
      open: false,
      pieces: "",
      totalAmount: "",
    };
  },
  computed: {
    displayTitle() {
      return this.label || this.$t("unitPriceCalcTitle") || "حاسبة سعر القطعة";
    },
    unitPrice() {
      const pieces = Number(this.pieces);
      const total = Number(this.totalAmount);
      if (!(pieces > 0) || this.totalAmount === "" || this.totalAmount == null) {
        return null;
      }
      if (!Number.isFinite(total) || total < 0) {
        return null;
      }
      return Math.round((total / pieces) * 100) / 100;
    },
  },
  watch: {
    pieces() {
      this.emitIfReady();
    },
    totalAmount() {
      this.emitIfReady();
    },
  },
  methods: {
    emitIfReady() {
      if (this.unitPrice == null) return;
      this.$emit("apply", this.unitPrice);
    },
    clear() {
      this.pieces = "";
      this.totalAmount = "";
    },
    formatResult(value) {
      const n = Number(value);
      if (!Number.isFinite(n)) return "";
      return Number.isInteger(n) ? String(n) : n.toFixed(2);
    },
  },
};
</script>

<style scoped>
.unit-price-calc {
  margin-top: 0.4rem;
  border-radius: 10px;
  border: 1px solid color-mix(in srgb, var(--primary-color, #0f6e6e) 28%, transparent);
  background: color-mix(in srgb, var(--primary-color, #0f6e6e) 8%, transparent);
  overflow: hidden;
}

.unit-price-calc.is-open {
  background: color-mix(in srgb, var(--primary-color, #0f6e6e) 12%, transparent);
}

.unit-price-calc__toggle {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  width: 100%;
  margin: 0;
  padding: 0.4rem 0.65rem;
  border: 0;
  background: transparent;
  color: var(--text-secondary, #94a3b8);
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  text-align: start;
}

.unit-price-calc__toggle:hover {
  color: var(--text-primary, #e2e8f0);
}

.unit-price-calc.is-open .unit-price-calc__toggle {
  color: var(--primary-color, #0f6e6e);
  border-bottom: 1px solid color-mix(in srgb, var(--primary-color, #0f6e6e) 22%, transparent);
}

.unit-price-calc__toggle-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.15rem;
  height: 1.15rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary-color, #0f6e6e) 18%, transparent);
  color: var(--primary-color, #0f6e6e);
  font-size: 0.72rem;
  font-weight: 700;
  flex-shrink: 0;
}

.unit-price-calc__toggle-text {
  flex: 1;
  min-width: 0;
}

.unit-price-calc__chevron {
  width: 0.4rem;
  height: 0.4rem;
  border-inline-end: 2px solid currentColor;
  border-bottom: 2px solid currentColor;
  transform: rotate(45deg);
  transition: transform 0.15s ease;
  opacity: 0.7;
  flex-shrink: 0;
  margin-inline-end: 0.15rem;
}

.unit-price-calc__chevron.is-open {
  transform: rotate(225deg);
  margin-top: 0.2rem;
}

.unit-price-calc__body {
  padding: 0.55rem 0.65rem 0.6rem;
}

.unit-price-calc__row {
  display: grid;
  grid-template-columns: 1fr 1fr minmax(5.5rem, 0.9fr);
  gap: 0.4rem;
  align-items: end;
}

.unit-price-calc__field {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  margin: 0;
  min-width: 0;
}

.unit-price-calc__field > span {
  font-size: 0.68rem;
  font-weight: 600;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.2;
}

.unit-price-calc__input {
  width: 100%;
  min-height: 2rem;
  padding: 0.28rem 0.5rem;
  border: 1px solid var(--border-color, rgba(148, 163, 184, 0.35));
  border-radius: 8px;
  background: var(--bg-primary, #0f172a);
  color: var(--text-primary, #e2e8f0);
  font-size: 0.85rem;
  font-weight: 600;
  outline: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
  /* hide spinners — cleaner in compact row */
  appearance: textfield;
  -moz-appearance: textfield;
}

.unit-price-calc__input::-webkit-outer-spin-button,
.unit-price-calc__input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.unit-price-calc__input:focus {
  border-color: var(--primary-color, #0f6e6e);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color, #0f6e6e) 18%, transparent);
}

.unit-price-calc__input::placeholder {
  color: var(--text-secondary, #64748b);
  font-weight: 500;
  opacity: 0.85;
}

.unit-price-calc__result {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 0.1rem;
  min-height: 2rem;
  padding: 0.28rem 0.5rem;
  border-radius: 8px;
  border: 1px dashed color-mix(in srgb, var(--border-color, #94a3b8) 55%, transparent);
  background: color-mix(in srgb, var(--bg-primary, #0f172a) 55%, transparent);
}

.unit-price-calc__result.has-value {
  border-style: solid;
  border-color: color-mix(in srgb, var(--primary-color, #0f6e6e) 45%, transparent);
  background: color-mix(in srgb, var(--primary-color, #0f6e6e) 16%, transparent);
}

.unit-price-calc__result-label {
  font-size: 0.62rem;
  font-weight: 600;
  color: var(--text-secondary, #94a3b8);
  line-height: 1.1;
}

.unit-price-calc__result-value {
  font-size: 0.9rem;
  font-weight: 700;
  color: var(--text-primary, #e2e8f0);
  line-height: 1.2;
  font-variant-numeric: tabular-nums;
}

.unit-price-calc__result.has-value .unit-price-calc__result-value {
  color: var(--primary-color, #0f6e6e);
}

.unit-price-calc__clear {
  display: inline-flex;
  margin-top: 0.4rem;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--text-secondary, #94a3b8);
  font-size: 0.72rem;
  font-weight: 600;
  cursor: pointer;
}

.unit-price-calc__clear:hover {
  color: var(--text-primary, #e2e8f0);
  text-decoration: underline;
}

@media (max-width: 560px) {
  .unit-price-calc__row {
    grid-template-columns: 1fr 1fr;
  }

  .unit-price-calc__result {
    grid-column: 1 / -1;
  }
}
</style>
