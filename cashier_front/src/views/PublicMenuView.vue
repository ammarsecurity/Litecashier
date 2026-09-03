<template>
  <div class="pm" :class="{ 'pm--cart-open': cartOpen }">
    <header class="pm-header">
      <div class="pm-brand">
        <img
          v-if="logoSrc"
          :src="logoSrc"
          alt=""
          class="pm-logo"
          @error="logoError = true"
        />
        <div v-else class="pm-logo pm-logo--fallback">{{ storeInitial }}</div>
        <div class="pm-brand-text">
          <h1 class="pm-title">{{ storeName }}</h1>
          <p class="pm-subtitle">{{ $t("publicMenuTagline") || "اطلب من المنيو واستلم من المحل" }}</p>
        </div>
      </div>
      <div class="pm-search-wrap">
        <span class="pm-search-icon" aria-hidden="true">⌕</span>
        <input
          v-model.trim="search"
          type="search"
          class="pm-search"
          :placeholder="$t('searchItems') || 'ابحث عن صنف...'"
        />
      </div>
    </header>

    <div v-if="loading" class="pm-skel">
      <div v-for="n in 6" :key="n" class="pm-skel-card"></div>
    </div>

    <div v-else-if="error" class="pm-state">
      <p>{{ error }}</p>
      <button type="button" class="pm-btn pm-btn--primary" @click="loadAll">
        {{ $t("retry") || "إعادة المحاولة" }}
      </button>
    </div>

    <div v-else-if="!items.length" class="pm-state">
      <p>{{ $t("noItemsFound") || "لا توجد مواد لعرضها." }}</p>
    </div>

    <template v-else>
      <nav v-if="categories.length" class="pm-cats" aria-label="categories">
        <button
          type="button"
          class="pm-chip"
          :class="{ 'pm-chip--on': !activeCategory }"
          @click="activeCategory = ''"
        >
          {{ $t("allCategories") || "جميع الأقسام" }}
        </button>
        <button
          v-for="cat in categories"
          :key="cat"
          type="button"
          class="pm-chip"
          :class="{ 'pm-chip--on': activeCategory === cat }"
          @click="activeCategory = cat"
        >
          {{ cat }}
        </button>
      </nav>

      <main class="pm-grid">
        <article
          v-for="item in visibleItems"
          :key="item.id"
          class="pm-card"
          :class="{ 'pm-card--off': !item.isAvailable }"
        >
          <div class="pm-card-media">
            <img
              :src="itemImage(item)"
              :alt="item.name"
              loading="lazy"
              @error="onProductImageError(item)"
            />
            <span v-if="!item.isAvailable" class="pm-soldout">
              {{ $t("soldOut") || "نفد" }}
            </span>
          </div>
          <div class="pm-card-body">
            <h2 class="pm-card-name">{{ item.name }}</h2>
            <p v-if="item.description" class="pm-card-desc">{{ item.description }}</p>
            <div class="pm-card-row">
              <div class="pm-price">
                <strong>{{ formatMenuPrice(itemUnitPrice(item)) }}</strong>
                <span>{{ $t("currency") }}</span>
                <s v-if="item.discountPrice">{{ formatMenuPrice(item.sellingPrice) }}</s>
              </div>
              <div v-if="qtyInCart(item.id)" class="pm-stepper">
                <button type="button" class="pm-stepper-btn" @click="changeQty(item, -1)">−</button>
                <span>{{ qtyInCart(item.id) }}</span>
                <button
                  type="button"
                  class="pm-stepper-btn"
                  :disabled="!item.isAvailable"
                  @click="changeQty(item, 1)"
                >
                  +
                </button>
              </div>
              <button
                v-else
                type="button"
                class="pm-add"
                :disabled="!item.isAvailable"
                @click="changeQty(item, 1)"
              >
                {{ $t("add") || "إضافة" }}
              </button>
            </div>
          </div>
        </article>
      </main>

      <p v-if="!visibleItems.length" class="pm-state pm-state--inline">
        {{ $t("noItemsFound") || "لا توجد مواد لعرضها." }}
      </p>
    </template>

    <button
      v-if="cartCount"
      type="button"
      class="pm-cartbar"
      @click="cartOpen = true"
    >
      <span class="pm-cartbar-count">{{ cartCount }}</span>
      <span>{{ $t("viewCart") || "عرض السلة" }}</span>
      <strong>{{ formatMenuPrice(cartTotal) }} {{ $t("currency") }}</strong>
    </button>

    <div v-if="cartOpen" class="pm-sheet-backdrop" @click="cartOpen = false"></div>
    <aside v-if="cartOpen" class="pm-sheet" role="dialog" aria-modal="true">
      <header class="pm-sheet-head">
        <h2>{{ $t("cart") || "السلة" }}</h2>
        <button type="button" class="pm-icon-btn" @click="cartOpen = false">✕</button>
      </header>
      <div class="pm-sheet-body">
        <ul class="pm-lines">
          <li v-for="line in cart" :key="line.id">
            <div>
              <strong>{{ line.name }}</strong>
              <p>{{ formatMenuPrice(line.unitPrice) }} {{ $t("currency") }}</p>
            </div>
            <div class="pm-stepper">
              <button type="button" class="pm-stepper-btn" @click="changeQty(line, -1)">−</button>
              <span>{{ line.quantity }}</span>
              <button type="button" class="pm-stepper-btn" @click="changeQty(line, 1)">+</button>
            </div>
          </li>
        </ul>

        <form class="pm-form" @submit.prevent="submitOrder">
          <label>
            {{ $t("customerName") || "اسم الزبون" }}
            <input v-model.trim="customerName" type="text" required maxlength="120" />
          </label>
          <label>
            {{ $t("phoneNumber") || "الهاتف" }}
            <div class="pm-phone">
              <span>+964</span>
              <input
                v-model.trim="customerPhone"
                type="tel"
                inputmode="numeric"
                dir="ltr"
                required
                maxlength="15"
                :placeholder="$t('phonePlaceholder') || '7xx xxx xxxx'"
              />
            </div>
          </label>
          <label>
            {{ $t("notes") || "ملاحظات" }}
            <textarea v-model.trim="notes" rows="2" maxlength="1000"></textarea>
          </label>
          <p v-if="submitError" class="pm-error">{{ submitError }}</p>
          <button type="submit" class="pm-btn pm-btn--primary pm-btn--block" :disabled="submitting">
            {{ submitting ? ($t("sending") || "جاري الإرسال...") : ($t("placeOrder") || "إرسال الطلب") }}
            · {{ formatMenuPrice(cartTotal) }} {{ $t("currency") }}
          </button>
        </form>
      </div>
    </aside>

    <div v-if="successOrder" class="pm-success">
      <div class="pm-success-card">
        <p class="pm-success-kicker">{{ $t("orderSent") || "تم إرسال طلبك" }}</p>
        <h2>{{ successOrder.orderCode }}</h2>
        <p>{{ $t("orderSentHint") || "ادفع في المحل عند الاستلام. احتفظ برقم الطلب." }}</p>
        <button type="button" class="pm-btn pm-btn--primary" @click="resetAfterSuccess">
          {{ $t("newOrder") || "طلب جديد" }}
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import { publicHttp } from "@/http/publicHttp.js";
import { formatMenuPrice, itemUnitPrice } from "@/utils/publicMenu.js";
import { BUILTIN_DEFAULT_PRODUCT_IMAGE, onProductImageError } from "@/utils/productImage.js";

