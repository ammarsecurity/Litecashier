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
      />
      <div
        class="main-content-wrapper pos-route"
        :class="{ 'pos-fullscreen': isFullscreen }"
      >
        <b-container fluid class="pos-container-fluid">
          <div class="pos-page-container">
            <!-- Left Side: Tables and Products -->
            <div class="pos-main-section">
            <!-- Quick Actions Bar -->
            <div class="pos-quick-actions">
              <div class="pos-quick-search">
                <b-icon icon="search" class="pos-quick-search-icon"></b-icon>
                <input
                  v-model="quickSearch"
                  ref="posQuickSearchInput"
                  type="search"
                  :placeholder="$t('searchPlaceholder')"
                  class="pos-quick-search-input"
                />
              </div>
            </div>

            <!-- Tables: summary bar + modal picker + row actions (one card) -->
            <div class="pos-tables-section-compact">
              <div class="pos-tables-block">
                <div class="pos-tables-picker-bar">
                  <div class="pos-tables-picker-main">
                    <b-icon icon="table" class="pos-tables-picker-icon"></b-icon>
                    <div class="pos-tables-picker-text">
                      <span class="pos-tables-picker-label">{{ $t("tables") || "الطاولات" }}</span>
                      <span class="pos-tables-picker-value">{{ selectedTableSummary }}</span>
                    </div>
                  </div>
                  <div class="pos-tables-picker-actions">
                    <button
                      v-if="selectedTableIds.length > 1"
                      type="button"
                      class="pos-merge-tables-btn-compact"
                      @click="openMergeTablesModal"
                      :title="$t('mergeTables') || 'دمج طاولات'"
                    >
                      <b-icon icon="layers"></b-icon>
                      <span>{{ $t("mergeTables") || "دمج" }}</span>
                    </button>
                    <button type="button" class="pos-tables-open-modal-btn" @click="showTablesModal = true">
                      <b-icon icon="grid-3x3-gap-fill" class="me-1"></b-icon>
                      {{ $t("chooseTable") || "اختيار الطاولة" }}
                    </button>
                  </div>
                </div>

                <div v-if="selectedTableId" class="pos-table-actions-bar">
                  <div class="pos-table-actions-meta">
                    <template v-if="mergedTableIds.length > 1">
                      <b-icon icon="layers-fill" class="pos-table-actions-meta-icon"></b-icon>
                      <span class="pos-table-actions-meta-label">{{ $t("mergedTables") || "الطاولات المدمجة" }}</span>
                      <span class="pos-table-actions-count">{{ mergedTableIds.length }}</span>
                    </template>
                    <template v-else>
                      <b-icon icon="table" class="pos-table-actions-meta-icon"></b-icon>
                      <span class="pos-table-actions-meta-label">{{ $t("selectedTable") || "الطاولة المختارة" }}</span>
                      <span class="pos-table-actions-count">{{ selectedTable ? selectedTable.tableNumber : "" }}</span>
                    </template>
                  </div>
                  <div class="pos-table-actions-buttons">
                    <template v-if="mergedTableIds.length > 1">
                      <button class="pos-table-action-btn pos-table-action-save" v-if="carditems.length > 0" @click="addOrder(false)">
                        <b-icon icon="check-circle-fill"></b-icon>
                        <span>{{ $t("saveForAllMergedTables") || "حفظ لجميع الطاولات" }}</span>
                      </button>
                      <button class="pos-table-action-btn pos-table-action-close" v-if="selectedTable && selectedTable.status === 'Occupied'" @click="closeTableOrder(selectedTableId)">
                        <b-icon icon="x-circle-fill"></b-icon>
                        <span>{{ $t("closeAndPrint") || "إغلاق وطباعة" }}</span>
                      </button>
                      <button class="pos-table-action-btn pos-table-action-deselect" @click="deselectTable">
                        <b-icon icon="x-circle-fill"></b-icon>
                        <span>{{ $t("deselectAllMergedTables") || "إلغاء جميع الطاولات" }}</span>
                      </button>
                    </template>
                    <template v-else>
                      <button class="pos-table-action-btn pos-table-action-save" v-if="carditems.length > 0" @click="addOrder(false)">
                        <b-icon icon="check-circle-fill"></b-icon>
                        <span>{{ $t("save") || "حفظ" }}</span>
                      </button>
                      <button class="pos-table-action-btn pos-table-action-close" v-if="selectedTable && selectedTable.status === 'Occupied'" @click="closeTableOrder(selectedTableId)">
                        <b-icon icon="x-circle-fill"></b-icon>
                        <span>{{ $t("closeAndPrint") || "إغلاق وطباعة" }}</span>
                      </button>
                      <button class="pos-table-action-btn pos-table-action-deselect" @click="deselectTable">
                        <b-icon icon="x-circle-fill"></b-icon>
                        <span>{{ $t("deselectTable") || "إلغاء" }}</span>
                      </button>
                    </template>
                  </div>
                </div>
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
                  class="pos-category-btn pos-category-btn-accent"
                  @click="posSelectAllProducts"
                >
                  {{ $t("all") }}
                </button>
                <button
                  v-for="tag in posRootTagsList"
                  :key="tag.id"
                  type="button"
                  class="pos-category-btn"
                  @click="posSelectRoot(tag)"
                >
                  {{ tag.name }}
                </button>
              </div>

              <div v-else-if="posBrowseStep === 'subs'" class="pos-categories-list">
                <button
                  v-for="tag in posSubTagsList"
                  :key="tag.id"
                  type="button"
                  class="pos-category-btn"
                  @click="posSelectSub(tag)"
                >
                  {{ tag.name }}
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
                        height: 60,
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
                    <div class="pos-product-meta">
                      <div class="pos-product-category">
                        <b-icon icon="tags" class="me-1"></b-icon>
                        {{ item.tags }}
                      </div>
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
                    <div class="pos-product-unavailable-badge" v-if="!item.isAvailable">
                      <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                      {{ $t("notAvailable") || "غير متوفر" }}
                    </div>
                  </div>
                </div>
              </div>
            </div>
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

            <!-- Close Table Order Modal -->
            <b-modal id="modal-close-table" :title="$t('confirmCloseTableOrder')" hide-header hide-footer class="users-modal">
              <div class="modal-content-wrapper">
                <div class="delete-confirmation-content">
                  <div class="delete-icon-wrapper">
                    <b-icon icon="door-open" class="delete-warning-icon"></b-icon>
                  </div>
                  <h3 class="delete-confirmation-title">{{ $t("confirmCloseTableOrder") || "إغلاق حساب الطاولة" }}</h3>
                  <p class="delete-confirmation-text">
                    {{ $t("confirmCloseTableOrderMessage") || "اختر الإجراء المطلوب:" }}
                  </p>
                  <div class="table-close-actions">
                    <button class="table-close-action-btn table-close-action-print" @click="closeTableOrderWithPrint">
                      <b-icon icon="printer-fill" class="me-2"></b-icon>
                      {{ $t("closeAndPrint") || "إغلاق وطباعة" }}
                    </button>
                    <button class="table-close-action-btn table-close-action-close" @click="closeTableOrderOnly">
                      <b-icon icon="door-closed" class="me-2"></b-icon>
                      {{ $t("closeOnly") || "إغلاق فقط" }}
                    </button>
                    <button class="delete-cancel-button" @click="closeModel('modal-close-table')">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") || "إلغاء" }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

            <!-- Merge Tables Modal -->
            <b-modal id="modal-merge-tables" :title="$t('mergeTables') || 'دمج طاولات'" hide-header hide-footer class="users-modal">
              <div class="merge-tables-content">
                <div class="merge-tables-info">
                  <p class="merge-tables-message">
                    {{ $t("mergeTablesMessage") || "الطاولات المحددة للدمج:" }}
                  </p>
                  <div class="merge-tables-list">
                    <div 
                      v-for="tableId in selectedTableIds" 
                      :key="tableId"
                      class="merge-table-item"
                    >
                      <b-icon icon="table" class="me-2"></b-icon>
                      <span>{{ getTableNumberById(tableId) }}</span>
                      <button 
                        class="merge-table-remove-btn"
                        @click="removeTableFromSelection(tableId)"
                      >
                        <b-icon icon="x-circle-fill"></b-icon>
                      </button>
                    </div>
                  </div>
                </div>
                <div class="merge-tables-actions">
                  <button class="merge-tables-cancel-btn" @click="closeMergeTablesModal">
                    <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                    {{ $t("cancelButton") || "إلغاء" }}
                  </button>
                  <button 
                    class="merge-tables-confirm-btn" 
                    @click="confirmMergeTables"
                    :disabled="selectedTableIds.length < 2 || loadingMergeTables"
                  >
                    <b-spinner small v-if="loadingMergeTables" class="me-2"></b-spinner>
                    <b-icon v-else icon="layers" class="me-2"></b-icon>
                    {{ loadingMergeTables ? ($t("merging") || "جاري الدمج...") : ($t("confirmMerge") || "تأكيد الدمج") }}
                  </button>
                </div>
              </div>
            </b-modal>

            <!-- Transfer Table Modal -->
            <b-modal id="modal-transfer-table" :title="$t('transferTable') || 'تبديل الطاولة'" hide-header hide-footer class="users-modal">
              <div class="transfer-table-content">
                <div class="transfer-table-info">
                  <p class="transfer-table-message">
                    {{ $t("transferTableMessage") || "اختر الطاولة الجديدة لنقل الطلب من طاولة" }} <strong>{{ getSelectedTableNumber() }}</strong>
                  </p>
                </div>
                <div class="transfer-table-select">
                  <label class="transfer-table-label">
                    <b-icon icon="table" class="me-2"></b-icon>
                    {{ $t("selectNewTable") || "اختر الطاولة الجديدة" }}
                  </label>
                  <select v-model="transferToTableId" class="transfer-table-select-input">
                    <option value="">{{ $t("selectTable") || "اختر طاولة" }}</option>
                    <option 
                      v-for="table in availableTablesForTransfer" 
                      :key="table.id" 
                      :value="table.id"
                    >
                      {{ table.tableNumber }} - {{ table.zone || $t("zone") || "المنطقة" }} ({{ $t(table.status.toLowerCase()) || table.status }})
                    </option>
                  </select>
                </div>
                <div class="transfer-table-actions">
                  <button class="transfer-table-cancel-btn" @click="closeTransferTableModal">
                    <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                    {{ $t("cancelButton") || "إلغاء" }}
                  </button>
                  <button 
                    class="transfer-table-confirm-btn" 
                    @click="confirmTransferTable"
                    :disabled="!transferToTableId || transferToTableId === selectedTableId || loadingTransferTable"
                  >
                    <b-spinner small v-if="loadingTransferTable" class="me-2"></b-spinner>
                    <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
                    {{ loadingTransferTable ? ($t("transferring") || "جاري التبديل...") : ($t("confirmTransfer") || "تأكيد التبديل") }}
                  </button>
                </div>
              </div>
            </b-modal>

            <!-- Order Notes Modal -->
            <b-modal id="modal-order-notes" :title="$t('orderNotes') || 'ملاحظات الطلب'" hide-header hide-footer class="users-modal">
              <div class="modal-content-wrapper">
                <div class="order-notes-content">
                  <div class="order-notes-header">
                    <b-icon icon="file-text" class="me-2"></b-icon>
                    <h3 class="order-notes-title">{{ $t("orderNotes") || "ملاحظات الطلب" }}</h3>
                  </div>
                  <div class="order-notes-input-wrapper">
                    <label class="order-notes-label">{{ $t("notesLabel") || "الملاحظات (اختياري)" }}</label>
                    <textarea
                      v-model="orderForSend.notes"
                      class="order-notes-textarea"
                      :placeholder="$t('notesPlaceholder') || 'اكتب ملاحظاتك هنا...'"
                      rows="4"
                    ></textarea>
                  </div>
                  <div class="order-notes-input-wrapper">
                    <label class="order-notes-label">{{ $t("pagerNumber") || "رقم جهاز النداء (اختياري)" }}</label>
                    <input
                      v-model="orderForSend.pagerNumber"
                      type="text"
                      class="order-notes-input"
                      :placeholder="$t('enterPagerNumber') || 'أدخل رقم جهاز النداء...'"
                    />
                  </div>
                  <div class="order-notes-actions">
                    <button class="order-notes-confirm-button" @click="confirmAddOrder">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("confirmButton") || "تأكيد" }}
                    </button>
                    <button class="order-notes-cancel-button" @click="closeModel('modal-order-notes')">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") || "إلغاء" }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

          <!-- Cart Section -->
          <div
            class="pos-cart-section"
            :class="{ 'pos-cart-section--mobile-open': posMobileCartOpen }"
          >
              <div class="pos-mobile-cart-handle" aria-hidden="true"></div>
              <div class="pos-cart-container" ref="posCartScrollArea">
                <!-- Cart Items List -->
                <div class="pos-cart-items-section">
                  <div class="pos-cart-header">
                    <h3 class="pos-cart-title">
                      <b-icon icon="cart-fill" class="me-2"></b-icon>
                      {{ $t("cart") || 'السلة' }}
                    </h3>
                    <div class="pos-cart-header-actions">
                      <span class="pos-cart-count-badge" v-if="carditems.length > 0">
                        {{ carditems.length }}
                      </span>
                      <button
                        type="button"
                        class="pos-cart-mobile-close-btn"
                        @click.stop="closePosMobileCart"
                        :aria-label="$t('close')"
                        :title="$t('close')"
                      >
                        <b-icon icon="chevron-down"></b-icon>
                      </button>
                    </div>
                  </div>
                  <div
                    class="pos-cart-items-list"
                    v-if="carditems.length > 0"
                    ref="posCartItemsList"
                  >
                    <div
                      class="pos-cart-item"
                      v-for="(item, index) in carditems"
                      :key="index"
                    >
                      <!-- Item Name and Price -->
                      <div class="pos-cart-item-info">
                        <h4 class="pos-cart-item-name">{{ item.name }}</h4>
                        <div class="pos-cart-item-price-row">
                          <span class="pos-cart-item-price">
                            {{ formatPrice((item.disCountPrice > 0 && item.disCountPrice !== item.price) ? item.disCountPrice : (item.price || 0)) }} {{ $t("currency") }}
                          </span>
                          <span class="pos-cart-item-total">
                            {{ formatPrice(item.total) }} {{ $t("currency") }}
                          </span>
                        </div>
                      </div>
                      
                      <!-- Quantity Controls and Delete -->
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
                          class="pos-cart-item-delete"
                          @click.stop="deleteItem(index)"
                          :title="$t('delete') || 'حذف'"
                        >
                          <b-icon icon="x-lg"></b-icon>
                        </button>
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

                <!-- Cart Summary -->
                <div class="pos-cart-summary" v-if="carditems.length > 0">
                  <div class="pos-cart-summary-row">
                    <span class="pos-cart-summary-label">
                      <b-icon icon="box-seam" class="me-2"></b-icon>
                      {{ $t("countLabel") }}:
                    </span>
                    <span class="pos-cart-summary-value">{{ totalCardItems }} {{ $t("itemLabel") }}</span>
                  </div>
                  <div class="pos-cart-summary-row pos-cart-total-row">
                    <span class="pos-cart-summary-label">
                      <b-icon icon="currency-dollar" class="me-2"></b-icon>
                      {{ $t("totalLabel") }}:
                    </span>
                    <span class="pos-cart-summary-value pos-cart-total-value">
                      {{ formattedNumber }} {{ $t("currency") }}
                    </span>
                  </div>
                </div>

                <!-- Order type + payment (accordion on narrow screens) -->
                <div class="pos-mobile-cart-accordion" v-if="carditems.length > 0">
                  <button
                    type="button"
                    class="pos-mobile-cart-accordion-trigger"
                    @click="posCartAccordionOrderOpen = !posCartAccordionOrderOpen"
                    :aria-expanded="posCartAccordionOrderOpen ? 'true' : 'false'"
                  >
                    <span class="pos-mobile-cart-accordion-trigger-label">
                      <b-icon icon="sliders" class="me-2"></b-icon>
                      {{ $t("posCartAccordionOrderPayment") }}
                    </span>
                    <b-icon
                      :icon="posCartAccordionOrderOpen ? 'chevron-up' : 'chevron-down'"
                      class="pos-mobile-cart-accordion-chevron"
                    />
                  </button>
                  <div
                    v-show="posCartAccordionOrderOpen"
                    class="pos-mobile-cart-accordion-panel"
                  >
                    <div class="pos-printer-section">
                      <div class="pos-printer-header">
                        <b-icon icon="shop" class="me-2"></b-icon>
                        <span>{{ $t("orderType") || "نوع الطلب" }}</span>
                      </div>
                      <div class="pos-order-types-grid">
                        <button
                          v-if="selectedTableId"
                          class="pos-order-type-btn pos-order-type-active"
                          disabled
                        >
                          <b-icon icon="house-door" class="pos-order-type-icon"></b-icon>
                          <span class="pos-order-type-label">{{ $t("dineIn") || "داخلي" }}</span>
                        </button>
                        <button
                          v-if="selectedTableId"
                          class="pos-order-type-btn pos-transfer-table-btn"
                          @click="openTransferTableModal"
                          :title="$t('transferTable') || 'تبديل الطاولة'"
                        >
                          <b-icon icon="arrow-left-right" class="pos-order-type-icon"></b-icon>
                          <span class="pos-order-type-label">{{ $t("transferTable") || "تبديل الطاولة" }}</span>
                        </button>
                        <template v-if="!selectedTableId">
                          <button
                            class="pos-order-type-btn"
                            :class="{ 'pos-order-type-active': orderForSend.orderType === 'Takeaway' }"
                            @click="orderForSend.orderType = 'Takeaway'"
                          >
                            <b-icon icon="bag" class="pos-order-type-icon"></b-icon>
                            <span class="pos-order-type-label">{{ $t("takeaway") || "طلب خارجي" }}</span>
                          </button>
                          <button
                            class="pos-order-type-btn"
                            :class="{ 'pos-order-type-active': orderForSend.orderType === 'Delivery' }"
                            @click="orderForSend.orderType = 'Delivery'"
                          >
                            <b-icon icon="truck" class="pos-order-type-icon"></b-icon>
                            <span class="pos-order-type-label">{{ $t("delivery") || "توصيل" }}</span>
                          </button>
                        </template>
                      </div>
                    </div>

                    <div class="pos-printer-section">
                      <div class="pos-printer-header">
                        <b-icon icon="credit-card-fill" class="me-2"></b-icon>
                        <span>{{ $t("paymentMethod") || "طريقة الدفع" }}</span>
                      </div>
                      <div class="pos-payment-methods-grid">
                        <button
                          class="pos-payment-method-btn"
                          :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Cash' }"
                          @click="orderForSend.paymentMethod = 'Cash'"
                        >
                          <b-icon icon="cash-stack" class="pos-payment-icon"></b-icon>
                          <span class="pos-payment-label">{{ $t("cash") || "نقد" }}</span>
                        </button>
                        <button
                          class="pos-payment-method-btn"
                          :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Card' }"
                          @click="orderForSend.paymentMethod = 'Card'"
                        >
                          <b-icon icon="credit-card" class="pos-payment-icon"></b-icon>
                          <span class="pos-payment-label">{{ $t("card") || "بطاقة" }}</span>
                        </button>
                        <button
                          class="pos-payment-method-btn"
                          :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Credit' }"
                          @click="orderForSend.paymentMethod = 'Credit'"
                        >
                          <b-icon icon="clock-history" class="pos-payment-icon"></b-icon>
                          <span class="pos-payment-label">{{ $t("credit") || "دفع لاحق" }}</span>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Delivery (accordion on narrow screens) -->
                <div v-if="orderForSend.orderType === 'Delivery'" class="pos-mobile-cart-accordion">
                  <button
                    type="button"
                    class="pos-mobile-cart-accordion-trigger"
                    @click="posCartAccordionDeliveryOpen = !posCartAccordionDeliveryOpen"
                    :aria-expanded="posCartAccordionDeliveryOpen ? 'true' : 'false'"
                  >
                    <span class="pos-mobile-cart-accordion-trigger-label">
                      <b-icon icon="truck" class="me-2"></b-icon>
                      {{ $t("posCartAccordionDelivery") }}
                    </span>
                    <b-icon
                      :icon="posCartAccordionDeliveryOpen ? 'chevron-up' : 'chevron-down'"
                      class="pos-mobile-cart-accordion-chevron"
                    />
                  </button>
                  <div
                    v-show="posCartAccordionDeliveryOpen"
                    class="pos-mobile-cart-accordion-panel pos-mobile-cart-accordion-panel--delivery"
                  >
                    <div class="delivery-info-section">
                  <h5 class="delivery-section-title">
                    <b-icon icon="truck" class="me-2"></b-icon>
                    {{ $t("deliveryInformation") || "معلومات التوصيل" }}
                  </h5>
                  
                  <form class="users-form">
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
                    
                    <!-- Driver Selection -->
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
                    
                    <!-- Existing Driver Selection -->
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
                    
                    <!-- New Driver Button -->
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
                  </form>
                    </div>
                  </div>
                </div>

                <!-- Printer (accordion on narrow screens) -->
                <div
                  class="pos-mobile-cart-accordion"
                  v-if="availablePrinters.length > 0 || webPrintAPISupported"
                >
                  <button
                    type="button"
                    class="pos-mobile-cart-accordion-trigger"
                    @click="posCartAccordionPrinterOpen = !posCartAccordionPrinterOpen"
                    :aria-expanded="posCartAccordionPrinterOpen ? 'true' : 'false'"
                  >
                    <span class="pos-mobile-cart-accordion-trigger-label">
                      <b-icon icon="printer-fill" class="me-2"></b-icon>
                      {{ $t("posCartAccordionPrinter") }}
                    </span>
                    <b-icon
                      :icon="posCartAccordionPrinterOpen ? 'chevron-up' : 'chevron-down'"
                      class="pos-mobile-cart-accordion-chevron"
                    />
                  </button>
                  <div
                    v-show="posCartAccordionPrinterOpen"
                    class="pos-mobile-cart-accordion-panel"
                  >
                    <div class="pos-printer-section">
                  <div class="pos-printer-header">
                    <b-icon icon="printer-fill" class="me-2"></b-icon>
                    <span>{{ $t("printerSettings") || "إعدادات الطابعة" }}</span>
                  </div>
                  
                  <!-- Web Print API Support Status -->
                  <div class="pos-printer-status" v-if="webPrintAPISupported">
                    <div class="pos-printer-status-badge pos-printer-status-supported">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      <span>{{ $t("webPrintAPISupported") || "المتصفح يدعم الطباعة المباشرة" }}</span>
                    </div>
                  </div>
                  <div class="pos-printer-status" v-else>
                    <div class="pos-printer-status-badge pos-printer-status-not-supported">
                      <b-icon icon="info-circle-fill" class="me-2"></b-icon>
                      <span>{{ $t("webPrintAPINotSupported") || "سيتم استخدام نافذة الطباعة العادية" }}</span>
                    </div>
                  </div>

                  <!-- Printer Selection Dropdown -->
                  <div class="pos-printer-select-wrapper" v-if="availablePrinters.length > 0">
                    <label class="pos-printer-select-label">
                      {{ $t("selectPrinter") || "اختر الطابعة" }}
                    </label>
                    <select 
                      v-model="selectedPrinterId" 
                      @change="onPrinterChange"
                      class="pos-printer-select"
                    >
                      <option 
                        v-for="printer in availablePrinters" 
                        :key="printer.id" 
                        :value="printer.id"
                      >
                        {{ printer.name }} {{ printer.isDefault ? ' (افتراضي)' : '' }}
                      </option>
                    </select>
                  </div>
                  <div class="pos-printer-select-wrapper" v-else-if="webPrintAPISupported">
                    <label class="pos-printer-select-label">
                      {{ $t("loadingPrinters") || "جاري تحميل الطابعات..." }}
                    </label>
                  </div>
                    </div>
                  </div>
                </div>

                <!-- Cart Actions -->
                <div class="pos-cart-actions">
                  <button
                    class="pos-action-btn pos-action-btn-primary"
                    @click="openOrderNotesModal"
                    :disabled="totalCardItems <= 0"
                  >
                    <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                    {{ $t("saveAndClear") || "حفظ وافراغ" }}
                  </button>
                  <button
                    class="pos-action-btn pos-action-btn-danger"
                    v-b-modal.modal-empty
                    :disabled="totalCardItems <= 0"
                  >
                    <b-icon icon="trash-fill" class="me-2"></b-icon>
                    {{ $t("emptyButton") || "افراغ فقط" }}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </b-container>

        <button
          v-show="!posMobileCartOpen"
          type="button"
          class="pos-mobile-cart-fab"
          @click="openPosMobileCart"
          :aria-label="$t('posOpenCart')"
          :title="$t('posOpenCart')"
        >
          <b-icon icon="cart-fill" class="pos-mobile-cart-fab-icon"></b-icon>
          <span v-if="carditems.length > 0" class="pos-mobile-cart-fab-badge">{{ carditems.length }}</span>
        </button>
        <div
          v-if="posMobileCartOpen"
          class="pos-mobile-cart-backdrop"
          @click="closePosMobileCart"
          aria-hidden="true"
        ></div>
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
            src="../assets/logoarabic.png"
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
          {{ $t("mergeTablesHint") || "Ctrl أو ⌘ + نقر لتحديد عدة طاولات للدمج" }}
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
              <div
                class="pos-table-close-compact"
                v-if="table.status === 'Occupied' && !(mergedTableIds.includes(table.id) && mergedTableIds.length > 1)"
                @click.stop="closeTableOrder(table.id)"
              >
                <b-icon icon="x-circle-fill"></b-icon>
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

  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import CalculatorComp from "@/components/CalculatorComp.vue";
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";
import { HTTP } from "../http/api.js";
import { htmlToPaper } from 'vue-html-to-paper';
import signalRService from "../services/signalr.js";
import {
  rootTags,
  childTagsOf,
  tagItemStorageValue,
  tagDisplayName,
} from "@/utils/tagHierarchy.js";
// import store from '../store/store'; // Adjust the path based on your actual folder structure

