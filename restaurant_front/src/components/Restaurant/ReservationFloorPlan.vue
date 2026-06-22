<template>
  <div class="res-floor-plan">
    <div v-if="planKeysForTabs.length" class="res-floor-tabs-wrap">
      <div class="res-floor-tabs-head">
        <span class="res-floor-tabs-label">
          <b-icon icon="geo-alt-fill"></b-icon>
          {{ $t("floorPlanFloorTabs") || $t("zone") || "الموقع" }}
        </span>
      </div>
      <div class="res-floor-tabs" role="tablist">
        <button
          v-for="k in planKeysForTabs"
          :key="'res-plan-' + k"
          type="button"
          role="tab"
          class="res-floor-tab"
          :class="{ 'res-floor-tab--active': selectedPlanKey === k }"
          :aria-selected="selectedPlanKey === k ? 'true' : 'false'"
          @click="selectedPlanKey = k"
        >
          <b-icon icon="diagram-2" class="res-floor-tab-icon"></b-icon>
          <span class="res-floor-tab-text">{{ k || ($t("allZones") || "عام") }}</span>
          <span class="res-floor-tab-count">{{ tablesCountForZone(k) }}</span>
        </button>
      </div>
    </div>

    <div v-if="loading" class="res-floor-loading">
      <b-spinner small></b-spinner>
      <span>{{ $t('loading') }}</span>
    </div>

    <div v-else class="res-floor-canvas-wrap" :style="floorTableChipVarsStyle">
      <div class="res-floor-canvas" :style="canvasBgStyle" ref="canvas">
        <button
          v-for="t in placedTables"
          :key="'res-chip-' + tableId(t)"
          type="button"
          class="floor-table-chip res-floor-chip"
          :class="chipClassForTable(t)"
          :style="chipStyle(tableId(t))"
          :title="chipTitle(t)"
          @click="$emit('table-select', t)"
        >
          {{ t.tableNumber }}
        </button>
      </div>
    </div>

    <div class="res-floor-legend">
      <span class="res-legend-item res-legend-item--avail">{{ $t('available') || 'متاحة' }}</span>
      <span class="res-legend-item res-legend-item--res">{{ $t('reserved') || 'محجوزة' }}</span>
      <span class="res-legend-item res-legend-item--occ">{{ $t('occupied') || 'مشغولة' }}</span>
      <span class="res-legend-item res-legend-item--out">{{ $t('outOfService') || 'خارج الخدمة' }}</span>
    </div>
  </div>
</template>

<script>
import { HTTP } from "@/http/api.js";

