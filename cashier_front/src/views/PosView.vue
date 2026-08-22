<template>
  <div>
    <b-overlay
      :show="show"
      spinner-variant="danger"
      spinner-type="grow"
      spinner-large
      rounded="sm"
    >
      <AppHeader>
        <template #pos-center>
          <div class="pos-quick-search pos-quick-search--header">
            <b-icon icon="search" class="pos-quick-search-icon" aria-hidden="true"></b-icon>
            <input
              v-model="quickSearch"
              ref="posQuickSearchInput"
              type="search"
              :placeholder="$t('searchPlaceholder')"
              class="pos-quick-search-input"
              :title="`${$t('searchPlaceholder') || 'بحث'} (F3)`"
              :aria-label="`${$t('searchPlaceholder') || 'بحث'} (F3)`"
            />
            <kbd class="pos-kbd pos-kbd--quick-search" title="F3">F3</kbd>
          </div>
        </template>
      </AppHeader>
      <div
        class="main-content-wrapper pos-route pos-route--v2"
        :class="{
          'pos-has-checkout-bar': showPosCheckoutBar,
          'pos-has-checkout-bar--with-discounts': carditems.length > 0,
          'pos-has-checkout-bar--change-calc': changeCalcOpen && carditems.length > 0,
        }"
      >
        <b-container fluid class="pos-container-fluid">
          <div class="pos-page-container pos-page-container--v2">
            <div class="pos-invoice-tabs" role="tablist" :aria-label="$t('posInvoiceTabs') || 'فواتير مفتوحة'">
              <button
                v-for="tab in invoiceTabs"
                :key="tab.id"
                type="button"
                role="tab"
                class="pos-invoice-tab"
                :class="{
                  'pos-invoice-tab--active': tab.id === activeInvoiceTabId,
                  'pos-invoice-tab--renaming': invoiceTabRenamingId === tab.id,
                }"
                :aria-selected="tab.id === activeInvoiceTabId"
                :title="$t('posInvoiceTabRenameHint') || 'نقرة مزدوجة لتعديل الاسم'"
                @click="switchInvoiceTab(tab.id)"
              >
                <input
                  v-if="invoiceTabRenamingId === tab.id"
                  :ref="'invoiceTabRename_' + tab.id"
                  v-model="invoiceTabRenameDraft"
                  type="text"
                  class="pos-invoice-tab-rename-input"
                  maxlength="40"
                  :aria-label="$t('posInvoiceTabRename') || 'اسم الفاتورة'"
                  @click.stop
                  @mousedown.stop
                  @keydown.enter.prevent="commitRenameInvoiceTab"
                  @keydown.esc.prevent="cancelRenameInvoiceTab"
                  @blur="commitRenameInvoiceTab"
                />
                <span
                  v-else
                  class="pos-invoice-tab-label"
                  @dblclick.stop.prevent="startRenameInvoiceTab(tab, $event)"
                >{{ invoiceTabLabel(tab) }}</span>
                <span
                  v-if="invoiceTabCount(tab) > 0 && invoiceTabRenamingId !== tab.id"
                  class="pos-invoice-tab-count"
                >{{ invoiceTabCount(tab) }}</span>
                <span
                  class="pos-invoice-tab-close"
                  role="button"
                  tabindex="0"
                  :title="$t('posInvoiceTabClose') || 'إغلاق'"
                  @click="requestCloseInvoiceTab(tab.id, $event)"
                  @keydown.enter.prevent="requestCloseInvoiceTab(tab.id, $event)"
                >
                  <b-icon icon="x"></b-icon>
                </span>
              </button>
              <button
                type="button"
                class="pos-invoice-tab-add"
                :disabled="!canAddInvoiceTab"
                :title="`${$t('posInvoiceTabNew') || 'فاتورة جديدة'} (F9)`"
                @click="addInvoiceTab"
              >
                <b-icon icon="plus-lg"></b-icon>
                <span class="pos-invoice-tab-add-text">{{ $t("posInvoiceTabNew") || "جديدة" }}</span>
                <kbd class="pos-kbd">F9</kbd>
              </button>
            </div>

            <div class="pos-workspace pos-workspace--v2">
              <main class="pos-workspace-main">
                <div class="pos-main-section pos-main-section--v2">
                  <div class="pos-quick-actions pos-quick-actions--barcode">
                    <label class="pos-quick-barcode">
                      <span class="pos-quick-barcode-icon" aria-hidden="true">
                        <b-icon icon="upc-scan"></b-icon>
                      </span>
                      <span class="pos-quick-barcode-field">
                        <span class="pos-quick-barcode-label">{{ $t("barcodeScanLabel") || "مسح الباركود" }}</span>
                        <input
                          v-model="searchCode"
                          ref="codeNumber"
                          type="text"
                          :placeholder="$t('barcodeScanPlaceholder') || 'امسح أو اكتب كود المنتج...'"
                          class="pos-quick-barcode-input"
                          :aria-label="$t('itemCodeLabel') || 'كود المنتج'"
                          autocomplete="off"
                          spellcheck="false"
                          autofocus
                          @keydown="handleBarcodeKeydown"
                          @input="handleBarcodeInput"
                          @paste="handleBarcodePaste"
                        />
                      </span>
                      <span class="pos-quick-barcode-actions">
                        <button
                          type="button"
                          class="pos-shortcuts-trigger"
                          :title="$t('posShortcutsTitle') || 'اختصارات لوحة المفاتيح'"
                          :aria-label="$t('posShortcutsTitle') || 'اختصارات لوحة المفاتيح'"
                          @click="showShortcutsModal = true"
                        >
                          <b-icon icon="keyboard-fill" aria-hidden="true"></b-icon>
                        </button>
                        <kbd class="pos-kbd pos-kbd--barcode">F2</kbd>
                        <span class="pos-quick-barcode-enter-hint" aria-hidden="true">
                          <span class="pos-quick-barcode-enter-text">Enter</span>
                          <b-icon icon="arrow-return-left"></b-icon>
                        </span>
                      </span>
                    </label>
                  </div>

                  <div class="pos-categories-scroll pos-categories-scroll--pills">
                    <div class="pos-categories-list pos-categories-list--pills">
                      <button
                        type="button"
                        class="pos-category-btn pos-category-btn--pill pos-category-btn-accent"
                        :class="{ 'pos-category-btn-active': activeCategory === '' }"
                        @click="selectCategory('')"
                      >
                        <span class="pos-category-btn-icon" aria-hidden="true">
                          <b-icon icon="grid-3x3-gap-fill"></b-icon>
                        </span>
                        <span class="pos-category-btn-label">{{ $t("all") }}</span>
                      </button>
                      <button
                        v-for="tag in tags"
                        :key="tag.id"
                        type="button"
                        class="pos-category-btn pos-category-btn--pill"
                        :class="{ 'pos-category-btn-active': activeCategory === tag.name }"
                        @click="selectCategory(tag.name)"
                      >
                        <span class="pos-category-btn-icon" aria-hidden="true">
                          <b-icon icon="tag-fill"></b-icon>
                        </span>
                        <span class="pos-category-btn-label">{{ tag.name }}</span>
                      </button>
                    </div>
                  </div>

                  <div ref="posProductsGridSection" class="pos-products-grid-section">
                    <div class="pos-products-grid">
                      <div
                        class="pos-product-card"
                        :class="{ 'pos-product-card-disabled': !item.quantity || item.quantity <= 0 }"
                        v-for="item in Items"
                        :key="item.id"
                        @click="item.quantity > 0 ? addToCartList(item) : null"
                      >
                        <div
                          v-if="!isWholesale && item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
                          class="pos-product-discount-badge"
                        >
                          <b-icon icon="tag-fill" class="me-1"></b-icon>
                          {{ $t("discountLabel") }}
                        </div>

                        <div class="pos-product-media">
                          <vue-barcode
                            v-if="showbarCode"
                            ref="BarImg"
                            tag="img"
                            class="pos-product-barcode"
                            :value="item.code.toString()"
                            :options="{
                              displayValue: true,
                              lineColor: '#2B2B2C',
                              width: 1.5,
                              height: 48,
                            }"
                          />
                          <div v-else class="pos-product-image-container">
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
                          <span
                            v-if="!item.quantity || item.quantity <= 0"
                            class="pos-product-stock-badge pos-product-stock-badge--out"
                          >
                            {{ $t("itemOutOfStock") || "غير متوفر" }}
                          </span>
                          <span
                            v-else
                            class="pos-product-stock-badge pos-product-stock-badge--qty"
                          >
                            {{ item.quantity }}
                          </span>
                        </div>

                        <div class="pos-product-info">
                          <h4 class="pos-product-name" :title="item.name">{{ item.name }}</h4>
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
                                {{ formatPrice(displayCatalogUnitPrice(item)) }} {{ $t("currency") }}
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

                    <div class="pos-pagination-section">
                      <b-pagination
                        v-model="pageNumber"
                        :total-rows="totalItems"
                        :per-page="pageSize"
                        aria-controls="pos-products"
                        class="pos-pagination"
                      >
                      </b-pagination>
                    </div>
                  </div>
                </div>
              </main>

              <aside
                class="pos-cart-shell"
                :class="{ 'pos-cart-shell--open': posMobileCartOpen }"
                :aria-label="$t('cart')"
              >
                <div
                  class="pos-cart-backdrop d-lg-none"
                  aria-hidden="true"
                  @click="closePosMobileCart"
                />
                <div class="pos-cart-panel pos-cart-panel--v2">
                  <header class="pos-cart-panel-head d-lg-none">
                    <span class="pos-cart-panel-brand">{{ $t("cart") }}</span>
                    <button type="button" class="pos-cart-panel-dismiss" @click="closePosMobileCart">
                      <b-icon icon="x-lg" />
                    </button>
                  </header>
                  <div class="pos-cart-container" ref="posCartScrollArea">
                    <div class="pos-cart-items-section">
                      <div class="pos-cart-header" ref="posCartHeader">
                        <h3 class="pos-cart-title">
                          <b-icon icon="cart-fill" class="me-2"></b-icon>
                          {{ invoiceTabLabel(activeInvoiceTab) || ($t("cart") || "السلة") }}
                          <span v-if="carditems.length > 0" class="pos-cart-count-badge pos-cart-count-badge--inline">
                            {{ totalCardItems }}
                          </span>
                        </h3>
                        <div class="pos-cart-header-actions">
                          <div class="pos-price-mode-toggle" role="group" :aria-label="$t('wholesalePriceMode')">
                            <button
                              type="button"
                              class="pos-price-mode-btn"
                              :class="{ 'pos-price-mode-btn-active': !isWholesale }"
                              @click="setPriceMode(false)"
                            >
                              {{ $t("retailPriceMode") || "مفرد" }}
                            </button>
                            <button
                              type="button"
                              class="pos-price-mode-btn"
                              :class="{ 'pos-price-mode-btn-active': isWholesale }"
                              @click="setPriceMode(true)"
                            >
                              {{ $t("wholesalePriceMode") || "جملة" }}
                            </button>
                          </div>
                          <button
                            v-if="carditems.length > 0"
                            type="button"
                            class="pos-cart-header-clear-btn"
                            v-b-modal.modal-empty
                            :disabled="totalCardItems <= 0"
                            :title="$t('emptyButton') || 'افراغ فقط'"
                          >
                            <b-icon icon="trash-fill" class="pos-cart-header-clear-ic"></b-icon>
                            <span class="pos-cart-header-clear-label">{{ $t("emptyButton") || "افراغ فقط" }}</span>
                          </button>
                        </div>
                      </div>
                      <div
                        v-if="warehouses.length"
                        class="pos-warehouse-bar"
                      >
                        <label class="pos-warehouse-bar__label" for="posWarehouseSelect">
                          <b-icon icon="building"></b-icon>
                          <span>{{ $t("selectWarehouse") }}</span>
                        </label>
                        <select
                          id="posWarehouseSelect"
                          class="pos-warehouse-bar__select"
                          v-model.number="selectedWarehouseId"
                          @change="onWarehouseChanged"
                        >
                          <option v-for="w in warehouses" :key="w.id" :value="w.id">
                            {{ w.name }}
                          </option>
                        </select>
                      </div>
                      <div
                        class="pos-cart-items-list"
                        v-if="carditems.length > 0"
                        ref="posCartItemsList"
                      >
                        <div
                          class="pos-cart-item pos-cart-item--v2"
                          v-for="(item, index) in carditems"
                          :key="index"
                          @dblclick="increaseQuantity(index)"
                        >
                          <div class="pos-cart-item-top">
                            <div class="pos-cart-item-name-wrap">
                              <h4 class="pos-cart-item-name">{{ item.name }}</h4>
                            </div>
                            <div class="pos-cart-item-line-total">
                              {{ formatPrice(item.total) }} {{ $t("currency") }}
                            </div>
                          </div>
                          <div class="pos-cart-item-bottom">
                            <div class="pos-cart-item-unit-wrap">
                              <span class="pos-cart-item-unit-price">
                                {{ formatPrice(cartLineUnitPrice(item)) }} × {{ item.quantity }}
                              </span>
                              <span v-if="cartLineHasDiscount(item)" class="pos-cart-item-discount-tag">
                                {{ $t("discountLabel") }}
                              </span>
                            </div>
                            <div class="pos-cart-item-controls">
                              <div class="pos-cart-item-quantity">
                                <button
                                  type="button"
                                  class="pos-quantity-btn pos-quantity-decrease"
                                  @click.stop="decreaseQuantity(index)"
                                  :title="$t('decrease') || 'تقليل'"
                                >
                                  <b-icon icon="dash-lg"></b-icon>
                                </button>
                                <input
                                  type="number"
                                  :value="item.quantity"
                                  @input="updateQuantity(index, $event.target.value)"
                                  @click.stop
                                  class="pos-quantity-input"
                                  min="1"
                                />
                                <button
                                  type="button"
                                  class="pos-quantity-btn pos-quantity-increase"
                                  @click.stop="increaseQuantity(index)"
                                  :title="$t('increase') || 'زيادة'"
                                >
                                  <b-icon icon="plus-lg"></b-icon>
                                </button>
                              </div>
                              <button
                                type="button"
                                class="pos-cart-item-delete"
                                @click.stop="deleteItem(index, { silent: true })"
                                :title="$t('delete') || 'حذف'"
                              >
                                <b-icon icon="x-lg"></b-icon>
                              </button>
                            </div>
                          </div>
                        </div>
                      </div>
                      <div
                        class="pos-cart-empty"
                        v-if="carditems.length === 0"
                      >
                        <div class="pos-cart-empty-inner">
                          <b-icon icon="cart-x" class="pos-cart-empty-icon"></b-icon>
                          <p class="pos-cart-empty-text">{{ $t("emptyCart") || "السلة فارغة" }}</p>
                          <p class="pos-cart-empty-hint">{{ $t("emptyCartHint") || "اختر منتجات من القائمة لإضافتها" }}</p>
                        </div>
                      </div>
                      <div v-if="carditems.length > 0" class="pos-cart-total-strip">
                        <div class="pos-cart-total-strip-row">
                          <span class="pos-cart-total-strip-label">{{ $t("countLabel") }}</span>
                          <strong>{{ totalCardItems }} {{ $t("itemLabel") }}</strong>
                        </div>
                        <div
                          v-if="orderDiscountAmount > 0"
                          class="pos-cart-total-strip-row pos-cart-total-strip-row--discount"
                        >
                          <span class="pos-cart-total-strip-label">{{ $t("discountLabel") }}</span>
                          <strong>− {{ formatPrice(orderDiscountAmount) }} {{ $t("currency") }}</strong>
                        </div>
                        <div class="pos-cart-total-strip-row pos-cart-total-strip-row--grand">
                          <span class="pos-cart-total-strip-label">{{ $t("totalLabel") }}</span>
                          <strong>{{ formattedNumber }} {{ $t("currency") }}</strong>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </aside>
            </div>

            <b-modal
              id="modal-pos-shortcuts"
              :visible.sync="showShortcutsModal"
              hide-header
              hide-footer
              centered
              size="md"
              modal-class="users-modal pos-ui-modal pos-shortcuts-modal"
              content-class="pos-ui-modal-content"
              body-class="pos-ui-modal-body"
            >
              <div class="modal-content-wrapper pos-ui-modal-wrapper">
                <div class="pos-ui-modal-hero">
                  <div class="pos-ui-modal-hero-icon" aria-hidden="true">
                    <b-icon icon="keyboard-fill"></b-icon>
                  </div>
                  <div class="pos-ui-modal-hero-text">
                    <h3 class="pos-ui-modal-title">
                      {{ $t("posShortcutsTitle") || "اختصارات لوحة المفاتيح" }}
                    </h3>
                    <p class="pos-ui-modal-subtitle">
                      {{ $t("posShortcutsSubtitle") || "استخدم الاختصارات لتسريع عمليات البيع والدفع" }}
                    </p>
                  </div>
                  <button
                    type="button"
                    class="pos-ui-modal-close"
                    :aria-label="$t('close') || 'إغلاق'"
                    @click="showShortcutsModal = false"
                  >
                    <b-icon icon="x-lg"></b-icon>
                  </button>
                </div>

                <div class="pos-ui-modal-body-content">
                  <div class="pos-shortcuts-list" :aria-label="$t('posShortcutsTitle')">
                    <section class="pos-shortcuts-section">
                      <header class="pos-shortcuts-section-head">
                        <span class="pos-shortcuts-section-icon pos-shortcuts-section-icon--pay" aria-hidden="true">
                          <b-icon icon="cash-coin"></b-icon>
                        </span>
                        <h4 class="pos-shortcuts-section-title">{{ $t("posShortcutGroupPayment") || "الدفع" }}</h4>
                      </header>
                      <ul class="pos-shortcuts-rows">
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("payNow") || "دفع" }}</span>
                          <kbd class="pos-shortcuts-key pos-shortcuts-key--pay">F4</kbd>
                        </li>
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("payAndPrint") || "دفع وطباعة" }}</span>
                          <kbd class="pos-shortcuts-key pos-shortcuts-key--pay">F5</kbd>
                        </li>
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("printOnly") || "طباعة فقط" }}</span>
                          <kbd class="pos-shortcuts-key">F6</kbd>
                        </li>
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("changeCalculator") || "حاسبة الباقي" }}</span>
                          <kbd class="pos-shortcuts-key">F7</kbd>
                        </li>
                      </ul>
                    </section>

                    <section class="pos-shortcuts-section">
                      <header class="pos-shortcuts-section-head">
                        <span class="pos-shortcuts-section-icon pos-shortcuts-section-icon--order" aria-hidden="true">
                          <b-icon icon="receipt"></b-icon>
                        </span>
                        <h4 class="pos-shortcuts-section-title">{{ $t("posShortcutGroupOrder") || "الطلب" }}</h4>
                      </header>
                      <ul class="pos-shortcuts-rows">
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("barcodeScanLabel") || "باركود" }}</span>
                          <kbd class="pos-shortcuts-key">F2</kbd>
                        </li>
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("searchPlaceholder") || "بحث" }}</span>
                          <kbd class="pos-shortcuts-key">F3</kbd>
                        </li>
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("discountAndNotes") || "خصم وملاحظات" }}</span>
                          <kbd class="pos-shortcuts-key">F8</kbd>
                        </li>
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("posInvoiceTabNew") || "فاتورة جديدة" }}</span>
                          <kbd class="pos-shortcuts-key">F9</kbd>
                        </li>
                      </ul>
                    </section>

                    <section class="pos-shortcuts-section">
                      <header class="pos-shortcuts-section-head">
                        <span class="pos-shortcuts-section-icon pos-shortcuts-section-icon--cart" aria-hidden="true">
                          <b-icon icon="cart3"></b-icon>
                        </span>
                        <h4 class="pos-shortcuts-section-title">{{ $t("posShortcutGroupCart") || "السلة" }}</h4>
                      </header>
                      <ul class="pos-shortcuts-rows">
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("quantity") || "الكمية" }}</span>
                          <span class="pos-shortcuts-keys">
                            <kbd class="pos-shortcuts-key">+</kbd>
                            <kbd class="pos-shortcuts-key">−</kbd>
                          </span>
                        </li>
                        <li class="pos-shortcuts-row">
                          <span class="pos-shortcuts-row-label">{{ $t("posShortcutRemoveLast") || "حذف آخر منتج" }}</span>
                          <kbd class="pos-shortcuts-key pos-shortcuts-key--danger">Del</kbd>
                        </li>
                      </ul>
                    </section>
                  </div>
                </div>

                <div class="pos-ui-modal-actions pos-ui-modal-actions--single">
                  <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--primary" @click="showShortcutsModal = false">
                    <b-icon icon="check-lg"></b-icon>
                    {{ $t("close") || "إغلاق" }}
                  </button>
                </div>
              </div>
            </b-modal>

            <b-modal
              id="modal-empty"
              hide-header
              hide-footer
              centered
              size="md"
              modal-class="users-modal pos-ui-modal pos-ui-modal--sm"
              content-class="pos-ui-modal-content"
              body-class="pos-ui-modal-body"
            >
              <div class="modal-content-wrapper pos-ui-modal-wrapper">
                <div class="pos-ui-modal-hero pos-ui-modal-hero--danger">
                  <div class="pos-ui-modal-hero-icon" aria-hidden="true">
                    <b-icon icon="trash-fill"></b-icon>
                  </div>
                  <div class="pos-ui-modal-hero-text">
                    <h3 class="pos-ui-modal-title">{{ $t("confirmClearCartTitle") }}</h3>
                    <p class="pos-ui-modal-subtitle">{{ $t("confirmClearCartMessage") }}</p>
                  </div>
                  <button
                    type="button"
                    class="pos-ui-modal-close"
                    :aria-label="$t('close') || 'إغلاق'"
                    @click="closeModel('modal-empty')"
                  >
                    <b-icon icon="x-lg"></b-icon>
                  </button>
                </div>
                <div class="pos-ui-modal-body-content">
                  <div class="pos-ui-modal-note pos-ui-modal-note--danger">
                    <b-icon icon="exclamation-triangle-fill" class="pos-ui-modal-note-icon" aria-hidden="true"></b-icon>
                    <p class="pos-ui-modal-note-text">
                      {{ $t("confirmClearCartHint") || "سيتم حذف جميع المنتجات من السلة الحالية ولا يمكن التراجع عن هذا الإجراء." }}
                    </p>
                  </div>
                </div>
                <div class="pos-ui-modal-actions">
                  <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--secondary" @click="closeModel('modal-empty')">
                    <b-icon icon="x-circle-fill"></b-icon>
                    {{ $t("cancelButton") }}
                  </button>
                  <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--danger" @click="EmptycardList('modal-empty')">
                    <b-icon icon="trash-fill"></b-icon>
                    {{ $t("confirmButton") }}
                  </button>
                </div>
              </div>
            </b-modal>

            <b-modal
              id="modal-close-invoice-tab"
              hide-header
              hide-footer
              centered
              size="md"
              modal-class="users-modal pos-ui-modal pos-ui-modal--sm"
              content-class="pos-ui-modal-content"
              body-class="pos-ui-modal-body"
              @hidden="invoiceTabPendingCloseId = null"
            >
              <div class="modal-content-wrapper pos-ui-modal-wrapper">
                <div class="pos-ui-modal-hero pos-ui-modal-hero--danger">
                  <div class="pos-ui-modal-hero-icon" aria-hidden="true">
                    <b-icon icon="x-octagon-fill"></b-icon>
                  </div>
                  <div class="pos-ui-modal-hero-text">
                    <h3 class="pos-ui-modal-title">
                      {{ $t("posInvoiceTabCloseTitle") || "إغلاق الفاتورة؟" }}
                    </h3>
                    <p class="pos-ui-modal-subtitle">
                      {{ $t("posInvoiceTabCloseMessage") || "هذه الفاتورة تحتوي أصنافاً. هل تريد إغلاقها وفقدان محتوياتها؟" }}
                    </p>
                  </div>
                  <button
                    type="button"
                    class="pos-ui-modal-close"
                    :aria-label="$t('close') || 'إغلاق'"
                    @click="closeModel('modal-close-invoice-tab')"
                  >
                    <b-icon icon="x-lg"></b-icon>
                  </button>
                </div>
                <div class="pos-ui-modal-body-content">
                  <div class="pos-ui-modal-note pos-ui-modal-note--danger">
                    <b-icon icon="exclamation-triangle-fill" class="pos-ui-modal-note-icon" aria-hidden="true"></b-icon>
                    <p class="pos-ui-modal-note-text">
                      {{ $t("posInvoiceTabCloseHint") || "إغلاق الفاتورة سيفقد الأصناف غير المحفوظة فيها." }}
                    </p>
                  </div>
                </div>
                <div class="pos-ui-modal-actions">
                  <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--secondary" @click="closeModel('modal-close-invoice-tab')">
                    <b-icon icon="x-circle-fill"></b-icon>
                    {{ $t("cancelButton") || "إلغاء" }}
                  </button>
                  <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--danger" @click="confirmCloseInvoiceTab">
                    <b-icon icon="check-circle-fill"></b-icon>
                    {{ $t("confirmButton") || "تأكيد" }}
                  </button>
                </div>
              </div>
            </b-modal>

            <b-modal
              id="modal-order-notes"
              hide-header
              hide-footer
              centered
              size="lg"
              modal-class="users-modal pos-ui-modal pos-ui-modal--lg"
              content-class="pos-ui-modal-content"
              body-class="pos-ui-modal-body"
            >
              <div class="modal-content-wrapper pos-ui-modal-wrapper">
                <div class="pos-ui-modal-hero">
                  <div class="pos-ui-modal-hero-icon" aria-hidden="true">
                    <b-icon icon="tag-fill"></b-icon>
                  </div>
                  <div class="pos-ui-modal-hero-text">
                    <h3 class="pos-ui-modal-title">{{ $t("discountAndNotes") || "خصم وملاحظات" }}</h3>
                    <p class="pos-ui-modal-subtitle">
                      {{ $t("discountAndNotesHint") || "أضف ملاحظات الطلب أو خصماً على الفاتورة الحالية" }}
                    </p>
                  </div>
                  <button
                    type="button"
                    class="pos-ui-modal-close"
                    :aria-label="$t('close') || 'إغلاق'"
                    @click="closeModel('modal-order-notes')"
                  >
                    <b-icon icon="x-lg"></b-icon>
                  </button>
                </div>
                <form @submit.prevent="applyOrderExtras">
                  <div class="pos-ui-modal-body-content">
                    <div class="order-notes-content">
                      <div class="order-notes-input-wrapper">
                        <label class="order-notes-label" for="pos-order-notes">{{ $t("notesLabel") || "الملاحظات (اختياري)" }}</label>
                        <textarea
                          id="pos-order-notes"
                          v-model="orderForSend.notes"
                          class="order-notes-textarea"
                          :placeholder="$t('notesPlaceholder') || 'اكتب ملاحظاتك هنا...'"
                          rows="3"
                        ></textarea>
                      </div>
                      <div class="order-notes-input-wrapper order-discount-wrapper">
                        <label class="order-notes-label">{{ $t("orderDiscount") || "خصم الطلب" }}</label>
                        <div class="order-discount-type-toggle">
                          <button
                            type="button"
                            class="order-discount-type-btn"
                            :class="{ 'order-discount-type-btn-active': orderDiscountType === 'amount' }"
                            @click="orderDiscountType = 'amount'"
                          >
                            {{ $t("discountByAmount") || "مبلغ" }}
                          </button>
                          <button
                            type="button"
                            class="order-discount-type-btn"
                            :class="{ 'order-discount-type-btn-active': orderDiscountType === 'percentage' }"
                            @click="orderDiscountType = 'percentage'"
                          >
                            {{ $t("discountByPercentage") || "نسبة" }}
                          </button>
                        </div>
                        <div class="order-discount-input-row">
                          <input
                            v-model.number="orderDiscountValue"
                            type="number"
                            min="0"
                            :max="orderDiscountType === 'percentage' ? 100 : null"
                            class="order-notes-input"
                            :placeholder="orderDiscountType === 'percentage' ? (($t('discountPercentPlaceholder') || 'ادخل النسبة %')) : (($t('discountAmountPlaceholder') || 'ادخل مبلغ الخصم'))"
                          />
                          <button type="button" class="order-discount-clear-btn" @click="clearOrderDiscount">
                            {{ $t("clear") || "مسح" }}
                          </button>
                        </div>
                        <div class="order-discount-presets">
                          <button
                            v-for="preset in orderDiscountPresets"
                            :key="preset.id"
                            type="button"
                            class="order-discount-preset-btn"
                            @click="applyOrderDiscountPreset(preset)"
                          >
                            {{ preset.label }} {{ preset.type === "amount" ? $t("currency") : "" }}
                          </button>
                        </div>
                        <div class="order-discount-preview">
                          <div class="order-discount-preview-row">
                            <span>{{ $t("subtotal") || "المجموع قبل الخصم" }}</span>
                            <strong>{{ formatPrice(totaPrice) }} {{ $t("currency") }}</strong>
                          </div>
                          <div class="order-discount-preview-row">
                            <span>{{ $t("discountLabel") || "الخصم" }} ({{ orderDiscountPreviewLabel }})</span>
                            <strong>- {{ formatPrice(orderDiscountAmount) }} {{ $t("currency") }}</strong>
                          </div>
                          <div class="order-discount-preview-row order-discount-preview-row-total">
                            <span>{{ $t("totalLabel") || "الإجمالي" }}</span>
                            <strong>{{ formattedNumber }} {{ $t("currency") }}</strong>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="pos-ui-modal-actions">
                    <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--secondary" @click="closeModel('modal-order-notes')">
                      <b-icon icon="x-circle-fill"></b-icon>
                      {{ $t("cancelButton") || "تراجع" }}
                    </button>
                    <button type="submit" class="pos-ui-modal-btn pos-ui-modal-btn--primary">
                      <b-icon icon="check-circle-fill"></b-icon>
                      {{ $t("apply") || "تطبيق" }}
                    </button>
                  </div>
                </form>
              </div>
            </b-modal>

            <CardPaymentWaitModal
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
              @cancel="onCardPaymentWaitCancel"
              @close="onCardPaymentWaitClose"
            />

            <b-modal
              id="modal-print-only-confirm"
              hide-header
              hide-footer
              centered
              size="md"
              modal-class="users-modal pos-ui-modal pos-ui-modal--sm"
              content-class="pos-ui-modal-content"
              body-class="pos-ui-modal-body"
            >
              <div class="modal-content-wrapper pos-ui-modal-wrapper">
                <div class="pos-ui-modal-hero pos-ui-modal-hero--print">
                  <div class="pos-ui-modal-hero-icon" aria-hidden="true">
                    <b-icon icon="printer-fill"></b-icon>
                  </div>
                  <div class="pos-ui-modal-hero-text">
                    <h3 class="pos-ui-modal-title">{{ $t("printOnly") || "طباعة فقط" }}</h3>
                    <p class="pos-ui-modal-subtitle">
                      {{ $t("confirmPrintOnlyMessage") || "هل أنت متأكد من تنفيذ الطباعة فقط؟" }}
                    </p>
                  </div>
                  <button
                    type="button"
                    class="pos-ui-modal-close"
                    :aria-label="$t('close') || 'إغلاق'"
                    @click="closeModel('modal-print-only-confirm')"
                  >
                    <b-icon icon="x-lg"></b-icon>
                  </button>
                </div>
                <div class="pos-ui-modal-body-content">
                  <div class="pos-ui-modal-note">
                    <b-icon icon="info-circle-fill" class="pos-ui-modal-note-icon" aria-hidden="true"></b-icon>
                    <p class="pos-ui-modal-note-text">
                      {{ $t("confirmPrintOnlyHint") || "سيتم طباعة الفاتورة الحالية دون تسجيل عملية دفع." }}
                    </p>
                  </div>
                </div>
                <div class="pos-ui-modal-actions">
                  <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--secondary" @click="closeModel('modal-print-only-confirm')">
                    <b-icon icon="x-circle-fill"></b-icon>
                    {{ $t("cancelButton") || "تراجع" }}
                  </button>
                  <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--primary" @click="confirmPrintCartOnly">
                    <b-icon icon="printer-fill"></b-icon>
                    {{ $t("confirm") || "تأكيد" }}
                  </button>
                </div>
              </div>
            </b-modal>

            <b-modal
              id="modal-credit-payment"
              hide-header
              hide-footer
              centered
              size="lg"
              modal-class="users-modal pos-ui-modal pos-ui-modal--lg"
              content-class="pos-ui-modal-content"
              body-class="pos-ui-modal-body"
            >
              <div class="modal-content-wrapper pos-ui-modal-wrapper">
                <div class="pos-ui-modal-hero">
                  <div class="pos-ui-modal-hero-icon" aria-hidden="true">
                    <b-icon icon="wallet2"></b-icon>
                  </div>
                  <div class="pos-ui-modal-hero-text">
                    <h3 class="pos-ui-modal-title">{{ $t("creditPaymentModalTitle") }}</h3>
                    <p class="pos-ui-modal-subtitle">
                      {{ $t("creditPaymentModalHint") || "اختر عميل الدفع الآجل لإتمام العملية" }}
                    </p>
                  </div>
                  <button
                    type="button"
                    class="pos-ui-modal-close"
                    :aria-label="$t('close') || 'إغلاق'"
                    @click="cancelCreditPaymentModal"
                  >
                    <b-icon icon="x-lg"></b-icon>
                  </button>
                </div>
                <form @submit.prevent="confirmCreditPaymentSelection">
                  <div class="pos-ui-modal-body-content">
                    <div class="users-form-group mb-0">
                      <label class="users-form-label">
                        <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                        {{ $t("selectCreditCustomer") }}
                      </label>
                      <div class="credit-customer-select-row">
                        <select
                          v-model="orderForSend.creditCustomerId"
                          class="users-form-select"
                          :disabled="loadingCreditCustomers"
                        >
                          <option value="">{{ $t("selectCreditCustomer") }}</option>
                          <option
                            v-for="c in creditCustomers.filter((x) => x.isActive !== false)"
                            :key="'cc-' + c.id"
                            :value="c.id"
                          >
                            {{ c.name }} — {{ c.phoneNumber }}
                          </option>
                        </select>
                        <button
                          type="button"
                          class="credit-quick-add-btn"
                          :disabled="loadingCreditCustomers || savingCreditCustomer"
                          @click="openQuickAddCustomerForCredit"
                        >
                          <b-icon icon="person-plus-fill" class="me-2"></b-icon>
                          {{ $t("quickAddCreditCustomer") || "عميل جديد" }}
                        </button>
                      </div>
                      <p class="users-form-hint">
                        {{ $t("quickAddCreditCustomerHint") || "إذا لم يكن العميل مسجلاً، أضفه مباشرة من هنا" }}
                      </p>
                    </div>
                  </div>
                  <div class="pos-ui-modal-actions">
                    <button type="button" class="pos-ui-modal-btn pos-ui-modal-btn--secondary" @click="cancelCreditPaymentModal">
                      <b-icon icon="x-circle-fill"></b-icon>
                      {{ $t("cancelButton") }}
                    </button>
                    <button type="submit" class="pos-ui-modal-btn pos-ui-modal-btn--primary">
                      <b-icon icon="check-circle-fill"></b-icon>
                      {{ $t("confirm") || "تأكيد" }}
                    </button>
                  </div>
                </form>
              </div>
            </b-modal>

            <b-modal
              v-model="showAddCreditCustomerModal"
              hide-header
              hide-footer
              centered
              size="lg"
              modal-class="users-modal pos-ui-modal pos-ui-modal--lg"
              content-class="pos-ui-modal-content"
              body-class="pos-ui-modal-body"
              @hidden="resetNewCreditCustomerForm"
            >
              <div class="modal-content-wrapper pos-ui-modal-wrapper">
                <div class="pos-ui-modal-hero">
                  <div class="pos-ui-modal-hero-icon" aria-hidden="true">
                    <b-icon icon="person-plus-fill"></b-icon>
                  </div>
                  <div class="pos-ui-modal-hero-text">
                    <h3 class="pos-ui-modal-title">
                      {{ $t("quickAddCreditCustomerModal") || "إضافة عميل للدفع الآجل" }}
                    </h3>
                    <p class="pos-ui-modal-subtitle">
                      {{ $t("quickAddCreditCustomerModalHint") || "أدخل بيانات العميل لإضافته واستخدامه مباشرة" }}
                    </p>
                  </div>
                  <button
                    type="button"
                    class="pos-ui-modal-close"
                    :aria-label="$t('close') || 'إغلاق'"
                    :disabled="savingCreditCustomer"
                    @click="showAddCreditCustomerModal = false"
                  >
                    <b-icon icon="x-lg"></b-icon>
                  </button>
                </div>
                <form @submit.prevent="saveNewCreditCustomer">
                  <div class="pos-ui-modal-body-content">
                    <div class="users-form">
                      <div class="modal-form-grid">
                        <div class="users-form-group">
                          <label class="users-form-label">
                            <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                            {{ $t("customerNameField") || "اسم العميل" }} <span class="required">*</span>
                          </label>
                          <input
                            v-model="newCreditCustomerForm.name"
                            type="text"
                            class="users-form-input"
                            :placeholder="$t('enterCustomerNamePlaceholder') || 'أدخل الاسم'"
                            required
                          />
                        </div>
                        <div class="users-form-group">
                          <label class="users-form-label">
                            <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                            {{ $t("phoneNumber") }} <span class="required">*</span>
                          </label>
                          <input
                            v-model="newCreditCustomerForm.phoneNumber"
                            type="text"
                            class="users-form-input"
                            :placeholder="$t('enterPhoneNumber') || 'أدخل رقم الهاتف'"
                            required
                          />
                        </div>
                      </div>
                      <div class="users-form-group">
                        <label class="users-form-label">
                          <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                          {{ $t("address") }}
                        </label>
                        <input
                          v-model="newCreditCustomerForm.address"
                          type="text"
                          class="users-form-input"
                          :placeholder="$t('enterAddress') || 'العنوان (اختياري)'"
                        />
                      </div>
                      <div class="users-form-group mb-0">
                        <label class="users-form-label">
                          <b-icon icon="chat-left-text-fill" class="form-label-icon"></b-icon>
                          {{ $t("notes") }}
                        </label>
                        <textarea
                          v-model="newCreditCustomerForm.notes"
                          class="users-form-input"
                          rows="2"
                          :placeholder="$t('customerNotesPlaceholder') || ''"
                        ></textarea>
                      </div>
                    </div>
                  </div>
                  <div class="pos-ui-modal-actions">
                    <button
                      type="button"
                      class="pos-ui-modal-btn pos-ui-modal-btn--secondary"
                      :disabled="savingCreditCustomer"
                      @click="showAddCreditCustomerModal = false"
                    >
                      <b-icon icon="x-circle-fill"></b-icon>
                      {{ $t("cancel") }}
                    </button>
                    <button type="submit" class="pos-ui-modal-btn pos-ui-modal-btn--primary" :disabled="savingCreditCustomer">
                      <b-spinner v-if="savingCreditCustomer" small></b-spinner>
                      <b-icon v-else icon="person-plus-fill"></b-icon>
                      {{ savingCreditCustomer ? ($t("adding") || "جاري الإضافة...") : ($t("add") || "إضافة") }}
                    </button>
                  </div>
                </form>
              </div>
            </b-modal>

            <div v-if="showPosCheckoutBar" class="pos-cart-checkout-bar">
              <div class="pos-cart-checkout-bar-inner">
                <div v-if="carditems.length > 0" class="pos-checkout-quick-row">
                  <span class="pos-cart-checkout-segment-label">{{ $t("quickDiscount") || "خصم سريع" }}</span>
                  <div class="pos-checkout-discount-presets">
                    <button
                      v-for="preset in orderDiscountPresets"
                      :key="preset.id"
                      type="button"
                      class="order-discount-preset-btn"
                      @click="applyOrderDiscountPreset(preset)"
                    >
                      {{ preset.label }}{{ preset.type === "amount" ? ` ${$t("currency")}` : "" }}
                    </button>
                    <button
                      v-if="orderDiscountAmount > 0"
                      type="button"
                      class="order-discount-clear-btn"
                      @click="clearOrderDiscount"
                    >
                      {{ $t("clear") || "مسح" }}
                    </button>
                  </div>
                </div>

                <div v-if="carditems.length > 0 && changeCalcOpen" class="pos-checkout-change-panel">
                  <div class="pos-change-calc-grid">
                    <div class="pos-change-calc-field">
                      <span class="pos-change-calc-label">{{ $t("changeCalcOrderTotal") }}</span>
                      <strong class="pos-change-calc-total">
                        {{ formatPrice(finalOrderTotal) }} {{ $t("currency") }}
                      </strong>
                    </div>
                    <div class="pos-change-calc-field">
                      <label class="pos-change-calc-label" for="pos-customer-paid-input">
                        {{ $t("changeCalcAmountReceived") }}
                      </label>
                      <input
                        id="pos-customer-paid-input"
                        ref="customerPaidInput"
                        v-model.number="customerPaidAmount"
                        type="number"
                        min="0"
                        step="250"
                        class="pos-change-calc-input"
                        :placeholder="$t('changeCalcAmountReceivedPlaceholder')"
                        @keyup.enter="focusPosBarcode"
                      />
                    </div>
                    <div class="pos-change-calc-field pos-change-calc-field--result">
                      <span class="pos-change-calc-label">{{ $t("changeCalcChangeDue") }}</span>
                      <strong
                        class="pos-change-calc-result"
                        :class="{
                          'pos-change-calc-result--ok': changeDueAmount > 0,
                          'pos-change-calc-result--exact': changeDueAmount === 0 && customerPaidAmount > 0,
                          'pos-change-calc-result--warn': isInsufficientPayment,
                        }"
                      >
                        <template v-if="isInsufficientPayment">
                          {{ $t("changeCalcInsufficient") }} − {{ formatPrice(paymentShortfall) }} {{ $t("currency") }}
                        </template>
                        <template v-else-if="customerPaidAmount > 0">
                          {{ formatPrice(changeDueAmount) }} {{ $t("currency") }}
                        </template>
                        <template v-else>—</template>
                      </strong>
                    </div>
                  </div>
                  <div class="pos-change-calc-presets">
                    <button type="button" class="pos-change-calc-preset-btn" @click="setCustomerPaidAmount(finalOrderTotal)">
                      {{ $t("changeCalcExactAmount") }}
                    </button>
                    <button
                      v-for="amount in changeCalcQuickAmounts"
                      :key="amount"
                      type="button"
                      class="pos-change-calc-preset-btn"
                      @click="setCustomerPaidAmount(amount)"
                    >
                      {{ formatPrice(amount) }} {{ $t("currency") }}
                    </button>
                    <button type="button" class="pos-change-calc-clear-btn" @click="resetChangeCalculator(true)">
                      {{ $t("clear") }}
                    </button>
                  </div>
                </div>

                <div class="pos-cart-checkout-strip">
                  <template v-if="carditems.length > 0">
                    <div class="pos-cart-checkout-segment pos-cart-checkout-segment--stats">
                      <span class="pos-cart-checkout-segment-label">{{ $t("checkoutSummary") }}</span>
                      <div class="pos-cart-checkout-btn-row pos-cart-checkout-stats-row">
                        <span class="pos-cart-checkout-stat pos-cart-checkout-stat--pill">
                          <b-icon icon="box-seam" class="pos-cart-checkout-ic"></b-icon>
                          <span class="pos-cart-checkout-stat-text">{{ $t("countLabel") }}</span>
                          <strong>{{ totalCardItems }} {{ $t("itemLabel") }}</strong>
                        </span>
                        <span
                          v-if="orderDiscountAmount > 0"
                          class="pos-cart-checkout-stat pos-cart-checkout-stat--pill pos-cart-checkout-stat--pill-discount"
                        >
                          <b-icon icon="tag-fill" class="pos-cart-checkout-ic"></b-icon>
                          <span class="pos-cart-checkout-stat-text">− {{ formatPrice(orderDiscountAmount) }} {{ $t("currency") }}</span>
                        </span>
                        <span class="pos-cart-checkout-stat pos-cart-checkout-stat--pill pos-cart-checkout-stat--pill-total">
                          <span class="pos-cart-checkout-stat-text">{{ $t("totalLabel") }}</span>
                          <strong>{{ formattedNumber }} {{ $t("currency") }}</strong>
                        </span>
                      </div>
                    </div>

                    <div class="pos-cart-checkout-segment pos-cart-checkout-segment--actions">
                      <span class="pos-cart-checkout-segment-label">{{ $t("checkoutActions") }}</span>
                      <div class="pos-cart-checkout-btn-row pos-cart-checkout-summary-actions">
                        <button
                          type="button"
                          class="pos-action-btn pos-action-btn-primary pos-cart-checkout-action-btn"
                          @click="quickPay(false)"
                          :disabled="totalCardItems <= 0 || orderPersisting"
                          :title="`${$t('payNow') || 'دفع'} (F4)`"
                        >
                          <b-icon icon="check-circle-fill" class="me-1"></b-icon>
                          {{ $t("payNow") || "دفع" }}
                          <kbd class="pos-kbd">F4</kbd>
                        </button>
                        <button
                          type="button"
                          class="pos-action-btn pos-action-btn-success pos-cart-checkout-action-btn"
                          @click="quickPay(true)"
                          :disabled="totalCardItems <= 0 || orderPersisting"
                          :title="`${$t('payAndPrint') || 'دفع وطباعة'} (F5)`"
                        >
                          <b-icon icon="receipt-cutoff" class="me-1"></b-icon>
                          {{ $t("payAndPrint") || "دفع وطباعة" }}
                          <kbd class="pos-kbd">F5</kbd>
                        </button>
                        <button
                          type="button"
                          class="pos-action-btn pos-action-btn-secondary pos-cart-checkout-action-btn"
                          @click="openPrintOnlyConfirm"
                          :disabled="totalCardItems <= 0"
                          :title="`${$t('printOnly') || 'طباعة فقط'} (F6)`"
                        >
                          <b-icon icon="printer-fill" class="me-1"></b-icon>
                          {{ $t("printOnly") || "طباعة فقط" }}
                          <kbd class="pos-kbd">F6</kbd>
                        </button>
                        <button
                          type="button"
                          class="pos-action-btn pos-action-btn-secondary pos-cart-checkout-action-btn"
                          :class="{ 'pos-cart-checkout-action-btn--active': changeCalcOpen }"
                          @click="toggleChangeCalculator"
                          :disabled="totalCardItems <= 0"
                          :title="`${$t('changeCalculator') || 'حاسبة الباقي'} (F7)`"
                        >
                          <b-icon icon="calculator-fill" class="me-1"></b-icon>
                          {{ $t("changeCalculator") }}
                          <kbd class="pos-kbd">F7</kbd>
                        </button>
                        <button
                          type="button"
                          class="pos-action-btn pos-action-btn-secondary pos-cart-checkout-action-btn"
                          @click="openOrderExtrasModal"
                          :disabled="totalCardItems <= 0"
                          :title="`${$t('discountAndNotes') || 'خصم وملاحظات'} (F8)`"
                        >
                          <b-icon icon="tag-fill" class="me-1"></b-icon>
                          {{ $t("discountAndNotes") || "خصم وملاحظات" }}
                          <kbd class="pos-kbd">F8</kbd>
                        </button>
                      </div>
                    </div>

                    <div class="pos-cart-checkout-segment pos-cart-checkout-segment--pay">
                      <span class="pos-cart-checkout-segment-label">{{ $t("paymentMethod") }}</span>
                      <div class="pos-cart-checkout-btn-row pos-cart-checkout-pay-row">
                        <button
                          type="button"
                          class="pos-payment-method-btn"
                          :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Cash' }"
                          @click="setPosPaymentMethod('Cash')"
                        >
                          <b-icon icon="cash-stack" class="pos-payment-icon"></b-icon>
                          <span class="pos-payment-label">{{ $t("cash") || "نقد" }}</span>
                        </button>
                        <button
                          type="button"
                          class="pos-payment-method-btn"
                          :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Card' }"
                          @click="setPosPaymentMethod('Card')"
                        >
                          <b-icon icon="credit-card" class="pos-payment-icon"></b-icon>
                          <span class="pos-payment-label">{{ $t("card") || "بطاقة" }}</span>
                        </button>
                        <button
                          type="button"
                          class="pos-payment-method-btn"
                          :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Credit' }"
                          @click="openCreditPaymentModal"
                        >
                          <b-icon icon="clock-history" class="pos-payment-icon"></b-icon>
                          <span class="pos-payment-label">{{ $t("credit") || "دفع لاحق" }}</span>
                        </button>
                      </div>
                    </div>
                  </template>

                  <div
                    v-if="activeCheckoutPrinters.length > 0 || loadingManagedPrinters"
                    class="pos-cart-checkout-segment pos-cart-checkout-segment--printer"
                  >
                    <span class="pos-cart-checkout-segment-label">{{ $t("selectPrinter") || "الطابعة" }}</span>
                    <select
                      v-if="activeCheckoutPrinters.length > 0"
                      v-model="selectedManagedPrinterId"
                      @change="onManagedPrinterChange"
                      class="pos-cart-checkout-printer-select"
                    >
                      <option
                        v-for="printer in activeCheckoutPrinters"
                        :key="printer.id"
                        :value="printer.id"
                      >
                        {{ printer.name }}{{ printer.isMain ? ` (${$t("mainPrinter") || "رئيسية"})` : "" }}
                      </option>
                    </select>
                    <span v-else class="pos-cart-checkout-printer-loading">
                      {{ $t("loadingPrinters") || "جاري تحميل الطابعات..." }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </b-container>

        <button
          v-show="!posMobileCartOpen"
          type="button"
          class="pos-mobile-cart-fab d-lg-none"
          @click="openPosMobileCart"
          :aria-label="$t('posOpenCart')"
          :title="$t('posOpenCart')"
        >
          <b-icon icon="cart-fill" class="pos-mobile-cart-fab-icon"></b-icon>
          <span v-if="carditems.length > 0" class="pos-mobile-cart-fab-badge">{{ carditems.length }}</span>
        </button>
      </div>
      <b-sidebar id="sidebar-right" title="Sidebar" no-header right shadow>
        <div class="px-3 py-2">
          <CalculatorComp />
        </div>
      </b-sidebar>
    </b-overlay>

<!-- Print Section (Hidden) -->
    <div class="print_hide" id="print" style="display: none;">
      <div class="bill-container">
        <!-- Header Section -->
        <div class="bill-header">
          <div class="bill-logo-section">
            <img
              v-if="commercialUserInfo.logo"
              :src="commercialUserInfo.logo"
              alt="logo"
              class="bill-logo-img"
            />
            <img
              v-else
              src="../assets/logo.png"
              alt="logo"
              class="bill-logo-img"
            />
          </div>
          <div class="bill-store-info">
            <h2 class="bill-store-name">{{ commercialUserInfo.storeName || 'LiteCashier' }}</h2>
            <p class="bill-store-subtitle">{{ $t("app-name") }}</p>
          </div>
        </div>

        <!-- Invoice Info Section -->
        <div class="bill-info-section">
          <div class="bill-info-row">
            <span class="bill-info-label">{{ $t("invoice_number") }}:</span>
            <span class="bill-info-value">{{ orderForSend.orderCode || '---' }}</span>
          </div>
          <!-- Barcode for Order Number -->
          <div class="bill-barcode-section" v-if="orderForSend.orderCode">
            <vue-barcode
              tag="img"
              class="bill-barcode-img"
              :value="orderForSend.orderCode.toString()"
              :options="{
                displayValue: true,
                fontSize: 12,
                height: 40,
                width: 1.5,
                margin: 0
              }"
            />
          </div>
          <div class="bill-info-row">
            <span class="bill-info-label">{{ $t("employeeLabel") }}:</span>
            <span class="bill-info-value">{{ userInfo.name || userInfo.fullName || '---' }}</span>
          </div>
          <div class="bill-info-row" v-if="orderForSend.orderType">
            <span class="bill-info-label">{{ $t("orderType") }}:</span>
            <span class="bill-info-value">{{ getOrderTypeText(orderForSend.orderType) }}</span>
          </div>
          <div class="bill-info-row" v-if="orderForSend.paymentMethod">
            <span class="bill-info-label">{{ $t("paymentMethod") }}:</span>
            <span class="bill-info-value">{{ getPaymentMethodText(orderForSend.paymentMethod) }}</span>
          </div>
          <div class="bill-info-row">
            <span class="bill-info-label">{{ $t("priceModeLabel") || "نوع السعر" }}:</span>
            <span class="bill-info-value">{{ isWholesale ? ($t("wholesalePriceMode") || "جملة") : ($t("retailPriceMode") || "مفرد") }}</span>
          </div>
          <div class="bill-info-row">
            <span class="bill-info-label">{{ $t("from_date") }}:</span>
            <span class="bill-info-value">{{ getCurrentDateTime() }}</span>
          </div>
        </div>

        <div class="bill-divider"></div>

        <!-- Items Table -->
        <div class="bill-items-section">
          <table class="bill-items-table">
            <thead>
              <tr>
                <th class="bill-item-name-col">{{ $t("item_name_label") }}</th>
                <th class="bill-item-qty-col">{{ $t("quantity_label") }}</th>
                <th class="bill-item-price-col">{{ $t("selling_price_label") }}</th>
                <th class="bill-item-total-col">{{ $t("total_label") }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(item, index) in carditems" :key="index">
                <td class="bill-item-name">
                  {{ item.name }}
                  <span
                    v-if="cartLineHasDiscount(item)"
                    class="bill-discount-badge"
                  >{{ $t("discountLabel") || "خصم" }}</span>
                </td>
                <td class="bill-item-qty">{{ item.quantity }}</td>
                <td class="bill-item-price">
                  <span
                    v-if="cartLineHasDiscount(item)"
                    class="bill-price-discounted"
                  >
                    <span class="bill-original-price">{{ formatPrice(item.price || 0) }}</span>
                    <span class="bill-discount-price">{{ formatPrice(cartLineUnitPrice(item)) }}</span>
                  </span>
                  <span v-else>{{ formatPrice(cartLineUnitPrice(item)) }}</span>
                </td>
                <td class="bill-item-total">
                  {{ formatPrice(cartLineUnitPrice(item) * (item.quantity || 1)) }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="bill-divider"></div>

        <!-- Summary Section -->
        <div class="bill-summary-section">
          <div class="bill-summary-row">
            <span class="bill-summary-label">{{ $t("countLabel") }}:</span>
            <span class="bill-summary-value">{{ totalCardItems }} {{ $t("itemLabel") }}</span>
          </div>
          <div class="bill-summary-row" v-if="orderDiscountAmount > 0">
            <span class="bill-summary-label">{{ $t("discountLabel") }}:</span>
            <span class="bill-summary-value">− {{ formatPrice(orderDiscountAmount) }} {{ $t("currency") }}</span>
          </div>
          <div class="bill-summary-row bill-summary-total">
            <span class="bill-summary-label">{{ $t("totalLabel") }}:</span>
            <span class="bill-summary-value">{{ formattedNumber }} {{ $t("currency") }}</span>
          </div>
        </div>

        <div class="bill-notes-section" v-if="orderForSend.notes">
          <div class="bill-divider"></div>
          <div class="bill-notes-content">
            <div class="bill-notes-label">{{ $t("notesLabel") || "ملاحظات" }}:</div>
            <div class="bill-notes-text">{{ orderForSend.notes }}</div>
          </div>
        </div>

        <!-- Footer Section -->
        <div class="bill-footer">
          <p class="bill-footer-text">{{ $t("thankYouMessage") || "شكراً لزيارتك" }}</p>
          <p class="bill-footer-date">{{ getCurrentDate() }}</p>
          <p v-if="commercialUserInfo.footerCreditText" class="bill-footer-credit">{{ commercialUserInfo.footerCreditText }}</p>
          <p v-if="commercialUserInfo.footerCreditPhone" class="bill-footer-credit-phone">{{ commercialUserInfo.footerCreditPhone }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import CalculatorComp from "@/components/CalculatorComp.vue";
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";
import { HTTP } from "../http/api.js";
import { resolveAbsoluteAssetUrl } from "@/utils/apiBase.js";
import posOrderPersistMixin from "@/mixins/posOrderPersistMixin.js";
import posCardPaymentMixin from "@/mixins/posCardPaymentMixin.js";
import posPrintMixin from "@/mixins/posPrintMixin.js";
import posBarcodeScanMixin from "@/mixins/posBarcodeScanMixin.js";
import CardPaymentWaitModal from "@/components/CardPaymentWaitModal.vue";
import {
  findCartLineIndex,
  getCartLineUnitPrice,
  getCartLineTotal,
  hasCartLineDiscount,
  promoteCartLineToFront,
} from "@/utils/mergeCartLines.js";
import { applyPosPageSize, POS_ITEMS_PER_PAGE } from "@/utils/posPageSize.js";
import {
  productImageSrc,
  isProductImageFallback,
  onProductImageError,
} from "@/utils/productImage.js";
import {
  POS_INVOICE_TABS_MAX,
  createEmptyInvoiceTab,
  snapshotFromPos,
  applySnapshotToPos,
  loadPosInvoiceTabs,
  savePosInvoiceTabs,
  nextInvoiceTabIndex,
  tabItemCount,
  tabHasItems,
} from "@/utils/posInvoiceTabs.js";

export default {
  name: "PosView",
  mixins: [
    posOrderPersistMixin,
    posCardPaymentMixin,
    posPrintMixin,
    posBarcodeScanMixin,
  ],
  components: {
    AppHeader,
    ClockVue,
    "vue-barcode": VueBarcode,
    CalculatorComp,
    CardPaymentWaitModal,
  },
  data() {
    return {
      showbarCode: false,
      showShortcutsModal: false,
      show: false,
      totaPrice: 0,
      carditems: [],
      isWholesale: false,
      doneTypingInterval: 300,
      silentCartToasts: true,
      isSearching: false,
      searchAbortController: null,
      lastAddedItem: null,
      itemsAddedCount: 0,
      addItemTimer: null,
      Items: [],
      tags: [],
      pageNumber: 1,
      totalItems: 0,
      pageSize: POS_ITEMS_PER_PAGE,
      search: {
        info: "",
      },
      searchCode: "",
      SearchItems: [],

      totalCardItems: 0,
      userInfo: {},
      commercialUserInfo: {
        storeName: 'LiteCashier',
        logo: null,
        printInvoiceFormat: 'Pos',
        footerCreditText: null,
        footerCreditPhone: null,
      },
      orderForSend: {
        orderCode: "",
        paymentMethod: "Cash",
        customerOrderItem: [],
        orderType: "Takeaway",
        notes: "",
        creditCustomerId: null,
        warehouseId: null,
      },
      warehouses: [],
      selectedWarehouseId: null,
      posMobileCartOpen: false,
      quickSearch: "",
      quickSearchTimer: null,
      posSuppressQuickSearchSync: false,
      activeCategory: "",
      orderDiscountType: "amount",
      orderDiscountValue: null,
      changeCalcOpen: false,
      customerPaidAmount: null,
      changeCalcQuickAmounts: [25000, 50000, 100000],
      creditCustomers: [],
      loadingCreditCustomers: false,
      showAddCreditCustomerModal: false,
      savingCreditCustomer: false,
      newCreditCustomerForm: {
        name: "",
        phoneNumber: "",
        address: "",
        notes: "",
      },
      orderDiscountPresets: [
        { id: "p5", type: "percentage", value: 5, label: "5%" },
        { id: "p10", type: "percentage", value: 10, label: "10%" },
        { id: "p15", type: "percentage", value: 15, label: "15%" },
        { id: "a5000", type: "amount", value: 5000, label: "5,000" },
        { id: "a10000", type: "amount", value: 10000, label: "10,000" },
      ],
      invoiceTabs: [createEmptyInvoiceTab(1)],
      activeInvoiceTabId: null,
      invoiceTabPendingCloseId: null,
      invoiceTabRenamingId: null,
      invoiceTabRenameDraft: "",
      _invoiceTabsHydrating: false,
      _invoiceTabsSaveTimer: null,
    };
  },

  created() {
    if (!this.activeInvoiceTabId && this.invoiceTabs[0]) {
      this.activeInvoiceTabId = this.invoiceTabs[0].id;
    }
  },

  computed: {
    activeInvoiceTab() {
      return (this.invoiceTabs || []).find((t) => t.id === this.activeInvoiceTabId) || null;
    },
    activeInvoiceTabIndex() {
      return this.activeInvoiceTab?.index || 1;
    },
    canAddInvoiceTab() {
      return (this.invoiceTabs || []).length < POS_INVOICE_TABS_MAX;
    },
    orderDiscountAmount() {
      const rawValue = Number(this.orderDiscountValue) || 0;
      if (rawValue <= 0) return 0;
      if (this.orderDiscountType === "percentage") {
        return Math.min(this.totaPrice, (this.totaPrice * Math.min(rawValue, 100)) / 100);
      }
      return Math.min(this.totaPrice, rawValue);
    },
    finalOrderTotal() {
      return Math.max(this.totaPrice - this.orderDiscountAmount, 0);
    },
    formattedNumber() {
      return this.finalOrderTotal.toLocaleString();
    },
    changeDueAmount() {
      const paid = Number(this.customerPaidAmount) || 0;
      if (paid <= 0) return 0;
      return Math.max(0, paid - this.finalOrderTotal);
    },
    isInsufficientPayment() {
      const paid = Number(this.customerPaidAmount) || 0;
      return paid > 0 && paid < this.finalOrderTotal;
    },
    paymentShortfall() {
      const paid = Number(this.customerPaidAmount) || 0;
      if (!this.isInsufficientPayment) return 0;
      return this.finalOrderTotal - paid;
    },
    orderDiscountPreviewLabel() {
      if (!this.orderDiscountAmount) {
        return this.$t("noDiscount") || "بدون خصم";
      }
      if (this.orderDiscountType === "percentage") {
        return `${Number(this.orderDiscountValue) || 0}%`;
      }
      return `${this.formatPrice(this.orderDiscountValue)} ${this.$t("currency")}`;
    },
    showPosCheckoutBar() {
      if (Array.isArray(this.carditems) && this.carditems.length > 0) return true;
      return this.activeCheckoutPrinters.length > 0;
    },
    isCreditPayment() {
      return this.orderForSend?.paymentMethod === "Credit";
    },
    hasCreditAccountSelected() {
      const c = this.orderForSend?.creditCustomerId;
      return c != null && c !== "";
    },
    cardfields() {
      const lang = this.$i18n.locale;
      if (!lang) {
        return [];
      }
      return [
        {
          key: "name",
          label: this.$i18n.t("itemLabel"),
        },
        {
          key: "quantity",
          label: this.$i18n.t("countLabel"),
        },
        {
          key: "price",
          label: this.$i18n.t("price"),
        },
        {
          key: "total",
          label: this.$i18n.t("total"),
        },
        {
          key: "actions",
          label: this.$i18n.t("actions"),
        },
      ];
    },
    posCardFields() {
      const lang = this.$i18n.locale;
      if (!lang) {
        return [];
      }
      return [
        {
          key: "name",
          label: this.$i18n.t("itemLabel"),
        },
        {
          key: "quantity",
          label: this.$i18n.t("countLabel"),
        },
        {
          key: "price",
          label: this.$i18n.t("price"),
        },
        {
          key: "total",
          label: this.$i18n.t("total"),
        },
      ];
    },
  },
  watch: {
    carditems: {
      handler() {
        this.totaPrice = 0;
        this.carditems.forEach((item) => {
          item.isWholesale = this.isWholesale;
          item.total = getCartLineTotal(item, this.isWholesale);
          this.totaPrice += item.total || 0;
        });
        this.totalCardItems = this.carditems.reduce(
          (sum, item) => sum + (Number(item.quantity) || 0),
          0
        );
        this.scheduleActiveInvoiceTabSync();
      },
      deep: true,
    },
    isWholesale() {
      this.scheduleActiveInvoiceTabSync();
    },
    orderDiscountType() {
      this.scheduleActiveInvoiceTabSync();
    },
    orderDiscountValue() {
      this.scheduleActiveInvoiceTabSync();
    },
    "orderForSend.notes"() {
      this.scheduleActiveInvoiceTabSync();
    },
    "orderForSend.paymentMethod"() {
      this.scheduleActiveInvoiceTabSync();
    },
    "orderForSend.creditCustomerId"() {
      this.scheduleActiveInvoiceTabSync();
    },
    "orderForSend.orderCode"() {
      this.scheduleActiveInvoiceTabSync();
    },
    "orderForSend.orderType"() {
      this.scheduleActiveInvoiceTabSync();
    },
    search: {
      handler() {
        this.GetAllItems();
      },
      deep: true,
    },
    pageNumber() {
      this.GetAllItems();
    },
    quickSearch(newVal) {
      if (this.posSuppressQuickSearchSync) {
        return;
      }
      clearTimeout(this.quickSearchTimer);
      this.quickSearchTimer = setTimeout(() => {
        this.activeCategory = "";
        this.search.info = newVal;
        this.GetAllItems();
      }, this.doneTypingInterval);
    },
    posMobileCartOpen(val) {
      if (typeof document === "undefined") return;
      const isNarrowViewport =
        typeof window !== "undefined" &&
        window.matchMedia("(max-width: 1200px)").matches;
      document.body.style.overflow = val && isNarrowViewport ? "hidden" : "";
    },
  },

  mounted() {
    try {
      this.getTags();
      this.loadWarehouses().finally(() => {
        this.$nextTick(() => {
          if (this.$refs.codeNumber) {
            this.$refs.codeNumber.focus();
          }
          applyPosPageSize(this, false);
          this.GetAllItems();
        });
      });
      this._posResizeHandler = () => {
        if (this._isDestroyed) return;
        clearTimeout(this._posResizeTimer);
        this._posResizeTimer = setTimeout(() => {
          if (!this._isDestroyed) applyPosPageSize(this);
        }, 150);
      };
      window.addEventListener("resize", this._posResizeHandler);
      
      const userInfoStr = localStorage.getItem("info");
      if (userInfoStr) {
        this.userInfo = JSON.parse(userInfoStr);
      }

      // Load commercial user info for printing
      this.loadCommercialUserInfo();

      this.loadManagedPrinters();

      const savedPayment = localStorage.getItem("posPaymentMethod");
      if (savedPayment && ["Cash", "Card", "Credit"].includes(savedPayment)) {
        this.orderForSend.paymentMethod = savedPayment;
      }

      this.initInvoiceTabs(savedPayment);

      this.handleKeyup = (e) => {
        if (e.ctrlKey && e.keyCode === 38) {
          this.$root.$emit("bv::toggle::collapse", "sidebar-right");
        }
      };
      window.addEventListener("keyup", this.handleKeyup);

      this.posKeyboardHandler = (e) => this.handlePosKeyboard(e);
      window.addEventListener("keydown", this.posKeyboardHandler);

      this.posMobileCartEscape = (e) => {
        if (e.key === "Escape" && this.posMobileCartOpen) {
          this.closePosMobileCart();
        }
      };
      window.addEventListener("keydown", this.posMobileCartEscape);
    } catch (error) {
      this.$notify.error(this.$i18n.t("error") || "An error occurred", {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    }
  },
  
  beforeDestroy() {
    clearTimeout(this.quickSearchTimer);
    clearTimeout(this._posResizeTimer);
    clearTimeout(this._invoiceTabsSaveTimer);
    this.syncActiveInvoiceTabSnapshot(true);
    if (this._posResizeHandler) {
      window.removeEventListener("resize", this._posResizeHandler);
    }
    if (typeof document !== "undefined") {
      document.body.style.overflow = "";
    }
    if (this.handleKeyup) {
      window.removeEventListener("keyup", this.handleKeyup);
    }
    if (this.posMobileCartEscape) {
      window.removeEventListener("keydown", this.posMobileCartEscape);
    }
    if (this.posKeyboardHandler) {
      window.removeEventListener("keydown", this.posKeyboardHandler);
    }
  },

  methods: {
    productImageSrc,
    isProductImageFallback,
    onProductImageError,
    loadCommercialUserInfo() {
      HTTP.get("Admin/CommercialUserInfo")
        .then((response) => {
          if (response.data && response.data.data) {
            const d = response.data.data;
            const format =
              String(d.printInvoiceFormat || d.PrintInvoiceFormat || "Pos").toUpperCase() ===
              "A4"
                ? "A4"
                : "Pos";
            this.commercialUserInfo = {
              storeName: d.storeName || d.StoreName || "LiteCashier",
              logo: resolveAbsoluteAssetUrl(d.logo || d.Logo) || null,
              printInvoiceFormat: format,
              footerCreditText: d.footerCreditText || d.FooterCreditText || null,
              footerCreditPhone: d.footerCreditPhone || d.FooterCreditPhone || null,
            };
            localStorage.setItem("printInvoiceFormat", format);
          }
        })
        .catch((error) => {
          console.error("Error loading commercial user info:", error);
          this.commercialUserInfo = {
            storeName: "LiteCashier",
            logo: null,
            printInvoiceFormat:
              localStorage.getItem("printInvoiceFormat") === "A4" ? "A4" : "Pos",
            footerCreditText: null,
            footerCreditPhone: null,
          };
        });
    },
    applyOrderDiscountPreset(preset) {
      if (!preset) return;
      this.orderDiscountType = preset.type;
      this.orderDiscountValue = preset.value;
    },
    selectCategory(name) {
      this.posSuppressQuickSearchSync = true;
      this.quickSearch = "";
      this.activeCategory = name;
      this.search.info = name;
      this.pageNumber = 1;
      this.$nextTick(() => {
        this.posSuppressQuickSearchSync = false;
      });
    },
    updatePosPageSize(reload = true) {
      applyPosPageSize(this, reload);
    },
    setPosPaymentMethod(method) {
      if (method === "Credit") {
        this.openCreditPaymentModal();
        return;
      }
      this.orderForSend.paymentMethod = method;
      this.orderForSend.creditCustomerId = null;
      localStorage.setItem("posPaymentMethod", method);
      if (method !== "Cash") {
        this.resetChangeCalculator(false);
      }
    },
    async loadCreditCustomers() {
      try {
        this.loadingCreditCustomers = true;
        const response = await HTTP.get("Customers");
        if (response.data && !response.data.errorStatus) {
          this.creditCustomers = response.data.data || [];
        } else {
          this.creditCustomers = [];
        }
      } catch (error) {
        console.error("Error loading customers:", error);
        this.creditCustomers = [];
      } finally {
        this.loadingCreditCustomers = false;
      }
    },
    async openCreditPaymentModal() {
      await this.loadCreditCustomers();
      this.$bvModal.show("modal-credit-payment");
    },
    openQuickAddCustomerForCredit() {
      this.resetNewCreditCustomerForm();
      this.showAddCreditCustomerModal = true;
    },
    resetNewCreditCustomerForm() {
      this.newCreditCustomerForm = {
        name: "",
        phoneNumber: "",
        address: "",
        notes: "",
      };
    },
    async saveNewCreditCustomer() {
      const textDirection = document.documentElement.dir;
      const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";
      if (!this.newCreditCustomerForm.name || !this.newCreditCustomerForm.name.trim()) {
        this.$notify.error(this.$i18n.t("pleaseEnterCustomerName") || "يرجى إدخال اسم العميل", {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      if (!this.newCreditCustomerForm.phoneNumber || !this.newCreditCustomerForm.phoneNumber.trim()) {
        this.$notify.error(this.$i18n.t("pleaseEnterPhoneNumber") || "يرجى إدخال رقم الهاتف", {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      try {
        this.savingCreditCustomer = true;
        const response = await HTTP.post("Customers", {
          name: this.newCreditCustomerForm.name.trim(),
          phoneNumber: this.newCreditCustomerForm.phoneNumber.trim(),
          address: this.newCreditCustomerForm.address
            ? this.newCreditCustomerForm.address.trim()
            : null,
          notes: this.newCreditCustomerForm.notes
            ? this.newCreditCustomerForm.notes.trim()
            : null,
          isActive: true,
        });
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(this.$i18n.t("customerAddedSuccess") || "تم إضافة العميل بنجاح", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          await this.loadCreditCustomers();
          const newId = response.data.data && response.data.data.id;
          if (newId) {
            this.orderForSend.creditCustomerId = newId;
          }
          this.showAddCreditCustomerModal = false;
          this.resetNewCreditCustomerForm();
        } else {
          this.$notify.error(
            response.data?.message || this.$i18n.t("customerSaveFailed") || "فشل حفظ العميل",
            {
              position: toastPosition,
              timeout: 2500,
              maxToasts: 1,
            }
          );
        }
      } catch (error) {
        console.error("Error saving credit customer from POS:", error);
        this.$notify.error(
          error.response?.data?.message || this.$i18n.t("customerSaveFailed") || "حدث خطأ",
          {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          }
        );
      } finally {
        this.savingCreditCustomer = false;
      }
    },
    confirmCreditPaymentSelection() {
      const textDirection = document.documentElement.dir;
      const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";
      if (
        this.orderForSend.creditCustomerId == null ||
        this.orderForSend.creditCustomerId === ""
      ) {
        this.$notify.error(this.$i18n.t("selectCreditCustomer") || "اختر العميل", {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      this.orderForSend.paymentMethod = "Credit";
      localStorage.setItem("posPaymentMethod", "Credit");
      this.resetChangeCalculator(false);
      this.$bvModal.hide("modal-credit-payment");
    },
    cancelCreditPaymentModal() {
      this.$bvModal.hide("modal-credit-payment");
    },
    validateCreditForOrder(toastPosition) {
      if (this.orderForSend.paymentMethod !== "Credit") return true;
      const c = this.orderForSend.creditCustomerId;
      const hasC = c != null && c !== "";
      if (hasC) return true;
      this.$notify.error(
        this.$i18n.t("pleaseSelectCreditAccount") || "اختر حساباً للدفع الآجل",
        {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        }
      );
      return false;
    },
    toggleChangeCalculator() {
      this.changeCalcOpen = !this.changeCalcOpen;
      if (this.changeCalcOpen) {
        this.$nextTick(() => {
          this.$refs.customerPaidInput?.focus?.();
          this.$refs.customerPaidInput?.select?.();
        });
      }
    },
    setCustomerPaidAmount(amount) {
      this.customerPaidAmount = Math.max(0, Number(amount) || 0);
      if (!this.changeCalcOpen) {
        this.changeCalcOpen = true;
      }
    },
    resetChangeCalculator(keepOpen = true) {
      this.customerPaidAmount = null;
      this.changeCalcOpen = keepOpen;
    },
    focusPosBarcode() {
      if (this.$refs.codeNumber) {
        this.$refs.codeNumber.focus();
        this.$refs.codeNumber.select?.();
      }
    },
    focusPosQuickSearch() {
      const input = this.$refs.posQuickSearchInput;
      if (!input) return;
      input.focus();
      input.select?.();
    },
    flashCart() {
      const el = this.$refs.posCartHeader;
      if (!el) return;
      el.classList.add("pos-cart-flash");
      setTimeout(() => el.classList.remove("pos-cart-flash"), 350);
    },
    isPosShortcutBlocked() {
      const el = document.activeElement;
      if (!el) return false;
      if (el.classList?.contains("pos-quantity-input")) return true;
      if (el.tagName === "TEXTAREA") return true;
      if (el.tagName === "SELECT") return true;
      if (el === this.$refs.posQuickSearchInput) return true;
      return false;
    },
    handlePosKeyboard(e) {
      if (e.defaultPrevented || e.ctrlKey || e.altKey || e.metaKey) return;

      const key = e.key;
      const modalOpen = !!document.querySelector(".modal.show");

      if (key === "F2") {
        e.preventDefault();
        this.focusPosBarcode();
        return;
      }
      if (key === "F3") {
        e.preventDefault();
        this.focusPosQuickSearch();
        return;
      }
      if (key === "F9") {
        e.preventDefault();
        this.addInvoiceTab();
        return;
      }

      if (modalOpen && key !== "Escape") return;

      if (this.isPosShortcutBlocked()) {
        if (key === "Escape") {
          if (document.activeElement === this.$refs.posQuickSearchInput) {
            this.focusPosBarcode();
            return;
          }
          this.closeModel("modal-order-notes");
          this.$bvModal.hide("modal-empty");
          this.$bvModal.hide("modal-print-only-confirm");
        }
        return;
      }

      if (key === "F4") {
        e.preventDefault();
        this.quickPay(false);
        return;
      }
      if (key === "F5") {
        e.preventDefault();
        this.quickPay(true);
        return;
      }
      if (key === "F6") {
        e.preventDefault();
        this.openPrintOnlyConfirm();
        return;
      }
      if (key === "F7") {
        e.preventDefault();
        if (this.totalCardItems > 0) {
          this.toggleChangeCalculator();
        }
        return;
      }
      if (key === "F8") {
        e.preventDefault();
        this.openOrderExtrasModal();
        return;
      }
      if (key === "Escape") {
        this.closeModel("modal-order-notes");
        this.$bvModal.hide("modal-empty");
        this.$bvModal.hide("modal-print-only-confirm");
        if (this.posMobileCartOpen) this.closePosMobileCart();
        return;
      }
      if ((key === "+" || key === "=") && this.carditems.length > 0) {
        e.preventDefault();
        this.increaseQuantity(0);
        return;
      }
      if (key === "-" && this.carditems.length > 0) {
        e.preventDefault();
        this.decreaseQuantity(0);
        return;
      }
      if (key === "Delete" && this.carditems.length > 0) {
        e.preventDefault();
        this.deleteItem(0, { silent: true });
      }
    },
    async quickPay(withPrint = false) {
      if (this.orderForSend.paymentMethod === "Credit" && !this.hasCreditAccountSelected) {
        await this.openCreditPaymentModal();
        return;
      }
      const toastPosition = this.getOrderPersistToastPosition
        ? this.getOrderPersistToastPosition()
        : "top-right";
      if (!this.validateCreditForOrder(toastPosition)) {
        await this.openCreditPaymentModal();
        return;
      }
      await this.checkoutWithPayment(withPrint);
      this.$nextTick(() => this.focusPosBarcode());
    },
    openOrderExtrasModal() {
      if (this.carditems.length <= 0) {
        this.$notify.error(this.$i18n.t("emptyCartMessage") || this.$i18n.t("emptyCart"), {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      this.$bvModal.show("modal-order-notes");
    },
    applyOrderExtras() {
      this.$bvModal.hide("modal-order-notes");
      this.focusPosBarcode();
    },
    openPosMobileCart() {
      this.posMobileCartOpen = true;
      this.$nextTick(() => {
        requestAnimationFrame(() => {
          const sc = this.$refs.posCartScrollArea;
          if (sc) {
            sc.scrollTop = 0;
          }
        });
      });
    },
    closePosMobileCart() {
      this.posMobileCartOpen = false;
    },
    openPrintOnlyConfirm() {
      if (this.carditems.length <= 0) {
        this.$notify.error(this.$i18n.t("emptyCartMessage") || this.$i18n.t("emptyCart"), {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      this.$bvModal.show("modal-print-only-confirm");
    },
    async confirmPrintCartOnly() {
      this.$bvModal.hide("modal-print-only-confirm");
      await this.printCartOnly();
    },
    ensureOrderCodeForPrint() {
      const existing = String(this.orderForSend?.orderCode || "").trim();
      if (existing && existing !== "---") {
        return existing;
      }
      this.orderForSend.orderCode = Math.floor(Math.random() * 1000000000)
        .toString()
        .padStart(9, "0");
      return this.orderForSend.orderCode;
    },
    async printCartOnly() {
      if (this.carditems.length <= 0) {
        this.$notify.error(this.$i18n.t("emptyCartMessage") || this.$i18n.t("emptyCart"), {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      const result = await this.printCard(null, { raiseOnError: false });
      if (!result?.ok) {
        this.$notify.warning(
          this.$t("printError") || "تعذرت الطباعة — تحقق من خادم الطباعة أو استخدم نافذة المتصفح",
          { position: "top-right", timeout: 3500, maxToasts: 1 }
        );
      }
    },
    getTags() {
      HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`)
        .then((response) => {
          this.tags = response.data.data.items;
        })
        .catch((error) => {
          this.$notify.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
        });
    },
    formatPrice(price) {
      const n = Number(price);
      if (!Number.isFinite(n)) return "0";
      return n.toLocaleString("en-EG");
    },
    displayCatalogUnitPrice(item) {
      if (this.isWholesale) {
        const wholesale = Number(item?.wholesalePrice) || 0;
        return wholesale > 0 ? wholesale : Number(item?.sellingPrice) || 0;
      }
      return Number(item?.sellingPrice) || 0;
    },
    setPriceMode(wholesale) {
      const next = !!wholesale;
      if (this.isWholesale === next) return;
      this.isWholesale = next;
      this.carditems.forEach((line) => {
        line.isWholesale = next;
        line.total = getCartLineTotal(line, next);
      });
    },
    cartLineUnitPrice(item) {
      return getCartLineUnitPrice(item, this.isWholesale);
    },
    cartLineHasDiscount(item) {
      return hasCartLineDiscount(item, this.isWholesale);
    },
    clearOrderDiscount() {
      this.orderDiscountType = "amount";
      this.orderDiscountValue = null;
    },
    buildOrderDiscountPayload() {
      const discountAmount = Number(this.orderDiscountAmount) || 0;
      const discountPercent =
        this.orderDiscountType === "percentage"
          ? Math.min(Math.max(Number(this.orderDiscountValue) || 0, 0), 100)
          : 0;
      return {
        discountType: discountAmount > 0 ? this.orderDiscountType : null,
        discountValue: discountAmount > 0 ? Number(this.orderDiscountValue) || 0 : null,
        discountAmount: discountAmount > 0 ? discountAmount : 0,
        discountPercent: discountAmount > 0 ? discountPercent : 0,
        orderSubTotal: Number(this.totaPrice) || 0,
        orderTotalAfterDiscount: Number(this.finalOrderTotal) || 0,
      };
    },
    getCurrentDateTime() {
      const now = new Date();
      const date = now.toLocaleDateString('ar-IQ', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit' 
      });
      const time = now.toLocaleTimeString('ar-IQ', { 
        hour: '2-digit', 
        minute: '2-digit' 
      });
      return `${date} ${time}`;
    },
    getCurrentDate() {
      const now = new Date();
      return now.toLocaleDateString('ar-IQ', { 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric',
        weekday: 'long'
      });
    },
    getOrderTypeText(type) {
      if (!type) return '-';
      const types = {
        'DineIn': this.$t('dineIn') || 'داخلي',
        'Takeaway': this.$t('takeaway') || 'طلب خارجي',
        'Delivery': this.$t('delivery') || 'توصيل'
      };
      return types[type] || type;
    },
    getPaymentMethodText(method) {
      if (!method) return '-';
      const methods = {
        'Cash': this.$t('cash') || 'نقدي',
        'Card': this.$t('card') || 'بطاقة',
        'Credit': this.$t('credit') || 'آجل',
        'BankTransfer': this.$t('bankTransfer') || 'تحويل بنكي'
      };
      return methods[method] || method;
    },

    EmptycardList(id) {
      this.carditems = [];
      this.isWholesale = false;
      this.$bvModal.hide(id);
      this.orderForSend.orderType = "Takeaway";
      this.clearOrderDiscount();
      this.resetChangeCalculator(false);
      this.orderForSend.notes = "";
      this.orderForSend.creditCustomerId = null;
      this.orderForSend.orderCode = "";
      this.syncActiveInvoiceTabSnapshot(true);
      this.focusPosBarcode();
    },
    invoiceTabLabel(tab) {
      if (!tab) return "";
      const custom = String(tab.title || "").trim();
      if (custom) return custom;
      const code = String(tab.orderForSend?.orderCode || "").trim();
      if (code && code !== "---") {
        return code;
      }
      return `${this.$t("posInvoiceTabLabel") || "فاتورة"} ${tab.index || ""}`.trim();
    },
    startRenameInvoiceTab(tab, event) {
      if (!tab?.id) return;
      if (event) {
        event.preventDefault();
        event.stopPropagation();
      }
      if (this.orderPersisting || this.cardPaymentWait?.show) {
        this.$notify.warning(
          this.$t("posInvoiceTabBusy") || "أكمل العملية الحالية قبل تبديل الفاتورة",
          { position: "top-right", timeout: 2500, maxToasts: 1 }
        );
        return;
      }
      this.invoiceTabRenamingId = tab.id;
      this.invoiceTabRenameDraft = String(tab.title || this.invoiceTabLabel(tab) || "").trim();
      this.$nextTick(() => {
        const ref = this.$refs[`invoiceTabRename_${tab.id}`];
        const input = Array.isArray(ref) ? ref[0] : ref;
        if (input && typeof input.focus === "function") {
          input.focus();
          input.select();
        }
      });
    },
    commitRenameInvoiceTab() {
      const tabId = this.invoiceTabRenamingId;
      if (!tabId) return;
      const idx = this.invoiceTabs.findIndex((t) => t.id === tabId);
      const draft = String(this.invoiceTabRenameDraft || "").trim();
      this.invoiceTabRenamingId = null;
      this.invoiceTabRenameDraft = "";
      if (idx < 0) return;

      const defaultLabel = `${this.$t("posInvoiceTabLabel") || "فاتورة"} ${
        this.invoiceTabs[idx].index || ""
      }`.trim();
      const nextTitle =
        !draft || draft === defaultLabel
          ? ""
          : draft.slice(0, 40);

      if (String(this.invoiceTabs[idx].title || "").trim() === nextTitle) return;
      this.$set(this.invoiceTabs[idx], "title", nextTitle);
      this.persistInvoiceTabs();
    },
    cancelRenameInvoiceTab() {
      this.invoiceTabRenamingId = null;
      this.invoiceTabRenameDraft = "";
    },
    invoiceTabCount(tab) {
      return tabItemCount(tab);
    },
    scheduleActiveInvoiceTabSync() {
      if (this._invoiceTabsHydrating || this._isDestroyed) return;
      clearTimeout(this._invoiceTabsSaveTimer);
      this._invoiceTabsSaveTimer = setTimeout(() => {
        this.syncActiveInvoiceTabSnapshot(true);
      }, 180);
    },
    syncActiveInvoiceTabSnapshot(persist = false) {
      if (this._invoiceTabsHydrating || !this.activeInvoiceTabId) return;
      const idx = this.invoiceTabs.findIndex((t) => t.id === this.activeInvoiceTabId);
      if (idx < 0) return;
      const snap = snapshotFromPos(this);
      snap.id = this.activeInvoiceTabId;
      snap.index = this.invoiceTabs[idx].index || snap.index;
      snap.title = String(
        snap.title || this.invoiceTabs[idx].title || ""
      ).trim();
      this.$set(this.invoiceTabs, idx, snap);
      if (persist) {
        savePosInvoiceTabs(this.userInfo, this.invoiceTabs, this.activeInvoiceTabId);
      }
    },
    persistInvoiceTabs() {
      savePosInvoiceTabs(this.userInfo, this.invoiceTabs, this.activeInvoiceTabId);
    },
    initInvoiceTabs(savedPayment) {
      const defaults = {
        paymentMethod:
          savedPayment && ["Cash", "Card", "Credit"].includes(savedPayment)
            ? savedPayment
            : this.orderForSend.paymentMethod || "Cash",
      };
      const loaded = loadPosInvoiceTabs(this.userInfo, defaults);
      this._invoiceTabsHydrating = true;
      this.invoiceTabs = loaded.tabs;
      this.activeInvoiceTabId = loaded.activeId;
      const active =
        this.invoiceTabs.find((t) => t.id === this.activeInvoiceTabId) ||
        this.invoiceTabs[0];
      applySnapshotToPos(this, active, { keepPaymentPreference: true });
      this.$nextTick(() => {
        this._invoiceTabsHydrating = false;
        this.persistInvoiceTabs();
      });
    },
    switchInvoiceTab(tabId) {
      if (!tabId || tabId === this.activeInvoiceTabId) return;
      if (this.invoiceTabRenamingId) {
        this.cancelRenameInvoiceTab();
      }
      if (this.orderPersisting || this.cardPaymentWait?.show) {
        this.$notify.warning(
          this.$t("posInvoiceTabBusy") || "أكمل العملية الحالية قبل تبديل الفاتورة",
          { position: "top-right", timeout: 2500, maxToasts: 1 }
        );
        return;
      }
      const target = this.invoiceTabs.find((t) => t.id === tabId);
      if (!target) return;

      this.syncActiveInvoiceTabSnapshot(false);
      this._invoiceTabsHydrating = true;
      this.activeInvoiceTabId = tabId;
      applySnapshotToPos(this, target);
      this.$nextTick(() => {
        this._invoiceTabsHydrating = false;
        this.persistInvoiceTabs();
        this.focusPosBarcode();
      });
    },
    addInvoiceTab() {
      if (!this.canAddInvoiceTab) {
        this.$notify.warning(
          this.$t("posInvoiceTabLimit") || `الحد الأقصى ${POS_INVOICE_TABS_MAX} فواتير`,
          { position: "top-right", timeout: 2500, maxToasts: 1 }
        );
        return;
      }
      if (this.orderPersisting || this.cardPaymentWait?.show) {
        this.$notify.warning(
          this.$t("posInvoiceTabBusy") || "أكمل العملية الحالية قبل فتح فاتورة جديدة",
          { position: "top-right", timeout: 2500, maxToasts: 1 }
        );
        return;
      }

      this.syncActiveInvoiceTabSnapshot(false);
      const tab = createEmptyInvoiceTab(nextInvoiceTabIndex(this.invoiceTabs), {
        paymentMethod: this.orderForSend.paymentMethod || "Cash",
      });
      this.invoiceTabs.push(tab);
      this._invoiceTabsHydrating = true;
      this.activeInvoiceTabId = tab.id;
      applySnapshotToPos(this, tab);
      this.$nextTick(() => {
        this._invoiceTabsHydrating = false;
        this.persistInvoiceTabs();
        this.focusPosBarcode();
      });
    },
    requestCloseInvoiceTab(tabId, event) {
      if (event) {
        event.preventDefault();
        event.stopPropagation();
      }
      const tab = this.invoiceTabs.find((t) => t.id === tabId);
      if (!tab) return;
      if (this.invoiceTabs.length <= 1) {
        this.$notify.warning(
          this.$t("posInvoiceTabKeepOne") || "يجب الإبقاء على فاتورة واحدة على الأقل",
          { position: "top-right", timeout: 2200, maxToasts: 1 }
        );
        return;
      }
      if (tabHasItems(tab) || (tab.id === this.activeInvoiceTabId && this.carditems.length > 0)) {
        this.invoiceTabPendingCloseId = tabId;
        this.$bvModal.show("modal-close-invoice-tab");
        return;
      }
      this.closeInvoiceTab(tabId);
    },
    confirmCloseInvoiceTab() {
      const id = this.invoiceTabPendingCloseId;
      this.invoiceTabPendingCloseId = null;
      this.$bvModal.hide("modal-close-invoice-tab");
      if (id) this.closeInvoiceTab(id);
    },
    closeInvoiceTab(tabId) {
      const idx = this.invoiceTabs.findIndex((t) => t.id === tabId);
      if (idx < 0 || this.invoiceTabs.length <= 1) return;

      if (tabId === this.activeInvoiceTabId) {
        this.syncActiveInvoiceTabSnapshot(false);
      }

      const wasActive = tabId === this.activeInvoiceTabId;
      this.invoiceTabs.splice(idx, 1);

      if (wasActive) {
        const next = this.invoiceTabs[Math.max(0, idx - 1)] || this.invoiceTabs[0];
        this._invoiceTabsHydrating = true;
        this.activeInvoiceTabId = next.id;
        applySnapshotToPos(this, next);
        this.$nextTick(() => {
          this._invoiceTabsHydrating = false;
          this.persistInvoiceTabs();
          this.focusPosBarcode();
        });
      } else {
        this.persistInvoiceTabs();
      }
    },
    onActiveInvoiceTabClearedAfterSale() {
      this.orderForSend.orderCode = "";
      this.resetChangeCalculator(false);
      this.syncActiveInvoiceTabSnapshot(true);
    },
    closeModel(id) {
      this.$bvModal.hide(id);
    },
    addToCartList(item) {
      try {
        const bodyElement = document.querySelector("body");
        const textDirection = bodyElement.getAttribute("dir");
        const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";
        
        // Check if item has available quantity
        if (!item.quantity || item.quantity <= 0) {
          this.$notify.error(
            this.$i18n.t("itemOutOfStock") || "المنتج غير متوفر في المخزون",
            {
              position: toastPosition,
              timeout: 2000,
              maxToasts: 1,
            }
          );
          return;
        }
        
        // Check if item already exists in cart
        const existingItemIndex = findCartLineIndex(this.carditems, item.id);
        
        if (existingItemIndex !== -1) {
          this.carditems[existingItemIndex].quantity += 1;
          this.carditems[existingItemIndex].isWholesale = this.isWholesale;
          this.carditems[existingItemIndex].total = getCartLineTotal(
            this.carditems[existingItemIndex],
            this.isWholesale
          );
          promoteCartLineToFront(this.carditems, existingItemIndex);
        } else {
          const cartItem = {
            name: item.name,
            quantity: 1,
            price: Number(item.sellingPrice) || 0,
            disCountPrice: Number(item.disCountPrice) || 0,
            wholesalePrice: Number(item.wholesalePrice) || 0,
            isWholesale: this.isWholesale,
            id: item.id,
          };
          cartItem.total = getCartLineTotal(cartItem, this.isWholesale);
          this.carditems.unshift(cartItem);
        }

        if (this.$refs.codeNumber) {
          this.$refs.codeNumber.focus();
        }

        this.feedbackItemAdded(item.name);
      } catch (error) {
        console.error("Error adding item to cart:", error);
        this.$notify.error(this.$i18n.t("error"), {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
          newestOnTop: true,
        });
      }
    },

    deleteItem(index, { silent = false } = {}) {
      this.carditems.splice(index, 1);
      if (!silent) {
        this.$notify.error(this.$i18n.t("deleteItemFromOrderSucsses"), {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      }
      this.focusPosBarcode();
    },
    increaseQuantity(index) {
      if (this.carditems[index]) {
        this.carditems[index].quantity += 1;
        this.updateItemTotal(index);
        this.flashCart();
      }
    },
    decreaseQuantity(index) {
      if (this.carditems[index] && this.carditems[index].quantity > 1) {
        this.carditems[index].quantity -= 1;
        this.updateItemTotal(index);
        this.flashCart();
      }
    },
    updateQuantity(index, value) {
      const quantity = parseInt(value) || 1;
      if (quantity > 0 && this.carditems[index]) {
          this.carditems[index].quantity = quantity;
        this.updateItemTotal(index);
      }
    },
    updateItemTotal(index) {
      if (this.carditems[index]) {
        this.carditems[index].isWholesale = this.isWholesale;
        this.carditems[index].total = getCartLineTotal(
          this.carditems[index],
          this.isWholesale
        );
      }
    },
    async loadWarehouses() {
      try {
        const res = await HTTP.get("Warehouses/ForPos");
        const raw = res.data?.data || res.data?.Data || [];
        this.warehouses = (Array.isArray(raw) ? raw : []).map((w) => ({
          id: w.id ?? w.Id,
          name: w.name ?? w.Name ?? "—",
          isDefault: !!(w.isDefault ?? w.IsDefault),
        }));
        const userId = this.userInfo?.id || localStorage.getItem("userId") || "anon";
        const key = `posWarehouseId_${userId}`;
        const saved = Number(localStorage.getItem(key));
        const match = this.warehouses.find((w) => w.id === saved);
        const def = this.warehouses.find((w) => w.isDefault) || this.warehouses[0];
        this.selectedWarehouseId = match?.id || def?.id || null;
        this.orderForSend.warehouseId = this.selectedWarehouseId;
      } catch (error) {
        console.warn("loadWarehouses failed:", error?.response?.status || error?.message);
        this.warehouses = [];
      }
    },
    onWarehouseChanged() {
      const userId = this.userInfo?.id || localStorage.getItem("userId") || "anon";
      localStorage.setItem(`posWarehouseId_${userId}`, String(this.selectedWarehouseId || ""));
      this.orderForSend.warehouseId = this.selectedWarehouseId;
      this.GetAllItems();
    },
    GetAllItems() {
      this.show = true;
      const wh = this.selectedWarehouseId
        ? `&warehouseId=${this.selectedWarehouseId}`
        : "";
      HTTP.get(
        `Admin/GetItems?pageNumber=${this.pageNumber - 1}&pageSize=${
          this.pageSize
        }&info=${this.search.info || ""}${wh}`
      )
        .then((response) => {
          this.Items = response.data.data.items.map(item => ({
            ...item,
            imageError: false
          }));
          this.totalItems = response.data.data.totalItems;
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
        });
    },
    feedbackItemAdded(itemName) {
      this.flashCart();
      if (this.silentCartToasts) return;

      if (this.addItemTimer) clearTimeout(this.addItemTimer);
      this.itemsAddedCount++;
      this.lastAddedItem = itemName;
      this.$notify.clear();

      const message =
        this.itemsAddedCount > 1
          ? `${this.itemsAddedCount} ${this.$i18n.t("itemsAdded") || "مواد مضافة"}`
          : `${itemName} : ${this.$i18n.t("itemToCard")}`;

      this.$notify.success(message, {
        position: "top-right",
        timeout: 1200,
        maxToasts: 1,
        newestOnTop: true,
      });

      this.addItemTimer = setTimeout(() => {
        this.itemsAddedCount = 0;
        this.lastAddedItem = null;
      }, 2000);
    },
  },
};
</script>

<style scoped>
.credit-customer-select-row {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.credit-customer-select-row .users-form-select {
  width: 100%;
}

.credit-quick-add-btn {
  width: 100%;
  padding: 0.875rem 1.5rem;
  border-radius: 0.75rem;
  border: 2px dashed var(--border-color, #ced4da);
  background: var(--bg-tertiary, #f8f9fa);
  color: var(--primary-color);
  font-weight: 600;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.credit-quick-add-btn:hover:not(:disabled) {
  background: var(--primary-color);
  color: #ffffff;
  border-color: var(--primary-color);
  transform: translateY(-2px);
}

.credit-quick-add-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.users-form-hint {
  margin-top: 0.5rem;
  font-size: 0.875rem;
  color: var(--text-secondary, #6c757d);
}

.pos-warehouse-bar {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  margin: 0 0 0.55rem;
  padding: 0.25rem 0.65rem;
  background: transparent;
  border: none;
}

.pos-warehouse-bar__label {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  margin: 0;
  white-space: nowrap;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-primary, #212529);
}

.pos-warehouse-bar__label .b-icon {
  color: var(--primary-color, #0f6e6e);
}

.pos-warehouse-bar__select {
  flex: 1 1 auto;
  min-width: 0;
  border: 1px solid var(--border-color, #ced4da);
  border-radius: 0.5rem;
  padding: 0.38rem 0.55rem;
  font-size: 0.85rem;
  font-weight: 600;
  background: var(--bg-primary, #fff);
  color: var(--text-primary, #212529);
}
</style>
