<template>
  <div class="system-sections-fab-shell">
    <div
      class="system-sections-fab-root"
      :class="{
        'system-sections-fab-root--open': modalVisible,
        'system-sections-fab-root--has-badge': publicOrderFabBadgeCount > 0,
      }"
    >
      <button
        type="button"
        class="system-sections-fab"
        :class="{ 'system-sections-fab--open': modalVisible }"
        :title="fabTitle"
        :aria-label="fabAriaLabel"
        :aria-expanded="modalVisible ? 'true' : 'false'"
        @click="openModal"
      >
        <span class="system-sections-fab-glow" aria-hidden="true"></span>
        <b-icon icon="grid-3x3-gap-fill" class="system-sections-fab-icon" />
      </button>
      <span
        v-if="publicOrderFabBadgeCount > 0"
        class="system-sections-fab-badge"
        :title="$t('publicOrders') || 'الطلبات العامة'"
        aria-hidden="true"
      >{{ publicOrderFabBadgeLabel }}</span>
    </div>

    <b-modal
      v-model="modalVisible"
      size="xl"
      modal-class="system-sections-modal"
      content-class="system-sections-modal-content"
      body-class="system-sections-modal-body"
      hide-header
      hide-footer
      centered
      scrollable
      @shown="liftSectionsModalLayer"
      @hidden="onModalHidden"
    >
      <div class="system-sections-modal-header">
        <div class="system-sections-modal-header-text">
          <h2 class="system-sections-modal-title">
            {{ $t("systemModules") || "أقسام النظام" }}
          </h2>
          <p class="system-sections-modal-subtitle">
            {{ $t("sectionsPageSubtitle") || "اختر القسم للانتقال السريع" }}
          </p>
        </div>
        <button
          type="button"
          class="system-sections-modal-close"
          :aria-label="$t('close') || 'إغلاق'"
          @click="closeModal"
        >
          <b-icon icon="x-lg" />
        </button>
      </div>

      <section v-if="flatHubItems.length" class="system-sections-hub">
        <div class="hub-cards-grid">
          <router-link
            v-for="item in flatHubItems"
            :key="item.name"
            :to="item.link"
            class="hub-module-card"
            active-class="hub-module-card--active"
            exact-active-class="hub-module-card--active"
            @click.native="closeModal"
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

      <p v-else class="system-sections-empty">
        {{ $t("noSectionsAvailable") || "لا توجد أقسام متاحة" }}
      </p>

      <footer class="system-sections-modal-footer">
        <router-link
          to="/logout"
          class="system-sections-logout-btn"
          @click.native="closeModal"
        >
          <b-icon icon="box-arrow-right" class="system-sections-logout-icon"></b-icon>
          <span>{{ $t("Logout") || "تسجيل الخروج" }}</span>
        </router-link>
      </footer>
    </b-modal>
  </div>
</template>

<script>
import sectionsHubMixin from "@/mixins/sectionsHubMixin.js";

/** فوق بوابة مخطط الطاولات (10050) وتحت التوستات العليا عند الحاجة */
const SECTIONS_MODAL_Z_INDEX = 10160;
const SECTIONS_MODAL_BACKDROP_Z_INDEX = 10150;

