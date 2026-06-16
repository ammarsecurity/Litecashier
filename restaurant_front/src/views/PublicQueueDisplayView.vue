<template>
  <div class="qd">
    <!-- Error -->
    <div v-if="loadError" class="qd-error">
      <b-icon icon="exclamation-triangle-fill"></b-icon>
      <p>{{ loadError }}</p>
    </div>

    <template v-else>
      <!-- Signage header -->
      <header class="qd-header">
        <div class="qd-header-brand">
          <img
            v-if="restaurantLogo && !logoError"
            :src="restaurantLogo"
            alt=""
            class="qd-logo"
            @error="logoError = true"
          />
          <div v-else class="qd-logo-fallback">
            <b-icon icon="shop"></b-icon>
          </div>
          <div>
            <h1 class="qd-restaurant">{{ restaurantName || $t('orderQueue') || 'طابور الطلبات' }}</h1>
            <p class="qd-subtitle">{{ $t('queueDisplaySubtitle') || 'تابع رقم طلبك على الشاشة' }}</p>
          </div>
        </div>
        <div class="qd-header-meta">
          <span class="qd-live">
            <span class="qd-live-dot"></span>
            {{ $t('live') || 'مباشر' }}
          </span>
          <span class="qd-clock">{{ currentTime }}</span>
        </div>
      </header>

      <!-- Board -->
      <div class="qd-board">
        <!-- Preparing -->
        <section class="qd-panel qd-panel--prep">
          <div class="qd-panel-head">
            <div class="qd-panel-title-wrap">
              <b-icon icon="hourglass-split" class="qd-panel-icon"></b-icon>
              <div>
                <h2 class="qd-panel-title">{{ $t('queuePreparing') || 'قيد التحضير' }}</h2>
                <p class="qd-panel-hint">{{ $t('queueActiveHint') || 'انتظار · تحضير · جاهز' }}</p>
              </div>
            </div>
            <span class="qd-panel-count">{{ activeOrders.length }}</span>
          </div>

          <div class="qd-grid-wrap">
            <div v-if="activeOrders.length" class="qd-grid">
              <div
                v-for="order in sortedActiveOrders"
                :key="order.id"
                class="qd-tile"
                :class="tileClass(order)"
              >
                <div class="qd-tile-main">
                  <span class="qd-tile-num">{{ order.dailySequenceNumber || order.id }}</span>
                  <span v-if="order.orderCode" class="qd-tile-invoice">{{ order.orderCode }}</span>
                </div>
                <span class="qd-tile-status">{{ getStatusText(order.orderStatus) }}</span>
              </div>
            </div>
            <div v-else class="qd-empty">
              <b-icon icon="emoji-smile"></b-icon>
              <p>{{ $t('noPendingOrders') || 'لا توجد طلبات حالياً' }}</p>
            </div>
          </div>
        </section>

        <!-- Ready / Completed -->
        <section class="qd-panel qd-panel--done">
          <div class="qd-panel-head">
            <div class="qd-panel-title-wrap">
              <b-icon icon="bag-check-fill" class="qd-panel-icon"></b-icon>
              <div>
                <h2 class="qd-panel-title">{{ $t('completed') || 'مكتمل' }}</h2>
                <p class="qd-panel-hint">{{ $t('queueCompletedHint') || 'تم تسليم الطلب' }}</p>
              </div>
            </div>
            <span class="qd-panel-count">{{ completedOrders.length }}</span>
          </div>

          <div class="qd-grid-wrap">
            <div v-if="completedOrders.length" class="qd-grid">
              <div
                v-for="order in sortedDoneOrders"
                :key="order.id"
                class="qd-tile qd-tile--done"
              >
                <div class="qd-tile-main">
                  <span class="qd-tile-num">{{ order.dailySequenceNumber || order.id }}</span>
                  <span v-if="order.orderCode" class="qd-tile-invoice">{{ order.orderCode }}</span>
                </div>
                <span class="qd-tile-status">{{ $t('completed') || 'مكتمل' }}</span>
              </div>
            </div>
            <div v-else class="qd-empty">
              <b-icon icon="inbox"></b-icon>
              <p>{{ $t('noCompletedOrders') || 'لا توجد طلبات جاهزة' }}</p>
            </div>
          </div>
        </section>
      </div>

      <footer class="qd-footer">
        <span>{{ $t('queueFooterHint') || 'رقم الطلب يظهر على فاتورتك' }}</span>
      </footer>
    </template>
  </div>
