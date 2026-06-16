<template>
  <div class="os">
    <!-- Hero -->
    <header class="os-hero">
      <div class="os-hero-bg"></div>
      <div class="os-hero-inner">
        <div class="os-brand">
          <div class="os-logo-wrap">
            <img
              v-if="restaurantLogo && !logoError"
              :src="restaurantLogo"
              alt=""
              class="os-logo"
              @error="logoError = true"
            />
            <div v-else class="os-logo-fallback">
              <b-icon icon="shop"></b-icon>
            </div>
          </div>
          <div class="os-brand-text">
            <p class="os-eyebrow">{{ $t('orderStatusTitle') || 'تتبع الطلب' }}</p>
            <h1 class="os-title">{{ restaurantName || ($t('orderStatus') || 'حالة الطلب') }}</h1>
            <p class="os-tagline">{{ $t('trackOrderHint') || 'أدخل كود الطلب لمعرفة حالة تحضيره' }}</p>
          </div>
        </div>
      </div>
    </header>

    <main class="os-main">
      <!-- Search -->
      <section v-if="!order" class="os-search-card">
        <div class="os-search-head">
          <b-icon icon="search" class="os-search-head-icon"></b-icon>
          <div>
            <h2 class="os-search-title">{{ $t('searchOrder') || 'بحث عن الطلب' }}</h2>
            <p class="os-search-sub">{{ $t('enterOrderCodeToCheck') || 'أدخل كود الطلب للتحقق من حالته' }}</p>
          </div>
        </div>

        <div class="os-search-form">
          <div class="os-input-wrap">
            <b-icon icon="hash" class="os-input-icon"></b-icon>
            <input
              v-model="orderCodeInput"
              type="text"
              inputmode="numeric"
              class="os-input"
              :placeholder="$t('enterOrderCode') || 'أدخل كود الطلب'"
              @keyup.enter="searchOrder"
              autofocus
            />
          </div>
          <button
            type="button"
            class="os-btn os-btn--primary"
            @click="searchOrder"
            :disabled="loading || !orderCodeInput.trim()"
          >
            <b-spinner small v-if="loading"></b-spinner>
            <template v-else>
              <b-icon icon="arrow-left-circle-fill"></b-icon>
              {{ $t('searchOrder') || 'بحث' }}
            </template>
          </button>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="os-inline-state">
          <div class="os-spinner"></div>
          <span>{{ $t('loading') || 'جاري التحميل...' }}</span>
        </div>

        <!-- Error -->
        <div v-else-if="error" class="os-alert os-alert--error">
          <b-icon icon="exclamation-triangle-fill"></b-icon>
          <div>
            <p>{{ error }}</p>
            <button type="button" class="os-link-btn" @click="searchOrder">
              {{ $t('tryAgain') || 'حاول مرة أخرى' }}
            </button>
          </div>
        </div>

        <!-- Empty hint -->
        <div v-else class="os-hint">
          <b-icon icon="receipt"></b-icon>
          <span>{{ $t('orderCodeOnReceipt') || 'ستجد كود الطلب على الفاتورة' }}</span>
        </div>
      </section>

      <!-- Order result -->
      <section v-else class="os-result">
        <!-- Status hero -->
        <div class="os-status-hero" :class="statusHeroClass">
          <div class="os-status-icon-wrap">
            <b-icon :icon="statusIcon"></b-icon>
          </div>
          <p class="os-status-label">{{ $t('currentStatus') || 'الحالة الحالية' }}</p>
          <h2 class="os-status-text">{{ statusText }}</h2>
          <p v-if="statusHint" class="os-status-hint">{{ statusHint }}</p>
        </div>

        <!-- Progress timeline -->
        <div class="os-timeline" v-if="!isCancelled">
          <div
            v-for="(step, idx) in statusSteps"
            :key="step.key"
            class="os-step"
            :class="{
              'os-step--done': step.done,
              'os-step--active': step.active,
            }"
          >
            <div class="os-step-dot">
              <b-icon v-if="step.done" icon="check-lg"></b-icon>
              <span v-else>{{ idx + 1 }}</span>
            </div>
            <span class="os-step-label">{{ step.label }}</span>
          </div>
          <div class="os-timeline-track" :style="{ '--progress': timelineProgress }"></div>
        </div>

        <!-- Order card -->
        <div class="os-order-card">
          <div class="os-order-top">
            <div>
              <span class="os-order-label">{{ $t('orderNumber') || 'رقم الطلب' }}</span>
              <div class="os-order-code">{{ order.orderCode }}</div>
              <div v-if="order.dailySequenceNumber" class="os-daily-num">
                #{{ order.dailySequenceNumber }}
              </div>
            </div>
            <div class="os-order-time">
              <b-icon icon="clock"></b-icon>
              <span>{{ formattedTime }}</span>
            </div>
          </div>

          <div class="os-chips">
            <span v-if="order.orderType" class="os-chip os-chip--type">
              <b-icon :icon="getOrderTypeIcon(order.orderType)"></b-icon>
              {{ getOrderTypeText(order.orderType) }}
            </span>
            <span v-if="order.tableNumber" class="os-chip">
              <b-icon icon="table"></b-icon>
              {{ $t('table') || 'طاولة' }} #{{ order.tableNumber }}
            </span>
            <span v-if="order.paymentStatus" class="os-chip" :class="paymentChipClass">
              <b-icon :icon="getPaymentStatusIcon(order.paymentStatus)"></b-icon>
              {{ getPaymentStatusText(order.paymentStatus) }}
            </span>
          </div>

          <div class="os-stats">
            <div class="os-stat">
              <b-icon icon="box-seam"></b-icon>
              <div>
                <span class="os-stat-val">{{ order.itemsCount }}</span>
                <span class="os-stat-lbl">{{ $t('itemsCount') || 'عدد العناصر' }}</span>
              </div>
            </div>
            <div class="os-stat">
              <b-icon icon="cash-stack"></b-icon>
              <div>
                <span class="os-stat-val">{{ formattedTotal }}</span>
                <span class="os-stat-lbl">{{ $t('orderTotal') || 'المجموع' }} (د.ع)</span>
              </div>
            </div>
          </div>

          <!-- Items -->
          <div v-if="order.items && order.items.length" class="os-items">
            <h3 class="os-items-title">{{ $t('orderItems') || 'عناصر الطلب' }}</h3>
            <div class="os-items-list">
              <div v-for="item in order.items" :key="item.id" class="os-item-row">
                <div class="os-item-info">
                  <span class="os-item-name">{{ item.itemName }}</span>
                  <span class="os-item-qty">× {{ item.quantity }}</span>
                </div>
                <span class="os-item-price">{{ formatPrice(item.total) }} د.ع</span>
              </div>
            </div>
          </div>

          <!-- Delivery -->
          <div v-if="order.orderType === 'Delivery' && order.deliveryStatus" class="os-delivery">
            <div class="os-delivery-badge" :class="getDeliveryStatusClass(order.deliveryStatus)">
              <b-icon icon="truck"></b-icon>
              {{ getDeliveryStatusText(order.deliveryStatus) }}
            </div>
            <p v-if="order.deliveryDriver" class="os-driver">
              <b-icon icon="person"></b-icon>
              {{ $t('driverName') || 'السائق' }}: {{ order.deliveryDriver.name }}
            </p>
          </div>

          <!-- Notes -->
          <div v-if="order.notes" class="os-notes">
            <b-icon icon="chat-left-text"></b-icon>
            <p>{{ order.notes }}</p>
          </div>

          <div class="os-refresh">
            <b-icon icon="arrow-clockwise" class="os-refresh-icon"></b-icon>
            {{ $t('autoRefresh') || 'تحديث تلقائي كل 10 ثوانٍ' }}
          </div>
        </div>

        <div class="os-actions">
          <button type="button" class="os-btn os-btn--outline" @click="resetSearch">
            <b-icon icon="search"></b-icon>
            {{ $t('searchAnotherOrder') || 'بحث عن طلب آخر' }}
          </button>
          <router-link :to="menuLink" class="os-btn os-btn--ghost">
            <b-icon icon="book"></b-icon>
            {{ $t('publicMenu') || 'القائمة' }}
          </router-link>
        </div>
      </section>
    </main>

    <footer class="os-footer">
      <p>{{ restaurantName }}</p>
      <span>Lite Casher</span>
    </footer>
  </div>
