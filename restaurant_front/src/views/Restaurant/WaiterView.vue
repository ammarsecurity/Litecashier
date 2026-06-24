<template>
  <div>
    <!-- طبقة فوق كل الصفحة: لا تُخفى بـ sessionStorage حتى يظهر التحديث دائماً حتى التخطي -->
    <div
      v-if="posFloorPlanGateVisible"
      class="pos-floor-plan-gate pos-floor-plan-gate--fullscreen pos-floor-plan-gate--page"
      role="dialog"
      aria-modal="true"
    >
        <b-overlay
          :show="posFloorPlanLoading"
          spinner-variant="light"
          spinner-type="grow"
          rounded="sm"
          class="pos-floor-plan-gate-overlay pos-floor-plan-gate-overlay--v2 pos-floor-plan-gate-overlay--fill"
          opacity="0.45"
        >
          <div class="pos-floor-plan-gate-card pos-floor-plan-gate-card--v2 pos-fp-page-root">
            <div class="pos-fp-launch">
              <div class="pos-fp-launch__intro pos-fp-launch__intro--navbar">
                <div class="pos-fp-launch__intro-main">
                  <header class="pos-fp-launch__intro-head">
                    <p class="pos-fp-launch__eyebrow">{{ $t("posFloorPlanEyebrow") }}</p>
                    <h2 class="pos-floor-plan-gate-title">{{ $t("posFloorPlanGateTitle") }}</h2>
                  </header>

                  <div
                    v-if="posFloorPlanKeysForTabs.length"
                    class="pos-fp-gate-tabs-card pos-fp-gate-tabs-card--navbar"
                  >
                    <div class="pos-fp-gate-tabs-card__header">
                      <div class="pos-fp-gate-tabs-card__icon-wrap" aria-hidden="true">
                        <b-icon icon="geo-alt-fill" />
                      </div>
                      <label class="pos-fp-gate-tabs-label pos-fp-gate-plan-select-label" for="pos-fp-gate-plan-select">
                        {{ $t("floorPlanFloorTabs") }}
                      </label>
                    </div>
                    <div class="pos-fp-gate-plan-select-wrap">
                      <select
                        id="pos-fp-gate-plan-select"
                        class="pos-fp-gate-plan-select form-control"
                        :value="posFloorPlanSelectedKey"
                        :aria-label="$t('floorPlanFloorTabs')"
                        @change="selectPosFloorPlanKey($event.target.value)"
                      >
                        <option
                          v-for="k in posFloorPlanKeysForTabsSorted"
                          :key="'fp-opt-' + k"
                          :value="k"
                        >
                          {{ k }}
                        </option>
                      </select>
                    </div>
                  </div>
                </div>
              </div>

              <div class="pos-floor-plan-gate-canvas-outer">
                <div class="pos-floor-plan-gate-canvas-wrap" dir="ltr">
                  <div class="pos-floor-plan-gate-canvas" :style="[posFloorCanvasBgStyle, posFloorTableChipVarsStyle]">
                    <div
                      v-for="(z, zi) in posFloorPlanZoneRects"
                      :key="'pos-fpz-' + zi"
                      class="pos-floor-plan-gate-zone"
                      :style="posFloorZoneRectStyle(z)"
                    >
                      <span class="pos-floor-plan-gate-zone-label">{{ z.name }}</span>
                    </div>
                    <button
                      v-for="t in posFloorPlanPlacedTables"
                      :key="'pos-fpt-' + t.id"
                      type="button"
                      class="pos-floor-plan-gate-table-chip"
                      :class="[
                        posFloorTableStatusClassForTable(t),
                        {},
                      ]"
                      :style="posFloorChipStyle(t.id)"
                      :disabled="t.status === 'OutOfService'"
                      @click="onPosFloorPlanTableClick(t, $event)"
                    >
                      {{ t.tableNumber }}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </b-overlay>
    </div>

    <TableGuestsModal
      :table-number="floorPlanGuestModal.tableNumber"
      :count.sync="floorPlanGuestModal.count"
      @confirm="confirmFloorPlanGuestModal"
      @cancel="cancelFloorPlanGuestModal"
    />

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
      id="modal-cancel-order"
      modal-class="users-modal"
      :title="$t('confirmCancelOrderTitle')"
      hide-header
      hide-footer
      centered
    >
      <div class="modal-content-wrapper">
        <div class="delete-confirmation-content">
          <div class="delete-icon-wrapper">
            <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
          </div>
          <h3 class="delete-confirmation-title">{{ $t("confirmCancelOrderTitle") || "تأكيد إلغاء الطلب" }}</h3>
          <p class="delete-confirmation-text">
            {{ $t("confirmCancelOrderMessage") || "سيتم إلغاء فاتورة الطاولة بالكامل وتحريرها. لن تُحسب كمبيع." }}
          </p>
          <div class="delete-confirmation-actions">
            <button type="button" class="delete-confirm-button" @click="confirmCancelDineInOrder">
              <b-icon icon="check-circle-fill" class="me-2"></b-icon>
              {{ $t("confirmButton") }}
            </button>
            <button type="button" class="delete-cancel-button" @click="closeModel('modal-cancel-order')">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancelButton") }}
            </button>
          </div>
        </div>
      </div>
    </b-modal>

    <b-overlay
      :show="show"
      spinner-variant="danger"
      spinner-type="grow"
      spinner-large
      rounded="sm"
    >
    <AppHeader
      v-if="!posFloorPlanGateVisible"
      :show-pos-fullscreen-button="true"
      :pos-fullscreen-active="isFullscreen"
      @toggle-pos-fullscreen="toggleFullscreen"
    >
      <template #header-start>
        <button
          type="button"
          class="app-top-header-tables-btn"
          :class="{ 'app-top-header-tables-btn--active': posFloorPlanGateVisible }"
          @click="initPosFloorPlanGate"
          :title="$t('backToTables') || 'الرجوع إلى الطاولات'"
        >
          <b-icon icon="table" class="app-top-header-tables-btn-icon"></b-icon>
          <span class="app-top-header-tables-btn-text">{{ $t("tables") || "الطاولات" }}</span>
        </button>
      </template>
    </AppHeader>
    <div
        class="main-content-wrapper pos-route pos-route--v2"
        :class="{ 'pos-fullscreen': isFullscreen }"
      >
        <b-container fluid class="pos-container-fluid">
          <div class="pos-page-container pos-page-container--v2">
            <div class="pos-workspace pos-workspace--v2">
              <main class="pos-workspace-main">
            <!-- الكتالوج: بحث، طاولات، أقسام، منتجات -->
            <div class="pos-main-section pos-main-section--v2">
            <!-- Tables: summary bar + modal picker + row actions (one card) -->
            <div class="pos-tables-section-compact">
              <div class="pos-tables-block">
                <div class="pos-tables-toolbar-unified">
                  <div class="pos-tables-picker-main">
                    <b-icon icon="table" class="pos-tables-picker-icon pos-tables-picker-icon--toolbar"></b-icon>
                    <div class="pos-tables-picker-text pos-tables-picker-text--inline">
                      <span class="pos-tables-picker-label">{{ $t("tables") || "الطاولات" }}</span>
                      <span class="pos-tables-picker-sep" aria-hidden="true">·</span>
                      <span class="pos-tables-picker-value">{{ selectedTableSummary }}</span>
                      <span
                        v-if="selectedTableId && mergedTableIds.length > 1"
                        class="pos-tables-picker-badge"
                        :title="$t('mergedTables') || ''"
                      >
                        {{ mergedTableIds.length }}
                      </span>
                    </div>
                  </div>
                  <div class="pos-tables-toolbar-end">
                    <div v-if="selectedTableId" class="pos-table-actions-buttons pos-table-actions-buttons--inline">
                      <div class="pos-table-action-row pos-table-action-row--ops">
                        <div
                          v-if="selectedTable && selectedTable.status === 'Occupied'"
                          class="pos-table-action-transfer-group"
                          dir="ltr"
                        >
                          <button
                            class="pos-table-action-btn pos-table-action-transfer pos-table-action-transfer--merge"
                            @click="openOrderMoveModal('merge')"
                          >
                            <b-icon icon="layers"></b-icon>
                            <span>{{ $t("mergeTwoInvoices") || "دمج فاتورتين" }}</span>
                          </button>
                          <button
                            class="pos-table-action-btn pos-table-action-transfer pos-table-action-transfer--full"
                            @click="openOrderMoveModal('full')"
                          >
                            <b-icon icon="arrow-left-right"></b-icon>
                            <span>{{ $t("transferFullOrder") || "نقل الطلب كامل" }}</span>
                          </button>
                          <button
                            class="pos-table-action-btn pos-table-action-transfer pos-table-action-transfer--item"
                            @click="openOrderMoveModal('item')"
                          >
                            <b-icon icon="arrow-left-right"></b-icon>
                            <span>{{ $t("transferOneItem") || "نقل عنصر" }}</span>
                          </button>
                        </div>
                        <button
                          type="button"
                          class="pos-table-action-btn pos-table-action-btn--off-table"
                          @click="startOffTableOrderSession('Takeaway')"
                        >
                          <b-icon icon="bag"></b-icon>
                          <span>{{ $t("newOffTableOrder") || "طلب بدون طاولة" }}</span>
                        </button>
                      </div>
                      <div v-if="carditems.length > 0" class="pos-table-action-row pos-table-action-row--save">
                        <template v-if="mergedTableIds.length > 1">
                          <button class="pos-table-action-btn pos-table-action-save" :disabled="orderPersisting" @click="addOrderAndClear(true)">
                            <b-icon icon="check-circle-fill"></b-icon>
                            <span>{{ $t("saveForAllMergedTables") || "حفظ لجميع الطاولات" }}</span>
                          </button>
                          <button class="pos-table-action-btn pos-table-action-save-print" :disabled="orderPersisting" @click="addOrderAndClear(false)">
                            <b-icon icon="printer-fill"></b-icon>
                            <span>{{ $t("saveAndPrint") || "حفظ وطباعة" }}</span>
                          </button>
                        </template>
                        <template v-else>
                          <button class="pos-table-action-btn pos-table-action-save" :disabled="orderPersisting" @click="addOrderAndClear(true)">
                            <b-icon icon="check-circle-fill"></b-icon>
                            <span>{{ $t("save") || "حفظ" }}</span>
                          </button>
                          <button class="pos-table-action-btn pos-table-action-save-print" :disabled="orderPersisting" @click="addOrderAndClear(false)">
                            <b-icon icon="printer-fill"></b-icon>
                            <span>{{ $t("saveAndPrint") || "حفظ وطباعة" }}</span>
                          </button>
                        </template>
                      </div>
                    </div>
                  </div>
                </div>
                <TableReservationInfoBanner embedded :reservation="activeTableReservation" />
              </div>
            </div>

            <!-- Categories: step browse (root → sub) then products -->
            <div class="pos-categories-scroll">
              <div class="pos-browse-toolbar">
                <button
                  v-if="posBrowseStep !== 'roots'"
                  type="button"
                  class="pos-browse-back-btn"
                  @click="posGoBack"
                >
                  <b-icon icon="arrow-right"></b-icon>
                  <span>{{ $t("posBack") }}</span>
                </button>
                <div class="pos-browse-titles">
                  <span class="pos-browse-primary">{{ posBrowseToolbarPrimary }}</span>
                  <span v-if="posBrowseToolbarSecondary" class="pos-browse-secondary">{{ posBrowseToolbarSecondary }}</span>
                </div>
              </div>

              <div v-if="posBrowseStep === 'roots'" class="pos-categories-list">
                <button
                  type="button"
                  class="pos-category-btn pos-category-btn--all"
                  @click="posSelectAllProducts"
                >
                  <span class="pos-category-btn-icon" aria-hidden="true">
                    <b-icon icon="grid-3x3-gap-fill" />
                  </span>
                  <span class="pos-category-btn-label">{{ $t("all") }}</span>
                </button>
                <button
                  v-for="tag in posRootTagsList"
                  :key="tag.id"
                  type="button"
                  class="pos-category-btn"
                  :class="{ 'pos-category-btn--has-subs': posCategoryHasSubs(tag) }"
                  :style="posCategoryTileStyle(tag)"
                  @click="posSelectRoot(tag)"
                >
                  <span class="pos-category-btn-icon" aria-hidden="true">
                    <b-icon :icon="posCategoryHasSubs(tag) ? 'folder2' : 'tag-fill'" />
                  </span>
                  <span class="pos-category-btn-label">{{ tag.name }}</span>
                  <b-icon
                    v-if="posCategoryHasSubs(tag)"
                    icon="chevron-left"
                    class="pos-category-btn-arrow"
                  />
                </button>
              </div>

              <div v-else-if="posBrowseStep === 'subs'" class="pos-categories-list">
                <button
                  v-for="tag in posSubTagsList"
                  :key="tag.id"
                  type="button"
                  class="pos-category-btn"
                  :style="posCategoryTileStyle(tag)"
                  @click="posSelectSub(tag)"
                >
                  <span class="pos-category-btn-icon" aria-hidden="true">
                    <b-icon icon="tag-fill" />
                  </span>
                  <span class="pos-category-btn-label">{{ tag.name }}</span>
                </button>
              </div>
            </div>

            <!-- Products Grid -->
            <div v-if="posBrowseStep === 'products'" class="pos-products-grid-section">
              <div class="pos-products-grid">
                <div
                  class="pos-product-card"
                  :class="{ 'pos-product-card-unavailable': !item.isAvailable }"
                  v-for="item in Items"
                  :key="item.id"
                  @click="item.isAvailable ? addToCartList(item) : null"
                >
                  <!-- Discount Badge -->
                  <div
                    v-if="item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
                    class="pos-product-discount-badge"
                  >
                    <b-icon icon="tag-fill" class="me-1"></b-icon>
                    {{ $t("discountLabel") }}
                  </div>

                  <!-- Product Image/Barcode -->
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

                  <!-- Product Info -->
                  <div class="pos-product-info">
                    <h4 class="pos-product-name">{{ item.name }}</h4>
                    <div class="pos-product-unavailable-badge" v-if="!item.isAvailable">
                      <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                      {{ $t("notAvailable") || "غير متوفر" }}
                    </div>
                  </div>
                </div>
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
                <TableReservationInfoBanner
                  v-if="selectedTableId"
                  :reservation="activeTableReservation"
                />
                <!-- Cart Items List -->
                <div class="pos-cart-items-section">
                  <div class="pos-cart-header">
                    <div class="pos-cart-title-group">
                      <span class="pos-cart-title-icon-wrap" aria-hidden="true">
                        <b-icon icon="cart-fill" class="pos-cart-title-icon"></b-icon>
                      </span>
                      <div class="pos-cart-title-copy">
                        <h3 class="pos-cart-title">{{ $t("cart") || "السلة" }}</h3>
                        <p class="pos-cart-title-sub">
                          <template v-if="carditems.length > 0">
                            <span class="pos-cart-title-count">{{ carditems.length }}</span>
                            {{ $t("itemLabel") || "صنف" }}
                          </template>
                          <template v-else>{{ $t("cartEmptyHint") || "أضف أصناف من القائمة" }}</template>
                        </p>
                      </div>
                    </div>
                    <div class="pos-cart-header-actions" v-if="carditems.length > 0">
                      <button
                        v-if="canCancelDineInOrder()"
                        type="button"
                        class="pos-cart-header-clear-btn pos-cart-header-cancel-btn"
                        @click.stop="openCancelDineInOrderModal"
                        :title="$t('cancelOrder') || 'إلغاء الطلب'"
                      >
                        <b-icon icon="x-circle-fill" class="pos-cart-header-clear-ic"></b-icon>
                        <span class="pos-cart-header-clear-label">{{ $t("cancelOrder") || "إلغاء الطلب" }}</span>
                      </button>
                      <button
                        v-if="!canCancelDineInOrder()"
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
                      class="pos-cart-item pos-cart-item--v2"
                      v-for="(item, index) in carditems"
                      :key="index"
                    >
                      <div class="pos-cart-item-top">
                        <div class="pos-cart-item-name-wrap">
                          <h4 class="pos-cart-item-name">{{ item.name }}</h4>
                          <p v-if="item.lineNote" class="pos-cart-item-line-note">{{ item.lineNote }}</p>
                        </div>
                        <div class="pos-cart-item-line-total">
                          {{ formatPrice(item.total) }} {{ $t("currency") }}
                        </div>
                      </div>
                      <div class="pos-cart-item-bottom">
                        <div class="pos-cart-item-unit-wrap">
                          <span class="pos-cart-item-unit-price">
                            {{ $t("unitPrice") }}:
                            {{
                              formatPrice(
                                (item.disCountPrice > 0 && item.disCountPrice !== item.price)
                                  ? item.disCountPrice
                                  : (item.price || 0)
                              )
                            }}
                            {{ $t("currency") }}
                          </span>
                          <span class="pos-cart-item-qty-badge">× {{ item.quantity }}</span>
                        </div>
                        <div class="pos-cart-item-controls">
                          <div class="pos-cart-item-quantity">
                            <button
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
                              class="pos-quantity-btn pos-quantity-increase"
                              @click.stop="increaseQuantity(index)"
                              :title="$t('increase') || 'زيادة'"
                            >
                              <b-icon icon="plus-lg"></b-icon>
                            </button>
                          </div>

                          <button
                            type="button"
                            class="pos-cart-item-note"
                            :class="{ 'pos-cart-item-note--active': item.lineNote }"
                            @click.stop="openCartLineNoteModal(index)"
                            :title="$t('itemLineNote') || 'ملاحظة الصنف'"
                          >
                            <b-icon icon="chat-left-text"></b-icon>
                          </button>
                          <button
                            class="pos-cart-item-transfer"
                            @click.stop="openOrderMoveModal('item', item)"
                            :title="$t('transferOneItem') || 'نقل عنصر'"
                          >
                            <b-icon icon="arrow-left-right"></b-icon>
                          </button>
                          <button
                            class="pos-cart-item-delete"
                            @click.stop="openDeleteItemConfirm(index)"
                            :title="$t('delete') || 'حذف'"
                          >
                            <b-icon icon="x-lg"></b-icon>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="pos-cart-empty" v-else>
                    <b-icon icon="cart-x" class="pos-cart-empty-icon"></b-icon>
                    <p class="pos-cart-empty-text">{{ $t("emptyCart") || 'السلة فارغة' }}</p>
                  </div>
                  
                  <!-- Order Notes Section -->
                  <div class="pos-orders-notes-section" v-if="tableOrders.length > 0 && hasOrderNotes">
                    <div class="pos-orders-notes-header">
                      <b-icon icon="file-text-fill" class="me-2"></b-icon>
                      <h4 class="pos-orders-notes-title">{{ $t("orderNotes") || "ملاحظات الطلبات" }}</h4>
                    </div>
                    <div class="pos-orders-notes-list">
                      <div 
                        class="pos-order-note-item" 
                        v-for="(order, index) in tableOrdersWithNotes" 
                        :key="order.id || index"
                      >
                        <div class="pos-order-note-header">
                          <span class="pos-order-note-code">
                            <b-icon icon="receipt" class="me-1"></b-icon>
                            {{ order.orderCode || `#${order.id}` }}
                          </span>
                          <span class="pos-order-note-date" v-if="order.insertDate">
                            {{ formatDate(order.insertDate) }}
                          </span>
                        </div>
                        <div class="pos-order-note-content">
                          {{ order.notes }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            </aside>
            </div>

            <!-- Empty Cart Modal -->
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

            <b-modal id="modal-delete-cart-item" :title="$t('deleteConfirmationModalTitle')" hide-header hide-footer class="users-modal">
              <div class="modal-content-wrapper">
                <div class="delete-confirmation-content">
                  <div class="delete-icon-wrapper">
                    <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
                  </div>
                  <h3 class="delete-confirmation-title">{{ $t("deleteConfirmationModalTitle") || "تأكيد عملية المسح" }}</h3>
                  <p class="delete-confirmation-text">
                    {{ $t("confirmDeleteCartItemMessage") || "هل أنت متأكد من حذف هذا العنصر من السلة؟" }}
                  </p>
                  <div class="delete-confirmation-actions">
                    <button class="delete-confirm-button" @click="confirmDeleteCartItem">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("confirmButton") }}
                    </button>
                    <button class="delete-cancel-button" @click="closeModel('modal-delete-cart-item')">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

            <b-modal
              id="modal-cart-line-note"
              :title="$t('itemLineNote') || 'ملاحظة الصنف'"
              hide-header
              hide-footer
              class="users-modal"
            >
              <div class="modal-content-wrapper">
                <div class="order-notes-content">
                  <div class="order-notes-header">
                    <b-icon icon="chat-left-text" class="me-2"></b-icon>
                    <h3 class="order-notes-title">{{ $t("itemLineNote") || "ملاحظة الصنف" }}</h3>
                  </div>
                  <p v-if="lineNoteCartItemName" class="pos-line-note-item-name">{{ lineNoteCartItemName }}</p>
                  <p class="pos-line-note-hint">{{ $t("itemLineNoteHint") || "تظهر في طباعة المطبخ فقط" }}</p>
                  <div class="order-notes-input-wrapper">
                    <label class="order-notes-label">{{ $t("itemLineNoteLabel") || "الملاحظة" }}</label>
                    <textarea
                      v-model="lineNoteDraft"
                      class="order-notes-textarea"
                      :placeholder="$t('itemLineNotePlaceholder') || 'مثال: بدون بصل، حار جداً...'"
                      rows="3"
                      maxlength="500"
                    ></textarea>
                  </div>
                  <div class="order-notes-actions">
                    <button type="button" class="order-notes-confirm-button" @click="saveCartLineNote">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("save") || "حفظ" }}
                    </button>
                    <button
                      v-if="lineNoteDraft"
                      type="button"
                      class="order-notes-cancel-button"
                      @click="clearCartLineNote"
                    >
                      <b-icon icon="trash" class="me-2"></b-icon>
                      {{ $t("clear") || "مسح" }}
                    </button>
                    <button type="button" class="order-notes-cancel-button" @click="$bvModal.hide('modal-cart-line-note')">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") || "إلغاء" }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

            <!-- Cancel DineIn Order Modal -->
            <b-modal id="modal-order-move" :title="orderMoveTitle" hide-header hide-footer class="users-modal">
              <div class="modal-content-wrapper">
                <div class="delete-confirmation-content">
                  <div class="delete-icon-wrapper">
                    <b-icon icon="arrow-left-right" class="delete-warning-icon"></b-icon>
                  </div>
                  <h3 class="delete-confirmation-title">{{ orderMoveTitle }}</h3>
                  <p class="delete-confirmation-text">{{ orderMoveMessage }}</p>

                  <div class="users-form-grid" style="width:100%;">
                    <div class="users-input-group">
                      <label>{{ $t("sourceZoneFilterLabel") || "موقع المصدر" }}</label>
                      <select
                        v-model="orderMove.sourceZoneFilter"
                        class="users-form-input"
                        @change="onOrderMoveSourceZoneFilterChanged"
                      >
                        <option value="">{{ $t("allZones") || "جميع المواقع" }}</option>
                        <option v-for="zone in uniqueZones" :key="'om-src-zone-' + zone" :value="zone">{{ zone }}</option>
                      </select>
                    </div>
                    <div class="users-input-group">
                      <label>{{ $t("sourceTable") || "الطاولة المصدر" }}</label>
                      <select v-model.number="orderMove.sourceTableId" class="users-form-input" @change="onOrderMoveSourceChanged">
                        <option :value="null">{{ $t("selectTable") || "اختر طاولة" }}</option>
                        <option
                          v-for="table in orderMoveSourceTables"
                          :key="`src-${table.id}`"
                          :value="table.id"
                        >
                          {{ formatOrderMoveTableOption(table) }}
                        </option>
                      </select>
                    </div>

                    <div class="users-input-group">
                      <label>{{ $t("destinationZoneFilterLabel") || "موقع الهدف" }}</label>
                      <select
                        v-model="orderMove.destinationZoneFilter"
                        class="users-form-input"
                        @change="onOrderMoveDestinationZoneFilterChanged"
                      >
                        <option value="">{{ $t("allZones") || "جميع المواقع" }}</option>
                        <option v-for="zone in uniqueZones" :key="'om-dst-zone-' + zone" :value="zone">{{ zone }}</option>
                      </select>
                    </div>
                    <div class="users-input-group">
                      <label>{{ $t("destinationTable") || "الطاولة الهدف" }}</label>
                      <select v-model.number="orderMove.destinationTableId" class="users-form-input">
                        <option :value="null">{{ $t("selectTable") || "اختر طاولة" }}</option>
                        <option
                          v-for="table in orderMoveDestinationTables"
                          :key="`dst-${table.id}`"
                          :value="table.id"
                        >
                          {{ formatOrderMoveTableOption(table) }}
                        </option>
                      </select>
                    </div>

                    <div v-if="orderMove.mode === 'item'" class="users-input-group">
                      <label>{{ $t("orderItem") || "العنصر" }}</label>
                      <select v-model.number="orderMove.sourceOrderItemId" class="users-form-input" @change="syncOrderMoveQuantityFromSelection">
                        <option :value="null">{{ $t("selectItem") || "اختر عنصر" }}</option>
                        <option
                          v-for="opt in orderMove.sourceItems"
                          :key="`item-${opt.sourceOrderItemId}`"
                          :value="opt.sourceOrderItemId"
                        >
                          {{ opt.label }}
                        </option>
                      </select>
                    </div>
                    <div v-if="orderMove.mode === 'item' && orderMove.sourceOrderItemId" class="users-input-group">
                      <label>{{ $t("quantity") || "الكمية" }}</label>
                      <input
                        v-model.number="orderMove.transferQuantity"
                        type="number"
                        class="users-form-input"
                        min="1"
                        :max="orderMoveSelectedItemMaxQuantity"
                      />
                      <small class="text-muted">
                        {{ ($t("maxQuantity") || "الكمية القصوى") }}: {{ orderMoveSelectedItemMaxQuantity }}
                      </small>
                    </div>
                  </div>

                  <div class="table-close-actions order-move-actions">
                    <button class="delete-cancel-button order-move-cancel-btn" @click="closeOrderMoveModal">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") || "إلغاء" }}
                    </button>
                    <button class="table-close-action-btn table-close-action-print order-move-confirm-btn" :disabled="!orderMoveCanConfirm || orderMove.submitting" @click="confirmOrderMove">
                      <b-spinner small v-if="orderMove.submitting" class="me-2"></b-spinner>
                      <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
                      {{ orderMoveConfirmLabel }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

            <b-modal
              id="modal-sensitive-action-password"
              :title="$t('sensitiveActionPasswordTitle') || 'تأكيد الباسورد'"
              hide-header
              hide-footer
              class="users-modal"
              @hidden="onSensitiveActionPasswordModalHidden"
            >
              <div class="modal-content-wrapper">
                                <div class="order-notes-content">
                  <div class="order-notes-header">
                    <b-icon icon="shield-lock" class="me-2"></b-icon>
                    <h3 class="order-notes-title">{{ $t("sensitiveActionPasswordTitle") || "تأكيد الباسورد" }}</h3>
                  </div>
                  <div class="order-notes-input-wrapper">
                    <label class="order-notes-label">
                      {{ $t("sensitiveActionLabel") || "الإجراء" }}:
                      {{ sensitiveActionLabel }}
                    </label>
                  </div>
                  <div class="order-notes-input-wrapper">
                    <label class="order-notes-label">{{ sensitiveAuthFieldLabel }}</label>
                    <input
                      v-model="sensitiveActionAuth.password"
                      type="password"
                      :inputmode="sensitiveAuthUsesOwnLoginCode ? 'numeric' : null"
                      :maxlength="sensitiveAuthUsesOwnLoginCode ? 12 : null"
                      class="order-notes-input"
                      :placeholder="sensitiveAuthFieldPlaceholder"
                      autocomplete="off"
                      @keyup.enter="confirmSensitiveActionPassword"
                    />
                    <small v-if="!sensitiveAuthUsesOwnLoginCode" class="text-muted d-block mt-1">
                      {{ $t("sensitiveAuthPosHint") }}
                    </small>
                  </div>
                  <div class="order-notes-actions">
                    <button class="order-notes-confirm-button" :disabled="sensitiveActionAuth.verifying" @click="confirmSensitiveActionPassword">
                      <b-spinner small v-if="sensitiveActionAuth.verifying" class="me-2"></b-spinner>
                      <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("verifyPasswordAction") || "تأكيد الباسورد" }}
                    </button>
                    <button class="order-notes-cancel-button" :disabled="sensitiveActionAuth.verifying" @click="closeSensitiveActionPasswordModal">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") || "إلغاء" }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

            <!-- Delivery Information Modal -->
            <b-modal
              id="modal-delivery-info"
              modal-class="users-modal pos-delivery-modal"
              hide-header
              hide-footer
              centered
              size="lg"
            >
              <div class="modal-content-wrapper">
                <div class="pos-modal-custom-header">
                  <h3 class="delete-confirmation-title mb-0">
                    {{ $t('deliveryInformation') || 'معلومات التوصيل' }}
                  </h3>
                  <button class="pos-modal-close-btn" @click="$bvModal.hide('modal-delivery-info')">
                    <b-icon icon="x-lg" class="me-2"></b-icon>
                    {{ $t("close") || "إغلاق" }}
                  </button>
                </div>
                <div class="delivery-info-section">
                <form class="users-form">
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="person-lines-fill" class="form-label-icon"></b-icon>
                      {{ $t("customerRecipientSelection") || "المستلم" }}
                    </label>
                    <div class="delivery-radio-group">
                      <label class="delivery-radio-label">
                        <input
                          type="radio"
                          v-model="useExistingCustomer"
                          :value="true"
                          class="delivery-radio-input"
                        />
                        <span class="delivery-radio-text">{{ $t("useExistingCustomer") || "استخدام عميل موجود" }}</span>
                      </label>
                      <label class="delivery-radio-label">
                        <input
                          type="radio"
                          v-model="useExistingCustomer"
                          :value="false"
                          class="delivery-radio-input"
                        />
                        <span class="delivery-radio-text">{{ $t("addNewCustomerDelivery") || "إضافة عميل جديد" }}</span>
                      </label>
                    </div>
                  </div>

                  <div v-if="useExistingCustomer" class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                      {{ $t("selectCustomer") || "اختر العميل" }}
                    </label>
                    <select
                      v-model="selectedDeliveryCustomerId"
                      class="users-form-select"
                      :disabled="loadingDeliveryCustomers"
                      @change="applySelectedDeliveryCustomer"
                    >
                      <option value="">{{ $t("selectCustomer") || "اختر العميل" }}</option>
                      <option
                        v-for="c in deliveryCustomers.filter((x) => x.isActive !== false)"
                        :key="'dc-' + c.id"
                        :value="c.id"
                      >
                        {{ c.name }} — {{ c.phoneNumber }}
                      </option>
                    </select>
                  </div>

                  <div v-else class="users-form-group">
                    <button
                      type="button"
                      class="delivery-add-btn"
                      @click="showAddCustomerModal = true"
                    >
                      <b-icon icon="person-plus-fill" class="me-2"></b-icon>
                      {{ $t("addNewCustomerDelivery") || "إضافة عميل جديد" }}
                    </button>
                  </div>

                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="person-badge-fill" class="form-label-icon"></b-icon>
                      {{ $t("customerName") || "اسم المستلم" }} <span class="required">*</span>
                    </label>
                    <input
                      v-model="orderForSend.deliveryCustomerName"
                      type="text"
                      class="users-form-input"
                      :placeholder="$t('enterCustomerName') || 'أدخل اسم المستلم'"
                      :disabled="useExistingCustomer"
                      required
                    />
                  </div>

                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                      {{ $t("deliveryPhoneNumber") || "رقم هاتف المستلم" }} <span class="required">*</span>
                    </label>
                    <input
                      v-model="orderForSend.deliveryPhoneNumber"
                      type="text"
                      class="users-form-input"
                      :placeholder="$t('enterPhoneNumber') || 'أدخل رقم الهاتف'"
                      :disabled="useExistingCustomer"
                      required
                    />
                  </div>

                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                      {{ $t("deliveryAddress") || "عنوان التوصيل" }} <span class="required">*</span>
                    </label>
                    <textarea
                      v-model="orderForSend.deliveryAddress"
                      class="users-form-input"
                      rows="2"
                      :placeholder="$t('enterDeliveryAddress') || 'أدخل عنوان التوصيل'"
                      required
                    ></textarea>
                  </div>

                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="cash-coin" class="form-label-icon"></b-icon>
                      {{ $t("deliveryFee") || "رسوم التوصيل" }}
                    </label>
                    <input
                      v-model.number="orderForSend.deliveryFee"
                      type="number"
                      class="users-form-input"
                      min="0"
                      step="0.01"
                      :placeholder="$t('enterDeliveryFee') || 'أدخل رسوم التوصيل (اختياري)'"
                    />
                  </div>

                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="truck" class="form-label-icon"></b-icon>
                      {{ $t("driverSelection") || "اختيار السائق" }}
                    </label>
                    <div class="delivery-radio-group">
                      <label class="delivery-radio-label">
                        <input
                          type="radio"
                          v-model="useExistingDriver"
                          :value="true"
                          class="delivery-radio-input"
                        />
                        <span class="delivery-radio-text">{{ $t("useExistingDriver") || "استخدام سائق موجود" }}</span>
                      </label>
                      <label class="delivery-radio-label">
                        <input
                          type="radio"
                          v-model="useExistingDriver"
                          :value="false"
                          class="delivery-radio-input"
                        />
                        <span class="delivery-radio-text">{{ $t("addNewDriver") || "إضافة سائق جديد" }}</span>
                      </label>
                    </div>
                  </div>

                  <div v-if="useExistingDriver" class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="person-badge" class="form-label-icon"></b-icon>
                      {{ $t("selectDriver") || "اختر السائق" }}
                    </label>
                    <select
                      v-model="orderForSend.deliveryDriverId"
                      class="users-form-select"
                      :disabled="loadingDeliveryDrivers"
                    >
                      <option value="">{{ $t("selectDriver") || "اختر السائق" }}</option>
                      <option
                        v-for="driver in deliveryDrivers.filter(d => d.isActive)"
                        :key="driver.id"
                        :value="driver.id"
                      >
                        {{ driver.name }} - {{ driver.phoneNumber }}
                      </option>
                    </select>
                  </div>

                  <div v-else class="users-form-group">
                    <button
                      type="button"
                      class="delivery-add-btn"
                      @click="showAddDriverModal = true"
                    >
                      <b-icon icon="person-plus-fill" class="me-2"></b-icon>
                      {{ $t("addNewDriver") || "إضافة سائق جديد" }}
                    </button>
                  </div>

                  <div class="order-notes-actions mt-3">
                    <button
                      type="button"
                      class="order-notes-confirm-button"
                      @click="$bvModal.hide('modal-delivery-info')"
                    >
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("save") || "حفظ" }}
                    </button>
                  </div>
                </form>
                </div>
              </div>
            </b-modal>

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
          <img
            v-if="commercialUserInfo.logo"
            :src="commercialUserInfo.logo"
            alt="logo"
            class="bill-logo-img"
          />
          <img
            v-else
            src="@/assets/logoarabic.png"
            alt="logo"
            class="bill-logo-img"
          />
          <h2 class="bill-store-name">{{ commercialUserInfo.restaurantName || 'LiteCashier' }}</h2>
          <p class="bill-store-subtitle">{{ $t("app-name") }}</p>
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
          <div class="bill-info-row" v-if="selectedTableId">
            <span class="bill-info-label">{{ $t("tableNumber") || "رقم الطاولة" }}:</span>
            <span class="bill-info-value">{{ selectedTableSummary }}</span>
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
          <div class="bill-info-row" v-if="orderForSend.pagerNumber">
            <span class="bill-info-label">{{ $t("pagerNumber") || "رقم جهاز النداء" }}:</span>
            <span class="bill-info-value">{{ orderForSend.pagerNumber }}</span>
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
                  <span v-if="item.disCountPrice > 0 && item.disCountPrice !== item.price" class="bill-discount-badge">خصم</span>
                </td>
                <td class="bill-item-qty">{{ item.quantity }}</td>
                <td class="bill-item-price">
                  <span v-if="item.disCountPrice > 0 && item.disCountPrice !== item.price" class="bill-price-discounted">
                    <span class="bill-original-price">{{ formatPrice(item.price || 0) }}</span>
                    <span class="bill-discount-price">{{ formatPrice(item.disCountPrice) }}</span>
                  </span>
                  <span v-else>{{ formatPrice(item.price || 0) }}</span>
                </td>
                <td class="bill-item-total">{{ formatPrice(((item.disCountPrice > 0 && item.disCountPrice !== item.price) ? item.disCountPrice : (item.price || 0)) * (item.quantity || 1)) }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Divider -->
        <div class="bill-divider"></div>

        <!-- Summary Section -->
        <div class="bill-summary-section">
          <div class="bill-summary-row">
            <span class="bill-summary-label">{{ $t("count") }}:</span>
            <span class="bill-summary-value">{{ totalCardItems }} {{ $t("items") }}</span>
          </div>
          <div class="bill-summary-row" v-if="orderDiscountAmount > 0">
            <span class="bill-summary-label">{{ $t("discountLabel") || "الخصم" }}:</span>
            <span class="bill-summary-value">- {{ formatPrice(orderDiscountAmount) }} {{ $t("currency") }}</span>
          </div>
          <div class="bill-summary-row bill-summary-total">
            <span class="bill-summary-label">{{ $t("total") }}:</span>
            <span class="bill-summary-value">{{ formattedNumber }} {{ $t("currency") }}</span>
          </div>
        </div>

        <!-- Notes and Pager Section -->
        <div class="bill-notes-section" v-if="orderForSend.notes || orderForSend.pagerNumber">
          <div class="bill-divider"></div>
          <div class="bill-notes-content" v-if="orderForSend.notes">
            <div class="bill-notes-label">{{ $t("notes") || "ملاحظات" }}:</div>
            <div class="bill-notes-text">{{ orderForSend.notes }}</div>
          </div>
          <div class="bill-notes-content" v-if="orderForSend.pagerNumber">
            <div class="bill-notes-label">{{ $t("pagerNumber") || "رقم جهاز النداء" }}:</div>
            <div class="bill-notes-text">{{ orderForSend.pagerNumber }}</div>
          </div>
        </div>

        <!-- Footer Section -->
        <div class="bill-footer">
          <p class="bill-footer-text">شكراً لزيارتكم</p>
          <p class="bill-footer-text">Thank you for your visit</p>
        </div>
      </div>
    </div>

    <!-- Tables picker modal (by zone) — same shell as users-modal elsewhere -->
    <b-modal
      id="modal-pos-tables"
      v-model="showTablesModal"
      :title="$t('tables') || 'الطاولات'"
      size="xl"
      scrollable
      hide-header
      hide-footer
      class="users-modal"
      centered
    >
      <div class="modal-content-wrapper pos-tables-picker-modal">
        <h2 class="modal-title">{{ $t("tables") || "الطاولات" }}</h2>
        <p class="pos-tables-picker-modal-hint">
          {{ $t("tablesListHint") || "اختر طاولة لفتح الطلب" }}
        </p>

      <b-overlay
        :show="loadingTableOrders"
        spinner-variant="primary"
        spinner-type="border"
        spinner-small
        rounded="sm"
        opacity="0.6"
      >
        <div class="pos-tables-modal-filters pos-tables-filters">
          <div class="pos-table-filter-group">
            <label class="pos-table-filter-label">
              <b-icon icon="geo-alt-fill" class="me-1"></b-icon>
              {{ $t("zone") || "الموقع" }}
            </label>
            <select v-model="tableFilters.zone" class="pos-table-filter-select">
              <option value="">{{ $t("allZones") || "جميع المواقع" }}</option>
              <option v-for="zone in uniqueZones" :key="zone" :value="zone">{{ zone }}</option>
            </select>
          </div>
          <div class="pos-table-filter-group">
            <label class="pos-table-filter-label">
              <b-icon icon="hash" class="me-1"></b-icon>
              {{ $t("tableNumber") || "رقم الطاولة" }}
            </label>
            <input
              v-model="tableFilters.tableNumber"
              type="number"
              :placeholder="$t('searchTableNumber') || 'ابحث برقم الطاولة'"
              class="pos-table-filter-input"
            />
          </div>
          <div class="pos-table-filter-group">
            <label class="pos-table-filter-label">
              <b-icon icon="filter" class="me-1"></b-icon>
              {{ $t("status") || "الحالة" }}
            </label>
            <select v-model="tableFilters.status" class="pos-table-filter-select">
              <option value="">{{ $t("allStatuses") || "جميع الحالات" }}</option>
              <option value="Available">{{ $t("available") || "متاحة" }}</option>
              <option value="Occupied">{{ $t("occupied") || "مشغولة" }}</option>
              <option value="Reserved">{{ $t("reserved") || "محجوزة" }}</option>
              <option value="OutOfService">{{ $t("outOfService") || "خارج الخدمة" }}</option>
            </select>
          </div>
          <button
            v-if="tableFilters.zone || tableFilters.tableNumber || tableFilters.status"
            type="button"
            class="pos-table-filter-clear"
            @click="clearTableFilters"
          >
            <b-icon icon="x-circle-fill" class="me-1"></b-icon>
            {{ $t("clearFilters") || "مسح الفلاتر" }}
          </button>
        </div>

        <div
          v-for="group in tablesGroupedByZone"
          :key="group.zoneKey"
          class="pos-tables-modal-zone"
        >
          <h4 class="pos-tables-modal-zone-title">
            <b-icon icon="geo-alt-fill" class="me-2"></b-icon>
            {{ group.zoneLabel }}
            <span class="pos-tables-modal-zone-count">({{ group.tables.length }})</span>
          </h4>
          <div class="pos-tables-scroll pos-tables-scroll-modal">
            <div
              v-for="table in group.tables"
              :key="table.id"
              class="pos-table-card-compact"
              :class="{
                'pos-table-available': table.status === 'Available',
                'pos-table-occupied': table.status === 'Occupied',
                'pos-table-reserved': table.status === 'Reserved',
                'pos-table-selected': selectedTableId === table.id || selectedTableIds.includes(table.id),
                'pos-table-multi-selected': selectedTableIds.includes(table.id) && selectedTableIds.length > 1,
                'pos-table-merged': mergedTableIds.includes(table.id) && mergedTableIds.length > 1
              }"
              @click="selectTableInModal(table, $event)"
            >
              <div class="pos-table-number-compact">
                <span v-if="table.mergedTableNumbers">{{ table.mergedTableNumbers }}</span>
                <span v-else>{{ table.tableNumber }}</span>
              </div>
              <div class="pos-table-status-compact" :class="`pos-table-status-${table.status.toLowerCase()}`">
                {{ getTableStatusText(table.status) }}
              </div>
              <div class="pos-table-zone-compact" v-if="table.zone">
                {{ table.zone }}
              </div>
            </div>
          </div>
        </div>

        <p v-if="!tablesGroupedByZone.length" class="pos-tables-modal-empty text-center text-muted py-4 mb-0">
          {{ $t("noTablesMatchFilters") || "لا توجد طاولات مطابقة للفلتر" }}
        </p>

        <div class="users-form-actions pos-tables-picker-modal-actions">
          <span class="pos-tables-picker-modal-count text-muted">
            {{ $t("tablesCount") || "العدد" }}: {{ filteredTables.length }}
          </span>
          <button type="button" class="users-form-cancel-button" @click="showTablesModal = false">
            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </b-overlay>
      </div>
    </b-modal>

    <!-- Add New Driver Modal -->
    <b-modal 
      v-model="showAddDriverModal" 
      :title="$t('addNewDriver') || 'إضافة سائق جديد'" 
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
      @hidden="resetNewDriverForm"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addNewDriver") || "إضافة سائق جديد" }}</h2>
        <form @submit.prevent="saveNewDriver" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                {{ $t("driverName") || "اسم السائق" }} <span class="required">*</span>
              </label>
              <input 
                v-model="newDriverForm.name" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterDriverName') || 'أدخل اسم السائق'"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                {{ $t("driverPhone") || "رقم هاتف السائق" }} <span class="required">*</span>
              </label>
              <input 
                v-model="newDriverForm.phoneNumber" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterDriverPhone') || 'أدخل رقم هاتف السائق'"
                required
              />
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
              {{ $t("driverAddress") || "عنوان السائق" }}
            </label>
            <input 
              v-model="newDriverForm.address" 
              type="text" 
              class="users-form-input"
              :placeholder="$t('enterDriverAddress') || 'أدخل عنوان السائق (اختياري)'"
            />
          </div>
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="car-front-fill" class="form-label-icon"></b-icon>
                {{ $t("vehicleType") || "نوع المركبة" }}
              </label>
              <input 
                v-model="newDriverForm.vehicleType" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterVehicleType') || 'مثال: دراجة، سيارة'"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="123" class="form-label-icon"></b-icon>
                {{ $t("vehicleNumber") || "رقم المركبة" }}
              </label>
              <input 
                v-model="newDriverForm.vehicleNumber" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterVehicleNumber') || 'أدخل رقم المركبة'"
              />
            </div>
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showAddDriverModal = false" :disabled="savingDriver">
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="savingDriver">
              <b-spinner small v-if="savingDriver" class="me-2"></b-spinner>
              {{ savingDriver ? ($t("adding") || "جاري الإضافة...") : ($t("add") || "إضافة") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- إضافة عميل من نافذة التوصيل (مثل إضافة السائق) -->
    <b-modal
      v-model="showAddCustomerModal"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
      @hidden="resetNewCustomerForm"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addNewCustomerDeliveryModal") || "إضافة عميل جديد" }}</h2>
        <form class="users-form" @submit.prevent="saveNewCustomerFromDelivery">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                {{ $t("customerNameField") || "اسم العميل" }} <span class="required">*</span>
              </label>
              <input
                v-model="newCustomerForm.name"
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
                v-model="newCustomerForm.phoneNumber"
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
              v-model="newCustomerForm.address"
              type="text"
              class="users-form-input"
              :placeholder="$t('enterAddress') || 'العنوان (اختياري)'"
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="chat-left-text-fill" class="form-label-icon"></b-icon>
              {{ $t("notes") }}
            </label>
            <textarea
              v-model="newCustomerForm.notes"
              class="users-form-input"
              rows="2"
              :placeholder="$t('customerNotesPlaceholder') || ''"
            ></textarea>
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" :disabled="savingDeliveryCustomer" @click="showAddCustomerModal = false">
              {{ $t("cancel") }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="savingDeliveryCustomer">
              <b-spinner v-if="savingDeliveryCustomer" small class="me-2"></b-spinner>
              {{ savingDeliveryCustomer ? ($t("adding") || "جاري الإضافة...") : ($t("add") || "إضافة") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import CalculatorComp from "@/components/CalculatorComp.vue";
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";
import { HTTP } from "@/http/api.js";
import { htmlToPaper } from 'vue-html-to-paper';
import signalRService from "@/services/signalr.js";
import {
  RECEIPT_PRINT_STYLES_HTML,
  buildReceiptPrintDocument,
  getReceiptHtmlFromElement,
  PRINT_API_TIMEOUT_MS,
  PRINT_SERVER_FETCH_TIMEOUT_MS,
  computeGroupPrintTotals,
  buildReceiptItemsTableHtml,
  buildReceiptSummaryHtml,
  replaceReceiptSummarySection,
  stripKitchenFinancialFromReceiptHtml,
  ensurePrintTableNumberInHtml,
  ensurePrintOrderCodeInHtml,
} from "@/utils/receiptPrint.js";
import {
  rootTags,
  childTagsOf,
  tagItemStorageValue,
  tagDisplayName,
  groupItemsForDepartmentPrinting,
} from "@/utils/tagHierarchy.js";
import { resolveFloorPlanOverlaps } from "@/utils/floorPlanLayout.js";
import posOrderPersistMixin from "@/mixins/posOrderPersistMixin.js";
import posTableSelectMixin from "@/mixins/posTableSelectMixin.js";
import posFullscreenMixin from "@/mixins/posFullscreenMixin.js";
import CardPaymentWaitModal from "@/components/CardPaymentWaitModal.vue";
import TableGuestsModal from "@/components/TableGuestsModal.vue";
import TableReservationInfoBanner from "@/components/Restaurant/TableReservationInfoBanner.vue";
import { findCartLineIndex, mergeCartLines } from "@/utils/mergeCartLines.js";
// import store from '../store/store'; // Adjust the path based on your actual folder structure

export default {
  name: "PosView",
  mixins: [posOrderPersistMixin, posTableSelectMixin, posFullscreenMixin],
  components: {
    AppHeader,
    ClockVue,
    "vue-barcode": VueBarcode,
    CalculatorComp,
    CardPaymentWaitModal,
    TableGuestsModal,
    TableReservationInfoBanner,
  },
  data() {
    return {
      showbarCode: false,
      show: false,
      totaPrice: 0,
      carditems: [],
      lineNoteCartIndex: null,
      lineNoteDraft: "",
      typingTimer: null,
      lastAddedItem: null,
      itemsAddedCount: 0,
      addItemTimer: null,
      selectedPrinter: null,
      selectedPrinterId: null,
      availablePrinters: [],
      webPrintAPISupported: false,
      Items: [],
      tags: [],
      tagPrinters: [],
      managedPrinters: [],
      search: {
        info: "",
      },

      totalCardItems: 0,
      userInfo: {},
      commercialUserInfo: {
        restaurantName: '',
        logo: null,
        address: '',
        phone: ''
      },
      orderForSend: {
        orderCode: "",
        paymentMethod: "Cash",
        creditEmployeeId: null,
        creditCustomerId: null,
        customerOrderItem: [],
        orderType: "Takeaway",
        numberOfGuests: 0,
        tableId: null,
        reservationId: null,
        notes: "",
        pagerNumber: "",
        // Delivery fields
        deliveryDriverId: null,
        deliveryStatus: "Pending",
        deliveryAddress: "",
        deliveryPhoneNumber: "",
        deliveryCustomerName: "",
        deliveryFee: null,
        newDriverName: "",
        newDriverPhone: "",
        newDriverAddress: "",
        newDriverVehicleType: "",
        newDriverVehicleNumber: "",
        discountType: null,
        discountValue: null,
        discountAmount: 0,
        discountPercent: 0,
        orderSubTotal: 0,
        orderTotalAfterDiscount: 0
      },
      orderDiscountType: "amount",
      orderDiscountValue: null,
      orderDiscountPresets: [
        { id: "p5", type: "percentage", value: 5, label: "5%" },
        { id: "p10", type: "percentage", value: 10, label: "10%" },
        { id: "p15", type: "percentage", value: 15, label: "15%" },
        { id: "a5000", type: "amount", value: 5000, label: "5,000" },
        { id: "a10000", type: "amount", value: 10000, label: "10,000" },
      ],
      deliveryDrivers: [],
      loadingDeliveryDrivers: false,
      deliveryCustomers: [],
      loadingDeliveryCustomers: false,
      useExistingCustomer: true,
      selectedDeliveryCustomerId: "",
      showAddCustomerModal: false,
      savingDeliveryCustomer: false,
      newCustomerForm: {
        name: "",
        phoneNumber: "",
        address: "",
        notes: "",
      },
      useExistingDriver: true,
      showAddDriverModal: false,
      savingDriver: false,
      newDriverForm: {
        name: '',
        phoneNumber: '',
        address: '',
        vehicleType: '',
        vehicleNumber: ''
      },
      availableTables: [],
      allTables: [],
      selectedTableId: null,
      selectedTableIds: [], // للطاولات المحددة للدمج
      tableIds: [], // للطلب الجديد
      tableOrders: [],
      tableFilters: {
        zone: '',
        tableNumber: '',
        status: ''
      },
      tableToClose: null,
      tablesToClose: null, // For merged tables
      showTablesModal: false,
      loadingTableOrders: false,
      mergedTableIdsCache: {}, // Cache for merged table IDs

      posBrowseStep: "roots",
      posSelectedRoot: null,
      posSelectedSub: null,
      posMobileCartOpen: false,
      posFloorPlanGateVisible: false,
      posFloorPlanLoading: false,
      posFloorPlanSelectedKey: "",
      floorPlanGuestModal: {
        table: null,
        tableNumber: "",
        count: 1,
      },
      posFloorPlanAvailableKeys: [],
      posFloorPlanSettings: null,
      posFloorPlanPositions: {},
      posFloorPlanBackgroundColor: "#f1f5f9",
      posFloorTableChipSizePx: 56,
      posFloorPlanZoneRects: [],

      posFloorPlanForceDefaultTab: false,
      orderMove: {
        mode: "item", // item | full | merge
        sourceTableId: null,
        destinationTableId: null,
        sourceOrderItemId: null,
        transferQuantity: 1,
        sourceItems: [],
        submitting: false,
        sourceZoneFilter: "",
        destinationZoneFilter: "",
      },
      sensitiveActionAuth: {
        actionKey: "",
        password: "",
        verifying: false,
        resolver: null,
      },
      pendingDeleteItemIndex: null,
    };
  },

  computed: {
    posRootTagsList() {
      return rootTags(this.tags);
    },
    posSubTagsList() {
      return childTagsOf(this.posSelectedRoot, this.tags);
    },
    posBrowseToolbarPrimary() {
      if (this.posBrowseStep === "roots") {
        return this.$t("posChooseMainCategory");
      }
      if (this.posBrowseStep === "subs") {
        return this.$t("posChooseSubCategory");
      }
      return this.$t("posProductsToolbar");
    },
    posBrowseToolbarSecondary() {
      if (this.posBrowseStep === "subs" && this.posSelectedRoot) {
        return this.posSelectedRoot.name || "";
      }
      if (this.posBrowseStep !== "products") {
        return "";
      }
      if (this.posSelectedSub) {
        return tagDisplayName(this.posSelectedSub, this.tags);
      }
      if (this.posSelectedRoot) {
        return this.posSelectedRoot.name || "";
      }
      if (this.search.info) {
        return this.search.info;
      }
      return this.$t("all");
    },
    formattedNumber() {
      return this.finalOrderTotal.toLocaleString();
    },
    lineNoteCartItemName() {
      const index = this.lineNoteCartIndex;
      if (index == null || !this.carditems[index]) return "";
      return this.carditems[index].name || "";
    },
    orderDiscountAmount() {
      const subTotal = Number(this.totaPrice) || 0;
      const value = Number(this.orderDiscountValue);
      if (subTotal <= 0 || !Number.isFinite(value) || value <= 0) {
        return 0;
      }

      if (this.orderDiscountType === "percentage") {
        const clampedPercent = Math.min(Math.max(value, 0), 100);
        return Math.min((subTotal * clampedPercent) / 100, subTotal);
      }

      return Math.min(value, subTotal);
    },
    finalOrderTotal() {
      const subTotal = Number(this.totaPrice) || 0;
      const discount = Number(this.orderDiscountAmount) || 0;
      return Math.max(subTotal - discount, 0);
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
    uniqueZones() {
      if (!Array.isArray(this.allTables)) {
        return [];
      }
      const zones = this.allTables
        .map(table => table.zone)
        .filter(zone => zone && zone.trim() !== '');
      return [...new Set(zones)].sort();
    },
    posFloorCanvasBgStyle() {
      const img = this.posFloorPlanSettings && this.posFloorPlanSettings.floorPlanImageUrl;
      if (img) {
        return {
          backgroundImage: `url("${img}")`,
          backgroundSize: "contain",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
          backgroundColor: this.posFloorPlanBackgroundColor || "#f1f5f9",
        };
      }
      return {
        backgroundColor: this.posFloorPlanBackgroundColor || "#f1f5f9",
      };
    },
    posFloorTableChipVarsStyle() {
      const px = this.clampPosTableChipSize(this.posFloorTableChipSizePx);
      return {
        "--floor-table-chip-size": `${px}px`,
        "--floor-table-chip-font": `${Math.max(11, Math.round(px * 0.32))}px`,
      };
    },
    posFloorPlanKeysForTabs() {
      return this.posFloorPlanAvailableKeys.filter((k) => String(k ?? "").trim() !== "");
    },
    posFloorPlanKeysForTabsSorted() {
      return [...this.posFloorPlanKeysForTabs].sort((a, b) =>
        String(a ?? "").localeCompare(String(b ?? ""), undefined, {
          numeric: true,
          sensitivity: "base",
        })
      );
    },
    posFloorPlanPlacedTables() {
      if (!Array.isArray(this.allTables)) return [];
      const pk = (this.posFloorPlanSelectedKey ?? "").trim();
      return this.allTables.filter((t) => {
        const id = String(t.id);
        if (this.posFloorPlanPositions[id] == null) return false;
        const z = (t.zone ?? "").trim();
        if (pk === "") return z === "";
        return z === pk;
      });
    },
    filteredTables() {
      if (!Array.isArray(this.allTables)) {
        return [];
      }
      let filtered = [...this.allTables];
      
      // Filter by zone
      if (this.tableFilters.zone) {
        filtered = filtered.filter(table => table.zone === this.tableFilters.zone);
      }
      
      // Filter by table number
      if (this.tableFilters.tableNumber) {
        const searchNumber = parseInt(this.tableFilters.tableNumber);
        if (!isNaN(searchNumber)) {
          filtered = filtered.filter(table => {
            const tableNumber = parseInt(table.tableNumber);
            return !isNaN(tableNumber) && tableNumber === searchNumber;
          });
        }
      }
      
      // Filter by status
      if (this.tableFilters.status) {
        filtered = filtered.filter(table => table.status === this.tableFilters.status);
      }
      
      // Sort by table number
      return filtered.sort((a, b) => a.tableNumber - b.tableNumber);
    },
    tablesGroupedByZone() {
      const map = new Map();
      for (const table of this.filteredTables) {
        const z =
          table.zone && String(table.zone).trim() !== ""
            ? String(table.zone).trim()
            : "";
        if (!map.has(z)) map.set(z, []);
        map.get(z).push(table);
      }
      const noZoneLabel = this.$t("noZone") || "بدون موقع";
      const sortedEntries = [...map.entries()].sort((a, b) => {
        if (a[0] === "") return 1;
        if (b[0] === "") return -1;
        return a[0].localeCompare(b[0], "ar");
      });
      return sortedEntries.map(([zone, tables]) => ({
        zoneKey: zone || "__empty",
        zoneLabel: zone || noZoneLabel,
        tables: [...tables].sort((a, b) =>
          String(a.tableNumber).localeCompare(String(b.tableNumber), undefined, {
            numeric: true,
          })
        ),
      }));
    },
    selectedTableSummary() {
      if (!this.selectedTableId) {
        return this.$t("noTableSelected") || "لم يتم اختيار طاولة";
      }
      if (this.mergedTableIds.length > 1) {
        return this.mergedTableIds
          .map((id) => this.allTables.find((t) => t.id === id)?.tableNumber)
          .filter(Boolean)
          .join(" + ");
      }
      return this.selectedTable?.tableNumber ?? "";
    },
    hasDepartmentPrinters() {
      return (this.tagPrinters || []).length > 0;
    },
    mainPrinter() {
      return (this.managedPrinters || []).find(
        (p) => (p.isMain ?? p.IsMain) && (p.isActive ?? p.IsActive) !== false
      ) || null;
    },
    
    mergedTableIds() {
      // Get all merged table IDs for the currently selected table
      if (!this.selectedTableId) {
        return [];
      }
      
      const selectedTable = this.allTables.find(t => t.id === this.selectedTableId);
      if (!selectedTable || !selectedTable.currentOrderId) {
        return [this.selectedTableId];
      }
      
      // Find all tables with the same currentOrderId
      return this.allTables
        .filter(t => t.currentOrderId === selectedTable.currentOrderId && !t.IsDeleted)
        .map(t => t.id);
    },
    selectedTable() {
      if (!this.selectedTableId) {
        return null;
      }
      return this.allTables.find(t => t.id === this.selectedTableId);
    },
    orderMoveTitle() {
      if (this.orderMove.mode === "full") {
        return this.$t("transferFullOrder") || "نقل الطلب كامل";
      }
      if (this.orderMove.mode === "merge") {
        return this.$t("mergeTwoInvoices") || "دمج فاتورتين";
      }
      return this.$t("transferOneItem") || "نقل عنصر";
    },
    orderMoveMessage() {
      if (this.orderMove.mode === "full") {
        return this.$t("transferFullOrderMessage") || "اختر طاولة المصدر ثم الهدف لنقل الطلب بالكامل.";
      }
      if (this.orderMove.mode === "merge") {
        return this.$t("mergeTwoInvoicesMessage") || "اختر طاولة المصدر (فاتورة ستُدمج) ثم الطاولة الهدف (فاتورة رئيسية).";
      }
      return this.$t("transferOneItemMessage") || "اختر عنصر من المصدر ثم الطاولة الهدف.";
    },
    orderMoveConfirmLabel() {
      if (this.orderMove.submitting) {
        return this.$t("processing") || "جاري التنفيذ...";
      }
      if (this.orderMove.mode === "full") {
        return this.$t("confirmFullTransfer") || "تأكيد نقل الطلب";
      }
      if (this.orderMove.mode === "merge") {
        return this.$t("confirmMergeInvoices") || "تأكيد الدمج";
      }
      return this.$t("confirmItemTransfer") || "تأكيد نقل العنصر";
    },
    orderMoveSourceTables() {
      if (!Array.isArray(this.allTables)) return [];
      let list = this.allTables.filter((t) => t.status === "Occupied");
      const z = (this.orderMove.sourceZoneFilter ?? "").trim();
      if (z) {
        list = list.filter((t) => (t.zone && String(t.zone).trim()) === z);
      }
      return list.sort((a, b) => Number(a.tableNumber) - Number(b.tableNumber));
    },
    orderMoveDestinationTables() {
      if (!Array.isArray(this.allTables)) return [];
      let list = this.allTables.filter(
        (t) =>
          (t.status === "Available" || t.status === "Occupied") &&
          t.id !== this.orderMove.sourceTableId
      );
      const z = (this.orderMove.destinationZoneFilter ?? "").trim();
      if (z) {
        list = list.filter((t) => (t.zone && String(t.zone).trim()) === z);
      }
      return list.sort((a, b) => Number(a.tableNumber) - Number(b.tableNumber));
    },
    orderMoveCanConfirm() {
      if (!this.orderMove.sourceTableId || !this.orderMove.destinationTableId) {
        return false;
      }
      if (this.orderMove.sourceTableId === this.orderMove.destinationTableId) {
        return false;
      }
      if (this.orderMove.mode === "item" && !this.orderMove.sourceOrderItemId) {
        return false;
      }
      if (this.orderMove.mode === "item") {
        if (!this.orderMove.transferQuantity || this.orderMove.transferQuantity <= 0) {
          return false;
        }
        if (this.orderMove.transferQuantity > this.orderMoveSelectedItemMaxQuantity) {
          return false;
        }
      }
      return true;
    },
    orderMoveSelectedItem() {
      if (!this.orderMove.sourceOrderItemId) return null;
      return this.orderMove.sourceItems.find((i) => i.sourceOrderItemId === this.orderMove.sourceOrderItemId) || null;
    },
    orderMoveSelectedItemMaxQuantity() {
      return this.orderMoveSelectedItem?.quantity || 1;
    },
    sensitiveActionLabel() {
      const key = this.sensitiveActionAuth.actionKey;
      const labels = {
        transfer_item: this.$t("sensitiveActionTransferItem") || "نقل عنصر",
        transfer_full: this.$t("sensitiveActionTransferFullOrder") || "نقل الطلب كامل",
        merge_invoices: this.$t("sensitiveActionMergeInvoices") || "دمج فاتورتين",
        order_discount: this.$t("sensitiveActionOrderDiscount") || "تطبيق الخصم",
        cancel_order: this.$t("sensitiveActionCancelOrder") || "إلغاء الطلب",
      };
      return labels[key] || (this.$t("sensitiveActionGeneral") || "إجراء حساس");
    },
    sensitiveAuthUsesOwnLoginCode() {
      const role = localStorage.getItem("role");
      if (role !== "Manager") return false;
      try {
        const info = JSON.parse(localStorage.getItem("info") || "{}");
        return !!(
          info.canUseOwnLoginCodeForSensitiveActions ||
          info.CanUseOwnLoginCodeForSensitiveActions
        );
      } catch {
        return false;
      }
    },
    sensitiveAuthFieldLabel() {
      if (this.sensitiveAuthUsesOwnLoginCode) {
        return this.$t("sensitiveAuthLoginCodeLabel") || "رمز التأكيد";
      }
      return this.$t("sensitiveAuthPasswordLabel") || "تأكيد الصلاحية";
    },
    sensitiveAuthFieldPlaceholder() {
      if (this.sensitiveAuthUsesOwnLoginCode) {
        return this.$t("enterYourLoginCode") || "أدخل رمز الدخول الخاص بك";
      }
      return this.$t("enterManagerPassword") || "أدخل باسورد المدير";
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
    hasOrderNotes() {
      return this.tableOrders.some(order => order.notes && order.notes.trim().length > 0);
    },
    tableOrdersWithNotes() {
      return this.tableOrders.filter(order => order.notes && order.notes.trim().length > 0);
    },
  },
  watch: {
    selectedTableId: {
      immediate: true,
      handler(id) {
        if (!id) {
          this.clearActiveTableReservation();
          return;
        }
        this.loadActiveReservationForTable(id);
      },
    },
    carditems: {
      handler() {
        this.totaPrice = 0;
        this.carditems.forEach((item) => {
          // Ensure prices are valid numbers
          const price = item.price || 0;
          const disCountPrice = item.disCountPrice || 0;
          const finalPrice = (disCountPrice > 0 && disCountPrice !== price) ? disCountPrice : price;
          
          // Ensure total is calculated if missing
          if (item.total === undefined || isNaN(item.total) || item.total === null) {
            item.total = finalPrice * (item.quantity || 1);
          }
          
          // Ensure price and disCountPrice are set correctly
          if (!item.price || item.price === null || item.price === undefined) {
            item.price = price;
          }
          if (item.disCountPrice === null || item.disCountPrice === undefined) {
            item.disCountPrice = disCountPrice;
          }
          
          this.totaPrice += item.total || 0;
        });
        this.totalCardItems = this.carditems.length;
      },
      deep: true,
    },
    "orderForSend.orderType": {
      handler(newType) {
        // Clear delivery fields when order type changes away from Delivery
        if (newType !== 'Delivery') {
          this.orderForSend.deliveryDriverId = null;
          this.orderForSend.deliveryStatus = null;
          this.orderForSend.deliveryAddress = "";
          this.orderForSend.deliveryPhoneNumber = "";
          this.orderForSend.deliveryCustomerName = "";
          this.orderForSend.deliveryFee = null;
          this.orderForSend.newDriverName = "";
          this.orderForSend.newDriverPhone = "";
          this.orderForSend.newDriverAddress = "";
          this.orderForSend.newDriverVehicleType = "";
          this.orderForSend.newDriverVehicleNumber = "";
          this.useExistingDriver = true;
          this.useExistingCustomer = true;
          this.selectedDeliveryCustomerId = "";
          this.showAddCustomerModal = false;
          this.$bvModal.hide('modal-delivery-info');
        } else {
          // Set default delivery status when switching to Delivery
          if (!this.orderForSend.deliveryStatus) {
            this.orderForSend.deliveryStatus = "Pending";
          }
          this.loadDeliveryCustomers();
          this.$nextTick(() => {
            this.$bvModal.show('modal-delivery-info');
          });
        }
      }
    },
    posMobileCartOpen(val) {
      if (typeof document === "undefined") return;
      const isNarrowViewport =
        typeof window !== "undefined" &&
        window.matchMedia("(max-width: 1200px)").matches;
      document.body.style.overflow = val && isNarrowViewport ? "hidden" : "";
    },

    useExistingCustomer(val) {
      if (val === false) {
        this.selectedDeliveryCustomerId = "";
      }
    },

  },

  mounted() {
    try {
      this.getTags();
      this.initPosFloorPlanGate();
      
      const userInfoStr = localStorage.getItem("info");
      if (userInfoStr) {
        this.userInfo = JSON.parse(userInfoStr);
      }

      // Load commercial user info for printing
      this.loadCommercialUserInfo();

      // Initialize printers on mount
      this.initializePrinters();
      
      // Load tag printers for tag-based printing
      this.loadTagPrinters();
      
      // Load managed printers to get main printer
      this.loadManagedPrinters();
      
      // Load delivery drivers & customers (delivery)
      this.loadDeliveryDrivers();
      this.loadDeliveryCustomers();
      
      // Add keyboard shortcut listener
      this.handleKeyup = (e) => {
        if (e.ctrlKey && e.keyCode === 38) {
          this.$root.$emit("bv::toggle::collapse", "sidebar-right");
        }
      };
      window.addEventListener("keyup", this.handleKeyup);

      this.posMobileCartEscape = (e) => {
        if (e.key === "Escape" && this.posMobileCartOpen) {
          this.closePosMobileCart();
        }
      };
      window.addEventListener("keydown", this.posMobileCartEscape);

      // Initialize SignalR for real-time updates
      this.initializeSignalR();
    } catch (error) {
      this.$toast.error(this.$i18n.t("error") || "An error occurred", {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    }
  },
  
  beforeDestroy() {
    if (typeof document !== "undefined") {
      document.body.style.overflow = "";
    }
    // Cleanup: Remove event listener
    if (this.handleKeyup) {
      window.removeEventListener("keyup", this.handleKeyup);
    }
    if (this.posMobileCartEscape) {
      window.removeEventListener("keydown", this.posMobileCartEscape);
    }
    // Cleanup: Stop SignalR connection
    this.cleanupSignalR();
  },

  methods: {
    applyOrderDiscountPreset(preset) {
      if (!preset) return;
      this.orderDiscountType = preset.type;
      this.orderDiscountValue = preset.value;
    },
    clearOrderDiscount() {
      this.orderDiscountType = "amount";
      this.orderDiscountValue = null;
      this.orderForSend.discountType = null;
      this.orderForSend.discountValue = null;
      this.orderForSend.discountAmount = 0;
      this.orderForSend.discountPercent = 0;
      this.orderForSend.orderSubTotal = 0;
      this.orderForSend.orderTotalAfterDiscount = 0;
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
    openDeliveryInfoModal() {
      this.loadDeliveryCustomers();
      this.$bvModal.show('modal-delivery-info');
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
    loadCommercialUserInfo() {
      HTTP.get("Admin/CommercialUserInfo")
        .then((response) => {
          if (response.data && response.data.data) {
            this.commercialUserInfo = {
              restaurantName: response.data.data.restaurantName || '',
              logo: response.data.data.logo || null,
              address: response.data.data.address || '',
              phone: response.data.data.phone || ''
            };
          }
        })
        .catch((error) => {
          console.error('Error loading commercial user info:', error);
          // Use defaults if error occurs
          this.commercialUserInfo = {
            restaurantName: '',
            logo: null,
            address: '',
            phone: ''
          };
        });
    },
    async getTags(showErrorToast = true) {
      try {
        const response = await HTTP.get(
          `Admin/GetTags?pageNumber=0&pageSize=10000`
        );
        this.tags = response.data?.data?.items || [];
      } catch (error) {
        console.error("Error loading tags:", error);
        if (showErrorToast) {
          this.$toast.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
        }
      }
    },
    async loadTagPrinters() {
      try {
        const response = await HTTP.get('TagPrinters');
        if (response.data && !response.data.errorStatus) {
          this.tagPrinters = response.data.data || [];
        } else {
          this.tagPrinters = [];
        }
      } catch (error) {
        console.error('Error loading tag printers:', error);
        this.tagPrinters = [];
      }
    },
    async loadManagedPrinters() {
      try {
        const response = await HTTP.get('Printers');
        if (response.data && !response.data.errorStatus) {
          this.managedPrinters = response.data.data || [];
        } else {
          this.managedPrinters = [];
        }
      } catch (error) {
        console.error('Error loading managed printers:', error);
        this.managedPrinters = [];
      }
    },
    async loadDeliveryDrivers() {
      try {
        this.loadingDeliveryDrivers = true;
        const response = await HTTP.get('DeliveryDrivers');
        if (response.data && !response.data.errorStatus) {
          this.deliveryDrivers = response.data.data || [];
        } else {
          this.deliveryDrivers = [];
        }
      } catch (error) {
        console.error('Error loading delivery drivers:', error);
        this.deliveryDrivers = [];
      } finally {
        this.loadingDeliveryDrivers = false;
      }
    },
    async loadDeliveryCustomers() {
      try {
        this.loadingDeliveryCustomers = true;
        const response = await HTTP.get("Customers");
        if (response.data && !response.data.errorStatus) {
          this.deliveryCustomers = response.data.data || [];
        } else {
          this.deliveryCustomers = [];
        }
      } catch (error) {
        console.error("Error loading customers:", error);
        this.deliveryCustomers = [];
      } finally {
        this.loadingDeliveryCustomers = false;
      }
    },
    setPosPaymentMethod(method) {
      this.orderForSend.paymentMethod = method;
      if (method !== "Credit") {
        this.orderForSend.creditEmployeeId = null;
        this.orderForSend.creditCustomerId = null;
      }
    },
    validateCreditForOrder(toastPosition) {
      if (this.orderForSend.paymentMethod !== "Credit") return true;
      const e = this.orderForSend.creditEmployeeId;
      const c = this.orderForSend.creditCustomerId;
      const hasE = e != null && e !== "";
      const hasC = c != null && c !== "";
      if ((hasE && !hasC) || (!hasE && hasC)) return true;
      this.$toast.error(
        this.$i18n.t("pleaseSelectCreditAccount") || "اختر حساباً للدفع الآجل",
        {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        }
      );
      return false;
    },
    applySelectedDeliveryCustomer() {
      const id = this.selectedDeliveryCustomerId;
      if (id === "" || id === null || id === undefined) {
        return;
      }
      const numId = Number(id);
      const c = this.deliveryCustomers.find((x) => Number(x.id) === numId);
      if (c) {
        this.orderForSend.deliveryCustomerName = c.name || "";
        this.orderForSend.deliveryPhoneNumber = c.phoneNumber || "";
        this.orderForSend.deliveryAddress = c.address || "";
      }
    },
    async saveNewCustomerFromDelivery() {
      if (!this.newCustomerForm.name || !this.newCustomerForm.name.trim()) {
        this.$toast.error(this.$i18n.t("pleaseEnterCustomerName") || "يرجى إدخال اسم العميل", {
          position: "top-right",
          timeout: 2500,
          rtl: this.$i18n.locale === "ar",
        });
        return;
      }
      if (!this.newCustomerForm.phoneNumber || !this.newCustomerForm.phoneNumber.trim()) {
        this.$toast.error(this.$i18n.t("pleaseEnterPhoneNumber") || "يرجى إدخال رقم الهاتف", {
          position: "top-right",
          timeout: 2500,
          rtl: this.$i18n.locale === "ar",
        });
        return;
      }
      try {
        this.savingDeliveryCustomer = true;
        const response = await HTTP.post("Customers", {
          name: this.newCustomerForm.name.trim(),
          phoneNumber: this.newCustomerForm.phoneNumber.trim(),
          address: this.newCustomerForm.address ? this.newCustomerForm.address.trim() : null,
          notes: this.newCustomerForm.notes ? this.newCustomerForm.notes.trim() : null,
          isActive: true,
        });
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("customerAddedSuccess") || "تم إضافة العميل بنجاح", {
            position: "top-right",
            timeout: 2500,
            rtl: this.$i18n.locale === "ar",
          });
          await this.loadDeliveryCustomers();
          const newId = response.data.data && response.data.data.id;
          if (newId) {
            this.selectedDeliveryCustomerId = newId;
            this.applySelectedDeliveryCustomer();
            this.useExistingCustomer = true;
          }
          this.showAddCustomerModal = false;
          this.resetNewCustomerForm();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("customerSaveFailed") || "فشل حفظ العميل", {
            position: "top-right",
            timeout: 2500,
            rtl: this.$i18n.locale === "ar",
          });
        }
      } catch (error) {
        console.error("Error saving customer from POS:", error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("customerSaveFailed") || "حدث خطأ", {
          position: "top-right",
          timeout: 2500,
          rtl: this.$i18n.locale === "ar",
        });
      } finally {
        this.savingDeliveryCustomer = false;
      }
    },
    resetNewCustomerForm() {
      this.newCustomerForm = {
        name: "",
        phoneNumber: "",
        address: "",
        notes: "",
      };
    },
    async saveNewDriver() {
      try {
        if (!this.newDriverForm.name || !this.newDriverForm.name.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterDriverName") || "يرجى إدخال اسم السائق", {
            position: "top-right",
            timeout: 2500,
            rtl: this.$i18n.locale === 'ar'
          });
          return;
        }
        if (!this.newDriverForm.phoneNumber || !this.newDriverForm.phoneNumber.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterDriverPhone") || "يرجى إدخال رقم هاتف السائق", {
            position: "top-right",
            timeout: 2500,
            rtl: this.$i18n.locale === 'ar'
          });
          return;
        }

        this.savingDriver = true;
        const response = await HTTP.post('DeliveryDrivers', {
          name: this.newDriverForm.name.trim(),
          phoneNumber: this.newDriverForm.phoneNumber.trim(),
          address: this.newDriverForm.address ? this.newDriverForm.address.trim() : null,
          vehicleType: this.newDriverForm.vehicleType ? this.newDriverForm.vehicleType.trim() : null,
          vehicleNumber: this.newDriverForm.vehicleNumber ? this.newDriverForm.vehicleNumber.trim() : null,
          isActive: true
        });

        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("driverAddedSuccess") || "تم إضافة السائق بنجاح", {
            position: "top-right",
            timeout: 2500,
            rtl: this.$i18n.locale === 'ar'
          });
          
          // Reload drivers list
          await this.loadDeliveryDrivers();
          
          // Select the newly added driver
          if (response.data.data && response.data.data.id) {
            this.orderForSend.deliveryDriverId = response.data.data.id;
            this.useExistingDriver = true;
          }
          
          // Close modal and reset form
          this.showAddDriverModal = false;
          this.resetNewDriverForm();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("driverAddFailed") || "فشل إضافة السائق", {
            position: "top-right",
            timeout: 2500,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error saving new driver:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("driverAddError") || "حدث خطأ أثناء إضافة السائق", {
          position: "top-right",
          timeout: 2500,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingDriver = false;
      }
    },
    resetNewDriverForm() {
      this.newDriverForm = {
        name: '',
        phoneNumber: '',
        address: '',
        vehicleType: '',
        vehicleNumber: ''
      };
    },
    getTables() {
      // الـ API الافتراضي pageSize=10 فيخفي طاولات على المخطط/القوائم رغم وجود مواضع لها
      return HTTP.get("Tables", { params: { pageNumber: 0, pageSize: 500 } })
        .then((response) => {
          const data = response.data.data.items;
          this.allTables = Array.isArray(data) ? data : [];
          this.availableTables = Array.isArray(this.allTables)
            ? this.allTables.filter((t) => t.status === "Available" || t.status === "Occupied")
            : [];
        })
        .catch((error) => {
          console.error("Error loading tables:", error);
          // Set to empty array on error to prevent further issues
          this.allTables = [];
          this.availableTables = [];
        });
    },
    initPosFloorPlanGate() {
      try {
        this.resetPosFloorPlanGateTools();
        this.posFloorPlanForceDefaultTab = true;
        this.posFloorPlanGateVisible = true;
        this.reconcileReservationTables()
          .finally(() => this.getTables())
          .finally(() => this.loadPosFloorPlan());
      } catch (_) {
        this.posFloorPlanGateVisible = false;
      }
    },
    async reconcileReservationTables() {
      try {
        await HTTP.post("Reservations/reconcile-tables");
      } catch (e) {
        console.warn("reconcileReservationTables", e);
      }
    },
    async loadPosFloorPlan() {
      this.posFloorPlanLoading = true;
      try {
        const res = await HTTP.get("Tables/floor-plan", {
          params: { planKey: this.posFloorPlanSelectedKey },
        });
        const payload = res.data?.data || res.data?.Data || {};
        const keys = payload.availablePlanKeys || [];
        this.posFloorPlanAvailableKeys = keys.length ? keys : [""];
        if (this.posFloorPlanForceDefaultTab) {
          const firstVisibleTab = this.posFloorPlanAvailableKeys.find(
            (k) => String(k ?? "").trim() !== ""
          );
          const defaultTab = firstVisibleTab ?? (this.posFloorPlanAvailableKeys[0] ?? "");
          this.posFloorPlanForceDefaultTab = false;
          if (defaultTab !== this.posFloorPlanSelectedKey) {
            this.posFloorPlanSelectedKey = defaultTab;
            return this.loadPosFloorPlan();
          }
        }
        if (!this.posFloorPlanAvailableKeys.includes(this.posFloorPlanSelectedKey)) {
          this.posFloorPlanSelectedKey = this.posFloorPlanAvailableKeys[0] ?? "";
          return this.loadPosFloorPlan();
        }
        this.posFloorPlanSettings = payload.settings || null;
        if (this.posFloorPlanSettings && this.posFloorPlanSettings.backgroundColor) {
          this.posFloorPlanBackgroundColor = this.posFloorPlanSettings.backgroundColor;
        }
        const rawChip =
          (this.posFloorPlanSettings &&
            (this.posFloorPlanSettings.tableChipSizePx ?? this.posFloorPlanSettings.TableChipSizePx)) ??
          null;
        this.posFloorTableChipSizePx = rawChip != null ? this.clampPosTableChipSize(rawChip) : 56;
        this.posFloorPlanZoneRects = [];
        const zj = this.posFloorPlanSettings && this.posFloorPlanSettings.zonesJson;
        if (zj) {
          try {
            const parsed = JSON.parse(zj);
            if (Array.isArray(parsed)) this.posFloorPlanZoneRects = parsed;
          } catch (_) {}
        }
        const rawTables = payload.tables || [];
        const next = {};
        rawTables.forEach((t) => {
          const id = String(t.id ?? t.Id);
          const lx = t.layoutPosX ?? t.LayoutPosX;
          const ly = t.layoutPosY ?? t.LayoutPosY;
          if (lx != null && ly != null) {
            next[id] = { x: Number(lx), y: Number(ly) };
          }
        });
        this.posFloorPlanPositions = resolveFloorPlanOverlaps(next, this.posFloorTableChipSizePx);
      } catch (e) {
        console.error("loadPosFloorPlan", e);
      } finally {
        this.posFloorPlanLoading = false;
      }
    },
    selectPosFloorPlanKey(k) {
      if (k === this.posFloorPlanSelectedKey) return;
      this.posFloorPlanSelectedKey = k;
      this.loadPosFloorPlan();
    },
    skipPosFloorPlanGate() {
      this.resetPosFloorPlanGateTools();
      this.posFloorPlanForceDefaultTab = false;
      this.posFloorPlanGateVisible = false;
      this.resetOrderSession({
        orderType: "Takeaway",
        silent: true,
      });
    },
    resetPosFloorPlanGateTools() {
      // merge/transfer tools removed
    },
    clampPosTableChipSize(v) {
      const n = Number(v);
      if (!Number.isFinite(n)) return 56;
      return Math.round(Math.max(32, Math.min(96, n)));
    },
    posFloorChipStyle(id) {
      const p = this.posFloorPlanPositions[String(id)];
      if (!p) return {};
      return {
        left: `${p.x * 100}%`,
        top: `${p.y * 100}%`,
      };
    },
    async confirmFloorPlanGuestModal() {
      const table = this.floorPlanGuestModal.table;
      const guestCount = Number(this.floorPlanGuestModal.count || 0);
      if (!table) {
        this.cancelFloorPlanGuestModal();
        return;
      }
      if (!guestCount || guestCount <= 0) {
        this.$toast.error(this.$t("numberOfGuestsRequired") || "يرجى إدخال عدد زبائن صحيح", {
          timeout: 2200,
          maxToasts: 1,
        });
        return;
      }

      this.orderForSend.numberOfGuests = guestCount;
      await this.selectTable(table, null);
      this.posFloorPlanGateVisible = false;
      this.resetPosFloorPlanGateTools();
      this.clearFloorPlanGuestModalState();
    },
    posFloorZoneRectStyle(z) {
      return {
        left: `${z.x * 100}%`,
        top: `${z.y * 100}%`,
        width: `${z.w * 100}%`,
        height: `${z.h * 100}%`,
        borderColor: z.color || "#6366f1",
        backgroundColor: z.color ? `${z.color}33` : "rgba(99,102,241,0.12)",
      };
    },
    async loadMergedTableIds(tableId) {
      // Initialize cache if not exists
      if (!this.mergedTableIdsCache) {
        this.mergedTableIdsCache = {};
      }
      
      // Check cache first
      if (this.mergedTableIdsCache[tableId]) {
        return this.mergedTableIdsCache[tableId];
      }
      
      try {
        const response = await HTTP.get(`Admin/GetMergedTables?tableId=${tableId}`);
        const mergedIds = response.data?.data || [tableId];
        // Ensure mergedIds is an array
        const mergedIdsArray = Array.isArray(mergedIds) ? mergedIds : [tableId];
        this.mergedTableIdsCache[tableId] = mergedIdsArray;
        return mergedIdsArray;
      } catch (error) {
        console.error('Error loading merged table IDs:', error);
        // Fallback to single table
        const fallback = [tableId];
        this.mergedTableIdsCache[tableId] = fallback;
        return fallback;
      }
    },
    async getMergedTableIds(tableId) {
      return await this.loadMergedTableIds(tableId);
    },
    async selectTableInModal(table, event) {
      const multi = event && (event.ctrlKey || event.metaKey);
      await this.selectTable(table, event);
      const { isOccupied, isAvailable } = this.getTableOccupancyFlags(table);
      if (!multi && (isAvailable || isOccupied)) {
        this.showTablesModal = false;
        if (this.posFloorPlanGateVisible) {
          this.posFloorPlanGateVisible = false;
        }
      }
    },
    async confirmCancelDineInOrder() {
      this.$bvModal.hide("modal-cancel-order");
      const canProceed = await this.requestSensitiveActionPassword("cancel_order");
      if (!canProceed) return;
      await this.cancelDineInTableOrderAfterAuth();
    },
    getTableNumberById(tableId) {
      const table = this.allTables.find(t => t.id === tableId);
      return table ? table.tableNumber : '';
    },
    clearTableFilters() {
      this.tableFilters = {
        zone: '',
        tableNumber: '',
        status: ''
      };
    },
    getTableStatusText(status) {
      const statusTexts = {
        Available: this.$t("available") || "متاحة",
        Occupied: this.$t("occupied") || "مشغولة",
        Reserved: this.$t("reserved") || "محجوزة",
        OutOfService: this.$t("outOfService") || "خارج الخدمة"
      };
      return statusTexts[status] || status;
    },
    formatOrderMoveTableOption(table) {
      if (!table) return "";
      const num = table.tableNumber ?? "";
      const statusText = this.getTableStatusText(table.status);
      const zone = table.zone != null ? String(table.zone).trim() : "";
      if (zone) {
        return `${zone} - ${num} - ${statusText}`;
      }
      return `${num} - ${statusText}`;
    },
    formatPrice(price) {
      if (price !== null && price !== undefined && !isNaN(price)) {
        const numPrice = typeof price === 'string' ? parseFloat(price) : price;
        if (!isNaN(numPrice) && numPrice >= 0) {
          return numPrice.toLocaleString("en-EG");
        }
      }
      return "0";
    },
    formatDate(dateString) {
      if (!dateString) return '';
      const date = new Date(dateString);
      return date.toLocaleString('ar-EG', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
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
    getCurrentTime() {
      const now = new Date();
      return now.toLocaleTimeString('ar-IQ', { 
        hour: '2-digit', 
        minute: '2-digit' 
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
        'Credit': this.$t('credit') || 'آجل'
      };
      return methods[method] || method;
    },
    async initializePrinters() {
      // Check if Web Print API is supported (experimental)
      if ('navigator' in window && 'printer' in navigator) {
        this.webPrintAPISupported = true;
        try {
          // Get available printers
          const printers = await navigator.printer.getPrinters();
          this.availablePrinters = printers;
          
          // Try to get saved printer preference
          const savedPrinterId = localStorage.getItem('selectedPrinter');
          if (savedPrinterId) {
            const printer = printers.find(p => p.id === savedPrinterId);
            if (printer) {
              this.selectedPrinter = printer;
              this.selectedPrinterId = printer.id;
            }
          }
          
          // If no saved printer, use default
          if (!this.selectedPrinter && printers.length > 0) {
            const defaultPrinter = printers.find(p => p.isDefault) || printers[0];
            this.selectedPrinter = defaultPrinter;
            this.selectedPrinterId = defaultPrinter.id;
            localStorage.setItem('selectedPrinter', defaultPrinter.id);
          }
        } catch (error) {
          console.warn('Web Print API not fully supported:', error);
          this.webPrintAPISupported = false;
          // Web Print API not available, will use standard print
        }
      } else {
        // Web Print API not supported, use standard print
        this.webPrintAPISupported = false;
        console.log('Web Print API not supported, using standard print dialog');
      }
    },
    onPrinterChange() {
      const printer = this.availablePrinters.find(p => p.id === this.selectedPrinterId);
      if (printer) {
        this.selectedPrinter = printer;
        localStorage.setItem('selectedPrinter', printer.id);
      }
    },
    async printWithWebPrintAPI(printContent, stylesHtml) {
      try {
        // Check if Web Print API is supported
        if (!('navigator' in window && 'printer' in navigator)) {
          throw new Error('Web Print API not supported');
        }

        // Get printer (use selected or default)
        let printer = this.selectedPrinter;
        if (!printer && this.selectedPrinterId) {
          const printers = await navigator.printer.getPrinters();
          printer = printers.find(p => p.id === this.selectedPrinterId);
        }
        
        if (!printer) {
          const printers = await navigator.printer.getPrinters();
          printer = printers.find(p => p.isDefault) || printers[0];
          if (!printer) {
            throw new Error('No printer available');
          }
        }

        // Create print job
        const printJob = await navigator.printer.print({
          printer: printer.id,
          pages: [{
            html: printContent,
            css: stylesHtml
          }]
        });

        // Wait for print job to complete
        await printJob.complete;
        return true;
      } catch (error) {
        console.error('Web Print API error:', error);
        throw error;
      }
    },
    async checkPythonServerHealth() {
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 3000); // 3 seconds timeout for health check
        
        const response = await fetch('http://localhost:5000/health', {
          method: 'GET',
          signal: controller.signal
        });
        
        clearTimeout(timeoutId);
        
        if (response.ok) {
          const health = await response.json();
          return health.status === 'ok' && health.printer?.available;
        }
        return false;
      } catch (error) {
        console.warn('Python print server health check failed:', error);
        return false;
      }
    },
    async printWithPythonServer(itemsToPrint = null, providedPrintData = null) {
      try {
        const printItems = itemsToPrint || this.carditems;
        
        if (!printItems || printItems.length === 0) {
          console.warn('No items to print');
          return;
        }
        
        // Check if Python server is available
        const serverAvailable = await this.checkPythonServerHealth();
        if (!serverAvailable) {
          console.warn('Python print server is not available, skipping...');
          return false; // Return false to fallback to other print methods
        }
        
        // Use provided print data or prepare new one
        let printData = providedPrintData;
        if (!printData) {
          printData = {
            storeName: this.commercialUserInfo.restaurantName || 'متجر المطعم',
            storeAddress: '',
            storePhone: '',
            orderCode: this.orderForSend.orderCode || '',
            date: new Date().toLocaleDateString('ar-EG'),
            time: new Date().toLocaleTimeString('ar-EG'),
            tableNumber: this.selectedTableId ? this.allTables.find(t => t.id === this.selectedTableId)?.tableNumber : null,
            employeeName: this.userInfo.name || '',
            items: printItems.map(item => ({
              name: item.name || '',
              quantity: item.quantity || 0,
              price: Number(item.price ?? 0),
              total: Number(item.total ?? 0),
              discount: item.discount || null
            })),
            subtotal: this.totaPrice.toLocaleString(),
            discount: this.orderDiscountAmount.toLocaleString(),
            tax: '0',
            total: this.finalOrderTotal.toLocaleString(),
            paymentMethod: this.orderForSend.paymentMethod === 'Cash' ? 'نقدي' : 
                          this.orderForSend.paymentMethod === 'Card' ? 'بطاقة' : 
                          this.orderForSend.paymentMethod || 'نقدي'
          };
          
          // Get HTML content with POS receipt styles (matches browser print preview)
          await this.$nextTick();
          const printElement = document.getElementById("print");
          if (printElement) {
            const invoiceTitle = (this.$t("invoice_number") || "فاتورة") + ' - ' + (this.orderForSend.orderCode || '');
            printData.htmlContent = getReceiptHtmlFromElement(printElement, invoiceTitle);
          }
        }
        
        // Send to Python print server with timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), PRINT_SERVER_FETCH_TIMEOUT_MS);
        
        try {
          const response = await fetch('http://localhost:5000/print', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(printData),
            signal: controller.signal
          });
          
          clearTimeout(timeoutId);
          
          if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
          }
          
          const result = await response.json();
          console.log("Print result:", result);  
          
          if (result.success) {
            this.$toast.success(this.$i18n.t("printSuccess") || 'تم الطباعة بنجاح', {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            });
            return true;
          } else {
            throw new Error(result.message || 'فشلت الطباعة');
          }
        } catch (fetchError) {
          clearTimeout(timeoutId);
          
          // Don't show error toast, just return false to allow fallback
          if (fetchError.name === 'AbortError') {
            console.warn('Python print server timeout - falling back to other methods');
          } else if (fetchError.message.includes('Failed to fetch') || fetchError.message.includes('NetworkError')) {
            console.warn('Python print server not available - falling back to other methods');
          } else {
            console.warn('Python print server error - falling back to other methods:', fetchError);
          }
          return false; // Return false to allow fallback to other print methods
        }
      } catch (error) {
        console.warn('Python print server error - falling back to other methods:', error);
        return false; // Return false to allow fallback to other print methods
      }
    },
    groupItemsByTag(items) {
      return groupItemsForDepartmentPrinting(
        items,
        this.tagPrinters,
        this.tags
      );
    },
    async ensurePrintPrintersReady() {
      if (!this.tagPrinters?.length) {
        await this.loadTagPrinters();
      }
      if (!this.managedPrinters?.length) {
        await this.loadManagedPrinters();
      }
      if (!this.tags?.length) {
        await this.getTags(false);
      }
    },
    findPrinterForPrint(printerId) {
      if (printerId == null) return null;
      const id = String(printerId);
      const fromManaged = (this.managedPrinters || []).find(
        (p) => String(p.id ?? p.Id) === id
      );
      if (fromManaged) return fromManaged;
      const link = (this.tagPrinters || []).find((tp) => {
        const pid =
          tp.printer?.id ??
          tp.printer?.Id ??
          tp.printerId ??
          tp.PrinterId;
        return String(pid) === id;
      });
      return link?.printer ?? link?.Printer ?? null;
    },
    ensureOrderCodeForPrint() {
      const existing = String(this.orderForSend?.orderCode || "").trim();
      if (existing && existing !== "---") {
        return existing;
      }
      const activeOrder = Array.isArray(this.tableOrders) ? this.tableOrders[0] : null;
      const fromOrder = activeOrder?.orderCode ?? activeOrder?.OrderCode ?? null;
      if (fromOrder) {
        this.orderForSend.orderCode = String(fromOrder);
        return this.orderForSend.orderCode;
      }
      this.orderForSend.orderCode = Math.floor(
        Math.random() * 1000000000
      )
        .toString()
        .padStart(9, "0");
      return this.orderForSend.orderCode;
    },
    async generateHTMLForItems(items, tagName = null, options = {}) {
      const hidePrices = !!(options && options.hidePrices);
      const orderCode = this.ensureOrderCodeForPrint();
      const { groupDiscount, groupTotal, totalItems } = computeGroupPrintTotals(
        items,
        this.totaPrice,
        this.orderDiscountAmount
      );
      const currency = this.$t("currency") || "د.ع";
      const discountLabel = this.$t("discountLabel") || "الخصم";
      const receiptLabels = {
        itemName: this.$t("item_name_label") || "طبق/مشروب",
        quantity: this.$t("quantity_label") || "العدد",
        price: this.$t("selling_price_label") || "السعر",
        total: this.$t("total_label") || "المجموع",
        countLabel: "العدد:",
        countSuffix: " طبق/مشروب",
        sectionLabel: "القسم:",
        discountLabel,
        totalLabel: `${this.$t("total") || "المجموع"}:`,
        currency,
      };

      const savedCarditems = this.carditems;
      this.carditems = items;
      await this.$nextTick();

      const printElement = document.getElementById("print");
      if (!printElement) {
        this.carditems = savedCarditems;
        await this.$nextTick();
        return "";
      }

      let htmlContent = printElement.innerHTML;

      htmlContent = ensurePrintTableNumberInHtml(
        htmlContent,
        this.selectedTableId ? this.selectedTableSummary : "",
        this.$t("tableNumber") || "رقم الطاولة",
        (t) => this.escapeHtml(t)
      );
      htmlContent = ensurePrintOrderCodeInHtml(
        htmlContent,
        orderCode,
        (t) => this.escapeHtml(t)
      );

      const itemsTableHTML = buildReceiptItemsTableHtml({
        items,
        labels: receiptLabels,
        escapeHtml: (t) => this.escapeHtml(t),
        formatPrice: (n) => this.formatPrice(n),
        hidePrices,
      });

      const tableRegex = /<table[^>]*class="bill-items-table"[^>]*>[\s\S]*?<\/table>/i;
      htmlContent = htmlContent.replace(tableRegex, itemsTableHTML);

      const summaryHTML = buildReceiptSummaryHtml({
        totalItems,
        tagName,
        hidePrices,
        groupDiscount,
        groupTotal,
        labels: receiptLabels,
        formatPrice: (n) => this.formatPrice(n),
        escapeHtml: (t) => this.escapeHtml(t),
      });
      htmlContent = replaceReceiptSummarySection(htmlContent, summaryHTML);
      if (hidePrices) {
        htmlContent = stripKitchenFinancialFromReceiptHtml(htmlContent);
      }
      
      // Add notes and pager number section before footer
      const notesSectionHTML = `
        <div data-v-f8758d62="" class="bill-notes-section" ${!this.orderForSend.notes && !this.orderForSend.pagerNumber ? 'style="display:none;"' : ''}>
          <div data-v-f8758d62="" class="bill-divider"></div>
          ${this.orderForSend.notes ? `
          <div data-v-f8758d62="" class="bill-notes-content">
            <div data-v-f8758d62="" class="bill-notes-label">${this.$t("notes") || "ملاحظات"}:</div>
            <div data-v-f8758d62="" class="bill-notes-text">${this.escapeHtml(this.orderForSend.notes)}</div>
          </div>
          ` : ''}
          ${this.orderForSend.pagerNumber ? `
          <div data-v-f8758d62="" class="bill-notes-content">
            <div data-v-f8758d62="" class="bill-notes-label">${this.$t("pagerNumber") || "رقم جهاز النداء"}:</div>
            <div data-v-f8758d62="" class="bill-notes-text">${this.escapeHtml(this.orderForSend.pagerNumber)}</div>
          </div>
          ` : ''}
        </div>
      `;
      
      // Insert notes section before footer
      const footerRegex = /<div[^>]*class="bill-footer"[^>]*>/i;
      if (footerRegex.test(htmlContent)) {
        htmlContent = htmlContent.replace(footerRegex, notesSectionHTML + '\n          $&');
      } else {
        // If footer not found, append before closing bill-container
        htmlContent = htmlContent.replace(/<\/div>\s*<\/div>\s*<\/div>\s*<\/div>\s*$/i, notesSectionHTML + '\n        $&');
      }
      
      // Also update pager number in bill-info-section if exists
      const pagerInfoRegex = /<div[^>]*class="bill-info-row"[^>]*>[\s\S]*?رقم جهاز النداء[\s\S]*?<\/div>/i;
      if (this.orderForSend.pagerNumber && !pagerInfoRegex.test(htmlContent)) {
        // Add pager number to info section
        const infoSectionRegex = /(<div[^>]*class="bill-info-section"[^>]*>[\s\S]*?)(<div[^>]*class="bill-info-row"[^>]*>[\s\S]*?from_date[\s\S]*?<\/div>)/i;
        if (infoSectionRegex.test(htmlContent)) {
          htmlContent = htmlContent.replace(infoSectionRegex, `$1
          <div data-v-f8758d62="" class="bill-info-row">
            <span data-v-f8758d62="" class="bill-info-label">${this.$t("pagerNumber") || "رقم جهاز النداء"}:</span>
            <span data-v-f8758d62="" class="bill-info-value">${this.escapeHtml(this.orderForSend.pagerNumber)}</span>
          </div>
          $2`);
        }
      }
      
      const invoiceTitle = (this.$t("invoice_number") || "فاتورة") + ' - ' + (this.orderForSend.orderCode || tagName || '');
      const doc = buildReceiptPrintDocument(htmlContent, invoiceTitle);
      this.carditems = savedCarditems;
      await this.$nextTick();
      return doc;
    },
    escapeHtml(text) {
      const div = document.createElement('div');
      div.textContent = text;
      return div.innerHTML;
    },
    async printItemsByTag(tagName, items, printerId) {
      try {
        const printer = this.findPrinterForPrint(printerId);
        const printerName = printer
          ? printer.printerName ?? printer.PrinterName
          : null;
        const printerType = printer
          ? printer.printerType ?? printer.PrinterType ?? "windows"
          : "windows";
        
        // Prepare print data for this group
        const printData = {
          storeName: this.commercialUserInfo.restaurantName || 'متجر المطعم',
          storeAddress: '',
          storePhone: '',
          orderCode: this.orderForSend.orderCode || '',
          date: new Date().toLocaleDateString('ar-EG'),
          time: new Date().toLocaleTimeString('ar-EG'),
          tableNumber: this.selectedTableId ? this.allTables.find(t => t.id === this.selectedTableId)?.tableNumber : null,
          employeeName: this.userInfo.name || '',
          items: items.map((item) => ({
            name: item.name || "",
            quantity: item.quantity || 0,
          })),
          subtotal: "0",
          discount: "0",
          tax: "0",
          total: "0",
          paymentMethod: this.orderForSend.paymentMethod === 'Cash' ? 'نقدي' : 
                        this.orderForSend.paymentMethod === 'Card' ? 'بطاقة' : 
                        this.orderForSend.paymentMethod || 'نقدi'
        };
        
        const htmlContent = await this.generateHTMLForItems(items, tagName, {
          hidePrices: true,
        });
        printData.htmlContent = htmlContent;

        if (!htmlContent) {
          console.warn(`Empty print HTML for tag "${tagName}"`);
          return false;
        }

        if (printerId) {
          try {
            const response = await HTTP.post(`Printers/${printerId}/print`, {
              htmlContent,
              copies: 1,
            }, { timeout: PRINT_API_TIMEOUT_MS });

            if (response.data && !response.data.errorStatus) {
              console.log(
                `Printed ${tagName} to printer ${printerName || printerId}`
              );
              return true;
            }
            console.warn(
              `Printers/${printerId}/print failed:`,
              response.data?.message
            );
          } catch (error) {
            console.error(`Error printing ${tagName} to printer ${printerId}:`, error);
          }
          if (printerName) {
            printData.printerName = printerName;
            printData.printerType = printerType;
            return await this.printWithPythonServer(items, printData);
          }
          return false;
        }

        return await this.printWithPythonServer(items, printData);
      } catch (error) {
        console.error(`Error in printItemsByTag for ${tagName}:`, error);
        return false;
      }
    },
    async printCard(itemsToPrint = null, printOptions = {}) {
      const raiseOnError = !!(printOptions && printOptions.raiseOnError);
      const departmentPrintersOnly = !!(printOptions && printOptions.departmentPrintersOnly);
      let originalCarditems = null;
      try {
        this.ensureOrderCodeForPrint();
        // Use provided items or fallback to current carditems
        const printItems = itemsToPrint || this.carditems;
        
        // Temporarily replace carditems for printing if needed
        originalCarditems = this.carditems;
        if (itemsToPrint) {
          this.carditems = itemsToPrint;
        }
        
        // Wait for Vue to update the DOM
        await this.$nextTick();
        
        // Get the print content
        const printElement = document.getElementById("print");
        if (!printElement) {
          console.error("Print element not found");
          // Restore original carditems if we changed it
          if (itemsToPrint && originalCarditems !== null) {
            this.carditems = originalCarditems;
          }
          if (raiseOnError) {
            throw new Error("Print element not found");
          }
          return { ok: false, reason: "noPrintElement" };
        }

        const stylesHtml = RECEIPT_PRINT_STYLES_HTML;
        const invoiceTitle = (this.$t("invoice_number") || "فاتورة") + ' - ' + (this.orderForSend.orderCode || 'Invoice');

        // Step 1: Print full receipt to main printer (if exists)
        if (!departmentPrintersOnly && this.mainPrinter) {
          try {
            console.log('Printing full receipt to main printer:', this.mainPrinter.name);
            // Prepare full receipt data
            await this.$nextTick();
            const printElement = document.getElementById("print");
            if (printElement) {
              const fullReceiptHtml = getReceiptHtmlFromElement(printElement, invoiceTitle);
              const fullReceiptData = {
                storeName: this.commercialUserInfo.restaurantName || 'متجر المطعم',
                storeAddress: '',
                storePhone: '',
                orderCode: this.orderForSend.orderCode || '',
                date: new Date().toLocaleDateString('ar-EG'),
                time: new Date().toLocaleTimeString('ar-EG'),
                tableNumber: this.selectedTableId ? this.allTables.find(t => t.id === this.selectedTableId)?.tableNumber : null,
                employeeName: this.userInfo.name || '',
                items: printItems.map(item => ({
                  name: item.name || '',
                  quantity: item.quantity || 0,
                  price: Number(item.price ?? 0),
                  total: Number(item.total ?? 0),
                  discount: item.discount || null
                })),
                subtotal: this.totaPrice.toLocaleString(),
                discount: this.orderDiscountAmount.toLocaleString(),
                tax: '0',
                total: this.finalOrderTotal.toLocaleString(),
                paymentMethod: this.orderForSend.paymentMethod === 'Cash' ? 'نقدي' : 
                              this.orderForSend.paymentMethod === 'Card' ? 'بطاقة' : 
                              this.orderForSend.paymentMethod || 'نقدي',
                htmlContent: fullReceiptHtml,
                printerName: this.mainPrinter.printerName,
                printerType: this.mainPrinter.printerType || 'windows'
              };
              
              // Print to main printer via backend API
              const response = await HTTP.post(`Printers/${this.mainPrinter.id}/print`, {
                htmlContent: fullReceiptHtml,
                copies: 1
              }, { timeout: PRINT_API_TIMEOUT_MS });
              
              if (response.data && !response.data.errorStatus) {
                console.log('Successfully printed full receipt to main printer');
              } else {
                console.warn('Failed to print to main printer via API, trying Python server');
                // Fallback to Python print server
                await this.printWithPythonServer(printItems, fullReceiptData);
              }
            }
          } catch (mainPrinterError) {
            console.warn('Error printing to main printer:', mainPrinterError);
          }
        }
        
        // Step 2: Try tag-based printing for specific items
        try {
          await this.ensurePrintPrintersReady();
          const groupedItems = this.groupItemsByTag(printItems);
          const tagGroups = Object.keys(groupedItems);

          if (tagGroups.length > 0) {
            let anyPrinted = false;
            let hadMappedGroup = false;

            console.debug("[print]", {
              tagPrinters: this.tagPrinters?.length,
              tags: this.tags?.length,
              grouped: Object.fromEntries(
                Object.entries(groupedItems).map(([k, g]) => [
                  k,
                  g.items?.length ?? 0,
                ])
              ),
              sampleTags: (printItems || [])
                .slice(0, 3)
                .map((i) => i.tags),
            });

            for (const groupKey of tagGroups) {
              const group = groupedItems[groupKey];
              if (!group.items.length) continue;
              if (groupKey === "unmapped" || !group.printerId) {
                continue;
              }
              hadMappedGroup = true;
              const ok = await this.printItemsByTag(
                group.tagName,
                group.items,
                group.printerId
              );
              if (ok) anyPrinted = true;
            }

            if (itemsToPrint) {
              this.carditems = originalCarditems;
            }

            if (anyPrinted) {
              this.$toast.success(
                this.$i18n.t("printSuccess") || "تم الطباعة بنجاح",
                { position: "top-right", timeout: 2000, maxToasts: 1 }
              );
              return { ok: true };
            }

            if (hadMappedGroup && !anyPrinted) {
              this.$toast.error(
                this.$i18n.t("error") || "حدث خطأ أثناء الطباعة",
                { position: "top-right", timeout: 3000, maxToasts: 1 }
              );
              return { ok: false, reason: "deptPrintFailed" };
            }

            if (
              this.hasDepartmentPrinters &&
              !this.mainPrinter &&
              !hadMappedGroup
            ) {
              console.warn(
                "[print] No cart items match configured department printers"
              );
              return { ok: false, reason: "allUnmapped" };
            }

            if (!departmentPrintersOnly && this.mainPrinter) {
              return { ok: true };
            }
          }
        } catch (tagPrintError) {
          console.warn("Tag-based printing error, trying fallback methods:", tagPrintError);
        }

        if (departmentPrintersOnly) {
          if (itemsToPrint) {
            this.carditems = originalCarditems;
          }
          return { ok: false, reason: "deptOnlyNoPrint" };
        }

        // Try Python print server as fallback (if available)
        try {
          const pythonPrintSuccess = await this.printWithPythonServer(itemsToPrint);
          if (pythonPrintSuccess) {
            // Restore original carditems if we changed it
            if (itemsToPrint) {
              this.carditems = originalCarditems;
            }
            return; // Success - exit early
          }
        } catch (pythonError) {
          console.warn('Python print server not available, trying other methods:', pythonError);
          // Fall through to other print methods
        }

        // Check if Web Print API is truly supported and printer is selected
        const isWebPrintAPISupported = 'navigator' in window && 
                                       'printer' in navigator && 
                                       typeof navigator.printer !== 'undefined' &&
                                       this.selectedPrinter &&
                                       this.webPrintAPISupported;

        // Try Web Print API (if truly supported)
        if (isWebPrintAPISupported) {
          try {
            const printContent = printElement.innerHTML;
            await this.printWithWebPrintAPI(printContent, stylesHtml);
            // Restore original carditems if we changed it
            if (itemsToPrint) {
              this.carditems = originalCarditems;
            }
            return; // Success - exit early
          } catch (webPrintError) {
            console.warn('Web Print API failed, falling back to standard print:', webPrintError);
            // Fall through to standard print methods
          }
        }

        // Use standard browser print dialog (works in Chrome, Firefox, Edge, etc.)
        // Create a new window for printing
        const printWindow = window.open('', '_blank', 'width=800,height=600');
        if (printWindow) {
          // Build HTML content
          const htmlContent = buildReceiptPrintDocument(printElement.innerHTML, invoiceTitle);
          
          printWindow.document.write(htmlContent);
          printWindow.document.close();
          
          // Wait for content to load, then print
          setTimeout(() => {
            printWindow.focus();
            printWindow.print();
            // Close window after printing
            setTimeout(() => {
              printWindow.close();
              // Restore original carditems if we changed it
              if (itemsToPrint) {
                this.carditems = originalCarditems;
              }
            }, 500);
          }, 500);
        } else {
          // If popup blocked, use fallback method with iframe
          console.warn('Popup blocked, using fallback print method');
          this.fallbackPrint(itemsToPrint);
        }
      } catch (error) {
        console.error('Print card error:', error);
        // Restore original carditems if we changed it
        if (itemsToPrint && originalCarditems !== null) {
          this.carditems = originalCarditems;
        }
        if (raiseOnError) {
          throw error;
        }
        // Else: silently fail (other callers treat printing as optional)
      }
    },
    async fallbackPrint(itemsToPrint = null) {
      // Use provided items or fallback to current carditems
      const printItems = itemsToPrint || this.carditems;
      
      // Temporarily replace carditems for printing if needed
      const originalCarditems = this.carditems;
      if (itemsToPrint) {
        this.carditems = itemsToPrint;
      }
      
      // Wait for Vue to update the DOM
      await this.$nextTick();
      
      // Fallback method using iframe (original method)
      const prtHtml = document.getElementById("print").innerHTML;
      const stylesHtml = RECEIPT_PRINT_STYLES_HTML;

      const content = `
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="UTF-8">
      <title>فاتورة - ${this.orderForSend.orderCode || 'Invoice'}</title>
      ${stylesHtml}
    </head>
    <body>
      ${prtHtml}
    </body>
    </html>
  `;

      const iframe = document.createElement("iframe");
      iframe.style.position = "absolute";
      iframe.style.top = "-10000px";
      iframe.style.width = "80mm";
      iframe.style.height = "1000px";
      document.body.appendChild(iframe);

      const doc = iframe.contentWindow.document;
      doc.open();
      doc.write(content);
      doc.close();

      setTimeout(() => {
        iframe.contentWindow.focus();
        iframe.contentWindow.print();
        
        setTimeout(() => {
          if (document.body.contains(iframe)) {
            document.body.removeChild(iframe);
          }
          // Restore original carditems if we changed it
          if (itemsToPrint) {
            this.carditems = originalCarditems;
          }
        }, 1000);
      }, 250);
    },

    EmptycardList(id) {
      this.resetOrderSession({
        orderType: "Takeaway",
        resetPayment: true,
        silent: true,
      });
      this.$bvModal.hide(id);
    },
    closeModel(id) {
      this.$bvModal.hide(id);
    },
    resolveSensitiveAuthErrorMessage(rawMessage) {
      const msg = rawMessage != null ? String(rawMessage).trim() : "";
      if (msg && this.$te(msg)) return this.$t(msg);
      if (this.sensitiveAuthUsesOwnLoginCode) {
        return this.$t("invalidManagerLoginCode") || "رمز الدخول غير صحيح";
      }
      return this.$t("invalidSensitiveAuth") || this.$t("invalidManagerPassword") || "كلمة المرور غير صحيحة";
    },
    requestSensitiveActionPassword(actionKey) {
      if (this.sensitiveActionAuth.resolver) {
        this.sensitiveActionAuth.resolver(false);
      }
      this.sensitiveActionAuth.actionKey = actionKey || "general";
      this.sensitiveActionAuth.password = "";
      this.sensitiveActionAuth.verifying = false;
      this.$bvModal.show("modal-sensitive-action-password");
      return new Promise((resolve) => {
        this.sensitiveActionAuth.resolver = resolve;
      });
    },
    resolveSensitiveActionPassword(result) {
      if (this.sensitiveActionAuth.resolver) {
        this.sensitiveActionAuth.resolver(result);
        this.sensitiveActionAuth.resolver = null;
      }
      this.sensitiveActionAuth.password = "";
      this.sensitiveActionAuth.verifying = false;
      this.sensitiveActionAuth.actionKey = "";
    },
    closeSensitiveActionPasswordModal() {
      if (this.sensitiveActionAuth.verifying) return;
      this.$bvModal.hide("modal-sensitive-action-password");
    },
    onSensitiveActionPasswordModalHidden() {
      this.resolveSensitiveActionPassword(false);
    },
    async confirmSensitiveActionPassword() {
      if (this.sensitiveActionAuth.verifying) return;
      if (!this.sensitiveActionAuth.password || !this.sensitiveActionAuth.password.trim()) {
        const requiredMsg = this.sensitiveAuthUsesOwnLoginCode
          ? (this.$t("enterYourLoginCode") || "أدخل رمز الدخول الخاص بك")
          : (this.$t("managerPasswordRequired") || "يرجى إدخال باسورد المدير");
        this.$notify.error(requiredMsg, { timeout: 2500 });
        return;
      }

      try {
        this.sensitiveActionAuth.verifying = true;
        const response = await HTTP.post("Admin/VerifySensitiveActionPassword", {
          password: this.sensitiveActionAuth.password,
          actionKey: this.sensitiveActionAuth.actionKey,
        });

        if (!response?.data || response.data.errorStatus) {
          this.$notify.error(this.resolveSensitiveAuthErrorMessage(response?.data?.message), { timeout: 2500 });
          return;
        }

        this.resolveSensitiveActionPassword(true);
        this.$bvModal.hide("modal-sensitive-action-password");
      } catch (error) {
        this.$notify.error(this.resolveSensitiveAuthErrorMessage(error?.response?.data?.message), { timeout: 2500 });
      } finally {
        this.sensitiveActionAuth.verifying = false;
      }
    },
    addToCartList(item) {
      try {
        if (!this.guardCartModification()) {
          return;
        }

        const bodyElement = document.querySelector("body");
        const textDirection = bodyElement.getAttribute("dir");
        const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";
        
        // Check if item is available
        if (!item.isAvailable) {
            this.$toast.error(
            this.$i18n.t("itemNotAvailable") || "الطبق/المشروب غير متوفر",
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
          // Item exists, increment quantity
          this.carditems[existingItemIndex].quantity += 1;
          // Recalculate total with valid prices
          const existingItem = this.carditems[existingItemIndex];
          const price = existingItem.price || 0;
          const disCountPrice = existingItem.disCountPrice || 0;
          const finalPrice = (disCountPrice > 0 && disCountPrice !== price) ? disCountPrice : price;
          this.carditems[existingItemIndex].total = finalPrice * existingItem.quantity;
        } else {
          // New item, add to cart
          // Ensure prices are valid numbers
          const sellingPrice = item.sellingPrice || 0;
          const disCountPrice = item.disCountPrice || 0;
          const finalPrice = (disCountPrice > 0 && disCountPrice !== sellingPrice) ? disCountPrice : sellingPrice;
          
          const cartItem = {
            name: item.name,
            quantity: 1,
            price: sellingPrice,
            disCountPrice: disCountPrice,
            total: finalPrice * 1, // quantity is 1
            id: item.id,
            tags: item.tags || 'مواد اخرى', // Add tags from original item
          };

          this.carditems.push(cartItem);
        }

        // Show compact notification
        this.showItemAddedNotification(item.name);
      } catch (error) {
        console.error("Error adding item to cart:", error);
        this.$toast.error(this.$i18n.t("error"), {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
          newestOnTop: true,
        });
      }
    },

    openCartLineNoteModal(index) {
      const item = this.carditems[index];
      if (!item) return;
      this.lineNoteCartIndex = index;
      this.lineNoteDraft = item.lineNote ? String(item.lineNote) : "";
      this.$bvModal.show("modal-cart-line-note");
    },
    saveCartLineNote() {
      const index = this.lineNoteCartIndex;
      if (index == null || !this.carditems[index]) return;
      const note = String(this.lineNoteDraft || "").trim();
      if (note) {
        this.$set(this.carditems[index], "lineNote", note);
      } else {
        this.$delete(this.carditems[index], "lineNote");
      }
      this.$bvModal.hide("modal-cart-line-note");
      this.lineNoteCartIndex = null;
      this.lineNoteDraft = "";
    },
    clearCartLineNote() {
      const index = this.lineNoteCartIndex;
      if (index == null || !this.carditems[index]) return;
      this.$delete(this.carditems[index], "lineNote");
      this.lineNoteDraft = "";
      this.$bvModal.hide("modal-cart-line-note");
      this.lineNoteCartIndex = null;
    },
    openDeleteItemConfirm(index) {
      if (index === null || index === undefined || index < 0 || index >= this.carditems.length) return;
      this.pendingDeleteItemIndex = index;
      this.$bvModal.show("modal-delete-cart-item");
    },
    async confirmDeleteCartItem() {
      if (this.pendingDeleteItemIndex === null || this.pendingDeleteItemIndex === undefined) {
        this.$bvModal.hide("modal-delete-cart-item");
        return;
      }
      const index = this.pendingDeleteItemIndex;
      this.pendingDeleteItemIndex = null;
      this.$bvModal.hide("modal-delete-cart-item");
      await this.deleteItem(index);
    },
    async deleteItem(index) {
      const targetItem = this.carditems[index];
      if (!targetItem) return;

      const sourceOrderItemId = Number(targetItem.sourceOrderItemId || 0);
      const deletedQuantity = Math.max(1, Number(targetItem.quantity || 1));

      if (sourceOrderItemId > 0) {
        try {
          await HTTP.post(`Admin/LogReturnedOrderItem`, {
            sourceOrderItemId,
            deletedQuantity,
          });
        } catch (error) {
          console.error("Failed to log returned item:", error);
          this.$toast.error(
            error?.response?.data?.message ||
              this.$i18n.t("returnedItemLogFailed") ||
              "تعذر تسجيل المادة المسترجعة",
            {
              position: "top-right",
              timeout: 2500,
              maxToasts: 1,
            }
          );
          return;
        }
      }

      this.carditems.splice(index, 1);
      this.$toast.error(this.$i18n.t("deleteItemFromOrderSucsses"), {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    },
    increaseQuantity(index) {
      if (this.carditems[index]) {
        this.carditems[index].quantity += 1;
        this.updateItemTotal(index);
      }
    },
    decreaseQuantity(index) {
      if (this.carditems[index] && this.carditems[index].quantity > 1) {
        this.carditems[index].quantity -= 1;
        this.updateItemTotal(index);
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
        const item = this.carditems[index];
        // Ensure prices are valid numbers
        const price = item.price || 0;
        const disCountPrice = item.disCountPrice || 0;
        const finalPrice = (disCountPrice > 0 && disCountPrice !== price) ? disCountPrice : price;
        this.carditems[index].total = finalPrice * (item.quantity || 1);
      }
    },
    GetAllItems() {
      if (this.posBrowseStep !== "products") {
        this.show = false;
        return;
      }
      this.show = true;
      const searchQuery = this.search.info ? `&info=${encodeURIComponent(this.search.info)}` : '';
      HTTP.get(
        `Admin/GetItems?pageNumber=0&pageSize=10000${searchQuery}`
      )
        .then((response) => {
          this.Items = response.data.data.items.map(item => ({
            ...item,
            imageError: false
          }));
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
        });
    },
    showItemAddedNotification(itemName) {
      // Clear any existing timer
      if (this.addItemTimer) {
        clearTimeout(this.addItemTimer);
      }
      
      // Increment counter
      this.itemsAddedCount++;
      this.lastAddedItem = itemName;
      
      // Clear previous success toasts
      this.$toast.clear();
      
      // Show aggregated notification
      const message = this.itemsAddedCount > 1 
        ? `${this.itemsAddedCount} ${this.$i18n.t("itemsAdded") || "مواد مضافة"}`
        : `${itemName} : ${this.$i18n.t("itemToCard")}`;
      
      this.$toast.success(message, {
        position: "top-right",
        timeout: 1500,
        closeOnClick: true,
        pauseOnFocusLoss: false,
        pauseOnHover: false,
        draggable: false,
        hideProgressBar: true,
        maxToasts: 1,
        newestOnTop: true,
        icon: true,
      });
      
      // Reset counter after 2 seconds of inactivity
      this.addItemTimer = setTimeout(() => {
        this.itemsAddedCount = 0;
        this.lastAddedItem = null;
      }, 2000);
    },
    initializeSignalR() {
      signalRService.startConnection()
        .then(() => {
          // Listen for order updates
          signalRService.on('OrderAdded', (data) => {
            console.log('Order added via SignalR:', data);
            if (!data) return;
            // Refresh tables if order is for a table
            if (data.TableId) {
              // Refresh tables first to get latest status
              this.getTables();
              // If this table is currently selected, reload its orders after a short delay
              if (this.selectedTableId === data.TableId) {
                setTimeout(() => {
                  const table = this.allTables.find(t => t.id === data.TableId);
                  if (table) {
                    this.selectTable(table);
                  }
                }, 500);
              }
            }
          });

          // Listen for table updates
          signalRService.on('TableUpdated', (data) => {
            console.log('Table updated via SignalR:', data);
            if (!data) return;
            // Refresh tables list to get latest data
            this.getTables();
            // If updated table is currently selected, update its status immediately
            if (this.selectedTableId === data.TableId) {
              const table = this.allTables.find(t => t.id === data.TableId);
              if (table) {
                table.status = data.Status;
                // If table became available, clear selection
                if (data.Status === 'Available') {
                  this.resetOrderSession({
                    orderType: "Takeaway",
                    silent: true,
                  });
                }
              }
            }
          });

          // Listen for order transfers
          signalRService.on('FloorPlanUpdated', () => {
            this.getTables();
            if (this.posFloorPlanGateVisible) {
              this.loadPosFloorPlan();
            }
          });

          signalRService.on('OrderTransferred', (data) => {
            console.log('Order transferred via SignalR:', data);
            if (!data) return;
            const sourceTableId = data.SourceTableId || data.FromTableId;
            const destinationTableId = data.DestinationTableId || data.ToTableId;
            const mode = data.Mode || "full";

            this.getTables().then(() => {
              if (this.selectedTableId === sourceTableId || this.selectedTableId === destinationTableId) {
                const tableToReload = this.allTables.find((t) => t.id === destinationTableId)
                  || this.allTables.find((t) => t.id === sourceTableId);
                if (tableToReload) {
                  this.selectTable(tableToReload);
                }
              }
            });

            const modeLabel =
              mode === "item"
                ? (this.$t("transferOneItem") || "نقل عنصر")
                : mode === "merge"
                ? (this.$t("mergeTwoInvoices") || "دمج فاتورتين")
                : (this.$t("transferFullOrder") || "نقل الطلب كامل");

            this.$toast.info(`${modeLabel} - ${this.$t("orderTransferred") || "تم نقل الطلب"}`, {
              timeout: 2400,
              maxToasts: 1,
            });
          });
        })
        .catch(error => {
          console.error('Failed to start SignalR connection:', error);
        });
    },
    cleanupSignalR() {
      // Remove SignalR listeners
      signalRService.off('OrderAdded');
      signalRService.off('TableUpdated');
      signalRService.off('FloorPlanUpdated');
      signalRService.off('OrderTransferred');
    },
    getSelectedTableNumber() {
      if (!this.selectedTableId) return '';
      const table = this.allTables.find(t => t.id === this.selectedTableId);
      return table ? table.tableNumber : '';
    },
    resetOrderMove() {
      this.orderMove.mode = "item";
      this.orderMove.sourceTableId = null;
      this.orderMove.destinationTableId = null;
      this.orderMove.sourceOrderItemId = null;
      this.orderMove.transferQuantity = 1;
      this.orderMove.sourceItems = [];
      this.orderMove.submitting = false;
      this.orderMove.sourceZoneFilter = "";
      this.orderMove.destinationZoneFilter = "";
    },
    onOrderMoveSourceZoneFilterChanged() {
      const ok = this.orderMoveSourceTables.some((t) => t.id === this.orderMove.sourceTableId);
      if (!ok) {
        this.orderMove.sourceTableId = null;
        this.onOrderMoveSourceChanged();
      }
    },
    onOrderMoveDestinationZoneFilterChanged() {
      const ok = this.orderMoveDestinationTables.some((t) => t.id === this.orderMove.destinationTableId);
      if (!ok) {
        this.orderMove.destinationTableId = null;
      }
    },
    async openOrderMoveModal(mode = "item", item = null) {
      const safeMode = ["item", "full", "merge"].includes(mode) ? mode : "item";
      this.resetOrderMove();
      this.orderMove.mode = safeMode;
      const preferredOrderItemId = safeMode === "item" && item?.sourceOrderItemId ? Number(item.sourceOrderItemId) : null;
      const preferredQuantity = safeMode === "item" && item?.quantity ? Number(item.quantity) : null;

      if (this.selectedTableId) {
        this.orderMove.sourceTableId = Number(this.selectedTableId);
      }

      await this.onOrderMoveSourceChanged();
      if (preferredOrderItemId) {
        const target = this.orderMove.sourceItems.find((x) => x.sourceOrderItemId === preferredOrderItemId);
        if (target) {
          this.orderMove.sourceOrderItemId = preferredOrderItemId;
          this.orderMove.transferQuantity = preferredQuantity && preferredQuantity > 0
            ? Math.min(preferredQuantity, target.quantity || preferredQuantity)
            : (target.quantity || 1);
        }
      }
      this.$bvModal.show("modal-order-move");
    },
    closeOrderMoveModal() {
      this.$bvModal.hide("modal-order-move");
      this.resetOrderMove();
    },
    async onOrderMoveSourceChanged() {
      this.orderMove.sourceOrderItemId = null;
      this.orderMove.transferQuantity = 1;
      this.orderMove.sourceItems = [];
      if (this.orderMove.mode !== "item" || !this.orderMove.sourceTableId) {
        this.syncOrderMoveDestinationAfterSourceChange();
        return;
      }
      await this.loadOrderMoveSourceItems(this.orderMove.sourceTableId);
      this.syncOrderMoveDestinationAfterSourceChange();
    },
    syncOrderMoveDestinationAfterSourceChange() {
      if (this.orderMove.destinationTableId === this.orderMove.sourceTableId) {
        this.orderMove.destinationTableId = null;
      }
      const ok = this.orderMoveDestinationTables.some((t) => t.id === this.orderMove.destinationTableId);
      if (!ok) {
        this.orderMove.destinationTableId = null;
      }
    },
    syncOrderMoveQuantityFromSelection() {
      const selected = this.orderMove.sourceItems.find((x) => x.sourceOrderItemId === this.orderMove.sourceOrderItemId);
      this.orderMove.transferQuantity = selected?.quantity || 1;
    },
    async loadOrderMoveSourceItems(sourceTableId) {
      try {
        const response = await HTTP.get(`Admin/GetTableOrders?tableId=${sourceTableId}`);
        const orders = response?.data?.data || [];
        const mapped = [];
        orders.forEach((order) => {
          (order.customerOrderItem || []).forEach((orderItem) => {
            if (!orderItem?.item || orderItem?.isDeleted) return;
            const itemName = orderItem.item.name || `#${orderItem.itemId}`;
            mapped.push({
              sourceOrderItemId: Number(orderItem.id),
              quantity: Number(orderItem.quantity || 1),
              label: `${itemName} × ${orderItem.quantity}`,
            });
          });
        });
        this.orderMove.sourceItems = mapped;
      } catch (error) {
        console.error("Error loading source order items:", error);
        this.$toast.error(this.$t("errorLoadingTableOrders") || "خطأ في تحميل طلبات الطاولة", {
          timeout: 2200,
          maxToasts: 1,
        });
      }
    },
    async confirmOrderMove() {
      if (!this.orderMoveCanConfirm || this.orderMove.submitting) return;

      const sourceTableId = Number(this.orderMove.sourceTableId);
      const destinationTableId = Number(this.orderMove.destinationTableId);
      const mode = this.orderMove.mode;
      const actionMap = {
        item: "transfer_item",
        full: "transfer_full",
        merge: "merge_invoices",
      };
      const canProceed = await this.requestSensitiveActionPassword(actionMap[mode] || "general");
      if (!canProceed) return;

      try {
        this.orderMove.submitting = true;
        let response;
        if (mode === "item") {
          response = await HTTP.post("Admin/TransferOrderItem", {
            sourceTableId,
            destinationTableId,
            sourceOrderItemId: Number(this.orderMove.sourceOrderItemId),
            transferQuantity: Number(this.orderMove.transferQuantity || 0),
          });
        } else if (mode === "full") {
          response = await HTTP.post("Admin/TransferFullOrder", {
            sourceTableId,
            destinationTableId,
          });
        } else {
          response = await HTTP.post("Admin/MergeTableOrders", {
            sourceTableId,
            destinationTableId,
          });
        }

        if (!response?.data || response.data.errorStatus) {
          this.$toast.error(response?.data?.message || (this.$t("error") || "حدث خطأ"), {
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }

        this.$toast.success(response.data.message || (this.$t("done") || "تم التنفيذ بنجاح"), {
          timeout: 2500,
          maxToasts: 1,
        });

        await this.getTables();
        if (mode === "item") {
          await this.refreshSourceAfterItemMove(sourceTableId);
        } else {
          const focusTable = this.allTables.find((t) => t.id === destinationTableId);
          if (focusTable) {
            await this.selectTable(focusTable);
          } else {
            this.selectedTableId = null;
            this.selectedTableIds = [];
            this.carditems = [];
            this.tableOrders = [];
          }
        }

        if (this.posFloorPlanGateVisible) {
          await this.loadPosFloorPlan();
        }

        this.closeOrderMoveModal();
      } catch (error) {
        console.error("Error confirming order move:", error);
        this.$toast.error(error?.response?.data?.message || (this.$t("error") || "حدث خطأ"), {
          timeout: 2500,
          maxToasts: 1,
        });
      } finally {
        this.orderMove.submitting = false;
      }
    },
    async refreshSourceAfterItemMove(sourceTableId) {
      const sourceTable = this.allTables.find((t) => t.id === sourceTableId);
      if (!sourceTable) {
        this.selectedTableId = null;
        this.selectedTableIds = [];
        this.carditems = [];
        this.tableOrders = [];
        return;
      }

      if (sourceTable.status === "Occupied") {
        await this.selectTable(sourceTable);
        return;
      }

      // Source table no longer has an active order after full transfer.
      this.selectedTableId = sourceTable.id;
      this.selectedTableIds = [sourceTable.id];
      this.carditems = [];
      this.tableOrders = [];
      this.orderForSend.tableId = sourceTable.id;
      this.orderForSend.tableIds = null;
      this.orderForSend.orderType = "DineIn";
    },

    posCategoryHasSubs(tag) {
      return childTagsOf(tag, this.tags).length > 0;
    },

    posCategoryTileStyle(tag) {
      const id = Number(tag?.id ?? tag?.Id ?? 0);
      const hue = (id * 53 + 17) % 360;
      return { "--pos-cat-hue": String(hue) };
    },

    posSelectAllProducts() {
      this.posBrowseStep = "products";
      this.posSelectedRoot = null;
      this.posSelectedSub = null;
      this.search.info = "";
      this.GetAllItems();
    },

    posSelectRoot(root) {
      if (!root) return;
      const subs = childTagsOf(root, this.tags);
      this.posSelectedRoot = root;
      this.posSelectedSub = null;
      this.search.info = "";
      if (subs.length > 0) {
        this.posBrowseStep = "subs";
        this.Items = [];
        return;
      }
      this.posBrowseStep = "products";
      this.search.info = root.name || "";
      this.GetAllItems();
    },

    posSelectSub(sub) {
      if (!sub) return;
      this.posSelectedSub = sub;
      this.posBrowseStep = "products";
      this.search.info = tagItemStorageValue(sub, this.tags);
      this.GetAllItems();
    },

    posGoBack() {
      if (this.posBrowseStep === "subs") {
        this.posBrowseStep = "roots";
        this.posSelectedRoot = null;
        this.posSelectedSub = null;
        this.Items = [];
        return;
      }
      if (this.posBrowseStep !== "products") {
        return;
      }
      if (this.posSelectedSub) {
        this.posBrowseStep = "subs";
        this.posSelectedSub = null;
        this.search.info = "";
        this.Items = [];
        return;
      }
      if (this.posSelectedRoot) {
        this.posBrowseStep = "roots";
        this.posSelectedRoot = null;
        this.search.info = "";
        this.Items = [];
        return;
      }
      this.posBrowseStep = "roots";
      this.search.info = "";
      this.Items = [];
    },
    
  },
};
</script>

<style scoped>
/* Avoid double frame: main.css styles .pos-tables-section-compact; inner .pos-tables-block is the real card */
.pos-tables-section-compact {
  background: transparent;
  border: none;
  padding: 0;
  margin-bottom: 0.75rem;
}

/* Tables: كارت واحد — صف موحّد (اختيار المخطط من أيقونة الهيدر) */
.pos-tables-block {
  border: 1px solid var(--border-color, rgba(255, 255, 255, 0.1));
  border-radius: 0.75rem;
  overflow: hidden;
  background: var(--bg-tertiary, #1e1e2e);
}

.pos-tables-toolbar-unified {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  align-content: flex-start;
  justify-content: space-between;
  gap: 0.45rem 0.65rem;
  padding: 0.45rem 0.65rem;
  background: transparent;
  /* لا يمتد لملء ارتفاع العمود الأب (كان يُظهر فراغاً ضمن الشريط) */
  flex: 0 0 auto;
  min-height: 0;
}

.pos-tables-picker-main {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  min-width: 0;
  /* بدون flex-grow: في الشريط العمودي لا يبتلع الفراغ الرأسي */
  flex: 0 1 auto;
  max-width: 100%;
}

.pos-tables-picker-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
  flex-shrink: 0;
}

.pos-tables-picker-icon--toolbar {
  font-size: 1.15rem;
}

.pos-tables-picker-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
  gap: 0.15rem;
}

.pos-tables-picker-text--inline {
  flex-direction: row;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 0.25rem 0.4rem;
}

.pos-tables-picker-text--inline .pos-tables-picker-label {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: none;
  letter-spacing: normal;
}

.pos-tables-picker-text--inline .pos-tables-picker-value {
  font-size: 0.9375rem;
}

.pos-tables-picker-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.pos-tables-picker-value {
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-primary);
  word-break: break-word;
}

.pos-tables-picker-sep {
  color: var(--text-secondary);
  opacity: 0.55;
  font-weight: 700;
}

.pos-tables-picker-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.35rem;
  height: 1.35rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 800;
  background: rgba(129, 140, 248, 0.22);
  color: var(--primary-color);
  border: 1px solid rgba(129, 140, 248, 0.38);
}

.pos-tables-toolbar-end {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 0.5rem;
  flex: 0 1 auto;
  min-width: 0;
}

.pos-table-actions-buttons--inline {
  flex: 0 1 auto;
}

.pos-merge-tables-btn-compact-text {
  display: none;
}

@media (min-width: 576px) {
  .pos-merge-tables-btn-compact-text {
    display: inline;
  }
}

/* Order Notes Modal Styles */
.order-notes-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.order-notes-header {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.order-notes-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.order-notes-input-wrapper {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.order-notes-label {
  font-weight: 600;
  color: var(--text-primary);
  font-size: 0.9375rem;
}

.order-notes-input {
  width: 100%;
  padding: 0.75rem;
  border: 2px solid var(--border-color);
  border-radius: var(--radius-md);
  font-size: 0.9375rem;
  font-family: 'Cairo', sans-serif;
  transition: all 0.3s ease;
  background: var(--bg-secondary);
  color: var(--text-primary);
}

.order-notes-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.1);
  outline: none;
}

.order-notes-textarea {
  width: 100%;
  padding: 0.875rem 1rem;
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  font-size: 1rem;
  font-family: 'Cairo', sans-serif;
  transition: all 0.3s ease;
  background: var(--bg-primary);
  color: var(--text-primary);
  resize: vertical;
  min-height: 120px;
}

.order-notes-textarea:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.1);
  outline: none;
}

.order-notes-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 0.5rem;
}

.order-notes-confirm-button,
.order-notes-cancel-button {
  display: flex;
  align-items: center;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.order-notes-confirm-button {
  background: var(--primary-color);
  color: #ffffff;
}

.order-notes-confirm-button:hover {
  background: var(--primary-hover);
  transform: translateY(-1px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.order-notes-cancel-button {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 2px solid var(--border-color);
}

.order-notes-cancel-button:hover {
  background: var(--bg-tertiary);
  border-color: var(--danger-color);
  color: var(--danger-color);
}

.order-discount-wrapper {
  padding: 0.85rem;
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
  background: var(--bg-secondary);
}

.order-discount-type-toggle {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.5rem;
}

.order-discount-type-btn {
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  border-radius: 0.65rem;
  padding: 0.55rem 0.75rem;
  font-weight: 700;
  cursor: pointer;
}

.order-discount-type-btn-active {
  border-color: var(--primary-color);
  color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.12);
}

.order-discount-input-row {
  margin-top: 0.65rem;
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.5rem;
}

.order-discount-clear-btn {
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  border-radius: 0.65rem;
  padding: 0.5rem 0.9rem;
  font-weight: 700;
  cursor: pointer;
}

.order-discount-presets {
  margin-top: 0.65rem;
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.order-discount-preset-btn {
  border: 1px dashed var(--primary-color);
  background: rgba(99, 102, 241, 0.08);
  color: var(--primary-color);
  border-radius: 999px;
  padding: 0.35rem 0.7rem;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
}

.order-discount-preview {
  margin-top: 0.75rem;
  padding-top: 0.65rem;
  border-top: 1px solid var(--border-light);
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.order-discount-preview-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  font-size: 0.9rem;
}

.order-discount-preview-row-total {
  color: var(--primary-color);
  font-weight: 800;
}

/* Order Notes Section Styles */
.pos-orders-notes-section {
  margin-top: 1rem;
  padding: 1rem;
  background: var(--bg-secondary, #f8f9fa);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color, #dee2e6);
}

.pos-orders-notes-header {
  display: flex;
  align-items: center;
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--border-color, #dee2e6);
}

.pos-orders-notes-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary, #212529);
  margin: 0;
  display: flex;
  align-items: center;
}

.pos-orders-notes-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.pos-order-note-item {
  padding: 0.75rem;
  background: white;
  border-radius: 0.5rem;
  border-left: 3px solid var(--primary-color, #818cf8);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.pos-order-note-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
}

.pos-order-note-code {
  font-weight: 600;
  color: var(--primary-color, #818cf8);
  display: flex;
  align-items: center;
}

.pos-order-note-date {
  color: var(--text-secondary, #6c757d);
  font-size: 0.8125rem;
}

.pos-order-note-content {
  color: var(--text-primary, #212529);
  font-size: 0.9375rem;
  line-height: 1.5;
  white-space: pre-wrap;
  word-wrap: break-word;
}

/* Fullscreen mode - POS content area */
.main-content-wrapper.pos-fullscreen {
  margin-left: 0 !important;
  margin-right: 0 !important;
  width: 100% !important;
  max-width: 100% !important;
}

[dir="rtl"] .main-content-wrapper.pos-fullscreen {
  margin-left: 0 !important;
  margin-right: 0 !important;
  width: 100% !important;
  max-width: 100% !important;
}

@media (max-width: 1023px) {
  .main-content-wrapper.pos-fullscreen {
    margin-left: 0 !important;
    margin-right: 0 !important;
    width: 100% !important;
  }
}

/* Transfer Table Button */
.pos-transfer-table-btn {
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.15) 0%, rgba(167, 139, 250, 0.15) 100%);
  border: 1px solid rgba(129, 140, 248, 0.3);
  color: var(--primary-color);
}

.pos-transfer-table-btn:hover {
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.25) 0%, rgba(167, 139, 250, 0.25) 100%);
  border-color: rgba(129, 140, 248, 0.5);
  color: #ffffff;
}

/* Transfer Table Modal Styles */
.transfer-table-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.transfer-table-info {
  text-align: center;
}

.transfer-table-message {
  font-size: 1rem;
  color: var(--text-primary);
  margin: 0;
}

.transfer-table-message strong {
  color: var(--primary-color);
  font-weight: 700;
}

.transfer-table-select {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.transfer-table-label {
  display: flex;
  align-items: center;
  font-weight: 600;
  color: var(--text-primary);
  font-size: 0.9375rem;
}

.transfer-table-select-input {
  width: 100%;
  padding: 0.875rem 1rem;
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  font-size: 1rem;
  font-family: 'Cairo', sans-serif;
  transition: all 0.3s ease;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.transfer-table-select-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.1);
  outline: none;
}

.transfer-table-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 0.5rem;
}

.transfer-table-cancel-btn,
.transfer-table-confirm-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.transfer-table-cancel-btn {
  background: var(--bg-secondary);
  color: var(--text-primary);
}

.transfer-table-cancel-btn:hover {
  background: var(--border-color);
  transform: translateY(-1px);
}

.transfer-table-confirm-btn {
  background: var(--primary-color);
  color: #ffffff;
}

.transfer-table-confirm-btn:hover:not(:disabled) {
  background: var(--primary-hover);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.transfer-table-confirm-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}

/* Tables Header Actions */
.pos-tables-header-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

/* Merge Tables Button */
.pos-merge-tables-btn-compact {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.375rem;
  padding: 0.5rem 0.75rem;
  border: none;
  border-radius: 0.5rem;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.15) 0%, rgba(167, 139, 250, 0.15) 100%);
  color: var(--primary-color);
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  border: 1px solid rgba(129, 140, 248, 0.3);
}

.pos-merge-tables-btn-compact:hover {
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.25) 0%, rgba(167, 139, 250, 0.25) 100%);
  border-color: rgba(129, 140, 248, 0.5);
  color: #ffffff;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(129, 140, 248, 0.3);
}

.pos-merge-tables-btn-compact .b-icon {
  font-size: 1rem;
}

.pos-merge-tables-btn-compact span {
  font-size: 0.8125rem;
}

/* Multi-Selected Table */
.pos-table-multi-selected {
  border: 2px solid var(--primary-color) !important;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.1) 0%, rgba(167, 139, 250, 0.1) 100%) !important;
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.2) !important;
}

