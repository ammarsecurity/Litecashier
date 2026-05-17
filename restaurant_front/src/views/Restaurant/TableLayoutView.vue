<template>
  <b-overlay :show="loading" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <div class="users-header-section">
            <div class="users-header-content">
              <h1 class="users-page-title">{{ $t("tableFloorPlanTitle") }}</h1>
              <div class="tables-header-actions">
                <button
                  v-if="canEditFloorPlan"
                  type="button"
                  class="users-add-button"
                  :disabled="saving"
                  @click="savePositions"
                >
                  <b-spinner v-if="saving" small class="me-2" />
                  <b-icon v-else icon="check2-circle" class="button-icon" />
                  <span class="button-text">{{ $t("floorPlanSave") }}</span>
                </button>
                <router-link to="/restaurant/tables" class="users-add-button floor-plan-back-btn">
                  <b-icon icon="arrow-right" class="button-icon" />
                  <span class="button-text">{{ $t("floorPlanBackToTables") }}</span>
                </router-link>
              </div>
            </div>
          </div>

          <div v-if="planKeysForTabs.length" class="floor-plan-tabs-card">
            <div class="floor-plan-tabs-label">{{ $t("floorPlanFloorTabs") }}</div>
            <div class="floor-plan-tabs" role="tablist">
              <button
                v-for="k in planKeysForTabs"
                :key="'plan-tab-' + k"
                type="button"
                role="tab"
                class="floor-plan-tab"
                :class="{ 'floor-plan-tab--active': selectedPlanKey === k }"
                :aria-selected="selectedPlanKey === k ? 'true' : 'false'"
                @click="selectPlanKey(k)"
              >
                {{ k }}
              </button>
            </div>
          </div>

          <div v-if="canEditFloorPlan" class="floor-plan-toolbar-section">
            <div class="floor-plan-toolbar-card">
              <div class="floor-plan-toolbar-header">
                <b-icon icon="sliders" class="floor-plan-toolbar-header-icon" />
                <span class="floor-plan-toolbar-header-text">{{ $t("floorPlanToolbarSettings") }}</span>
              </div>
              <div class="floor-plan-toolbar-grid">
                <div class="floor-plan-toolbar-item">
                  <label class="floor-plan-field-label">
                    <b-icon icon="image" class="floor-plan-field-icon" />
                    {{ $t("floorPlanUploadImage") }}
                  </label>
                  <input type="file" ref="fileInput" accept="image/*" class="d-none" @change="onFile" />
                  <button type="button" class="floor-plan-tool-btn" @click="$refs.fileInput && $refs.fileInput.click()">
                    <b-icon icon="cloud-upload" class="me-2" />
                    {{ $t("floorPlanSelectFile") }}
                  </button>
                </div>
                <div class="floor-plan-toolbar-item">
                  <label class="floor-plan-field-label">
                    <b-icon icon="aspect-ratio" class="floor-plan-field-icon" />
                    {{ $t("floorPlanTableChipSize") }}
                  </label>
                  <div class="floor-plan-chip-size-row">
                    <input
                      v-model.number="tableChipSizePx"
                      type="range"
                      class="floor-plan-chip-size-range"
                      min="32"
                      max="96"
                      step="2"
                      @input="onTableChipSizeChange"
                    />
                    <span class="floor-plan-chip-size-value">{{ clampTableChipSize(tableChipSizePx) }}px</span>
                  </div>
                </div>
                <div class="floor-plan-toolbar-item">
                  <label class="floor-plan-field-label">
                    <b-icon icon="palette" class="floor-plan-field-icon" />
                    {{ $t("floorPlanBackgroundColor") }}
                  </label>
                  <div class="floor-plan-color-row">
                    <input
                      v-model="backgroundColor"
                      type="color"
                      class="floor-plan-color-swatch"
                      :title="$t('floorPlanBackgroundColor')"
                      @change="debouncedSaveSettings"
                    />
                    <span class="floor-plan-color-value">{{ backgroundColor }}</span>
                  </div>
                </div>
                <div class="floor-plan-toolbar-item floor-plan-toolbar-item--switch">
                  <label class="floor-plan-field-label">
                    <b-icon icon="diagram-3" class="floor-plan-field-icon" />
                    {{ $t("floorPlanEditZones") }}
                  </label>
                  <div class="floor-plan-switch-wrap">
                    <b-form-checkbox v-model="editZonesMode" switch class="floor-plan-zone-switch mb-0" />
                  </div>
                </div>
              </div>
              <div
                v-if="canEditFloorPlan && zonesForCurrentPlan.length"
                class="floor-plan-zones-strip"
              >
                <span class="floor-plan-zones-strip-label">{{ $t("floorPlanZonesFromTables") }}</span>
                <div class="floor-plan-zones-badges">
                  <b-badge
                    v-for="zn in zonesForCurrentPlan"
                    :key="'zn-' + zn"
                    pill
                    variant="light"
                    class="floor-plan-zone-badge"
                  >
                    {{ zn }}
                  </b-badge>
                </div>
              </div>
            </div>
          </div>

          <b-modal
            id="modal-floor-zone-pick"
            :title="$t('floorPlanPickZoneTitle')"
            :ok-title="$t('confirm') || 'تأكيد'"
            :cancel-title="$t('cancel') || 'إلغاء'"
            centered
            @ok="confirmZonePick"
            @hidden="onZonePickModalHidden"
          >
            <p class="text-muted small">{{ $t("floorPlanPickZoneHelp") }}</p>
            <label class="floor-plan-field-label mb-2">
              <b-icon icon="geo-alt-fill" class="floor-plan-field-icon" />
              {{ $t("zone") }}
            </label>
            <b-form-select v-model="zonePickSelected" :options="zonePickOptions" size="lg" class="mb-0" />
          </b-modal>

          <div class="floor-workspace" :class="{ 'floor-workspace--readonly': !canEditFloorPlan }">
            <aside v-if="canEditFloorPlan" class="floor-sidebar">
              <h3 class="floor-sidebar-title">{{ $t("floorPlanUnplacedTables") }}</h3>
              <p class="text-muted small">{{ $t("floorPlanUnplacedHint") }}</p>
              <div class="floor-sidebar-list">
                <button
                  v-for="t in unplacedTables"
                  :key="'u-' + tableId(t)"
                  type="button"
                  class="floor-sidebar-chip"
                  @click="addTableToCanvas(t)"
                >
                  <b-icon icon="table" class="me-2" />
                  {{ t.tableNumber }}
                  <span v-if="t.zone" class="floor-chip-zone">{{ t.zone }}</span>
                </button>
                <p v-if="!unplacedTables.length" class="text-muted small mb-0">{{ $t("floorPlanAllPlaced") }}</p>
              </div>
            </aside>

            <div class="floor-canvas-outer">
              <div ref="canvasWrap" class="floor-canvas-wrap" dir="ltr">
                <div
                  ref="floorCanvas"
                  class="floor-canvas"
                  :style="[canvasBgStyle, floorTableChipVarsStyle]"
                  @mousedown.self="onCanvasMouseDown"
                >
                  <div
                    v-for="(z, zi) in zoneRects"
                    :key="'z-' + zi"
                    class="floor-zone-rect"
                    :style="zoneRectStyle(z)"
                  >
                    <span class="floor-zone-label">{{ z.name }}</span>
                  </div>

                  <div
                    v-if="drawingRect && drawingRect.w > 0 && drawingRect.h > 0"
                    class="floor-zone-draw-preview"
                    :style="zoneRectStyle(drawingRect)"
                  />

                  <button
                    v-for="t in placedTables"
                    :key="'p-' + tableId(t)"
                    type="button"
                    class="floor-table-chip"
                    :class="[statusChipClass(t.status), { 'floor-table-chip--readonly': !canEditFloorPlan }]"
                    :style="chipStyle(tableId(t))"
                    @mousedown.stop.prevent="canEditFloorPlan ? startDrag(tableId(t), $event) : undefined"
                  >
                    {{ t.tableNumber }}
                  </button>
                </div>
              </div>
              <p class="floor-hint text-muted small mt-2">{{ $t("floorPlanCanvasHint") }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </b-overlay>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../../http/api.js";
import {
  findOpenFloorPlanSlot,
  resolveFloorPlanOverlaps,
} from "@/utils/floorPlanLayout.js";

export default {
  name: "TableLayoutView",
  components: { AppHeader },
  data() {
    return {
      loading: false,
      saving: false,
      tables: [],
      settings: null,
      positions: {},
      backgroundColor: "#f1f5f9",
      tableChipSizePx: 56,
      zoneRects: [],
      editZonesMode: false,
      drawingRect: null,
      drawStart: null,
      dragTableId: null,
      grab: null,
      saveSettingsTimer: null,
      pendingZoneShape: null,
      zonePickSelected: "",
      selectedPlanKey: "",
      availablePlanKeys: [],
    };
  },
  computed: {
    /** تبويبات المخططات — بدون المفتاح الفارغ (الذي كان يُعرض كـ «عام») */
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
      return {
        backgroundColor: this.backgroundColor || "#f1f5f9",
      };
    },
    floorTableChipVarsStyle() {
      const px = this.clampTableChipSize(this.tableChipSizePx);
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
      return this.tablesForCurrentPlan.filter((t) => {
        const id = String(this.tableId(t));
        return this.positions[id] != null;
      });
    },
    unplacedTables() {
      return this.tablesForCurrentPlan.filter((t) => {
        const id = String(this.tableId(t));
        return this.positions[id] == null;
      });
    },
    canEditFloorPlan() {
      const r = (typeof localStorage !== "undefined" && localStorage.getItem("role")) || "";
      return ["Commercial", "Admin"].includes(r);
    },
    /** مواقع الطاولات ضمن المخطط الحالي (نفس تبويب الطابق) */
    zonesForCurrentPlan() {
      const set = new Set();
      this.tablesForCurrentPlan.forEach((t) => {
        const z = (t.zone ?? t.Zone ?? "").trim();
        if (z) set.add(z);
      });
      return Array.from(set).sort((a, b) => a.localeCompare(b, "ar"));
    },
    zonePickOptions() {
      return this.zonesForCurrentPlan.map((z) => ({ value: z, text: z }));
    },
  },
  mounted() {
    const saved = typeof sessionStorage !== "undefined" ? sessionStorage.getItem("floorPlanPlanKey") : null;
    if (saved !== null) this.selectedPlanKey = saved;
    this.loadFloorPlan();
    window.addEventListener("mousemove", this.onDrawMouseMove);
    window.addEventListener("mouseup", this.onDrawMouseUp);
    window.addEventListener("mousemove", this.onDragMouseMove);
    window.addEventListener("mouseup", this.onDragMouseUp);
  },
  beforeDestroy() {
    window.removeEventListener("mousemove", this.onDrawMouseMove);
    window.removeEventListener("mouseup", this.onDrawMouseUp);
    window.removeEventListener("mousemove", this.onDragMouseMove);
    window.removeEventListener("mouseup", this.onDragMouseUp);
  },
  methods: {
    tableId(t) {
      return t.id ?? t.Id;
    },
    clampTableChipSize(v) {
      const n = Number(v);
      if (!Number.isFinite(n)) return 56;
      return Math.round(Math.max(32, Math.min(96, n)));
    },
    onTableChipSizeChange() {
      this.tableChipSizePx = this.clampTableChipSize(this.tableChipSizePx);
      this.debouncedSaveSettings();
    },
    selectPlanKey(key) {
      if (key === this.selectedPlanKey) return;
      this.selectedPlanKey = key;
      if (typeof sessionStorage !== "undefined") sessionStorage.setItem("floorPlanPlanKey", key);
      this.loadFloorPlan();
    },
    async loadFloorPlan() {
      this.loading = true;
      try {
        const res = await HTTP.get("Tables/floor-plan", {
          params: { planKey: this.selectedPlanKey },
        });
        const root = res.data || {};
        const payload = root.data || root.Data || {};
        const rawTables = payload.tables || [];
        this.tables = rawTables.map((x) => ({
          ...x,
          id: x.id ?? x.Id,
        }));
        const keys = payload.availablePlanKeys || [];
        this.availablePlanKeys = keys.length ? keys : [""];
        if (!this.availablePlanKeys.includes(this.selectedPlanKey)) {
          this.selectedPlanKey = this.availablePlanKeys[0] ?? "";
          if (typeof sessionStorage !== "undefined") {
            sessionStorage.setItem("floorPlanPlanKey", this.selectedPlanKey);
          }
          this.loading = false;
          return this.loadFloorPlan();
        }
        if (typeof sessionStorage !== "undefined") {
          sessionStorage.setItem("floorPlanPlanKey", this.selectedPlanKey);
        }
        this.settings = payload.settings || null;
        if (this.settings && this.settings.backgroundColor) {
          this.backgroundColor = this.settings.backgroundColor;
        }
        const rawChip =
          (this.settings && (this.settings.tableChipSizePx ?? this.settings.TableChipSizePx)) ?? null;
        this.tableChipSizePx = rawChip != null ? this.clampTableChipSize(rawChip) : 56;
        this.zoneRects = [];
        if (this.settings && this.settings.zonesJson) {
          try {
            const parsed = JSON.parse(this.settings.zonesJson);
            if (Array.isArray(parsed)) this.zoneRects = parsed;
          } catch (_) {}
        }
        const next = {};
        this.tables.forEach((t) => {
          const id = String(this.tableId(t));
          const lx = t.layoutPosX ?? t.LayoutPosX;
          const ly = t.layoutPosY ?? t.LayoutPosY;
          if (lx != null && ly != null) {
            next[id] = {
              x: Number(lx),
              y: Number(ly),
            };
          }
        });
        this.positions = resolveFloorPlanOverlaps(next, this.tableChipSizePx);
      } catch (e) {
        this.$toast.error(this.$i18n.t("error") || "خطأ", { position: "top-right", timeout: 4000 });
      } finally {
        this.loading = false;
      }
    },
    chipStyle(id) {
      const p = this.positions[String(id)];
      if (!p) return {};
      const left = p.x * 100;
      const top = p.y * 100;
      return {
        left: `${left}%`,
        top: `${top}%`,
      };
    },
    zoneRectStyle(z) {
      return {
        left: `${z.x * 100}%`,
        top: `${z.y * 100}%`,
        width: `${z.w * 100}%`,
        height: `${z.h * 100}%`,
        borderColor: z.color || "#6366f1",
        backgroundColor: z.color ? `${z.color}33` : "rgba(99,102,241,0.12)",
      };
    },
    statusChipClass(status) {
      const m = {
        Available: "chip-avail",
        Occupied: "chip-occ",
        Reserved: "chip-res",
        OutOfService: "chip-out",
      };
      return m[status] || "chip-avail";
    },
    addTableToCanvas(t) {
      const id = String(this.tableId(t));
      const slot = findOpenFloorPlanSlot(this.positions, this.tableChipSizePx);
      this.$set(this.positions, id, slot);
    },
    normalizeRect(ax, ay, bx, by) {
      const x = Math.min(ax, bx);
      const y = Math.min(ay, by);
      const w = Math.abs(bx - ax);
      const h = Math.abs(by - ay);
      return { x, y, w, h, name: `${this.$t("floorPlanZonePrefix")} ${this.zoneRects.length + 1}`, color: "#93c5fd" };
    },
    canvasNormCoords(clientX, clientY) {
      const el = this.$refs.floorCanvas;
      if (!el) return { nx: 0, ny: 0 };
      const r = el.getBoundingClientRect();
      let nx = (clientX - r.left) / r.width;
      let ny = (clientY - r.top) / r.height;
      nx = Math.max(0, Math.min(1, nx));
      ny = Math.max(0, Math.min(1, ny));
      return { nx, ny };
    },
    onCanvasMouseDown(e) {
      if (!this.canEditFloorPlan) return;
      if (!this.editZonesMode) return;
      if (e.target !== this.$refs.floorCanvas) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      this.drawStart = { nx, ny };
      this.drawingRect = { x: nx, y: ny, w: 0, h: 0, name: "", color: "#93c5fd" };
    },
    onDrawMouseMove(e) {
      if (!this.drawStart || !this.drawingRect) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      const r = this.normalizeRect(this.drawStart.nx, this.drawStart.ny, nx, ny);
      this.drawingRect = { ...this.drawingRect, ...r };
    },
    onDrawMouseUp() {
      if (!this.drawStart || !this.drawingRect) {
        this.drawStart = null;
        this.drawingRect = null;
        return;
      }
      const r = this.drawingRect;
      this.drawStart = null;
      this.drawingRect = null;
      if (r.w < 0.02 || r.h < 0.02) return;
      const zones = this.zonesForCurrentPlan;
      const shape = {
        x: r.x,
        y: r.y,
        w: r.w,
        h: r.h,
        color: r.color || "#93c5fd",
      };
      if (zones.length === 1) {
        this.zoneRects.push({ ...shape, name: zones[0] });
        this.saveZonesOnly();
        return;
      }
      if (zones.length > 1) {
        this.pendingZoneShape = shape;
        this.zonePickSelected = zones[0] || "";
        this.$nextTick(() => this.$bvModal.show("modal-floor-zone-pick"));
        return;
      }
      const name = r.name || `${this.$t("floorPlanZonePrefix")} ${this.zoneRects.length + 1}`;
      this.zoneRects.push({
        ...shape,
        name,
      });
      this.saveZonesOnly();
    },
    confirmZonePick(bvModalEvt) {
      if (!this.pendingZoneShape) return;
      const name = (this.zonePickSelected || "").trim();
      if (!name) {
        if (bvModalEvt) bvModalEvt.preventDefault();
        return;
      }
      this.zoneRects.push({
        ...this.pendingZoneShape,
        name,
      });
      this.pendingZoneShape = null;
      this.saveZonesOnly();
    },
    onZonePickModalHidden() {
      this.pendingZoneShape = null;
    },
    debouncedSaveSettings() {
      if (this.saveSettingsTimer) clearTimeout(this.saveSettingsTimer);
      this.saveSettingsTimer = setTimeout(() => this.saveBgSettings(), 400);
    },
    async saveBgSettings() {
      try {
        await HTTP.put("Tables/floor-plan/settings", {
          planKey: this.selectedPlanKey,
          backgroundColor: this.backgroundColor || null,
          zonesJson: JSON.stringify(this.zoneRects),
          tableChipSizePx: this.clampTableChipSize(this.tableChipSizePx),
          clearFloorPlanImage: false,
        });
      } catch (_) {}
    },
    async saveZonesOnly() {
      try {
        await HTTP.put("Tables/floor-plan/settings", {
          planKey: this.selectedPlanKey,
          backgroundColor: this.backgroundColor || null,
          zonesJson: JSON.stringify(this.zoneRects),
          tableChipSizePx: this.clampTableChipSize(this.tableChipSizePx),
          clearFloorPlanImage: false,
        });
      } catch (e) {
        this.$toast.error(this.$i18n.t("error"), { position: "top-right", timeout: 4000 });
      }
    },
    async onFile(ev) {
      const file = ev.target.files && ev.target.files[0];
      if (!file) return;
      const fd = new FormData();
      fd.append("file", file);
      fd.append("planKey", this.selectedPlanKey || "");
      this.loading = true;
      try {
        await HTTP.post("Tables/floor-plan/image", fd, {
          headers: { "Content-Type": "multipart/form-data" },
        });
        await this.loadFloorPlan();
        this.$toast.success(this.$t("floorPlanImageUploaded") || "تم", { position: "top-right", timeout: 3000 });
      } catch (e) {
        this.$toast.error(e.response?.data?.message || this.$i18n.t("error"), {
          position: "top-right",
          timeout: 4000,
        });
      } finally {
        this.loading = false;
        ev.target.value = "";
      }
    },
    startDrag(tableId, e) {
      const id = String(tableId);
      const p = this.positions[id];
      if (!p) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      this.dragTableId = id;
      this.grab = { dx: nx - p.x, dy: ny - p.y };
    },
    onDragMouseMove(e) {
      if (this.dragTableId == null || !this.grab) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      let x = nx - this.grab.dx;
      let y = ny - this.grab.dy;
      x = Math.max(0, Math.min(1, x));
      y = Math.max(0, Math.min(1, y));
      this.$set(this.positions, String(this.dragTableId), { x, y });
    },
    onDragMouseUp() {
      if (this.dragTableId != null) {
        this.positions = resolveFloorPlanOverlaps(this.positions, this.tableChipSizePx);
      }
      this.dragTableId = null;
      this.grab = null;
    },
    zoneAtPoint(x, y) {
      const rects = this.zoneRects;
      const hits = rects.filter((z) => x >= z.x && x <= z.x + z.w && y >= z.y && y <= z.y + z.h);
      if (!hits.length) return null;
      hits.sort((a, b) => a.w * a.h - b.w * b.h);
      return hits[0].name;
    },
    async savePositions() {
      this.positions = resolveFloorPlanOverlaps(this.positions, this.tableChipSizePx);
      const payload = [];
      Object.keys(this.positions).forEach((idStr) => {
        const id = Number(idStr);
        const p = this.positions[idStr];
        const zn = this.zoneAtPoint(p.x, p.y);
        const row = {
          tableId: id,
          layoutPosX: p.x,
          layoutPosY: p.y,
        };
        if (zn) row.zone = zn;
        payload.push(row);
      });
      if (!payload.length) {
        this.$toast.info(this.$t("floorPlanNothingToSave") || "", { position: "top-right", timeout: 3000 });
        return;
      }
      this.saving = true;
      try {
        await HTTP.post("Tables/floor-plan/positions", payload, {
          params: { planKey: this.selectedPlanKey },
        });
        await HTTP.put("Tables/floor-plan/settings", {
          planKey: this.selectedPlanKey,
          backgroundColor: this.backgroundColor || null,
          zonesJson: JSON.stringify(this.zoneRects),
          tableChipSizePx: this.clampTableChipSize(this.tableChipSizePx),
          clearFloorPlanImage: false,
        });
        this.$toast.success(this.$t("floorPlanSaved") || "تم الحفظ", { position: "top-right", timeout: 4000 });
        await this.loadFloorPlan();
      } catch (e) {
        this.$toast.error(e.response?.data?.message || this.$i18n.t("error"), {
          position: "top-right",
          timeout: 4000,
        });
      } finally {
        this.saving = false;
      }
    },
  },
};
</script>