</template>

<script>
import { HTTP } from '../http/api.js';
import { formatBusinessDateTime } from '../utils/formatBusinessDateTime.js';

const STATUS_ORDER = ['Pending', 'Processing', 'Ready', 'Completed'];

export default {
  name: 'OrderStatusView',
  data() {
    return {
      orderCodeInput: '',
      order: null,
      loading: false,
      error: null,
      refreshInterval: null,
      previousStatus: null,
      restaurantName: '',
      restaurantLogo: '',
      logoError: false,
      commercialUserId: null,
    };
  },
  computed: {
    menuLink() {
      return `/menu/${this.commercialUserId}`;
    },
    isCancelled() {
      return this.order?.orderStatus === 'Cancelled';
    },
    statusText() {
      if (!this.order) return '';
      const map = {
        Pending: this.$t('orderStatusPending') || 'قيد الانتظار',
        Processing: this.$t('orderStatusProcessing') || 'قيد التحضير',
        Ready: this.$t('orderStatusReady') || 'جاهز',
        Completed: this.$t('orderStatusCompleted') || 'مكتمل',
        Cancelled: this.$t('cancelled') || 'ملغي',
      };
      return map[this.order.orderStatus] || this.order.orderStatus;
    },
    statusHint() {
      const hints = {
        Pending: this.$t('statusHintPending') || 'تم استلام طلبك وسيبدأ التحضير قريباً',
        Processing: this.$t('statusHintProcessing') || 'المطبخ يحضّر طلبك الآن',
        Ready: this.$t('statusHintReady') || 'طلبك جاهز للاستلام!',
        Completed: this.$t('statusHintCompleted') || 'تم تسليم الطلب — بالعافية',
        Cancelled: this.$t('statusHintCancelled') || 'تم إلغاء هذا الطلب',
      };
      return hints[this.order?.orderStatus] || '';
    },
    statusIcon() {
      const icons = {
        Pending: 'clock-history',
        Processing: 'gear-fill',
        Ready: 'bag-check-fill',
        Completed: 'check-circle-fill',
        Cancelled: 'x-circle-fill',
      };
      return icons[this.order?.orderStatus] || 'receipt';
    },
    statusHeroClass() {
      return `os-status-hero--${(this.order?.orderStatus || 'pending').toLowerCase()}`;
    },
    paymentChipClass() {
      const s = this.order?.paymentStatus;
      if (s === 'Paid') return 'os-chip--paid';
      if (s === 'Refunded') return 'os-chip--refunded';
      return 'os-chip--pending-pay';
    },
    statusSteps() {
      const current = this.order?.orderStatus || 'Pending';
      const currentIdx = STATUS_ORDER.indexOf(current);
      return STATUS_ORDER.map((key, idx) => ({
        key,
        label: {
          Pending: this.$t('orderStatusPending') || 'انتظار',
          Processing: this.$t('orderStatusProcessing') || 'تحضير',
          Ready: this.$t('orderStatusReady') || 'جاهز',
          Completed: this.$t('orderStatusCompleted') || 'مكتمل',
        }[key],
        done: currentIdx > idx || current === 'Completed',
        active: current === key,
      }));
    },
    timelineProgress() {
      const current = this.order?.orderStatus || 'Pending';
      const idx = STATUS_ORDER.indexOf(current);
      if (idx < 0) return '0%';
      return `${(idx / (STATUS_ORDER.length - 1)) * 100}%`;
    },
    formattedTime() {
      if (!this.order?.insertDate) return '';
      return formatBusinessDateTime(this.order.insertDate);
    },
    formattedTotal() {
      if (!this.order?.total) return '0';
      return this.formatPrice(this.order.total);
    },
  },
  mounted() {
    this.commercialUserId = parseInt(this.$route.params.commercialUserId, 10);

    if (this.$route.params.orderCode) {
      this.orderCodeInput = this.$route.params.orderCode;
      this.searchOrder();
    }

    this.loadRestaurantInfo();
    document.documentElement.classList.add('order-status-page');
  },
  beforeDestroy() {
    this.stopAutoRefresh();
    document.documentElement.classList.remove('order-status-page');
  },
  methods: {
    async searchOrder() {
      if (!this.orderCodeInput.trim()) {
        this.error = this.$t('enterOrderCode') || 'يرجى إدخال كود الطلب';
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

          if (this.previousStatus && this.previousStatus !== this.order.orderStatus) {
            this.playNotificationSound();
          }
        } else {
          this.error = response.data?.message || this.$t('orderNotFound') || 'الطلب غير موجود';
          this.order = null;
          this.stopAutoRefresh();
        }
      } catch (err) {
        console.error('Error fetching order status:', err);
        this.error = err.response?.data?.message || this.$t('orderNotFound') || 'الطلب غير موجود';
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
          const oldStatus = this.order.orderStatus;
          this.order = response.data.data;

          if (oldStatus !== this.order.orderStatus) {
            this.playNotificationSound();
          }
        }
      } catch (err) {
        console.error('Error refreshing order status:', err);
      }
    },
    startAutoRefresh() {
      this.stopAutoRefresh();
      this.refreshInterval = setInterval(() => this.fetchOrderStatus(), 10000);
    },
    stopAutoRefresh() {
      if (this.refreshInterval) {
        clearInterval(this.refreshInterval);
        this.refreshInterval = null;
      }
    },
    resetSearch() {
      this.order = null;
      this.orderCodeInput = '';
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
        console.error('Error loading restaurant info:', err);
      }
    },
    formatPrice(price) {
      if (!price) return '0';
      return parseFloat(price).toLocaleString('ar-IQ');
    },
    getOrderTypeIcon(type) {
      return { DineIn: 'house-door', Takeaway: 'bag', Delivery: 'truck' }[type] || 'cart';
    },
    getOrderTypeText(type) {
      return {
        DineIn: this.$t('dineIn') || 'داخلي',
        Takeaway: this.$t('takeaway') || 'طلب خارجي',
        Delivery: this.$t('delivery') || 'توصيل',
      }[type] || type;
    },
    getPaymentStatusIcon(status) {
      return { Pending: 'clock', Paid: 'check-circle', Refunded: 'arrow-counterclockwise' }[status] || 'clock';
    },
    getPaymentStatusText(status) {
      return {
        Pending: this.$t('pending') || 'قيد الانتظار',
        Paid: this.$t('paid') || 'مدفوع',
        Refunded: this.$t('refunded') || 'مسترد',
      }[status] || status;
    },
    getDeliveryStatusClass(status) {
      return {
        Pending: 'os-delivery--pending',
        InTransit: 'os-delivery--transit',
        Delivered: 'os-delivery--done',
        Failed: 'os-delivery--fail',
      }[status] || '';
    },
    getDeliveryStatusText(status) {
      return {
        Pending: this.$t('pending') || 'قيد الانتظار',
        InTransit: this.$t('inTransit') || 'قيد التوصيل',
        Delivered: this.$t('delivered') || 'تم التوصيل',
        Failed: this.$t('failed') || 'فشل التوصيل',
      }[status] || status;
    },
    playNotificationSound() {
      try {
        const audio = new Audio(require('../assets/beep.mp3'));
        audio.volume = 0.5;
        audio.play().catch(() => {});
      } catch {
        /* optional */
      }
    },
  },
};
</script>