export default {
  name: "ReservationFloorPlan",
  props: {
    filterDate: { type: String, default: "" },
    filterDateTo: { type: String, default: "" },
    filterTime: { type: String, default: "" },
    selectedTableId: { type: [Number, String], default: null },
  },
  data() {
    return {
      loading: false,
      tables: [],
      positions: {},
      settings: null,
      backgroundColor: "#f1f5f9",
      tableChipSizePx: 56,
      selectedPlanKey: "",
      availablePlanKeys: [],
      availabilityMap: {},
    };
  },
  computed: {
    planKeysForTabs() {
      return this.availablePlanKeys.filter((k) => String(k ?? "").trim() !== "");
    },
    canvasBgStyle() {
      const img = this.settings && this.settings.floorPlanImageUrl;
      if (img) {
        return {
          backgroundImage: `url("${img}")`,
          backgroundSize: "contain",
          backgroundPosition: "center",
          backgroundRepeat: "no-repeat",
          backgroundColor: this.backgroundColor || "#f1f5f9",
        };
      }
      return { backgroundColor: this.backgroundColor || "#f1f5f9" };
    },
    floorTableChipVarsStyle() {
      const px = Math.min(96, Math.max(32, Number(this.tableChipSizePx) || 56));
      return {
        "--floor-table-chip-size": `${px}px`,
        "--floor-table-chip-font": `${Math.max(11, Math.round(px * 0.32))}px`,
      };
    },
    tablesForCurrentPlan() {
      const pk = (this.selectedPlanKey ?? "").trim();
      return this.tables.filter((t) => {
        const z = (t.zone ?? t.Zone ?? "").trim();
        if (pk === "") return z === "";
        return z === pk;
      });
    },
    placedTables() {
      return this.tablesForCurrentPlan.filter((t) => this.positions[String(this.tableId(t))] != null);
    },
  },
  watch: {
    filterDate: { handler: "reloadAll", immediate: false },
    filterDateTo: { handler: "loadAvailability", immediate: false },
    filterTime: { handler: "loadAvailability", immediate: false },
    selectedPlanKey: { handler: "loadFloorPlan", immediate: false },
  },
  mounted() {
    this.reloadAll();
  },
  methods: {
    tableId(t) {
      return t.id ?? t.Id;
    },
    tablesCountForZone(zoneKey) {
      const pk = String(zoneKey ?? "").trim();
      return this.tables.filter((t) => {
        const z = String(t.zone ?? t.Zone ?? "").trim();
        return z === pk;
      }).length;
    },
    chipStyle(id) {
      const p = this.positions[String(id)];
      if (!p) return {};
      return {
        left: `${p.x * 100}%`,
        top: `${p.y * 100}%`,
      };
    },
    chipClassForTable(t) {
      const id = Number(this.tableId(t));
      const selected = this.selectedTableId != null && Number(this.selectedTableId) === id;
      const avail = this.availabilityMap[id];
      if (selected) return "res-chip-selected";
      if (avail) {
        if (avail.tableStatus === "OutOfService") return "res-chip-out";
        if (avail.reservationId) {
          const st = String(avail.reservationStatus || "").trim();
          if (st === "Seated" || avail.tableStatus === "Occupied") return "res-chip-occupied";
          return "res-chip-reserved";
        }
        if (avail.tableStatus === "Reserved") return "res-chip-reserved";
        if (avail.tableStatus === "Occupied" || avail.hasConflict) return "res-chip-occupied";
        return "res-chip-available";
      }
      const st = String(t.status || "").trim();
      if (st === "Occupied") return "res-chip-occupied";
      if (st === "Reserved") return "res-chip-reserved";
      if (st === "OutOfService") return "res-chip-out";
      return "res-chip-available";
    },
    chipTitle(t) {
      const id = Number(this.tableId(t));
      const a = this.availabilityMap[id];
      if (a && a.customerName) {
        return `${a.customerName} — ${this.formatDt(a.reservationDateTime)}`;
      }
      return `${t.tableNumber}${t.zone ? ` (${t.zone})` : ""}`;
    },
    formatDt(v) {
      if (!v) return "";
      try {
        return new Date(v).toLocaleString(this.$i18n?.locale === "en" ? "en" : "ar-IQ");
      } catch (e) {
        return String(v);
      }
    },
    async reloadAll() {
      await this.loadFloorPlan();
      await this.loadAvailability();
    },
    async loadPlanKeys() {
      /* keys loaded with floor-plan response */
    },
    async loadFloorPlan() {
      this.loading = true;
      try {
        const res = await HTTP.get("Tables/floor-plan", {
          params: { planKey: this.selectedPlanKey },
        });
        const payload = res?.data?.data || {};
        const rawTables = payload.tables || [];
        this.tables = rawTables.map((x) => ({ ...x, id: x.id ?? x.Id }));
        const keys = payload.availablePlanKeys || [];
        this.availablePlanKeys = keys.length ? keys : [""];
        if (!this.availablePlanKeys.includes(this.selectedPlanKey)) {
          this.selectedPlanKey = this.availablePlanKeys[0] ?? "";
        }
        this.settings = payload.settings || null;
        this.backgroundColor = this.settings?.backgroundColor || "#f1f5f9";
        this.tableChipSizePx = this.settings?.tableChipSizePx || 56;
        const pos = {};
        (this.tables || []).forEach((t) => {
          const id = String(this.tableId(t));
          const lx = t.layoutPosX ?? t.LayoutPosX;
          const ly = t.layoutPosY ?? t.LayoutPosY;
          if (lx != null && ly != null) {
            pos[id] = { x: Number(lx), y: Number(ly) };
          }
        });
        this.positions = pos;
      } catch (e) {
        console.error(e);
      } finally {
        this.loading = false;
      }
    },
    async loadAvailability() {
      if (!this.filterDate) return;
      try {
        const params = { date: `${this.filterDate}T00:00:00` };
        const endDate = this.filterDateTo || this.filterDate;
        if (this.filterTime) {
          params.time = this.filterTime;
        } else {
          params.calendarView = true;
          params.toDate = `${endDate}T23:59:59`;
        }
        const res = await HTTP.get("Reservations/availability", { params });
        const tables = res?.data?.data?.tables || [];
        const map = {};
        tables.forEach((row) => {
          map[Number(row.tableId)] = row;
        });
        this.availabilityMap = map;
      } catch (e) {
        console.error(e);
        this.availabilityMap = {};
      }
    },
  },
};
</script>