.pos-table-multi-selected .pos-table-number-compact {
  color: var(--primary-color);
  font-weight: 700;
}

.pos-table-merged {
  border: 2px solid #10b981 !important;
  background: linear-gradient(135deg, rgba(16, 185, 129, 0.1) 0%, rgba(5, 150, 105, 0.1) 100%) !important;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2) !important;
}

.pos-table-merged .pos-table-number-compact {
  color: #10b981;
  font-weight: 700;
}

/* Ensure zone text doesn't overlap with buttons */
.pos-table-card-compact.pos-table-merged .pos-table-zone-compact {
  margin-bottom: 0.25rem;
}

.pos-table-actions-buttons {
  display: flex;
  gap: 0.45rem;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  flex: 0 1 auto;
}

/* في RTL يكون flex-end يضع المجموعة عند حافة الشاشة البعيدة عن ملخّص الطاولات */
.pos-table-actions-buttons:dir(rtl) {
  justify-content: flex-start;
}

.pos-table-actions-buttons.pos-table-actions-buttons--inline {
  flex-direction: column;
  align-items: flex-start;
  gap: 0.45rem;
}

.pos-table-actions-buttons--inline:dir(rtl) {
  align-items: flex-end;
}

.pos-table-action-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.4rem;
  width: 100%;
}