<style scoped>
.os {
  --os-bg: #f7f3ee;
  --os-surface: #ffffff;
  --os-accent: #b8864a;
  --os-accent-dark: #966b35;
  --os-accent-soft: rgba(184, 134, 74, 0.12);
  --os-text: #1c1917;
  --os-muted: #78716c;
  --os-border: #e7e0d8;
  --os-shadow: 0 4px 24px rgba(28, 25, 23, 0.08);
  --os-radius: 16px;

  min-height: 100vh;
  background: var(--os-bg);
  color: var(--os-text);
  font-family: 'Cairo', sans-serif;
  padding-bottom: 2rem;
}

/* Hero */
.os-hero {
  position: relative;
  overflow: hidden;
  background: linear-gradient(145deg, #2c2419 0%, #1a1612 55%, #0f0d0b 100%);
  color: #fff;
}

.os-hero-bg {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(ellipse 80% 60% at 80% 20%, rgba(184, 134, 74, 0.25), transparent),
    radial-gradient(ellipse 60% 50% at 10% 80%, rgba(184, 134, 74, 0.12), transparent);
  pointer-events: none;
}

.os-hero-inner {
  position: relative;
  max-width: 640px;
  margin: 0 auto;
  padding: 2rem 1.25rem 1.75rem;
}

.os-brand {
  display: flex;
  align-items: center;
  gap: 1.125rem;
}

.os-logo {
  width: 76px;
  height: 76px;
  object-fit: contain;
  border-radius: 50%;
  background: #fff;
  padding: 0.4rem;
  box-shadow: 0 8px 28px rgba(0, 0, 0, 0.35);
}

.os-logo-fallback {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  background: var(--os-accent-soft);
  border: 2px solid rgba(184, 134, 74, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  color: var(--os-accent);
}

.os-eyebrow {
  margin: 0 0 0.2rem;
  font-size: 0.8125rem;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.55);
  letter-spacing: 0.04em;
}

.os-title {
  margin: 0 0 0.3rem;
  font-size: clamp(1.375rem, 4vw, 1.875rem);
  font-weight: 800;
  line-height: 1.25;
  color: #fff8f0;
  background: none;
  -webkit-text-fill-color: #fff8f0;
}

.os-tagline {
  margin: 0;
  font-size: 0.875rem;
  color: rgba(255, 255, 255, 0.62);
}

/* Main */
.os-main {
  max-width: 640px;
  margin: -1rem auto 0;
  padding: 0 1.25rem;
  position: relative;
  z-index: 1;
}

/* Search card */
.os-search-card {
  background: var(--os-surface);
  border-radius: var(--os-radius);
  padding: 1.5rem;
  box-shadow: var(--os-shadow);
  border: 1px solid var(--os-border);
}

.os-search-head {
  display: flex;
  align-items: flex-start;
  gap: 0.875rem;
  margin-bottom: 1.25rem;
}

.os-search-head-icon {
  font-size: 1.5rem;
  color: var(--os-accent);
  margin-top: 0.15rem;
}

.os-search-title {
  margin: 0 0 0.2rem;
  font-size: 1.125rem;
  font-weight: 800;
}

.os-search-sub {
  margin: 0;
  font-size: 0.875rem;
  color: var(--os-muted);
}

.os-search-form {
  display: flex;
  gap: 0.625rem;
}

.os-input-wrap {
  flex: 1;
  position: relative;
}

.os-input-icon {
  position: absolute;
  right: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--os-muted);
  pointer-events: none;
}

