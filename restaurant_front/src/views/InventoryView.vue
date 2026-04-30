<template>
  <b-overlay :show="false" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <div class="users-header-section">
            <div class="users-header-content">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="box-seam" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("inventoryTitle") || "مخزن المواد" }}</h1>
                  <p class="header-subtitle">{{ $t("inventorySubtitle") || "عرض الكميات وإضافة وسحب المخزون — اسم المادة كتابة حرة (لا علاقة بالأطباق/المشروبات)" }}</p>
                </div>
              </div>
              <button class="users-add-button" @click="openAddModal()">
                <b-icon icon="plus-circle" class="me-1"></b-icon>
                {{ $t("addStock") || "إضافة دخول مخزون" }}
              </button>
            </div>
          </div>

          <!-- تابات: مخزن المواد | سجل الحركات | إدارة الموردين -->
          <div class="reports-tabs-section inventory-tabs-section">
            <div class="reports-tabs">
              <button
                class="report-tab"
                :class="{ 'report-tab-active': activeInventoryTab === 'stock' }"
                @click="activeInventoryTab = 'stock'; onStockTabClick()"
              >
                <b-icon icon="box-seam" class="me-2"></b-icon>
                {{ $t("inventory") || "مخزن المواد" }}
              </button>
              <button
                class="report-tab"
                :class="{ 'report-tab-active': activeInventoryTab === 'movements' }"
                @click="activeInventoryTab = 'movements'; onMovementsTabClick()"
              >
                <b-icon icon="arrow-left-right" class="me-2"></b-icon>
                {{ $t("movementsHistory") || "سجل الحركات (إضافة وسحب)" }}
              </button>
              <button
                class="report-tab"
                :class="{ 'report-tab-active': activeInventoryTab === 'suppliers' }"
                @click="activeInventoryTab = 'suppliers'; onSuppliersTabClick()"
              >
                <b-icon icon="people" class="me-2"></b-icon>
                {{ $t("manageSuppliers") || "إدارة الموردين" }}
              </button>
            </div>
          </div>

          <!-- محتوى تاب مخزن المواد -->
          <div v-if="activeInventoryTab === 'stock'" class="inventory-tab-content">
            <div class="users-search-section">
              <div class="users-search-container">
                <b-icon icon="search" class="search-icon"></b-icon>
                <input
                  v-model="searchQuery"
                  type="text"
                  :placeholder="$t('search') || 'بحث...'"
                  class="users-search-input"
                  @input="debounceSearch"
                />
              </div>
            </div>
            <div class="users-grid-container">
              <div v-if="loading" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else class="table-responsive">
                <table class="table users-table">
                  <thead>
                    <tr>
                      <th>{{ $t("itemName") || "اسم المادة" }}</th>
                      <th>{{ $t("currentStock") || "الكمية الحالية" }}</th>
                      <th>{{ $t("totalAdded") || "إجمالي الداخل" }}</th>
                      <th>{{ $t("totalWithdrawn") || "إجمالي السحب" }}</th>
                      <th>{{ $t("unitType") || "الوحدة" }}</th>
                      <th>{{ $t("supplierName") || "المورد" }}</th>
                      <th>{{ $t("receiptNumber") || "رقم الوصل" }}</th>
                      <th>{{ $t("date") || "التاريخ" }}</th>
                      <th>{{ $t("actions") || "العمليات" }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(row, idx) in items" :key="(row.materialName || '') + '-' + idx">
                      <td>{{ row.materialName }}</td>
                      <td>{{ formatNumber(row.currentQuantity) }}</td>
                      <td>{{ formatNumber(row.totalAdded) }}</td>
                      <td>{{ formatNumber(row.totalWithdrawn) }}</td>
                      <td>{{ row.unitType || ($t("piece") || "قطعة") }}</td>
                      <td>{{ row.lastSupplierName || '—' }}</td>
                      <td>{{ row.lastReceiptNumber || '—' }}</td>
                      <td>{{ formatMovementDate(row.lastMovementDate) }}</td>
                      <td>
                        <button class="btn-inventory-add" @click="openAddModal(row)">
                          <b-icon icon="plus-circle" class="me-1"></b-icon>
                          {{ $t("add") || "إضافة" }}
                        </button>
                        <button class="btn-inventory-withdraw" @click="openWithdrawModal(row)">
                          <b-icon icon="dash-circle" class="me-1"></b-icon>
                          {{ $t("withdraw") || "سحب" }}
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div v-if="items.length === 0 && !loading" class="empty-state">
              <b-icon icon="box-seam" class="empty-icon"></b-icon>
              <p class="empty-text">{{ $t("noInventoryItems") || "لا توجد مواد في المخزن أو لا توجد نتائج" }}</p>
            </div>
          </div>

          <!-- محتوى تاب سجل الحركات -->
          <div v-if="activeInventoryTab === 'movements'" class="inventory-tab-content">
              <div class="movements-filters">
                <input
                  v-model="movementFilterMaterial"
                  type="text"
                  :placeholder="$t('itemName') || 'اسم المادة'"
                  class="users-search-input movements-filter-input"
                  @input="debounceLoadMovements"
                />
                <input
                  v-model="movementFilterReceiptNumber"
                  type="text"
                  :placeholder="$t('receiptNumber') || 'رقم الوصل'"
                  class="users-search-input movements-filter-input"
                  @input="debounceLoadMovements"
                />
                <select v-model="movementFilterType" class="users-search-input movements-filter-input" @change="loadStockMovements">
                  <option value="">{{ $t("all") || "الكل" }}</option>
                  <option value="Add">{{ $t("add") || "إضافة" }}</option>
                  <option value="Withdraw">{{ $t("withdraw") || "سحب" }}</option>
                </select>
                <button type="button" class="btn-refresh-movements" @click="loadStockMovements">
                  <b-icon icon="arrow-clockwise"></b-icon>
                  {{ $t("refresh") || "تحديث" }}
                </button>
              </div>
              <div v-if="loadingMovements" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else class="table-responsive">
                <table class="table users-table movements-table">
                  <thead>
                    <tr>
                      <th>{{ $t("date") || "التاريخ" }}</th>
                      <th>{{ $t("itemName") || "اسم المادة" }}</th>
                      <th>{{ $t("movementType") || "النوع" }}</th>
                      <th>{{ $t("quantity") || "الكمية" }}</th>
                      <th>{{ $t("supplierName") || "المورد" }}</th>
                      <th>{{ $t("amount") || "المبلغ" }}</th>
                      <th>{{ $t("receiptNumber") || "رقم الوصل" }}</th>
                      <th>{{ $t("receiptAttachment") || "مرفق الوصل" }}</th>
                      <th>{{ $t("unitType") || "الوحدة" }}</th>
                      <th>{{ $t("notes") || "ملاحظات" }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="m in movementsList" :key="m.id" :class="m.movementType === 'Add' ? 'movement-add' : 'movement-withdraw'">
                      <td>{{ formatMovementDate(m.insertDate) }}</td>
                      <td>{{ m.materialName }}</td>
                      <td>
                        <span :class="m.movementType === 'Add' ? 'badge-add' : 'badge-withdraw'">
                          {{ m.movementType === 'Add' ? ($t('add') || 'إضافة') : ($t('withdraw') || 'سحب') }}
                        </span>
                      </td>
                      <td>{{ formatNumber(m.quantity) }}</td>
                      <td>{{ m.supplierName || '—' }}</td>
                      <td>{{ m.amount != null ? formatNumber(m.amount) : '—' }}</td>
                      <td>{{ m.receiptNumber || '—' }}</td>
                      <td>
                        <a
                          v-if="m.receiptAttachmentPath"
                          :href="buildReceiptUrl(m.receiptAttachmentPath)"
                          target="_blank"
                          rel="noopener"
                          class="receipt-link"
                        >
                          <b-icon icon="paperclip"></b-icon>
                          {{ $t("open") || "فتح" }}
                        </a>
                        <span v-else>—</span>
                      </td>
                      <td>{{ m.unitType || '—' }}</td>
                      <td>{{ m.notes || '—' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div v-if="movementsList.length === 0 && !loadingMovements" class="empty-movements">
                {{ $t("noMovements") || "لا توجد حركات" }}
              </div>
              <div v-if="movementsTotal > movementsPageSize" class="movements-pagination">
                <b-pagination
                  v-model="movementsPage"
                  :total-rows="movementsTotal"
                  :per-page="movementsPageSize"
                  size="sm"
                  @change="loadStockMovements"
                ></b-pagination>
              </div>
          </div>

          <!-- محتوى تاب إدارة الموردين -->
          <div v-if="activeInventoryTab === 'suppliers'" class="inventory-tab-content">
            <div class="suppliers-tab-header">
              <button type="button" class="btn-add-supplier" @click="openAddSupplierModal">
                <b-icon icon="plus-circle" class="me-1"></b-icon>
                {{ $t("addSupplier") || "إضافة مورد" }}
              </button>
            </div>
            <div v-if="loadingSuppliers" class="loading-state-full">
              <b-spinner variant="primary"></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else class="table-responsive">
              <table class="table users-table movements-table">
                <thead>
                  <tr>
                    <th>{{ $t("supplierName") || "اسم المورد" }}</th>
                    <th>{{ $t("notes") || "ملاحظات" }}</th>
                    <th>{{ $t("actions") || "العمليات" }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="s in suppliersList" :key="s.id">
                    <td>{{ s.name }}</td>
                    <td>{{ s.notes || '—' }}</td>
                    <td>
                      <button type="button" class="btn-inventory-add btn-sm me-1" @click="openEditSupplierModal(s)">{{ $t("editSupplier") || "تعديل" }}</button>
                      <button type="button" class="btn-inventory-withdraw btn-sm" @click="confirmDeleteSupplier(s)">{{ $t("deleteSupplier") || "حذف" }}</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div v-if="suppliersList.length === 0 && !loadingSuppliers" class="empty-movements">
              {{ $t("noSuppliers") || "لا يوجد موردين" }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal: إضافة مخزون -->
    <b-modal
      v-model="showAddModal"
      :title="$t('addStock') || 'إضافة دخول مخزون'"
      @hidden="resetAddForm"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addStock") || "إضافة دخول مخزون" }}</h2>
        <form @submit.prevent="submitAddStock" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="person-badge" class="form-label-icon"></b-icon>
                {{ $t("supplierName") || "اسم المورد" }}
              </label>
              <select v-model="addForm.supplierSelect" class="users-form-input" @change="onSupplierSelectChange">
                <option value="">{{ $t("selectSupplier") || "اختر المورد" }}</option>
                <option v-for="s in suppliersList" :key="s.id" :value="'id_' + s.id">{{ s.name }}</option>
                <option value="__other__">{{ $t("otherSupplier") || "مورد آخر" }}</option>
              </select>
              <input
                v-if="addForm.supplierSelect === '__other__'"
                v-model="addForm.supplierOtherName"
                type="text"
                class="users-form-input mt-2"
                :placeholder="$t('supplierNamePlaceholder') || 'اسم المورد'"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="receipt" class="form-label-icon"></b-icon>
                {{ $t("receiptNumber") || "رقم الوصل" }}
              </label>
              <input
                v-model="addForm.receiptNumber"
                type="text"
                class="users-form-input"
                :placeholder="$t('receiptNumberPlaceholder') || 'أدخل رقم الوصل'"
              />
            </div>
          </div>

          <div class="users-form-group">
            <div class="stock-items-header">
              <label class="users-form-label mb-0">
                <b-icon icon="list-ul" class="form-label-icon"></b-icon>
                {{ $t("inventoryItemsList") || "قائمة المواد المدخلة" }}
              </label>
              <button type="button" class="btn-inventory-add" @click="addStockItemRow">
                <b-icon icon="plus-circle" class="me-1"></b-icon>
                {{ $t("addItem") || "إضافة منتج" }}
              </button>
            </div>

            <div class="table-responsive">
              <table class="table users-table movements-table stock-items-table">
                <thead>
                  <tr>
                    <th>{{ $t("itemName") || "اسم المادة" }}</th>
                    <th>{{ $t("unitPrice") || "سعر الوحدة" }}</th>
                    <th>{{ $t("quantity") || "الكمية" }}</th>
                    <th>{{ $t("amount") || "المبلغ" }}</th>
                    <th>{{ $t("unitType") || "الوحدة" }}</th>
                    <th>{{ $t("actions") || "العمليات" }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(itemRow, rowIndex) in addForm.items" :key="'stock-item-' + rowIndex">
                    <td>
                      <input
                        v-model="itemRow.materialName"
                        type="text"
                        class="users-form-input"
                        :placeholder="$t('materialNamePlaceholder') || 'اكتب اسم المادة'"
                      />
                    </td>
                    <td>
                      <input
                        v-model.number="itemRow.unitPrice"
                        type="number"
                        min="0"
                        step="0.01"
                        class="users-form-input"
                        :placeholder="$t('unitPrice') || 'سعر الوحدة'"
                      />
                    </td>
                    <td>
                      <input
                        v-model.number="itemRow.quantity"
                        type="number"
                        min="0.01"
                        step="0.01"
                        class="users-form-input"
                        :placeholder="$t('quantityPlaceholder') || 'الكمية'"
                      />
                    </td>
                    <td>{{ formatNumber(calculateRowAmount(itemRow)) }}</td>
                    <td>
                      <select v-model="itemRow.unitType" class="users-form-input">
                        <option value="">{{ $t("selectUnit") || "اختر الوحدة" }}</option>
                        <option value="قطعة">{{ $t("piece") || "قطعة" }}</option>
                        <option value="كارتون">{{ $t("carton") || "كارتون" }}</option>
                        <option value="كيلو">{{ $t("kilo") || "كيلو" }}</option>
                        <option value="لتر">{{ $t("liter") || "لتر" }}</option>
                        <option value="علبة">{{ $t("box") || "علبة" }}</option>
                        <option value="أخرى">{{ $t("other") || "أخرى" }}</option>
                      </select>
                    </td>
                    <td>
                      <button type="button" class="btn-inventory-withdraw" @click="removeStockItemRow(rowIndex)">
                        <b-icon icon="trash"></b-icon>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="stock-total-row">
            <span>{{ $t("totalAmount") || "إجمالي المبلغ" }}</span>
            <strong>{{ formatNumber(totalStockInvoiceAmount) }}</strong>
          </div>

          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="paperclip" class="form-label-icon"></b-icon>
                {{ $t("receiptAttachment") || "مرفق الوصل" }}
              </label>
              <input
                ref="receiptInput"
                type="file"
                accept=".jpg,.jpeg,.png,.gif,.pdf"
                class="users-form-input"
                @change="onReceiptSelect"
              />
              <small v-if="addForm.receiptFileName" class="text-muted">{{ addForm.receiptFileName }}</small>
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text" class="form-label-icon"></b-icon>
              {{ $t("notes") || "ملاحظات" }}
            </label>
            <textarea
              v-model="addForm.notes"
              class="users-form-input"
              rows="2"
              :placeholder="$t('notesPlaceholder') || 'ملاحظات (اختياري)'"
            ></textarea>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingAdd">
              <b-spinner small v-if="savingAdd" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{ savingAdd ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showAddModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Modal: سحب مخزون -->
    <b-modal
      v-model="showWithdrawModal"
      :title="$t('withdrawStock') || 'سحب من المخزن'"
      @hidden="resetWithdrawForm"
      hide-header
      hide-footer
      class="users-modal"
      centered
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("withdrawStock") || "سحب من المخزن" }}</h2>
        <form @submit.prevent="submitWithdraw" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("itemName") || "اسم المادة" }}</label>
            <input v-model="withdrawForm.materialName" type="text" class="users-form-input" readonly />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              {{ $t("currentStock") || "الكمية الحالية" }}: {{ formatNumber(withdrawForm.currentStock) }}
            </label>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              {{ $t("quantity") || "الكمية" }} <span class="required">*</span>
            </label>
            <input
              v-model.number="withdrawForm.quantity"
              type="number"
              step="0.01"
              min="0.01"
              class="users-form-input"
              required
              :placeholder="$t('quantityPlaceholder') || 'الكمية'"
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("notes") || "ملاحظات" }}</label>
            <textarea
              v-model="withdrawForm.notes"
              class="users-form-input"
              rows="2"
              :placeholder="$t('notesPlaceholder') || 'ملاحظات (اختياري)'"
            ></textarea>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingWithdraw">
              <b-spinner small v-if="savingWithdraw" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{ savingWithdraw ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showWithdrawModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Modal: إضافة مورد -->
    <b-modal
      v-model="showAddSupplierModal"
      :title="$t('addSupplier') || 'إضافة مورد'"
      hide-header
      hide-footer
      class="users-modal"
      centered
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addSupplier") || "إضافة مورد" }}</h2>
        <form @submit.prevent="submitAddSupplier" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="person-badge" class="form-label-icon"></b-icon>
              {{ $t("supplierName") || "اسم المورد" }} <span class="required">*</span>
            </label>
            <input v-model="supplierForm.name" type="text" class="users-form-input" required :placeholder="$t('supplierNamePlaceholder') || 'اسم المورد'" />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text" class="form-label-icon"></b-icon>
              {{ $t("notes") || "ملاحظات" }}
            </label>
            <textarea v-model="supplierForm.notes" class="users-form-input" rows="2" :placeholder="$t('notesPlaceholder') || 'ملاحظات (اختياري)'"></textarea>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingSupplier">
              <b-spinner small v-if="savingSupplier" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{ savingSupplier ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showAddSupplierModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Modal: تعديل مورد -->
    <b-modal
      v-model="showEditSupplierModal"
      :title="$t('editSupplier') || 'تعديل مورد'"
      hide-header
      hide-footer
      class="users-modal"
      centered
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("editSupplier") || "تعديل مورد" }}</h2>
        <form @submit.prevent="submitEditSupplier" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="person-badge" class="form-label-icon"></b-icon>
              {{ $t("supplierName") || "اسم المورد" }} <span class="required">*</span>
            </label>
            <input v-model="supplierForm.name" type="text" class="users-form-input" required :placeholder="$t('supplierNamePlaceholder') || 'اسم المورد'" />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text" class="form-label-icon"></b-icon>
              {{ $t("notes") || "ملاحظات" }}
            </label>
            <textarea v-model="supplierForm.notes" class="users-form-input" rows="2" :placeholder="$t('notesPlaceholder') || 'ملاحظات (اختياري)'"></textarea>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingSupplier">
              <b-spinner small v-if="savingSupplier" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{ savingSupplier ? ($t("updating") || "جاري التحديث...") : ($t("update") || "تحديث") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showEditSupplierModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>
  </b-overlay>
</template>

<script>
import { HTTP } from '../http/api.js';
import AppHeader from '../components/Layout/AppHeader.vue';

export default {
  name: 'InventoryView',
  components: { AppHeader },
  data() {
    return {
      items: [],
      loading: false,
      searchQuery: '',
      searchTimer: null,
      showAddModal: false,
      showWithdrawModal: false,
      savingAdd: false,
      savingWithdraw: false,
      addForm: {
        supplierSelect: '',
        supplierOtherName: '',
        receiptNumber: '',
        items: [
          { materialName: '', unitPrice: 0, quantity: 0.01, unitType: '' }
        ],
        notes: '',
        receiptFile: null,
        receiptFileName: ''
      },
      withdrawForm: {
        materialName: '',
        currentStock: 0,
        quantity: 0.01,
        notes: ''
      },
      movementsList: [],
      loadingMovements: false,
      movementsPage: 1,
      movementsPageSize: 20,
      movementsTotal: 0,
      movementFilterMaterial: '',
      movementFilterType: '',
      movementFilterReceiptNumber: '',
      movementsSearchTimer: null,
      activeInventoryTab: 'stock',
      suppliersList: [],
      loadingSuppliers: false,
      showAddSupplierModal: false,
      showEditSupplierModal: false,
      supplierForm: { name: '', notes: '' },
      editingSupplierId: null,
      savingSupplier: false
    };
  },
  computed: {
    totalStockInvoiceAmount() {
      return (this.addForm.items || []).reduce((sum, row) => sum + this.calculateRowAmount(row), 0);
    }
  },
  mounted() {
    this.loadInventory();
  },
  beforeDestroy() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    if (this.movementsSearchTimer) clearTimeout(this.movementsSearchTimer);
  },
  methods: {
    onStockTabClick() {
      if (this.items.length === 0) this.loadInventory();
    },
    onMovementsTabClick() {
      if (this.movementsList.length === 0) this.loadStockMovements();
    },
    onSuppliersTabClick() {
      if (this.suppliersList.length === 0) this.loadSuppliers();
    },
    async loadSuppliers() {
      try {
        this.loadingSuppliers = true;
        const response = await HTTP.get('Inventory/GetSuppliers?pageNumber=0&pageSize=500');
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.suppliersList = response.data.data.items || [];
        } else {
          this.suppliersList = [];
        }
      } catch (error) {
        console.error('Error loading suppliers:', error);
        this.suppliersList = [];
      } finally {
        this.loadingSuppliers = false;
      }
    },
    onSupplierSelectChange() {
      if (this.addForm.supplierSelect !== '__other__') this.addForm.supplierOtherName = '';
    },
    openAddSupplierModal() {
      this.supplierForm = { name: '', notes: '' };
      this.showAddSupplierModal = true;
    },
    async submitAddSupplier() {
      const name = (this.supplierForm.name || '').trim();
      if (!name) {
        this.$bvToast.toast(this.$t('supplierNameRequired') || 'اسم المورد مطلوب', { variant: 'warning', solid: true });
        return;
      }
      try {
        this.savingSupplier = true;
        const response = await HTTP.post('Inventory/AddSupplier', { name, notes: this.supplierForm.notes?.trim() || null });
        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || this.$t('supplierAdded'), { title: this.$t('success'), variant: 'success', solid: true });
          this.showAddSupplierModal = false;
          this.loadSuppliers();
        } else {
          this.$bvToast.toast(response.data?.message || this.$t('error'), { variant: 'danger', solid: true });
        }
      } catch (error) {
        this.$bvToast.toast(error.response?.data?.message || this.$t('error'), { variant: 'danger', solid: true });
      } finally {
        this.savingSupplier = false;
      }
    },
    openEditSupplierModal(s) {
      this.editingSupplierId = s.id;
      this.supplierForm = { name: s.name || '', notes: s.notes || '' };
      this.showEditSupplierModal = true;
    },
    async submitEditSupplier() {
      const name = (this.supplierForm.name || '').trim();
      if (!name) {
        this.$bvToast.toast(this.$t('supplierNameRequired') || 'اسم المورد مطلوب', { variant: 'warning', solid: true });
        return;
      }
      if (this.editingSupplierId == null) return;
      try {
        this.savingSupplier = true;
        const response = await HTTP.put(`Inventory/UpdateSupplier/${this.editingSupplierId}`, { name, notes: this.supplierForm.notes?.trim() || null });
        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || this.$t('supplierUpdated'), { title: this.$t('success'), variant: 'success', solid: true });
          this.showEditSupplierModal = false;
          this.loadSuppliers();
        } else {
          this.$bvToast.toast(response.data?.message || this.$t('error'), { variant: 'danger', solid: true });
        }
      } catch (error) {
        this.$bvToast.toast(error.response?.data?.message || this.$t('error'), { variant: 'danger', solid: true });
      } finally {
        this.savingSupplier = false;
      }
    },
    confirmDeleteSupplier(s) {
      this.$bvModal.msgBoxConfirm(this.$t('confirmDeleteSupplier') || 'هل تريد حذف هذا المورد؟', {
        title: this.$t('deleteSupplier') || 'حذف مورد',
        okTitle: this.$t('confirmButton') || 'تأكيد',
        cancelTitle: this.$t('cancel') || 'إلغاء',
        okVariant: 'danger'
      }).then(ok => {
        if (ok) this.deleteSupplier(s.id);
      }).catch(() => {});
    },
    async deleteSupplier(id) {
      try {
        const response = await HTTP.delete(`Inventory/DeleteSupplier/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || this.$t('supplierDeleted'), { title: this.$t('success'), variant: 'success', solid: true });
          this.loadSuppliers();
        } else {
          this.$bvToast.toast(response.data?.message || this.$t('error'), { variant: 'danger', solid: true });
        }
      } catch (error) {
        this.$bvToast.toast(error.response?.data?.message || this.$t('error'), { variant: 'danger', solid: true });
      }
    },
    async loadStockMovements() {
      try {
        this.loadingMovements = true;
        const params = new URLSearchParams({
          pageNumber: String(this.movementsPage - 1),
          pageSize: String(this.movementsPageSize)
        });
        if (this.movementFilterMaterial) params.append('materialName', this.movementFilterMaterial);
        if (this.movementFilterType) params.append('movementType', this.movementFilterType);
        if (this.movementFilterReceiptNumber) params.append('receiptNumber', this.movementFilterReceiptNumber);
        const response = await HTTP.get(`Inventory/GetStockMovements?${params.toString()}`);
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.movementsList = response.data.data.items || [];
          this.movementsTotal = response.data.data.totalItems || 0;
        } else {
          this.movementsList = [];
          this.movementsTotal = 0;
        }
      } catch (error) {
        console.error('Error loading movements:', error);
        this.movementsList = [];
        this.movementsTotal = 0;
      } finally {
        this.loadingMovements = false;
      }
    },
    debounceLoadMovements() {
      if (this.movementsSearchTimer) clearTimeout(this.movementsSearchTimer);
      this.movementsSearchTimer = setTimeout(() => {
        this.movementsPage = 1;
        this.loadStockMovements();
      }, 400);
    },
    formatMovementDate(d) {
      if (!d) return '—';
      const date = new Date(d);
      return date.toLocaleString('ar-EG', { dateStyle: 'short', timeStyle: 'short' });
    },
    async loadInventory() {
      try {
        this.loading = true;
        const params = new URLSearchParams({
          pageNumber: '0',
          pageSize: '500'
        });
        if (this.searchQuery) params.append('info', this.searchQuery);
        const response = await HTTP.get(`Inventory/GetInventory?${params.toString()}`);
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.items = response.data.data.items || [];
        } else {
          this.items = [];
        }
      } catch (error) {
        console.error('Error loading inventory:', error);
        this.$bvToast.toast(error.response?.data?.message || this.$t('error') || 'حدث خطأ', {
          title: this.$t('error') || 'خطأ',
          variant: 'danger',
          solid: true
        });
        this.items = [];
      } finally {
        this.loading = false;
      }
    },
    debounceSearch() {
      if (this.searchTimer) clearTimeout(this.searchTimer);
      this.searchTimer = setTimeout(() => this.loadInventory(), 400);
    },
    addStockItemRow() {
      this.addForm.items.push({ materialName: '', unitPrice: 0, quantity: 0.01, unitType: '' });
    },
    removeStockItemRow(index) {
      if (this.addForm.items.length <= 1) {
        this.$bvToast.toast(this.$t('materialNameRequired') || 'يجب إدخال مادة واحدة على الأقل', { variant: 'warning', solid: true });
        return;
      }
      this.addForm.items.splice(index, 1);
    },
    calculateRowAmount(row) {
      const unitPrice = Number(row?.unitPrice || 0);
      const quantity = Number(row?.quantity || 0);
      return Math.max(0, unitPrice * quantity);
    },
    openAddModal(row) {
      this.addForm.supplierSelect = '';
      this.addForm.supplierOtherName = '';
      this.addForm.receiptNumber = '';
      this.addForm.items = [{
        materialName: row && row.materialName ? row.materialName : '',
        unitPrice: 0,
        quantity: 0.01,
        unitType: row && row.unitType ? row.unitType : ''
      }];
      this.addForm.notes = '';
      this.addForm.receiptFile = null;
      this.addForm.receiptFileName = '';
      if (this.$refs.receiptInput) this.$refs.receiptInput.value = '';
      if (this.suppliersList.length === 0) this.loadSuppliers();
      this.showAddModal = true;
    },
    resetAddForm() {
      this.addForm.supplierSelect = '';
      this.addForm.supplierOtherName = '';
      this.addForm.receiptNumber = '';
      this.addForm.items = [{ materialName: '', unitPrice: 0, quantity: 0.01, unitType: '' }];
      this.addForm.receiptFile = null;
      this.addForm.receiptFileName = '';
    },
    getAddFormSupplierName() {
      if (this.addForm.supplierSelect === '__other__') return (this.addForm.supplierOtherName || '').trim();
      if (this.addForm.supplierSelect && this.addForm.supplierSelect.startsWith('id_')) {
        const id = parseInt(this.addForm.supplierSelect.replace('id_', ''), 10);
        const s = this.suppliersList.find(x => x.id === id);
        return s ? s.name : '';
      }
      return '';
    },
    onReceiptSelect(e) {
      const file = e.target.files && e.target.files[0];
      this.addForm.receiptFile = file || null;
      this.addForm.receiptFileName = file ? file.name : '';
    },
    async submitAddStock() {
      const rows = (this.addForm.items || []).map((row) => ({
        materialName: (row.materialName || '').trim(),
        quantity: Number(row.quantity || 0),
        unitPrice: Number(row.unitPrice || 0),
        amount: this.calculateRowAmount(row),
        unitType: row.unitType || ''
      }));
      const validRows = rows.filter((row) => row.materialName && row.quantity > 0);
      if (validRows.length === 0) {
        this.$bvToast.toast(this.$t('materialNameRequired') || 'يرجى إدخال مادة واحدة على الأقل مع كمية صحيحة', { variant: 'warning', solid: true });
        return;
      }
      try {
        this.savingAdd = true;
        const formData = new FormData();
        formData.append('supplierName', this.getAddFormSupplierName());
        formData.append('receiptNumber', this.addForm.receiptNumber || '');
        formData.append('notes', this.addForm.notes || '');
        formData.append('itemsJson', JSON.stringify(validRows));
        if (this.addForm.receiptFile) formData.append('receiptFile', this.addForm.receiptFile);
        const response = await HTTP.post('Inventory/AddStockBatch', formData, {
          headers: { 'Content-Type': 'multipart/form-data' }
        });
        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || (this.$t('addStockSuccess') || 'تمت إضافة الكمية بنجاح'), {
            title: this.$t('success') || 'نجاح',
            variant: 'success',
            solid: true
          });
          this.showAddModal = false;
          this.loadInventory();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$bvToast.toast(response.data?.message || (this.$t('error') || 'حدث خطأ'), {
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        this.$bvToast.toast(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'), {
          variant: 'danger',
          solid: true
        });
      } finally {
        this.savingAdd = false;
      }
    },
    openWithdrawModal(row) {
      this.withdrawForm.materialName = row.materialName || '';
      this.withdrawForm.currentStock = row.currentQuantity ?? 0;
      this.withdrawForm.quantity = 0.01;
      this.withdrawForm.notes = '';
      this.showWithdrawModal = true;
    },
    resetWithdrawForm() {
      this.withdrawForm.materialName = '';
      this.withdrawForm.currentStock = 0;
    },
    async submitWithdraw() {
      if (!(this.withdrawForm.materialName || '').trim()) {
        this.$bvToast.toast(this.$t('materialNameRequired') || 'اسم المادة مطلوب', { variant: 'warning', solid: true });
        return;
      }
      if (!this.withdrawForm.quantity || this.withdrawForm.quantity <= 0) {
        this.$bvToast.toast(this.$t('quantityRequired') || 'الكمية مطلوبة وأكبر من صفر', { variant: 'warning', solid: true });
        return;
      }
      if (this.withdrawForm.quantity > this.withdrawForm.currentStock) {
        this.$bvToast.toast(this.$t('insufficientQuantity') || 'الكمية المتاحة غير كافية', { variant: 'danger', solid: true });
        return;
      }
      try {
        this.savingWithdraw = true;
        const response = await HTTP.post('Inventory/WithdrawStock', {
          materialName: this.withdrawForm.materialName.trim(),
          quantity: this.withdrawForm.quantity,
          notes: this.withdrawForm.notes || null
        });
        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || (this.$t('withdrawSuccess') || 'تم السحب بنجاح'), {
            title: this.$t('success') || 'نجاح',
            variant: 'success',
            solid: true
          });
          this.showWithdrawModal = false;
          this.loadInventory();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$bvToast.toast(response.data?.message || (this.$t('error') || 'حدث خطأ'), {
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        this.$bvToast.toast(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'), {
          variant: 'danger',
          solid: true
        });
      } finally {
        this.savingWithdraw = false;
      }
    },
    formatNumber(val) {
      if (val == null || val === '') return '0';
      return Number(val).toLocaleString('ar-EG', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    },
    buildReceiptUrl(fileName) {
      if (!fileName) return '#';
      return `${window.location.origin}/Receipts/${fileName}`;
    }
  }
};
</script>

<style scoped>
.users-table {
  width: 100%;
  margin-top: 1rem;
}
.users-table th,
.users-table td {
  padding: 0.75rem 1rem;
  vertical-align: middle;
}
.btn-inventory-add,
.btn-inventory-withdraw {
  border: none;
  border-radius: 0.5rem;
  padding: 0.4rem 0.75rem;
  font-size: 0.875rem;
  margin-left: 0.25rem;
  display: inline-flex;
  align-items: center;
  cursor: pointer;
}
.btn-inventory-add {
  background: rgba(34, 197, 94, 0.15);
  color: #16a34a;
}
.btn-inventory-add:hover {
  background: #16a34a;
  color: #fff;
}
.btn-inventory-withdraw {
  background: rgba(234, 179, 8, 0.2);
  color: #ca8a04;
}
.btn-inventory-withdraw:hover {
  background: #ca8a04;
  color: #fff;
}
.modal-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}
@media (max-width: 768px) {
  .modal-form-grid {
    grid-template-columns: 1fr;
  }
}
.required {
  color: #ef4444;
}
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 50vh;
  padding: 2rem;
  text-align: center;
}
.empty-icon {
  font-size: 4rem;
  opacity: 0.5;
  margin-bottom: 1rem;
}
.empty-text {
  margin: 0;
  font-size: 1.125rem;
  color: var(--text-secondary, #6b7280);
}
.inventory-tabs-section {
  margin-top: 2rem;
}
.inventory-tab-content {
  margin-top: 1rem;
  padding: 1rem;
  background: var(--bg-secondary, #f8f9fa);
  border-radius: 1rem;
  border: 1px solid var(--border-color, #e5e7eb);
}
.suppliers-tab-header {
  margin-bottom: 1rem;
  display: flex;
  justify-content: flex-end;
}
.btn-add-supplier {
  border: none;
  border-radius: 0.5rem;
  padding: 0.4rem 0.75rem;
  font-size: 0.875rem;
  background: rgba(34, 197, 94, 0.15);
  color: #16a34a;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
}
.btn-add-supplier:hover {
  background: #16a34a;
  color: #fff;
}
.movements-section {
  margin-top: 2rem;
  border: 1px solid var(--border-color, #e5e7eb);
  border-radius: 0.75rem;
  overflow: hidden;
}
.movements-section-header {
  padding: 0.75rem 1rem;
  background: var(--bg-secondary, #f9fafb);
}
.btn-movements-toggle {
  display: inline-flex;
  align-items: center;
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 0.5rem;
  background: transparent;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary, #111827);
}
.btn-movements-toggle:hover {
  background: rgba(0, 0, 0, 0.05);
}
.movements-section-body {
  padding: 1rem;
}
.movements-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-bottom: 1rem;
}
.movements-filter-input {
  max-width: 200px;
}
.btn-refresh-movements {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 0.5rem;
  background: rgba(59, 130, 246, 0.15);
  color: #2563eb;
  cursor: pointer;
  font-size: 0.875rem;
}
.btn-refresh-movements:hover {
  background: #2563eb;
  color: #fff;
}
.movements-table th,
.movements-table td {
  font-size: 0.875rem;
}
.badge-add {
  padding: 0.2rem 0.5rem;
  border-radius: 0.25rem;
  background: rgba(34, 197, 94, 0.2);
  color: #16a34a;
  font-weight: 500;
}
.badge-withdraw {
  padding: 0.2rem 0.5rem;
  border-radius: 0.25rem;
  background: rgba(234, 179, 8, 0.25);
  color: #ca8a04;
  font-weight: 500;
}
.empty-movements {
  text-align: center;
  padding: 1.5rem;
  color: var(--text-secondary, #6b7280);
}
.movements-pagination {
  margin-top: 1rem;
  display: flex;
  justify-content: center;
}

.stock-items-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}

.stock-items-table .users-form-input {
  min-width: 120px;
}

.stock-total-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color, #d1d5db);
  background: var(--bg-primary, #fff);
  font-weight: 700;
}

.receipt-link {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  color: var(--primary-color);
  text-decoration: none;
  font-weight: 600;
}

.receipt-link:hover {
  color: var(--primary-hover);
}
</style>