<style scoped>
.tables-header-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.floor-plan-back-btn {
  text-decoration: none;
  background: var(--bg-primary) !important;
  color: var(--text-primary) !important;
  border: 2px solid var(--border-color) !important;
  box-shadow: var(--shadow-sm) !important;
}

.floor-plan-back-btn:hover {
  transform: translateY(-1px);
  border-color: var(--primary-color) !important;
  color: var(--primary-color) !important;
  box-shadow: 0 4px 12px rgba(129, 140, 248, 0.2) !important;
}

.floor-plan-back-btn:active {
  transform: translateY(0);
}

.floor-plan-tabs-card {
  margin-bottom: 1.25rem;
  padding: 1rem 1.25rem;
  background: var(--bg-primary);
  border-radius: 1rem;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-md);
}

.floor-plan-tabs-label {
  font-size: 0.875rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin-bottom: 0.75rem;
}

.floor-plan-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.floor-plan-tab {
  padding: 0.5rem 1rem;
  border-radius: 0.75rem;
  border: 2px solid var(--border-color);
  background: var(--bg-tertiary);
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.9375rem;
  cursor: pointer;
  transition: border-color 0.2s ease, color 0.2s ease, box-shadow 0.2s ease;
}

.floor-plan-tab:hover {
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.floor-plan-tab--active {
  border-color: var(--primary-color);
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.18) 0%, rgba(167, 139, 250, 0.14) 100%);
  color: var(--primary-color);
  box-shadow: 0 2px 10px rgba(129, 140, 248, 0.22);
}

