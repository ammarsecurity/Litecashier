<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <SidebarView />
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
                                @click="activeTab = 'byEmployee'; loadSalesByEmployee()"
                            >
                                <b-icon icon="people-fill" class="me-2"></b-icon>
                                {{ $t('salesByEmployee') || 'المبيعات حسب الموظف' }}
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

                    <!-- Advanced Reports Date Filter -->
                    <div class="users-search-section" v-if="activeTab !== 'orders' && activeTab !== 'lowStock'">
                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem;">
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
                        </div>
                    </div>

                    <!-- Orders Grid (Default View) -->
                    <div v-if="activeTab === 'orders'">
                        <!-- Search Section -->
                        <div class="users-search-section">
                            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem;">
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
                            </div>
                        </div>

                        <!-- Orders Grid -->
                        <div class="users-grid-container">
                            <div class="users-grid">
                                <div class="user-card" v-for="item in Orders" :key="item.id">
                                    <div class="user-card-header">
                                        <div class="user-avatar">
                                            <b-icon icon="receipt-cutoff" class="avatar-icon"></b-icon>
                                        </div>
                                        <h3 class="user-name">{{ item.orderCode }}</h3>
                                    </div>
                                    <div class="user-card-body">
                                        <div class="user-info-item">
                                            <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                                            <span class="info-label">{{ $t('invoice_amount') }}:</span>
                                            <span class="info-value">{{ item.orderPrice }} {{ $t('currency') }}</span>
                                        </div>
                                        <div class="user-info-item">
                                            <b-icon icon="box-seam" class="info-icon"></b-icon>
                                            <span class="info-label">{{ $t('items_count') }}:</span>
                                            <span class="info-value">{{ item.itemsCount }} {{ $t('items') }}</span>
                                        </div>
                                        <div class="user-info-item" v-if="item.paymentMethod">
                                            <b-icon :icon="getPaymentMethodIcon(item.paymentMethod)" class="info-icon"></b-icon>
                                            <span class="info-label">{{ $t('paymentMethod') }}:</span>
                                            <span class="info-value">{{ getPaymentMethodText(item.paymentMethod) }}</span>
                                        </div>
                                        <div class="user-info-item" v-if="item.orderType">
                                            <b-icon :icon="getOrderTypeIcon(item.orderType)" class="info-icon"></b-icon>
                                            <span class="info-label">{{ $t('orderType') }}:</span>
                                            <span class="info-value">{{ getOrderTypeText(item.orderType) }}</span>
                                        </div>
                                        <div class="user-info-item" v-if="item.tags">
                                            <b-icon icon="tags" class="info-icon"></b-icon>
                                            <span class="info-label">{{ $t('categoryPlaceholder') }}:</span>
                                            <span class="info-value">{{ item.tags }}</span>
                                        </div>
                                    </div>
                                    <div class="user-card-footer">
                                        <button class="user-action-button user-edit-button" @click="showItemsModel(item.customerOrderItem, item)" style="width: 100%;">
                                            <b-icon icon="eye-fill" class="action-icon"></b-icon>
                                            <span>{{ $t('view_items') }}</span>
                                        </button>
                                    </div>
                                </div>
                            </div>
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
                            <div class="report-info-banner" v-if="topSellingItems.length > 0">
                                <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                <span>{{ $t('topSellingItemsDescription') || 'عرض أفضل المنتجات مبيعاً حسب الكمية المباعة' }}</span>
                            </div>
                            <div class="report-table-container">
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

                        <!-- Sales By Category -->
                        <div v-if="activeTab === 'byCategory'" class="report-section">
                            <div class="report-info-banner" v-if="salesByCategory.length > 0">
                                <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                <span>{{ $t('salesByCategoryDescription') || 'تحليل المبيعات حسب الفئات المختلفة' }}</span>
                            </div>
                            <div class="report-table-container">
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
                            <div class="report-info-banner" v-if="salesByEmployee.length > 0">
                                <b-icon icon="info-circle-fill" class="banner-icon"></b-icon>
                                <span>{{ $t('salesByEmployeeDescription') || 'مقارنة أداء الموظفين في المبيعات' }}</span>
                            </div>
                            <div class="report-table-container">
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

                        <!-- Low Stock Items -->
                        <div v-if="activeTab === 'lowStock'" class="report-section">
                            <div class="low-stock-header">
                                <div class="users-search-section">
                                    <div class="users-search-container">
                                        <b-icon icon="exclamation-triangle-fill" class="search-icon"></b-icon>
                                        <input 
                                            v-model.number="lowStockThreshold" 
                                            type="number" 
                                            :placeholder="$t('threshold') || 'حد الكمية'"
                                            class="users-search-input"
                                            @change="loadLowStockItems()"
                                        />
                                    </div>
                                </div>
                                <div class="low-stock-summary" v-if="lowStockItems.length > 0">
                                    <div class="summary-item">
                                        <b-icon icon="exclamation-triangle-fill" class="summary-icon warning"></b-icon>
                                        <span class="summary-label">{{ $t('lowStockCount') || 'منتجات قليلة المخزون' }}:</span>
                                        <span class="summary-value">{{ lowStockItems.filter(item => item.currentQuantity > 0 && item.currentQuantity <= item.threshold).length }}</span>
                                    </div>
                                    <div class="summary-item">
                                        <b-icon icon="x-circle-fill" class="summary-icon danger"></b-icon>
                                        <span class="summary-label">{{ $t('outOfStockCount') || 'منتجات منتهية' }}:</span>
                                        <span class="summary-value">{{ lowStockItems.filter(item => item.currentQuantity === 0).length }}</span>
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
                                <div class="bill-info-row">
                                    <span class="bill-info-label">{{ $t('employeeLabel') }}:</span>
                                    <span class="bill-info-value" v-if="order">{{ userInfo.name }}</span>
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
                                <div class="bill-summary-row bill-summary-total">
                                    <span class="bill-summary-label">{{ $t('total') }}:</span>
                                    <span class="bill-summary-value">{{ formattedNumber }} {{ $t('currency') }}</span>
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
        </div>
    </b-overlay>
</template>
<script>
import SidebarView from "@/components/Layout/SidebarView.vue";
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";
import { HTTP } from '../http/api.js';
export default {
    name: "OrdersView",
    components: {
        SidebarView,
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
            },
            reportFilters: {
                startDate: "",
                endDate: "",
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
            salesByCategory: [],
            salesByEmployee: [],
            lowStockItems: [],
            lowStockThreshold: 10,
            
            // Search debounce timer
            searchTimer: null,
        };
    },
    computed: {
        formattedNumber() {
            return this.totaPrice.toLocaleString()
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
    },

    mounted() {
        this.GetAllOrders();
        this.userInfo = JSON.parse(localStorage.getItem('info'));
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
            if (dateTime) {
                const [date, time] = dateTime.split("T");
                return date + " " + time.split(".")[0];
            }
            return "";
        },
        formatPrice(price) {
            if (price) {
                return price.toLocaleString("en-EG");
            }
            return "0";
        },
        print() {
            const prtHtml = document.getElementById('print').innerHTML;
            
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


        GetAllOrders() {
            this.show = true;
            HTTP.get(`Admin/GetOrders?pageNumber=${this.pageNumber - 1}&pageSize=${this.pageSize}&info=${this.search.info}&startDate=${this.search.startDate}&endDate=${this.search.endDate}`)
                .then((response) => {
                    this.Orders = response.data.data.items;
                    this.totalOrders = response.data.data.totalItems;
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                });
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
            }
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