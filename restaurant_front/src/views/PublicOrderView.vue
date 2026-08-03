<template>
  <div class="po">
    <!-- Hero -->
    <header class="po-hero">
      <div class="po-hero-bg"></div>
      <div class="po-hero-inner">
        <div class="po-brand">
          <div class="po-logo-wrap">
            <img
              v-if="restaurantLogo && !logoError"
              :src="restaurantLogo"
              alt=""
              class="po-logo"
              @error="logoError = true"
            />
            <div v-else class="po-logo-fallback">
              <b-icon icon="shop"></b-icon>
            </div>
          </div>
          <div class="po-brand-text">
            <p class="po-eyebrow">{{ $t('publicOrder') || 'الطلب' }}</p>
            <h1 class="po-title">{{ restaurantName || 'اطلب الآن' }}</h1>
            <p class="po-tagline">{{ $t('orderOnlineHint') || 'اختر أصنافك وأكّد طلبك' }}</p>
          </div>
        </div>
      </div>
    </header>

    <!-- Loading -->
    <div v-if="loading" class="po-state">
      <div class="po-spinner"></div>
      <p>{{ $t('loadingMenu') || 'جاري تحميل القائمة...' }}</p>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="po-state po-state--error">
      <b-icon icon="exclamation-triangle-fill"></b-icon>
      <p>{{ error }}</p>
    </div>

    <!-- Menu + order -->
    <template v-else>
      <!-- Toolbar -->
      <div class="po-toolbar">
        <div class="po-toolbar-inner">
          <div v-if="categories.length" class="po-cats">
            <button
              type="button"
              class="po-cat"
              :class="{ 'po-cat--active': selectedCategory === null }"
              @click="selectedCategory = null"
            >
              {{ $t('all') || 'الكل' }}
              <span class="po-cat-count">{{ items.length }}</span>
            </button>
            <button
              v-for="cat in sortedCategories"
              :key="cat"
              type="button"
              class="po-cat"
              :class="{ 'po-cat--active': selectedCategory === cat }"
              @click="selectedCategory = cat"
            >
              {{ cat }}
              <span class="po-cat-count">{{ categoryCounts[cat] || 0 }}</span>
            </button>
          </div>
        </div>
      </div>

      <main class="po-main" :class="{ 'po-main--cart': cartItems.length > 0 }">
        <section
          v-for="section in menuSections"
          :key="section.name"
          class="po-section"
        >
          <div class="po-section-head">
            <h2 class="po-section-title">{{ section.name }}</h2>
            <span class="po-section-line"></span>
            <span class="po-section-count">{{ section.items.length }}</span>
          </div>

          <div class="po-grid">
            <article
              v-for="item in section.items"
              :key="item.id"
              class="po-card"
              :class="{ 'po-card--in-cart': getCartQty(item.id) > 0 }"
            >
              <div class="po-card-media">
                <img
                  :src="productImageSrc(item.image, item.imageError)"
                  :alt="item.name"
                  class="po-card-img"
                  :class="{
                    'po-card-img--brand-fallback': isProductImageFallback(
                      item.image,
                      item.imageError
                    ),
                  }"
                  loading="lazy"
                  @error="onProductImageError(item)"
                />
                <span v-if="discountPercent(item)" class="po-badge">
                  -{{ discountPercent(item) }}%
                </span>
              </div>

              <div class="po-card-body">
                <h3 class="po-card-name">{{ item.name }}</h3>
                <p v-if="item.description" class="po-card-desc">{{ item.description }}</p>
                <div class="po-card-foot">
                  <div class="po-price-block">
                    <span v-if="item.discountPrice" class="po-price-old">
                      {{ formatPrice(item.sellingPrice) }}
                    </span>
                    <span class="po-price">
                      {{ formatPrice(item.discountPrice || item.sellingPrice) }}
                      <small>د.ع</small>
                    </span>
                  </div>

                  <div v-if="getCartQty(item.id) > 0" class="po-qty-ctrl">
                    <button type="button" class="po-qty-btn" @click.stop="decreaseItem(item)">
                      <b-icon icon="dash"></b-icon>
                    </button>
                    <span class="po-qty-val">{{ getCartQty(item.id) }}</span>
                    <button type="button" class="po-qty-btn" @click.stop="addToCart(item)">
                      <b-icon icon="plus"></b-icon>
                    </button>
                  </div>
                  <button v-else type="button" class="po-add-btn" @click.stop="addToCart(item)">
                    <b-icon icon="plus-lg"></b-icon>
                  </button>
                </div>
              </div>
            </article>
          </div>
        </section>

        <div v-if="menuSections.length === 0" class="po-empty">
          <b-icon icon="inbox"></b-icon>
          <p>{{ $t('noItemsInCategory') || 'لا توجد عناصر' }}</p>
        </div>
      </main>
    </template>

    <!-- Cart bar -->
    <div v-if="cartItems.length > 0" class="po-cart">
      <div class="po-cart-bar" @click="showCart = !showCart">
        <div class="po-cart-bar-left">
          <span class="po-cart-badge">{{ totalItems }}</span>
          <span class="po-cart-label">{{ $t('cart') || 'السلة' }}</span>
        </div>
        <div class="po-cart-bar-total">
          {{ formatPrice(cartTotal) }} <small>د.ع</small>
        </div>
        <b-icon :icon="showCart ? 'chevron-down' : 'chevron-up'" class="po-cart-chevron"></b-icon>
      </div>

      <transition name="po-slide">
        <div v-if="showCart" class="po-cart-panel">
          <div class="po-cart-items">
            <div v-for="(cartItem, index) in cartItems" :key="index" class="po-cart-row">
              <div class="po-cart-row-info">
                <span class="po-cart-row-name">{{ cartItem.name }}</span>
                <span class="po-cart-row-price">{{ formatPrice(cartItem.price * cartItem.quantity) }} د.ع</span>
              </div>
              <div class="po-qty-ctrl po-qty-ctrl--sm">
                <button type="button" class="po-qty-btn" @click="decreaseQuantity(index)">
                  <b-icon icon="dash"></b-icon>
                </button>
                <span class="po-qty-val">{{ cartItem.quantity }}</span>
                <button type="button" class="po-qty-btn" @click="increaseQuantity(index)">
                  <b-icon icon="plus"></b-icon>
                </button>
              </div>
            </div>
          </div>

          <div class="po-checkout">
            <p class="po-checkout-label">{{ $t('paymentMethod') || 'طريقة الدفع' }}</p>
            <div class="po-pay-options">
              <button
                type="button"
                class="po-pay-opt"
                :class="{ 'po-pay-opt--active': paymentMethod === 'Cash' }"
                @click="paymentMethod = 'Cash'"
              >
                <b-icon icon="cash-coin"></b-icon>
                {{ $t('cash') || 'كاش' }}
              </button>
              <button
                v-if="cardPaymentEnabled"
                type="button"
                class="po-pay-opt"
                :class="{ 'po-pay-opt--active': paymentMethod === 'Card' }"
                @click="paymentMethod = 'Card'"
              >
                <b-icon icon="credit-card"></b-icon>
                {{ $t('card') || 'بطاقة' }}
              </button>
            </div>

            <button
              type="button"
              class="po-submit"
              :disabled="submitting"
              @click="submitOrder"
            >
              <b-spinner small v-if="submitting"></b-spinner>
              <template v-else>
                <b-icon icon="bag-check-fill"></b-icon>
                {{ $t('confirmOrder') || 'تأكيد الطلب' }}
                · {{ formatPrice(cartTotal) }} د.ع
              </template>
            </button>
          </div>
        </div>
      </transition>
    </div>

    <CardPaymentWaitModal
      theme="light"
      :visible.sync="cardPaymentWait.show"
      :status="cardPaymentWait.status"
      :amount="cardPaymentWait.amount"
      :currency-code="cardPaymentWait.currencyCode"
      :device-name="cardPaymentWait.deviceName"
      :message="cardPaymentWait.message"
      :auth-code="cardPaymentWait.authCode"
      :ref-no="cardPaymentWait.refNo"
      :error-message="cardPaymentWait.errorMessage"
      :cancelling="cardPaymentWait.cancelling"
      @cancel="onPublicCardPaymentWaitCancel"
      @close="onPublicCardPaymentWaitClose"
    />

    <!-- Success -->
    <transition name="po-fade">
      <div v-if="showSuccessModal" class="po-success-backdrop">
        <div class="po-success">
          <div class="po-success-icon">
            <b-icon icon="check-circle-fill"></b-icon>
          </div>
          <h2 class="po-success-title">{{ $t('orderSubmitted') || 'تم إرسال الطلب بنجاح!' }}</h2>
          <p class="po-success-sub">{{ $t('orderSuccessMessage') || 'شكراً لك، سيتم تحضير طلبك قريباً' }}</p>

          <div class="po-success-code">
            <span class="po-success-code-lbl">{{ $t('orderNumber') || 'رقم الطلب' }}</span>
            <span class="po-success-code-val">{{ orderCode }}</span>
          </div>

          <div class="po-success-actions">
            <button type="button" class="po-btn po-btn--primary" @click="resetOrder">
              {{ $t('newOrder') || 'طلب جديد' }}
            </button>
          </div>
        </div>
      </div>
    </transition>

    <footer class="po-footer">
      <p>{{ restaurantName }}</p>
      <span>Lite Casher</span>
    </footer>
  </div>
