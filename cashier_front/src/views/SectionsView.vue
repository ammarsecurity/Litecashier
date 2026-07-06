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

          <div class="app-section-card" v-if="flatHubItems.length">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="grid-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("systemModules") }}</h3>
                  <p class="app-section-subtitle">{{ flatHubItems.length }} {{ $t("sectionsAvailable") || "قسم متاح" }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <div class="hub-cards-grid">
                <router-link
                  v-for="item in flatHubItems"
                  :key="item.name"
                  :to="item.link"
                  class="hub-module-card"
                >
                  <div class="hub-module-icon-wrap">
                    <b-icon :icon="item.icon" class="hub-module-icon"></b-icon>
                  </div>
                  <span class="hub-module-label">{{ item.label }}</span>
                </router-link>
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
import { flatNavItemsForHub } from "@/navigation/navItems.js";
import { getAllowedSections } from "@/navigation/sectionRegistry.js";

export default {
  name: "SectionsView",
  components: { AppHeader },
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
      if (this.role === "Manager") {
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
};
</script>

<style scoped>
.hub-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(132px, 1fr));
  gap: 1rem;
}

.hub-module-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.65rem;
  padding: 1.15rem 0.75rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
  text-decoration: none;
  color: var(--text-primary);
  transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;
  min-height: 112px;
  text-align: center;
}

.hub-module-card:hover {
  transform: translateY(-3px);
  border-color: var(--primary-color);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
}

.hub-module-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: color-mix(in srgb, var(--primary-color) 10%, var(--bg-primary));
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border-color);
}

.hub-module-icon {
  font-size: 1.35rem;
  color: var(--primary-color);
}

.hub-module-label {
  font-size: 0.8125rem;
  font-weight: 600;
  line-height: 1.35;
}
</style>