.os-input {
  width: 100%;
  padding: 0.875rem 2.75rem 0.875rem 1rem;
  border: 1.5px solid var(--os-border);
  border-radius: 12px;
  font-family: inherit;
  font-size: 1.0625rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  color: var(--os-text);
  background: var(--os-bg);
  transition: border-color 0.2s, box-shadow 0.2s;
}

.os-input:focus {
  outline: none;
  border-color: var(--os-accent);
  box-shadow: 0 0 0 3px var(--os-accent-soft);
  background: var(--os-surface);
}

.os-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.875rem 1.25rem;
  border-radius: 12px;
  font-family: inherit;
  font-size: 0.9375rem;
  font-weight: 700;
  border: none;
  cursor: pointer;
  text-decoration: none;
  transition: transform 0.15s, box-shadow 0.2s;
  white-space: nowrap;
}

.os-btn:active:not(:disabled) {
  transform: scale(0.98);
}

.os-btn--primary {
  background: linear-gradient(135deg, var(--os-accent) 0%, var(--os-accent-dark) 100%);
  color: #fff;
  box-shadow: 0 4px 14px rgba(184, 134, 74, 0.35);
}

.os-btn--primary:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.os-btn--outline {
  background: var(--os-surface);
  color: var(--os-accent-dark);
  border: 1.5px solid var(--os-accent);
  flex: 1;
}

