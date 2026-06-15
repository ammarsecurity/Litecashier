<template>
  <div>
    <b-overlay
      :show="show"
      spinner-variant="danger"
      spinner-type="grow"
      spinner-large
      rounded="sm"
    >
      <AppHeader
        :show-pos-fullscreen-button="true"
        :pos-fullscreen-active="isFullscreen"
        @toggle-pos-fullscreen="toggleFullscreen"
      >
        <template #pos-center>
          <div class="pos-quick-search pos-quick-search--header">
            <b-icon icon="search" class="pos-quick-search-icon"></b-icon>
            <input
              v-model="quickSearch"
              ref="posQuickSearchInput"
              type="search"
              :placeholder="$t('searchPlaceholder')"
              class="pos-quick-search-input"
            />
          </div>
        </template>
      </AppHeader>
      <div
        class="main-content-wrapper pos-route pos-route--v2"
        :class="{
          'pos-fullscreen': isFullscreen,
          'pos-has-checkout-bar': showPosCheckoutBar,
          'pos-has-checkout-bar--with-discounts': carditems.length > 0,
        }"
      >
        <b-container fluid class="pos-container-fluid">
          <div class="pos-page-container pos-page-container--v2">
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
                          @keyup.enter="handleBarcodeSearch"
                          @input="handleBarcodeInput"
                        />
                      </span>
                      <span class="pos-quick-barcode-actions">
                        <kbd class="pos-kbd pos-kbd--barcode">F2</kbd>
                        <span class="pos-quick-barcode-enter-hint" aria-hidden="true">
                          <span class="pos-quick-barcode-enter-text">Enter</span>
                          <b-icon icon="arrow-return-left"></b-icon>
                        </span>
                      </span>
                    </label>
                  </div>

                  <div class="pos-shortcuts-panel" :aria-label="$t('posShortcutsTitle')">
                    <div class="pos-shortcuts-panel-head">
                      <b-icon icon="keyboard-fill" class="pos-shortcuts-panel-icon" aria-hidden="true"></b-icon>
                      <span class="pos-shortcuts-panel-title">{{ $t("posShortcutsTitle") || "اختصارات لوحة المفاتيح" }}</span>
                    </div>
                    <div class="pos-shortcuts-groups">
                      <div class="pos-shortcuts-group">
                        <span class="pos-shortcuts-group-label">{{ $t("posShortcutGroupPayment") || "الدفع" }}</span>
                        <div class="pos-shortcuts-group-chips">
                          <span class="pos-shortcut-chip pos-shortcut-chip--pay">
                            <kbd class="pos-kbd">F4</kbd>
                            <span class="pos-shortcut-chip-label">{{ $t("payNow") || "دفع" }}</span>
                          </span>
                          <span class="pos-shortcut-chip pos-shortcut-chip--pay">
                            <kbd class="pos-kbd">F5</kbd>
                            <span class="pos-shortcut-chip-label">{{ $t("payAndPrint") || "دفع وطباعة" }}</span>
                          </span>
                        </div>
                      </div>
                      <div class="pos-shortcuts-group">
                        <span class="pos-shortcuts-group-label">{{ $t("posShortcutGroupOrder") || "الطلب" }}</span>
                        <div class="pos-shortcuts-group-chips">
                          <span class="pos-shortcut-chip">
                            <kbd class="pos-kbd">F8</kbd>
                            <span class="pos-shortcut-chip-label">{{ $t("discountAndNotes") || "خصم وملاحظات" }}</span>
                          </span>
                        </div>
                      </div>
                      <div class="pos-shortcuts-group">
                        <span class="pos-shortcuts-group-label">{{ $t("posShortcutGroupCart") || "السلة" }}</span>
                        <div class="pos-shortcuts-group-chips">
                          <span class="pos-shortcut-chip">
                            <kbd class="pos-kbd">+</kbd>
                            <kbd class="pos-kbd">−</kbd>
                            <span class="pos-shortcut-chip-label">{{ $t("quantity") || "الكمية" }}</span>
                          </span>
                          <span class="pos-shortcut-chip pos-shortcut-chip--danger">
                            <kbd class="pos-kbd">Del</kbd>
                            <span class="pos-shortcut-chip-label">{{ $t("posShortcutRemoveLast") || "حذف آخر منتج" }}</span>
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="pos-categories-scroll">
                    <div class="pos-categories-list">
                      <button
                        type="button"
                        class="pos-category-btn pos-category-btn-accent"
                        :class="{ 'pos-category-btn-active': activeCategory === '' }"
                        @click="selectCategory('')"
                      >
                        {{ $t("all") }}
                      </button>
                      <button
                        v-for="tag in tags"
                        :key="tag.id"
                        type="button"
                        class="pos-category-btn"
                        :class="{ 'pos-category-btn-active': activeCategory === tag.name }"
                        @click="selectCategory(tag.name)"
                      >
                        {{ tag.name }}
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
                          v-if="item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
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
                              v-if="item.image && !item.imageError"
                              :src="item.image"
                              :alt="item.name"
                              class="pos-product-image"
                              @error="item.imageError = true"
                            />
                            <div v-else class="pos-product-image-placeholder">
                              <b-icon icon="box-fill" class="pos-product-placeholder-icon"></b-icon>
                            </div>
                          </div>
                        </div>

                        <div class="pos-product-info">
                          <h4 class="pos-product-name">{{ item.name }}</h4>
                          <div class="pos-product-meta">
                            <div class="pos-product-price">
                              <div
                                v-if="item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
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
                                {{ formatPrice(item.sellingPrice) }} {{ $t("currency") }}
                              </div>
                            </div>
                          </div>
                          <div class="pos-product-add-badge" v-if="item.quantity && item.quantity > 0">
                            <b-icon icon="plus-circle-fill" class="me-1"></b-icon>
                            {{ $t("addButton") || "أضف" }}
                          </div>
                          <div class="pos-product-out-of-stock-badge" v-if="!item.quantity || item.quantity <= 0">
                            <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                            {{ $t("itemOutOfStock") || "غير متوفر" }}
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
                          {{ $t("cart") || "السلة" }}
                          <span v-if="carditems.length > 0" class="pos-cart-count-badge pos-cart-count-badge--inline">
                            {{ totalCardItems }}
                          </span>
                        </h3>
                        <div class="pos-cart-header-actions" v-if="carditems.length > 0">
                          <button
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
                        class="pos-cart-items-list"
                        v-if="carditems.length > 0"
                        ref="posCartItemsList"
                      >
                        <div
                          class="pos-cart-item pos-cart-item--v2 pos-cart-item--row"
                          v-for="(item, index) in carditems"
                          :key="index"
                          @dblclick="increaseQuantity(index)"
                        >
                          <div class="pos-cart-item-qty-row">
                            <button
                              type="button"
                              class="pos-quantity-btn pos-quantity-decrease"
                              @click.stop="decreaseQuantity(index)"
                              :title="$t('decrease') || 'تقليل'"
                            >
                              <b-icon icon="dash-lg"></b-icon>
                            </button>
                            <span class="pos-cart-item-qty-num">{{ item.quantity }}</span>
                            <button
                              type="button"
                              class="pos-quantity-btn pos-quantity-increase"
                              @click.stop="increaseQuantity(index)"
                              :title="$t('increase') || 'زيادة'"
                            >
                              <b-icon icon="plus-lg"></b-icon>
                            </button>
                          </div>
                          <div class="pos-cart-item-body">
                            <div class="pos-cart-item-name">{{ item.name }}</div>
                            <div class="pos-cart-item-meta">
                              <span class="pos-cart-item-unit-line">
                                {{ formatPrice(cartLineUnitPrice(item)) }} × {{ item.quantity }}
                              </span>
                              <span v-if="cartLineHasDiscount(item)" class="pos-cart-item-discount-tag">
                                {{ $t("discountLabel") }}
                              </span>
                            </div>
                          </div>
                          <div class="pos-cart-item-end">
                            <span class="pos-cart-item-line-total">
                              {{ formatPrice(item.total) }}
                              <span class="pos-cart-currency">{{ $t("currency") }}</span>
                            </span>
                        
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

            <b-modal id="modal-empty" :title="$t('confirmClearCartTitle')" hide-header hide-footer class="users-modal">
              <div class="modal-content-wrapper">
                <div class="delete-confirmation-content">
                  <div class="delete-icon-wrapper">
                    <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
                  </div>
                  <h3 class="delete-confirmation-title">{{ $t("confirmClearCartTitle") }}</h3>
                  <p class="delete-confirmation-text">{{ $t("confirmClearCartMessage") }}</p>
                  <div class="delete-confirmation-actions">
                    <button class="delete-confirm-button" @click="EmptycardList('modal-empty')">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("confirmButton") }}
                    </button>
                    <button class="delete-cancel-button" @click="closeModel('modal-empty')">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

            <b-modal
              id="modal-order-notes"
              :title="$t('orderNotes') || 'ملاحظات الطلب'"
              hide-header
              hide-footer
              centered
              size="lg"
              class="users-modal pos-order-modal"
            >
              <div class="modal-content-wrapper">
                <h2 class="modal-title">
                  <b-icon icon="tag-fill"></b-icon>
                  {{ $t("discountAndNotes") || "خصم وملاحظات" }}
                </h2>
                <form class="order-notes-content" @submit.prevent="applyOrderExtras">
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
                  <div class="order-notes-actions">
                    <button type="submit" class="order-notes-confirm-button">
                      <b-icon icon="check-circle-fill"></b-icon>
                      {{ $t("apply") || "تطبيق" }}
                    </button>
                    <button type="button" class="order-notes-cancel-button" @click="closeModel('modal-order-notes')">
                      <b-icon icon="x-circle-fill"></b-icon>
                      {{ $t("cancelButton") || "تراجع" }}
                    </button>
                  </div>
                </form>
              </div>
            </b-modal>

            <b-modal id="modal-print-only-confirm" :title="$t('printOnly')" hide-header hide-footer class="users-modal">
              <div class="modal-content-wrapper">
                <div class="delete-confirmation-content">
                  <div class="delete-icon-wrapper">
                    <b-icon icon="printer-fill" class="delete-warning-icon"></b-icon>
                  </div>
                  <h3 class="delete-confirmation-title">{{ $t("printOnly") || "طباعة فقط" }}</h3>
                  <p class="delete-confirmation-text">
                    {{ $t("confirmPrintOnlyMessage") || "هل أنت متأكد من تنفيذ الطباعة فقط؟" }}
                  </p>
                  <div class="delete-confirmation-actions">
                    <button class="delete-confirm-button" @click="confirmPrintCartOnly">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("confirm") }}
                    </button>
                    <button class="delete-cancel-button" @click="closeModel('modal-print-only-confirm')">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") }}
                    </button>
                  </div>
                </div>
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
                          :title="$t('printOnly')"
                        >
                          <b-icon icon="printer-fill" class="me-1"></b-icon>
                          {{ $t("printOnly") || "طباعة فقط" }}
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
                          @click="setPosPaymentMethod('Credit')"
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
              src="../assets/logoarabic.png"
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
                    v-if="item.disCountPrice > 0 && item.disCountPrice !== item.price"
                    class="bill-discount-badge"
                  >{{ $t("discountLabel") || "خصم" }}</span>
                </td>
                <td class="bill-item-qty">{{ item.quantity }}</td>
                <td class="bill-item-price">
                  <span
                    v-if="item.disCountPrice > 0 && item.disCountPrice !== item.price"
                    class="bill-price-discounted"
                  >
                    <span class="bill-original-price">{{ formatPrice(item.price || 0) }}</span>
                    <span class="bill-discount-price">{{ formatPrice(item.disCountPrice) }}</span>
                  </span>
                  <span v-else>{{ formatPrice(item.price || 0) }}</span>
                </td>
                <td class="bill-item-total">
                  {{
                    formatPrice(
                      (item.disCountPrice > 0 && item.disCountPrice !== item.price
                        ? item.disCountPrice
                        : (item.price || 0)) * (item.quantity || 1)
                    )
                  }}
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
import posOrderPersistMixin from "@/mixins/posOrderPersistMixin.js";
import posPrintMixin from "@/mixins/posPrintMixin.js";
import {
  findCartLineIndex,
  getCartLineUnitPrice,
  getCartLineTotal,
  hasCartLineDiscount,
} from "@/utils/mergeCartLines.js";
import { applyPosPageSize } from "@/utils/posPageSize.js";

