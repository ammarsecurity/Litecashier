<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="sections-page-container">
        <div class="sections-page-content">
          <header class="sections-page-header">
            <p class="sections-page-eyebrow">{{ $t("app-name") }}</p>
            <h1 class="sections-page-title">{{ $t("systemModules") }}</h1>
            <p class="sections-page-subtitle">
              {{ $t("sectionsPageSubtitle") || "اختر القسم للانتقال السريع" }}
            </p>
          </header>

          <AnnouncementsSlider :items="announcements" />

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
                  <span
                    v-if="sectionBadgeCount(item)"
                    class="hub-module-badge"
                    :title="$t('pending') || 'قيد الانتظار'"
                  >
                    {{ sectionBadgeCount(item) }}
                  </span>
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
import AnnouncementsSlider from "@/components/AnnouncementsSlider.vue";
import sectionsHubMixin from "@/mixins/sectionsHubMixin.js";
import { HTTP } from "@/http/api.js";
import { openDevicePausedGate } from "@/utils/devicePausedGateBus.js";

export default {
  name: "SectionsView",
  components: { AppHeader, AnnouncementsSlider },
  mixins: [sectionsHubMixin],
  data() {
    return {
      announcements: [],
    };
  },
  mounted() {
    this.loadAnnouncements();
    window.addEventListener("online", this.loadAnnouncements);
  },
  beforeDestroy() {
    window.removeEventListener("online", this.loadAnnouncements);
  },
  methods: {
    async loadAnnouncements() {
      try {
        await HTTP.post("License/device-sync");
      } catch {
        /* offline */
      }
      try {
        const { data } = await HTTP.get("License/device-status");
        this.announcements = Array.isArray(data?.announcements) ? data.announcements : [];
        if (data?.isPaused) {
          openDevicePausedGate({ deviceStatus: data, pauseReason: data.pauseReason });
        }
      } catch {
        this.announcements = [];
      }
    },
  },
};
</script>

<style scoped>
.sections-page-container {
  padding: 24px 16px 40px;
  max-width: 1200px;
  margin: 0 auto;
}

.sections-page-header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--border-light, var(--border-color));
}

.sections-page-eyebrow {
  margin: 0 0 8px;
  font-size: 13px;
  font-weight: 700;
  color: var(--primary-color);
}

.sections-page-title {
  font-size: 28px;
  font-weight: 800;
  letter-spacing: -0.03em;
  color: var(--text-primary);
  margin: 0 0 8px;
}

.sections-page-subtitle {
  font-size: 15px;
  font-weight: 500;
  color: var(--text-secondary);
  margin: 0;
}

.hub-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(132px, 1fr));
  gap: 16px;
}

.hub-module-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 16px 12px;
  background: var(--bg-primary);
  border: none;
  border-radius: 16px;
  text-decoration: none;
  color: var(--text-primary);
  min-height: 112px;
  text-align: center;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
}

.hub-module-card:hover {
  transform: none;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.12);
}

.hub-module-icon-wrap {
  position: relative;
  width: 48px;
  height: 48px;
  border-radius: 14px;
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
}

.hub-module-icon {
  font-size: 1.35rem;
  color: var(--primary-color);
}

.hub-module-badge {
  position: absolute;
  top: -6px;
  inset-inline-end: -6px;
  min-width: 1.35rem;
  height: 1.35rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  background: #f59e0b;
  color: #fff;
  border: 2px solid var(--bg-tertiary);
  font-size: 0.6875rem;
  font-weight: 800;
  line-height: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.45);
}

.hub-module-label {
  font-size: 0.8125rem;
  font-weight: 600;
  line-height: 1.35;
}
</style>
