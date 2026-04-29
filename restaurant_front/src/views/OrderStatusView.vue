<template>
  <div class="order-status-container">
    <!-- Header Section -->
    <header class="order-status-header">
      <div class="header-content">
        <div class="logo-section">
          <img 
            v-if="restaurantLogo && !logoError" 
            :src="restaurantLogo" 
            alt="Logo" 
            class="status-logo"
            @error="logoError = true"
          />
          <div v-else class="logo-placeholder">
            <b-icon icon="shop" class="logo-icon"></b-icon>
          </div>
        </div>
        <h1 class="restaurant-name">{{ restaurantName || 'حالة الطلب' }}</h1>
      </div>
    </header>

    <!-- Search Section -->
    <div class="search-section" v-if="!order">
      <div class="search-container">
        <div class="search-input-wrapper">
          <b-icon icon="hash" class="search-icon"></b-icon>
          <input 
            v-model="orderCodeInput" 
            type="text"
            :placeholder="$t('enterOrderCode') || 'أدخل كود الطلب'"
            class="search-input"
            @keyup.enter="searchOrder"
            autofocus
          />
        </div>
        <button 
          class="search-button" 
          @click="searchOrder"
          :disabled="loading || !orderCodeInput.trim()"
        >
          <b-spinner small v-if="loading" class="me-2"></b-spinner>
          <b-icon v-else icon="search" class="me-2"></b-icon>
          {{ $t('searchOrder') || 'بحث عن الطلب' }}
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner"></div>
      <p class="loading-text">{{ $t('loading') || 'جاري التحميل...' }}</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error && !order" class="error-container">
      <b-icon icon="exclamation-triangle-fill" class="error-icon"></b-icon>
      <p class="error-text">{{ error }}</p>
      <button class="retry-button" @click="searchOrder">
        <b-icon icon="arrow-clockwise" class="me-2"></b-icon>
        {{ $t('tryAgain') || 'حاول مرة أخرى' }}
      </button>
    </div>

    <!-- Order Status Display -->
    <div v-else-if="order" class="order-display-section">
      <!-- Order Card -->
      <div 
        class="order-status-card" 
        :class="statusClass"
        :key="order.id"
      >
        <!-- Status Badge -->
        <div class="status-badge" :class="statusBadgeClass">
          <div class="status-indicator"></div>
          <span class="status-text">{{ statusText }}</span>
        </div>

        <!-- Order Header -->
        <div class="order-header">
          <div class="order-number-section">
            <h2 class="order-number-label">{{ $t('orderNumber') || 'رقم الطلب' }}</h2>
            <h1 class="order-number">{{ order.orderCode }}</h1>
          </div>
          <div class="order-time-section">
            <b-icon icon="clock" class="time-icon"></b-icon>
            <div class="time-info">
              <span class="time-label">{{ $t('orderTime') || 'وقت الطلب' }}</span>
              <span class="time-value">{{ formattedTime }}</span>
            </div>
          </div>
        </div>

        <!-- Order Details -->
        <div class="order-details">
          <div class="detail-row">
            <div class="detail-item">
              <b-icon icon="cart-check" class="detail-icon"></b-icon>
              <div class="detail-content">
                <span class="detail-label">{{ $t('itemsCount') || 'عدد العناصر' }}</span>
                <span class="detail-value">{{ order.itemsCount }}</span>
              </div>
            </div>
            <div class="detail-item">
              <b-icon icon="currency-exchange" class="detail-icon"></b-icon>
              <div class="detail-content">
                <span class="detail-label">{{ $t('orderTotal') || 'المبلغ الإجمالي' }}</span>
                <span class="detail-value">{{ formattedTotal }} د.ع</span>
              </div>
            </div>
          </div>

          <!-- Order Type -->
          <div class="order-type-badge" v-if="order.orderType">
            <b-icon :icon="getOrderTypeIcon(order.orderType)" class="me-2"></b-icon>
            <span>{{ getOrderTypeText(order.orderType) }}</span>
          </div>

          <!-- Table Number (if DineIn) -->
          <div class="table-info" v-if="order.tableNumber">
            <b-icon icon="table" class="me-2"></b-icon>
            <span>{{ $t('table') || 'طاولة' }} #{{ order.tableNumber }}</span>
          </div>
        </div>

        <!-- Order Items -->
        <div class="order-items-section" v-if="order.items && order.items.length > 0">
          <h3 class="items-title">{{ $t('orderItems') || 'عناصر الطلب' }}</h3>
          <div class="items-list">
            <div 
              v-for="item in order.items" 
              :key="item.id"
              class="order-item"
            >
              <div class="item-info">
                <span class="item-name">{{ item.itemName }}</span>
                <span class="item-quantity">x{{ item.quantity }}</span>
              </div>
              <span class="item-price">{{ formatPrice(item.total) }} د.ع</span>
            </div>
          </div>
        </div>

        <!-- Payment Status -->
        <div class="payment-status" v-if="order.paymentStatus">
          <div class="payment-badge" :class="getPaymentStatusClass(order.paymentStatus)">
            <b-icon :icon="getPaymentStatusIcon(order.paymentStatus)" class="me-2"></b-icon>
            <span>{{ getPaymentStatusText(order.paymentStatus) }}</span>
          </div>
        </div>

        <!-- Delivery Info (if Delivery) -->
        <div class="delivery-info" v-if="order.orderType === 'Delivery' && order.deliveryStatus">
          <div class="delivery-status-badge" :class="getDeliveryStatusClass(order.deliveryStatus)">
            <b-icon icon="truck" class="me-2"></b-icon>
            <span>{{ getDeliveryStatusText(order.deliveryStatus) }}</span>
          </div>
          <div class="delivery-details" v-if="order.deliveryDriver">
            <p class="delivery-driver">
              <b-icon icon="person" class="me-2"></b-icon>
              {{ $t('driverName') || 'السائق' }}: {{ order.deliveryDriver.name }}
            </p>
          </div>
        </div>

        <!-- Notes -->
        <div class="order-notes" v-if="order.notes">
          <b-icon icon="chat-left-text" class="notes-icon"></b-icon>
          <p class="notes-text">{{ order.notes }}</p>
        </div>

        <!-- Auto Refresh Indicator -->
        <div class="auto-refresh-indicator">
          <b-icon icon="arrow-clockwise" class="refresh-icon"></b-icon>
          <span>{{ $t('autoRefresh') || 'تحديث تلقائي' }}</span>
        </div>
      </div>

      <!-- Search Another Order Button -->
      <button class="search-another-button" @click="resetSearch">
        <b-icon icon="search" class="me-2"></b-icon>
        {{ $t('searchAnotherOrder') || 'بحث عن طلب آخر' }}
      </button>
    </div>

    <!-- Empty State (Initial) -->
    <div v-else class="empty-state">
      <b-icon icon="receipt" class="empty-icon"></b-icon>
      <p class="empty-text">{{ $t('enterOrderCodeToCheck') || 'أدخل كود الطلب للتحقق من حالته' }}</p>
    </div>
  </div>