.pos-table-action-row--ops {
  justify-content: flex-start;
}

.pos-table-action-row--ops:dir(rtl) {
  justify-content: flex-end;
}

.pos-table-action-row--save {
  justify-content: flex-start;
}

.pos-table-action-row--save:dir(rtl) {
  justify-content: flex-end;
}

.pos-table-action-btn:not(.pos-table-action-btn--off-table) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  padding: 0.45rem 0.75rem;
  border: none;
  border-radius: 0.45rem;
  font-size: 0.8125rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.pos-table-action-btn b-icon {
  font-size: 1rem;
  flex-shrink: 0;
}

.pos-table-action-btn span {
  text-align: center;
}

.pos-table-action-transfer-group {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
  width: auto;
  flex: 0 1 auto;
}

.pos-table-action-transfer-group .pos-table-action-transfer {
  flex: 0 1 auto;
}

.pos-table-action-transfer {
  min-height: 2.55rem;
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(30, 41, 59, 0.24);
}

.pos-table-action-transfer:hover {
  border-color: rgba(255, 255, 255, 0.3);
  transform: translateY(-1px);
}

.pos-table-action-transfer--item {
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
}

.pos-table-action-transfer--item:hover {
  background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
}

.pos-table-action-transfer--full {
  background: linear-gradient(135deg, #14b8a6 0%, #0d9488 100%);
}

.pos-table-action-transfer--full:hover {
  background: linear-gradient(135deg, #0d9488 0%, #0f766e 100%);
}

.pos-table-action-transfer--merge {
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
}

.pos-table-action-transfer--merge:hover {
  background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%);
}

.pos-table-action-transfer .b-icon {
  font-size: 0.94rem;
}

.pos-table-action-transfer span {
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.01em;
  white-space: nowrap;
}

/* Order move modal buttons: keep compact and consistent */
#modal-order-move .order-move-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.55rem;
  margin-top: 0.95rem;
}

