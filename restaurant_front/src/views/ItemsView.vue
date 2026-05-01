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
      <div class="users-page-container">
        <div class="users-page-content">
          <!-- Header Section -->
          <div class="users-header-section">
            <div class="users-header-content">
              <h1 class="users-page-title">{{ $t("allItemsLabel") }}</h1>
              <div style="display: flex; gap: 0.75rem;">
                <button class="users-add-button ai-generate-button" v-b-modal.modal-ai-generate-items>
                  <b-icon icon="cpu-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t('aiGenerateItems') || 'إنشاء أطباق بالذكاء الاصطناعي' }}</span>
                </button>
                <button class="users-add-button btn-upload-images" @click="showUploadModal = true">
                  <b-icon icon="image-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("uploadImages") || "رفع الصور" }}</span>
                </button>
              <button class="users-add-button" v-b-modal.modal-addItem>
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addItemLabel") }}</span>
              </button>
              </div>
            </div>
          </div>

          <!-- Search Section -->
          <div class="users-search-section">
            <div class="users-search-container">
              <b-icon icon="search" class="search-icon"></b-icon>
              <input 
                v-model="search.info" 
                type="search" 
                :placeholder="$t('searchPlaceholder')"
                class="users-search-input"
              />
            </div>
          </div>

          <!-- Items Table -->
          <div class="items-table-container">
            <b-table
              :items="Items"
              :fields="itemFields"
              striped
              hover
              responsive
              class="items-table"
            >
              <template #cell(image)="row">
                <div class="item-image-cell">
                  <img 
                    v-if="row.item.image && !row.item.imageError" 
                    :src="row.item.image" 
                    :alt="row.item.name"
                    class="item-table-image"
                    @error="row.item.imageError = true"
                  />
                  <div v-else class="item-image-placeholder-small">
                    <b-icon icon="box-fill" class="item-placeholder-icon-small"></b-icon>
                  </div>
                </div>
              </template>

              <template #cell(name)="row">
                <span class="item-name-text">{{ row.item.name }}</span>
              </template>

              <template #cell(sellingPrice)="row">
                <span class="item-price-text">{{ formatPrice(row.item.sellingPrice) }} {{ $t("currency") }}</span>
              </template>

              <template #cell(purchasingPrice)="row">
                <span class="item-cost-text">{{ formatPrice(row.item.purchasingPrice || 0) }} {{ $t("currency") }}</span>
              </template>

              <template #cell(profit)="row">
                <span class="item-profit-text">{{ formatPrice(getItemProfit(row.item)) }} {{ $t("currency") }}</span>
                <span v-if="(row.item.purchasingPrice || 0) > 0" class="item-profit-percent">({{ getItemProfitPercent(row.item) }}%)</span>
              </template>

              <template #cell(isAvailable)="row">
                <span class="item-status-badge" :class="{ 'status-available': row.item.isAvailable, 'status-unavailable': !row.item.isAvailable }">
                  {{ row.item.isAvailable ? ($t("isAvailable") || "متوفر") : ($t("notAvailable") || "غير متوفر") }}
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
                    class="action-btn action-btn--icon action-btn--ai" 
                    @click="generateItemImageWithAI(row.item)"
                    :title="$t('generateImageWithAI') || 'إنشاء صورة بالذكاء الاصطناعي'"
                  >
                    <b-icon icon="image-fill" class="action-icon"></b-icon>
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

      <!-- Add Item Modal -->
      <b-modal
        id="modal-addItem"
        :title="$t('addItemModalTitle')"
        hide-header
        hide-footer
        class="users-modal"
        size="lg"
        scrollable
        @shown="onAddItemModalShown"
      >
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
                  <b-icon icon="diagram-3" class="form-label-icon"></b-icon>
                  {{ $t("itemMainCategory") }}
                </label>
                <select
                  v-model="addFormCategoryRootId"
                  class="users-form-select"
                  required
                  @change="onAddFormRootChange"
                >
                  <option value="">{{ $t("selectMainCategory") }}</option>
                  <option
                    v-for="t in itemRootTags"
                    :key="'add-r-' + (t.id ?? t.Id)"
                    :value="String(t.id ?? t.Id)"
                  >
                    {{ t.name }}
                  </option>
                </select>
              </div>
              <div v-if="addFormSubTagOptions.length" class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tags" class="form-label-icon"></b-icon>
                  {{ $t("itemSubCategory") }}
                </label>
                <select
                  v-model="addFormCategorySubId"
                  class="users-form-select"
                  required
                >
                  <option value="">{{ $t("selectSubCategory") }}</option>
                  <option
                    v-for="t in addFormSubTagOptions"
                    :key="'add-s-' + (t.id ?? t.Id)"
                    :value="String(t.id ?? t.Id)"
                  >
                    {{ t.name }}
                  </option>
                </select>
              </div>
              <div v-else-if="addFormCategoryRootId" class="users-form-group">
                <p class="item-category-hint">
                  <b-icon icon="info-circle" class="me-1"></b-icon>
                  {{ $t("itemCategoryNoSubsHint") }}
                </p>
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
                  {{ $t("purchasingPricePlaceholder") }} ({{ $t("costPriceLabel") || "سعر التكلفة" }})
                </label>
                <input 
                  id="inputPurchasingPrice"
                  v-model.number="addForm.purchasingPrice" 
                  type="number"
                  min="0"
                  step="0.01"
                  :placeholder="$t('purchasingPricePlaceholder')" 
                  class="users-form-input"
                />
                <div v-if="addFormProfit !== null" class="profit-preview">
                  <span class="profit-preview-label">{{ $t("expectedProfitLabel") || "الربح المتوقع" }}:</span>
                  <span class="profit-preview-value">{{ formatPrice(addFormProfit) }} {{ $t("currency") }}</span>
                  <span v-if="addFormProfitPercent !== null" class="profit-preview-percent">({{ addFormProfitPercent }}%)</span>
                </div>
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="check-circle-fill" class="form-label-icon"></b-icon>
                  {{ $t("status") || "الحالة" }}
                </label>
                <select v-model="addForm.isAvailable" class="users-form-select">
                  <option :value="true">{{ $t("isAvailable") || "متوفر" }}</option>
                  <option :value="false">{{ $t("notAvailable") || "غير متوفر" }}</option>
                </select>
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
                  <b-icon icon="diagram-3" class="form-label-icon"></b-icon>
                  {{ $t("itemMainCategory") }}
                </label>
                <select
                  v-model="editFormCategoryRootId"
                  class="users-form-select"
                  required
                  @change="onEditFormRootChange"
                >
                  <option value="">{{ $t("selectMainCategory") }}</option>
                  <option
                    v-for="t in itemRootTags"
                    :key="'ed-r-' + (t.id ?? t.Id)"
                    :value="String(t.id ?? t.Id)"
                  >
                    {{ t.name }}
                  </option>
                </select>
              </div>
              <div v-if="editFormSubTagOptions.length" class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tags" class="form-label-icon"></b-icon>
                  {{ $t("itemSubCategory") }}
                </label>
                <select
                  v-model="editFormCategorySubId"
                  class="users-form-select"
                  required
                >
                  <option value="">{{ $t("selectSubCategory") }}</option>
                  <option
                    v-for="t in editFormSubTagOptions"
                    :key="'ed-s-' + (t.id ?? t.Id)"
                    :value="String(t.id ?? t.Id)"
                  >
                    {{ t.name }}
                  </option>
                </select>
              </div>
              <div v-else-if="editFormCategoryRootId" class="users-form-group">
                <p class="item-category-hint">
                  <b-icon icon="info-circle" class="me-1"></b-icon>
                  {{ $t("itemCategoryNoSubsHint") }}
                </p>
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
                  {{ $t("purchasingPricePlaceholder") }} ({{ $t("costPriceLabel") || "سعر التكلفة" }})
                </label>
                <input 
                  id="editInputPurchasingPrice"
                  v-model.number="editForm.purchasingPrice" 
                  type="number"
                  min="0"
                  step="0.01"
                  :placeholder="$t('purchasingPricePlaceholder')" 
                  class="users-form-input"
                />
                <div v-if="editFormProfit !== null" class="profit-preview">
                  <span class="profit-preview-label">{{ $t("expectedProfitLabel") || "الربح المتوقع" }}:</span>
                  <span class="profit-preview-value">{{ formatPrice(editFormProfit) }} {{ $t("currency") }}</span>
                  <span v-if="editFormProfitPercent !== null" class="profit-preview-percent">({{ editFormProfitPercent }}%)</span>
                </div>
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="check-circle-fill" class="form-label-icon"></b-icon>
                  {{ $t("status") || "الحالة" }}
                </label>
                <select v-model="editForm.isAvailable" class="users-form-select">
                  <option :value="true">{{ $t("isAvailable") || "متوفر" }}</option>
                  <option :value="false">{{ $t("notAvailable") || "غير متوفر" }}</option>
                </select>
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

      <!-- Upload Images Modal -->
      <b-modal 
        v-model="showUploadModal" 
        :title="$t('uploadImages') || 'رفع الصور'"
        @hidden="resetUploadForm"
        hide-header 
        hide-footer 
        class="users-modal" 
        centered
        size="xl"
      >
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("uploadImages") || "رفع الصور" }}</h2>
          
          <!-- Upload Mode Selection -->
          <div class="upload-mode-selection">
            <button 
              class="upload-mode-btn" 
              :class="{ 'active': uploadMode === 'single' }"
              @click="uploadMode = 'single'"
            >
              <b-icon icon="image"></b-icon>
              {{ $t("uploadSingleImage") || "رفع صورة واحدة" }}
            </button>
            <button 
              class="upload-mode-btn" 
              :class="{ 'active': uploadMode === 'multiple' }"
              @click="uploadMode = 'multiple'"
            >
              <b-icon icon="images"></b-icon>
              {{ $t("uploadMultipleImages") || "رفع عدة صور دفعة واحدة" }}
            </button>
          </div>

          <!-- Single Image Upload -->
          <div v-if="uploadMode === 'single'" class="upload-section">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
                {{ $t("selectItem") || "اختر المنتج" }} <span class="required">*</span>
              </label>
              <select 
                v-model="selectedItemId" 
                class="users-form-input"
                required
              >
                <option value="">{{ $t("selectItem") || "اختر المنتج" }}</option>
                <option v-for="item in allItems" :key="item.id" :value="item.id">
                  {{ item.name }} {{ item.code ? `(${item.code})` : '' }}
                </option>
              </select>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="image-fill" class="form-label-icon"></b-icon>
                {{ $t("selectImage") || "اختر الصورة" }} <span class="required">*</span>
              </label>
              <div class="image-upload-area" @click="$refs.singleImageInput.click()">
                <input 
                  ref="singleImageInput"
                  type="file" 
                  accept="image/*" 
                  @change="handleSingleImageSelect"
                  hidden
                />
                <div v-if="!singleImagePreview" class="upload-placeholder">
                  <b-icon icon="cloud-upload" class="upload-icon"></b-icon>
                  <p>{{ $t("clickToSelectImage") || "اضغط لاختيار الصورة" }}</p>
                  <span class="upload-hint">{{ $t("supportedFormats") || "الصيغ المدعومة: JPG, PNG, GIF" }}</span>
                </div>
                <div v-else class="image-preview-container">
                  <img :src="singleImagePreview" alt="Preview" class="image-preview" />
                  <button class="remove-image-btn" @click.stop="removeSingleImage">
                    <b-icon icon="x-circle-fill"></b-icon>
                  </button>
                </div>
              </div>
            </div>
            <div class="users-form-actions">
              <button 
                type="button" 
                class="users-form-submit-button" 
                @click="uploadSingleImage"
                :disabled="!selectedItemId || !singleImageFile || uploadingImage"
              >
                <b-spinner small v-if="uploadingImage" class="me-2"></b-spinner>
                <b-icon v-else icon="upload" class="me-2"></b-icon>
                {{ uploadingImage ? ($t("uploading") || "جاري الرفع...") : ($t("upload") || "رفع") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="showUploadModal = false">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancel") || "إلغاء" }}
              </button>
            </div>
          </div>

          <!-- Multiple Images Upload -->
          <div v-if="uploadMode === 'multiple'" class="upload-section">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="images" class="form-label-icon"></b-icon>
                {{ $t("selectImages") || "اختر الصور" }} <span class="required">*</span>
              </label>
              <div class="image-upload-area" @click="$refs.multipleImagesInput.click()">
                <input 
                  ref="multipleImagesInput"
                  type="file" 
                  accept="image/*" 
                  multiple
                  @change="handleMultipleImagesSelect"
                  hidden
                />
                <div v-if="multipleImagesFiles.length === 0" class="upload-placeholder">
                  <b-icon icon="cloud-upload" class="upload-icon"></b-icon>
                  <p>{{ $t("clickToSelectImages") || "اضغط لاختيار الصور" }}</p>
                  <span class="upload-hint">{{ $t("selectMultipleImages") || "يمكنك اختيار عدة صور دفعة واحدة" }}</span>
                </div>
                <div v-else class="multiple-images-preview">
                  <div 
                    v-for="(file, index) in multipleImagesFiles" 
                    :key="index"
                    class="image-item-preview"
                    @click.stop
                  >
                    <img :src="file.preview" alt="Preview" class="image-preview-small" />
                    <div class="image-item-info" @click.stop>
                      <select 
                        v-model="file.itemId" 
                        class="item-select-small"
                        :placeholder="$t('selectItem') || 'اختر المنتج'"
                        @click.stop
                        @focus.stop
                      >
                        <option value="">{{ $t("selectItem") || "اختر المنتج" }}</option>
                        <option v-for="item in allItems" :key="item.id" :value="item.id">
                          {{ item.name }} {{ item.code ? `(${item.code})` : '' }}
                        </option>
                      </select>
                      <button class="remove-image-btn-small" @click.stop="removeImageItem(index)">
                        <b-icon icon="x-circle-fill"></b-icon>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="users-form-actions">
              <button 
                type="button" 
                class="users-form-submit-button" 
                @click="uploadMultipleImages"
                :disabled="multipleImagesFiles.length === 0 || !allItemsSelected || uploadingImages"
              >
                <b-spinner small v-if="uploadingImages" class="me-2"></b-spinner>
                <b-icon v-else icon="upload" class="me-2"></b-icon>
                {{ uploadingImages ? ($t("uploading") || "جاري الرفع...") : ($t("uploadAll") || `رفع ${multipleImagesFiles.length} صورة`) }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="showUploadModal = false">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancel") || "إلغاء" }}
              </button>
            </div>
          </div>
        </div>
      </b-modal>

      <!-- AI Generate Items Modal -->
      <b-modal id="modal-ai-generate-items" :title="$t('aiGenerateItems') || 'إنشاء أطباق بالذكاء الاصطناعي'" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t('aiGenerateItems') || 'إنشاء أطباق بالذكاء الاصطناعي' }}</h2>
          <form @submit.prevent="generateItemsWithAI" class="users-form">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="file-text" class="form-label-icon"></b-icon>
                {{ $t('enterDescription') }}
              </label>
              <textarea 
                v-model="aiDescription" 
                :placeholder="$t('enterDescription')"
                rows="6"
                required 
                class="users-form-textarea"
              ></textarea>
            </div>
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="generatingItems">
                <b-spinner small v-if="generatingItems" class="me-2"></b-spinner>
                <b-icon icon="magic" class="me-2" v-if="!generatingItems"></b-icon>
                {{ generatingItems ? ($t('generatingItems') || 'جاري إنشاء الأطباق...') : ($t('generateItems') || 'إنشاء الأطباق') }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-ai-generate-items')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t('close') }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Generated Items Modal -->
      <b-modal id="modal-ai-items" :title="$t('generatedItems') || 'الأطباق المقترحة'" hide-header hide-footer class="users-modal" size="xl">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t('generatedItems') || 'الأطباق المقترحة' }}</h2>
          <div class="generated-items-container">
            <div class="items-actions-header">
              <button type="button" class="select-all-button" @click="selectAllItems">
                <b-icon icon="check-square" class="me-2"></b-icon>
                {{ $t('selectAll') }}
              </button>
              <button type="button" class="deselect-all-button" @click="deselectAllItems">
                <b-icon icon="square" class="me-2"></b-icon>
                {{ $t('deselectAll') }}
              </button>
              <button type="button" class="add-more-ai-button" @click="addMoreItemsWithAI" :disabled="generatingMoreItems">
                <b-spinner small v-if="generatingMoreItems" class="me-2"></b-spinner>
                <b-icon icon="arrow-repeat" class="me-2" v-if="!generatingMoreItems"></b-icon>
                {{ generatingMoreItems ? ($t('generatingItems') || 'جاري إنشاء الأطباق...') : ($t('addMoreWithAI') || 'إضافة المزيد') }}
              </button>
              <button type="button" class="add-category-button" @click="addManualItem">
                <b-icon icon="plus-circle" class="me-2"></b-icon>
                {{ $t('addItem') || 'إضافة طبق' }}
              </button>
            </div>
            <div class="items-list">
              <div 
                v-for="(item, index) in generatedItems" 
                :key="index"
                class="item-row"
              >
                <input 
                  type="checkbox" 
                  v-model="item.selected"
                  class="item-checkbox"
                />
                <div class="item-fields">
                  <input 
                    type="text" 
                    v-model="item.name"
                    :placeholder="$t('itemNamePlaceholder') || 'اسم الطبق'"
                    class="item-field-input"
                  />
                  <select
                    v-if="categoryAssignmentOptions.length"
                    v-model="item.category"
                    class="item-field-input"
                  >
                    <option v-for="opt in categoryAssignmentOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                  </select>
                  <input
                    v-else
                    type="text"
                    v-model="item.category"
                    :placeholder="$t('categoryPlaceholder') || 'القسم'"
                    class="item-field-input"
                  />
                  <input 
                    type="number" 
                    v-model.number="item.sellingPrice"
                    :placeholder="$t('sellingPricePlaceholder') || 'سعر البيع'"
                    class="item-field-input"
                    min="0"
                    step="100"
                  />
                  <input 
                    type="text" 
                    v-model="item.description"
                    :placeholder="$t('descriptionPlaceholder') || 'الوصف (اختياري)'"
                    class="item-field-input"
                  />
                </div>
                <button 
                  type="button" 
                  class="remove-item-btn" 
                  @click="removeItem(index)"
                  :title="$t('delete')"
                >
                  <b-icon icon="trash-fill"></b-icon>
                </button>
              </div>
            </div>
            <div v-if="generatedItems.length === 0" class="no-items-message">
              {{ $t('noItemsGenerated') || 'لم يتم إنشاء أي أطباق' }}
            </div>
          </div>
          <div class="users-form-actions">
            <button 
              type="button" 
              class="users-form-submit-button" 
              @click="saveGeneratedItems"
              :disabled="savingItems || selectedItemsCount === 0"
            >
              <b-spinner small v-if="savingItems" class="me-2"></b-spinner>
              <b-icon icon="check-circle-fill" class="me-2" v-if="!savingItems"></b-icon>
              {{ savingItems ? ($t('savingItems') || 'جاري حفظ الأطباق...') : ($t('saveSelectedItems') || 'حفظ الأطباق المحددة') }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-ai-items')">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t('close') }}
            </button>
          </div>
        </div>
      </b-modal>

      <!-- AI Generated Image Modal -->
      <b-modal id="modal-ai-image" :title="$t('generatedImage') || 'الصورة المقترحة'" hide-header hide-footer class="users-modal" size="lg">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t('generatedImage') || 'الصورة المقترحة' }}</h2>
          <div class="ai-image-container">
            <div v-if="generatingImage" class="image-loading">
              <b-spinner class="mb-3"></b-spinner>
              <p>{{ $t('generatingImage') || 'جاري إنشاء الصورة...' }}</p>
            </div>
            <div v-else-if="generatedImageUrl" class="generated-image-preview">
              <img :src="generatedImageUrl" alt="Generated Image" class="preview-image" />
              <div class="image-info">
                <p class="item-name-preview">{{ selectedItemForImage?.name }}</p>
                <p v-if="selectedItemForImage?.description" class="item-description-preview">{{ selectedItemForImage.description }}</p>
              </div>
            </div>
            <div v-else class="no-image-message">
              {{ $t('noImageGenerated') || 'لم يتم إنشاء أي صورة' }}
            </div>
          </div>
          <div class="users-form-actions">
            <button 
              type="button" 
              class="users-form-submit-button" 
              @click="saveGeneratedImage"
              :disabled="savingImage || !generatedImageUrl"
            >
              <b-spinner small v-if="savingImage" class="me-2"></b-spinner>
              <b-icon icon="check-circle-fill" class="me-2" v-if="!savingImage"></b-icon>
              {{ savingImage ? ($t('savingImage') || 'جاري حفظ الصورة...') : ($t('saveImage') || 'حفظ الصورة') }}
            </button>
            <button 
              type="button" 
              class="users-form-submit-button" 
              @click="regenerateImage"
              :disabled="generatingImage || !selectedItemForImage"
            >
              <b-spinner small v-if="generatingImage" class="me-2"></b-spinner>
              <b-icon icon="arrow-repeat" class="me-2" v-if="!generatingImage"></b-icon>
              {{ generatingImage ? ($t('generatingImage') || 'جاري إنشاء الصورة...') : ($t('regenerateImage') || 'إنشاء صورة جديدة') }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-ai-image')">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t('close') }}
            </button>
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
  posCategoryEntries,
  rootTags,
  childTagsOf,
  tagItemStorageValue,
  resolveItemTagsToCategoryIds,
} from "@/utils/tagHierarchy.js";

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
      allItems: [], // جميع المنتجات بدون pagination للاستخدام في الـ select
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
        isAvailable: true,
        tags: "مواد اخرى",
        code: "",
        id: "",
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
        isAvailable: true,
        code: "",
      },
      addFormCategoryRootId: "",
      addFormCategorySubId: "",
      editFormCategoryRootId: "",
      editFormCategorySubId: "",
      barCodeList: [],
      itemId: "",
      tags: [],
      showUploadModal: false,
      uploadMode: 'single', // 'single' or 'multiple'
      selectedItemId: '',
      // AI Generate Items
      aiDescription: '',
      savedAiDescription: '', // حفظ الوصف الأصلي
      generatedItems: [],
      generatingItems: false,
      generatingMoreItems: false,
      savingItems: false,
      // AI Generate Image
      selectedItemForImage: null,
      generatedImageUrl: '',
      generatingImage: false,
      savingImage: false,
      singleImageFile: null,
      singleImagePreview: '',
      multipleImagesFiles: [],
      uploadingImage: false,
      uploadingImages: false,
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
    this.loadAllItemsForSelect();
    this.addForm.code = Math.floor(Math.random() * 1000000000).toString();
    this.userInfo = JSON.parse(localStorage.getItem("info"));
  },

  computed: {
    allItemsSelected() {
      return this.multipleImagesFiles.length > 0 && 
             this.multipleImagesFiles.every(item => item.itemId !== '');
    },
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
          label: this.$t('itemNamePlaceholder') || 'اسم الطبق/المشروب',
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
          key: 'purchasingPrice',
          label: this.$t('costPriceLabel') || 'سعر التكلفة',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'profit',
          label: this.$t('profitPerItemLabel') || 'الربح',
          sortable: false,
          thClass: 'item-header-cell'
        },
        {
          key: 'isAvailable',
          label: this.$t('status') || 'الحالة',
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
    categoryAssignmentOptions() {
      return posCategoryEntries(this.tags);
    },
    itemRootTags() {
      return rootTags(this.tags);
    },
    addFormSelectedRoot() {
      if (!this.addFormCategoryRootId) return null;
      return (
        this.tags.find(
          (t) => String(t.id ?? t.Id) === String(this.addFormCategoryRootId)
        ) || null
      );
    },
    addFormSubTagOptions() {
      return childTagsOf(this.addFormSelectedRoot, this.tags);
    },
    resolvedAddItemTags() {
      const root = this.addFormSelectedRoot;
      if (!root) return null;
      const subs = childTagsOf(root, this.tags);
      if (subs.length === 0) {
        return tagItemStorageValue(root, this.tags);
      }
      if (!this.addFormCategorySubId) return null;
      const sub = this.tags.find(
        (t) => String(t.id ?? t.Id) === String(this.addFormCategorySubId)
      );
      return sub ? tagItemStorageValue(sub, this.tags) : null;
    },
    editFormSelectedRoot() {
      if (!this.editFormCategoryRootId) return null;
      return (
        this.tags.find(
          (t) => String(t.id ?? t.Id) === String(this.editFormCategoryRootId)
        ) || null
      );
    },
    editFormSubTagOptions() {
      return childTagsOf(this.editFormSelectedRoot, this.tags);
    },
    resolvedEditItemTags() {
      const root = this.editFormSelectedRoot;
      if (!root) return null;
      const subs = childTagsOf(root, this.tags);
      if (subs.length === 0) {
        return tagItemStorageValue(root, this.tags);
      }
      if (!this.editFormCategorySubId) return null;
      const sub = this.tags.find(
        (t) => String(t.id ?? t.Id) === String(this.editFormCategorySubId)
      );
      return sub ? tagItemStorageValue(sub, this.tags) : null;
    },
    selectedItemsCount() {
      return this.generatedItems.filter(item => item.selected && item.name && item.name.trim() !== '' && item.sellingPrice > 0).length;
    },
    addFormProfit() {
      const selling = Number(this.addForm.sellingPrice) || 0;
      const cost = Number(this.addForm.purchasingPrice) || 0;
      if (selling <= 0) return null;
      return Math.max(0, selling - cost);
    },
    addFormProfitPercent() {
      const cost = Number(this.addForm.purchasingPrice) || 0;
      if (cost <= 0) return null;
      const p = this.addFormProfit;
      return p !== null ? Math.round((p / cost) * 100) : null;
    },
    editFormProfit() {
      const selling = Number(this.editForm.sellingPrice) || 0;
      const cost = Number(this.editForm.purchasingPrice) || 0;
      if (selling <= 0) return null;
      return Math.max(0, selling - cost);
    },
    editFormProfitPercent() {
      const cost = Number(this.editForm.purchasingPrice) || 0;
      if (cost <= 0) return null;
      const p = this.editFormProfit;
      return p !== null ? Math.round((p / cost) * 100) : null;
    },
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
        isAvailable: item.isAvailable !== undefined ? item.isAvailable : true,
        tags: item.tags || "مواد اخرى",
        code: item.code || "",
      };
      const { rootId, subId } = resolveItemTagsToCategoryIds(
        item.tags || "",
        this.tags
      );
      this.editFormCategoryRootId =
        rootId != null ? String(rootId) : "";
      this.editFormCategorySubId = subId != null ? String(subId) : "";
      this.$bvModal.show("modal-editItem");
    },
    onAddItemModalShown() {
      this.addFormCategoryRootId = "";
      this.addFormCategorySubId = "";
    },
    onAddFormRootChange() {
      this.addFormCategorySubId = "";
    },
    onEditFormRootChange() {
      this.editFormCategorySubId = "";
    },
    addItem() {
      const tagsPayload = this.resolvedAddItemTags;
      if (!tagsPayload) {
        if (!this.addFormCategoryRootId) {
          this.$toast.warning(
            this.$i18n.t("selectCategoryRequired") ||
              "يرجى اختيار القسم",
            { position: "top-right", timeout: 3000 }
          );
        } else {
          this.$toast.warning(
            this.$i18n.t("selectSubCategoryRequired") ||
              "يرجى اختيار قسم فرعي",
            { position: "top-right", timeout: 3000 }
          );
        }
        return;
      }
      this.show = true;
      var formData = new FormData();
      formData.append("Name", this.addForm.name);
      formData.append("Description", this.addForm.description);
      formData.append("SellingPrice", this.addForm.sellingPrice);
      formData.append("PurchasingPrice", this.addForm.purchasingPrice);
      formData.append("IsAvailable", this.addForm.isAvailable);
      formData.append("Tags", tagsPayload);
      formData.append("Code", this.addForm.code);
      formData.append("Image", this.itemPhoto);
      formData.append("DisCountPrice", this.addForm.disCountPrice);

      HTTP.post(`Admin/AddItem`, formData)
        .then((response) => {
          this.show = false;
          this.$toast.success(this.$i18n.t("addItemToOrderSucsses"), {
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
          this.addForm.isAvailable = true;
          this.addForm.code = Math.floor(
            Math.random() * 1000000000000
          ).toString();
          this.addForm.disCountPrice = 0;
          this.addFormCategoryRootId = "";
          this.addFormCategorySubId = "";
          this.imagePreview = "";
          this.itemPhoto = null;
          this.GetAllItems();
          this.$bvModal.hide("modal-addItem");
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(this.$i18n.t("error"), {
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
      const tagsPayload = this.resolvedEditItemTags;
      if (!tagsPayload) {
        if (!this.editFormCategoryRootId) {
          this.$toast.warning(
            this.$i18n.t("selectCategoryRequired") ||
              "يرجى اختيار القسم",
            { position: "top-right", timeout: 3000 }
          );
        } else {
          this.$toast.warning(
            this.$i18n.t("selectSubCategoryRequired") ||
              "يرجى اختيار قسم فرعي",
            { position: "top-right", timeout: 3000 }
          );
        }
        return;
      }
      var formData = new FormData();
      formData.append("Name", this.editForm.name);
      formData.append("Description", this.editForm.description);
      formData.append("SellingPrice", this.editForm.sellingPrice);
      formData.append("PurchasingPrice", this.editForm.purchasingPrice);
      formData.append("IsAvailable", this.editForm.isAvailable);
      formData.append("Tags", tagsPayload);
      formData.append("Code", this.editForm.code);
      formData.append("Image", this.itemPhoto);
      formData.append("DisCountPrice", this.editForm.disCountPrice);

      this.show = true;
      HTTP.put(`Admin/UpdateItem?id=${this.editForm.id}`, formData)
        .then((response) => {
          this.show = false;
          this.$toast.success(this.$i18n.t("itemHadbeenEditSuccessfully"), {
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
          this.$toast.error(this.$i18n.t("somethingWrong"), {
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
          this.$toast.success(this.$i18n.t("somethingWrong"), {
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
          this.$toast.error(this.$i18n.t("somethingWrong"), {
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
    getItemProfit(item) {
      const selling = Number(item.sellingPrice) || 0;
      const cost = Number(item.purchasingPrice) || 0;
      return Math.max(0, selling - cost);
    },
    getItemProfitPercent(item) {
      const cost = Number(item.purchasingPrice) || 0;
      if (cost <= 0) return 0;
      const profit = this.getItemProfit(item);
      return Math.round((profit / cost) * 100);
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
    loadAllItemsForSelect() {
      // جلب جميع المنتجات بدون pagination للاستخدام في الـ select
      HTTP.get(`Admin/GetItems?pageNumber=0&pageSize=10000&info=`)
        .then((response) => {
          if (response.data && response.data.data && response.data.data.items) {
            this.allItems = response.data.data.items.map(item => ({
              ...item,
              imageError: false
            }));
          }
        })
        .catch((error) => {
          console.error('Error loading all items:', error);
        });
    },
    onPageChange(page) {
      this.pageNumber = page;
      this.GetAllItems();
    },
    handleSingleImageSelect(event) {
      const file = event.target.files[0];
      if (file) {
        this.singleImageFile = file;
        const reader = new FileReader();
        reader.onload = (e) => {
          this.singleImagePreview = e.target.result;
        };
        reader.readAsDataURL(file);
      }
    },
    removeSingleImage() {
      this.singleImageFile = null;
      this.singleImagePreview = '';
      if (this.$refs.singleImageInput) {
        this.$refs.singleImageInput.value = '';
      }
    },
    handleMultipleImagesSelect(event) {
      const files = Array.from(event.target.files);
      files.forEach(file => {
        const reader = new FileReader();
        reader.onload = (e) => {
          this.multipleImagesFiles.push({
            file: file,
            preview: e.target.result,
            itemId: ''
          });
        };
        reader.readAsDataURL(file);
      });
    },
    removeImageItem(index) {
      this.multipleImagesFiles.splice(index, 1);
    },
    async uploadSingleImage() {
      if (!this.selectedItemId || !this.singleImageFile) {
        return;
      }

      try {
        this.uploadingImage = true;
        const formData = new FormData();
        formData.append('image', this.singleImageFile);

        const response = await HTTP.post(`Admin/UploadItemImage/${this.selectedItemId}`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });

        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || 'تم رفع الصورة بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });
          this.resetUploadForm();
          this.showUploadModal = false;
          this.GetAllItems();
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء رفع الصورة', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error uploading image:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء رفع الصورة', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.uploadingImage = false;
      }
    },
    async uploadMultipleImages() {
      if (this.multipleImagesFiles.length === 0 || !this.allItemsSelected) {
        return;
      }

      try {
        this.uploadingImages = true;
        const formData = new FormData();
        
        const images = [];
        const itemIds = [];
        
        this.multipleImagesFiles.forEach(item => {
          if (item.itemId) {
            images.push(item.file);
            itemIds.push(item.itemId);
          }
        });

        images.forEach((image, index) => {
          formData.append('images', image);
        });
        
        itemIds.forEach((itemId, index) => {
          formData.append('itemIds', itemId.toString());
        });

        const response = await HTTP.post('Admin/UploadMultipleItemImages', formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });

        if (response.data && !response.data.errorStatus) {
          const successCount = response.data.data.successCount || 0;
          const failCount = response.data.data.failCount || 0;
          
          this.$bvToast.toast(response.data.message || `تم رفع ${successCount} صورة بنجاح`, {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });
          
          if (failCount > 0) {
            this.$bvToast.toast(`فشل رفع ${failCount} صورة`, {
              title: 'تحذير',
              variant: 'warning',
              solid: true
            });
          }
          
          this.resetUploadForm();
          this.showUploadModal = false;
          this.GetAllItems();
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء رفع الصور', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error uploading images:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء رفع الصور', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.uploadingImages = false;
      }
    },
    resetUploadForm() {
      this.uploadMode = 'single';
      this.selectedItemId = '';
      this.singleImageFile = null;
      this.singleImagePreview = '';
      this.multipleImagesFiles = [];
      if (this.$refs.singleImageInput) {
        this.$refs.singleImageInput.value = '';
      }
      if (this.$refs.multipleImagesInput) {
        this.$refs.multipleImagesInput.value = '';
      }
    },

    // AI Generate Items Methods
    async generateItemsWithAI() {
      if (!this.aiDescription || this.aiDescription.trim() === '') {
        this.$toast.error(this.$i18n.t('enterDescription'), {
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
        return;
      }

      this.generatingItems = true;
      try {
        const response = await HTTP.post('Admin/GenerateItemsWithAI', {
          description: this.aiDescription,
          maxItems: 15
        });

        if (response.data.errorStatus) {
          this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
        } else {
          this.generatedItems = response.data.data.map(item => ({
            name: item.name || '',
            category: item.category || 'مواد اخرى',
            sellingPrice: item.sellingPrice || 0,
            purchasingPrice: item.purchasingPrice || 0,
            disCountPrice: item.disCountPrice || item.sellingPrice || 0,
            description: item.description || '',
            selected: true
          }));
          // حفظ الوصف الأصلي للاستخدام لاحقاً
          this.savedAiDescription = this.aiDescription;
          this.$bvModal.hide('modal-ai-generate-items');
          this.$bvModal.show('modal-ai-items');
          this.aiDescription = '';
        }
      } catch (error) {
        this.$toast.error(this.$i18n.t('somethingWrong'), {
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
      } finally {
        this.generatingItems = false;
      }
    },

    async saveGeneratedItems() {
      const selectedItems = this.generatedItems
        .filter(item => item.selected && item.name && item.name.trim() !== '' && item.sellingPrice > 0)
        .map(item => ({
          name: item.name.trim(),
          category: item.category || 'مواد اخرى',
          sellingPrice: item.sellingPrice,
          purchasingPrice: item.purchasingPrice || item.sellingPrice * 0.6,
          disCountPrice: item.disCountPrice || item.sellingPrice,
          description: item.description || null
        }));

      if (selectedItems.length === 0) {
        this.$toast.error(this.$i18n.t('noItemsSelected') || 'لم يتم تحديد أي أطباق', {
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
        return;
      }

      this.savingItems = true;
      try {
        const response = await HTTP.post('Admin/AddMultipleItems', selectedItems);

        if (response.data.errorStatus) {
          this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
        } else {
          this.$toast.success(response.data.message || this.$i18n.t('itemsSavedSuccessfully') || 'تم حفظ الأطباق بنجاح', {
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
          this.generatedItems = [];
          this.savedAiDescription = '';
          this.$bvModal.hide('modal-ai-items');
          this.GetAllItems(); // تحديث قائمة الأطباق
        }
      } catch (error) {
        this.$toast.error(this.$i18n.t('somethingWrong'), {
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
      } finally {
        this.savingItems = false;
      }
    },

    addManualItem() {
      this.generatedItems.push({
        name: '',
        category: 'مواد اخرى',
        sellingPrice: 0,
        purchasingPrice: 0,
        disCountPrice: 0,
        description: '',
        selected: true
      });
    },

    removeItem(index) {
      this.generatedItems.splice(index, 1);
    },

    selectAllItems() {
      this.generatedItems.forEach(item => {
        item.selected = true;
      });
    },

    deselectAllItems() {
      this.generatedItems.forEach(item => {
        item.selected = false;
      });
    },

    async addMoreItemsWithAI() {
      if (!this.savedAiDescription || this.savedAiDescription.trim() === '') {
        this.$toast.error(this.$i18n.t('noOriginalDescription') || 'الوصف الأصلي غير موجود', {
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
        return;
      }

      this.generatingMoreItems = true;
      try {
        const existingItems = this.generatedItems
          .map(item => ({
            name: item.name.trim(),
            category: item.category || ''
          }))
          .filter(item => item.name !== '');

        const response = await HTTP.post('Admin/GenerateItemsWithAI', {
          description: this.savedAiDescription,
          maxItems: 15,
          existingItems: existingItems
        });

        if (response.data.errorStatus) {
          this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
        } else {
          const newItems = response.data.data || [];
          const existingNames = this.generatedItems.map(item => item.name.toLowerCase().trim());
          
          const uniqueNewItems = newItems
            .filter(item => {
              const normalizedName = item.name.toLowerCase().trim();
              return !existingNames.includes(normalizedName);
            })
            .map(item => ({
              name: item.name,
              category: item.category || 'مواد اخرى',
              sellingPrice: item.sellingPrice,
              purchasingPrice: item.purchasingPrice || item.sellingPrice * 0.6,
              disCountPrice: item.disCountPrice || item.sellingPrice,
              description: item.description || '',
              selected: true
            }));

          if (uniqueNewItems.length === 0) {
            this.$toast.info(this.$i18n.t('noNewItemsFound') || 'لم يتم العثور على أطباق جديدة', {
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
          } else {
            this.generatedItems.push(...uniqueNewItems);
            this.$toast.success(`${uniqueNewItems.length} ${this.$i18n.t('newItemsAdded') || 'طبق جديد تم إضافته'}`, {
              position: "top-right",
              timeout: 3000,
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
          }
        }
      } catch (error) {
        this.$toast.error(this.$i18n.t('somethingWrong'), {
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
      } finally {
        this.generatingMoreItems = false;
      }
    },

    // AI Generate Image Methods
    async generateItemImageWithAI(item) {
      this.selectedItemForImage = item;
      this.generatedImageUrl = '';
      this.$bvModal.show('modal-ai-image');
      await this.regenerateImage();
    },

    async regenerateImage() {
      if (!this.selectedItemForImage) return;

      this.generatingImage = true;
      this.generatedImageUrl = '';
      
      try {
        const response = await HTTP.post('Admin/GenerateItemImageWithAI', {
          itemName: this.selectedItemForImage.name,
          description: this.selectedItemForImage.description || null,
          category: this.selectedItemForImage.tags || null
        });

        if (response.data.errorStatus) {
          this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
        } else {
          this.generatedImageUrl = response.data.data;
        }
      } catch (error) {
        this.$toast.error(this.$i18n.t('somethingWrong'), {
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
      } finally {
        this.generatingImage = false;
      }
    },

    async saveGeneratedImage() {
      if (!this.generatedImageUrl || !this.selectedItemForImage) {
        return;
      }

      this.savingImage = true;
      try {
        const response = await HTTP.post(`Admin/SaveGeneratedItemImage/${this.selectedItemForImage.id}`, {
          imageUrl: this.generatedImageUrl
        });

        if (response.data.errorStatus) {
          this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
        } else {
          this.$toast.success(response.data.message || this.$i18n.t('imageSavedSuccessfully') || 'تم حفظ الصورة بنجاح', {
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
          this.generatedImageUrl = '';
          this.selectedItemForImage = null;
          this.$bvModal.hide('modal-ai-image');
          this.GetAllItems(); // تحديث قائمة الأطباق
        }
      } catch (error) {
        this.$toast.error(this.$i18n.t('somethingWrong'), {
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
      } finally {
        this.savingImage = false;
      }
    },
  },
};
</script>

<style scoped>
.items-table-container {
  background: #ffffff;
  border-radius: 0.75rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  margin-top: 1.5rem;
}

.items-table {
  margin: 0;
}

.items-table >>> thead th {
  background-color: #f9fafb;
  color: #374151;
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 1rem;
  border-bottom: 2px solid #e5e7eb;
}

.items-table >>> tbody td {
  padding: 1rem;
  vertical-align: middle;
  border-bottom: 1px solid #f3f4f6;
}

.items-table >>> tbody tr:hover {
  background-color: #f9fafb;
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

.item-cost-text {
  font-size: 0.9375rem;
  color: var(--text-muted);
}

.item-profit-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: var(--success-color);
}

.item-profit-percent {
  font-size: 0.75rem;
  color: var(--success-color);
  margin-right: 0.25rem;
}

.profit-preview {
  margin-top: 0.5rem;
  padding: 0.5rem 0.75rem;
  background: rgba(34, 197, 94, 0.1);
  border-radius: 0.5rem;
  font-size: 0.875rem;
}

.profit-preview-label {
  color: var(--text-secondary);
  margin-left: 0.5rem;
}

.profit-preview-value {
  font-weight: 600;
  color: var(--success-color);
}

.profit-preview-percent {
  color: var(--success-color);
  margin-right: 0.25rem;
}

.item-status-badge {
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  display: inline-block;
}

.item-status-badge.status-available {
  background-color: var(--success-light);
  color: var(--success-color);
}

.item-status-badge.status-unavailable {
  background-color: var(--danger-light);
  color: var(--danger-color);
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

.btn-upload-images {
  background: var(--info-color);
  color: #ffffff;
}

.btn-upload-images:hover {
  background: #0369a1;
}

.upload-mode-selection {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-bottom: 2rem;
}

.upload-mode-btn {
  padding: 1rem;
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  background: var(--bg-secondary);
  color: var(--text-primary);
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  transition: all 0.3s ease;
  font-weight: 600;
}

.upload-mode-btn:hover {
  border-color: var(--primary-color);
  background: var(--bg-primary);
}

.upload-mode-btn.active {
  border-color: var(--primary-color);
  background: rgba(129, 140, 248, 0.1);
  color: var(--primary-color);
}

.upload-section {
  margin-top: 1.5rem;
}

.image-upload-area {
  border: 2px dashed var(--border-color);
  border-radius: 0.75rem;
  padding: 2rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s ease;
  background: var(--bg-secondary);
  min-height: 200px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.image-upload-area:hover {
  border-color: var(--primary-color);
  background: var(--bg-primary);
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.upload-icon {
  font-size: 3rem;
  color: var(--text-secondary);
}

.upload-placeholder p {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
}

.upload-hint {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.image-preview-container {
  position: relative;
  display: inline-block;
}

.image-preview {
  max-width: 300px;
  max-height: 300px;
  border-radius: 0.75rem;
  object-fit: cover;
}

.remove-image-btn {
  position: absolute;
  top: -10px;
  right: -10px;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: var(--danger-color);
  color: #ffffff;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 1.125rem;
  transition: all 0.3s ease;
}

.remove-image-btn:hover {
  transform: scale(1.1);
  box-shadow: var(--shadow-md);
}

.multiple-images-preview {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1rem;
  width: 100%;
}

.image-item-preview {
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  padding: 1rem;
  background: var(--bg-secondary);
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.image-preview-small {
  width: 100%;
  height: 150px;
  object-fit: cover;
  border-radius: 0.5rem;
}

.image-item-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.item-select-small {
  width: 100%;
  padding: 0.5rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.875rem;
}

.remove-image-btn-small {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--danger-color);
  color: #ffffff;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 1rem;
  align-self: flex-end;
  transition: all 0.3s ease;
}

.remove-image-btn-small:hover {
  transform: scale(1.1);
}

/* AI Generate Items Styles */
.ai-generate-button {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
}

.ai-generate-button:hover {
  background: linear-gradient(135deg, #5568d3 0%, #653a8f 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.users-form-textarea {
  width: 100%;
  padding: 0.75rem;
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  font-size: 0.9375rem;
  font-family: inherit;
  background-color: var(--bg-secondary);
  color: var(--text-primary);
  resize: vertical;
  transition: border-color 0.2s ease;
}

.users-form-textarea:focus {
  outline: none;
  border-color: var(--primary-color);
}

.users-form-textarea::placeholder {
  color: var(--text-muted);
}

.generated-items-container {
  margin: 1.5rem 0;
}

.items-actions-header {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.select-all-button,
.deselect-all-button {
  padding: 0.5rem 1rem;
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  background-color: var(--bg-secondary);
  color: var(--text-primary);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
}

.select-all-button:hover,
.deselect-all-button:hover {
  background-color: var(--bg-primary);
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.add-more-ai-button,
.add-category-button {
  padding: 0.5rem 1rem;
  border: 2px solid var(--primary-color);
  border-radius: 0.5rem;
  background-color: var(--primary-color);
  color: white;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
}

.add-more-ai-button:hover:not(:disabled),
.add-category-button:hover {
  background-color: var(--primary-color-dark);
  border-color: var(--primary-color-dark);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(99, 102, 241, 0.3);
}

.add-more-ai-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.items-list {
  max-height: 500px;
  overflow-y: auto;
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  padding: 1rem;
  background-color: var(--bg-secondary);
}

.item-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  margin-bottom: 0.5rem;
  background-color: var(--bg-primary);
  border-radius: 0.5rem;
  border: 1px solid var(--border-color);
  transition: all 0.2s ease;
}

.item-row:hover {
  border-color: var(--primary-color);
  box-shadow: 0 2px 8px rgba(99, 102, 241, 0.1);
}

.item-checkbox {
  width: 20px;
  height: 20px;
  cursor: pointer;
  accent-color: var(--primary-color);
  flex-shrink: 0;
}

.item-fields {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr 2fr;
  gap: 0.5rem;
  flex: 1;
}

.item-field-input {
  padding: 0.5rem 0.75rem;
  border: 2px solid var(--border-color);
  border-radius: 0.375rem;
  font-size: 0.875rem;
  background-color: var(--bg-secondary);
  color: var(--text-primary);
  transition: border-color 0.2s ease;
}

.item-field-input:focus {
  outline: none;
  border-color: var(--primary-color);
}

.remove-item-btn {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 0.375rem;
  background-color: #fee2e2;
  color: #991b1b;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  flex-shrink: 0;
}

.remove-item-btn:hover {
  background-color: #991b1b;
  color: white;
  transform: scale(1.05);
}

.no-items-message {
  text-align: center;
  padding: 2rem;
  color: var(--text-muted);
  font-size: 0.9375rem;
}

@media (max-width: 768px) {
  .item-fields {
    grid-template-columns: 1fr;
  }
}

/* AI Generate Image Styles */
.ai-image-container {
  margin: 1.5rem 0;
  min-height: 400px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.image-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  color: var(--text-muted);
}

.generated-image-preview {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  width: 100%;
}

.preview-image {
  width: 100%;
  max-width: 600px;
  height: auto;
  border-radius: 0.75rem;
  border: 2px solid var(--border-color);
  margin: 0 auto;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.image-info {
  text-align: center;
  padding: 1rem;
  background-color: var(--bg-secondary);
  border-radius: 0.5rem;
}

.item-name-preview {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 0.5rem;
}

.item-description-preview {
  font-size: 0.9375rem;
  color: var(--text-muted);
  margin: 0;
}

.no-image-message {
  text-align: center;
  padding: 3rem;
  color: var(--text-muted);
  font-size: 0.9375rem;
}

.item-category-hint {
  margin: 0;
  font-size: 0.875rem;
  color: #6b7280;
  line-height: 1.5;
  display: flex;
  align-items: flex-start;
  gap: 0.35rem;
}
</style>