export default {
  name: "PosView",
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
      doneTypingInterval: 500,
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
        customerOrderItem: [],
        orderType: "Takeaway",
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
        newDriverVehicleNumber: ""
      },
      deliveryDrivers: [],
      loadingDeliveryDrivers: false,
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
      isFullscreen: false,
      showTablesModal: false,
      transferToTableId: null,
      loadingTableOrders: false,
      loadingTransferTable: false,
      loadingMergeTables: false,
      mergedTableIdsCache: {}, // Cache for merged table IDs

      posBrowseStep: "roots",
      posSelectedRoot: null,
      posSelectedSub: null,
      quickSearch: "",
      posSuppressQuickSearchSync: false,
      quickSearchTimer: null,
      posMobileCartOpen: false,
      posCartAccordionOrderOpen: true,
      posCartAccordionDeliveryOpen: true,
      posCartAccordionPrinterOpen: false,
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
      return this.totaPrice.toLocaleString();
    },
    orderDiscount() {
      // Calculate total discount from items if needed
      return 0;
    },
    orderTax() {
      // Calculate tax if needed
      return 0;
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
    tagPrintersMap() {
      // Create a map from tag name to printer ID
      const map = {};
      this.tagPrinters.forEach(tagPrinter => {
        if (tagPrinter.tag && tagPrinter.printer) {
          map[tagPrinter.tag.name] = tagPrinter.printer.id;
        }
      });
      return map;
    },
    mainPrinter() {
      // Get the main printer (IsMain = true)
      return this.managedPrinters.find(p => p.isMain && p.isActive) || null;
    },
    availableTablesForTransfer() {
      // Get tables that are available or can accept transfer (excluding current selected table)
      return this.allTables.filter(table => 
        table.id !== this.selectedTableId && 
        (table.status === 'Available' || table.status === 'Occupied')
      ).sort((a, b) => a.tableNumber - b.tableNumber);
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
        } else {
          // Set default delivery status when switching to Delivery
          if (!this.orderForSend.deliveryStatus) {
            this.orderForSend.deliveryStatus = "Pending";
          }
          this.posCartAccordionDeliveryOpen = true;
        }
      }
    },
    quickSearch(newVal, oldVal) {
      if (this.posSuppressQuickSearchSync) {
        return;
      }
      clearTimeout(this.quickSearchTimer);
      this.quickSearchTimer = setTimeout(() => {
        this.posBrowseStep = "products";
        this.posSelectedRoot = null;
        this.posSelectedSub = null;
        this.search.info = newVal;
        this.GetAllItems();
      }, this.doneTypingInterval);
    },
    posMobileCartOpen(val) {
      if (typeof document === "undefined") return;
      document.body.style.overflow = val ? "hidden" : "";
    },

  },

  mounted() {
    try {
      // Load fullscreen state from localStorage
      const savedFullscreen = localStorage.getItem('posFullscreen');
      if (savedFullscreen === 'true') {
        this.isFullscreen = true;
      }

      this.getTags();
      this.$nextTick(() => {
        if (this.$refs.posQuickSearchInput) {
          this.$refs.posQuickSearchInput.focus();
        }
      });
      this.getTables();
      
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
      
      // Load delivery drivers
      this.loadDeliveryDrivers();
      
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
    clearTimeout(this.quickSearchTimer);
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
    getTags() {
      HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`)
        .then((response) => {
          this.tags = response.data.data.items;
        })
        .catch((error) => {
          this.$toast.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
        });
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
      HTTP.get("Tables")
        .then((response) => {
          // Ensure we always have an array
          const data = response.data.data.items;
          console.log("data", data);
          this.allTables = Array.isArray(data) ? data : [];
          this.availableTables = Array.isArray(this.allTables) 
            ? this.allTables.filter(t => t.status === "Available" || t.status === "Occupied")
            : [];
        })
        .catch((error) => {
          console.error('Error loading tables:', error);
          // Set to empty array on error to prevent further issues
          this.allTables = [];
          this.availableTables = [];
        });
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
      if (
        !multi &&
        (table.status === "Available" || table.status === "Occupied")
      ) {
        this.showTablesModal = false;
      }
    },
    async selectTable(table, event) {
      // Check if Ctrl or Cmd key is pressed for multi-select
      const isMultiSelect = event && (event.ctrlKey || event.metaKey);
      
      if (isMultiSelect && (table.status === 'Occupied' || table.status === 'Available')) {
        // Multi-select mode for merging tables
        if (!this.selectedTableIds.includes(table.id)) {
          this.selectedTableIds.push(table.id);
        } else {
          this.selectedTableIds = this.selectedTableIds.filter(id => id !== table.id);
        }
        return;
      }
      
      // Single select mode (existing behavior)
      if (table.status === 'Occupied') {
        // Load table orders
        this.loadingTableOrders = true;
        try {
          const response = await HTTP.get(`Admin/GetTableOrders?tableId=${table.id}`);
          this.tableOrders = response.data.data || [];
          
          // Load items from orders into cart
          this.carditems = [];
          this.tableOrders.forEach(order => {
            if (order.customerOrderItem) {
              order.customerOrderItem.forEach(orderItem => {
                if (orderItem.item) {
                  const existingItem = this.carditems.find(item => item.id === orderItem.item.id);
                  if (existingItem) {
                    existingItem.quantity += orderItem.quantity;
                    // Update total for existing item - ensure prices are valid
                    const price = existingItem.price || 0;
                    const disCountPrice = existingItem.disCountPrice || 0;
                    const finalPrice = (disCountPrice > 0 && disCountPrice !== price) ? disCountPrice : price;
                    existingItem.total = finalPrice * existingItem.quantity;
                  } else {
                    // Calculate final price - ensure prices are valid numbers
                    const sellingPrice = orderItem.sellingPrice || 0;
                    const discountPrice = orderItem.item.disCountPrice || 0;
                    const finalPrice = (discountPrice > 0 && discountPrice !== sellingPrice) ? discountPrice : sellingPrice;
                    
                    this.carditems.push({
                      id: orderItem.item.id,
                      name: orderItem.item.name,
                      price: sellingPrice,
                      disCountPrice: discountPrice,
                      quantity: orderItem.quantity || 1,
                      code: orderItem.item.code,
                      image: orderItem.item.image,
                      total: finalPrice * (orderItem.quantity || 1),
                      tags: orderItem.item.tags || 'مواد اخرى' // Add tags from order item
                    });
                  }
                }
              });
            }
          });
          
          this.selectedTableId = table.id;
          
          // Get merged tables for this table
          const mergedIds = await this.loadMergedTableIds(table.id);
          // Ensure mergedIds is an array
          const mergedIdsArray = Array.isArray(mergedIds) ? mergedIds : [table.id];
          this.selectedTableIds = mergedIdsArray; // Select all merged tables
          
          // Ensure orderForSend has the correct table IDs
          if (mergedIdsArray.length > 1) {
            this.orderForSend.tableIds = [...mergedIdsArray];
            this.orderForSend.tableId = mergedIdsArray[0]; // First table for backward compatibility
          } else {
          this.orderForSend.tableId = table.id;
            this.orderForSend.tableIds = null;
          }
          this.orderForSend.orderType = 'DineIn';
          
          this.$toast.success(this.$i18n.t("tableOrdersLoaded") || "تم تحميل طلبات الطاولة", {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
        } catch (error) {
          console.error('Error loading table orders:', error);
          this.$toast.error(this.$i18n.t("errorLoadingTableOrders") || "خطأ في تحميل طلبات الطاولة", {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
        } finally {
          this.loadingTableOrders = false;
        }
      } else if (table.status === 'Available') {
        // Start new order for available table
        this.selectedTableId = table.id;
        this.selectedTableIds = [table.id]; // Reset multi-select
        this.orderForSend.tableId = table.id;
        this.orderForSend.orderType = 'DineIn';
        this.carditems = [];
        this.tableOrders = [];
        
        this.$toast.info(this.$i18n.t("newTableOrderStarted") || "تم بدء طلب جديد للطاولة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      }
    },
    async closeTableOrder(tableId) {
      // If merged tables, use mergedTableIds, otherwise use single tableId
      const tablesToClose = this.mergedTableIds.length > 1 ? this.mergedTableIds : [tableId];
      this.tableToClose = tableId; // Keep for modal display
      this.tablesToClose = tablesToClose; // Store all tables to close
      this.$bvModal.show('modal-close-table');
    },
    async closeTableOrderWithPrint() {
      if (!this.tableToClose) {
        return;
      }
      
      const tableId = this.tableToClose;
      
      // Print first if there are items
      if (this.carditems.length > 0) {
        try {
          await this.printCard();
          // Wait a bit for print dialog to open
          await new Promise(resolve => setTimeout(resolve, 500));
        } catch (error) {
          console.error('Error printing:', error);
        }
      }
      
      // Then close table (don't clear cart)
      await this.performCloseTableOrder(tableId, false);
    },
    async closeTableOrderOnly() {
      if (!this.tableToClose) {
        return;
      }
      
      const tableId = this.tableToClose;
      await this.performCloseTableOrder(tableId, false);
    },
    async performCloseTableOrder(tableId, clearCart = false) {
      this.$bvModal.hide('modal-close-table');
      
      try {
        // Use tablesToClose if available (for merged tables), otherwise use single tableId
        const tablesToClose = this.tablesToClose && this.tablesToClose.length > 1 
          ? this.tablesToClose 
          : [tableId];
        
        let response;
        if (tablesToClose.length > 1) {
          // Multiple tables - send as body
          response = await HTTP.put(`Admin/CloseTableOrder`, tablesToClose);
        } else {
          // Single table - use query parameter for backward compatibility
          response = await HTTP.put(`Admin/CloseTableOrder?tableId=${tableId}`);
        }
        
        this.selectedTableId = null;
        this.selectedTableIds = [];
        this.orderForSend.tableId = null;
        this.orderForSend.tableIds = null;
        this.orderForSend.orderType = 'Takeaway'; // Reset to default when closing table
        if (clearCart) {
          this.carditems = [];
        }
        this.tableOrders = [];
        this.tableToClose = null;
        this.tablesToClose = null;
        await this.getTables();
        
        const message = tablesToClose.length > 1 
          ? (this.$i18n.t("mergedTablesClosed") || `تم إغلاق حساب ${tablesToClose.length} طاولات بنجاح`)
          : (this.$i18n.t("tableOrderClosed") || "تم إغلاق حساب الطاولة بنجاح");
        
        this.$toast.success(message, {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      } catch (error) {
        console.error('Error closing table order:', error);
        this.$toast.error(this.$i18n.t("errorClosingTableOrder") || "خطأ في إغلاق حساب الطاولة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      }
    },
    async deselectTable() {
      // If we have merged tables selected, deselect all of them
      if (this.selectedTableIds.length > 1) {
        // Clear all merged tables
        this.selectedTableIds = [];
      this.selectedTableId = null;
      this.orderForSend.tableId = null;
        this.orderForSend.orderType = 'Takeaway';
        this.carditems = [];
        this.tableOrders = [];
        
        this.$toast.info(this.$i18n.t("allMergedTablesDeselected") || "تم إلغاء اختيار جميع الطاولات المدمجة", {
          position: "top-right",
          timeout: 1500,
          maxToasts: 1,
        });
      } else {
        // Single table deselection
        this.selectedTableId = null;
        this.selectedTableIds = [];
        this.orderForSend.tableId = null;
        this.orderForSend.orderType = 'Takeaway';
      this.carditems = [];
      this.tableOrders = [];
      
      this.$toast.info(this.$i18n.t("tableDeselected") || "تم إلغاء اختيار الطاولة", {
        position: "top-right",
        timeout: 1500,
        maxToasts: 1,
      });
      }
    },
    openMergeTablesModal() {
      if (this.selectedTableIds.length < 2) {
        this.$toast.warning(this.$i18n.t("selectAtLeastTwoTables") || "يرجى اختيار طاولتين على الأقل للدمج", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
        return;
      }
      this.$bvModal.show('modal-merge-tables');
    },
    closeMergeTablesModal() {
      this.$bvModal.hide('modal-merge-tables');
    },
    removeTableFromSelection(tableId) {
      this.selectedTableIds = this.selectedTableIds.filter(id => id !== tableId);
      if (this.selectedTableIds.length === 0) {
        this.closeMergeTablesModal();
      }
    },
    getTableNumberById(tableId) {
      const table = this.allTables.find(t => t.id === tableId);
      return table ? table.tableNumber : '';
    },
    async confirmMergeTables() {
      if (this.selectedTableIds.length < 2) {
        this.$toast.warning(this.$i18n.t("selectAtLeastTwoTables") || "يرجى اختيار طاولتين على الأقل للدمج", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
        return;
      }

      this.loadingMergeTables = true;
      try {
        const response = await HTTP.post('Admin/MergeTables', this.selectedTableIds);
        
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(response.data.message || this.$i18n.t("tablesMergedSuccess") || "تم دمج الطاولات بنجاح", {
            position: "top-right",
            timeout: 2500,
            maxToasts: 1,
          });
          
          // Refresh tables
          await this.getTables();
          
          // Load the merged order if order ID is available
          if (response.data.data && response.data.data.id) {
            // Find the primary table (first table in selectedTableIds)
            const primaryTableId = this.selectedTableIds[0];
            
            // Wait a bit for tables to refresh
            await new Promise(resolve => setTimeout(resolve, 500));
            
            // Refresh tables to get updated status
            await this.getTables();
            
            const primaryTable = this.allTables.find(t => t.id === primaryTableId);
            
            if (primaryTable) {
              // Select the primary table to load its order (this will load items into cart)
              await this.selectTable(primaryTable);
            }
          } else {
            // No order ID, just refresh tables
            await this.getTables();
          }
          
          // Reset multi-select
          this.selectedTableIds = [];
          
          this.closeMergeTablesModal();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("mergeTablesFailed") || "فشل دمج الطاولات", {
            position: "top-right",
            timeout: 2500,
            maxToasts: 1,
          });
        }
      } catch (error) {
        console.error('Error merging tables:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("mergeTablesError") || "حدث خطأ أثناء دمج الطاولات", {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
      } finally {
        this.loadingMergeTables = false;
      }
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
    addOrderAndClear() {
      const textDirection = document.documentElement.dir;
      const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      
      // Validate Delivery information if order type is Delivery
      if (this.orderForSend.orderType === 'Delivery') {
        if (!this.orderForSend.deliveryCustomerName || !this.orderForSend.deliveryCustomerName.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterCustomerName") || "يرجى إدخال اسم المستلم", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
        if (!this.orderForSend.deliveryPhoneNumber || !this.orderForSend.deliveryPhoneNumber.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterPhoneNumber") || "يرجى إدخال رقم هاتف المستلم", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
        if (!this.orderForSend.deliveryAddress || !this.orderForSend.deliveryAddress.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterDeliveryAddress") || "يرجى إدخال عنوان التوصيل", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
        if (!this.orderForSend.deliveryDriverId) {
          this.$toast.error(this.$i18n.t("pleaseSelectDriver") || "يرجى اختيار سائق", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
      }
      
      this.show = true;
      this.orderForSend.orderCode = "";
      this.orderForSend.paymentMethod = this.orderForSend.paymentMethod || "Cash";
      this.orderForSend.customerOrderItem = [];
      for (const item of this.carditems) {
        this.orderForSend.customerOrderItem.push({
          itemId: item.id,
          quantity: item.quantity,
        });
      }
      this.orderForSend.orderCode = Math.floor(
        Math.random() * 1000000000
      ).toString().padStart(9, '0');
      
      // Handle multiple tables or single table
      // For merged tables, use selectedTableIds if it contains multiple tables (from merged selection)
      // Otherwise check mergedTableIds computed property
      let tableIdsToUse = [];
      
      // Check if we have multiple selected tables (from merged selection)
      if (this.selectedTableIds.length > 1) {
        tableIdsToUse = [...this.selectedTableIds];
      } 
      // Check mergedTableIds computed property (for already merged tables)
      else if (this.mergedTableIds.length > 1) {
        tableIdsToUse = [...this.mergedTableIds];
      }
      // Fallback to single table
      else if (this.selectedTableId) {
        tableIdsToUse = [this.selectedTableId];
      }
      
      if (tableIdsToUse.length > 1) {
        // Multiple tables - use TableIds
        this.orderForSend.tableIds = [...tableIdsToUse];
        this.orderForSend.tableId = tableIdsToUse[0]; // First table for backward compatibility
      } else if (tableIdsToUse.length === 1) {
        // Single table - use TableId
        this.orderForSend.tableId = tableIdsToUse[0];
        this.orderForSend.tableIds = null;
      } else {
        // No table selected
        this.orderForSend.tableId = null;
        this.orderForSend.tableIds = null;
      }
      
      if (!this.orderForSend.reservationId) {
        this.orderForSend.reservationId = null;
      }
      
      // Clear Delivery fields if not Delivery order
      if (this.orderForSend.orderType !== 'Delivery') {
        this.orderForSend.deliveryDriverId = null;
        this.orderForSend.deliveryStatus = null;
        this.orderForSend.deliveryAddress = null;
        this.orderForSend.deliveryPhoneNumber = null;
        this.orderForSend.deliveryCustomerName = null;
        this.orderForSend.deliveryFee = null;
        this.orderForSend.newDriverName = null;
        this.orderForSend.newDriverPhone = null;
        this.orderForSend.newDriverAddress = null;
        this.orderForSend.newDriverVehicleType = null;
        this.orderForSend.newDriverVehicleNumber = null;
      } else {
        // Set delivery status if not set
        if (!this.orderForSend.deliveryStatus) {
          this.orderForSend.deliveryStatus = "Pending";
        }
        // Clear new driver fields (no longer used, drivers are added via modal)
        this.orderForSend.newDriverName = null;
        this.orderForSend.newDriverPhone = null;
        this.orderForSend.newDriverAddress = null;
        this.orderForSend.newDriverVehicleType = null;
        this.orderForSend.newDriverVehicleNumber = null;
      }
      
      HTTP.post(`Admin/AddOrder`, this.orderForSend)
        .then((response) => {
          if (response) {
            this.show = false;
            // Save a copy of carditems for printing before clearing
            const itemsForPrint = JSON.parse(JSON.stringify(this.carditems));
            // Save tableId before clearing
            const tableIdToUpdate = this.selectedTableId;
            // Clear cart after successful save
            this.carditems = [];
            this.selectedTableId = null;
            this.selectedTableIds = [];
            this.orderForSend.tableId = null;
            this.orderForSend.tableIds = null;
            this.orderForSend.orderType = 'Takeaway'; // Reset to default when clearing
            this.orderForSend.notes = ""; // Reset notes
            this.orderForSend.pagerNumber = ""; // Reset pager number
            this.tableOrders = [];
            
            // Update table status to Available if table was selected
            if (tableIdToUpdate) {
              const tableToUpdate = this.allTables.find(t => t.id === tableIdToUpdate);
              if (tableToUpdate) {
                HTTP.put(`Tables/${tableIdToUpdate}`, {
                  tableNumber: tableToUpdate.tableNumber,
                  capacity: tableToUpdate.capacity,
                  zone: tableToUpdate.zone,
                  notes: tableToUpdate.notes,
                  status: 'Available'
                })
                  .then(() => {
                    // Refresh tables after status update
                    this.getTables();
                  })
                  .catch((error) => {
                    console.error('Error updating table status:', error);
                    // Still refresh tables even if update fails
                    this.getTables();
                  });
              } else {
                this.getTables();
              }
            } else {
              this.getTables();
            }
            
            this.$toast.success(this.$i18n.t("orderSavedAndCleared") || "تم حفظ الطلب وافراغ السلة بنجاح", {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            });
            
            // Print automatically after saving
            setTimeout(() => {
              try {
                this.printCard(itemsForPrint);
              } catch (printError) {
                console.error('Print error:', printError);
                // Don't show error to user, printing is optional
                // The order was saved successfully
              }
            }, 100);
          }
        })
        .catch((error) => {
          this.show = false;
          console.error('Order save error:', error);
          let errorMessage = this.$i18n.t("error") || "حدث خطأ ما";
          
          if (error.response) {
            if (error.response.data && error.response.data.message) {
              errorMessage = error.response.data.message;
            } else if (error.response.status === 400) {
              errorMessage = this.$i18n.t("badRequest") || "طلب غير صحيح";
            } else if (error.response.status === 401) {
              errorMessage = this.$i18n.t("unauthorized") || "غير مصرح";
            } else if (error.response.status === 500) {
              errorMessage = this.$i18n.t("serverError") || "خطأ في الخادم";
            }
          } else if (error.request) {
            errorMessage = this.$i18n.t("networkError") || "خطأ في الاتصال بالخادم";
          }
          
          this.$toast.error(errorMessage, {
            position: "top-right",
            timeout: 3000,
            maxToasts: 1,
          });
        });
    },
    addOrder(isPrint) {
      const textDirection = document.documentElement.dir;
      const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      
      // Validate Delivery information if order type is Delivery
      if (this.orderForSend.orderType === 'Delivery') {
        if (!this.orderForSend.deliveryCustomerName || !this.orderForSend.deliveryCustomerName.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterCustomerName") || "يرجى إدخال اسم المستلم", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
        if (!this.orderForSend.deliveryPhoneNumber || !this.orderForSend.deliveryPhoneNumber.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterPhoneNumber") || "يرجى إدخال رقم هاتف المستلم", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
        if (!this.orderForSend.deliveryAddress || !this.orderForSend.deliveryAddress.trim()) {
          this.$toast.error(this.$i18n.t("pleaseEnterDeliveryAddress") || "يرجى إدخال عنوان التوصيل", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
        if (!this.orderForSend.deliveryDriverId) {
          this.$toast.error(this.$i18n.t("pleaseSelectDriver") || "يرجى اختيار سائق", {
            position: toastPosition,
            timeout: 2500,
            maxToasts: 1,
          });
          return;
        }
      }
      
      this.show = true;
      this.orderForSend.orderCode = "";
      this.orderForSend.paymentMethod = this.orderForSend.paymentMethod || "Cash";
      this.orderForSend.customerOrderItem = [];
      for (const item of this.carditems) {
        this.orderForSend.customerOrderItem.push({
          itemId: item.id,
          quantity: item.quantity,
        });
      }
      this.orderForSend.orderCode = Math.floor(
        Math.random() * 1000000000
      ).toString().padStart(9, '0');
      
      // Handle multiple tables or single table
      // For merged tables, use selectedTableIds if it contains multiple tables (from merged selection)
      // Otherwise check mergedTableIds computed property
      let tableIdsToUse = [];
      
      // Check if we have multiple selected tables (from merged selection)
      if (this.selectedTableIds.length > 1) {
        tableIdsToUse = [...this.selectedTableIds];
      } 
      // Check mergedTableIds computed property (for already merged tables)
      else if (this.mergedTableIds.length > 1) {
        tableIdsToUse = [...this.mergedTableIds];
      }
      // Fallback to single table
      else if (this.selectedTableId) {
        tableIdsToUse = [this.selectedTableId];
      }
      
      if (tableIdsToUse.length > 1) {
        // Multiple tables - use TableIds
        this.orderForSend.tableIds = [...tableIdsToUse];
        this.orderForSend.tableId = tableIdsToUse[0]; // First table for backward compatibility
      } else if (tableIdsToUse.length === 1) {
        // Single table - use TableId
        this.orderForSend.tableId = tableIdsToUse[0];
        this.orderForSend.tableIds = null;
      } else {
        // No table selected
        this.orderForSend.tableId = null;
        this.orderForSend.tableIds = null;
      }
      
      if (!this.orderForSend.reservationId) {
        this.orderForSend.reservationId = null;
      }
      
      // Clear Delivery fields if not Delivery order
      if (this.orderForSend.orderType !== 'Delivery') {
        this.orderForSend.deliveryDriverId = null;
        this.orderForSend.deliveryStatus = null;
        this.orderForSend.deliveryAddress = null;
        this.orderForSend.deliveryPhoneNumber = null;
        this.orderForSend.deliveryCustomerName = null;
        this.orderForSend.deliveryFee = null;
        this.orderForSend.newDriverName = null;
        this.orderForSend.newDriverPhone = null;
        this.orderForSend.newDriverAddress = null;
        this.orderForSend.newDriverVehicleType = null;
        this.orderForSend.newDriverVehicleNumber = null;
      } else {
        // Set delivery status if not set
        if (!this.orderForSend.deliveryStatus) {
          this.orderForSend.deliveryStatus = "Pending";
        }
        // Clear new driver fields (no longer used, drivers are added via modal)
        this.orderForSend.newDriverName = null;
        this.orderForSend.newDriverPhone = null;
        this.orderForSend.newDriverAddress = null;
        this.orderForSend.newDriverVehicleType = null;
        this.orderForSend.newDriverVehicleNumber = null;
      }
      
      HTTP.post(`Admin/AddOrder`, this.orderForSend)
        .then((response) => {
          if (response) {
            this.show = false;
            this.$toast.warning(this.$i18n.t("addOrderSucsses"), {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            });
            // Save a copy of carditems for printing before clearing
            const itemsForPrint = JSON.parse(JSON.stringify(this.carditems));
            this.carditems = [];
            if (this.$refs.posQuickSearchInput) {
              this.$refs.posQuickSearchInput.focus();
            }
            
            // Refresh tables after order is added
            this.getTables();
            
            if (isPrint) {
              // Use setTimeout to ensure print happens after UI updates
              setTimeout(() => {
                try {
                  this.printCard(itemsForPrint);
                } catch (printError) {
                  console.error('Print error:', printError);
                  // Don't show error to user, printing is optional
                  // The order was saved successfully
                }
              }, 100);
            }
          }
        })
        .catch((error) => {
          this.show = false;
          console.error('Order save error:', error);
          let errorMessage = this.$i18n.t("error") || "حدث خطأ ما";
          
          if (error.response) {
            // Server responded with error status
            if (error.response.data && error.response.data.message) {
              errorMessage = error.response.data.message;
            } else if (error.response.status === 400) {
              errorMessage = this.$i18n.t("badRequest") || "طلب غير صحيح";
            } else if (error.response.status === 401) {
              errorMessage = this.$i18n.t("unauthorized") || "غير مصرح";
            } else if (error.response.status === 500) {
              errorMessage = this.$i18n.t("serverError") || "خطأ في الخادم";
            }
          } else if (error.request) {
            // Request was made but no response received
            errorMessage = this.$i18n.t("networkError") || "خطأ في الاتصال بالخادم";
          }
          
          this.$toast.error(errorMessage, {
            position: "top-right",
            timeout: 3000,
            maxToasts: 1,
          });
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
              price: item.price ? item.price.toLocaleString() : '0',
              total: item.total ? item.total.toLocaleString() : '0',
              discount: item.discount || null
            })),
            subtotal: this.totaPrice.toLocaleString(),
            discount: '0',
            tax: '0',
            total: this.totaPrice.toLocaleString(),
            paymentMethod: this.orderForSend.paymentMethod === 'Cash' ? 'نقدي' : 
                          this.orderForSend.paymentMethod === 'Card' ? 'بطاقة' : 
                          this.orderForSend.paymentMethod || 'نقدي'
          };
          
          // Get HTML content if needed
          await this.$nextTick();
          const printElement = document.getElementById("print");
          if (printElement) {
            printData.htmlContent = printElement.innerHTML;
          }
        }
        
        // Send to Python print server with timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 seconds timeout
        
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
      // Group items by their tags and map to printers
      const grouped = {};
      const tagPrintersMap = this.tagPrintersMap;
      
      items.forEach(item => {
        const tagName = item.tags || 'مواد اخرى';
        const printerId = tagPrintersMap[tagName];
        
        if (printerId) {
          // يوجد printer محدد لهذا tag
          if (!grouped[tagName]) {
            grouped[tagName] = {
              items: [],
              printerId: printerId,
              tagName: tagName
            };
          }
          grouped[tagName].items.push(item);
        } else {
          // لا يوجد printer محدد - إضافة إلى default
          if (!grouped['default']) {
            grouped['default'] = {
              items: [],
              printerId: null,
              tagName: 'default'
            };
          }
          grouped['default'].items.push(item);
        }
      });
      
      return grouped;
    },
    generateHTMLForItems(items, tagName = null) {
      // Calculate totals for this group
      const subtotal = items.reduce((sum, item) => sum + (item.total || 0), 0);
      const totalItems = items.reduce((sum, item) => sum + (item.quantity || 0), 0);
      
      // Get print element HTML structure
      const printElement = document.getElementById("print");
      if (!printElement) {
        return '';
      }
      
      // Clone the structure but replace items table with filtered items
      let htmlContent = printElement.innerHTML;
      
      // Create items table HTML for this group
      let itemsTableHTML = `
        <table data-v-f8758d62="" class="bill-table">
          <thead data-v-f8758d62="">
            <tr data-v-f8758d62="" class="bill-table-header">
              <th data-v-f8758d62="" class="bill-table-cell bill-col-item">طبق/مشروب</th>
              <th data-v-f8758d62="" class="bill-table-cell bill-col-qty">العدد</th>
              <th data-v-f8758d62="" class="bill-table-cell bill-col-price">السعر</th>
              <th data-v-f8758d62="" class="bill-table-cell bill-col-total">المجموع</th>
            </tr>
          </thead>
          <tbody data-v-f8758d62="">
      `;
      
      items.forEach(item => {
        const itemPrice = item.price !== item.disCountPrice ? item.disCountPrice : item.price;
        itemsTableHTML += `
          <tr data-v-f8758d62="" class="bill-table-row">
            <td data-v-f8758d62="" class="bill-table-cell bill-col-item">${this.escapeHtml(item.name || '')}</td>
            <td data-v-f8758d62="" class="bill-table-cell bill-col-qty">${item.quantity || 0}</td>
            <td data-v-f8758d62="" class="bill-table-cell bill-col-price">${itemPrice ? itemPrice.toLocaleString() : '0'}</td>
            <td data-v-f8758d62="" class="bill-table-cell bill-col-total">${item.total ? item.total.toLocaleString() : '0'}</td>
          </tr>
        `;
      });
      
      itemsTableHTML += `
          </tbody>
        </table>
      `;
      
      // Replace the items table in HTML
      const tableRegex = /<table[^>]*class="bill-table"[^>]*>[\s\S]*?<\/table>/i;
      htmlContent = htmlContent.replace(tableRegex, itemsTableHTML);
      
      // Update summary section
      const summaryRegex = /<div[^>]*class="bill-summary-section"[^>]*>[\s\S]*?<\/div>/i;
      const summaryHTML = `
        <div data-v-f8758d62="" class="bill-summary-section">
          <div data-v-f8758d62="" class="bill-summary-row">
            <span data-v-f8758d62="" class="bill-summary-label">العدد:</span>
            <span data-v-f8758d62="" class="bill-summary-value">${totalItems} طبق/مشروب</span>
          </div>
          ${tagName && tagName !== 'default' ? `
          <div data-v-f8758d62="" class="bill-summary-row">
            <span data-v-f8758d62="" class="bill-summary-label">القسم:</span>
            <span data-v-f8758d62="" class="bill-summary-value">${this.escapeHtml(tagName)}</span>
          </div>
          ` : ''}
          <div data-v-f8758d62="" class="bill-summary-row bill-total-row">
            <span data-v-f8758d62="" class="bill-summary-label">المجموع:</span>
            <span data-v-f8758d62="" class="bill-summary-value bill-total-amount">${subtotal.toLocaleString()} د.ع</span>
          </div>
        </div>
      `;
      htmlContent = htmlContent.replace(summaryRegex, summaryHTML);
      
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
      
      return htmlContent;
    },
    escapeHtml(text) {
      const div = document.createElement('div');
      div.textContent = text;
      return div.innerHTML;
    },
    async printItemsByTag(tagName, items, printerId) {
      try {
        // Calculate totals for this group
        const subtotal = items.reduce((sum, item) => sum + (item.total || 0), 0);
        const totalItems = items.reduce((sum, item) => sum + (item.quantity || 0), 0);
        
        // Find printer details from managedPrinters
        const printer = this.managedPrinters.find(p => p.id === printerId);
        const printerName = printer ? printer.printerName : null;
        const printerType = printer ? printer.printerType : 'windows';
        
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
          items: items.map(item => ({
            name: item.name || '',
            quantity: item.quantity || 0,
            price: item.price ? item.price.toLocaleString() : '0',
            total: item.total ? item.total.toLocaleString() : '0',
            discount: item.discount || null
          })),
          subtotal: subtotal.toLocaleString(),
          discount: '0',
          tax: '0',
          total: subtotal.toLocaleString(),
          paymentMethod: this.orderForSend.paymentMethod === 'Cash' ? 'نقدي' : 
                        this.orderForSend.paymentMethod === 'Card' ? 'بطاقة' : 
                        this.orderForSend.paymentMethod || 'نقدi'
        };
        
        // Generate HTML content for this group
        const htmlContent = this.generateHTMLForItems(items, tagName);
        printData.htmlContent = htmlContent;
        
        if (printerId && printerName) {
          // Print to specific printer via backend API
          try {
            const response = await HTTP.post(`Printers/${printerId}/print`, {
              htmlContent: htmlContent,
              copies: 1
            });
            
            if (response.data && !response.data.errorStatus) {
              console.log(`Successfully printed ${tagName} items to printer ${printerName} (ID: ${printerId})`);
              return true;
            } else {
              console.warn(`Failed to print ${tagName} items to printer ${printerId}:`, response.data?.message);
              // Fallback to Python print server with printer name
              printData.printerName = printerName;
              printData.printerType = printerType;
              return await this.printWithPythonServer(items, printData);
            }
          } catch (error) {
            console.error(`Error printing ${tagName} items to printer ${printerId}:`, error);
            // Fallback to Python print server with printer name
            printData.printerName = printerName;
            printData.printerType = printerType;
            return await this.printWithPythonServer(items, printData);
          }
        } else {
          // No specific printer - use default Python print server
          return await this.printWithPythonServer(items);
        }
      } catch (error) {
        console.error(`Error in printItemsByTag for ${tagName}:`, error);
        return false;
      }
    },
    async printCard(itemsToPrint = null) {
      try {
        // Use provided items or fallback to current carditems
        const printItems = itemsToPrint || this.carditems;
        
        // Temporarily replace carditems for printing if needed
        const originalCarditems = this.carditems;
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
          if (itemsToPrint) {
            this.carditems = originalCarditems;
          }
          return;
        }

        // Professional print styles optimized for POS printers (58mm/80mm) - Unified with Reports design
        const stylesHtml = `
    <style>
      @page {
        size: 80mm auto;
        margin: 0;
      }
      
      * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
      }
      
      body {
        font-family: 'Cairo', 'Arial', sans-serif;
        direction: rtl;
        font-size: 11px;
        line-height: 1.3;
        color: #000;
        background: #fff;
        padding: 5mm;
        width: 80mm;
      }
      
      .bill-container {
        width: 100%;
        max-width: 80mm;
        margin: 0 auto;
      }
      
      .bill-header {
        text-align: center;
        margin-bottom: 8px;
        padding-bottom: 8px;
        border-bottom: 1px dashed #000;
      }
      
      .bill-logo-img {
        max-width: 50px;
        height: auto;
        margin-bottom: 4px;
      }
      
      .bill-store-name {
        font-size: 16px;
        font-weight: 800;
        margin: 4px 0 2px 0;
        color: #000;
      }
      
      .bill-store-subtitle {
        font-size: 9px;
        color: #666;
        margin: 0;
      }
      
      .bill-info-section {
        margin: 8px 0;
        font-size: 10px;
      }
      
      .bill-info-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 3px;
      }
      
      .bill-info-label {
        font-weight: 600;
      }
      
      .bill-info-value {
        font-weight: 400;
      }
      
      .bill-barcode-section {
        text-align: center;
        margin: 8px 0;
        padding: 4px 0;
      }
      
      .bill-barcode-img {
        max-width: 100%;
        height: auto;
        display: block;
        margin: 0 auto;
      }
      
      .bill-divider {
        border-top: 1px dashed #000;
        margin: 8px 0;
      }
      
      .bill-items-section {
        margin: 8px 0;
      }
      
      .bill-items-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 10px;
      }
      
      .bill-items-table thead {
        border-bottom: 1px solid #000;
      }
      
      .bill-items-table th {
        padding: 4px 2px;
        text-align: right;
        font-weight: 700;
        font-size: 9px;
      }
      
      .bill-item-name-col {
        width: 40%;
      }
      
      .bill-item-qty-col {
        width: 15%;
        text-align: center;
      }
      
      .bill-item-price-col {
        width: 20%;
        text-align: left;
      }
      
      .bill-item-total-col {
        width: 25%;
        text-align: left;
      }
      
      .bill-items-table td {
        padding: 3px 2px;
        vertical-align: top;
      }
      
      .bill-item-name {
        font-weight: 500;
        word-break: break-word;
      }
      
      .bill-discount-badge {
        display: block;
        font-size: 7px;
        color: #dc2626;
        font-weight: 600;
        margin-top: 2px;
      }
      
      .bill-item-qty {
        text-align: center;
        font-weight: 600;
      }
      
      .bill-item-price {
        text-align: left;
        font-size: 9px;
      }
      
      .bill-price-discounted {
        display: block;
      }
      
      .bill-original-price {
        display: block;
        text-decoration: line-through;
        color: #999;
        font-size: 8px;
      }
      
      .bill-discount-price {
        display: block;
        color: #dc2626;
        font-weight: 600;
      }
      
      .bill-item-total {
        text-align: left;
        font-weight: 700;
      }
      
      .bill-summary-section {
        margin: 8px 0;
        font-size: 11px;
      }
      
      .bill-summary-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 4px;
      }
      
      .bill-summary-label {
        font-weight: 600;
      }
      
      .bill-summary-value {
        font-weight: 400;
      }
      
      .bill-summary-total {
        border-top: 1px solid #000;
        padding-top: 4px;
        margin-top: 4px;
        font-size: 12px;
      }
      
      .bill-summary-total .bill-summary-label {
        font-weight: 700;
        font-size: 13px;
      }
      
      .bill-summary-total .bill-summary-value {
        font-weight: 800;
        font-size: 13px;
      }
      
      .bill-notes-section {
        margin-top: 12px;
        padding-top: 8px;
      }
      
      .bill-notes-content {
        margin-bottom: 8px;
        padding: 6px 0;
      }
      
      .bill-notes-label {
        font-weight: 600;
        font-size: 10px;
        margin-bottom: 4px;
        color: #000;
      }
      
      .bill-notes-text {
        font-size: 10px;
        color: #333;
        line-height: 1.4;
        word-wrap: break-word;
      }
      
      .bill-footer {
        text-align: center;
        margin-top: 12px;
        padding-top: 8px;
        border-top: 1px dashed #000;
      }
      
      .bill-footer-text {
        font-size: 9px;
        margin: 2px 0;
        color: #666;
      }
      
      @media print {
        body {
          padding: 0;
        }
        
        .bill-container {
          width: 80mm;
        }
      }
    </style>
  `;

        // Step 1: Print full receipt to main printer (if exists)
        if (this.mainPrinter) {
          try {
            console.log('Printing full receipt to main printer:', this.mainPrinter.name);
            // Prepare full receipt data
            await this.$nextTick();
            const printElement = document.getElementById("print");
            if (printElement) {
              const fullReceiptHtml = printElement.innerHTML;
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
                  price: item.price ? item.price.toLocaleString() : '0',
                  total: item.total ? item.total.toLocaleString() : '0',
                  discount: item.discount || null
                })),
                subtotal: this.totaPrice.toLocaleString(),
                discount: '0',
                tax: '0',
                total: this.totaPrice.toLocaleString(),
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
              });
              
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
          // Group items by tags
          const groupedItems = this.groupItemsByTag(printItems);
          const tagGroups = Object.keys(groupedItems);
          
          if (tagGroups.length > 0) {
            let allPrintSuccess = true;
            let hasTagPrinters = false;
            
            // Print each group to its assigned printer
            for (const tagName of tagGroups) {
              const group = groupedItems[tagName];
              
              if (group.items.length > 0) {
                if (group.printerId) {
                  // Print to specific printer for this tag
                  hasTagPrinters = true;
                  const printSuccess = await this.printItemsByTag(tagName, group.items, group.printerId);
                  if (!printSuccess) {
                    allPrintSuccess = false;
                  }
                } else {
                  // No printer assigned - skip (already printed to main printer)
                  console.log(`No printer assigned for tag "${tagName}", skipping (already printed to main printer)`);
                }
              }
            }
            
            if (hasTagPrinters || this.mainPrinter) {
              // Restore original carditems if we changed it
              if (itemsToPrint) {
                this.carditems = originalCarditems;
              }
              this.$toast.success(this.$i18n.t("printSuccess") || 'تم الطباعة بنجاح', {
                position: "top-right",
                timeout: 2000,
                maxToasts: 1,
              });
              return; // Success - exit early
            }
          }
        } catch (tagPrintError) {
          console.warn('Tag-based printing error, trying fallback methods:', tagPrintError);
          // Fall through to other print methods
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
          const invoiceTitle = (this.$t("invoice_number") || "فاتورة") + ' - ' + (this.orderForSend.orderCode || 'Invoice');
          const htmlContent = '<!DOCTYPE html><html><head><title>' + invoiceTitle +
            '</title><meta charset="UTF-8">' + stylesHtml +
            '</head><body>' + printElement.innerHTML + '</body></html>';
          
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
        if (itemsToPrint) {
          this.carditems = originalCarditems;
        }
        // Silently fail - order was saved successfully, printing is optional
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
      const stylesHtml = `
    <style>
      @page {
        size: 80mm auto;
        margin: 0;
      }
      
      * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
      }
      
      body {
        font-family: 'Cairo', 'Arial', sans-serif;
        direction: rtl;
        font-size: 11px;
        line-height: 1.3;
        color: #000;
        background: #fff;
        padding: 5mm;
        width: 80mm;
      }
      
      .bill-container {
        width: 100%;
        max-width: 80mm;
        margin: 0 auto;
      }
      
      .bill-header {
        text-align: center;
        margin-bottom: 8px;
        padding-bottom: 8px;
        border-bottom: 1px dashed #000;
      }
      
      .bill-logo-img {
        max-width: 50px;
        height: auto;
        margin-bottom: 4px;
      }
      
      .bill-store-name {
        font-size: 16px;
        font-weight: 800;
        margin: 4px 0 2px 0;
        color: #000;
      }
      
      .bill-store-subtitle {
        font-size: 9px;
        color: #666;
        margin: 0;
      }
      
      .bill-info-section {
        margin: 8px 0;
        font-size: 10px;
      }
      
      .bill-info-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 3px;
      }
      
      .bill-info-label {
        font-weight: 600;
      }
      
      .bill-info-value {
        font-weight: 400;
      }
      
      .bill-barcode-section {
        text-align: center;
        margin: 8px 0;
        padding: 4px 0;
      }
      
      .bill-barcode-img {
        max-width: 100%;
        height: auto;
        display: block;
        margin: 0 auto;
      }
      
      .bill-divider {
        border-top: 1px dashed #000;
        margin: 8px 0;
      }
      
      .bill-items-section {
        margin: 8px 0;
      }
      
      .bill-items-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 10px;
      }
      
      .bill-items-table thead {
        border-bottom: 1px solid #000;
      }
      
      .bill-items-table th {
        padding: 4px 2px;
        text-align: right;
        font-weight: 700;
        font-size: 9px;
      }
      
      .bill-item-name-col {
        width: 40%;
      }
      
      .bill-item-qty-col {
        width: 15%;
        text-align: center;
      }
      
      .bill-item-price-col {
        width: 20%;
        text-align: left;
      }
      
      .bill-item-total-col {
        width: 25%;
        text-align: left;
      }
      
      .bill-items-table td {
        padding: 3px 2px;
        vertical-align: top;
      }
      
      .bill-item-name {
        font-weight: 500;
        word-break: break-word;
      }
      
      .bill-discount-badge {
        display: block;
        font-size: 7px;
        color: #dc2626;
        font-weight: 600;
        margin-top: 2px;
      }
      
      .bill-item-qty {
        text-align: center;
        font-weight: 600;
      }
      
      .bill-item-price {
        text-align: left;
        font-size: 9px;
      }
      
      .bill-price-discounted {
        display: block;
      }
      
      .bill-original-price {
        display: block;
        text-decoration: line-through;
        color: #999;
        font-size: 8px;
      }
      
      .bill-discount-price {
        display: block;
        color: #dc2626;
        font-weight: 600;
      }
      
      .bill-item-total {
        text-align: left;
        font-weight: 700;
      }
      
      .bill-summary-section {
        margin: 8px 0;
        font-size: 11px;
      }
      
      .bill-summary-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 4px;
      }
      
      .bill-summary-label {
        font-weight: 600;
      }
      
      .bill-summary-value {
        font-weight: 400;
      }
      
      .bill-summary-total {
        border-top: 1px solid #000;
        padding-top: 4px;
        margin-top: 4px;
        font-size: 12px;
      }
      
      .bill-summary-total .bill-summary-label {
        font-weight: 700;
        font-size: 13px;
      }
      
      .bill-summary-total .bill-summary-value {
        font-weight: 800;
        font-size: 13px;
      }
      
      .bill-notes-section {
        margin-top: 12px;
        padding-top: 8px;
      }
      
      .bill-notes-content {
        margin-bottom: 8px;
        padding: 6px 0;
      }
      
      .bill-notes-label {
        font-weight: 600;
        font-size: 10px;
        margin-bottom: 4px;
        color: #000;
      }
      
      .bill-notes-text {
        font-size: 10px;
        color: #333;
        line-height: 1.4;
        word-wrap: break-word;
      }
      
      .bill-footer {
        text-align: center;
        margin-top: 12px;
        padding-top: 8px;
        border-top: 1px dashed #000;
      }
      
      .bill-footer-text {
        font-size: 9px;
        margin: 2px 0;
        color: #666;
      }
      
      @media print {
        body {
          padding: 0;
        }
        
        .bill-container {
          width: 80mm;
        }
      }
    </style>
  `;

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
      this.carditems = [];
      this.$bvModal.hide(id);
      // Reset table selection and order type when clearing cart
      if (this.selectedTableId) {
        this.selectedTableId = null;
        this.orderForSend.tableId = null;
        this.orderForSend.orderType = 'Takeaway'; // Default to Takeaway when no table
      }
      if (this.$refs.posQuickSearchInput) {
        this.$refs.posQuickSearchInput.focus();
      }
    },
    closeModel(id) {
      this.$bvModal.hide(id);
    },
    openOrderNotesModal() {
      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      // Reset notes before opening modal
      this.orderForSend.notes = "";
      this.orderForSend.pagerNumber = "";
      this.$bvModal.show('modal-order-notes');
    },
    confirmAddOrder() {
      this.$bvModal.hide('modal-order-notes');
      // Call the actual add order function
      this.addOrderAndClear();
    },
    addToCartList(item) {
      try {
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
        const existingItemIndex = this.carditems.findIndex(cartItem => cartItem.id === item.id);
        
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

        if (this.$refs.posQuickSearchInput) {
          this.$refs.posQuickSearchInput.focus();
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

    deleteItem(index) {
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
                  this.selectedTableId = null;
                  this.orderForSend.tableId = null;
                  this.orderForSend.orderType = 'Takeaway';
                  this.carditems = [];
                  this.tableOrders = [];
                }
              }
            }
          });

          // Listen for order transfers
          signalRService.on('OrderTransferred', (data) => {
            console.log('Order transferred via SignalR:', data);
            if (!data) return;
            // Refresh tables list
            this.getTables();
            // If the order was transferred from currently selected table, update selection
            if (this.selectedTableId === data.FromTableId) {
              const newTable = this.allTables.find(t => t.id === data.ToTableId);
              if (newTable) {
                this.selectedTableId = newTable.id;
                this.orderForSend.tableId = newTable.id;
                // Reload table orders
                this.selectTable(newTable);
              }
            }
            // Show notification
            this.$toast.info(
              `${this.$i18n.t('orderTransferred') || 'تم نقل الطلب'} من طاولة ${data.FromTableNumber} إلى طاولة ${data.ToTableNumber}`,
              {
                position: "top-right",
                timeout: 3000,
                maxToasts: 1,
              }
            );
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
      signalRService.off('OrderTransferred');
    },
    toggleFullscreen() {
      this.isFullscreen = !this.isFullscreen;
      localStorage.setItem('posFullscreen', this.isFullscreen);
      
      // Show notification
      const message = this.isFullscreen 
        ? (this.$i18n.t('fullscreenEnabled') || 'تم تفعيل الوضع الكامل')
        : (this.$i18n.t('fullscreenDisabled') || 'تم إلغاء الوضع الكامل');
      
      this.$toast.info(message, {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    },
    getSelectedTableNumber() {
      if (!this.selectedTableId) return '';
      const table = this.allTables.find(t => t.id === this.selectedTableId);
      return table ? table.tableNumber : '';
    },

    posClearSuppressAndQuickSearch() {
      clearTimeout(this.quickSearchTimer);
      this.quickSearchTimer = null;
      this.posSuppressQuickSearchSync = true;
      this.quickSearch = "";
      this.$nextTick(() => {
        this.posSuppressQuickSearchSync = false;
      });
    },

    posSelectAllProducts() {
      this.posClearSuppressAndQuickSearch();
      this.posBrowseStep = "products";
      this.posSelectedRoot = null;
      this.posSelectedSub = null;
      this.search.info = "";
      this.GetAllItems();
    },

    posSelectRoot(root) {
      if (!root) return;
      const subs = childTagsOf(root, this.tags);
      this.posClearSuppressAndQuickSearch();
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
      this.posClearSuppressAndQuickSearch();
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
        this.posClearSuppressAndQuickSearch();
        this.Items = [];
        return;
      }
      if (this.posSelectedRoot) {
        this.posBrowseStep = "roots";
        this.posSelectedRoot = null;
        this.search.info = "";
        this.posClearSuppressAndQuickSearch();
        this.Items = [];
        return;
      }
      this.posBrowseStep = "roots";
      this.search.info = "";
      this.posClearSuppressAndQuickSearch();
      this.Items = [];
    },
    openTransferTableModal() {
      this.transferToTableId = null;
      this.$root.$emit('bv::show::modal', 'modal-transfer-table');
    },
    closeTransferTableModal() {
      this.transferToTableId = null;
      this.$root.$emit('bv::hide::modal', 'modal-transfer-table');
    },
    async confirmTransferTable() {
      if (!this.transferToTableId || this.transferToTableId === this.selectedTableId) {
        this.$toast.warning(this.$i18n.t('pleaseSelectDifferentTable') || 'يرجى اختيار طاولة مختلفة', {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
        return;
      }

      try {
        this.loadingTransferTable = true;
        const response = await HTTP.put(`Admin/TransferTable?fromTableId=${this.selectedTableId}&toTableId=${this.transferToTableId}`);
        
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(response.data.message || this.$i18n.t('tableTransferredSuccessfully') || 'تم تبديل الطاولة بنجاح', {
            position: "top-right",
            timeout: 3000,
            maxToasts: 1,
          });

          // Update selected table to new table
          const newTable = this.allTables.find(t => t.id === this.transferToTableId);
          if (newTable) {
            this.selectedTableId = newTable.id;
            this.orderForSend.tableId = newTable.id;
            // Reload table orders
            await this.selectTable(newTable);
          }

          // Refresh tables
          await this.getTables();
          
          this.closeTransferTableModal();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t('errorTransferringTable') || 'حدث خطأ أثناء تبديل الطاولة', {
            position: "top-right",
            timeout: 3000,
            maxToasts: 1,
          });
        }
      } catch (error) {
        console.error('Error transferring table:', error);
        this.$toast.error(this.$i18n.t('errorTransferringTable') || 'حدث خطأ أثناء تبديل الطاولة', {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } finally {
        this.loadingTransferTable = false;
      }
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

/* Tables: one card (picker + optional actions row) */
.pos-tables-block {
  border: 1px solid var(--border-color, rgba(255, 255, 255, 0.1));
  border-radius: 0.75rem;
  overflow: hidden;
  background: var(--bg-tertiary, #1e1e2e);
}

.pos-tables-picker-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem 1rem;
  padding: 0.75rem 1rem;
  background: transparent;
  border: none;
  border-radius: 0;
}

.pos-tables-picker-main {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
  flex: 1;
}

.pos-tables-picker-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
  flex-shrink: 0;
}

.pos-tables-picker-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
  gap: 0.15rem;
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

.pos-tables-picker-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  flex-shrink: 0;
}

.pos-tables-open-modal-btn {
  display: inline-flex;
  align-items: center;
  padding: 0.5rem 0.9rem;
  border-radius: 0.5rem;
  border: 1px solid var(--primary-color);
  background: rgba(129, 140, 248, 0.15);
  color: var(--primary-color);
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.pos-tables-open-modal-btn:hover {
  background: var(--primary-color);
  color: #fff;
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

.pos-table-merged-actions {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-top: 0.75rem;
  width: 100%;
  padding-top: 0.5rem;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.pos-table-save-compact {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #ffffff;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.3s ease;
  font-size: 0.875rem;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);
  width: 100%;
  border: none;
  white-space: nowrap;
}

.pos-table-save-compact:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
}

.pos-table-save-compact b-icon {
  font-size: 1.125rem;
  flex-shrink: 0;
}

.pos-table-save-compact span {
  flex: 1;
  text-align: center;
}

.pos-table-merged-actions .pos-table-deselect-compact {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.3s ease;
  font-size: 0.875rem;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.3);
  width: 100%;
  border: none;
  white-space: nowrap;
}

.pos-table-merged-actions .pos-table-deselect-compact:hover {
  background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.4);
}

.pos-table-merged-actions .pos-table-deselect-compact b-icon {
  font-size: 1.125rem;
  flex-shrink: 0;
}

.pos-table-merged-actions .pos-table-deselect-compact span {
  flex: 1;
  text-align: center;
}

/* Ensure merged table card has enough space for buttons */
.pos-table-card-compact.pos-table-merged {
  min-height: auto;
  padding-bottom: 0.75rem;
}

.pos-table-card-compact.pos-table-merged .pos-table-merged-actions {
  position: relative;
  z-index: 10;
}

/* Hide regular deselect button when merged actions are shown */
.pos-table-card-compact.pos-table-merged .pos-table-deselect-compact:not(.pos-table-merged-actions .pos-table-deselect-compact) {
  display: none;
}

/* Ensure zone text doesn't overlap with buttons */
.pos-table-card-compact.pos-table-merged .pos-table-zone-compact {
  margin-bottom: 0.25rem;
}

/* Table actions: second row inside .pos-tables-block */
.pos-table-actions-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem 1rem;
  padding: 0.6rem 1rem 0.75rem;
  border-top: 1px solid var(--border-color, rgba(255, 255, 255, 0.08));
  background: var(--bg-secondary, rgba(0, 0, 0, 0.2));
}

.pos-table-actions-meta {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  min-width: 0;
  flex: 1 1 12rem;
}

.pos-table-actions-meta-icon {
  font-size: 1.125rem;
  color: var(--primary-color);
  flex-shrink: 0;
}

.pos-table-actions-meta-label {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--text-secondary);
  white-space: nowrap;
}

.pos-table-actions-count {
  background: var(--primary-color);
  color: #ffffff;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  line-height: 1.2;
  flex-shrink: 0;
}

.pos-table-actions-buttons {
  display: flex;
  gap: 0.45rem;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  flex: 1 1 auto;
}

.pos-table-action-btn {
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

.pos-table-action-save {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);
}

.pos-table-action-save:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
}

.pos-table-action-close {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.3);
}

.pos-table-action-close:hover {
  background: linear-gradient(135deg, #d97706 0%, #b45309 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.4);
}

.pos-table-action-deselect {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.3);
}

.pos-table-action-deselect:hover {
  background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.4);
}

@media (max-width: 768px) {
  .pos-table-actions-bar {
    flex-direction: column;
    align-items: stretch;
  }

  .pos-table-actions-buttons {
    justify-content: stretch;
  }

  .pos-table-action-btn {
    flex: 1 1 auto;
    min-width: 0;
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

[dir="rtl"] .pos-cart-item {
  flex-direction: row;
}

[dir="rtl"] .pos-cart-item-price-row {
  flex-direction: row;
}

[dir="rtl"] .pos-cart-item-controls {
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

/* Responsive Design */
@media (max-width: 768px) {
  .pos-cart-section {
    border-radius: 0;
    border-left: none;
    border-right: none;
  }
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