#modal-order-move .order-move-cancel-btn,
#modal-order-move .order-move-confirm-btn {
  width: 100%;
  min-height: 2.65rem;
  margin: 0;
  padding: 0.62rem 0.9rem;
  border-radius: 0.68rem;
  justify-content: center;
  font-size: 0.88rem;
}

#modal-order-move .order-move-cancel-btn {
  border-width: 1px;
}

.pos-table-action-save {
  min-height: 2.55rem;
  border: 1px solid rgba(255, 255, 255, 0.2);
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);
  box-sizing: border-box;
  padding: 0.52rem 1.1rem;
  gap: 0.5rem;
  border-radius: 0.55rem;
}

.pos-table-action-save:hover {
  border-color: rgba(255, 255, 255, 0.3);
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
}

.pos-table-action-save .b-icon {
  font-size: 0.94rem;
}

.pos-table-action-save span {
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.01em;
}

.pos-table-action-save-print {
  min-height: 2.55rem;
  border: 1px solid rgba(255, 255, 255, 0.2);
  background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(14, 165, 233, 0.3);
  box-sizing: border-box;
  padding: 0.52rem 1.1rem;
  gap: 0.5rem;
  border-radius: 0.55rem;
}

.pos-table-action-save-print:hover {
  border-color: rgba(255, 255, 255, 0.3);
  background: linear-gradient(135deg, #0284c7 0%, #0369a1 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(14, 165, 233, 0.4);
}

.pos-table-action-save-print .b-icon {
  font-size: 0.94rem;
}

.pos-table-action-save-print span {
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.01em;
}

/* سطح المكتب: عرض مرن بجانب ملخص الطاولات، بدل ثُلث صفّ فارغ عند زرّين فقط */
@media (min-width: 992px) {
  .pos-table-action-row--save > .pos-table-action-save,
  .pos-table-action-row--save > .pos-table-action-save-print {
    flex: 0 1 auto;
    width: auto;
    min-width: 10rem;
    max-width: 20rem;
  }
}

@media (max-width: 991px) {
  /* لوحيّات وموبايل: عمود مضغوط بدون flex-grow يبلع الفراغ الرأسي */
  .pos-tables-toolbar-unified {
    flex-direction: column;
    align-items: stretch;
    align-content: flex-start;
    gap: 0.35rem;
    padding: 0.38rem 0.5rem;
  }

  .pos-tables-picker-main {
    flex: 0 0 auto !important;
  }

  .pos-tables-toolbar-end {
    flex: 0 0 auto !important;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: center;
    justify-content: flex-start;
    gap: 0.3rem;
  }

  .pos-merge-tables-btn-compact {
    flex: 0 0 auto;
    padding: 0.35rem 0.5rem !important;
  }

  /* صفوف الأوامر في المقاسات الصغيرة */
  .pos-table-actions-buttons.pos-table-actions-buttons--inline {
    width: 100% !important;
    flex: 0 0 auto !important;
  }

  .pos-table-action-row--ops {
    display: grid !important;
    grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
    gap: 0.3rem !important;
  }

  .pos-table-action-transfer-group {
    display: contents;
  }

  .pos-table-action-row--save {
    display: grid !important;
    grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
    gap: 0.3rem !important;
  }

  .pos-table-actions-buttons,
  .pos-table-actions-buttons--inline {
    flex: 0 0 auto !important;
  }

  .pos-table-action-btn {
    flex: unset !important;
    min-width: 0 !important;
    width: 100%;
    padding: 0.4rem 0.3rem !important;
    font-size: 0.7rem !important;
    font-weight: 700 !important;
    white-space: normal !important;
    line-height: 1.2;
    min-height: 2.6rem;
    box-sizing: border-box;
  }

  .pos-table-action-btn span {
    display: block;
    max-width: 100%;
    overflow-wrap: break-word;
    hyphens: auto;
    font-size: 0.66rem;
  }

  .pos-table-action-btn b-icon {
    font-size: 0.9rem;
  }

  .pos-table-action-transfer-group {
    gap: 0.3rem;
  }
}

@media (max-width: 767px) {
  .pos-table-action-row--ops {
    grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
    gap: 0.38rem !important;
  }

  .pos-table-action-btn {
    min-height: 2.95rem !important;
    padding: 0.45rem 0.35rem !important;
    border-radius: 0.6rem !important;
  }

  .pos-table-action-btn span {
    font-size: 0.72rem !important;
    line-height: 1.2 !important;
  }

  .pos-table-action-transfer span {
    white-space: normal;
    font-size: 0.7rem !important;
  }

  #modal-order-move .order-move-actions {
    grid-template-columns: 1fr;
    gap: 0.45rem;
  }

  #modal-order-move .order-move-confirm-btn {
    order: 1;
  }

  #modal-order-move .order-move-cancel-btn {
    order: 2;
  }
}