<style scoped>
.res-floor-plan {
  margin: 0;
}

/* Zone tabs */
.res-floor-tabs-wrap {
  margin-bottom: 0.85rem;
}

.res-floor-tabs-head {
  margin-bottom: 0.5rem;
}

.res-floor-tabs-label {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.75rem;
  font-weight: 650;
  color: var(--text-secondary);
  letter-spacing: 0.01em;
}

.res-floor-tabs {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  padding: 0.3rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--bg-secondary) 90%, var(--border-color) 10%);
}

.res-floor-tab {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.48rem 0.9rem;
  border: none;
  border-radius: 0.55rem;
  background: transparent;
  color: var(--text-secondary);
  font-size: 0.8125rem;
  font-weight: 650;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease, box-shadow 0.15s ease;
  white-space: nowrap;
}

.res-floor-tab-icon {
  font-size: 0.9rem;
  opacity: 0.85;
  flex-shrink: 0;
}

.res-floor-tab-text {
  line-height: 1.2;
}

.res-floor-tab-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.35rem;
  height: 1.35rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  background: rgba(148, 163, 184, 0.2);
  color: var(--text-muted);
  font-size: 0.6875rem;
  font-weight: 700;
  flex-shrink: 0;
}

.res-floor-tab:hover {
  color: #a78bfa;
  background: rgba(124, 58, 237, 0.08);
}

.res-floor-tab--active {
  background: linear-gradient(135deg, #a78bfa, #7c3aed);
  color: #fff;
  box-shadow: 0 2px 10px rgba(124, 58, 237, 0.35);
}

.res-floor-tab--active .res-floor-tab-icon {
  opacity: 1;
}

.res-floor-tab--active .res-floor-tab-count {
  background: rgba(255, 255, 255, 0.22);
  color: #fff;
}

.res-floor-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  min-height: 200px;
  color: var(--text-secondary);
  font-size: 0.875rem;
}
.res-floor-canvas-wrap {
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  overflow: hidden;
  background: var(--bg-secondary);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
}
.res-floor-canvas {
  position: relative;
  width: 100%;
  min-height: 320px;
  aspect-ratio: 16 / 10;
}
.res-floor-chip {
  position: absolute;
  transform: translate(-50%, -50%);
  width: var(--floor-table-chip-size, 56px);
  height: var(--floor-table-chip-size, 56px);
  font-size: var(--floor-table-chip-font, 14px);
  border-radius: 50%;
  border: 2px solid rgba(255, 255, 255, 0.35);
  font-weight: 800;
  cursor: pointer;
  transition: transform 0.15s, box-shadow 0.15s;
}
.res-floor-chip:hover {
  transform: translate(-50%, -50%) scale(1.06);
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.2);
}
.res-chip-available {
  background: linear-gradient(135deg, #22c55e, #16a34a);
  color: #fff;
}
.res-chip-reserved {
  background: linear-gradient(135deg, #a78bfa, #7c3aed);
  color: #fff;
}
.res-chip-occupied {
  background: linear-gradient(135deg, #ef4444, #dc2626);
  color: #fff;
}
.res-chip-out {
  background: #94a3b8;
  color: #fff;
}
.res-chip-selected {
  background: linear-gradient(135deg, #6366f1, #4f46e5);
  color: #fff;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.45);
}
.res-floor-legend {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.75rem 1.25rem;
  margin-top: 0.75rem;
  font-size: 0.8rem;
}
.res-legend-item {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}
.res-legend-item::before {
  content: "";
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
}
.res-legend-item--avail::before {
  background: #16a34a;
}
.res-legend-item--res::before {
  background: #7c3aed;
}
.res-legend-item--occ::before {
  background: #dc2626;
}
.res-legend-item--out::before {
  background: #94a3b8;
}

@media (max-width: 576px) {
  .res-floor-tabs {
    display: flex;
    width: 100%;
  }

  .res-floor-tab {
    flex: 1 1 auto;
    justify-content: center;
    padding: 0.5rem 0.55rem;
    font-size: 0.75rem;
  }

  .res-floor-tab-icon {
    display: none;
  }
}
</style>
