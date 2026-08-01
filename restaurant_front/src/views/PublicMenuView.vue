<template>
  <div class="pm" :class="{ 'pm--no-order': !showPublicOrdering }">
    <!-- Hero -->
    <header class="pm-hero">
      <div class="pm-hero-bg"></div>
      <div class="pm-hero-inner">
        <div class="pm-brand">
          <div class="pm-logo-wrap">
            <img
              :src="restaurantLogo && !logoError ? restaurantLogo : BRAND_LOGO"
              alt=""
              class="pm-logo"
              :class="{ 'pm-logo--brand-fallback': !restaurantLogo || logoError }"
              @error="logoError = true"
            />
          </div>
          <div class="pm-brand-text">
            <p class="pm-eyebrow">{{ $t('publicMenu') || 'قائمة الطعام' }}</p>
            <h1 class="pm-title">{{ restaurantName || 'قائمة الطعام' }}</h1>
            <p class="pm-tagline">{{ $t('enjoyMeals') || 'استمتع بأشهى المأكولات' }}</p>
          </div>
        </div>

        <div v-if="!loading && !error && items.length" class="pm-stats">
          <div class="pm-stat">
            <span class="pm-stat-num">{{ categories.length || 1 }}</span>
            <span class="pm-stat-label">{{ $t('categories') || 'تصنيف' }}</span>
          </div>
          <div class="pm-stat-divider"></div>
          <div class="pm-stat">
            <span class="pm-stat-num">{{ items.length }}</span>
            <span class="pm-stat-label">{{ $t('menuDishes') || 'صنف' }}</span>
          </div>
        </div>
      </div>
    </header>

    <!-- Loading -->
    <div v-if="loading" class="pm-state">
      <div class="pm-spinner"></div>
      <p>{{ $t('loadingMenu') || 'جاري تحميل القائمة...' }}</p>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="pm-state pm-state--error">
      <b-icon icon="exclamation-triangle-fill"></b-icon>
      <p>{{ error }}</p>
    </div>

    <!-- Menu body -->
    <template v-else>
      <!-- Toolbar -->
      <div class="pm-toolbar" ref="toolbar">
        <div class="pm-toolbar-inner">
          <div class="pm-search">
            <b-icon icon="search" class="pm-search-icon"></b-icon>
            <input
              v-model="searchQuery"
              type="search"
              class="pm-search-input"
              :placeholder="$t('searchMenu') || 'ابحث عن صنف...'"
              autocomplete="off"
            />
            <button
              v-if="searchQuery"
              type="button"
              class="pm-search-clear"
              @click="searchQuery = ''"
              aria-label="clear"
            >
              <b-icon icon="x-lg"></b-icon>
            </button>
          </div>

          <div v-if="categories.length" class="pm-cats">
            <button
              type="button"
              class="pm-cat"
              :class="{ 'pm-cat--active': selectedCategory === null }"
              @click="selectCategory(null)"
            >
              {{ $t('all') || 'الكل' }}
              <span class="pm-cat-count">{{ items.length }}</span>
            </button>
            <button
              v-for="cat in sortedCategories"
              :key="cat"
              type="button"
              class="pm-cat"
              :class="{ 'pm-cat--active': selectedCategory === cat }"
              @click="selectCategory(cat)"
            >
              {{ cat }}
              <span class="pm-cat-count">{{ categoryCounts[cat] || 0 }}</span>
            </button>
          </div>
        </div>
      </div>

      <main class="pm-main">
        <!-- Grouped sections -->
        <section
          v-for="section in menuSections"
          :key="section.name"
          :id="'cat-' + sectionSlug(section.name)"
          class="pm-section"
        >
          <div class="pm-section-head">
            <h2 class="pm-section-title">{{ section.name }}</h2>
            <span class="pm-section-line"></span>
            <span class="pm-section-count">{{ section.items.length }}</span>
          </div>

          <div class="pm-items">
            <article
              v-for="item in section.items"
              :key="item.id"
              class="pm-item"
              tabindex="0"
              @click="openItem(item)"
              @keyup.enter="openItem(item)"
            >
              <div class="pm-item-media">
                <img
                  :src="productImageSrc(item.image, item.imageError)"
                  :alt="item.name"
                  class="pm-item-img"
                  :class="{
                    'pm-item-img--brand-fallback': isProductImageFallback(
                      item.image,
                      item.imageError
                    ),
                  }"
                  loading="lazy"
                  @error="onProductImageError(item)"
                />
                <span v-if="discountPercent(item)" class="pm-badge">
                  -{{ discountPercent(item) }}%
                </span>
              </div>

              <div class="pm-item-body">
                <h3 class="pm-item-name">{{ item.name }}</h3>
                <p v-if="item.description" class="pm-item-desc">{{ item.description }}</p>
                <div class="pm-item-foot">
                  <span v-if="item.code" class="pm-item-code">#{{ item.code }}</span>
                  <div class="pm-item-price-block">
                    <span v-if="item.discountPrice" class="pm-price-old">
                      {{ formatPrice(item.sellingPrice) }}
                    </span>
                    <span class="pm-price">
                      {{ formatPrice(item.discountPrice || item.sellingPrice) }}
                      <small>د.ع</small>
                    </span>
                  </div>
                </div>
              </div>
            </article>
          </div>
        </section>

        <!-- Empty -->
        <div v-if="menuSections.length === 0" class="pm-empty">
          <b-icon icon="inbox"></b-icon>
          <p>{{ searchQuery ? ($t('noSearchResults') || 'لا توجد نتائج') : ($t('noItemsInCategory') || 'لا توجد عناصر') }}</p>
        </div>
      </main>

      <!-- Floating actions (hidden when public ordering is disabled) -->
      <div v-if="showPublicOrdering" class="pm-actions">
        <router-link :to="orderLink" class="pm-btn pm-btn--primary">
          <b-icon icon="bag-check-fill"></b-icon>
          {{ $t('orderNow') || 'اطلب الآن' }}
        </router-link>
        <router-link :to="trackLink" class="pm-btn pm-btn--ghost">
          <b-icon icon="geo-alt"></b-icon>
          <span class="pm-btn-label">{{ $t('trackOrder') || 'تتبع الطلب' }}</span>
        </router-link>
      </div>
    </template>

    <!-- Item modal -->
    <transition name="pm-fade">
      <div v-if="selectedItem" class="pm-modal-backdrop" @click.self="selectedItem = null">
        <div class="pm-modal" role="dialog">
          <button type="button" class="pm-modal-close" @click="selectedItem = null">
            <b-icon icon="x-lg"></b-icon>
          </button>

          <div class="pm-modal-media">
            <img
              :src="productImageSrc(selectedItem.image, selectedItem.imageError)"
              :alt="selectedItem.name"
              class="pm-modal-img"
              :class="{
                'pm-modal-img--brand-fallback': isProductImageFallback(
                  selectedItem.image,
                  selectedItem.imageError
                ),
              }"
              @error="onProductImageError(selectedItem)"
            />
            <span v-if="discountPercent(selectedItem)" class="pm-badge pm-badge--lg">
              -{{ discountPercent(selectedItem) }}%
            </span>
          </div>

          <div class="pm-modal-body">
            <span v-if="selectedItem.tags" class="pm-modal-tag">{{ selectedItem.tags }}</span>
            <h2 class="pm-modal-title">{{ selectedItem.name }}</h2>
            <p v-if="selectedItem.description" class="pm-modal-desc">{{ selectedItem.description }}</p>
            <div class="pm-modal-price">
              <span v-if="selectedItem.discountPrice" class="pm-price-old">
                {{ formatPrice(selectedItem.sellingPrice) }} د.ع
              </span>
              <span class="pm-price pm-price--lg">
                {{ formatPrice(selectedItem.discountPrice || selectedItem.sellingPrice) }}
                <small>د.ع</small>
              </span>
            </div>
            <router-link
              v-if="showPublicOrdering"
              :to="orderLink"
              class="pm-btn pm-btn--primary pm-btn--block"
              @click.native="selectedItem = null"
            >
              <b-icon icon="plus-circle"></b-icon>
              {{ $t('addToOrder') || 'أضف للطلب' }}
            </router-link>
          </div>
        </div>
      </div>
    </transition>

    <footer class="pm-footer">
      <p>{{ restaurantName }}</p>
      <span class="pm-footer-brand">Lite Casher</span>
    </footer>
  </div>
