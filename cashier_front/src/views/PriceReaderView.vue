<template>
  <div class="pr-kiosk" :class="`pr-kiosk--${phase}`" @click="focusScanner">
    <AppHeader />

    <main class="pr-stage">
      <!-- Idle: waiting for barcode -->
      <section v-if="phase === 'idle'" class="pr-panel pr-panel--idle" key="idle">
        <div class="pr-scan-ring" aria-hidden="true">
          <div class="pr-scan-ring__pulse"></div>
          <div class="pr-scan-ring__core">
            <b-icon icon="upc-scan" class="pr-scan-ring__icon"></b-icon>
          </div>
          <div class="pr-scan-beam"></div>
        </div>
        <h1 class="pr-headline">{{ $t("priceReaderWaitingTitle") || "بانتظار المسح" }}</h1>
        <p class="pr-subline">
          {{ $t("priceReaderWaitingHint") || "مرّر الباركود على القارئ لعرض السعر" }}
        </p>
        <div class="pr-idle-badges">
          <span class="pr-idle-badge">
            <b-icon icon="lightning-charge-fill"></b-icon>
            {{ $t("priceReaderReady") || "جاهز للمسح" }}
          </span>
        </div>
      </section>

      <!-- Searching -->
      <section v-else-if="phase === 'searching'" class="pr-panel pr-panel--searching" key="searching">
        <div class="pr-loader" aria-hidden="true">
          <span></span><span></span><span></span>
        </div>
        <h2 class="pr-headline pr-headline--sm">{{ $t("searching") || "جاري البحث..." }}</h2>
        <p class="pr-code-chip" v-if="lastScannedCode">{{ lastScannedCode }}</p>
      </section>

      <!-- Found -->
      <section v-else-if="phase === 'found'" class="pr-panel pr-panel--found" key="found">
        <div class="pr-result-card">
          <div class="pr-result-media">
            <img
              :src="productImageSrc(foundItem.image, foundItem.imageError)"
              :alt="foundItem.name"
              class="pr-result-img"
              :class="{ 'pr-result-img--fallback': isProductImageFallback(foundItem.image, foundItem.imageError) }"
              @error="onProductImageError(foundItem)"
            />
          </div>
          <div class="pr-result-body">
            <p class="pr-result-label">{{ $t("priceFound") || "تم العثور على السعر" }}</p>
            <h2 class="pr-result-name">{{ foundItem.name }}</h2>
            <p v-if="foundItem.code" class="pr-result-code">
              <b-icon icon="upc"></b-icon>
              {{ foundItem.code }}
            </p>

            <div class="pr-price-block">
              <template v-if="hasDiscount">
                <span class="pr-price-old">
                  {{ formatPrice(foundItem.sellingPrice) }}
                  <small>{{ $t("currency") }}</small>
                </span>
                <span class="pr-price-main pr-price-main--sale">
                  {{ formatPrice(foundItem.displayPrice) }}
                  <small>{{ $t("currency") }}</small>
                </span>
                <span class="pr-price-tag">{{ $t("disCountPricePlaceholder") || "بعد الخصم" }}</span>
              </template>
              <template v-else>
                <span class="pr-price-main">
                  {{ formatPrice(foundItem.displayPrice) }}
                  <small>{{ $t("currency") }}</small>
                </span>
              </template>
            </div>
          </div>
        </div>

        <div class="pr-auto-reset" aria-live="polite">
          <div class="pr-auto-reset__track">
            <div class="pr-auto-reset__fill" :style="{ width: resetProgress + '%' }"></div>
          </div>
          <span>
            {{ $t("priceReaderAutoReset") || "العودة لشاشة الانتظار خلال" }}
            {{ resetSecondsLeft }}
            {{ $t("seconds") || "ث" }}
          </span>
        </div>
      </section>

      <!-- Not found -->
      <section v-else class="pr-panel pr-panel--missing" key="missing">
        <div class="pr-missing-icon" aria-hidden="true">
          <b-icon icon="exclamation-triangle-fill"></b-icon>
        </div>
        <h2 class="pr-headline pr-headline--sm">
          {{ $t("priceNotFound") || "لم يتم العثور على السعر" }}
        </h2>
        <p class="pr-subline">
          {{ $t("priceNotFoundDescription") || "تأكد من صحة الباركود وحاول مرة أخرى" }}
        </p>
        <p class="pr-code-chip" v-if="lastScannedCode">{{ lastScannedCode }}</p>
        <div class="pr-auto-reset pr-auto-reset--warn">
          <div class="pr-auto-reset__track">
            <div class="pr-auto-reset__fill" :style="{ width: resetProgress + '%' }"></div>
          </div>
          <span>
            {{ $t("priceReaderAutoReset") || "العودة لشاشة الانتظار خلال" }}
            {{ resetSecondsLeft }}
            {{ $t("seconds") || "ث" }}
          </span>
        </div>
      </section>
    </main>

    <!-- Always-focused ghost input for hardware scanners -->
    <input
      ref="codeNumber"
      v-model="searchCode"
      type="text"
      class="pr-ghost-input"
      autocomplete="off"
      autocapitalize="off"
      spellcheck="false"
      inputmode="none"
      :aria-label="$t('barcode') || 'الباركود'"
      @keydown.enter.prevent="handleBarcodeSearch"
      @input="handleBarcodeInput"
    />
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";
import {
  productImageSrc,
  isProductImageFallback,
  onProductImageError,
} from "@/utils/productImage.js";