export default {
  name: "PublicMenuView",
  data() {
    return {
      loading: true,
      error: "",
      storeName: "",
      logo: "",
      logoError: false,
      items: [],
      categories: [],
      search: "",
      activeCategory: "",
      cart: [],
      cartOpen: false,
      customerName: "",
      customerPhone: "",
      notes: "",
      submitting: false,
      submitError: "",
      successOrder: null,
      defaultProductImage: "",
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
    visibleItems() {
      const q = this.search.toLowerCase();
      return this.items.filter((item) => {
        if (this.activeCategory && item.tags !== this.activeCategory) return false;
        if (!q) return true;
        return `${item.name} ${item.description || ""}`.toLowerCase().includes(q);
      });
    },
    cartCount() {
      return this.cart.reduce((sum, line) => sum + line.quantity, 0);
    },
    cartTotal() {
      return this.cart.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0);
    },
  },
  mounted() {
    document.documentElement.classList.add("public-menu-page");
    document.body.classList.add("public-menu-page");
    this.loadAll();
  },
  beforeDestroy() {
    document.documentElement.classList.remove("public-menu-page");
    document.body.classList.remove("public-menu-page");
  },
  methods: {
    formatMenuPrice,
    itemUnitPrice,
    onProductImageError,
    itemImage(item) {
      if (item.image && !item.imageError) return item.image;
      return this.defaultProductImage || BUILTIN_DEFAULT_PRODUCT_IMAGE;
    },
    qtyInCart(id) {
      return this.cart.find((l) => l.id === id)?.quantity || 0;
    },
    async loadAll() {
      if (!this.commercialUserId) {
        this.loading = false;
        this.error = this.$t("invalidCommercialId") || "معرف غير صالح";
        return;
      }
      this.loading = true;
      this.error = "";
      try {
        const [menuRes, catRes] = await Promise.all([
          publicHttp.get(`PublicMenu/${this.commercialUserId}`),
          publicHttp.get(`PublicMenu/${this.commercialUserId}/categories`),
        ]);
        const menu = menuRes.data?.data;
        if (menuRes.data?.errorStatus || !menu) {
          throw new Error(menuRes.data?.message || "error");
        }
        this.storeName = menu.storeName || menu.StoreName || "";
        this.logo = menu.logo || menu.Logo || "";
        this.defaultProductImage = menu.defaultProductImage || menu.DefaultProductImage || "";
        this.items = (menu.items || menu.Items || []).map((item) => ({
          id: item.id ?? item.Id,
          name: item.name ?? item.Name,
          description: item.description ?? item.Description,
          image: item.image ?? item.Image,
          sellingPrice: item.sellingPrice ?? item.SellingPrice,
          discountPrice: item.discountPrice ?? item.DiscountPrice,
          tags: item.tags ?? item.Tags ?? "",
          isAvailable: (item.isAvailable ?? item.IsAvailable) !== false,
          imageError: false,
        }));
        this.categories = catRes.data?.data || [];
      } catch (err) {
        this.error =
          err?.response?.data?.message === "storeNotFound"
            ? this.$t("storeNotFound") || "المتجر غير موجود"
            : this.$t("errorFetchingMenuItems") || "تعذر تحميل المنيو";
      } finally {
        this.loading = false;
      }
    },
    changeQty(item, delta) {
      const id = item.id;
      const existing = this.cart.find((l) => l.id === id);
      if (!existing && delta > 0) {
        this.cart.push({
          id,
          name: item.name,
          unitPrice: itemUnitPrice(item),
          quantity: 1,
          isAvailable: item.isAvailable,
        });
        return;
      }
      if (!existing) return;
      existing.quantity += delta;
      if (existing.quantity <= 0) {
        this.cart = this.cart.filter((l) => l.id !== id);
      }
    },
    async submitOrder() {
      this.submitError = "";
      if (!this.cart.length) return;
      this.submitting = true;
      try {
        const body = {
          customerName: this.customerName,
          customerPhone: `964${this.customerPhone.replace(/\D/g, "")}`,
          notes: this.notes || null,
          items: this.cart.map((line) => ({
            itemId: line.id,
            quantity: line.quantity,
          })),
        };
        const res = await publicHttp.post(`PublicMenu/${this.commercialUserId}/order`, body);
        if (res.data?.errorStatus) {
          throw new Error(res.data.message || "failed");
        }
        const data = res.data.data || {};
        this.successOrder = {
          orderCode: data.orderCode || data.OrderCode,
        };
        this.cart = [];
        this.cartOpen = false;
        this.notes = "";
      } catch (err) {
        const msg = err?.response?.data?.message || err.message || "";
        this.submitError = this.mapSubmitError(msg);
      } finally {
        this.submitting = false;
      }
    },
    mapSubmitError(msg) {
      if (msg === "customerNameRequired") return this.$t("customerNameRequired") || "أدخل اسم الزبون";
      if (msg === "customerPhoneRequired") return this.$t("customerPhoneRequired") || "أدخل رقم الهاتف";
      if (msg === "orderMustContainItems") return this.$t("emptyCart") || "السلة فارغة";
      return this.$t("orderSendFailed") || "تعذر إرسال الطلب";
    },
    resetAfterSuccess() {
      this.successOrder = null;
    },
  },
};
</script>