.os-btn--ghost {
  background: var(--os-bg);
  color: var(--os-text);
  border: 1.5px solid var(--os-border);
}

.os-inline-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  margin-top: 1.25rem;
  color: var(--os-muted);
  font-size: 0.9375rem;
}

.os-spinner {
  width: 22px;
  height: 22px;
  border: 2px solid var(--os-border);
  border-top-color: var(--os-accent);
  border-radius: 50%;
  animation: os-spin 0.7s linear infinite;
}

@keyframes os-spin {
  to { transform: rotate(360deg); }
}

.os-alert {
  display: flex;
  gap: 0.75rem;
  margin-top: 1.25rem;
  padding: 1rem;
  border-radius: 12px;
  font-size: 0.9375rem;
}

.os-alert--error {
  background: #fef2f2;
  color: #b91c1c;
  border: 1px solid #fecaca;
}

.os-alert p {
  margin: 0 0 0.35rem;
}

.os-link-btn {
  background: none;
  border: none;
  color: #b91c1c;
  font-family: inherit;
  font-weight: 700;
  cursor: pointer;
  text-decoration: underline;
  padding: 0;
  font-size: 0.875rem;
}

.os-hint {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-top: 1.25rem;
  padding-top: 1.25rem;
  border-top: 1px dashed var(--os-border);
  color: #78716c;
  font-size: 0.875rem;
}

