<template>
  <div class="pm" :class="{ 'pm--cart-open': cartOpen, 'pm--lock': cartOpen || successOrder }">
    <header class="pm-header">
      <div class="pm-top">
        <div class="pm-brand">
          <img
            v-if="logoSrc"
            :src="logoSrc"
            alt=""
            class="pm-avatar"
            @error="logoError = true"
          />
          <div v-else class="pm-avatar pm-avatar--fallback">{{ storeInitial }}</div>
          <div class="pm-brand-text">
            <h1 class="pm-title">{{ storeName }}</h1>
            <p class="pm-greeting">{{ greeting }}</p>
          </div>
        </div>
        <button
          type="button"
          class="pm-round-btn"
          :aria-label="$t('cart') || 'السلة'"
          @click="cartOpen = true"
        >
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path
              fill="currentColor"
              d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2m10 0c-1.1 0-1.99.9-1.99 2S15.9 22 17 22s2-.9 2-2-.9-2-2-2M7.2 14.6l.1-.6h9.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A1 1 0 0 0 21.2 5H6.21L5.27 3H2v2h2l3.6 7.59-1.35 2.44C5.52 16.37 6.48 18 8 18h12v-2H8z"
            />
          </svg>
          <span v-if="cartCount" class="pm-round-btn__dot">{{ cartCount }}</span>
        </button>
      </div>

      <div class="pm-search-row">
        <label class="pm-search-wrap">
          <span class="pm-search-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="18" height="18">
              <path
                fill="currentColor"
                d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14"
              />
            </svg>
          </span>
          <input
            v-model.trim="search"
            type="search"
            class="pm-search"
            :placeholder="$t('search') || 'بحث'"
          />
        </label>
        <button
          type="button"
          class="pm-filter-btn"
          :class="{ 'pm-filter-btn--on': filtersOpen || activeCategory }"
          :aria-label="$t('publicMenuFilter') || 'تصفية'"
          @click="filtersOpen = !filtersOpen"
        >
          <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden="true">
            <path
              fill="currentColor"
              d="M3 5h18v2H3zm4 6h10v2H7zm3 6h4v2h-4z"
            />
          </svg>
        </button>
      </div>
    </header>

    <div ref="pmScroll" class="pm-scroll">
    <div v-if="loading" class="pm-skel">
      <div class="pm-skel-banner"></div>
      <div class="pm-skel-grid">
        <div v-for="n in 6" :key="n" class="pm-skel-card"></div>
      </div>
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
      <section v-if="ads.length" class="pm-slider" dir="ltr">
        <div class="pm-slider-viewport" @touchstart="onSlideTouchStart" @touchend="onSlideTouchEnd">
          <div
            class="pm-slider-track"
            :style="{ transform: `translateX(-${slideIndex * 100}%)` }"
          >
            <article v-for="ad in ads" :key="ad.id" class="pm-slide">
              <img :src="ad.image" alt="" />
              <div v-if="ad.title" class="pm-slide-title">{{ ad.title }}</div>
            </article>
          </div>
        </div>
        <div v-if="ads.length > 1" class="pm-slider-dots">
          <button
            v-for="(ad, i) in ads"
            :key="ad.id"
            type="button"
            class="pm-dot"
            :class="{ 'pm-dot--on': slideIndex === i }"
            :aria-label="String(i + 1)"
            @click="goToSlide(i)"
          />
        </div>
      </section>

      <section v-if="categories.length" class="pm-cats-block">
        <div class="pm-section-head">
          <h2>{{ $t("categories") || "الأقسام" }}</h2>
          <button type="button" class="pm-see-all" @click="showAllCategories">
            {{ $t("seeAll") || "عرض الكل" }}
          </button>
        </div>
        <nav class="pm-cats" :class="{ 'pm-cats--wrap': filtersOpen }" aria-label="categories">
          <button
            type="button"
            class="pm-chip"
            :class="{ 'pm-chip--on': !activeCategory }"
            @click="activeCategory = ''"
          >
            {{ $t("publicMenuAll") || "الكل" }}
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
      </section>

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
            <span v-if="!item.isAvailable" class="pm-badge pm-badge--sold">
              {{ $t("soldOut") || "نفد" }}
            </span>
            <span v-else-if="itemDiscountPercent(item)" class="pm-badge pm-badge--off">
              {{ itemDiscountPercent(item) }}% {{ $t("off") || "خصم" }}
            </span>
          </div>
          <div class="pm-card-body">
            <h3 class="pm-card-name">{{ item.name }}</h3>
            <div class="pm-card-foot">
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
                :aria-label="$t('add') || 'إضافة'"
                @click="changeQty(item, 1)"
              >
                <svg class="pm-add-arrow" viewBox="0 0 24 24" width="16" height="16" aria-hidden="true">
                  <path fill="currentColor" d="M8.6 4.8 15.8 12l-7.2 7.2-1.7-1.7L12.4 12 6.9 6.5z" />
                </svg>
              </button>
            </div>
          </div>
        </article>
      </main>

      <p v-if="!visibleItems.length" class="pm-state pm-state--inline">
        {{ $t("noItemsFound") || "لا توجد مواد لعرضها." }}
      </p>
    </template>
    </div>

    <nav class="pm-tabbar" aria-label="menu">
      <button type="button" class="pm-tab pm-tab--on" @click="scrollHome">
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path fill="currentColor" d="M12 3.2 4 10v10h5.5v-6h5V20H20V10z" />
        </svg>
        <span>{{ $t("home") }}</span>
      </button>
      <button type="button" class="pm-tab" @click="goTrackTab">
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path
            fill="currentColor"
            d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm1 15h-2v-2h2zm0-4h-2V7h2z"
          />
        </svg>
        <span>{{ $t("trackOrder") || "تتبع" }}</span>
      </button>
      <button type="button" class="pm-tab" @click="cartOpen = true">
        <span class="pm-tab-icon">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path
              fill="currentColor"
              d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2m10 0c-1.1 0-1.99.9-1.99 2S15.9 22 17 22s2-.9 2-2-.9-2-2-2M7.2 14.6l.1-.6h9.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A1 1 0 0 0 21.2 5H6.21L5.27 3H2v2h2l3.6 7.59-1.35 2.44C5.52 16.37 6.48 18 8 18h12v-2H8z"
            />
          </svg>
          <i v-if="cartCount" class="pm-tab-badge">{{ cartCount }}</i>
        </span>
        <span>{{ $t("cart") }}</span>
      </button>
    </nav>

    <div v-if="cartOpen" class="pm-sheet-backdrop" @click="cartOpen = false"></div>
    <aside v-if="cartOpen" class="pm-sheet" role="dialog" aria-modal="true">
      <header class="pm-sheet-head">
        <h2 class="pm-sheet-title">{{ $t("cart") || "السلة" }}</h2>
        <button type="button" class="pm-icon-btn" @click="cartOpen = false">✕</button>
      </header>
      <div class="pm-sheet-body">
        <p v-if="!cart.length" class="pm-state pm-state--inline">
          {{ $t("emptyCart") || "السلة فارغة" }}
        </p>
        <form v-else class="pm-form" novalidate @submit.prevent="submitOrder">
          <div class="pm-sheet-scroll">
            <ul class="pm-lines">
              <li v-for="line in cart" :key="line.id">
                <div>
                  <strong>{{ line.name }}</strong>
                  <p>
                    {{ line.quantity }} × {{ formatMenuPrice(line.unitPrice) }} {{ $t("currency") }}
                  </p>
                </div>
                <div class="pm-line-end">
                  <strong class="pm-line-total">
                    {{ formatMenuPrice(line.unitPrice * line.quantity) }} {{ $t("currency") }}
                  </strong>
                  <div class="pm-stepper">
                    <button type="button" class="pm-stepper-btn" @click="changeQty(line, -1)">−</button>
                    <span>{{ line.quantity }}</span>
                    <button type="button" class="pm-stepper-btn" @click="changeQty(line, 1)">+</button>
                  </div>
                </div>
              </li>
            </ul>
            <label>
              {{ $t("customerName") || "اسم الزبون" }}
              <input
                v-model.trim="customerName"
                type="text"
                maxlength="120"
                autocomplete="name"
                :class="{ 'pm-input--invalid': nameError }"
                :placeholder="$t('customerNamePlaceholder') || 'الاسم الثلاثي'"
                @blur="validateNameField"
                @input="nameError = ''"
              />
              <span v-if="nameError" class="pm-field-error">{{ nameError }}</span>
            </label>
            <label>
              {{ $t("phoneNumber") || "الهاتف" }}
              <input
                :value="customerPhone"
                type="tel"
                inputmode="numeric"
                dir="ltr"
                maxlength="11"
                autocomplete="tel"
                class="pm-phone-input"
                :class="{ 'pm-input--invalid': phoneError }"
                :placeholder="$t('phonePlaceholder') || '078xxxxxxx'"
                @input="onPhoneInput"
                @blur="validatePhoneField"
              />
              <span class="pm-field-hint">{{ $t("iraqiPhoneHint") }}</span>
              <span v-if="phoneError" class="pm-field-error">{{ phoneError }}</span>
            </label>
            <label>
              {{ $t("publicMenuNotes") || "الملاحظات والعنوان" }}
              <textarea
                v-model.trim="notes"
                rows="2"
                maxlength="1000"
                required
                :class="{ 'pm-input--invalid': notesError }"
                :placeholder="$t('publicMenuNotesPlaceholder') || 'اكتب العنوان أو أي ملاحظة للمحل'"
                @blur="validateNotesField"
                @input="notesError = ''"
              ></textarea>
              <span v-if="notesError" class="pm-field-error">{{ notesError }}</span>
            </label>
            <p v-if="submitError" class="pm-error">{{ submitError }}</p>
          </div>
          <div class="pm-sheet-foot">
            <div class="pm-cart-summary">
              <span>{{ $t("cartTotal") || "المجموع الكلي" }}</span>
              <strong>{{ formatMenuPrice(cartTotal) }} {{ $t("currency") }}</strong>
            </div>
            <button type="submit" class="pm-btn pm-btn--primary pm-btn--block" :disabled="submitting">
              {{ submitting ? ($t("sending") || "جاري الإرسال...") : ($t("placeOrder") || "إرسال الطلب") }}
            </button>
          </div>
        </form>
      </div>
    </aside>

    <div v-if="successOrder" class="pm-success">
      <div class="pm-success-card">
        <div class="pm-success-icon" aria-hidden="true">✓</div>
        <p class="pm-success-kicker">{{ $t("orderSent") || "تم إرسال طلبك" }}</p>
        <p class="pm-success-label">{{ $t("orderCode") || "رقم الطلب" }}</p>
        <div class="pm-success-code">
          <h2>{{ successOrder.orderCode }}</h2>
          <button
            type="button"
            class="pm-copy-btn"
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
        <p>{{ $t("orderSentHint") || "ادفع في المحل عند الاستلام. احتفظ برقم الطلب." }}</p>
        <div class="pm-success-actions">
          <button type="button" class="pm-btn pm-btn--primary pm-btn--block" @click="goTrackOrder">
            {{ $t("trackOrder") || "تتبع الطلب" }}
          </button>
          <button type="button" class="pm-btn pm-btn--ghost pm-btn--block" @click="resetAfterSuccess">
            {{ $t("newOrder") || "طلب جديد" }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { publicHttp } from "@/http/publicHttp.js";
import {
  formatMenuPrice,
  itemUnitPrice,
  itemDiscountPercent,
  normalizeIraqiPhone,
  isValidIraqiPhone,
  normalizeCustomerName,
  isValidCustomerName,
  saveLastPublicOrder,
} from "@/utils/publicMenu.js";
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
      ads: [],
      categories: [],
      search: "",
      activeCategory: "",
      filtersOpen: false,
      slideIndex: 0,
      sliderTimer: null,
      touchStartX: 0,
      cart: [],
      cartOpen: false,
      customerName: "",
      customerPhone: "",
      notes: "",
      submitting: false,
      submitError: "",
      nameError: "",
      phoneError: "",
      notesError: "",
      successOrder: null,
      codeCopied: false,
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
    greeting() {
      const hour = new Date().getHours();
      if (hour < 12) return this.$t("publicMenuGreetingMorning") || "صباح الخير 👋";
      if (hour < 17) return this.$t("publicMenuGreetingAfternoon") || "طاب يومك 👋";
      return this.$t("publicMenuGreetingEvening") || "مساء الخير 👋";
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
      return this.cart.reduce(
        (sum, line) => sum + (Number(line.unitPrice) || 0) * (Number(line.quantity) || 0),
        0
      );
    },
  },
  watch: {
    ads() {
      this.startSlider();
    },
  },
  mounted() {
    document.documentElement.classList.add("public-menu-page");
    document.body.classList.add("public-menu-page");
    this.loadAll();
  },
  beforeDestroy() {
    this.stopSlider();
    if (this._copyTimer) clearTimeout(this._copyTimer);
    document.documentElement.classList.remove("public-menu-page");
    document.body.classList.remove("public-menu-page");
  },
  methods: {
    formatMenuPrice,
    itemUnitPrice,
    itemDiscountPercent,
    onProductImageError,
    itemImage(item) {
      if (item.image && !item.imageError) return item.image;
      return this.defaultProductImage || BUILTIN_DEFAULT_PRODUCT_IMAGE;
    },
    qtyInCart(id) {
      return this.cart.find((l) => l.id === id)?.quantity || 0;
    },
    showAllCategories() {
      this.activeCategory = "";
      this.filtersOpen = false;
    },
    scrollHome() {
      this.cartOpen = false;
      const scroller = this.$refs.pmScroll;
      if (scroller && typeof scroller.scrollTo === "function") {
        scroller.scrollTo({ top: 0, behavior: "smooth" });
        return;
      }
      window.scrollTo({ top: 0, behavior: "smooth" });
    },
    goToSlide(index) {
      if (!this.ads.length) return;
      this.slideIndex = (index + this.ads.length) % this.ads.length;
      this.startSlider();
    },
    startSlider() {
      this.stopSlider();
      if (this.ads.length < 2) return;
      this.sliderTimer = setInterval(() => {
        this.slideIndex = (this.slideIndex + 1) % this.ads.length;
      }, 4500);
    },
    stopSlider() {
      if (this.sliderTimer) {
        clearInterval(this.sliderTimer);
        this.sliderTimer = null;
      }
    },
    onSlideTouchStart(event) {
      this.touchStartX = event.changedTouches?.[0]?.clientX || 0;
    },
    onSlideTouchEnd(event) {
      const endX = event.changedTouches?.[0]?.clientX || 0;
      const delta = endX - this.touchStartX;
      if (Math.abs(delta) < 40) return;
      this.goToSlide(this.slideIndex + (delta < 0 ? 1 : -1));
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
        this.ads = (menu.ads || menu.Ads || [])
          .map((ad) => ({
            id: ad.id ?? ad.Id,
            image: ad.image ?? ad.Image,
            title: (ad.title ?? ad.Title) || "",
          }))
          .filter((ad) => ad.image);
        this.slideIndex = 0;
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
      if (!this.validateOrderFields()) return;
      this.submitting = true;
      try {
        const body = {
          customerName: normalizeCustomerName(this.customerName),
          customerPhone: normalizeIraqiPhone(this.customerPhone),
          notes: this.notes,
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
        const orderCode = data.orderCode || data.OrderCode;
        saveLastPublicOrder({
          commercialUserId: this.commercialUserId,
          orderCode,
          phone: this.customerPhone,
        });
        this.codeCopied = false;
        this.successOrder = { orderCode };
        this.cart = [];
        this.cartOpen = false;
        this.notes = "";
        this.nameError = "";
        this.phoneError = "";
        this.notesError = "";
      } catch (err) {
        const msg = err?.response?.data?.message || err.message || "";
        this.submitError = this.mapSubmitError(msg);
      } finally {
        this.submitting = false;
      }
    },
    mapSubmitError(msg) {
      if (msg === "customerNameRequired") return this.$t("customerNameRequired") || "أدخل اسم الزبون";
      if (msg === "customerNameInvalid") return this.$t("customerNameInvalid") || "الاسم يجب أن يحتوي على حروف فقط";
      if (msg === "customerPhoneRequired") return this.$t("customerPhoneRequired") || "أدخل رقم الهاتف";
      if (msg === "customerPhoneInvalid") {
        return this.$t("customerPhoneInvalid") || "رقم الهاتف يجب أن يكون 11 رقماً ويبدأ بـ 078 أو 077 أو 075 أو 074";
      }
      if (msg === "orderNotesRequired") {
        return this.$t("publicMenuNotesRequired") || "أدخل الملاحظات أو العنوان";
      }
      if (msg === "orderMustContainItems") return this.$t("emptyCart") || "السلة فارغة";
      return this.$t("orderSendFailed") || "تعذر إرسال الطلب";
    },
    onPhoneInput(event) {
      const digits = String(event?.target?.value || "").replace(/\D/g, "").slice(0, 11);
      this.customerPhone = digits;
      this.phoneError = "";
    },
    validateNameField() {
      const name = normalizeCustomerName(this.customerName);
      if (!name) {
        this.nameError = this.$t("customerNameRequired") || "أدخل اسم الزبون";
        return false;
      }
      if (!isValidCustomerName(name)) {
        this.nameError = this.$t("customerNameInvalid") || "الاسم يجب أن يحتوي على حروف فقط، حرفين على الأقل";
        return false;
      }
      this.nameError = "";
      return true;
    },
    validatePhoneField() {
      const phone = normalizeIraqiPhone(this.customerPhone);
      if (!phone) {
        this.phoneError = this.$t("customerPhoneRequired") || "أدخل رقم الهاتف";
        return false;
      }
      if (!isValidIraqiPhone(phone)) {
        this.phoneError =
          this.$t("customerPhoneInvalid") ||
          "رقم الهاتف يجب أن يكون 11 رقماً ويبدأ بـ 078 أو 077 أو 075 أو 074";
        return false;
      }
      this.phoneError = "";
      return true;
    },
    validateNotesField() {
      const value = String(this.notes || "").trim();
      if (!value) {
        this.notesError = this.$t("publicMenuNotesRequired") || "أدخل الملاحظات أو العنوان";
        return false;
      }
      if (value.length < 2) {
        this.notesError = this.$t("publicMenuNotesRequired") || "أدخل الملاحظات أو العنوان";
        return false;
      }
      this.notesError = "";
      return true;
    },
    validateOrderFields() {
      const nameOk = this.validateNameField();
      const phoneOk = this.validatePhoneField();
      const notesOk = this.validateNotesField();
      return nameOk && phoneOk && notesOk;
    },
    resetAfterSuccess() {
      this.successOrder = null;
      this.codeCopied = false;
    },
    goTrackTab() {
      this.cartOpen = false;
      this.$router.push({
        name: "publicMenuTrack",
        params: { commercialUserId: String(this.commercialUserId) },
      });
    },
    goTrackOrder() {
      const code = this.successOrder && this.successOrder.orderCode;
      this.successOrder = null;
      this.codeCopied = false;
      this.$router.push({
        name: "publicMenuTrack",
        params: {
          commercialUserId: String(this.commercialUserId),
          orderCode: code || undefined,
        },
      });
    },
    copyOrderCode() {
      const code = String((this.successOrder && this.successOrder.orderCode) || "");
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
.pm {
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
.pm-header {
  position: relative;
  z-index: 20;
  flex: 0 0 auto;
  background: #fef9f3;
  padding: max(8px, env(safe-area-inset-top)) 16px 12px;
}
.pm-scroll {
  flex: 1 1 auto;
  min-height: 0;
  overflow-x: hidden;
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior: contain;
  padding: 0 16px 16px;
}
.pm--lock .pm-scroll {
  overflow: hidden;
}
.pm-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
}
.pm-brand {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}
.pm-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  object-fit: cover;
  background: #fff;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.08);
  flex: 0 0 auto;
}
.pm-avatar--fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  background: #ff9f1c;
  color: #fff;
  font-weight: 800;
  font-size: 20px;
}
.pm-title {
  margin: 0;
  font-size: 18px;
  line-height: 1.3;
  font-weight: 800;
  color: #1c1917;
  -webkit-text-fill-color: #1c1917;
}
.pm-greeting {
  margin: 2px 0 0;
  color: #78716c;
  font-size: 13px;
}
.pm-round-btn {
  position: relative;
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
.pm-round-btn__dot {
  position: absolute;
  top: 4px;
  inset-inline-end: 4px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 999px;
  background: #ef4444;
  color: #fff;
  font-size: 10px;
  font-weight: 800;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.pm-search-row {
  display: flex;
  align-items: center;
  gap: 10px;
}
.pm-search-wrap {
  position: relative;
  flex: 1;
  min-width: 0;
}
.pm-search-icon {
  position: absolute;
  inset-inline-start: 16px;
  top: 50%;
  transform: translateY(-50%);
  color: #a8a29e;
  display: flex;
}
.pm-search {
  width: 100%;
  height: 48px;
  border: 0;
  border-radius: 999px;
  background: #fff;
  padding-inline: 44px 16px;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.06);
  font-size: 16px;
}
.pm-search:focus {
  outline: 2px solid #ffd199;
  outline-offset: 2px;
}
.pm-filter-btn {
  width: 48px;
  height: 48px;
  border: 0;
  border-radius: 16px;
  background: #fff;
  color: #1c1917;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.08);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
}
.pm-filter-btn--on {
  background: #ff9f1c;
  color: #fff;
}
.pm-slider {
  margin: 8px 0 20px;
}
.pm-slider-viewport {
  overflow: hidden;
  border-radius: 24px;
}
.pm-slider-track {
  display: flex;
  transition: transform 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}
.pm-slide {
  position: relative;
  min-width: 100%;
  height: 168px;
  background: linear-gradient(135deg, #ffe4c4 0%, #ffd199 100%);
}
.pm-slide img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.pm-slide-title {
  position: absolute;
  left: 16px;
  right: 16px;
  bottom: 14px;
  color: #fff;
  font-weight: 800;
  font-size: 16px;
  text-shadow: 0 1px 8px rgba(28, 25, 23, 0.45);
}
.pm-slider-dots {
  display: flex;
  justify-content: center;
  gap: 6px;
  margin-top: 10px;
}
.pm-dot {
  width: 8px;
  height: 8px;
  border: 0;
  border-radius: 999px;
  background: #e7d7c5;
  padding: 0;
}
.pm-dot--on {
  width: 20px;
  background: #ff9f1c;
}
.pm-cats-block {
  margin-bottom: 16px;
}
.pm-section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.pm-section-head h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #1c1917;
  -webkit-text-fill-color: #1c1917;
}
.pm-see-all {
  border: 0;
  background: transparent;
  color: #6b8f3e;
  font-weight: 700;
  font-size: 13px;
  padding: 0;
}
.pm-cats {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  overflow-y: hidden;
  padding-bottom: 4px;
  margin-inline: -4px;
  padding-inline: 4px;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior-x: contain;
  touch-action: pan-x;
  scrollbar-width: none;
}
.pm-cats::-webkit-scrollbar {
  display: none;
}
.pm-cats--wrap {
  flex-wrap: wrap;
  overflow: visible;
  touch-action: auto;
}
.pm-chip {
  display: inline-flex;
  align-items: center;
  flex: 0 0 auto;
  height: 40px;
  padding: 0 16px;
  border: 0;
  border-radius: 999px;
  background: #fff;
  color: #1c1917;
  font-weight: 700;
  font-size: 13px;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.06);
}
.pm-chip--on {
  background: #ff9f1c;
  color: #fff;
}
.pm-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
.pm-card {
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 22px;
  box-shadow: 0 1px 3px rgba(28, 25, 23, 0.08);
  overflow: hidden;
}
.pm-card--off {
  opacity: 0.62;
}
.pm-card-media {
  position: relative;
  height: 128px;
  background: #f5f0ea;
  overflow: hidden;
}
.pm-card-media img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center;
  display: block;
}
.pm-badge {
  position: absolute;
  top: 10px;
  inset-inline-start: 0;
  padding: 4px 10px;
  font-size: 10px;
  font-weight: 800;
  color: #fff;
  border-radius: 0 8px 8px 0;
}
[dir="rtl"] .pm-badge {
  border-radius: 8px 0 0 8px;
}
.pm-badge--off {
  background: #f43f5e;
}
.pm-badge--sold {
  background: #78716c;
}
.pm-card-body {
  padding: 10px 12px 12px;
}
.pm-card-name {
  margin: 0 0 8px;
  font-size: 15px;
  font-weight: 800;
  line-height: 1.35;
  color: #1c1917;
  -webkit-text-fill-color: #1c1917;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  min-height: 2.7em;
}
.pm-card-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
.pm-price {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 4px;
  font-size: 11px;
  color: #78716c;
  min-width: 0;
}
.pm-price strong {
  font-size: 15px;
  color: #1c1917;
}
.pm-price s {
  color: #a8a29e;
}
.pm-add {
  width: 32px;
  height: 32px;
  border: 0;
  border-radius: 50%;
  background: #ff9f1c;
  color: #fff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
  padding: 0;
}
.pm-add:disabled {
  background: #d6d3d1;
}
.pm-add-arrow {
  transform: scaleX(-1);
}
[dir="ltr"] .pm-add-arrow {
  transform: none;
}
.pm-add:focus,
.pm-stepper-btn:focus,
.pm-chip:focus,
.pm-search:focus,
.pm-round-btn:focus,
.pm-filter-btn:focus,
.pm-tab:focus {
  outline: 2px solid #ffd199;
  outline-offset: 2px;
}
.pm-btn {
  height: 48px;
  min-width: 72px;
  border: 0;
  border-radius: 999px;
  padding: 0 18px;
  font-weight: 800;
  font-size: 15px;
  background: #ff9f1c;
  color: #fff;
}
.pm-btn--primary {
  background: #ff9f1c;
}
.pm-btn--ghost {
  background: #f5f0ea;
  color: #1c1917;
}
.pm-btn--block {
  width: 100%;
}
.pm-stepper {
  display: flex;
  align-items: center;
  gap: 4px;
}
.pm-stepper span {
  min-width: 16px;
  text-align: center;
  font-weight: 800;
  font-size: 13px;
}
.pm-stepper-btn {
  width: 28px;
  height: 28px;
  border: 0;
  border-radius: 50%;
  background: #fff3e0;
  color: #c2410c;
  font-size: 16px;
  font-weight: 700;
}
.pm-tabbar {
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
.pm-tab {
  border: 0;
  background: transparent;
  color: #a8a29e;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  font-size: 11px;
  font-weight: 700;
  min-width: 72px;
  min-height: 48px;
  touch-action: manipulation;
}
.pm-tab--on {
  color: #ff9f1c;
}
.pm-tab-icon {
  position: relative;
  display: inline-flex;
}
.pm-tab-badge {
  position: absolute;
  top: -6px;
  inset-inline-end: -8px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 999px;
  background: #ef4444;
  color: #fff;
  font-size: 10px;
  font-style: normal;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.pm-sheet-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(28, 25, 23, 0.4);
  z-index: 40;
}
.pm-sheet {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  max-height: min(88dvh, calc(100dvh - env(safe-area-inset-top, 12px)));
  background: #fff;
  border-radius: 28px 28px 0 0;
  z-index: 50;
  box-shadow: 0 8px 24px rgba(28, 25, 23, 0.16);
  display: flex;
  flex-direction: column;
  max-width: 520px;
  margin: 0 auto;
  padding-bottom: env(safe-area-inset-bottom);
  overflow: hidden;
}
.pm-sheet-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
}
.pm-sheet-head h2,
.pm-sheet-title {
  margin: 0;
  font-size: 20px;
  font-weight: 800;
  color: #1c1917 !important;
  -webkit-text-fill-color: #1c1917 !important;
  background: none !important;
}
.pm-icon-btn {
  width: 40px;
  height: 40px;
  border: 0;
  border-radius: 12px;
  background: #f5f0ea;
}
.pm-sheet-body {
  overflow: hidden;
  padding: 0;
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}
.pm-form {
  display: flex;
  flex-direction: column;
  min-height: 0;
  flex: 1;
}
.pm-sheet-scroll {
  overflow: auto;
  padding: 0 20px 8px;
  flex: 1;
  min-height: 0;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior: contain;
}
.pm-sheet-foot {
  padding: 12px 20px 20px;
  border-top: 1px solid #f5f0ea;
  background: #fff;
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
  border-bottom: 1px solid #f5f0ea;
}
.pm-lines p {
  margin: 4px 0 0;
  color: #78716c;
  font-size: 13px;
}
.pm-line-end {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 8px;
  flex: 0 0 auto;
}
.pm-line-total {
  font-size: 14px;
  font-weight: 800;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}