</template>

<script>
import { HTTP } from "../http/api.js";

export default {
  name: "OrderStatusView",
  data() {
    return {
      orderCodeInput: "",
      order: null,
      loading: false,
      error: null,
      refreshInterval: null,
      previousStatus: null,
      restaurantName: "",
      restaurantLogo: "",
      logoError: false,
      commercialUserId: null
    };
  },
  computed: {
    statusClass() {
      if (!this.order) return "";
      const status = this.order.orderStatus?.toLowerCase();
      return `status-${status}`;
    },
    statusBadgeClass() {
      if (!this.order) return "";
      const status = this.order.orderStatus?.toLowerCase();
      return `badge-${status}`;
    },
    statusText() {
      if (!this.order) return "";
      const status = this.order.orderStatus;
      const statusMap = {
        "Pending": this.$t("orderStatusPending") || "قيد الانتظار",
        "Processing": this.$t("orderStatusProcessing") || "قيد التحضير",
        "Ready": this.$t("orderStatusReady") || "جاهز",
        "Completed": this.$t("orderStatusCompleted") || "مكتمل",
        "Cancelled": this.$t("cancelled") || "ملغي"
      };
      return statusMap[status] || status;
    },
    formattedTime() {
      if (!this.order || !this.order.insertDate) return "";
      const date = new Date(this.order.insertDate);
      return date.toLocaleString("ar-IQ", {
        year: "numeric",
        month: "long",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit"
      });
    },
    formattedTotal() {
      if (!this.order || !this.order.total) return "0";
      return this.formatPrice(this.order.total);
    }
  },
  mounted() {
    // Get commercialUserId from route params
    this.commercialUserId = parseInt(this.$route.params.commercialUserId);
    
    // If orderCode is in route, search automatically
    if (this.$route.params.orderCode) {
      this.orderCodeInput = this.$route.params.orderCode;
      this.searchOrder();
    }

    // Load restaurant info
    this.loadRestaurantInfo();
  },
  beforeDestroy() {
    this.stopAutoRefresh();
  },
  methods: {
    async searchOrder() {
      if (!this.orderCodeInput.trim()) {
        this.error = this.$t("enterOrderCode") || "يرجى إدخال كود الطلب";
        return;
      }

      this.loading = true;
      this.error = null;
      this.previousStatus = this.order?.orderStatus;

      try {
        const response = await HTTP.get(
          `PublicMenu/${this.commercialUserId}/order-status/${this.orderCodeInput.trim()}`
        );

        if (response.data && !response.data.errorStatus && response.data.data) {
          this.order = response.data.data;
          this.startAutoRefresh();
          
          // Play sound if status changed
          if (this.previousStatus && this.previousStatus !== this.order.orderStatus) {
            this.playNotificationSound();
          }
        } else {
          this.error = response.data?.message || (this.$t("orderNotFound") || "الطلب غير موجود");
          this.order = null;
          this.stopAutoRefresh();
        }
      } catch (err) {
        console.error("Error fetching order status:", err);
        this.error = err.response?.data?.message || (this.$t("orderNotFound") || "الطلب غير موجود");
        this.order = null;
        this.stopAutoRefresh();
      } finally {
        this.loading = false;
      }
    },
    async fetchOrderStatus() {
      if (!this.order || !this.orderCodeInput.trim()) return;

      try {
        const response = await HTTP.get(
          `PublicMenu/${this.commercialUserId}/order-status/${this.orderCodeInput.trim()}`
        );

        if (response.data && !response.data.errorStatus && response.data.data) {
          const newStatus = response.data.data.orderStatus;
          const oldStatus = this.order.orderStatus;
          
          this.order = response.data.data;
          
          // Play sound if status changed
          if (oldStatus !== newStatus) {
            this.playNotificationSound();
          }
        }
      } catch (err) {
        console.error("Error refreshing order status:", err);
        // Don't show error on auto-refresh, just log it
      }
    },
    startAutoRefresh() {
      this.stopAutoRefresh();
      // Refresh every 10 seconds
      this.refreshInterval = setInterval(() => {
        this.fetchOrderStatus();
      }, 10000);
    },
    stopAutoRefresh() {
      if (this.refreshInterval) {
        clearInterval(this.refreshInterval);
        this.refreshInterval = null;
      }
    },
    resetSearch() {
      this.order = null;
      this.orderCodeInput = "";
      this.error = null;
      this.stopAutoRefresh();
      this.previousStatus = null;
    },
    async loadRestaurantInfo() {
      try {
        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}`);
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.restaurantName = response.data.data.restaurantName;
          this.restaurantLogo = response.data.data.logo;
        }
      } catch (err) {
        console.error("Error loading restaurant info:", err);
      }
    },
    formatPrice(price) {
      if (!price) return "0";
      return parseFloat(price).toLocaleString("ar-IQ");
    },
    getOrderTypeIcon(type) {
      const icons = {
        "DineIn": "house-door",
        "Takeaway": "bag",
        "Delivery": "truck"
      };
      return icons[type] || "cart";
    },
    getOrderTypeText(type) {
      const texts = {
        "DineIn": this.$t("dineIn") || "داخلي",
        "Takeaway": this.$t("takeaway") || "طلب خارجي",
        "Delivery": this.$t("delivery") || "توصيل"
      };
      return texts[type] || type;
    },
    getPaymentStatusClass(status) {
      const classes = {
        "Pending": "payment-pending",
        "Paid": "payment-paid",
        "Refunded": "payment-refunded"
      };
      return classes[status] || "";
    },
    getPaymentStatusIcon(status) {
      const icons = {
        "Pending": "clock",
        "Paid": "check-circle",
        "Refunded": "arrow-counterclockwise"
      };
      return icons[status] || "clock";
    },
    getPaymentStatusText(status) {
      const texts = {
        "Pending": this.$t("pending") || "قيد الانتظار",
        "Paid": this.$t("paid") || "مدفوع",
        "Refunded": this.$t("refunded") || "مسترد"
      };
      return texts[status] || status;
    },
    getDeliveryStatusClass(status) {
      const classes = {
        "Pending": "delivery-pending",
        "InTransit": "delivery-transit",
        "Delivered": "delivery-delivered",
        "Failed": "delivery-failed"
      };
      return classes[status] || "";
    },
    getDeliveryStatusText(status) {
      const texts = {
        "Pending": this.$t("pending") || "قيد الانتظار",
        "InTransit": this.$t("inTransit") || "قيد التوصيل",
        "Delivered": this.$t("delivered") || "تم التوصيل",
        "Failed": this.$t("failed") || "فشل التوصيل"
      };
      return texts[status] || status;
    },
    playNotificationSound() {
      // Try to play notification sound
      try {
        const audio = new Audio(require("../assets/beep.mp3"));
        audio.volume = 0.5;
        audio.play().catch(err => {
          console.log("Could not play notification sound:", err);
        });
      } catch (err) {
        console.log("Notification sound not available");
      }
    }
  }
};
</script>

<style scoped>
.order-status-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.order-status-header {
  width: 100%;
  max-width: 1200px;
  margin-bottom: 2rem;
}

.header-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.logo-section {
  display: flex;
  align-items: center;
  justify-content: center;
}

.status-logo {
  max-width: 120px;
  max-height: 120px;
  object-fit: contain;
  border-radius: 1rem;
  background: white;
  padding: 0.5rem;
}

.logo-placeholder {
  width: 120px;
  height: 120px;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.logo-icon {
  font-size: 3rem;
  color: white;
}

.restaurant-name {
  color: white;
  font-size: 2.5rem;
  font-weight: 700;
  text-align: center;
  margin: 0;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.3);
}

.search-section {
  width: 100%;
  max-width: 800px;
  margin-bottom: 2rem;
}

.search-container {
  background: white;
  border-radius: 1rem;
  padding: 2rem;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
  display: flex;
  gap: 1rem;
  align-items: center;
}

.search-input-wrapper {
  flex: 1;
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  right: 1rem;
  font-size: 1.5rem;
  color: #6b7280;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: 1rem 3rem 1rem 1rem;
  font-size: 1.25rem;
  border: 2px solid #e5e7eb;
  border-radius: 0.75rem;
  outline: none;
  transition: all 0.3s ease;
}

.search-input:focus {
  border-color: #667eea;
  box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.1);
}

.search-button {
  padding: 1rem 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 0.75rem;
  font-size: 1.125rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.search-button:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(102, 126, 234, 0.3);
}

.search-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading-container,
.error-container,
.empty-state {
  width: 100%;
  max-width: 800px;
  background: white;
  border-radius: 1rem;
  padding: 3rem;
  text-align: center;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
}

.loading-spinner {
  width: 60px;
  height: 60px;
  border: 4px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.loading-text,
.error-text,
.empty-text {
  font-size: 1.25rem;
  color: #6b7280;
  margin: 0;
}

.error-icon {
  font-size: 4rem;
  color: #ef4444;
  margin-bottom: 1rem;
}

.retry-button {
  margin-top: 1.5rem;
  padding: 0.75rem 1.5rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 0.5rem;
  font-size: 1rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  transition: all 0.3s ease;
}

.retry-button:hover {
  background: #5568d3;
  transform: translateY(-2px);
}

.empty-icon {
  font-size: 4rem;
  color: #9ca3af;
  margin-bottom: 1rem;
}

.order-display-section {
  width: 100%;
  max-width: 900px;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.order-status-card {
  background: white;
  border-radius: 1.5rem;
  padding: 2.5rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  position: relative;
  overflow: hidden;
  animation: slideIn 0.5s ease-out;
  border-left: 6px solid;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.status-pending {
  border-left-color: #f59e0b;
}

.status-processing {
  border-left-color: #3b82f6;
}

.status-ready {
  border-left-color: #10b981;
}

.status-completed {
  border-left-color: #6b7280;
}

.status-badge {
  position: absolute;
  top: 1.5rem;
  left: 1.5rem;
  padding: 0.75rem 1.5rem;
  border-radius: 2rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 700;
  font-size: 1rem;
  color: white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.status-indicator {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: white;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.7;
    transform: scale(1.2);
  }
}

.badge-pending {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
}

.badge-processing {
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
}

.badge-ready {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
}

.badge-completed {
  background: linear-gradient(135deg, #6b7280 0%, #4b5563 100%);
}

.order-header {
  margin-top: 4rem;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
  padding-bottom: 2rem;
  border-bottom: 2px solid #e5e7eb;
}

.order-number-section {
  flex: 1;
}

.order-number-label {
  font-size: 1rem;
  color: #6b7280;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
}

.order-number {
  font-size: 3rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0;
  letter-spacing: 0.1em;
}

.order-time-section {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 0.75rem;
}

.time-icon {
  font-size: 1.5rem;
  color: #667eea;
}

.time-info {
  display: flex;
  flex-direction: column;
}

.time-label {
  font-size: 0.875rem;
  color: #6b7280;
}

.time-value {
  font-size: 1rem;
  font-weight: 600;
  color: #1f2937;
}

.order-details {
  margin-bottom: 2rem;
}

.detail-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
  margin-bottom: 1.5rem;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
  background: #f9fafb;
  border-radius: 0.75rem;
}

.detail-icon {
  font-size: 2rem;
  color: #667eea;
}

.detail-content {
  display: flex;
  flex-direction: column;
}

.detail-label {
  font-size: 0.875rem;
  color: #6b7280;
}

.detail-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
}

.order-type-badge,
.table-info {
  display: inline-flex;
  align-items: center;
  padding: 0.75rem 1.5rem;
  background: #eff6ff;
  color: #1e40af;
  border-radius: 0.75rem;
  font-weight: 600;
  margin-bottom: 1rem;
  margin-right: 1rem;
}

.order-items-section {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 2px solid #e5e7eb;
}

.items-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 1.5rem 0;
}

.items-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.order-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 0.75rem;
}

.item-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.item-name {
  font-size: 1.125rem;
  font-weight: 600;
  color: #1f2937;
}

.item-quantity {
  font-size: 0.875rem;
  color: #6b7280;
}

.item-price {
  font-size: 1.125rem;
  font-weight: 700;
  color: #667eea;
}

.payment-status {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 2px solid #e5e7eb;
}

.payment-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.75rem 1.5rem;
  border-radius: 0.75rem;
  font-weight: 600;
  color: white;
}

.payment-pending {
  background: #f59e0b;
}

.payment-paid {
  background: #10b981;
}

.payment-refunded {
  background: #ef4444;
}

.delivery-info {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 2px solid #e5e7eb;
}

.delivery-status-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.75rem 1.5rem;
  border-radius: 0.75rem;
  font-weight: 600;
  color: white;
  margin-bottom: 1rem;
}

.delivery-pending {
  background: #f59e0b;
}

.delivery-transit {
  background: #3b82f6;
}

.delivery-delivered {
  background: #10b981;
}

.delivery-failed {
  background: #ef4444;
}

.delivery-details {
  margin-top: 1rem;
}

.delivery-driver {
  color: #6b7280;
  margin: 0;
}

.order-notes {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #fef3c7;
  border-right: 4px solid #f59e0b;
  border-radius: 0.5rem;
  display: flex;
  gap: 0.75rem;
}

.notes-icon {
  font-size: 1.25rem;
  color: #f59e0b;
  flex-shrink: 0;
}

.notes-text {
  margin: 0;
  color: #92400e;
  font-size: 0.9375rem;
}

.auto-refresh-indicator {
  margin-top: 2rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  color: #9ca3af;
  font-size: 0.875rem;
}

.refresh-icon {
  animation: rotate 2s linear infinite;
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.search-another-button {
  padding: 1rem 2rem;
  background: white;
  color: #667eea;
  border: 2px solid #667eea;
  border-radius: 0.75rem;
  font-size: 1.125rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  transition: all 0.3s ease;
  width: 100%;
  max-width: 400px;
  margin: 0 auto;
}

.search-another-button:hover {
  background: #667eea;
  color: white;
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(102, 126, 234, 0.3);
}

/* Responsive Design */
@media (max-width: 768px) {
  .order-status-container {
    padding: 1rem;
  }

  .restaurant-name {
    font-size: 1.75rem;
  }

  .search-container {
    flex-direction: column;
    padding: 1.5rem;
  }

  .search-input {
    font-size: 1rem;
  }

  .order-status-card {
    padding: 1.5rem;
  }

  .order-header {
    flex-direction: column;
    gap: 1.5rem;
  }

  .order-number {
    font-size: 2rem;
  }

  .detail-row {
    grid-template-columns: 1fr;
  }

  .status-badge {
    position: relative;
    top: 0;
    left: 0;
    margin-bottom: 1rem;
  }
}
</style>

