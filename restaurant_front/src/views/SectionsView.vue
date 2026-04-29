<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="sections-page-container">
        <div class="sections-page-content">
          <div class="sections-page-header">
            <h1 class="sections-page-title">{{ $t("systemModules") }}</h1>
            <p class="sections-page-subtitle">
              {{ $t("sectionsPageSubtitle") || "اختر القسم للانتقال السريع" }}
            </p>
          </div>

          <section v-if="flatHubItems.length" class="dashboard-modules-hub">
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
          </section>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { flatNavItemsForHub } from "@/navigation/navItems.js";

export default {
  name: "SectionsView",
  components: { AppHeader },
  computed: {
    role() {
      return localStorage.getItem("role");
    },
    flatHubItems() {
      return flatNavItemsForHub(this.role, (k) => this.$t(k));
    },
  },
};
</script>

<style scoped>
.sections-page-container {
  padding: 1.25rem 1rem 2rem;
  max-width: 1200px;
  margin: 0 auto;
}

.sections-page-header {
  margin-bottom: 1.75rem;
}

.sections-page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 0.35rem;
}

.sections-page-subtitle {
  font-size: 0.95rem;
  color: var(--text-secondary);
  margin: 0;
}

.dashboard-modules-hub {
  margin-bottom: 1rem;
}

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
  background: var(--bg-tertiary);
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
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
}

.hub-module-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: var(--bg-primary);
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