.floor-plan-toolbar-section {
  margin-bottom: 1.5rem;
}

.floor-plan-toolbar-card {
  background: var(--bg-primary);
  border-radius: 1rem;
  padding: 1.5rem;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-md);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.floor-plan-toolbar-card:hover {
  border-color: var(--border-dark);
  box-shadow: var(--shadow-lg);
}

.floor-plan-toolbar-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.25rem;
  padding-bottom: 1rem;
  border-bottom: 2px solid var(--border-color);
}

.floor-plan-toolbar-header-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.floor-plan-toolbar-header-text {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
}

.floor-plan-toolbar-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1.25rem;
  align-items: end;
}

.floor-plan-toolbar-item {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}

.floor-plan-toolbar-item--switch {
  align-self: end;
}

.floor-plan-field-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  font-size: 0.9375rem;
  color: var(--text-secondary);
  margin: 0;
}

.floor-plan-field-icon {
  font-size: 1rem;
  color: var(--primary-color);
  flex-shrink: 0;
}

.floor-plan-tool-btn {
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.875rem 1rem;
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--text-primary);
  background: var(--bg-tertiary);
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.floor-plan-tool-btn:hover {
  border-color: var(--primary-color);
  color: var(--primary-color);
  background: var(--bg-primary);
  box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.12);
}

