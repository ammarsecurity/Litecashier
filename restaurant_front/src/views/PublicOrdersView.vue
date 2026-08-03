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
                  <b-icon icon="cart-check-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">الطلبات العامة</h1>
                  <p class="header-subtitle">عرض وإدارة الطلبات التي طلبها الزبائن</p>
                </div>
              </div>
            </div>
          </div>

          <div class="app-filters-panel app-filters-panel--inset">
            <div class="app-filters-panel-head">
              <div class="app-filters-panel-title">
                <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                <div>
                                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                                    <p>{{ $t('ordersFiltersHint') || 'تصفية الطلبات بالتاريخ أو رقم الطلب' }}</p>
                                </div>
              </div>
            </div>
            <div class="app-filters-fields app-filters-fields--3">
              <div class="users-search-container">
                <b-icon icon="hash" class="search-icon"></b-icon>
                <input 
                  v-model="searchQuery" 
                  type="number" 
                  :placeholder="$t('searchByOrderNumber') || 'ابحث برقم الطلب...'"
                  class="users-search-input"
                  @input="debounceSearch"
                />
              </div>
              <div class="users-search-container">
                <b-icon icon="search" class="search-icon"></b-icon>
                <input 
                  v-model="orderCodeQuery" 
                  type="text" 
                  :placeholder="$t('searchByOrderCode') || 'ابحث برمز الطلب...'"
                  class="users-search-input"
                  @input="debounceSearch"
                />
              </div>
              <div class="users-search-container">
                <b-icon icon="calendar" class="search-icon"></b-icon>
                <input 
                  v-model="startDate" 
                  type="date" 
                  :placeholder="$t('from_date') || 'من تاريخ'"
                  class="users-search-input"
                  @change="loadOrders"
                />
              </div>
              <div class="users-search-container">
                <b-icon icon="calendar-check" class="search-icon"></b-icon>
                <input 
                  v-model="endDate" 
                  type="date" 
                  :placeholder="$t('to_date') || 'إلى تاريخ'"
                  class="users-search-input"
                  @change="loadOrders"
                />
              </div>
              <div class="users-search-container">
                <b-icon icon="tag" class="search-icon"></b-icon>
                <select 
                  v-model="orderTypeFilter" 
                  class="users-search-input"
                  @change="loadOrders"
                >
                  <option value="">{{ $t('allOrderTypes') || 'جميع الأنواع' }}</option>
                  <option value="DineIn">{{ $t('dineIn') || 'داخل المطعم' }}</option>
                  <option value="Takeaway">{{ $t('takeaway') || 'خارجي' }}</option>
                  <option value="Delivery">{{ $t('delivery') || 'توصيل' }}</option>
                </select>
              </div>
              <div v-if="orderTypeFilter === 'Delivery'" class="users-search-container">
                <b-icon icon="truck" class="search-icon"></b-icon>
                <select 
                  v-model="driverFilter" 
                  class="users-search-input"
                  @change="loadOrders"
                >
                  <option value="">{{ $t('allDrivers') || 'جميع السائقين' }}</option>
                  <option v-for="driver in deliveryDrivers" :key="driver.id" :value="driver.id">
                    {{ driver.name }}
                  </option>
                </select>
              </div>
            </div>
          </div>

          <!-- Orders Table -->
          <div class="report-table-container">
            <b-table
              id="orders-table"
              :items="Orders"
              :fields="ordersTableFields"
              hover
              responsive
              class="reports-table"
              :empty-text="$t('noOrders') || 'لا توجد طلبات'"
            >
              <template #cell(orderCode)="row">
                <span class="item-name-text">#{{ row.item.dailySequenceNumber || '-' }} - {{ row.item.orderCode }}</span>
              </template>
              <template #cell(insertDate)="row">
                <span class="orders-date-text">{{ formatDate(row.item.insertDate) }}</span>
              </template>
              <template #cell(orderType)="row">
                <span class="order-type-badge" :class="getOrderTypeClass(row.item.orderType)">
                  {{ getOrderTypeText(row.item.orderType) }}
                </span>
              </template>
              <template #cell(paymentMethod)="row">
                <span>{{ getPaymentMethodText(row.item.paymentMethod) }}</span>
              </template>
              <template #cell(totalAmount)="row">
                <span class="stat-amount">{{ formatPrice(row.item.orderTotalAfterDiscount ?? row.item.orderPrice ?? 0) }} {{ $t("currency") || "د.ع" }}</span>
              </template>
              <template #cell(discountAmount)="row">
                <span v-if="Number(row.item.discountAmount || 0) > 0" class="stat-danger">- {{ formatPrice(row.item.discountAmount || 0) }}</span>
                <span v-else>-</span>
              </template>
              <template #cell(itemsCount)="row">
                <span class="quantity-badge">{{ row.item.itemsCount || 0 }}</span>
              </template>
              <template #cell(orderStatus)="row">
                <select
                  v-model="row.item.orderStatus"
                  class="status-select status-select-table"
                  @change="updateOrderStatus(row.item.id, 'orderStatus', row.item.orderStatus)"
                >
                  <option value="Pending">{{ $t("pending") || "قيد الانتظار" }}</option>
                  <option value="Processing">{{ $t("processing") || "قيد التحضير" }}</option>
                  <option value="Ready">{{ $t("ready") || "جاهز" }}</option>
                  <option value="Completed">{{ $t("completed") || "مكتمل" }}</option>
                  <option value="Cancelled">{{ $t("cancelled") || "ملغي" }}</option>
                </select>
              </template>
              <template #cell(paymentStatus)="row">
                <select
                  v-model="row.item.paymentStatus"
                  class="status-select status-select-table"
                  @change="updateOrderStatus(row.item.id, 'paymentStatus', row.item.paymentStatus)"
                >
                  <option value="Pending">{{ $t("pending") || "قيد الانتظار" }}</option>
                  <option value="Paid">{{ $t("paid") || "مدفوع" }}</option>
                  <option value="Refunded">{{ $t("refunded") || "مسترد" }}</option>
                </select>
              </template>
              <template #cell(actions)="row">
                <div class="actions-cell">
                  <button
                    v-if="canPrintOrder(row.item.orderStatus)"
                    type="button"
                    class="action-btn action-btn--icon action-btn--print"
                    :disabled="printingOrderId === row.item.id"
                    @click="printOrder(row.item)"
                    :title="$t('printOrder') || 'طباعة الطلب'"
                  >
                    <b-spinner v-if="printingOrderId === row.item.id" small></b-spinner>
                    <b-icon v-else icon="printer" class="action-icon"></b-icon>
                  </button>
                  <button type="button" class="action-btn action-btn--icon action-btn--view" @click="showItemsModal(row.item)">
                    <b-icon icon="eye" class="action-icon"></b-icon>
                  </button>
                  <button
                    v-if="canCancelPendingPublicOrder(row.item)"
                    type="button"
                    class="action-btn action-btn--icon action-btn--delete"
                    :disabled="cancellingOrderId === row.item.id"
                    :title="$t('cancelPendingOrder') || 'إلغاء الطلب'"
                    @click="confirmCancelPendingOrder(row.item)"
                  >
                    <b-spinner v-if="cancellingOrderId === row.item.id" small></b-spinner>
                    <b-icon v-else icon="x-circle-fill" class="action-icon"></b-icon>
                  </button>
                </div>
              </template>
            </b-table>
          </div>

          <!-- Empty State -->
          <div v-if="Orders.length === 0 && !show" class="empty-state">
            <b-icon icon="inbox" class="empty-icon"></b-icon>
            <p class="empty-text">لا توجد طلبات</p>
          </div>

          <!-- Pagination -->
          <div class="users-pagination-section">
            <b-pagination 
              v-model="pageNumber" 
              :total-rows="totalOrders" 
              :per-page="pageSize"
              aria-controls="orders-table"
              class="users-pagination"
              @change="loadOrders"
            ></b-pagination>
          </div>
        </div>
      </div>
    </div>

    <!-- Items Modal -->
    <b-modal 
      v-model="showItemsModalValue" 
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
      @hidden="selectedOrder = null"
    >
      <div class="modal-content-wrapper" v-if="selectedOrder">
        <h2 class="modal-title">{{ $t("orderItems") || "عناصر الطلب" }}</h2>
        <div class="order-details-content">
          <!-- Order Basic Info -->
          <div class="order-info-section">
            <div class="order-info-grid">
              <div class="order-info-item">
                <label class="order-info-label">{{ $t("orderCode") || "رقم الطلب" }}</label>
                <span class="order-info-value">#{{ selectedOrder.dailySequenceNumber }} - {{ selectedOrder.orderCode }}</span>
              </div>
              <div class="order-info-item">
                <label class="order-info-label">{{ $t("date") || "التاريخ" }}</label>
                <span class="order-info-value">{{ formatDate(selectedOrder.insertDate) }}</span>
              </div>
              <div class="order-info-item">
                <label class="order-info-label">{{ $t("orderType") || "نوع الطلب" }}</label>
                <span class="order-info-value">
                  <span class="order-type-badge" :class="getOrderTypeClass(selectedOrder.orderType)">
                    {{ getOrderTypeText(selectedOrder.orderType) }}
                  </span>
                </span>
              </div>
              <div class="order-info-item" v-if="selectedOrder.paymentMethod">
                <label class="order-info-label">{{ $t("paymentMethod") || "طريقة الدفع" }}</label>
                <span class="order-info-value">{{ getPaymentMethodText(selectedOrder.paymentMethod) }}</span>
              </div>
            </div>
          </div>

          <!-- Order Status Section -->
          <div class="order-status-section">
            <h3 class="order-section-title">{{ $t("orderStatus") || "حالة الطلب" }}</h3>
            <div class="order-status-grid">
              <div class="order-status-item">
                <label class="order-status-label">{{ $t("orderStatus") || "حالة الطلب:" }}</label>
                <div class="status-control-group">
                  <select 
                    v-model="selectedOrder.orderStatus" 
                    class="status-select-modal"
                    @change="updateOrderStatus(selectedOrder.id, 'orderStatus', selectedOrder.orderStatus)"
                  >
                    <option value="Pending">{{ $t("pending") || "قيد الانتظار" }}</option>
                    <option value="Processing">{{ $t("processing") || "قيد التحضير" }}</option>
                    <option value="Ready">{{ $t("ready") || "جاهز" }}</option>
                    <option value="Completed">{{ $t("completed") || "مكتمل" }}</option>
                    <option value="Cancelled">{{ $t("cancelled") || "ملغي" }}</option>
                  </select>
                  <span class="status-badge-modal" :class="getOrderStatusClass(selectedOrder.orderStatus)">
                    {{ getOrderStatusText(selectedOrder.orderStatus) }}
                  </span>
                </div>
              </div>
              
              <div class="order-status-item">
                <label class="order-status-label">{{ $t("paymentStatus") || "حالة الدفع:" }}</label>
                <div class="status-control-group">
                  <select 
                    v-model="selectedOrder.paymentStatus" 
                    class="status-select-modal"
                    @change="updateOrderStatus(selectedOrder.id, 'paymentStatus', selectedOrder.paymentStatus)"
                  >
                    <option value="Pending">{{ $t("pending") || "قيد الانتظار" }}</option>
                    <option value="Paid">{{ $t("paid") || "مدفوع" }}</option>
                    <option value="Refunded">{{ $t("refunded") || "مسترد" }}</option>
                  </select>
                  <span class="status-badge-modal" :class="getPaymentStatusClass(selectedOrder.paymentStatus)">
                    {{ getPaymentStatusText(selectedOrder.paymentStatus) }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Delivery Information (if Delivery order) -->
          <div v-if="selectedOrder.orderType === 'Delivery'" class="order-delivery-section">
            <h3 class="order-section-title">{{ $t("deliveryInformation") || "معلومات التوصيل" }}</h3>
            <div class="order-delivery-grid">
              <div class="order-info-item" v-if="selectedOrder.deliveryCustomerName">
                <label class="order-info-label">{{ $t("customerName") || "اسم العميل" }}</label>
                <span class="order-info-value">{{ selectedOrder.deliveryCustomerName }}</span>
              </div>
              <div class="order-info-item" v-if="selectedOrder.deliveryPhoneNumber">
                <label class="order-info-label">{{ $t("phoneNumber") || "رقم الهاتف" }}</label>
                <span class="order-info-value">{{ selectedOrder.deliveryPhoneNumber }}</span>
              </div>
              <div class="order-info-item" v-if="selectedOrder.deliveryAddress">
                <label class="order-info-label">{{ $t("address") || "العنوان" }}</label>
                <span class="order-info-value">{{ selectedOrder.deliveryAddress }}</span>
              </div>
              <div class="order-info-item" v-if="selectedOrder.deliveryDriver">
                <label class="order-info-label">{{ $t("driverName") || "اسم السائق" }}</label>
                <span class="order-info-value">{{ selectedOrder.deliveryDriver.name }}</span>
              </div>
              <div class="order-info-item" v-if="selectedOrder.deliveryDriver?.phoneNumber">
                <label class="order-info-label">{{ $t("driverPhoneNumber") || "رقم هاتف السائق" }}</label>
                <span class="order-info-value">{{ selectedOrder.deliveryDriver.phoneNumber }}</span>
              </div>
              <div class="order-info-item" v-if="selectedOrder.deliveryStatus">
                <label class="order-info-label">{{ $t("deliveryStatus") || "حالة التوصيل" }}</label>
                <span class="order-info-value">
                  <span class="delivery-status-badge" :class="getDeliveryStatusClass(selectedOrder.deliveryStatus)">
                    {{ getDeliveryStatusText(selectedOrder.deliveryStatus) }}
                  </span>
                </span>
              </div>
            </div>
          </div>

          <!-- Order Items -->
          <div class="order-items-section">
            <h3 class="order-section-title">{{ $t("orderItems") || "عناصر الطلب" }}</h3>
            <div class="items-list">
              <div 
                v-for="(orderItem, index) in selectedOrder.customerOrderItem" 
                :key="index"
                class="order-item-row"
              >
                <div class="item-info">
                  <h4 class="item-name">{{ orderItem.itemName }}</h4>
                  <span class="item-quantity">{{ $t("quantity") || "الكمية" }}: {{ orderItem.quantity }}</span>
                </div>
                <div class="item-price">
                  <span class="item-total">{{ formatPrice(orderItem.total) }} {{ $t("currency") || "د.ع" }}</span>
                  <span class="item-unit-price">{{ formatPrice(orderItem.sellingPrice) }} × {{ orderItem.quantity }}</span>
                </div>
              </div>
            </div>
            <div class="order-total-section">
              <span class="total-label">{{ $t("total") || "المجموع" }}:</span>
              <span class="total-amount">{{ formatPrice(selectedOrder.orderTotalAfterDiscount ?? selectedOrder.orderPrice ?? 0) }} {{ $t("currency") || "د.ع" }}</span>
            </div>
            <div class="order-total-section" v-if="Number(selectedOrder.discountAmount || 0) > 0">
              <span class="total-label">{{ $t("discountLabel") || "الخصم" }}:</span>
              <span class="total-amount">- {{ formatPrice(selectedOrder.discountAmount || 0) }} {{ $t("currency") || "د.ع" }}</span>
            </div>
          </div>
        </div>
        <div class="users-form-actions">
          <button
            v-if="selectedOrder && canCancelPendingPublicOrder(selectedOrder)"
            type="button"
            class="users-form-cancel-button"
            :disabled="cancellingOrderId === selectedOrder.id"
            @click="confirmCancelPendingOrder(selectedOrder)"
          >
            <b-spinner v-if="cancellingOrderId === selectedOrder.id" small class="me-1"></b-spinner>
            <b-icon v-else icon="x-circle-fill" class="me-1"></b-icon>
            {{ $t("cancelPendingOrder") || "إلغاء الطلب" }}
          </button>
          <button
            v-if="selectedOrder && canPrintOrder(selectedOrder.orderStatus)"
            type="button"
            class="users-form-submit-button"
            :disabled="printingOrderId === selectedOrder.id"
            @click="printOrder(selectedOrder)"
          >
            <b-spinner v-if="printingOrderId === selectedOrder.id" small class="me-1"></b-spinner>
            <b-icon v-else icon="printer-fill" class="me-1"></b-icon>
            {{ $t('printOrder') || 'طباعة الطلب' }}
          </button>
          <button type="button" class="users-form-cancel-button" @click="showItemsModalValue = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </div>
    </b-modal>
  </b-overlay>
</template>

<script>
import AppHeader from '../components/Layout/AppHeader.vue';
import { HTTP } from '../http/api.js';
import signalRService from '../services/signalr.js';
import {
  todayBusinessDateString,
  formatBusinessDate,
  businessDateStringFrom,
} from '../utils/formatBusinessDateTime.js';
import {
  resolveCommercialUserIdFromStorage,
  canCancelPendingPublicOrder as isPendingPublicOrderCancellable,
  cancelPendingPublicOrder,
} from '../utils/queueOrders.js';
import {
  printPublicOrderLikePos,
  canPrintOrderStatus,
  shouldAutoPrintOnStatusChange,
  shouldAutoPrintPublicCardOrder,
  fetchPublicOrderById,
  notifyPrintOrderResult,
  resolvePrintFailureMessage,
} from '../utils/orderPrintService.js';
import notify from '../utils/notify.js';

export default {
  name: 'PublicOrdersView',
  components: {
    AppHeader
  },
  data() {
    return {
      show: false,
      Orders: [],
      totalOrders: 0,
      pageNumber: 1,
      pageSize: 10,
      searchQuery: '',
      orderCodeQuery: '',
      startDate: todayBusinessDateString(),
      endDate: todayBusinessDateString(),
      orderTypeFilter: '',
      driverFilter: '',
      deliveryDrivers: [],
      searchTimer: null,
      showItemsModalValue: false,
      selectedOrder: null,
      commercialUserId: null,
      printingOrderId: null,
      cancellingOrderId: null,
    };
  },
  computed: {
    ordersTableFields() {
      return [
        { key: 'orderCode', label: this.$t("orderCode") || "رقم الطلب" },
        { key: 'insertDate', label: this.$t("date") || "التاريخ" },
        { key: 'orderType', label: this.$t("orderType") || "نوع الطلب" },
        { key: 'paymentMethod', label: this.$t("paymentMethod") || "طريقة الدفع" },
        { key: 'itemsCount', label: this.$t("itemsCount") || "العناصر" },
        { key: 'discountAmount', label: this.$t("discountLabel") || "الخصم" },
        { key: 'totalAmount', label: this.$t("total") || "المجموع" },
        { key: 'orderStatus', label: this.$t("orderStatus") || "حالة الطلب" },
        { key: 'paymentStatus', label: this.$t("paymentStatus") || "حالة الدفع" },
        { key: 'actions', label: this.$t("actions") || "إجراءات" }
      ];
    }
  },
  mounted() {
    const today = todayBusinessDateString();
    this.startDate = today;
    this.endDate = today;

    this.commercialUserId = resolveCommercialUserIdFromStorage();
    
    if (!this.commercialUserId) {
      this.$bvToast.toast('معرف المطعم غير موجود', {
        title: 'خطأ',
        variant: 'danger',
        solid: true
      });
      return;
    }

    this.loadOrders();
    this.initializeSignalR();
  },
  watch: {
    orderTypeFilter(newValue) {
      if (newValue === 'Delivery') {
        this.loadDeliveryDrivers();
      } else {
        // Clear driver filter when order type changes away from Delivery
        this.driverFilter = '';
      }
    }
  },
  beforeDestroy() {
    this.cleanupSignalR();
  },
  methods: {
    async loadOrders() {
      try {
        this.show = true;
        
        const params = new URLSearchParams({
          pageNumber: (this.pageNumber - 1).toString(),
          pageSize: this.pageSize.toString()
        });

        if (this.startDate) {
          params.append('startDate', this.startDate);
        }
        if (this.endDate) {
          params.append('endDate', this.endDate);
        }
        if (this.searchQuery) {
          params.append('dailySequenceNumber', this.searchQuery);
        }
        if (this.orderCodeQuery) {
          params.append('orderCode', this.orderCodeQuery);
        }
        if (this.orderTypeFilter) {
          params.append('orderType', this.orderTypeFilter);
        }
        if (this.driverFilter) {
          params.append('deliveryDriverId', this.driverFilter);
        }

        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}/orders?${params.toString()}`);
        
        if (response.data && !response.data.errorStatus) {
          this.Orders = response.data.data.items || [];
          this.totalOrders = response.data.data.totalItems || 0;
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء جلب الطلبات', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error loading orders:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء جلب الطلبات', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.show = false;
      }
    },
    debounceSearch() {
      clearTimeout(this.searchTimer);
      this.searchTimer = setTimeout(() => {
        this.pageNumber = 1;
        this.loadOrders();
      }, 500);
    },
    async updateOrderStatus(orderId, statusType, statusValue) {
      const orderIndex = this.Orders.findIndex(o => o.id === orderId);
      const previousStatus =
        orderIndex !== -1
          ? this.Orders[orderIndex].orderStatus
          : this.selectedOrder?.id === orderId
            ? this.selectedOrder.orderStatus
            : null;

      try {
        const updateData = {};
        if (statusType === 'orderStatus') {
          updateData.OrderStatus = statusValue;
        } else if (statusType === 'paymentStatus') {
          updateData.PaymentStatus = statusValue;
        }

        const response = await HTTP.put(
          `PublicMenu/${this.commercialUserId}/orders/${orderId}/status`,
          updateData
        );

        if (response.data && !response.data.errorStatus) {
          if (orderIndex !== -1) {
            if (statusType === 'orderStatus') {
              this.Orders[orderIndex].orderStatus = statusValue;
            } else if (statusType === 'paymentStatus') {
              this.Orders[orderIndex].paymentStatus = statusValue;
            }
          }

          if (this.selectedOrder && this.selectedOrder.id === orderId) {
            if (statusType === 'orderStatus') {
              this.selectedOrder.orderStatus = statusValue;
            } else if (statusType === 'paymentStatus') {
              this.selectedOrder.paymentStatus = statusValue;
            }
          }

          this.$bvToast.toast('تم تحديث الحالة بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });

          if (
            statusType === 'orderStatus' &&
            shouldAutoPrintOnStatusChange(previousStatus, statusValue)
          ) {
            const orderToPrint =
              this.selectedOrder?.id === orderId
                ? this.selectedOrder
                : this.Orders[orderIndex] || null;
            if (orderToPrint) {
              await this.printOrder(orderToPrint, { silent: false });
            }
          }
        } else {
          // Revert the change on error
          const orderIndex = this.Orders.findIndex(o => o.id === orderId);
          if (orderIndex !== -1) {
            this.loadOrders();
          }
          if (this.selectedOrder && this.selectedOrder.id === orderId) {
            this.loadOrders();
          }
          
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء تحديث الحالة', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error updating order status:', error);
        
        // Revert the change on error
        const orderIndex = this.Orders.findIndex(o => o.id === orderId);
        if (orderIndex !== -1) {
          this.loadOrders();
        }
        if (this.selectedOrder && this.selectedOrder.id === orderId) {
          this.loadOrders();
        }
        
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء تحديث الحالة', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      }
    },
    canCancelPendingPublicOrder(order) {
      return isPendingPublicOrderCancellable(order);
    },
    async confirmCancelPendingOrder(order) {
      if (!order?.id || !isPendingPublicOrderCancellable(order)) return;

      const confirmed = await this.$confirm({
        title: this.$t("confirmCancelPendingOrderTitle") || "تأكيد إلغاء الطلب",
        message:
          this.$t("confirmCancelPendingOrderMessage") ||
          "هل تريد إلغاء هذا الطلب؟ يُسمح بالإلغاء فقط للطلبات قيد الانتظار.",
        confirmText: this.$t("cancelPendingOrder") || "إلغاء الطلب",
        cancelText: this.$t("cancelButtonLabel") || "تراجع",
        variant: "danger",
      });
      if (!confirmed) return;

      await this.cancelPendingOrder(order.id);
    },
    mapCancelPendingOrderError(error) {
      const apiMessage = error?.response?.data?.message || error?.response?.data?.Message;
      if (apiMessage === "cannotCancelNonPendingOrder") {
        return (
          this.$t("cannotCancelNonPendingOrder") ||
          "لا يمكن إلغاء الطلب إلا وهو قيد الانتظار"
        );
      }
      return (
        error?.response?.data?.message ||
        this.$t("errorCancellingOrder") ||
        "حدث خطأ أثناء إلغاء الطلب"
      );
    },
    async cancelPendingOrder(orderId) {
      if (!this.commercialUserId || this.cancellingOrderId != null) return;

      this.cancellingOrderId = orderId;
      try {
        const result = await cancelPendingPublicOrder(
          HTTP,
          this.commercialUserId,
          orderId
        );

        if (result && !result.errorStatus) {
          const orderIndex = this.Orders.findIndex((o) => o.id === orderId);
          if (orderIndex !== -1) {
            this.Orders[orderIndex].orderStatus = "Cancelled";
          }

          if (this.selectedOrder?.id === orderId) {
            this.selectedOrder.orderStatus = "Cancelled";
          }

          this.$bvToast.toast(
            this.$t("orderCancelledSuccessfully") || "تم إلغاء الطلب بنجاح",
            {
              title: this.$t("success") || "نجاح",
              variant: "success",
              solid: true,
            }
          );
        } else {
          this.$bvToast.toast(result?.message || this.mapCancelPendingOrderError({}), {
            title: this.$t("error") || "خطأ",
            variant: "danger",
            solid: true,
          });
        }
      } catch (error) {
        console.error("Error cancelling pending order:", error);
        this.$bvToast.toast(this.mapCancelPendingOrderError(error), {
          title: this.$t("error") || "خطأ",
          variant: "danger",
          solid: true,
        });
      } finally {
        this.cancellingOrderId = null;
      }
    },
    showItemsModal(order) {
      this.selectedOrder = order;
      this.showItemsModalValue = true;
    },
    canPrintOrder(status) {
      return canPrintOrderStatus(status);
    },
    async printOrder(order, options = {}) {
      if (!order || !this.commercialUserId) return;
      const silent = options.silent === true;
      this.printingOrderId = order.id;
      if (!silent) {
        notify.info(this.$t('printingOrder') || 'جاري الطباعة...', {
          timeout: 1500,
          maxToasts: 1,
        });
      }
      try {
        const result = await printPublicOrderLikePos(order, {
          http: HTTP,
          commercialUserId: this.commercialUserId,
          t: (key) => this.$t(key),
        });

        notifyPrintOrderResult(result, notify, (key) => this.$t(key), options);
      } catch (error) {
        console.error('Error printing public order:', error);
        notify.error(
          error.response?.data?.message ||
            resolvePrintFailureMessage({ errors: ['unknown'] }, (key) => this.$t(key)),
          { timeout: 4500, maxToasts: 1 }
        );
      } finally {
        this.printingOrderId = null;
      }
    },
    async autoPrintPublicOrder(orderId) {
      if (!orderId || !this.commercialUserId) return;
      try {
        const order = await fetchPublicOrderById(HTTP, this.commercialUserId, orderId);
        if (order) {
          await this.printOrder(order, { silent: true });
        }
      } catch (error) {
        console.error('Auto-print public order failed:', error);
      }
    },
    getPaymentMethodIcon(method) {
      const icons = {
        'Cash': 'cash-coin',
        'Card': 'credit-card',
        'Credit': 'wallet2'
      };
      return icons[method] || 'currency-dollar';
    },
    getPaymentMethodText(method) {
      const texts = {
        'Cash': 'كاش',
        'Card': 'بطاقة',
        'Credit': 'آجل'
      };
      return texts[method] || method;
    },
    getOrderStatusClass(status) {
      const classes = {
        'Pending': 'status-pending',
        'Processing': 'status-processing',
        'Ready': 'status-ready',
        'Completed': 'status-completed',
        'Cancelled': 'status-cancelled'
      };
      return classes[status] || 'status-pending';
    },
    getOrderStatusText(status) {
      const texts = {
        'Pending': 'قيد الانتظار',
        'Processing': 'قيد التحضير',
        'Ready': 'جاهز',
        'Completed': 'مكتمل',
        'Cancelled': 'ملغي'
      };
      return texts[status] || status;
    },
    getPaymentStatusClass(status) {
      const classes = {
        'Pending': 'payment-pending',
        'Paid': 'payment-paid',
        'Refunded': 'payment-refunded'
      };
      return classes[status] || 'payment-pending';
    },
    getPaymentStatusText(status) {
      const texts = {
        'Pending': this.$t("pending") || 'قيد الانتظار',
        'Paid': this.$t("paid") || 'مدفوع',
        'Refunded': this.$t("refunded") || 'مسترد'
      };
      return texts[status] || status;
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
    async loadDeliveryDrivers() {
      try {
        const response = await HTTP.get('DeliveryDrivers');
        if (response.data && !response.data.errorStatus) {
          this.deliveryDrivers = response.data.data || [];
        } else {
          this.deliveryDrivers = [];
        }
      } catch (error) {
        console.error('Error loading delivery drivers:', error);
        this.deliveryDrivers = [];
      }
    },
    formatPrice(price) {
      return new Intl.NumberFormat('ar-IQ').format(price);
    },
    formatDate(date) {
      return formatBusinessDate(date);
    },
    initializeSignalR() {
      signalRService.startConnection()
        .then(() => {
          // Listen for new public orders
          signalRService.on('PublicOrderAdded', async (data) => {
            console.log('Public order added via SignalR:', data);
            // Only reload if the order belongs to this commercial user
            if (data.CommercialUserId === this.commercialUserId) {
              // Check if order matches current filters (date range and type)
              const orderDay = businessDateStringFrom(data.InsertDate);
              const matchesDate =
                (!this.startDate || orderDay >= this.startDate) &&
                (!this.endDate || orderDay <= this.endDate);
              const matchesType = data.OrderType === 'Takeaway' || data.OrderType === 'Delivery';
              
              if (matchesDate && matchesType && !this.searchQuery && !this.orderCodeQuery) {
                this.loadOrders();
              }

              if (shouldAutoPrintPublicCardOrder(data)) {
                const orderId = data.OrderId ?? data.orderId;
                await this.autoPrintPublicOrder(orderId);
              }
            }
          });

          // Listen for public order updates
          signalRService.on('PublicOrderUpdated', (data) => {
            console.log('Public order updated via SignalR:', data);
            // Only update if the order belongs to this commercial user
            if (data.CommercialUserId === this.commercialUserId) {
              // Update the order in the list if it exists
              const orderIndex = this.Orders.findIndex(o => o.id === data.OrderId);
              if (orderIndex !== -1) {
                this.Orders[orderIndex].orderStatus = data.OrderStatus;
                this.Orders[orderIndex].paymentStatus = data.PaymentStatus;
              } else {
                // If order not in current view, reload to check if it should be shown
                this.loadOrders();
              }
            }
          });

          // Also listen for regular OrderAdded from POS (for Takeaway/Delivery orders)
          signalRService.on('OrderAdded', (data) => {
            console.log('Order added via SignalR:', data);
            // Only reload if it's a Takeaway or Delivery order for this commercial user
            if ((data.OrderType === 'Takeaway' || data.OrderType === 'Delivery') && !this.searchQuery && !this.orderCodeQuery) {
              this.loadOrders();
            }
          });
        })
        .catch(error => {
          console.error('Failed to start SignalR connection:', error);
        });
    },
    cleanupSignalR() {
      // Remove SignalR listeners
      signalRService.off('PublicOrderAdded');
      signalRService.off('PublicOrderUpdated');
      signalRService.off('OrderAdded');
    }
  }
};
</script>

<style scoped>
.order-header-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.order-date {
  font-size: 0.75rem;
  color: var(--text-secondary);
  font-weight: 400;
}

.status-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

.status-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.status-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
  min-width: 100px;
}

.status-select {
  flex: 1;
  padding: 0.5rem 0.75rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
  font-family: 'Cairo', sans-serif;
  -webkit-appearance: none;
  -moz-appearance: none;
  appearance: none;
}

.status-select:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 10%, transparent);
}

.status-select-table {
  min-width: 130px;
  font-size: 0.8125rem;
  padding: 0.375rem 0.5rem;
}

.reports-table .user-action-button {
  width: 34px;
  height: 34px;
  padding: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.reports-table .stat-value {
  text-shadow: none !important;
  filter: none !important;
  background: none !important;
  -webkit-text-fill-color: currentColor !important;
  color: var(--text-primary) !important;
}

.orders-date-text {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
}

.status-badge {
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.75rem;
  font-weight: 700;
  white-space: nowrap;
}

.status-pending {
  background: rgba(245, 158, 11, 0.2);
  color: #f59e0b;
  border: 1px solid rgba(245, 158, 11, 0.4);
}

.status-processing {
  background: rgba(59, 130, 246, 0.2);
  color: #3b82f6;
  border: 1px solid rgba(59, 130, 246, 0.4);
}

.status-ready {
  background: rgba(34, 197, 94, 0.2);
  color: #22c55e;
  border: 1px solid rgba(34, 197, 94, 0.4);
}

.status-completed {
  background: rgba(16, 185, 129, 0.2);
  color: #10b981;
  border: 1px solid rgba(16, 185, 129, 0.4);
}

.status-cancelled {
  background: rgba(239, 68, 68, 0.2);
  color: #ef4444;
  border: 1px solid rgba(239, 68, 68, 0.4);
}

.payment-pending {
  background: rgba(245, 158, 11, 0.2);
  color: #f59e0b;
  border: 1px solid rgba(245, 158, 11, 0.4);
}

.payment-paid {
  background: rgba(34, 197, 94, 0.2);
  color: #22c55e;
  border: 1px solid rgba(34, 197, 94, 0.4);
}

.payment-refunded {
  background: rgba(239, 68, 68, 0.2);
  color: #ef4444;
  border: 1px solid rgba(239, 68, 68, 0.4);
}

/* Order Type Badges */
.order-type-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.8125rem;
  font-weight: 600;
}

.order-type-dinein {
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
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

/* Delivery Info Section */
.delivery-info-section {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

/* Delivery Status Badges */
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

/* Order Details Modal Styles */
.order-details-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.order-info-section {
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.order-info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.order-info-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.order-info-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.order-info-value {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.order-delivery-section {
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.order-section-title {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 1rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--border-color);
}

.order-delivery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.order-items-section {
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.items-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.order-item-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.item-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.item-name {
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.item-quantity {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.item-price {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.25rem;
}

.item-total {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--primary-color);
}

.item-unit-price {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.order-total-section {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  margin-top: 1rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 2px solid var(--primary-color);
}

.total-label {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
}

.total-amount {
  font-size: 1.5rem;
  font-weight: 800;
  color: var(--primary-color);
}

.empty-state {
  text-align: center;
  padding: 4rem 2rem;
}

.empty-icon {
  font-size: 4rem;
  color: color-mix(in srgb, var(--primary-color) 40%, transparent);
  margin-bottom: 1rem;
}

.empty-text {
  font-size: 1.125rem;
  color: var(--text-secondary);
}

/* Order Status Section in Modal */
.order-status-section {
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
}

.order-status-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
}

.order-status-item {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.order-status-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.status-control-group {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.status-select-modal {
  flex: 1;
  min-width: 150px;
  padding: 0.625rem 0.875rem;
  background: var(--bg-secondary);
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
  font-family: 'Cairo', sans-serif;
  -webkit-appearance: none;
  -moz-appearance: none;
  appearance: none;
}

.status-select-modal:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 10%, transparent);
}

.status-badge-modal {
  padding: 0.5rem 0.875rem;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 700;
  white-space: nowrap;
  flex-shrink: 0;
}
</style>