.os-hint span,
.os-hint .b-icon {
  color: #78716c;
  -webkit-text-fill-color: #78716c;
}

/* Result */
.os-result {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* Status hero — light card, accent border (matches menu theme) */
.os-status-hero {
  text-align: center;
  padding: 1.75rem 1.25rem;
  border-radius: var(--os-radius);
  background: var(--os-surface);
  border: 1px solid var(--os-border);
  border-right: 5px solid var(--os-status-accent, var(--os-accent));
  box-shadow: var(--os-shadow);
  color: var(--os-text);
}

.os-status-hero--pending {
  --os-status-accent: #d4a017;
  --os-status-soft: rgba(212, 160, 23, 0.12);
  --os-status-icon: #b8860b;
  background: linear-gradient(135deg, rgba(212, 160, 23, 0.1) 0%, var(--os-surface) 55%);
}

.os-status-hero--processing {
  --os-status-accent: #b8864a;
  --os-status-soft: rgba(184, 134, 74, 0.14);
  --os-status-icon: #966b35;
  background: linear-gradient(135deg, rgba(184, 134, 74, 0.12) 0%, var(--os-surface) 55%);
}

.os-status-hero--ready {
  --os-status-accent: #5a9a6e;
  --os-status-soft: rgba(90, 154, 110, 0.12);
  --os-status-icon: #3d7a52;
  background: linear-gradient(135deg, rgba(90, 154, 110, 0.1) 0%, var(--os-surface) 55%);
}

.os-status-hero--completed {
  --os-status-accent: #78716c;
  --os-status-soft: rgba(120, 113, 108, 0.1);
  --os-status-icon: #57534e;
  background: linear-gradient(135deg, rgba(120, 113, 108, 0.08) 0%, var(--os-surface) 55%);
}

.os-status-hero--cancelled {
  --os-status-accent: #c45c5c;
  --os-status-soft: rgba(196, 92, 92, 0.1);
  --os-status-icon: #b04040;
  background: linear-gradient(135deg, rgba(196, 92, 92, 0.08) 0%, var(--os-surface) 55%);
}

.os-status-icon-wrap {
  width: 56px;
  height: 56px;
  margin: 0 auto 0.75rem;
  background: var(--os-status-soft, var(--os-accent-soft));
  color: var(--os-status-icon, var(--os-accent-dark));
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.625rem;
}

.os-status-label {
  margin: 0 0 0.25rem;
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--os-muted);
}