.floor-plan-color-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.875rem;
  background: var(--bg-tertiary);
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  min-height: 3rem;
}

.floor-plan-color-swatch {
  width: 2.75rem;
  height: 2.25rem;
  padding: 0;
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  cursor: pointer;
  flex-shrink: 0;
}

.floor-plan-color-value {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-muted);
  font-variant-numeric: tabular-nums;
}

.floor-plan-switch-wrap {
  display: flex;
  align-items: center;
  min-height: 3rem;
  padding: 0 0.25rem;
}

.floor-plan-zone-switch >>> .custom-switch .custom-control-label::before {
  border-color: var(--border-color);
}

.floor-plan-zone-switch >>> .custom-control-input:checked ~ .custom-control-label::before {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
}

@media (max-width: 768px) {
  .floor-plan-toolbar-grid {
    grid-template-columns: 1fr;
  }

  .floor-plan-toolbar-card {
    padding: 1rem;
  }
}

.floor-plan-zones-strip {
  margin-top: 1.25rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--border-color);
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem 0.75rem;
}

.floor-plan-zones-strip-label {
  font-weight: 700;
  font-size: 0.9375rem;
  color: var(--text-secondary);
}

.floor-plan-zones-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}

.floor-plan-zone-badge {
  font-weight: 600;
  font-size: 0.8125rem;
  border: 1px solid var(--border-color) !important;
  color: var(--text-primary) !important;
  padding: 0.35rem 0.75rem !important;
}

