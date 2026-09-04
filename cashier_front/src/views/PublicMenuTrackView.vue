<template>
  <div class="pt">
    <header class="pt-header">
      <button type="button" class="pt-back" :aria-label="$t('backToMenu') || 'العودة للمنيو'" @click="goMenu">
        <svg class="pt-back-icon" viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path fill="currentColor" d="M15.4 4.8 8.2 12l7.2 7.2 1.7-1.7L11.6 12l5.5-5.5z" />
        </svg>
      </button>
      <div class="pt-brand">
        <img v-if="logoSrc" :src="logoSrc" alt="" class="pt-avatar" @error="logoError = true" />
        <div v-else class="pt-avatar pt-avatar--fallback">{{ storeInitial }}</div>
        <div class="pt-brand-text">
          <p class="pt-kicker">{{ $t("trackOrder") || "تتبع الطلب" }}</p>
          <h1>{{ storeName || ($t("publicMenu") || "المنيو الإلكتروني") }}</h1>
        </div>
      </div>
    </header>

    <div class="pt-scroll">
    <div v-if="loading" class="pt-skel">
      <div class="pt-skel-card"></div>
      <div class="pt-skel-card pt-skel-card--line"></div>
      <div class="pt-skel-card pt-skel-card--line"></div>
    </div>

    <form v-else-if="!order" class="pt-lookup" novalidate @submit.prevent="lookup">
      <p class="pt-lead">{{ $t("trackOrderHint") || "أدخل رقم الطلب ورقم الهاتف المستخدم عند الطلب." }}</p>
      <label>
        {{ $t("orderCode") || "رقم الطلب" }}
        <input
          v-model.trim="formCode"
          type="text"
          inputmode="numeric"
          dir="ltr"
          maxlength="20"
          autocomplete="off"
          :placeholder="$t('orderCode') || 'رقم الطلب'"
        />
      </label>
      <label>
        {{ $t("phoneNumber") || "الهاتف" }}
        <input
          :value="formPhone"
          type="tel"
          inputmode="numeric"
          dir="ltr"
          maxlength="11"
          autocomplete="tel"
          class="pt-phone"
          :placeholder="$t('phonePlaceholder') || '078xxxxxxx'"
          @input="onPhoneInput"
        />
        <span class="pt-hint">{{ $t("iraqiPhoneHint") }}</span>
      </label>
      <p v-if="error" class="pt-error">{{ error }}</p>
      <button type="submit" class="pt-btn pt-btn--primary" :disabled="submitting">
        {{ submitting ? ($t("sending") || "جاري البحث...") : ($t("trackOrder") || "تتبع الطلب") }}
      </button>
    </form>

    <section v-else class="pt-result">
      <div class="pt-status-card">
        <span class="pt-status" :class="statusClass">{{ statusLabel }}</span>
        <p class="pt-label">{{ $t("orderCode") || "رقم الطلب" }}</p>
        <div class="pt-code-row">
          <h2>{{ order.orderCode }}</h2>
          <button
            type="button"
            class="pt-copy"
            :aria-label="$t('copyOrderCode') || 'نسخ رقم الطلب'"
            @click="copyOrderCode"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
              <path
                fill="currentColor"
                d="M16 1H4c-1.1 0-2 .9-2 2v12h2V3h12V1zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm0 16H8V7h11v14z"
              />
            </svg>
            {{ codeCopied ? ($t("orderCodeCopied") || "تم النسخ") : ($t("copyOrderCode") || "نسخ") }}
          </button>
        </div>
        <p v-if="order.insertDate" class="pt-meta">{{ formatDate(order.insertDate) }}</p>
        <p class="pt-meta">{{ $t("orderSentHint") || "ادفع في المحل عند الاستلام. احتفظ برقم الطلب." }}</p>
      </div>

      <div class="pt-section-head">
        <h3>{{ $t("orderItems") || "المواد" }}</h3>
        <span>{{ order.items.length }}</span>
      </div>
      <ul class="pt-items">
        <li v-for="item in order.items" :key="item.id">
          <div class="pt-item-media">
            <img :src="itemImage(item)" :alt="item.name" loading="lazy" @error="onItemImageError(item)" />
          </div>
          <div class="pt-item-body">
            <h4>{{ item.name }}</h4>
            <p>
              {{ item.quantity }} × {{ formatMenuPrice(item.unitPrice) }} {{ $t("currency") }}
            </p>
          </div>
          <strong class="pt-item-total">
            {{ formatMenuPrice(item.total) }} {{ $t("currency") }}
          </strong>
        </li>
      </ul>

      <div class="pt-total">
        <span>{{ $t("cartTotal") || "المجموع الكلي" }}</span>
        <strong>{{ formatMenuPrice(order.total) }} {{ $t("currency") }}</strong>
      </div>

      <p v-if="error" class="pt-error">{{ error }}</p>
      <button type="button" class="pt-btn pt-btn--ghost" :disabled="submitting" @click="lookup">
        {{ $t("refreshStatus") || "تحديث الحالة" }}
      </button>
      <button type="button" class="pt-btn pt-btn--ghost" @click="resetLookup">
        {{ $t("trackAnotherOrder") || "تتبع طلب آخر" }}
      </button>
    </section>
    </div>

    <nav class="pt-tabbar" aria-label="menu">
      <button type="button" class="pt-tab" @click="goMenu">
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path fill="currentColor" d="M12 3.2 4 10v10h5.5v-6h5V20H20V10z" />
        </svg>
        <span>{{ $t("home") }}</span>
      </button>
      <button type="button" class="pt-tab pt-tab--on">
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path
            fill="currentColor"
            d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm1 15h-2v-2h2zm0-4h-2V7h2z"
          />
        </svg>
        <span>{{ $t("trackOrder") || "تتبع" }}</span>
      </button>
      <button type="button" class="pt-tab" @click="goMenu">
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path
            fill="currentColor"
            d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2m10 0c-1.1 0-1.99.9-1.99 2S15.9 22 17 22s2-.9 2-2-.9-2-2-2M7.2 14.6l.1-.6h9.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A1 1 0 0 0 21.2 5H6.21L5.27 3H2v2h2l3.6 7.59-1.35 2.44C5.52 16.37 6.48 18 8 18h12v-2H8z"
          />
        </svg>
        <span>{{ $t("cart") }}</span>
      </button>
    </nav>
  </div>