<style>
html.public-menu-page,
html.public-menu-page.dark-theme,
html.public-menu-page.light-theme,
body.public-menu-page {
  background: #f8fafc !important;
  color: #0f172a !important;
  min-height: 100%;
}
body.public-menu-page #app {
  background: #f8fafc;
  min-height: 100vh;
}
</style>

<style scoped>
.pm {
  min-height: 100vh;
  padding: 16px 16px 96px;
  max-width: 720px;
  margin: 0 auto;
  font-family: Cairo, "IBM Plex Sans Arabic", system-ui, sans-serif;
  color: #0f172a;
}
.pm-header {
  position: sticky;
  top: 0;
  z-index: 20;
  background: #f8fafc;
  padding-bottom: 12px;
}
.pm-brand {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}
.pm-logo {
  width: 56px;
  height: 56px;
  border-radius: 16px;
  object-fit: cover;
  background: #fff;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
}
.pm-logo--fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  background: #2563eb;
  color: #fff;
  font-weight: 800;
  font-size: 22px;
}
.pm-title {
  margin: 0;
  font-size: 22px;
  line-height: 1.3;
  font-weight: 800;
}
.pm-subtitle {
  margin: 4px 0 0;
  color: #475569;
  font-size: 13px;
}
.pm-search-wrap {
  position: relative;
}
.pm-search-icon {
  position: absolute;
  right: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
}
.pm-search {
  width: 100%;
  height: 48px;
  border: 0;
  border-radius: 16px;
  background: #fff;
  padding: 0 40px 0 16px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
  font-size: 16px;
}
.pm-cats {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding: 4px 0 16px;
  -webkit-overflow-scrolling: touch;
}
.pm-chip {
  flex: 0 0 auto;
  height: 40px;
  padding: 0 16px;
  border: 0;
  border-radius: 999px;
  background: #fff;
  color: #475569;
  font-weight: 600;
}
.pm-chip--on {
  background: #2563eb;
  color: #fff;
}
.pm-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;
}
@media (min-width: 600px) {
  .pm-grid {
    grid-template-columns: 1fr 1fr;
  }
  .pm-title {
    font-size: 26px;
  }
}
.pm-card {
  background: #fff;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
}
.pm-card--off {
  opacity: 0.7;
}
.pm-card-media {
  position: relative;
  aspect-ratio: 4 / 3;
  background: #f1f5f9;
}
.pm-card-media img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.pm-soldout {
  position: absolute;
  inset-inline-start: 12px;
  top: 12px;
  background: #ef4444;
  color: #fff;
  border-radius: 999px;
  padding: 4px 10px;
  font-size: 12px;
  font-weight: 700;
}
.pm-card-body {
  padding: 16px;
}
.pm-card-name {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.pm-card-desc {
  margin: 6px 0 0;
  color: #475569;
  font-size: 13px;
}
.pm-card-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 16px;
}
.pm-price {
  display: flex;
  align-items: baseline;
  gap: 6px;
  font-size: 13px;
  color: #475569;
}
.pm-price strong {
  font-size: 18px;
  color: #0f172a;
}
.pm-price s {
  color: #94a3b8;
}
.pm-add,
.pm-btn {
  height: 44px;
  border: 0;
  border-radius: 12px;
  padding: 0 16px;
  font-weight: 700;
  font-size: 16px;
  background: #2563eb;
  color: #fff;
}
.pm-add:disabled {
  background: #94a3b8;
}
.pm-btn--primary {
  background: #2563eb;
}
.pm-btn--block {
  width: 100%;
}
.pm-stepper {
  display: flex;
  align-items: center;
  gap: 8px;
}
.pm-stepper-btn {
  width: 36px;
  height: 36px;
  border: 0;
  border-radius: 12px;
  background: #eff6ff;
  color: #1d4ed8;
  font-size: 18px;
  font-weight: 700;
}
.pm-cartbar {
  position: fixed;
  left: 16px;
  right: 16px;
  bottom: 16px;
  max-width: 688px;
  margin: 0 auto;
  height: 56px;
  border: 0;
  border-radius: 16px;
  background: #0f172a;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  box-shadow: 0 6px 16px rgba(37, 99, 235, 0.24);
  z-index: 30;
}
.pm-cartbar-count {
  min-width: 28px;
  height: 28px;
  border-radius: 999px;
  background: #2563eb;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 800;
}
.pm-sheet-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.4);
  z-index: 40;
}
.pm-sheet {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  max-height: 88vh;
  background: #fff;
  border-radius: 24px 24px 0 0;
  z-index: 50;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.16);
  display: flex;
  flex-direction: column;
}
.pm-sheet-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
}
.pm-sheet-head h2 {
  margin: 0;
  font-size: 20px;
}
.pm-icon-btn {
  width: 40px;
  height: 40px;
  border: 0;
  border-radius: 12px;
  background: #f1f5f9;
}
.pm-sheet-body {
  overflow: auto;
  padding: 0 20px 24px;
}
.pm-lines {
  list-style: none;
  margin: 0 0 20px;
  padding: 0;
}
.pm-lines li {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 0;
  border-bottom: 1px solid #e2e8f0;
}
.pm-lines p {
  margin: 4px 0 0;
  color: #475569;
  font-size: 13px;
}
.pm-form label {
  display: block;
  margin-bottom: 16px;
  font-size: 13px;
  color: #475569;
}
.pm-form input,
.pm-form textarea {
  width: 100%;
  margin-top: 8px;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  min-height: 48px;
  padding: 12px 14px;
  font-size: 16px;
}
.pm-phone {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  background: #fff;
}
.pm-phone span {
  padding: 0 12px;
  font-weight: 700;
  color: #0f172a;
}
.pm-phone input {
  margin: 0;
  border: 0;
}
.pm-error {
  color: #ef4444;
  margin: 0 0 12px;
}
.pm-state,
.pm-skel {
  padding: 32px 0;
  text-align: center;
  color: #475569;
}
.pm-skel {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;
}
.pm-skel-card {
  height: 220px;
  border-radius: 16px;
  background: linear-gradient(90deg, #e2e8f0 25%, #f1f5f9 50%, #e2e8f0 75%);
  background-size: 200% 100%;
  animation: pm-shimmer 1.2s infinite;
}
@keyframes pm-shimmer {
  0% { background-position: 100% 0; }
  100% { background-position: -100% 0; }
}
.pm-success {
  position: fixed;
  inset: 0;
  background: rgba(248, 250, 252, 0.96);
  z-index: 60;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}
.pm-success-card {
  width: 100%;
  max-width: 400px;
  background: #fff;
  border-radius: 16px;
  padding: 32px 24px;
  text-align: center;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.16);
}
.pm-success-kicker {
  color: #22c55e;
  font-weight: 700;
  margin: 0 0 8px;
}
.pm-success-card h2 {
  margin: 0 0 12px;
  font-size: 32px;
  letter-spacing: 1px;
}
</style>