</template>

<script>
import { HTTP } from '../http/api.js';
import {
  productImageSrc,
  isProductImageFallback,
  onProductImageError,
  BRAND_LOGO,
} from '@/utils/productImage.js';

const UNCategorized = 'أخرى';

export default {
  name: 'PublicMenuView',
  data() {
    return {
      BRAND_LOGO,
      loading: true,
      error: null,
      items: [],
      categories: [],
      selectedCategory: null,
      searchQuery: '',
      restaurantName: '',
      restaurantLogo: null,
      logoError: false,
      commercialUserId: null,
      selectedItem: null,
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
      const q = this.searchQuery.trim().toLowerCase();
      if (!q) return list;
      return list.filter(
        (item) =>
          (item.name && item.name.toLowerCase().includes(q)) ||
          (item.description && item.description.toLowerCase().includes(q)) ||
          (item.tags && item.tags.toLowerCase().includes(q)) ||
          (item.code && String(item.code).toLowerCase().includes(q))
      );
    },
    menuSections() {
      const items = this.filteredItems;
      if (!items.length) return [];

      if (this.selectedCategory || this.searchQuery.trim()) {
        const name = this.selectedCategory || (this.$t('searchResults') || 'نتائج البحث');
        return [{ name, items }];
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
    orderLink() {
      return `/order/${this.commercialUserId}`;
    },
    trackLink() {
      return `/order-status/${this.commercialUserId}`;
    },
    /** Set VUE_APP_PUBLIC_MENU_ORDER_ENABLED=true to show order buttons again. */
    showPublicOrdering() {
      const flag = process.env.VUE_APP_PUBLIC_MENU_ORDER_ENABLED;
      return flag === 'true' || flag === '1';
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
    document.documentElement.classList.add('public-menu-page');
  },
  beforeDestroy() {
    document.documentElement.classList.remove('public-menu-page');
  },
  methods: {
    productImageSrc,
    isProductImageFallback,
    onProductImageError,
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
            const fromItems = [...new Set(this.items.map((i) => i.tags).filter(Boolean))];
            this.categories = fromItems;
          }
        } else {
          this.error = this.$t('errorFetchingMenuItems') || 'فشل تحميل القائمة';
        }
      } catch (err) {
        console.error('Error loading menu:', err);
        this.error =
          err.response?.data?.message ||
          this.$t('errorFetchingMenuItems') ||
          'حدث خطأ أثناء تحميل القائمة';
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
    selectCategory(cat) {
      this.selectedCategory = cat;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },
    sectionSlug(name) {
      return String(name).replace(/\s+/g, '-');
    },
    openItem(item) {
      this.selectedItem = item;
    },
    discountPercent(item) {
      if (!item?.discountPrice || !item.sellingPrice || item.discountPrice >= item.sellingPrice) {
        return 0;
      }
      return Math.round(((item.sellingPrice - item.discountPrice) / item.sellingPrice) * 100);
    },
    formatPrice(price) {
      return new Intl.NumberFormat('ar-IQ').format(price);
    },
  },
};
</script>

<style scoped>
.pm {
  --pm-bg: #f7f3ee;
  --pm-surface: #ffffff;
  --pm-accent: var(--primary-color, #002536);
  --pm-accent-dark: var(--primary-dark, #001820);
  --pm-accent-soft: color-mix(in srgb, var(--primary-color, #002536) 12%, transparent);
  --pm-text: #1c1917;
  --pm-muted: #78716c;
  --pm-border: #e7e0d8;
  --pm-shadow: 0 4px 24px rgba(28, 25, 23, 0.08);
  --pm-radius: 16px;
  --pm-content-max: 920px;

  min-height: 100vh;
  background: var(--pm-bg);
  color: var(--pm-text);
  font-family: 'Cairo', sans-serif;
  padding-bottom: 5.5rem;
}

.pm--no-order {
  padding-bottom: 1.5rem;
}

/* Hero */
.pm-hero {
  position: relative;
  overflow: hidden;
  background: linear-gradient(145deg, #2c2419 0%, #1a1612 55%, #0f0d0b 100%);
  color: #fff;
}

.pm-hero-bg {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(ellipse 80% 60% at 80% 20%, rgba(184, 134, 74, 0.25), transparent),
    radial-gradient(ellipse 60% 50% at 10% 80%, rgba(184, 134, 74, 0.12), transparent);
  pointer-events: none;
}

.pm-hero-inner {
  position: relative;
  max-width: var(--pm-content-max);
  margin: 0 auto;
  padding: 2.5rem 1.25rem 2rem;
}

.pm-brand {
  display: flex;
  align-items: center;
  gap: 1.25rem;
}

.pm-logo-wrap {
  flex-shrink: 0;
}

.pm-logo {
  width: 88px;
  height: 88px;
  object-fit: contain;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.95);
  padding: 0.5rem;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.35);
}

.pm-logo-fallback {
  width: 88px;
  height: 88px;
  border-radius: 50%;
  background: var(--pm-accent-soft);
  border: 2px solid rgba(184, 134, 74, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2rem;
  color: var(--pm-accent);
}

.pm-eyebrow {
  margin: 0 0 0.25rem;
  font-size: 0.8125rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: rgba(255, 255, 255, 0.55);
}

.pm-title {
  margin: 0 0 0.35rem;
  font-size: clamp(1.5rem, 4vw, 2.125rem);
  font-weight: 800;
  line-height: 1.25;
  color: #fff8f0;
  background: none;
  -webkit-text-fill-color: #fff8f0;
}

.pm-tagline {
  margin: 0;
  font-size: 0.9375rem;
  color: rgba(255, 255, 255, 0.65);
}

.pm-stats {
  display: flex;
  align-items: center;
  gap: 1.25rem;
  margin-top: 1.75rem;
  padding-top: 1.25rem;
  border-top: 1px solid rgba(255, 255, 255, 0.12);
}

.pm-stat {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.pm-stat-num {
  font-size: 1.375rem;
  font-weight: 800;
  color: var(--pm-accent);
}

.pm-stat-label {
  font-size: 0.8125rem;
  color: rgba(255, 255, 255, 0.55);
}

.pm-stat-divider {
  width: 1px;
  height: 2rem;
  background: rgba(255, 255, 255, 0.15);
}

/* States */
.pm-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 40vh;
  gap: 1rem;
  color: var(--pm-muted);
  font-size: 1rem;
}

.pm-state--error {
  color: #b91c1c;
}

.pm-state--error .b-icon {
  font-size: 2.5rem;
}

.pm-spinner {
  width: 44px;
  height: 44px;
  border: 3px solid var(--pm-border);
  border-top-color: var(--pm-accent);
  border-radius: 50%;
  animation: pm-spin 0.8s linear infinite;
}

@keyframes pm-spin {
  to { transform: rotate(360deg); }
}

/* Toolbar */
.pm-toolbar {
  position: sticky;
  top: 0;
  z-index: 50;
  background: rgba(247, 243, 238, 0.92);
  backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--pm-border);
  box-shadow: 0 2px 12px rgba(28, 25, 23, 0.04);
}

.pm-toolbar-inner {
  max-width: var(--pm-content-max);
  margin: 0 auto;
  padding: 0.875rem 1.25rem 1rem;
}

.pm-search {
  position: relative;
  margin-bottom: 0.75rem;
}

.pm-search-icon {
  position: absolute;
  right: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--pm-muted);
  pointer-events: none;
}

.pm-search-input {
  width: 100%;
  padding: 0.75rem 2.75rem 0.75rem 2.5rem;
  border: 1.5px solid var(--pm-border);
  border-radius: 999px;
  background: var(--pm-surface);
  font-family: inherit;
  font-size: 0.9375rem;
  color: var(--pm-text);
  transition: border-color 0.2s, box-shadow 0.2s;
}

.pm-search-input:focus {
  outline: none;
  border-color: var(--pm-accent);
  box-shadow: 0 0 0 3px var(--pm-accent-soft);
}

.pm-search-clear {
  position: absolute;
  left: 0.75rem;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: var(--pm-muted);
  cursor: pointer;
  padding: 0.25rem;
  line-height: 1;
}

.pm-cats {
  display: flex;
  gap: 0.5rem;
  overflow-x: auto;
  padding-bottom: 0.25rem;
  scrollbar-width: none;
}

.pm-cats::-webkit-scrollbar {
  display: none;
}

.pm-cat {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 1rem;
  border: 1.5px solid var(--pm-border);
  border-radius: 999px;
  background: var(--pm-surface);
  font-family: inherit;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--pm-muted);
  cursor: pointer;
  transition: all 0.2s;
}

.pm-cat:hover {
  border-color: var(--pm-accent);
  color: var(--pm-accent-dark);
}

.pm-cat--active {
  background: var(--pm-accent);
  border-color: var(--pm-accent);
  color: #fff;
}

.pm-cat-count {
  font-size: 0.75rem;
  opacity: 0.75;
  font-weight: 700;
}

/* Main */
.pm-main {
  max-width: var(--pm-content-max);
  margin: 0 auto;
  padding: 1.5rem 1.25rem 2rem;
}

.pm-section {
  margin-bottom: 2.5rem;
}

.pm-section-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.25rem;
}

.pm-section-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 800;
  color: var(--pm-text);
  white-space: nowrap;
}

