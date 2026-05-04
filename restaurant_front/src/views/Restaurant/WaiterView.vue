<template>
  <div class="waiter-view-container">
    <b-overlay
      :show="show"
      spinner-variant="primary"
      spinner-type="grow"
      spinner-large
      rounded="sm"
    >
      <div class="waiter-main-wrapper">
        <b-container fluid class="waiter-container-fluid">
          <div class="waiter-page-container">
            <!-- Left Side: Tables and Products -->
            <div class="waiter-main-section">
              <!-- Header Section -->
              <div class="waiter-header-section">
                <div class="waiter-header-top">
                  <div class="waiter-logo-section">
                    <img src="../../assets/logoarabic.png" alt="logo" class="waiter-logo" />
                  </div>
                  <div class="waiter-employee-info">
                    <b-icon icon="person-circle" class="me-2"></b-icon>
                    <span class="waiter-employee-label">{{ $t("employeeLabel") }}</span>
                    <span class="waiter-employee-name">{{ userInfo.name }}</span>
                  </div>
                  <div class="waiter-logout-section">
                    <button class="waiter-logout-btn" @click="logout" :title="$t('Logout') || 'تسجيل الخروج'">
                      <b-icon icon="box-arrow-right" class="waiter-logout-icon"></b-icon>
                      <span class="waiter-logout-text">{{ $t("Logout") || "تسجيل الخروج" }}</span>
                    </button>
                  </div>
                </div>
              </div>

              <!-- Quick Actions Bar -->
              <div class="waiter-quick-actions">
                <div class="waiter-quick-search">
                  <b-icon icon="search" class="waiter-quick-search-icon"></b-icon>
                  <input
                    v-model="search.info"
                    type="search"
                    :placeholder="$t('searchPlaceholder')"
                    class="waiter-quick-search-input"
                  />
                </div>
                <div class="waiter-quick-barcode">
                  <b-icon icon="upc-scan" class="me-2"></b-icon>
                  <input
                    v-model="searchCode"
                    ref="codeNumber"
                    type="search"
                    :placeholder="$t('itemCodeLabel')"
                    class="waiter-quick-barcode-input"
                    autofocus
                    @keyup.enter="handleBarcodeSearch"
                  />
                </div>
              </div>

              <!-- Tables Section -->
              <div class="waiter-tables-section-compact">
                <b-overlay
                  :show="loadingTableOrders"
                  spinner-variant="primary"
                  spinner-type="border"
                  spinner-small
                  rounded="sm"
                  opacity="0.6"
                >
                  <div class="waiter-tables-header-compact">
                  <div class="waiter-tables-title">
                    <b-icon icon="table" class="me-2"></b-icon>
                    <span>{{ $t("tables") || "الطاولات" }}</span>
                    <span class="waiter-tables-count">({{ filteredTables.length }})</span>
                  </div>
                  <button 
                    v-if="selectedTableIds.length > 1" 
                    class="waiter-merge-tables-btn-compact" 
                    @click="openMergeTablesModal"
                    :title="$t('mergeTables') || 'دمج طاولات'"
                  >
                    <b-icon icon="layers"></b-icon>
                    <span>{{ $t("mergeTables") || "دمج" }}</span>
                  </button>
                  <router-link
                    to="/restaurant/table-layout"
                    class="waiter-floor-plan-link"
                    :title="$t('tableFloorPlanTitle') || ''"
                  >
                    <b-icon icon="columns-gap"></b-icon>
                  </router-link>
                  <button class="waiter-refresh-tables-btn-compact" @click="getTables" :title="$t('refresh') || 'تحديث'">
                    <b-icon icon="arrow-clockwise"></b-icon>
                  </button>
                </div>
                
                <!-- Tables Filters -->
                <div class="waiter-tables-filters">
                  <div class="waiter-table-filter-group">
                    <label class="waiter-table-filter-label">
                      <b-icon icon="geo-alt-fill" class="me-1"></b-icon>
                      {{ $t("zone") || "الموقع" }}
                    </label>
                    <select v-model="tableFilters.zone" class="waiter-table-filter-select">
                      <option value="">{{ $t("allZones") || "جميع المواقع" }}</option>
                      <option v-for="zone in uniqueZones" :key="zone" :value="zone">{{ zone }}</option>
                    </select>
                  </div>
                  <div class="waiter-table-filter-group">
                    <label class="waiter-table-filter-label">
                      <b-icon icon="hash" class="me-1"></b-icon>
                      {{ $t("tableNumber") || "رقم الطاولة" }}
                    </label>
                    <input
                      v-model="tableFilters.tableNumber"
                      type="number"
                      :placeholder="$t('searchTableNumber') || 'ابحث برقم الطاولة'"
                      class="waiter-table-filter-input"
                    />
                  </div>
                  <div class="waiter-table-filter-group">
                    <label class="waiter-table-filter-label">
                      <b-icon icon="filter" class="me-1"></b-icon>
                      {{ $t("status") || "الحالة" }}
                    </label>
                    <select v-model="tableFilters.status" class="waiter-table-filter-select">
                      <option value="">{{ $t("allStatuses") || "جميع الحالات" }}</option>
                      <option value="Available">{{ $t("available") || "متاحة" }}</option>
                      <option value="Occupied">{{ $t("occupied") || "مشغولة" }}</option>
                      <option value="Reserved">{{ $t("reserved") || "محجوزة" }}</option>
                      <option value="OutOfService">{{ $t("outOfService") || "خارج الخدمة" }}</option>
                    </select>
                  </div>
                  <button 
                    v-if="tableFilters.zone || tableFilters.tableNumber || tableFilters.status"
                    class="waiter-table-filter-clear"
                    @click="clearTableFilters"
                  >
                    <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                    {{ $t("clearFilters") || "مسح الفلاتر" }}
                  </button>
                </div>
                
                <!-- Tables Cards -->
                <div class="waiter-tables-scroll">
                  <div 
                    v-for="table in filteredTables" 
                    :key="table.id"
                    class="waiter-table-card-compact"
                    :class="{
                      'waiter-table-available': table.status === 'Available',
                      'waiter-table-occupied': table.status === 'Occupied',
                      'waiter-table-reserved': table.status === 'Reserved',
                      'waiter-table-selected': selectedTableId === table.id || (selectedTableIds && selectedTableIds.includes(table.id)),
                      'waiter-table-multi-selected': (selectedTableIds && selectedTableIds.includes(table.id) && selectedTableIds.length > 1),
                      'waiter-table-merged': (mergedTableIds && mergedTableIds.includes(table.id) && mergedTableIds.length > 1)
                    }"
                    @click="selectTable(table, $event)"
                  >
                    <div class="waiter-table-number-compact">
                      <span v-if="table.mergedTableNumbers">{{ table.mergedTableNumbers }}</span>
                      <span v-else>{{ table.tableNumber }}</span>
                    </div>
                    <div class="waiter-table-status-compact" :class="`waiter-table-status-${table.status.toLowerCase()}`">
                      {{ getTableStatusText(table.status) }}
                    </div>
                    <div class="waiter-table-zone-compact" v-if="table.zone">
                      {{ table.zone }}
                    </div>
                    <div class="waiter-table-deselect-compact" v-if="selectedTableId === table.id" @click.stop="deselectTable">
                      <b-icon icon="x-circle-fill"></b-icon>
                      <span>{{ $t("deselectTable") || "إلغاء" }}</span>
                    </div>
                  </div>
                </div>
                </b-overlay>
              </div>

              <!-- Categories Section -->
              <div class="waiter-categories-scroll">
                <div class="waiter-categories-list">
                  <button
                    v-for="tag in tags"
                    :key="tag.id"
                    class="waiter-category-btn"
                    :class="{ 'waiter-category-btn-active': search.info === tag.name }"
                    @click="search.info = tag.name"
                  >
                    {{ tag.name }}
                  </button>
                  <button
                    class="waiter-category-btn"
                    :class="{ 'waiter-category-btn-active': search.info === '' }"
                    @click="search.info = ''"
                  >
                    {{ $t("all") }}
                  </button>
                </div>
              </div>

              <!-- Products Grid -->
              <div class="waiter-products-grid-section">
                <div class="waiter-products-grid">
                  <div
                    class="waiter-product-card"
                    :class="{ 'waiter-product-card-unavailable': !item.isAvailable }"
                    v-for="item in Items"
                    :key="item.id"
                    @click="item.isAvailable ? addToCartList(item) : null"
                  >
                    <!-- Discount Badge -->
                    <div
                      v-if="item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
                      class="waiter-product-discount-badge"
                    >
                      <b-icon icon="tag-fill" class="me-1"></b-icon>
                      {{ $t("discountLabel") }}
                    </div>

                    <!-- Product Image/Barcode -->
                    <div class="waiter-product-media">
                      <div v-if="item.image && !item.imageError" class="waiter-product-image-container">
                        <img
                          :src="item.image"
                          :alt="item.name"
                          class="waiter-product-image"
                          @error="item.imageError = true"
                        />
                      </div>
                      <div v-else class="waiter-product-image-placeholder">
                        <b-icon icon="box-fill" class="waiter-product-placeholder-icon"></b-icon>
                      </div>
                    </div>

                    <!-- Product Info -->
                    <div class="waiter-product-info">
                      <h4 class="waiter-product-name">{{ item.name }}</h4>
                      <div class="waiter-product-meta">
                        <div class="waiter-product-category">
                          <b-icon icon="tags" class="me-1"></b-icon>
                          {{ item.tags }}
                        </div>
                        <div class="waiter-product-price">
                          <div
                            v-if="item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
                            class="waiter-product-price-discounted"
                          >
                            <span class="waiter-product-price-current">
                              {{ formatPrice(item.disCountPrice) }} {{ $t("currency") }}
                            </span>
                            <span class="waiter-product-price-old">
                              {{ formatPrice(item.sellingPrice) }} {{ $t("currency") }}
                            </span>
                          </div>
                          <div v-else class="waiter-product-price-regular">
                            {{ formatPrice(item.sellingPrice) }} {{ $t("currency") }}
                          </div>
                        </div>
                      </div>
                      <div class="waiter-product-unavailable-badge" v-if="!item.isAvailable">
                        <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                        {{ $t("notAvailable") || "غير متوفر" }}
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Pagination -->
                <div class="waiter-pagination-section">
                  <b-pagination
                    v-model="pageNumber"
                    :total-rows="totalItems"
                    :per-page="pageSize"
                    aria-controls="waiter-products"
                    class="waiter-pagination"
                  >
                  </b-pagination>
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

            <!-- Merge Tables Modal -->
            <b-modal id="modal-merge-tables" :title="$t('mergeTables') || 'دمج الطاولات'" hide-header hide-footer class="users-modal">
              <div class="merge-tables-content">
                <div class="merge-tables-info">
                  <b-icon icon="layers" class="merge-tables-icon"></b-icon>
                  <h3 class="merge-tables-title">{{ $t("selectTablesToMerge") || "اختر الطاولات للدمج" }}</h3>
                  <p class="merge-tables-message">
                    {{ $t("mergeTablesMessage") || "سيتم دمج الطاولات المحددة في طلب واحد. الطاولات المحددة:" }}
                  </p>
                </div>
                <div class="merge-tables-list">
                  <div 
                    v-for="tableId in selectedTableIds" 
                    :key="tableId"
                    class="merge-table-item"
                  >
                    <div class="merge-table-info">
                      <b-icon icon="table" class="me-2"></b-icon>
                      <span>{{ getTableNumberById(tableId) }}</span>
                    </div>
                    <button 
                      class="merge-table-remove-btn"
                      @click="removeTableFromSelection(tableId)"
                      :title="$t('removeTable') || 'إزالة الطاولة'"
                    >
                      <b-icon icon="x-circle-fill"></b-icon>
                    </button>
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

            <!-- Order Notes Modal -->
            <b-modal id="modal-transfer-table" :title="$t('transferTable') || 'تبديل الطاولة'" hide-header hide-footer class="users-modal">
              <div class="transfer-table-content">
                <div class="transfer-table-info">
                  <p class="transfer-table-message">
                    {{ $t("transferTableMessage") || "اختر الطاولة الجديدة لنقل الطلب من طاولة" }} <strong>{{ selectedTableNumber }}</strong>
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
                    :disabled="!transferToTableId || transferToTableId === selectedTableId"
                  >
                    <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                    {{ $t("confirmTransfer") || "تأكيد التبديل" }}
                  </button>
                </div>
              </div>
            </b-modal>

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
                    src="../../assets/logoarabic.png"
                    alt="logo"
                    class="bill-logo-img"
                  />
                  <h2 class="bill-store-name">{{ commercialUserInfo.restaurantName || 'LiteCashier' }}</h2>
                  <p class="bill-store-subtitle">{{ $t("app-name") }}</p>
                </div>

                <div class="bill-divider"></div>

                <!-- Order Info Section -->
                <div class="bill-info-section">
                  <div class="bill-info-row" v-if="orderForSend.orderCode">
                    <span class="bill-info-label">{{ $t("invoice_number") }}:</span>
                    <span class="bill-info-value">{{ orderForSend.orderCode }}</span>
                  </div>
                  <div class="bill-info-row">
                    <span class="bill-info-label">{{ $t("from_date") }}:</span>
                    <span class="bill-info-value">{{ getCurrentDateTime() }}</span>
                  </div>
                  <div class="bill-info-row" v-if="orderForSend.orderType">
                    <span class="bill-info-label">{{ $t("orderType") }}:</span>
                    <span class="bill-info-value">{{ getOrderTypeText(orderForSend.orderType) }}</span>
                  </div>
                  <div class="bill-info-row" v-if="orderForSend.paymentMethod">
                    <span class="bill-info-label">{{ $t("paymentMethod") }}:</span>
                    <span class="bill-info-value">{{ getPaymentMethodText(orderForSend.paymentMethod) }}</span>
                  </div>
                  <div class="bill-info-row" v-if="selectedTableNumber">
                    <span class="bill-info-label">{{ $t("table") || "الطاولة" }}:</span>
                    <span class="bill-info-value">{{ selectedTableNumber }}</span>
                  </div>
                  <div class="bill-info-row">
                    <span class="bill-info-label">{{ $t("employeeLabel") }}:</span>
                    <span class="bill-info-value">{{ userInfo.name || userInfo.fullName || '---' }}</span>
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

                <!-- Footer Section -->
                <div class="bill-footer">
                  <p class="bill-footer-text">شكراً لزيارتكم</p>
                  <p class="bill-footer-text">Thank you for your visit</p>
                </div>
              </div>
            </div>

            <!-- Cart Section -->
            <div class="waiter-cart-section">
              <div class="waiter-cart-container">
                <!-- Selected Table Info -->
                <div class="waiter-selected-table-info" v-if="selectedTableId">
                  <div class="waiter-selected-table-header">
                    <b-icon icon="table" class="me-2"></b-icon>
                    <span>{{ $t("selectedTable") || "الطاولة المختارة" }}: {{ selectedTableNumber }}</span>
                  </div>
                  <button 
                    class="waiter-transfer-table-btn" 
                    @click="openTransferTableModal"
                    :title="$t('transferTable') || 'تبديل الطاولة'"
                  >
                    <b-icon icon="arrow-left-right" class="me-2"></b-icon>
                    <span>{{ $t("transferTable") || "تبديل الطاولة" }}</span>
                  </button>
                </div>

                <!-- Cart Items List -->
                <div class="waiter-cart-items-section">
                  <div class="waiter-cart-header">
                    <h3 class="waiter-cart-title">
                      <b-icon icon="cart-fill" class="me-2"></b-icon>
                      {{ $t("cart") || 'السلة' }}
                    </h3>
                    <span class="waiter-cart-count-badge" v-if="carditems.length > 0">
                      {{ carditems.length }}
                    </span>
                  </div>
                  <div class="waiter-cart-items-list" v-if="carditems.length > 0">
                    <div
                      class="waiter-cart-item"
                      v-for="(item, index) in carditems"
                      :key="index"
                    >
                      <div class="waiter-cart-item-info">
                        <h4 class="waiter-cart-item-name">{{ item.name }}</h4>
                        <div class="waiter-cart-item-price">
                          {{ formatPrice((item.disCountPrice && item.disCountPrice > 0 && item.disCountPrice < item.price) ? item.disCountPrice : item.price) }} {{ $t("currency") }}
                        </div>
                      </div>
                      <div class="waiter-cart-item-controls">
                        <div class="waiter-cart-item-quantity">
                          <button
                            class="waiter-quantity-btn waiter-quantity-decrease"
                            @click.stop="decreaseQuantity(index)"
                          >
                            <b-icon icon="dash"></b-icon>
                          </button>
                          <input
                            type="number"
                            :value="item.quantity"
                            @input="updateQuantity(index, $event.target.value)"
                            @click.stop
                            class="waiter-quantity-input"
                            min="1"
                          />
                          <button
                            class="waiter-quantity-btn waiter-quantity-increase"
                            @click.stop="increaseQuantity(index)"
                          >
                            <b-icon icon="plus"></b-icon>
                          </button>
                        </div>
                        <div class="waiter-cart-item-total">
                          {{ formatPrice(item.total) }} {{ $t("currency") }}
                        </div>
                        <button
                          class="waiter-cart-item-delete"
                          @click.stop="deleteItem(index)"
                        >
                          <b-icon icon="trash-fill"></b-icon>
                        </button>
                      </div>
                    </div>
                  </div>
                  <div class="waiter-cart-empty" v-else>
                    <b-icon icon="cart-x" class="waiter-cart-empty-icon"></b-icon>
                    <p class="waiter-cart-empty-text">{{ $t("emptyCart") || 'السلة فارغة' }}</p>
                  </div>
                  
                  <!-- Order Notes Section -->
                  <div class="waiter-orders-notes-section" v-if="tableOrders.length > 0 && hasOrderNotes">
                    <div class="waiter-orders-notes-header">
                      <b-icon icon="file-text-fill" class="me-2"></b-icon>
                      <h4 class="waiter-orders-notes-title">{{ $t("orderNotes") || "ملاحظات الطلبات" }}</h4>
                    </div>
                    <div class="waiter-orders-notes-list">
                      <div 
                        class="waiter-order-note-item" 
                        v-for="(order, index) in tableOrdersWithNotes" 
                        :key="order.id || index"
                      >
                        <div class="waiter-order-note-header">
                          <span class="waiter-order-note-code">
                            <b-icon icon="receipt" class="me-1"></b-icon>
                            {{ order.orderCode || `#${order.id}` }}
                          </span>
                          <span class="waiter-order-note-date" v-if="order.insertDate">
                            {{ formatDate(order.insertDate) }}
                          </span>
                        </div>
                        <div class="waiter-order-note-content">
                          {{ order.notes }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Cart Summary -->
                <div class="waiter-cart-summary" v-if="carditems.length > 0">
                  <div class="waiter-cart-summary-row">
                    <span class="waiter-cart-summary-label">
                      <b-icon icon="box-seam" class="me-2"></b-icon>
                      {{ $t("countLabel") }}:
                    </span>
                    <span class="waiter-cart-summary-value">{{ totalCardItems }} {{ $t("itemLabel") }}</span>
                  </div>
                  <div class="waiter-cart-summary-row waiter-cart-total-row">
                    <span class="waiter-cart-summary-label">
                      <b-icon icon="currency-dollar" class="me-2"></b-icon>
                      {{ $t("totalLabel") }}:
                    </span>
                    <span class="waiter-cart-summary-value waiter-cart-total-value">
                      {{ formattedNumber }} {{ $t("currency") }}
                    </span>
                  </div>
                </div>

                <!-- Cart Actions -->
                <div class="waiter-cart-actions">
                  <button
                    class="waiter-action-btn waiter-action-btn-primary"
                    @click="openOrderNotesModal"
                    :disabled="totalCardItems <= 0 || !selectedTableId"
                  >
                    <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                    {{ $t("saveAndClear") || "حفظ وافراغ" }}
                  </button>
                  <button
                    class="waiter-action-btn waiter-action-btn-danger"
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
      </div>
    </b-overlay>
  </div>
</template>

<script>
import { HTTP } from "../../http/api.js";
import signalRService from "../../services/signalr.js";
import VueBarcode from "@chenfengyuan/vue-barcode";

export default {
  name: "WaiterView",
  components: {
    "vue-barcode": VueBarcode,
  },
  data() {
    return {
      show: false,
      totaPrice: 0,
      carditems: [],
      typingTimer: null,
      doneTypingInterval: 500,
      isSearching: false,
      searchAbortController: null,
      Items: [],
      tags: [],
      pageNumber: 1,
      totalItems: 0,
      pageSize: 20,
      search: {
        info: "",
      },
      searchCode: "",
      SearchItems: [],
      totalCardItems: 0,
      userInfo: {},
      orderForSend: {
        orderCode: "",
        paymentMethod: "Cash",
        customerOrderItem: [],
        orderType: "DineIn",
        tableId: null,
        tableIds: null, // For merged tables
        reservationId: null,
        notes: ""
      },
      allTables: [],
      selectedTableId: null,
      selectedTableNumber: null,
      selectedTableIds: [], // للطاولات المحددة للدمج
      tableOrders: [],
      tableFilters: {
        zone: '',
        tableNumber: '',
        status: ''
      },
      refreshInterval: null,
      transferToTableId: null,
      loadingTableOrders: false,
      loadingMergeTables: false,
      tablesToClose: null, // For merged tables
      // Print related variables
      commercialUserInfo: {
        restaurantName: '',
        logo: null
      },
      selectedPrinter: null,
      selectedPrinterId: null,
      availablePrinters: [],
      webPrintAPISupported: false,
      tagPrinters: [],
      managedPrinters: [],
    };
  },
  computed: {
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
    hasOrderNotes() {
      return this.tableOrders.some(order => order.notes && order.notes.trim().length > 0);
    },
    tableOrdersWithNotes() {
      return this.tableOrders.filter(order => order.notes && order.notes.trim().length > 0);
    },
    uniqueZones() {
      const zones = this.allTables
        .map(table => table.zone)
        .filter(zone => zone && zone.trim() !== '');
      return [...new Set(zones)].sort();
    },
    filteredTables() {
      let filtered = [...this.allTables];
      
      if (this.tableFilters.zone) {
        filtered = filtered.filter(table => table.zone === this.tableFilters.zone);
      }
      
      if (this.tableFilters.tableNumber) {
        const searchNumber = parseInt(this.tableFilters.tableNumber);
        if (!isNaN(searchNumber)) {
          filtered = filtered.filter(table => {
            const tableNumber = parseInt(table.tableNumber);
            return !isNaN(tableNumber) && tableNumber === searchNumber;
          });
        }
      }
      
      if (this.tableFilters.status) {
        filtered = filtered.filter(table => table.status === this.tableFilters.status);
      }
      
      return filtered.sort((a, b) => a.tableNumber - b.tableNumber);
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
    }
  },
  watch: {
    carditems: {
      handler() {
        this.totalCardItems = this.carditems.reduce((sum, item) => sum + item.quantity, 0);
        this.totaPrice = this.carditems.reduce((sum, item) => {
          const itemTotal = item.total;
          if (isNaN(itemTotal) || itemTotal === undefined || itemTotal === null) {
            // حساب السعر النهائي - استخدام disCountPrice فقط إذا كان مختلف عن price وأكبر من 0
            const finalPrice = (item.disCountPrice && item.disCountPrice > 0 && item.disCountPrice < item.price)
              ? item.disCountPrice
              : item.price;
            return sum + (finalPrice * item.quantity);
          }
          return sum + itemTotal;
        }, 0);
      },
      deep: true,
    },
    "search.info": {
      handler() {
        clearTimeout(this.typingTimer);
        this.typingTimer = setTimeout(() => {
          this.getItems();
        }, this.doneTypingInterval);
      },
    },
  },
  mounted() {
    this.getTags();
    this.getItems();
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
    
    // Initialize SignalR for real-time updates
    this.initializeSignalR();
    
    // Refresh tables every 30 seconds (fallback)
    this.refreshInterval = setInterval(() => {
      this.getTables();
    }, 30000);
  },
  beforeDestroy() {
    // Cleanup: Stop refresh interval
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
    // Cleanup: Stop SignalR connection
    this.cleanupSignalR();
  },
  methods: {
    getTags() {
      HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`)
        .then((response) => {
          this.tags = response.data.data.items;
        })
        .catch((error) => {
          this.$toast.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    getItems() {
      if (this.isSearching && this.searchAbortController) {
        this.searchAbortController.abort();
      }
      
      this.isSearching = true;
      this.searchAbortController = new AbortController();
      
      const searchQuery = this.search.info ? `&info=${encodeURIComponent(this.search.info)}` : '';
      
      HTTP.get(`Admin/GetItems?pageNumber=${this.pageNumber - 1}&pageSize=${this.pageSize}${searchQuery}`, {
        signal: this.searchAbortController.signal
      })
        .then((response) => {
          this.isSearching = false;
          if (response.data && response.data.data) {
            this.Items = response.data.data.items || [];
            this.totalItems = response.data.data.totalCount || 0;
          }
        })
        .catch((error) => {
          if (error.name !== 'AbortError') {
            this.isSearching = false;
            console.error('Error fetching items:', error);
          }
        });
    },
    getTables() {
      HTTP.get("Tables")
        .then((response) => {
          const pagedData = response.data.data;
          this.allTables = pagedData.items || response.data.data || [];
        })
        .catch((error) => {
          console.error('Error loading tables:', error);
        });
    },
    async getTableOrders(table) {
        this.loadingTableOrders = true;
        try {
          const response = await HTTP.get(`Admin/GetTableOrders?tableId=${table.id}`);
          this.tableOrders = response.data.data || [];
          
          this.carditems = [];
          this.tableOrders.forEach(order => {
            if (order.customerOrderItem) {
              order.customerOrderItem.forEach(orderItem => {
                if (orderItem.item) {
                  const existingItem = this.carditems.find(item => item.id === orderItem.item.id);
                  if (existingItem) {
                    existingItem.quantity += orderItem.quantity;
                    // حساب السعر النهائي - استخدام disCountPrice فقط إذا كان مختلف عن price وأكبر من 0
                    const finalPrice = (existingItem.disCountPrice && existingItem.disCountPrice > 0 && existingItem.disCountPrice < existingItem.price)
                      ? existingItem.disCountPrice
                      : existingItem.price;
                    existingItem.total = finalPrice * existingItem.quantity;
                  } else {
                    // حساب السعر النهائي - استخدام disCountPrice فقط إذا كان أكبر من 0 وأقل من sellingPrice
                    const sellingPrice = orderItem.sellingPrice || 0;
                    const discountPrice = orderItem.item.disCountPrice && orderItem.item.disCountPrice > 0 && orderItem.item.disCountPrice < sellingPrice
                      ? orderItem.item.disCountPrice
                      : null;
                    const finalPrice = discountPrice || sellingPrice;
                    
                    this.carditems.push({
                      id: orderItem.item.id,
                      name: orderItem.item.name,
                      price: sellingPrice,
                      disCountPrice: discountPrice || sellingPrice,
                      quantity: orderItem.quantity || 1,
                      code: orderItem.item.code,
                      image: orderItem.item.image,
                      total: finalPrice * (orderItem.quantity || 1)
                    });
                  }
                }
              });
            }
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
    },
    async selectTable(table, event = null) {
      // Check if Ctrl/Cmd key is pressed for multi-selection
      const isMultiSelect = event && (event.ctrlKey || event.metaKey);
      
      // If table is already merged, select all merged tables automatically
      if (table.currentOrderId && !isMultiSelect) {
        const mergedTables = this.allTables.filter(t => 
          t.currentOrderId === table.currentOrderId && !t.IsDeleted
        );
        
        if (mergedTables.length > 1) {
          // Auto-select all merged tables
          this.selectedTableIds = mergedTables.map(t => t.id);
          this.selectedTableId = table.id;
          this.selectedTableNumber = table.tableNumber;
          
          // Load orders for the primary table
          await this.getTableOrders(table);
          this.orderForSend.tableId = table.id;
          this.orderForSend.orderType = 'DineIn';
          
          this.$toast.success(this.$i18n.t("mergedTablesSelected") || `تم اختيار ${mergedTables.length} طاولات مدمجة`, {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
          return;
        }
      }
      
      // Multi-select mode (Ctrl/Cmd + click)
      if (isMultiSelect) {
        if (this.selectedTableIds.includes(table.id)) {
          // Deselect if already selected
          this.selectedTableIds = this.selectedTableIds.filter(id => id !== table.id);
          if (this.selectedTableIds.length === 0) {
            this.selectedTableId = null;
            this.selectedTableNumber = null;
            this.orderForSend.tableId = null;
            this.carditems = [];
          } else if (this.selectedTableId === table.id) {
            // If deselected table was the primary, switch to first remaining
            this.selectedTableId = this.selectedTableIds[0];
            const firstTable = this.allTables.find(t => t.id === this.selectedTableId);
            this.selectedTableNumber = firstTable ? firstTable.tableNumber : null;
          }
        } else {
          // Add to selection
          this.selectedTableIds.push(table.id);
          if (!this.selectedTableId) {
            this.selectedTableId = table.id;
            this.selectedTableNumber = table.tableNumber;
          }
        }
        return;
      }
      
      // Single select mode
      if (table.status === 'Occupied') {
        await this.getTableOrders(table);
          
          this.selectedTableId = table.id;
          this.selectedTableNumber = table.tableNumber;
        this.selectedTableIds = [table.id]; // Reset multi-select
          this.orderForSend.tableId = table.id;
          this.orderForSend.orderType = 'DineIn';
          
          this.$toast.success(this.$i18n.t("tableOrdersLoaded") || "تم تحميل طلبات الطاولة", {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
      } else if (table.status === 'Available') {
        this.selectedTableId = table.id;
        this.selectedTableNumber = table.tableNumber;
        this.selectedTableIds = [table.id]; // Reset multi-select
        this.orderForSend.tableId = table.id;
        this.orderForSend.orderType = 'DineIn';
        this.carditems = [];
        
        this.$toast.success(this.$i18n.t("newTableOrderStarted") || "تم بدء طلب جديد للطاولة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      }
    },
    deselectTable() {
      if (this.mergedTableIds.length > 1) {
        // Deselect all merged tables
        this.selectedTableIds = [];
      this.selectedTableId = null;
      this.selectedTableNumber = null;
      this.orderForSend.tableId = null;
      this.orderForSend.orderType = 'DineIn';
      this.carditems = [];
        
        this.$toast.info(this.$i18n.t("allMergedTablesDeselected") || "تم إلغاء اختيار جميع الطاولات المدمجة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      } else {
        // Deselect single table
        this.selectedTableIds = [];
        this.selectedTableId = null;
        this.selectedTableNumber = null;
        this.orderForSend.tableId = null;
        this.orderForSend.orderType = 'DineIn';
        this.carditems = [];
        
        this.$toast.info(this.$i18n.t("tableDeselected") || "تم إلغاء اختيار الطاولة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
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
        'Available': this.$t("available") || "متاحة",
        'Occupied': this.$t("occupied") || "مشغولة",
        'Reserved': this.$t("reserved") || "محجوزة",
        'OutOfService': this.$t("outOfService") || "خارج الخدمة"
      };
      return statusTexts[status] || status;
    },
    formatPrice(price) {
      if (price) {
        return price.toLocaleString("en-EG");
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
      if (!this.selectedTableId) {
        this.$toast.error(this.$i18n.t("pleaseSelectTable") || "الرجاء اختيار طاولة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
        return;
      }

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: "top-right",
          timeout: 4000,
        });
        return;
      }
      
      this.show = true;
      this.orderForSend.orderCode = "";
      this.orderForSend.paymentMethod = "Cash";
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
      
      this.orderForSend.reservationId = null;
      
      HTTP.post(`Admin/AddOrder`, this.orderForSend)
        .then((response) => {
          this.show = false;
          // Save a copy of carditems for printing before clearing
          const itemsForPrint = JSON.parse(JSON.stringify(this.carditems));
          this.carditems = [];
          this.orderForSend.notes = ""; // Reset notes
          this.getTables();
          
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
        })
        .catch((error) => {
          this.show = false;
          let errorMessage = this.$i18n.t("error") || "حدث خطأ";
          
          if (error.response) {
            if (error.response.status === 400) {
              errorMessage = error.response.data?.message || this.$i18n.t("badRequest") || "طلب غير صحيح";
            } else if (error.response.status === 401) {
              errorMessage = this.$i18n.t("unauthorized") || "غير مصرح";
            } else if (error.response.status === 500) {
              errorMessage = this.$i18n.t("serverError") || "خطأ في الخادم";
            }
          } else if (error.request) {
            errorMessage = this.$i18n.t("networkError") || "خطأ في الاتصال";
          }
          
          this.$toast.error(errorMessage, {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
        });
    },
    addToCartList(item) {
      try {
        if (!item.isAvailable) {
          this.$toast.error(
            this.$i18n.t("itemNotAvailable") || "الطبق/المشروب غير متوفر",
            {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            }
          );
          return;
        }
        
        if (!this.selectedTableId) {
          this.$toast.error(this.$i18n.t("pleaseSelectTable") || "الرجاء اختيار طاولة أولاً", {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
          return;
        }
        
        const existingItemIndex = this.carditems.findIndex(cartItem => cartItem.id === item.id);
        
        if (existingItemIndex !== -1) {
          this.carditems[existingItemIndex].quantity += 1;
          // حساب السعر النهائي - استخدام disCountPrice فقط إذا كان مختلف عن price وأكبر من 0
          const item = this.carditems[existingItemIndex];
          const finalPrice = (item.disCountPrice && item.disCountPrice > 0 && item.disCountPrice < item.price)
            ? item.disCountPrice
            : item.price;
          this.carditems[existingItemIndex].total = finalPrice * this.carditems[existingItemIndex].quantity;
        } else {
          // حساب السعر النهائي - استخدام disCountPrice فقط إذا كان أكبر من 0 وأقل من sellingPrice
          const discountPrice = item.disCountPrice && item.disCountPrice > 0 && item.disCountPrice < item.sellingPrice 
            ? item.disCountPrice 
            : null;
          const finalPrice = discountPrice || item.sellingPrice;
          
          const cartItem = {
            name: item.name,
            quantity: 1,
            price: item.sellingPrice,
            disCountPrice: discountPrice || item.sellingPrice,
            total: finalPrice,
            id: item.id,
          };

          this.carditems.push(cartItem);
        }

        if (this.$refs.codeNumber) {
          this.$refs.codeNumber.focus();
        }
      } catch (error) {
        console.error("Error adding item to cart:", error);
      }
    },
    deleteItem(index) {
      this.carditems.splice(index, 1);
    },
    increaseQuantity(index) {
      if (this.carditems[index]) {
        this.carditems[index].quantity += 1;
        const item = this.carditems[index];
        const finalPrice = (item.disCountPrice && item.disCountPrice > 0 && item.disCountPrice < item.price)
          ? item.disCountPrice
          : item.price;
        this.carditems[index].total = finalPrice * this.carditems[index].quantity;
      }
    },
    decreaseQuantity(index) {
      if (this.carditems[index] && this.carditems[index].quantity > 1) {
        this.carditems[index].quantity -= 1;
        const item = this.carditems[index];
        const finalPrice = (item.disCountPrice && item.disCountPrice > 0 && item.disCountPrice < item.price)
          ? item.disCountPrice
          : item.price;
        this.carditems[index].total = finalPrice * this.carditems[index].quantity;
      }
    },
    updateQuantity(index, value) {
      const quantity = parseInt(value);
      if (this.carditems[index] && quantity > 0) {
        this.carditems[index].quantity = quantity;
        const item = this.carditems[index];
        const finalPrice = (item.disCountPrice && item.disCountPrice > 0 && item.disCountPrice < item.price)
          ? item.disCountPrice
          : item.price;
        this.carditems[index].total = finalPrice * quantity;
      }
    },
    handleBarcodeSearch() {
      if (!this.searchCode || this.searchCode.trim() === '') {
        return;
      }
      
      if (this.isSearching && this.searchAbortController) {
        this.searchAbortController.abort();
      }
      
      this.isSearching = true;
      this.searchAbortController = new AbortController();
      
      HTTP.get(`Admin/GetItemByCode?code=${this.searchCode}`, {
        signal: this.searchAbortController.signal
      })
        .then((response) => {
          this.isSearching = false;
          
          if (response.data && response.data.data) {
            this.SearchItems = response.data.data;
            
            const existingItemIndex = this.carditems.findIndex(cartItem => cartItem.id === this.SearchItems.id);
            
            if (existingItemIndex !== -1) {
              this.carditems[existingItemIndex].quantity += 1;
              this.carditems[existingItemIndex].total = 
                (this.carditems[existingItemIndex].price !== this.carditems[existingItemIndex].disCountPrice
                  ? this.carditems[existingItemIndex].disCountPrice
                  : this.carditems[existingItemIndex].price) * this.carditems[existingItemIndex].quantity;
            } else {
              if (!this.SearchItems.isAvailable) {
                this.$toast.error(
                  this.$i18n.t("itemNotAvailable") || "الطبق/المشروب غير متوفر",
                  {
                    position: "top-right",
                    timeout: 3000,
                  }
                );
                this.searchCode = "";
                if (this.$refs.codeNumber) {
                  this.$refs.codeNumber.focus();
                }
                return;
              }
              
              if (!this.selectedTableId) {
                this.$toast.error(this.$i18n.t("pleaseSelectTable") || "الرجاء اختيار طاولة أولاً", {
                  position: "top-right",
                  timeout: 3000,
                });
                this.searchCode = "";
                if (this.$refs.codeNumber) {
                  this.$refs.codeNumber.focus();
                }
                return;
              }
              
              const finalPrice = this.SearchItems.disCountPrice > 0 && this.SearchItems.disCountPrice !== this.SearchItems.sellingPrice
                ? this.SearchItems.disCountPrice
                : this.SearchItems.sellingPrice;
                
              var item = {
                name: this.SearchItems.name,
                quantity: 1,
                price: this.SearchItems.sellingPrice,
                disCountPrice: this.SearchItems.disCountPrice,
                total: finalPrice,
                id: this.SearchItems.id,
              };

              this.carditems.push(item);
            }
            
            this.searchCode = "";
            if (this.$refs.codeNumber) {
              this.$refs.codeNumber.focus();
            }
          }
        })
        .catch((error) => {
          if (error.name !== 'AbortError') {
            this.isSearching = false;
            this.$toast.error(this.$i18n.t("itemNotFound") || "المنتج غير موجود", {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            });
            this.searchCode = "";
            if (this.$refs.codeNumber) {
              this.$refs.codeNumber.focus();
            }
          }
        });
    },
    EmptycardList(id) {
      this.carditems = [];
      this.$bvModal.hide(id);
      if (this.$refs.codeNumber) {
        this.$refs.codeNumber.focus();
      }
    },
    closeModel(id) {
      this.$bvModal.hide(id);
    },
    openOrderNotesModal() {
      if (!this.selectedTableId) {
        this.$toast.error(this.$i18n.t("pleaseSelectTable") || "الرجاء اختيار طاولة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
        return;
      }

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: "top-right",
          timeout: 4000,
        });
        return;
      }
      // Reset notes before opening modal
      this.orderForSend.notes = "";
      this.$bvModal.show('modal-order-notes');
    },
    confirmAddOrder() {
      this.$bvModal.hide('modal-order-notes');
      // Call the actual add order function
      this.addOrderAndClear();
    },
    logout() {
      localStorage.removeItem('token');
      localStorage.removeItem('role');
      localStorage.removeItem('info');
      this.$router.push('/login');
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
                  this.selectedTableNumber = null;
                  this.orderForSend.tableId = null;
                  this.orderForSend.orderType = 'DineIn';
                  this.carditems = [];
                  this.tableOrders = [];
                }
              }
            }
          });

          // Listen for order transfers
          signalRService.on('FloorPlanUpdated', (data) => {
            console.log('Floor plan updated via SignalR:', data);
            this.getTables();
          });

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
                this.selectedTableNumber = newTable.tableNumber;
                this.orderForSend.tableId = newTable.id;
                // Reload table orders
                this.getTableOrders(newTable);
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
      signalRService.off('FloorPlanUpdated');
      signalRService.off('OrderTransferred');
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
        this.show = true;
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
            this.selectedTableNumber = newTable.tableNumber;
            // Reload table orders
            await this.getTableOrders(newTable);
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
        this.show = false;
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
    getMergedTableNumbers() {
      if (this.mergedTableIds.length <= 1) {
        return this.selectedTableNumber || '';
      }
      const mergedTables = this.allTables
        .filter(t => this.mergedTableIds.includes(t.id))
        .sort((a, b) => a.tableNumber - b.tableNumber);
      return mergedTables.map(t => t.tableNumber).join('و');
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
    addOrder(isPrint = false) {
      if (!this.selectedTableId) {
        this.$toast.error(this.$i18n.t("pleaseSelectTable") || "الرجاء اختيار طاولة", {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
        return;
      }

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: "top-right",
          timeout: 4000,
        });
        return;
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
      
      this.orderForSend.reservationId = null;
      
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
            this.$refs.codeNumber.focus();
            
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
    closeTableOrder(tableId) {
      // Set tables to close - use mergedTableIds if multiple tables are merged
      if (this.mergedTableIds.length > 1) {
        this.tablesToClose = [...this.mergedTableIds];
      } else {
        this.tablesToClose = [tableId];
      }
      
      this.performCloseTableOrder();
    },
    async performCloseTableOrder() {
      if (!this.tablesToClose || this.tablesToClose.length === 0) {
        return;
      }
      
      try {
        this.show = true;
        
        let response;
        if (this.tablesToClose.length > 1) {
          // Send multiple table IDs in request body
          response = await HTTP.put('Admin/CloseTableOrder', this.tablesToClose);
        } else {
          // Send single table ID as query parameter
          response = await HTTP.put(`Admin/CloseTableOrder?tableId=${this.tablesToClose[0]}`);
        }
        
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(response.data.message || this.$i18n.t("tableOrderClosed") || "تم إغلاق حساب الطاولة بنجاح", {
            position: "top-right",
            timeout: 2500,
            maxToasts: 1,
          });
          
          // Refresh tables
          await this.getTables();
          
          // Clear selection if closed table was selected
          if (this.selectedTableId && this.tablesToClose.includes(this.selectedTableId)) {
            this.deselectTable();
          }
          
          this.tablesToClose = null;
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("errorClosingTable") || "حدث خطأ أثناء إغلاق حساب الطاولة", {
            position: "top-right",
            timeout: 2500,
            maxToasts: 1,
          });
        }
      } catch (error) {
        console.error('Error closing table order:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("errorClosingTable") || "حدث خطأ أثناء إغلاق حساب الطاولة", {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
      } finally {
        this.show = false;
        this.tablesToClose = null;
      }
    },
    // Print helper functions
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
    async loadTagPrinters() {
      try {
        const response = await HTTP.get('TagPrinters');
        if (response.data && response.data.data) {
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
        if (response.data && response.data.data) {
          this.managedPrinters = response.data.data || [];
        } else {
          this.managedPrinters = [];
        }
      } catch (error) {
        console.error('Error loading managed printers:', error);
        this.managedPrinters = [];
      }
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
    async checkPythonServerHealth() {
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 3000);
        
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
        <table class="bill-table">
          <thead>
            <tr class="bill-table-header">
              <th class="bill-table-cell bill-col-item">طبق/مشروب</th>
              <th class="bill-table-cell bill-col-qty">العدد</th>
              <th class="bill-table-cell bill-col-price">السعر</th>
              <th class="bill-table-cell bill-col-total">المجموع</th>
            </tr>
          </thead>
          <tbody>
      `;
      
      items.forEach(item => {
        const itemPrice = item.price !== item.disCountPrice ? item.disCountPrice : item.price;
        itemsTableHTML += `
          <tr class="bill-table-row">
            <td class="bill-table-cell bill-col-item">${this.escapeHtml(item.name || '')}</td>
            <td class="bill-table-cell bill-col-qty">${item.quantity || 0}</td>
            <td class="bill-table-cell bill-col-price">${itemPrice ? itemPrice.toLocaleString() : '0'}</td>
            <td class="bill-table-cell bill-col-total">${item.total ? item.total.toLocaleString() : '0'}</td>
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
        <div class="bill-summary-section">
          <div class="bill-summary-row">
            <span class="bill-summary-label">العدد:</span>
            <span class="bill-summary-value">${totalItems} طبق/مشروب</span>
          </div>
          ${tagName && tagName !== 'default' ? `
          <div class="bill-summary-row">
            <span class="bill-summary-label">القسم:</span>
            <span class="bill-summary-value">${this.escapeHtml(tagName)}</span>
          </div>
          ` : ''}
          <div class="bill-summary-row bill-total-row">
            <span class="bill-summary-label">المجموع:</span>
            <span class="bill-summary-value bill-total-amount">${subtotal.toLocaleString()} د.ع</span>
          </div>
        </div>
      `;
      htmlContent = htmlContent.replace(summaryRegex, summaryHTML);
      
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
                        this.orderForSend.paymentMethod || 'نقدي'
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
      } catch (error) {
        console.error('Web Print API error:', error);
        throw error;
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
      
      .bill-title {
        font-size: 18px;
        font-weight: 800;
        margin: 8px 0;
        color: #000;
        text-align: center;
      }
      
      .bill-store-section {
        text-align: center;
        margin: 8px 0;
        font-size: 11px;
      }
      
      .bill-store-name {
        font-size: 16px;
        font-weight: 800;
        margin: 4px 0 2px 0;
        color: #000;
      }
      
      .bill-store-address {
        font-size: 10px;
        color: #666;
        margin: 2px 0;
      }
      
      .bill-store-phone {
        font-size: 10px;
        color: #666;
        margin: 2px 0;
      }
      
      .bill-logo-img {
        max-width: 50px;
        height: auto;
        margin-bottom: 4px;
      }
      
      .bill-store-subtitle {
        font-size: 9px;
        color: #666;
        margin: 0;
      }
      
      .bill-payment-section {
        margin-top: 8px;
        padding-top: 8px;
        border-top: 1px dashed #000;
        font-size: 11px;
      }
      
      .bill-payment-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 4px;
      }
      
      .bill-payment-label {
        font-weight: 600;
      }
      
      .bill-payment-value {
        font-weight: 400;
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
      
      const iframe = document.createElement('iframe');
      iframe.style.position = 'absolute';
      iframe.style.width = '0';
      iframe.style.height = '0';
      iframe.style.border = '0';
      document.body.appendChild(iframe);
      
      const doc = iframe.contentWindow.document;
      doc.open();
      doc.write('<!DOCTYPE html><html><head><meta charset="UTF-8">' + stylesHtml + '</head><body>' + prtHtml + '</body></html>');
      doc.close();
      
      setTimeout(() => {
        iframe.contentWindow.focus();
        iframe.contentWindow.print();
        setTimeout(() => {
          document.body.removeChild(iframe);
          // Restore original carditems if we changed it
          if (itemsToPrint) {
            this.carditems = originalCarditems;
          }
        }, 500);
      }, 500);
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
  },
};
</script>

<style scoped>
.waiter-view-container {
  width: 100%;
  height: 100vh;
  overflow: hidden;
}

.waiter-main-wrapper {
  width: 100%;
  height: 100vh;
  overflow: hidden;
}

.waiter-container-fluid {
  padding: 0;
  height: 100vh;
  overflow: hidden;
}

.waiter-page-container {
  display: grid;
  grid-template-columns: 1fr 400px;
  gap: 1rem;
  padding: 1rem;
  height: 100vh;
  max-width: 100%;
  overflow: hidden;
}

.waiter-main-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  overflow: hidden;
  height: 100%;
}

.waiter-header-section {
  background: var(--bg-primary);
  border-radius: 0.75rem;
  padding: 1rem 1.5rem;
  box-shadow: var(--shadow-sm);
  flex-shrink: 0;
}

.waiter-header-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.waiter-logo-section {
  display: flex;
  align-items: center;
}

.waiter-logo {
  height: 50px;
  width: auto;
}

.waiter-employee-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.waiter-clock-section {
  display: flex;
  align-items: center;
}

.waiter-logout-section {
  display: flex;
  align-items: center;
}

.waiter-logout-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
  color: #ffffff;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(220, 38, 38, 0.2);
}

.waiter-logout-btn:hover {
  background: linear-gradient(135deg, #b91c1c 0%, #991b1b 100%);
  box-shadow: 0 4px 8px rgba(220, 38, 38, 0.3);
  transform: translateY(-1px);
}

.waiter-logout-btn:active {
  transform: translateY(0);
}

.waiter-logout-icon {
  font-size: 1rem;
}

.waiter-logout-text {
  font-size: 0.875rem;
}

@media (max-width: 768px) {
  .waiter-logout-text {
    display: none;
  }
  
  .waiter-logout-btn {
    padding: 0.5rem;
    min-width: 40px;
    justify-content: center;
  }
}

.waiter-quick-actions {
  display: flex;
  gap: 1rem;
  flex-shrink: 0;
}

.waiter-quick-search,
.waiter-quick-barcode {
  display: flex;
  align-items: center;
  background: var(--bg-primary);
  border-radius: 0.5rem;
  padding: 0.5rem 1rem;
  border: 1px solid var(--border-color);
  flex: 1;
}

.waiter-quick-search-icon {
  color: var(--text-secondary);
  margin-left: 0.5rem;
}

.waiter-quick-search-input,
.waiter-quick-barcode-input {
  border: none;
  background: transparent;
  color: var(--text-primary);
  font-size: 1rem;
  width: 100%;
  outline: none;
}

.waiter-tables-section-compact {
  background: var(--bg-primary);
  border-radius: 0.75rem;
  padding: 1rem;
  box-shadow: var(--shadow-sm);
  flex-shrink: 0;
}

.waiter-tables-header-compact {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.waiter-tables-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
}

.waiter-tables-count {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-secondary);
}

.waiter-floor-plan-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  margin-inline-end: 0.35rem;
  background: var(--bg-tertiary, #e5e7eb);
  color: var(--text-primary, #1f2937);
  border-radius: 0.5rem;
  text-decoration: none;
  transition: background 0.2s ease;
}

.waiter-floor-plan-link:hover {
  background: var(--border-color, #d1d5db);
  color: var(--primary-color, #6366f1);
}

.waiter-refresh-tables-btn-compact {
  padding: 0.5rem;
  border: none;
  background: var(--primary-color);
  color: #ffffff;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.3s ease;
}

.waiter-refresh-tables-btn-compact:hover {
  background: var(--primary-hover);
}

.waiter-tables-filters {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.waiter-table-filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  flex: 1;
  min-width: 120px;
}

.waiter-table-filter-label {
  display: flex;
  align-items: center;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-primary);
}

.waiter-table-filter-select,
.waiter-table-filter-input {
  padding: 0.5rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  font-size: 0.875rem;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.waiter-table-filter-clear {
  padding: 0.5rem 1rem;
  border: 1px solid var(--danger-color);
  background: transparent;
  color: var(--danger-color);
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 600;
  align-self: flex-end;
  transition: all 0.3s ease;
}

.waiter-table-filter-clear:hover {
  background: var(--danger-color);
  color: #ffffff;
}

.waiter-table-deselect-compact:hover {
  background: #b91c1c;
}

.waiter-tables-scroll {
  display: flex;
  gap: 0.75rem;
  overflow-x: auto;
  padding-bottom: 0.5rem;
}

.waiter-table-card-compact {
  background: var(--bg-secondary);
  border-radius: 0.75rem;
  padding: 1rem;
  border: 2px solid var(--border-color);
  cursor: pointer;
  transition: all 0.3s ease;
  position: relative;
  min-width: 120px;
  flex-shrink: 0;
}

.waiter-table-card-compact:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.waiter-table-available {
  border-color: var(--success-color);
}

.waiter-table-occupied {
  border-color: var(--danger-color);
}

.waiter-table-reserved {
  border-color: var(--warning-color);
}

.waiter-table-selected {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(30, 64, 175, 0.2);
}

.waiter-table-multi-selected {
  border-color: var(--warning-color);
  box-shadow: 0 0 0 3px rgba(245, 158, 11, 0.3);
  background: rgba(245, 158, 11, 0.05);
}

.waiter-table-merged {
  border-color: var(--info-color, #3b82f6);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.2);
  background: rgba(59, 130, 246, 0.05);
}

.waiter-merge-tables-btn-compact {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: #ffffff;
  border: none;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.3);
}

.waiter-merge-tables-btn-compact:hover {
  background: linear-gradient(135deg, #d97706 0%, #b45309 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.4);
}

.waiter-table-actions-section {
  background: var(--bg-secondary);
  border-radius: var(--radius-md);
  padding: 1rem;
  border: 1px solid var(--border-color);
  margin-top: 1rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.waiter-table-actions-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.waiter-table-actions-count {
  font-size: 1rem;
  font-weight: 600;
  color: var(--primary-color);
  margin-left: 0.5rem;
}

.waiter-table-actions-buttons {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.waiter-table-action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  flex: 1;
  min-width: 120px;
}

.waiter-table-action-save {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);
}

.waiter-table-action-save:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
}

.waiter-table-action-close {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: #ffffff;
  border: none;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.3);
}

.waiter-table-action-close:hover {
  background: linear-gradient(135deg, #d97706 0%, #b45309 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.4);
}

.waiter-table-action-deselect {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  border: none;
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.3);
}

.waiter-table-action-deselect:hover {
  background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.4);
}

@media (max-width: 768px) {
  .waiter-table-actions-buttons {
    flex-direction: column;
  }
}

/* Merge Tables Modal Styles */
.merge-tables-content {
  padding: 1rem;
}

.merge-tables-info {
  text-align: center;
  margin-bottom: 1.5rem;
}

.merge-tables-icon {
  font-size: 3rem;
  color: var(--primary-color);
  margin-bottom: 1rem;
}

.merge-tables-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 0.5rem;
}

.merge-tables-message {
  font-size: 1rem;
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.merge-tables-list {
  max-height: 300px;
  overflow-y: auto;
  margin-bottom: 1.5rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  padding: 0.5rem;
}

.merge-table-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  background: var(--bg-secondary);
  border-radius: 0.5rem;
  margin-bottom: 0.5rem;
}

.merge-table-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.merge-table-remove-btn {
  background: var(--danger-color);
  color: #ffffff;
  border: none;
  border-radius: 50%;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s ease;
}

.merge-table-remove-btn:hover {
  background: var(--danger-color-dark);
  transform: scale(1.1);
}

.merge-tables-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}

.merge-tables-cancel-btn,
.merge-tables-confirm-btn {
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.merge-tables-cancel-btn {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
}

.merge-tables-cancel-btn:hover {
  background: var(--bg-tertiary);
}

.merge-tables-confirm-btn {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: #ffffff;
  border: none;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.3);
}

.merge-tables-confirm-btn:hover:not(:disabled) {
  background: linear-gradient(135deg, #d97706 0%, #b45309 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.4);
}

.merge-tables-confirm-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.waiter-table-number-compact {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 0.5rem;
}

.waiter-table-status-compact {
  padding: 0.25rem 0.5rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  margin-bottom: 0.5rem;
}

.waiter-table-status-available {
  background: var(--success-light);
  color: var(--success-color);
}

.waiter-table-status-occupied {
  background: var(--danger-light);
  color: var(--danger-color);
}

.waiter-table-status-reserved {
  background: var(--warning-light);
  color: var(--warning-color);
}

.waiter-table-zone-compact {
  font-size: 0.75rem;
  color: var(--text-secondary);
}

.waiter-table-deselect-compact {
  position: absolute;
  bottom: 0.5rem;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  align-items: center;
  gap: 0.25rem;
  background: var(--danger-color);
  color: #ffffff;
  padding: 0.25rem 0.5rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.waiter-table-deselect-compact:hover {
  background: #b91c1c;
}

.waiter-categories-scroll {
  overflow-x: auto;
  flex-shrink: 0;
  padding-bottom: 0.5rem;
}

.waiter-categories-list {
  display: flex;
  gap: 0.5rem;
  padding: 0.5rem 0;
}

.waiter-category-btn {
  padding: 0.5rem 1rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 600;
  white-space: nowrap;
  transition: all 0.3s ease;
}

.waiter-category-btn:hover {
  background: var(--primary-color);
  color: #ffffff;
  border-color: var(--primary-color);
}

.waiter-category-btn-active {
  background: var(--primary-color);
  color: #ffffff;
  border-color: var(--primary-color);
}

.waiter-products-grid-section {
  flex: 1;
  overflow-y: auto;
  background: var(--bg-primary);
  border-radius: 0.75rem;
  padding: 1rem;
  box-shadow: var(--shadow-sm);
}

.waiter-products-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 1rem;
}

.waiter-product-card {
  background: var(--bg-secondary);
  border-radius: 0.75rem;
  padding: 1rem;
  border: 1px solid var(--border-color);
  cursor: pointer;
  transition: all 0.3s ease;
  position: relative;
}

.waiter-product-card:hover:not(.waiter-product-card-unavailable) {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.waiter-product-card-unavailable {
  opacity: 0.5;
  cursor: not-allowed;
  background: var(--bg-tertiary);
  filter: grayscale(100%);
  pointer-events: none;
}

.waiter-product-discount-badge {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  background: var(--danger-color);
  color: #ffffff;
  padding: 0.25rem 0.5rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 600;
  z-index: 1;
}

.waiter-product-media {
  width: 100%;
  min-height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  border-radius: 0.5rem;
  margin-bottom: 0.75rem;
}

.waiter-product-image {
  width: 100%;
  height: 120px;
  object-fit: cover;
  border-radius: 0.5rem;
}

.waiter-product-image-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 120px;
}

.waiter-product-placeholder-icon {
  font-size: 3rem;
  color: var(--text-secondary);
}

.waiter-product-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.waiter-product-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  min-height: 2.5rem;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.waiter-product-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
}

.waiter-product-category {
  font-size: 0.75rem;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
}

.waiter-product-price {
  font-weight: 700;
  color: var(--primary-color);
}

.waiter-product-price-discounted {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
}

.waiter-product-price-current {
  font-size: 1rem;
  color: var(--primary-color);
}

.waiter-product-price-old {
  font-size: 0.75rem;
  color: var(--text-secondary);
  text-decoration: line-through;
}

.waiter-product-price-regular {
  font-size: 1rem;
}

.waiter-product-unavailable-badge {
  background: var(--danger-light);
  color: var(--danger-color);
  padding: 0.25rem 0.5rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-align: center;
}

.waiter-pagination-section {
  margin-top: 1rem;
  display: flex;
  justify-content: center;
}

.waiter-cart-section {
  background: var(--bg-primary);
  border-radius: 0.75rem;
  box-shadow: var(--shadow-sm);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  height: 100%;
}

.waiter-cart-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

.waiter-selected-table-info {
  padding: 1rem;
  background: var(--primary-color);
  color: #ffffff;
  border-bottom: 2px solid rgba(255, 255, 255, 0.2);
}

.waiter-selected-table-header {
  display: flex;
  align-items: center;
  font-weight: 700;
  font-size: 1.125rem;
  margin-bottom: 0.75rem;
}

.waiter-transfer-table-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  background: rgba(255, 255, 255, 0.2);
  color: #ffffff;
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  width: 100%;
}