export default {
  name: "PosView",
  mixins: [posOrderPersistMixin, posPrintMixin],
  components: {
    AppHeader,
    ClockVue,
    "vue-barcode": VueBarcode,
    CalculatorComp,
  },
  data() {
    return {
      showbarCode: false,
      show: false,
      totaPrice: 0,
      carditems: [],
      typingTimer: null,
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
      pageSize: 36,
      search: {
        info: "",
      },
      searchCode: "",
      SearchItems: [],

      totalCardItems: 0,
      userInfo: {},
      commercialUserInfo: {
        storeName: 'LiteCashier',
        logo: null
      },
      orderForSend: {
        orderCode: "",
        paymentMethod: "Cash",
        customerOrderItem: [],
        orderType: "Takeaway",
        notes: "",
      },
      isFullscreen: false,
      posMobileCartOpen: false,
      quickSearch: "",
      quickSearchTimer: null,
      posSuppressQuickSearchSync: false,
      activeCategory: "",
      orderDiscountType: "amount",
      orderDiscountValue: null,
      orderDiscountPresets: [
        { id: "p5", type: "percentage", value: 5, label: "5%" },
        { id: "p10", type: "percentage", value: 10, label: "10%" },
        { id: "p15", type: "percentage", value: 15, label: "15%" },
        { id: "a5000", type: "amount", value: 5000, label: "5,000" },
        { id: "a10000", type: "amount", value: 10000, label: "10,000" },
      ],
    };
  },

  computed: {
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
          item.total = getCartLineTotal(item);
          this.totaPrice += item.total || 0;
        });
        this.totalCardItems = this.carditems.reduce(
          (sum, item) => sum + (Number(item.quantity) || 0),
          0
        );
      },
      deep: true,
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
    isFullscreen() {
      this.$nextTick(() => {
        applyPosPageSize(this);
      });
    },
  },

  mounted() {
    try {
      const savedFullscreen = localStorage.getItem("posFullscreen");
      if (savedFullscreen === "true") {
        this.isFullscreen = true;
      }

      this.getTags();
      this.$nextTick(() => {
        if (this.$refs.codeNumber) {
          this.$refs.codeNumber.focus();
        }
        applyPosPageSize(this, false);
        this.GetAllItems();
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
    loadCommercialUserInfo() {
      HTTP.get("Admin/CommercialUserInfo")
        .then((response) => {
          if (response.data && response.data.data) {
            this.commercialUserInfo = {
              storeName: response.data.data.storeName || response.data.data.StoreName || 'LiteCashier',
              logo: response.data.data.logo || response.data.data.Logo || null
            };
          }
        })
        .catch((error) => {
          console.error('Error loading commercial user info:', error);
          this.commercialUserInfo = {
            storeName: 'LiteCashier',
            logo: null
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
      this.orderForSend.paymentMethod = method;
      localStorage.setItem("posPaymentMethod", method);
    },
    focusPosBarcode() {
      if (this.$refs.codeNumber) {
        this.$refs.codeNumber.focus();
        this.$refs.codeNumber.select?.();
      }
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

      if (modalOpen && key !== "Escape") return;

      if (this.isPosShortcutBlocked()) {
        if (key === "Escape") {
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
        this.increaseQuantity(this.carditems.length - 1);
        return;
      }
      if (key === "-" && this.carditems.length > 0) {
        e.preventDefault();
        this.decreaseQuantity(this.carditems.length - 1);
        return;
      }
      if (key === "Delete" && this.carditems.length > 0) {
        e.preventDefault();
        this.deleteItem(this.carditems.length - 1, { silent: true });
      }
    },
    async quickPay(withPrint = false) {
      if (this.carditems.length <= 0) {
        this.$notify.error(this.$i18n.t("emptyCartMessage") || this.$i18n.t("emptyCart"), {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      await this.addOrder(withPrint);
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
    toggleFullscreen() {
      this.isFullscreen = !this.isFullscreen;
      localStorage.setItem("posFullscreen", this.isFullscreen);
      const message = this.isFullscreen
        ? this.$i18n.t("fullscreenEnabled") || "تم تفعيل الوضع الكامل"
        : this.$i18n.t("fullscreenDisabled") || "تم إلغاء الوضع الكامل";
      this.$notify.info(message, {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
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
    cartLineUnitPrice(item) {
      return getCartLineUnitPrice(item);
    },
    cartLineHasDiscount(item) {
      return hasCartLineDiscount(item);
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
      this.$bvModal.hide(id);
      this.orderForSend.orderType = "Takeaway";
      this.clearOrderDiscount();
      this.orderForSend.notes = "";
      this.focusPosBarcode();
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
          this.carditems[existingItemIndex].total = getCartLineTotal(
            this.carditems[existingItemIndex]
          );
        } else {
          const cartItem = {
            name: item.name,
            quantity: 1,
            price: Number(item.sellingPrice) || 0,
            disCountPrice: Number(item.disCountPrice) || 0,
            id: item.id,
          };
          cartItem.total = getCartLineTotal(cartItem);
          this.carditems.push(cartItem);
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
        this.carditems[index].total = getCartLineTotal(this.carditems[index]);
      }
    },
    GetAllItems() {
      this.show = true;
      HTTP.get(
        `Admin/GetItems?pageNumber=${this.pageNumber - 1}&pageSize=${
          this.pageSize
        }&info=${this.search.info}`
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
    handleBarcodeSearch() {
      // Immediate search when Enter is pressed (barcode scanner)
      if (this.searchCode && this.searchCode.trim() !== "") {
        clearTimeout(this.typingTimer);
        // Cancel any pending debounced search
        this.typingTimer = null;
        this.SearchByCode();
      }
    },
    handleBarcodeInput() {
      // Cancel any pending search
      clearTimeout(this.typingTimer);
      
      if (this.searchCode.trim() === "") {
        return;
      }
      
      // Use debounce for all searches to prevent multiple requests
      // Barcode scanners send codes quickly, so we wait a bit to ensure complete code
      this.typingTimer = setTimeout(() => {
        // Only search if code is long enough (likely complete)
        // Minimum 3 chars for manual typing, but prefer longer codes
        if (this.searchCode.length >= 3) {
          this.SearchByCode();
        }
      }, this.doneTypingInterval);
    },
    SearchByCode() {
      // Prevent multiple simultaneous searches
      if (this.isSearching) {
        return;
      }
      
      if (!this.searchCode || this.searchCode.trim() === "") {
        return;
      }
      
      // Cancel any previous request
      if (this.searchAbortController) {
        this.searchAbortController.abort();
      }
      
      // Create new abort controller for this request
      this.searchAbortController = new AbortController();
      this.isSearching = true;
      
      HTTP.get(`Admin/GetItemsByCode?code=${this.searchCode}`, {
        signal: this.searchAbortController.signal
      })
        .then((response) => {
          this.isSearching = false;
          
          if (response.data && response.data.data) {
            this.SearchItems = response.data.data;
            
            // Check if item already exists in cart
            const existingItemIndex = findCartLineIndex(this.carditems, this.SearchItems.id);
            
            if (existingItemIndex !== -1) {
              this.carditems[existingItemIndex].quantity += 1;
              this.carditems[existingItemIndex].total = getCartLineTotal(
                this.carditems[existingItemIndex]
              );
            } else {
              // Check if item has available quantity
              if (!this.SearchItems.quantity || this.SearchItems.quantity <= 0) {
                const toastPosition = document.documentElement.dir === "rtl" ? "top-right" : "top-left";
                this.$notify.error(
                  this.$i18n.t("itemOutOfStock") || "المنتج غير متوفر في المخزون",
                  {
                    position: toastPosition,
                    timeout: 2000,
                    maxToasts: 1,
                    newestOnTop: true,
                  }
                );
                this.searchCode = "";
                if (this.$refs.codeNumber) {
                  this.$refs.codeNumber.focus();
                }
                return;
              }
              
              // New item, add to cart
              const item = {
                name: this.SearchItems.name,
                quantity: 1,
                price: Number(this.SearchItems.sellingPrice) || 0,
                disCountPrice: Number(this.SearchItems.disCountPrice) || 0,
                id: this.SearchItems.id,
              };
              item.total = getCartLineTotal(item);
              this.carditems.push(item);
            }
            
            this.feedbackItemAdded(this.SearchItems.name);
            
            this.searchCode = "";
            if (this.$refs.codeNumber) {
              this.$refs.codeNumber.focus();
            }
          }
        })
        .catch((error) => {
          this.isSearching = false;
          
          // Don't show error if request was aborted
          if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') {
            return;
          }
          
          this.searchCode = "";
          // Show error notification (only one at a time)
          this.$notify.error(this.$i18n.t("itemNotFound") || "Item not found", {
            position: "top-right",
            timeout: 2000,
            closeOnClick: true,
            pauseOnFocusLoss: false,
            pauseOnHover: false,
            draggable: false,
            hideProgressBar: false,
            maxToasts: 1,
            newestOnTop: true,
          });
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