.pm-section-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(to left, transparent, var(--pm-border), transparent);
}

.pm-section-count {
  font-size: 0.8125rem;
  font-weight: 700;
  color: var(--pm-accent);
  background: var(--pm-accent-soft);
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
}

/* Items */
.pm-items {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.pm-item {
  display: flex;
  flex-direction: row;
  align-items: stretch;
  gap: 1rem;
  background: var(--pm-surface);
  border: 1px solid var(--pm-border);
  border-radius: var(--pm-radius);
  padding: 0.875rem;
  cursor: pointer;
  transition: box-shadow 0.2s, transform 0.2s, border-color 0.2s;
  box-shadow: var(--pm-shadow);
}

.pm-item:hover,
.pm-item:focus {
  outline: none;
  border-color: rgba(184, 134, 74, 0.45);
  transform: translateY(-2px);
  box-shadow: 0 8px 28px rgba(28, 25, 23, 0.12);
}

.pm-item-media {
  position: relative;
  flex-shrink: 0;
  width: 96px;
  height: 96px;
  border-radius: 12px;
  overflow: hidden;
  background: var(--pm-accent-soft);
}

.pm-item-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.pm-item-img--brand-fallback {
  object-fit: contain;
  padding: 16%;
  background:
    radial-gradient(circle at 50% 40%, color-mix(in srgb, var(--primary-bright, #3db4d0) 22%, transparent), transparent 65%),
    var(--primary-gradient-soft, linear-gradient(160deg, #002536 0%, #0a5a73 100%));
}

.pm-item-img-fallback {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--pm-accent-soft);
  color: var(--pm-accent);
  font-size: 1.75rem;
}

.pm-logo--brand-fallback {
  object-fit: contain;
  padding: 10%;
  background: #002536;
}

.pm-badge {
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

.pm-badge--lg {
  font-size: 0.8125rem;
  padding: 0.25rem 0.6rem;
}

.pm-item-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.pm-item-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.pm-item-foot {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 0.75rem;
  margin-top: auto;
  padding-top: 0.65rem;
  border-top: 1px dashed var(--pm-border);
}

.pm-item-name {
  margin: 0;
  font-size: 1.0625rem;
  font-weight: 700;
  line-height: 1.35;
  color: var(--pm-text);
}

.pm-item-price-block {
  text-align: left;
  flex-shrink: 0;
}

.pm-price-old {
  display: block;
  font-size: 0.75rem;
  color: var(--pm-muted);
  text-decoration: line-through;
  margin-bottom: 0.1rem;
}

.pm-price {
  font-size: 1.0625rem;
  font-weight: 800;
  color: var(--pm-accent-dark);
  white-space: nowrap;
}

.pm-price small {
  font-size: 0.75rem;
  font-weight: 600;
}

.pm-price--lg {
  font-size: 1.375rem;
}

.pm-item-desc {
  margin: 0;
  font-size: 0.875rem;
  line-height: 1.55;
  color: var(--pm-muted);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.pm-item-meta {
  margin-top: auto;
}

.pm-item-code {
  font-size: 0.6875rem;
  color: var(--pm-muted);
  opacity: 0.75;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

/* Empty */
.pm-empty {
  text-align: center;
  padding: 3rem 1rem;
  color: #78716c;
}

.pm-empty .b-icon {
  font-size: 3rem;
  color: #b8864a;
  opacity: 0.35;
  margin-bottom: 0.75rem;
}

.pm-empty p {
  margin: 0;
  color: #78716c;
  font-weight: 600;
  -webkit-text-fill-color: #78716c;
}

/* Floating actions */
.pm-actions {
  position: fixed;
  bottom: 0;
  left: 50%;
  z-index: 60;
  transform: translateX(-50%);
  width: min(var(--pm-content-max), calc(100% - 1.5rem));
  display: flex;
  gap: 0.625rem;
  padding: 0.75rem 1rem calc(0.75rem + env(safe-area-inset-bottom, 0));
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(12px);
  border: 1px solid var(--pm-border);
  border-bottom: none;
  border-radius: 16px 16px 0 0;
  box-shadow: 0 -8px 32px rgba(28, 25, 23, 0.1);
}

.pm-btn {
  flex: 1;
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
  transition: background 0.2s, transform 0.15s;
}

.pm-btn:active {
  transform: scale(0.98);
}

.pm-btn--primary {
  background: linear-gradient(135deg, var(--pm-accent) 0%, var(--pm-accent-dark) 100%);
  color: #fff;
  box-shadow: 0 4px 14px rgba(184, 134, 74, 0.4);
}

.pm-btn--ghost {
  background: var(--pm-bg);
  color: var(--pm-text);
  border: 1.5px solid var(--pm-border);
  flex: 0 0 auto;
  padding-inline: 1rem;
}

.pm-btn--block {
  width: 100%;
  margin-top: 1rem;
}

/* Modal */
.pm-modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  background: rgba(15, 13, 11, 0.65);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding: 1rem;
}

.pm-modal {
  position: relative;
  width: 100%;
  max-width: 480px;
  max-height: 90vh;
  overflow-y: auto;
  background: var(--pm-surface);
  border-radius: 20px 20px 16px 16px;
  box-shadow: 0 24px 64px rgba(0, 0, 0, 0.35);
}

.pm-modal-close {
  position: absolute;
  top: 0.75rem;
  left: 0.75rem;
  z-index: 2;
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.92);
  color: var(--pm-text);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: var(--pm-shadow);
}

.pm-modal-media {
  position: relative;
  height: 220px;
  background: var(--pm-bg);
}

.pm-modal-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.pm-modal-img--brand-fallback {
  object-fit: contain;
  padding: 18%;
  background:
    radial-gradient(circle at 50% 40%, color-mix(in srgb, var(--primary-bright, #3db4d0) 22%, transparent), transparent 65%),
    var(--primary-gradient-soft, linear-gradient(160deg, #002536 0%, #0a5a73 100%));
}

.pm-modal-img-fallback {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 4rem;
  color: var(--pm-accent);
  opacity: 0.4;
}

.pm-modal-body {
  padding: 1.25rem 1.25rem 1.5rem;
}

.pm-modal-tag {
  display: inline-block;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--pm-accent-dark);
  background: var(--pm-accent-soft);
  padding: 0.2rem 0.65rem;
  border-radius: 999px;
  margin-bottom: 0.5rem;
}

.pm-modal-title {
  margin: 0 0 0.5rem;
  font-size: 1.375rem;
  font-weight: 800;
}

.pm-modal-desc {
  margin: 0 0 1rem;
  font-size: 0.9375rem;
  line-height: 1.65;
  color: var(--pm-muted);
}

.pm-modal-price {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
}

.pm-fade-enter-active,
.pm-fade-leave-active {
  transition: opacity 0.25s;
}

.pm-fade-enter,
.pm-fade-leave-to {
  opacity: 0;
}

/* Footer */
.pm-footer {
  text-align: center;
  padding: 2rem 1rem 1rem;
  color: var(--pm-muted);
  font-size: 0.8125rem;
}

.pm-footer p {
  margin: 0 0 0.25rem;
  font-weight: 600;
  color: var(--pm-text);
}

.pm-footer-brand {
  opacity: 0.5;
  font-size: 0.75rem;
}

/* Responsive */
@media (min-width: 640px) {
  .pm {
    --pm-content-max: 960px;
  }

  .pm-items {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
    gap: 1rem;
  }

  .pm-item {
    flex-direction: column;
    padding: 0;
    overflow: hidden;
    height: 100%;
  }

  .pm-item-media {
    width: 100%;
    height: auto;
    aspect-ratio: 4 / 3;
    border-radius: 0;
  }

  .pm-item-body {
    padding: 0.875rem 1rem 1rem;
    flex: 1;
  }

  .pm-item-name {
    font-size: 1rem;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .pm-cats {
    flex-wrap: wrap;
    overflow-x: visible;
    justify-content: flex-start;
  }

  .pm-modal-backdrop {
    align-items: center;
  }

  .pm-modal {
    border-radius: 20px;
  }
}

@media (min-width: 900px) {
  .pm {
    --pm-content-max: 1080px;
    background:
      linear-gradient(
        90deg,
        #ebe6df 0%,
        #ebe6df calc((100% - var(--pm-content-max)) / 2),
        var(--pm-bg) calc((100% - var(--pm-content-max)) / 2),
        var(--pm-bg) calc((100% + var(--pm-content-max)) / 2),
        #ebe6df calc((100% + var(--pm-content-max)) / 2),
        #ebe6df 100%
      );
  }

  .pm-hero-inner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 2rem;
    padding-top: 2rem;
    padding-bottom: 2rem;
  }

  .pm-brand {
    flex: 1;
    min-width: 0;
  }

  .pm-logo,
  .pm-logo-fallback {
    width: 100px;
    height: 100px;
  }

  .pm-title {
    font-size: 2.25rem;
  }

  .pm-stats {
    margin-top: 0;
    padding-top: 0;
    border-top: none;
    flex-shrink: 0;
    padding: 1rem 1.5rem;
    background: rgba(255, 255, 255, 0.06);
    border-radius: 16px;
    border: 1px solid rgba(255, 255, 255, 0.1);
  }

  .pm-stat-num {
    font-size: 1.75rem;
  }

  .pm-items {
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
    gap: 1.25rem;
  }

  .pm-section-title {
    font-size: 1.375rem;
  }

  .pm-toolbar-inner,
  .pm-main {
    padding-left: 2rem;
    padding-right: 2rem;
  }
}

@media (min-width: 1200px) {
  .pm {
    --pm-content-max: 1140px;
  }

  .pm-items {
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  }
}

@media (max-width: 639px) {
  .pm-item-foot {
    border-top: none;
    padding-top: 0.25rem;
    margin-top: 0.15rem;
  }

  .pm-item-name {
    font-size: 0.9375rem;
  }

  .pm-price {
    font-size: 0.9375rem;
  }
}

@media (max-width: 480px) {
  .pm-brand {
    flex-direction: column;
    text-align: center;
  }

  .pm-stats {
    justify-content: center;
  }

  .pm-item-media {
    width: 80px;
    height: 80px;
  }

  .pm-item-name {
    font-size: 0.9375rem;
  }

  .pm-price {
    font-size: 0.9375rem;
  }

  .pm-btn--ghost .pm-btn-label {
    display: none;
  }
}
</style>

<style>
/* Override app dark theme on public menu page */
html.public-menu-page,
html.public-menu-page body {
  background: #f7f3ee !important;
  color: #1c1917 !important;
}

html.public-menu-page #app {
  background: #f7f3ee;
}

html.public-menu-page .pm-title {
  color: #fff8f0 !important;
  -webkit-text-fill-color: #fff8f0 !important;
  background: none !important;
}

html.public-menu-page .pm-empty p,
html.public-menu-page .pm-tagline,
html.public-menu-page .pm-eyebrow,
html.public-menu-page .pm-footer,
html.public-menu-page .pm-state {
  -webkit-text-fill-color: unset !important;
  background: none !important;
}

html.public-menu-page .pm-empty p {
  color: #78716c !important;
}
</style>
