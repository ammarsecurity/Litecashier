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
        <div class="app-page-content reservations-page">
          <!-- Header -->
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper res-page-icon">
                  <b-icon icon="calendar-check-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("reservations") || "الحجوزات" }}</h1>
                  <p class="header-subtitle">{{ $t("reservationsPageSubtitle") || "إدارة حجوزات الطاولات ومتابعة التوفر" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="refreshPage" :disabled="show">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Overview stats -->
          <div class="app-overview-grid">
            <div class="app-overview-stat res-stat-card">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="calendar-day"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ totalItems }}</div>
                <div class="app-overview-stat-label">{{ $t("filteredReservations") || "نتائج الفلتر" }}</div>
              </div>
            </div>
            <div class="app-overview-stat res-stat-card">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="hourglass-split"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ summaryStats.pendingCount || 0 }}</div>
                <div class="app-overview-stat-label">{{ $t("pending") || "قيد الانتظار" }}</div>
              </div>
            </div>
            <div class="app-overview-stat res-stat-card">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ summaryStats.confirmedCount || 0 }}</div>
                <div class="app-overview-stat-label">{{ $t("confirmed") || "مؤكد" }}</div>
              </div>
            </div>
            <div class="app-overview-stat res-stat-card">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="table"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ summaryStats.reservedTablesCount || 0 }}</div>
                <div class="app-overview-stat-label">{{ $t("reservedTables") || "طاولات محجوزة" }}</div>
              </div>
            </div>
          </div>

          <!-- Filters toolbar -->
          <div class="app-section-card app-filters-panel app-filters-panel--inset">
            <div class="app-filters-panel-head app-section-header app-section-header--toolbar res-filters-toolbar">
              <div class="res-toolbar-main">
              <div class="app-filters-fields app-filters-fields--3 res-toolbar-fields">
                  <div class="app-search-wrap res-search-wrap">
                    <b-icon icon="search" class="app-search-icon"></b-icon>
                    <input
                      v-model="searchQuery"
                      type="search"
                      class="app-search-input"
                      :placeholder="$t('searchReservationsPlaceholder') || 'اسم أو هاتف...'"
                      autocomplete="off"
                      @input="onSearchInput"
                    />
                  </div>
                  <select v-model="statusFilter" class="res-toolbar-select" @change="onFilterChange">
                    <option value="">{{ $t("allStatuses") || "كل الحالات" }}</option>
                    <option value="Pending">{{ $t("pending") || "قيد الانتظار" }}</option>
                    <option value="Confirmed">{{ $t("confirmed") || "مؤكد" }}</option>
                    <option value="Seated">{{ $t("seated") || "جلس" }}</option>
                    <option value="Completed">{{ $t("completed") || "مكتمل" }}</option>
                    <option value="Cancelled">{{ $t("cancelled") || "ملغي" }}</option>
                  </select>
                  <button
                    type="button"
                    class="res-advanced-toggle"
                    :class="{ 'res-advanced-toggle--open': showAdvancedFilters }"
                    @click="showAdvancedFilters = !showAdvancedFilters"
                  >
                    <b-icon icon="sliders"></b-icon>
                    <span>{{ $t("advancedFilters") || "فلاتر متقدمة" }}</span>
                    <b-icon :icon="showAdvancedFilters ? 'chevron-up' : 'chevron-down'" class="res-advanced-chevron"></b-icon>
                  </button>
                </div>
                <div class="res-date-chips" role="tablist">
                  <button
                    type="button"
                    class="res-date-chip"
                    :class="{ 'res-date-chip--active': activeQuickFilter === 'today' }"
                    @click="applyQuickFilter('today')"
                  >
                    {{ $t("quickFilterToday") || "اليوم" }}
                  </button>
                  <button
                    type="button"
                    class="res-date-chip"
                    :class="{ 'res-date-chip--active': activeQuickFilter === 'tomorrow' }"
                    @click="applyQuickFilter('tomorrow')"
                  >
                    {{ $t("quickFilterTomorrow") || "غداً" }}
                  </button>
                  <button
                    type="button"
                    class="res-date-chip"
                    :class="{ 'res-date-chip--active': activeQuickFilter === 'week' }"
                    @click="applyQuickFilter('week')"
                  >
                    {{ $t("quickFilterWeek") || "هذا الأسبوع" }}
                  </button>
                </div>
              </div>
            </div>
            <div v-show="showAdvancedFilters" class="app-section-body res-advanced-filters">
              <div class="app-filters-fields app-filters-fields--3 res-filter-grid">
                <div class="res-filter-field">
                  <label class="res-filter-label">{{ $t("filterMode") || "نمط التاريخ" }}</label>
                  <select v-model="filterMode" class="res-filter-input" @change="onFilterModeChange">
                    <option value="single">{{ $t("reservationSingleDay") || "يوم واحد" }}</option>
                    <option value="range">{{ $t("dateRange") || "نطاق" }}</option>
                  </select>
                </div>
                <div class="res-filter-field">
                  <label class="res-filter-label">
                    {{ filterMode === 'single' ? ($t('reservationSingleDay') || 'يوم محدد') : ($t('fromDate') || 'من تاريخ') }}
                  </label>
                  <input
                    v-if="filterMode === 'single'"
                    type="date"
                    v-model="singleDate"
                    class="res-filter-input"
                    @change="onFilterChange"
                  />
                  <input
                    v-else
                    type="date"
                    v-model="fromDate"
                    class="res-filter-input"
                    @change="onFilterChange"
                  />
                </div>
                <div v-if="filterMode === 'range'" class="res-filter-field">
                  <label class="res-filter-label">{{ $t("toDate") || "إلى تاريخ" }}</label>
                  <input type="date" v-model="toDate" class="res-filter-input" @change="onFilterChange" />
                </div>
                <div class="res-filter-field">
                  <label class="res-filter-label">{{ $t("reservationTimeFrom") || "من وقت" }}</label>
                  <input type="time" v-model="fromTime" class="res-filter-input" @change="onFilterChange" />
                </div>
                <div class="res-filter-field">
                  <label class="res-filter-label">{{ $t("reservationTimeTo") || "إلى وقت" }}</label>
                  <input type="time" v-model="toTime" class="res-filter-input" @change="onFilterChange" />
                </div>
                <div class="res-filter-field">
                  <label class="res-filter-label">{{ $t("table") || "الطاولة" }}</label>
                  <select v-model="tableFilterId" class="res-filter-input" @change="onFilterChange">
                    <option :value="null">{{ $t("all") || "الكل" }}</option>
                    <option v-for="t in allTables" :key="t.id" :value="t.id">
                      {{ t.tableNumber }}{{ t.zone ? ` - ${t.zone}` : '' }}
                    </option>
                  </select>
                </div>
              </div>
            </div>
          </div>

          <!-- Floor plan (full width) -->
          <div class="app-section-card res-floor-section">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap app-section-icon-wrap--purple">
                  <b-icon icon="diagram-3"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("reservationFloorPlan") || "مخطط الحجوزات" }}</h3>
                  <p class="app-section-subtitle">{{ floorPlanSubtitle }}</p>
                </div>
              </div>
              <button v-if="tableFilterId" type="button" class="res-clear-filter-btn" @click="clearTableFilter">
                <b-icon icon="x-circle"></b-icon>
                {{ $t("clearTableFilter") || "إلغاء فلتر الطاولة" }}
              </button>
            </div>
            <div class="app-section-body res-floor-body">
              <ReservationFloorPlan
                :filter-date="floorPlanDate"
                :filter-date-to="floorPlanDateTo"
                :filter-time="fromTime"
                :selected-table-id="addForm.tableId"
                @table-select="onFloorTableSelect"
              />
            </div>
          </div>

          <!-- Reservations list -->
          <div class="app-section-card res-list-section">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="list-ul"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">
                    {{ $t("reservationsListTitle") || "قائمة الحجوزات" }}
                    <span v-if="totalItems" class="res-count-inline">{{ totalItems }}</span>
                  </h3>
                  <p class="app-section-subtitle">{{ $t("reservationsListHint") || "عرض وتعديل حالات الحجز" }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body res-table-body">
              <div v-if="!show && !reservations.length" class="res-empty-state">
                <b-icon icon="calendar-x" class="res-empty-icon"></b-icon>
                <p class="res-empty-title">{{ $t("noReservations") || "لا توجد حجوزات" }}</p>
                <p class="res-empty-hint">{{ $t("noReservationsHint") || "غيّر الفلاتر أو اختر طاولة من المخطط لإضافة حجز" }}</p>
              </div>
              <div v-else class="report-table-container">
                <b-table
                  :items="reservations"
                  :fields="reservationFields"
                  hover
                  responsive
                  class="reports-table reservations-table"
                  :tbody-tr-class="getReservationRowClass"
                  :empty-text="$t('noData') || 'لا توجد بيانات'"
                >
              <template #cell(customerName)="row">
                <div class="reservation-customer-cell">
                  <b-icon icon="person-fill" class="customer-icon"></b-icon>
                  <span class="customer-name-text">{{ row.item.customerName }}</span>
                </div>
              </template>

              <template #cell(phoneNumber)="row">
                <div class="reservation-phone-cell">
                  <b-icon icon="telephone-fill" class="phone-icon"></b-icon>
                  <span>{{ row.item.phoneNumber }}</span>
                </div>
              </template>

              <template #cell(reservationDateTime)="row">
                <div class="reservation-datetime-cell">
                  <b-icon icon="clock-fill" class="datetime-icon"></b-icon>
                  <span>{{ formatDateTime(row.item.reservationDateTime) }}</span>
                </div>
              </template>

              <template #cell(numberOfGuests)="row">
                <div class="reservation-guests-cell">
                  <b-icon icon="people-fill" class="guests-icon"></b-icon>
                  <span>{{ row.item.numberOfGuests }}</span>
                </div>
              </template>

              <template #cell(table)="row">
                <span v-if="row.item.table" class="reservation-table-cell">
                  <b-icon icon="table" class="table-icon"></b-icon>
                  {{ row.item.table.tableNumber }}
                </span>
                <span v-else class="text-muted">-</span>
              </template>

              <template #cell(status)="row">
                <span class="reservation-status-badge" :class="getReservationStatusClass(row.item.status)">
                  {{ getStatusText(row.item.status) }}
                </span>
              </template>

              <template #head(actions)>
                <span class="res-actions-head">{{ $t("actions") || "الإجراءات" }}</span>
              </template>

              <template #cell(actions)="row">
                <div class="actions-cell res-actions-cell">
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--edit"
                    @click="editReservation(row.item)"
                    :title="$t('edit')"
                  >
                    <b-icon icon="pencil-square" class="action-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--status res-status-dropdown"
                    :class="{ 'res-status-dropdown--open': isStatusMenuOpen(row.item.id) }"
                    :title="$t('changeStatus') || 'تغيير الحالة'"
                    @click.stop="toggleStatusMenu($event, row.item)"
                  >
                    <b-icon icon="arrow-repeat" class="action-icon"></b-icon>
                  </button>
                </div>
              </template>
            </b-table>

            <div class="pagination-container" v-if="totalPages > 1">
              <b-pagination
                v-model="currentPage"
                :total-rows="totalItems"
                :per-page="pageSize"
                :limit="7"
                first-number
                last-number
                @change="onPageChange"
                class="reservations-pagination"
              ></b-pagination>
              <div class="pagination-info">
                <span>{{ $t('showing') || 'عرض' }} {{ ((currentPage - 1) * pageSize) + 1 }} - {{ Math.min(currentPage * pageSize, totalItems) }} {{ $t('of') || 'من' }} {{ totalItems }}</span>
              </div>
            </div>
              </div>
            </div>
          </div>

          <!-- Status flyout — fixed position, outside table overflow -->
          <div
            v-if="statusMenu.open"
            class="res-status-menu-backdrop"
            @click="closeStatusMenu"
          ></div>
          <div
            v-if="statusMenu.open"
            ref="statusMenuFlyout"
            class="res-status-menu res-status-menu-flyout"
            :style="statusMenuStyle"
            role="menu"
            @click.stop
          >
            <div class="res-status-menu-header">
              {{ $t("changeStatus") || "تغيير الحالة" }}
            </div>
            <button
              v-for="opt in statusMenuPrimaryOptions"
              :key="opt.status"
              type="button"
              role="menuitem"
              class="res-status-option"
              :class="[
                `res-status-option--${opt.variant}`,
                { active: statusMenu.reservation && statusMenu.reservation.status === opt.status },
              ]"
              @click="pickReservationStatus(opt.status)"
            >
              <b-icon :icon="opt.icon"></b-icon>
              <span>{{ $t(opt.labelKey) || opt.fallback }}</span>
            </button>
            <hr class="res-status-menu-divider" role="separator" />
            <button
              type="button"
              role="menuitem"
              class="res-status-option res-status-option--cancelled"
              :class="{ active: statusMenu.reservation && statusMenu.reservation.status === 'Cancelled' }"
              @click="pickReservationStatus('Cancelled')"
            >
              <b-icon icon="x-circle-fill"></b-icon>
              <span>{{ $t("cancelled") }}</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Add Reservation Modal -->
      <b-modal id="modal-addReservation" :title="$t('addReservation')" hide-header hide-footer class="users-modal" size="lg">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("addReservation") || "إضافة حجز" }}</h2>
          <form @submit.prevent="addReservation" class="users-form">
            <div class="modal-form-grid">
              <div class="users-form-group users-form-group--full res-customer-section">
                <label class="users-form-label">
                  <b-icon icon="person-lines-fill" class="form-label-icon"></b-icon>
                  {{ $t("customerRecipientSelection") || "العميل" }}
                </label>
                <div class="res-customer-mode">
                  <label class="res-customer-mode-label">
                    <input v-model="addCustomerMode" type="radio" value="existing" class="res-customer-mode-input" />
                    <span>{{ $t("useExistingCustomer") || "عميل موجود" }}</span>
                  </label>
                  <label class="res-customer-mode-label">
                    <input v-model="addCustomerMode" type="radio" value="new" class="res-customer-mode-input" />
                    <span>{{ $t("addNewCustomer") || "عميل جديد" }}</span>
                  </label>
                </div>

                <div v-if="addCustomerMode === 'existing'" class="res-customer-existing">
                  <div class="table-search-input-wrapper res-customer-search">
                    <b-icon icon="search" class="table-search-icon"></b-icon>
                    <input
                      v-model="addCustomerSearch"
                      type="search"
                      class="table-search-input"
                      :placeholder="$t('searchCustomerPlaceholder') || 'بحث بالاسم أو الهاتف...'"
                      autocomplete="off"
                    />
                  </div>
                  <select
                    v-model="addSelectedCustomerKey"
                    class="users-form-input"
                    :disabled="loadingReservationCustomers"
                    required
                    @change="applyAddSelectedCustomer"
                  >
                    <option value="">{{ $t("selectCustomer") || "اختر العميل" }}</option>
                    <option
                      v-for="c in filteredAddCustomers"
                      :key="customerOptionKey(c)"
                      :value="customerOptionKey(c)"
                    >
                      {{ c.name }} — {{ c.phoneNumber }}
                    </option>
                  </select>
                  <div v-if="addForm.customerName" class="res-customer-preview">
                    <b-icon icon="person-check-fill"></b-icon>
                    <span>{{ addForm.customerName }} · {{ addForm.phoneNumber }}</span>
                  </div>
                </div>

                <div v-else class="res-customer-new modal-form-grid">
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                      {{ $t("customerName") || "اسم العميل" }}
                    </label>
                    <input
                      id="inputCustomerName"
                      v-model="addForm.customerName"
                      type="text"
                      :placeholder="$t('customerName') || 'اسم العميل'"
                      required
                      class="users-form-input"
                    />
                  </div>
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                      {{ $t("phoneNumber") || "رقم الهاتف" }}
                    </label>
                    <input
                      id="inputPhoneNumber"
                      v-model="addForm.phoneNumber"
                      type="tel"
                      :placeholder="$t('phoneNumber') || 'رقم الهاتف'"
                      required
                      class="users-form-input"
                    />
                  </div>
                </div>
              </div>

              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="calendar-fill" class="form-label-icon"></b-icon>
                  {{ $t("reservationDateTime") || "تاريخ ووقت الحجز" }}
                </label>
                <input 
                  id="inputDateTime"
                  v-model="addForm.reservationDateTime" 
                  type="datetime-local"
                  required 
                  class="users-form-input"
                  @change="onAddFormDateTimeChange"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                  {{ $t("numberOfGuests") || "عدد الضيوف" }}
                </label>
                <input 
                  id="inputNumberOfGuests"
                  v-model="addForm.numberOfGuests" 
                  type="number"
                  min="1"
                  max="50"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">{{ $t("status") || "الحالة" }}</label>
                <select v-model="addForm.status" class="users-form-input">
                  <option value="Pending">{{ $t("pending") }}</option>
                  <option value="Confirmed">{{ $t("confirmed") }}</option>
                  <option value="Seated">{{ $t("seated") }}</option>
                </select>
              </div>
              <div class="users-form-group users-form-group--full">
                <label class="users-form-label">
                  <b-icon icon="table" class="form-label-icon"></b-icon>
                  {{ $t("table") || "الطاولة" }}
                </label>
                <div v-if="selectedAddTableLabel" class="res-selected-table-display">
                  <b-icon icon="table" class="res-selected-table-icon"></b-icon>
                  <span class="res-selected-table-name">{{ selectedAddTableLabel }}</span>
                </div>
                <p v-else class="res-table-floor-hint">
                  <b-icon icon="diagram-3"></b-icon>
                  {{ $t("selectTableFromFloorPlan") || "اختر الطاولة من مخطط الحجوزات" }}
                </p>
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="chat-left-text-fill" class="form-label-icon"></b-icon>
                {{ $t("specialRequests") || "طلبات خاصة (اختياري)" }}
              </label>
              <textarea 
                id="inputSpecialRequests"
                v-model="addForm.specialRequests" 
                :placeholder="$t('specialRequests') || 'طلبات خاصة'" 
                class="users-form-input"
                rows="3"
              ></textarea>
            </div>
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("add") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addReservation')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("close") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Edit Reservation Modal -->
      <b-modal id="modal-editReservation" :title="$t('editReservation')" hide-header hide-footer class="users-modal" size="lg">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("editReservation") || "تعديل حجز" }}</h2>
          <form @submit.prevent="updateReservation" class="users-form">
            <div class="modal-form-grid">
              <div class="users-form-group users-form-group--full res-customer-section">
                <label class="users-form-label">
                  <b-icon icon="person-lines-fill" class="form-label-icon"></b-icon>
                  {{ $t("customerRecipientSelection") || "العميل" }}
                </label>
                <div class="res-customer-mode">
                  <label class="res-customer-mode-label">
                    <input v-model="editCustomerMode" type="radio" value="existing" class="res-customer-mode-input" />
                    <span>{{ $t("useExistingCustomer") || "عميل موجود" }}</span>
                  </label>
                  <label class="res-customer-mode-label">
                    <input v-model="editCustomerMode" type="radio" value="new" class="res-customer-mode-input" />
                    <span>{{ $t("addNewCustomer") || "عميل جديد" }}</span>
                  </label>
                </div>

                <div v-if="editCustomerMode === 'existing'" class="res-customer-existing">
                  <div class="table-search-input-wrapper res-customer-search">
                    <b-icon icon="search" class="table-search-icon"></b-icon>
                    <input
                      v-model="editCustomerSearch"
                      type="search"
                      class="table-search-input"
                      :placeholder="$t('searchCustomerPlaceholder') || 'بحث بالاسم أو الهاتف...'"
                      autocomplete="off"
                    />
                  </div>
                  <select
                    v-model="editSelectedCustomerKey"
                    class="users-form-input"
                    :disabled="loadingReservationCustomers"
                    required
                    @change="applyEditSelectedCustomer"
                  >
                    <option value="">{{ $t("selectCustomer") || "اختر العميل" }}</option>
                    <option
                      v-for="c in filteredEditCustomers"
                      :key="'edit-c-' + customerOptionKey(c)"
                      :value="customerOptionKey(c)"
                    >
                      {{ c.name }} — {{ c.phoneNumber }}
                    </option>
                  </select>
                  <div v-if="editForm.customerName" class="res-customer-preview">
                    <b-icon icon="person-check-fill"></b-icon>
                    <span>{{ editForm.customerName }} · {{ editForm.phoneNumber }}</span>
                  </div>
                </div>

                <div v-else class="res-customer-new modal-form-grid">
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                      {{ $t("customerName") || "اسم العميل" }}
                    </label>
                    <input
                      id="inputCustomerNameEdit"
                      v-model="editForm.customerName"
                      type="text"
                      required
                      class="users-form-input"
                    />
                  </div>
                  <div class="users-form-group">
                    <label class="users-form-label">
                      <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                      {{ $t("phoneNumber") || "رقم الهاتف" }}
                    </label>
                    <input
                      id="inputPhoneNumberEdit"
                      v-model="editForm.phoneNumber"
                      type="tel"
                      required
                      class="users-form-input"
                    />
                  </div>
                </div>
              </div>

              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="calendar-fill" class="form-label-icon"></b-icon>
                  {{ $t("reservationDateTime") || "تاريخ ووقت الحجز" }}
                </label>
                <input 
                  id="inputDateTimeEdit"
                  v-model="editForm.reservationDateTime" 
                  type="datetime-local"
                  required 
                  class="users-form-input"
                  @change="onEditFormDateTimeChange"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                  {{ $t("numberOfGuests") || "عدد الضيوف" }}
                </label>
                <input 
                  id="inputNumberOfGuestsEdit"
                  v-model="editForm.numberOfGuests" 
                  type="number"
                  min="1"
                  max="50"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">{{ $t("status") || "الحالة" }}</label>
                <select v-model="editForm.status" class="users-form-input">
                  <option value="Pending">{{ $t("pending") }}</option>
                  <option value="Confirmed">{{ $t("confirmed") }}</option>
                  <option value="Seated">{{ $t("seated") }}</option>
                  <option value="Completed">{{ $t("completed") }}</option>
                  <option value="Cancelled">{{ $t("cancelled") }}</option>
                </select>
              </div>
              <div class="users-form-group users-form-group--full">
                <label class="users-form-label">
                  <b-icon icon="table" class="form-label-icon"></b-icon>
                  {{ $t("table") || "الطاولة" }}
                </label>
                <div class="reservation-table-picker">
                  <div class="users-form-sublabel">
                    <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                    {{ $t("zone") || "الموقع" }}
                  </div>
                  <select
                    v-model="tableZoneFilter"
                    class="users-form-input reservation-zone-select"
                    @change="onReservationTableZoneFilterChanged"
                  >
                  <option value="">{{ $t("allZones") || "جميع المواقع" }}</option>
                  <option v-for="zone in uniqueZones" :key="'edit-z-' + zone" :value="zone">{{ zone }}</option>
                </select>
                <div class="table-search-wrapper">
                  <div class="table-search-input-wrapper">
                    <b-icon icon="search" class="table-search-icon"></b-icon>
                    <input
                      v-model="tableSearchQuery"
                      type="text"
                      :placeholder="$t('searchTable') || 'ابحث عن طاولة...'"
                      class="table-search-input"
                      autocomplete="off"
                    />
                  </div>
                </div>
                <select v-model="editForm.tableId" class="users-form-input reservation-table-select" size="8">
                  <option :value="null">{{ $t("selectTable") || "اختر طاولة" }}</option>
                  <option
                    v-for="table in filteredTables"
                    :key="table.id"
                    :value="table.id"
                    :disabled="isTableUnavailableForForm(table.id)"
                  >
                    {{ getTableOptionLabel(table) }}
                  </option>
                </select>
                </div>
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="chat-left-text-fill" class="form-label-icon"></b-icon>
                {{ $t("specialRequests") || "طلبات خاصة" }}
              </label>
              <textarea 
                id="inputSpecialRequestsEdit"
                v-model="editForm.specialRequests" 
                class="users-form-input"
                rows="3"
              ></textarea>
            </div>
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("save") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editReservation')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("close") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>
    </div>
  </b-overlay>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import ReservationFloorPlan from "@/components/Restaurant/ReservationFloorPlan.vue";