</template>

<script>
import { HTTP } from '../http/api.js';
import signalRService from '../services/signalr.js';
import { BUSINESS_TIME_ZONE } from '../utils/formatBusinessDateTime.js';
import {
  filterQueueActive,
  filterQueueCompleted,
  buildQueueDisplayQueryParams,
} from '../utils/queueOrders.js';

export default {
  name: 'PublicQueueDisplayView',
  data() {
    return {
      Orders: [],
      commercialUserId: null,
      refreshInterval: null,
      clockInterval: null,
      loadError: null,
      restaurantName: '',
      restaurantLogo: null,
      logoError: false,
      currentTime: '',
    };
  },
  computed: {
    activeOrders() {
      return filterQueueActive(this.Orders);
    },
    completedOrders() {
      return filterQueueCompleted(this.Orders);
    },
    sortedActiveOrders() {
      return this.sortBySequence(this.activeOrders);
    },
    sortedDoneOrders() {
      return this.sortBySequence(this.completedOrders);
    },
  },
  mounted() {
    this.commercialUserId = parseInt(this.$route.params.commercialUserId, 10);

    if (!this.commercialUserId) {
      this.loadError = this.$t('restaurantNotFound') || 'المطعم غير موجود';
      return;
    }

    this.updateClock();
    this.clockInterval = setInterval(this.updateClock, 1000);

    this.loadRestaurantInfo();
    this.loadOrders();
    this.initializeSignalR();

    this.refreshInterval = setInterval(() => this.loadOrders(), 5000);

    document.documentElement.classList.add('queue-display-page');
  },
  beforeDestroy() {
    this.cleanupSignalR();
    if (this.refreshInterval) clearInterval(this.refreshInterval);
    if (this.clockInterval) clearInterval(this.clockInterval);
    document.documentElement.classList.remove('queue-display-page');
  },
  methods: {
    sortBySequence(orders) {
      return [...orders].sort(
        (a, b) => (b.dailySequenceNumber || 0) - (a.dailySequenceNumber || 0)
      );
    },
    tileClass(order) {
      const s = order.orderStatus || 'Pending';
      return {
        'qd-tile--pending': s === 'Pending',
        'qd-tile--processing': s === 'Processing',
        'qd-tile--ready': s === 'Ready',
      };
    },
    updateClock() {
      this.currentTime = new Intl.DateTimeFormat('ar-IQ', {
        timeZone: BUSINESS_TIME_ZONE,
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: true,
      }).format(new Date());
    },
    async loadRestaurantInfo() {
      try {
        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}`);
        if (response.data?.data) {
          this.restaurantName = response.data.data.restaurantName || '';
          this.restaurantLogo = response.data.data.logo || null;
        }
      } catch {
        /* optional */
      }
    },
    async loadOrders() {
      try {
        const params = buildQueueDisplayQueryParams();
        const response = await HTTP.get(
          `PublicMenu/${this.commercialUserId}/queue-display?${params.toString()}`
        );

        if (response.data && !response.data.errorStatus) {
          this.Orders = response.data.data?.items || response.data.data?.Items || [];
          this.loadError = null;
          return;
        }

        this.Orders = [];
        this.loadError =
          response.data?.message ||
          this.$t('errorLoadingOrders') ||
          'حدث خطأ أثناء جلب الطلبات';
      } catch (error) {
        console.error('Error loading queue display:', error);
        this.Orders = [];
        this.loadError =
          error.response?.status === 404
            ? this.$t('restaurantNotFound') || 'المطعم غير موجود'
            : error.response?.data?.message ||
              this.$t('errorLoadingOrders') ||
              'حدث خطأ أثناء جلب الطلبات';
      }
    },
    getStatusText(status) {
      const texts = {
        Pending: this.$t('pending') || 'انتظار',
        Processing: this.$t('processing') || 'تحضير',
        Ready: this.$t('ready') || 'جاهز',
        Completed: this.$t('completed') || 'مكتمل',
      };
      return texts[status] || status;
    },
    initializeSignalR() {
      signalRService.startConnection().then(() => {
        const refresh = (data) => {
          if (data.CommercialUserId === this.commercialUserId) this.loadOrders();
        };
        signalRService.on('PublicOrderUpdated', refresh);
        signalRService.on('PublicOrderAdded', refresh);
      });
    },
    cleanupSignalR() {
      signalRService.off('PublicOrderUpdated');
      signalRService.off('PublicOrderAdded');
    },
  },
};
</script>

<style scoped>
.qd {
  --qd-bg: #12100e;
  --qd-gold: #d4a574;
  --qd-gold-light: #e8c9a0;
  --qd-gold-dim: rgba(212, 165, 116, 0.14);
  --qd-prep: #c9956a;
  --qd-prep-bg: rgba(201, 149, 106, 0.1);
  --qd-prep-head: #221c16;
  --qd-proc: #b8864a;
  --qd-proc-bg: rgba(184, 134, 74, 0.14);
  --qd-done: #6b9e7a;
  --qd-done-light: #9ec9ab;
  --qd-done-bg: rgba(107, 158, 122, 0.12);
  --qd-done-head: #161c18;
  --qd-text: #fff8f0;
  --qd-muted: rgba(255, 248, 240, 0.58);

  min-height: 100vh;
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--qd-bg);
  color: var(--qd-text);
  font-family: 'Cairo', sans-serif;
  overflow: hidden;
}

.qd h1,
.qd h2,
.qd p,
.qd span {
  -webkit-text-fill-color: unset;
  background: none;
}

/* Header */
.qd-header {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.5rem;
  padding: 1rem 2rem;
  background: linear-gradient(180deg, #1a1612 0%, var(--qd-bg) 100%);
  border-bottom: 2px solid rgba(212, 165, 116, 0.25);
}

.qd-header-brand {
  display: flex;
  align-items: center;
  gap: 1.25rem;
}

.qd-logo {
  width: 64px;
  height: 64px;
  object-fit: contain;
  border-radius: 50%;
  background: #fff;
  padding: 0.35rem;
}

.qd-logo-fallback {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: var(--qd-gold-dim);
  border: 2px solid rgba(212, 165, 116, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  color: var(--qd-gold);
}

.qd-restaurant {
  margin: 0;
  font-size: clamp(1.25rem, 2.5vw, 2rem);
  font-weight: 800;
  line-height: 1.2;
  color: var(--qd-text);
}

.qd-subtitle {
  margin: 0.15rem 0 0;
  font-size: clamp(0.8125rem, 1.2vw, 1rem);
  color: rgba(255, 248, 240, 0.58);
}

.qd-header-meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.35rem;
}

.qd-live {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  font-weight: 700;
  color: var(--qd-done-light);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.qd-live-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: var(--qd-done);
  animation: qd-pulse 1.5s ease-in-out infinite;
}

@keyframes qd-pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.5; transform: scale(0.85); }
}

.qd-clock {
  font-size: clamp(1.125rem, 2vw, 1.75rem);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: var(--qd-gold);
}

/* Board */
.qd-board {
  flex: 1;
  display: grid;
  grid-template-columns: 1fr 1fr;
  min-height: 0;
}

.qd-panel {
  display: flex;
  flex-direction: column;
  min-height: 0;
  border-left: 1px solid rgba(255, 255, 255, 0.06);
}

.qd-panel:first-child {
  border-left: none;
}

.qd-panel-head {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem 1.75rem;
}

.qd-panel--prep .qd-panel-head {
  background: var(--qd-prep-head);
  border-bottom: 3px solid var(--qd-prep);
}

.qd-panel--done .qd-panel-head {
  background: var(--qd-done-head);
  border-bottom: 3px solid var(--qd-done);
}

.qd-panel-title-wrap {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.qd-panel-icon {
  font-size: clamp(1.75rem, 3vw, 2.5rem);
  opacity: 0.9;
}

.qd-panel--prep .qd-panel-icon { color: var(--qd-gold-light); }
.qd-panel--done .qd-panel-icon { color: var(--qd-done-light); }

.qd-panel-title {
  margin: 0;
  font-size: clamp(1.25rem, 2.2vw, 2rem);
  font-weight: 800;
  color: var(--qd-text);
}

.qd-panel-hint {
  margin: 0.15rem 0 0;
  font-size: clamp(0.75rem, 1vw, 0.9375rem);
  color: rgba(255, 248, 240, 0.55);
}

.qd-panel-count {
  min-width: 3rem;
  height: 3rem;
  padding: 0 1rem;
  border-radius: 999px;
  background: rgba(255, 248, 240, 0.08);
  border: 2px solid rgba(255, 248, 240, 0.12);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: clamp(1.25rem, 2vw, 1.75rem);
  font-weight: 800;
  color: var(--qd-text);
}

.qd-panel--prep .qd-panel-count {
  border-color: rgba(201, 149, 106, 0.45);
  color: var(--qd-gold-light);
}

.qd-panel--done .qd-panel-count {
  border-color: rgba(107, 158, 122, 0.45);
  color: var(--qd-done-light);
}

.qd-panel--prep .qd-grid-wrap {
  background: #161310;
}

.qd-panel--done .qd-grid-wrap {
  background: #101412;
}

/* Grid */
.qd-grid-wrap {
  flex: 1;
  overflow-y: auto;
  padding: 1.25rem 1.75rem 1.5rem;
}

.qd-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 140px), 1fr));
  gap: clamp(0.75rem, 1.5vw, 1.25rem);
  align-content: start;
}

.qd-tile {
  aspect-ratio: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  padding: 0.5rem;
  border-radius: 16px;
  border: 2px solid rgba(255, 248, 240, 0.1);
  background: rgba(255, 248, 240, 0.04);
  transition: transform 0.2s, box-shadow 0.2s;
}

.qd-tile-main {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.2rem;
  min-width: 0;
  max-width: 100%;
}

.qd-tile-num {
  font-size: clamp(2.5rem, 6vw, 4.5rem);
  font-weight: 900;
  line-height: 1;
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.02em;
  color: var(--qd-text);
}

.qd-tile-invoice {
  font-size: clamp(0.625rem, 1vw, 0.8125rem);
  font-weight: 700;
  line-height: 1.2;
  color: rgba(255, 248, 240, 0.55);
  letter-spacing: 0.02em;
  text-align: center;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  padding: 0 0.25rem;
}

.qd-tile--pending {
  border-color: rgba(201, 149, 106, 0.55);
  background: var(--qd-prep-bg);
}

.qd-tile--processing {
  border-color: rgba(184, 134, 74, 0.65);
  background: var(--qd-proc-bg);
  animation: qd-glow-proc 2.5s ease-in-out infinite;
}

.qd-tile--done {
  border-color: rgba(107, 158, 122, 0.55);
  background: var(--qd-done-bg);
}

.qd-tile--ready {
  border-color: rgba(107, 158, 122, 0.75);
  background: rgba(107, 158, 122, 0.18);
  animation: qd-glow-ready 2s ease-in-out infinite;
  box-shadow: 0 0 20px rgba(107, 158, 122, 0.25);
}

.qd-tile--ready .qd-tile-num { color: var(--qd-done-light); }

@keyframes qd-glow-proc {
  0%, 100% { box-shadow: 0 0 0 rgba(184, 134, 74, 0); }
  50% { box-shadow: 0 0 18px rgba(184, 134, 74, 0.3); }
}

@keyframes qd-glow-ready {
  0%, 100% { transform: scale(1); box-shadow: 0 0 12px rgba(107, 158, 122, 0.2); }
  50% { transform: scale(1.02); box-shadow: 0 0 22px rgba(107, 158, 122, 0.35); }
}

.qd-tile--pending .qd-tile-num { color: var(--qd-gold-light); }
.qd-tile--processing .qd-tile-num { color: var(--qd-gold); }
.qd-tile--done .qd-tile-num { color: var(--qd-done-light); }

.qd-tile-status {
  font-size: clamp(0.6875rem, 1.2vw, 0.875rem);
  font-weight: 700;
  color: rgba(255, 248, 240, 0.5);
  letter-spacing: 0.03em;
}

/* Empty */
.qd-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 200px;
  color: rgba(255, 248, 240, 0.62);
  text-align: center;
  gap: 0.75rem;
}

.qd-empty .b-icon {
  font-size: 3rem;
  color: var(--qd-gold);
  opacity: 0.4;
}

.qd-empty p {
  margin: 0;
  font-size: clamp(1rem, 1.8vw, 1.375rem);
  font-weight: 600;
  color: rgba(255, 248, 240, 0.62);
  -webkit-text-fill-color: rgba(255, 248, 240, 0.62);
}

/* Footer */
.qd-footer {
  flex-shrink: 0;
  text-align: center;
  padding: 0.625rem 1rem;
  font-size: 0.8125rem;
  font-weight: 500;
  color: rgba(255, 248, 240, 0.45);
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  background: #0e0c0a;
}

/* Error */
.qd-error {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  gap: 1rem;
  color: #f87171;
  padding: 2rem;
  text-align: center;
}

.qd-error .b-icon {
  font-size: 4rem;
}

.qd-error p {
  margin: 0;
  font-size: 1.5rem;
  max-width: 28rem;
}

.qd-grid-wrap::-webkit-scrollbar {
  width: 8px;
}

.qd-grid-wrap::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.15);
  border-radius: 4px;
}

/* TV / large screen */
@media (min-width: 1400px) {
  .qd-grid {
    grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  }
}

@media (max-width: 768px) {
  .qd-board {
    grid-template-columns: 1fr;
    grid-template-rows: 1fr 1fr;
  }

  .qd-header {
    flex-direction: column;
    align-items: flex-start;
    padding: 1rem;
  }

  .qd-header-meta {
    flex-direction: row;
    align-items: center;
    width: 100%;
    justify-content: space-between;
  }
}
</style>

<style>
html.queue-display-page,
html.queue-display-page body {
  margin: 0;
  padding: 0;
  overflow: hidden;
  background: #12100e !important;
  color: #fff8f0 !important;
}

html.queue-display-page #app {
  background: #12100e;
  min-height: 100vh;
}

html.queue-display-page .qd h1,
html.queue-display-page .qd h2,
html.queue-display-page .qd h3,
html.queue-display-page .qd p,
html.queue-display-page .qd span {
  background: none !important;
  -webkit-background-clip: unset !important;
  background-clip: unset !important;
}

html.queue-display-page .qd-empty p,
html.queue-display-page .qd-footer,
html.queue-display-page .qd-subtitle,
html.queue-display-page .qd-panel-hint,
html.queue-display-page .qd-tile-status {
  -webkit-text-fill-color: unset !important;
}

html.queue-display-page .qd-empty p {
  color: rgba(255, 248, 240, 0.62) !important;
}

html.queue-display-page .qd-footer {
  color: rgba(255, 248, 240, 0.45) !important;
}

html.queue-display-page .qd-subtitle,
html.queue-display-page .qd-panel-hint {
  color: rgba(255, 248, 240, 0.55) !important;
}
</style>