export default {
  name: "SystemSectionsFab",
  mixins: [sectionsHubMixin],
  data() {
    return {
      modalVisible: false,
      _sectionsModalEl: null,
      _sectionsModalBackdrop: null,
    };
  },
  computed: {
    fabTitle() {
      const base = this.$t("systemModules") || "أقسام النظام";
      const count = this.publicOrderFabBadgeCount;
      if (!count) return base;
      const ordersLabel = this.$t("publicOrders") || "الطلبات العامة";
      return `${base} — ${count} ${ordersLabel}`;
    },
    fabAriaLabel() {
      return this.fabTitle;
    },
  },
  methods: {
    liftSectionsModalLayer() {
      this.$nextTick(() => {
        const modalEl = document.querySelector(".modal.system-sections-modal");
        if (modalEl) {
          modalEl.style.setProperty(
            "z-index",
            String(SECTIONS_MODAL_Z_INDEX),
            "important"
          );
          this._sectionsModalEl = modalEl;
        }

        const backdrops = document.querySelectorAll(".modal-backdrop");
        const backdropEl = backdrops.length
          ? backdrops[backdrops.length - 1]
          : null;
        if (backdropEl) {
          backdropEl.style.setProperty(
            "z-index",
            String(SECTIONS_MODAL_BACKDROP_Z_INDEX),
            "important"
          );
          this._sectionsModalBackdrop = backdropEl;
        }
      });
    },
    clearSectionsModalLayer() {
      if (this._sectionsModalEl) {
        this._sectionsModalEl.style.removeProperty("z-index");
        this._sectionsModalEl = null;
      }
      if (this._sectionsModalBackdrop) {
        this._sectionsModalBackdrop.style.removeProperty("z-index");
        this._sectionsModalBackdrop = null;
      }
    },
    openModal() {
      this.modalVisible = true;
      if (this.shouldTrackPendingOrders) {
        this.refreshSectionsHubPendingCount({ silent: true });
      }
      this.$nextTick(() => {
        this.liftSectionsModalLayer();
        setTimeout(() => this.liftSectionsModalLayer(), 60);
      });
    },
    closeModal() {
      this.modalVisible = false;
    },
    onModalHidden() {
      this.modalVisible = false;
      this.clearSectionsModalLayer();
    },
  },
  beforeDestroy() {
    this.clearSectionsModalLayer();
  },
};
</script>

<style scoped>
.system-sections-fab-shell {
  display: contents;
}

.system-sections-fab-root {
  position: fixed;
  bottom: max(1.15rem, env(safe-area-inset-bottom, 0px));
  inset-inline-end: 1.15rem;
  z-index: 10055;
  width: 4rem;
  height: 4rem;
  pointer-events: none;
}

.system-sections-fab {
  position: relative;
  width: 100%;
  height: 100%;
  top: auto;
  transform: none;
  pointer-events: auto;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: 1px solid color-mix(in srgb, var(--primary-color) 32%, transparent);
  border-radius: 0.85rem;
  background: var(--bg-primary);
  color: var(--primary-color);
  box-shadow:
    0 6px 22px color-mix(in srgb, var(--primary-color) 20%, transparent),
    0 0 0 1px color-mix(in srgb, var(--primary-color) 8%, transparent);
  cursor: pointer;
  overflow: hidden;
  transition:
    background 0.22s ease,
    color 0.22s ease,
    box-shadow 0.22s ease,
    transform 0.22s ease,
    border-color 0.22s ease;
}

[dir="ltr"] .system-sections-fab {
  border-radius: 0.85rem;
}

.system-sections-fab-glow {
  position: absolute;
  inset: 4px;
  border-radius: 0.7rem;
  background: linear-gradient(
    145deg,
    color-mix(in srgb, var(--primary-color) 14%, transparent) 0%,
    color-mix(in srgb, var(--primary-color) 6%, transparent) 48%,
    transparent 100%
  );
  opacity: 0.65;
  transition: opacity 0.22s ease, background 0.22s ease;
  pointer-events: none;
}

