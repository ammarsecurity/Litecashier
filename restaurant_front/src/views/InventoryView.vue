<template>
  <b-overlay :show="false" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content inventory-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="box-seam" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("inventoryTitle") || "مخزن المواد" }}</h1>
                  <p class="header-subtitle">{{ $t("inventorySubtitle") || "عرض الكميات وإضافة وسحب المخزون" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button
                  type="button"
                  class="btn-refresh"
                  @click="refreshPage"
                  :disabled="pageRefreshing"
                >
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: pageRefreshing }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
                <button type="button" class="users-add-button" @click="openAddModal()">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addStock") || "إضافة دخول مخزون" }}</span>
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
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ inventoryMaterialsCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("inventoryOverviewMaterials") || "مواد في المخزن" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="stack"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ formatNumber(totalCurrentStockQuantity) }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("inventoryOverviewStock") || "إجمالي الكميات الحالية" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="people"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingSuppliers && suppliersList.length === 0"></b-spinner>
                  <template v-else>{{ suppliersOverviewCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("inventoryOverviewSuppliers") || "الموردون" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="arrow-left-right"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingMovementsOverview"></b-spinner>
                  <template v-else>{{ formatNumber(movementsOverviewCount) }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("inventoryOverviewMovements") || "حركات مسجلة" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card app-section-card--flush">
            <div class="app-section-body app-section-body--tabs">
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
            </div>

          <div v-if="activeInventoryTab === 'stock'" class="inventory-tab-panel">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="box-seam"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("inventory") || "مخزن المواد" }}</h3>
                  <p class="app-section-subtitle">{{ $t("inventoryListHint") || "الكميات الحالية وإجراءات السحب" }}</p>
                </div>
              </div>
            </div>
            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                                    <p>{{ $t('inventoryFiltersHint') || 'بحث في مواد المخزن' }}</p>
                                </div>
                </div>
                <div class="app-filters-panel-actions" v-if="searchQuery">
                  <button type="button" class="users-form-cancel-button" @click="searchQuery = ''; debounceSearch()"><b-icon icon="x-circle"></b-icon>{{ $t('clearFilters') || 'مسح الفلاتر' }}</button>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--1">
                <div class="app-search-wrap app-search-wrap--wide">
                  <b-icon icon="search" class="app-search-icon"></b-icon>
                  <input v-model="searchQuery" type="search" class="app-search-input" :placeholder="$t('search') || 'بحث...'" autocomplete="off" @input="debounceSearch" />
                </div>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding inventory-table-section">
              <div v-if="loading" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="items.length > 0" class="table-responsive">
                <table class="table users-table reports-table">
                  <thead>
                    <tr>
                      <th>{{ $t("inventoryMaterialName") || "اسم المادة" }}</th>
                      <th>{{ $t("currentStock") || "الكمية الحالية" }}</th>
                      <th>{{ $t("totalAdded") || "إجمالي الداخل" }}</th>
                      <th>{{ $t("totalWithdrawn") || "إجمالي السحب" }}</th>
                      <th>{{ $t("unitType") || "الوحدة" }}</th>
                      <th>{{ $t("supplierName") || "المورد" }}</th>
                      <th>{{ $t("receiptNumber") || "رقم الوصل" }}</th>
                      <th>{{ $t("receiptAttachment") || "مرفق الوصل" }}</th>
                      <th>{{ $t("date") || "التاريخ" }}</th>
                      <th>{{ $t("actions") || "العمليات" }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(row, idx) in items" :key="inventoryRowKey(row, idx)">
                      <td>{{ row.materialName }}</td>
                      <td>{{ formatNumber(row.currentQuantity) }}</td>
                      <td>{{ formatNumber(row.totalAdded) }}</td>
                      <td>{{ formatNumber(row.totalWithdrawn) }}</td>
                      <td>{{ row.unitType || ($t("piece") || "قطعة") }}</td>
                      <td>{{ row.lastSupplierName || '—' }}</td>
                      <td>{{ row.lastReceiptNumber || '—' }}</td>
                      <td class="inventory-receipt-cell">
                        <template v-if="row.lastReceiptAttachmentPath">
                          <a
                            v-if="isReceiptImageFile(row.lastReceiptAttachmentPath)"
                            :href="receiptPublicUrl(row.lastReceiptAttachmentPath)"
                            target="_blank"
                            rel="noopener"
                            class="inventory-receipt-thumb-frame"
                            :title="$t('open') || 'فتح'"
                            :aria-label="$t('open') || 'فتح'"
                          >
                            <img
                              :src="receiptPublicUrl(row.lastReceiptAttachmentPath)"
                              alt=""
                              class="inventory-receipt-thumb"
                            />
                          </a>
                          <a
                            v-else
                            :href="receiptPublicUrl(row.lastReceiptAttachmentPath)"
                            target="_blank"
                            rel="noopener"
                            class="receipt-link-btn"
                          >
                            <b-icon icon="box-arrow-up-right" class="receipt-link-btn__icon"></b-icon>
                            <span>{{ $t('open') || 'فتح' }}</span>
                          </a>
                        </template>
                        <span v-else>—</span>
                      </td>
                      <td>{{ formatMovementDate(row.lastMovementDate) }}</td>
                      <td>
                        <div class="actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                          <button
                            type="button"
                            class="action-btn action-btn--icon action-btn--warn"
                            :title="$t('withdraw') || 'سحب'"
                            @click="openWithdrawModal(row)"
                          >
                            <b-icon icon="dash-circle" class="action-icon"></b-icon>
                          </button>
                          <button
                            type="button"
                            class="action-btn action-btn--icon action-btn--edit"
                            :title="$t('editStockMaterial') || $t('edit') || 'تعديل المادة'"
                            @click="openEditMaterialModal(row)"
                          >
                            <b-icon icon="pencil-square" class="action-icon"></b-icon>
                          </button>
                          <button
                            type="button"
                            class="action-btn action-btn--icon action-btn--delete"
                            :title="$t('deleteStockMaterial') || $t('delete') || 'حذف المادة'"
                            @click="confirmDeleteMaterial(row)"
                          >
                            <b-icon icon="trash" class="action-icon"></b-icon>
                          </button>
                          <button
                            v-if="row.lastReceiptNumber || row.stockReceiptKey"
                            type="button"
                            class="action-btn action-btn--icon action-btn--view"
                            :title="$t('editStockInvoice') || 'تعديل الفاتورة'"
                            @click.stop.prevent="openEditInvoiceModal(row.stockReceiptKey || row.lastReceiptNumber)"
                          >
                            <b-icon icon="receipt" class="action-icon"></b-icon>
                          </button>
                          <button
                            v-if="row.lastReceiptNumber || row.stockReceiptKey"
                            type="button"
                            class="action-btn action-btn--icon action-btn--delete"
                            :title="$t('deleteStockInvoice') || 'حذف الفاتورة'"
                            @click.stop.prevent="confirmDeleteInvoice(row.stockReceiptKey || row.lastReceiptNumber)"
                          >
                            <b-icon icon="file-earmark-x" class="action-icon"></b-icon>
                          </button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div v-else class="empty-state inventory-empty-state">
                <b-icon icon="box-seam" class="empty-icon"></b-icon>
                <p>{{ $t("noInventoryItems") || "لا توجد مواد في المخزن أو لا توجد نتائج" }}</p>
              </div>
            </div>
          </div>

          <div v-if="activeInventoryTab === 'movements'" class="inventory-tab-panel">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="arrow-left-right"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("movementsHistory") || "سجل الحركات" }}</h3>
                  <p class="app-section-subtitle">{{ $t("movementsHistoryHint") || "إضافة وسحب المخزون مع الفلاتر" }}</p>
                </div>
              </div>
            </div>
            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                                    <p>{{ $t('movementsFiltersHint') || 'تصفية حركات المخزون بالمادة والتاريخ والنوع' }}</p>
                                </div>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--3 movements-filters">
                <input
                  v-model="movementFilterMaterial"
                  type="text"
                  :placeholder="$t('inventoryMaterialName') || 'اسم المادة'"
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
                <input
                  v-model="movementFilterReceivedBy"
                  type="text"
                  :placeholder="$t('movementsFilterReceivedBy') || 'القسم المستلم'"
                  class="users-search-input movements-filter-input"
                  @input="debounceLoadMovements"
                />
                <select v-model="movementFilterType" class="users-search-input movements-filter-input" @change="loadStockMovements">
                  <option value="">{{ $t("all") || "الكل" }}</option>
                  <option value="Add">{{ $t("add") || "إضافة" }}</option>
                  <option value="Withdraw">{{ $t("withdraw") || "سحب" }}</option>
                </select>
                <input
                  v-model="movementFilterStartDate"
                  type="date"
                  class="users-search-input movements-filter-input movements-filter-date"
                  :aria-label="$t('fromDate') || 'من تاريخ'"
                  @change="onMovementDateFilterChange"
                />
                <input
                  v-model="movementFilterEndDate"
                  type="date"
                  class="users-search-input movements-filter-input movements-filter-date"
                  :aria-label="$t('toDate') || 'إلى تاريخ'"
                  @change="onMovementDateFilterChange"
                />
                <button type="button" class="btn-refresh" @click="loadStockMovements" :disabled="loadingMovements">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loadingMovements }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding inventory-table-section">
              <div v-if="!loadingMovements" class="movements-filter-summary-wrap">
                <div class="inventory-stock-total-bar movements-filter-total-bar" role="status">
                  <span class="inventory-stock-total-bar__label">{{
                    $t("movementsFilterTotalAmount") || "إجمالي المبلغ (حسب الفلتر)"
                  }}</span>
                  <span class="inventory-stock-total-bar__value">
                    {{ formatNumber(movementsTotalAmount) }} {{ $t("currency") || "د.ع" }}
                  </span>
                </div>
              </div>
              <div v-if="loadingMovements" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="movementsList.length > 0" class="table-responsive">
                <table class="table users-table movements-table reports-table">
                  <thead>
                    <tr>
                      <th>{{ $t("date") || "التاريخ" }}</th>
                      <th>{{ $t("inventoryMaterialName") || "اسم المادة" }}</th>
                      <th>{{ $t("movementType") || "النوع" }}</th>
                      <th>{{ $t("withdrawReceivedByDepartment") || "القسم المستلم" }}</th>
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
                      <td>{{ m.movementType === 'Withdraw' ? (m.receivedByDepartmentName || m.receivedByEmployeeName || '—') : '—' }}</td>
                      <td>{{ formatNumber(m.quantity) }}</td>
                      <td>{{ m.supplierName || '—' }}</td>
                      <td>{{ m.amount != null ? formatNumber(m.amount) : '—' }}</td>
                      <td>{{ m.receiptNumber || '—' }}</td>
                      <td class="inventory-receipt-cell">
                        <template v-if="movementAttachmentRef(m)">
                          <a
                            v-if="isReceiptImageFile(movementAttachmentRef(m))"
                            :href="receiptPublicUrl(movementAttachmentRef(m))"
                            target="_blank"
                            rel="noopener"
                            class="inventory-receipt-thumb-frame"
                            :title="$t('open') || 'فتح'"
                            :aria-label="$t('open') || 'فتح'"
                          >
                            <img
                              :src="receiptPublicUrl(movementAttachmentRef(m))"
                              alt=""
                              class="inventory-receipt-thumb"
                            />
                          </a>
                          <a
                            v-else
                            :href="receiptPublicUrl(movementAttachmentRef(m))"
                            target="_blank"
                            rel="noopener"
                            class="receipt-link-btn"
                          >
                            <b-icon icon="box-arrow-up-right" class="receipt-link-btn__icon"></b-icon>
                            <span>{{ $t("open") || "فتح" }}</span>
                          </a>
                        </template>
                        <span v-else>—</span>
                      </td>
                      <td>{{ m.unitType || '—' }}</td>
                      <td>{{ m.notes || '—' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div v-else class="empty-state inventory-empty-state">
                <b-icon icon="arrow-left-right" class="empty-icon"></b-icon>
                <p>{{ $t("noMovements") || "لا توجد حركات" }}</p>
              </div>
            </div>
            <div v-if="!loadingMovements && movementsTotal > movementsPageSize" class="app-section-body inventory-pagination-body">
              <div class="movements-pagination">
                <b-pagination
                  v-model="movementsPage"
                  :total-rows="movementsTotal"
                  :per-page="movementsPageSize"
                  size="sm"
                  @change="loadStockMovements"
                ></b-pagination>
              </div>
            </div>
          </div>

          <div v-if="activeInventoryTab === 'suppliers'" class="inventory-tab-panel">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="people"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("manageSuppliers") || "إدارة الموردين" }}</h3>
                  <p class="app-section-subtitle">{{ $t("suppliersListHint") || "إضافة وتعديل موردي المواد" }}</p>
                </div>
              </div>
              <button type="button" class="users-add-button" @click="openAddSupplierModal">
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addSupplier") || "إضافة مورد" }}</span>
              </button>
            </div>
            <div class="app-section-body app-section-body--no-padding inventory-table-section">
            <div v-if="loadingSuppliers" class="loading-state-full">
              <b-spinner variant="primary"></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="suppliersList.length > 0" class="table-responsive">
              <table class="table users-table movements-table reports-table">
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
                      <div class="actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                        <button
                          type="button"
                          class="action-btn action-btn--icon action-btn--edit"
                          :title="$t('editSupplier') || 'تعديل'"
                          @click="openEditSupplierModal(s)"
                        >
                          <b-icon icon="pencil-square" class="action-icon"></b-icon>
                        </button>
                        <button
                          type="button"
                          class="action-btn action-btn--icon action-btn--delete"
                          :title="$t('deleteSupplier') || 'حذف'"
                          @click="confirmDeleteSupplier(s)"
                        >
                          <b-icon icon="trash" class="action-icon"></b-icon>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div v-else class="empty-state inventory-empty-state">
              <b-icon icon="people" class="empty-icon"></b-icon>
              <p>{{ $t("noSuppliers") || "لا يوجد موردين" }}</p>
              <button type="button" class="empty-state-btn" @click="openAddSupplierModal">
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addSupplier") || "إضافة مورد" }}</span>
              </button>
            </div>
            </div>
          </div>

          </div>
        </div>
      </div>
    </div>

    <!-- Modal: إضافة مخزون -->
    <b-modal
      v-model="showAddModal"
      @hidden="resetAddForm"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="xl"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">
          {{
            editingInvoiceReceipt
              ? ($t("editStockInvoice") || "تعديل فاتورة المخزن")
              : ($t("addStock") || "إضافة دخول مخزون")
          }}
        </h2>
        <p class="inventory-modal-subtitle">{{ $t("inventoryStockEntryHint") || "ربط الوصل بالمورد ثم إضافة أسطر المواد والكميات والأسعار." }}</p>

        <form @submit.prevent="onStockModalSubmit" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group mb-0">
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
            <div class="users-form-group mb-0">
              <label class="users-form-label">
                <b-icon icon="receipt" class="form-label-icon"></b-icon>
                {{ $t("receiptNumber") || "رقم الوصل" }}
              </label>
              <input
                v-model="addForm.receiptNumber"
                type="text"
                class="users-form-input"
                :placeholder="$t('receiptNumberPlaceholder') || 'أدخل رقم الوصل'"
                autocomplete="off"
              />
            </div>
          </div>

          <div class="inventory-lines-panel">
            <div class="inventory-lines-toolbar">
              <div class="inventory-lines-toolbar__text">
                <span class="inventory-lines-heading">
                  <b-icon icon="box-seam" class="form-label-icon"></b-icon>
                  {{ $t("inventoryItemsList") || "قائمة المواد المدخلة" }}
                </span>
                <span class="inventory-lines-hint">{{ addForm.items.length }} {{ $t("linesCount") || "سطر" }}</span>
              </div>
              <button type="button" class="inventory-add-line-btn" @click="addStockItemRow">
                <b-icon icon="plus-lg" class="me-1"></b-icon>
                {{ $t("inventoryAddLine") || "إضافة سطر" }}
              </button>
            </div>

            <div class="table-responsive stock-lines-wrap">
              <table class="table users-table movements-table stock-lines-table">
                <thead>
                  <tr>
                    <th class="col-material">{{ $t("inventoryMaterialName") || "اسم المادة" }}</th>
                    <th class="col-num">{{ $t("unitPrice") || "سعر الوحدة" }}</th>
                    <th class="col-num">{{ $t("quantity") || "الكمية" }}</th>
                    <th class="col-num col-amount">{{ $t("lineTotal") || "المجموع" }}</th>
                    <th class="col-unit">{{ $t("unitType") || "الوحدة" }}</th>
                    <th class="col-actions">{{ $t("actions") || "حذف" }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(itemRow, rowIndex) in addForm.items" :key="'stock-item-' + rowIndex">
                    <td>
                      <input
                        v-model="itemRow.materialName"
                        type="text"
                        class="users-form-input stock-line-input"
                        list="inventory-material-suggestions"
                        autocomplete="off"
                        :placeholder="$t('materialNamePlaceholder') || 'اكتب اسم المادة'"
                        @change="onMaterialNamePicked(itemRow)"
                        @blur="onMaterialNamePicked(itemRow)"
                      />
                    </td>
                    <td>
                      <input
                        v-model.number="itemRow.unitPrice"
                        type="number"
                        min="0"
                        step="0.01"
                        class="users-form-input stock-line-input stock-line-input--num"
                        :placeholder="'0'"
                      />
                    </td>
                    <td>
                      <input
                        v-model.number="itemRow.quantity"
                        type="number"
                        min="0.01"
                        step="0.01"
                        class="users-form-input stock-line-input stock-line-input--num"
                        :placeholder="'0'"
                      />
                    </td>
                    <td class="stock-line-amount">{{ formatNumber(calculateRowAmount(itemRow)) }}</td>
                    <td>
                      <select v-model="itemRow.unitType" class="users-form-input stock-line-input stock-line-input--unit">
                        <option value="">{{ $t("selectUnit") || "الوحدة" }}</option>
                        <option value="قطعة">{{ $t("piece") || "قطعة" }}</option>
                        <option value="كارتون">{{ $t("carton") || "كارتون" }}</option>
                        <option value="كيلو">{{ $t("kilo") || "كيلو" }}</option>
                        <option value="لتر">{{ $t("liter") || "لتر" }}</option>
                        <option value="علبة">{{ $t("box") || "علبة" }}</option>
                        <option value="أخرى">{{ $t("other") || "أخرى" }}</option>
                      </select>
                    </td>
                    <td class="text-center">
                      <button
                        type="button"
                        class="stock-btn-remove-row"
                        :title="$t('delete') || 'حذف'"
                        @click="removeStockItemRow(rowIndex)"
                      >
                        <b-icon icon="trash"></b-icon>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <datalist id="inventory-material-suggestions">
              <option
                v-for="name in knownMaterialNames"
                :key="'mat-suggest-' + name"
                :value="name"
              ></option>
            </datalist>

            <div class="inventory-stock-total-bar">
              <span class="inventory-stock-total-bar__label">{{ $t("totalAmount") || "إجمالي المبلغ" }}</span>
              <span class="inventory-stock-total-bar__value">{{ formatNumber(totalStockInvoiceAmount) }} {{ $t("currency") || "د.ع" }}</span>
            </div>
          </div>

          <div class="modal-form-grid">
            <div class="users-form-group mb-0">
              <label class="users-form-label">
                <b-icon icon="paperclip" class="form-label-icon"></b-icon>
                {{ $t("receiptAttachment") || "مرفق الوصل" }}
              </label>
              <label class="inventory-file-drop">
                <input
                  ref="receiptInput"
                  type="file"
                  accept=".jpg,.jpeg,.png,.gif,.pdf"
                  class="inventory-file-drop__input"
                  @change="onReceiptSelect"
                />
                <span class="inventory-file-drop__text">
                  <b-icon icon="cloud-upload" class="me-2"></b-icon>
                  {{ $t("inventoryAttachHint") || "صورة أو PDF — انقر للاختيار" }}
                </span>
              </label>
              <small v-if="addForm.receiptFileName" class="inventory-file-name">{{ addForm.receiptFileName }}</small>
            </div>
            <div class="users-form-group mb-0">
              <label class="users-form-label">
                <b-icon icon="file-text" class="form-label-icon"></b-icon>
                {{ $t("notes") || "ملاحظات" }}
              </label>
              <textarea
                v-model="addForm.notes"
                class="users-form-input"
                rows="3"
                :placeholder="$t('inventoryNotesPlaceholder') || 'ملاحظات على هذه الفاتورة (اختياري)'"
              ></textarea>
            </div>
          </div>

          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingAdd">
              <b-spinner small v-if="savingAdd" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{
                savingAdd
                  ? ($t("saving") || "جاري الحفظ...")
                  : editingInvoiceReceipt
                    ? ($t("saveStockInvoice") || "حفظ الفاتورة")
                    : ($t("saveStockEntry") || "حفظ الإدخال")
              }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showAddModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Modal: تعديل مادة -->
    <b-modal
      v-model="showEditMaterialModal"
      @hidden="resetEditMaterialForm"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="md"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("editStockMaterial") || "تعديل المادة" }}</h2>
        <form @submit.prevent="submitEditMaterial" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="box-seam" class="form-label-icon"></b-icon>
              {{ $t("inventoryMaterialName") || "اسم المادة" }} <span class="required">*</span>
            </label>
            <input
              v-model="editMaterialForm.materialName"
              type="text"
              class="users-form-input"
              required
              :placeholder="$t('materialNamePlaceholder') || 'اسم المادة'"
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="hash" class="form-label-icon"></b-icon>
              {{ $t("totalAdded") || "إجمالي الداخل" }} <span class="required">*</span>
            </label>
            <input
              v-model.number="editMaterialForm.totalAddedQuantity"
              type="number"
              step="0.01"
              min="0.01"
              class="users-form-input"
              required
            />
            <small class="users-form-hint">
              {{ $t("editStockMaterialWithdrawnHint") || "المسحوب" }}:
              {{ formatNumber(editMaterialForm.totalWithdrawn) }}
            </small>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="rulers" class="form-label-icon"></b-icon>
              {{ $t("unitType") || "الوحدة" }}
            </label>
            <select v-model="editMaterialForm.unitType" class="users-form-input">
              <option value="">{{ $t("selectUnit") || "الوحدة" }}</option>
              <option value="قطعة">{{ $t("piece") || "قطعة" }}</option>
              <option value="كارتون">{{ $t("carton") || "كارتون" }}</option>
              <option value="كيلو">{{ $t("kilo") || "كيلو" }}</option>
              <option value="لتر">{{ $t("liter") || "لتر" }}</option>
              <option value="علبة">{{ $t("box") || "علبة" }}</option>
              <option value="أخرى">{{ $t("other") || "أخرى" }}</option>
            </select>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingEditMaterial">
              <b-spinner small v-if="savingEditMaterial" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{ savingEditMaterial ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showEditMaterialModal = false">
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
      @hidden="resetWithdrawForm"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="md"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("withdrawStock") || "سحب من المخزن" }}</h2>
        <form @submit.prevent="submitWithdraw" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="box-seam" class="form-label-icon"></b-icon>
              {{ $t("inventoryMaterialName") || "اسم المادة" }}
            </label>
            <input v-model="withdrawForm.materialName" type="text" class="users-form-input" readonly />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="layers" class="form-label-icon"></b-icon>
              {{ $t("currentStock") || "الكمية الحالية" }}: {{ formatNumber(withdrawForm.currentStock) }}
            </label>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="hash" class="form-label-icon"></b-icon>
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
            <label class="users-form-label">
              <b-icon icon="diagram-3" class="form-label-icon"></b-icon>
              {{ $t("withdrawDepartment") || "القسم" }} <span class="required">*</span>
            </label>
            <select v-model="withdrawForm.parentTagId" class="users-form-input" required @change="onWithdrawParentChange">
              <option disabled value="">{{ $t("selectWithdrawDepartment") || "اختر القسم" }}</option>
              <option v-for="t in withdrawRootTags" :key="'wd-root-' + t.id" :value="String(t.id)">{{ t.name }}</option>
            </select>
          </div>
          <div v-if="withdrawChildTags.length > 0" class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="diagram-2" class="form-label-icon"></b-icon>
              {{ $t("withdrawSubDepartment") || "القسم الفرعي" }}
            </label>
            <select v-model="withdrawForm.childTagId" class="users-form-input">
              <option value="">{{ $t("selectWithdrawSubDepartment") || "اختر القسم الفرعي (اختياري)" }}</option>
              <option v-for="t in withdrawChildTags" :key="'wd-child-' + t.id" :value="String(t.id)">{{ t.name }}</option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text" class="form-label-icon"></b-icon>
              {{ $t("notes") || "ملاحظات" }}
            </label>
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
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="md"
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
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="md"
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
import { rootTags, childTagsOf } from '@/utils/tagHierarchy.js';
import { resolveApiBaseUrl } from '@/utils/apiBase.js';