/* سطح مكتب واسع: شريط أفقي — المنتقي لا يبتلع ارتفاعاً زائداً */
@media (min-width: 992px) {
  .pos-tables-toolbar-unified {
    flex-direction: row;
    align-items: center;
    align-content: center;
  }

  .pos-tables-toolbar-end {
    justify-content: flex-end;
  }

  /* flex-end في RTL يلصق المجموعة بحافة الشاشة البعيدة عن «الطاولات» */
  .pos-tables-toolbar-end:dir(rtl) {
    justify-content: flex-start;
  }
}

/* عرض ضيق جداً: عمودان للأزرار الطويلة عند الدمج */
@media (max-width: 400px) {
  .pos-table-action-row--ops,
  .pos-table-action-row--save {
    grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
  }

  .pos-table-action-row--ops .pos-table-action-btn:last-child:nth-child(odd) {
    grid-column: 1 / -1;
  }
}

/* Merge Tables Modal Styles */
.merge-tables-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.merge-tables-info {
  text-align: center;
}

.merge-tables-message {
  font-size: 1rem;
  color: var(--text-primary);
  margin: 0 0 1rem 0;
  font-weight: 600;
}

.merge-tables-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  max-height: 300px;
  overflow-y: auto;
  padding: 0.5rem;
  background: var(--bg-secondary);
  border-radius: 0.5rem;
  border: 1px solid var(--border-color);
}