.system-sections-fab:hover:not(.system-sections-fab--open) {
  background: linear-gradient(145deg, var(--primary-color) 0%, #7c83f6 52%, var(--primary-color) 100%);
  border-color: rgba(255, 255, 255, 0.22);
  color: #ffffff;
  box-shadow:
    0 8px 22px color-mix(in srgb, var(--primary-color) 38%, transparent),
    0 0 0 1px color-mix(in srgb, var(--primary-color) 20%, transparent);
  transform: scale(1.05);
}

.system-sections-fab--open {
  background: linear-gradient(145deg, var(--primary-color) 0%, var(--primary-color) 48%, var(--primary-color) 100%);
  border-color: var(--primary-light);
  color: #ffffff;
  box-shadow:
    0 0 0 3px color-mix(in srgb, var(--primary-color) 28%, transparent),
    0 12px 30px color-mix(in srgb, var(--primary-color) 45%, transparent);
  transform: scale(1.07);
}

.system-sections-fab:hover:not(.system-sections-fab--open) .system-sections-fab-glow {
  inset: 0;
  border-radius: 0.85rem;
  opacity: 1;
  background: linear-gradient(
    145deg,
    rgba(255, 255, 255, 0.18) 0%,
    rgba(255, 255, 255, 0.06) 45%,
    transparent 100%
  );
}

.system-sections-fab--open .system-sections-fab-glow {
  inset: 0;
  border-radius: 0.85rem;
  opacity: 1;
  background: linear-gradient(
    145deg,
    rgba(255, 255, 255, 0.28) 0%,
    color-mix(in srgb, var(--primary-color) 16%, transparent) 42%,
    color-mix(in srgb, var(--primary-color) 8%, transparent) 100%
  );
}

.system-sections-fab--open .system-sections-fab-icon {
  color: #ffffff !important;
  filter: drop-shadow(0 1px 2px rgba(30, 27, 75, 0.35));
}

.system-sections-fab-icon {
  position: relative;
  z-index: 1;
  font-size: 1.5rem;
}

.system-sections-fab-badge {
  position: absolute;
  top: -0.45rem;
  inset-inline-start: -0.45rem;
  inset-inline-end: auto;
  z-index: 3;
  min-width: 1.45rem;
  height: 1.45rem;
  padding: 0 0.3rem;
  border-radius: 999px;
  background: linear-gradient(135deg, #f59e0b 0%, #ea580c 100%);
  color: #ffffff;
  border: 2px solid var(--bg-primary);
  font-size: 0.7rem;
  font-weight: 800;
  line-height: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 10px rgba(234, 88, 12, 0.45);
  pointer-events: none;
}

.system-sections-fab-root--has-badge:not(.system-sections-fab-root--open) .system-sections-fab-badge {
  animation: system-sections-fab-badge-pulse 2s ease-in-out infinite;
}

.system-sections-fab-root--open .system-sections-fab-badge {
  border-color: var(--primary-color);
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.28);
}

@keyframes system-sections-fab-badge-pulse {
  0%,
  100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.08);
  }
}

@media (max-width: 576px) {
  .system-sections-fab-root {
    width: 3.65rem;
    height: 3.65rem;
    bottom: max(0.9rem, env(safe-area-inset-bottom, 0px));
    inset-inline-end: 0.9rem;
  }

  .system-sections-fab-icon {
    font-size: 1.38rem;
  }
}

.system-sections-empty {
  text-align: center;
  color: var(--text-secondary);
  margin: 0;
  padding: 2rem 0;
}

.hub-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(108px, 1fr));
  gap: 0.75rem;
}

@media (min-width: 768px) {
  .hub-cards-grid {
    grid-template-columns: repeat(auto-fill, minmax(118px, 1fr));
    gap: 0.85rem;
  }
}

.hub-module-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  padding: 0.9rem 0.55rem;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  text-decoration: none;
  color: var(--text-primary);
  transition:
    transform 0.18s ease,
    box-shadow 0.18s ease,
    border-color 0.18s ease,
    background 0.18s ease;
  min-height: 102px;
  text-align: center;
}

.hub-module-card:hover {
  transform: translateY(-2px);
  border-color: color-mix(in srgb, var(--primary-color) 45%, transparent);
  box-shadow: 0 8px 20px color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--text-primary);
  text-decoration: none;
}

.hub-module-card--active {
  border-color: color-mix(in srgb, var(--primary-color) 55%, transparent);
  background: linear-gradient(
    180deg,
    color-mix(in srgb, var(--primary-color) 10%, transparent) 0%,
    color-mix(in srgb, var(--primary-color) 4%, transparent) 100%
  );
  box-shadow: 0 4px 14px color-mix(in srgb, var(--primary-color) 14%, transparent);
}

