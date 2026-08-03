<template>
  <b-overlay
    :show="show"
    spinner-variant="primary"
    spinner-type="grow"
    spinner-large
    rounded="sm"
  >
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content items-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="box-seam" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("allItemsLabel") }}</h1>
                  <p class="header-subtitle">{{ $t("itemsPageDescription") || "إدارة المنتجات والأسعار والمخزون" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="refreshPage" :disabled="show">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
                <button type="button" class="export-excel-btn" v-b-modal.modal-importItems>
                  <b-icon icon="file-earmark-arrow-up-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("importItems") }}</span>
                </button>
                <button type="button" class="users-add-button" v-b-modal.modal-addItem>
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addItemLabel") }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="box-seam"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ totalItems }}</div>
                <div class="app-overview-stat-label">{{ $t("itemsOverviewTotal") || "إجمالي المنتجات" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ itemsInStockOnPage }}</div>
                <div class="app-overview-stat-label">{{ $t("itemsOverviewInStock") || "متوفر (الصفحة)" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                <b-icon icon="exclamation-triangle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ itemsOutOfStockOnPage }}</div>
                <div class="app-overview-stat-label">{{ $t("itemsOverviewOutOfStock") || "نفد (الصفحة)" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="list-ul"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ Items.length }}</div>
                <div class="app-overview-stat-label">{{ $t("itemsOverviewOnPage") || "في الصفحة الحالية" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="list-ul"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("allItemsLabel") }}</h3>
                  <p class="app-section-subtitle">{{ $t("itemsListHint") || "قائمة المنتجات مع الأسعار والكميات" }}</p>
                </div>
              </div>
            </div>
            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                    <p>{{ $t("itemsFiltersHint") || "تصفية حسب القسم وحالة المخزون والبحث" }}</p>
                  </div>
                </div>
                <div class="app-filters-panel-actions" v-if="hasActiveItemFilters">
                  <button type="button" class="users-filter-clear-btn app-filters-clear-btn" @click="clearItemFilters">
                    <b-icon icon="x-circle" class="me-1"></b-icon>
                    {{ $t("clearFilters") || "مسح الفلاتر" }}
                  </button>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--3">
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("categoryPlaceholder") || "القسم" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="tags" class="search-icon"></b-icon>
                    <select v-model="search.tag" class="users-search-input reports-filter-select">
                      <option value="">{{ $t("all_categories") || "جميع الاقسام" }}</option>
                      <option v-for="tag in tags" :key="tag.id || tag.name" :value="tag.name">
                        {{ tag.name }}
                      </option>
                    </select>
                  </div>
                </label>
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("allStockStatuses") || "حالة المخزون" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="box-seam" class="search-icon"></b-icon>
                    <select v-model="search.stockStatus" class="users-search-input reports-filter-select">
                      <option value="">{{ $t("allStockStatuses") || "كل حالات المخزون" }}</option>
                      <option value="inStock">{{ $t("inStock") || "متوفر" }}</option>
                      <option value="outOfStock">{{ $t("outOfStock") || "نفد" }}</option>
                      <option value="lowStock">{{ $t("lowStock") || "تنبيه كمية" }}</option>
                    </select>
                  </div>
                </label>
                <label class="app-filter-field app-filter-field--grow">
                  <span class="app-filter-label">{{ $t("search") || "بحث" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="search" class="search-icon"></b-icon>
                    <input
                      v-model="search.info"
                      type="search"
                      :placeholder="$t('itemsSearchPlaceholder') || $t('searchPlaceholder')"
                      class="users-search-input"
                      autocomplete="off"
                    />
                  </div>
                </label>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding">
          <div class="items-table-container report-table-container">
            <b-table
              :items="Items"
              :fields="itemFields"
              hover
              responsive
              class="items-table reports-table items-table--compact"
              thead-class="items-table-head"
              tbody-tr-class="items-table-row"
            >
              <template #cell(name)="row">
                <div class="item-product-cell">
                  <div class="item-product-thumb">
                    <img
                      :src="productImageSrc(row.item.image, row.item.imageError)"
                      :alt="row.item.name"
                      class="item-table-image"
                      :class="{
                        'item-table-image--brand-fallback': isProductImageFallback(
                          row.item.image,
                          row.item.imageError
                        ),
                      }"
                      @error="onProductImageError(row.item)"
                    />
                  </div>
                  <div class="item-product-meta">
                    <span class="item-name-text">{{ row.item.name }}</span>
                    <span v-if="row.item.code" class="item-code-text">{{ row.item.code }}</span>
                  </div>
                </div>
              </template>

              <template #cell(sellingPrice)="row">
                <span class="item-price-text">{{ formatPrice(row.item.sellingPrice) }} {{ $t("currency") }}</span>
              </template>

              <template #cell(wholesalePrice)="row">
                <span class="item-price-text item-price-text--muted">{{ formatPrice(row.item.wholesalePrice) }} {{ $t("currency") }}</span>
              </template>

              <template #cell(quantity)="row">
                <span
                  class="item-quantity-badge"
                  :class="quantityCellClass(row.item)"
                >
                  {{ formatQuantity(row.item.quantity) }}
                  <b-icon
                    v-if="isStockAlertActive(row.item)"
                    icon="exclamation-triangle-fill"
                    class="item-stock-alert-icon"
                    :title="$t('stockAlertActiveHint')"
                  />
                </span>
              </template>

              <template #cell(tags)="row">
                <span class="item-tags-badge">{{ row.item.tags || "—" }}</span>
              </template>

              <template #cell(actions)="row">
                <div class="actions-cell items-actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                  <button
                    type="button"
                    class="item-op-btn item-op-btn--edit"
                    @click="getItemInfo(row.item)"
                    :title="$t('editButtonLabel')"
                    :aria-label="$t('editButtonLabel')"
                  >
                    <b-icon icon="pencil-square" class="item-op-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="item-op-btn item-op-btn--print"
                    @click="openPrintLabelsModal(row.item)"
                    :title="$t('printCodeButtonLabel')"
                    :aria-label="$t('printCodeButtonLabel')"
                  >
                    <b-icon icon="printer" class="item-op-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="item-op-btn item-op-btn--codes"
                    @click="openItemCodesModal(row.item)"
                    :title="$t('manageItemCodes') || 'إدارة الأكواد'"
                    :aria-label="$t('manageItemCodes') || 'إدارة الأكواد'"
                  >
                    <b-icon icon="upc" class="item-op-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="item-op-btn item-op-btn--delete"
                    @click="deleteItemModel(row.item.id)"
                    :title="$t('deleteButtonLabel')"
                    :aria-label="$t('deleteButtonLabel')"
                  >
                    <b-icon icon="trash" class="item-op-icon"></b-icon>
                  </button>
                </div>
              </template>
            </b-table>

            <!-- Pagination -->
            <div class="pagination-container" v-if="totalPages > 1">
              <b-pagination
                v-model="pageNumber"
                :total-rows="totalItems"
                :per-page="pageSize"
                :limit="7"
                first-number
                last-number
                @change="onPageChange"
                class="items-pagination"
              ></b-pagination>
              <div class="pagination-info">
                <span>{{ $t('showing') || 'عرض' }} {{ ((pageNumber - 1) * pageSize) + 1 }} - {{ Math.min(pageNumber * pageSize, totalItems) }} {{ $t('of') || 'من' }} {{ totalItems }}</span>
              </div>
            </div>
          </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Import Items Modal -->
      <b-modal id="modal-importItems" hide-header hide-footer class="users-modal" size="lg">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("importItemsTitle") }}</h2>
          <p class="import-items-hint">{{ $t("importItemsHint") }}</p>

          <div class="import-file-section">
            <label
              class="import-file-drop"
              :class="{ 'import-file-drop--selected': !!importFileName }"
            >
              <input
                ref="importFileInput"
                type="file"
                accept=".xlsx,.xls,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel"
                class="import-file-drop__input"
                @change="onImportFileSelected"
              />
              <div class="import-file-drop__content">
                <span class="import-file-drop__icon-wrap">
                  <b-icon icon="file-earmark-excel-fill" class="import-file-drop__icon"></b-icon>
                </span>
                <div class="import-file-drop__text-wrap">
                  <span class="import-file-drop__title">
                    {{ importFileName || $t("importItemsSelectFile") }}
                  </span>
                  <span class="import-file-drop__sub">
                    {{ importFileName ? $t("importItemsChangeFile") : $t("importItemsDropHint") }}
                  </span>
                </div>
                <b-icon icon="cloud-upload-fill" class="import-file-drop__action-icon"></b-icon>
              </div>
            </label>
            <button
              v-if="importFileName"
              type="button"
              class="import-file-clear"
              @click="clearImportFile"
            >
              <b-icon icon="x-circle-fill"></b-icon>
              <span>{{ $t("removeFile") }}</span>
            </button>
          </div>

          <div v-if="importResult" class="import-items-summary">
            <div class="import-items-summary-row">
              <span>{{ $t("importItemsCreated") }}</span>
              <strong>{{ importResult.itemsCreated }}</strong>
            </div>
            <div class="import-items-summary-row">
              <span>{{ $t("importItemsSkipped") }}</span>
              <strong>{{ importResult.itemsSkipped }}</strong>
            </div>
            <div class="import-items-summary-row">
              <span>{{ $t("importTagsCreated") }}</span>
              <strong>{{ importResult.tagsCreated }}</strong>
            </div>
            <div v-if="importResult.rowsWithErrors > 0" class="import-items-errors">
              <p class="import-items-errors-title">{{ $t("importItemsErrors") }} ({{ importResult.rowsWithErrors }})</p>
              <ul>
                <li v-for="(err, idx) in importResult.errors" :key="idx">
                  {{ $t("row") || "صف" }} {{ err.rowNumber }}: {{ mapImportError(err.message) }}
                </li>
              </ul>
            </div>
          </div>

          <div class="users-form-actions">
            <button
              type="button"
              class="users-form-submit-button"
              :disabled="!importFile || importUploading"
              @click="uploadImportFile"
            >
              <b-spinner small v-if="importUploading" class="me-2"></b-spinner>
              <b-icon v-else icon="cloud-upload-fill" class="me-2"></b-icon>
              {{ $t("importItems") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="closeImportModal">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("closeButton") }}
            </button>
          </div>
        </div>
      </b-modal>

      <!-- Add Item Modal -->
      <b-modal id="modal-addItem" :title="$t('addItemModalTitle')" hide-header hide-footer class="users-modal" size="lg" scrollable>
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("addItemModalTitle") }}</h2>
          <form @submit.prevent="addItem" class="users-form">
            <!-- Image Upload Section -->
            <div class="text-center mb-3" style="margin-bottom: 1rem;">
              <input type="file" ref="uploadPhoto" @change="uploadFile" hidden />
              <div @click="getFile" style="cursor: pointer; display: inline-block;">
                <img
                  v-if="!imagePreview"
                  @click="getFile"
                  src="../assets/upload.png"
                  alt="upload"
                  width="120"
                  style="cursor: pointer;"
                />
                <b-avatar v-if="imagePreview" :src="imagePreview" size="6rem"></b-avatar>
              </div>
            </div>

            <!-- Form Fields Grid -->
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
                  {{ $t("itemNamePlaceholder") }}
                </label>
                <input 
                  id="inputName"
                  v-model="addForm.name" 
                  type="text"
                  :placeholder="$t('itemNamePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tags" class="form-label-icon"></b-icon>
                  {{ $t("categoryPlaceholder") }}
                </label>
                <select v-model="addForm.tags" class="users-form-select">
                  <option v-for="item in tags" :value="item.name">{{ item.name }}</option>
                </select>
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="currency-dollar" class="form-label-icon"></b-icon>
                  {{ $t("sellingPricePlaceholder") }}
                </label>
                <input 
                  id="inputSellingPrice"
                  v-model="addForm.sellingPrice" 
                  type="number"
                  :placeholder="$t('sellingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="percent" class="form-label-icon"></b-icon>
                  {{ $t("disCountPricePlaceholder") }}
                </label>
                <input 
                  id="inputDisCountPrice"
                  v-model="addForm.disCountPrice" 
                  type="number"
                  :placeholder="$t('disCountPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="cash-stack" class="form-label-icon"></b-icon>
                  {{ $t("wholesalePricePlaceholder") }}
                </label>
                <input 
                  id="inputWholesalePrice"
                  v-model="addForm.wholesalePrice" 
                  type="number"
                  :placeholder="$t('wholesalePricePlaceholder')" 
                  min="0"
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="cart" class="form-label-icon"></b-icon>
                  {{ $t("purchasingPricePlaceholder") }}
                </label>
                <input 
                  id="inputPurchasingPrice"
                  v-model="addForm.purchasingPrice" 
                  type="number"
                  :placeholder="$t('purchasingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="upc-scan" class="form-label-icon"></b-icon>
                  {{ $t("codePlaceholder") }}
                </label>
                <input 
                  id="inputCode"
                  v-model="addForm.code" 
                  type="text"
                  :placeholder="$t('codePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="box" class="form-label-icon"></b-icon>
                  {{ $t("quantityPlaceholder") || "الكمية" }}
                </label>
                <input 
                  id="inputQuantity"
                  v-model="addForm.quantity" 
                  type="number"
                  :placeholder="$t('quantityPlaceholder') || 'الكمية'" 
                  required 
                  min="0"
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="bell-fill" class="form-label-icon"></b-icon>
                  {{ $t("lowStockAlertQuantityLabel") || "تنبيه الكمية" }}
                </label>
                <input
                  id="inputLowStockAlert"
                  v-model="addForm.lowStockAlertQuantity"
                  type="number"
                  min="0"
                  :placeholder="$t('lowStockAlertQuantityHint') || 'اتركه فارغاً لتعطيل التنبيه'"
                  class="users-form-input"
                />
                <small class="users-form-hint">{{ $t("lowStockAlertQuantityHint") }}</small>
              </div>
            </div>

            <!-- Description Full Width -->
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="file-text" class="form-label-icon"></b-icon>
                {{ $t("descriptionPlaceholder") }}
              </label>
              <input 
                id="inputDescription"
                v-model="addForm.description" 
                type="text"
                :placeholder="$t('descriptionPlaceholder')" 
                class="users-form-input"
              />
            </div>

            <!-- Barcode Preview -->
            <div class="text-center mb-3" v-if="addForm.code.toString()" style="margin-top: 0.5rem;">
              <vue-barcode
                ref="BarImg"
                v-if="addForm.code.toString()"
                tag="img"
                :value="addForm.code.toString()"
                :options="{ displayValue: true, lineColor: '#2B2B2C', width: 2, height: 60 }"
                style="max-width: 200px;"
              />
            </div>

            <!-- Form Actions -->
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("addButton") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addItem')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("closeButton") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Edit Item Modal -->
      <b-modal id="modal-editItem" :title="$t('editItemModalTitle')" hide-header hide-footer class="users-modal" size="lg" scrollable>
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("editItemModalTitle") }}</h2>
          <form @submit.prevent="EditItem" class="users-form">
            <!-- Image Upload Section -->
            <div class="text-center mb-3" style="margin-bottom: 1rem;">
              <input type="file" ref="uploadPhotoEdit" @change="uploadFileEdit" hidden />
              <div @click="getFileEdit" style="cursor: pointer; display: inline-block;">
                <img
                  v-if="!imagePreview && !itemImage"
                  @click="getFileEdit"
                  src="../assets/upload.png"
                  alt="upload"
                  width="120"
                  style="cursor: pointer;"
                />
                <b-avatar v-if="imagePreview || itemImage" :src="imagePreview || itemImage" size="6rem"></b-avatar>
              </div>
            </div>

            <!-- Form Fields Grid -->
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
                  {{ $t("itemNamePlaceholder") }}
                </label>
                <input 
                  id="editInputName"
                  v-model="editForm.name" 
                  type="text"
                  :placeholder="$t('itemNamePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tags" class="form-label-icon"></b-icon>
                  {{ $t("categoryPlaceholder") }}
                </label>
                <select v-model="editForm.tags" class="users-form-select">
                  <option v-for="item in tags" :value="item.name">{{ item.name }}</option>
                </select>
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="currency-dollar" class="form-label-icon"></b-icon>
                  {{ $t("sellingPricePlaceholder") }}
                </label>
                <input 
                  id="editInputSellingPrice"
                  v-model="editForm.sellingPrice" 
                  type="number"
                  :placeholder="$t('sellingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="percent" class="form-label-icon"></b-icon>
                  {{ $t("disCountPricePlaceholder") }}
                </label>
                <input 
                  id="editInputDisCountPrice"
                  v-model="editForm.disCountPrice" 
                  type="number"
                  :placeholder="$t('disCountPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="cash-stack" class="form-label-icon"></b-icon>
                  {{ $t("wholesalePricePlaceholder") }}
                </label>
                <input 
                  id="editInputWholesalePrice"
                  v-model="editForm.wholesalePrice" 
                  type="number"
                  :placeholder="$t('wholesalePricePlaceholder')" 
                  min="0"
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="cart" class="form-label-icon"></b-icon>
                  {{ $t("purchasingPricePlaceholder") }}
                </label>
                <input 
                  id="editInputPurchasingPrice"
                  v-model="editForm.purchasingPrice" 
                  type="number"
                  :placeholder="$t('purchasingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="upc-scan" class="form-label-icon"></b-icon>
                  {{ $t("codePlaceholder") }}
                </label>
                <input 
                  id="editInputCode"
                  v-model="editForm.code" 
                  type="text"
                  :placeholder="$t('codePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="box" class="form-label-icon"></b-icon>
                  {{ $t("quantityPlaceholder") || "الكمية" }}
                </label>
                <input 
                  id="editInputQuantity"
                  v-model="editForm.quantity" 
                  type="number"
                  :placeholder="$t('quantityPlaceholder') || 'الكمية'" 
                  required 
                  min="0"
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="bell-fill" class="form-label-icon"></b-icon>
                  {{ $t("lowStockAlertQuantityLabel") || "تنبيه الكمية" }}
                </label>
                <input
                  id="editInputLowStockAlert"
                  v-model="editForm.lowStockAlertQuantity"
                  type="number"
                  min="0"
                  :placeholder="$t('lowStockAlertQuantityHint') || 'اتركه فارغاً لتعطيل التنبيه'"
                  class="users-form-input"
                />
                <small class="users-form-hint">{{ $t("lowStockAlertQuantityHint") }}</small>
              </div>
            </div>

            <!-- Description Full Width -->
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="file-text" class="form-label-icon"></b-icon>
                {{ $t("descriptionPlaceholder") }}
              </label>
              <input 
                id="editInputDescription"
                v-model="editForm.description" 
                type="text"
                :placeholder="$t('descriptionPlaceholder')" 
                class="users-form-input"
              />
            </div>

            <!-- Barcode Preview -->
            <div class="text-center mb-3" v-if="editForm.code && editForm.code.toString()" style="margin-top: 0.5rem;">
              <vue-barcode
                ref="BarImgEdit"
                v-if="editForm.code.toString()"
                tag="img"
                :value="editForm.code.toString()"
                :options="{ displayValue: true, lineColor: '#2B2B2C', width: 2, height: 60 }"
                style="max-width: 200px;"
              />
            </div>

            <!-- Form Actions -->
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("editItemButtonLabel") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editItem')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("closeButton") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Delete Confirmation Modal -->
      <b-modal id="modal-delete" :title="$t('deleteConfirmationModalTitle')" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <div class="delete-confirmation-content">
            <div class="delete-icon-wrapper">
              <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
            </div>
            <h3 class="delete-confirmation-title">{{ $t("deleteConfirmationMessage") }}</h3>
            <p class="delete-confirmation-text">{{ $t("areYouSureDeleteUser") || 'هل أنت متأكد من حذف هذا المنتج؟' }}</p>
            <div class="delete-confirmation-actions">
              <button class="delete-confirm-button" @click="deleteItem('modal-delete')">
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("deleteButtonLabel") }}
              </button>
              <button class="delete-cancel-button" @click="closeModel('modal-delete')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancelButtonLabel") }}
              </button>
            </div>
          </div>
        </div>
      </b-modal>

      <!-- Manage item QR / barcode codes -->
      <b-modal
        id="modal-itemCodes"
        hide-header
        hide-footer
        class="users-modal"
        size="lg"
        @hidden="resetItemCodesModal"
      >
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("manageItemCodes") || "إدارة أكواد المنتج" }}</h2>
          <p class="item-codes-subtitle" v-if="codesModalItem">
            {{ codesModalItem.name }}
          </p>

          <div class="item-codes-primary" v-if="codesModalItem">
            <label class="users-form-label">{{ $t("primaryItemCode") || "الكود الأساسي" }}</label>
            <div class="item-codes-primary-row">
              <code class="item-codes-primary-value">{{ codesModalPrimary || "—" }}</code>
              <small class="item-codes-hint">{{ $t("primaryItemCodeHint") || "يُعدَّل من شاشة تعديل المنتج" }}</small>
            </div>
            <div class="item-codes-barcode" v-if="codesModalPrimary">
              <vue-barcode
                tag="svg"
                :value="String(codesModalPrimary)"
                :options="{ displayValue: true, width: 1.4, height: 48, margin: 4 }"
              />
            </div>
          </div>

          <div class="item-codes-add-row">
            <input
              v-model="newItemCode"
              type="text"
              class="users-form-input"
              :placeholder="$t('addItemCodePlaceholder') || 'أدخل كود / QR إضافي'"
              @keyup.enter="addItemCode"
            />
            <button
              type="button"
              class="users-form-submit-button item-codes-add-btn"
              :disabled="codesModalSaving || !String(newItemCode || '').trim()"
              @click="addItemCode"
            >
              <b-spinner small v-if="codesModalSaving"></b-spinner>
              <template v-else>
                <b-icon icon="plus-circle-fill" class="me-1"></b-icon>
                {{ $t("addItemCode") || "إضافة" }}
              </template>
            </button>
          </div>

          <div v-if="codesModalLoading" class="item-codes-loading">
            <b-spinner variant="primary"></b-spinner>
          </div>
          <div v-else-if="!codesModalList.length" class="item-codes-empty">
            {{ $t("itemCodesEmpty") || "لا توجد أكواد إضافية بعد" }}
          </div>
          <ul v-else class="item-codes-list">
            <li v-for="row in codesModalList" :key="row.id" class="item-codes-list-item">
              <div class="item-codes-list-main">
                <code>{{ row.code }}</code>
                <vue-barcode
                  tag="svg"
                  class="item-codes-list-barcode"
                  :value="String(row.code)"
                  :options="{ displayValue: false, width: 1.1, height: 32, margin: 0 }"
                />
              </div>
              <button
                type="button"
                class="action-btn action-btn--icon action-btn--delete"
                :disabled="codesModalDeletingId === row.id"
                :title="$t('deleteButtonLabel')"
                @click="deleteItemCode(row.id)"
              >
                <b-spinner small v-if="codesModalDeletingId === row.id"></b-spinner>
                <b-icon v-else icon="trash-fill" class="action-icon"></b-icon>
              </button>
            </li>
          </ul>

          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-itemCodes')">
              {{ $t("closeButton") || "إغلاق" }}
            </button>
          </div>
        </div>
      </b-modal>

      <!-- Print QR / barcode labels (label printer, not A4) -->
      <b-modal
        id="modal-printLabels"
        hide-header
        hide-footer
        class="users-modal"
        size="md"
        @hidden="resetPrintLabelsModal"
      >
        <div class="modal-content-wrapper" v-if="printLabelItem">
          <h2 class="modal-title">{{ $t("printCodeButtonLabel") || "طباعة الكود" }}</h2>
          <p class="users-form-hint">
            {{ $t("printQrLabelHint") || "مخصص لطابعات ملصقات HPRT (مثل N41) — ليس ورق A4. العرض الأدنى 50 مم." }}
          </p>

          <div class="users-form-group">
            <label class="users-form-label">{{ $t("itemNamePlaceholder") || "اسم المنتج" }}</label>
            <input class="users-form-input" type="text" :value="printLabelItem.name" readonly />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("itemCodePlaceholder") || "الكود" }}</label>
            <input class="users-form-input" type="text" :value="printLabelItem.code" readonly />
          </div>

          <div class="users-form-group">
            <label class="users-form-label">{{ $t("printQrLabelSize") || "حجم الملصق" }}</label>
            <select v-model="printLabelSizeId" class="users-form-select">
              <option
                v-for="size in qrLabelSizes"
                :key="size.id"
                :value="size.id"
              >
                {{ formatLabelSizeOption(size) }}
              </option>
            </select>
            <small class="users-form-hint" style="display:block;margin-top:0.4rem;">
              {{ $t("printQrLabelDriverHint") || "في إعدادات طابعة HPRT اختر نفس الحجم، Sensor=Gap، والهوامش 0 بدون Fit to page." }}
            </small>
          </div>

          <div class="users-form-group">
            <label class="users-form-label">{{ $t("printQrLabelOrientation") || "اتجاه الطباعة" }}</label>
            <select v-model="printLabelOrientation" class="users-form-select">
              <option value="landscape">{{ $t("printQrLabelOrientationLandscape") || "بالعرض (أفقي)" }}</option>
              <option value="portrait">{{ $t("printQrLabelOrientationPortrait") || "بالطول (عمودي)" }}</option>
            </select>
            <small class="users-form-hint" style="display:block;margin-top:0.4rem;">
              {{ $t("printQrLabelOrientationHint") || "إذا طُبع الملصق بالطول فقط، اختر بالعرض. تأكد أيضاً أن اتجاه الطابعة في ويندوز/تعريف HPRT مطابق." }}
            </small>
          </div>

          <div class="users-form-group">
            <label class="users-form-label">{{ $t("printQrLabelCopies") || "عدد الملصقات" }}</label>
            <input
              v-model.number="printLabelCopies"
              type="number"
              min="1"
              max="200"
              class="users-form-input"
            />
          </div>

          <div class="users-form-actions">
            <button type="button" class="users-form-submit-button" @click="confirmPrintLabels">
              <b-icon icon="printer-fill" class="me-2"></b-icon>
              {{ $t("print") || "طباعة" }}
            </button>
            <button
              type="button"
              class="users-form-cancel-button"
              @click="closeModel('modal-printLabels')"
            >
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("closeButtonLabel") || $t("close") || "إغلاق" }}
            </button>
          </div>
        </div>
      </b-modal>
    </div>
  </b-overlay>
