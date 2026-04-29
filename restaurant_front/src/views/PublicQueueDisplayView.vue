<template>
  <div class="public-queue-display">
    <!-- Queue Board - Full Screen -->
    <div class="public-queue-board">
      <!-- Pending Column -->
      <div class="public-queue-column pending-column">
        <div class="public-queue-column-header pending-header">
          <div class="public-column-header-content">
            <b-icon icon="clock-history" class="public-column-icon"></b-icon>
            <h2 class="public-column-title">{{ $t("pending") || "قيد الانتظار" }}</h2>
            <span class="public-column-count">{{ pendingOrders.length }}</span>
          </div>
        </div>
        <div class="public-queue-column-body">
          <div 
            class="public-queue-card" 
            v-for="order in pendingOrders" 
            :key="order.id"
          >
            <div class="public-card-header">
              <div class="public-order-code">{{ order.orderCode }}</div>
              <div class="public-order-type" :class="getOrderTypeClass(order.orderType)">
                {{ getOrderTypeText(order.orderType) }}
              </div>
            </div>
            <div class="public-card-body" v-if="order.deliveryDriver || order.notes">
              <div v-if="order.deliveryDriver" class="public-order-info-item">
                <b-icon icon="truck" class="public-info-icon"></b-icon>
                <span class="public-info-text">{{ order.deliveryDriver.name }}</span>
              </div>
              <div v-if="order.notes" class="public-order-info-item">
                <b-icon icon="chat-left-text" class="public-info-icon"></b-icon>
                <span class="public-info-text public-notes">{{ order.notes }}</span>
              </div>
            </div>
          </div>
          <div v-if="pendingOrders.length === 0" class="public-empty-state">
            <b-icon icon="inbox" class="public-empty-icon"></b-icon>
            <p class="public-empty-text">{{ $t("noPendingOrders") || "لا توجد طلبات قيد الانتظار" }}</p>
          </div>
        </div>
      </div>

      <!-- Completed Column -->
      <div class="public-queue-column completed-column">
        <div class="public-queue-column-header completed-header">
          <div class="public-column-header-content">
            <b-icon icon="check2-circle" class="public-column-icon"></b-icon>
            <h2 class="public-column-title">{{ $t("completed") || "مكتملة" }}</h2>
            <span class="public-column-count">{{ completedOrders.length }}</span>
          </div>
        </div>
        <div class="public-queue-column-body">
          <div 
            class="public-queue-card completed-card" 
            v-for="order in completedOrders" 
            :key="order.id"
          >
            <div class="public-card-header">
              <div class="public-order-code">{{ order.orderCode }}</div>
              <div class="public-order-type" :class="getOrderTypeClass(order.orderType)">
                {{ getOrderTypeText(order.orderType) }}
              </div>
            </div>
            <div class="public-card-body">
              <div class="public-order-info-item">
                <b-icon icon="hash" class="public-info-icon"></b-icon>
                <span class="public-info-text">{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
              </div>
              <div class="public-order-info-item">
                <b-icon icon="box-seam" class="public-info-icon"></b-icon>
                <span class="public-info-text">{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
              </div>
              <div class="public-order-info-item">
                <b-icon icon="currency-dollar" class="public-info-icon"></b-icon>
                <span class="public-info-text public-price">{{ formatPrice(order.orderPrice || 0) }} د.ع</span>
              </div>
              <div v-if="order.deliveryDriver" class="public-order-info-item">
                <b-icon icon="truck" class="public-info-icon"></b-icon>
                <span class="public-info-text">{{ order.deliveryDriver.name }}</span>
              </div>
            </div>
          </div>
          <div v-if="completedOrders.length === 0" class="public-empty-state">
            <b-icon icon="inbox" class="public-empty-icon"></b-icon>
            <p class="public-empty-text">{{ $t("noCompletedOrders") || "لا توجد طلبات مكتملة" }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { HTTP } from '../http/api.js';
import signalRService from '../services/signalr.js';

export default {
  name: 'PublicQueueDisplayView',
  data() {
    return {
      Orders: [],
      commercialUserId: null,
      refreshInterval: null
    };
  },
  computed: {
    pendingOrders() {
      return this.Orders.filter(o => !o.orderStatus || o.orderStatus === 'Pending');
    },
    completedOrders() {
      return this.Orders.filter(o => o.orderStatus === 'Completed');
    }
  },
  mounted() {
    // Get commercialUserId from route params
    this.commercialUserId = parseInt(this.$route.params.commercialUserId);
    
    if (!this.commercialUserId) {
      console.error('Commercial User ID not found in route params');
      return;
    }

    this.loadOrders();
    this.initializeSignalR();
    
    // Auto refresh every 5 seconds for public display
    this.refreshInterval = setInterval(() => {
      this.loadOrders();
    }, 5000);
  },
  beforeDestroy() {
    this.cleanupSignalR();
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  },
  methods: {
    async loadOrders() {
      try {
        const params = new URLSearchParams({
          pageNumber: '0',
          pageSize: '100'
        });

        // Only load orders from today
        const today = new Date().toISOString().split('T')[0];
        params.append('startDate', today);
        params.append('endDate', today);

        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}/orders?${params.toString()}`);
        
        if (response.data && !response.data.errorStatus) {
          const allOrders = response.data.data.items || [];
          // Filter to show only Pending and Completed orders
          this.Orders = allOrders.filter(o => 
            !o.orderStatus || 
            o.orderStatus === 'Pending' || 
            o.orderStatus === 'Completed'
          );
        }
      } catch (error) {
        console.error('Error loading orders:', error);
      }
    },
    getOrderTypeClass(type) {
      const classes = {
        'DineIn': 'public-dinein-badge',
        'Takeaway': 'public-takeaway-badge',
        'Delivery': 'public-delivery-badge'
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
    initializeSignalR() {
      signalRService.startConnection().then(() => {
        signalRService.on('PublicOrderUpdated', (data) => {
          // Reload orders when an order is updated
          if (data.CommercialUserId === this.commercialUserId) {
            this.loadOrders();
          }
        });
        signalRService.on('PublicOrderAdded', (data) => {
          // Reload orders when a new order is added
          if (data.CommercialUserId === this.commercialUserId) {
            this.loadOrders();
          }
        });
      });
    },
    cleanupSignalR() {
      signalRService.off('PublicOrderUpdated');
      signalRService.off('PublicOrderAdded');
    }
  }
};
</script>

<style scoped>
/* Force Light Mode - Override any theme variables */
.public-queue-display {
  width: 100vw;
  height: 100vh;
  overflow: hidden;
  background: #f8fafc !important;
  padding: 0;
  margin: 0;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
  font-smooth: always;
}

/* Ensure light mode even if system is in dark mode */
.public-queue-display,
.public-queue-display * {
  color-scheme: light;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
}

.public-queue-board {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0;
  height: 100vh;
  width: 100vw;
}

.public-queue-column {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.public-queue-column-header {
  padding: 2rem;
  color: white;
  font-weight: 700;
  flex-shrink: 0;
}

.pending-header {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
}

.completed-header {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
}

.public-column-header-content {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.public-column-icon {
  font-size: 3rem;
}

.public-column-title {
  margin: 0;
  font-size: 2.5rem;
  font-weight: 700;
  flex: 1;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
}

.public-column-count {
  background: rgba(255, 255, 255, 0.3);
  padding: 0.75rem 1.5rem;
  border-radius: 50px;
  font-size: 2rem;
  font-weight: 700;
}

.public-queue-column-body {
  flex: 1;
  overflow-y: auto;
  padding: 2rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  background: #ffffff;
}

.public-queue-card {
  background: #ffffff;
  border-radius: 1rem;
  padding: 2rem;
  border: 3px solid #e2e8f0;
  transition: all 0.3s ease;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.public-queue-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.15);
  border-color: #6366f1;
}

.public-queue-card.completed-card {
  opacity: 0.85;
}

.public-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.public-card-body {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 2px solid #e2e8f0;
}

.public-order-code {
  background: #0f172a;
  color: white;
  padding: 1rem 2rem;
  border-radius: 0.75rem;
  font-weight: 700;
  font-size: 2rem;
  letter-spacing: 0.05em;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
  font-family: 'Cairo', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

.public-order-type {
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-size: 1.5rem;
  font-weight: 600;
  white-space: nowrap;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
  font-family: 'Cairo', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

.public-dinein-badge {
  background: rgba(99, 102, 241, 0.15);
  color: #6366f1;
}

.public-takeaway-badge {
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
}

.public-delivery-badge {
  background: rgba(249, 115, 22, 0.15);
  color: #f97316;
}

.public-order-info-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  font-size: 1.25rem;
  color: #0f172a;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
}

.public-info-icon {
  color: #6366f1;
  font-size: 1.5rem;
  flex-shrink: 0;
}

.public-info-text {
  font-weight: 500;
  color: #0f172a;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
}

.public-notes {
  font-style: italic;
  color: rgba(15, 23, 42, 0.7);
}

.public-empty-state {
  text-align: center;
  padding: 4rem 2rem;
  color: rgba(15, 23, 42, 0.6);
}

.public-empty-icon {
  font-size: 5rem;
  color: rgba(99, 102, 241, 0.3);
  margin-bottom: 1.5rem;
}

.public-empty-text {
  margin: 0;
  font-size: 1.75rem;
  color: rgba(15, 23, 42, 0.6);
}

/* Scrollbar Styling */
.public-queue-column-body::-webkit-scrollbar {
  width: 12px;
}

.public-queue-column-body::-webkit-scrollbar-track {
  background: #f1f5f9;
}

.public-queue-column-body::-webkit-scrollbar-thumb {
  background: #cbd5e0;
  border-radius: 6px;
}

.public-queue-column-body::-webkit-scrollbar-thumb:hover {
  background: #6366f1;
}

@media (max-width: 1024px) {
  .public-queue-board {
    grid-template-columns: 1fr;
  }
  
  .public-column-title {
    font-size: 2rem;
  }
  
  .public-column-count {
    font-size: 1.5rem;
  }
  
  .public-order-code {
    font-size: 1.25rem;
  }
  
  .public-order-info-item {
    font-size: 1.25rem;
  }
}
</style>

