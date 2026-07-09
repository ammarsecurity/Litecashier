<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content stock-alerts-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="bell-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("stockAlertsTitle") || "تنبيهات المخزون" }}</h1>
                  <p class="header-subtitle">{{ $t("stockAlertsDescription") || "المنتجات التي وصلت لحد التنبيه أو أقل" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <router-link to="/items" class="btn-refresh stock-alerts-link-btn">
                  <b-icon icon="inbox-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("Items") || "المواد" }}</span>
                </router-link>
                <button type="button" class="btn-refresh" @click="loadAlerts" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="bell-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ alerts.length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("stockAlertsOverviewTotal") || "إجمالي التنبيهات" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                <b-icon icon="x-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ outOfStockCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("stockAlertsOverviewOut") || "نفدت" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="exclamation-triangle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ lowStockCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("stockAlertsOverviewLow") || "كمية قليلة" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <h2 class="app-section-title">{{ $t("stockAlertsListTitle") || "قائمة التنبيهات" }}</h2>
                <p class="app-section-subtitle">{{ $t("stockAlertsListHint") || "منتجات مفعّل لها تنبيه كمية ووصلت للحد المحدد" }}</p>
              </div>
              <div class="app-section-toolbar">
                <div class="users-search-wrapper">
                  <b-icon icon="search" class="search-icon"></b-icon>
                  <input
                    v-model="search"
                    type="text"
                    class="users-search-input"
                    :placeholder="$t('searchPlaceholder') || 'بحث...'"
                  />
                </div>
              </div>
            </div>

            <div class="app-section-body">
              <div v-if="loading" class="stock-alerts-loading">
                <b-spinner></b-spinner>
              </div>
              <div v-else-if="filteredAlerts.length === 0" class="stock-alerts-empty">
                <b-icon icon="check-circle" class="stock-alerts-empty-icon"></b-icon>
                <p>{{ $t("noStockAlerts") || "لا توجد تنبيهات حالياً" }}</p>
              </div>
              <div v-else class="table-responsive">
                <table class="table stock-alerts-table">
                  <thead>
                    <tr>
                      <th>{{ $t("itemNamePlaceholder") || "اسم المنتج" }}</th>
                      <th>{{ $t("codePlaceholder") || "الباركود" }}</th>
                      <th>{{ $t("categoryPlaceholder") || "القسم" }}</th>
                      <th>{{ $t("quantityLabel") || "الكمية" }}</th>
                      <th>{{ $t("stockAlertThresholdLabel") || "حد التنبيه" }}</th>
                      <th>{{ $t("status") || "الحالة" }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="item in filteredAlerts"
                      :key="item.itemId"
                      :class="{ 'stock-alerts-row--out': item.status === 'out' }"
                    >
                      <td>{{ item.itemName }}</td>
                      <td>{{ item.itemCode || "—" }}</td>
                      <td>{{ item.category || "—" }}</td>
                      <td class="stock-alerts-qty">{{ item.currentQuantity }}</td>
                      <td>{{ item.alertThreshold }}</td>
                      <td>
                        <span
                          class="stock-alerts-badge"
                          :class="item.status === 'out' ? 'stock-alerts-badge--out' : 'stock-alerts-badge--low'"
                        >
                          {{ statusLabel(item.status) }}
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
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";

export default {
  name: "StockAlertsView",
  components: { AppHeader },
  data() {
    return {
      loading: false,
      search: "",
      alerts: [],
    };
  },
  computed: {
    filteredAlerts() {
      const q = (this.search || "").trim().toLowerCase();
      if (!q) return this.alerts;
      return this.alerts.filter((item) => {
        const name = String(item.itemName || "").toLowerCase();
        const code = String(item.itemCode || "").toLowerCase();
        const category = String(item.category || "").toLowerCase();
        return name.includes(q) || code.includes(q) || category.includes(q);
      });
    },
    outOfStockCount() {
      return this.alerts.filter((item) => item.status === "out").length;
    },
    lowStockCount() {
      return this.alerts.filter((item) => item.status === "low").length;
    },
  },
  mounted() {
    this.loadAlerts();
  },
  methods: {
    loadAlerts() {
      this.loading = true;
      HTTP.get("Admin/GetStockAlerts")
        .then((response) => {
          this.alerts = response.data.data || [];
        })
        .catch(() => {
          this.$notify.error(this.$t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        })
        .finally(() => {
          this.loading = false;
        });
    },
    statusLabel(status) {
      if (status === "out") return this.$t("stockAlertStatusOut") || "نفد";
      return this.$t("stockAlertStatusLow") || "قليل";
    },
  },
};
</script>

<style scoped>
.stock-alerts-link-btn {
  text-decoration: none;
  margin-inline-end: 0.5rem;
}

.stock-alerts-loading,
.stock-alerts-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  color: var(--text-muted, #6b7280);
}

.stock-alerts-empty-icon {
  font-size: 2.5rem;
  margin-bottom: 0.75rem;
  color: #16a34a;
}

.stock-alerts-table th,
.stock-alerts-table td {
  vertical-align: middle;
}

.stock-alerts-qty {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.stock-alerts-row--out {
  background: rgba(220, 38, 38, 0.06);
}

.stock-alerts-badge {
  display: inline-block;
  padding: 0.2rem 0.65rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 600;
}

.stock-alerts-badge--low {
  background: rgba(217, 119, 6, 0.15);
  color: #b45309;
}

.stock-alerts-badge--out {
  background: rgba(220, 38, 38, 0.15);
  color: #dc2626;
}

.spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
