<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <b-overlay
        :show="show"
        spinner-variant="primary"
        spinner-type="border"
        rounded="sm"
      >
        <div class="app-page-container">
          <div class="app-page-content dashboard-page">
            <div class="users-header-section">
              <div class="users-header-content app-header-row">
                <div class="header-title-wrapper">
                  <div class="header-icon-wrapper">
                    <b-icon icon="speedometer2" class="header-icon"></b-icon>
                  </div>
                  <div>
                    <h1 class="users-page-title">{{ $t("welcomeToDashboard") || "لوحة التحكم" }}</h1>
                    <p class="header-subtitle">{{ $t("dashboardSubtitle") || "نظرة شاملة على إحصائيات متجرك" }}</p>
                  </div>
                </div>
                <div class="app-header-actions">
                  <button type="button" class="btn-refresh" @click="refreshPage" :disabled="show">
                    <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                    <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                  </button>
                  <router-link to="/sections" class="users-add-button dashboard-sections-link">
                    <b-icon icon="grid-3x3-gap-fill" class="button-icon"></b-icon>
                    <span class="button-text">{{ $t("systemModules") }}</span>
                  </router-link>
                </div>
              </div>
            </div>

            <div class="app-overview-grid">
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                  <b-icon icon="receipt-cutoff"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ stats.orders?.total || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("all_sales") }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--success">
                  <b-icon icon="currency-dollar"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value app-overview-stat-value--text">
                    {{ formattedNumber(stats.salesAmount?.total || 0) }} {{ $t("currency") }}
                  </div>
                  <div class="app-overview-stat-label">{{ $t("salesAmountStatisticsLabel") }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--info">
                  <b-icon icon="box-fill"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ stats.products?.total || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("Items") }}</div>
                </div>
              </div>
              <div class="app-overview-stat">
                <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                  <b-icon icon="people-fill"></b-icon>
                </span>
                <div>
                  <div class="app-overview-stat-value">{{ stats.users?.total || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("all_accounts") }}</div>
                </div>
              </div>
            </div>

            <div v-if="role === 'Commercial'" class="app-section-card dashboard-links-card">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap">
                    <b-icon icon="link-45deg"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("dashboardQuickLinks") || "روابط سريعة" }}</h3>
                    <p class="app-section-subtitle">{{ $t("dashboardQuickLinksHint") || "مشاركة المنيو وشاشة الطلبات" }}</p>
                  </div>
                </div>
              </div>
              <div class="app-section-body dashboard-links-body">
                <div class="dashboard-link-block">
                  <div class="dashboard-link-block-head">
                    <span class="dashboard-link-block-icon dashboard-link-block-icon--menu">
                      <img
                        v-if="commercialUserInfo.logo && !logoError"
                        :src="commercialUserInfo.logo"
                        alt=""
                        class="dashboard-link-logo"
                        @error="logoError = true"
                      />
                      <b-icon v-else icon="shop"></b-icon>
                    </span>
                    <div class="dashboard-link-block-text">
                      <strong>{{ commercialUserInfo.restaurantName || $t("publicMenu") || "القائمة العامة" }}</strong>
                      <span>{{ $t("publicMenuDescription") || "شارك رابط المنيو مع العملاء" }}</span>
                    </div>
                  </div>
                  <div class="dashboard-link-actions">
                    <input
                      type="text"
                      :value="publicMenuUrl"
                      readonly
                      class="dashboard-link-input"
                      :id="'publicMenuLink-' + commercialUserId"
                    />
                    <button type="button" class="btn-refresh dashboard-link-btn" @click="copyPublicMenuLink">
                      <b-icon icon="clipboard" class="button-icon"></b-icon>
                      <span class="button-text">{{ $t("copyLink") || "نسخ" }}</span>
                    </button>
                    <a :href="publicMenuUrl" target="_blank" rel="noopener" class="users-form-cancel-button dashboard-link-btn dashboard-link-btn--open">
                      <b-icon icon="box-arrow-up-right"></b-icon>
                      {{ $t("open") || "فتح" }}
                    </a>
                  </div>
                </div>
                <div class="dashboard-link-block">
                  <div class="dashboard-link-block-head">
                    <span class="dashboard-link-block-icon dashboard-link-block-icon--queue">
                      <b-icon icon="display"></b-icon>
                    </span>
                    <div class="dashboard-link-block-text">
                      <strong>{{ $t("publicQueueDisplay") || "شاشة عرض الطلبات" }}</strong>
                      <span>{{ $t("publicQueueDisplayDescription") || "رابط العرض على الشاشة الكبيرة" }}</span>
                    </div>
                  </div>
                  <div class="dashboard-link-actions">
                    <input
                      type="text"
                      :value="publicQueueDisplayUrl"
                      readonly
                      class="dashboard-link-input"
                      :id="'publicQueueDisplayLink-' + commercialUserId"
                    />
                    <button type="button" class="btn-refresh dashboard-link-btn" @click="copyPublicQueueDisplayLink">
                      <b-icon icon="clipboard" class="button-icon"></b-icon>
                      <span class="button-text">{{ $t("copyLink") || "نسخ" }}</span>
                    </button>
                    <a :href="publicQueueDisplayUrl" target="_blank" rel="noopener" class="users-form-cancel-button dashboard-link-btn dashboard-link-btn--open">
                      <b-icon icon="box-arrow-up-right"></b-icon>
                      {{ $t("open") || "فتح" }}
                    </a>
                  </div>
                </div>
              </div>
            </div>

            <div class="app-section-card dashboard-period-card">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap">
                    <b-icon icon="graph-up"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("dashboardPeriodStats") || "إحصائيات حسب الفترة" }}</h3>
                    <p class="app-section-subtitle">{{ $t("dashboardPeriodStatsHint") || "اليوم، الأسبوع، والشهر" }}</p>
                  </div>
                </div>
              </div>
              <div class="app-section-body dashboard-period-body">
                <div
                  v-for="group in periodStatGroups"
                  :key="group.key"
                  class="dashboard-stat-group"
                >
                  <h4 class="dashboard-stat-group-title">
                    <b-icon :icon="group.headerIcon"></b-icon>
                    {{ group.title }}
                  </h4>
                  <div class="app-overview-grid dashboard-period-grid">
                    <div
                      v-for="(item, idx) in group.items"
                      :key="group.key + '-' + idx"
                      class="app-overview-stat"
                    >
                      <span class="app-overview-stat-icon" :class="'app-overview-stat-icon--' + item.tone">
                        <b-icon :icon="item.icon"></b-icon>
                      </span>
                      <div>
                        <div
                          class="app-overview-stat-value"
                          :class="{ 'app-overview-stat-value--text': item.isText }"
                        >
                          {{ item.value }}
                        </div>
                        <div class="app-overview-stat-label">{{ item.label }}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="app-section-card dashboard-invoices-card">
              <div class="app-section-header app-section-header--toolbar">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap">
                    <b-icon icon="receipt-cutoff"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t("invoiceListTitle") || "قائمة الفواتير" }}</h3>
                    <p class="app-section-subtitle">{{ $t("invoiceListHint") || "بحث وعرض تفاصيل الفواتير" }}</p>
                  </div>
                </div>
                <button
                  type="button"
                  class="users-form-cancel-button section-view-details-btn"
                  @click="showInvoiceDetails = !showInvoiceDetails"
                >
                  <b-icon :icon="showInvoiceDetails ? 'chevron-up' : 'chevron-down'"></b-icon>
                  {{ showInvoiceDetails ? ($t("hideDetails") || "إخفاء") : ($t("viewDetails") || "عرض الفواتير") }}
                </button>
              </div>
              <div v-if="showInvoiceDetails" class="app-section-body dashboard-invoices-body">
              <div class="invoice-details-section">
                <!-- Date Filter -->
                <div class="invoice-filters-section">
                  <div class="invoice-filter-group">
                    <label class="invoice-filter-label">
                      <b-icon icon="calendar" class="me-2"></b-icon>
                      {{ $t("from_date") || "من تاريخ" }}
                    </label>
                    <input 
                      v-model="invoiceFilters.startDate" 
                      type="date" 
                      class="invoice-filter-input"
                      @change="loadInvoices"
                    />
                  </div>
                  <div class="invoice-filter-group">
                    <label class="invoice-filter-label">
                      <b-icon icon="calendar-check" class="me-2"></b-icon>
                      {{ $t("to_date") || "إلى تاريخ" }}
                    </label>
                    <input 
                      v-model="invoiceFilters.endDate" 
                      type="date" 
                      class="invoice-filter-input"
                      @change="loadInvoices"
                    />
                  </div>
                  <div class="invoice-filter-group">
                    <label class="invoice-filter-label">
                      <b-icon icon="search" class="me-2"></b-icon>
                      {{ $t("search") || "بحث" }}
                    </label>
                    <input 
                      v-model="invoiceFilters.search" 
                      type="text" 
                      class="invoice-filter-input"
                      :placeholder="$t('searchByOrderCode') || 'ابحث برقم الطلب'"
                      @input="debounceInvoiceSearch"
                    />
                  </div>
                  <div class="invoice-filter-group">
                    <button 
                      class="invoice-filter-clear-btn"
                      @click="clearInvoiceFilters"
                    >
                      <b-icon icon="x-circle" class="me-2"></b-icon>
                      {{ $t("clear") || "مسح" }}
                    </button>
                  </div>
                </div>

                <!-- Invoices Table -->
                <div class="invoice-table-section">
                  <div v-if="loadingInvoices" class="loading-state">
                    <b-spinner small></b-spinner>
                    <span>{{ $t("loading") || "جاري التحميل..." }}</span>
                  </div>
                  <div v-else-if="invoices.length > 0" class="invoice-table-wrapper">
                    <table class="invoice-table">
                      <thead>
                        <tr>
                          <th>{{ $t("orderCode") || "رقم الطلب" }}</th>
                          <th>{{ $t("date") || "التاريخ" }}</th>
                          <th>{{ $t("orderType") || "نوع الطلب" }}</th>
                          <th>{{ $t("paymentMethod") || "طريقة الدفع" }}</th>
                          <th>{{ $t("total") || "المجموع" }}</th>
                          <th>{{ $t("status") || "الحالة" }}</th>
                          <th>{{ $t("actions") || "الإجراءات" }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="invoice in invoices" :key="invoice.id">
                          <td>{{ invoice.orderCode || '-' }}</td>
                          <td>{{ formatDate(invoice.createdAt || invoice.insertDate) }}</td>
                          <td>
                            <span class="order-type-badge" :class="getOrderTypeClass(invoice.orderType)">
                              {{ getOrderTypeText(invoice.orderType) }}
                            </span>
                          </td>
                          <td>{{ getPaymentMethodText(invoice.paymentMethod) }}</td>
                          <td>{{ formatPrice(invoice.orderTotalAfterDiscount ?? invoice.total ?? invoice.orderPrice ?? 0) }} {{ $t("currency") || "د.ع" }}</td>
                          <td>
                            <span class="status-badge" :class="getStatusClass(invoice.orderStatus || 'Pending')">
                              {{ getStatusText(invoice.orderStatus || 'Pending') }}
                            </span>
                          </td>
                          <td>
                            <button 
                              class="invoice-action-btn"
                              @click="viewInvoiceDetails(invoice)"
                              :title="$t('viewDetails') || 'عرض التفاصيل'"
                            >
                              <b-icon icon="eye"></b-icon>
                            </button>
                          </td>
                        </tr>
                      </tbody>
                    </table>

                    <!-- Pagination -->
                    <div class="invoice-pagination">
                      <button 
                        class="pagination-btn"
                        @click="previousInvoicePage"
                        :disabled="invoicePageNumber === 1"
                      >
                        <b-icon icon="chevron-right"></b-icon>
                        {{ $t("previous") || "السابق" }}
                      </button>
                      <span class="pagination-info">
                        {{ $t("page") || "صفحة" }} {{ invoicePageNumber }} {{ $t("of") || "من" }} {{ totalInvoicePages }}
                      </span>
                      <button 
                        class="pagination-btn"
                        @click="nextInvoicePage"
                        :disabled="invoicePageNumber >= totalInvoicePages"
                      >
                        {{ $t("next") || "التالي" }}
                        <b-icon icon="chevron-left"></b-icon>
                      </button>
                    </div>
                  </div>
                  <div v-else class="empty-invoices-state">
                    <b-icon icon="receipt" class="empty-icon"></b-icon>
                    <p>{{ $t("noInvoicesFound") || "لا توجد فواتير" }}</p>
                  </div>
                </div>
              </div>
              </div>
            </div>
          </div>
        </div>
      </b-overlay>
    </div>

    <!-- Invoice Details Modal -->
    <b-modal 
      v-model="showInvoiceModal" 
      :title="$t('invoiceDetails') || 'تفاصيل الفاتورة'" 
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
      @hidden="selectedInvoice = null"
    >
      <div class="modal-content-wrapper" v-if="selectedInvoice">
        <h2 class="modal-title">{{ $t("invoiceDetails") || "تفاصيل الفاتورة" }}</h2>
        <div class="invoice-details-content">
          <div class="invoice-details-grid">
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("orderCode") || "رقم الطلب" }}</label>
              <span class="invoice-detail-value">{{ selectedInvoice.orderCode || '-' }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("date") || "التاريخ" }}</label>
              <span class="invoice-detail-value">{{ formatDate(selectedInvoice.createdAt || selectedInvoice.insertDate) }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("orderType") || "نوع الطلب" }}</label>
              <span class="invoice-detail-value">
                <span class="order-type-badge" :class="getOrderTypeClass(selectedInvoice.orderType)">
                  {{ getOrderTypeText(selectedInvoice.orderType) }}
                </span>
              </span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("paymentMethod") || "طريقة الدفع" }}</label>
              <span class="invoice-detail-value">{{ getPaymentMethodText(selectedInvoice.paymentMethod) }}</span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("status") || "الحالة" }}</label>
              <span class="invoice-detail-value">
                <span class="status-badge" :class="getStatusClass(selectedInvoice.orderStatus)">
                  {{ getStatusText(selectedInvoice.orderStatus) }}
                </span>
              </span>
            </div>
            <div class="invoice-detail-item">
              <label class="invoice-detail-label">{{ $t("total") || "المجموع" }}</label>
              <span class="invoice-detail-value invoice-total">{{ formatPrice(selectedInvoice.orderTotalAfterDiscount ?? selectedInvoice.total ?? selectedInvoice.orderPrice ?? 0) }} {{ $t("currency") || "د.ع" }}</span>
            </div>
            <div class="invoice-detail-item" v-if="Number(selectedInvoice.discountAmount || 0) > 0">
              <label class="invoice-detail-label">{{ $t("discountLabel") || "الخصم" }}</label>
              <span class="invoice-detail-value">- {{ formatPrice(selectedInvoice.discountAmount || 0) }} {{ $t("currency") || "د.ع" }}</span>
            </div>
          </div>

          <!-- Order Items -->
          <div v-if="selectedInvoice.customerOrderItem && selectedInvoice.customerOrderItem.length > 0" class="invoice-items-section">
            <h3 class="invoice-items-title">{{ $t("orderItems") || "عناصر الطلب" }}</h3>
            <table class="invoice-items-table">
              <thead>
                <tr>
                  <th>{{ $t("itemName") || "اسم المنتج" }}</th>
                  <th>{{ $t("quantity") || "الكمية" }}</th>
                  <th>{{ $t("price") || "السعر" }}</th>
                  <th>{{ $t("total") || "المجموع" }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in selectedInvoice.customerOrderItem" :key="index">
                  <td>{{ item.item?.name || '-' }}</td>
                  <td>{{ item.quantity || 0 }}</td>
                  <td>{{ formatPrice(item.sellingPrice || 0) }} {{ $t("currency") || "د.ع" }}</td>
                  <td>{{ formatPrice((item.sellingPrice || 0) * (item.quantity || 0)) }} {{ $t("currency") || "د.ع" }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Delivery Info (if applicable) -->
          <div v-if="selectedInvoice.orderType === 'Delivery'" class="invoice-delivery-info">
            <h3 class="invoice-delivery-title">{{ $t("deliveryInformation") || "معلومات التوصيل" }}</h3>
            
            <!-- Customer Information -->
            <div class="invoice-delivery-section">
              <h4 class="invoice-delivery-subtitle">{{ $t("customerInformation") || "معلومات العميل" }}</h4>
              <div class="invoice-delivery-grid">
                <div v-if="selectedInvoice.deliveryCustomerName" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("customerName") || "اسم العميل" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryCustomerName }}</span>
                </div>
                <div v-if="selectedInvoice.deliveryPhoneNumber" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("phoneNumber") || "رقم الهاتف" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryPhoneNumber }}</span>
                </div>
                <div v-if="selectedInvoice.deliveryAddress" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("address") || "العنوان" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryAddress }}</span>
                </div>
              </div>
            </div>

            <!-- Delivery Driver Information -->
            <div v-if="selectedInvoice.deliveryDriver" class="invoice-delivery-section">
              <h4 class="invoice-delivery-subtitle">{{ $t("deliveryDriverInformation") || "معلومات سائق التوصيل" }}</h4>
              <div class="invoice-delivery-grid">
                <div v-if="selectedInvoice.deliveryDriver.name" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("driverName") || "اسم السائق" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryDriver.name }}</span>
                </div>
                <div v-if="selectedInvoice.deliveryDriver.phoneNumber" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("driverPhoneNumber") || "رقم هاتف السائق" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryDriver.phoneNumber }}</span>
                </div>
                <div v-if="selectedInvoice.deliveryDriver.vehicleType" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("vehicleType") || "نوع المركبة" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryDriver.vehicleType }}</span>
                </div>
                <div v-if="selectedInvoice.deliveryDriver.vehicleNumber" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("vehicleNumber") || "رقم المركبة" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryDriver.vehicleNumber }}</span>
                </div>
                <div v-if="selectedInvoice.deliveryDriver.address" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("driverAddress") || "عنوان السائق" }}</label>
                  <span class="invoice-detail-value">{{ selectedInvoice.deliveryDriver.address }}</span>
                </div>
              </div>
            </div>

            <!-- Delivery Status -->
            <div class="invoice-delivery-section">
              <h4 class="invoice-delivery-subtitle">{{ $t("deliveryStatus") || "حالة التوصيل" }}</h4>
              <div class="invoice-delivery-grid">
                <div class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("status") || "الحالة" }}</label>
                  <span class="invoice-detail-value">
                    <span class="delivery-status-badge" :class="getDeliveryStatusClass(selectedInvoice.deliveryStatus)">
                      {{ getDeliveryStatusText(selectedInvoice.deliveryStatus) }}
                    </span>
                  </span>
                </div>
                <div v-if="selectedInvoice.deliveryFee" class="invoice-detail-item">
                  <label class="invoice-detail-label">{{ $t("deliveryFee") || "رسوم التوصيل" }}</label>
                  <span class="invoice-detail-value">{{ formatPrice(selectedInvoice.deliveryFee) }} {{ $t("currency") || "د.ع" }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Notes -->
          <div v-if="selectedInvoice.notes" class="invoice-notes-section">
            <label class="invoice-detail-label">{{ $t("notes") || "ملاحظات" }}</label>
            <p class="invoice-notes-text">{{ selectedInvoice.notes }}</p>
          </div>
        </div>
        <div class="users-form-actions">
          <button type="button" class="users-form-cancel-button" @click="showInvoiceModal = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../http/api.js";
import { formatBusinessDateTime } from "@/utils/formatBusinessDateTime.js";
export default {
  name: "DashboardView",
  components: {
    AppHeader,
  },
  data() {
    return {
      stats: {
        orders: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        items: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        salesAmount: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        products: {
          total: 0,
          active: 0,
        },
        users: {
          total: 0,
          active: 0,
        },
        categories: {
          total: 0,
          active: 0,
        },
      },
      show: false,
      commercialUserId: null,
      commercialUserInfo: {
        restaurantName: '',
        logo: null
      },
      logoError: false,
      showInvoiceDetails: false,
      loadingInvoices: false,
      invoices: [],
      totalInvoices: 0,
      invoicePageNumber: 1,
      invoicePageSize: 10,
      invoiceFilters: {
        startDate: '',
        endDate: '',
        search: ''
      },
      invoiceSearchTimer: null,
      selectedInvoice: null,
      showInvoiceModal: false,
    };
  },
  computed: {
    role() {
      return localStorage.getItem("role");
    },
    publicMenuUrl() {
      if (!this.commercialUserId) return '';
      const baseUrl = window.location.origin;
      return `${baseUrl}/menu/${this.commercialUserId}`;
    },
    publicQueueDisplayUrl() {
      if (!this.commercialUserId) return '';
      const baseUrl = window.location.origin;
      return `${baseUrl}/public-queue/${this.commercialUserId}`;
    },
    totalInvoicePages() {
      return Math.ceil(this.totalInvoices / this.invoicePageSize);
    },
    periodStatGroups() {
      const currency = this.$t("currency");
      const periodLabels = [
        { key: "total", label: this.$t("totalLabel"), tone: "primary" },
        { key: "today", label: this.$t("todayLabel"), tone: "danger" },
        { key: "week", label: this.$t("thisWeekLabel"), tone: "success" },
        { key: "month", label: this.$t("thisMonthLabel"), tone: "info" },
      ];
      const periodIcons = {
        total: "receipt-cutoff",
        today: "calendar-day",
        week: "calendar-week",
        month: "calendar-month",
      };
      const buildGroup = (key, title, headerIcon, source, formatValue) => ({
        key,
        title,
        headerIcon,
        items: periodLabels.map((p) => ({
          label: p.label,
          tone: p.tone,
          icon: key === "sales" && p.key === "total" ? "currency-dollar" : periodIcons[p.key],
          isText: key === "sales",
          value: formatValue(source, p.key),
        })),
      });
      return [
        buildGroup(
          "orders",
          this.$t("invoiceStatisticsLabel"),
          "receipt-cutoff",
          this.stats.orders,
          (src, p) => {
            const map = { total: src?.total, today: src?.today, week: src?.thisWeek, month: src?.thisMonth };
            return map[p] ?? 0;
          }
        ),
        buildGroup(
          "items",
          this.$t("itemsStatisticsLabel"),
          "box-fill",
          this.stats.items,
          (src, p) => {
            const map = { total: src?.total, today: src?.today, week: src?.thisWeek, month: src?.thisMonth };
            return map[p] ?? 0;
          }
        ),
        buildGroup(
          "sales",
          this.$t("salesAmountStatisticsLabel"),
          "currency-dollar",
          this.stats.salesAmount,
          (src, p) => {
            const map = { total: src?.total, today: src?.today, week: src?.thisWeek, month: src?.thisMonth };
            return `${this.formattedNumber(map[p] ?? 0)} ${currency}`;
          }
        ),
      ];
    },
  },
  watch: {
    showInvoiceDetails(newVal) {
      if (newVal && this.invoices.length === 0) {
        this.loadInvoices();
      }
    },
  },
  mounted() {
    this.getDashboardStats();
    this.getCommercialUserId();
    if (this.role === 'Commercial') {
      this.loadCommercialUserInfo();
    }
  },
  methods: {
    refreshPage() {
      this.getDashboardStats();
      if (this.role === "Commercial") {
        this.loadCommercialUserInfo();
      }
      if (this.showInvoiceDetails) {
        this.loadInvoices();
      }
    },
    getCommercialUserId() {
      // Get user ID from localStorage
      const userInfoStr = localStorage.getItem("info");
      if (userInfoStr) {
        try {
          const userInfo = JSON.parse(userInfoStr);
          this.commercialUserId = userInfo.id || null;
        } catch (error) {
          console.error("Error parsing user info:", error);
        }
      }
    },
    loadCommercialUserInfo() {
      HTTP.get("Admin/CommercialUserInfo")
        .then((response) => {
          if (response.data && response.data.data) {
            this.commercialUserInfo = {
              restaurantName: response.data.data.restaurantName || '',
              logo: response.data.data.logo || null
            };
          }
        })
        .catch((error) => {
          console.error('Error loading commercial user info:', error);
          this.commercialUserInfo = {
            restaurantName: '',
            logo: null
          };
        });
    },
    copyPublicMenuLink() {
      const input = document.getElementById(`publicMenuLink-${this.commercialUserId}`);
      if (input) {
        input.select();
        input.setSelectionRange(0, 99999); // For mobile devices
        try {
          document.execCommand('copy');
          this.$toast.success(this.$i18n.t("linkCopied") || "تم نسخ الرابط بنجاح", {
            position: "top-right",
            timeout: 2000,
          });
        } catch (err) {
          // Fallback for modern browsers
          navigator.clipboard.writeText(this.publicMenuUrl).then(() => {
            this.$toast.success(this.$i18n.t("linkCopied") || "تم نسخ الرابط بنجاح", {
              position: "top-right",
              timeout: 2000,
            });
          }).catch(() => {
            this.$toast.error(this.$i18n.t("copyFailed") || "فشل نسخ الرابط", {
              position: "top-right",
              timeout: 2000,
            });
          });
        }
      }
    },
    copyPublicQueueDisplayLink() {
      const input = document.getElementById(`publicQueueDisplayLink-${this.commercialUserId}`);
      if (input) {
        input.select();
        input.setSelectionRange(0, 99999); // For mobile devices
        try {
          document.execCommand('copy');
          this.$toast.success(this.$i18n.t("linkCopied") || "تم نسخ الرابط بنجاح", {
            position: "top-right",
            timeout: 2000,
          });
        } catch (err) {
          // Fallback for modern browsers
          navigator.clipboard.writeText(this.publicQueueDisplayUrl).then(() => {
            this.$toast.success(this.$i18n.t("linkCopied") || "تم نسخ الرابط بنجاح", {
              position: "top-right",
              timeout: 2000,
            });
          }).catch(() => {
            this.$toast.error(this.$i18n.t("copyFailed") || "فشل نسخ الرابط", {
              position: "top-right",
              timeout: 2000,
            });
          });
        }
      }
    },
    formattedNumber(info) {
      if (typeof info === 'number') {
        return info.toLocaleString("en-EG");
      }
      return info || "0";
    },
    getDashboardStats() {
      this.show = true;
      HTTP.get(`Admin/GetDashboardStats`)
        .then((response) => {
          if (response.data && response.data.data) {
            this.stats = response.data.data;
          }
          this.show = false;
        })
        .catch((error) => {
          console.error("Error fetching dashboard stats:", error);
          this.show = false;
        });
    },
    async loadInvoices() {
      try {
        this.loadingInvoices = true;
        const params = new URLSearchParams({
          pageNumber: (this.invoicePageNumber - 1).toString(),
          pageSize: this.invoicePageSize.toString()
        });

        if (this.invoiceFilters.startDate) {
          params.append('startDate', this.invoiceFilters.startDate);
        }
        if (this.invoiceFilters.endDate) {
          params.append('endDate', this.invoiceFilters.endDate);
        }
        if (this.invoiceFilters.search) {
          params.append('info', this.invoiceFilters.search);
        }

        const response = await HTTP.get(`Admin/GetOrders?${params.toString()}`);
        if (response.data && response.data.data) {
          this.invoices = response.data.data.items || [];
          this.totalInvoices = response.data.data.totalItems || 0;
        } else {
          this.invoices = [];
          this.totalInvoices = 0;
        }
      } catch (error) {
        console.error('Error loading invoices:', error);
        this.invoices = [];
        this.totalInvoices = 0;
        this.$toast.error(this.$i18n.t("errorLoadingInvoices") || "حدث خطأ أثناء تحميل الفواتير", {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.loadingInvoices = false;
      }
    },
    debounceInvoiceSearch() {
      clearTimeout(this.invoiceSearchTimer);
      this.invoiceSearchTimer = setTimeout(() => {
        this.invoicePageNumber = 1;
        this.loadInvoices();
      }, 500);
    },
    clearInvoiceFilters() {
      this.invoiceFilters = {
        startDate: '',
        endDate: '',
        search: ''
      };
      this.invoicePageNumber = 1;
      this.loadInvoices();
    },
    previousInvoicePage() {
      if (this.invoicePageNumber > 1) {
        this.invoicePageNumber--;
        this.loadInvoices();
      }
    },
    nextInvoicePage() {
      if (this.invoicePageNumber < this.totalInvoicePages) {
        this.invoicePageNumber++;
        this.loadInvoices();
      }
    },
    formatDate(dateString) {
      if (!dateString) return '-';
      return formatBusinessDateTime(dateString);
    },
    formatPrice(price) {
      if (price !== null && price !== undefined && !isNaN(price)) {
        return parseFloat(price).toLocaleString("en-EG");
      }
      return "0";
    },
    getOrderTypeText(type) {
      const types = {
        'DineIn': this.$t("dineIn") || "داخل المطعم",
        'Takeaway': this.$t("takeaway") || "خارجي",
        'Delivery': this.$t("delivery") || "توصيل"
      };
      return types[type] || type;
    },
    getOrderTypeClass(type) {
      const classes = {
        'DineIn': 'order-type-dinein',
        'Takeaway': 'order-type-takeaway',
        'Delivery': 'order-type-delivery'
      };
      return classes[type] || '';
    },
    getPaymentMethodText(method) {
      const methods = {
        'Cash': this.$t("cash") || "نقدي",
        'Card': this.$t("card") || "بطاقة",
        'Credit': this.$t("credit") || "آجل"
      };
      return methods[method] || method;
    },
    getStatusText(status) {
      const statuses = {
        'Pending': this.$t("pending") || "قيد الانتظار",
        'Processing': this.$t("processing") || "قيد المعالجة",
        'Completed': this.$t("completed") || "مكتمل",
        'Cancelled': this.$t("cancelled") || "ملغي"
      };
      return statuses[status] || status;
    },
    getStatusClass(status) {
      const classes = {
        'Pending': 'status-pending',
        'Processing': 'status-processing',
        'Completed': 'status-completed',
        'Cancelled': 'status-cancelled'
      };
      return classes[status] || '';
    },
    viewInvoiceDetails(invoice) {
      this.selectedInvoice = invoice;
      this.showInvoiceModal = true;
    },
    getDeliveryStatusText(status) {
      if (!status) return this.$t("notSet") || "غير محدد";
      const statuses = {
        'Pending': this.$t("pending") || "قيد الانتظار",
        'InTransit': this.$t("inTransit") || "قيد التوصيل",
        'Delivered': this.$t("delivered") || "تم التوصيل",
        'Failed': this.$t("failed") || "فشل التوصيل",
        'Completed': this.$t("completed") || "مكتمل"
      };
      return statuses[status] || status;
    },
    getDeliveryStatusClass(status) {
      if (!status) return 'delivery-status-unknown';
      const classes = {
        'Pending': 'delivery-status-pending',
        'InTransit': 'delivery-status-intransit',
        'Delivered': 'delivery-status-delivered',
        'Failed': 'delivery-status-failed',
        'Completed': 'delivery-status-completed'
      };
      return classes[status] || 'delivery-status-unknown';
    },
  },
};
</script>

<style scoped>
.dashboard-sections-link {
  text-decoration: none;
  color: #fff;
}

.section-view-details-btn {
  flex: 0 1 auto;
  width: auto;
  gap: 0.45rem;
}

.dashboard-links-card {
  margin-top: 10px;
}

.dashboard-links-body {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.dashboard-link-block {
  padding: 1rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.dashboard-link-block-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.dashboard-link-block-icon {
  width: 2.75rem;
  height: 2.75rem;
  border-radius: 0.65rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  font-size: 1.25rem;
}

.dashboard-link-block-icon--menu {
  background: rgba(129, 140, 248, 0.12);
  color: var(--primary-color);
  overflow: hidden;
}

.dashboard-link-block-icon--queue {
  background: rgba(59, 130, 246, 0.12);
  color: #2563eb;
}

.dashboard-link-logo {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.dashboard-link-block-text {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.dashboard-link-block-text strong {
  font-size: 0.95rem;
  color: var(--text-primary);
}

.dashboard-link-block-text span {
  font-size: 0.8rem;
  color: var(--text-secondary);
  line-height: 1.4;
}

.dashboard-link-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}

.dashboard-link-input {
  flex: 1 1 200px;
  min-width: 0;
  padding: 0.65rem 0.85rem;
  border-radius: 0.65rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.85rem;
  direction: ltr;
  text-align: left;
}

.dashboard-link-btn {
  flex: 0 1 auto;
  width: auto;
  padding: 0.65rem 1rem;
  font-size: 0.875rem;
  text-decoration: none;
}

.dashboard-link-btn--open {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.dashboard-period-body {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.dashboard-stat-group-title {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  margin: 0 0 0.65rem;
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--text-primary);
}

.dashboard-period-grid {
  margin-bottom: 0;
}

.dashboard-stat-group + .dashboard-stat-group {
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

.dashboard-invoices-body {
  padding-top: 0.5rem;
}

.invoice-details-section {
  animation: slideDown 0.3s ease;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.invoice-filters-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.invoice-filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.invoice-filter-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
}

.invoice-filter-input {
  padding: 0.75rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.9375rem;
  transition: all 0.3s ease;
}

.invoice-filter-input:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.1);
}

.invoice-filter-clear-btn {
  padding: 0.75rem 1.5rem;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  color: var(--text-primary);
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: 1.5rem;
}

.invoice-filter-clear-btn:hover {
  background: var(--danger-color);
  border-color: var(--danger-color);
  color: #ffffff;
}

.invoice-table-section {
  margin-top: 1.5rem;
}

.invoice-table-wrapper {
  overflow-x: auto;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
}

.invoice-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9375rem;
}

.invoice-table thead {
  background: var(--bg-secondary);
  border-bottom: 2px solid var(--border-color);
}

.invoice-table th {
  padding: 1rem;
  text-align: right;
  font-weight: 700;
  color: var(--text-primary);
  font-size: 0.875rem;
  text-transform: uppercase;
}

.invoice-table td {
  padding: 1rem;
  border-bottom: 1px solid var(--border-color);
  color: var(--text-primary);
}

.invoice-table tbody tr:hover {
  background: var(--bg-tertiary);
}

.invoice-table tbody tr:last-child td {
  border-bottom: none;
}

.order-type-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.8125rem;
  font-weight: 600;
}

.order-type-dinein {
  background: rgba(99, 102, 241, 0.1);
  color: var(--primary-color);
}

.order-type-takeaway {
  background: rgba(34, 197, 94, 0.1);
  color: var(--success-color);
}

.order-type-delivery {
  background: rgba(249, 115, 22, 0.1);
  color: #f97316;
}

.status-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.8125rem;
  font-weight: 600;
}

.status-pending {
  background: rgba(251, 191, 36, 0.1);
  color: #fbbf24;
}

.status-processing {
  background: rgba(59, 130, 246, 0.1);
  color: #3b82f6;
}

.status-completed {
  background: rgba(34, 197, 94, 0.1);
  color: var(--success-color);
}

.status-cancelled {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
}

.invoice-action-btn {
  padding: 0.5rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 0.375rem;
  color: var(--primary-color);
  cursor: pointer;
  transition: all 0.3s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.invoice-action-btn:hover {
  background: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
  transform: translateY(-1px);
}

.invoice-pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-top: 1.5rem;
  padding: 1rem;
}

.pagination-btn {
  padding: 0.5rem 1rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  color: var(--text-primary);
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.pagination-btn:hover:not(:disabled) {
  background: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.pagination-info {
  color: var(--text-secondary);
  font-weight: 600;
}

.empty-invoices-state {
  text-align: center;
  padding: 3rem 1rem;
  color: var(--text-secondary);
}

.empty-invoices-state .empty-icon {
  font-size: 4rem;
  color: var(--text-muted);
  margin-bottom: 1rem;
}

.invoice-details-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.invoice-details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1rem;
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.invoice-detail-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.invoice-detail-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.invoice-detail-value {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.invoice-total {
  font-size: 1.25rem;
  color: var(--primary-color);
}

.invoice-items-section {
  margin-top: 1.5rem;
}

.invoice-items-title {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 1rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--border-color);
}

.invoice-items-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9375rem;
}

.invoice-items-table thead {
  background: var(--bg-secondary);
  border-bottom: 2px solid var(--border-color);
}

.invoice-items-table th {
  padding: 0.75rem;
  text-align: right;
  font-weight: 700;
  color: var(--text-primary);
  font-size: 0.875rem;
}

.invoice-items-table td {
  padding: 0.75rem;
  border-bottom: 1px solid var(--border-color);
  color: var(--text-primary);
}

.invoice-items-table tbody tr:hover {
  background: var(--bg-tertiary);
}

.invoice-delivery-info {
  margin-top: 1.5rem;
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.invoice-delivery-title {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 1.5rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--border-color);
}

.invoice-delivery-section {
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid var(--border-color);
}

.invoice-delivery-section:last-child {
  border-bottom: none;
  margin-bottom: 0;
  padding-bottom: 0;
}

.invoice-delivery-subtitle {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin-bottom: 1rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.invoice-delivery-subtitle::before {
  content: '';
  width: 4px;
  height: 16px;
  background: var(--primary-color);
  border-radius: 2px;
}

.invoice-delivery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.delivery-status-badge {
  display: inline-block;
  padding: 0.375rem 0.875rem;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
}

.delivery-status-pending {
  background: rgba(251, 191, 36, 0.15);
  color: #fbbf24;
  border: 1px solid rgba(251, 191, 36, 0.3);
}

.delivery-status-intransit {
  background: rgba(59, 130, 246, 0.15);
  color: #3b82f6;
  border: 1px solid rgba(59, 130, 246, 0.3);
}

.delivery-status-delivered {
  background: rgba(34, 197, 94, 0.15);
  color: var(--success-color);
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.delivery-status-failed {
  background: rgba(239, 68, 68, 0.15);
  color: var(--danger-color);
  border: 1px solid rgba(239, 68, 68, 0.3);
}

.delivery-status-completed {
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
  border: 1px solid rgba(16, 185, 129, 0.3);
}

.delivery-status-unknown {
  background: rgba(107, 114, 128, 0.15);
  color: #6b7280;
  border: 1px solid rgba(107, 114, 128, 0.3);
}

.invoice-notes-section {
  margin-top: 1.5rem;
  padding: 1rem;
  background: var(--bg-tertiary);
  border-radius: 0.5rem;
  border: 1px solid var(--border-color);
}

.invoice-notes-text {
  margin-top: 0.5rem;
  color: var(--text-primary);
  line-height: 1.6;
  white-space: pre-wrap;
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 2rem;
  color: var(--text-secondary);
}

[dir="rtl"] .invoice-table th,
[dir="rtl"] .invoice-table td {
  text-align: right;
}

[dir="ltr"] .invoice-table th,
[dir="ltr"] .invoice-table td {
  text-align: left;
}
</style>





