<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
            <div class="app-page-container">
                <div class="app-page-content reports-page-content">
                    <div class="users-header-section">
                        <div class="users-header-content app-header-row">
                            <div class="header-title-wrapper">
                                <div class="header-icon-wrapper">
                                    <b-icon icon="file-earmark-bar-graph-fill" class="header-icon"></b-icon>
                                </div>
                                <div>
                                    <h1 class="users-page-title">{{ $t('all_sales') }}</h1>
                                    <p class="header-subtitle">{{ $t('reportsDescription') || 'نظام تقارير متكامل لتحليل المبيعات والأرباح' }}</p>
                                </div>
                            </div>
                            <div class="app-header-actions">
                                <button type="button" class="btn-refresh" @click="refreshReports" :disabled="show">
                                    <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                                    <span class="button-text">{{ $t('refresh') || 'تحديث' }}</span>
                                </button>
                            </div>
                        </div>
                    </div>

                    <div class="app-section-card app-section-card--flush">
                      <div class="app-section-body app-section-body--tabs">
                    <div class="reports-tabs-section">
                        <div class="reports-tabs">
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'orders' }"
                                @click="activeTab = 'orders'"
                            >
                                <b-icon icon="receipt-cutoff" class="me-2"></b-icon>
                                {{ $t('orders') || 'الفواتير' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'profit' }"
                                @click="activeTab = 'profit'; loadProfitReport()"
                            >
                                <b-icon icon="file-earmark-bar-graph-fill" class="me-2"></b-icon>
                                {{ $t('profitReport') || 'تقرير الأرباح' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'topItems' }"
                                @click="activeTab = 'topItems'; loadTopSellingItems()"
                            >
                                <b-icon icon="trophy-fill" class="me-2"></b-icon>
                                {{ $t('topSellingItems') || 'الأكثر مبيعاً' }}
                            </button>
                            <button
                                class="report-tab"
                                :class="{ 'report-tab-active': activeTab === 'productSales' }"
                                @click="activeTab = 'productSales'; ensureReportTags(); loadProductSalesReport()"
                            >
                                <b-icon icon="basket-fill" class="me-2"></b-icon>
                                {{ $t('productSalesReport') || 'مبيعات المنتجات' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'byCategory' }"
                                @click="activeTab = 'byCategory'; loadSalesByCategory()"
                            >
                                <b-icon icon="tags-fill" class="me-2"></b-icon>
                                {{ $t('salesByCategory') || 'المبيعات حسب الفئة' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'byEmployee' }"
                                @click="activeTab = 'byEmployee'; loadSalesByEmployee()"
                            >
                                <b-icon icon="people-fill" class="me-2"></b-icon>
                                {{ $t('salesByEmployee') || 'المبيعات حسب الموظف' }}
                            </button>
                            <button
                                class="report-tab"
                                :class="{ 'report-tab-active': activeTab === 'byWarehouse' }"
                                @click="activeTab = 'byWarehouse'; loadSalesByWarehouse()"
                            >
                                <b-icon icon="building" class="me-2"></b-icon>
                                {{ $t('salesByWarehouse') || 'المبيعات حسب المخازن' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'lowStock' }"
                                @click="activeTab = 'lowStock'; loadLowStockItems()"
                            >
                                <b-icon icon="exclamation-triangle-fill" class="me-2"></b-icon>
                                {{ $t('lowStockItems') || 'منتجات قليلة المخزون' }}
                            </button>
                        </div>
                    </div>

                    <div class="app-filters-panel reports-filters-panel">
                        <div class="app-filters-panel-head reports-filters-panel-head">
                            <div class="app-filters-panel-title reports-filters-panel-title">
                                <span class="app-filters-panel-icon reports-filters-panel-icon">
                                    <b-icon icon="funnel-fill"></b-icon>
                                </span>
                                <div>
                                    <h3>{{ $t('filters') || 'فلاتر التقرير' }}</h3>
                                    <p>{{ reportsFiltersHint }}</p>
                                </div>
                            </div>
                            <div class="reports-filters-panel-actions">
                                <button
                                    v-if="activeTab === 'productSales'"
                                    type="button"
                                    class="btn-refresh"
                                    @click="loadProductSalesReport()"
                                >
                                    <b-icon icon="search" class="button-icon"></b-icon>
                                    <span class="button-text">{{ $t('search') || 'بحث' }}</span>
                                </button>
                                <button
                                    v-if="activeTab === 'orders'"
                                    type="button"
                                    class="export-excel-btn"
                                    @click="exportCurrentReportExcel()"
                                    :disabled="exportingExcel"
                                >
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                                <button
                                    v-if="hasReportFilters"
                                    type="button"
                                    class="users-filter-clear-btn reports-filters-clear-btn"
                                    @click="clearCurrentTabFilters"
                                >
                                    <b-icon icon="x-circle" class="me-1"></b-icon>
                                    {{ $t('clearFilters') || 'مسح الفلاتر' }}
                                </button>
                            </div>
                        </div>

                        <div
                            class="reports-filters-fields"
                            :class="'reports-filters-fields--' + activeTab"
                        >
                            <!-- Orders -->
                            <template v-if="activeTab === 'orders'">
                                <label class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('invoice_number') || 'رقم الفاتورة' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="search" class="search-icon"></b-icon>
                                        <input
                                            v-model="search.info"
                                            type="search"
                                            :placeholder="$t('invoice_number')"
                                            class="users-search-input"
                                            autocomplete="off"
                                        />
                                    </div>
                                </label>
                                <label class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('from_date') || 'من تاريخ' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="calendar" class="search-icon"></b-icon>
                                        <input v-model="search.startDate" type="date" class="users-search-input" />
                                    </div>
                                </label>
                                <label class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('to_date') || 'إلى تاريخ' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="calendar-check" class="search-icon"></b-icon>
                                        <input v-model="search.endDate" type="date" class="users-search-input" />
                                    </div>
                                </label>
                                <label class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('paymentMethod') || 'طريقة الدفع' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="credit-card" class="search-icon"></b-icon>
                                        <select v-model="search.paymentMethod" class="users-search-input reports-filter-select">
                                            <option value="">{{ $t('allPaymentMethods') || 'جميع طرق الدفع' }}</option>
                                            <option value="Cash">{{ $t('cash') || 'نقد' }}</option>
                                            <option value="Card">{{ $t('card') || 'بطاقة' }}</option>
                                            <option value="Credit">{{ $t('credit') || 'دفع لاحق' }}</option>
                                        </select>
                                    </div>
                                </label>
                            </template>

                            <!-- Low stock -->
                            <template v-else-if="activeTab === 'lowStock'">
                                <label class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('threshold') || 'حد الكمية' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="exclamation-triangle" class="search-icon"></b-icon>
                                        <input
                                            v-model.number="lowStockThreshold"
                                            type="number"
                                            min="0"
                                            :placeholder="$t('threshold') || 'حد الكمية'"
                                            class="users-search-input"
                                            @change="loadLowStockItems()"
                                        />
                                    </div>
                                </label>
                            </template>

                            <!-- Advanced reports (dates + optional product sales) -->
                            <template v-else>
                                <label class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('from_date') || 'من تاريخ' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="calendar" class="search-icon"></b-icon>
                                        <input
                                            v-model="reportFilters.startDate"
                                            type="date"
                                            class="users-search-input"
                                            @change="loadAdvancedReport()"
                                        />
                                    </div>
                                </label>
                                <label class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('to_date') || 'إلى تاريخ' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="calendar-check" class="search-icon"></b-icon>
                                        <input
                                            v-model="reportFilters.endDate"
                                            type="date"
                                            class="users-search-input"
                                            @change="loadAdvancedReport()"
                                        />
                                    </div>
                                </label>
                                <label v-if="activeTab === 'productSales'" class="reports-filter-field">
                                    <span class="reports-filter-label">{{ $t('categoryPlaceholder') || $t('category') || 'القسم' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="tags" class="search-icon"></b-icon>
                                        <select
                                            v-model="productSalesFilters.tag"
                                            class="users-search-input reports-filter-select"
                                            @change="loadProductSalesReport()"
                                        >
                                            <option value="">{{ $t('all_categories') || 'جميع الاقسام' }}</option>
                                            <option
                                                v-for="tag in reportTags"
                                                :key="tag.id || tag.name"
                                                :value="tag.name"
                                            >
                                                {{ tag.name }}
                                            </option>
                                        </select>
                                    </div>
                                </label>
                                <label v-if="activeTab === 'productSales'" class="reports-filter-field reports-filter-field--grow">
                                    <span class="reports-filter-label">{{ $t('search') || 'بحث' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="search" class="search-icon"></b-icon>
                                        <input
                                            v-model="productSalesFilters.info"
                                            type="search"
                                            class="users-search-input"
                                            :placeholder="$t('productSalesSearchPlaceholder') || 'بحث عن منتج...'"
                                            autocomplete="off"
                                            @keyup.enter="loadProductSalesReport()"
                                        />
                                    </div>
                                </label>
                            </template>
                        </div>
                    </div>

                    <div v-if="activeTab === 'orders'">
                        <div class="app-overview-grid reports-orders-summary">
                            <div class="app-overview-stat">
                                <span class="app-overview-stat-icon app-overview-stat-icon--primary"><b-icon icon="receipt-cutoff"></b-icon></span>
                                <div>
                                    <div class="app-overview-stat-value">
                                        <b-spinner small v-if="show"></b-spinner>
                                        <template v-else>{{ ordersSummary.totalOrders || 0 }}</template>
                                    </div>
                                    <div class="app-overview-stat-label">{{ $t('totalOrders') || 'إجمالي الفواتير' }}</div>
                                </div>
                            </div>
                            <div class="app-overview-stat">
                                <span class="app-overview-stat-icon app-overview-stat-icon--info"><b-icon icon="calculator"></b-icon></span>
                                <div>
                                    <div class="app-overview-stat-value app-overview-stat-value--text">
                                        <b-spinner small v-if="show"></b-spinner>
                                        <template v-else>{{ formatPrice(ordersSummary.totalSubTotal || 0) }} {{ $t('currency') }}</template>
                                    </div>
                                    <div class="app-overview-stat-label">{{ $t('subtotal') || 'المجموع قبل الخصم' }}</div>
                                </div>
                            </div>
                            <div class="app-overview-stat">
                                <span class="app-overview-stat-icon app-overview-stat-icon--warning"><b-icon icon="percent"></b-icon></span>
                                <div>
                                    <div class="app-overview-stat-value app-overview-stat-value--text">
                                        <b-spinner small v-if="show"></b-spinner>
                                        <template v-else>{{ formatPrice(ordersSummary.totalDiscount || 0) }} {{ $t('currency') }}</template>
                                    </div>
                                    <div class="app-overview-stat-label">{{ $t('discountLabel') || 'الخصم' }}</div>
                                </div>
                            </div>
                            <div class="app-overview-stat">
                                <span class="app-overview-stat-icon app-overview-stat-icon--success"><b-icon icon="cash-stack"></b-icon></span>
                                <div>
                                    <div class="app-overview-stat-value app-overview-stat-value--text">
                                        <b-spinner small v-if="show"></b-spinner>
                                        <template v-else>{{ formatPrice(ordersSummary.totalSales || 0) }} {{ $t('currency') }}</template>
                                    </div>
                                    <div class="app-overview-stat-label">{{ $t('totalSales') || 'إجمالي المبيعات' }}</div>
                                </div>
                            </div>
                            <div class="app-overview-stat">
                                <span class="app-overview-stat-icon app-overview-stat-icon--danger"><b-icon icon="box-seam"></b-icon></span>
                                <div>
                                    <div class="app-overview-stat-value">
                                        <b-spinner small v-if="show"></b-spinner>
                                        <template v-else>{{ ordersSummary.totalItemsSold || 0 }}</template>
                                    </div>
                                    <div class="app-overview-stat-label">{{ $t('totalItemsSold') || 'المواد المباعة' }}</div>
                                </div>
                            </div>
                            <div class="app-overview-stat">
                                <span class="app-overview-stat-icon app-overview-stat-icon--primary"><b-icon icon="graph-up"></b-icon></span>
                                <div>
                                    <div class="app-overview-stat-value app-overview-stat-value--text">
                                        <b-spinner small v-if="show"></b-spinner>
                                        <template v-else>{{ formatPrice(ordersSummary.averageOrderValue || 0) }} {{ $t('currency') }}</template>
                                    </div>
                                    <div class="app-overview-stat-label">{{ $t('averageOrderValue') || 'متوسط قيمة الفاتورة' }}</div>
                                </div>
                            </div>
                        </div>
                        <p v-if="ordersReportPeriodColumn" class="reports-orders-summary-period">
                            {{ $t('reportDateRange') || 'فترة التقرير' }}: {{ ordersReportPeriodColumn }}
                        </p>

                        <div class="report-table-container">
                            <b-table
                                id="orders-table"
                                :items="Orders"
                                :fields="ordersTableFields"
                                striped
                                hover
                                responsive
                                class="reports-table"
                                :empty-text="$t('noInvoicesFound') || 'لا توجد فواتير'"
                            >
                                <template #cell(orderCode)="row">
                                    <span class="report-item-name">{{ row.item.orderCode }}</span>
                                </template>
                                <template #cell(insertDate)="row">
                                    <span>{{ formatDate(row.item.insertDate) }}</span>
                                </template>
                                <template #cell(paymentMethod)="row">
                                    <span>{{ getPaymentMethodText(row.item.paymentMethod) }}</span>
                                </template>
                                <template #cell(priceMode)="row">
                                    <span
                                      class="report-price-mode-badge"
                                      :class="row.item.isWholesale ? 'report-price-mode-badge--wholesale' : 'report-price-mode-badge--retail'"
                                    >
                                      {{ row.item.isWholesale ? ($t("wholesalePriceMode") || "جملة") : ($t("retailPriceMode") || "مفرد") }}
                                    </span>
                                </template>
                                <template #cell(orderType)="row">
                                    <span>{{ getOrderTypeText(row.item.orderType) }}</span>
                                </template>
                                <template #cell(itemsCount)="row">
                                    <span class="quantity-badge">{{ row.item.itemsCount || 0 }}</span>
                                </template>
                                <template #cell(discountAmount)="row">
                                    <span v-if="Number(row.item.discountAmount || 0) > 0" class="report-discount-value">
                                        − {{ formatPrice(row.item.discountAmount || 0) }}
                                    </span>
                                    <span v-else>—</span>
                                </template>
                                <template #cell(totalAmount)="row">
                                    <span class="report-amount-value">
                                        {{ formatPrice(row.item.orderTotalAfterDiscount ?? row.item.orderPrice ?? 0) }} {{ $t('currency') }}
                                    </span>
                                </template>
                                <template #cell(createdByUsername)="row">
                                    <span>{{ row.item.createdByUsername || '—' }}</span>
                                </template>
                                <template #cell(actions)="row">
                                    <div class="actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                                        <button
                                            type="button"
                                            class="action-btn action-btn--icon action-btn--view"
                                            @click="showItemsModel(row.item.customerOrderItem, row.item)"
                                            :title="$t('view_items')"
                                            :aria-label="$t('view_items')"
                                        >
                                            <b-icon icon="eye" class="action-icon"></b-icon>
                                        </button>
                                        <button
                                            type="button"
                                            class="action-btn action-btn--icon action-btn--print"
                                            :disabled="printingInvoice"
                                            @click="printOrderFromRow(row.item)"
                                            :title="$t('print') || 'طباعة'"
                                            :aria-label="$t('print') || 'طباعة'"
                                        >
                                            <b-icon icon="printer" class="action-icon"></b-icon>
                                        </button>
                                        <button
                                            type="button"
                                            class="action-btn action-btn--icon action-btn--edit"
                                            @click="editOrder(row.item)"
                                            :title="$t('editOrder')"
                                            :aria-label="$t('editOrder')"
                                        >
                                            <b-icon icon="pencil-square" class="action-icon"></b-icon>
                                        </button>
                                    </div>
                                </template>
                            </b-table>
                        </div>

                        <div class="users-pagination-section">
                            <b-pagination 
                                v-model="pageNumber" 
                                :total-rows="totalOrders" 
                                :per-page="pageSize"
                                aria-controls="orders-table"
                                class="users-pagination"
                            ></b-pagination>
                        </div>
                    </div>

                    <!-- Advanced Reports Views -->
                    <div v-else class="advanced-reports-container">
                        <!-- Profit Report -->
                        <div v-if="activeTab === 'profit'" class="report-section">
                            <div class="app-overview-grid report-stats-grid">
                                <div class="app-overview-stat report-stat-card">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--primary report-stat-icon">
                                        <b-icon icon="currency-dollar"></b-icon>
                                    </span>
                                    <div class="report-stat-content">
                                        <div class="app-overview-stat-value app-overview-stat-value--text report-stat-value">
                                            {{ formatPrice(profitReport.totalSales || 0) }} {{ $t('currency') }}
                                        </div>
                                        <div class="app-overview-stat-label report-stat-label">{{ $t('totalSales') || 'إجمالي المبيعات' }}</div>
                                        <p class="report-stat-detail" v-if="profitReport.period">
                                            {{ $t('period') || 'الفترة' }}: {{ profitReport.period.startDate || '-' }}
                                            {{ profitReport.period.endDate ? ' - ' + profitReport.period.endDate : '' }}
                                        </p>
                                    </div>
                                </div>
                                <div class="app-overview-stat report-stat-card">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--danger report-stat-icon">
                                        <b-icon icon="cart"></b-icon>
                                    </span>
                                    <div class="report-stat-content">
                                        <div class="app-overview-stat-value app-overview-stat-value--text report-stat-value">
                                            {{ formatPrice(profitReport.totalCost || 0) }} {{ $t('currency') }}
                                        </div>
                                        <div class="app-overview-stat-label report-stat-label">{{ $t('totalCost') || 'إجمالي التكلفة' }}</div>
                                        <p class="report-stat-detail" v-if="profitReport.totalItemsSold">
                                            {{ $t('totalItemsSold') || 'إجمالي المواد المباعة' }}: {{ profitReport.totalItemsSold }}
                                        </p>
                                    </div>
                                </div>
                                <div class="app-overview-stat report-stat-card">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--success report-stat-icon">
                                        <b-icon icon="file-earmark-bar-graph-fill"></b-icon>
                                    </span>
                                    <div class="report-stat-content">
                                        <div class="app-overview-stat-value app-overview-stat-value--text report-stat-value">
                                            {{ formatPrice(profitReport.totalProfit || 0) }} {{ $t('currency') }}
                                        </div>
                                        <div class="app-overview-stat-label report-stat-label">{{ $t('totalProfit') || 'إجمالي الربح' }}</div>
                                        <p class="report-stat-detail" v-if="profitReport.totalSales && profitReport.totalCost">
                                            {{ $t('profitRatio') || 'نسبة الربح' }}: {{ ((profitReport.totalProfit / profitReport.totalSales) * 100).toFixed(2) }}%
                                        </p>
                                    </div>
                                </div>
                                <div class="app-overview-stat report-stat-card">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--info report-stat-icon">
                                        <b-icon icon="percent"></b-icon>
                                    </span>
                                    <div class="report-stat-content">
                                        <div class="app-overview-stat-value report-stat-value">{{ profitReport.profitMargin || 0 }}%</div>
                                        <div class="app-overview-stat-label report-stat-label">{{ $t('profitMargin') || 'هامش الربح' }}</div>
                                        <p class="report-stat-detail">
                                            {{ $t('profitMarginDescription') || 'نسبة الربح من إجمالي المبيعات' }}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Top Selling Items -->
                        <div v-if="activeTab === 'topItems'" class="report-section">
                            <div class="report-section-intro" v-if="topSellingItems.length > 0">
                                <div class="report-info-banner">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('topSellingItemsDescription') || 'عرض أفضل المنتجات مبيعاً حسب الكمية المباعة' }}</span>
                                </div>
                            </div>
                            <div class="app-overview-grid reports-orders-summary" v-if="topSellingItems.length > 0">
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--success"><b-icon icon="currency-dollar"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value app-overview-stat-value--text">{{ formatPrice(topSellingItemsSummary.totalSales) }} {{ $t('currency') }}</div>
                                        <div class="app-overview-stat-label">{{ $t('totalSales') || 'إجمالي المبيعات' }}</div>
                                    </div>
                                </div>
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--danger"><b-icon icon="box-seam"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ topSellingItemsSummary.totalQuantitySold || 0 }}</div>
                                        <div class="app-overview-stat-label">{{ $t('totalQuantitySold') || 'الكمية المباعة' }}</div>
                                    </div>
                                </div>
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--info"><b-icon icon="grid-3x3-gap"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ topSellingItemsSummary.totalDistinctItems || 0 }}</div>
                                        <div class="app-overview-stat-label">{{ $t('distinctItemsCount') || 'عدد الأصناف' }}</div>
                                    </div>
                                </div>
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--primary"><b-icon icon="receipt-cutoff"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ topSellingItemsSummary.totalOrders || 0 }}</div>
                                        <div class="app-overview-stat-label">{{ $t('totalOrders') || 'عدد الطلبات' }}</div>
                                    </div>
                                </div>
                            </div>
                            <p v-if="topSellingItems.length > 0" class="reports-summary-note">
                                {{ $t('topSellingGrandTotalHint') || 'المجموع الكلي لجميع الأصناف المباعة في الفترة (وليس أعلى 10 فقط)' }}
                            </p>
                            <div class="report-table-container" v-if="topSellingItems.length > 0">
                                <table class="report-table">
                                    <thead>
                                        <tr>
                                            <th>{{ $t('rank') || 'الترتيب' }}</th>
                                            <th>{{ $t('itemName') || 'اسم المنتج' }}</th>
                                            <th>{{ $t('itemCode') || 'الكود' }}</th>
                                            <th>{{ $t('quantitySold') || 'الكمية المباعة' }}</th>
                                            <th>{{ $t('totalSales') || 'إجمالي المبيعات' }}</th>
                                            <th>{{ $t('orderCount') || 'عدد الطلبات' }}</th>
                                            <th>{{ $t('averagePrice') || 'متوسط السعر' }}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="(item, index) in topSellingItems" :key="item.itemId">
                                            <td class="report-item-rank">
                                                <span class="rank-badge" :class="getRankClass(index)">{{ index + 1 }}</span>
                                            </td>
                                            <td class="report-item-name">{{ item.itemName }}</td>
                                            <td class="report-item-code">{{ item.itemCode }}</td>
                                            <td class="report-item-quantity">
                                                <span class="quantity-badge">{{ item.totalQuantitySold }}</span>
                                            </td>
                                            <td class="report-item-price">{{ formatPrice(item.totalSales) }} {{ $t('currency') }}</td>
                                            <td class="report-item-quantity">{{ item.orderCount }}</td>
                                            <td class="report-item-price">{{ formatPrice(item.totalSales / item.totalQuantitySold) }} {{ $t('currency') }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Product Sales Report -->
                        <div v-if="activeTab === 'productSales'" class="report-section">
                            <div class="report-section-intro" v-if="productSalesItems.length > 0">
                                <div class="report-info-banner">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('productSalesReportDescription') || 'كمية المبيعات والمتبقي لكل منتج مع فلتر القسم والتاريخ' }}</span>
                                </div>
                            </div>
                            <div class="app-overview-grid reports-orders-summary" v-if="productSalesItems.length > 0">
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--success"><b-icon icon="currency-dollar"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value app-overview-stat-value--text">{{ formatPrice(productSalesSummary.totalSales) }} {{ $t('currency') }}</div>
                                        <div class="app-overview-stat-label">{{ $t('totalSales') || 'إجمالي المبيعات' }}</div>
                                    </div>
                                </div>
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--danger"><b-icon icon="box-seam"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ productSalesSummary.totalQuantitySold || 0 }}</div>
                                        <div class="app-overview-stat-label">{{ $t('totalQuantitySold') || 'الكمية المباعة' }}</div>
                                    </div>
                                </div>
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--info"><b-icon icon="archive"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ productSalesSummary.totalRemainingQuantity || 0 }}</div>
                                        <div class="app-overview-stat-label">{{ $t('totalRemainingQuantity') || 'إجمالي المتبقي' }}</div>
                                    </div>
                                </div>
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--primary"><b-icon icon="grid-3x3-gap"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ productSalesSummary.totalDistinctItems || 0 }}</div>
                                        <div class="app-overview-stat-label">{{ $t('distinctItemsCount') || 'عدد الأصناف' }}</div>
                                    </div>
                                </div>
                            </div>
                            <div class="report-table-container" v-if="productSalesItems.length > 0">
                                <table class="report-table">
                                    <thead>
                                        <tr>
                                            <th>{{ $t('itemName') || 'اسم المنتج' }}</th>
                                            <th>{{ $t('itemCode') || 'الكود' }}</th>
                                            <th>{{ $t('category') || 'القسم' }}</th>
                                            <th>{{ $t('quantitySold') || 'الكمية المباعة' }}</th>
                                            <th>{{ $t('remainingQuantity') || 'المتبقي' }}</th>
                                            <th>{{ $t('totalSales') || 'إجمالي المبيعات' }}</th>
                                            <th>{{ $t('orderCount') || 'عدد الطلبات' }}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="item in productSalesItems" :key="item.itemId">
                                            <td class="report-item-name">{{ item.itemName }}</td>
                                            <td class="report-item-code">{{ item.itemCode || '—' }}</td>
                                            <td>{{ item.category || '—' }}</td>
                                            <td class="report-item-quantity">
                                                <span class="quantity-badge">{{ item.quantitySold }}</span>
                                            </td>
                                            <td class="report-item-quantity">{{ item.remainingQuantity }}</td>
                                            <td class="report-item-price">{{ formatPrice(item.totalSales) }} {{ $t('currency') }}</td>
                                            <td class="report-item-quantity">{{ item.orderCount }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div v-else class="report-section-intro">
                                <div class="report-info-banner">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('productSalesEmpty') || 'لا توجد نتائج مطابقة للفلاتر' }}</span>
                                </div>
                            </div>
                        </div>

                        <!-- Sales By Category -->
                        <div v-if="activeTab === 'byCategory'" class="report-section">
                            <div class="report-section-intro" v-if="salesByCategory.length > 0">
                                <div class="report-info-banner">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('salesByCategoryDescription') || 'تحليل المبيعات حسب الفئات المختلفة' }}</span>
                                </div>
                            </div>
                            <div class="report-table-container" v-if="salesByCategory.length > 0">
                                <table class="report-table">
                                    <thead>
                                        <tr>
                                            <th>{{ $t('category') || 'الفئة' }}</th>
                                            <th>{{ $t('totalSales') || 'إجمالي المبيعات' }}</th>
                                            <th>{{ $t('totalQuantity') || 'إجمالي الكمية' }}</th>
                                            <th>{{ $t('itemCount') || 'عدد المنتجات' }}</th>
                                            <th>{{ $t('orderCount') || 'عدد الطلبات' }}</th>
                                            <th>{{ $t('averageOrderValue') || 'متوسط قيمة الطلب' }}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="category in salesByCategory" :key="category.category">
                                            <td class="report-item-name">
                                                <div class="category-cell">
                                                    <b-icon icon="tags-fill" class="category-icon"></b-icon>
                                                    {{ category.category }}
                                                </div>
                                            </td>
                                            <td class="report-item-price">{{ formatPrice(category.totalSales) }} {{ $t('currency') }}</td>
                                            <td class="report-item-quantity">{{ category.totalQuantity }}</td>
                                            <td class="report-item-quantity">{{ category.itemCount }}</td>
                                            <td class="report-item-quantity">{{ category.orderCount }}</td>
                                            <td class="report-item-price">{{ formatPrice(category.orderCount > 0 ? category.totalSales / category.orderCount : 0) }} {{ $t('currency') }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Sales By Employee -->
                        <div v-if="activeTab === 'byEmployee'" class="report-section">
                            <div class="report-section-intro" v-if="salesByEmployee.length > 0">
                                <div class="report-info-banner">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('salesByEmployeeDescription') || 'مقارنة أداء الموظفين في المبيعات' }}</span>
                                </div>
                            </div>
                            <div class="report-table-container" v-if="salesByEmployee.length > 0">
                                <table class="report-table">
                                    <thead>
                                        <tr>
                                            <th>{{ $t('employeeName') || 'اسم الموظف' }}</th>
                                            <th>{{ $t('totalOrders') || 'إجمالي الطلبات' }}</th>
                                            <th>{{ $t('totalSales') || 'إجمالي المبيعات' }}</th>
                                            <th>{{ $t('totalItemsSold') || 'إجمالي المواد المباعة' }}</th>
                                            <th>{{ $t('averageOrderValue') || 'متوسط قيمة الطلب' }}</th>
                                            <th>{{ $t('itemsPerOrder') || 'مواد لكل طلب' }}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="employee in salesByEmployee" :key="employee.employeeId">
                                            <td class="report-item-name">
                                                <div class="employee-cell">
                                                    <b-icon icon="person-fill" class="employee-icon"></b-icon>
                                                    {{ employee.employeeName }}
                                                </div>
                                            </td>
                                            <td class="report-item-quantity">{{ employee.totalOrders }}</td>
                                            <td class="report-item-price">{{ formatPrice(employee.totalSales) }} {{ $t('currency') }}</td>
                                            <td class="report-item-quantity">{{ employee.totalItemsSold }}</td>
                                            <td class="report-item-price">{{ formatPrice(employee.totalOrders > 0 ? employee.totalSales / employee.totalOrders : 0) }} {{ $t('currency') }}</td>
                                            <td class="report-item-quantity">{{ employee.totalOrders > 0 ? (employee.totalItemsSold / employee.totalOrders).toFixed(2) : 0 }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Sales By Warehouse -->
                        <div v-if="activeTab === 'byWarehouse'" class="report-section">
                            <div class="report-section-intro" v-if="salesByWarehouse.length > 0">
                                <div class="report-info-banner">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('salesByWarehouseDescription') || 'تحليل المبيعات حسب المخزن الذي خُصمت منه الكمية' }}</span>
                                </div>
                            </div>
                            <div class="report-table-container" v-if="salesByWarehouse.length > 0">
                                <table class="report-table">
                                    <thead>
                                        <tr>
                                            <th>{{ $t('warehouseName') || 'اسم المخزن' }}</th>
                                            <th>{{ $t('totalOrders') || 'إجمالي الطلبات' }}</th>
                                            <th>{{ $t('totalSales') || 'إجمالي المبيعات' }}</th>
                                            <th>{{ $t('totalItemsSold') || 'إجمالي المواد المباعة' }}</th>
                                            <th>{{ $t('averageOrderValue') || 'متوسط قيمة الطلب' }}</th>
                                            <th>{{ $t('itemsPerOrder') || 'مواد لكل طلب' }}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr
                                            v-for="row in salesByWarehouse"
                                            :key="row.warehouseId != null ? row.warehouseId : 'none'"
                                        >
                                            <td class="report-item-name">
                                                <div class="employee-cell">
                                                    <b-icon icon="building" class="employee-icon"></b-icon>
                                                    {{ row.warehouseName }}
                                                </div>
                                            </td>
                                            <td class="report-item-quantity">{{ row.totalOrders }}</td>
                                            <td class="report-item-price">{{ formatPrice(row.totalSales) }} {{ $t('currency') }}</td>
                                            <td class="report-item-quantity">{{ row.totalItemsSold }}</td>
                                            <td class="report-item-price">{{ formatPrice(row.totalOrders > 0 ? row.totalSales / row.totalOrders : 0) }} {{ $t('currency') }}</td>
                                            <td class="report-item-quantity">{{ row.totalOrders > 0 ? (row.totalItemsSold / row.totalOrders).toFixed(2) : 0 }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div v-else-if="!show" class="empty-state">
                                <b-icon icon="building" class="empty-icon"></b-icon>
                                <p>{{ $t('noWarehouseSalesData') || 'لا توجد مبيعات حسب المخازن في الفترة المحددة' }}</p>
                            </div>
                        </div>

                        <!-- Low Stock Items -->
                        <div v-if="activeTab === 'lowStock'" class="report-section">
                            <div v-if="lowStockItems.length > 0" class="app-overview-grid reports-orders-summary">
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--warning"><b-icon icon="exclamation-triangle-fill"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ lowStockItems.filter(item => item.currentQuantity > 0 && item.currentQuantity <= item.threshold).length }}</div>
                                        <div class="app-overview-stat-label">{{ $t('lowStockCount') || 'منتجات قليلة المخزون' }}</div>
                                    </div>
                                </div>
                                <div class="app-overview-stat">
                                    <span class="app-overview-stat-icon app-overview-stat-icon--danger"><b-icon icon="x-circle-fill"></b-icon></span>
                                    <div>
                                        <div class="app-overview-stat-value">{{ lowStockItems.filter(item => item.currentQuantity === 0).length }}</div>
                                        <div class="app-overview-stat-label">{{ $t('outOfStockCount') || 'منتجات منتهية' }}</div>
                                    </div>
                                </div>
                            </div>
                            <div class="report-table-container">
                                <table class="report-table">
                                    <thead>
                                        <tr>
                                            <th>{{ $t('itemName') || 'اسم المنتج' }}</th>
                                            <th>{{ $t('itemCode') || 'الكود' }}</th>
                                            <th>{{ $t('category') || 'الفئة' }}</th>
                                            <th>{{ $t('currentQuantity') || 'الكمية الحالية' }}</th>
                                            <th>{{ $t('threshold') || 'الحد الأدنى' }}</th>
                                            <th>{{ $t('status') || 'الحالة' }}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="item in lowStockItems" :key="item.itemId" :class="{ 'low-stock-row': item.currentQuantity === 0 }">
                                            <td class="report-item-name">{{ item.itemName }}</td>
                                            <td class="report-item-code">{{ item.itemCode }}</td>
                                            <td class="report-item-name">{{ item.category || '-' }}</td>
                                            <td class="report-item-quantity" :class="{ 'quantity-out': item.currentQuantity === 0, 'quantity-low': item.currentQuantity > 0 && item.currentQuantity <= item.threshold }">
                                                {{ item.currentQuantity }}
                                            </td>
                                            <td class="report-item-quantity">{{ item.threshold }}</td>
                                            <td class="report-item-status">
                                                <span class="status-badge" :class="getStockStatusClass(item)">
                                                    <b-icon :icon="getStockStatusIcon(item)" class="status-icon"></b-icon>
                                                    {{ getStockStatusText(item) }}
                                                </span>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                      </div>
                    </div>
                </div>
            </div>

            <!-- View Items Modal -->
            <b-modal id="modal-itemList" :title="$t('items')" hide-header hide-footer class="users-modal" size="xl" scrollable>
                <div class="modal-content-wrapper">
                    <!-- <h2 class="modal-title">{{ $t('items') }}</h2> -->
                    
                    <!-- Invoice Header - POS Printer Optimized -->
                    <div id="print" class="report-print-container">
                        <div class="bill-container">
                            <!-- Header -->
                            <div class="bill-header">
                                <div class="bill-logo-section">
                                    <img
                                        v-if="commercialUserInfo.logo"
                                        :src="commercialUserInfo.logo"
                                        alt="logo"
                                        class="bill-logo-img"
                                    />
                                    <img
                                        v-else
                                        src="../assets/logo.png"
                                        alt="logo"
                                        class="bill-logo-img"
                                    />
                                </div>
                                <h2 class="bill-store-name">{{ commercialUserInfo.storeName || 'LiteCashier' }}</h2>
                                <p class="bill-store-subtitle">{{ $t('app-name') || 'نظام نقطة البيع' }}</p>
                            </div>

                            <!-- Order Info -->
                            <div class="bill-info-section">
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('invoice_number') }}:</span>
                                    <span class="bill-info-value" v-if="order">{{ order.orderCode }}</span>
                                </div>
                                <div class="bill-barcode-section" v-if="order && order.orderCode">
                                    <vue-barcode
                                        tag="img"
                                        class="bill-barcode-img"
                                        :value="order.orderCode.toString()"
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
                                    <span class="bill-info-label">{{ $t('from_date') }}:</span>
                                    <span class="bill-info-value" v-if="order">{{ formatDate(order.insertDate) }}</span>
                                </div>
                                <div class="bill-info-row" v-if="order && order.paymentMethod">
                                    <span class="bill-info-label">{{ $t('paymentMethod') }}:</span>
                                    <span class="bill-info-value">{{ getPaymentMethodText(order.paymentMethod) }}</span>
                                </div>
                                <div class="bill-info-row" v-if="order">
                                    <span class="bill-info-label">{{ $t('priceModeLabel') || 'نوع السعر' }}:</span>
                                    <span class="bill-info-value">{{ order.isWholesale ? ($t('wholesalePriceMode') || 'جملة') : ($t('retailPriceMode') || 'مفرد') }}</span>
                                </div>
                                <div class="bill-info-row" v-if="order && order.orderType">
                                    <span class="bill-info-label">{{ $t('orderType') }}:</span>
                                    <span class="bill-info-value">{{ getOrderTypeText(order.orderType) }}</span>
                                </div>
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('employeeLabel') }}:</span>
                                    <span class="bill-info-value">{{ orderEmployeeName }}</span>
                                </div>
                            </div>

                            <!-- Divider -->
                            <div class="bill-divider"></div>

                            <!-- Items Table -->
                            <div class="bill-items-section">
                                <table class="bill-items-table">
                                    <thead>
                                        <tr>
                                            <th class="bill-item-name-col">{{ $t('item_name_label') }}</th>
                                            <th class="bill-item-qty-col">{{ $t('quantity_label') }}</th>
                                            <th class="bill-item-price-col">{{ $t('selling_price_label') }}</th>
                                            <th class="bill-item-total-col">{{ $t('total_label') }}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="(item, index) in customerOrderItemsWithTotalPrice" :key="index">
                                            <td class="bill-item-name">
                                                {{ item.item?.name || '—' }}
                                                <span v-if="hasDiscount(item)" class="bill-discount-badge">خصم</span>
                                            </td>
                                            <td class="bill-item-qty">{{ item.quantity }}</td>
                                            <td class="bill-item-price">
                                                <span v-if="hasDiscount(item)" class="bill-price-discounted">
                                                    <span class="bill-original-price">{{ formatPrice(item.item.sellingPrice) }}</span>
                                                    <span class="bill-discount-price">{{ formatPrice(getSellingPrice(item)) }}</span>
                                                </span>
                                                <span v-else>{{ formatPrice(getSellingPrice(item)) }}</span>
                                            </td>
                                            <td class="bill-item-total">{{ formatPrice(item.totalPrice) }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>

                            <!-- Divider -->
                            <div class="bill-divider"></div>

                            <!-- Summary -->
                            <div class="bill-summary-section">
                                <div class="bill-summary-row">
                                    <span class="bill-summary-label">{{ $t('count') }}:</span>
                                    <span class="bill-summary-value">{{ reportInvoiceItemCount }} {{ $t('items') }}</span>
                                </div>
                                <div class="bill-summary-row" v-if="Number(order?.discountAmount || 0) > 0">
                                    <span class="bill-summary-label">{{ $t('discountLabel') }}:</span>
                                    <span class="bill-summary-value">− {{ formatPrice(order.discountAmount) }} {{ $t('currency') }}</span>
                                </div>
                                <div class="bill-summary-row bill-summary-total">
                                    <span class="bill-summary-label">{{ $t('total') }}:</span>
                                    <span class="bill-summary-value">{{ formattedNumber }} {{ $t('currency') }}</span>
                                </div>
                            </div>

                            <!-- Footer -->
                            <div class="bill-footer">
                                <p class="bill-footer-text">{{ $t('thankYouMessage') || 'شكراً لزيارتك' }}</p>
                                <p class="bill-footer-credit">نظام لايت كاشير - برمجة وتصميم عمار الاصفر</p>
                                <p class="bill-footer-credit-phone">07830200030</p>
                            </div>
                        </div>
                    </div>

                    <!-- Modal Actions -->
                    <div class="users-form-actions report-invoice-actions" style="margin-top: 1.5rem;">
                        <select
                            v-if="activeCheckoutPrinters.length > 0"
                            v-model="selectedManagedPrinterId"
                            class="users-search-input reports-filter-select report-invoice-printer-select"
                            @change="onManagedPrinterChange"
                        >
                            <option
                                v-for="printer in activeCheckoutPrinters"
                                :key="printer.id"
                                :value="printer.id"
                            >
                                {{ printer.name }}{{ printer.isMain ? ` (${$t('mainPrinter') || 'رئيسية'})` : '' }}
                            </option>
                        </select>
                        <button
                            class="users-form-submit-button"
                            :disabled="printingInvoice"
                            @click="printReportInvoice()"
                        >
                            <b-spinner small v-if="printingInvoice" class="me-2"></b-spinner>
                            <b-icon v-else icon="printer-fill" class="me-2"></b-icon>
                            {{ $t('print') }}
                        </button>
                        <button type="button" class="users-form-cancel-button" @click="closeModel('modal-itemList')">
                            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                            {{ $t('close') }}
                        </button>
                    </div>
                </div>
            </b-modal>

            <!-- Edit Order Modal -->
            <b-modal id="modal-edit-order" :title="$t('editOrder') || 'تعديل الفاتورة'" hide-header hide-footer class="users-modal edit-order-modal" size="xl" scrollable>
                <div class="modal-content-wrapper" v-if="editOrderData">
                    <div class="modal-title-row">
                        <span class="modal-title-icon">
                            <b-icon icon="pencil-square"></b-icon>
                        </span>
                        <h2 class="modal-title">{{ $t('editOrder') || 'تعديل الفاتورة' }}</h2>
                    </div>

                    <div class="edit-order-section">
                        <h3 class="edit-order-section-title">{{ $t('orderInfo') || 'معلومات الطلب' }}</h3>
                        <div class="edit-order-form-grid">
                            <div class="edit-order-form-group">
                                <label class="edit-order-label">{{ $t('invoice_number') }}</label>
                                <input type="text" :value="editOrderData.orderCode" disabled class="edit-order-input" />
                            </div>
                            <div class="edit-order-form-group">
                                <label class="edit-order-label">{{ $t('paymentMethod') }}</label>
                                <select v-model="editOrderForm.paymentMethod" class="edit-order-input">
                                    <option value="Cash">{{ $t('cash') || 'نقد' }}</option>
                                    <option value="Card">{{ $t('card') || 'بطاقة' }}</option>
                                    <option value="Credit">{{ $t('credit') || 'دفع لاحق' }}</option>
                                </select>
                            </div>
                            <div class="edit-order-form-group edit-order-form-group--wide">
                                <label class="edit-order-label">{{ $t('orderDiscount') || 'خصم الطلب' }}</label>
                                <div class="edit-order-discount-row">
                                    <select v-model="editOrderForm.discountType" class="edit-order-input">
                                        <option :value="null">{{ $t('noDiscount') || 'بدون خصم' }}</option>
                                        <option value="amount">{{ $t('discountAmount') || 'مبلغ' }}</option>
                                        <option value="percentage">{{ $t('discountPercent') || 'نسبة' }}</option>
                                    </select>
                                    <input
                                        v-model.number="editOrderForm.discountValue"
                                        type="number"
                                        min="0"
                                        class="edit-order-input"
                                        :placeholder="$t('discountValuePlaceholder') || 'قيمة الخصم'"
                                        :disabled="!editOrderForm.discountType"
                                    />
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="edit-order-section">
                        <div class="edit-order-section-header">
                            <h3 class="edit-order-section-title">{{ $t('orderItems') || 'عناصر الطلب' }}</h3>
                            <button type="button" class="edit-order-add-item-btn" @click="showAddItemModal">
                                <b-icon icon="plus-circle-fill" class="me-2"></b-icon>
                                {{ $t('addItem') || 'إضافة مادة' }}
                            </button>
                        </div>
                        <div class="edit-order-items-list">
                            <div v-for="(item, index) in editOrderForm.items" :key="index" class="edit-order-item">
                                <div class="edit-order-item-info">
                                    <h4 class="edit-order-item-name">{{ item.name }}</h4>
                                    <div class="edit-order-item-details">
                                        <span v-if="item.code" class="edit-order-item-code">{{ $t('code') || 'الكود' }}: {{ item.code }}</span>
                                        <span class="edit-order-item-price">{{ formatPrice(item.price) }} {{ $t('currency') }}</span>
                                        <span class="edit-order-item-line-total">
                                            {{ formatPrice((item.price || 0) * (item.quantity || 0)) }} {{ $t('currency') }}
                                        </span>
                                    </div>
                                </div>
                                <div class="edit-order-item-controls">
                                    <div class="edit-order-item-quantity">
                                        <button type="button" class="edit-order-quantity-btn" @click="decreaseEditItemQuantity(index)">
                                            <b-icon icon="dash"></b-icon>
                                        </button>
                                        <input type="number" v-model.number="item.quantity" min="1" class="edit-order-quantity-input" />
                                        <button type="button" class="edit-order-quantity-btn" @click="increaseEditItemQuantity(index)">
                                            <b-icon icon="plus"></b-icon>
                                        </button>
                                    </div>
                                    <button type="button" class="edit-order-remove-btn" @click="removeEditItem(index)" :title="$t('delete')">
                                        <b-icon icon="trash-fill"></b-icon>
                                    </button>
                                </div>
                            </div>
                            <div v-if="editOrderForm.items.length === 0" class="edit-order-empty">
                                <b-icon icon="inbox" class="edit-order-empty-icon"></b-icon>
                                <p>{{ $t('noItems') || 'لا توجد عناصر' }}</p>
                            </div>
                        </div>

                        <div class="edit-order-totals">
                            <div class="edit-order-total-row">
                                <span class="edit-order-total-label">{{ $t('subtotal') || 'المجموع قبل الخصم' }}</span>
                                <span class="edit-order-total-value">{{ formatPrice(editOrderTotal) }} {{ $t('currency') }}</span>
                            </div>
                            <div v-if="editOrderDiscountAmount > 0" class="edit-order-total-row edit-order-total-row--discount">
                                <span class="edit-order-total-label">{{ $t('discountLabel') }} ({{ editOrderDiscountPreviewLabel }})</span>
                                <span class="edit-order-total-value">− {{ formatPrice(editOrderDiscountAmount) }} {{ $t('currency') }}</span>
                            </div>
                            <div class="edit-order-total-row edit-order-total-row--grand">
                                <span class="edit-order-total-label">{{ $t('total') || 'المجموع' }}</span>
                                <span class="edit-order-total-value">{{ formatPrice(editOrderFinalTotal) }} {{ $t('currency') }}</span>
                            </div>
                        </div>
                    </div>

                    <div class="users-form-actions">
                        <button type="button" class="users-form-submit-button" @click="updateOrder" :disabled="loadingUpdateOrder">
                            <b-spinner small v-if="loadingUpdateOrder" class="me-2"></b-spinner>
                            <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
                            {{ $t('save') || 'حفظ' }}
                        </button>
                        <button type="button" class="users-form-cancel-button" @click="closeEditOrderModal">
                            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                            {{ $t('cancel') || 'إلغاء' }}
                        </button>
                    </div>
                </div>
            </b-modal>

            <b-modal id="modal-add-item" hide-header hide-footer class="users-modal" size="lg" scrollable>
                <div class="modal-content-wrapper">
                    <div class="modal-title-row">
                        <span class="modal-title-icon">
                            <b-icon icon="plus-circle-fill"></b-icon>
                        </span>
                        <h2 class="modal-title">{{ $t('addItem') || 'إضافة مادة' }}</h2>
                    </div>
                    <div class="app-search-wrap app-search-wrap--wide edit-order-search-wrap">
                        <b-icon icon="search" class="app-search-icon"></b-icon>
                        <input
                            v-model="itemSearchQuery"
                            type="search"
                            class="app-search-input"
                            :placeholder="$t('search') || 'بحث...'"
                            autocomplete="off"
                            @input="searchItems"
                        />
                    </div>
                    <div class="edit-order-items-search-results">
                        <div
                            v-for="item in availableItems"
                            :key="item.id"
                            class="edit-order-search-item"
                            @click="addItemToEditOrder(item)"
                        >
                            <div class="edit-order-search-item-info">
                                <h4>{{ item.name }}</h4>
                                <span v-if="item.code" class="edit-order-search-item-code">{{ item.code }}</span>
                            </div>
                            <span class="edit-order-search-item-price">{{ formatPrice(item.sellingPrice) }} {{ $t('currency') }}</span>
                        </div>
                        <div v-if="itemSearchQuery.length >= 2 && availableItems.length === 0" class="edit-order-empty">
                            <b-icon icon="search" class="edit-order-empty-icon"></b-icon>
                            <p>{{ $t('noResults') || 'لا توجد نتائج' }}</p>
                        </div>
                        <div v-else-if="!itemSearchQuery || itemSearchQuery.length < 2" class="edit-order-empty edit-order-empty--hint">
                            <p>{{ $t('editOrderSearchHint') || 'اكتب حرفين على الأقل للبحث عن مادة' }}</p>
                        </div>
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
import { HTTP } from '../http/api.js';
import { formatBusinessDateTime } from '@/utils/formatBusinessDateTime.js';
import { mergeCartLinesForOrderPayload } from '@/utils/mergeCartLines.js';
import posPrintMixin from '@/mixins/posPrintMixin.js';
export default {
    name: "OrdersView",
    mixins: [posPrintMixin],
    components: {
        AppHeader,
        ClockVue,
        "vue-barcode": VueBarcode,

    },
    data() {
        return {
            show: false,
            printingInvoice: false,
            commercialUserInfo: {
                storeName: 'LiteCashier',
                logo: null,
                printInvoiceFormat: 'Pos',
            },
            orderForSend: {
                orderCode: "",
                paymentMethod: "Cash",
            },
            activeTab: 'orders',
            Orders: [],
            ordersSummary: {
                totalOrders: 0,
                totalSubTotal: 0,
                totalDiscount: 0,
                totalSales: 0,
                totalItemsSold: 0,
                averageOrderValue: 0,
            },
            pageNumber: 1,
            totalOrders: 0,
            pageSize: 18,
            search: {
                info: "",
                startDate: "",
                endDate: "",
                paymentMethod: "",
            },
            reportFilters: {
                startDate: "",
                endDate: "",
            },
            productSalesFilters: {
                tag: "",
                info: "",
            },
            reportTags: [],
            productSalesItems: [],
            productSalesSummary: {
                totalQuantitySold: 0,
                totalSales: 0,
                totalDistinctItems: 0,
                totalRemainingQuantity: 0,
                itemsWithSales: 0,
            },
            totalCardOrders: 0,
            userInfo: {},
            customerOrderItem: [],

            itemId: '',
            order: '',
            totaPrice: '',
            
            // Advanced Reports Data
            profitReport: {},
            topSellingItems: [],
            topSellingItemsSummary: {
                totalQuantitySold: 0,
                totalSales: 0,
                totalDistinctItems: 0,
                totalOrders: 0,
            },
            salesByCategory: [],
            salesByEmployee: [],
            salesByWarehouse: [],
            lowStockItems: [],
            lowStockThreshold: 10,
            exportingExcel: false,
            editOrderData: null,
            editOrderForm: {
                paymentMethod: 'Cash',
                discountType: null,
                discountValue: null,
                items: [],
            },
            availableItems: [],
            itemSearchQuery: '',
            itemSearchTimer: null,
            loadingUpdateOrder: false,
            
            // Search debounce timer
            searchTimer: null,
        };
    },
    computed: {
        ordersReportPeriodColumn() {
            return this.formatReportPeriod(this.search.startDate, this.search.endDate);
        },
        ordersTableFields() {
            return [
                { key: "orderCode", label: this.$t("invoice_number") || "رقم الفاتورة", sortable: true },
                { key: "insertDate", label: this.$t("date") || "التاريخ", sortable: true },
                { key: "paymentMethod", label: this.$t("paymentMethod") || "طريقة الدفع", sortable: true },
                { key: "priceMode", label: this.$t("priceModeLabel") || "نوع السعر", sortable: false },
                { key: "orderType", label: this.$t("orderType") || "نوع الطلب", sortable: true },
                { key: "itemsCount", label: this.$t("items_count") || "عدد العناصر", sortable: true },
                { key: "discountAmount", label: this.$t("discountLabel") || "الخصم", sortable: true },
                { key: "totalAmount", label: this.$t("invoice_amount") || "مبلغ الفاتورة", sortable: true },
                { key: "createdByUsername", label: this.$t("employeeLabel") || "الحساب", sortable: true },
                { key: "actions", label: this.$t("actions") || "الإجراءات", class: "text-center" },
            ];
        },
        hasActiveFilters() {
            return !!(
                this.search.info ||
                this.search.startDate ||
                this.search.endDate ||
                this.search.paymentMethod
            );
        },
        hasAdvancedFilters() {
            return !!(
                this.reportFilters.startDate ||
                this.reportFilters.endDate ||
                (this.activeTab === "productSales" &&
                    (this.productSalesFilters.tag ||
                        (this.productSalesFilters.info || "").trim()))
            );
        },
        hasReportFilters() {
            if (this.activeTab === "orders") return this.hasActiveFilters;
            if (this.activeTab === "lowStock") return Number(this.lowStockThreshold) !== 10;
            return this.hasAdvancedFilters;
        },
        reportsFiltersHint() {
            const map = {
                orders: this.$t("ordersFiltersHint") || "تصفية الفواتير بالتاريخ أو طريقة الدفع أو رقم الفاتورة",
                profit: this.$t("dateFiltersHint") || "حدد فترة التقرير",
                topItems: this.$t("dateFiltersHint") || "حدد فترة التقرير",
                productSales: this.$t("productSalesFiltersHint") || "فلترة حسب التاريخ والقسم واسم المنتج",
                byCategory: this.$t("dateFiltersHint") || "حدد فترة التقرير",
                byEmployee: this.$t("dateFiltersHint") || "حدد فترة التقرير",
                byWarehouse: this.$t("dateFiltersHint") || "حدد فترة التقرير",
                lowStock: this.$t("lowStockFiltersHint") || "حد الكمية لعرض المنتجات القليلة أو المنتهية",
            };
            return map[this.activeTab] || (this.$t("filters") || "فلاتر التقرير");
        },
        formattedNumber() {
            return this.totaPrice.toLocaleString()
        },
        reportInvoiceItemCount() {
            return (this.customerOrderItem || []).reduce(
                (sum, item) => sum + (Number(item.quantity) || 0),
                0
            );
        },
        orderEmployeeName() {
            return (
                this.order?.createdByUsername ||
                this.userInfo?.name ||
                this.userInfo?.fullName ||
                '—'
            );
        },

        customerOrderItemField() {
            const lang = this.$i18n.locale
            if (!lang) { return [] }
            return [
                {
                    key: "item.name",
                    label: this.$i18n.t('item_name_label'),
                },
                {
                    key: "item.purchasingPrice",
                    label: this.$i18n.t('purchase_price_label'),
                },
                {
                    key: "item.sellingPrice",
                    label: this.$i18n.t('selling_price_label'),
                },
                {
                    key: "quantity",
                    label: this.$i18n.t('quantity_label'),
                },
                {
                    key: "totalPrice",
                    label: this.$i18n.t('total_label'),
                },

            ];
        },

        customerOrderItemsWithTotalPrice() {
            return this.customerOrderItem.map(item => {
                const sellingPrice = this.getSellingPrice(item);
                return {
                    ...item,
                    totalPrice: item.quantity * sellingPrice,
                };
            });
        },
        editOrderTotal() {
            return this.editOrderForm.items.reduce((sum, item) => sum + (item.price || 0) * (item.quantity || 0), 0);
        },
        editOrderDiscountAmount() {
            const rawValue = Number(this.editOrderForm.discountValue) || 0;
            if (!this.editOrderForm.discountType || rawValue <= 0) return 0;
            if (this.editOrderForm.discountType === 'percentage') {
                return Math.min(this.editOrderTotal, (this.editOrderTotal * Math.min(rawValue, 100)) / 100);
            }
            return Math.min(this.editOrderTotal, rawValue);
        },
        editOrderFinalTotal() {
            return Math.max(this.editOrderTotal - this.editOrderDiscountAmount, 0);
        },
        editOrderDiscountPreviewLabel() {
            if (!this.editOrderForm.discountType || !(Number(this.editOrderForm.discountValue) > 0)) {
                return this.$t("noDiscount") || "بدون خصم";
            }
            if (this.editOrderForm.discountType === "percentage") {
                return `${Math.min(Number(this.editOrderForm.discountValue) || 0, 100)}%`;
            }
            return `${this.formatPrice(this.editOrderForm.discountValue)} ${this.$t("currency")}`;
        },
    },
    watch: {
        customerOrderItem: {
            handler() {
                this.totaPrice = 0;
                this.customerOrderItem.forEach((item) => {
                    const sellingPrice = this.getSellingPrice(item);
                    this.totaPrice += item.quantity * sellingPrice;
                });
            },
            deep: true,
        },
        search: {
            handler() {
                // Clear previous timer
                if (this.searchTimer) {
                    clearTimeout(this.searchTimer);
                }
                
                // Set new timer - wait 500ms after user stops typing
                this.searchTimer = setTimeout(() => {
                    this.pageNumber = 1;
                    this.GetAllOrders();
                }, 500);
            },
            deep: true,
        },

        pageNumber() {
            this.GetAllOrders();
        },
    },

    mounted() {
        this.GetAllOrders();
        this.userInfo = JSON.parse(localStorage.getItem('info'));
        this.loadCommercialUserInfo();
        this.loadManagedPrinters();
    },
    
    beforeDestroy() {
        // Clear search timer to prevent memory leaks
        if (this.searchTimer) {
            clearTimeout(this.searchTimer);
        }
    },

    methods: {
        formatReportPeriod(start, end) {
            if (start && end) return `${start} — ${end}`;
            if (start) return `${this.$t("from_date")}: ${start}`;
            if (end) return `${this.$t("to_date")}: ${end}`;
            return "";
        },
        refreshReports() {
            if (this.activeTab === "orders") {
                this.GetAllOrders();
            } else if (this.activeTab === "lowStock") {
                this.loadLowStockItems();
            } else {
                this.loadAdvancedReport();
            }
        },
        clearFilters() {
            this.search = {
                info: "",
                startDate: "",
                endDate: "",
                paymentMethod: "",
            };
            this.pageNumber = 1;
            this.GetAllOrders();
        },
        clearAdvancedFilters() {
            this.reportFilters = {
                startDate: "",
                endDate: "",
            };
            this.productSalesFilters = {
                tag: "",
                info: "",
            };
            this.loadAdvancedReport();
        },
        clearCurrentTabFilters() {
            if (this.activeTab === "orders") {
                this.clearFilters();
                return;
            }
            if (this.activeTab === "lowStock") {
                this.lowStockThreshold = 10;
                this.loadLowStockItems();
                return;
            }
            this.clearAdvancedFilters();
        },
        ensureReportTags() {
            if (this.reportTags.length) return;
            HTTP.get("Admin/GetTags?pageNumber=0&pageSize=10000")
                .then((response) => {
                    this.reportTags = response.data?.data?.items || [];
                })
                .catch(() => {
                    this.reportTags = [];
                });
        },
        hasDiscount(item) {
            if (this.order?.isWholesale) return false;
            return item.item && 
                   item.item.disCountPrice && 
                   item.item.disCountPrice > 0 && 
                   item.item.disCountPrice !== item.item.sellingPrice &&
                   Number(item.sellingPrice) === Number(item.item.disCountPrice);
        },
        getSellingPrice(item) {
            if (item.sellingPrice != null && item.sellingPrice !== undefined) {
                return Number(item.sellingPrice) || 0;
            }
            if (this.hasDiscount(item)) {
                return item.item.disCountPrice;
            }
            return item.item?.sellingPrice || 0;
        },
        getRankClass(index) {
            if (index === 0) return 'rank-gold';
            if (index === 1) return 'rank-silver';
            if (index === 2) return 'rank-bronze';
            return '';
        },
        getStockStatusClass(item) {
            if (item.currentQuantity === 0) return 'status-out';
            if (item.currentQuantity <= item.threshold) return 'status-low';
            return 'status-ok';
        },
        getStockStatusIcon(item) {
            if (item.currentQuantity === 0) return 'x-circle-fill';
            if (item.currentQuantity <= item.threshold) return 'exclamation-triangle-fill';
            return 'check-circle-fill';
        },
        getStockStatusText(item) {
            if (item.currentQuantity === 0) return this.$t('outOfStock') || 'منتهي';
            if (item.currentQuantity <= item.threshold) return this.$t('lowStock') || 'قليل';
            return this.$t('inStock') || 'متوفر';
        },
        getPaymentMethodText(method) {
            if (!method) return '-';
            const methods = {
                'Cash': this.$t('cash') || 'نقدي',
                'Card': this.$t('card') || 'بطاقة',
                'Credit': this.$t('credit') || 'آجل',
                'BankTransfer': this.$t('bankTransfer') || 'تحويل بنكي'
            };
            return methods[method] || method;
        },
        getPaymentMethodIcon(method) {
            if (!method) return 'cash-stack';
            const icons = {
                'Cash': 'cash-stack',
                'Card': 'credit-card',
                'Credit': 'clock-history',
                'BankTransfer': 'bank'
            };
            return icons[method] || 'cash-stack';
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
        getOrderTypeIcon(type) {
            if (!type) return 'house-door';
            const icons = {
                'DineIn': 'house-door',
                'Takeaway': 'bag',
                'Delivery': 'truck'
            };
            return icons[type] || 'house-door';
        },
        formatDate(dateTime) {
            return formatBusinessDateTime(dateTime);
        },
        formatPrice(price) {
            if (price) {
                return price.toLocaleString("en-EG");
            }
            return "0";
        },
        loadCommercialUserInfo() {
            HTTP.get("Admin/CommercialUserInfo")
                .then((response) => {
                    if (response.data && response.data.data) {
                        const d = response.data.data;
                        const format =
                            String(d.printInvoiceFormat || d.PrintInvoiceFormat || "Pos").toUpperCase() ===
                            "A4"
                                ? "A4"
                                : "Pos";
                        this.commercialUserInfo = {
                            storeName: d.storeName || d.StoreName || "LiteCashier",
                            logo: d.logo || d.Logo || null,
                            printInvoiceFormat: format,
                        };
                        localStorage.setItem("printInvoiceFormat", format);
                    }
                })
                .catch((error) => {
                    console.error("Error loading commercial user info:", error);
                    this.commercialUserInfo = {
                        storeName: "LiteCashier",
                        logo: null,
                        printInvoiceFormat:
                            localStorage.getItem("printInvoiceFormat") === "A4" ? "A4" : "Pos",
                    };
                });
        },
        ensureOrderCodeForPrint() {
            const fromOrder = String(this.order?.orderCode || "").trim();
            if (fromOrder && fromOrder !== "---") {
                this.orderForSend.orderCode = fromOrder;
                return fromOrder;
            }
            const existing = String(this.orderForSend?.orderCode || "").trim();
            if (existing && existing !== "---") {
                return existing;
            }
            this.orderForSend.orderCode = Math.floor(Math.random() * 1000000000)
                .toString()
                .padStart(9, "0");
            return this.orderForSend.orderCode;
        },
        prepareOrderForPrint(order) {
            const items = (order?.customerOrderItem || []).filter((item) => !item.isDeleted);
            this.customerOrderItem = items;
            this.order = order || '';
            this.orderForSend = {
                ...this.orderForSend,
                orderCode: order?.orderCode || "",
                paymentMethod: order?.paymentMethod || "Cash",
            };
            return items;
        },
        async printReportInvoice() {
            if (this.printingInvoice) return;
            if (!this.customerOrderItem || this.customerOrderItem.length === 0) {
                this.$notify.error(this.$t("emptyCartMessage") || this.$t("emptyCart") || "لا توجد عناصر للطباعة", {
                    position: "top-right",
                    timeout: 2500,
                    maxToasts: 1,
                });
                return;
            }

            this.printingInvoice = true;
            try {
                this.ensureOrderCodeForPrint();
                await this.ensurePrintPrintersReady();
                await this.$nextTick();

                const htmlContent = await this.getReceiptHtmlContent();
                if (!htmlContent) {
                    this.notifyPrintError(this.$t("printError") || "تعذرت الطباعة");
                    return;
                }

                const printerId = this.resolvePrintPrinterId();
                const printer = this.findManagedPrinter(printerId);

                if (printerId && printer) {
                    try {
                        const apiOk = await this.printViaApi(printerId, htmlContent);
                        if (apiOk) {
                            this.notifyPrintSuccess();
                            return;
                        }
                    } catch (apiError) {
                        console.warn("[reports print] API failed, trying print server:", apiError);
                    }

                    const directOk = await this.printViaPrintServer(htmlContent, printer);
                    if (directOk) {
                        this.notifyPrintSuccess();
                        return;
                    }
                }

                await this.browserPrintReceipt(htmlContent);
                this.notifyPrintSuccess();
            } catch (error) {
                console.error("printReportInvoice error:", error);
                this.notifyPrintError(error.message);
            } finally {
                this.printingInvoice = false;
            }
        },
        async printOrderFromRow(order) {
            const items = this.prepareOrderForPrint(order);
            if (!items.length) {
                this.$notify.error(this.$t("emptyCartMessage") || this.$t("emptyCart") || "لا توجد عناصر للطباعة", {
                    position: "top-right",
                    timeout: 2500,
                    maxToasts: 1,
                });
                return;
            }
            this.$bvModal.show("modal-itemList");
            await this.$nextTick();
            await this.printReportInvoice();
        },

        showItemsModel(items, order) {
            this.customerOrderItem = (items || []).filter((item) => !item.isDeleted);
            this.order = order;
            this.orderForSend = {
                ...this.orderForSend,
                orderCode: order?.orderCode || "",
                paymentMethod: order?.paymentMethod || "Cash",
            };
            this.$bvModal.show("modal-itemList");
        },

        getItemInfo(item) {
            this.editForm = item;
            this.$bvModal.show("modal-editItem");
        },



        closeModel(id) {
            this.$bvModal.hide(id);
        },


        GetAllOrders() {
            this.show = true;
            const params = new URLSearchParams();
            params.append('pageNumber', (this.pageNumber - 1).toString());
            params.append('pageSize', this.pageSize.toString());
            if (this.search.info) params.append('info', this.search.info);
            if (this.search.startDate) params.append('startDate', this.search.startDate);
            if (this.search.endDate) params.append('endDate', this.search.endDate);
            if (this.search.paymentMethod) params.append('paymentMethod', this.search.paymentMethod);
            HTTP.get(`Admin/GetOrders?${params.toString()}`)
                .then((response) => {
                    this.Orders = response.data.data.items;
                    this.totalOrders = response.data.data.totalItems;
                    const summary = response.data.data.summary;
                    this.ordersSummary = {
                        totalOrders: summary?.totalOrders ?? 0,
                        totalSubTotal: summary?.totalSubTotal ?? 0,
                        totalDiscount: summary?.totalDiscount ?? 0,
                        totalSales: summary?.totalSales ?? 0,
                        totalItemsSold: summary?.totalItemsSold ?? 0,
                        averageOrderValue: summary?.averageOrderValue ?? 0,
                    };
                    this.show = false;
                })
                .catch(() => {
                    this.show = false;
                });
        },

        editOrder(order) {
            this.editOrderData = order;
            this.editOrderForm = {
                paymentMethod: order.paymentMethod || 'Cash',
                discountType: order.discountType || null,
                discountValue: order.discountValue ?? null,
                items: order.customerOrderItem ? order.customerOrderItem.filter((item) => !item.isDeleted).map(item => ({
                    id: item.item?.id || item.itemId,
                    name: item.item?.name || '',
                    code: item.item?.code || '',
                    price: item.sellingPrice,
                    quantity: item.quantity,
                    itemId: item.itemId,
                })) : [],
            };
            this.$bvModal.show('modal-edit-order');
        },
        closeEditOrderModal() {
            this.editOrderData = null;
            this.editOrderForm = { paymentMethod: 'Cash', discountType: null, discountValue: null, items: [] };
            this.$bvModal.hide('modal-edit-order');
        },
        increaseEditItemQuantity(index) {
            if (this.editOrderForm.items[index]) this.editOrderForm.items[index].quantity++;
        },
        decreaseEditItemQuantity(index) {
            if (this.editOrderForm.items[index] && this.editOrderForm.items[index].quantity > 1) {
                this.editOrderForm.items[index].quantity--;
            }
        },
        removeEditItem(index) {
            this.editOrderForm.items.splice(index, 1);
        },
        showAddItemModal() {
            this.itemSearchQuery = '';
            this.availableItems = [];
            this.$bvModal.show('modal-add-item');
        },
        searchItems() {
            clearTimeout(this.itemSearchTimer);
            this.itemSearchTimer = setTimeout(() => {
                if (this.itemSearchQuery && this.itemSearchQuery.length >= 2) {
                    HTTP.get(`Admin/GetItems?pageNumber=0&pageSize=20&info=${encodeURIComponent(this.itemSearchQuery)}`)
                        .then((response) => { this.availableItems = response.data.data.items || []; })
                        .catch(() => { this.availableItems = []; });
                } else {
                    this.availableItems = [];
                }
            }, 300);
        },
        addItemToEditOrder(item) {
            const existingItem = this.editOrderForm.items.find(i => i.id === item.id);
            if (existingItem) {
                existingItem.quantity++;
            } else {
                let price;
                if (this.editOrderData?.isWholesale) {
                    const wholesale = Number(item.wholesalePrice) || 0;
                    price = wholesale > 0 ? wholesale : item.sellingPrice;
                } else {
                    price = item.disCountPrice > 0 && item.disCountPrice < item.sellingPrice ? item.disCountPrice : item.sellingPrice;
                }
                this.editOrderForm.items.push({ id: item.id, name: item.name, code: item.code, price, quantity: 1, itemId: item.id });
            }
            this.$bvModal.hide('modal-add-item');
        },
        async updateOrder() {
            if (!this.editOrderData || this.editOrderForm.items.length === 0) {
                this.$notify.error(this.$i18n.t('emptyCartMessage') || 'السلة فارغة', { position: 'top-right', timeout: 3000 });
                return;
            }
            this.loadingUpdateOrder = true;
            try {
                const request = {
                    paymentMethod: this.editOrderForm.paymentMethod,
                    isWholesale: !!this.editOrderData.isWholesale,
                    discountType: this.editOrderDiscountAmount > 0 ? this.editOrderForm.discountType : null,
                    discountValue: this.editOrderDiscountAmount > 0 ? (Number(this.editOrderForm.discountValue) || 0) : null,
                    discountAmount: this.editOrderDiscountAmount > 0 ? this.editOrderDiscountAmount : 0,
                    discountPercent: this.editOrderForm.discountType === 'percentage' ? (Number(this.editOrderForm.discountValue) || 0) : 0,
                    orderSubTotal: this.editOrderTotal,
                    orderTotalAfterDiscount: this.editOrderFinalTotal,
                    customerOrderItem: mergeCartLinesForOrderPayload(this.editOrderForm.items.map(item => ({
                        id: item.itemId || item.id,
                        quantity: item.quantity,
                    }))),
                };
                const response = await HTTP.put(`Admin/UpdateOrder/${this.editOrderData.id}`, request);
                if (response.data && !response.data.errorStatus) {
                    this.$notify.success(response.data.message || this.$i18n.t('orderUpdatedSuccessfully') || 'تم التحديث', { position: 'top-right', timeout: 3000 });
                    this.closeEditOrderModal();
                    this.GetAllOrders();
                } else {
                    this.$notify.error(response.data?.message || this.$i18n.t('error'), { position: 'top-right', timeout: 3000 });
                }
            } catch (error) {
                this.$notify.error(error.response?.data?.message || this.$i18n.t('error'), { position: 'top-right', timeout: 3000 });
            } finally {
                this.loadingUpdateOrder = false;
            }
        },
        async exportCurrentReportExcel() {
            this.exportingExcel = true;
            try {
                if (this.activeTab === 'orders') {
                    const params = new URLSearchParams();
                    if (this.search.info) params.append('info', this.search.info);
                    if (this.search.startDate) params.append('startDate', this.search.startDate);
                    if (this.search.endDate) params.append('endDate', this.search.endDate);
                    if (this.search.paymentMethod) params.append('paymentMethod', this.search.paymentMethod);
                    const response = await HTTP.get(`Admin/ExportOrders?${params.toString()}`, { responseType: 'blob' });
                    const blob = new Blob([response.data], { type: 'text/csv;charset=utf-8;' });
                    const link = document.createElement('a');
                    link.href = URL.createObjectURL(blob);
                    link.download = `orders_${new Date().toISOString().split('T')[0]}.csv`;
                    link.click();
                    URL.revokeObjectURL(link.href);
                }
            } catch (e) {
                this.$notify.error(this.$t('exportError') || 'خطأ بالتصدير', { position: 'top-right', timeout: 3000 });
            } finally {
                this.exportingExcel = false;
            }
        },

        // Advanced Reports Methods
        loadAdvancedReport() {
            if (this.activeTab === 'profit') {
                this.loadProfitReport();
            } else if (this.activeTab === 'topItems') {
                this.loadTopSellingItems();
            } else if (this.activeTab === 'productSales') {
                this.loadProductSalesReport();
            } else if (this.activeTab === 'byCategory') {
                this.loadSalesByCategory();
            } else if (this.activeTab === 'byEmployee') {
                this.loadSalesByEmployee();
            } else if (this.activeTab === 'byWarehouse') {
                this.loadSalesByWarehouse();
            }
        },

        loadProductSalesReport() {
            this.show = true;
            const params = new URLSearchParams();
            params.append('pageNumber', '0');
            params.append('pageSize', '500');
            params.append('onlyWithSales', 'false');
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            if (this.productSalesFilters.tag) params.append('tag', this.productSalesFilters.tag);
            if ((this.productSalesFilters.info || '').trim()) {
                params.append('info', this.productSalesFilters.info.trim());
            }

            HTTP.get(`Admin/GetProductSalesReport?${params.toString()}`)
                .then((response) => {
                    const payload = response.data.data || {};
                    this.productSalesItems = payload.items || [];
                    const summary = payload.summary || {};
                    this.productSalesSummary = {
                        totalQuantitySold: summary.totalQuantitySold ?? 0,
                        totalSales: summary.totalSales ?? 0,
                        totalDistinctItems: summary.totalDistinctItems ?? 0,
                        totalRemainingQuantity: summary.totalRemainingQuantity ?? 0,
                        itemsWithSales: summary.itemsWithSales ?? 0,
                    };
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    this.productSalesItems = [];
                    console.error('Error loading product sales report:', error);
                });
        },

        loadProfitReport() {
            this.show = true;
            const params = new URLSearchParams();
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            
            HTTP.get(`Admin/GetProfitReport?${params.toString()}`)
                .then((response) => {
                    this.profitReport = response.data.data || {};
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    console.error('Error loading profit report:', error);
                });
        },

        loadTopSellingItems() {
            this.show = true;
            const params = new URLSearchParams();
            params.append('topCount', '10');
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            
            HTTP.get(`Admin/GetTopSellingItems?${params.toString()}`)
                .then((response) => {
                    const payload = response.data.data;
                    const items = Array.isArray(payload) ? payload : (payload?.items || []);
                    const summary = payload?.summary;
                    this.topSellingItems = items;
                    this.topSellingItemsSummary = {
                        totalQuantitySold: summary?.totalQuantitySold ?? 0,
                        totalSales: summary?.totalSales ?? 0,
                        totalDistinctItems: summary?.totalDistinctItems ?? 0,
                        totalOrders: summary?.totalOrders ?? 0,
                    };
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    console.error('Error loading top selling items:', error);
                });
        },

        loadSalesByCategory() {
            this.show = true;
            const params = new URLSearchParams();
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            
            HTTP.get(`Admin/GetSalesByCategory?${params.toString()}`)
                .then((response) => {
                    this.salesByCategory = response.data.data || [];
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    console.error('Error loading sales by category:', error);
                });
        },

        loadSalesByEmployee() {
            this.show = true;
            const params = new URLSearchParams();
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            
            HTTP.get(`Admin/GetSalesByEmployee?${params.toString()}`)
                .then((response) => {
                    this.salesByEmployee = response.data.data || [];
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    console.error('Error loading sales by employee:', error);
                });
        },

        loadSalesByWarehouse() {
            this.show = true;
            const params = new URLSearchParams();
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);

            HTTP.get(`Admin/GetSalesByWarehouse?${params.toString()}`)
                .then((response) => {
                    this.salesByWarehouse = response.data?.data || response.data?.Data || [];
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    this.salesByWarehouse = [];
                    console.error('Error loading sales by warehouse:', error);
                });
        },

        loadLowStockItems() {
            this.show = true;
            HTTP.get(`Admin/GetLowStockItems?threshold=${this.lowStockThreshold}`)
                .then((response) => {
                    this.lowStockItems = response.data.data || [];
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    console.error('Error loading low stock items:', error);
                });
        },

    },


};
</script>