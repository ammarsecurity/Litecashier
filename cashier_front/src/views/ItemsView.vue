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
                  <b-icon icon="box-seam-fill" class="header-icon"></b-icon>
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
                <button
                  v-if="isCommercialUser"
                  type="button"
                  class="catalog-clear-btn"
                  v-b-modal.modal-clearCatalog
                >
                  <b-icon icon="trash-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("clearCatalogData") }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="box-seam-fill"></b-icon>
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
              <div class="app-search-wrap app-search-wrap--wide">
                <b-icon icon="search" class="app-search-icon"></b-icon>
                <input
                  v-model="search.info"
                  type="search"
                  :placeholder="$t('searchPlaceholder')"
                  class="app-search-input"
                  autocomplete="off"
                />
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding">
          <div class="items-table-container report-table-container">
            <b-table
              :items="Items"
              :fields="itemFields"
              striped
              hover
              responsive
              class="items-table reports-table"
            >
              <template #cell(image)="row">
                <div class="item-image-cell">
                  <img
                    :src="productImageSrc(row.item.image, row.item.imageError)"
                    :alt="row.item.name"
                    class="item-table-image"
                    @error="onProductImageError(row.item)"
                  />
                </div>
              </template>

              <template #cell(name)="row">
                <span class="item-name-text">{{ row.item.name }}</span>
              </template>

              <template #cell(sellingPrice)="row">
                <span class="item-price-text">{{ formatPrice(row.item.sellingPrice) }} {{ $t("currency") }}</span>
              </template>

              <template #cell(quantity)="row">
                <span
                  class="item-quantity-text"
                  :class="{ 'item-quantity-text--low': Number(row.item.quantity) <= 0 }"
                >
                  {{ formatQuantity(row.item.quantity) }}
                </span>
              </template>

              <template #cell(tags)="row">
                <span class="item-tags-text">{{ row.item.tags }}</span>
              </template>

              <template #cell(actions)="row">
                <div class="actions-cell">
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--edit"
                    @click="getItemInfo(row.item)"
                    :title="$t('editButtonLabel')"
                  >
                    <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--print"
                    @click="printListOfCode(row.item, 30)"
                    :title="$t('printCodeButtonLabel')"
                  >
                    <b-icon icon="printer-fill" class="action-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--delete"
                    @click="deleteItemModel(row.item.id)"
                    :title="$t('deleteButtonLabel')"
                  >
                    <b-icon icon="trash-fill" class="action-icon"></b-icon>
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

      <!-- Clear catalog modal -->
      <b-modal id="modal-clearCatalog" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <div class="delete-confirmation-content">
            <div class="delete-icon-wrapper">
              <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
            </div>
            <h3 class="delete-confirmation-title">{{ $t("clearCatalogTitle") }}</h3>
            <p class="delete-confirmation-text">{{ $t("clearCatalogWarning") }}</p>
            <ul class="clear-catalog-list">
              <li>{{ $t("clearCatalogTags") }}</li>
              <li>{{ $t("clearCatalogItems") }}</li>
              <li>{{ $t("clearCatalogOrders") }}</li>
            </ul>
            <div class="users-form-group">
              <label class="users-form-label">{{ $t("clearCatalogPasswordLabel") }}</label>
              <input
                v-model="clearCatalogPassword"
                type="password"
                class="users-form-input"
                :placeholder="$t('clearCatalogPasswordPlaceholder')"
                autocomplete="current-password"
                @keyup.enter="executeClearCatalog"
              />
            </div>
            <div v-if="clearCatalogResult" class="import-items-summary">
              <div class="import-items-summary-row">
                <span>{{ $t("clearCatalogTags") }}</span>
                <strong>{{ clearCatalogResult.tagsCleared }}</strong>
              </div>
              <div class="import-items-summary-row">
                <span>{{ $t("clearCatalogItems") }}</span>
                <strong>{{ clearCatalogResult.itemsCleared }}</strong>
              </div>
              <div class="import-items-summary-row">
                <span>{{ $t("clearCatalogOrders") }}</span>
                <strong>{{ clearCatalogResult.ordersCleared }}</strong>
              </div>
            </div>
            <div class="delete-confirmation-actions">
              <button
                type="button"
                class="delete-confirm-button"
                :disabled="clearCatalogLoading || !clearCatalogPassword"
                @click="executeClearCatalog"
              >
                <b-spinner small v-if="clearCatalogLoading" class="me-2"></b-spinner>
                <b-icon v-else icon="trash-fill" class="me-2"></b-icon>
                {{ $t("clearCatalogConfirm") }}
              </button>
              <button
                type="button"
                class="delete-cancel-button"
                :disabled="clearCatalogLoading"
                @click="closeClearCatalogModal"
              >
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancelButtonLabel") }}
              </button>
            </div>
          </div>
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

      <!-- Print Barcode (Hidden) -->
      <div id="printMe" class="text-align-center" style="display: none;">
        <b-row>
          <b-col
            class="text-align-center"
            sm="3"
            md="3"
            lg="3"
            v-for="item in barCodeList"
            :key="item.code"
          >
            <vue-barcode
              ref="BarImg"
              v-if="item.code.toString()"
              tag="img"
              :value="item.code.toString()"
              :options="{ displayValue: true, lineColor: '#2B2B2C' }"
            />
            <p class="item-name-center">{{ item.name }}</p>
          </b-col>
        </b-row>
      </div>
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
  onProductImageError,
} from "@/utils/productImage.js";
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
      search: "",
      Items: [],
      pageNumber: 1,
      totalItems: 0,
      pageSize: 12,
      search: {
        info: "",
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
        tags: "مواد اخرى",
        code: "",
        id: "",
        quantity: 0,
      },
      imagePreview: "",
      itemImage: "",
      showUpload: false,
      addForm: {
        name: "",
        description: "",
        sellingPrice: 0,
        purchasingPrice: 0,
        disCountPrice : 0,
        tags: "مواد اخرى",
        code: "",
        quantity: 0,
      },
      barCodeList: [],
      itemId: "",
      tags: [],
      importFile: null,
      importFileName: "",
      importUploading: false,
      importResult: null,
      clearCatalogPassword: "",
      clearCatalogLoading: false,
      clearCatalogResult: null,
    };
  },

  watch: {
    search: {
      handler() {
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
          key: 'image',
          label: '',
          sortable: false,
          thClass: 'item-header-cell',
          tdClass: 'item-image-column'
        },
        {
          key: 'name',
          label: this.$t('itemNamePlaceholder') || 'اسم المنتج',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'sellingPrice',
          label: this.$t('itemPriceLabel') || 'السعر',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'quantity',
          label: this.$t('quantityLabel') || this.$t('quantity') || 'الكمية',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'tags',
          label: this.$t('categoryPlaceholder') || 'القسم',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'actions',
          label: this.$t('actions') || 'الإجراءات',
          sortable: false,
          thClass: 'item-header-cell'
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
    isCommercialUser() {
      return localStorage.getItem("role") === "Commercial";
    },
  },

  methods: {
    productImageSrc,
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

    printListOfCode(code, count) {
      this.barCodeList = [];
      for (let index = 0; index < count; index++) {
        this.barCodeList.push({ code: code.code, name: code.name });
      }
      this.$nextTick(() => {
        this.print();
      });
    },
    print() {
      const printContents = document.getElementById("printMe").innerHTML;
      const printWindow = window.open("", "_blank");
      const originalHead = document.head.innerHTML;

      // Create the content for the new window
      const newContent = `
    <html>
      <head>
        ${originalHead}
      </head>
      <body dir="rtl">
        ${printContents}
      </body>
    </html>
  `;

      printWindow.document.open();
      printWindow.document.write(newContent);
      printWindow.document.close();

      // Wait for the window to load its content before printing
      printWindow.onload = () => {
        printWindow.print();
        printWindow.close();
      };
    },

    deleteItemModel(id) {
      this.itemId = id;
      this.$bvModal.show("modal-delete");
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
        tags: item.tags || "مواد اخرى",
        code: item.code || "",
        quantity: item.quantity || 0,
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
      formData.append("Quantity", this.addForm.quantity);

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
          this.addForm.quantity = 0;
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
      formData.append("Quantity", this.editForm.quantity);

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
    closeClearCatalogModal() {
      this.$bvModal.hide("modal-clearCatalog");
      this.clearCatalogPassword = "";
      this.clearCatalogResult = null;
      this.clearCatalogLoading = false;
    },
    async executeClearCatalog() {
      if (!this.clearCatalogPassword || this.clearCatalogLoading) return;

      this.clearCatalogLoading = true;
      this.clearCatalogResult = null;

      try {
        const response = await HTTP.post("Admin/ClearCatalog", {
          password: this.clearCatalogPassword,
        });
        const payload = response?.data;
        this.clearCatalogResult = payload?.data || null;

        this.$notify.success(
          this.$te(payload?.message) ? this.$t(payload.message) : this.$t("catalogClearSuccess"),
          { position: "top-right", timeout: 4500, maxToasts: 1 }
        );
        this.GetAllItems();
        this.getTags();
      } catch (error) {
        const msg = error?.response?.data?.message;
        const text =
          msg && this.$te(msg)
            ? this.$t(msg)
            : this.$t("catalogClearFailed");
        this.$notify.error(text, {
          position: "top-right",
          timeout: 4000,
          maxToasts: 1,
        });
      } finally {
        this.clearCatalogLoading = false;
      }
    },
  },
};
</script>

<style scoped>
.items-table-container {
  margin-top: 1.5rem;
}

.items-table {
  margin: 0;
}

.items-table >>> thead th .sr-only,
.items-table >>> thead th .visually-hidden {
  display: none !important;
}

.item-image-column {
  width: 80px;
}

.item-image-cell {
  display: flex;
  align-items: center;
  justify-content: center;
}

.item-table-image {
  width: 60px;
  height: 60px;
  object-fit: cover;
  border-radius: 0.5rem;
}

.item-image-placeholder-small {
  width: 60px;
  height: 60px;
  background-color: #f3f4f6;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #9ca3af;
}

.item-placeholder-icon-small {
  font-size: 1.5rem;
}

.item-name-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: #111827;
}

.item-price-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: var(--primary-color);
}

.item-quantity-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: #111827;
  font-variant-numeric: tabular-nums;
}

.item-quantity-text--low {
  color: var(--danger-color, #dc2626);
}

.item-tags-text {
  color: var(--text-muted);
  font-size: 0.875rem;
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
  background-color: rgba(99, 102, 241, 0.1);
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