.hub-module-icon-wrap {
  position: relative;
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: color-mix(in srgb, var(--primary-color) 8%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid color-mix(in srgb, var(--primary-color) 12%, transparent);
  transition: background 0.18s ease, border-color 0.18s ease;
}

.hub-module-card:hover .hub-module-icon-wrap,
.hub-module-card--active .hub-module-icon-wrap {
  background: color-mix(in srgb, var(--primary-color) 14%, transparent);
  border-color: color-mix(in srgb, var(--primary-color) 28%, transparent);
}

.hub-module-icon {
  font-size: 1.2rem;
  color: var(--primary-color);
}

.hub-module-badge {
  position: absolute;
  top: -5px;
  inset-inline-end: -5px;
  min-width: 1.25rem;
  height: 1.25rem;
  padding: 0 0.3rem;
  border-radius: 999px;
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: #fff;
  border: 2px solid var(--bg-primary);
  font-size: 0.625rem;
  font-weight: 800;
  line-height: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.4);
}

.hub-module-label {
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1.3;
  max-width: 100%;
}
</style>

<style>
/* b-modal يُعرض خارج الشجرة — أنماط المودال غير scoped */
body .modal.system-sections-modal {
  z-index: 10160 !important;
}

body .modal.system-sections-modal .modal-dialog {
  max-width: min(920px, 94vw);
}

.system-sections-modal-content {
  border: 1px solid color-mix(in srgb, var(--primary-color) 18%, transparent);
  border-radius: 1rem;
  overflow: hidden;
  box-shadow:
    0 24px 48px rgba(15, 23, 42, 0.14),
    0 0 0 1px color-mix(in srgb, var(--primary-color) 6%, transparent);
  background: var(--bg-primary);
}

.system-sections-modal-body {
  padding: 0;
  background: var(--bg-secondary);
}

.system-sections-modal-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem 1rem;
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--primary-color) 12%, transparent) 0%,
    color-mix(in srgb, var(--primary-color) 6%, transparent) 55%,
    transparent 100%
  );
  border-bottom: 1px solid color-mix(in srgb, var(--primary-color) 12%, transparent);
}

.system-sections-modal-title {
  margin: 0 0 0.25rem;
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--text-primary);
  line-height: 1.3;
}

.system-sections-modal-subtitle {
  margin: 0;
  font-size: 0.875rem;
  color: var(--text-secondary);
  line-height: 1.45;
}

.system-sections-modal-close {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.15rem;
  height: 2.15rem;
  padding: 0;
  border: 1px solid color-mix(in srgb, var(--primary-color) 20%, transparent);
  border-radius: 0.6rem;
  background: var(--bg-primary);
  color: var(--text-secondary);
  cursor: pointer;
  transition:
    background 0.18s ease,
    color 0.18s ease,
    border-color 0.18s ease,
    transform 0.18s ease;
}

.system-sections-modal-close:hover {
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
  border-color: color-mix(in srgb, var(--primary-color) 35%, transparent);
  color: var(--primary-color);
  transform: scale(1.04);
}

.system-sections-hub {
  padding: 1rem 1.15rem 0.75rem;
}

.system-sections-modal-footer {
  display: flex;
  align-items: center;
  justify-content: stretch;
  gap: 0.75rem;
  padding: 0.85rem 1.15rem 1.15rem;
  border-top: 1px solid color-mix(in srgb, var(--border-color) 85%, transparent);
  background: color-mix(in srgb, var(--bg-primary) 88%, var(--bg-secondary));
}

