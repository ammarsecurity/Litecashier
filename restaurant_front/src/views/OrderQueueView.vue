<template>
  <b-overlay
    :show="loading"
    spinner-variant="primary"
    spinner-type="grow"
    spinner-large
    rounded="sm"
  >
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content order-queue-page">
          <!-- Header Section -->
          <div class="users-header-section">
            <div class="users-header-content">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="list-task" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("orderQueue") || "طابور الطلبات" }}</h1>
                  <p class="header-subtitle">{{ $t("orderQueueDescription") || "إدارة ومتابعة الطلبات حسب الحالة" }}</p>
                </div>
              </div>
              <button type="button" class="users-add-button" @click="loadOrders({ silent: false })">
                <b-icon icon="arrow-clockwise" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
              </button>
            </div>
          </div>

          <div class="users-search-section">
            <div class="users-search-container order-queue-filter-wrap">
              <b-icon icon="filter" class="search-icon"></b-icon>
              <select
                v-model="orderTypeFilter"
                class="users-search-input"
                @change="loadOrders({ silent: true })"
                style="padding-inline-start: 2.5rem; min-width: 220px; max-width: 100%;"
              >
                <option value="">{{ $t("allOrderTypes") || "جميع الأنواع" }}</option>
                <option value="DineIn">{{ $t("dineIn") || "داخل المطعم" }}</option>
                <option value="Takeaway">{{ $t("takeaway") || "خارجي" }}</option>
                <option value="Delivery">{{ $t("delivery") || "توصيل" }}</option>
              </select>
            </div>
          </div>

          <!-- Queue Board -->
          <div class="queue-board order-queue-board">
            <!-- Pending Column -->
            <div class="queue-column">
              <div class="queue-column-header pending">
                <div class="column-header-content">
                  <b-icon icon="clock-history" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("pending") || "قيد الانتظار" }}</h3>
                  <span class="column-count">{{ pendingOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card" 
                  v-for="order in pendingOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                    <div v-if="order.notes" class="order-info-item">
                      <b-icon icon="chat-left-text" class="info-icon"></b-icon>
                      <span class="order-notes">{{ order.notes }}</span>
                    </div>
                  </div>
                  <div class="queue-card-footer">
                    <button 
                      class="queue-action-btn processing-btn" 
                      @click.stop="updateOrderStatus(order.id, 'Processing')"
                    >
                      <b-icon icon="play-circle" class="me-1"></b-icon>
                      {{ $t("startProcessing") || "بدء المعالجة" }}
                    </button>
                  </div>
                </div>
                <div v-if="pendingOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noPendingOrders") || "لا توجد طلبات قيد الانتظار" }}</p>
                </div>
              </div>
            </div>

            <!-- Processing Column -->
            <div class="queue-column">
              <div class="queue-column-header processing">
                <div class="column-header-content">
                  <b-icon icon="gear" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("processing") || "قيد المعالجة" }}</h3>
                  <span class="column-count">{{ processingOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card" 
                  v-for="order in processingOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                    <div v-if="order.notes" class="order-info-item">
                      <b-icon icon="chat-left-text" class="info-icon"></b-icon>
                      <span class="order-notes">{{ order.notes }}</span>
                    </div>
                  </div>
                  <div class="queue-card-footer">
                    <button 
                      class="queue-action-btn ready-btn" 
                      @click.stop="updateOrderStatus(order.id, 'Ready')"
                    >
                      <b-icon icon="check-circle" class="me-1"></b-icon>
                      {{ $t("markReady") || "تحديد كجاهز" }}
                    </button>
                  </div>
                </div>
                <div v-if="processingOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noProcessingOrders") || "لا توجد طلبات قيد المعالجة" }}</p>
                </div>
              </div>
            </div>

            <!-- Ready Column -->
            <div class="queue-column">
              <div class="queue-column-header ready">
                <div class="column-header-content">
                  <b-icon icon="check-circle" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("ready") || "جاهز" }}</h3>
                  <span class="column-count">{{ readyOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card" 
                  v-for="order in readyOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                    <div v-if="order.notes" class="order-info-item">
                      <b-icon icon="chat-left-text" class="info-icon"></b-icon>
                      <span class="order-notes">{{ order.notes }}</span>
                    </div>
                  </div>
                  <div class="queue-card-footer">
                    <button 
                      class="queue-action-btn completed-btn" 
                      @click.stop="updateOrderStatus(order.id, 'Completed')"
                    >
                      <b-icon icon="check2-circle" class="me-1"></b-icon>
                      {{ $t("markCompleted") || "تحديد كمكتمل" }}
                    </button>
                  </div>
                </div>
                <div v-if="readyOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noReadyOrders") || "لا توجد طلبات جاهزة" }}</p>
                </div>
              </div>
            </div>

            <!-- Completed Column -->
            <div class="queue-column">
              <div class="queue-column-header completed">
                <div class="column-header-content">
                  <b-icon icon="check2-circle" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("completed") || "مكتمل" }}</h3>
                  <span class="column-count">{{ completedOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card completed-card" 
                  v-for="order in completedOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                  </div>
                </div>
                <div v-if="completedOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noCompletedOrders") || "لا توجد طلبات مكتملة" }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Order Details Modal -->
    <b-modal 
      v-model="showOrderModal" 
      :title="$t('orderDetails') || 'تفاصيل الطلب'"
      size="lg"
      centered
      hide-footer
      content-class="order-queue-modal-content"
      header-class="order-queue-modal-header"
    >
      <div v-if="selectedOrder" class="order-details-modal">
        <div class="order-details-section">
          <h4 class="details-section-title">{{ $t("orderInfo") || "معلومات الطلب" }}</h4>
          <div class="details-grid">
            <div class="detail-item">
              <span class="detail-label">{{ $t("orderCode") || "رمز الطلب" }}:</span>
              <span class="detail-value">{{ selectedOrder.orderCode }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">{{ $t("orderNumber") || "رقم الطلب" }}:</span>
              <span class="detail-value">{{ selectedOrder.dailySequenceNumber || selectedOrder.id }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">{{ $t("orderType") || "نوع الطلب" }}:</span>
              <span class="detail-value">{{ getOrderTypeText(selectedOrder.orderType) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">{{ $t("orderStatus") || "حالة الطلب" }}:</span>
              <span class="detail-value" :class="getStatusClass(selectedOrder.orderStatus)">
                {{ getStatusText(selectedOrder.orderStatus) }}
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">{{ $t("paymentMethod") || "طريقة الدفع" }}:</span>
              <span class="detail-value">{{ getPaymentMethodText(selectedOrder.paymentMethod) }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">{{ $t("paymentStatus") || "حالة الدفع" }}:</span>
              <span class="detail-value" :class="getPaymentStatusClass(selectedOrder.paymentStatus)">
                {{ getPaymentStatusText(selectedOrder.paymentStatus) }}
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">{{ $t("total") || "المجموع" }}:</span>
              <span class="detail-value">{{ formatPrice(selectedOrder.orderTotalAfterDiscount ?? selectedOrder.orderPrice ?? 0) }} {{ $t("currency") }}</span>
            </div>
            <div class="detail-item" v-if="Number(selectedOrder.discountAmount || 0) > 0">
              <span class="detail-label">{{ $t("discountLabel") || "الخصم" }}:</span>
              <span class="detail-value">- {{ formatPrice(selectedOrder.discountAmount || 0) }} {{ $t("currency") }}</span>
            </div>
            <div v-if="selectedOrder.notes" class="detail-item full-width">
              <span class="detail-label">{{ $t("notes") || "ملاحظات" }}:</span>
              <span class="detail-value">{{ selectedOrder.notes }}</span>
            </div>
          </div>
        </div>

        <div v-if="selectedOrder.deliveryDriver" class="order-details-section">
          <h4 class="details-section-title">{{ $t("deliveryInfo") || "معلومات التوصيل" }}</h4>
          <div class="details-grid">
            <div class="detail-item">
              <span class="detail-label">{{ $t("driverName") || "اسم السائق" }}:</span>
              <span class="detail-value">{{ selectedOrder.deliveryDriver.name }}</span>
            </div>
            <div v-if="selectedOrder.deliveryAddress" class="detail-item">
              <span class="detail-label">{{ $t("address") || "العنوان" }}:</span>
              <span class="detail-value">{{ selectedOrder.deliveryAddress }}</span>
            </div>
            <div v-if="selectedOrder.deliveryCustomerName" class="detail-item">
              <span class="detail-label">{{ $t("customerName") || "اسم العميل" }}:</span>
              <span class="detail-value">{{ selectedOrder.deliveryCustomerName }}</span>
            </div>
            <div v-if="selectedOrder.deliveryPhoneNumber" class="detail-item">
              <span class="detail-label">{{ $t("phoneNumber") || "رقم الهاتف" }}:</span>
              <span class="detail-value">{{ selectedOrder.deliveryPhoneNumber }}</span>
            </div>
          </div>
        </div>

        <div class="order-details-section">
          <h4 class="details-section-title">{{ $t("orderItems") || "عناصر الطلب" }}</h4>
          <div class="order-items-list">
            <div 
              v-for="item in selectedOrder.customerOrderItem" 
              :key="item.id"
              class="order-item-row"
            >
              <div class="item-info">
                <span class="item-name">{{ item.itemName }}</span>
                <span class="item-quantity">x{{ item.quantity }}</span>
              </div>
              <div class="item-price">
                {{ formatPrice(item.sellingPrice * item.quantity) }} {{ $t("currency") }}
              </div>
            </div>
          </div>
        </div>

        <div class="order-details-actions">
          <button 
            v-if="selectedOrder.orderStatus === 'Pending'" 
            type="button"
            class="users-add-button order-queue-modal-action order-queue-modal-action--processing"
            @click="updateOrderStatus(selectedOrder.id, 'Processing')"
          >
            <b-icon icon="play-circle" class="me-2"></b-icon>
            {{ $t("startProcessing") || "بدء المعالجة" }}
          </button>
          <button 
            v-if="selectedOrder.orderStatus === 'Processing'" 
            type="button"
            class="users-add-button order-queue-modal-action order-queue-modal-action--ready"
            @click="updateOrderStatus(selectedOrder.id, 'Ready')"
          >
            <b-icon icon="check-circle" class="me-2"></b-icon>
            {{ $t("markReady") || "تحديد كجاهز" }}
          </button>
          <button 
            v-if="selectedOrder.orderStatus === 'Ready'" 
            type="button"
            class="users-add-button order-queue-modal-action order-queue-modal-action--done"
            @click="updateOrderStatus(selectedOrder.id, 'Completed')"
          >
            <b-icon icon="check2-circle" class="me-2"></b-icon>
            {{ $t("markCompleted") || "تحديد كمكتمل" }}
          </button>
          <button type="button" class="users-form-cancel-button" @click="showOrderModal = false">
            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
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

export default {
  name: 'OrderQueueView',
  components: {
    AppHeader
  },
  data() {
    return {
      Orders: [],
      orderTypeFilter: '',
      showOrderModal: false,
      selectedOrder: null,
      commercialUserId: null,
      refreshInterval: null,
      loading: false
    };
  },
  computed: {
    pendingOrders() {
      return this.Orders.filter(o => !o.orderStatus || o.orderStatus === 'Pending');
    },
    processingOrders() {
      return this.Orders.filter(o => o.orderStatus === 'Processing');
    },
    readyOrders() {
      return this.Orders.filter(o => o.orderStatus === 'Ready');
    },
    completedOrders() {
      return this.Orders.filter(o => o.orderStatus === 'Completed').slice(0, 10); // Show only last 10 completed
    }
  },
  mounted() {
    const userInfo = JSON.parse(localStorage.getItem('info') || '{}');
    this.commercialUserId = userInfo.id || userInfo.commercialUserId;
    
    if (!this.commercialUserId) {
      this.$bvToast.toast('معرف المطعم غير موجود', {
        title: 'خطأ',
        variant: 'danger',
        solid: true
      });
      return;
    }

    this.loadOrders({ silent: false });
    this.initializeSignalR();
    
    // Auto refresh every 10 seconds
    this.refreshInterval = setInterval(() => {
      this.loadOrders({ silent: true });
    }, 10000);
  },
  beforeDestroy() {
    this.cleanupSignalR();
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  },
  methods: {
    async loadOrders(options = {}) {
      const silent = options.silent === true;
      if (!this.commercialUserId) return;
      if (!silent) this.loading = true;
      try {
        const params = new URLSearchParams({
          pageNumber: '0',
          pageSize: '100' // Load more orders for queue view
        });

        if (this.orderTypeFilter) {
          params.append('orderType', this.orderTypeFilter);
        }

        // Only load orders from today
        const today = new Date().toISOString().split('T')[0];
        params.append('startDate', today);
        params.append('endDate', today);

        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}/orders?${params.toString()}`);
        
        if (response.data && !response.data.errorStatus) {
          // Filter to show only non-completed orders, plus recent completed
          const allOrders = response.data.data.items || [];
          this.Orders = allOrders.filter(o => 
            !o.orderStatus || 
            o.orderStatus === 'Pending' || 
            o.orderStatus === 'Processing' || 
            o.orderStatus === 'Ready' || 
            o.orderStatus === 'Completed'
          );
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
        if (!silent) this.loading = false;
      }
    },
    async updateOrderStatus(orderId, status) {
      try {
        const response = await HTTP.put(
          `PublicMenu/${this.commercialUserId}/orders/${orderId}/status`,
          { OrderStatus: status }
        );

        if (response.data && !response.data.errorStatus) {
          // Update the order in the list
          const orderIndex = this.Orders.findIndex(o => o.id === orderId);
          if (orderIndex !== -1) {
            this.Orders[orderIndex].orderStatus = status;
          }
          
          // Update selected order if it's the same
          if (this.selectedOrder && this.selectedOrder.id === orderId) {
            this.selectedOrder.orderStatus = status;
          }

          this.$bvToast.toast('تم تحديث الحالة بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء تحديث الحالة', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error updating order status:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء تحديث الحالة', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      }
    },
    selectOrder(order) {
      this.selectedOrder = order;
      this.showOrderModal = true;
    },
    getOrderTypeClass(type) {
      const classes = {
        'DineIn': 'dinein-badge',
        'Takeaway': 'takeaway-badge',
        'Delivery': 'delivery-badge'
      };
      return classes[type] || '';
    },
    getOrderTypeText(type) {
      const texts = {
        'DineIn': this.$t('dineIn') || 'داخل المطعم',
        'Takeaway': this.$t('takeaway') || 'خارجي',
        'Delivery': this.$t('delivery') || 'توصيل'
      };
      return texts[type] || type;
    },
    getStatusText(status) {
      const texts = {
        'Pending': this.$t('pending') || 'قيد الانتظار',
        'Processing': this.$t('processing') || 'قيد المعالجة',
        'Ready': this.$t('ready') || 'جاهز',
        'Completed': this.$t('completed') || 'مكتمل'
      };
      return texts[status] || status;
    },
    getStatusClass(status) {
      const classes = {
        'Pending': 'status-pending',
        'Processing': 'status-processing',
        'Ready': 'status-ready',
        'Completed': 'status-completed'
      };
      return classes[status] || '';
    },
    getPaymentMethodText(method) {
      const texts = {
        'Cash': this.$t('cash') || 'كاش',
        'Card': this.$t('card') || 'بطاقة',
        'Credit': this.$t('credit') || 'آجل'
      };
      return texts[method] || method;
    },
    getPaymentStatusText(status) {
      const texts = {
        'Pending': this.$t('pending') || 'قيد الانتظار',
        'Paid': this.$t('paid') || 'مدفوع',
        'Refunded': this.$t('refunded') || 'مسترد'
      };
      return texts[status] || status;
    },
    getPaymentStatusClass(status) {
      const classes = {
        'Pending': 'status-pending',
        'Paid': 'status-paid',
        'Refunded': 'status-refunded'
      };
      return classes[status] || '';
    },
    formatPrice(price) {
      return new Intl.NumberFormat('ar-IQ').format(price);
    },
    initializeSignalR() {
      signalRService.startConnection().then(() => {
        signalRService.on('PublicOrderUpdated', (data) => {
          // Reload orders when an order is updated
          this.loadOrders({ silent: true });
        });
      });
    },
    cleanupSignalR() {
      signalRService.off('PublicOrderUpdated');
    }
  }
};
</script>

<style scoped>
.order-queue-filter-wrap {
  max-width: 360px;
}

.order-queue-board {
  margin-top: 0;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 1.25rem;
}

.queue-column {
  background: var(--bg-primary);
  border-radius: 0.75rem;
  overflow: hidden;
  border: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  box-shadow: none;
}

.queue-column-header {
  padding: 0.875rem 1rem;
  background: var(--bg-secondary);
  color: var(--text-primary);
  font-weight: 700;
  border-bottom: 1px solid var(--border-color);
}

.queue-column-header.pending {
  border-bottom: 3px solid #f59e0b;
}

.queue-column-header.pending .column-icon {
  color: #d97706;
}

.queue-column-header.processing {
  border-bottom: 3px solid #3b82f6;
}

.queue-column-header.processing .column-icon {
  color: #2563eb;
}

.queue-column-header.ready {
  border-bottom: 3px solid #10b981;
}

.queue-column-header.ready .column-icon {
  color: #059669;
}

.queue-column-header.completed {
  border-bottom: 3px solid #64748b;
}

.queue-column-header.completed .column-icon {
  color: #64748b;
}

.column-header-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.column-icon {
  font-size: 1.35rem;
  flex-shrink: 0;
}

.column-title {
  margin: 0;
  font-size: 1rem;
  flex: 1;
  font-weight: 700;
  color: var(--text-primary);
}

.column-count {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  padding: 0.2rem 0.65rem;
  border-radius: 999px;
  font-size: 0.8125rem;
  font-weight: 700;
}

.queue-column-body {
  flex: 1;
  overflow-y: auto;
  max-height: min(62vh, 720px);
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
  background: var(--bg-primary);
}

.queue-card {
  background: var(--bg-secondary);
  border-radius: 0.65rem;
  padding: 0.875rem 1rem;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
  border: 1px solid var(--border-color);
}

.queue-card:hover {
  border-color: rgba(129, 140, 248, 0.45);
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.06);
}

.queue-card.completed-card {
  opacity: 0.92;
}

.queue-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.65rem;
  gap: 0.5rem;
}

.order-code-badge {
  background: var(--bg-tertiary);
  color: var(--text-primary);
  padding: 0.35rem 0.65rem;
  border-radius: 0.5rem;
  font-weight: 800;
  font-size: 0.75rem;
  border: 1px solid var(--border-color);
}

.order-type-badge {
  padding: 0.2rem 0.55rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 700;
}

.dinein-badge {
  background: rgba(99, 102, 241, 0.12);
  color: var(--primary-color);
  border: 1px solid rgba(99, 102, 241, 0.22);
}

.takeaway-badge {
  background: rgba(34, 197, 94, 0.1);
  color: var(--success-color);
  border: 1px solid rgba(34, 197, 94, 0.22);
}

.delivery-badge {
  background: rgba(249, 115, 22, 0.1);
  color: #ea580c;
  border: 1px solid rgba(249, 115, 22, 0.25);
}

.queue-card-body {
  margin-bottom: 0.65rem;
}

.order-info-item {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin-bottom: 0.4rem;
  font-size: 0.8125rem;
  color: var(--text-secondary);
}

.info-icon {
  color: var(--primary-color);
  font-size: 0.9rem;
  flex-shrink: 0;
  margin-top: 0.1rem;
}

.order-notes {
  font-style: italic;
  color: var(--text-secondary);
}

.queue-card-footer {
  display: flex;
  gap: 0.5rem;
  padding-top: 0.25rem;
}

.queue-action-btn {
  flex: 1;
  padding: 0.5rem 0.65rem;
  border: none;
  border-radius: 0.65rem;
  color: #fff;
  font-size: 0.8125rem;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  transition: filter 0.2s ease, box-shadow 0.2s ease;
  font-family: inherit;
}

.queue-action-btn:hover {
  filter: brightness(1.05);
}

.processing-btn {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.25);
}

.ready-btn {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.22);
}

.completed-btn {
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  box-shadow: 0 4px 12px rgba(71, 85, 105, 0.2);
}

.queue-empty-state {
  text-align: center;
  padding: 2.25rem 1rem;
  color: var(--text-secondary);
}

.empty-icon {
  font-size: 2.25rem;
  color: var(--text-secondary);
  opacity: 0.35;
  margin-bottom: 0.75rem;
}

.empty-text {
  margin: 0;
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.order-details-modal {
  padding: 0.25rem 0;
}

.order-details-section {
  margin-bottom: 1.25rem;
}

.details-section-title {
  font-size: 1rem;
  font-weight: 800;
  color: var(--text-primary);
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid var(--border-color);
}

.details-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.875rem;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.detail-item.full-width {
  grid-column: 1 / -1;
}

.detail-label {
  font-size: 0.8125rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.detail-value {
  font-size: 0.9rem;
  color: var(--text-primary);
  font-weight: 600;
}

.status-pending {
  color: #d97706;
}

.status-processing {
  color: #2563eb;
}

.status-ready {
  color: #059669;
}

.status-completed {
  color: #64748b;
}

.status-paid {
  color: var(--success-color);
}

.status-refunded {
  color: var(--danger-color);
}

.order-items-list {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.order-item-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 0.875rem;
  background: var(--bg-secondary);
  border-radius: 0.65rem;
  border: 1px solid var(--border-color);
}

.item-info {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.item-name {
  font-weight: 700;
  color: var(--text-primary);
  font-size: 0.9375rem;
}

.item-quantity {
  font-size: 0.8125rem;
  color: var(--text-secondary);
}

.item-price {
  font-weight: 700;
  color: var(--primary-color);
  flex-shrink: 0;
}

.order-details-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
  justify-content: flex-end;
  margin-top: 1.25rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
}

.order-queue-modal-action {
  padding: 0.65rem 1.15rem;
  font-size: 0.9rem;
}

.order-queue-modal-action--processing {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.28);
}

.order-queue-modal-action--ready,
.order-queue-modal-action--done {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.28);
}

@media (max-width: 768px) {
  .order-queue-board {
    grid-template-columns: 1fr;
  }

  .details-grid {
    grid-template-columns: 1fr;
  }

  .order-details-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .order-details-actions .users-add-button,
  .order-details-actions .users-form-cancel-button {
    width: 100%;
    justify-content: center;
  }
}
</style>

<style>
/* مربوط بنافذة b-modal (تُعرض خارج شجرة المكوّن) */
.order-queue-modal-content.modal-content {
  background: var(--bg-primary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
}

.order-queue-modal-header.modal-header {
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
  color: var(--text-primary);
}

.order-queue-modal-header.modal-header .close {
  color: var(--text-primary);
  opacity: 0.75;
  text-shadow: none;
}

.order-queue-modal-header .modal-title {
  font-weight: 800;
}
</style>