</template>

<script>
import { publicHttp } from "@/http/publicHttp.js";
import { BUILTIN_DEFAULT_PRODUCT_IMAGE } from "@/utils/productImage.js";
import {
  formatMenuPrice,
  normalizeIraqiPhone,
  isValidIraqiPhone,
  saveLastPublicOrder,
  loadLastPublicOrder,
} from "@/utils/publicMenu.js";

export default {
  name: "PublicMenuTrackView",
  data() {
    return {
      loading: false,
      submitting: false,
      error: "",
      storeName: "",
      logo: "",
      logoError: false,
      formCode: "",
      formPhone: "",
      order: null,
      codeCopied: false,
    };
  },
  computed: {
    commercialUserId() {
      return Number(this.$route.params.commercialUserId);
    },
    logoSrc() {
      return this.logoError ? "" : this.logo;
    },
    storeInitial() {
      return (this.storeName || "M").trim().charAt(0);
    },
    statusLabel() {
      const status = String((this.order && this.order.status) || "").toLowerCase();
      if (status === "approved") return this.$t("approved") || "موافق عليها";
      if (status === "cancelled") return this.$t("cancelled") || "ملغي";
      return this.$t("pending") || "قيد الانتظار";
    },
    statusClass() {
      const status = String((this.order && this.order.status) || "").toLowerCase();
      if (status === "approved") return "pt-status--ok";
      if (status === "cancelled") return "pt-status--bad";
      return "pt-status--wait";
    },
  },
  watch: {
    "$route.params.orderCode"() {
      this.prefillFromRoute();
      if (this.formCode && isValidIraqiPhone(this.formPhone)) this.lookup();
    },
  },
  mounted() {
    document.documentElement.classList.add("public-menu-page");
    document.body.classList.add("public-menu-page");
    this.prefillFromRoute();
    if (this.formCode && isValidIraqiPhone(this.formPhone)) {
      this.lookup();
    }
  },
  beforeDestroy() {
    if (this._copyTimer) clearTimeout(this._copyTimer);
    document.documentElement.classList.remove("public-menu-page");
    document.body.classList.remove("public-menu-page");
  },
  methods: {
    formatMenuPrice,
    prefillFromRoute() {
      const saved = loadLastPublicOrder(this.commercialUserId) || {};
      this.formCode = String(this.$route.params.orderCode || saved.orderCode || "");
      this.formPhone = normalizeIraqiPhone(saved.phone || this.formPhone);
    },
    onPhoneInput(event) {
      this.formPhone = String(event && event.target && event.target.value ? event.target.value : "")
        .replace(/\D/g, "")
        .slice(0, 11);
      this.error = "";
    },
    itemImage(item) {
      if (item.image && !item.imageError) return item.image;
      return BUILTIN_DEFAULT_PRODUCT_IMAGE;
    },
    onItemImageError(item) {
      this.$set(item, "imageError", true);
    },
    formatDate(value) {
      const d = new Date(value);
      if (Number.isNaN(d.getTime())) return "";
      return d.toLocaleString("en-GB", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    },
    goMenu() {
      this.$router.push({
        name: "publicMenu",
        params: { commercialUserId: String(this.commercialUserId) },
      });
    },
    resetLookup() {
      this.order = null;
      this.error = "";
      this.formCode = "";
    },
    async lookup() {
      this.error = "";
      const code = String(this.formCode || "").trim();
      const phone = normalizeIraqiPhone(this.formPhone);
      if (!code || !isValidIraqiPhone(phone)) {
        this.error = this.$t("orderTrackRequired") || "أدخل رقم الطلب ورقم الهاتف الصحيح";
        this.order = null;
        return;
      }
      this.submitting = true;
      this.loading = !this.order;
      try {
        const res = await publicHttp.get(`PublicMenu/${this.commercialUserId}/track`, {
          params: { code, phone },
        });
        if (res.data && res.data.errorStatus) {
          throw new Error(res.data.message || "failed");
        }
        const data = (res.data && res.data.data) || {};
        this.storeName = data.storeName || data.StoreName || this.storeName;
        this.logo = data.logo || data.Logo || "";
        this.logoError = false;
        this.order = this.mapOrder(data.order || data.Order);
        saveLastPublicOrder({
          commercialUserId: this.commercialUserId,
          orderCode: code,
          phone,
        });
      } catch (err) {
        const msg = (err && err.response && err.response.data && err.response.data.message) || err.message || "";
        this.error = this.mapError(msg);
        if (!this.order) this.order = null;
      } finally {
        this.submitting = false;
        this.loading = false;
      }
    },
    mapOrder(raw) {
      const src = raw || {};
      const lines = src.items || src.Items || [];
      const items = lines.map((line, index) => {
        const quantity = Number(line.quantity != null ? line.quantity : line.Quantity) || 0;
        const unitPrice = Number(line.sellingPrice != null ? line.sellingPrice : line.SellingPrice) || 0;
        const rawTotal = line.total != null ? line.total : line.Total;
        const total = Number(rawTotal);
        return {
          id: line.id != null ? line.id : (line.Id != null ? line.Id : index),
          name: line.name || line.Name || "",
          image: line.image || line.Image || "",
          imageError: false,
          quantity,
          unitPrice,
          total: Number.isFinite(total) ? total : unitPrice * quantity,
        };
      });
      const afterDiscount = Number(
        src.orderTotalAfterDiscount != null ? src.orderTotalAfterDiscount : src.OrderTotalAfterDiscount
      );
      const subTotal = Number(src.orderSubTotal != null ? src.orderSubTotal : src.OrderSubTotal);
      const total = afterDiscount || subTotal || items.reduce((sum, line) => sum + line.total, 0);
      return {
        orderCode: src.orderCode || src.OrderCode || this.formCode,
        status: src.orderStatus || src.OrderStatus || "Pending",
        insertDate: src.insertDate || src.InsertDate || "",
        total,
        items,
      };
    },
    mapError(msg) {
      if (msg === "orderNotFound") return this.$t("orderNotFound") || "لم يتم العثور على الطلب";
      if (msg === "orderTrackRequired") {
        return this.$t("orderTrackRequired") || "أدخل رقم الطلب ورقم الهاتف الصحيح";
      }
      if (msg === "storeNotFound") return this.$t("storeNotFound") || "المتجر غير موجود";
      return this.$t("orderTrackFailed") || "تعذر تتبع الطلب";
    },
    copyOrderCode() {
      const code = String((this.order && this.order.orderCode) || "");
      if (!code) return;
      const done = () => {
        this.codeCopied = true;
        if (this._copyTimer) clearTimeout(this._copyTimer);
        this._copyTimer = setTimeout(() => {
          this.codeCopied = false;
        }, 2000);
      };
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(code).then(done).catch(() => this.fallbackCopy(code, done));
        return;
      }
      this.fallbackCopy(code, done);
    },
    fallbackCopy(text, done) {
      try {
        const el = document.createElement("textarea");
        el.value = text;
        el.setAttribute("readonly", "");
        el.style.position = "fixed";
        el.style.opacity = "0";
        document.body.appendChild(el);
        el.select();
        document.execCommand("copy");
        document.body.removeChild(el);
        done();
      } catch {
        /* ignore */
      }
    },
  },
};
</script>