.waiter-transfer-table-btn:hover {
  background: rgba(255, 255, 255, 0.3);
  border-color: rgba(255, 255, 255, 0.5);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

.waiter-cart-items-section {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
}

.waiter-cart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.waiter-cart-title {
  display: flex;
  align-items: center;
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.waiter-cart-count-badge {
  background: var(--primary-color);
  color: #ffffff;
  padding: 0.25rem 0.5rem;
  border-radius: 50%;
  font-size: 0.875rem;
  font-weight: 700;
}

.waiter-cart-items-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.waiter-cart-item {
  background: var(--bg-secondary);
  border-radius: 0.5rem;
  padding: 0.75rem;
  border: 1px solid var(--border-color);
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
}

.waiter-cart-item-info {
  flex: 1;
  min-width: 0;
}

.waiter-cart-item-name {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 0.25rem 0;
}

.waiter-cart-item-price {
  font-size: 0.75rem;
  color: var(--text-secondary);
}

.waiter-cart-item-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.waiter-cart-item-quantity {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.waiter-quantity-btn {
  width: 28px;
  height: 28px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  border-radius: 0.25rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
}

.waiter-quantity-btn:hover {
  background: var(--primary-color);
  color: #ffffff;
  border-color: var(--primary-color);
}

.waiter-quantity-input {
  width: 40px;
  height: 28px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  border-radius: 0.25rem;
  text-align: center;
  font-size: 0.875rem;
}

.waiter-cart-item-total {
  font-weight: 700;
  color: var(--primary-color);
  font-size: 0.875rem;
  min-width: 60px;
  text-align: right;
}

.waiter-cart-item-delete {
  padding: 0.25rem;
  border: none;
  background: transparent;
  color: var(--danger-color);
  cursor: pointer;
  border-radius: 0.25rem;
  transition: all 0.3s ease;
}

.waiter-cart-item-delete:hover {
  background: var(--danger-light);
}

.waiter-cart-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  color: var(--text-secondary);
}

.waiter-cart-empty-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.waiter-cart-empty-text {
  font-size: 1rem;
  font-weight: 600;
}

.waiter-cart-summary {
  padding: 1rem;
  border-top: 2px solid var(--border-color);
  background: var(--bg-secondary);
}

.waiter-cart-summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.waiter-cart-summary-row:last-child {
  margin-bottom: 0;
}

.waiter-cart-summary-label {
  display: flex;
  align-items: center;
  font-weight: 600;
  color: var(--text-primary);
}

.waiter-cart-summary-value {
  font-weight: 700;
  color: var(--text-primary);
}

.waiter-cart-total-row {
  padding-top: 0.75rem;
  border-top: 1px solid var(--border-color);
  font-size: 1.125rem;
}

.waiter-cart-total-value {
  font-size: 1.25rem;
  color: var(--primary-color);
}

.waiter-cart-actions {
  padding: 1rem;
  border-top: 2px solid var(--border-color);
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.waiter-action-btn {
  padding: 0.875rem 1rem;
  border: none;
  border-radius: 0.5rem;
  font-size: 1rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.waiter-action-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.waiter-action-btn-primary {
  background: var(--primary-color);
  color: #ffffff;
}

.waiter-action-btn-primary:hover:not(:disabled) {
  background: var(--primary-hover);
}

.waiter-action-btn-danger {
  background: var(--danger-color);
  color: #ffffff;
}

.waiter-action-btn-danger:hover:not(:disabled) {
  background: #b91c1c;
}

@media (max-width: 1024px) {
  .waiter-page-container {
    grid-template-columns: 1fr;
    grid-template-rows: 1fr auto;
  }
  
  .waiter-cart-section {
    max-height: 50vh;
  }
}

@media (min-width: 768px) and (max-width: 1024px) {
  .waiter-page-container {
    grid-template-columns: 1fr 350px;
  }
}

@media (min-width: 1024px) {
  .waiter-page-container {
    grid-template-columns: 1fr 400px;
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
.waiter-orders-notes-section {
  margin-top: 1rem;
  padding: 1rem;
  background: var(--bg-secondary, #f8f9fa);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color, #dee2e6);
}

.waiter-orders-notes-header {
  display: flex;
  align-items: center;
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--border-color, #dee2e6);
}

.waiter-orders-notes-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary, #212529);
  margin: 0;
  display: flex;
  align-items: center;
}

.waiter-orders-notes-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.waiter-order-note-item {
  padding: 0.75rem;
  background: white;
  border-radius: 0.5rem;
  border-left: 3px solid var(--primary-color, #818cf8);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.waiter-order-note-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
}

.waiter-order-note-code {
  font-weight: 600;
  color: var(--primary-color, #818cf8);
  display: flex;
  align-items: center;
}

.waiter-order-note-date {
  color: var(--text-secondary, #6c757d);
  font-size: 0.8125rem;
}

.waiter-order-note-content {
  color: var(--text-primary, #212529);
  font-size: 0.9375rem;
  line-height: 1.5;
  white-space: pre-wrap;
  word-wrap: break-word;
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

/* Print Styles */
.print_hide {
  display: none !important;
}

.bill-container {
  width: 100%;
  max-width: 80mm;
  margin: 0 auto;
  padding: 8mm;
}

.bill-header {
  text-align: center;
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px dashed #000;
}

.bill-logo-section {
  margin-bottom: 8px;
}

.bill-logo-img {
  max-width: 60px;
  height: auto;
  margin-bottom: 4px;
}

.bill-store-info {
  margin-top: 8px;
}

.bill-store-name {
  font-size: 16px;
  font-weight: 800;
  margin: 4px 0 2px 0;
  color: #000;
}

.bill-store-subtitle {
  font-size: 10px;
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
  margin-bottom: 4px;
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
  border: none;
  border-top: 1px dashed #000;
  margin: 8px 0;
}

.bill-table {
  width: 100%;
  border-collapse: collapse;
  margin: 8px 0;
  font-size: 10px;
}

.bill-table-header {
  background: #f5f5f5;
  border-bottom: 2px solid #000;
}

.bill-table-cell {
  padding: 4px 2px;
  text-align: right;
  border-bottom: 1px dotted #ccc;
}

.bill-table-header .bill-table-cell {
  font-weight: 700;
  font-size: 10px;
  padding: 6px 2px;
}

.bill-col-item {
  width: 40%;
  text-align: right;
}

.bill-col-qty {
  width: 15%;
  text-align: center;
}

.bill-col-price {
  width: 20%;
  text-align: left;
}

.bill-col-total {
  width: 25%;
  text-align: left;
  font-weight: 600;
}

.bill-summary-section {
  margin-top: 12px;
  padding-top: 8px;
  border-top: 2px solid #000;
  font-size: 11px;
}

.bill-summary-row {
  display: flex;
  justify-content: space-between;
  margin-bottom: 6px;
}

.bill-summary-label {
  font-weight: 600;
}

.bill-summary-value {
  font-weight: 400;
}

.bill-total-row {
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px dashed #000;
  font-size: 14px;
}

.bill-total-amount {
  font-weight: 800;
  font-size: 16px;
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
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px dashed #000;
  font-size: 10px;
}

.bill-footer-text {
  margin: 4px 0;
  font-weight: 600;
}

.bill-footer-date {
  margin: 4px 0;
  color: #666;
  font-size: 9px;
}
</style>