export default {
  name: 'InventoryView',
  components: { AppHeader },
  data() {
    return {
      items: [],
      materialCatalog: [],
      loading: false,
      searchQuery: '',
      searchTimer: null,
      showAddModal: false,
      showWithdrawModal: false,
      showEditMaterialModal: false,
      savingAdd: false,
      savingWithdraw: false,
      savingEditMaterial: false,
      editingInvoiceReceipt: '',
      addForm: {
        supplierSelect: '',
        supplierOtherName: '',
        receiptNumber: '',
        items: [
          { id: null, materialName: '', unitPrice: 0, quantity: 0.01, unitType: '' }
        ],
        notes: '',
        receiptFile: null,
        receiptFileName: ''
      },
      editMaterialForm: {
        originalMaterialName: '',
        stockReceiptKey: '',
        materialName: '',
        totalAddedQuantity: 0.01,
        totalWithdrawn: 0,
        unitType: ''
      },
      withdrawTags: [],
      withdrawForm: {
        materialName: '',
        stockReceiptKey: '',
        currentStock: 0,
        quantity: 0.01,
        parentTagId: '',
        childTagId: '',
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
      movementFilterReceivedBy: '',
      movementFilterStartDate: '',
      movementFilterEndDate: '',
      movementsTotalAmount: 0,
      movementsSearchTimer: null,
      activeInventoryTab: 'stock',
      suppliersList: [],
      loadingSuppliers: false,
      showAddSupplierModal: false,
      showEditSupplierModal: false,
      supplierForm: { name: '', notes: '' },
      editingSupplierId: null,
      savingSupplier: false,
      loadingMovementsOverview: false,
      movementsOverviewCount: 0
    };
  },
  computed: {
    totalStockInvoiceAmount() {
      return (this.addForm.items || []).reduce((sum, row) => sum + this.calculateRowAmount(row), 0);
    },
    pageRefreshing() {
      return (
        this.loading ||
        (this.activeInventoryTab === 'movements' && this.loadingMovements) ||
        (this.activeInventoryTab === 'suppliers' && this.loadingSuppliers)
      );
    },
    inventoryMaterialsCount() {
      return this.items.length;
    },
    totalCurrentStockQuantity() {
      return (this.items || []).reduce((sum, row) => sum + (Number(row.currentQuantity) || 0), 0);
    },
    suppliersOverviewCount() {
      return this.suppliersList.length;
    },
    withdrawRootTags() {
      return rootTags(this.withdrawTags);
    },
    withdrawChildTags() {
      if (!this.withdrawForm.parentTagId) return [];
      const parent = this.withdrawTags.find(
        (t) => String(t.id) === String(this.withdrawForm.parentTagId)
      );
      return childTagsOf(parent, this.withdrawTags);
    },
    /** أسماء مواد المخزن المعروفة للإكمال التلقائي عند إدخال فاتورة */
    knownMaterialNames() {
      const names = new Set();
      (this.materialCatalog || []).forEach((row) => {
        const n = (row.materialName || '').trim();
        if (n) names.add(n);
      });
      (this.items || []).forEach((row) => {
        const n = (row.materialName || '').trim();
        if (n) names.add(n);
      });
      (this.movementsList || []).forEach((m) => {
        const n = (m.materialName || '').trim();
        if (n) names.add(n);
      });
      return Array.from(names).sort((a, b) => a.localeCompare(b, 'ar'));
    },
    materialUnitByName() {
      const map = {};
      const sources = [...(this.materialCatalog || []), ...(this.items || []), ...(this.movementsList || [])];
      sources.forEach((row) => {
        const n = (row.materialName || '').trim();
        if (!n || map[n]) return;
        if (row.unitType) map[n] = row.unitType;
      });
      return map;
    }
  },
  mounted() {
    this.loadInventory();
    this.loadMaterialCatalog();
    this.loadSuppliers();
    this.loadMovementsOverviewCount();
  },
  beforeDestroy() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    if (this.movementsSearchTimer) clearTimeout(this.movementsSearchTimer);
  },
  methods: {
    async refreshPage() {
      await Promise.all([
        this.loadInventory(),
        this.loadSuppliers(),
        this.loadMovementsOverviewCount()
      ]);
      if (this.activeInventoryTab === 'movements') {
        await this.loadStockMovements();
      }
    },
    async loadMovementsOverviewCount() {
      try {
        this.loadingMovementsOverview = true;
        const response = await HTTP.get('Inventory/GetStockMovements?pageNumber=0&pageSize=1');
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.movementsOverviewCount = response.data.data.totalItems || 0;
        } else {
          this.movementsOverviewCount = 0;
        }
      } catch (error) {
        console.error('Error loading movements overview:', error);
        this.movementsOverviewCount = 0;
      } finally {
        this.loadingMovementsOverview = false;
      }
    },
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
        this.$notify.warning(this.$t('supplierNameRequired') || 'اسم المورد مطلوب');
        return;
      }
      try {
        this.savingSupplier = true;
        const response = await HTTP.post('Inventory/AddSupplier', { name, notes: this.supplierForm.notes?.trim() || null });
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(response.data.message || this.$t('supplierAdded'));
          this.showAddSupplierModal = false;
          this.loadSuppliers();
        } else {
          this.$notify.error(response.data?.message || this.$t('error'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || this.$t('error'));
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
        this.$notify.warning(this.$t('supplierNameRequired') || 'اسم المورد مطلوب');
        return;
      }
      if (this.editingSupplierId == null) return;
      try {
        this.savingSupplier = true;
        const response = await HTTP.put(`Inventory/UpdateSupplier/${this.editingSupplierId}`, { name, notes: this.supplierForm.notes?.trim() || null });
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(response.data.message || this.$t('supplierUpdated'));
          this.showEditSupplierModal = false;
          this.loadSuppliers();
        } else {
          this.$notify.error(response.data?.message || this.$t('error'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || this.$t('error'));
      } finally {
        this.savingSupplier = false;
      }
    },
    async confirmDeleteSupplier(s) {
      const ok = await this.$confirm({
        title: this.$t('deleteSupplier'),
        message: this.$t('confirmDeleteSupplier', { name: s.name || '' }),
      });
      if (ok) {
        this.deleteSupplier(s.id);
      }
    },
    async deleteSupplier(id) {
      try {
        const response = await HTTP.delete(`Inventory/DeleteSupplier/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(response.data.message || this.$t('supplierDeleted'));
          this.loadSuppliers();
        } else {
          this.$notify.error(response.data?.message || this.$t('error'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || this.$t('error'));
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
        if (this.movementFilterReceivedBy) params.append('receivedByEmployeeName', this.movementFilterReceivedBy);
        if (this.movementFilterStartDate) params.append('startDate', this.movementFilterStartDate);
        if (this.movementFilterEndDate) params.append('endDate', this.movementFilterEndDate);
        const response = await HTTP.get(`Inventory/GetStockMovements?${params.toString()}`);
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.movementsList = response.data.data.items || [];
          this.movementsTotal = response.data.data.totalItems || 0;
          this.movementsOverviewCount = this.movementsTotal;
          const d = response.data.data;
          const rawTotal = d.totalFilteredAmount ?? d.TotalFilteredAmount;
          this.movementsTotalAmount =
            rawTotal !== undefined && rawTotal !== null ? Number(rawTotal) : 0;
        } else {
          this.movementsList = [];
          this.movementsTotal = 0;
          this.movementsTotalAmount = 0;
        }
      } catch (error) {
        console.error('Error loading movements:', error);
        this.movementsList = [];
        this.movementsTotal = 0;
        this.movementsTotalAmount = 0;
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
    onMovementDateFilterChange() {
      this.movementsPage = 1;
      this.loadStockMovements();
    },
    formatMovementDate(d) {
      if (!d) return '—';
      const date = new Date(d);
      return date.toLocaleString('ar-EG', {
        dateStyle: 'short',
        timeStyle: 'short',
        numberingSystem: 'latn'
      });
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
          if (!this.searchQuery) {
            this.materialCatalog = this.items;
          }
        } else {
          this.items = [];
        }
      } catch (error) {
        console.error('Error loading inventory:', error);
        this.$notify.error(error.response?.data?.message || this.$t('error') || 'حدث خطأ');
        this.items = [];
      } finally {
        this.loading = false;
      }
    },
    async loadMaterialCatalog() {
      try {
        const response = await HTTP.get('Inventory/GetInventory?pageNumber=0&pageSize=500');
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.materialCatalog = response.data.data.items || [];
        }
      } catch (error) {
        console.error('Error loading material catalog:', error);
      }
    },
    debounceSearch() {
      if (this.searchTimer) clearTimeout(this.searchTimer);
      this.searchTimer = setTimeout(() => this.loadInventory(), 400);
    },
    addStockItemRow() {
      this.addForm.items.push({ id: null, materialName: '', unitPrice: 0, quantity: 0.01, unitType: '' });
    },
    removeStockItemRow(index) {
      if (this.addForm.items.length <= 1) {
        this.$notify.warning(this.$t('materialNameRequired') || 'يجب إدخال مادة واحدة على الأقل');
        return;
      }
      this.addForm.items.splice(index, 1);
    },
    calculateRowAmount(row) {
      const unitPrice = Number(row?.unitPrice || 0);
      const quantity = Number(row?.quantity || 0);
      return Math.max(0, unitPrice * quantity);
    },
    onMaterialNamePicked(itemRow) {
      if (!itemRow) return;
      const name = (itemRow.materialName || '').trim();
      if (!name) return;
      itemRow.materialName = name;
      const knownUnit = this.materialUnitByName[name];
      if (knownUnit && !itemRow.unitType) {
        itemRow.unitType = knownUnit;
      }
    },
    async openAddModal(row) {
      this.editingInvoiceReceipt = '';
      this.addForm.supplierSelect = '';
      this.addForm.supplierOtherName = '';
      this.addForm.receiptNumber = '';
      this.addForm.items = [{
        id: null,
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
      if (this.materialCatalog.length === 0) await this.loadMaterialCatalog();
      this.showAddModal = true;
    },
    resetAddForm() {
      this.editingInvoiceReceipt = '';
      this.addForm.supplierSelect = '';
      this.addForm.supplierOtherName = '';
      this.addForm.receiptNumber = '';
      this.addForm.items = [{ id: null, materialName: '', unitPrice: 0, quantity: 0.01, unitType: '' }];
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
        this.$notify.warning(this.$t('materialNameRequired') || 'يرجى إدخال مادة واحدة على الأقل مع كمية صحيحة');
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
          this.$notify.success(
            response.data.message || (this.$t('addStockSuccess') || 'تمت إضافة الكمية بنجاح')
          );
          this.showAddModal = false;
          this.loadInventory();
          this.loadMaterialCatalog();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$notify.error(response.data?.message || (this.$t('error') || 'حدث خطأ'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'));
      } finally {
        this.savingAdd = false;
      }
    },
    inventoryRowKey(row, idx) {
      const name = row.materialName || '';
      const rk = row.stockReceiptKey != null ? String(row.stockReceiptKey) : (row.lastReceiptNumber || '');
      return `${name}|||${rk}|||${idx}`;
    },
    onStockModalSubmit() {
      if (this.editingInvoiceReceipt) {
        return this.submitEditInvoice();
      }
      return this.submitAddStock();
    },
    async openEditInvoiceModal(receiptNumber) {
      const rn = String(receiptNumber || '').trim();
      if (!rn) {
        this.$notify.warning(this.$t('receiptNumberRequired') || 'رقم الوصل مطلوب لتعديل الفاتورة');
        return;
      }
      try {
        if (this.suppliersList.length === 0) await this.loadSuppliers();
        const response = await HTTP.get('Inventory/GetStockInvoice', {
          params: { receiptNumber: rn }
        });
        const payload = response && response.data;
        if (!payload || typeof payload !== 'object' || payload.errorStatus || !payload.data) {
          const msg =
            (payload && (payload.message || payload.Message)) ||
            (this.$t('error') || 'حدث خطأ');
          this.$notify.error(msg);
          return;
        }
        const inv = payload.data;
        const itemsRaw = Array.isArray(inv.items) ? inv.items : [];
        this.editingInvoiceReceipt = String(inv.receiptNumber || rn);
        this.addForm.receiptNumber = String(inv.receiptNumber || rn);
        this.addForm.notes = inv.notes || '';
        this.addForm.receiptFile = null;
        this.addForm.receiptFileName = inv.receiptFileName || '';
        const supplierName = (inv.supplierName || '').trim();
        const matched = this.suppliersList.find((s) => s.name === supplierName);
        if (matched) {
          this.addForm.supplierSelect = 'id_' + matched.id;
          this.addForm.supplierOtherName = '';
        } else if (supplierName) {
          this.addForm.supplierSelect = '__other__';
          this.addForm.supplierOtherName = supplierName;
        } else {
          this.addForm.supplierSelect = '';
          this.addForm.supplierOtherName = '';
        }
        this.addForm.items = itemsRaw.length
          ? itemsRaw.map((it) => ({
              id: it.id || null,
              materialName: it.materialName || '',
              unitPrice: Number(it.unitPrice) || 0,
              quantity: Number(it.quantity) || 0.01,
              unitType: it.unitType || ''
            }))
          : [{ id: null, materialName: '', unitPrice: 0, quantity: 0.01, unitType: '' }];
        this.showAddModal = true;
        this.$nextTick(() => {
          if (this.$refs.receiptInput) this.$refs.receiptInput.value = '';
        });
      } catch (error) {
        console.error('openEditInvoiceModal failed:', error);
        const data = error && error.response && error.response.data;
        const msg =
          (data && typeof data === 'object' && (data.message || data.Message)) ||
          (this.$t('error') || 'حدث خطأ أثناء فتح الفاتورة');
        this.$notify.error(msg);
      }
    },
    async submitEditInvoice() {
      const original = (this.editingInvoiceReceipt || '').trim();
      if (!original) return;
      const rows = (this.addForm.items || []).map((row) => ({
        id: row.id || null,
        materialName: (row.materialName || '').trim(),
        quantity: Number(row.quantity || 0),
        unitPrice: Number(row.unitPrice || 0),
        amount: this.calculateRowAmount(row),
        unitType: row.unitType || ''
      }));
      const validRows = rows.filter((row) => row.materialName && row.quantity > 0);
      if (validRows.length === 0) {
        this.$notify.warning(this.$t('materialNameRequired') || 'يرجى إدخال مادة واحدة على الأقل مع كمية صحيحة');
        return;
      }
      try {
        this.savingAdd = true;
        const formData = new FormData();
        formData.append('originalReceiptNumber', original);
        formData.append('supplierName', this.getAddFormSupplierName());
        formData.append('receiptNumber', this.addForm.receiptNumber || original);
        formData.append('notes', this.addForm.notes || '');
        formData.append('itemsJson', JSON.stringify(validRows));
        if (this.addForm.receiptFile) formData.append('receiptFile', this.addForm.receiptFile);
        const response = await HTTP.put('Inventory/UpdateStockBatch', formData, {
          headers: { 'Content-Type': 'multipart/form-data' }
        });
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(response.data.message || (this.$t('stockInvoiceUpdated') || 'تم تعديل الفاتورة'));
          this.showAddModal = false;
          this.loadInventory();
          this.loadMaterialCatalog();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$notify.error(response.data?.message || (this.$t('error') || 'حدث خطأ'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'));
      } finally {
        this.savingAdd = false;
      }
    },
    async confirmDeleteInvoice(receiptNumber) {
      const rn = (receiptNumber || '').trim();
      if (!rn) return;
      const ok = await this.$confirm({
        title: this.$t('deleteStockInvoice') || 'حذف الفاتورة',
        message:
          this.$t('confirmDeleteStockInvoice', { number: rn }) ||
          `هل تريد حذف فاتورة الوصل «${rn}» بالكامل مع كل موادها وسحوباتها؟`
      });
      if (!ok) return;
      try {
        const response = await HTTP.delete(
          `Inventory/DeleteStockInvoice?receiptNumber=${encodeURIComponent(rn)}`
        );
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(response.data.message || (this.$t('stockInvoiceDeleted') || 'تم حذف الفاتورة'));
          this.loadInventory();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$notify.error(response.data?.message || (this.$t('error') || 'حدث خطأ'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'));
      }
    },
    openEditMaterialModal(row) {
      this.editMaterialForm = {
        originalMaterialName: row.materialName || '',
        stockReceiptKey:
          row.stockReceiptKey != null && row.stockReceiptKey !== undefined
            ? String(row.stockReceiptKey)
            : row.lastReceiptNumber || '',
        materialName: row.materialName || '',
        totalAddedQuantity: Number(row.totalAdded) || 0.01,
        totalWithdrawn: Number(row.totalWithdrawn) || 0,
        unitType: row.unitType || ''
      };
      this.showEditMaterialModal = true;
    },
    resetEditMaterialForm() {
      this.editMaterialForm = {
        originalMaterialName: '',
        stockReceiptKey: '',
        materialName: '',
        totalAddedQuantity: 0.01,
        totalWithdrawn: 0,
        unitType: ''
      };
    },
    async submitEditMaterial() {
      const name = (this.editMaterialForm.materialName || '').trim();
      if (!name) {
        this.$notify.warning(this.$t('materialNameRequired') || 'اسم المادة مطلوب');
        return;
      }
      if (
        !this.editMaterialForm.totalAddedQuantity ||
        this.editMaterialForm.totalAddedQuantity < this.editMaterialForm.totalWithdrawn
      ) {
        this.$notify.warning(
          this.$t('editStockMaterialQtyInvalid') ||
            'إجمالي الداخل يجب أن يكون أكبر من أو يساوي المسحوب'
        );
        return;
      }
      try {
        this.savingEditMaterial = true;
        const response = await HTTP.put('Inventory/UpdateStockLine', {
          materialName: this.editMaterialForm.originalMaterialName,
          receiptNumber: this.editMaterialForm.stockReceiptKey || null,
          newMaterialName: name,
          unitType: this.editMaterialForm.unitType || '',
          totalAddedQuantity: this.editMaterialForm.totalAddedQuantity
        });
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(response.data.message || (this.$t('stockMaterialUpdated') || 'تم تعديل المادة'));
          this.showEditMaterialModal = false;
          this.loadInventory();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$notify.error(response.data?.message || (this.$t('error') || 'حدث خطأ'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'));
      } finally {
        this.savingEditMaterial = false;
      }
    },
    async confirmDeleteMaterial(row) {
      const ok = await this.$confirm({
        title: this.$t('deleteStockMaterial') || 'حذف المادة',
        message:
          this.$t('confirmDeleteStockMaterial', { name: row.materialName || '' }) ||
          `هل تريد حذف المادة «${row.materialName || ''}» من المخزن؟`
      });
      if (!ok) return;
      try {
        const response = await HTTP.post('Inventory/DeleteStockLine', {
          materialName: row.materialName,
          receiptNumber:
            row.stockReceiptKey != null && row.stockReceiptKey !== undefined
              ? String(row.stockReceiptKey)
              : row.lastReceiptNumber || null
        });
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(response.data.message || (this.$t('stockMaterialDeleted') || 'تم حذف المادة'));
          this.loadInventory();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$notify.error(response.data?.message || (this.$t('error') || 'حدث خطأ'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'));
      }
    },
    async loadWithdrawDepartments() {
      try {
        const response = await HTTP.get('Inventory/GetWithdrawDepartments');
        if (response.data && !response.data.errorStatus) {
          this.withdrawTags = response.data.data || [];
        } else {
          this.withdrawTags = [];
        }
      } catch (e) {
        console.error('Error loading withdraw departments:', e);
        this.withdrawTags = [];
      }
    },
    onWithdrawParentChange() {
      this.withdrawForm.childTagId = '';
    },
    async openWithdrawModal(row) {
      if (this.withdrawTags.length === 0) {
        await this.loadWithdrawDepartments();
      }
      if (this.withdrawRootTags.length === 0) {
        this.$notify.warning(
          this.$t('noDepartmentsForWithdraw') ||
            'لا توجد أقسام. أضف قسماً من إدارة الأقسام أولاً.'
        );
        return;
      }
      this.withdrawForm.materialName = row.materialName || '';
      this.withdrawForm.stockReceiptKey =
        row.stockReceiptKey != null && row.stockReceiptKey !== undefined
          ? String(row.stockReceiptKey)
          : row.lastReceiptNumber || '';
      this.withdrawForm.currentStock = row.currentQuantity ?? 0;
      this.withdrawForm.quantity = 0.01;
      this.withdrawForm.parentTagId = '';
      this.withdrawForm.childTagId = '';
      this.withdrawForm.notes = '';
      this.showWithdrawModal = true;
    },
    resetWithdrawForm() {
      this.withdrawForm.materialName = '';
      this.withdrawForm.stockReceiptKey = '';
      this.withdrawForm.currentStock = 0;
      this.withdrawForm.parentTagId = '';
      this.withdrawForm.childTagId = '';
      this.withdrawForm.notes = '';
    },
    async submitWithdraw() {
      if (!(this.withdrawForm.materialName || '').trim()) {
        this.$notify.warning(this.$t('materialNameRequired') || 'اسم المادة مطلوب');
        return;
      }
      if (!this.withdrawForm.quantity || this.withdrawForm.quantity <= 0) {
        this.$notify.warning(this.$t('quantityRequired') || 'الكمية مطلوبة وأكبر من صفر');
        return;
      }
      if (this.withdrawForm.quantity > this.withdrawForm.currentStock) {
        this.$notify.error(this.$t('insufficientQuantity') || 'الكمية المتاحة غير كافية');
        return;
      }
      if (!this.withdrawForm.parentTagId) {
        this.$notify.warning(this.$t('selectWithdrawDepartment') || 'اختر القسم');
        return;
      }
      const tagId = this.withdrawForm.childTagId
        ? Number(this.withdrawForm.childTagId)
        : Number(this.withdrawForm.parentTagId);
      if (!tagId) {
        this.$notify.warning(this.$t('selectWithdrawDepartment') || 'اختر القسم');
        return;
      }
      try {
        this.savingWithdraw = true;
        const response = await HTTP.post('Inventory/WithdrawStock', {
          materialName: this.withdrawForm.materialName.trim(),
          receiptNumber: this.withdrawForm.stockReceiptKey || null,
          quantity: this.withdrawForm.quantity,
          tagId,
          notes: this.withdrawForm.notes || null
        });
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(
            response.data.message || (this.$t('withdrawSuccess') || 'تم السحب بنجاح')
          );
          this.showWithdrawModal = false;
          this.loadInventory();
          if (this.activeInventoryTab === 'movements') this.loadStockMovements();
        } else {
          this.$notify.error(response.data?.message || (this.$t('error') || 'حدث خطأ'));
        }
      } catch (error) {
        this.$notify.error(error.response?.data?.message || (this.$t('error') || 'حدث خطأ'));
      } finally {
        this.savingWithdraw = false;
      }
    },
    /** أرقام لاتينية (0–9) لتنسيق موحّد في الجداول بغض النظر عن لغة الواجهة */
    formatNumber(val) {
      if (val == null || val === '') return '0';
      const n = Number(val);
      if (Number.isNaN(n)) return '0';
      return n.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    },
    buildReceiptUrl(fileName) {
      if (!fileName) return '#';
      const name = String(fileName).split(/[/\\]/).pop();
      if (!name) return '#';
      const base = String(resolveApiBaseUrl() || '').replace(/\/$/, '');
      if (base) return `${base}/Receipts/${encodeURIComponent(name)}`;
      return `${window.location.origin}/Receipts/${encodeURIComponent(name)}`;
    },
    /** رابط كامل من الباك أو اسم ملف قديم → URL للعرض */
    receiptPublicUrl(ref) {
      if (!ref) return '#';
      const s = String(ref).trim();
      if (/^https?:\/\//i.test(s)) return s;
      return this.buildReceiptUrl(s);
    },
    /** مرجع المرفق من سجل الحركات: رابط كامل من الباك أو اسم ملف */
    movementAttachmentRef(m) {
      if (!m) return '';
      return m.receiptAttachmentUrl || m.receiptAttachmentPath || m.receiptFileName || '';
    },
    isReceiptImageFile(pathOrUrl) {
      if (!pathOrUrl || typeof pathOrUrl !== 'string') return false;
      const segment = pathOrUrl.split(/[/\\]/).pop() || '';
      const base = segment.split('?')[0];
      const ext = (base.includes('.') ? base.split('.').pop() : '').toLowerCase();
      return ['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(ext);
    }
  }
};
</script>

<style scoped>
.users-form-hint {
  display: block;
  margin-top: 0.4rem;
  font-size: 0.85rem;
  color: var(--text-secondary, #6b7280);
}
.users-table {
  width: 100%;
  margin-top: 1rem;
}
.users-table th,
.users-table td {
  padding: 0.75rem 1rem;
  vertical-align: middle;
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
  margin: 0;
}

.inventory-tab-panel {
  border-top: 1px solid var(--border-color);
}

.inventory-filters-body {
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.inventory-table-section {
  padding: 0;
}

.inventory-table-section .table-responsive {
  margin: 0;
}

.inventory-table-section .users-table {
  margin-top: 0;
}

.inventory-empty-state {
  min-height: 220px;
  padding: 2rem 1rem;
}

.inventory-pagination-body {
  padding-top: 0.75rem;
  border-top: 1px solid var(--border-color);
}

.movements-filters .btn-refresh {
  flex-shrink: 0;
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
.movements-filter-date {
  min-width: 0;
}

.movements-filter-summary-wrap {
  width: 100%;
  padding: 0.85rem 1rem;
  border-bottom: 1px solid var(--border-color);
}
.movements-filter-total-bar.inventory-stock-total-bar {
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-sm, 0 1px 2px rgba(0, 0, 0, 0.06));
}
.movements-filters {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.65rem;
  align-items: center;
}

.movements-filter-input {
  max-width: none;
  width: 100%;
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

/* مودالات المخزون — متوافقة مع users-modal في main.css */
.inventory-modal-subtitle {
  text-align: center;
  margin: -0.35rem 0 1.25rem;
  font-size: 0.9375rem;
  color: var(--text-secondary);
  line-height: 1.55;
  font-weight: 500;
}

.inventory-lines-panel {
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  overflow: hidden;
  background: var(--bg-primary);
}

.inventory-lines-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
}

.inventory-lines-toolbar__text {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.inventory-lines-heading {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-weight: 700;
  font-size: 0.9375rem;
  color: var(--text-primary);
}

.inventory-lines-hint {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-secondary);
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
}

.inventory-add-line-btn {
  display: inline-flex;
  align-items: center;
  border: none;
  border-radius: 0.5rem;
  padding: 0.45rem 0.95rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  background: var(--primary-color);
  color: #fff;
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.inventory-add-line-btn:hover {
  opacity: 0.92;
  transform: translateY(-1px);
}

.stock-lines-wrap {
  margin: 0;
  max-height: min(52vh, 420px);
  overflow: auto;
}

/* يطابق ألوان/تدرجات الجداول العامة في main.css — لا تُعاد تعريف خلفية الرأس أو صفوف الجسم هنا */
.stock-lines-table {
  width: 100%;
  margin-top: 0;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.stock-lines-table thead th {
  position: sticky;
  top: 0;
  z-index: 2;
  white-space: nowrap;
}

.stock-lines-table.users-table th,
.stock-lines-table.users-table td {
  padding: 0.5rem 0.5rem;
  vertical-align: middle;
}

.stock-lines-table .col-material {
  min-width: 160px;
  text-align: right;
}

.stock-lines-table .col-num {
  width: 1%;
  min-width: 88px;
}

.stock-lines-table .col-amount {
  min-width: 96px;
}

.stock-lines-table .col-unit {
  min-width: 110px;
}

.stock-lines-table .col-actions {
  width: 52px;
}

.stock-line-input {
  width: 100%;
  min-width: 0;
  padding: 0.45rem 0.55rem;
  font-size: 0.875rem;
  border-radius: 0.45rem;
}

.stock-line-input--num {
  text-align: center;
}

.stock-line-input--unit {
  padding-right: 0.35rem;
  padding-left: 0.35rem;
}

.stock-line-amount {
  font-weight: 700;
  text-align: center;
  color: var(--primary-color) !important;
  font-variant-numeric: tabular-nums;
}

.stock-btn-remove-row {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  padding: 0;
  border: 1px solid rgba(239, 68, 68, 0.35);
  border-radius: 0.45rem;
  background: rgba(239, 68, 68, 0.08);
  color: #dc2626;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}

.stock-btn-remove-row:hover {
  background: #ef4444;
  color: #fff;
  border-color: #ef4444;
}

.inventory-stock-total-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.85rem 1rem;
  background: color-mix(in srgb, var(--primary-color) 8%, transparent);
  border-top: 1px solid var(--border-color);
}

.inventory-stock-total-bar__label {
  font-weight: 700;
  font-size: 0.9375rem;
  color: var(--text-primary);
}

.inventory-stock-total-bar__value {
  font-weight: 800;
  font-size: 1.05rem;
  color: var(--primary-color);
  font-variant-numeric: tabular-nums;
}

.inventory-file-drop {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 3rem;
  padding: 0.65rem 1rem;
  border: 2px dashed var(--border-color);
  border-radius: 0.65rem;
  background: var(--bg-secondary);
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
}

.inventory-file-drop:hover {
  border-color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 6%, transparent);
}

.inventory-file-drop__input {
  position: absolute;
  width: 0;
  height: 0;
  opacity: 0;
  overflow: hidden;
}

.inventory-file-drop__text {
  display: inline-flex;
  align-items: center;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.inventory-file-name {
  display: block;
  margin-top: 0.35rem;
  font-size: 0.8rem;
  color: var(--success-color, #16a34a);
  font-weight: 600;
}

.inventory-receipt-cell {
  vertical-align: middle;
  min-width: 7.5rem;
}

.receipt-link-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.7rem;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
  border: 1px solid color-mix(in srgb, var(--primary-color) 35%, transparent);
  text-decoration: none;
  line-height: 1.2;
  transition:
    background 0.15s ease,
    border-color 0.15s ease,
    color 0.15s ease,
    box-shadow 0.15s ease;
  white-space: nowrap;
}

.receipt-link-btn:hover {
  background: color-mix(in srgb, var(--primary-color) 18%, transparent);
  border-color: var(--primary-color);
  color: var(--primary-hover, var(--primary-color));
  text-decoration: none;
  box-shadow: 0 2px 8px color-mix(in srgb, var(--primary-color) 20%, transparent);
}

.receipt-link-btn__icon {
  font-size: 0.95rem;
  flex-shrink: 0;
}

.inventory-receipt-thumb-frame {
  display: block;
  flex-shrink: 0;
  border-radius: 0.5rem;
  overflow: hidden;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.08));
  line-height: 0;
  transition:
    box-shadow 0.2s ease,
    transform 0.2s ease,
    border-color 0.2s ease;
}

.inventory-receipt-thumb-frame:hover {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md, 0 4px 12px rgba(0, 0, 0, 0.1));
  transform: scale(1.03);
}

.inventory-receipt-thumb {
  display: block;
  width: 52px;
  height: 52px;
  object-fit: cover;
}
</style>
