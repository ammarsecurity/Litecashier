<template>
  <div class="pos-catalog-browser" :class="{ 'pos-catalog-browser--embedded': embedded }">
    <div v-if="showHero" class="pos-catalog-hero">
      <div class="pos-catalog-hero-text">
        <h3 class="pos-catalog-title">
          <b-icon icon="box-seam" class="me-2"></b-icon>
          {{ $t("posCatalogModalTitle") || "كتالوج المنتجات" }}
        </h3>
        <p class="pos-catalog-subtitle">
          {{ $t("posCatalogModalSubtitle") || "ابحث بالاسم أو الكود، أو اختر قسماً ثم أضف للسلة" }}
        </p>
      </div>
      <button
        v-if="showClose"
        type="button"
        class="pos-ui-modal-close"
        :aria-label="$t('close') || 'إغلاق'"
        @click="$emit('close')"
      >
        <b-icon icon="x-lg"></b-icon>
      </button>
    </div>

    <div class="pos-catalog-toolbar">
      <div class="pos-catalog-search">
        <b-icon icon="search" class="pos-catalog-search-icon" aria-hidden="true"></b-icon>
        <input
          ref="catalogSearchInput"
          :value="quickSearch"
          type="search"
          class="pos-catalog-search-input"
          :placeholder="$t('posCatalogSearchPlaceholder') || 'ابحث عن منتج بالاسم أو الكود...'"
          :aria-label="$t('posCatalogSearchPlaceholder') || 'بحث المنتجات'"
          autocomplete="off"
          spellcheck="false"
          @input="$emit('update:quickSearch', $event.target.value)"
        />
        <button
          v-if="quickSearch"
          type="button"
          class="pos-catalog-search-clear"
          :aria-label="$t('clear') || 'مسح'"
          @click="$emit('clear-search')"
        >
          <b-icon icon="x"></b-icon>
        </button>
      </div>
      <div class="pos-catalog-meta">
        <span class="pos-catalog-count">
          {{ totalItems }}
          {{ $t("posCatalogItemUnit") || "مادة" }}
        </span>
      </div>
    </div>

    <div class="pos-catalog-categories" role="tablist">
      <button
        type="button"
        class="pos-catalog-cat-chip"
        :class="{ 'pos-catalog-cat-chip--active': activeCategory === '' }"
        @click="$emit('select-category', '')"
      >
        {{ $t("all") || "الكل" }}
      </button>
      <button
        v-for="tag in tags"
        :key="tag.id"
        type="button"
        class="pos-catalog-cat-chip"
        :class="{ 'pos-catalog-cat-chip--active': activeCategory === tag.name }"
        @click="$emit('select-category', tag.name)"
      >
        {{ tag.name }}
      </button>
    </div>

    <div class="pos-catalog-grid-wrap">
      <div v-if="catalogLoading && !items.length" class="pos-catalog-loading">
        {{ $t("pleaseWait") || "جاري التحميل..." }}
      </div>
      <div v-else-if="!items.length" class="pos-catalog-empty">
        <b-icon icon="inbox"></b-icon>
        <p>{{ $t("noItemsFound") || "لا توجد منتجات مطابقة" }}</p>
      </div>
      <div v-else class="pos-products-grid pos-catalog-products-grid">
        <div
          class="pos-product-card"
          :class="{ 'pos-product-card-disabled': !item.quantity || item.quantity <= 0 }"
          v-for="item in items"
          :key="item.id"
          @click="item.quantity > 0 ? $emit('add-item', item) : null"
        >
          <div
            v-if="!isWholesale && item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
            class="pos-product-discount-badge"
          >
            <b-icon icon="tag-fill" class="me-1"></b-icon>
            {{ $t("discountLabel") }}
          </div>
          <div class="pos-product-media">
            <div class="pos-product-image-container">
              <img
                :src="productImageSrc(item.image, item.imageError)"
                :alt="item.name"
                class="pos-product-image"
                :class="{
                  'pos-product-image--brand-fallback': isProductImageFallback(
                    item.image,
                    item.imageError
                  ),
                }"
                @error="onProductImageError(item)"
              />
            </div>
          </div>
          <div class="pos-product-info">
            <h4 class="pos-product-name" :title="item.name">{{ item.name }}</h4>
            <div class="pos-catalog-stock-row">
              <span
                v-if="!item.quantity || item.quantity <= 0"
                class="pos-catalog-stock-chip pos-catalog-stock-chip--out"
              >
                {{ $t("itemOutOfStock") || "غير متوفر" }}
              </span>
              <span v-else class="pos-catalog-stock-chip pos-catalog-stock-chip--qty">
                <b-icon icon="box-seam" aria-hidden="true"></b-icon>
                {{ item.quantity }}
              </span>
            </div>
            <div class="pos-product-footer">
              <div class="pos-product-price">
                <div
                  v-if="!isWholesale && item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
                  class="pos-product-price-discounted"
                >
                  <span class="pos-product-price-current">
                    {{ formatPrice(item.disCountPrice) }} {{ $t("currency") }}
                  </span>
                  <span class="pos-product-price-old">
                    {{ formatPrice(item.sellingPrice) }} {{ $t("currency") }}
                  </span>
                </div>
                <div v-else class="pos-product-price-regular">
                  {{ formatPrice(unitPrice(item)) }} {{ $t("currency") }}
                </div>
              </div>
              <span
                v-if="item.quantity && item.quantity > 0"
                class="pos-product-add-btn"
                aria-hidden="true"
              >
                <b-icon icon="plus-lg"></b-icon>
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="pos-catalog-footer">
      <b-pagination
        :value="pageNumber"
        :total-rows="totalItems"
        :per-page="pageSize"
        aria-controls="pos-catalog-products"
        class="pos-pagination pos-catalog-pagination"
        @input="$emit('update:pageNumber', $event)"
      />
      <button
        v-if="showDone"
        type="button"
        class="pos-ui-modal-btn pos-ui-modal-btn--primary"
        @click="$emit('close')"
      >
        <b-icon icon="check-lg"></b-icon>
        {{ $t("done") || $t("close") || "تم" }}
      </button>
    </div>
  </div>
</template>

<script>
import {
  productImageSrc,
  isProductImageFallback,
  onProductImageError,
} from "@/utils/productImage.js";

export default {
  name: "PosCatalogBrowser",
  props: {
    items: { type: Array, default: () => [] },
    tags: { type: Array, default: () => [] },
    quickSearch: { type: String, default: "" },
    activeCategory: { type: String, default: "" },
    totalItems: { type: Number, default: 0 },
    pageNumber: { type: Number, default: 1 },
    pageSize: { type: Number, default: 20 },
    catalogLoading: { type: Boolean, default: false },
    isWholesale: { type: Boolean, default: false },
    embedded: { type: Boolean, default: false },
    showHero: { type: Boolean, default: true },
    showClose: { type: Boolean, default: true },
    showDone: { type: Boolean, default: true },
    unitPrice: { type: Function, required: true },
    formatPrice: { type: Function, required: true },
  },
  methods: {
    productImageSrc,
    isProductImageFallback,
    onProductImageError,
    focusSearch() {
      this.$nextTick(() => {
        this.$refs.catalogSearchInput?.focus?.();
        this.$refs.catalogSearchInput?.select?.();
      });
    },
  },
};
</script>