.merge-table-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  transition: all 0.3s ease;
}

.merge-table-item:hover {
  background: var(--bg-tertiary);
  border-color: var(--primary-color);
  transform: translateX(4px);
}

.merge-table-item .b-icon {
  color: var(--primary-color);
  font-size: 1.125rem;
}

.merge-table-item span {
  font-weight: 600;
  color: var(--text-primary);
  font-size: 1rem;
}

.merge-table-remove-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 50%;
  background: var(--danger-color, #ef4444);
  color: #ffffff;
  cursor: pointer;
  transition: all 0.3s ease;
  padding: 0;
}

.merge-table-remove-btn:hover {
  background: var(--danger-hover, #dc2626);
  transform: scale(1.1);
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.3);
}

.merge-table-remove-btn .b-icon {
  font-size: 0.875rem;
}

.merge-tables-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 0.5rem;
}

.merge-tables-cancel-btn,
.merge-tables-confirm-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.merge-tables-cancel-btn {
  background: var(--bg-secondary);
  color: var(--text-primary);
}

.merge-tables-cancel-btn:hover {
  background: var(--border-color);
  transform: translateY(-1px);
}

.merge-tables-confirm-btn {
  background: var(--primary-color);
  color: #ffffff;
}

.merge-tables-confirm-btn:hover:not(:disabled) {
  background: var(--primary-hover);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.merge-tables-confirm-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}

/* RTL Support for Merge Tables */
[dir="rtl"] .merge-table-item:hover {
  transform: translateX(-4px);
}

[dir="rtl"] .merge-tables-actions {
  flex-direction: row-reverse;
}

/* Tables picker modal — align with users-modal footer pattern */
.pos-tables-picker-modal-hint {
  text-align: center;
  font-size: 0.9375rem;
  color: var(--text-secondary);
  margin: -0.35rem 0 1.35rem;
  line-height: 1.55;
}