const SUCCESS_HOLD_MS = 6500;
const ERROR_HOLD_MS = 4000;
const DEBOUNCE_MS = 180;

export default {
  name: "PriceReaderView",
  components: { AppHeader },
  data() {
    return {
      searchCode: "",
      lastScannedCode: "",
      phase: "idle", // idle | searching | found | missing
      foundItem: {
        name: "",
        code: "",
        image: null,
        imageError: false,
        sellingPrice: 0,
        displayPrice: 0,
        disCountPrice: 0,
      },
      typingTimer: null,
      resetTimer: null,
      resetTickTimer: null,
      resetStartedAt: 0,
      resetDurationMs: SUCCESS_HOLD_MS,
      resetProgress: 100,
      resetSecondsLeft: 0,
      searchAbortController: null,
      isSearching: false,
    };
  },
  computed: {
    hasDiscount() {
      const sell = Number(this.foundItem.sellingPrice) || 0;
      const disc = Number(this.foundItem.disCountPrice) || 0;
      return disc > 0 && disc < sell;
    },
  },
  mounted() {
    document.body.classList.add("price-reader-active");
    this.$nextTick(() => this.focusScanner());
    document.addEventListener("click", this.focusScanner);
    window.addEventListener("focus", this.focusScanner);
  },
  beforeDestroy() {
    document.body.classList.remove("price-reader-active");
    this.clearTypingTimer();
    this.clearResetTimers();
    if (this.searchAbortController) this.searchAbortController.abort();
    document.removeEventListener("click", this.focusScanner);
    window.removeEventListener("focus", this.focusScanner);
  },
  methods: {
    productImageSrc,
    isProductImageFallback,
    onProductImageError,
    focusScanner() {
      const el = this.$refs.codeNumber;
      if (!el) return;
      try {
        el.focus({ preventScroll: true });
      } catch (_) {
        el.focus();
      }
    },
    formatPrice(value) {
      const n = Number(value);
      if (!Number.isFinite(n)) return "0";
      return n.toLocaleString("en-US");
    },
    clearTypingTimer() {
      if (this.typingTimer) {
        clearTimeout(this.typingTimer);
        this.typingTimer = null;
      }
    },
    clearResetTimers() {
      if (this.resetTimer) {
        clearTimeout(this.resetTimer);
        this.resetTimer = null;
      }
      if (this.resetTickTimer) {
        clearInterval(this.resetTickTimer);
        this.resetTickTimer = null;
      }
    },
    goIdle() {
      this.clearResetTimers();
      this.phase = "idle";
      this.searchCode = "";
      this.resetProgress = 100;
      this.resetSecondsLeft = 0;
      this.isSearching = false;
      this.$nextTick(() => this.focusScanner());
    },
    scheduleAutoReset(durationMs) {
      this.clearResetTimers();
      this.resetDurationMs = durationMs;
      this.resetStartedAt = Date.now();
      this.resetProgress = 100;
      this.resetSecondsLeft = Math.ceil(durationMs / 1000);

      this.resetTickTimer = setInterval(() => {
        const elapsed = Date.now() - this.resetStartedAt;
        const left = Math.max(0, durationMs - elapsed);
        this.resetProgress = (left / durationMs) * 100;
        this.resetSecondsLeft = Math.ceil(left / 1000);
      }, 50);

      this.resetTimer = setTimeout(() => {
        this.goIdle();
      }, durationMs);
    },
    handleBarcodeSearch() {
      const code = String(this.searchCode || "").trim();
      if (!code) return;
      this.clearTypingTimer();
      this.SearchByCode(code);
    },
    handleBarcodeInput() {
      this.clearTypingTimer();
      const code = String(this.searchCode || "").trim();
      if (!code) return;

      // While showing a result, a new scan should interrupt immediately after debounce
      this.typingTimer = setTimeout(() => {
        if (String(this.searchCode || "").trim().length >= 3) {
          this.SearchByCode(String(this.searchCode || "").trim());
        }
      }, DEBOUNCE_MS);
    },
    SearchByCode(code) {
      const query = String(code || "").trim();
      if (!query) return;
      if (this.isSearching) return;

      this.clearResetTimers();
      if (this.searchAbortController) {
        this.searchAbortController.abort();
      }

      this.searchAbortController = new AbortController();
      this.isSearching = true;
      this.phase = "searching";
      this.lastScannedCode = query;

      HTTP.get(`Admin/GetItemsByCode?code=${encodeURIComponent(query)}`, {
        signal: this.searchAbortController.signal,
      })
        .then((response) => {
          this.isSearching = false;
          const item = response?.data?.data || response?.data?.Data;
          if (item && (item.sellingPrice != null || item.SellingPrice != null)) {
            const selling = Number(item.sellingPrice ?? item.SellingPrice) || 0;
            const discount = Number(item.disCountPrice ?? item.DisCountPrice) || 0;
            const display =
              discount > 0 && discount < selling ? discount : selling;

            this.foundItem = {
              name: item.name || item.Name || "—",
              code: item.code || item.Code || query,
              image: item.image || item.Image || null,
              imageError: false,
              sellingPrice: selling,
              disCountPrice: discount,
              displayPrice: display,
            };
            this.phase = "found";
            this.searchCode = "";
            this.scheduleAutoReset(SUCCESS_HOLD_MS);
            this.$nextTick(() => this.focusScanner());
          } else {
            this.phase = "missing";
            this.searchCode = "";
            this.scheduleAutoReset(ERROR_HOLD_MS);
            this.$nextTick(() => this.focusScanner());
          }
        })
        .catch((error) => {
          this.isSearching = false;
          if (error?.name === "AbortError" || error?.name === "CanceledError") {
            return;
          }
          this.phase = "missing";
          this.searchCode = "";
          this.scheduleAutoReset(ERROR_HOLD_MS);
          this.$nextTick(() => this.focusScanner());
        });
    },
  },
};
</script>

