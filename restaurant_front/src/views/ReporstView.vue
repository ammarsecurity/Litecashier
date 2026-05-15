<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
            <div class="users-page-container">
                <div class="users-page-content">
                    <!-- Header Section -->
                    <div class="users-header-section">
                        <div class="users-header-content">
                            <div class="header-title-wrapper">
                                <div class="header-icon-wrapper">
                                    <b-icon icon="file-earmark-bar-graph-fill" class="header-icon"></b-icon>
                                </div>
                                <div>
                                    <h1 class="users-page-title">{{ $t('all_sales') }}</h1>
                                    <p class="header-subtitle">{{ $t('reportsDescription') || 'نظام تقارير متكامل لتحليل المبيعات والأرباح' }}</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Reports Tabs -->
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
                                :class="{ 'report-tab-active': activeTab === 'byCategory' }"
                                @click="activeTab = 'byCategory'; loadSalesByCategory()"
                            >
                                <b-icon icon="tags-fill" class="me-2"></b-icon>
                                {{ $t('salesByCategory') || 'المبيعات حسب الفئة' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'byEmployee' }"
                                @click="activeTab = 'byEmployee'; loadSalesReportStaff(); loadSalesByEmployee()"
                            >
                                <b-icon icon="people-fill" class="me-2"></b-icon>
                                {{ $t('salesByEmployee') || 'المبيعات حسب الموظف' }}
                            </button>
                            <button
                                class="report-tab"
                                :class="{ 'report-tab-active': activeTab === 'returnedItems' }"
                                @click="activeTab = 'returnedItems'; loadReturnedItems()"
                            >
                                <b-icon icon="arrow-counterclockwise" class="me-2"></b-icon>
                                {{ $t('returnedItemsReport') || 'المواد المسترجعة' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'delivery' }"
                                @click="activeTab = 'delivery'; loadDeliveryStatistics()"
                            >
                                <b-icon icon="truck" class="me-2"></b-icon>
                                {{ $t('deliveryStatistics') || 'إحصائيات التوصيل' }}
                            </button>
                            <button 
                                class="report-tab" 
                                :class="{ 'report-tab-active': activeTab === 'expensesReport' }"
                                @click="activeTab = 'expensesReport'; loadExpensesReport()"
                            >
                                <b-icon icon="wallet2" class="me-2"></b-icon>
                                {{ $t('expensesReport') || 'تقارير الصرفيات' }}
                            </button>
                        </div>
                    </div>

                    <!-- Advanced Reports Filters -->
                    <div class="users-search-section" v-if="activeTab !== 'orders' && activeTab !== 'delivery' && activeTab !== 'expensesReport'">
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem;">
                            <div class="users-search-container">
                                <b-icon icon="calendar" class="search-icon"></b-icon>
                                <input 
                                    v-model="reportFilters.startDate" 
                                    type="date" 
                                    :placeholder="$t('from_date')"
                                    class="users-search-input"
                                    @change="loadAdvancedReport()"
                                />
                            </div>
                            <div class="users-search-container">
                                <b-icon icon="calendar-check" class="search-icon"></b-icon>
                                <input 
                                    v-model="reportFilters.endDate" 
                                    type="date" 
                                    :placeholder="$t('to_date')"
                                    class="users-search-input"
                                    @change="loadAdvancedReport()"
                                />
                            </div>
                            <div class="users-search-container">
                                <b-icon icon="box-seam" class="search-icon"></b-icon>
                                <select 
                                    v-model="reportFilters.orderType" 
                                    class="users-search-input"
                                    style="padding-right: 2.5rem;"
                                    @change="loadAdvancedReport()"
                                >
                                    <option value="">{{ $t('allOrderTypes') || 'جميع أنواع الطلبات' }}</option>
                                    <option value="DineIn">{{ $t('dineIn') || 'داخلي' }}</option>
                                    <option value="Takeaway">{{ $t('takeaway') || 'طلب خارجي' }}</option>
                                    <option value="Delivery">{{ $t('delivery') || 'توصيل' }}</option>
                                </select>
                            </div>
                            <div class="users-search-container">
                                <b-icon icon="credit-card" class="search-icon"></b-icon>
                                <select 
                                    v-model="reportFilters.paymentMethod" 
                                    class="users-search-input"
                                    style="padding-right: 2.5rem;"
                                    @change="loadAdvancedReport()"
                                >
                                    <option value="">{{ $t('allPaymentMethods') || 'جميع طرق الدفع' }}</option>
                                    <option value="Cash">{{ $t('cash') || 'نقد' }}</option>
                                    <option value="Card">{{ $t('card') || 'بطاقة' }}</option>
                                    <option value="Credit">{{ $t('credit') || 'دفع لاحق' }}</option>
                                </select>
                            </div>
                            <div class="users-search-container" v-show="activeTab === 'byEmployee'">
                                <b-icon icon="person-badge" class="search-icon"></b-icon>
                                <select
                                    v-model="reportFilters.staffRoleFilter"
                                    class="users-search-input"
                                    style="padding-right: 2.5rem;"
                                    @change="loadAdvancedReport()"
                                >
                                    <option value="">{{ $t('salesByEmployeeAllStaff') || 'كل الحسابات' }}</option>
                                    <option value="SalesStaff">{{ $t('salesByEmployeePosAndWaiter') || 'كاشير ونادل فقط' }}</option>
                                    <option value="POS">{{ $t('rolePOS') || 'كاشير (POS)' }}</option>
                                    <option value="Waiter">{{ $t('roleWaiter') || 'نادل' }}</option>
                                </select>
                            </div>
                            <div class="users-search-container" v-show="activeTab === 'byEmployee'">
                                <b-icon icon="people" class="search-icon"></b-icon>
                                <select
                                    v-model="reportFilters.salesByEmployeeUserId"
                                    class="users-search-input"
                                    style="padding-right: 2.5rem;"
                                    @change="loadAdvancedReport()"
                                >
                                    <option value="">{{ $t('salesByEmployeeAllEmployees') || 'كل الموظفين' }}</option>
                                    <option
                                        v-for="s in salesReportStaffList"
                                        :key="s.id"
                                        :value="String(s.id)"
                                    >
                                        {{ s.name }} — {{ salesReportStaffRoleLabel(s.role) }}
                                    </option>
                                </select>
                            </div>
                            <div class="users-search-container" v-if="hasAdvancedFilters">
                                <button 
                                    class="users-filter-clear-btn"
                                    @click="clearAdvancedFilters"
                                >
                                    <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                                    {{ $t('clearFilters') || 'مسح الفلاتر' }}
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- Orders Grid (Default View) -->
                    <div v-if="activeTab === 'orders'">
                        <!-- Search Section -->
                        <div class="users-search-section">
                            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem;">
                                <div class="users-search-container">
                                    <b-icon icon="search" class="search-icon"></b-icon>
                                    <input 
                                        v-model="search.info" 
                                        type="text" 
                                        :placeholder="$t('invoice_number')"
                                        class="users-search-input"
                                    />
                                </div>
                                <div class="users-search-container">
                                    <b-icon icon="calendar" class="search-icon"></b-icon>
                                    <input 
                                        v-model="search.startDate" 
                                        type="date" 
                                        :placeholder="$t('from_date')"
                                        class="users-search-input"
                                    />
                                </div>
                                <div class="users-search-container">
                                    <b-icon icon="calendar-check" class="search-icon"></b-icon>
                                    <input 
                                        v-model="search.endDate" 
                                        type="date" 
                                        :placeholder="$t('to_date')"
                                        class="users-search-input"
                                    />
                                </div>
                                <div class="users-search-container">
                                    <b-icon icon="box-seam" class="search-icon"></b-icon>
                                    <select 
                                        v-model="search.orderType" 
                                        class="users-search-input"
                                        style="padding-right: 2.5rem;"
                                    >
                                        <option value="">{{ $t('allOrderTypes') || 'جميع أنواع الطلبات' }}</option>
                                        <option value="DineIn">{{ $t('dineIn') || 'داخلي' }}</option>
                                        <option value="Takeaway">{{ $t('takeaway') || 'طلب خارجي' }}</option>
                                        <option value="Delivery">{{ $t('delivery') || 'توصيل' }}</option>
                                    </select>
                                </div>
                                <div class="users-search-container">
                                    <b-icon icon="credit-card" class="search-icon"></b-icon>
                                    <select 
                                        v-model="search.paymentMethod" 
                                        class="users-search-input"
                                        style="padding-right: 2.5rem;"
                                    >
                                        <option value="">{{ $t('allPaymentMethods') || 'جميع طرق الدفع' }}</option>
                                        <option value="Cash">{{ $t('cash') || 'نقد' }}</option>
                                        <option value="Card">{{ $t('card') || 'بطاقة' }}</option>
                                        <option value="Credit">{{ $t('credit') || 'دفع لاحق' }}</option>
                                    </select>
                                </div>
                                <div class="users-search-container">
                                    <b-icon icon="truck" class="search-icon"></b-icon>
                                    <select 
                                        v-model="search.deliveryDriverId" 
                                        class="users-search-input"
                                        style="padding-right: 2.5rem;"
                                    >
                                        <option value="">{{ $t('allDrivers') || 'جميع السائقين' }}</option>
                                        <option v-for="driver in deliveryDrivers" :key="driver.id" :value="driver.id">
                                            {{ driver.name }}
                                        </option>
                                    </select>
                                </div>
                                <div class="users-search-container" v-if="hasActiveFilters">
                                    <button 
                                        class="users-filter-clear-btn"
                                        @click="clearFilters"
                                    >
                                        <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                                        {{ $t('clearFilters') || 'مسح الفلاتر' }}
                                    </button>
                                </div>
                                <div class="users-search-container">
                                    <button 
                                        class="export-excel-btn" 
                                        @click="exportCurrentReportExcel()" 
                                        :disabled="exportingExcel"
                                    >
                                        <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                        <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                        {{ $t('downloadExcel') || 'تحميل Excel' }}
                                    </button>
                                </div>
                            </div>
                        </div>

                        <!-- Orders Table -->
                        <div class="report-table-container">
                            <b-table
                                id="orders-table"
                                :items="ordersForTable"
                                :fields="ordersTableFields"
                                striped
                                hover
                                responsive
                                class="reports-table"
                                :empty-text="$t('noInvoicesFound') || 'لا توجد فواتير'"
                            >
                                <template #cell(reportPeriod)="row">
                                    <span class="stat-value text-muted small">{{ row.item.reportPeriod }}</span>
                                </template>
                                <template #cell(orderCode)="row">
                                    <span class="item-name-text">{{ row.item.orderCode }}</span>
                                </template>
                                <template #cell(insertDate)="row">
                                    <span class="stat-value">{{ formatDate(row.item.insertDate) }}</span>
                                </template>
                                <template #cell(dailySequenceNumber)="row">
                                    <span class="quantity-badge">{{ row.item.dailySequenceNumber || '-' }}</span>
                                </template>
                                <template #cell(orderType)="row">
                                    <span>{{ getOrderTypeText(row.item.orderType) }}</span>
                                </template>
                                <template #cell(paymentMethod)="row">
                                    <span>{{ getPaymentMethodText(row.item.paymentMethod) }}</span>
                                </template>
                                <template #cell(itemsCount)="row">
                                    <span class="quantity-badge">{{ row.item.itemsCount || 0 }}</span>
                                </template>
                                <template #cell(discountAmount)="row">
                                    <span v-if="Number(row.item.discountAmount || 0) > 0" class="stat-danger">
                                        - {{ formatPrice(row.item.discountAmount || 0) }}
                                    </span>
                                    <span v-else>-</span>
                                </template>
                                <template #cell(totalAmount)="row">
                                    <span class="stat-amount">
                                        {{ formatPrice(row.item.orderTotalAfterDiscount ?? row.item.orderPrice ?? 0) }} {{ $t('currency') }}
                                    </span>
                                </template>
                                <template #cell(createdByUsername)="row">
                                    <span>{{ row.item.createdByUsername || '-' }}</span>
                                </template>
                                <template #cell(actions)="row">
                                    <div class="actions-cell">
                                        <button type="button" class="action-btn action-btn--icon action-btn--view" @click="showItemsModel(row.item.customerOrderItem, row.item)">
                                            <b-icon icon="eye-fill" class="action-icon"></b-icon>
                                        </button>
                                        <button type="button" class="action-btn action-btn--icon action-btn--edit" @click="editOrder(row.item)">
                                            <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                                        </button>
                                    </div>
                                </template>
                            </b-table>
                        </div>

                        <!-- Pagination -->
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
                            <div class="report-section-header" style="display: flex; justify-content: flex-end; margin-bottom: 1rem;">
                                <button class="export-excel-btn" @click="exportCurrentReportExcel()" :disabled="!profitReport || Object.keys(profitReport).length === 0 || exportingExcel">
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                            </div>
                            <div class="report-stats-grid">
                                <div class="report-stat-card report-stat-primary">
                                    <div class="report-stat-icon">
                                        <b-icon icon="currency-dollar"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ formatPrice(profitReport.totalSales || 0) }}</h3>
                                        <p class="report-stat-label">{{ $t('totalSales') || 'إجمالي المبيعات' }}</p>
                                        <p class="report-stat-detail" v-if="profitReport.period">
                                            {{ $t('period') || 'الفترة' }}: {{ profitReport.period.startDate || '-' }} 
                                            {{ profitReport.period.endDate ? ' - ' + profitReport.period.endDate : '' }}
                                        </p>
                                    </div>
                                </div>
                                <div class="report-stat-card report-stat-danger">
                                    <div class="report-stat-icon">
                                        <b-icon icon="cart"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ formatPrice(profitReport.totalCost || 0) }}</h3>
                                        <p class="report-stat-label">{{ $t('totalCost') || 'إجمالي التكلفة' }}</p>
                                        <p class="report-stat-detail" v-if="profitReport.totalItemsSold">
                                            {{ $t('totalItemsSold') || 'إجمالي المواد المباعة' }}: {{ profitReport.totalItemsSold }}
                                        </p>
                                    </div>
                                </div>
                                <div class="report-stat-card report-stat-success">
                                    <div class="report-stat-icon">
                                        <b-icon icon="file-earmark-bar-graph-fill"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ formatPrice(profitReport.totalProfit || 0) }}</h3>
                                        <p class="report-stat-label">{{ $t('totalProfit') || 'إجمالي الربح' }}</p>
                                        <p class="report-stat-detail" v-if="profitReport.totalSales && profitReport.totalCost">
                                            {{ $t('profitRatio') || 'نسبة الربح' }}: {{ ((profitReport.totalProfit / profitReport.totalSales) * 100).toFixed(2) }}%
                                        </p>
                                    </div>
                                </div>
                                <div class="report-stat-card report-stat-info">
                                    <div class="report-stat-icon">
                                        <b-icon icon="percent"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ profitReport.profitMargin || 0 }}%</h3>
                                        <p class="report-stat-label">{{ $t('profitMargin') || 'هامش الربح' }}</p>
                                        <p class="report-stat-detail">
                                            {{ $t('profitMarginDescription') || 'نسبة الربح من إجمالي المبيعات' }}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Top Selling Items -->
                        <div v-if="activeTab === 'topItems'" class="report-section">
                            <div class="report-section-header" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem;">
                                <div class="report-info-banner" v-if="topSellingItems.length > 0" style="margin: 0;">
                                <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                <span>{{ $t('topSellingItemsDescription') || 'عرض أفضل المنتجات مبيعاً حسب الكمية المباعة' }}</span>
                                </div>
                                <button class="export-excel-btn" @click="exportCurrentReportExcel()" :disabled="!topSellingItems.length || exportingExcel">
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                            </div>
                            <div class="report-table-container">
                                <b-table
                                    :items="topSellingItemsForTable"
                                    :fields="topSellingItemsFields"
                                    striped
                                    hover
                                    responsive
                                    class="reports-table"
                                    :empty-text="$t('noTopSellingItems') || 'لا توجد منتجات'"
                                >
                                    <template #cell(reportPeriod)="row">
                                        <span class="stat-value text-muted small">{{ row.item.reportPeriod }}</span>
                                    </template>
                                    <template #cell(rank)="row">
                                        <span class="rank-badge" :class="getRankClass(row.index)">{{ row.index + 1 }}</span>
                                    </template>
                                    <template #cell(itemName)="row">
                                        <span class="item-name-text">{{ row.item.itemName }}</span>
                                    </template>
                                    <template #cell(totalQuantitySold)="row">
                                        <span class="quantity-badge">{{ row.item.totalQuantitySold }}</span>
                                    </template>
                                    <template #cell(totalSales)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.totalSales) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(averagePrice)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.totalSales / row.item.totalQuantitySold) }} {{ $t('currency') }}</span>
                                    </template>
                                </b-table>
                            </div>
                        </div>

                        <!-- Sales By Category -->
                        <div v-if="activeTab === 'byCategory'" class="report-section">
                            <div class="report-section-header" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem;">
                                <div class="report-info-banner" v-if="salesByCategory.length > 0" style="margin: 0;">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('salesByCategoryDescription') || 'تحليل المبيعات حسب الفئات المختلفة' }}</span>
                                </div>
                                <button class="export-excel-btn" @click="exportCurrentReportExcel()" :disabled="!salesByCategory.length || exportingExcel">
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                            </div>
                            <div class="report-table-container">
                                <b-table
                                    :items="salesByCategoryForTable"
                                    :fields="salesByCategoryFields"
                                    striped
                                    hover
                                    responsive
                                    class="reports-table"
                                    :empty-text="$t('noSalesByCategory') || 'لا توجد مبيعات حسب الفئة'"
                                >
                                    <template #cell(reportPeriod)="row">
                                        <span class="stat-value text-muted small">{{ row.item.reportPeriod }}</span>
                                    </template>
                                    <template #cell(category)="row">
                                        <div class="category-cell">
                                            <b-icon icon="tags-fill" class="category-icon"></b-icon>
                                            <span>{{ row.item.category }}</span>
                                        </div>
                                    </template>
                                    <template #cell(totalSales)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.totalSales) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(totalExpenses)="row">
                                        <span class="stat-amount stat-expense">{{ formatPrice(row.item.totalExpenses || 0) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(averageOrderValue)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.orderCount > 0 ? row.item.totalSales / row.item.orderCount : 0) }} {{ $t('currency') }}</span>
                                    </template>
                                </b-table>
                            </div>
                        </div>

                        <!-- Sales By Employee -->
                        <div v-if="activeTab === 'byEmployee'" class="report-section">
                            <div class="report-section-header" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem;">
                                <div class="report-info-banner" v-if="salesByEmployee.length > 0" style="margin: 0;">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('salesByEmployeeDescription') || 'مقارنة أداء الموظفين في المبيعات' }}</span>
                                </div>
                                <button class="export-excel-btn" @click="exportCurrentReportExcel()" :disabled="!salesByEmployee.length || exportingExcel">
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                            </div>
                            <div class="report-table-container">
                                <b-table
                                    :items="salesByEmployeeForTable"
                                    :fields="salesByEmployeeFields"
                                    striped
                                    hover
                                    responsive
                                    class="reports-table"
                                    :empty-text="$t('noSalesByEmployee') || 'لا توجد مبيعات حسب الموظف'"
                                >
                                    <template #cell(reportPeriod)="row">
                                        <span class="stat-value text-muted small">{{ row.item.reportPeriod }}</span>
                                    </template>
                                    <template #cell(employeeName)="row">
                                        <div class="employee-cell">
                                            <b-icon icon="person-fill" class="employee-icon"></b-icon>
                                            <span>{{ row.item.employeeName }}</span>
                                        </div>
                                    </template>
                                    <template #cell(totalSales)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.totalSales) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(averageOrderValue)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.totalOrders > 0 ? row.item.totalSales / row.item.totalOrders : 0) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(itemsPerOrder)="row">
                                        <span class="stat-value">{{ row.item.totalOrders > 0 ? (row.item.totalItemsSold / row.item.totalOrders).toFixed(2) : 0 }}</span>
                                    </template>
                                </b-table>
                            </div>
                        </div>

                        <!-- Returned Items -->
                        <div v-if="activeTab === 'returnedItems'" class="report-section">
                            <div class="report-section-header" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem;">
                                <div class="report-info-banner" v-if="returnedItems.length > 0" style="margin: 0;">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('returnedItemsDescription') || 'المواد المحذوفة من الفواتير المحفوظة في POS' }}</span>
                                </div>
                                <button class="export-excel-btn" @click="exportCurrentReportExcel()" :disabled="!returnedItems.length || exportingExcel">
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                            </div>
                            <div class="report-table-container">
                                <b-table
                                    :items="returnedItemsForTable"
                                    :fields="returnedItemsFields"
                                    striped
                                    hover
                                    responsive
                                    class="reports-table"
                                    :empty-text="$t('noReturnedItems') || 'لا توجد مواد مسترجعة'"
                                >
                                    <template #cell(reportPeriod)="row">
                                        <span class="stat-value text-muted small">{{ row.item.reportPeriod }}</span>
                                    </template>
                                    <template #cell(lineTotal)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.lineTotal || 0) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(unitPrice)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.unitPrice || 0) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(insertDate)="row">
                                        <span class="stat-value">{{ formatDate(row.item.insertDate) }}</span>
                                    </template>
                                    <template #cell(tableDisplay)="row">
                                        <span>{{ row.item.mergedTableNumbers || row.item.tableNumber || '-' }}</span>
                                    </template>
                                </b-table>
                            </div>
                            <div class="users-pagination-container mt-3" v-if="totalReturnedItems > returnedItemsPageSize">
                                <b-pagination
                                    v-model="returnedItemsPageNumber"
                                    :total-rows="totalReturnedItems"
                                    :per-page="returnedItemsPageSize"
                                    class="users-pagination"
                                ></b-pagination>
                            </div>
                        </div>

                        <!-- Delivery Statistics -->
                        <div v-if="activeTab === 'delivery'" class="report-section">
                            <div class="report-section-header" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem;">
                                <div class="report-info-banner" v-if="deliveryStatistics" style="margin: 0;">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('deliveryStatisticsDescription') || 'إحصائيات شاملة لطلبات التوصيل والسائقين' }}</span>
                                </div>
                                <button class="export-excel-btn" @click="exportCurrentReportExcel()" :disabled="!deliveryStatistics || exportingExcel">
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                            </div>
                            <!-- Overall Statistics -->
                            <div class="report-stats-grid" v-if="deliveryStatistics">
                                <div class="report-stat-card report-stat-primary">
                                    <div class="report-stat-icon">
                                        <b-icon icon="truck"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ deliveryStatistics.totalDrivers || 0 }}</h3>
                                        <p class="report-stat-label">{{ $t('totalDrivers') || 'إجمالي السائقين' }}</p>
                                        <p class="report-stat-detail">
                                            {{ $t('activeDrivers') || 'نشط' }}: {{ deliveryStatistics.activeDrivers || 0 }}
                                        </p>
                                    </div>
                                </div>
                                <div class="report-stat-card report-stat-info">
                                    <div class="report-stat-icon">
                                        <b-icon icon="clipboard-check"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ deliveryStatistics.totalOrders || 0 }}</h3>
                                        <p class="report-stat-label">{{ $t('totalDeliveries') || 'إجمالي التوصيلات' }}</p>
                                    </div>
                                </div>
                                <div class="report-stat-card report-stat-success">
                                    <div class="report-stat-icon">
                                        <b-icon icon="check2-circle"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ deliveryStatistics.deliveredOrders || 0 }}</h3>
                                        <p class="report-stat-label">{{ $t('deliveredOrders') || 'الطلبات الواصلة' }}</p>
                                    </div>
                                </div>
                                <div class="report-stat-card report-stat-warning">
                                    <div class="report-stat-icon">
                                        <b-icon icon="clock-history"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ deliveryStatistics.pendingOrders || 0 }}</h3>
                                        <p class="report-stat-label">{{ $t('pendingDeliveries') || 'التوصيلات المعلقة' }}</p>
                                    </div>
                                </div>
                                <div class="report-stat-card report-stat-danger">
                                    <div class="report-stat-icon">
                                        <b-icon icon="x-circle"></b-icon>
                                    </div>
                                    <div class="report-stat-content">
                                        <h3 class="report-stat-value">{{ deliveryStatistics.failedOrders || 0 }}</h3>
                                        <p class="report-stat-label">{{ $t('failedDeliveries') || 'التوصيلات الفاشلة' }}</p>
                                    </div>
                                </div>
                            </div>

                            <!-- Drivers Statistics Table -->
                            <div class="report-table-container" v-if="deliveryStatistics && deliveryStatistics.drivers">
                                <h4 class="report-section-title">{{ $t('driversStatistics') || 'إحصائيات السائقين' }}</h4>
                                <b-table
                                    :items="deliveryDriversForTable"
                                    :fields="driversStatisticsFields"
                                    striped
                                    hover
                                    responsive
                                    class="drivers-statistics-table"
                                    :empty-text="$t('noDeliveryStatistics') || 'لا توجد إحصائيات توصيل متاحة'"
                                >
                                    <template #cell(reportPeriod)="row">
                                        <span class="stat-value text-muted small">{{ row.item.reportPeriod }}</span>
                                    </template>
                                    <template #cell(driverName)="row">
                                        <div class="driver-name-cell">
                                            <b-icon icon="truck" class="driver-icon"></b-icon>
                                            <span>{{ row.item.driverName }}</span>
                                        </div>
                                    </template>
                                    <template #cell(status)="row">
                                        <span :class="row.item.isActive ? 'status-badge status-active' : 'status-badge status-inactive'">
                                            {{ row.item.isActive ? ($t('active') || 'نشط') : ($t('inactive') || 'غير نشط') }}
                                        </span>
                                    </template>
                                    <template #cell(totalOrders)="row">
                                        <span class="stat-value">{{ row.item.totalOrders || 0 }}</span>
                                    </template>
                                    <template #cell(deliveredOrders)="row">
                                        <span class="stat-value stat-success">{{ row.item.deliveredOrders || 0 }}</span>
                                    </template>
                                    <template #cell(pendingOrders)="row">
                                        <span class="stat-value stat-warning">{{ row.item.pendingOrders || 0 }}</span>
                                    </template>
                                    <template #cell(failedOrders)="row">
                                        <span class="stat-value stat-danger">{{ row.item.failedOrders || 0 }}</span>
                                    </template>
                                    <template #cell(totalAmount)="row">
                                        <span class="stat-amount">{{ formatPrice(row.item.totalAmount || 0) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(paidAmount)="row">
                                        <span class="stat-amount stat-success">{{ formatPrice(row.item.paidAmount || 0) }} {{ $t('currency') }}</span>
                                    </template>
                                    <template #cell(remainingAmount)="row">
                                        <span class="stat-amount stat-warning">{{ formatPrice(row.item.remainingAmount || 0) }} {{ $t('currency') }}</span>
                                    </template>
                                </b-table>
                            </div>
                            <div v-else-if="!loadingDeliveryStatistics" class="empty-state">
                                <b-icon icon="truck" class="empty-icon"></b-icon>
                                <p>{{ $t('noDeliveryStatistics') || 'لا توجد إحصائيات توصيل متاحة' }}</p>
                            </div>
                        </div>

                        <!-- Expenses Report -->
                        <div v-if="activeTab === 'expensesReport'" class="report-section">
                            <div class="report-section-header">
                                <div class="report-info-banner" v-if="expensesReport">
                                    <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                    <span>{{ $t('expensesReportDescription') || 'تقارير وإحصائيات الصرفيات حسب الفترة والفئة' }}</span>
                                </div>
                                <button 
                                    class="export-excel-btn" 
                                    @click="exportCurrentReportExcel()" 
                                    :disabled="!expensesReport || exportingExcel"
                                >
                                    <b-spinner small v-if="exportingExcel" class="me-2"></b-spinner>
                                    <b-icon v-else icon="file-earmark-excel" class="me-2"></b-icon>
                                    {{ $t('downloadExcel') || 'تحميل Excel' }}
                                </button>
                            </div>
                            <div class="expenses-report-filters" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.5rem;">
                                <div class="users-search-container">
                                    <b-icon icon="calendar" class="search-icon"></b-icon>
                                    <input v-model="reportFilters.startDate" type="date" :placeholder="$t('from_date')" class="users-search-input" @change="loadExpensesReport()" />
                                </div>
                                <div class="users-search-container">
                                    <b-icon icon="calendar-check" class="search-icon"></b-icon>
                                    <input v-model="reportFilters.endDate" type="date" :placeholder="$t('to_date')" class="users-search-input" @change="loadExpensesReport()" />
                                </div>
                            </div>
                            <div v-if="loadingExpensesReport" class="text-center py-5">
                                <b-spinner></b-spinner>
                                <p class="mt-2">{{ $t('loading') || 'جاري التحميل...' }}</p>
                            </div>
                            <template v-else-if="expensesReport">
                                <div class="report-stats-grid">
                                    <div class="report-stat-card report-stat-danger">
                                        <div class="report-stat-icon"><b-icon icon="wallet2"></b-icon></div>
                                        <div class="report-stat-content">
                                            <h3 class="report-stat-value">{{ formatPrice(expensesReport.totalExpenses || 0) }}</h3>
                                            <p class="report-stat-label">{{ $t('totalExpenses') || 'إجمالي الصرفيات' }}</p>
                                            <p class="report-stat-detail">{{ $t('totalCount') || 'العدد' }}: {{ expensesReport.totalCount || 0 }}</p>
                                        </div>
                                    </div>
                                    <div class="report-stat-card report-stat-info">
                                        <div class="report-stat-icon"><b-icon icon="calendar-month"></b-icon></div>
                                        <div class="report-stat-content">
                                            <h3 class="report-stat-value">{{ formatPrice(expensesReport.thisMonthExpenses || 0) }}</h3>
                                            <p class="report-stat-label">{{ $t('thisMonthExpenses') || 'صرفيات هذا الشهر' }}</p>
                                        </div>
                                    </div>
                                    <div class="report-stat-card report-stat-primary">
                                        <div class="report-stat-icon"><b-icon icon="calendar-week"></b-icon></div>
                                        <div class="report-stat-content">
                                            <h3 class="report-stat-value">{{ formatPrice(expensesReport.thisWeekExpenses || 0) }}</h3>
                                            <p class="report-stat-label">{{ $t('thisWeekExpenses') || 'صرفيات هذا الأسبوع' }}</p>
                                        </div>
                                    </div>
                                </div>
                                <div class="report-table-container">
                                    <h4 class="report-section-title">{{ $t('expensesByCategory') || 'الصرفيات حسب الفئة' }}</h4>
                                    <b-table
                                        :items="expensesByCategoryForTable"
                                        :fields="expensesReportFields"
                                        striped
                                        hover
                                        responsive
                                        class="reports-table"
                                        :empty-text="$t('noExpensesByCategory') || 'لا توجد صرفيات حسب الفئة'"
                                    >
                                        <template #cell(reportPeriod)="row">
                                            <span class="stat-value text-muted small">{{ row.item.reportPeriod }}</span>
                                        </template>
                                        <template #cell(category)="row">
                                            <span>{{ row.item.category || '-' }}</span>
                                        </template>
                                        <template #cell(totalAmount)="row">
                                            <span class="stat-amount stat-expense">{{ formatPrice(row.item.totalAmount || 0) }} {{ $t('currency') }}</span>
                                        </template>
                                    </b-table>
                                </div>
                            </template>
                            <div v-else-if="!loadingExpensesReport" class="empty-state">
                                <b-icon icon="wallet2" class="empty-icon"></b-icon>
                                <p>{{ $t('noExpensesReport') || 'لا توجد بيانات صرفيات' }}</p>
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
                                <img src="../assets/logoarabic.png" class="bill-logo-img" />
                                <h2 class="bill-store-name">نظام لايت كاشير</h2>
                                <p class="bill-store-subtitle">نظام إدارة المطاعم</p>
                            </div>

                            <!-- Order Info -->
                            <div class="bill-info-section">
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('invoice_number') }}:</span>
                                    <span class="bill-info-value" v-if="order">{{ order.orderCode }}</span>
                                </div>
                                <div class="bill-info-row" v-if="order && order.dailySequenceNumber">
                                    <span class="bill-info-label">{{ $t('orderNumber') || 'رقم الطلب اليومي' }}:</span>
                                    <span class="bill-info-value">#{{ order.dailySequenceNumber }}</span>
                                </div>
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('from_date') }}:</span>
                                    <span class="bill-info-value" v-if="order">{{ formatDate(order.insertDate) }}</span>
                                </div>
                                <div class="bill-info-row" v-if="order && order.paymentMethod">
                                    <span class="bill-info-label">{{ $t('paymentMethod') }}:</span>
                                    <span class="bill-info-value">{{ getPaymentMethodText(order.paymentMethod) }}</span>
                                </div>
                                <div class="bill-info-row" v-if="order && order.orderType">
                                    <span class="bill-info-label">{{ $t('orderType') }}:</span>
                                    <span class="bill-info-value">{{ getOrderTypeText(order.orderType) }}</span>
                                </div>
                                <div class="bill-info-row" v-if="order && order.orderType === 'DineIn' && (order.mergedTableNumbers || (order.tables && order.tables.length > 0))">
                                    <span class="bill-info-label">{{ $t('table') || 'الطاولة' }}:</span>
                                    <span class="bill-info-value">{{ order.mergedTableNumbers || (order.tables && order.tables.length > 0 ? order.tables[0].tableNumber : '') }}</span>
                                </div>
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('employeeLabel') }}:</span>
                                    <span class="bill-info-value" v-if="order">{{ order.createdByUsername || userInfo.name }}</span>
                                </div>
                            </div>
                            <div v-if="orderDetailRows.length" class="bill-extra-details">
                                <h4 class="bill-extra-title">{{ $t('additionalDetails') || 'تفاصيل إضافية' }}</h4>
                                <div class="bill-extra-grid">
                                    <div v-for="detail in orderDetailRows" :key="detail.key" class="bill-extra-card">
                                        <span class="bill-extra-label">{{ detail.label }}</span>
                                        <span class="bill-extra-value" :class="{ 'bill-extra-value--emphasis': detail.emphasis }">{{ detail.value }}</span>
                                    </div>
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
                                                {{ item.item.name }}
                                                <span v-if="hasDiscount(item)" class="bill-discount-badge">خصم</span>
                                            </td>
                                            <td class="bill-item-qty">{{ item.quantity }}</td>
                                            <td class="bill-item-price">
                                                <span v-if="hasDiscount(item)" class="bill-price-discounted">
                                                    <span class="bill-original-price">{{ formatPrice(item.item.sellingPrice) }}</span>
                                                    <span class="bill-discount-price">{{ formatPrice(item.item.disCountPrice) }}</span>
                                                </span>
                                                <span v-else>{{ formatPrice(item.item.sellingPrice) }}</span>
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
                                    <span class="bill-summary-value">{{ order.itemsCount }} {{ $t('items') }}</span>
                                </div>
                                <div class="bill-summary-row">
                                    <span class="bill-summary-label">{{ $t('subtotal') || 'المجموع قبل الخصم' }}:</span>
                                    <span class="bill-summary-value">{{ formatPrice(orderModalSubtotal) }} {{ $t('currency') }}</span>
                                </div>
                                <div class="bill-summary-row" v-if="orderModalDiscountAmount > 0">
                                    <span class="bill-summary-label">{{ $t('discountLabel') || 'الخصم' }} ({{ orderModalDiscountLabel }}):</span>
                                    <span class="bill-summary-value">- {{ formatPrice(orderModalDiscountAmount) }} {{ $t('currency') }}</span>
                                </div>
                                <div class="bill-summary-row bill-summary-total">
                                    <span class="bill-summary-label">{{ $t('total') }}:</span>
                                    <span class="bill-summary-value">{{ formatPrice(orderModalFinalTotal) }} {{ $t('currency') }}</span>
                                </div>
                            </div>

                            <!-- Footer -->
                            <div class="bill-footer">
                                <p class="bill-footer-text">شكراً لزيارتكم</p>
                                <p class="bill-footer-text">Thank you for your visit</p>
                            </div>
                        </div>
                    </div>

                    <!-- Modal Actions -->
                    <div class="users-form-actions" style="margin-top: 1.5rem;">
                        <button class="users-form-submit-button" @click="print()">
                            <b-icon icon="printer-fill" class="me-2"></b-icon>
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
            <b-modal id="modal-edit-order" :title="$t('editOrder') || 'تعديل الفاتورة'" hide-header hide-footer class="users-modal" size="xl" scrollable>
                <div class="modal-content-wrapper">
                    <div v-if="editOrderData">
                        <!-- Order Info -->
                        <div class="edit-order-section">
                            <h3 class="edit-order-section-title">{{ $t('orderInfo') || 'معلومات الطلب' }}</h3>
                            <div class="edit-order-form-grid">
                                <div class="edit-order-form-group">
                                    <label class="edit-order-label">{{ $t('orderCode') || 'رقم الفاتورة' }}</label>
                                    <input type="text" :value="editOrderData.orderCode" disabled class="edit-order-input" />
                                </div>
                                <div class="edit-order-form-group">
                                    <label class="edit-order-label">{{ $t('paymentMethod') || 'طريقة الدفع' }}</label>
                                    <select v-model="editOrderForm.paymentMethod" class="edit-order-input">
                                        <option value="Cash">{{ $t('cash') || 'نقد' }}</option>
                                        <option value="Card">{{ $t('card') || 'بطاقة' }}</option>
                                        <option value="Credit">{{ $t('credit') || 'دفع لاحق' }}</option>
                                    </select>
                                </div>
                                <div class="edit-order-form-group">
                                    <label class="edit-order-label">{{ $t('orderType') || 'نوع الطلب' }}</label>
                                    <select v-model="editOrderForm.orderType" class="edit-order-input">
                                        <option value="DineIn">{{ $t('dineIn') || 'داخلي' }}</option>
                                        <option value="Takeaway">{{ $t('takeaway') || 'طلب خارجي' }}</option>
                                        <option value="Delivery">{{ $t('delivery') || 'توصيل' }}</option>
                                    </select>
                                </div>
                                <div class="edit-order-form-group">
                                    <label class="edit-order-label">{{ $t('orderNotes') || 'ملاحظات' }}</label>
                                    <textarea v-model="editOrderForm.notes" class="edit-order-input" rows="3"></textarea>
                                </div>
                            </div>
                        </div>

                        <!-- Order Items -->
                        <div class="edit-order-section">
                            <div class="edit-order-section-header">
                                <h3 class="edit-order-section-title">{{ $t('orderItems') || 'عناصر الطلب' }}</h3>
                                <button class="edit-order-add-item-btn" @click="showAddItemModal">
                                    <b-icon icon="plus-circle-fill" class="me-2"></b-icon>
                                    {{ $t('addItem') || 'إضافة منتج' }}
                                </button>
                            </div>
                            <div class="edit-order-items-list">
                                <div v-for="(item, index) in editOrderForm.items" :key="index" class="edit-order-item">
                                    <div class="edit-order-item-info">
                                        <h4 class="edit-order-item-name">{{ item.name }}</h4>
                                        <div class="edit-order-item-details">
                                            <span class="edit-order-item-code">{{ $t('code') || 'الكود' }}: {{ item.code }}</span>
                                            <span class="edit-order-item-price">{{ formatPrice(item.price) }} {{ $t('currency') }}</span>
                                        </div>
                                    </div>
                                    <div class="edit-order-item-controls">
                                        <div class="edit-order-item-quantity">
                                            <button class="edit-order-quantity-btn" @click="decreaseEditItemQuantity(index)">
                                                <b-icon icon="dash"></b-icon>
                                            </button>
                                            <input type="number" v-model.number="item.quantity" min="1" class="edit-order-quantity-input" />
                                            <button class="edit-order-quantity-btn" @click="increaseEditItemQuantity(index)">
                                                <b-icon icon="plus"></b-icon>
                                            </button>
                                        </div>
                                        <button class="edit-order-remove-btn" @click="removeEditItem(index)">
                                            <b-icon icon="trash-fill"></b-icon>
                                        </button>
                                    </div>
                                </div>
                                <div v-if="editOrderForm.items.length === 0" class="edit-order-empty">
                                    <b-icon icon="inbox" class="edit-order-empty-icon"></b-icon>
                                    <p>{{ $t('noItems') || 'لا توجد عناصر' }}</p>
                                </div>
                            </div>
                            <div class="edit-order-total">
                                <span class="edit-order-total-label">{{ $t('subtotal') || 'المجموع قبل الخصم' }}:</span>
                                <span class="edit-order-total-value">{{ formatPrice(editOrderTotal) }} {{ $t('currency') }}</span>
                            </div>
                            <div class="edit-order-total" v-if="editOrderDiscountAmount > 0">
                                <span class="edit-order-total-label">{{ $t('discountLabel') || 'الخصم' }} ({{ editOrderDiscountPreviewLabel }}):</span>
                                <span class="edit-order-total-value">- {{ formatPrice(editOrderDiscountAmount) }} {{ $t('currency') }}</span>
                            </div>
                            <div class="edit-order-total">
                                <span class="edit-order-total-label">{{ $t('total') || 'المجموع' }}:</span>
                                <span class="edit-order-total-value">{{ formatPrice(editOrderFinalTotal) }} {{ $t('currency') }}</span>
                            </div>
                        </div>
                    </div>

                    <div id="print-edit-order" style="display: none;" v-if="editOrderData">
                        <div class="bill-container">
                            <div class="bill-header">
                                <img src="../assets/logoarabic.png" class="bill-logo-img" />
                                <h2 class="bill-store-name">نظام لايت كاشير</h2>
                            </div>
                            <div class="bill-info-section">
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('invoice_number') }}:</span>
                                    <span class="bill-info-value">{{ editOrderData.orderCode || '-' }}</span>
                                </div>
                                <div class="bill-info-row" v-if="editOrderData.dailySequenceNumber">
                                    <span class="bill-info-label">{{ $t('orderNumber') || 'رقم الطلب اليومي' }}:</span>
                                    <span class="bill-info-value">#{{ editOrderData.dailySequenceNumber }}</span>
                                </div>
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('orderType') }}:</span>
                                    <span class="bill-info-value">{{ getOrderTypeText(editOrderForm.orderType) }}</span>
                                </div>
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('paymentMethod') }}:</span>
                                    <span class="bill-info-value">{{ getPaymentMethodText(editOrderForm.paymentMethod) }}</span>
                                </div>
                                <div class="bill-info-row" v-if="editOrderData.createdByUsername">
                                    <span class="bill-info-label">{{ $t('employeeLabel') || 'الحساب المنشئ' }}:</span>
                                    <span class="bill-info-value">{{ editOrderData.createdByUsername }}</span>
                                </div>
                            </div>
                            <div class="bill-divider"></div>
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
                                        <tr v-for="(item, index) in editOrderForm.items" :key="`print-edit-${index}`">
                                            <td class="bill-item-name">{{ item.name }}</td>
                                            <td class="bill-item-qty">{{ item.quantity }}</td>
                                            <td class="bill-item-price">{{ formatPrice(item.price) }}</td>
                                            <td class="bill-item-total">{{ formatPrice(item.price * item.quantity) }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                            <div class="bill-divider"></div>
                            <div class="bill-summary-section">
                                <div class="bill-summary-row">
                                    <span class="bill-summary-label">{{ $t('subtotal') || 'المجموع قبل الخصم' }}:</span>
                                    <span class="bill-summary-value">{{ formatPrice(editOrderTotal) }} {{ $t('currency') }}</span>
                                </div>
                                <div class="bill-summary-row" v-if="editOrderDiscountAmount > 0">
                                    <span class="bill-summary-label">{{ $t('discountLabel') || 'الخصم' }} ({{ editOrderDiscountPreviewLabel }}):</span>
                                    <span class="bill-summary-value">- {{ formatPrice(editOrderDiscountAmount) }} {{ $t('currency') }}</span>
                                </div>
                                <div class="bill-summary-row bill-summary-total">
                                    <span class="bill-summary-label">{{ $t('total') || 'المجموع' }}:</span>
                                    <span class="bill-summary-value">{{ formatPrice(editOrderFinalTotal) }} {{ $t('currency') }}</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Modal Actions -->
                    <div class="users-form-actions" style="margin-top: 1.5rem;">
                        <button type="button" class="users-form-submit-button" @click="printEditOrder" :disabled="loadingUpdateOrder">
                            <b-icon icon="printer-fill" class="me-2"></b-icon>
                            {{ $t('print') || 'طباعة' }}
                        </button>
                        <button class="users-form-submit-button" @click="updateOrder" :disabled="loadingUpdateOrder">
                            <b-spinner small v-if="loadingUpdateOrder" class="me-2"></b-spinner>
                            <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
                            {{ loadingUpdateOrder ? ($t('saving') || 'جاري الحفظ...') : ($t('save') || 'حفظ') }}
                        </button>
                        <button type="button" class="users-form-cancel-button" @click="closeEditOrderModal" :disabled="loadingUpdateOrder">
                            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                            {{ $t('cancel') || 'إلغاء' }}
                        </button>
                    </div>
                </div>
            </b-modal>

            <!-- Add Item Modal -->
            <b-modal id="modal-add-item" :title="$t('addItem') || 'إضافة منتج'" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <div class="edit-order-form-group">
                        <label class="edit-order-label">{{ $t('searchPlaceholder') || 'بحث' }}</label>
                        <input 
                            v-model="itemSearchQuery" 
                            type="text" 
                            class="edit-order-input" 
                            :placeholder="$t('searchPlaceholder') || 'ابحث عن منتج...'"
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
                                <span class="edit-order-search-item-code">{{ $t('code') || 'الكود' }}: {{ item.code }}</span>
                            </div>
                            <div class="edit-order-search-item-price">
                                {{ formatPrice(item.disCountPrice || item.sellingPrice) }} {{ $t('currency') }}
                            </div>
                        </div>
                        <div v-if="availableItems.length === 0 && itemSearchQuery" class="edit-order-empty">
                            <p>{{ $t('noResults') || 'لا توجد نتائج' }}</p>
                        </div>
                    </div>
                    <div class="users-form-actions" style="margin-top: 1rem;">
                        <button type="button" class="users-form-cancel-button" @click="closeAddItemModal">
                            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                            {{ $t('close') || 'إغلاق' }}
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
import { HTTP } from '../http/api.js';
export default {
    name: "OrdersView",
    components: {
        AppHeader,
        ClockVue,
        "vue-barcode": VueBarcode,

    },
    data() {
        return {
            show: false,
            activeTab: 'orders',
            Orders: [],
            pageNumber: 1,
            totalOrders: 0,
            pageSize: 18,
            search: {
                info: "",
                startDate: "",
                endDate: "",
                orderType: "",
                paymentMethod: "",
                deliveryDriverId: "",
            },
            reportFilters: {
                startDate: "",
                endDate: "",
                orderType: "",
                paymentMethod: "",
                staffRoleFilter: "",
                salesByEmployeeUserId: "",
            },
            salesReportStaffList: [],
            deliveryDrivers: [],
            loadingDeliveryDrivers: false,
            totalCardOrders: 0,
            userInfo: {},
            customerOrderItem: [],

            itemId: '',
            order: '',
            totaPrice: '',
            
            // Advanced Reports Data
            profitReport: {},
            topSellingItems: [],
            salesByCategory: [],
            salesByEmployee: [],
            returnedItems: [],
            totalReturnedItems: 0,
            returnedItemsPageNumber: 1,
            returnedItemsPageSize: 15,
            deliveryStatistics: null,
            loadingDeliveryStatistics: false,
            expensesReport: null,
            loadingExpensesReport: false,
            exportingExcel: false,
            
            // Search debounce timer
            searchTimer: null,
            
            // Edit Order Data
            editOrderData: null,
            editOrderForm: {
                paymentMethod: 'Cash',
                orderType: 'DineIn',
                notes: '',
                discountType: null,
                discountValue: null,
                items: []
            },
            availableItems: [],
            itemSearchQuery: '',
            itemSearchTimer: null,
            loadingUpdateOrder: false,
        };
    },
    computed: {
        ordersReportPeriodColumn() {
            return this.formatReportPeriod(this.search.startDate, this.search.endDate);
        },
        advancedReportsPeriodColumn() {
            return this.formatReportPeriod(this.reportFilters.startDate, this.reportFilters.endDate);
        },
        ordersForTable() {
            const p = this.ordersReportPeriodColumn;
            return (this.Orders || []).map((row) => ({ ...row, reportPeriod: p }));
        },
        topSellingItemsForTable() {
            const p = this.advancedReportsPeriodColumn;
            return (this.topSellingItems || []).map((row) => ({ ...row, reportPeriod: p }));
        },
        salesByCategoryForTable() {
            const p = this.advancedReportsPeriodColumn;
            return (this.salesByCategory || []).map((row) => ({ ...row, reportPeriod: p }));
        },
        salesByEmployeeForTable() {
            const p = this.advancedReportsPeriodColumn;
            return (this.salesByEmployee || []).map((row) => ({ ...row, reportPeriod: p }));
        },
        returnedItemsForTable() {
            const p = this.advancedReportsPeriodColumn;
            return (this.returnedItems || []).map((row) => ({ ...row, reportPeriod: p }));
        },
        deliveryDriversForTable() {
            const p = this.$t("reportPeriodCumulative") || "كل الفترات (تراكمي)";
            const drivers = this.deliveryStatistics?.drivers || [];
            return drivers.map((d) => ({ ...d, reportPeriod: p }));
        },
        expensesByCategoryForTable() {
            const p = this.formatReportPeriod(this.reportFilters.startDate, this.reportFilters.endDate);
            const rows = this.expensesReport?.expensesByCategory || [];
            return rows.map((row) => ({ ...row, reportPeriod: p }));
        },
        formattedNumber() {
            return this.totaPrice.toLocaleString()
        },
        orderModalSubtotal() {
            return Number(this.order?.orderSubTotal ?? this.totaPrice ?? 0);
        },
        orderModalDiscountAmount() {
            return Number(this.order?.discountAmount ?? 0);
        },
        orderModalFinalTotal() {
            const fallbackTotal = Number(this.totaPrice ?? 0);
            const orderFinal = this.order?.orderTotalAfterDiscount;
            if (orderFinal !== null && orderFinal !== undefined) return Number(orderFinal);
            return Math.max(this.orderModalSubtotal - this.orderModalDiscountAmount, fallbackTotal);
        },
        orderModalDiscountLabel() {
            const type = this.order?.discountType;
            const value = Number(this.order?.discountValue ?? 0);
            if (type === 'percentage') return `${Math.min(value, 100)}%`;
            return `${this.formatPrice(value)} ${this.$t('currency')}`;
        },
        orderDetailRows() {
            if (!this.order) return [];

            const details = [];
            const pushDetail = (key, label, value, emphasis = false) => {
                if (!this.isMeaningfulValue(value)) return;
                details.push({ key, label, value, emphasis });
            };

            pushDetail('order-status', this.$t('status') || 'الحالة', this.getOrderStatusText(this.order.orderStatus));
            pushDetail('daily-sequence', this.$t('orderNumber') || 'رقم الطلب اليومي', this.order.dailySequenceNumber ? `#${this.order.dailySequenceNumber}` : '');
            pushDetail('tables', this.$t('table') || 'الطاولة', this.getOrderTablesText(this.order));

            const peopleCount = this.getOrderPeopleCount(this.order);
            pushDetail('people-count', this.$t('peopleCount') || 'عدد الأشخاص', peopleCount);

            pushDetail('delivery-customer', this.$t('customerName') || 'اسم العميل', this.order.deliveryCustomerName);
            pushDetail('delivery-phone', this.$t('phoneNumber') || 'رقم الهاتف', this.order.deliveryPhoneNumber);
            pushDetail('delivery-address', this.$t('address') || 'العنوان', this.order.deliveryAddress);
            pushDetail('delivery-driver', this.$t('driverName') || 'اسم السائق', this.order.deliveryDriver?.name || this.order.deliveryDriver?.username);
            pushDetail('delivery-status', this.$t('deliveryStatus') || 'حالة التوصيل', this.getDeliveryStatusText(this.order.deliveryStatus));

            if (this.order.paymentMethod === 'Credit') {
                const creditEmployeeName = this.order.creditEmployeeName;
                const creditCustomerName = this.order.creditCustomerName;
                const creditAccountName = creditEmployeeName || creditCustomerName || '-';
                const creditAccountLabel = creditEmployeeName
                    ? (this.$t('creditAccountEmployee') || 'حساب الموظف')
                    : (this.$t('creditAccountCustomer') || 'حساب العميل');
                pushDetail('credit-account', creditAccountLabel, creditAccountName, true);
            }

            if (Number(this.order.deliveryFee || 0) > 0) {
                pushDetail(
                    'delivery-fee',
                    this.$t('deliveryFee') || 'رسوم التوصيل',
                    `${this.formatPrice(Number(this.order.deliveryFee || 0))} ${this.$t('currency')}`,
                    true
                );
            }

            if (this.orderModalDiscountAmount > 0) {
                pushDetail(
                    'discount',
                    `${this.$t('discountLabel') || 'الخصم'} (${this.orderModalDiscountLabel})`,
                    `- ${this.formatPrice(this.orderModalDiscountAmount)} ${this.$t('currency')}`,
                    true
                );
            }

            pushDetail('notes', this.$t('orderNotes') || 'ملاحظات الطلب', this.order.notes);
            return details;
        },
        editOrderTotal() {
            return this.editOrderForm.items.reduce((sum, item) => {
                return sum + (item.price * item.quantity);
            }, 0);
        },
        editOrderDiscountAmount() {
            const rawValue = Number(this.editOrderForm.discountValue) || 0;
            if (rawValue <= 0) return 0;
            if (this.editOrderForm.discountType === 'percentage') {
                return Math.min(this.editOrderTotal, (this.editOrderTotal * Math.min(rawValue, 100)) / 100);
            }
            return Math.min(this.editOrderTotal, rawValue);
        },
        editOrderFinalTotal() {
            return Math.max(this.editOrderTotal - this.editOrderDiscountAmount, 0);
        },
        editOrderDiscountPreviewLabel() {
            if (this.editOrderForm.discountType === 'percentage') {
                const percent = Number(this.editOrderForm.discountValue) || 0;
                return `${Math.min(percent, 100)}%`;
            }
            return `${this.formatPrice(Number(this.editOrderForm.discountValue) || 0)} ${this.$t('currency')}`;
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
                // Use discount price if available, otherwise use selling price
                const sellingPrice = this.getSellingPrice(item);
                return {
                    ...item,
                    totalPrice: item.quantity * sellingPrice,
                };
            });
        },
        returnedItemsFields() {
            return [
                {
                    key: 'reportPeriod',
                    label: this.$t('reportDateRange') || 'فترة التقرير',
                    sortable: false
                },
                {
                    key: 'itemName',
                    label: this.$t('itemName') || 'اسم المنتج',
                    sortable: true
                },
                {
                    key: 'quantity',
                    label: this.$t('quantity') || 'الكمية',
                    sortable: true
                },
                {
                    key: 'unitPrice',
                    label: this.$t('unitPrice') || 'سعر الوحدة',
                    sortable: true
                },
                {
                    key: 'lineTotal',
                    label: this.$t('lineTotal') || 'المجموع',
                    sortable: true
                },
                {
                    key: 'orderCode',
                    label: this.$t('invoiceNumber') || 'رقم الفاتورة',
                    sortable: true
                },
                {
                    key: 'tableDisplay',
                    label: this.$t('table') || 'الطاولة',
                    sortable: false
                },
                {
                    key: 'orderType',
                    label: this.$t('orderType') || 'نوع الطلب',
                    sortable: true
                },
                {
                    key: 'deletedByUsername',
                    label: this.$t('deletedBy') || 'حذف بواسطة',
                    sortable: true
                },
                {
                    key: 'insertDate',
                    label: this.$t('deletedAt') || 'وقت الحذف',
                    sortable: true
                }
            ];
        },
        driversStatisticsFields() {
            return [
                {
                    key: 'reportPeriod',
                    label: this.$t('reportDateRange') || 'فترة التقرير',
                    sortable: false,
                },
                {
                    key: 'driverName',
                    label: this.$t('driverName') || 'اسم السائق',
                    sortable: true
                },
                {
                    key: 'phoneNumber',
                    label: this.$t('phoneNumber') || 'رقم الهاتف',
                    sortable: false
                },
                {
                    key: 'status',
                    label: this.$t('status') || 'الحالة',
                    sortable: true
                },
                {
                    key: 'totalOrders',
                    label: this.$t('totalOrders') || 'إجمالي الطلبات',
                    sortable: true
                },
                {
                    key: 'deliveredOrders',
                    label: this.$t('deliveredOrders') || 'واصلة',
                    sortable: true
                },
                {
                    key: 'pendingOrders',
                    label: this.$t('pendingDeliveries') || 'معلقة',
                    sortable: true
                },
                {
                    key: 'failedOrders',
                    label: this.$t('failedDeliveries') || 'فاشلة',
                    sortable: true
                },
                {
                    key: 'totalAmount',
                    label: this.$t('totalAmount') || 'إجمالي المبلغ',
                    sortable: true
                },
                {
                    key: 'paidAmount',
                    label: this.$t('paidAmount') || 'مدفوع',
                    sortable: true
                },
                {
                    key: 'remainingAmount',
                    label: this.$t('remainingAmount') || 'متبقي',
                    sortable: true
                }
            ];
        },
        topSellingItemsFields() {
            return [
                {
                    key: 'reportPeriod',
                    label: this.$t('reportDateRange') || 'فترة التقرير',
                    sortable: false,
                },
                {
                    key: 'rank',
                    label: this.$t('rank') || 'الترتيب',
                    sortable: false
                },
                {
                    key: 'itemName',
                    label: this.$t('itemName') || 'اسم المنتج',
                    sortable: true
                },
                {
                    key: 'itemCode',
                    label: this.$t('itemCode') || 'الكود',
                    sortable: true
                },
                {
                    key: 'totalQuantitySold',
                    label: this.$t('quantitySold') || 'الكمية المباعة',
                    sortable: true
                },
                {
                    key: 'totalSales',
                    label: this.$t('totalSales') || 'إجمالي المبيعات',
                    sortable: true
                },
                {
                    key: 'orderCount',
                    label: this.$t('orderCount') || 'عدد الطلبات',
                    sortable: true
                },
                {
                    key: 'averagePrice',
                    label: this.$t('averagePrice') || 'متوسط السعر',
                    sortable: true
                }
            ];
        },
        salesByCategoryFields() {
            return [
                {
                    key: 'reportPeriod',
                    label: this.$t('reportDateRange') || 'فترة التقرير',
                    sortable: false,
                },
                {
                    key: 'category',
                    label: this.$t('category') || 'الفئة',
                    sortable: true
                },
                {
                    key: 'totalSales',
                    label: this.$t('totalSales') || 'إجمالي المبيعات',
                    sortable: true
                },
                {
                    key: 'totalExpenses',
                    label: this.$t('categoryExpensesLabel') || 'صرفيات الفئة',
                    sortable: true
                },
                {
                    key: 'totalQuantity',
                    label: this.$t('totalQuantity') || 'إجمالي الكمية',
                    sortable: true
                },
                {
                    key: 'itemCount',
                    label: this.$t('itemCount') || 'عدد المنتجات',
                    sortable: true
                },
                {
                    key: 'orderCount',
                    label: this.$t('orderCount') || 'عدد الطلبات',
                    sortable: true
                },
                {
                    key: 'averageOrderValue',
                    label: this.$t('averageOrderValue') || 'متوسط قيمة الطلب',
                    sortable: true
                }
            ];
        },
        expensesReportFields() {
            return [
                { key: 'reportPeriod', label: this.$t('reportDateRange') || 'فترة التقرير', sortable: false },
                { key: 'category', label: this.$t('category') || 'الفئة', sortable: true },
                { key: 'totalAmount', label: this.$t('totalExpenses') || 'إجمالي الصرفيات', sortable: true },
                { key: 'count', label: this.$t('count') || 'العدد', sortable: true }
            ];
        },
        salesByEmployeeFields() {
            return [
                {
                    key: 'reportPeriod',
                    label: this.$t('reportDateRange') || 'فترة التقرير',
                    sortable: false,
                },
                {
                    key: 'employeeName',
                    label: this.$t('employeeName') || 'اسم الموظف',
                    sortable: true
                },
                {
                    key: 'totalOrders',
                    label: this.$t('totalOrders') || 'إجمالي الطلبات',
                    sortable: true
                },
                {
                    key: 'totalSales',
                    label: this.$t('totalSales') || 'إجمالي المبيعات',
                    sortable: true
                },
                {
                    key: 'totalItemsSold',
                    label: this.$t('totalItemsSold') || 'إجمالي المواد المباعة',
                    sortable: true
                },
                {
                    key: 'averageOrderValue',
                    label: this.$t('averageOrderValue') || 'متوسط قيمة الطلب',
                    sortable: true
                },
                {
                    key: 'itemsPerOrder',
                    label: this.$t('itemsPerOrder') || 'مواد لكل طلب',
                    sortable: true
                }
            ];
        },
        ordersTableFields() {
            return [
                { key: 'reportPeriod', label: this.$t('reportDateRange') || 'فترة التقرير', sortable: false },
                { key: 'orderCode', label: this.$t('invoice_number') || 'رقم الفاتورة', sortable: true },
                { key: 'insertDate', label: this.$t('date') || 'التاريخ', sortable: true },
                { key: 'dailySequenceNumber', label: this.$t('orderNumber') || 'الرقم اليومي', sortable: true },
                { key: 'orderType', label: this.$t('orderType') || 'نوع الطلب', sortable: true },
                { key: 'paymentMethod', label: this.$t('paymentMethod') || 'طريقة الدفع', sortable: true },
                { key: 'itemsCount', label: this.$t('items_count') || 'عدد العناصر', sortable: true },
                { key: 'discountAmount', label: this.$t('discountLabel') || 'الخصم', sortable: true },
                { key: 'totalAmount', label: this.$t('invoice_amount') || 'مبلغ الفاتورة', sortable: true },
                { key: 'createdByUsername', label: this.$t('employeeLabel') || 'الحساب المنشئ', sortable: true },
                { key: 'actions', label: this.$t('actions') || 'الإجراءات' }
            ];
        },
        hasActiveFilters() {
            return this.search.orderType || 
                   this.search.paymentMethod || 
                   this.search.deliveryDriverId ||
                   this.search.startDate || 
                   this.search.endDate ||
                   this.search.info;
        },
        hasAdvancedFilters() {
            return this.reportFilters.orderType || 
                   this.reportFilters.paymentMethod || 
                   this.reportFilters.startDate || 
                   this.reportFilters.endDate ||
                   this.reportFilters.staffRoleFilter ||
                   this.reportFilters.salesByEmployeeUserId;
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
                    this.GetAllOrders();
                }, 500);
            },
            deep: true,
        },

        pageNumber() {
            this.GetAllOrders();
        },
        returnedItemsPageNumber() {
            if (this.activeTab === 'returnedItems') {
                this.loadReturnedItems();
            }
        },
    },

    mounted() {
        const routeQuery = this.$route?.query || {};
        const hasDriverFilter = !!routeQuery.deliveryDriverId;
        if (hasDriverFilter) {
            this.search.deliveryDriverId = String(routeQuery.deliveryDriverId);
            this.search.startDate = routeQuery.startDate ? String(routeQuery.startDate) : "";
            this.search.endDate = routeQuery.endDate ? String(routeQuery.endDate) : "";
            this.activeTab = "orders";
        }
        this.GetAllOrders();
        this.userInfo = JSON.parse(localStorage.getItem('info'));
        this.loadDeliveryDrivers();
        this.loadSalesReportStaff();
    },
    
    beforeDestroy() {
        // Clear search timer to prevent memory leaks
        if (this.searchTimer) {
            clearTimeout(this.searchTimer);
        }
    },

    methods: {
        hasDiscount(item) {
            return item.item && 
                   item.item.disCountPrice && 
                   item.item.disCountPrice > 0 && 
                   item.item.disCountPrice !== item.item.sellingPrice;
        },
        getSellingPrice(item) {
            if (this.hasDiscount(item)) {
                return item.item.disCountPrice;
            }
            return item.item.sellingPrice;
        },
        getRankClass(index) {
            if (index === 0) return 'rank-gold';
            if (index === 1) return 'rank-silver';
            if (index === 2) return 'rank-bronze';
            return '';
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
        getPaymentMethodIcon(method) {
            if (!method) return 'cash-stack';
            const icons = {
                'Cash': 'cash-stack',
                'Card': 'credit-card',
                'Credit': 'clock-history'
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
        getOrderStatusText(status) {
            if (!status) return '';
            const statuses = {
                Pending: this.$t('pending') || 'قيد الانتظار',
                InProgress: this.$t('inProgress') || 'قيد التحضير',
                Completed: this.$t('completed') || 'مكتمل',
                Cancelled: this.$t('cancelled') || 'ملغي'
            };
            return statuses[status] || status;
        },
        getDeliveryStatusText(status) {
            if (!status) return '';
            const statuses = {
                Pending: this.$t('pending') || 'قيد الانتظار',
                OnTheWay: this.$t('onTheWay') || 'في الطريق',
                Delivered: this.$t('delivered') || 'تم التسليم',
                Failed: this.$t('failed') || 'فشل التسليم'
            };
            return statuses[status] || status;
        },
        getOrderTablesText(order) {
            if (!order) return '';
            if (order.mergedTableNumbers) return order.mergedTableNumbers;

            if (Array.isArray(order.tables) && order.tables.length > 0) {
                return order.tables
                    .map((table) => table?.tableNumber)
                    .filter((tableNumber) => this.isMeaningfulValue(tableNumber))
                    .join(' - ');
            }

            return '';
        },
        getOrderPeopleCount(order) {
            if (!order) return '';

            const directCount =
                order.peopleCount ??
                order.personCount ??
                order.guestCount ??
                order.customerCount;

            if (this.isMeaningfulValue(directCount)) {
                return directCount;
            }

            if (Array.isArray(order.tables) && order.tables.length > 0) {
                const totalCapacity = order.tables.reduce((sum, table) => {
                    const tableCapacity = Number(table?.capacity || 0);
                    return sum + (Number.isFinite(tableCapacity) ? tableCapacity : 0);
                }, 0);
                if (totalCapacity > 0) {
                    return totalCapacity;
                }
            }

            return '';
        },
        isMeaningfulValue(value) {
            if (value === null || value === undefined) return false;
            if (typeof value === 'string') return value.trim().length > 0;
            return true;
        },
        formatDate(dateTime) {
            if (dateTime == null || dateTime === "") return "";
            const s = String(dateTime);
            if (!s.includes("T")) {
                return (s.split(" ")[0] || s).trim();
            }
            const [date, timePart] = s.split("T");
            const time = timePart ? timePart.split(".")[0] : "";
            return time ? `${date} ${time}` : date;
        },
        /** عرض نطاق التواريخ المختار في تقارير الجداول */
        formatReportPeriod(startStr, endStr) {
            if (!startStr && !endStr) {
                return this.$t("allDatesRange") || "كل التواريخ";
            }
            const fmt = (d) => (d ? this.formatDate(`${d}T12:00:00`) : "");
            if (startStr && endStr) {
                return `${fmt(startStr)} – ${fmt(endStr)}`;
            }
            if (startStr) {
                return `${this.$t("from_date") || "من"} ${fmt(startStr)}`;
            }
            return `${this.$t("to_date") || "إلى"} ${fmt(endStr)}`;
        },
        staffRoleFilterLabel() {
            const v = this.reportFilters.staffRoleFilter;
            if (!v) return this.$t("salesByEmployeeAllStaff") || "كل الحسابات";
            if (v === "SalesStaff") return this.$t("salesByEmployeePosAndWaiter") || "كاشير ونادل فقط";
            if (v === "POS") return this.$t("rolePOS") || "كاشير (POS)";
            if (v === "Waiter") return this.$t("roleWaiter") || "نادل";
            return v;
        },
        salesReportStaffRoleLabel(role) {
            if (role === "POS") return this.$t("rolePOS") || "كاشير (POS)";
            if (role === "Waiter") return this.$t("roleWaiter") || "نادل";
            return role || "";
        },
        salesByEmployeeStaffFilterLabel() {
            const id = this.reportFilters.salesByEmployeeUserId;
            if (!id) return this.$t("salesByEmployeeAllEmployees") || "كل الموظفين";
            const s = (this.salesReportStaffList || []).find((x) => String(x.id) === String(id));
            if (!s) return String(id);
            return `${s.name} (${this.salesReportStaffRoleLabel(s.role)})`;
        },
        formatPrice(price) {
            if (price) {
                return price.toLocaleString("en-EG");
            }
            return "0";
        },
        print(printTargetId = 'print') {
            const targetNode = document.getElementById(printTargetId);
            if (!targetNode) return;
            const prtHtml = targetNode.innerHTML;
            
            // Professional POS printer styles (80mm thermal printer)
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

                    .bill-extra-details {
                        margin-top: 8px;
                        padding-top: 6px;
                        border-top: 1px dashed #000;
                    }

                    .bill-extra-title {
                        font-size: 10px;
                        font-weight: 700;
                        margin-bottom: 6px;
                    }

                    .bill-extra-grid {
                        display: grid;
                        grid-template-columns: 1fr;
                        gap: 4px;
                    }

                    .bill-extra-card {
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        gap: 6px;
                    }

                    .bill-extra-label {
                        font-size: 9px;
                        font-weight: 600;
                    }

                    .bill-extra-value {
                        font-size: 9px;
                        text-align: left;
                        word-break: break-word;
                    }

                    .bill-extra-value--emphasis {
                        font-weight: 700;
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
            
            const WinPrint = window.open('', '', 'left=0,top=0,width=400,height=600,toolbar=0,scrollbars=0,status=0');
            WinPrint.document.write(`<!DOCTYPE html>
                <html>
                <head>
                    <meta charset="UTF-8">
                    ${stylesHtml}
                </head>
                <body>
                    ${prtHtml}
                </body>
                </html>`);

            WinPrint.document.close();
            WinPrint.focus();
            
            // Wait a bit before printing to ensure content is loaded
            setTimeout(() => {
                WinPrint.print();
                setTimeout(() => {
                    WinPrint.close();
                }, 100);
            }, 250);
        },
        printEditOrder() {
            this.print('print-edit-order');
        },

        showItemsModel(items, order) {
            this.customerOrderItem = items;
            this.order = order;
            this.$bvModal.show("modal-itemList");
        },

        getItemInfo(item) {
            this.editForm = item;
            this.$bvModal.show("modal-editItem");
        },



        closeModel(id) {
            this.$bvModal.hide(id);
        },

        editOrder(order) {
            this.editOrderData = order;
            this.editOrderForm = {
                paymentMethod: order.paymentMethod || 'Cash',
                orderType: order.orderType || 'DineIn',
                notes: order.notes || '',
                discountType: order.discountType || null,
                discountValue: order.discountValue ?? null,
                items: order.customerOrderItem ? order.customerOrderItem.map(item => ({
                    id: item.item?.id || item.itemId,
                    name: item.item?.name || '',
                    code: item.item?.code || '',
                    price: item.sellingPrice,
                    quantity: item.quantity,
                    itemId: item.itemId
                })) : []
            };
            this.$bvModal.show('modal-edit-order');
        },

        closeEditOrderModal() {
            this.editOrderData = null;
            this.editOrderForm = {
                paymentMethod: 'Cash',
                orderType: 'DineIn',
                notes: '',
                discountType: null,
                discountValue: null,
                items: []
            };
            this.$bvModal.hide('modal-edit-order');
        },

        increaseEditItemQuantity(index) {
            if (this.editOrderForm.items[index]) {
                this.editOrderForm.items[index].quantity++;
            }
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

        closeAddItemModal() {
            this.itemSearchQuery = '';
            this.availableItems = [];
            this.$bvModal.hide('modal-add-item');
        },

        searchItems() {
            if (this.itemSearchTimer) {
                clearTimeout(this.itemSearchTimer);
            }
            this.itemSearchTimer = setTimeout(() => {
                if (this.itemSearchQuery && this.itemSearchQuery.length >= 2) {
                    HTTP.get(`Admin/GetItems?pageNumber=0&pageSize=20&info=${encodeURIComponent(this.itemSearchQuery)}`)
                        .then((response) => {
                            this.availableItems = response.data.data.items || [];
                        })
                        .catch((error) => {
                            console.error('Error searching items:', error);
                            this.availableItems = [];
                        });
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
                const price = item.disCountPrice > 0 && item.disCountPrice < item.sellingPrice
                    ? item.disCountPrice
                    : item.sellingPrice;
                this.editOrderForm.items.push({
                    id: item.id,
                    name: item.name,
                    code: item.code,
                    price: price,
                    quantity: 1,
                    itemId: item.id
                });
            }
            this.closeAddItemModal();
        },

        async updateOrder() {
            if (!this.editOrderData) return;

            if (this.editOrderForm.items.length === 0) {
                this.$toast.error(this.$i18n.t('emptyCartMessage') || 'السلة فارغة', {
                    position: "top-right",
                    timeout: 3000,
                });
                return;
            }

            this.loadingUpdateOrder = true;
            try {
                const request = {
                    paymentMethod: this.editOrderForm.paymentMethod,
                    orderType: this.editOrderForm.orderType,
                    notes: this.editOrderForm.notes,
                    discountType: this.editOrderDiscountAmount > 0 ? this.editOrderForm.discountType : null,
                    discountValue: this.editOrderDiscountAmount > 0 ? (Number(this.editOrderForm.discountValue) || 0) : null,
                    discountAmount: this.editOrderDiscountAmount > 0 ? this.editOrderDiscountAmount : 0,
                    discountPercent: this.editOrderForm.discountType === 'percentage' ? (Number(this.editOrderForm.discountValue) || 0) : 0,
                    orderSubTotal: this.editOrderTotal,
                    orderTotalAfterDiscount: this.editOrderFinalTotal,
                    customerOrderItem: this.editOrderForm.items.map(item => ({
                        itemId: item.itemId || item.id,
                        quantity: item.quantity
                    })),
                    tableId: this.editOrderData.tableId || null,
                    reservationId: this.editOrderData.reservationId || null
                };

                const response = await HTTP.put(`Admin/UpdateOrder/${this.editOrderData.id}`, request);
                
                if (response.data && !response.data.errorStatus) {
                    this.$toast.success(response.data.message || this.$i18n.t('orderUpdatedSuccessfully') || 'تم تحديث الفاتورة بنجاح', {
                        position: "top-right",
                        timeout: 3000,
                    });
                    this.closeEditOrderModal();
                    this.GetAllOrders();
                } else {
                    this.$toast.error(response.data?.message || this.$i18n.t('error') || 'حدث خطأ', {
                        position: "top-right",
                        timeout: 3000,
                    });
                }
            } catch (error) {
                console.error('Error updating order:', error);
                this.$toast.error(error.response?.data?.message || this.$i18n.t('error') || 'حدث خطأ أثناء تحديث الفاتورة', {
                    position: "top-right",
                    timeout: 3000,
                });
            } finally {
                this.loadingUpdateOrder = false;
            }
        },

        GetAllOrders() {
            this.show = true;
            const params = new URLSearchParams();
            params.append('pageNumber', (this.pageNumber - 1).toString());
            params.append('pageSize', this.pageSize.toString());
            if (this.search.info) params.append('info', this.search.info);
            if (this.search.startDate) params.append('startDate', this.search.startDate);
            if (this.search.endDate) params.append('endDate', this.search.endDate);
            if (this.search.orderType) params.append('orderType', this.search.orderType);
            if (this.search.paymentMethod) params.append('paymentMethod', this.search.paymentMethod);
            if (this.search.deliveryDriverId) params.append('deliveryDriverId', this.search.deliveryDriverId);
            
            HTTP.get(`Admin/GetOrders?${params.toString()}`)
                .then((response) => {
                    this.Orders = response.data.data.items;
                    this.totalOrders = response.data.data.totalItems;
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                });
        },
        async loadDeliveryDrivers() {
            try {
                this.loadingDeliveryDrivers = true;
                const response = await HTTP.get('DeliveryDrivers');
                if (response.data && !response.data.errorStatus) {
                    this.deliveryDrivers = response.data.data || [];
                } else {
                    this.deliveryDrivers = [];
                }
            } catch (error) {
                console.error('Error loading delivery drivers:', error);
                this.deliveryDrivers = [];
            } finally {
                this.loadingDeliveryDrivers = false;
            }
        },
        clearFilters() {
            this.search = {
                info: "",
                startDate: "",
                endDate: "",
                orderType: "",
                paymentMethod: "",
                deliveryDriverId: "",
            };
            this.GetAllOrders();
        },
        clearAdvancedFilters() {
            this.reportFilters = {
                startDate: "",
                endDate: "",
                orderType: "",
                paymentMethod: "",
                staffRoleFilter: "",
                salesByEmployeeUserId: "",
            };
            this.loadAdvancedReport();
        },

        // Advanced Reports Methods
        loadAdvancedReport() {
            if (this.activeTab === 'profit') {
                this.loadProfitReport();
            } else if (this.activeTab === 'topItems') {
                this.loadTopSellingItems();
            } else if (this.activeTab === 'byCategory') {
                this.loadSalesByCategory();
            } else if (this.activeTab === 'byEmployee') {
                this.loadSalesByEmployee();
            } else if (this.activeTab === 'returnedItems') {
                this.returnedItemsPageNumber = 1;
                this.loadReturnedItems();
            } else if (this.activeTab === 'delivery') {
                this.loadDeliveryStatistics();
            }
        },

        loadProfitReport() {
            this.show = true;
            const params = new URLSearchParams();
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
            if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
            
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
            if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
            if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
            
            HTTP.get(`Admin/GetTopSellingItems?${params.toString()}`)
                .then((response) => {
                    this.topSellingItems = response.data.data || [];
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
            if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
            if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
            
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

        loadSalesReportStaff() {
            HTTP.get("Admin/GetSalesReportStaff")
                .then((response) => {
                    this.salesReportStaffList = response.data.data || [];
                })
                .catch((error) => {
                    this.salesReportStaffList = [];
                    console.error("Error loading sales report staff:", error);
                });
        },

        loadSalesByEmployee() {
            this.show = true;
            const params = new URLSearchParams();
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
            if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
            if (this.reportFilters.staffRoleFilter) params.append('roleFilter', this.reportFilters.staffRoleFilter);
            if (this.reportFilters.salesByEmployeeUserId) params.append('createdByUserId', this.reportFilters.salesByEmployeeUserId);
            
            HTTP.get(`Admin/GetSalesByEmployee?${params.toString()}`)
                .then((response) => {
                    this.salesByEmployee = response.data.data || [];
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    this.salesByEmployee = [];
                    const status = error?.response?.status;
                    const apiMsg = error?.response?.data?.message;
                    if (status === 400 && apiMsg) {
                        this.$toast.error(apiMsg, {
                            position: 'top-right',
                            timeout: 4000,
                            rtl: this.$i18n.locale === 'ar',
                        });
                    } else {
                        console.error('Error loading sales by employee:', error);
                    }
                });
        },

        loadReturnedItems() {
            this.show = true;
            const params = new URLSearchParams();
            params.append('pageNumber', (this.returnedItemsPageNumber - 1).toString());
            params.append('pageSize', this.returnedItemsPageSize.toString());
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            if (this.search.info) params.append('info', this.search.info);

            HTTP.get(`Admin/GetReturnedOrderItems?${params.toString()}`)
                .then((response) => {
                    const payload = response?.data?.data;
                    this.returnedItems = payload?.items || [];
                    this.totalReturnedItems = payload?.totalItems || 0;
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                    this.returnedItems = [];
                    this.totalReturnedItems = 0;
                    console.error('Error loading returned items:', error);
                });
        },

        loadDeliveryStatistics() {
            this.loadingDeliveryStatistics = true;
            HTTP.get('DeliveryDrivers/Statistics/All')
                .then((response) => {
                    if (response.data && !response.data.errorStatus) {
                        this.deliveryStatistics = response.data.data || null;
                    } else {
                        this.deliveryStatistics = null;
                    }
                    this.loadingDeliveryStatistics = false;
                })
                .catch((error) => {
                    console.error('Error loading delivery statistics:', error);
                    this.deliveryStatistics = null;
                    this.loadingDeliveryStatistics = false;
                    this.$toast.error(this.$i18n.t("failedToLoadStatistics") || 'فشل تحميل إحصائيات التوصيل', {
                        position: "top-right",
                        timeout: 3000,
                        rtl: this.$i18n.locale === 'ar'
                    });
                });
        },

        loadExpensesReport() {
            this.loadingExpensesReport = true;
            const params = new URLSearchParams();
            if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
            if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
            HTTP.get(`Expenses/Statistics?${params.toString()}`)
                .then((response) => {
                    if (response.data && !response.data.errorStatus) {
                        this.expensesReport = response.data.data || null;
                    } else {
                        this.expensesReport = null;
                    }
                    this.loadingExpensesReport = false;
                })
                .catch((error) => {
                    console.error('Error loading expenses report:', error);
                    this.expensesReport = null;
                    this.loadingExpensesReport = false;
                });
        },

        csvEscape(val) {
            if (val == null) return '';
            const s = String(val);
            if (/[,"\n\r]/.test(s)) return '"' + s.replace(/"/g, '""') + '"';
            return s;
        },
        downloadCsv(content, filename) {
            const BOM = '\uFEFF';
            const blob = new Blob([BOM + content], { type: 'text/csv;charset=utf-8;' });
            const link = document.createElement('a');
            const url = URL.createObjectURL(blob);
            link.setAttribute('href', url);
            link.setAttribute('download', filename);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        },
        async exportCurrentReportExcel() {
            this.exportingExcel = true;
            const dateStr = new Date().toISOString().split('T')[0];
            try {
                if (this.activeTab === 'orders') {
                    const params = new URLSearchParams();
                    if (this.search.info) params.append('info', this.search.info);
                    if (this.search.startDate) params.append('startDate', this.search.startDate);
                    if (this.search.endDate) params.append('endDate', this.search.endDate);
                    if (this.search.orderType) params.append('orderType', this.search.orderType);
                    if (this.search.paymentMethod) params.append('paymentMethod', this.search.paymentMethod);
                    if (this.search.deliveryDriverId) params.append('deliveryDriverId', this.search.deliveryDriverId);
                    const response = await HTTP.get(`Admin/ExportOrders?${params.toString()}`, { responseType: 'blob' });
                    const blob = new Blob([response.data], { type: 'text/csv;charset=utf-8;' });
                    const link = document.createElement('a');
                    link.href = URL.createObjectURL(blob);
                    link.download = `orders_${dateStr}.csv`;
                    link.style.visibility = 'hidden';
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                    URL.revokeObjectURL(link.href);
                } else if (this.activeTab === 'profit') {
                    const params = new URLSearchParams();
                    if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
                    if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
                    if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
                    if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
                    const res = await HTTP.get(`Admin/GetProfitReport?${params.toString()}`);
                    const r = res.data?.data;
                    if (!r || Object.keys(r).length === 0) {
                        this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                    } else {
                        const csv = [ [this.$t('totalSales') || 'إجمالي المبيعات', this.$t('totalCost') || 'إجمالي التكلفة', this.$t('totalProfit') || 'إجمالي الربح', this.$t('profitMargin') || 'هامش الربح (%)'].map(this.csvEscape).join(','), [r.totalSales ?? '', r.totalCost ?? '', r.totalProfit ?? '', r.profitMargin ?? ''].map(this.csvEscape).join(',') ].join('\r\n');
                        this.downloadCsv(csv, `profit_report_${dateStr}.csv`);
                    }
                } else if (this.activeTab === 'topItems') {
                    const params = new URLSearchParams();
                    params.append('topCount', '9999');
                    if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
                    if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
                    if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
                    if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
                    const res = await HTTP.get(`Admin/GetTopSellingItems?${params.toString()}`);
                    const list = res.data?.data || [];
                    if (!list.length) {
                        this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                    } else {
                        const period = this.formatReportPeriod(this.reportFilters.startDate, this.reportFilters.endDate);
                        const headers = [this.$t('reportDateRange') || 'فترة التقرير', this.$t('rank') || 'الترتيب', this.$t('itemName') || 'اسم المنتج', this.$t('itemCode') || 'الكود', this.$t('quantitySold') || 'الكمية المباعة', this.$t('totalSales') || 'إجمالي المبيعات', this.$t('orderCount') || 'عدد الطلبات', this.$t('averagePrice') || 'متوسط السعر'];
                        let csv = headers.map(this.csvEscape).join(',') + '\r\n';
                        list.forEach((item, i) => {
                            csv += [period, i + 1, item.itemName || '', item.itemCode || '', item.totalQuantitySold ?? '', item.totalSales ?? '', item.orderCount ?? '', item.totalQuantitySold ? (item.totalSales / item.totalQuantitySold) : ''].map(this.csvEscape).join(',') + '\r\n';
                        });
                        this.downloadCsv(csv, `top_selling_items_${dateStr}.csv`);
                    }
                } else if (this.activeTab === 'byCategory') {
                    const params = new URLSearchParams();
                    if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
                    if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
                    if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
                    if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
                    const res = await HTTP.get(`Admin/GetSalesByCategory?${params.toString()}`);
                    const list = res.data?.data || [];
                    if (!list.length) {
                        this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                    } else {
                        const period = this.formatReportPeriod(this.reportFilters.startDate, this.reportFilters.endDate);
                        const headers = [this.$t('reportDateRange') || 'فترة التقرير', this.$t('category') || 'الفئة', this.$t('totalSales') || 'إجمالي المبيعات', this.$t('categoryExpensesLabel') || 'صرفيات الفئة', this.$t('totalQuantity') || 'إجمالي الكمية', this.$t('itemCount') || 'عدد المنتجات', this.$t('orderCount') || 'عدد الطلبات'];
                        let csv = headers.map(this.csvEscape).join(',') + '\r\n';
                        list.forEach(item => {
                            csv += [period, item.category, item.totalSales ?? '', item.totalExpenses ?? '', item.totalQuantity ?? '', item.itemCount ?? '', item.orderCount ?? ''].map(this.csvEscape).join(',') + '\r\n';
                        });
                        this.downloadCsv(csv, `sales_by_category_${dateStr}.csv`);
                    }
                } else if (this.activeTab === 'byEmployee') {
                    const params = new URLSearchParams();
                    if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
                    if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
                    if (this.reportFilters.orderType) params.append('orderType', this.reportFilters.orderType);
                    if (this.reportFilters.paymentMethod) params.append('paymentMethod', this.reportFilters.paymentMethod);
                    if (this.reportFilters.staffRoleFilter) params.append('roleFilter', this.reportFilters.staffRoleFilter);
                    if (this.reportFilters.salesByEmployeeUserId) params.append('createdByUserId', this.reportFilters.salesByEmployeeUserId);
                    const res = await HTTP.get(`Admin/GetSalesByEmployee?${params.toString()}`);
                    const list = res.data?.data || [];
                    if (!list.length) {
                        this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                    } else {
                        const period = this.formatReportPeriod(this.reportFilters.startDate, this.reportFilters.endDate);
                        const roleCol = this.staffRoleFilterLabel();
                        const staffCol = this.salesByEmployeeStaffFilterLabel();
                        const headers = [this.$t('reportDateRange') || 'فترة التقرير', this.$t('reportStaffRoleFilter') || 'تصفية الدور', this.$t('reportSelectedEmployee') || 'الموظف المحدد', this.$t('employeeName') || 'اسم الموظف', this.$t('totalOrders') || 'إجمالي الطلبات', this.$t('totalSales') || 'إجمالي المبيعات', this.$t('totalItemsSold') || 'إجمالي المواد المباعة', this.$t('averageOrderValue') || 'متوسط قيمة الطلب', this.$t('itemsPerOrder') || 'مواد لكل طلب'];
                        let csv = headers.map(this.csvEscape).join(',') + '\r\n';
                        list.forEach(item => {
                            const avg = item.totalOrders > 0 ? item.totalSales / item.totalOrders : 0;
                            const perOrder = item.totalOrders > 0 ? (item.totalItemsSold / item.totalOrders) : 0;
                            csv += [period, roleCol, staffCol, item.employeeName, item.totalOrders ?? '', item.totalSales ?? '', item.totalItemsSold ?? '', avg, perOrder].map(this.csvEscape).join(',') + '\r\n';
                        });
                        this.downloadCsv(csv, `sales_by_employee_${dateStr}.csv`);
                    }
                } else if (this.activeTab === 'returnedItems') {
                    const params = new URLSearchParams();
                    params.append('pageNumber', '0');
                    params.append('pageSize', '5000');
                    if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
                    if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
                    if (this.search.info) params.append('info', this.search.info);
                    const res = await HTTP.get(`Admin/GetReturnedOrderItems?${params.toString()}`);
                    const list = res.data?.data?.items || [];
                    if (!list.length) {
                        this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                    } else {
                        const period = this.formatReportPeriod(this.reportFilters.startDate, this.reportFilters.endDate);
                        const headers = [
                            this.$t('reportDateRange') || 'فترة التقرير',
                            this.$t('itemName') || 'اسم المنتج',
                            this.$t('quantity') || 'الكمية',
                            this.$t('unitPrice') || 'سعر الوحدة',
                            this.$t('lineTotal') || 'المجموع',
                            this.$t('invoiceNumber') || 'رقم الفاتورة',
                            this.$t('table') || 'الطاولة',
                            this.$t('orderType') || 'نوع الطلب',
                            this.$t('deletedBy') || 'حذف بواسطة',
                            this.$t('deletedAt') || 'وقت الحذف'
                        ];
                        let csv = headers.map(this.csvEscape).join(',') + '\r\n';
                        list.forEach(item => {
                            csv += [
                                period,
                                item.itemName || '',
                                item.quantity ?? '',
                                item.unitPrice ?? '',
                                item.lineTotal ?? '',
                                item.orderCode || '',
                                item.mergedTableNumbers || item.tableNumber || '',
                                item.orderType || '',
                                item.deletedByUsername || '',
                                this.formatDate(item.insertDate)
                            ].map(this.csvEscape).join(',') + '\r\n';
                        });
                        this.downloadCsv(csv, `returned_items_${dateStr}.csv`);
                    }
                } else if (this.activeTab === 'delivery') {
                    const res = await HTTP.get('DeliveryDrivers/Statistics/All');
                    const data = res.data?.data;
                    const drivers = data?.drivers || [];
                    if (!drivers.length) {
                        this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                    } else {
                        const period = this.$t('reportPeriodCumulative') || 'كل الفترات (تراكمي)';
                        const headers = [this.$t('reportDateRange') || 'فترة التقرير', this.$t('driverName') || 'اسم السائق', this.$t('phoneNumber') || 'رقم الهاتف', this.$t('status') || 'الحالة', this.$t('totalOrders') || 'إجمالي الطلبات', this.$t('deliveredOrders') || 'واصلة', this.$t('pendingDeliveries') || 'معلقة', this.$t('failedDeliveries') || 'فاشلة', this.$t('totalAmount') || 'إجمالي المبلغ', this.$t('paidAmount') || 'مدفوع', this.$t('remainingAmount') || 'متبقي'];
                        let csv = headers.map(this.csvEscape).join(',') + '\r\n';
                        drivers.forEach(d => {
                            csv += [period, d.driverName || '', d.phoneNumber || '', d.isActive ? (this.$t('active') || 'نشط') : (this.$t('inactive') || 'غير نشط'), d.totalOrders ?? '', d.deliveredOrders ?? '', d.pendingOrders ?? '', d.failedOrders ?? '', d.totalAmount ?? '', d.paidAmount ?? '', d.remainingAmount ?? ''].map(this.csvEscape).join(',') + '\r\n';
                        });
                        this.downloadCsv(csv, `delivery_statistics_${dateStr}.csv`);
                    }
                } else if (this.activeTab === 'expensesReport') {
                    const params = new URLSearchParams();
                    if (this.reportFilters.startDate) params.append('startDate', this.reportFilters.startDate);
                    if (this.reportFilters.endDate) params.append('endDate', this.reportFilters.endDate);
                    const res = await HTTP.get(`Expenses/Statistics?${params.toString()}`);
                    const r = res.data?.data;
                    if (!r) {
                        this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                    } else {
                        const summary = [ [this.$t('totalExpenses') || 'إجمالي الصرفيات', this.$t('thisMonthExpenses') || 'صرفيات هذا الشهر', this.$t('thisWeekExpenses') || 'صرفيات هذا الأسبوع', this.$t('totalCount') || 'العدد'].map(this.csvEscape).join(','), [r.totalExpenses ?? '', r.thisMonthExpenses ?? '', r.thisWeekExpenses ?? '', r.totalCount ?? ''].map(this.csvEscape).join(',') ].join('\r\n') + '\r\n';
                        const expPeriod = this.formatReportPeriod(this.reportFilters.startDate, this.reportFilters.endDate);
                        const catHeaders = [this.$t('reportDateRange') || 'فترة التقرير', this.$t('category') || 'الفئة', this.$t('totalExpenses') || 'إجمالي الصرفيات', this.$t('count') || 'العدد'];
                        let csv = summary + catHeaders.map(this.csvEscape).join(',') + '\r\n';
                        (r.expensesByCategory || []).forEach(item => {
                            csv += [expPeriod, item.category ?? item.Category ?? '', item.totalAmount ?? item.TotalAmount ?? '', item.count ?? item.Count ?? ''].map(this.csvEscape).join(',') + '\r\n';
                        });
                        this.downloadCsv(csv, `expenses_report_${dateStr}.csv`);
                    }
                } else {
                    this.$toast.info(this.$t('noDataToExport') || 'لا توجد بيانات للتصدير', { position: 'top-right', timeout: 3000 });
                }
            } catch (err) {
                console.error('Export error:', err);
                this.$toast.error(this.$t('exportError') || 'حدث خطأ أثناء التصدير', { position: 'top-right', timeout: 3000 });
            }
            this.exportingExcel = false;
        },

    },


};
</script>

<style scoped>
/* Edit Order Styles */
.edit-order-section {
    margin-bottom: 2rem;
    padding: 1.5rem;
    background: var(--bg-primary);
    border-radius: 0.75rem;
    box-shadow: var(--shadow-sm);
}

.edit-order-section-title {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--text-primary);
    margin-bottom: 1rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.edit-order-section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
}

.edit-order-form-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1rem;
}

.edit-order-form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.edit-order-label {
    font-weight: 600;
    color: var(--text-primary);
    font-size: 0.9375rem;
}

.edit-order-input {
    padding: 0.75rem 1rem;
    border: 2px solid var(--border-color);
    border-radius: 0.5rem;
    font-size: 1rem;
    font-family: 'Cairo', sans-serif;
    transition: all 0.3s ease;
    background: var(--bg-secondary);
    color: var(--text-primary);
}

.edit-order-input:focus {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.1);
    outline: none;
}

.edit-order-input:disabled {
    opacity: 0.6;
    cursor: not-allowed;
}

.edit-order-add-item-btn {
    display: flex;
    align-items: center;
    padding: 0.625rem 1rem;
    background: var(--primary-color);
    color: #ffffff;
    border: none;
    border-radius: 0.5rem;
    font-size: 0.9375rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.3s ease;
}

.edit-order-add-item-btn:hover {
    background: var(--primary-hover);
    transform: translateY(-1px);
    box-shadow: var(--shadow-md);
}

.edit-order-items-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.edit-order-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    background: var(--bg-secondary);
    border-radius: 0.5rem;
    border: 1px solid var(--border-color);
}

.edit-order-item-info {
    flex: 1;
}

.edit-order-item-name {
    font-size: 1rem;
    font-weight: 600;
    color: var(--text-primary);
    margin-bottom: 0.5rem;
}

.edit-order-item-details {
    display: flex;
    gap: 1rem;
    font-size: 0.875rem;
    color: var(--text-secondary);
}

.edit-order-item-controls {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.edit-order-item-quantity {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.edit-order-quantity-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    border: 1px solid var(--border-color);
    border-radius: 0.375rem;
    background: var(--bg-primary);
    color: var(--text-primary);
    cursor: pointer;
    transition: all 0.3s ease;
}

.edit-order-quantity-btn:hover {
    background: var(--primary-color);
    color: #ffffff;
    border-color: var(--primary-color);
}

.edit-order-quantity-input {
    width: 60px;
    padding: 0.5rem;
    border: 1px solid var(--border-color);
    border-radius: 0.375rem;
    text-align: center;
    font-size: 0.9375rem;
    font-weight: 600;
}

.edit-order-remove-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    border: none;
    border-radius: 0.375rem;
    background: #dc2626;
    color: #ffffff;
    cursor: pointer;
    transition: all 0.3s ease;
}

.edit-order-remove-btn:hover {
    background: #b91c1c;
    transform: scale(1.1);
}

.edit-order-empty {
    text-align: center;
    padding: 3rem 1rem;
    color: var(--text-secondary);
}

.edit-order-empty-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

.edit-order-total {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    margin-top: 1rem;
    background: var(--bg-secondary);
    border-radius: 0.5rem;
    border: 2px solid var(--primary-color);
}

.edit-order-total-label {
    font-size: 1.125rem;
    font-weight: 600;
    color: var(--text-primary);
}

.edit-order-total-value {
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--primary-color);
}

/* Bill table in view modals - match system design */
.report-print-container {
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: 0.75rem;
    padding: 1rem;
}

.report-print-container .bill-container {
    background: var(--bg-primary);
    border: 1px solid var(--border-color);
    border-radius: 0.75rem;
    padding: 1rem;
}

.report-print-container .bill-items-table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 0;
    overflow: hidden;
    border: 1px solid var(--border-color);
    border-radius: 0.625rem;
    background: var(--bg-primary) !important;
}

.report-print-container .bill-items-table thead {
    background: transparent !important;
    box-shadow: none !important;
}

.report-print-container .bill-items-table thead th {
    background: var(--bg-secondary) !important;
    color: var(--text-primary) !important;
    font-weight: 700;
    font-size: 0.875rem;
    padding: 0.75rem;
    border-bottom: 1px solid var(--border-color) !important;
    text-shadow: none !important;
}

.report-print-container .bill-items-table tbody td {
    padding: 0.75rem;
    color: var(--text-primary) !important;
    border-bottom: 1px solid var(--border-color) !important;
    font-size: 0.875rem;
}

.report-print-container .bill-items-table tbody tr:last-child td {
    border-bottom: none;
}

.report-print-container .bill-items-table tbody tr:nth-child(even) {
    background: color-mix(in srgb, var(--bg-secondary) 45%, transparent) !important;
}

.report-print-container .bill-items-table tbody tr:nth-child(odd) {
    background: var(--bg-primary) !important;
}

.report-print-container .bill-item-qty,
.report-print-container .bill-item-total,
.report-print-container .bill-item-price {
    font-weight: 600;
}

.report-print-container .bill-summary-section {
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: 0.625rem;
    padding: 0.75rem;
}

.bill-extra-details {
    margin: 0.75rem 0;
    padding: 0.75rem;
    border: 1px dashed var(--border-color);
    border-radius: 0.625rem;
    background: color-mix(in srgb, var(--bg-secondary) 65%, transparent);
}

.bill-extra-title {
    margin: 0 0 0.625rem;
    font-size: 0.95rem;
    font-weight: 700;
    color: var(--text-primary);
}

.bill-extra-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 0.5rem;
}

.bill-extra-card {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.5rem;
    padding: 0.5rem 0.625rem;
    border: 1px solid var(--border-color);
    border-radius: 0.5rem;
    background: var(--bg-primary);
}

.bill-extra-label {
    font-size: 0.8125rem;
    color: var(--text-secondary);
    font-weight: 600;
}

.bill-extra-value {
    font-size: 0.875rem;
    color: var(--text-primary);
    font-weight: 600;
    text-align: left;
    word-break: break-word;
}

.bill-extra-value--emphasis {
    color: var(--primary-color);
}

.edit-order-items-search-results {
    max-height: 400px;
    overflow-y: auto;
    margin-top: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.edit-order-search-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    background: var(--bg-secondary);
    border-radius: 0.5rem;
    border: 1px solid var(--border-color);
    cursor: pointer;
    transition: all 0.3s ease;
}

.edit-order-search-item:hover {
    background: var(--primary-color);
    color: #ffffff;
    border-color: var(--primary-color);
    transform: translateX(4px);
}

.edit-order-search-item-info {
    flex: 1;
}

.edit-order-search-item-info h4 {
    font-size: 1rem;
    font-weight: 600;
    margin-bottom: 0.25rem;
}

.edit-order-search-item-code {
    font-size: 0.875rem;
    opacity: 0.8;
}

.edit-order-search-item-price {
    font-size: 1rem;
    font-weight: 600;
}

.report-section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

/* Export Excel Button */
.export-excel-btn {
  display: inline-flex;
  align-items: center;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: #0d6e2f;
  background: rgba(13, 110, 47, 0.12);
  border: 1px solid rgba(13, 110, 47, 0.3);
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s ease;
}
.export-excel-btn:hover:not(:disabled) {
  background: #0d6e2f;
  color: #fff;
  border-color: #0d6e2f;
}
.export-excel-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Report section title - aligned with system theme */
.report-section-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 1rem 0;
  padding: 0.75rem;
  border-bottom: 2px solid var(--border-color);
  border-inline-start: 4px solid var(--primary-color);
  line-height: 1.4;
  font-family: inherit;
}

/* Reports Tables Styles */
.reports-table,
.drivers-statistics-table {
  background: var(--bg-primary, #ffffff);
  border-radius: 0.5rem;
  overflow: hidden;
}

.reports-table ::v-deep .table,
.drivers-statistics-table ::v-deep .table {
  margin-bottom: 0;
}

.reports-table ::v-deep thead th,
.drivers-statistics-table ::v-deep thead th {
  background: var(--bg-secondary, #f8f9fa);
  color: var(--text-primary, #212529);
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  padding: 1rem;
  border-bottom: 2px solid var(--border-color, #dee2e6);
}

.reports-table ::v-deep tbody td,
.drivers-statistics-table ::v-deep tbody td {
  padding: 1rem;
  vertical-align: middle;
  border-bottom: 1px solid var(--border-color, #e9ecef);
}

.reports-table ::v-deep tbody tr:hover,
.drivers-statistics-table ::v-deep tbody tr:hover {
  background: var(--bg-secondary, #f8f9fa);
}

.driver-name-cell,
.employee-cell,
.category-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.driver-icon,
.employee-icon,
.category-icon {
  color: var(--primary-color, #007bff);
  font-size: 1.125rem;
}

.item-name-text {
  font-weight: 500;
  color: var(--text-primary, #212529);
}

.rank-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-weight: 700;
  font-size: 0.875rem;
}

.quantity-badge {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  background: var(--bg-secondary, #f8f9fa);
  border-radius: 0.375rem;
  font-weight: 600;
  font-size: 0.875rem;
}

.status-badge {
  display: inline-block;
  padding: 0.375rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.status-active {
  background: #d4edda;
  color: #155724;
}

.status-inactive {
  background: #f8d7da;
  color: #721c24;
}

.stat-value {
  font-weight: 600;
  font-size: 0.9375rem;
}

.stat-success {
  color: #28a745;
}

.stat-warning {
  color: #ffc107;
}

.stat-danger {
  color: #dc3545;
}

.stat-amount {
  font-weight: 600;
  font-size: 0.9375rem;
}

.stat-expense {
  color: var(--danger-color, #dc3545);
}

.success-text {
  color: #28a745;
  font-weight: 600;
}

.warning-text {
  color: #ffc107;
  font-weight: 600;
}

.danger-text {
  color: #dc3545;
  font-weight: 600;
}

.active-badge {
  background: rgba(40, 167, 69, 0.1);
  color: #28a745;
  padding: 0.25rem 0.5rem;
  border-radius: var(--radius-sm, 4px);
  font-size: 0.75rem;
  font-weight: 600;
}

.inactive-badge {
  background: rgba(220, 53, 69, 0.1);
  color: #dc3545;
  padding: 0.25rem 0.5rem;
  border-radius: var(--radius-sm, 4px);
  font-size: 0.75rem;
  font-weight: 600;
}

.report-stat-warning {
  background: linear-gradient(135deg, rgba(255, 193, 7, 0.1) 0%, rgba(255, 193, 7, 0.05) 100%);
  border: 1px solid rgba(255, 193, 7, 0.3);
}

.report-stat-warning .report-stat-icon {
  background: rgba(255, 193, 7, 0.2);
  color: #ffc107;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.empty-icon {
  font-size: 4rem;
  color: var(--text-secondary, #6c757d);
  margin-bottom: 1rem;
  opacity: 0.5;
}

.empty-state p {
  color: var(--text-secondary, #6c757d);
  font-size: 1.1rem;
}

/* Filter Clear Button */
.users-filter-clear-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.625rem 1rem;
  background: #dc3545;
  color: #ffffff;
  border: none;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  width: 100%;
}

.users-filter-clear-btn:hover {
  background: #c82333;
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(220, 53, 69, 0.3);
}

.users-search-input[type="date"],
.users-search-input select {
  padding-right: 2.5rem;
  cursor: pointer;
}

.users-search-input select {
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23666' d='M6 9L1 4h10z'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: left 0.75rem center;
  background-size: 12px;
}
</style>