</template>

<script>
import { HTTP } from '../http/api.js';
import CardPaymentWaitModal from '@/components/CardPaymentWaitModal.vue';
import publicCardPaymentMixin from '@/mixins/publicCardPaymentMixin.js';
import {
  productImageSrc,
  isProductImageFallback,
  onProductImageError,
} from '@/utils/productImage.js';

const UNCategorized = 'أخرى';

export default {
  name: 'PublicOrderView',
  components: {
    CardPaymentWaitModal,
  },
  mixins: [publicCardPaymentMixin],
  data() {
    return {
      loading: true,
      error: null,
      items: [],
      categories: [],
      selectedCategory: null,
      restaurantName: '',
      restaurantLogo: null,
      logoError: false,
      commercialUserId: null,
      cartItems: [],
      showCart: false,
      paymentMethod: 'Cash',
      submitting: false,
      showSuccessModal: false,
      orderCode: '',
    };
  },
  computed: {
    categoryCounts() {
      const counts = {};
      this.items.forEach((item) => {
        const cat = item.tags || UNCategorized;
        counts[cat] = (counts[cat] || 0) + 1;
      });
      return counts;
    },
    filteredItems() {
      let list = this.items;
      if (this.selectedCategory) {
        list = list.filter((item) => (item.tags || UNCategorized) === this.selectedCategory);
      }
      return list;
    },
    menuSections() {
      const items = this.filteredItems;
      if (!items.length) return [];

      if (this.selectedCategory) {
        return [{ name: this.selectedCategory, items }];
      }

      const groups = {};
      items.forEach((item) => {
        const cat = item.tags || UNCategorized;
        if (!groups[cat]) groups[cat] = [];
        groups[cat].push(item);
      });

      return Object.keys(groups)
        .sort((a, b) => a.localeCompare(b, 'ar'))
        .map((name) => ({ name, items: groups[name] }));
    },
    sortedCategories() {
      return [...this.categories].sort((a, b) => a.localeCompare(b, 'ar'));
    },
    totalItems() {
      return this.cartItems.reduce((sum, item) => sum + item.quantity, 0);
    },
    cartTotal() {
      return this.cartItems.reduce((sum, item) => sum + item.price * item.quantity, 0);
    },
  },
  mounted() {
    this.commercialUserId = this.$route.params.commercialUserId || this.$route.query.commercialUserId;

    if (!this.commercialUserId) {
      this.error = this.$t('restaurantNotFound') || 'معرف المطعم غير موجود';
      this.loading = false;
      return;
    }

    this.loadMenu();
    this.loadCategories();
    this.loadPaymentCapabilities();
    document.documentElement.classList.add('public-order-page');
  },
  beforeDestroy() {
    document.documentElement.classList.remove('public-order-page');
  },
  methods: {
    productImageSrc,
    isProductImageFallback,
    onProductImageError,
    async loadPaymentCapabilities() {
      if (!this.commercialUserId) {
        this.cardPaymentEnabled = false;
        return;
      }
      try {
        const res = await HTTP.get(`PublicMenu/${this.commercialUserId}/payment-capabilities`);
        const enabled = res?.data?.data?.cardPaymentEnabled === true;
        this.cardPaymentEnabled = enabled;
        if (!enabled && this.paymentMethod === 'Card') {
          this.paymentMethod = 'Cash';
        }
      } catch (e) {
        console.warn('loadPaymentCapabilities', e);
        this.cardPaymentEnabled = false;
        if (this.paymentMethod === 'Card') {
          this.paymentMethod = 'Cash';
        }
      }
    },
    async loadMenu() {
      try {
        this.loading = true;
        this.error = null;

        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}`);

        if (response.data && response.data.data) {
          const menuData = response.data.data;
          this.restaurantName = menuData.restaurantName || '';
          this.restaurantLogo = menuData.logo || null;
          this.items = (menuData.items || []).map((item) => ({
            ...item,
            imageError: false,
          }));

          if (!this.categories.length) {
            this.categories = [...new Set(this.items.map((i) => i.tags).filter(Boolean))];
          }
        } else {
          this.error = this.$t('errorFetchingMenuItems') || 'فشل تحميل القائمة';
        }
      } catch (err) {
        console.error('Error loading menu:', err);
        this.error = err.response?.data?.message || this.$t('errorFetchingMenuItems') || 'حدث خطأ أثناء تحميل القائمة';
      } finally {
        this.loading = false;
      }
    },
    async loadCategories() {
      if (!this.commercialUserId) return;

      try {
        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}/categories`);
        if (response.data && response.data.data && response.data.data.length) {
          this.categories = response.data.data;
        }
      } catch (err) {
        console.error('Error loading categories:', err);
      }
    },
    getCartQty(itemId) {
      const found = this.cartItems.find((c) => c.id === itemId);
      return found ? found.quantity : 0;
    },
    addToCart(item) {
      const price = item.discountPrice || item.sellingPrice;
      const existing = this.cartItems.find((c) => c.id === item.id);

      if (existing) {
        existing.quantity++;
      } else {
        this.cartItems.push({
          id: item.id,
          name: item.name,
          price,
          quantity: 1,
        });
      }
    },
    decreaseItem(item) {
      const idx = this.cartItems.findIndex((c) => c.id === item.id);
      if (idx >= 0) this.decreaseQuantity(idx);
    },
    increaseQuantity(index) {
      this.cartItems[index].quantity++;
    },
    decreaseQuantity(index) {
      if (this.cartItems[index].quantity > 1) {
        this.cartItems[index].quantity--;
      } else {
        this.removeFromCart(index);
      }
    },
    removeFromCart(index) {
      this.cartItems.splice(index, 1);
      if (this.cartItems.length === 0) {
        this.showCart = false;
      }
    },
    discountPercent(item) {
      if (!item?.discountPrice || !item.sellingPrice || item.discountPrice >= item.sellingPrice) {
        return 0;
      }
      return Math.round(((item.sellingPrice - item.discountPrice) / item.sellingPrice) * 100);
    },
    async submitOrder() {
      if (this.cartItems.length === 0) return;

      try {
        this.submitting = true;

        let cardPaymentTransactionId = null;
        if (this.paymentMethod === 'Card') {
          if (!this.cardPaymentEnabled) {
            this.$bvToast.toast(
              this.$t('noPaymentDeviceConfigured') || 'جهاز الدفع غير مُعد',
              { title: this.$t('error') || 'خطأ', variant: 'danger', solid: true }
            );
            return;
          }
          cardPaymentTransactionId = await this.processPublicCardPayment(this.cartTotal);
          if (!cardPaymentTransactionId) {
            return;
          }
        }

        const orderRequest = {
          PaymentMethod: this.paymentMethod,
          CardPaymentTransactionId: cardPaymentTransactionId,
          CustomerOrderItem: this.cartItems.map((item) => ({
            ItemId: item.id,
            Quantity: item.quantity,
          })),
          OrderType: 'Takeaway',
          OrderSubTotal: this.cartTotal,
          OrderTotalAfterDiscount: this.cartTotal,
        };

        const response = await HTTP.post(`PublicMenu/${this.commercialUserId}/order`, orderRequest);

        if (response.data && !response.data.errorStatus) {
          this.orderCode = response.data.data?.OrderCode || response.data.data?.orderCode || '';
          this.showSuccessModal = true;
          this.showCart = false;
        } else {
          this.$bvToast.toast(response.data?.message || this.$t('orderSubmitError') || 'حدث خطأ أثناء إرسال الطلب', {
            title: this.$t('error') || 'خطأ',
            variant: 'danger',
            solid: true,
          });
        }
      } catch (err) {
        console.error('Error submitting order:', err);
        this.$bvToast.toast(err.response?.data?.message || this.$t('orderSubmitError') || 'حدث خطأ أثناء إرسال الطلب', {
          title: this.$t('error') || 'خطأ',
          variant: 'danger',
          solid: true,
        });
      } finally {
        this.submitting = false;
      }
    },
    resetOrder() {
      this.cartItems = [];
      this.paymentMethod = 'Cash';
      this.showSuccessModal = false;
      this.orderCode = '';
      this.showCart = false;
    },
    formatPrice(price) {
      return new Intl.NumberFormat('ar-IQ').format(price);
    },
  },
};
</script>

