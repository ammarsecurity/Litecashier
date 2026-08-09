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
                  <template v-else>{{ filteredAlerts.length }}</template>
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
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="bell-fill"></b-icon>
                </div>
                <div>
                  <h2 class="app-section-title">{{ $t("stockAlertsListTitle") || "قائمة التنبيهات" }}</h2>
                  <p class="app-section-subtitle">{{ $t("stockAlertsListHint") || "منتجات مفعّل لها تنبيه كمية ووصلت للحد المحدد" }}</p>
                </div>
              </div>
            </div>

            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                    <p>{{ $t("stockAlertsFiltersHint") || "تصفية تنبيهات المخزون حسب القسم والحالة" }}</p>
                  </div>
                </div>
                <div class="app-filters-panel-actions" v-if="hasActiveFilters">
                  <button type="button" class="users-filter-clear-btn app-filters-clear-btn" @click="clearFilters">
                    <b-icon icon="x-circle" class="me-1"></b-icon>
                    {{ $t("clearFilters") || "مسح الفلاتر" }}
                  </button>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--3">
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("categoryPlaceholder") || "القسم" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="tags" class="search-icon"></b-icon>
                    <select v-model="selectedCategory" class="users-search-input reports-filter-select">
                      <option value="">{{ $t("all_categories") || "جميع الاقسام" }}</option>
                      <option v-for="tag in tags" :key="tag.id || tag.name" :value="tag.name">
                        {{ tag.name }}
                      </option>
                    </select>
                  </div>
                </label>
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("status") || "الحالة" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="funnel" class="search-icon"></b-icon>
                    <select v-model="selectedStatus" class="users-search-input reports-filter-select">
                      <option value="">{{ $t("allStockStatuses") || "كل الحالات" }}</option>
                      <option value="out">{{ $t("stockAlertStatusOut") || "نفد" }}</option>
                      <option value="low">{{ $t("stockAlertStatusLow") || "قليل" }}</option>
                    </select>
                  </div>
                </label>
                <label class="app-filter-field app-filter-field--grow">
                  <span class="app-filter-label">{{ $t("search") || "بحث" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="search" class="search-icon"></b-icon>
                    <input
                      v-model="search"
                      type="search"
                      class="users-search-input"
                      :placeholder="$t('searchPlaceholder') || 'بحث...'"
                      autocomplete="off"
                    />
                  </div>
                </label>
              </div>
            </div>

            <div class="app-section-body app-section-body--no-padding">
              <div v-if="loading" class="stock-alerts-loading">
                <b-spinner></b-spinner>
              </div>
              <div v-else-if="filteredAlerts.length === 0" class="stock-alerts-empty">
                <b-icon
                  :icon="hasActiveFilters ? 'search' : 'check-circle'"
                  class="stock-alerts-empty-icon"
                  :class="{ 'stock-alerts-empty-icon--muted': hasActiveFilters }"
                ></b-icon>
                <p class="stock-alerts-empty-title">
                  {{
                    hasActiveFilters
                      ? ($t("noFilterResults") || "لا توجد نتائج مطابقة للفلاتر")
                      : ($t("noStockAlerts") || "لا توجد تنبيهات حالياً")
                  }}
                </p>
                <p v-if="hasActiveFilters" class="stock-alerts-empty-hint">
                  {{ $t("tryClearFilters") || "جرّب مسح الفلاتر أو تغيير القسم" }}
                </p>
                <button
                  v-if="hasActiveFilters"
                  type="button"
                  class="btn-refresh stock-alerts-empty-clear"
                  @click="clearFilters"
                >
                  <b-icon icon="x-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("clearFilters") || "مسح الفلاتر" }}</span>
                </button>
              </div>
              <div v-else class="table-responsive stock-alerts-table-wrap">
                <table class="table stock-alerts-table reports-table">
                  <thead>
                    <tr>
                      <th>{{ $t("itemNamePlaceholder") || "اسم المنتج" }}</th>
                      <th>{{ $t("codePlaceholder") || "الباركود" }}</th>
                      <th>{{ $t("warehouseName") || "المخزن" }}</th>
                      <th>{{ $t("categoryPlaceholder") || "القسم" }}</th>
                      <th>{{ $t("quantityLabel") || "الكمية" }}</th>
                      <th>{{ $t("stockAlertThresholdLabel") || "حد التنبيه" }}</th>
                      <th>{{ $t("status") || "الحالة" }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="item in filteredAlerts"
                      :key="(item.itemId || '') + '-' + (item.warehouseId || 'x')"
                      :class="{ 'stock-alerts-row--out': item.status === 'out' }"
                    >
                      <td class="stock-alerts-name">{{ item.itemName }}</td>
                      <td class="stock-alerts-code">{{ item.itemCode || "—" }}</td>
                      <td>{{ item.warehouseName || "—" }}</td>
                      <td>
                        <span class="stock-alerts-category">{{ item.category || "—" }}</span>
                      </td>
                      <td class="stock-alerts-qty">{{ item.currentQuantity }}</td>
                      <td class="stock-alerts-threshold">{{ item.alertThreshold }}</td>
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
      selectedCategory: "",
      selectedStatus: "",
      tags: [],
      alerts: [],
    };
  },
  computed: {
    hasActiveFilters() {
      return !!(
        this.selectedCategory ||
        this.selectedStatus ||
        (this.search || "").trim()
      );
    },
    filteredAlerts() {
      const q = (this.search || "").trim().toLowerCase();
      const category = (this.selectedCategory || "").trim();
      const status = (this.selectedStatus || "").trim();

      return this.alerts.filter((item) => {
        if (category && String(item.category || "").trim() !== category) {
          return false;
        }
        if (status && item.status !== status) {
          return false;
        }
        if (!q) return true;
        const name = String(item.itemName || "").toLowerCase();
        const code = String(item.itemCode || "").toLowerCase();
        const itemCategory = String(item.category || "").toLowerCase();
        return name.includes(q) || code.includes(q) || itemCategory.includes(q);
      });
    },
    outOfStockCount() {
      return this.filteredAlerts.filter((item) => item.status === "out").length;
    },
    lowStockCount() {
      return this.filteredAlerts.filter((item) => item.status === "low").length;
    },
  },
  mounted() {
    this.loadTags();
    this.loadAlerts();
  },
  methods: {
    loadTags() {
      HTTP.get("Admin/GetTags?pageNumber=0&pageSize=10000")
        .then((response) => {
          this.tags = response.data?.data?.items || [];
        })
        .catch(() => {
          this.tags = [];
        });
    },
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
    clearFilters() {
      this.selectedCategory = "";
      this.selectedStatus = "";
      this.search = "";
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

.stock-alerts-filters {
  padding: 0 1.25rem 1rem;
  border-bottom: 1px solid var(--border-color, #e5e7eb);
}

.stock-alerts-filters-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.75rem;
  align-items: stretch;
  margin-top: 1rem;
}

.stock-alerts-clear-wrap .users-filter-clear-btn {
  width: 100%;
}

.stock-alerts-loading,
.stock-alerts-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3.5rem 1.25rem;
  text-align: center;
  color: var(--text-muted, #6b7280);
}

.stock-alerts-empty-icon {
  font-size: 2.75rem;
  margin-bottom: 0.85rem;
  color: #16a34a;
}

.stock-alerts-empty-icon--muted {
  color: var(--text-muted, #9ca3af);
}

.stock-alerts-empty-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--text-primary, #111827);
}

.stock-alerts-empty-hint {
  margin: 0.4rem 0 0;
  font-size: 0.9rem;
  color: var(--text-muted, #6b7280);
}

.stock-alerts-empty-clear {
  margin-top: 1rem;
  text-decoration: none;
}

.stock-alerts-table-wrap {
  padding: 0;
}

.stock-alerts-table {
  margin: 0;
}

.stock-alerts-table th,
.stock-alerts-table td {
  vertical-align: middle;
  padding: 0.85rem 1rem;
}

.stock-alerts-name {
  font-weight: 600;
  color: var(--text-primary, #111827);
}

.stock-alerts-code {
  font-variant-numeric: tabular-nums;
  color: var(--text-secondary, #4b5563);
}

.stock-alerts-category {
  display: inline-block;
  padding: 0.15rem 0.55rem;
  border-radius: 0.4rem;
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
  color: var(--text-primary, #111827);
  font-size: 0.85rem;
  font-weight: 500;
}

.stock-alerts-qty {
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.stock-alerts-threshold {
  font-variant-numeric: tabular-nums;
  color: var(--text-secondary, #4b5563);
}

.stock-alerts-row--out {
  background: rgba(220, 38, 38, 0.05);
}

.stock-alerts-badge {
  display: inline-block;
  padding: 0.25rem 0.7rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 600;
  white-space: nowrap;
}

.stock-alerts-badge--low {
  background: rgba(217, 119, 6, 0.14);
  color: #b45309;
}

.stock-alerts-badge--out {
  background: rgba(220, 38, 38, 0.14);
  color: #dc2626;
}

.spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

@media (max-width: 992px) {
  .stock-alerts-filters-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 576px) {
  .stock-alerts-filters-grid {
    grid-template-columns: 1fr;
  }
}
</style>