<style>
:root.dark-theme.public-menu-page,
:root.light-theme.public-menu-page,
html.public-menu-page,
html.public-menu-page.dark-theme,
html.public-menu-page.light-theme,
body.public-menu-page {
  --text-primary: #1c1917;
  --text-secondary: #44403c;
  --text-muted: #78716c;
  --bg-primary: #ffffff;
  --bg-secondary: #fef9f3;
  background: #fef9f3 !important;
  color: #1c1917 !important;
  min-height: 100%;
  height: 100%;
  height: 100dvh;
  overflow: hidden;
  overscroll-behavior: none;
}
body.public-menu-page #app {
  background: #fef9f3;
  min-height: 100%;
  height: 100%;
  height: 100dvh;
  overflow: hidden;
  color: #1c1917;
}
html.public-menu-page h1,
html.public-menu-page h2,
html.public-menu-page h3,
html.public-menu-page h4,
html.public-menu-page h5,
html.public-menu-page h6 {
  color: #1c1917 !important;
  -webkit-text-fill-color: #1c1917 !important;
  background: none !important;
}
</style>

<style scoped>
.pt {
  height: 100%;
  height: 100dvh;
  max-width: 520px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  font-family: Cairo, "IBM Plex Sans Arabic", system-ui, sans-serif;
  color: #1c1917;
}
.pt-header {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 0 0 auto;
  padding: max(8px, env(safe-area-inset-top)) 16px 16px;
}
.pt-scroll {
  flex: 1 1 auto;
  min-height: 0;
  overflow-x: hidden;
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior: contain;
  padding: 0 16px 16px;
}
.pt-back {
  width: 44px;
  height: 44px;
  border: 0;
  border-radius: 50%;
  background: #fff;
  color: #1c1917;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.08);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
}
.pt-back-icon {
  transform: scaleX(-1);
}
[dir="ltr"] .pt-back-icon {
  transform: none;
}
.pt-brand {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}
.pt-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  object-fit: cover;
  background: #fff;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.08);
}
.pt-avatar--fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  background: #ff9f1c;
  color: #fff;
  font-weight: 800;
  font-size: 20px;
}
.pt-kicker {
  margin: 0;
  color: #78716c;
  font-size: 13px;
}
.pt-brand-text h1 {
  margin: 2px 0 0;
  font-size: 18px;
  line-height: 1.3;
  color: #1c1917;
  -webkit-text-fill-color: #1c1917;
}
.pt-lead {
  margin: 0 0 20px;
  color: #78716c;
  line-height: 1.6;
}
.pt-lookup label,
.pt-result {
  display: block;
}
.pt-lookup label {
  margin-bottom: 16px;
  font-size: 13px;
  color: #78716c;
}
.pt-lookup input {
  width: 100%;
  margin-top: 8px;
  border: 1px solid #e7e5e4;
  border-radius: 16px;
  min-height: 48px;
  padding: 12px 14px;
  font-size: 16px;
  background: #fff;
}
.pt-lookup input:focus,
.pt-copy:focus,
.pt-btn:focus,
.pt-back:focus,
.pt-tab:focus {
  outline: 2px solid #ffd199;
  outline-offset: 2px;
}
.pt-phone {
  letter-spacing: 0.04em;
  font-variant-numeric: tabular-nums;
}
.pt-hint {
  display: block;
  margin-top: 6px;
  font-size: 12px;
  color: #a8a29e;
}
.pt-error {
  color: #ef4444;
  margin: 0 0 12px;
  font-weight: 700;
}
.pt-btn {
  width: 100%;
  height: 48px;
  border: 0;
  border-radius: 999px;
  font-weight: 800;
  font-size: 16px;
  margin-bottom: 12px;
}
.pt-btn--primary {
  background: #ff9f1c;
  color: #fff;
}
.pt-btn--primary:disabled,
.pt-btn--ghost:disabled {
  opacity: 0.6;
}
.pt-btn--ghost {
  background: #f5f0ea;
  color: #1c1917;
}
.pt-status-card {
  background: #fff;
  border-radius: 24px;
  padding: 24px 20px;
  text-align: center;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.08);
  margin-bottom: 24px;
}
.pt-status {
  display: inline-flex;
  align-items: center;
  height: 32px;
  padding: 0 12px;
  border-radius: 999px;
  font-size: 13px;
  font-weight: 800;
  margin-bottom: 16px;
}
.pt-status--wait {
  background: #fff7ed;
  color: #c2410c;
}
.pt-status--ok {
  background: #dcfce7;
  color: #166534;
}
.pt-status--bad {
  background: #fee2e2;
  color: #b91c1c;
}
.pt-label {
  margin: 0;
  color: #78716c;
  font-size: 13px;
}
.pt-code-row {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  gap: 8px;
  margin: 8px 0 12px;
}
.pt-code-row h2 {
  margin: 0;
  font-size: 28px;
  letter-spacing: 1px;
  font-variant-numeric: tabular-nums;
  color: #1c1917;
}
.pt-copy {
  height: 40px;
  padding: 0 12px;
  border: 0;
  border-radius: 12px;
  background: #fff7ed;
  color: #c2410c;
  font-weight: 800;
  font-size: 13px;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.pt-meta {
  margin: 0 0 8px;
  color: #78716c;
  font-size: 13px;
  line-height: 1.6;
}
.pt-section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.pt-section-head h3 {
  margin: 0;
  font-size: 18px;
  color: #1c1917;
}
.pt-section-head span {
  color: #78716c;
  font-weight: 700;
}
.pt-items {
  list-style: none;
  margin: 0 0 16px;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.pt-items li {
  display: flex;
  align-items: center;
  gap: 12px;
  background: #fff;
  border-radius: 20px;
  padding: 12px;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.08);
}
.pt-item-media {
  width: 72px;
  height: 72px;
  border-radius: 16px;
  overflow: hidden;
  background: #f5f0ea;
  flex: 0 0 auto;
}
.pt-item-media img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center;
  display: block;
}
.pt-item-body {
  min-width: 0;
  flex: 1;
}
.pt-item-body h4 {
  margin: 0;
  font-size: 15px;
  line-height: 1.4;
  color: #1c1917;
}
.pt-item-body p {
  margin: 4px 0 0;
  color: #78716c;
  font-size: 13px;
}
.pt-item-total {
  font-size: 14px;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}