import { HTTP } from "../../http/api.js";

export default {
  name: "ReservationsView",
  components: {
    AppHeader,
    ReservationFloorPlan,
  },
  data() {
    return {
      show: false,
      reservations: [],
      availableTables: [],
      allTables: [],
      tableAvailability: {},
      tableSearchQuery: "",
      tableZoneFilter: "",
      searchQuery: "",
      searchDebounceTimer: null,
      showAdvancedFilters: false,
      filterMode: "single",
      singleDate: "",
      fromDate: "",
      toDate: "",
      fromTime: "",
      toTime: "",
      statusFilter: "",
      tableFilterId: null,
      summaryStats: {
        todayCount: 0,
        pendingCount: 0,
        confirmedCount: 0,
        reservedTablesCount: 0,
      },
      currentPage: 1,
      pageSize: 10,
      totalItems: 0,
      totalPages: 0,
      addForm: {
        customerName: "",
        phoneNumber: "",
        reservationDateTime: "",
        numberOfGuests: 2,
        tableId: null,
        specialRequests: "",
        status: "Pending"
      },
      editForm: {
        id: null,
        customerName: "",
        phoneNumber: "",
        reservationDateTime: "",
        numberOfGuests: 2,
        tableId: null,
        specialRequests: "",
        status: "Pending"
      },
      reservationCustomers: [],
      loadingReservationCustomers: false,
      addCustomerMode: "new",
      addSelectedCustomerKey: "",
      addCustomerSearch: "",
      editCustomerMode: "new",
      editSelectedCustomerKey: "",
      editCustomerSearch: "",
      statusMenu: {
        open: false,
        reservation: null,
        top: 0,
        left: 0,
      },
    };
  },
  computed: {
    statusMenuStyle() {
      return {
        top: `${this.statusMenu.top}px`,
        left: `${this.statusMenu.left}px`,
      };
    },
    statusMenuPrimaryOptions() {
      return [
        { status: "Pending", variant: "pending", icon: "hourglass-split", labelKey: "pending", fallback: "قيد الانتظار" },
        { status: "Confirmed", variant: "confirmed", icon: "check-circle-fill", labelKey: "confirmed", fallback: "مؤكد" },
        { status: "Seated", variant: "seated", icon: "person-check-fill", labelKey: "markSeated", fallback: "تعيين جلس" },
        { status: "Completed", variant: "completed", icon: "check2-all", labelKey: "completed", fallback: "مكتمل" },
      ];
    },
    uniqueZones() {
      if (!Array.isArray(this.allTables)) return [];
      const zones = this.allTables
        .map((table) => table.zone)
        .filter((zone) => zone && String(zone).trim() !== "");
      return [...new Set(zones)].sort();
    },
    filteredTables() {
      let tables = Array.isArray(this.allTables) ? [...this.allTables] : [];
      const zf = (this.tableZoneFilter ?? "").trim();
      if (zf) {
        tables = tables.filter((t) => (t.zone && String(t.zone).trim()) === zf);
      }
      if (!this.tableSearchQuery) {
        return tables;
      }
      const query = this.tableSearchQuery.toLowerCase().trim();
      return tables.filter((table) => {
        const tableNumber = String(table.tableNumber || "").toLowerCase();
        const zone = (table.zone || "").toLowerCase();
        const status = (table.status || "").toLowerCase();
        return (
          tableNumber.includes(query) ||
          zone.includes(query) ||
          status.includes(query)
        );
      });
    },
    reservationFields() {
      return [
        {
          key: 'customerName',
          label: this.$t('customerName') || 'اسم العميل',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'phoneNumber',
          label: this.$t('phoneNumber') || 'رقم الهاتف',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'reservationDateTime',
          label: this.$t('reservationDateTime') || 'تاريخ ووقت الحجز',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'numberOfGuests',
          label: this.$t('numberOfGuests') || 'عدد الضيوف',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'table',
          label: this.$t('table') || 'الطاولة',
          sortable: false,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'status',
          label: this.$t('status') || 'الحالة',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'actions',
          label: this.$t('actions') || 'الإجراءات',
          sortable: false,
          thClass: 'reservation-header-cell res-th-actions',
          tdClass: 'res-td-actions'
        }
      ];
    },
    floorPlanDate() {
      if (this.filterMode === "single" && this.singleDate) {
        return this.singleDate;
      }
      return this.fromDate || this.singleDate;
    },
    floorPlanDateTo() {
      if (this.filterMode === "single" && this.singleDate) {
        return this.singleDate;
      }
      return this.toDate || this.fromDate || this.singleDate;
    },
    activeQuickFilter() {
      const today = new Date();
      const fmt = (offset) => {
        const x = new Date(today);
        x.setDate(x.getDate() + offset);
        return x.toISOString().split("T")[0];
      };
      const t0 = fmt(0);
      const t1 = fmt(1);
      const t7 = fmt(7);
      if (this.filterMode === "single" && this.singleDate === t0) return "today";
      if (this.filterMode === "single" && this.singleDate === t1) return "tomorrow";
      if (this.filterMode === "range" && this.fromDate === t0 && this.toDate === t7) return "week";
      return "";
    },
    floorPlanSubtitle() {
      const dateLabel = this.floorPlanDate || "";
      const timeLabel = this.fromTime ? ` · ${this.fromTime}` : "";
      const hint = this.$t("reservationFloorPlanHint") || "اضغط على طاولة لإضافة حجز";
      return dateLabel ? `${dateLabel}${timeLabel} — ${hint}` : hint;
    },
    filteredAddCustomers() {
      return this.filterReservationCustomers(this.addCustomerSearch);
    },
    filteredEditCustomers() {
      return this.filterReservationCustomers(this.editCustomerSearch);
    },
    selectedAddTableLabel() {
      if (!this.addForm.tableId) return "";
      const t = this.allTables.find((x) => Number(x.id) === Number(this.addForm.tableId));
      if (!t) return "";
      return String(t.tableNumber ?? t.TableNumber ?? "");
    },
  },
  created() {
    const today = new Date();
    const nextWeek = new Date();
    nextWeek.setDate(today.getDate() + 7);
    const d = today.toISOString().split("T")[0];
    const w = nextWeek.toISOString().split("T")[0];
    this.filterMode = "range";
    this.singleDate = d;
    this.fromDate = d;
    this.toDate = w;
  },
  mounted() {
    this.getReservations();
    this.getSummary();
    this.getTables();
    this._onStatusMenuReposition = () => {
      if (this.statusMenu.open) this.positionStatusMenu();
    };
    this._onStatusMenuKeydown = (e) => {
      if (e.key === "Escape") this.closeStatusMenu();
    };
    window.addEventListener("resize", this._onStatusMenuReposition);
    window.addEventListener("scroll", this._onStatusMenuReposition, true);
    document.addEventListener("keydown", this._onStatusMenuKeydown);
  },
  beforeDestroy() {
    window.removeEventListener("resize", this._onStatusMenuReposition);
    window.removeEventListener("scroll", this._onStatusMenuReposition, true);
    document.removeEventListener("keydown", this._onStatusMenuKeydown);
  },
  methods: {
    refreshPage() {
      this.getReservations();
      this.getSummary();
      this.getTables();
    },
    getReservations() {
      this.show = true;
      const params = {
        pageNumber: this.currentPage - 1,
        pageSize: this.pageSize,
      };

      if (this.filterMode === "single" && this.singleDate) {
        params.reservationDate = `${this.singleDate}T00:00:00`;
      } else {
        if (this.fromDate) {
          params.fromDate = `${this.fromDate}T00:00:00`;
        }
        if (this.toDate) {
          params.toDate = `${this.toDate}T23:59:59`;
        }
      }

      if (this.fromTime) {
        params.fromTime = this.fromTime;
      }
      if (this.toTime) {
        params.toTime = this.toTime;
      }
      if (this.tableFilterId) {
        params.tableId = this.tableFilterId;
      }
      if (this.statusFilter) {
        params.status = this.statusFilter;
      }
      if (this.searchQuery && this.searchQuery.trim()) {
        params.search = this.searchQuery.trim();
      }

      HTTP.get("Reservations", { params })
        .then((response) => {
          const pagedData = response.data.data;
          this.reservations = pagedData.items || [];
          this.totalItems = pagedData.totalItems || 0;
          this.totalPages = pagedData.totalPages || 0;
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    onPageChange(page) {
      this.currentPage = page;
      this.getReservations();
    },
    onFilterChange() {
      this.currentPage = 1;
      this.getReservations();
      this.getSummary();
    },
    onFilterModeChange() {
      this.onFilterChange();
    },
    onSearchInput() {
      if (this.searchDebounceTimer) {
        clearTimeout(this.searchDebounceTimer);
      }
      this.searchDebounceTimer = setTimeout(() => {
        this.onFilterChange();
      }, 300);
    },
    applyQuickFilter(mode) {
      const today = new Date();
      const d = (offset) => {
        const x = new Date(today);
        x.setDate(x.getDate() + offset);
        return x.toISOString().split("T")[0];
      };
      if (mode === "today") {
        this.filterMode = "single";
        this.singleDate = d(0);
        this.fromDate = d(0);
        this.toDate = d(0);
      } else if (mode === "tomorrow") {
        this.filterMode = "single";
        this.singleDate = d(1);
        this.fromDate = d(1);
        this.toDate = d(1);
      } else if (mode === "week") {
        this.filterMode = "range";
        this.fromDate = d(0);
        this.toDate = d(7);
        this.singleDate = d(0);
      }
      this.onFilterChange();
    },
    onFloorTableSelect(table) {
      this.openAddReservationModal({ table });
    },
    buildDefaultReservationDateTime() {
      const dateStr = this.floorPlanDate || this.singleDate || new Date().toISOString().split("T")[0];
      let time = (this.fromTime || "19:00").trim();
      if (time.length > 5) {
        time = time.slice(0, 5);
      }
      return `${dateStr}T${time}`;
    },
    resetAddForm(dateTime) {
      this.tableZoneFilter = "";
      this.tableSearchQuery = "";
      this.tableAvailability = {};
      this.addCustomerMode = this.reservationCustomers.length ? "existing" : "new";
      this.addSelectedCustomerKey = "";
      this.addCustomerSearch = "";
      this.addForm = {
        customerName: "",
        phoneNumber: "",
        reservationDateTime: dateTime || this.buildDefaultReservationDateTime(),
        numberOfGuests: 2,
        tableId: null,
        specialRequests: "",
        status: "Pending",
      };
    },
    filterReservationCustomers(query) {
      const q = String(query || "").trim().toLowerCase();
      if (!q) return this.reservationCustomers;
      return this.reservationCustomers.filter(
        (c) =>
          String(c.name || "").toLowerCase().includes(q) ||
          String(c.phoneNumber || "").toLowerCase().includes(q)
      );
    },
    customerOptionKey(c) {
      if (c.customerId != null) return `id:${c.customerId}`;
      return `p:${c.phoneNumber}`;
    },
    findCustomerByKey(key) {
      if (!key) return null;
      return this.reservationCustomers.find((c) => this.customerOptionKey(c) === key) || null;
    },
    async loadReservationCustomers() {
      this.loadingReservationCustomers = true;
      try {
        const res = await HTTP.get("Reservations/customers");
        this.reservationCustomers = res?.data?.data || [];
      } catch (e) {
        this.reservationCustomers = [];
      } finally {
        this.loadingReservationCustomers = false;
      }
    },
    applyAddSelectedCustomer() {
      const c = this.findCustomerByKey(this.addSelectedCustomerKey);
      if (!c) return;
      this.addForm.customerName = c.name;
      this.addForm.phoneNumber = c.phoneNumber;
    },
    applyEditSelectedCustomer() {
      const c = this.findCustomerByKey(this.editSelectedCustomerKey);
      if (!c) return;
      this.editForm.customerName = c.name;
      this.editForm.phoneNumber = c.phoneNumber;
    },
    syncCustomerSelectionForEdit() {
      const match = this.reservationCustomers.find(
        (c) =>
          String(c.phoneNumber || "").trim() === String(this.editForm.phoneNumber || "").trim() &&
          String(c.name || "").trim() === String(this.editForm.customerName || "").trim()
      );
      if (match) {
        this.editCustomerMode = "existing";
        this.editSelectedCustomerKey = this.customerOptionKey(match);
      } else {
        this.editCustomerMode = "new";
        this.editSelectedCustomerKey = "";
      }
    },
    async saveReservationCustomer(name, phone) {
      if (!name || !phone) return;
      try {
        await HTTP.post("Reservations/customers", {
          name: name.trim(),
          phoneNumber: phone.trim(),
        });
        await this.loadReservationCustomers();
      } catch (e) {
        /* duplicate or permission — reservation still saved */
      }
    },
    async openAddReservationModal({ table } = {}) {
      if (!table) return;
      await this.loadReservationCustomers();
      const reservationDateTime = this.buildDefaultReservationDateTime();
      const tableId = table.id ?? table.Id;
      const zone = String(table.zone ?? table.Zone ?? "").trim();
      this.tableZoneFilter = zone;
      this.tableSearchQuery = "";
      this.addCustomerMode = this.reservationCustomers.length ? "existing" : "new";
      this.addSelectedCustomerKey = "";
      this.addCustomerSearch = "";
      this.addForm = {
        customerName: "",
        phoneNumber: "",
        reservationDateTime,
        numberOfGuests: 2,
        tableId,
        specialRequests: "",
        status: "Pending",
      };
      await this.loadTableAvailabilityForForm(reservationDateTime);
      if (this.isTableUnavailableForForm(tableId)) {
        this.$toast.warning(this.$t("tableHasReservation") || "الطاولة محجوزة في هذا الوقت", {
          position: "top-right",
          timeout: 4500,
        });
      }
      this.$bvModal.show("modal-addReservation");
    },
    clearTableFilter() {
      this.tableFilterId = null;
      this.onFilterChange();
    },
    getSummary() {
      const params = {};
      if (this.filterMode === "single" && this.singleDate) {
        params.fromDate = `${this.singleDate}T00:00:00`;
        params.toDate = `${this.singleDate}T23:59:59`;
      } else {
        if (this.fromDate) params.fromDate = `${this.fromDate}T00:00:00`;
        if (this.toDate) params.toDate = `${this.toDate}T23:59:59`;
      }
      HTTP.get("Reservations/summary", { params })
        .then((res) => {
          this.summaryStats = res?.data?.data || this.summaryStats;
        })
        .catch(() => {});
    },
    getTables() {
      HTTP.get("Tables", { 
        params: { 
          pageNumber: 0, 
          pageSize: 1000 // Get all tables for dropdown
        } 
      })
        .then((response) => {
          const pagedData = response.data.data;
          this.allTables = pagedData.items || [];
          this.availableTables = this.allTables; // Keep for backward compatibility
        })
        .catch(() => {
          // Ignore errors
        });
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
    getTableOptionLabel(table) {
      const base = `${table.tableNumber}${table.zone ? ` - ${table.zone}` : ""} (${this.$t("capacity") || "سعة"}: ${table.capacity}) - ${this.getTableStatusText(table.status)}`;
      const avail = this.tableAvailability[table.id];
      if (avail && avail.hasConflict) {
        const who = avail.customerName ? ` (${avail.customerName})` : "";
        return `${base} — ${this.$t("tableHasReservation") || "محجوزة"}${who}`;
      }
      if (avail && !avail.hasConflict) {
        return `${base} — ${this.$t("tableAvailableAtTime") || "متاحة في هذا الوقت"}`;
      }
      return base;
    },
    addReservation() {
      if (!this.addForm.tableId) {
        this.$toast.error(this.$t("selectTableFromFloorPlan") || "اختر الطاولة من مخطط الحجوزات", {
          position: "top-right",
          timeout: 4000,
        });
        return;
      }
      if (this.addCustomerMode === "existing") {
        if (!this.addSelectedCustomerKey) {
          this.$toast.error(this.$t("selectCustomer") || "اختر العميل", {
            position: "top-right",
            timeout: 4000,
          });
          return;
        }
        this.applyAddSelectedCustomer();
      }
      if (this.addForm.tableId && this.isTableUnavailableForForm(this.addForm.tableId)) {
        this.$toast.error(this.$t("tableHasReservation") || "الطاولة محجوزة في هذا الوقت", {
          position: "top-right",
          timeout: 4000,
        });
        return;
      }
      this.show = true;
      // Convert datetime-local to ISO format
      const formData = {
        customerName: this.addForm.customerName,
        phoneNumber: this.addForm.phoneNumber,
        reservationDateTime: new Date(this.addForm.reservationDateTime).toISOString(),
        numberOfGuests: this.addForm.numberOfGuests,
        status: this.addForm.status || "Pending"
      };
      
      // Only include tableId if it's not null
      if (this.addForm.tableId !== null && this.addForm.tableId !== undefined) {
        formData.tableId = this.addForm.tableId;
      }
      
      // Only include specialRequests if it's not empty
      if (this.addForm.specialRequests && this.addForm.specialRequests.trim() !== "") {
        formData.specialRequests = this.addForm.specialRequests.trim();
      }
      
      HTTP.post("Reservations", formData)
        .then(async (response) => {
          this.show = false;
          if (this.addCustomerMode === "new") {
            await this.saveReservationCustomer(this.addForm.customerName, this.addForm.phoneNumber);
          }
          this.$toast.success(this.$i18n.t("reservationAddedSuccessfully") || "تم إضافة الحجز بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.addForm = {
            customerName: "",
            phoneNumber: "",
            reservationDateTime: "",
            numberOfGuests: 2,
            tableId: null,
            specialRequests: "",
            status: "Pending"
          };
          this.tableZoneFilter = "";
          this.tableSearchQuery = "";
          this.tableAvailability = {};
          this.addSelectedCustomerKey = "";
          this.addCustomerSearch = "";
          this.$bvModal.hide("modal-addReservation");
          this.getReservations();
          this.getSummary();
          this.getTables();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    async editReservation(reservation) {
      await this.loadReservationCustomers();
      this.editForm = {
        id: reservation.id,
        customerName: reservation.customerName,
        phoneNumber: reservation.phoneNumber,
        reservationDateTime: new Date(reservation.reservationDateTime).toISOString().slice(0, 16),
        numberOfGuests: reservation.numberOfGuests,
        tableId: reservation.tableId,
        specialRequests: reservation.specialRequests || "",
        status: reservation.status
      };
      this.editCustomerSearch = "";
      this.syncCustomerSelectionForEdit();
      this.loadTableAvailabilityForForm(this.editForm.reservationDateTime, this.editForm.id);
      this.$bvModal.show("modal-editReservation");
    },
    updateReservation() {
      if (this.editCustomerMode === "existing") {
        if (!this.editSelectedCustomerKey) {
          this.$toast.error(this.$t("selectCustomer") || "اختر العميل", {
            position: "top-right",
            timeout: 4000,
          });
          return;
        }
        this.applyEditSelectedCustomer();
      }
      if (this.editForm.tableId && this.isTableUnavailableForForm(this.editForm.tableId)) {
        this.$toast.error(this.$t("tableHasReservation") || "الطاولة محجوزة في هذا الوقت", {
          position: "top-right",
          timeout: 4000,
        });
        return;
      }
      this.show = true;
      const formData = {
        ...this.editForm,
        reservationDateTime: new Date(this.editForm.reservationDateTime).toISOString()
      };
      
      HTTP.put(`Reservations/${this.editForm.id}`, formData)
        .then(async () => {
          this.show = false;
          if (this.editCustomerMode === "new") {
            await this.saveReservationCustomer(this.editForm.customerName, this.editForm.phoneNumber);
          }
          this.$toast.success(this.$i18n.t("reservationUpdatedSuccessfully") || "تم تحديث الحجز بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.$bvModal.hide("modal-editReservation");
          this.getReservations();
          this.getSummary();
          this.getTables();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    updateReservationStatus(reservation) {
      const newStatus = reservation.status === "Confirmed" ? "Cancelled" : "Confirmed";
      this.setReservationStatus(reservation, newStatus);
    },
    setReservationStatus(reservation, newStatus) {
      if (!reservation || !newStatus || reservation.status === newStatus) return;
      this.show = true;
      HTTP.put(`Reservations/${reservation.id}/status`, { status: newStatus })
        .then(() => {
          this.show = false;
          this.$toast.success(this.$i18n.t("reservationStatusUpdated") || "تم تحديث حالة الحجز بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.getReservations();
          this.getSummary();
          this.getTables();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    isStatusMenuOpen(reservationId) {
      return this.statusMenu.open && this.statusMenu.reservation && this.statusMenu.reservation.id === reservationId;
    },
    toggleStatusMenu(event, reservation) {
      if (this.isStatusMenuOpen(reservation.id)) {
        this.closeStatusMenu();
        return;
      }
      this.statusMenu.open = true;
      this.statusMenu.reservation = reservation;
      this.statusMenu.anchorEl = event.currentTarget;
      this.$nextTick(() => this.positionStatusMenu());
    },
    positionStatusMenu() {
      const anchor = this.statusMenu.anchorEl;
      const menu = this.$refs.statusMenuFlyout;
      if (!anchor || !menu) return;

      const rect = anchor.getBoundingClientRect();
      const menuWidth = menu.offsetWidth || 184;
      const menuHeight = menu.offsetHeight || 280;
      const gap = 8;
      const padding = 10;
      const isRtl = document.documentElement.dir === "rtl";

      let top = rect.bottom + gap;
      let left = isRtl ? rect.right - menuWidth : rect.left;

      if (top + menuHeight > window.innerHeight - padding) {
        top = rect.top - menuHeight - gap;
      }
      if (top < padding) {
        top = padding;
      }

      left = Math.max(padding, Math.min(left, window.innerWidth - menuWidth - padding));

      this.statusMenu.top = Math.round(top);
      this.statusMenu.left = Math.round(left);
    },
    closeStatusMenu() {
      this.statusMenu.open = false;
      this.statusMenu.reservation = null;
      this.statusMenu.anchorEl = null;
    },
    pickReservationStatus(newStatus) {
      const reservation = this.statusMenu.reservation;
      this.closeStatusMenu();
      if (reservation) {
        this.setReservationStatus(reservation, newStatus);
      }
    },
    async loadTableAvailabilityForForm(dateTimeLocal, excludeReservationId) {
      if (!dateTimeLocal) {
        this.tableAvailability = {};
        return;
      }
      try {
        const dt = new Date(dateTimeLocal);
        const date = dt.toISOString().split("T")[0];
        const time = `${String(dt.getHours()).padStart(2, "0")}:${String(dt.getMinutes()).padStart(2, "0")}:00`;
        const params = { date: `${date}T00:00:00`, time };
        if (excludeReservationId) {
          params.excludeReservationId = excludeReservationId;
        }
        const res = await HTTP.get("Reservations/availability", { params });
        const tables = res?.data?.data?.tables || [];
        const map = {};
        tables.forEach((row) => {
          map[row.tableId] = row;
        });
        this.tableAvailability = map;
      } catch (e) {
        this.tableAvailability = {};
      }
    },
    onAddFormDateTimeChange() {
      this.loadTableAvailabilityForForm(this.addForm.reservationDateTime);
    },
    onEditFormDateTimeChange() {
      this.loadTableAvailabilityForForm(this.editForm.reservationDateTime, this.editForm.id);
    },
    isTableUnavailableForForm(tableId) {
      const row = this.tableAvailability[tableId];
      return row && row.hasConflict;
    },
    formatDateTime(dateTime) {
      if (!dateTime) return "";
      const date = new Date(dateTime);
      return date.toLocaleString('ar-IQ', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    },
    getReservationRowClass(item, type) {
      if (!item || type !== 'row') return '';
      return `reservation-row-${this.getReservationStatusClass(item.status)}`;
    },
    getReservationStatusClass(status) {
      const statusClasses = {
        Pending: "reservation-pending",
        Confirmed: "reservation-confirmed",
        Seated: "reservation-seated",
        Completed: "reservation-completed",
        Cancelled: "reservation-cancelled"
      };
      return statusClasses[status] || "";
    },
    getStatusText(status) {
      const statusTexts = {
        Pending: this.$t("pending") || "قيد الانتظار",
        Confirmed: this.$t("confirmed") || "مؤكد",
        Seated: this.$t("seated") || "جلس",
        Completed: this.$t("completed") || "مكتمل",
        Cancelled: this.$t("cancelled") || "ملغي"
      };
      return statusTexts[status] || status;
    },
    onReservationTableZoneFilterChanged() {
      const ids = new Set(this.filteredTables.map((t) => t.id));
      if (this.addForm.tableId != null && !ids.has(this.addForm.tableId)) {
        this.addForm.tableId = null;
      }
      if (this.editForm.tableId != null && !ids.has(this.editForm.tableId)) {
        this.editForm.tableId = null;
      }
    },
    closeModel(modalId) {
      this.$bvModal.hide(modalId);
      if (modalId === "modal-addReservation" || modalId === "modal-editReservation") {
        this.tableSearchQuery = "";
        this.tableZoneFilter = "";
        this.tableAvailability = {};
        if (modalId === "modal-addReservation") {
          this.addForm.tableId = null;
          this.addSelectedCustomerKey = "";
          this.addCustomerSearch = "";
        }
        if (modalId === "modal-editReservation") {
          this.editSelectedCustomerKey = "";
          this.editCustomerSearch = "";
        }
      }
    }
  },
};
</script>

<style scoped>
/* Page */
.reservations-page .res-page-icon {
  background: linear-gradient(135deg, rgba(124, 58, 237, 0.18), rgba(99, 102, 241, 0.12));
  color: #7c3aed;
}

.app-overview-stat-icon--purple {
  background: rgba(124, 58, 237, 0.14);
  color: #7c3aed;
}

.res-stat-card {
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.res-stat-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.spinning {
  animation: res-spin 0.8s linear infinite;
}

@keyframes res-spin {
  to { transform: rotate(360deg); }
}

/* Toolbar */
.res-filters-toolbar {
  padding: 0.85rem 1.1rem;
}

.res-toolbar-main {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem 1rem;
  width: 100%;
}

.res-toolbar-fields {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  flex: 1 1 320px;
  min-width: 0;
}

.res-date-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
  flex-shrink: 0;
}

.res-floor-section {
  margin-bottom: 1.25rem;
  width: 100%;
}

.res-floor-body {
  padding: 0.85rem 1rem 1rem;
}

.res-floor-body :deep(.res-floor-canvas) {
  min-height: 360px;
}

@media (min-width: 992px) {
  .res-floor-body :deep(.res-floor-canvas) {
    min-height: 420px;
  }
}

.res-list-section {
  margin-bottom: 1.25rem;
  overflow: visible;
}

.reservations-page .res-list-section.app-section-card {
  overflow: visible;
}

.reservations-page .res-table-body {
  overflow: visible;
}

.reservations-page .report-table-container {
  overflow: visible;
}

.reservations-page .table-responsive {
  overflow: visible !important;
}

.res-count-inline {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.5rem;
  height: 1.5rem;
  margin-inline-start: 0.45rem;
  padding: 0 0.45rem;
  border-radius: 999px;
  background: rgba(124, 58, 237, 0.15);
  color: #a78bfa;
  font-size: 0.75rem;
  font-weight: 700;
  vertical-align: middle;
}

.res-count-badge {
  display: none;
}

.res-search-wrap {
  min-width: min(100%, 220px);
  flex: 1 1 200px;
}

.res-toolbar-select {
  min-height: 2.5rem;
  padding: 0.4rem 0.75rem;
  border-radius: 0.6rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.8125rem;
  font-weight: 600;
}

.res-date-chip {
  padding: 0.42rem 0.95rem;
  border-radius: 999px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-size: 0.8125rem;
  font-weight: 650;
  cursor: pointer;
  transition: all 0.15s ease;
}

.res-date-chip:hover {
  border-color: #a78bfa;
  color: #6d28d9;
}

.res-date-chip--active {
  background: linear-gradient(135deg, #a78bfa, #7c3aed);
  border-color: transparent;
  color: #fff;
  box-shadow: 0 2px 8px rgba(124, 58, 237, 0.35);
}

.res-advanced-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  min-height: 2.5rem;
  padding: 0.4rem 0.75rem;
  border-radius: 0.6rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  font-size: 0.8125rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}

.res-advanced-toggle:hover,
.res-advanced-toggle--open {
  border-color: var(--primary-color);
  color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 8%, var(--bg-primary));
}

.res-advanced-chevron {
  font-size: 0.75rem;
  opacity: 0.8;
}

.res-advanced-filters {
  padding: 1rem 1.1rem 1.15rem;
  border-top: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--bg-secondary) 88%, transparent);
}

.res-filter-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.85rem 1rem;
}

.res-filter-field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.res-filter-label {
  font-size: 0.75rem;
  font-weight: 650;
  color: var(--text-secondary);
}

.res-filter-input {
  width: 100%;
  min-height: 2.5rem;
  padding: 0.45rem 0.7rem;
  border-radius: 0.55rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.875rem;
}

.res-filter-input:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 18%, transparent);
}

/* Floor plan */
.app-section-icon-wrap--purple {
  background: linear-gradient(135deg, rgba(124, 58, 237, 0.16), rgba(109, 40, 217, 0.08));
  color: #7c3aed;
}

.res-clear-filter-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.4rem 0.75rem;
  border-radius: 0.55rem;
  border: 1px solid rgba(124, 58, 237, 0.35);
  background: rgba(124, 58, 237, 0.08);
  color: #6d28d9;
  font-size: 0.8125rem;
  font-weight: 600;
  cursor: pointer;
}

.res-clear-filter-btn:hover {
  background: rgba(124, 58, 237, 0.14);
}

/* Table */
.res-table-body {
  padding: 0;
  overflow: visible;
}

.reservations-page .report-table-container {
  overflow: visible;
}

.reservations-page .reservations-table thead th.res-th-actions {
  width: 6.5rem;
  min-width: 6.5rem;
  max-width: 6.5rem;
  text-align: center !important;
  vertical-align: middle;
  padding-inline: 0.5rem !important;
}

.reservations-page .reservations-table tbody td.res-td-actions {
  width: 6.5rem;
  min-width: 6.5rem;
  max-width: 6.5rem;
  text-align: center !important;
  vertical-align: middle !important;
  padding: 0.65rem 0.5rem !important;
}

.res-actions-head {
  display: block;
  width: 100%;
  text-align: center;
  font-size: 0.8125rem;
  white-space: nowrap;
}

.reservations-page .res-actions-cell {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  flex-wrap: nowrap;
  width: 100%;
}

.reservations-page .res-actions-cell .action-btn.res-status-dropdown {
  display: inline-flex !important;
  align-items: center;
  justify-content: center;
  padding: 0 !important;
  margin: 0 !important;
  line-height: 1 !important;
  width: 36px;
  min-width: 36px;
  height: 36px;
  border-radius: 0.6rem !important;
}

.res-status-dropdown--open {
  background: rgba(124, 58, 237, 0.18) !important;
  border-color: rgba(124, 58, 237, 0.45) !important;
  color: #a78bfa !important;
}

.reservations-page .reservations-table tbody tr:hover {
  transform: none;
}

.res-empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 3rem 1.5rem;
  gap: 0.5rem;
}

.res-empty-icon {
  font-size: 2.75rem;
  color: #a78bfa;
  opacity: 0.85;
  margin-bottom: 0.25rem;
}

.res-empty-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text-primary);
}

.res-empty-hint {
  margin: 0 0 0.75rem;
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.res-empty-btn {
  margin-top: 0.25rem;
}

.reservations-table {
  margin: 0;
}

/* Status */
.reservation-pending { --res-accent: #d97706; }
.reservation-confirmed { --res-accent: #7c3aed; }
.reservation-seated { --res-accent: #059669; }
.reservation-completed { --res-accent: #64748b; }
.reservation-cancelled { --res-accent: #dc2626; }

.reservation-row-reservation-pending,
.reservation-row-reservation-confirmed,
.reservation-row-reservation-seated,
.reservation-row-reservation-completed,
.reservation-row-reservation-cancelled {
  border-inline-start: 3px solid var(--res-accent, transparent);
}

.reservation-customer-cell,
.reservation-phone-cell,
.reservation-datetime-cell,
.reservation-guests-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 500;
  color: var(--text-primary);
}

.customer-icon,
.phone-icon,
.datetime-icon,
.guests-icon,
.table-icon {
  color: var(--text-muted);
  flex-shrink: 0;
}

.customer-name-text {
  font-weight: 650;
}

.reservation-table-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.2rem 0.55rem;
  border-radius: 0.45rem;
  background: color-mix(in srgb, #7c3aed 10%, var(--bg-secondary));
  color: #6d28d9;
  font-weight: 600;
  font-size: 0.8125rem;
}

.reservation-status-badge {
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 650;
  display: inline-block;
}

.reservation-status-badge.reservation-pending {
  background-color: var(--warning-light);
  color: var(--warning-color);
}

.reservation-status-badge.reservation-confirmed {
  background-color: rgba(124, 58, 237, 0.12);
  color: #6d28d9;
}

.reservation-status-badge.reservation-seated {
  background-color: var(--success-light);
  color: var(--success-color);
}

.reservation-status-badge.reservation-completed {
  background-color: rgba(100, 116, 139, 0.15);
  color: #64748b;
}

.reservation-status-badge.reservation-cancelled {
  background-color: var(--danger-light);
  color: var(--danger-color);
}

.actions-cell {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
}

.res-status-dropdown {
  text-decoration: none !important;
  box-shadow: none !important;
  background-image: none !important;
}

.res-status-dropdown:focus,
.res-status-dropdown:active {
  box-shadow: none !important;
  outline: none !important;
}

.action-btn--status {
  background: rgba(124, 58, 237, 0.12);
  color: #7c3aed;
  border-color: rgba(124, 58, 237, 0.28);
}

.action-btn--status:hover,
.action-btn--status:focus {
  background: rgba(124, 58, 237, 0.2) !important;
  color: #6d28d9 !important;
  border-color: rgba(124, 58, 237, 0.4) !important;
}

.pagination-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  border-top: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.pagination-info {
  color: var(--text-muted);
  font-size: 0.8125rem;
}

.reservations-pagination >>> .page-link {
  color: var(--text-primary);
  border-color: var(--border-color);
  background-color: var(--bg-primary);
}

.reservations-pagination >>> .page-item.active .page-link {
  background: linear-gradient(135deg, #a78bfa, #7c3aed);
  border-color: #7c3aed;
  color: #fff;
}

.text-muted {
  color: var(--text-muted);
  font-style: italic;
}

@media (max-width: 768px) {
  .res-toolbar-fields {
    margin-inline-start: 0;
    width: 100%;
  }

  .res-search-wrap {
    flex: 1 1 100%;
  }

  .res-toolbar-select,
  .res-advanced-toggle {
    flex: 1 1 auto;
  }
}

/* Modal table picker */
.res-customer-section {
  padding: 0.85rem 1rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--bg-secondary) 92%, var(--border-color) 8%);
}

.res-customer-mode {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
  margin: 0.5rem 0 0.85rem;
}

.res-customer-mode-label {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.45rem 0.85rem;
  border-radius: 0.55rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--text-secondary);
  cursor: pointer;
  transition: border-color 0.15s ease, color 0.15s ease, background 0.15s ease;
}

.res-customer-mode-label:has(.res-customer-mode-input:checked) {
  border-color: #a78bfa;
  color: #7c3aed;
  background: rgba(124, 58, 237, 0.08);
}

.res-customer-mode-input {
  accent-color: #7c3aed;
}

.res-customer-existing {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.res-customer-search {
  margin: 0;
}

.res-customer-new {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.75rem 1rem;
}

.res-customer-preview {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.65rem;
  border-radius: 0.5rem;
  background: rgba(124, 58, 237, 0.1);
  color: #6d28d9;
  font-size: 0.8125rem;
  font-weight: 600;
}

.res-selected-table-display {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.65rem 1rem;
  border-radius: 0.65rem;
  border: 1px solid rgba(124, 58, 237, 0.35);
  background: rgba(124, 58, 237, 0.1);
  color: #6d28d9;
  font-size: 0.9375rem;
  font-weight: 700;
}

.res-selected-table-icon {
  font-size: 1.1rem;
  flex-shrink: 0;
}

.res-table-floor-hint {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  margin: 0;
  padding: 0.65rem 0.85rem;
  border-radius: 0.55rem;
  border: 1px dashed var(--border-color);
  background: var(--bg-secondary);
  color: var(--text-secondary);
  font-size: 0.8125rem;
}

/* Modal table picker */
.reservation-table-picker {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  width: 100%;
  box-sizing: border-box;
  padding: 1rem 1.1rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--bg-secondary) 92%, var(--border-color) 8%);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
}

.users-form-sublabel {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.8125rem;
  font-weight: 650;
  color: var(--text-secondary);
  margin: 0;
  letter-spacing: 0.01em;
}

.users-form-sublabel .form-label-icon {
  font-size: 0.9rem;
  opacity: 0.9;
}

.reservation-zone-select {
  margin: 0 !important;
}

/* Table Search */
.table-search-wrapper {
  margin: 0;
  width: 100%;
}

.table-search-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.table-search-icon {
  position: absolute;
  inset-inline-end: 0.875rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  font-size: 1.05rem;
  pointer-events: none;
  z-index: 2;
  opacity: 0.88;
}

.table-search-input {
  width: 100%;
  box-sizing: border-box;
  min-height: 2.875rem;
  padding-block: 0.65rem;
  padding-inline: 1rem 2.75rem;
  border: 2px solid var(--border-color);
  border-radius: 0.65rem;
  font-size: 0.9375rem;
  line-height: 1.35;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.table-search-input::placeholder {
  color: var(--text-muted);
  opacity: 0.85;
}

.table-search-input:hover {
  border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
}

.table-search-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 22%, transparent);
  outline: none;
  background: var(--bg-primary);
}

.reservation-table-select {
  margin: 0 !important;
  width: 100%;
  min-height: 11.5rem;
  max-height: min(42vh, 280px);
  padding: 0.4rem;
  border-radius: 0.65rem;
  border: 2px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  line-height: 1.5;
  font-size: 0.875rem;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.reservation-table-select:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 18%, transparent);
  outline: none;
}

.users-form-input[multiple] {
  min-height: 200px;
  overflow-y: auto;
  padding: 0.5rem;
}

.users-form-input[multiple] option {
  padding: 0.5rem;
  margin-bottom: 0.25rem;
  border-radius: 0.25rem;
  cursor: pointer;
}

.users-form-input[multiple] option:hover {
  background-color: var(--primary-color);
  color: white;
}

.users-form-input[multiple] option:checked {
  background-color: var(--primary-color);
  color: white;
}

@media (max-width: 576px) {
  .reservation-table-picker {
    padding: 0.85rem;
    gap: 0.55rem;
  }

  .reservation-table-select {
    max-height: min(38vh, 220px);
    font-size: 0.8125rem;
  }

  .table-search-input {
    min-height: 2.75rem;
    font-size: 0.875rem;
  }
}
</style>

<style>
/* Status flyout — fixed on viewport, never clipped by table */
.res-status-menu-backdrop {
  position: fixed;
  inset: 0;
  z-index: 10055;
  background: transparent;
}

.res-status-menu-flyout {
  position: fixed !important;
  z-index: 10060 !important;
  margin: 0 !important;
  transform: none !important;
  pointer-events: auto;
}

.res-status-menu.dropdown-menu,
.res-status-menu.res-status-menu-flyout {
  border-radius: 0.85rem !important;
  border: 1px solid rgba(124, 58, 237, 0.22) !important;
  background: var(--bg-primary, #0f172a) !important;
  box-shadow:
    0 4px 6px rgba(15, 23, 42, 0.08),
    0 18px 40px rgba(15, 23, 42, 0.28) !important;
  padding: 0.45rem !important;
  min-width: 11.5rem;
}

.res-status-menu-header {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.6875rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #a78bfa !important;
  padding: 0.4rem 0.55rem 0.5rem;
  background: transparent !important;
  border-bottom: 1px solid rgba(148, 163, 184, 0.14);
  margin-bottom: 0.25rem;
}

.res-status-menu-divider {
  margin: 0.35rem 0.45rem;
  border: 0;
  border-top: 1px solid var(--border-color, #334155);
  opacity: 1;
}

.res-status-menu .res-status-option {
  display: flex !important;
  align-items: center;
  gap: 0.55rem;
  width: 100%;
  border: none;
  border-radius: 0.55rem;
  padding: 0.55rem 0.72rem !important;
  margin: 0.12rem 0;
  font-size: 0.8125rem !important;
  font-weight: 650;
  color: var(--text-primary, #e2e8f0) !important;
  background: transparent !important;
  border-inline-start: 3px solid transparent;
  transition: background 0.12s ease, border-color 0.12s ease, color 0.12s ease;
  text-align: start;
  cursor: pointer;
}

.res-status-menu .res-status-option span {
  color: inherit !important;
  flex: 1;
}

.res-status-menu .res-status-option .b-icon {
  font-size: 1rem;
  flex-shrink: 0;
  width: 1.1rem;
  text-align: center;
}

.res-status-menu .res-status-option:hover,
.res-status-menu .res-status-option:focus {
  background: var(--bg-secondary, #1e293b) !important;
  color: var(--text-primary, #f8fafc) !important;
  outline: none;
}

.res-status-menu .res-status-option.active {
  font-weight: 800;
}

.res-status-menu .res-status-option--pending {
  border-inline-start-color: #f59e0b;
}
.res-status-menu .res-status-option--pending .b-icon { color: #f59e0b !important; }
.res-status-menu .res-status-option--pending.active {
  background: rgba(245, 158, 11, 0.14) !important;
  color: #fcd34d !important;
}

.res-status-menu .res-status-option--confirmed {
  border-inline-start-color: #a78bfa;
}
.res-status-menu .res-status-option--confirmed .b-icon { color: #a78bfa !important; }
.res-status-menu .res-status-option--confirmed.active {
  background: rgba(124, 58, 237, 0.2) !important;
  color: #ddd6fe !important;
}

.res-status-menu .res-status-option--seated {
  border-inline-start-color: #34d399;
}
.res-status-menu .res-status-option--seated .b-icon { color: #34d399 !important; }
.res-status-menu .res-status-option--seated.active {
  background: rgba(52, 211, 153, 0.14) !important;
  color: #a7f3d0 !important;
}

.res-status-menu .res-status-option--completed {
  border-inline-start-color: #94a3b8;
}
.res-status-menu .res-status-option--completed .b-icon { color: #94a3b8 !important; }
.res-status-menu .res-status-option--completed.active {
  background: rgba(148, 163, 184, 0.14) !important;
  color: #e2e8f0 !important;
}

.res-status-menu .res-status-option--cancelled {
  border-inline-start-color: #f87171;
}
.res-status-menu .res-status-option--cancelled .b-icon { color: #f87171 !important; }
.res-status-menu .res-status-option--cancelled:hover,
.res-status-menu .res-status-option--cancelled:focus {
  background: rgba(220, 38, 38, 0.12) !important;
  color: #fecaca !important;
}
.res-status-menu .res-status-option--cancelled.active {
  background: rgba(220, 38, 38, 0.2) !important;
  color: #fecaca !important;
}

/* Light mode */
:root.light-theme .res-status-menu.dropdown-menu,
:root.light-theme .res-status-menu.res-status-menu-flyout {
  border-color: rgba(124, 58, 237, 0.16) !important;
  background: #ffffff !important;
  box-shadow:
    0 4px 6px rgba(15, 23, 42, 0.04),
    0 16px 36px rgba(124, 58, 237, 0.12) !important;
}

:root.light-theme .res-status-menu-header {
  color: #6d28d9 !important;
  border-bottom-color: rgba(124, 58, 237, 0.1);
}

:root.light-theme .res-status-menu .res-status-option {
  color: #0f172a !important;
}

:root.light-theme .res-status-menu .res-status-option:hover,
:root.light-theme .res-status-menu .res-status-option:focus {
  background: #f8fafc !important;
  color: #0f172a !important;
}

:root.light-theme .res-status-menu .res-status-option--pending.active {
  background: rgba(245, 158, 11, 0.12) !important;
  color: #b45309 !important;
}

:root.light-theme .res-status-menu .res-status-option--confirmed.active {
  background: rgba(124, 58, 237, 0.1) !important;
  color: #5b21b6 !important;
}

:root.light-theme .res-status-menu .res-status-option--seated.active {
  background: rgba(52, 211, 153, 0.12) !important;
  color: #047857 !important;
}

:root.light-theme .res-status-menu .res-status-option--completed.active {
  background: rgba(100, 116, 139, 0.1) !important;
  color: #334155 !important;
}

:root.light-theme .res-status-menu .res-status-option--cancelled:hover,
:root.light-theme .res-status-menu .res-status-option--cancelled:focus {
  background: rgba(220, 38, 38, 0.08) !important;
  color: #b91c1c !important;
}

:root.light-theme .res-status-menu .res-status-option--cancelled.active {
  background: rgba(220, 38, 38, 0.1) !important;
  color: #b91c1c !important;
}
</style>

