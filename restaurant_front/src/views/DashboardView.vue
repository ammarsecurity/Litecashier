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
        <div class="dashboard-page-container">
          <div class="dashboard-page-content">
            <!-- Welcome Header -->
            <div class="dashboard-welcome-section">
              <h1 class="dashboard-welcome-title">{{ $t("welcomeToDashboard") || "مرحباً بك في لوحة التحكم" }}</h1>
              <p class="dashboard-welcome-subtitle">{{ $t("dashboardSubtitle") || "نظرة شاملة على إحصائيات متجرك" }}</p>
              <div class="dashboard-to-sections-wrap">
                <router-link to="/sections" class="dashboard-to-sections-btn">
                  <b-icon icon="grid-3x3-gap-fill" class="me-2"></b-icon>
                  {{ $t("systemModules") }}
                </router-link>
              </div>
            </div>

            <!-- Quick Stats Overview -->
            <div class="dashboard-quick-stats">
              <div class="quick-stat-card quick-stat-primary">
                <div class="quick-stat-icon">
                  <b-icon icon="receipt-cutoff"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ stats.orders?.total || 0 }}</h3>
                  <p class="quick-stat-label">{{ $t("all_sales") }}</p>
                </div>
              </div>
              <div class="quick-stat-card quick-stat-success">
                <div class="quick-stat-icon">
                  <b-icon icon="currency-dollar"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ formattedNumber(stats.salesAmount?.total || 0) }} {{ $t("currency") }}</h3>
                  <p class="quick-stat-label">{{ $t("totalLabel") }} {{ $t("salesAmountStatisticsLabel") }}</p>
                </div>
              </div>
              <div class="quick-stat-card quick-stat-info">
                <div class="quick-stat-icon">
                  <b-icon icon="box-fill"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ stats.products?.total || 0 }}</h3>
                  <p class="quick-stat-label">{{ $t("Items") }}</p>
                </div>
              </div>
              <div class="quick-stat-card quick-stat-warning">
                <div class="quick-stat-icon">
                  <b-icon icon="people"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ stats.users?.total || 0 }}</h3>
                  <p class="quick-stat-label">{{ $t("all_accounts") }}</p>
                </div>
              </div>
            </div>

            <!-- Public Menu Link Section (Only for Commercial users) -->
            <div v-if="role === 'Commercial'" class="public-menu-section">
              <div class="public-menu-card">
                <div class="public-menu-header">
                  <div class="public-menu-logo-wrapper">
                    <img 
                      v-if="commercialUserInfo.logo && !logoError" 
                      :src="commercialUserInfo.logo" 
                      alt="Restaurant Logo" 
                      class="public-menu-logo"
                      @error="logoError = true"
                    />
                    <div v-else class="public-menu-icon-wrapper">
                      <b-icon icon="shop" class="public-menu-icon"></b-icon>
                    </div>
                  </div>
                  <div class="public-menu-content">
                    <h3 class="public-menu-title">{{ commercialUserInfo.restaurantName || $t("publicMenu") || "القائمة العامة" }}</h3>
                    <p class="public-menu-description">{{ $t("publicMenuDescription") || "شارك رابط المنيو الخاص بك مع العملاء" }}</p>
                  </div>
                </div>
                <div class="public-menu-link-wrapper">
                  <div class="public-menu-link-box">
                    <input 
                      type="text" 
                      :value="publicMenuUrl" 
                      readonly 
                      class="public-menu-link-input"
                      :id="'publicMenuLink-' + commercialUserId"
                    />
                    <button 
                      class="public-menu-copy-btn"
                      @click="copyPublicMenuLink"
                      :title="$t('copyLink') || 'نسخ الرابط'"
                    >
                      <b-icon icon="clipboard" class="me-1"></b-icon>
                      {{ $t("copyLink") || "نسخ" }}
                    </button>
                    <a 
                      :href="publicMenuUrl" 
                      target="_blank" 
                      class="public-menu-open-btn"
                      :title="$t('openInNewTab') || 'فتح في نافذة جديدة'"
                    >
                      <b-icon icon="box-arrow-up-right" class="me-1"></b-icon>
                      {{ $t("open") || "فتح" }}
                    </a>
                  </div>
                </div>
              </div>
            </div>

            <!-- Public Queue Display Link Section (Only for Commercial users) -->
            <div v-if="role === 'Commercial'" class="public-menu-section">
              <div class="public-menu-card">
                <div class="public-menu-header">
                  <div class="public-menu-logo-wrapper">
                    <div class="public-menu-icon-wrapper">
                      <b-icon icon="display" class="public-menu-icon"></b-icon>
                    </div>
                  </div>
                  <div class="public-menu-content">
                    <h3 class="public-menu-title">{{ $t("publicQueueDisplay") || "شاشة عرض الطلبات" }}</h3>
                    <p class="public-menu-description">{{ $t("publicQueueDisplayDescription") || "شارك رابط شاشة عرض الطلبات للعرض على الشاشة الكبيرة" }}</p>
                  </div>
                </div>
                <div class="public-menu-link-wrapper">
                  <div class="public-menu-link-box">
                    <input 
                      type="text" 
                      :value="publicQueueDisplayUrl" 
                      readonly 
                      class="public-menu-link-input"
                      :id="'publicQueueDisplayLink-' + commercialUserId"
                    />
                    <button 
                      class="public-menu-copy-btn"
                      @click="copyPublicQueueDisplayLink"
                      :title="$t('copyLink') || 'نسخ الرابط'"
                    >
                      <b-icon icon="clipboard" class="me-1"></b-icon>
                      {{ $t("copyLink") || "نسخ" }}
                    </button>
                    <a 
                      :href="publicQueueDisplayUrl" 
                      target="_blank" 
                      class="public-menu-open-btn"
                      :title="$t('openInNewTab') || 'فتح في نافذة جديدة'"
                    >
                      <b-icon icon="box-arrow-up-right" class="me-1"></b-icon>
                      {{ $t("open") || "فتح" }}
                    </a>
                  </div>
                </div>
              </div>
            </div>

            <!-- Invoice Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="receipt-cutoff" class="section-title-icon"></b-icon>
                  {{ $t("invoiceStatisticsLabel") }}
                </h2>
                <button 
                  class="section-view-details-btn"
                  @click="showInvoiceDetails = !showInvoiceDetails"
                >
                  <b-icon :icon="showInvoiceDetails ? 'chevron-up' : 'chevron-down'" class="me-2"></b-icon>
                  {{ showInvoiceDetails ? ($t("hideDetails") || "إخفاء التفاصيل") : ($t("viewDetails") || "عرض التفاصيل") }}
                </button>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="primary"
                  :value="stats.orders?.total || 0"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="receipt-cutoff" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="stats.orders?.today || 0"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.orders?.thisWeek || 0"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="stats.orders?.thisMonth || 0"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>

              <!-- Invoice Details Section (Expandable) -->
              <div v-if="showInvoiceDetails" class="invoice-details-section">
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
            </section>

            <!-- Items Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="box-fill" class="section-title-icon"></b-icon>
                  {{ $t("itemsStatisticsLabel") }}
                </h2>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="primary"
                  :value="stats.items?.total || 0"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="box-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="stats.items?.today || 0"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.items?.thisWeek || 0"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="stats.items?.thisMonth || 0"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Sales Amount Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="currency-dollar" class="section-title-icon"></b-icon>
                  {{ $t("salesAmountStatisticsLabel") }}
                </h2>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="primary"
                  :value="formattedNumber(stats.salesAmount?.total || 0) + ' ' + $t('currency')"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="currency-dollar" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="formattedNumber(stats.salesAmount?.today || 0) + ' ' + $t('currency')"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="formattedNumber(stats.salesAmount?.thisWeek || 0) + ' ' + $t('currency')"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="formattedNumber(stats.salesAmount?.thisMonth || 0) + ' ' + $t('currency')"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Additional Statistics -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="graph-up" class="section-title-icon"></b-icon>
                  {{ $t("additionalStats") || "إحصائيات إضافية" }}
                </h2>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="info"
                  :value="stats.products?.total || 0"
                  :label="$t('Items') + ' (' + $t('totalLabel') + ')'"
                >
                  <template #icon>
                    <b-icon icon="box-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="warning"
                  :value="stats.users?.total || 0"
                  :label="$t('all_accounts')"
                >
                  <template #icon>
                    <b-icon icon="people-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.categories?.total || 0"
                  :label="$t('all_categories')"
                >
                  <template #icon>
                    <b-icon icon="tags-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>
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
import StatCard from "@/components/StatCard.vue";

export default {
  name: "DashboardView",
  components: {
    AppHeader,
    StatCard,
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
      const date = new Date(dateString);
      return date.toLocaleDateString('ar-EG', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
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
.dashboard-to-sections-wrap {
  margin-top: 1.25rem;
}

.dashboard-to-sections-btn {
  display: inline-flex;
  align-items: center;
  padding: 0.6rem 1.1rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.9375rem;
  text-decoration: none;
  transition: background 0.2s ease, border-color 0.2s ease, transform 0.2s ease;
}

.dashboard-to-sections-btn:hover {
  background: var(--primary-color);
  border-color: var(--primary-color);
  color: #fff;
  transform: translateY(-1px);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.section-view-details-btn {
  display: flex;
  align-items: center;
  padding: 0.5rem 1rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.3s ease;
}

.section-view-details-btn:hover {
  background: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(129, 140, 248, 0.3);
}

.invoice-details-section {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 2px solid var(--border-color);
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

