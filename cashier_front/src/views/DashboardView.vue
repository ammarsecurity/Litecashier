<template>
  <div>
    <SidebarView />
    <div class="main-content-wrapper">
      <b-overlay
        :show="show"
        spinner-variant="primary"
        spinner-type="border"
        rounded="sm"
      >
        <div class="dashboard-page-container">
          <div class="dashboard-page-content">
            <!-- Welcome Header -->
            <div class="dashboard-welcome-section">
              <h1 class="dashboard-welcome-title">{{ $t("welcomeToDashboard") || "مرحباً بك في لوحة التحكم" }}</h1>
              <p class="dashboard-welcome-subtitle">{{ $t("dashboardSubtitle") || "نظرة شاملة على إحصائيات متجرك" }}</p>
            </div>

            <!-- Quick Stats Overview -->
            <div class="dashboard-quick-stats">
              <div class="quick-stat-card quick-stat-primary">
                <div class="quick-stat-icon">
                  <b-icon icon="receipt-cutoff"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ stats.orders?.total || 0 }}</h3>
                  <p class="quick-stat-label">{{ $t("all_sales") }}</p>
                </div>
              </div>
              <div class="quick-stat-card quick-stat-success">
                <div class="quick-stat-icon">
                  <b-icon icon="currency-dollar"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ formattedNumber(stats.salesAmount?.total || 0) }} {{ $t("currency") }}</h3>
                  <p class="quick-stat-label">{{ $t("totalLabel") }} {{ $t("salesAmountStatisticsLabel") }}</p>
                </div>
              </div>
              <div class="quick-stat-card quick-stat-info">
                <div class="quick-stat-icon">
                  <b-icon icon="box-fill"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ stats.products?.total || 0 }}</h3>
                  <p class="quick-stat-label">{{ $t("Items") }}</p>
                </div>
              </div>
              <div class="quick-stat-card quick-stat-warning">
                <div class="quick-stat-icon">
                  <b-icon icon="people"></b-icon>
                </div>
                <div class="quick-stat-content">
                  <h3 class="quick-stat-value">{{ stats.users?.total || 0 }}</h3>
                  <p class="quick-stat-label">{{ $t("all_accounts") }}</p>
                </div>
              </div>
            </div>

            <!-- Invoice Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="receipt-cutoff" class="section-title-icon"></b-icon>
                  {{ $t("invoiceStatisticsLabel") }}
                </h2>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="primary"
                  :value="stats.orders?.total || 0"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="receipt-cutoff" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="stats.orders?.today || 0"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.orders?.thisWeek || 0"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="stats.orders?.thisMonth || 0"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Items Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="box-fill" class="section-title-icon"></b-icon>
                  {{ $t("itemsStatisticsLabel") }}
                </h2>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="primary"
                  :value="stats.items?.total || 0"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="box-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="stats.items?.today || 0"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.items?.thisWeek || 0"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="stats.items?.thisMonth || 0"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Sales Amount Statistics Section -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="currency-dollar" class="section-title-icon"></b-icon>
                  {{ $t("salesAmountStatisticsLabel") }}
                </h2>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="primary"
                  :value="formattedNumber(stats.salesAmount?.total || 0) + ' ' + $t('currency')"
                  :label="$t('totalLabel')"
                >
                  <template #icon>
                    <b-icon icon="currency-dollar" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="danger"
                  :value="formattedNumber(stats.salesAmount?.today || 0) + ' ' + $t('currency')"
                  :label="$t('todayLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-day" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="formattedNumber(stats.salesAmount?.thisWeek || 0) + ' ' + $t('currency')"
                  :label="$t('thisWeekLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-week" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="info"
                  :value="formattedNumber(stats.salesAmount?.thisMonth || 0) + ' ' + $t('currency')"
                  :label="$t('thisMonthLabel')"
                >
                  <template #icon>
                    <b-icon icon="calendar-month" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>

            <!-- Additional Statistics -->
            <section class="dashboard-section">
              <div class="section-header">
                <h2 class="section-title">
                  <b-icon icon="graph-up" class="section-title-icon"></b-icon>
                  {{ $t("additionalStats") || "إحصائيات إضافية" }}
                </h2>
              </div>
              <div class="stats-grid">
                <StatCard
                  color="info"
                  :value="stats.products?.total || 0"
                  :label="$t('Items') + ' (' + $t('totalLabel') + ')'"
                >
                  <template #icon>
                    <b-icon icon="box-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="warning"
                  :value="stats.users?.total || 0"
                  :label="$t('all_accounts')"
                >
                  <template #icon>
                    <b-icon icon="people-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
                <StatCard
                  color="success"
                  :value="stats.categories?.total || 0"
                  :label="$t('all_categories')"
                >
                  <template #icon>
                    <b-icon icon="tags-fill" class="stat-icon-large"></b-icon>
                  </template>
                </StatCard>
              </div>
            </section>
          </div>
        </div>
      </b-overlay>
    </div>
  </div>
</template>

<script>
import SidebarView from "@/components/Layout/SidebarView.vue";
import { HTTP } from "../http/api.js";
import StatCard from "@/components/StatCard.vue";

export default {
  name: "DashboardView",
  components: {
    SidebarView,
    StatCard,
  },
  data() {
    return {
      stats: {
        orders: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        items: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        salesAmount: {
          total: 0,
          today: 0,
          thisWeek: 0,
          thisMonth: 0,
        },
        products: {
          total: 0,
          active: 0,
        },
        users: {
          total: 0,
          active: 0,
        },
        categories: {
          total: 0,
          active: 0,
        },
      },
      show: false,
    };
  },
  computed: {
    role() {
      return localStorage.getItem("role");
    },
  },
  mounted() {
    this.getDashboardStats();
  },
  methods: {
    formattedNumber(info) {
      if (typeof info === 'number') {
        return info.toLocaleString("en-EG");
      }
      return info || "0";
    },
    getDashboardStats() {
      this.show = true;
      HTTP.get(`Admin/GetDashboardStats`)
        .then((response) => {
          if (response.data && response.data.data) {
            this.stats = response.data.data;
          }
          this.show = false;
        })
        .catch((error) => {
          console.error("Error fetching dashboard stats:", error);
          this.show = false;
        });
    },
  },
};
</script>