.system-sections-logout-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  width: 100%;
  min-height: 2.65rem;
  padding: 0.7rem 1rem;
  border-radius: 0.75rem;
  border: 1px solid color-mix(in srgb, #dc2626 28%, var(--border-color));
  background: color-mix(in srgb, #dc2626 8%, var(--bg-primary));
  color: #b91c1c;
  font-weight: 700;
  font-size: 0.95rem;
  text-decoration: none;
  cursor: pointer;
  transition:
    background 0.18s ease,
    border-color 0.18s ease,
    color 0.18s ease,
    box-shadow 0.18s ease,
    transform 0.18s ease;
}

.system-sections-logout-btn:hover {
  background: color-mix(in srgb, #dc2626 14%, var(--bg-primary));
  border-color: color-mix(in srgb, #dc2626 45%, var(--border-color));
  color: #991b1b;
  box-shadow: 0 6px 16px color-mix(in srgb, #dc2626 18%, transparent);
  text-decoration: none;
  transform: translateY(-1px);
}

.system-sections-logout-icon {
  font-size: 1.05rem;
}

:root.dark-theme .system-sections-logout-btn {
  background: color-mix(in srgb, #ef4444 14%, var(--bg-primary));
  border-color: color-mix(in srgb, #ef4444 35%, var(--border-color));
  color: #fca5a5;
}

:root.dark-theme .system-sections-logout-btn:hover {
  background: color-mix(in srgb, #ef4444 22%, var(--bg-primary));
  color: #fecaca;
}

:root.light-theme .system-sections-modal-content {
  border-color: color-mix(in srgb, var(--primary-color) 22%, transparent);
  box-shadow:
    0 20px 40px rgba(15, 23, 42, 0.08),
    0 0 0 1px color-mix(in srgb, var(--primary-color) 8%, transparent);
}

:root.light-theme .system-sections-modal-header {
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--primary-color) 6%, #ffffff) 0%,
    #f8fafc 72%,
    #ffffff 100%
  );
  border-bottom-color: color-mix(in srgb, var(--primary-color) 14%, transparent);
}

:root.dark-theme .system-sections-modal-header {
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--primary-color) 22%, transparent) 0%,
    rgba(30, 41, 59, 0.4) 100%
  );
}

:root.light-theme .system-sections-fab {
  background: #ffffff;
  border-color: color-mix(in srgb, var(--primary-color) 28%, transparent);
  color: var(--primary-color);
  box-shadow:
    0 4px 14px color-mix(in srgb, var(--primary-color) 12%, transparent),
    0 0 0 1px color-mix(in srgb, var(--primary-color) 8%, transparent);
}

:root.light-theme .system-sections-fab-glow {
  background: linear-gradient(
    145deg,
    color-mix(in srgb, var(--primary-color) 10%, transparent) 0%,
    color-mix(in srgb, var(--primary-color) 4%, transparent) 50%,
    transparent 100%
  );
}

:root.light-theme .system-sections-fab:hover:not(.system-sections-fab--open) {
  background: linear-gradient(145deg, var(--primary-color) 0%, #7c83f6 100%);
  border-color: rgba(255, 255, 255, 0.35);
  color: #ffffff;
}

:root.light-theme .system-sections-fab.system-sections-fab--open {
  background: linear-gradient(145deg, var(--primary-color) 0%, var(--primary-color) 45%, var(--primary-color) 100%);
  border-color: color-mix(in srgb, var(--primary-color) 35%, #ffffff);
  color: #ffffff;
  box-shadow:
    0 0 0 3px color-mix(in srgb, var(--primary-color) 22%, transparent),
    0 12px 28px rgba(67, 56, 202, 0.38);
}

:root.light-theme .system-sections-fab.system-sections-fab--open .system-sections-fab-glow {
  background: linear-gradient(
    145deg,
    rgba(255, 255, 255, 0.32) 0%,
    rgba(224, 231, 255, 0.2) 50%,
    transparent 100%
  );
}

:root.dark-theme .system-sections-fab {
  background: linear-gradient(180deg, #1e293b 0%, #172033 100%);
  border-color: color-mix(in srgb, var(--primary-color) 38%, transparent);
  color: var(--primary-light);
  box-shadow:
    0 4px 18px rgba(0, 0, 0, 0.28),
    0 0 0 1px color-mix(in srgb, var(--primary-color) 12%, transparent);
}

:root.dark-theme .system-sections-fab-glow {
  opacity: 0.85;
}

:root.dark-theme .system-sections-fab.system-sections-fab--open {
  background: linear-gradient(145deg, var(--primary-dark) 0%, var(--primary-color) 50%, var(--primary-color) 100%);
  border-color: var(--primary-color);
  color: #ffffff;
  box-shadow:
    0 0 0 3px color-mix(in srgb, var(--primary-color) 32%, transparent),
    0 12px 30px rgba(67, 56, 202, 0.55);
}

:root.dark-theme .system-sections-fab.system-sections-fab--open .system-sections-fab-glow {
  background: linear-gradient(
    145deg,
    rgba(255, 255, 255, 0.22) 0%,
    color-mix(in srgb, var(--primary-color) 14%, transparent) 45%,
    transparent 100%
  );
}
</style>