<style scoped>
.po {
  --po-bg: #f8fafc;
  --po-surface: #ffffff;
  --po-accent: var(--primary-color, #002536);
  --po-accent-dark: var(--primary-dark, #001820);
  --po-accent-soft: color-mix(in srgb, var(--primary-color, #002536) 12%, transparent);
  --po-text: #0f172a;
  --po-muted: #64748b;
  --po-border: #e2e8f0;
  --po-shadow: 0 4px 24px rgba(15, 23, 42, 0.06);
  --po-radius: 16px;

  min-height: 100vh;
  background: var(--po-bg);
  color: var(--po-text);
  font-family: 'Cairo', sans-serif;
  padding-bottom: 1rem;
}

/* Hero */
.po-hero {
  position: relative;
  overflow: hidden;
  background: linear-gradient(
    135deg,
    #ffffff 0%,
    #f8fafc 42%,
    color-mix(in srgb, var(--po-accent) 10%, #f8fafc) 100%
  );
  color: var(--po-text);
  border-bottom: 1px solid var(--po-border);
}

.po-hero-bg {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(ellipse 75% 70% at 95% 5%, color-mix(in srgb, var(--po-accent) 22%, transparent), transparent),
    radial-gradient(ellipse 55% 55% at 5% 95%, color-mix(in srgb, var(--po-accent) 12%, transparent), transparent);
  pointer-events: none;
}

.po-hero-inner {
  position: relative;
  max-width: 920px;
  margin: 0 auto;
  padding: 2rem 1.25rem 1.5rem;
}

.po-brand {
  display: flex;
  align-items: center;
  gap: 1.125rem;
}

.po-logo {
  width: 76px;
  height: 76px;
  object-fit: contain;
  border-radius: 50%;
  background: #fff;
  padding: 0.4rem;
  border: 1px solid var(--po-border);
  box-shadow: 0 4px 18px color-mix(in srgb, var(--po-accent) 18%, transparent);
}

.po-logo-fallback {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  background: var(--po-accent-soft);
  border: 2px solid color-mix(in srgb, var(--po-accent) 35%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  color: var(--po-accent);
}

.po-eyebrow {
  margin: 0 0 0.2rem;
  font-size: 0.8125rem;
  font-weight: 700;
  letter-spacing: 0.03em;
  color: var(--po-accent-dark);
}

.po-title {
  margin: 0 0 0.3rem;
  font-size: clamp(1.375rem, 4vw, 1.875rem);
  font-weight: 800;
  line-height: 1.25;
  color: var(--po-text);
  background: none;
  -webkit-text-fill-color: var(--po-text);
}

.po-tagline {
  margin: 0;
  font-size: 0.875rem;
  color: var(--po-muted);
}

/* States */
.po-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 40vh;
  gap: 1rem;
  color: var(--po-muted);
}

.po-state--error {
  color: #b91c1c;
}

.po-spinner {
  width: 44px;
  height: 44px;
  border: 3px solid var(--po-border);
  border-top-color: var(--po-accent);
  border-radius: 50%;
  animation: po-spin 0.8s linear infinite;
}

@keyframes po-spin {
  to { transform: rotate(360deg); }
}

/* Toolbar */
.po-toolbar {
  position: sticky;
  top: 0;
  z-index: 40;
  background: color-mix(in srgb, #ffffff 90%, var(--po-bg));
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--po-border);
  box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
}

.po-toolbar-inner {
  max-width: 920px;
  margin: 0 auto;
  padding: 0.75rem 1.25rem 0.875rem;
}

.po-cats {
  display: flex;
  gap: 0.5rem;
  overflow-x: auto;
  scrollbar-width: none;
  padding-bottom: 0.15rem;
}

.po-cats::-webkit-scrollbar {
  display: none;
}

.po-cat {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 1rem;
  border: 1.5px solid var(--po-border);
  border-radius: 999px;
  background: var(--po-surface);
  font-family: inherit;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--po-text);
  cursor: pointer;
  transition: border-color 0.2s ease, background 0.2s ease, color 0.2s ease, box-shadow 0.2s ease;
}

.po-cat:hover {
  border-color: color-mix(in srgb, var(--po-accent) 45%, var(--po-border));
  color: var(--po-accent-dark);
}

.po-cat--active {
  background: var(--po-accent);
  border-color: var(--po-accent);
  color: #fff;
  box-shadow: 0 2px 10px color-mix(in srgb, var(--po-accent) 30%, transparent);
}

.po-cat-count {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--po-muted);
}

.po-cat--active .po-cat-count {
  color: rgba(255, 255, 255, 0.92);
}

/* Main */
.po-main {
  max-width: 920px;
  margin: 0 auto;
  padding: 1.25rem 1.25rem 2rem;
  transition: padding-bottom 0.3s;
}

.po-main--cart {
  padding-bottom: 5.5rem;
}

.po-section {
  margin-bottom: 2rem;
}

.po-section-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.po-section-title {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 800;
  white-space: nowrap;
  color: var(--po-text);
}

.po-section-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(to left, transparent, var(--po-border), transparent);
}

.po-section-count {
  font-size: 0.8125rem;
  font-weight: 700;
  color: var(--po-accent);
  background: var(--po-accent-soft);
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
}

/* Grid */
.po-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.75rem;
}

.po-card {
  background: var(--po-surface);
  border: 1px solid var(--po-border);
  border-radius: 14px;
  overflow: hidden;
  box-shadow: var(--po-shadow);
  display: flex;
  flex-direction: column;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.po-card--in-cart {
  border-color: color-mix(in srgb, var(--po-accent) 45%, transparent);
  box-shadow: 0 4px 20px color-mix(in srgb, var(--po-accent) 18%, transparent);
}

.po-card-media {
  position: relative;
  height: 110px;
  background: var(--po-bg);
}

.po-card-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.po-card-img--brand-fallback {
  object-fit: contain;
  padding: 16%;
  background:
    radial-gradient(circle at 50% 40%, color-mix(in srgb, var(--primary-bright, #3db4d0) 22%, transparent), transparent 65%),
    var(--primary-gradient-soft, linear-gradient(160deg, #002536 0%, #0a5a73 100%));
}

.po-card-img-fallback {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--po-accent);
  font-size: 1.75rem;
  opacity: 0.5;
}

.po-badge {
  position: absolute;
  top: 0.4rem;
  right: 0.4rem;
  background: #dc2626;
  color: #fff;
  font-size: 0.6875rem;
  font-weight: 800;
  padding: 0.15rem 0.45rem;
  border-radius: 6px;
}

.po-card-body {
  padding: 0.75rem;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.po-card-name {
  margin: 0;
  font-size: 0.9375rem;
  font-weight: 700;
  line-height: 1.35;
  color: var(--po-text);
}

.po-card-desc {
  margin: 0;
  font-size: 0.75rem;
  color: var(--po-muted);
  line-height: 1.45;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.po-card-foot {
  margin-top: auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding-top: 0.35rem;
}

.po-price-old {
  display: block;
  font-size: 0.6875rem;
  color: var(--po-muted);
  text-decoration: line-through;
}

.po-price {
  font-size: 0.9375rem;
  font-weight: 800;
  color: var(--po-accent-dark);
}

.po-price small {
  font-size: 0.6875rem;
}

.po-add-btn {
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--po-accent), var(--po-accent-dark));
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  flex-shrink: 0;
  box-shadow: 0 3px 10px color-mix(in srgb, var(--po-accent) 35%, transparent);
}

.po-qty-ctrl {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  background: var(--po-bg);
  border-radius: 999px;
  padding: 0.2rem;
  border: 1px solid var(--po-border);
}

.po-qty-ctrl--sm {
  flex-shrink: 0;
}

.po-qty-btn {
  width: 30px;
  height: 30px;
  border: none;
  border-radius: 50%;
  background: var(--po-surface);
  color: var(--po-accent-dark);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 0.875rem;
}

.po-qty-val {
  min-width: 1.25rem;
  text-align: center;
  font-weight: 800;
  font-size: 0.9375rem;
}

.po-empty {
  text-align: center;
  padding: 3rem 1rem;
  color: #78716c;
}

.po-empty .b-icon {
  font-size: 3rem;
  color: #b8864a;
  opacity: 0.35;
  margin-bottom: 0.75rem;
}

.po-empty p {
  margin: 0;
  color: #78716c;
  font-weight: 600;
  -webkit-text-fill-color: #78716c;
}

/* Cart */
.po-cart {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 100;
  max-width: 920px;
  margin: 0 auto;
}

.po-cart-bar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.875rem 1.25rem;
  background: linear-gradient(135deg, var(--po-accent) 0%, var(--po-accent-dark) 100%);
  color: #fff;
  cursor: pointer;
  box-shadow: 0 -4px 24px rgba(28, 25, 23, 0.15);
}

.po-cart-bar-left {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.po-cart-badge {
  background: #fff;
  color: var(--po-accent-dark);
  font-weight: 800;
  font-size: 0.875rem;
  min-width: 1.75rem;
  height: 1.75rem;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.po-cart-label {
  font-weight: 700;
  font-size: 0.9375rem;
}

.po-cart-bar-total {
  margin-right: auto;
  font-size: 1.125rem;
  font-weight: 800;
}

.po-cart-bar-total small {
  font-size: 0.75rem;
  font-weight: 600;
}

.po-cart-chevron {
  font-size: 1.125rem;
  opacity: 0.85;
}

.po-cart-panel {
  background: var(--po-surface);
  border-top: 1px solid var(--po-border);
  max-height: 70vh;
  overflow-y: auto;
  box-shadow: 0 -8px 32px rgba(28, 25, 23, 0.12);
}

.po-cart-items {
  padding: 1rem 1.25rem 0;
}

.po-cart-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.75rem 0;
  border-bottom: 1px solid var(--po-border);
}

.po-cart-row-name {
  display: block;
  font-weight: 700;
  font-size: 0.9375rem;
}

.po-cart-row-price {
  font-size: 0.8125rem;
  color: var(--po-accent-dark);
  font-weight: 700;
}

.po-checkout {
  padding: 1rem 1.25rem 1.25rem;
  padding-bottom: calc(1.25rem + env(safe-area-inset-bottom, 0));
}

.po-checkout-label {
  margin: 0 0 0.5rem;
  font-size: 0.875rem;
  font-weight: 700;
  color: var(--po-text);
}

.po-pay-options {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.625rem;
  margin-bottom: 1rem;
}

.po-pay-opt {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
  padding: 0.875rem;
  border: 1.5px solid var(--po-border);
  border-radius: 12px;
  background: var(--po-bg);
  font-family: inherit;
  font-size: 0.875rem;
  font-weight: 700;
  color: var(--po-muted);
  cursor: pointer;
  transition: all 0.2s;
}

.po-pay-opt--active {
  background: var(--po-accent-soft);
  border-color: var(--po-accent);
  color: var(--po-accent-dark);
}

.po-submit {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 1rem;
  border: none;
  border-radius: 14px;
  background: linear-gradient(135deg, var(--po-accent) 0%, var(--po-accent-dark) 100%);
  color: #fff;
  font-family: inherit;
  font-size: 1rem;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 16px rgba(184, 134, 74, 0.4);
}

.po-submit:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

/* Success */
.po-success-backdrop {
  position: fixed;
  inset: 0;
  z-index: 200;
  background: rgba(15, 13, 11, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.25rem;
}

.po-success {
  width: 100%;
  max-width: 400px;
  background: var(--po-surface);
  border-radius: 20px;
  padding: 2rem 1.5rem;
  text-align: center;
  box-shadow: 0 24px 64px rgba(0, 0, 0, 0.25);
}

.po-success-icon {
  font-size: 3.5rem;
  color: #059669;
  margin-bottom: 0.75rem;
}

.po-success-title {
  margin: 0 0 0.35rem;
  font-size: 1.375rem;
  font-weight: 800;
  color: var(--po-text);
}

.po-success-sub {
  margin: 0 0 1.25rem;
  font-size: 0.9375rem;
  color: var(--po-muted);
  line-height: 1.55;
}

.po-success-code {
  background: var(--po-accent-soft);
  border: 1.5px solid rgba(184, 134, 74, 0.3);
  border-radius: 14px;
  padding: 1rem;
  margin-bottom: 1.25rem;
}

.po-success-code-lbl {
  display: block;
  font-size: 0.8125rem;
  color: var(--po-muted);
  margin-bottom: 0.25rem;
}

.po-success-code-val {
  font-size: 1.625rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  color: var(--po-accent-dark);
}

.po-success-actions {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
}

.po-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.875rem 1rem;
  border-radius: 12px;
  font-family: inherit;
  font-size: 0.9375rem;
  font-weight: 700;
  text-decoration: none;
  border: none;
  cursor: pointer;
}

.po-btn--primary {
  background: linear-gradient(135deg, var(--po-accent), var(--po-accent-dark));
  color: #fff;
}

.po-btn--ghost {
  background: var(--po-bg);
  color: var(--po-text);
  border: 1.5px solid var(--po-border);
}

/* Footer */
.po-footer {
  text-align: center;
  padding: 2rem 1rem 1rem;
  color: var(--po-muted);
  font-size: 0.8125rem;
}

.po-footer p {
  margin: 0 0 0.2rem;
  font-weight: 600;
  color: var(--po-text);
}

/* Transitions */
.po-slide-enter-active,
.po-slide-leave-active {
  transition: max-height 0.3s ease, opacity 0.25s;
  overflow: hidden;
}

.po-slide-enter,
.po-slide-leave-to {
  max-height: 0;
  opacity: 0;
}

.po-fade-enter-active,
.po-fade-leave-active {
  transition: opacity 0.25s;
}

.po-fade-enter,
.po-fade-leave-to {
  opacity: 0;
}

/* Responsive */
@media (min-width: 640px) {
  .po-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 1rem;
  }

  .po-card-media {
    height: 130px;
  }

  .po-main--cart {
    padding-bottom: 6rem;
  }
}

@media (max-width: 480px) {
  .po-brand {
    flex-direction: column;
    text-align: center;
  }
}
</style>

<style>
html.public-order-page,
html.public-order-page body {
  background: #f8fafc !important;
  color: #0f172a !important;
}

html.public-order-page #app {
  background: #f8fafc;
}

html.public-order-page .po h2,
html.public-order-page .po h3,
html.public-order-page .po-card-name,
html.public-order-page .po-section-title {
  color: #0f172a !important;
  -webkit-text-fill-color: #0f172a !important;
  background: none !important;
  background-clip: unset !important;
  -webkit-background-clip: unset !important;
}

html.public-order-page .po-hero h1,
html.public-order-page .po-hero p {
  background: none !important;
  -webkit-background-clip: unset !important;
  background-clip: unset !important;
  -webkit-text-fill-color: unset !important;
}

html.public-order-page .po-title {
  color: #0f172a !important;
  -webkit-text-fill-color: #0f172a !important;
}

html.public-order-page .po-eyebrow {
  color: var(--primary-color) !important;
}

html.public-order-page .po-tagline {
  color: #64748b !important;
}

html.public-order-page .po-empty p,
html.public-order-page .po-footer,
html.public-order-page .po-state,
html.public-order-page .po-hint {
  -webkit-text-fill-color: unset !important;
  background: none !important;
}

html.public-order-page .po-empty p {
  color: #64748b !important;
}

/* Toasts — match public order light / indigo theme */
html.public-order-page .b-toaster {
  padding: 1rem;
  max-width: min(92vw, 380px);
}

html.public-order-page .b-toast {
  --po-toast-accent: var(--primary-color);
  overflow: hidden;
  border-radius: 16px !important;
  border: 1px solid #e2e8f0 !important;
  border-inline-start: 4px solid var(--po-toast-accent) !important;
  background: #ffffff !important;
  box-shadow: 0 4px 24px rgba(15, 23, 42, 0.08) !important;
  font-family: 'Cairo', sans-serif;
  color: #0f172a !important;
  min-width: min(92vw, 320px);
  max-width: 380px;
}

html.public-order-page .b-toast-solid.b-toast-danger {
  --po-toast-accent: #ef4444;
}

html.public-order-page .b-toast-solid.b-toast-success {
  --po-toast-accent: var(--primary-color);
}

html.public-order-page .b-toast-solid.b-toast-warning {
  --po-toast-accent: #f59e0b;
}

html.public-order-page .b-toast-solid.b-toast-info {
  --po-toast-accent: var(--primary-color);
}

html.public-order-page .b-toast-solid.b-toast-danger,
html.public-order-page .b-toast-solid.b-toast-success,
html.public-order-page .b-toast-solid.b-toast-warning,
html.public-order-page .b-toast-solid.b-toast-info,
html.public-order-page .b-toast-solid.b-toast-default {
  background: #ffffff !important;
  color: #0f172a !important;
}

html.public-order-page .b-toast .toast-header {
  background: transparent !important;
  border-bottom: 1px solid #e2e8f0 !important;
  color: #0f172a !important;
  font-weight: 700;
  padding: 0.75rem 1rem 0.5rem !important;
}

html.public-order-page .b-toast .toast-header strong {
  color: #0f172a !important;
}

html.public-order-page .b-toast .toast-header .close {
  color: #64748b !important;
  opacity: 1 !important;
  text-shadow: none !important;
}

html.public-order-page .b-toast .toast-header .close:hover {
  color: #0f172a !important;
  background: color-mix(in srgb, var(--primary-color) 10%, #f1f5f9);
}

html.public-order-page .b-toast .toast-body {
  padding: 0.5rem 1rem 0.85rem !important;
  color: #64748b !important;
  font-size: 0.875rem;
  line-height: 1.45;
}
</style>