.pos-tables-picker-modal-actions {
  justify-content: space-between !important;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.pos-tables-picker-modal-actions .users-form-cancel-button {
  flex: 0 1 auto;
  min-width: 8rem;
}

.pos-tables-picker-modal-count {
  font-size: 0.9375rem;
  font-weight: 600;
}

[dir="rtl"] .pos-tables-picker-modal-actions {
  flex-direction: row-reverse;
}

/* Delivery Information Section */
.delivery-info-section {
  background: var(--bg-secondary, #f8f9fa);
  border: 1px solid var(--border-color, #dee2e6);
  border-radius: var(--radius-md, 8px);
  padding: 1.5rem;
  margin-top: 1rem;
  margin-bottom: 1rem;
}

.delivery-section-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--text-primary, #212529);
  margin-bottom: 1rem;
  display: flex;
  align-items: center;
}

.delivery-info-section .users-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.delivery-info-section .required {
  color: var(--danger-color, #dc3545);
}

.delivery-radio-group {
  display: flex;
  gap: 1.5rem;
  flex-wrap: wrap;
  margin-top: 0.5rem;
}

.delivery-radio-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  border: 2px solid var(--border-color);
  background: var(--bg-tertiary);
  transition: all 0.3s ease;
  user-select: none;
}

.delivery-radio-label:hover {
  border-color: var(--primary-color);
  background: var(--bg-primary);
}

.delivery-radio-input {
  margin: 0;
  cursor: pointer;
  width: 18px;
  height: 18px;
  accent-color: var(--primary-color);
}

.delivery-radio-input:checked + .delivery-radio-text {
  color: var(--primary-color);
  font-weight: 600;
}

.delivery-radio-label:has(.delivery-radio-input:checked) {
  border-color: var(--primary-color);
  background: rgba(129, 140, 248, 0.1);
}

.delivery-radio-text {
  font-size: 0.9375rem;
  color: var(--text-primary);
  transition: all 0.3s ease;
}

.new-driver-section {
  background: var(--bg-tertiary, #ffffff);
  border: 1px dashed var(--border-color, #dee2e6);
  border-radius: var(--radius-md, 8px);
  padding: 1.5rem;
  margin-top: 1rem;
}

.new-driver-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary, #212529);
  margin-bottom: 1.25rem;
  display: flex;
  align-items: center;
}

.delivery-add-btn {
  width: 100%;
  padding: 0.875rem 1.5rem;
  border-radius: 0.75rem;
  border: 2px dashed var(--border-color);
  background: var(--bg-tertiary);
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

.delivery-add-btn:hover {
  background: var(--primary-color);
  color: #ffffff;
  border-color: var(--primary-color);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(129, 140, 248, 0.3);
}

[dir="rtl"] .delivery-info-section {
  direction: rtl;
}

[dir="rtl"] .delivery-section-title {
  flex-direction: row-reverse;
}

/* Orders Notes Section */
.pos-orders-notes-section {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 2px solid var(--border-color, #e5e7eb);
}

.pos-orders-notes-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.pos-orders-notes-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary, #1f2937);
  margin: 0;
}

.pos-orders-notes-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.pos-order-note-item {
  background: var(--bg-secondary, #f9fafb);
  border: 1px solid var(--border-color, #e5e7eb);
  border-radius: var(--radius-md, 8px);
  padding: 0.75rem;
}

.pos-order-note-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
  gap: 1rem;
}

.pos-order-note-code {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--primary-color, #818cf8);
}

.pos-order-note-date {
  font-size: 0.75rem;
  color: var(--text-secondary, #6b7280);
}

.pos-order-note-content {
  font-size: 0.875rem;
  color: var(--text-primary, #1f2937);
  line-height: 1.5;
}

/* سطر المنتج في السلة — هيكل أوضح: اسم + إجمالي السطر | سعر الوحدة × كمية | تحكم */
.pos-route--v2 .pos-cart-item--v2 {
  display: flex !important;
  flex-direction: column;
  align-items: stretch;
  gap: 0.45rem;
  padding: 0.55rem 0.7rem !important;
  border-radius: 0.75rem !important;
  border: 1px solid rgba(148, 163, 184, 0.22) !important;
  background: var(--bg-secondary, rgba(30, 41, 59, 0.72)) !important;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.14) !important;
  grid-template-columns: unset !important;
  transform: none !important;
}

.pos-route--v2 .pos-cart-item--v2:hover {
  border-color: rgba(129, 140, 248, 0.38) !important;
  box-shadow: 0 4px 14px rgba(99, 102, 241, 0.14) !important;
}

.pos-cart-item-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.6rem;
  min-width: 0;
  width: 100%;
}

.pos-cart-item-line-total {
  flex-shrink: 0;
  font-size: 0.95rem;
  font-weight: 800;
  color: #a5b4fc;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
  line-height: 1.25;
  padding-top: 0.08rem;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-name {
  margin: 0;
  font-size: 0.9rem;
  font-weight: 700;
  line-height: 1.38;
}

.pos-cart-item-bottom {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: flex-start;
  gap: 0.42rem;
  padding-top: 0.38rem;
  border-top: 1px solid rgba(148, 163, 184, 0.14);
  width: 100%;
}

.pos-cart-item-unit-wrap {
  display: flex;
  flex-wrap: nowrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.45rem;
  min-width: 0;
  flex: 0 0 auto;
  width: 100%;
}

.pos-cart-item-unit-price {
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--text-secondary);
  font-variant-numeric: tabular-nums;
}

.pos-cart-item-qty-badge {
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.02em;
  color: var(--text-primary);
  padding: 0.1rem 0.42rem;
  border-radius: 999px;
  background: rgba(99, 102, 241, 0.14);
  border: 1px solid rgba(129, 140, 248, 0.28);
  font-variant-numeric: tabular-nums;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-controls {
  grid-column: unset !important;
  grid-row: unset !important;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: nowrap;
  gap: 0.45rem;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-quantity {
  margin-inline-start: auto;
  padding: 0.22rem;
  gap: 0.28rem;
}

.pos-route--v2 .pos-cart-item--v2 .pos-quantity-btn {
  width: 2rem;
  height: 2rem;
  min-width: 2rem;
  min-height: 2rem;
  font-size: 0.92rem;
}

.pos-route--v2 .pos-cart-item--v2 .pos-quantity-input {
  width: 2.35rem;
  height: 2rem;
  min-height: 2rem;
  font-size: 0.82rem;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-delete {
  width: 2.05rem;
  height: 2.05rem;
  min-width: 2.05rem;
  min-height: 2.05rem;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-name-wrap {
  flex: 1;
  min-width: 0;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-line-note {
  margin: 0.12rem 0 0;
  font-size: 0.68rem;
  line-height: 1.25;
  color: #b45309;
  font-weight: 600;
  word-break: break-word;
}

.pos-line-note-item-name {
  margin: 0 0 0.35rem;
  font-size: 0.9rem;
  font-weight: 700;
  color: #1f2937;
}

.pos-line-note-hint {
  margin: 0 0 0.75rem;
  font-size: 0.75rem;
  color: #6b7280;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-note {
  width: 2.05rem;
  height: 2.05rem;
  min-width: 2.05rem;
  min-height: 2.05rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.55rem;
  border: 1px solid rgba(217, 119, 6, 0.28);
  background: linear-gradient(180deg, rgba(251, 191, 36, 0.16) 0%, rgba(245, 158, 11, 0.08) 100%);
  color: #d97706;
  transition: all 0.16s ease;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-note--active {
  border-color: rgba(217, 119, 6, 0.5);
  background: linear-gradient(180deg, rgba(251, 191, 36, 0.28) 0%, rgba(245, 158, 11, 0.16) 100%);
  color: #b45309;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-note:hover {
  border-color: rgba(217, 119, 6, 0.45);
  color: #b45309;
  transform: translateY(-1px);
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer {
  width: 2.05rem;
  height: 2.05rem;
  min-width: 2.05rem;
  min-height: 2.05rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.55rem;
  border: 1px solid rgba(79, 70, 229, 0.24);
  background: linear-gradient(180deg, rgba(99, 102, 241, 0.14) 0%, rgba(79, 70, 229, 0.08) 100%);
  color: #4f46e5;
  transition: all 0.16s ease;
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer:hover {
  border-color: rgba(79, 70, 229, 0.42);
  background: linear-gradient(180deg, rgba(99, 102, 241, 0.2) 0%, rgba(79, 70, 229, 0.13) 100%);
  color: #3730a3;
  transform: translateY(-1px);
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer:active {
  transform: translateY(0);
}

.pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer:focus-visible {
  outline: none;
  box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.26);
}

/* السلة على الشاشات الصغيرة/المتوسطة: ترتيب أوضح لسطر المادة */
@media (max-width: 1199px) {
  .pos-route--v2 .pos-cart-item--v2 {
    padding: 0.45rem 0.55rem !important;
    gap: 0.35rem;
    border-radius: 0.65rem !important;
  }

  .pos-cart-item-top {
    flex-direction: column;
    align-items: stretch;
    gap: 0.25rem;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-name {
    font-size: 0.82rem;
    line-height: 1.3;
  }

  .pos-cart-item-line-total {
    align-self: flex-start;
    font-size: 0.88rem;
    padding-top: 0;
  }

  .pos-cart-item-bottom {
    gap: 0.35rem;
    padding-top: 0.28rem;
  }

  .pos-cart-item-unit-wrap {
    order: 1;
    flex: 0 0 auto;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-controls {
    order: 2;
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: nowrap;
    gap: 0.5rem;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-quantity {
    padding: 0.18rem;
    gap: 0.22rem;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-btn,
  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-input,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-note,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-delete {
    height: 1.9rem;
    min-height: 1.9rem;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-btn {
    width: 1.9rem;
    min-width: 1.9rem;
    font-size: 0.84rem;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-input {
    width: 2.1rem;
    font-size: 0.78rem;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-delete {
    width: 1.95rem;
    min-width: 1.95rem;
    margin-inline-start: 0;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer {
    width: 1.95rem;
    min-width: 1.95rem;
  }
}

@media (max-width: 575px) {
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-controls {
    gap: 0.38rem !important;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-quantity {
    padding: 0.2rem !important;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-btn,
  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-input,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-note,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-delete {
    min-height: 2.1rem !important;
    height: 2.1rem !important;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-btn,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-transfer,
  .pos-route--v2 .pos-cart-item--v2 .pos-cart-item-delete {
    min-width: 2.1rem !important;
    width: 2.1rem !important;
  }

  .pos-route--v2 .pos-cart-item--v2 .pos-quantity-input {
    width: 2.3rem !important;
    font-size: 0.82rem !important;
  }
}

/* Scrollbar Styling */
.pos-cart-items-section::-webkit-scrollbar,
.pos-cart-items-list::-webkit-scrollbar {
  width: 6px;
}

.pos-cart-items-section::-webkit-scrollbar-track,
.pos-cart-items-list::-webkit-scrollbar-track {
  background: var(--bg-secondary, #f9fafb);
  border-radius: 3px;
}

.pos-cart-items-section::-webkit-scrollbar-thumb,
.pos-cart-items-list::-webkit-scrollbar-thumb {
  background: var(--border-color, #d1d5db);
  border-radius: 3px;
}

.pos-cart-items-section::-webkit-scrollbar-thumb:hover,
.pos-cart-items-list::-webkit-scrollbar-thumb:hover {
  background: var(--primary-color, #818cf8);
}

/* RTL Support */
[dir="rtl"] .pos-cart-header {
  flex-direction: row;
}

[dir="rtl"] .pos-cart-item--v2 .pos-cart-item-controls {
  flex-direction: row;
}

[dir="rtl"] .pos-order-note-header {
  flex-direction: row-reverse;
}

/* POS Header Section - Light Theme Support */
:root.light-theme .pos-header-section {
  background: linear-gradient(135deg, var(--bg-primary) 0%, var(--bg-tertiary) 50%, var(--bg-primary) 100%);
  box-shadow: 
    0 4px 20px rgba(0, 0, 0, 0.08),
    0 0 0 1px var(--border-color),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
  border: 1px solid var(--border-color);
}

:root.light-theme .pos-header-section::before {
  background: 
    radial-gradient(circle at 20% 50%, rgba(99, 102, 241, 0.08) 0%, transparent 50%),
    radial-gradient(circle at 80% 50%, rgba(99, 102, 241, 0.05) 0%, transparent 50%);
}

:root.light-theme .pos-header-section::after {
  background: linear-gradient(90deg, 
    transparent 0%, 
    rgba(99, 102, 241, 0.3) 20%, 
    rgba(99, 102, 241, 0.3) 50%, 
    rgba(99, 102, 241, 0.3) 80%, 
    transparent 100%);
}

:root.light-theme .pos-logo-section {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
}

:root.light-theme .pos-logo-section:hover {
  background: var(--bg-dark);
  border-color: var(--primary-color);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.15);
}

:root.light-theme .pos-logo {
  filter: drop-shadow(0 2px 8px rgba(0, 0, 0, 0.1));
}

:root.light-theme .pos-employee-info {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
}

:root.light-theme .pos-employee-info:hover {
  background: var(--bg-dark);
  border-color: var(--primary-color);
}

:root.light-theme .pos-employee-info .b-icon {
  filter: drop-shadow(0 2px 4px rgba(99, 102, 241, 0.2));
}

:root.light-theme .pos-employee-label {
  color: var(--text-secondary);
}

:root.light-theme .pos-employee-name {
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-light) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

/* POS — بوابة مخطط الطاولات عند الدخول (teleport إلى body) */
.pos-floor-plan-gate {
  position: relative;
  z-index: 50;
  margin-bottom: 1rem;
}

/* بوابة المخطط: شاشة كاملة (بدون نافذة صغيرة في المنتصف) */
.pos-floor-plan-gate--fullscreen.pos-floor-plan-gate--page {
  position: fixed;
  inset: 0;
  z-index: 999;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: flex-start;
  padding: 0;
  margin: 0;
  width: 100%;
  min-width: 100%;
  max-width: 100vw;
  min-height: 100vh;
  min-height: 100dvh;
  background: var(--bg-secondary);
  box-sizing: border-box;
  overflow: hidden;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card,
.pos-floor-plan-gate--page .pos-fp-page-root {
  width: 100%;
  max-width: none;
  flex: 1 1 auto;
  min-height: 0;
  max-height: none;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* جعل b-overlay يملأ ارتفاع البوابة */
.pos-floor-plan-gate--page .pos-floor-plan-gate-overlay--fill {
  flex: 1 1 auto;
  min-height: 0;
  display: flex !important;
  flex-direction: column;
  width: 100% !important;
  height: 100% !important;
  border-radius: 0 !important;
}

.pos-floor-plan-gate--page ::v-deep .b-overlay-wrap {
  position: relative;
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
  width: 100%;
  height: 100%;
}

.pos-floor-plan-gate--page ::v-deep .b-overlay > .position-relative {
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.pos-floor-plan-gate--page .pos-fp-launch {
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 0;
  overflow: hidden;
}

@media (min-width: 900px) {
  .pos-floor-plan-gate--page .pos-fp-launch {
    flex-direction: column;
    align-items: stretch;
    gap: 0;
    padding: 0;
  }

  .pos-floor-plan-gate--page .pos-fp-launch__intro.pos-fp-launch__intro--navbar {
    flex: 0 0 auto;
    width: 100%;
    max-width: none;
    align-self: stretch;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: stretch;
    gap: 0.5rem 0.85rem;
    padding: 0.45rem 0.75rem;
    border-inline-end: none;
    border-bottom: 1px solid var(--border-color);
    overflow: visible;
    background: var(--bg-primary);
    box-shadow: 0 2px 14px rgba(15, 23, 42, 0.07);
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-canvas-outer {
    flex: 1 1 auto;
    min-width: 0;
    min-height: 0;
    margin: 0;
    padding: 0.5rem 0.65rem 0.65rem;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    background: var(--bg-primary);
  }
}

@media (max-width: 899px) {
  .pos-floor-plan-gate--page .pos-fp-launch__intro.pos-fp-launch__intro--navbar {
    flex-shrink: 0;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: stretch;
    gap: 0.45rem 0.65rem;
    max-width: none;
    width: 100%;
    padding: 0.55rem 0.65rem;
    overflow: visible;
    background: var(--bg-primary);
    border-bottom: 1px solid var(--border-color);
    box-shadow: 0 2px 10px rgba(15, 23, 42, 0.06);
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-canvas-outer {
    flex: 1 1 auto;
    min-width: 0;
    min-height: 0;
    margin: 0;
    padding: 0.5rem 0.75rem 0.75rem;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    background: var(--bg-primary);
  }
}

/*
 * تخطيط متوسط (تابلت أفقي / لاب صغير): عمود أوضح للعربية، أزرار أسهل، وهوامش أوضح للمخطط
 */
@media (min-width: 900px) and (max-width: 1439px) {
  .pos-floor-plan-gate--page .pos-fp-launch__intro.pos-fp-launch__intro--navbar {
    padding: 0.42rem 0.65rem;
    gap: 0.45rem 0.65rem;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-canvas-outer {
    padding: 0.45rem 0.55rem 0.6rem;
  }

  .pos-floor-plan-gate--page .pos-fp-launch__eyebrow {
    font-size: 0.6875rem;
    margin-bottom: 0.35rem;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-title {
    font-size: clamp(0.95rem, 1.45vw, 1.22rem);
    margin-bottom: 0.42rem;
    line-height: 1.22;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tabs-card {
    padding: 0.48rem 0.58rem;
    border-radius: 0.7rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tabs-card__header .pos-fp-gate-tabs-label {
    font-size: 0.75rem;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-tab {
    padding: 0.3rem 0.48rem;
    font-size: 0.8125rem;
    min-height: 2.35rem;
    border-radius: 0.5rem;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-tabs {
    gap: 0.32rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tools {
    margin-top: 0.38rem;
    padding-top: 0.48rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tools-title {
    font-size: 0.75rem;
    margin-bottom: 0.24rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tools-hint {
    font-size: 0.66rem;
    line-height: 1.4;
    margin-bottom: 0.42rem;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-help-item {
    font-size: 0.64rem;
    padding: 0.28rem 0.36rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tool-toggle,
  .pos-floor-plan-gate--page .pos-fp-gate-tool-btn {
    padding: 0.48rem 0.52rem;
    font-size: 0.72rem;
    margin-bottom: 0.38rem;
    min-height: 2.55rem;
    border-radius: 0.5rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tool-copy strong {
    font-size: 0.72rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tool-copy small {
    font-size: 0.62rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tool-state {
    font-size: 0.58rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tool-ic {
    font-size: 1rem;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip {
    padding: 0.45rem 0.58rem;
    font-size: 0.8125rem;
    min-height: 2.65rem;
    border-radius: 0.5rem;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip .button-icon {
    font-size: 0.95rem !important;
  }
}

/*
 * بوابة الصفحة (ملء الشاشة): اللوحة تمتد لباقي ارتفاع وعرض المنطقة المتاحة تحت شريط الـ navbar.
 * الإحداثيات 0–1 تبقى نسبية لأبعاد اللوحة الفعلية.
 */
.pos-floor-plan-gate--page .pos-floor-plan-gate-canvas-outer {
  width: 100%;
  max-width: 100%;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-canvas-wrap {
  flex: 1 1 0;
  min-height: 0;
  width: 100%;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  overflow: hidden;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-canvas {
  position: relative;
  flex: 1 1 0;
  min-height: 0;
  width: 100%;
  max-width: 100%;
  aspect-ratio: unset;
  box-sizing: border-box;
  overflow: hidden;
}

.pos-floor-plan-gate--fullscreen .pos-floor-plan-gate-canvas {
  max-height: none;
}

.pos-floor-plan-gate-overlay {
  border-radius: 1rem;
}

.pos-floor-plan-gate-card {
  background: var(--bg-primary, #1e1e2e);
  border: 1px solid var(--border-color, rgba(255, 255, 255, 0.12));
  border-radius: 1rem;
  padding: 1rem 1.25rem 1.25rem;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.35);
}

.pos-floor-plan-gate-title {
  font-size: clamp(1.1rem, 2vw, 1.35rem);
  font-weight: 800;
  margin: 0 0 0.35rem;
  color: var(--text-primary, #f9fafb);
}

/* بطاقة اختيار الموقع — سطح المكتب: لف متعدد الأسطر */
.pos-fp-gate-tabs-card {
  margin-bottom: 0;
  margin-top: 0.15rem;
  padding: 0.85rem 1rem 1rem;
  background: linear-gradient(
    155deg,
    rgba(129, 140, 248, 0.09) 0%,
    var(--bg-tertiary) 42%,
    var(--bg-tertiary) 100%
  );
  border-radius: 1rem;
  border: 1px solid rgba(129, 140, 248, 0.22);
  box-shadow:
    0 6px 22px rgba(0, 0, 0, 0.12),
    inset 0 1px 0 rgba(255, 255, 255, 0.06);
}

.pos-fp-gate-tabs-card__header {
  display: flex;
  align-items: flex-start;
  gap: 0.55rem;
  margin-bottom: 0.7rem;
  padding-bottom: 0.55rem;
  border-bottom: 1px solid rgba(129, 140, 248, 0.18);
}

.pos-fp-gate-tabs-card__icon-wrap {
  flex-shrink: 0;
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.65rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(
    145deg,
    rgba(129, 140, 248, 0.35) 0%,
    rgba(167, 139, 250, 0.22) 100%
  );
  color: var(--primary-color);
  font-size: 1.05rem;
  box-shadow: 0 2px 10px rgba(129, 140, 248, 0.25);
}

.pos-fp-gate-tabs-card__header .pos-fp-gate-tabs-label {
  margin-bottom: 0;
  font-size: 0.9rem;
  font-weight: 800;
  color: var(--text-primary);
  line-height: 1.35;
  letter-spacing: 0.01em;
}

.pos-fp-gate-tabs-label {
  font-size: 0.875rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin-bottom: 0.75rem;
}

.pos-fp-gate-tabs-scroll {
  width: 100%;
}

.pos-floor-plan-gate-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.pos-floor-plan-gate-tab {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem 1rem;
  border-radius: 0.75rem;
  border: 2px solid var(--border-color);
  border-inline-start: 3px solid transparent;
  background: var(--bg-primary);
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.9375rem;
  cursor: pointer;
  transition:
    border-color 0.2s ease,
    border-inline-start-color 0.2s ease,
    color 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.15s ease,
    background 0.2s ease;
}

.pos-floor-plan-gate-tab:hover {
  border-color: var(--primary-color);
  border-inline-start-color: rgba(129, 140, 248, 0.45);
  color: var(--primary-color);
  transform: translateY(-1px);
  box-shadow: 0 4px 14px rgba(129, 140, 248, 0.18);
}

.pos-floor-plan-gate-tab:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.35);
}

.pos-floor-plan-gate-tab--active {
  border-color: rgba(129, 140, 248, 0.55);
  border-inline-start-color: var(--primary-color);
  background: linear-gradient(
    118deg,
    rgba(129, 140, 248, 0.22) 0%,
    rgba(167, 139, 250, 0.12) 55%,
    var(--bg-primary) 100%
  );
  color: var(--primary-color);
  box-shadow:
    0 3px 14px rgba(129, 140, 248, 0.28),
    inset 0 1px 0 rgba(255, 255, 255, 0.06);
}

/* شاشات اللمس والتابلت: لا تمرير أفقي — قائمة عمودية، أزرار بعرض كامل */
@media (max-width: 1023px) {
  .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) {
    padding: 1rem 0.85rem;
    background: linear-gradient(
      165deg,
      rgba(129, 140, 248, 0.08) 0%,
      var(--bg-primary) 48%
    );
  }

  .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) .pos-fp-gate-tabs-scroll {
    overflow: visible;
  }

  .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) .pos-fp-gate-tabs-scroll .pos-floor-plan-gate-tabs {
    flex-direction: column;
    flex-wrap: nowrap;
    width: 100%;
    gap: 0.65rem;
  }

  .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) .pos-fp-gate-tabs-scroll .pos-floor-plan-gate-tab {
    width: 100%;
    min-height: 54px;
    padding: 0.85rem 1rem;
    font-size: 1.0625rem;
    border-radius: 0.85rem;
    flex: 0 0 auto;
    touch-action: manipulation;
    -webkit-tap-highlight-color: rgba(129, 140, 248, 0.25);
    box-sizing: border-box;
  }

  .pos-fp-gate-tabs-label {
    margin-bottom: 0.65rem;
    font-size: 0.9rem;
  }

  /* بوابة الصفحة على اللمس: أزرار أوضح من دون استهلاك كامل 52px */
  .pos-floor-plan-gate--page .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) .pos-fp-gate-tabs-scroll .pos-floor-plan-gate-tabs {
    gap: 0.42rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) .pos-fp-gate-tabs-scroll .pos-floor-plan-gate-tab {
    min-height: 44px;
    padding: 0.48rem 0.72rem;
    font-size: 0.9rem;
    line-height: 1.2;
    white-space: nowrap;
    border-radius: 0.65rem;
    align-items: center;
    justify-content: center;
    touch-action: manipulation;
    -webkit-tap-highlight-color: rgba(129, 140, 248, 0.2);
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) .pos-fp-gate-tabs-card__header .pos-fp-gate-tabs-label {
    font-size: 0.8125rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tabs-card:not(.pos-fp-gate-tabs-card--navbar) {
    padding: 0.65rem 0.6rem;
  }

  .pos-floor-plan-gate--page .pos-fp-gate-tabs-card--navbar .pos-fp-gate-plan-select {
    min-height: 2.65rem;
    font-size: 0.9375rem;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip {
    padding: 0.55rem 0.8rem;
    font-size: 0.9375rem;
    min-height: 48px;
  }

  .pos-floor-plan-gate--page .pos-fp-launch__intro--navbar > .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip {
    padding: 0.45rem 0.95rem !important;
    font-size: 0.9375rem !important;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip .button-icon {
    font-size: 1.05rem !important;
  }
}

@media (min-width: 1024px) {
  .pos-fp-gate-tabs-scroll .pos-floor-plan-gate-tabs {
    flex-direction: row;
    flex-wrap: wrap;
  }

  .pos-floor-plan-gate-tab {
    width: auto;
    min-height: unset;
  }
}

.pos-floor-plan-gate-canvas-outer {
  margin-bottom: 1rem;
}

.pos-floor-plan-gate-canvas-wrap {
  border-radius: 1rem;
  overflow: hidden;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  box-shadow: var(--shadow-md);
}

.pos-floor-plan-gate-canvas {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 10;
  min-height: 200px;
  max-height: min(42vh, 420px);
}

.pos-floor-plan-gate-zone {
  position: absolute;
  border: 2px dashed;
  border-radius: 4px;
  pointer-events: none;
  box-sizing: border-box;
}

.pos-floor-plan-gate-zone-label {
  position: absolute;
  top: 2px;
  left: 4px;
  font-size: 10px;
  font-weight: 700;
  color: #374151;
  text-shadow: 0 0 4px #fff;
}

.pos-floor-plan-gate-table-chip {
  position: absolute;
  transform: translate(-50%, -50%);
  box-sizing: border-box;
  min-width: var(--floor-table-chip-size, 3.5rem);
  width: var(--floor-table-chip-size, 3.5rem);
  height: var(--floor-table-chip-size, 3.5rem);
  padding: 0;
  border-radius: 0.5rem;
  border: 2px solid #fff;
  font-weight: 700;
  font-size: var(--floor-table-chip-font, 0.9375rem);
  line-height: 1;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
  z-index: 2;
  touch-action: manipulation;
  -webkit-tap-highlight-color: rgba(255, 255, 255, 0.35);
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.pos-floor-plan-gate-table-chip--picked {
  outline: 3px solid #fbbf24;
  outline-offset: 1px;
  box-shadow: 0 0 0 2px rgba(251, 191, 36, 0.45), 0 2px 10px rgba(0, 0, 0, 0.35);
  z-index: 3;
}

.pos-floor-plan-gate-table-chip--transfer-source {
  outline: 3px solid #22d3ee;
  outline-offset: 1px;
  box-shadow: 0 0 0 2px rgba(34, 211, 238, 0.45), 0 6px 16px rgba(14, 116, 144, 0.35);
  z-index: 4;
}

.pos-floor-plan-gate-table-chip:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.pos-fp-chip-avail {
  background: linear-gradient(135deg, #22c55e, #16a34a);
  color: #fff;
}

.pos-fp-chip-occ {
  background: linear-gradient(135deg, #ef4444, #dc2626);
  color: #fff;
}

.pos-fp-chip-res {
  background: linear-gradient(135deg, #a78bfa, #7c3aed);
  color: #fff;
}

.pos-fp-chip-out {
  background: #64748b;
  color: #fff;
}

.pos-floor-plan-gate-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  justify-content: flex-start;
  align-items: stretch;
}

/* زر «متابعة بدون طاولة» أسفل بطاقة التبويبات */
.pos-floor-plan-gate-actions--footer {
  margin-top: 0.75rem;
  padding-top: 0.75rem;
  flex-direction: column;
}

.pos-floor-plan-gate-actions--footer.pos-floor-plan-gate-actions--after-tabs {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

.pos-floor-plan-gate-actions--footer .users-add-button {
  width: 100%;
  justify-content: center;
}

@media (min-width: 900px) {
  .pos-floor-plan-gate-actions--footer .users-add-button {
    width: auto;
    align-self: stretch;
  }

  .pos-floor-plan-gate--page .pos-floor-plan-gate-actions--footer.pos-floor-plan-gate-actions--intro-foot .users-add-button.pos-fp-gate-btn-skip {
    width: 100%;
  }

  .pos-floor-plan-gate--page .pos-fp-launch__intro--navbar > .pos-floor-plan-gate-actions--footer.pos-floor-plan-gate-actions--intro-foot .users-add-button.pos-fp-gate-btn-skip {
    width: auto !important;
    align-self: center;
  }
}

.pos-floor-plan-gate-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.65rem 1.2rem;
  border-radius: 0.75rem;
  font-weight: 700;
  font-size: 0.95rem;
  cursor: pointer;
  border: none;
  transition: transform 0.12s ease, box-shadow 0.15s ease;
}

.pos-floor-plan-gate-btn--primary {
  background: linear-gradient(135deg, #818cf8 0%, #a78bfa 100%);
  color: #fff;
  box-shadow: 0 4px 12px rgba(129, 140, 248, 0.3);
}

.pos-floor-plan-gate-btn--secondary {
  background: var(--bg-primary);
  color: var(--text-primary);
  border: 2px solid var(--border-color);
  box-shadow: var(--shadow-sm);
}

.pos-main-section--dimmed {
  opacity: 0.35;
  pointer-events: none;
  user-select: none;
}

:root.light-theme .pos-floor-plan-gate-card {
  background: #fff;
  border-color: var(--border-color, #e5e7eb);
  box-shadow: var(--shadow-lg, 0 10px 40px rgba(0, 0, 0, 0.08));
}

:root.light-theme .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-title {
  background: linear-gradient(135deg, #6366f1 0%, #818cf8 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

:root.light-theme .pos-floor-plan-gate-tab {
  background: var(--bg-tertiary, #f3f4f6);
  border-color: var(--border-color, #e5e7eb);
  color: var(--text-primary, #1f2937);
}

:root.light-theme .pos-floor-plan-gate-canvas-wrap {
  border-color: var(--border-color, #e5e7eb);
  background: #e5e7eb;
}

:root.light-theme .pos-floor-plan-gate--page {
  background: var(--bg-secondary);
}

:root.light-theme .pos-floor-plan-gate--page .pos-floor-plan-gate-card,
:root.light-theme .pos-floor-plan-gate--page .pos-fp-page-root {
  background: transparent;
}

@media (min-width: 900px) {
  :root.light-theme .pos-floor-plan-gate--page .pos-fp-launch__intro.pos-fp-launch__intro--navbar {
    background: var(--bg-primary);
    border-bottom-color: var(--border-color);
  }
}

:root.light-theme .pos-fp-gate-tabs-card {
  border-color: rgba(99, 102, 241, 0.22);
  background: linear-gradient(
    155deg,
    rgba(99, 102, 241, 0.06) 0%,
    var(--bg-tertiary, #f3f4f6) 45%
  );
  box-shadow: 0 4px 18px rgba(15, 23, 42, 0.06);
}

:root.light-theme .pos-fp-gate-tabs-card__header {
  border-bottom-color: rgba(99, 102, 241, 0.14);
}

:root.light-theme .pos-floor-plan-gate--page .pos-fp-launch__eyebrow {
  background: linear-gradient(
    135deg,
    rgba(99, 102, 241, 0.12) 0%,
    rgba(139, 92, 246, 0.08) 100%
  );
  border-color: rgba(99, 102, 241, 0.28);
}

/* ——— POS v2: هيكل، سلة جانبية، أرضية ——— */
.pos-route--v2 .main-content-wrapper,
.pos-route--v2 .pos-container-fluid {
  max-width: 100%;
  padding-left: 0.75rem;
  padding-right: 0.75rem;
}

@media (min-width: 700px) {
  .pos-route--v2 .main-content-wrapper {
    background: var(--bg-secondary);
    min-height: calc(100vh - 56px);
  }
}

.pos-workspace--v2 {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0;
  align-items: stretch;
}

/* اجعل منطقة العمل تملأ المساحة بين الهيدر وشريط الإنهاء */
.main-content-wrapper.pos-route.pos-route--v2 .pos-workspace--v2 {
  min-height: calc(100dvh - 56px);
}

@media (min-width: 700px) {
  .pos-workspace--v2 {
    grid-template-columns: minmax(0, 1fr) min(420px, 34vw);
    gap: 1.25rem;
    /* يمتد عمود السلة لارتفاع الصف مثل المحتوى الرئيسي */
    align-items: stretch;
  }
}

.pos-workspace-main {
  min-width: 0;
}

.pos-main-section--v2 {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}

.pos-tables-block {
  border-radius: 1rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  box-shadow: var(--shadow-md);
}

.pos-categories-scroll {
  padding: 0.35rem 0 0.25rem;
}

.pos-categories-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(5.75rem, 1fr));
  gap: 0.55rem;
  align-items: stretch;
  padding: 0 0.12rem 0.12rem;
}

/* واجهة v2 — شريط التصنيفات والأدوات أقل ارتفاعاً */
.pos-main-section--v2 .pos-categories-scroll {
  padding: 0.04rem 0 0.02rem;
}

.pos-main-section--v2 .pos-browse-toolbar {
  margin-top: 0;
  margin-bottom: 0.16rem;
  gap: 0.5rem;
  padding: 0.02rem 0;
}

.pos-main-section--v2 .pos-browse-back-btn {
  min-height: 2.25rem;
  padding: 0.35rem 0.75rem;
  border-radius: 0.55rem;
  border-width: 1px;
  font-size: clamp(0.82rem, 0.65vw + 0.72rem, 0.98rem);
}

.pos-main-section--v2 .pos-browse-titles {
  gap: 0.15rem;
}

.pos-main-section--v2 .pos-browse-primary {
  font-size: clamp(0.86rem, 0.55vw + 0.76rem, 1rem);
  line-height: 1.22;
}

.pos-main-section--v2 .pos-browse-secondary {
  font-size: clamp(0.78rem, 0.35vw + 0.68rem, 0.88rem);
  line-height: 1.28;
}

.pos-main-section--v2 .pos-categories-list {
  grid-template-columns: repeat(auto-fill, minmax(4.85rem, 1fr)) !important;
  gap: 0.48rem !important;
  padding: 0 0.1rem 0.16rem !important;
}

.pos-main-section--v2 .pos-category-btn {
  min-height: 4.35rem;
  padding: 0.5rem 0.35rem 0.42rem;
  border-radius: 0.72rem;
  font-size: 0.74rem;
  gap: 0.3rem;
}

.pos-main-section--v2 .pos-category-btn-icon {
  width: 1.95rem;
  height: 1.95rem;
  font-size: 0.9rem;
  border-radius: 0.52rem;
}

.pos-main-section--v2 .pos-category-btn:hover {
  transform: translateY(-1px);
}

@media (max-width: 575px) {
  .pos-main-section--v2 .pos-categories-list {
    grid-template-columns: repeat(auto-fill, minmax(4.2rem, 1fr)) !important;
    gap: 0.4rem !important;
  }

  .pos-main-section--v2 .pos-category-btn {
    min-height: 4rem;
    padding: 0.42rem 0.28rem 0.36rem;
    border-radius: 0.62rem;
    font-size: 0.68rem;
  }

  .pos-main-section--v2 .pos-category-btn-icon {
    width: 1.8rem;
    height: 1.8rem;
    font-size: 0.82rem;
  }
}

@media (min-width: 576px) and (max-width: 991px) {
  .pos-main-section--v2 .pos-categories-list {
    grid-template-columns: repeat(auto-fill, minmax(4.75rem, 1fr)) !important;
  }
}

@media (min-width: 992px) {
  .pos-main-section--v2 .pos-categories-list {
    grid-template-columns: repeat(auto-fill, minmax(5.5rem, 1fr)) !important;
    gap: 0.52rem !important;
  }

  .pos-main-section--v2 .pos-category-btn {
    min-height: 4.55rem;
    font-size: 0.78rem;
    border-radius: 0.78rem;
  }
}

.pos-products-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 0.85rem;
}

@media (min-width: 768px) {
  .pos-products-grid {
    grid-template-columns: repeat(auto-fill, minmax(168px, 1fr));
  }
}

.pos-product-card {
  border-radius: 1rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  box-shadow: var(--shadow-md);
  transition: transform 0.14s ease, box-shadow 0.14s ease, border-color 0.14s ease;
}

.pos-product-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
  border-color: var(--primary-color);
}

/* بطاقات المنتج — v2: أصغر، بدون مسار التصنيف */
.pos-main-section--v2 .pos-products-grid {
  gap: 0.55rem;
  grid-template-columns: repeat(auto-fill, minmax(104px, 1fr));
}

@media (min-width: 768px) {
  .pos-main-section--v2 .pos-products-grid {
    grid-template-columns: repeat(auto-fill, minmax(118px, 1fr));
  }
}

.pos-main-section--v2 .pos-product-card {
  padding: 0.35rem 0.45rem;
  border-radius: 0.65rem;
}

.pos-main-section--v2 .pos-product-media {
  margin-bottom: 0.3rem;
  min-height: 52px;
}

.pos-main-section--v2 .pos-product-image {
  max-height: 52px;
}

.pos-main-section--v2 .pos-product-image-placeholder {
  height: 52px;
}

.pos-main-section--v2 .pos-product-placeholder-icon {
  font-size: 1.35rem;
}

.pos-main-section--v2 .pos-product-info {
  gap: 0.3rem;
}

.pos-main-section--v2 .pos-product-name {
  font-size: 0.7rem;
  min-height: 1.45rem;
  line-height: 1.2;
  font-weight: 600;
}

/* سلة جانبية / درج */
.pos-cart-shell {
  position: fixed;
  inset: 0;
  z-index: 1040;
  pointer-events: none;
  display: flex;
  justify-content: flex-start;
  align-items: stretch;
}

[dir="rtl"] .pos-cart-shell {
  justify-content: flex-end;
}

.pos-cart-shell--open {
  pointer-events: auto;
}

.pos-cart-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(2, 6, 23, 0.62);
  opacity: 0;
  transition: opacity 0.25s ease;
}

.pos-cart-shell--open .pos-cart-backdrop {
  opacity: 1;
}

.pos-cart-panel {
  position: relative;
  pointer-events: auto;
  width: min(100%, 440px);
  max-width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  background: var(--bg-primary);
  border-inline-start: 1px solid var(--border-color);
  box-shadow: none;
  transform: translateX(110%);
  transition: transform 0.32s cubic-bezier(0.22, 1, 0.36, 1);
}

[dir="rtl"] .pos-cart-panel {
  transform: translateX(-110%);
}

.pos-cart-shell--open .pos-cart-panel {
  transform: translateX(0);
}

.pos-cart-panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.1rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  flex-shrink: 0;
}

.pos-cart-panel-brand {
  font-size: 1.15rem;
  font-weight: 800;
  color: var(--text-primary);
}

.pos-cart-panel-dismiss {
  border: none;
  background: rgba(255, 255, 255, 0.08);
  color: #e2e8f0;
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.75rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.pos-cart-container {
  flex: 1;
  height: 100%;
  min-height: 0;
  overflow: auto;
  padding: 0.75rem 0.9rem 1.25rem;
}

/* امتلاء عمود السلة v2: القائمة/الفراغ يمتدان لارتفاع الحاوي (سلة فارغة = رسالة في منتصف المساحة) */
.main-content-wrapper.pos-route.pos-route--v2 .pos-cart-panel--v2 {
  display: flex !important;
  flex-direction: column !important;
  height: 100% !important;
  min-height: 0 !important;
}

.main-content-wrapper.pos-route.pos-route--v2 .pos-cart-container {
  display: flex !important;
  flex-direction: column !important;
  flex: 1 1 0% !important;
  height: 100% !important;
  min-height: 0 !important;
  overflow: hidden !important;
}

.main-content-wrapper.pos-route.pos-route--v2 .pos-cart-items-section {
  flex: 1 1 0% !important;
  height: 100% !important;
  min-height: 0 !important;
  max-height: none !important;
  display: flex !important;
  flex-direction: column !important;
}

.main-content-wrapper.pos-route.pos-route--v2 .pos-cart-items-list {
  flex: 1 1 0% !important;
  height: 100% !important;
  min-height: 0 !important;
  max-height: none !important;
  overflow-x: hidden !important;
  overflow-y: auto !important;
}

.main-content-wrapper.pos-route.pos-route--v2 .pos-cart-empty {
  flex: 1 1 auto !important;
  min-height: 0 !important;
  margin: 0 !important;
  align-self: stretch !important;
}

@media (min-width: 700px) {
  .pos-cart-shell {
    position: sticky;
    top: 0;
    inset: auto;
    align-self: stretch;
    height: 100%;
    max-height: none;
    pointer-events: auto;
    z-index: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;
  }

  .pos-cart-backdrop {
    display: none !important;
  }

  .pos-cart-panel {
    width: 100%;
    flex: 1 1 auto;
    min-height: 0;
    height: 100%;
    max-height: none;
    display: flex;
    flex-direction: column;
    transform: none !important;
    border-radius: 1.15rem;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-inline-start-width: 1px;
    box-shadow: none;
  }

  .pos-cart-shell--open .pos-cart-panel {
    transform: none !important;
  }

  .pos-cart-panel-head.d-lg-none {
    display: none !important;
  }

  .pos-mobile-cart-fab.d-lg-none {
    display: none !important;
  }
}

/* بوابة المخطط — نصوص وأزرار (التخطيط ملء الشاشة يُدار بـ --page أعلاه) */
.pos-floor-plan-gate-card--v2 {
  background: transparent;
  border: none;
  box-shadow: none;
  padding: 0;
  max-width: none;
}

.pos-fp-launch__intro {
  text-align: start;
}

/* عمود البوابة: التمرير على العنوان/التبويبات فقط، وزر التخطي ثابت أسفل العمود */
.pos-floor-plan-gate--page .pos-fp-launch__intro {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro-main {
  flex: 1 1 auto;
  min-height: 0;
  overflow-x: hidden;
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
}

/* شريط علوي (navbar): عنوان + مواقع + تمرير أفقي للتبويبات */
.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar {
  flex-direction: row;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar .pos-fp-launch__intro-main {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: stretch;
  gap: 0.45rem 0.85rem;
  flex: 1 1 auto;
  min-width: 0;
  overflow-x: visible;
  overflow-y: visible;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar .pos-fp-launch__intro-head {
  margin-bottom: 0;
  flex: 0 1 auto;
  min-width: min(100%, 11rem);
  align-self: center;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card--navbar {
  flex: 1 1 240px;
  min-width: 0;
  margin-top: 0 !important;
  margin-bottom: 0 !important;
  display: flex;
  flex-direction: row;
  align-items: stretch;
  align-self: stretch;
  gap: 0.45rem 0.65rem;
  padding: 0.35rem 0.5rem !important;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card--navbar .pos-fp-gate-tabs-card__header {
  flex-shrink: 0;
  flex-direction: row;
  align-items: center;
  align-self: center;
  margin-bottom: 0;
  padding-bottom: 0;
  border-bottom: none;
  border-inline-end: 1px solid rgba(129, 140, 248, 0.22);
  padding-inline-end: 0.5rem;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card--navbar .pos-fp-gate-tabs-card__icon-wrap {
  width: 1.65rem;
  height: 1.65rem;
  border-radius: 0.45rem;
  font-size: 0.82rem;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card--navbar .pos-fp-gate-tabs-card__header .pos-fp-gate-tabs-label {
  display: block;
  margin: 0;
  font-size: 0.72rem;
  font-weight: 800;
  color: var(--text-secondary);
  line-height: 1.25;
  max-width: 6.5rem;
}

.pos-floor-plan-gate--page .pos-fp-gate-plan-select-wrap {
  flex: 1 1 200px;
  min-width: 0;
  max-width: 100%;
  align-self: stretch;
  display: flex;
  align-items: stretch;
}

.pos-floor-plan-gate--page .pos-fp-gate-plan-select {
  width: 100%;
  flex: 1 1 auto;
  min-height: 2.45rem;
  height: 100%;
  padding: 0.4rem 2.25rem 0.4rem 0.65rem;
  font-size: 0.875rem;
  font-weight: 600;
  line-height: 1.25;
  border-radius: 0.55rem;
  border: 2px solid rgba(129, 140, 248, 0.35);
  background: var(--bg-primary);
  color: var(--text-primary);
  cursor: pointer;
  appearance: auto;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.06);
  transition:
    border-color 0.15s ease,
    box-shadow 0.15s ease;
}

.pos-floor-plan-gate--page .pos-fp-gate-plan-select:hover {
  border-color: rgba(129, 140, 248, 0.55);
}

.pos-floor-plan-gate--page .pos-fp-gate-plan-select:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.28);
}

[dir="rtl"] .pos-floor-plan-gate--page .pos-fp-gate-plan-select {
  padding: 0.4rem 0.65rem 0.4rem 2.25rem;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro:not(.pos-fp-launch__intro--navbar) .pos-floor-plan-gate-actions--intro-foot {
  flex-shrink: 0;
  margin-top: auto;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar .pos-floor-plan-gate-actions--intro-foot {
  flex: 0 0 auto;
  flex-shrink: 0;
  margin-top: 0 !important;
  margin-inline-start: auto;
  width: auto;
  align-self: stretch;
  display: flex;
  flex-direction: column;
  justify-content: stretch;
  align-items: stretch;
  min-height: 0;
}

/* أدوات دمج / نقل من بوابة المخطط */
.pos-fp-gate-tools {
  flex-shrink: 0;
  margin-top: 0.35rem;
  padding-top: 0.45rem;
  border-top: 1px solid var(--border-color);
}

.pos-fp-gate-tools-title {
  font-size: 0.65rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin: 0 0 0.2rem;
}

.pos-fp-gate-tools-hint {
  font-size: 0.6rem;
  line-height: 1.3;
  color: var(--text-muted);
  margin: 0 0 0.45rem;
}

.pos-fp-gate-help-list {
  display: flex;
  flex-direction: column;
  gap: 0.28rem;
  margin: 0 0 0.48rem;
}

.pos-fp-gate-help-item {
  display: flex;
  align-items: flex-start;
  gap: 0.35rem;
  font-size: 0.6rem;
  line-height: 1.35;
  color: var(--text-secondary);
  padding: 0.24rem 0.34rem;
  border: 1px dashed rgba(129, 140, 248, 0.32);
  border-radius: 0.4rem;
  background: rgba(129, 140, 248, 0.06);
}

.pos-fp-gate-help-ic {
  flex-shrink: 0;
  font-size: 0.72rem;
  color: var(--primary-color);
  margin-top: 0.04rem;
}

.pos-fp-gate-tool-toggle,
.pos-fp-gate-tool-btn {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 0.35rem;
  width: 100%;
  margin-bottom: 0.35rem;
  padding: 0.38rem 0.45rem;
  font-size: 0.65rem;
  font-weight: 600;
  line-height: 1.2;
  border-radius: 0.45rem;
  border: 1px solid var(--border-color);
  background: var(--bg-tertiary);
  color: var(--text-primary);
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
  box-sizing: border-box;
}

.pos-fp-gate-tool-copy {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.06rem;
  min-width: 0;
  flex: 1 1 auto;
}

.pos-fp-gate-tool-copy strong {
  font-size: 0.66rem;
  line-height: 1.2;
  font-weight: 700;
}

.pos-fp-gate-tool-copy small {
  font-size: 0.56rem;
  line-height: 1.25;
  color: var(--text-muted);
}

.pos-fp-gate-tool-state {
  flex-shrink: 0;
  font-size: 0.52rem;
  font-weight: 700;
  line-height: 1;
  padding: 0.22rem 0.34rem;
  border-radius: 999px;
  border: 1px solid rgba(148, 163, 184, 0.34);
  background: rgba(148, 163, 184, 0.1);
  color: var(--text-secondary);
}

.pos-fp-gate-tool-state--on {
  border-color: rgba(16, 185, 129, 0.4);
  background: rgba(16, 185, 129, 0.14);
  color: #10b981;
}

.pos-fp-gate-tool-count {
  flex-shrink: 0;
  min-width: 1.28rem;
  height: 1.28rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  border: 1px solid rgba(129, 140, 248, 0.45);
  background: rgba(129, 140, 248, 0.18);
  color: var(--primary-color);
  font-size: 0.58rem;
  font-weight: 700;
}

.pos-fp-gate-tool-toggle:last-child,
.pos-fp-gate-tool-btn:last-child {
  margin-bottom: 0;
}

.pos-fp-gate-tool-toggle:hover,
.pos-fp-gate-tool-btn:hover:not(:disabled) {
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.pos-fp-gate-tool-toggle--on {
  border-color: var(--primary-color);
  background: linear-gradient(
    135deg,
    rgba(129, 140, 248, 0.16) 0%,
    rgba(167, 139, 250, 0.12) 100%
  );
  color: var(--primary-color);
}

.pos-fp-gate-tool-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.pos-fp-gate-tool-btn--accent {
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.12) 0%, rgba(167, 139, 250, 0.08) 100%);
}

.pos-fp-gate-tool-btn--on {
  border-color: var(--primary-color);
  background: linear-gradient(
    135deg,
    rgba(129, 140, 248, 0.18) 0%,
    rgba(167, 139, 250, 0.14) 100%
  );
  color: var(--primary-color);
  box-shadow: 0 0 0 2px rgba(129, 140, 248, 0.16);
}

.pos-fp-gate-tool-ic {
  flex-shrink: 0;
  font-size: 0.85rem;
}

.pos-fp-launch__intro-head {
  margin-bottom: 0.65rem;
}

.pos-fp-launch__eyebrow {
  font-size: 0.875rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin: 0 0 0.75rem;
  letter-spacing: 0.02em;
}

.pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-title {
  font-size: clamp(1.5rem, 2.2vw, 2rem);
  font-weight: 800;
  line-height: 1.25;
  margin: 0 0 0.65rem;
  background: linear-gradient(135deg, #818cf8 0%, #a78bfa 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

/* عمود البوابة (صفحة POS) — عرض أضيق ونصوص وأزرار أصغر (مساحة أكبر للمخطط) */
.pos-floor-plan-gate--page .pos-fp-launch__intro-head {
  margin-bottom: 0.45rem;
}

.pos-floor-plan-gate--page .pos-fp-launch__eyebrow {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.625rem;
  margin: 0 0 0.38rem;
  padding: 0.22rem 0.55rem;
  border-radius: 999px;
  font-weight: 800;
  letter-spacing: 0.04em;
  color: var(--primary-color);
  background: linear-gradient(
    135deg,
    rgba(129, 140, 248, 0.16) 0%,
    rgba(167, 139, 250, 0.1) 100%
  );
  border: 1px solid rgba(129, 140, 248, 0.35);
  box-shadow: 0 2px 8px rgba(129, 140, 248, 0.12);
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-title {
  font-size: clamp(0.9rem, 1.15vw, 1.12rem);
  line-height: 1.28;
  margin-bottom: 0;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card {
  padding: 0.55rem 0.58rem 0.62rem;
  border-radius: 0.75rem;
  margin-top: 0.35rem;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card__header {
  gap: 0.42rem;
  margin-bottom: 0.48rem;
  padding-bottom: 0.42rem;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card__icon-wrap {
  width: 1.85rem;
  height: 1.85rem;
  border-radius: 0.55rem;
  font-size: 0.92rem;
}

.pos-floor-plan-gate--page .pos-fp-gate-tabs-card__header .pos-fp-gate-tabs-label {
  font-size: 0.6875rem;
  line-height: 1.3;
  white-space: normal;
  overflow: visible;
  text-overflow: unset;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  line-clamp: 3;
  -webkit-box-orient: vertical;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-tab {
  padding: 0.2rem 0.38rem;
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1.15;
  border-radius: 0.45rem;
  border-width: 1px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-tabs {
  gap: 0.22rem;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer {
  margin-top: 0.55rem;
  gap: 0.45rem;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer.pos-floor-plan-gate-actions--intro-foot {
  margin-top: auto;
  padding-top: 0.45rem;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer.pos-floor-plan-gate-actions--intro-foot.pos-floor-plan-gate-actions--after-tabs {
  margin-top: auto;
  padding-top: 0.55rem;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar > .pos-floor-plan-gate-actions--footer.pos-floor-plan-gate-actions--intro-foot {
  margin-top: 0 !important;
  padding-top: 0 !important;
  border-top: none !important;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar > .pos-floor-plan-gate-actions--footer.pos-floor-plan-gate-actions--intro-foot.pos-floor-plan-gate-actions--after-tabs {
  margin-top: 0 !important;
  padding-top: 0 !important;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip {
  padding: 0.32rem 0.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1.15;
  gap: 0.35rem;
  border-radius: 0.45rem;
  white-space: nowrap;
  min-height: unset;
  box-shadow: 0 2px 8px rgba(129, 140, 248, 0.28);
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip .button-text {
  font-size: inherit;
  line-height: 1.15;
  white-space: nowrap;
}

.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip .button-icon {
  font-size: 0.875rem !important;
}

/* صف الـ navbar: زر التخطي يمتد لنفس ارتفاع بطاقة المواقع (stretch) */
.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar > .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip {
  flex: 1 1 auto;
  min-height: 0 !important;
  height: auto;
  padding: 0.35rem 0.85rem !important;
  font-size: 0.875rem !important;
  line-height: 1.25 !important;
  border-radius: 0.55rem !important;
  display: flex !important;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  width: auto !important;
  max-width: 100%;
}

.pos-floor-plan-gate--page .pos-fp-launch__intro--navbar > .pos-floor-plan-gate-actions--footer .users-add-button.pos-fp-gate-btn-skip .button-icon {
  font-size: 1rem !important;
}

.pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-actions--footer {
  margin-top: 1rem;
}

.pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-canvas-wrap {
  border-radius: 1rem;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-md);
}

.pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-canvas {
  max-height: min(52vh, 520px);
}

/* ملء الصفحة: إلغاء سقف الارتفاع حتى تمتد اللوحة مع سلسلة الـ flex أعلاه */
.pos-floor-plan-gate--page .pos-floor-plan-gate-card--v2 .pos-floor-plan-gate-canvas {
  max-height: none;
}

.pos-mobile-cart-fab {
  bottom: 1.35rem;
  inset-inline-start: 1.35rem;
  width: 3.75rem;
  height: 3.75rem;
  border-radius: 1rem;
  background: linear-gradient(135deg, #818cf8 0%, #a78bfa 100%);
  border: none;
  box-shadow: 0 4px 12px rgba(129, 140, 248, 0.35);
}

:root.light-theme .pos-route--v2 .main-content-wrapper {
  background: var(--bg-secondary);
}

:root.light-theme .pos-tables-block {
  background: var(--bg-primary);
  border-color: var(--border-color);
  box-shadow: var(--shadow-md);
}

:root.light-theme .pos-product-card {
  background: var(--bg-primary);
  border-color: var(--border-color);
}

:root.light-theme .pos-cart-panel {
  background: var(--bg-primary);
  border-color: var(--border-color);
}

:root.light-theme .pos-cart-panel-brand {
  color: var(--text-primary);
}

</style>

<style>
/* Tables modal: teleported to body — zone grid + filter panel */
.modal-content-wrapper .pos-tables-modal-filters {
  padding: 1rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
  margin-bottom: 1.25rem;
}

.pos-tables-modal-zone {
  margin-bottom: 1.5rem;
}

.pos-tables-modal-zone:last-of-type {
  margin-bottom: 0;
}

.pos-tables-modal-zone-title {
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text-primary, #e5e7eb);
  margin-bottom: 0.75rem;
  padding-bottom: 0.4rem;
  border-bottom: 1px solid var(--border-color, rgba(255, 255, 255, 0.12));
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.35rem;
}

.pos-tables-modal-zone-count {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-secondary, #9ca3af);
}

.pos-tables-scroll-modal {
  max-height: none;
}

:root.light-theme .pos-tables-modal-zone-title {
  color: var(--text-primary, #111827);
  border-bottom-color: var(--border-color, #e5e7eb);
}

:root.light-theme .pos-tables-modal-zone-count {
  color: var(--text-secondary, #6b7280);
}

/* POS hierarchical category browse — أهداف لمس أوضح */
.pos-browse-toolbar {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.85rem;
  flex-wrap: wrap;
  padding: 0.35rem 0;
}

.pos-browse-back-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  min-height: 3rem;
  padding: 0.65rem 1.1rem;
  border-radius: 0.75rem;
  border: 2px solid var(--border-color, rgba(255, 255, 255, 0.15));
  background: var(--bg-secondary, #2a2a3e);
  color: var(--text-primary, #f3f4f6);
  font-size: clamp(0.95rem, 1vw + 0.8rem, 1.1rem);
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s ease, border-color 0.15s ease, transform 0.12s ease;
  touch-action: manipulation;
  -webkit-tap-highlight-color: transparent;
}

.pos-browse-back-btn:hover {
  background: var(--primary-color, #6366f1);
  border-color: var(--primary-color, #6366f1);
  color: #fff;
}

.pos-browse-back-btn:active {
  transform: scale(0.98);
}

.pos-browse-titles {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 0;
  flex: 1;
}

.pos-browse-primary {
  font-size: clamp(1.05rem, 1.2vw + 0.85rem, 1.25rem);
  font-weight: 700;
  color: var(--text-primary, #f9fafb);
  line-height: 1.35;
}

.pos-browse-secondary {
  font-size: clamp(0.9rem, 0.5vw + 0.8rem, 1rem);
  color: var(--text-secondary, #9ca3af);
  word-break: break-word;
  line-height: 1.45;
}

</style>

