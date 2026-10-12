<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content sections-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="grid-3x3-gap-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("systemModules") }}</h1>
                  <p class="header-subtitle">
                    {{ $t("sectionsPageSubtitle") || "اختر القسم للانتقال السريع" }}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div v-if="flatHubItems.length" class="hub-modules-panel">
            <div class="hub-cards-grid">
              <router-link
                v-for="(item, index) in flatHubItems"
                :key="item.name"
                :to="item.link"
                class="hub-module-card"
                :class="[
                  `hub-module-card--tone-${toneFor(item, index)}`,
                  { 'hub-module-card--active': isActive(item.link) },
                ]"
              >
                <div class="hub-module-text">
                  <span class="hub-module-label">{{ item.label }}</span>
                  <span class="hub-module-desc">{{ sectionDescription(item) }}</span>
                </div>
                <div class="hub-module-icon-wrap">
                  <b-icon :icon="item.icon" class="hub-module-icon"></b-icon>
                  <span
                    v-if="item.name === 'publicOrders' && pendingPublicOrders"
                    class="hub-badge"
                  >
                    {{ pendingPublicOrders }}
                  </span>
                </div>
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { flatNavItemsForHub } from "@/navigation/navItems.js";
import { getAllowedSections } from "@/navigation/sectionRegistry.js";
import { HTTP } from "@/http/api.js";
import { resolveCommercialUserId } from "@/utils/publicMenu.js";
import signalRService from "@/services/signalr.js";

const SECTION_DESC_KEYS = {
  "dashboard-home": "hubDescDashboard",
  pos: "hubDescPos",
  publicOrders: "hubDescPublicOrders",
  category: "hubDescCategory",
  brands: "hubDescBrands",
  items: "hubDescItems",
  shortcutItems: "hubDescShortcutItems",
  priceReader: "hubDescPriceReader",
  users: "hubDescUsers",
  reports: "hubDescReports",
  endOfDayReport: "hubDescEndOfDay",
  expenses: "hubDescExpenses",
  inventory: "hubDescInventory",
  warehouses: "hubDescWarehouses",
  printServer: "hubDescPrintServer",
  paymentDevices: "hubDescPaymentDevices",
  cardPayments: "hubDescCardPayments",
  employees: "hubDescEmployees",
  payroll: "hubDescPayroll",
  customers: "hubDescCustomers",
  deferredPayments: "hubDescDeferred",
  stockAlerts: "hubDescStockAlerts",
  stockReturns: "hubDescStockReturns",
  auditLog: "hubDescAuditLog",
  profile: "hubDescProfile",
  settings: "hubDescSettings",
};

const SECTION_TONES = {
  pos: "teal",
  publicOrders: "orange",
  customers: "blue",
  deferredPayments: "mint",
  items: "amber",
  inventory: "violet",
  stockAlerts: "pink",
  stockReturns: "rose",
  reports: "sky",
  expenses: "emerald",
  employees: "indigo",
  settings: "slate",
  category: "amber",
  brands: "violet",
  warehouses: "indigo",
  users: "blue",
  payroll: "emerald",
  endOfDayReport: "sky",
  cardPayments: "mint",
  paymentDevices: "slate",
  printServer: "slate",
  priceReader: "orange",
  shortcutItems: "amber",
  auditLog: "indigo",
  profile: "blue",
  "dashboard-home": "teal",
};

const TONE_CYCLE = ["teal", "orange", "blue", "mint", "amber", "violet", "pink", "rose", "sky", "emerald", "indigo", "slate"];

export default {
  name: "SectionsView",
  components: { AppHeader },
  data() {
    return {
      pendingPublicOrders: 0,
    };
  },
  computed: {
    role() {
      return localStorage.getItem("role");
    },
    allowedSections() {
      return getAllowedSections();
    },
    flatHubItems() {
      const modules = flatNavItemsForHub(
        this.role,
        (k) => this.$t(k),
        this.allowedSections
      );
      if (this.role === "Manager" || this.role === "Admin") {
        return modules;
      }
      const dashboardEntry = {
        name: "dashboard-home",
        label: this.$t("appHomeLink") || this.$t("home") || "الرئيسية",
        link: "/dashboard",
        icon: "house-door-fill",
      };
      return [dashboardEntry, ...modules];
    },
  },
  mounted() {
    this.loadPendingPublicOrders();
    this.bindRealtime();
  },
  beforeDestroy() {
    this.unbindRealtime();
  },
  methods: {
    isActive(link) {
      const path = (this.$route?.path || "").replace(/\/$/, "") || "/";
      const target = String(link || "").replace(/\/$/, "") || "/";
      return path === target || path.startsWith(`${target}/`);
    },
    toneFor(item, index) {
      return SECTION_TONES[item.name] || TONE_CYCLE[index % TONE_CYCLE.length];
    },
    sectionDescription(item) {
      const key = SECTION_DESC_KEYS[item.name];
      if (key && this.$te(key)) return this.$t(key);
      return this.$t("hubDescDefault") || "الانتقال إلى القسم";
    },
    async loadPendingPublicOrders() {
      const id = resolveCommercialUserId();
      if (!id) return;
      try {
        const res = await HTTP.get(`PublicMenu/${id}/pending-count`);
        this.pendingPublicOrders = Number(res.data?.data?.count || 0);
      } catch (_) {
        this.pendingPublicOrders = 0;
      }
    },
    onPublicOrderRealtime(payload) {
      const id = Number(payload?.commercialUserId ?? payload?.CommercialUserId);
      const mine = resolveCommercialUserId();
      if (id && mine && id !== Number(mine)) return;
      this.loadPendingPublicOrders();
    },
    async bindRealtime() {
      try {
        await signalRService.startConnection();
        signalRService.on("PublicOrderAdded", this.onPublicOrderRealtime);
        signalRService.on("PublicOrderUpdated", this.onPublicOrderRealtime);
      } catch (_) {
        /* ignore */
      }
    },
    unbindRealtime() {
      signalRService.off("PublicOrderAdded", this.onPublicOrderRealtime);
      signalRService.off("PublicOrderUpdated", this.onPublicOrderRealtime);
    },
  },
};
</script>