</template>
<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";

import { HTTP } from "../http/api.js";
import {
  productImageSrc,
  isProductImageFallback,
  onProductImageError,
} from "@/utils/productImage.js";
import {
  QR_LABEL_SIZES,
  DEFAULT_QR_LABEL_SIZE_ID,
  DEFAULT_QR_LABEL_ORIENTATION,
  formatQrLabelSizeOption,
  printQrLabels,
} from "@/utils/qrLabelPrint.js";
export default {
  name: "ItemsView",
  components: {
    AppHeader,
    ClockVue,
    "vue-barcode": VueBarcode,
  },
  data() {
    return {
      selected: null,
      options: ["list", "of", "options"],
      show: false,
      Items: [],
      pageNumber: 1,
      totalItems: 0,
      pageSize: 12,
      search: {
        info: "",
        tag: "",
        stockStatus: "",
      },
      SearchItems: [],
      totalCardItems: 0,
      userInfo: {},
      editForm: {
        name: "",
        description: "",
        sellingPrice: 0,
        purchasingPrice: 0,
        disCountPrice: 0,
        wholesalePrice: 0,
        tags: "مواد اخرى",
        code: "",
        id: "",
        quantity: 0,
        lowStockAlertQuantity: "",
      },
      imagePreview: "",
      itemPhoto: null,
      itemImage: "",
      showUpload: false,
      addForm: {
        name: "",
        description: "",
        sellingPrice: 0,
        purchasingPrice: 0,
        disCountPrice : 0,
        wholesalePrice: 0,
        tags: "مواد اخرى",
        code: "",
        quantity: 0,
        lowStockAlertQuantity: "",
      },
      barCodeList: [],
      printLabelItem: null,
      printLabelCopies: 1,
      printLabelSizeId: DEFAULT_QR_LABEL_SIZE_ID,
      printLabelOrientation: DEFAULT_QR_LABEL_ORIENTATION,
      qrLabelSizes: QR_LABEL_SIZES,
      itemId: "",
      tags: [],
      importFile: null,
      importFileName: "",
      importUploading: false,
      importResult: null,
      codesModalItem: null,
      codesModalPrimary: "",
      codesModalList: [],
      codesModalLoading: false,
      codesModalSaving: false,
      codesModalDeletingId: null,
      newItemCode: "",
    };
  },

  watch: {
    search: {
      handler() {
        if (this.pageNumber !== 1) {
          this.pageNumber = 1;
          return;
        }
        this.GetAllItems();
      },
      deep: true,
    },

    pageNumber() {
      this.GetAllItems();
    },
    
    // if disCountPrice 0 make it equal to sellingPrice
    "addForm.sellingPrice": {
      handler() {
          this.addForm.disCountPrice = this.addForm.sellingPrice;
      },
      deep: true,
    },

    
  },

  mounted() {
    this.getTags();
    this.GetAllItems();
    this.addForm.code = Math.floor(Math.random() * 1000000000).toString();
    this.userInfo = JSON.parse(localStorage.getItem("info"));
  },

  computed: {
    itemFields() {
      return [
        {
          key: 'name',
          label: this.$t('itemNamePlaceholder') || 'اسم المنتج',
          sortable: true,
          thClass: 'item-header-cell item-col-product',
          tdClass: 'item-col-product',
        },
        {
          key: 'sellingPrice',
          label: this.$t('itemPriceLabel') || 'السعر',
          sortable: true,
          thClass: 'item-header-cell item-col-price',
          tdClass: 'item-col-price',
        },
        {
          key: 'wholesalePrice',
          label: this.$t('wholesalePricePlaceholder') || 'سعر الجملة',
          sortable: true,
          thClass: 'item-header-cell item-col-price',
          tdClass: 'item-col-price',
        },
        {
          key: 'quantity',
          label: this.$t('quantityLabel') || this.$t('quantity') || 'الكمية',
          sortable: true,
          thClass: 'item-header-cell item-col-qty',
          tdClass: 'item-col-qty',
        },
        {
          key: 'tags',
          label: this.$t('categoryPlaceholder') || 'القسم',
          sortable: true,
          thClass: 'item-header-cell item-col-tag',
          tdClass: 'item-col-tag',
        },
        {
          key: 'actions',
          label: this.$t('actions') || this.$t('operations') || 'العمليات',
          sortable: false,
          thClass: 'item-header-cell item-col-actions',
          tdClass: 'item-col-actions',
        }
      ];
    },
    totalPages() {
      return Math.ceil(this.totalItems / this.pageSize);
    },
    itemsInStockOnPage() {
      return (this.Items || []).filter((item) => Number(item.quantity) > 0).length;
    },
    itemsOutOfStockOnPage() {
      return (this.Items || []).filter((item) => Number(item.quantity) <= 0).length;
    },
    hasActiveItemFilters() {
      return !!(
        (this.search.info && String(this.search.info).trim()) ||
        this.search.tag ||
        this.search.stockStatus
      );
    },
  },

  methods: {
    productImageSrc,
    isProductImageFallback,
    onProductImageError,
    refreshPage() {
      this.GetAllItems();
    },
    getTags() {
      HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`)
        .then((response) => {
          this.tags = response.data.data.items;
        })
        .catch((error) => {
          this.$notify.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 4000,
          });
        });
    },

    getFile() {
      this.$refs.uploadPhoto.click();
    },

    getFileEdit() {
      this.$refs.uploadPhotoEdit.click();
    },

    uploadFile(event) {
      const selectedFile = event.target.files[0];
      this.itemPhoto = selectedFile;
      if (selectedFile) {
        this.imagePreview = URL.createObjectURL(selectedFile);
        this.showUpload = false;
      }
    },

    uploadFileEdit(event) {
      const selectedFile = event.target.files[0];
      this.itemPhoto = selectedFile;
      if (selectedFile) {
        this.imagePreview = URL.createObjectURL(selectedFile);
        this.showUpload = false;
      }
    },

    openPrintLabelsModal(item) {
      if (!item?.code) {
        this.$notify.error(this.$t("itemCodeRequired") || "لا يوجد كود للمنتج", {
          position: "top-right",
          timeout: 2500,
        });
        return;
      }
      this.printLabelItem = item;
      this.printLabelCopies = 1;
      this.printLabelSizeId = DEFAULT_QR_LABEL_SIZE_ID;
      this.printLabelOrientation = DEFAULT_QR_LABEL_ORIENTATION;
      this.$bvModal.show("modal-printLabels");
    },
    resetPrintLabelsModal() {
      this.printLabelItem = null;
      this.printLabelCopies = 1;
      this.printLabelSizeId = DEFAULT_QR_LABEL_SIZE_ID;
      this.printLabelOrientation = DEFAULT_QR_LABEL_ORIENTATION;
    },
    formatLabelSizeOption(size) {
      return formatQrLabelSizeOption(size, (k) => this.$t(k));
    },
    confirmPrintLabels() {
      if (!this.printLabelItem?.code) return;
      const copies = Math.min(Math.max(Number(this.printLabelCopies) || 1, 1), 200);
      const price = Number(
        this.printLabelItem.disCountPrice > 0
          ? this.printLabelItem.disCountPrice
          : this.printLabelItem.sellingPrice
      );
      const currency = this.$t("currency") || "";
      const priceText =
        Number.isFinite(price) && price > 0
          ? `${price.toLocaleString("en-EG")} ${currency}`.trim()
          : "";

      const ok = printQrLabels(
        {
          code: this.printLabelItem.code,
          name: this.printLabelItem.name,
          priceText,
        },
        {
          copies,
          sizeId: this.printLabelSizeId,
          orientation: this.printLabelOrientation,
        }
      );

      if (!ok) {
        this.$notify.error(
          this.$t("printError") || "تعذرت الطباعة — اسمح بالنوافذ المنبثقة",
          { position: "top-right", timeout: 3500 }
        );
        return;
      }
      this.closeModel("modal-printLabels");
    },

    deleteItemModel(id) {
      this.itemId = id;
      this.$bvModal.show("modal-delete");
    },
    openItemCodesModal(item) {
      this.codesModalItem = item;
      this.codesModalPrimary = item?.code || "";
      this.codesModalList = [];
      this.newItemCode = "";
      this.$bvModal.show("modal-itemCodes");
      this.loadItemCodes();
    },
    resetItemCodesModal() {
      this.codesModalItem = null;
      this.codesModalPrimary = "";
      this.codesModalList = [];
      this.newItemCode = "";
      this.codesModalLoading = false;
      this.codesModalSaving = false;
      this.codesModalDeletingId = null;
    },
    mapItemCodeError(key) {
      if (key && this.$te(key)) return this.$t(key);
      return key || this.$t("error");
    },
    async loadItemCodes() {
      if (!this.codesModalItem?.id) return;
      this.codesModalLoading = true;
      try {
        const response = await HTTP.get(
          `Admin/GetItemCodes?itemId=${this.codesModalItem.id}`
        );
        const data = response?.data?.data;
        this.codesModalPrimary = data?.primaryCode || this.codesModalItem.code || "";
        this.codesModalList = Array.isArray(data?.codes) ? data.codes : [];
      } catch (error) {
        this.$notify.error(this.mapItemCodeError(error?.response?.data?.message), {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } finally {
        this.codesModalLoading = false;
      }
    },
    async addItemCode() {
      const code = String(this.newItemCode || "").trim();
      if (!code || !this.codesModalItem?.id || this.codesModalSaving) return;
      this.codesModalSaving = true;
      try {
        const response = await HTTP.post("Admin/AddItemCode", {
          itemId: this.codesModalItem.id,
          code,
        });
        if (response?.data?.errorStatus) {
          throw { response: { data: { message: response.data.message } } };
        }
        this.newItemCode = "";
        this.$notify.success(this.$t("itemCodeAdded") || "تمت إضافة الكود", {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        await this.loadItemCodes();
      } catch (error) {
        this.$notify.error(this.mapItemCodeError(error?.response?.data?.message), {
          position: "top-right",
          timeout: 3500,
          maxToasts: 1,
        });
      } finally {
        this.codesModalSaving = false;
      }
    },
    async deleteItemCode(id) {
      if (!id || this.codesModalDeletingId) return;
      this.codesModalDeletingId = id;
      try {
        const response = await HTTP.delete(`Admin/DeleteItemCode?id=${id}`);
        if (response?.data?.errorStatus) {
          throw { response: { data: { message: response.data.message } } };
        }
        this.$notify.success(this.$t("itemCodeDeleted") || "تم حذف الكود", {
          position: "top-right",
          timeout: 2500,
          maxToasts: 1,
        });
        await this.loadItemCodes();
      } catch (error) {
        this.$notify.error(this.mapItemCodeError(error?.response?.data?.message), {
          position: "top-right",
          timeout: 3500,
          maxToasts: 1,
        });
      } finally {
        this.codesModalDeletingId = null;
      }
    },
    getItemInfo(item) {
      this.itemPhoto = null;
      this.itemImage = item.image || "";
      this.imagePreview = "";
      this.editForm = {
        id: item.id,
        name: item.name || "",
        description: item.description || "",
        sellingPrice: item.sellingPrice || 0,
        purchasingPrice: item.purchasingPrice || 0,
        disCountPrice: item.disCountPrice || 0,
        wholesalePrice: item.wholesalePrice || 0,
        tags: item.tags || "مواد اخرى",
        code: item.code || "",
        quantity: item.quantity || 0,
        lowStockAlertQuantity:
          item.lowStockAlertQuantity ?? item.LowStockAlertQuantity ?? "",
      };
      this.$bvModal.show("modal-editItem");
    },
    addItem() {
      this.show = true;
      var formData = new FormData();
      formData.append("Name", this.addForm.name);
      formData.append("Description", this.addForm.description);
      formData.append("SellingPrice", this.addForm.sellingPrice);
      formData.append("PurchasingPrice", this.addForm.purchasingPrice);
      formData.append("Tags", this.addForm.tags);
      formData.append("Code", this.addForm.code);
      formData.append("Image", this.itemPhoto);
      formData.append("DisCountPrice", this.addForm.disCountPrice);
      formData.append("WholesalePrice", this.addForm.wholesalePrice);
      formData.append("Quantity", this.addForm.quantity);
      this.appendLowStockAlertQuantity(formData, this.addForm.lowStockAlertQuantity);

      HTTP.post(`Admin/AddItem`, formData)
        .then((response) => {
          this.$notify.success(this.$i18n.t("addItemToOrderSucsses"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
          this.addForm.name = "";
          this.addForm.description = "";
          this.addForm.sellingPrice = 0;
          this.addForm.purchasingPrice = 0;
          this.addForm.code = Math.floor(
            Math.random() * 1000000000000
          ).toString();
          this.addForm.disCountPrice = 0;
          this.addForm.wholesalePrice = 0;
          this.addForm.quantity = 0;
          this.addForm.lowStockAlertQuantity = "";
          this.imagePreview = "";
          this.itemPhoto = null;
          this.GetAllItems();
          this.$bvModal.hide("modal-addItem");
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
        });
    },

    EditItem() {
      var formData = new FormData();
      formData.append("Name", this.editForm.name);
      formData.append("Description", this.editForm.description);
      formData.append("SellingPrice", this.editForm.sellingPrice);
      formData.append("PurchasingPrice", this.editForm.purchasingPrice);
      formData.append("Tags", this.editForm.tags);
      formData.append("Code", this.editForm.code);
      formData.append("Image", this.itemPhoto);
      formData.append("DisCountPrice", this.editForm.disCountPrice);
      formData.append("WholesalePrice", this.editForm.wholesalePrice);
      formData.append("Quantity", this.editForm.quantity);
      this.appendLowStockAlertQuantity(formData, this.editForm.lowStockAlertQuantity, true);

      this.show = true;
      HTTP.put(`Admin/UpdateItem?id=${this.editForm.id}`, formData)
        .then((response) => {
          this.show = false;
          this.$notify.success(this.$i18n.t("itemHadbeenEditSuccessfully"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
          this.GetAllItems();
          this.$bvModal.hide("modal-editItem");
          this.imagePreview = "";
          this.itemImage = "";
          this.itemPhoto = null;
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(this.$i18n.t("somethingWrong"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
        });
    },

    deleteItem(modelId) {
      this.show = true;
      HTTP.delete(`Admin/DeleteItem?id=${this.itemId}`)
        .then((response) => {
          this.show = false;
          this.$notify.success(this.$i18n.t("somethingWrong"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
          this.GetAllItems();
          this.$bvModal.hide(modelId);
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(this.$i18n.t("somethingWrong"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
        });
    },

    formatPrice(price) {
      if (price) {
        return price.toLocaleString("en-EG"); // Use the "ar-EG" locale for Arabic formatting
      }
      return "";
    },
    formatQuantity(quantity) {
      const value = Number(quantity);
      if (Number.isNaN(value)) return "0";
      return value.toLocaleString("en-EG");
    },
    getLowStockAlertThreshold(item) {
      const raw = item?.lowStockAlertQuantity ?? item?.LowStockAlertQuantity;
      if (raw === null || raw === undefined || raw === "") return null;
      const n = Number(raw);
      return Number.isFinite(n) ? n : null;
    },
    isStockAlertActive(item) {
      const threshold = this.getLowStockAlertThreshold(item);
      if (threshold === null) return false;
      return Number(item?.quantity) <= threshold;
    },
    quantityCellClass(item) {
      if (Number(item?.quantity) <= 0) return "item-quantity-text--low";
      if (this.isStockAlertActive(item)) return "item-quantity-text--alert";
      return "";
    },
    appendLowStockAlertQuantity(formData, value, force = false) {
      const isEmpty = value === null || value === undefined || String(value).trim() === "";
      if (force) {
        formData.append("LowStockAlertQuantity", isEmpty ? "" : String(value));
        return;
      }
      if (!isEmpty) {
        formData.append("LowStockAlertQuantity", value);
      }
    },
    closeModel(id) {
      this.$bvModal.hide(id);
      if (id === 'modal-editItem') {
        this.imagePreview = "";
        this.itemImage = "";
        this.itemPhoto = null;
      }
    },

    GetAllItems() {
      this.show = true;
      const params = new URLSearchParams();
      params.append("pageNumber", String(this.pageNumber - 1));
      params.append("pageSize", String(this.pageSize));
      const info = String(this.search.info || "").trim();
      if (info) params.append("info", info);
      if (this.search.tag) params.append("tag", this.search.tag);
      if (this.search.stockStatus) params.append("stockStatus", this.search.stockStatus);

      HTTP.get(`Admin/GetItems?${params.toString()}`)
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
    clearItemFilters() {
      this.search = {
        info: "",
        tag: "",
        stockStatus: "",
      };
    },
    onPageChange(page) {
      this.pageNumber = page;
      this.GetAllItems();
    },
    clearImportFile() {
      this.importFile = null;
      this.importFileName = "";
      this.importResult = null;
      if (this.$refs.importFileInput) {
        this.$refs.importFileInput.value = "";
      }
    },
    onImportFileSelected(event) {
      const file = event.target.files?.[0];
      this.importFile = file || null;
      this.importFileName = file?.name || "";
      this.importResult = null;
    },
    mapImportError(key) {
      if (key && this.$te(key)) return this.$t(key);
      return key || this.$t("error");
    },
    closeImportModal() {
      this.$bvModal.hide("modal-importItems");
      this.resetImportState();
    },
    resetImportState() {
      this.importFile = null;
      this.importFileName = "";
      this.importResult = null;
      this.importUploading = false;
      if (this.$refs.importFileInput) {
        this.$refs.importFileInput.value = "";
      }
    },
    async uploadImportFile() {
      if (!this.importFile || this.importUploading) return;

      this.importUploading = true;
      this.importResult = null;

      const formData = new FormData();
      formData.append("file", this.importFile);

      try {
        const response = await HTTP.post("Admin/ImportItems", formData, {
          headers: { "Content-Type": "multipart/form-data" },
          timeout: 120000,
        });
        const payload = response?.data;
        this.importResult = payload?.data || null;

        this.$notify.success(
          this.$te(payload?.message) ? this.$t(payload.message) : this.$t("importItemsSuccess"),
          { position: "top-right", timeout: 4000, maxToasts: 1 }
        );
        this.GetAllItems();
        this.getTags();
      } catch (error) {
        const msg = error?.response?.data?.message;
        this.$notify.error(this.mapImportError(msg) || this.$t("importItemsFailed"), {
          position: "top-right",
          timeout: 4000,
          maxToasts: 1,
        });
      } finally {
        this.importUploading = false;
      }
    },
  },
};
</script>

<style scoped>
.items-table-container {
  margin-top: 0.75rem;
}

.item-codes-subtitle {
  margin: -0.35rem 0 1rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.item-codes-primary {
  margin-bottom: 1rem;
  padding: 0.85rem 1rem;
  border: 1px solid var(--border-light, #e5e7eb);
  border-radius: 0.75rem;
  background: var(--bg-secondary, #f8fafc);
}

.item-codes-primary-row {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.5rem 0.75rem;
}

.item-codes-primary-value {
  font-size: 1rem;
  font-weight: 700;
}

.item-codes-hint {
  color: var(--text-secondary);
}

.item-codes-barcode {
  margin-top: 0.65rem;
}

.item-codes-add-row {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.65rem;
  margin-bottom: 1rem;
  align-items: center;
}

.item-codes-add-btn {
  white-space: nowrap;
  min-height: 42px;
}

.item-codes-loading,
.item-codes-empty {
  text-align: center;
  padding: 1.25rem;
  color: var(--text-secondary);
}

.item-codes-list {
  list-style: none;
  margin: 0 0 1rem;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  max-height: 280px;
  overflow: auto;
}

.item-codes-list-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.65rem 0.8rem;
  border: 1px solid var(--border-light, #e5e7eb);
  border-radius: 0.65rem;
  background: var(--bg-primary, #fff);
}

.item-codes-list-main {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 0;
}

.item-codes-list-main code {
  font-weight: 700;
  word-break: break-all;
}

.item-codes-list-barcode {
  max-width: 100%;
  overflow: hidden;
}

.items-table {
  margin: 0;
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
}

.items-table >>> thead th .sr-only,
.items-table >>> thead th .visually-hidden {
  display: none !important;
}

.items-table--compact >>> thead th {
  padding: 0.7rem 0.85rem !important;
  font-size: 0.8rem !important;
  font-weight: 700 !important;
  white-space: nowrap;
  vertical-align: middle !important;
  border-bottom: 1px solid var(--border-color) !important;
  background: color-mix(in srgb, var(--primary-color) 7%, var(--bg-primary)) !important;
  color: var(--text-secondary) !important;
}

.items-table--compact >>> tbody td {
  padding: 0.65rem 0.85rem !important;
  vertical-align: middle !important;
  border-top: 1px solid color-mix(in srgb, var(--border-color) 70%, transparent) !important;
}

.items-table--compact >>> tbody tr:hover td {
  background: color-mix(in srgb, var(--primary-color) 5%, var(--bg-primary)) !important;
}

.item-col-product {
  min-width: 14rem;
  width: 32%;
}

.item-col-price {
  width: 12%;
  text-align: center !important;
  white-space: nowrap;
}

.item-col-qty {
  width: 9%;
  text-align: center !important;
  white-space: nowrap;
}

.item-col-tag {
  width: 12%;
  text-align: center !important;
}

.item-col-actions {
  width: 11rem;
  text-align: center !important;
  white-space: nowrap;
}

.item-product-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
  text-align: start;
}

.item-product-thumb {
  flex: 0 0 auto;
  width: 44px;
  height: 44px;
  border-radius: 0.65rem;
  overflow: hidden;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary, #f8fafc);
}

.item-table-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.item-table-image--brand-fallback {
  object-fit: contain;
  padding: 12%;
  background:
    radial-gradient(circle at 50% 40%, color-mix(in srgb, var(--primary-bright, #3db4d0) 22%, transparent), transparent 62%),
    var(--primary-gradient-soft, linear-gradient(160deg, #002536 0%, #0a5a73 100%));
}

.item-product-meta {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
}

.item-name-text {
  font-weight: 700;
  font-size: 0.92rem;
  color: var(--text-primary, #111827);
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 18rem;
}

.item-code-text {
  font-size: 0.75rem;
  color: var(--text-muted, #6b7280);
  font-variant-numeric: tabular-nums;
  direction: ltr;
  text-align: start;
}

.item-price-text {
  font-weight: 700;
  font-size: 0.88rem;
  color: var(--primary-color);
  font-variant-numeric: tabular-nums;
}

.item-price-text--muted {
  color: var(--text-secondary, #475569);
  font-weight: 600;
}

.item-quantity-badge,
.item-quantity-text {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.25rem;
  min-width: 3.25rem;
  padding: 0.28rem 0.55rem;
  border-radius: 999px;
  font-weight: 700;
  font-size: 0.84rem;
  color: var(--text-primary, #111827);
  background: color-mix(in srgb, var(--primary-color) 10%, var(--bg-secondary, #f1f5f9));
  font-variant-numeric: tabular-nums;
}

.item-quantity-badge.item-quantity-text--low,
.item-quantity-text--low {
  color: #b91c1c;
  background: rgba(239, 68, 68, 0.12);
}

.item-quantity-badge.item-quantity-text--alert,
.item-quantity-text--alert {
  color: #b45309;
  background: rgba(245, 158, 11, 0.16);
}

.item-stock-alert-icon {
  color: inherit;
  font-size: 0.78rem;
}

.item-tags-badge,
.item-tags-text {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  max-width: 9rem;
  padding: 0.28rem 0.65rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-secondary, #475569);
  background: var(--bg-secondary, #f1f5f9);
  border: 1px solid var(--border-color);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.users-form-hint {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.8rem;
  color: var(--text-muted, #6c757d);
}

@media (max-width: 768px) {
  .item-name-text {
    max-width: 10rem;
  }
}

.pagination-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background-color: var(--bg-primary);
  border-top: 1px solid var(--border-color);
}

.pagination-info {
  color: var(--text-muted);
  font-size: 0.875rem;
}

.items-pagination >>> .page-link {
  color: var(--text-primary);
  border-color: var(--border-color);
  background-color: var(--bg-tertiary);
}

.items-pagination >>> .page-item.active .page-link {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
}

.items-pagination >>> .page-link:hover {
  background-color: color-mix(in srgb, var(--primary-color) 10%, transparent);
  border-color: var(--border-dark);
  color: var(--primary-color);
}

.import-items-hint {
  margin: 0 0 1rem;
  padding: 0.75rem 1rem;
  font-size: 0.8125rem;
  color: var(--text-secondary);
  line-height: 1.55;
  white-space: pre-line;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
}

.import-file-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.import-file-drop {
  position: relative;
  display: block;
  cursor: pointer;
  border: 2px dashed rgba(13, 110, 47, 0.35);
  border-radius: 0.75rem;
  background: rgba(13, 110, 47, 0.06);
  transition: border-color 0.2s ease, background 0.2s ease, box-shadow 0.2s ease;
}

.import-file-drop:hover {
  border-color: rgba(13, 110, 47, 0.65);
  background: rgba(13, 110, 47, 0.1);
  box-shadow: 0 4px 14px rgba(13, 110, 47, 0.1);
}

.import-file-drop--selected {
  border-style: solid;
  border-color: rgba(13, 110, 47, 0.55);
  background: rgba(13, 110, 47, 0.12);
}

.import-file-drop__input {
  position: absolute;
  width: 0;
  height: 0;
  opacity: 0;
  overflow: hidden;
}

.import-file-drop__content {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 1rem 1.1rem;
}

.import-file-drop__icon-wrap {
  flex-shrink: 0;
  width: 2.75rem;
  height: 2.75rem;
  border-radius: 0.65rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(13, 110, 47, 0.16);
  border: 1px solid rgba(13, 110, 47, 0.28);
}

.import-file-drop__icon {
  font-size: 1.35rem;
  color: #0d6e2f;
}

.import-file-drop__text-wrap {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.import-file-drop__title {
  font-size: 0.9375rem;
  font-weight: 700;
  color: var(--text-primary);
  word-break: break-all;
}

.import-file-drop__sub {
  font-size: 0.78rem;
  color: var(--text-muted);
}

.import-file-drop__action-icon {
  flex-shrink: 0;
  font-size: 1.25rem;
  color: #0d6e2f;
  opacity: 0.85;
}

.import-file-clear {
  align-self: flex-start;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.75rem;
  border: none;
  border-radius: 0.5rem;
  background: transparent;
  color: var(--text-muted);
  font-size: 0.8125rem;
  font-weight: 600;
  cursor: pointer;
  transition: color 0.15s ease, background 0.15s ease;
}

.import-file-clear:hover {
  color: var(--danger-color, #ef4444);
  background: var(--danger-light, rgba(239, 68, 68, 0.12));
}

.import-items-summary {
  margin-bottom: 1rem;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
}

.import-items-summary-row {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.25rem 0;
  font-size: 0.875rem;
}

.import-items-errors {
  margin-top: 0.75rem;
  padding-top: 0.75rem;
  border-top: 1px solid var(--border-color);
}

.import-items-errors-title {
  margin: 0 0 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--danger-color, #dc2626);
}

.import-items-errors ul {
  margin: 0;
  padding-inline-start: 1.25rem;
  font-size: 0.8125rem;
  color: var(--text-secondary);
  max-height: 160px;
  overflow-y: auto;
}

.clear-catalog-list {
  margin: 0 0 1rem;
  padding-inline-start: 1.35rem;
  text-align: start;
  color: var(--text-secondary);
  font-size: 0.875rem;
  line-height: 1.6;
}
</style>