.pm-cart-summary {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin: 0 0 12px;
  padding: 16px;
  background: #fff7ed;
  border: 1px solid #ffd199;
  border-radius: 16px;
  font-size: 16px;
  font-weight: 700;
}
.pm-cart-summary strong {
  font-size: 20px;
  color: #c2410c;
  font-variant-numeric: tabular-nums;
}
.pm-form label {
  display: block;
  margin-bottom: 16px;
  font-size: 14px;
  font-weight: 700;
  color: #1c1917;
}
.pm-form input,
.pm-form textarea {
  width: 100%;
  margin-top: 8px;
  border: 1px solid #e7e5e4;
  border-radius: 16px;
  min-height: 48px;
  padding: 12px 14px;
  font-size: 16px;
  background: #fef9f3;
}
.pm-form input:focus,
.pm-form textarea:focus {
  outline: 2px solid #ffd199;
  outline-offset: 1px;
  border-color: #ff9f1c;
}
.pm-input--invalid {
  border-color: #ef4444 !important;
}
.pm-phone-input {
  letter-spacing: 0.04em;
  font-variant-numeric: tabular-nums;
}
.pm-field-hint {
  display: block;
  margin-top: 6px;
  font-size: 12px;
  color: #a8a29e;
}
.pm-field-error {
  display: block;
  margin-top: 6px;
  font-size: 12px;
  font-weight: 700;
  color: #ef4444;
}
.pm-error {
  color: #ef4444;
  margin: 0 0 12px;
}
.pm-state {
  padding: 32px 20px;
  text-align: center;
  color: #78716c;
}
.pm-skel {
  padding: 8px 0 32px;
}
.pm-skel-banner {
  height: 168px;
  border-radius: 24px;
  margin-bottom: 16px;
  background: linear-gradient(90deg, #f5e6d6 25%, #fef9f3 50%, #f5e6d6 75%);
  background-size: 200% 100%;
  animation: pm-shimmer 1.2s infinite;
}
.pm-skel-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
.pm-skel-card {
  height: 210px;
  border-radius: 22px;
  background: linear-gradient(90deg, #f5e6d6 25%, #fef9f3 50%, #f5e6d6 75%);
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
  background: rgba(254, 249, 243, 0.96);
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
  border-radius: 24px;
  padding: 32px 24px;
  text-align: center;
  box-shadow: 0 8px 24px rgba(28, 25, 23, 0.16);
}
.pm-success-icon {
  width: 56px;
  height: 56px;
  margin: 0 auto 16px;
  border-radius: 50%;
  background: #dcfce7;
  color: #16a34a;
  font-size: 28px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
}
.pm-success-kicker {
  color: #16a34a;
  font-weight: 800;
  font-size: 18px;
  margin: 0 0 16px;
}
.pm-success-label {
  margin: 0;
  color: #78716c;
  font-size: 13px;
}
.pm-success-code {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  gap: 8px;
  margin: 8px 0 16px;
}
.pm-success-code h2 {
  margin: 0;
  font-size: 32px;
  letter-spacing: 1px;
  font-variant-numeric: tabular-nums;
  user-select: all;
}
.pm-copy-btn {
  height: 40px;
  min-width: 48px;
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
.pm-copy-btn:focus {
  outline: 2px solid #ffd199;
  outline-offset: 2px;
}
.pm-success-card p {
  margin: 0;
  color: #78716c;
  line-height: 1.6;
}
.pm-success-actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 24px;
}
@media (min-width: 600px) {
  .pm {
    max-width: 720px;
  }
  .pm-header {
    padding-top: 16px;
  }
  .pm-sheet {
    max-width: 720px;
  }
  .pm-slide {
    height: 200px;
  }
  .pm-grid {
    gap: 16px;
  }
  .pm-card-media {
    height: 160px;
  }
}
</style>