<style scoped>
.hub-modules-panel {
  padding: 0.25rem 0 0.5rem;
}

.hub-cards-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
}

.hub-module-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  min-height: 88px;
  padding: 16px 18px;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 14px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.06);
  text-decoration: none;
  color: inherit;
  transition:
    transform 0.18s ease,
    box-shadow 0.18s ease,
    border-color 0.18s ease,
    background 0.18s ease;
}

.hub-module-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(15, 23, 42, 0.08);
  border-color: color-mix(in srgb, var(--primary-color) 28%, var(--border-color));
  text-decoration: none;
  color: inherit;
}

.hub-module-card:focus-visible {
  outline: 2px solid color-mix(in srgb, var(--primary-color) 45%, transparent);
  outline-offset: 2px;
}

.hub-module-text {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
  min-width: 0;
  flex: 1;
  text-align: start;
}

.hub-module-label {
  font-size: 15px;
  font-weight: 800;
  line-height: 1.35;
  color: #0f172a;
}

.hub-module-desc {
  font-size: 12px;
  font-weight: 500;
  line-height: 1.45;
  color: #64748b;
}

.hub-module-icon-wrap {
  position: relative;
  width: 48px;
  height: 48px;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
}

.hub-module-icon {
  font-size: 1.2rem;
}

.hub-badge {
  position: absolute;
  top: -4px;
  inset-inline-end: -4px;
  min-width: 1.2rem;
  height: 1.2rem;
  padding: 0 0.28rem;
  border-radius: 999px;
  background: #ef4444;
  color: #fff;
  font-size: 0.68rem;
  font-weight: 800;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 2px solid var(--bg-primary);
}

/* Tone accents */
.hub-module-card--tone-teal .hub-module-icon-wrap {
  background: #d1fae5;
  color: #0f766e;
}
.hub-module-card--tone-orange .hub-module-icon-wrap {
  background: #ffedd5;
  color: #c2410c;
}
.hub-module-card--tone-blue .hub-module-icon-wrap {
  background: #dbeafe;
  color: #1d4ed8;
}
.hub-module-card--tone-mint .hub-module-icon-wrap {
  background: #ccfbf1;
  color: #0d9488;
}
.hub-module-card--tone-amber .hub-module-icon-wrap {
  background: #fef3c7;
  color: #b45309;
}
.hub-module-card--tone-violet .hub-module-icon-wrap {
  background: #ede9fe;
  color: #6d28d9;
}
.hub-module-card--tone-pink .hub-module-icon-wrap {
  background: #fce7f3;
  color: #be185d;
}
.hub-module-card--tone-rose .hub-module-icon-wrap {
  background: #ffe4e6;
  color: #e11d48;
}
.hub-module-card--tone-sky .hub-module-icon-wrap {
  background: #e0f2fe;
  color: #0284c7;
}
.hub-module-card--tone-emerald .hub-module-icon-wrap {
  background: #d1fae5;
  color: #059669;
}
.hub-module-card--tone-indigo .hub-module-icon-wrap {
  background: #e0e7ff;
  color: #4338ca;
}
.hub-module-card--tone-slate .hub-module-icon-wrap {
  background: #e2e8f0;
  color: #334155;
}

/* Active card — teal like the reference */
.hub-module-card--active,
.hub-module-card.router-link-active {
  background: #0f766e;
  border-color: #0f766e;
  box-shadow: 0 8px 20px rgba(15, 118, 110, 0.28);
}

.hub-module-card--active .hub-module-label,
.hub-module-card.router-link-active .hub-module-label,
.hub-module-card--active .hub-module-desc,
.hub-module-card.router-link-active .hub-module-desc {
  color: #fff;
}

.hub-module-card--active .hub-module-icon-wrap,
.hub-module-card.router-link-active .hub-module-icon-wrap {
  background: rgba(255, 255, 255, 0.18);
  color: #fff;
}

.hub-module-card--active .hub-badge,
.hub-module-card.router-link-active .hub-badge {
  border-color: #0f766e;
}

:root.dark-theme .hub-module-card {
  background: var(--bg-primary);
  border-color: var(--border-color);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.25);
}

:root.dark-theme .hub-module-label {
  color: var(--text-primary);
}

:root.dark-theme .hub-module-desc {
  color: var(--text-secondary);
}

:root.dark-theme .hub-module-card--active,
:root.dark-theme .hub-module-card.router-link-active {
  background: #0f766e;
  border-color: #0f766e;
}

@media (max-width: 1100px) {
  .hub-cards-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 800px) {
  .hub-cards-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
  }

  .hub-module-card {
    min-height: 80px;
    padding: 14px;
  }
}

@media (max-width: 520px) {
  .hub-cards-grid {
    grid-template-columns: 1fr;
  }
}
</style>