<style scoped>
.pr-kiosk {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background:
    radial-gradient(1200px 600px at 80% -10%, rgba(15, 110, 110, 0.18), transparent 55%),
    radial-gradient(900px 500px at 0% 100%, rgba(20, 184, 166, 0.1), transparent 50%),
    var(--bg-primary, #0b1220);
  color: var(--text-primary, #e2e8f0);
}

.pr-stage {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(1rem, 3vw, 2.5rem);
  min-height: 0;
}

.pr-panel {
  width: min(960px, 100%);
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 1.1rem;
  animation: pr-fade-up 0.35s ease both;
}

@keyframes pr-fade-up {
  from {
    opacity: 0;
    transform: translateY(14px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.pr-scan-ring {
  position: relative;
  width: min(220px, 42vw);
  height: min(220px, 42vw);
  display: grid;
  place-items: center;
  margin-bottom: 0.5rem;
}

.pr-scan-ring__pulse {
  position: absolute;
  inset: 0;
  border-radius: 50%;
  border: 2px solid rgba(20, 184, 166, 0.35);
  animation: pr-pulse 2.2s ease-out infinite;
}

.pr-scan-ring__core {
  width: 72%;
  height: 72%;
  border-radius: 50%;
  display: grid;
  place-items: center;
  background: linear-gradient(160deg, rgba(15, 110, 110, 0.35), rgba(15, 110, 110, 0.08));
  border: 1px solid rgba(20, 184, 166, 0.4);
  box-shadow: 0 18px 50px rgba(15, 110, 110, 0.25);
}

.pr-scan-ring__icon {
  font-size: clamp(2.8rem, 8vw, 4rem);
  color: #14b8a6;
}

.pr-scan-beam {
  position: absolute;
  left: 18%;
  right: 18%;
  height: 3px;
  border-radius: 999px;
  background: linear-gradient(90deg, transparent, #5eead4, transparent);
  box-shadow: 0 0 16px rgba(94, 234, 212, 0.8);
  animation: pr-beam 2s ease-in-out infinite;
}

@keyframes pr-pulse {
  0% {
    transform: scale(0.85);
    opacity: 0.9;
  }
  100% {
    transform: scale(1.2);
    opacity: 0;
  }
}

@keyframes pr-beam {
  0%,
  100% {
    top: 28%;
  }
  50% {
    top: 68%;
  }
}

.pr-headline {
  margin: 0;
  font-size: clamp(1.8rem, 4.5vw, 2.8rem);
  font-weight: 800;
  letter-spacing: -0.02em;
  color: var(--text-primary, #f8fafc);
}

.pr-headline--sm {
  font-size: clamp(1.4rem, 3.5vw, 2rem);
}

.pr-subline {
  margin: 0;
  max-width: 28rem;
  font-size: clamp(1rem, 2.2vw, 1.25rem);
  color: var(--text-secondary, #94a3b8);
  line-height: 1.55;
}

.pr-idle-badges {
  display: flex;
  gap: 0.6rem;
  margin-top: 0.35rem;
}

.pr-idle-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.45rem 0.85rem;
  border-radius: 999px;
  font-size: 0.9rem;
  font-weight: 700;
  color: #5eead4;
  background: rgba(15, 110, 110, 0.2);
  border: 1px solid rgba(20, 184, 166, 0.35);
}

.pr-loader {
  display: flex;
  gap: 0.45rem;
  margin-bottom: 0.5rem;
}

.pr-loader span {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
  background: #14b8a6;
  animation: pr-bounce 0.9s ease-in-out infinite;
}

.pr-loader span:nth-child(2) {
  animation-delay: 0.15s;
}
.pr-loader span:nth-child(3) {
  animation-delay: 0.3s;
}

@keyframes pr-bounce {
  0%,
  80%,
  100% {
    transform: translateY(0);
    opacity: 0.4;
  }
  40% {
    transform: translateY(-10px);
    opacity: 1;
  }
}

.pr-code-chip {
  margin: 0;
  padding: 0.4rem 0.9rem;
  border-radius: 999px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 1rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  color: var(--text-primary, #e2e8f0);
  background: rgba(148, 163, 184, 0.12);
  border: 1px solid rgba(148, 163, 184, 0.25);
}

.pr-result-card {
  width: 100%;
  display: grid;
  grid-template-columns: minmax(140px, 240px) 1fr;
  gap: clamp(1rem, 3vw, 2rem);
  align-items: center;
  padding: clamp(1.1rem, 2.5vw, 1.75rem);
  border-radius: 1.5rem;
  background: linear-gradient(
    155deg,
    rgba(15, 110, 110, 0.2) 0%,
    rgba(15, 23, 42, 0.55) 55%,
    rgba(15, 23, 42, 0.35) 100%
  );
  border: 1px solid rgba(20, 184, 166, 0.35);
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.28);
  text-align: start;
}

.pr-result-media {
  aspect-ratio: 1;
  border-radius: 1.15rem;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(148, 163, 184, 0.2);
}

.pr-result-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.pr-result-img--fallback {
  object-fit: contain;
  padding: 12%;
  background: rgba(255, 255, 255, 0.9);
}

.pr-result-body {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  min-width: 0;
}

.pr-result-label {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 700;
  color: #5eead4;
}

.pr-result-name {
  margin: 0;
  font-size: clamp(1.5rem, 3.8vw, 2.4rem);
  font-weight: 800;
  line-height: 1.25;
  color: var(--text-primary, #f8fafc);
  word-break: break-word;
}

.pr-result-code {
  margin: 0;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  color: var(--text-secondary, #94a3b8);
  font-weight: 600;
  font-size: 1rem;
}

.pr-price-block {
  margin-top: 0.65rem;
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.55rem 0.85rem;
}

.pr-price-main {
  font-size: clamp(2.4rem, 7vw, 4.2rem);
  font-weight: 900;
  line-height: 1;
  color: #f8fafc;
  letter-spacing: -0.03em;
}

.pr-price-main small {
  font-size: 0.38em;
  font-weight: 700;
  margin-inline-start: 0.2em;
  color: #94a3b8;
}

.pr-price-main--sale {
  color: #5eead4;
}

.pr-price-old {
  font-size: clamp(1.1rem, 2.5vw, 1.5rem);
  font-weight: 700;
  color: #94a3b8;
  text-decoration: line-through;
}

.pr-price-old small {
  font-size: 0.75em;
  margin-inline-start: 0.15em;
}

.pr-price-tag {
  padding: 0.25rem 0.65rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 800;
  color: #0f766e;
  background: rgba(94, 234, 212, 0.9);
}

.pr-missing-icon {
  width: 96px;
  height: 96px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-size: 2.5rem;
  color: #fbbf24;
  background: rgba(251, 191, 36, 0.12);
  border: 1px solid rgba(251, 191, 36, 0.35);
}

.pr-auto-reset {
  width: min(420px, 100%);
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  margin-top: 0.35rem;
  color: var(--text-secondary, #94a3b8);
  font-size: 0.92rem;
  font-weight: 600;
}

.pr-auto-reset__track {
  height: 6px;
  border-radius: 999px;
  overflow: hidden;
  background: rgba(148, 163, 184, 0.2);
}

.pr-auto-reset__fill {
  height: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, #0f6e6e, #14b8a6);
  transition: width 0.05s linear;
}

.pr-auto-reset--warn .pr-auto-reset__fill {
  background: linear-gradient(90deg, #b45309, #f59e0b);
}

.pr-ghost-input {
  position: fixed;
  opacity: 0;
  pointer-events: none;
  width: 1px;
  height: 1px;
  border: 0;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
}

@media (max-width: 720px) {
  .pr-result-card {
    grid-template-columns: 1fr;
    text-align: center;
  }

  .pr-result-media {
    width: min(220px, 60vw);
    margin-inline: auto;
  }

  .pr-result-body {
    align-items: center;
  }

  .pr-price-block {
    justify-content: center;
  }
}
</style>

<style>
/* Keep page from scrolling under the kiosk */
body.price-reader-active {
  overflow: hidden;
}

/* Light theme readability */
:root.light-theme .pr-kiosk,
.light-theme .pr-kiosk {
  background:
    radial-gradient(1000px 520px at 85% -10%, rgba(15, 110, 110, 0.12), transparent 55%),
    radial-gradient(800px 420px at 0% 100%, rgba(15, 110, 110, 0.08), transparent 50%),
    #f3f6f8;
}

:root.light-theme .pr-headline,
.light-theme .pr-headline,
:root.light-theme .pr-result-name,
.light-theme .pr-result-name,
:root.light-theme .pr-price-main,
.light-theme .pr-price-main {
  color: #0f172a;
}

:root.light-theme .pr-result-card,
.light-theme .pr-result-card {
  background: linear-gradient(155deg, #ffffff 0%, #f0fdfa 100%);
  border-color: rgba(15, 110, 110, 0.22);
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.08);
}
</style>