.os-status-text {
  margin: 0 0 0.35rem;
  font-size: 1.625rem;
  font-weight: 800;
  color: var(--os-status-icon, var(--os-accent-dark));
}

.os-status-hint {
  margin: 0;
  font-size: 0.875rem;
  color: var(--os-muted);
  line-height: 1.55;
}

/* Timeline */
.os-timeline {
  position: relative;
  display: flex;
  justify-content: space-between;
  background: var(--os-surface);
  border-radius: var(--os-radius);
  padding: 1.25rem 1rem 1rem;
  border: 1px solid var(--os-border);
  box-shadow: var(--os-shadow);
  overflow: hidden;
}

.os-timeline-track {
  position: absolute;
  top: 2.05rem;
  right: 12%;
  left: 12%;
  height: 3px;
  background: var(--os-border);
  border-radius: 2px;
  pointer-events: none;
}

.os-timeline-track::after {
  content: '';
  position: absolute;
  top: 0;
  right: 0;
  height: 100%;
  width: var(--progress, 0%);
  background: var(--os-accent);
  border-radius: 2px;
  transition: width 0.5s ease;
}

.os-step {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  flex: 1;
}

.os-step-dot {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: var(--os-bg);
  border: 2px solid var(--os-border);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8125rem;
  font-weight: 800;
  color: var(--os-muted);
  transition: all 0.3s;
}

.os-step--done .os-step-dot,
.os-step--active .os-step-dot {
  background: var(--os-accent);
  border-color: var(--os-accent);
  color: #fff;
}

.os-step-label {
  font-size: 0.6875rem;
  font-weight: 700;
  color: var(--os-muted);
  text-align: center;
}

.os-step--active .os-step-label,
.os-step--done .os-step-label {
  color: var(--os-accent-dark);
}

/* Order card */
.os-order-card {
  background: var(--os-surface);
  border-radius: var(--os-radius);
  padding: 1.25rem;
  border: 1px solid var(--os-border);
  box-shadow: var(--os-shadow);
}

.os-order-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--os-border);
  margin-bottom: 1rem;
}

.os-order-label {
  font-size: 0.8125rem;
  color: var(--os-muted);
  font-weight: 600;
}

.os-order-code {
  font-size: 1.5rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  margin-top: 0.15rem;
}

.os-daily-num {
  display: inline-block;
  margin-top: 0.35rem;
  font-size: 0.875rem;
  font-weight: 800;
  color: var(--os-accent-dark);
  background: var(--os-accent-soft);
  padding: 0.15rem 0.55rem;
  border-radius: 6px;
}

.os-order-time {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.8125rem;
  color: var(--os-muted);
  background: var(--os-bg);
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  white-space: nowrap;
}

.os-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.os-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.35rem 0.75rem;
  border-radius: 999px;
  font-size: 0.8125rem;
  font-weight: 700;
  background: var(--os-bg);
  color: var(--os-text);
  border: 1px solid var(--os-border);
}

.os-chip--type {
  background: var(--os-accent-soft);
  color: var(--os-accent-dark);
  border-color: transparent;
}

