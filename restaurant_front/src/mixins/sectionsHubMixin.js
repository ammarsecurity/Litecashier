import { buildSectionsHubItems } from "@/navigation/navItems.js";
import { getAllowedSections } from "@/navigation/sectionRegistry.js";
import {
  resolveCommercialUserIdFromStorage,
  fetchPendingPublicOrderCount,
  PUBLIC_ORDER_BADGE_SECTIONS,
} from "@/utils/queueOrders.js";
import pendingOrderAlertSound from "@/utils/pendingOrderAlertSound.js";
import { HTTP } from "@/http/api.js";
import signalRService from "@/services/signalr.js";

let sectionsHubSoundMountCount = 0;
let sectionsHubVisibilityHandler = null;

export default {
  data() {
    return {
      pendingOrderCount: 0,
      commercialUserId: null,
      sectionsHubRefreshInterval: null,
      sectionsHubSignalRHandlers: [],
    };
  },
  computed: {
    sectionsHubRole() {
      return localStorage.getItem("role");
    },
    sectionsHubAllowedSections() {
      return getAllowedSections();
    },
    flatHubItems() {
      return buildSectionsHubItems(
        this.sectionsHubRole,
        (k) => this.$t(k),
        this.sectionsHubAllowedSections
      );
    },
    shouldTrackPendingOrders() {
      return this.flatHubItems.some((item) =>
        PUBLIC_ORDER_BADGE_SECTIONS.has(item.name)
      );
    },
    publicOrderFabBadgeCount() {
      if (!this.shouldTrackPendingOrders || !this.pendingOrderCount) {
        return 0;
      }
      return this.pendingOrderCount;
    },
    publicOrderFabBadgeLabel() {
      const count = this.publicOrderFabBadgeCount;
      if (!count) return "";
      return count > 99 ? "99+" : String(count);
    },
  },
  watch: {
    pendingOrderCount(next, prev) {
      this.syncPendingOrderAlertSound(next, prev);
    },
    shouldTrackPendingOrders(enabled) {
      if (!enabled) {
        pendingOrderAlertSound.stopLoop();
        return;
      }
      this.syncPendingOrderAlertSound(this.pendingOrderCount, 0);
    },
  },
  mounted() {
    this.mountSectionsHubSound();
    this.initSectionsHubTracking();
  },
  beforeDestroy() {
    this.unmountSectionsHubSound();
    this.teardownSectionsHubTracking();
  },
  methods: {
    mountSectionsHubSound() {
      sectionsHubSoundMountCount += 1;
      if (sectionsHubSoundMountCount !== 1) return;

      pendingOrderAlertSound.unlock();
      pendingOrderAlertSound.setTabHidden(document.hidden);

      sectionsHubVisibilityHandler = () => {
        pendingOrderAlertSound.setTabHidden(document.hidden);
      };
      document.addEventListener("visibilitychange", sectionsHubVisibilityHandler);
    },
    unmountSectionsHubSound() {
      sectionsHubSoundMountCount = Math.max(0, sectionsHubSoundMountCount - 1);
      if (sectionsHubSoundMountCount !== 0) return;

      if (sectionsHubVisibilityHandler) {
        document.removeEventListener(
          "visibilitychange",
          sectionsHubVisibilityHandler
        );
        sectionsHubVisibilityHandler = null;
      }
      pendingOrderAlertSound.stopLoop();
    },
    syncPendingOrderAlertSound(next, prev) {
      if (!this.shouldTrackPendingOrders) {
        pendingOrderAlertSound.stopLoop();
        return;
      }

      const nextCount = Number(next) || 0;
      const prevCount = Number(prev) || 0;

      if (nextCount <= 0) {
        pendingOrderAlertSound.stopLoop();
        return;
      }

      if (prevCount <= 0) {
        pendingOrderAlertSound.startLoop();
        return;
      }

      if (nextCount > prevCount) {
        pendingOrderAlertSound.playOnce();
      }
    },
    sectionBadgeCount(item) {
      if (!this.pendingOrderCount || !PUBLIC_ORDER_BADGE_SECTIONS.has(item.name)) {
        return 0;
      }
      return this.pendingOrderCount;
    },
    initSectionsHubTracking() {
      this.commercialUserId = resolveCommercialUserIdFromStorage();
      if (!this.shouldTrackPendingOrders || !this.commercialUserId) {
        return;
      }
      this.refreshSectionsHubPendingCount();
      this.initSectionsHubSignalR();
      this.sectionsHubRefreshInterval = setInterval(() => {
        this.refreshSectionsHubPendingCount({ silent: true });
      }, 15000);
    },
    teardownSectionsHubTracking() {
      if (this.sectionsHubRefreshInterval) {
        clearInterval(this.sectionsHubRefreshInterval);
        this.sectionsHubRefreshInterval = null;
      }
      this.sectionsHubSignalRHandlers.forEach(({ eventName, handler }) => {
        signalRService.off(eventName, handler);
      });
      this.sectionsHubSignalRHandlers = [];
    },
    async refreshSectionsHubPendingCount(options = {}) {
      if (!this.commercialUserId || !this.shouldTrackPendingOrders) return;
      try {
        const count = await fetchPendingPublicOrderCount(
          HTTP,
          this.commercialUserId
        );
        this.pendingOrderCount = count;
      } catch (error) {
        if (!options.silent) {
          console.error("Failed to load pending order count:", error);
        }
      }
    },
    initSectionsHubSignalR() {
      const onRefresh = (data) => {
        const commercialId =
          data?.CommercialUserId ?? data?.commercialUserId ?? null;
        if (
          commercialId != null &&
          Number(commercialId) !== Number(this.commercialUserId)
        ) {
          return;
        }
        this.refreshSectionsHubPendingCount({ silent: true });
      };

      signalRService.startConnection().then(() => {
        const events = ["PublicOrderAdded", "PublicOrderUpdated", "OrderAdded"];
        events.forEach((eventName) => {
          signalRService.on(eventName, onRefresh);
          this.sectionsHubSignalRHandlers.push({ eventName, handler: onRefresh });
        });
      });
    },
  },
};