.pt-total {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px;
  margin-bottom: 20px;
  background: #fff7ed;
  border: 1px solid #ffd199;
  border-radius: 16px;
  font-size: 16px;
  font-weight: 700;
}
.pt-total strong {
  font-size: 20px;
  color: #c2410c;
  font-variant-numeric: tabular-nums;
}
.pt-skel-card {
  height: 168px;
  border-radius: 24px;
  margin-bottom: 12px;
  background: linear-gradient(90deg, #f5e6d6 25%, #fef9f3 50%, #f5e6d6 75%);
  background-size: 200% 100%;
  animation: pt-shimmer 1.2s infinite;
}
.pt-skel-card--line {
  height: 88px;
}
@keyframes pt-shimmer {
  0% { background-position: 100% 0; }
  100% { background-position: -100% 0; }
}
.pt-tabbar {
  position: relative;
  z-index: 30;
  flex: 0 0 auto;
  width: 100%;
  min-height: 64px;
  background: #fff;
  display: flex;
  justify-content: space-around;
  align-items: center;
  box-shadow: 0 -4px 16px rgba(28, 25, 23, 0.06);
  padding: 6px 8px max(8px, env(safe-area-inset-bottom));
}
.pt-tab {
  border: 0;
  background: transparent;
  color: #a8a29e;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  font-weight: 700;
  min-width: 64px;
  min-height: 48px;
}
.pt-tab--on {
  color: #ff9f1c;
}
@media (min-width: 600px) {
  .pt {
    max-width: 720px;
  }
  .pt-header {
    padding-top: 16px;
  }
}
</style>