.os-chip--paid { background: #dcfce7; color: #166534; border-color: transparent; }
.os-chip--pending-pay { background: #fef3c7; color: #92400e; border-color: transparent; }
.os-chip--refunded { background: #fee2e2; color: #991b1b; border-color: transparent; }

.os-stats {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.os-stat {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.875rem;
  background: var(--os-bg);
  border-radius: 12px;
}

.os-stat .b-icon {
  font-size: 1.375rem;
  color: var(--os-accent);
}

.os-stat-val {
  display: block;
  font-size: 1.125rem;
  font-weight: 800;
  line-height: 1.2;
}

.os-stat-lbl {
  font-size: 0.75rem;
  color: var(--os-muted);
}

.os-items-title {
  margin: 0 0 0.75rem;
  font-size: 1rem;
  font-weight: 800;
}

.os-items-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.os-item-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.625rem 0.75rem;
  background: var(--os-bg);
  border-radius: 10px;
  gap: 0.75rem;
}

.os-item-name {
  font-weight: 700;
  font-size: 0.9375rem;
}

.os-item-qty {
  font-size: 0.8125rem;
  color: var(--os-muted);
  margin-right: 0.35rem;
}

.os-item-price {
  font-weight: 800;
  color: var(--os-accent-dark);
  white-space: nowrap;
  font-size: 0.9375rem;
}

.os-delivery {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--os-border);
}

.os-delivery-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 0.875rem;
  border-radius: 999px;
  font-size: 0.875rem;
  font-weight: 700;
  color: #fff;
}

.os-delivery--pending { background: #d97706; }
.os-delivery--transit { background: #2563eb; }
.os-delivery--done { background: #059669; }
.os-delivery--fail { background: #dc2626; }

.os-driver {
  margin: 0.625rem 0 0;
  font-size: 0.875rem;
  color: var(--os-muted);
  display: flex;
  align-items: center;
  gap: 0.35rem;
}

.os-notes {
  display: flex;
  gap: 0.625rem;
  margin-top: 1rem;
  padding: 0.875rem;
  background: #fffbeb;
  border-radius: 10px;
  border-right: 3px solid #f59e0b;
}

.os-notes p {
  margin: 0;
  font-size: 0.875rem;
  color: #92400e;
  line-height: 1.5;
}

.os-notes .b-icon {
  color: #f59e0b;
  flex-shrink: 0;
  margin-top: 0.1rem;
}

.os-refresh {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px dashed var(--os-border);
  font-size: 0.8125rem;
  color: var(--os-muted);
}

.os-refresh-icon {
  animation: os-spin 2s linear infinite;
}

.os-actions {
  display: flex;
  gap: 0.625rem;
}

/* Footer */
.os-footer {
  text-align: center;
  padding: 2rem 1rem 0.5rem;
  color: var(--os-muted);
  font-size: 0.8125rem;
}

.os-footer p {
  margin: 0 0 0.2rem;
  font-weight: 600;
  color: var(--os-text);
}

/* Responsive */
@media (max-width: 520px) {
  .os-brand {
    flex-direction: column;
    text-align: center;
  }

  .os-search-form {
    flex-direction: column;
  }

  .os-btn--primary {
    width: 100%;
  }

  .os-order-top {
    flex-direction: column;
  }

  .os-stats {
    grid-template-columns: 1fr;
  }

  .os-step-label {
    font-size: 0.625rem;
  }

  .os-actions {
    flex-direction: column;
  }
}
</style>

<style>
html.order-status-page,
html.order-status-page body {
  background: #f7f3ee !important;
  color: #1c1917 !important;
}

html.order-status-page #app {
  background: #f7f3ee;
}

html.order-status-page .os-title {
  color: #fff8f0 !important;
  -webkit-text-fill-color: #fff8f0 !important;
  background: none !important;
}

html.order-status-page .os-hint,
html.order-status-page .os-hint span,
html.order-status-page .os-footer,
html.order-status-page .os-state,
html.order-status-page .os-search-sub {
  -webkit-text-fill-color: unset !important;
  background: none !important;
}

html.order-status-page .os-hint,
html.order-status-page .os-hint span {
  color: #78716c !important;
}
</style>