.floor-plan-zone-hidden-note {
  margin-top: 0.75rem;
  line-height: 1.55;
}

.floor-workspace {
  display: grid;
  grid-template-columns: 260px 1fr;
  gap: 1.25rem;
  margin-top: 0;
}
.floor-workspace--readonly {
  grid-template-columns: 1fr;
}
@media (max-width: 991px) {
  .floor-workspace {
    grid-template-columns: 1fr;
  }
}
.floor-sidebar {
  background: var(--bg-primary);
  border-radius: 1rem;
  padding: 1.25rem;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-md);
  max-height: 70vh;
  overflow: auto;
}
.floor-sidebar-title {
  font-size: 1.0625rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 0.5rem;
  padding-bottom: 0.75rem;
  border-bottom: 2px solid var(--border-color);
}
.floor-sidebar-chip {
  display: flex;
  align-items: center;
  width: 100%;
  margin-bottom: 0.5rem;
  padding: 0.625rem 0.75rem;
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  background: var(--bg-tertiary);
  text-align: right;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
.floor-sidebar-chip:hover {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.12);
}
.floor-chip-zone {
  margin-right: auto;
  font-size: 0.75rem;
  color: var(--text-muted);
}
.floor-canvas-outer {
  min-width: 0;
}
.floor-canvas-wrap {
  position: relative;
  border-radius: 1rem;
  overflow: hidden;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-md);
  background: var(--bg-tertiary);
}
.floor-canvas {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 10;
  min-height: 280px;
  overflow: hidden;
}
.floor-zone-rect {
  position: absolute;
  border: 2px dashed;
  border-radius: 4px;
  pointer-events: none;
  box-sizing: border-box;
}
.floor-zone-label {
  position: absolute;
  top: 2px;
  left: 4px;
  font-size: 11px;
  font-weight: 600;
  color: #374151;
  text-shadow: 0 0 4px #fff;
}
.floor-zone-draw-preview {
  position: absolute;
  border: 2px dashed #6366f1;
  background: rgba(99, 102, 241, 0.15);
  pointer-events: none;
  border-radius: 4px;
}
.floor-plan-chip-size-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 2.75rem;
}
.floor-plan-chip-size-range {
  flex: 1 1 auto;
  min-width: 0;
  accent-color: #6366f1;
}
.floor-plan-chip-size-value {
  flex: 0 0 auto;
  min-width: 3rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
  text-align: end;
}
.floor-table-chip {
  position: absolute;
  transform: translate(-50%, -50%);
  box-sizing: border-box;
  min-width: var(--floor-table-chip-size, 3.5rem);
  width: var(--floor-table-chip-size, 3.5rem);
  height: var(--floor-table-chip-size, 3.5rem);
  padding: 0;
  border-radius: 0.5rem;
  border: 2px solid #fff;
  font-weight: 700;
  font-size: var(--floor-table-chip-font, 0.9375rem);
  line-height: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: grab;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 2;
}
.floor-table-chip:active {
  cursor: grabbing;
}
.floor-table-chip--readonly {
  cursor: default;
  pointer-events: auto;
}
.floor-table-chip--readonly:active {
  cursor: default;
}
.chip-avail {
  background: linear-gradient(135deg, #22c55e, #16a34a);
  color: #fff;
}
.chip-occ {
  background: linear-gradient(135deg, #ef4444, #dc2626);
  color: #fff;
}
.chip-res {
  background: linear-gradient(135deg, #f59e0b, #d97706);
  color: #fff;
}
.chip-out {
  background: #94a3b8;
  color: #fff;
}
.floor-hint {
  margin-bottom: 0;
}
</style>
