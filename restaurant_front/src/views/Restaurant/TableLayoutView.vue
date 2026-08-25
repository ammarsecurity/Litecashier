<template>
  <b-overlay :show="loading" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
          <div class="users-page-content floor-plan-page">
          <header class="fp-hero">
            <div class="fp-hero-copy">
              <p class="fp-eyebrow">{{ $t("floorPlanEyebrow") }}</p>
              <h1 class="fp-title">{{ $t("tableFloorPlanTitle") }}</h1>
              <p class="fp-subtitle">{{ $t("floorPlanSubtitle") }}</p>
            </div>
            <div class="fp-hero-aside">
              <div class="fp-stats">
                <div class="fp-stat">
                  <strong>{{ placedTables.length }}</strong>
                  <span>{{ $t("floorPlanPlacedCount") }}</span>
                </div>
                <div v-if="canEditFloorPlan" class="fp-stat">
                  <strong>{{ unplacedTables.length }}</strong>
                  <span>{{ $t("floorPlanUnplacedCount") }}</span>
                </div>
              </div>
              <div class="fp-hero-actions">
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
                <router-link to="/restaurant/tables" class="fp-back-btn">
                  <b-icon icon="arrow-right" />
                  <span>{{ $t("floorPlanBackToTables") }}</span>
                </router-link>
              </div>
            </div>
          </header>

          <div v-if="planKeysForTabs.length" class="fp-floors" role="tablist" :aria-label="$t('floorPlanFloorTabs')">
            <span class="fp-floors-label">{{ $t("floorPlanFloorTabs") }}</span>
            <div class="fp-floors-tabs">
              <button
                v-for="k in planKeysForTabs"
                :key="'plan-tab-' + k"
                type="button"
                role="tab"
                class="fp-floor-tab"
                :class="{ 'fp-floor-tab--active': selectedPlanKey === k }"
                :aria-selected="selectedPlanKey === k ? 'true' : 'false'"
                @click="selectPlanKey(k)"
              >
                {{ k }}
              </button>
            </div>
          </div>

          <div v-if="canEditFloorPlan" class="fp-tools">
            <input type="file" ref="fileInput" accept="image/*" class="d-none" @change="onFile" />
            <button type="button" class="fp-tool-btn" @click="$refs.fileInput && $refs.fileInput.click()">
              <b-icon icon="image" />
              <span>{{ $t("floorPlanSelectFile") }}</span>
            </button>
            <label class="fp-tool-size">
              <span>{{ $t("floorPlanTableChipSize") }}</span>
              <input
                v-model.number="tableChipSizePx"
                type="range"
                min="32"
                max="96"
                step="2"
                @input="onTableChipSizeChange"
              />
              <em>{{ clampTableChipSize(tableChipSizePx) }}</em>
            </label>
            <label class="fp-tool-color" :title="$t('floorPlanBackgroundColor')">
              <input
                v-model="backgroundColor"
                type="color"
                @change="debouncedSaveSettings"
              />
              <span>{{ $t("floorPlanBackgroundColor") }}</span>
            </label>
            <label class="fp-tool-switch">
              <b-form-checkbox v-model="editZonesMode" switch class="floor-plan-zone-switch mb-0" />
              <span>{{ $t("floorPlanEditZones") }}</span>
            </label>
            <button
              v-if="editZonesMode && selectedZoneIndex != null"
              type="button"
              class="fp-tool-btn fp-tool-btn--danger"
              @click="deleteSelectedZone"
            >
              <b-icon icon="trash" />
              <span>{{ $t("floorPlanDeleteZone") }}</span>
            </button>
            <div v-if="zonesForCurrentPlan.length" class="fp-zones">
              <span>{{ $t("floorPlanZonesFromTables") }}</span>
              <span v-for="zn in zonesForCurrentPlan" :key="'zn-' + zn" class="fp-zone-pill">{{ zn }}</span>
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
              <div class="fp-sidebar-head">
                <h3 class="floor-sidebar-title">{{ $t("floorPlanUnplacedTables") }}</h3>
                <span class="fp-sidebar-count">{{ unplacedTables.length }}</span>
              </div>
              <p class="fp-sidebar-hint">{{ $t("floorPlanUnplacedHint") }}</p>
              <div class="floor-sidebar-list">
                <button
                  v-for="t in unplacedTables"
                  :key="'u-' + tableId(t)"
                  type="button"
                  class="floor-sidebar-chip"
                  @click="addTableToCanvas(t)"
                >
                  <span class="fp-sidebar-chip-num">{{ t.tableNumber }}</span>
                  <span v-if="t.capacity" class="fp-sidebar-chip-cap">{{ t.capacity }}</span>
                </button>
                <p v-if="!unplacedTables.length" class="fp-sidebar-empty">{{ $t("floorPlanAllPlaced") }}</p>
              </div>
            </aside>

            <div class="floor-canvas-outer">
              <div
                ref="canvasWrap"
                class="floor-canvas-wrap"
                :class="{ 'floor-canvas-wrap--grid': !hasFloorImage }"
                dir="ltr"
              >
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
                    :class="{
                      'floor-zone-rect--editable': editZonesMode,
                      'floor-zone-rect--selected': editZonesMode && selectedZoneIndex === zi,
                    }"
                    :style="zoneRectStyle(z)"
                    @mousedown.stop="onZoneMouseDown($event, zi)"
                  >
                    <span class="floor-zone-label">{{ z.name }}</span>
                    <template v-if="editZonesMode && selectedZoneIndex === zi">
                      <span
                        v-for="handle in zoneResizeHandles"
                        :key="'h-' + zi + '-' + handle"
                        class="floor-zone-resize-handle"
                        :class="'floor-zone-resize-handle--' + handle"
                        @mousedown.stop.prevent="onZoneResizeMouseDown($event, zi, handle)"
                      />
                    </template>
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
                    :title="String(t.tableNumber)"
                    @mousedown.stop.prevent="canEditFloorPlan ? startDrag(tableId(t), $event) : undefined"
                  >
                    {{ t.tableNumber }}
                  </button>
                  <div v-if="!placedTables.length" class="fp-canvas-empty">
                    {{ $t("floorPlanEmptyCanvas") }}
                  </div>
                </div>
                <div class="fp-legend">
                  <span class="fp-legend-item"><i class="chip-avail"></i>{{ $t("available") }}</span>
                  <span class="fp-legend-item"><i class="chip-occ"></i>{{ $t("occupied") }}</span>
                  <span class="fp-legend-item"><i class="chip-res"></i>{{ $t("reserved") }}</span>
                  <span class="fp-legend-item"><i class="chip-out"></i>{{ $t("outOfService") }}</span>
                </div>
              </div>
              <p class="floor-hint">
                {{ canEditFloorPlan ? $t("floorPlanCanvasHintEdit") : $t("floorPlanCanvasHint") }}
              </p>
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
      selectedZoneIndex: null,
      zoneInteraction: null,
      zoneResizeHandles: ["nw", "ne", "sw", "se"],
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
    hasFloorImage() {
      return !!(this.settings && (this.settings.floorPlanImageUrl || this.settings.FloorPlanImageUrl));
    },
  },
  watch: {
    editZonesMode(enabled) {
      if (!enabled) {
        this.selectedZoneIndex = null;
        this.zoneInteraction = null;
      }
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
    window.addEventListener("keydown", this.onZoneKeyDown);
  },
  beforeDestroy() {
    window.removeEventListener("mousemove", this.onDrawMouseMove);
    window.removeEventListener("mouseup", this.onDrawMouseUp);
    window.removeEventListener("mousemove", this.onDragMouseMove);
    window.removeEventListener("mouseup", this.onDragMouseUp);
    window.removeEventListener("keydown", this.onZoneKeyDown);
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
        this.selectedZoneIndex = null;
        this.zoneInteraction = null;
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
        borderColor: z.color || "var(--primary-color)",
        backgroundColor: z.color ? `${z.color}33` : "color-mix(in srgb, var(--primary-color) 12%, transparent)",
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
      this.selectedZoneIndex = null;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      this.drawStart = { nx, ny };
      this.drawingRect = { x: nx, y: ny, w: 0, h: 0, name: "", color: "#93c5fd" };
    },
    onZoneMouseDown(e, index) {
      if (!this.canEditFloorPlan || !this.editZonesMode) return;
      this.selectedZoneIndex = index;
      const z = this.zoneRects[index];
      if (!z) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      this.zoneInteraction = {
        kind: "move",
        index,
        startNx: nx,
        startNy: ny,
        origin: { x: z.x, y: z.y, w: z.w, h: z.h },
      };
    },
    onZoneResizeMouseDown(e, index, handle) {
      if (!this.canEditFloorPlan || !this.editZonesMode) return;
      this.selectedZoneIndex = index;
      const z = this.zoneRects[index];
      if (!z) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      this.zoneInteraction = {
        kind: "resize",
        index,
        handle,
        startNx: nx,
        startNy: ny,
        origin: { x: z.x, y: z.y, w: z.w, h: z.h },
      };
    },
    applyZoneMove(interaction, nx, ny) {
      const idx = interaction.index;
      const z = this.zoneRects[idx];
      if (!z) return;
      const dx = nx - interaction.startNx;
      const dy = ny - interaction.startNy;
      let x = interaction.origin.x + dx;
      let y = interaction.origin.y + dy;
      x = Math.max(0, Math.min(1 - z.w, x));
      y = Math.max(0, Math.min(1 - z.h, y));
      this.$set(this.zoneRects, idx, { ...z, x, y });
    },
    applyZoneResize(interaction, nx, ny) {
      const idx = interaction.index;
      const z = this.zoneRects[idx];
      if (!z) return;
      const min = 0.02;
      const o = interaction.origin;
      let x = o.x;
      let y = o.y;
      let w = o.w;
      let h = o.h;
      const right = o.x + o.w;
      const bottom = o.y + o.h;

      if (interaction.handle.includes("e")) {
        w = Math.max(min, nx - x);
      }
      if (interaction.handle.includes("w")) {
        x = Math.min(nx, right - min);
        w = right - x;
      }
      if (interaction.handle.includes("s")) {
        h = Math.max(min, ny - y);
      }
      if (interaction.handle.includes("n")) {
        y = Math.min(ny, bottom - min);
        h = bottom - y;
      }

      x = Math.max(0, x);
      y = Math.max(0, y);
      if (x + w > 1) w = 1 - x;
      if (y + h > 1) h = 1 - y;

      this.$set(this.zoneRects, idx, { ...z, x, y, w, h });
    },
    onZoneInteractionMove(e) {
      if (!this.zoneInteraction) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      if (this.zoneInteraction.kind === "move") {
        this.applyZoneMove(this.zoneInteraction, nx, ny);
      } else {
        this.applyZoneResize(this.zoneInteraction, nx, ny);
      }
    },
    finishZoneInteraction() {
      if (!this.zoneInteraction) return;
      this.zoneInteraction = null;
      this.saveZonesOnly();
    },
    deleteSelectedZone() {
      if (this.selectedZoneIndex == null) return;
      this.zoneRects.splice(this.selectedZoneIndex, 1);
      this.selectedZoneIndex = null;
      this.zoneInteraction = null;
      this.saveZonesOnly();
    },
    onZoneKeyDown(e) {
      if (!this.canEditFloorPlan || !this.editZonesMode) return;
      if (this.selectedZoneIndex == null) return;
      if (e.key === "Delete" || e.key === "Backspace") {
        e.preventDefault();
        this.deleteSelectedZone();
      }
    },
    onDrawMouseMove(e) {
      if (this.zoneInteraction) {
        this.onZoneInteractionMove(e);
        return;
      }
      if (!this.drawStart || !this.drawingRect) return;
      const { nx, ny } = this.canvasNormCoords(e.clientX, e.clientY);
      const r = this.normalizeRect(this.drawStart.nx, this.drawStart.ny, nx, ny);
      this.drawingRect = { ...this.drawingRect, ...r };
    },
    onDrawMouseUp() {
      if (this.zoneInteraction) {
        this.finishZoneInteraction();
        return;
      }
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
.floor-plan-page {
  max-width: 1440px;
}

.fp-hero {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 16px;
  padding: 8px 0 16px;
  border-bottom: 1px solid var(--border-light, var(--border-color));
}

.fp-eyebrow {
  margin: 0 0 8px;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.04em;
  color: var(--primary-color);
}

.fp-title {
  margin: 0;
  font-size: 28px;
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.2;
  color: var(--text-primary);
}

.fp-subtitle {
  margin: 8px 0 0;
  font-size: 15px;
  font-weight: 500;
  line-height: 1.5;
  color: var(--text-secondary);
}

.fp-hero-aside {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 16px;
}

.fp-stats {
  display: flex;
  gap: 8px;
}

.fp-stat {
  min-width: 88px;
  padding: 10px 16px;
  border-radius: 14px;
  background: var(--bg-primary);
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
  text-align: center;
}

.fp-stat strong {
  display: block;
  font-size: 22px;
  font-weight: 800;
  line-height: 1.1;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
}

.fp-stat span {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-muted);
}

.fp-hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.fp-back-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 44px;
  padding: 0 16px;
  border-radius: 12px;
  border: none;
  background: transparent;
  color: var(--text-secondary);
  font-size: 15px;
  font-weight: 700;
  text-decoration: none;
}

.fp-back-btn:hover {
  color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 8%, transparent);
}

.fp-floors {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.fp-floors-label {
  font-size: 13px;
  font-weight: 700;
  color: var(--text-muted);
}

.fp-floors-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 4px;
  border-radius: 14px;
  background: var(--bg-tertiary, #f1f5f9);
}

.fp-floor-tab {
  min-height: 40px;
  padding: 0 16px;
  border: none;
  border-radius: 12px;
  background: transparent;
  color: var(--text-secondary);
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
}

.fp-floor-tab:hover {
  color: var(--text-primary);
}

.fp-floor-tab--active {
  background: var(--bg-primary, #fff);
  color: var(--primary-color);
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
}

.fp-tools {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px 16px;
  margin-bottom: 16px;
  padding: 12px 16px;
  border-radius: 16px;
  background: var(--bg-primary);
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
}

.fp-tool-btn,
.fp-tool-size,
.fp-tool-color,
.fp-tool-switch {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 44px;
  margin: 0;
  font-size: 13px;
  font-weight: 700;
  color: var(--text-secondary);
}

.fp-tool-btn {
  padding: 0 14px;
  border: none;
  border-radius: 12px;
  background: var(--bg-tertiary, #f1f5f9);
  cursor: pointer;
}

.fp-tool-btn:hover {
  color: var(--primary-color);
}

.fp-tool-btn--danger {
  color: #dc2626;
  background: rgba(239, 68, 68, 0.1);
}

.fp-tool-size input[type="range"] {
  width: 112px;
  accent-color: var(--primary-color);
}

.fp-tool-size em {
  min-width: 2rem;
  font-style: normal;
  font-variant-numeric: tabular-nums;
  color: var(--text-primary);
}

.fp-tool-color input[type="color"] {
  width: 28px;
  height: 28px;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  cursor: pointer;
}

.fp-zones {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-inline-start: auto;
  font-size: 13px;
  font-weight: 700;
  color: var(--text-muted);
}

.fp-zone-pill {
  padding: 6px 12px;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
  color: var(--primary-color);
  font-size: 12px;
}

.floor-plan-field-label {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 0;
  font-size: 14px;
  font-weight: 700;
  color: var(--text-secondary);
}

.floor-plan-field-icon {
  color: var(--primary-color);
}

.floor-plan-zone-switch >>> .custom-control-input:checked ~ .custom-control-label::before {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
}

.floor-workspace {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  gap: 16px;
}

.floor-workspace--readonly {
  grid-template-columns: 1fr;
}

@media (max-width: 991px) {
  .fp-hero,
  .fp-hero-aside {
    align-items: stretch;
    flex-direction: column;
  }

  .floor-workspace {
    grid-template-columns: 1fr;
  }
}

.floor-sidebar {
  background: var(--bg-primary);
  border-radius: 16px;
  padding: 16px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
  max-height: min(72vh, 720px);
  overflow: auto;
}

.fp-sidebar-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 8px;
}

.floor-sidebar-title {
  margin: 0;
  font-size: 15px;
  font-weight: 800;
  color: var(--text-primary);
}

.fp-sidebar-count {
  min-width: 28px;
  height: 28px;
  display: grid;
  place-items: center;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--primary-color);
  font-size: 13px;
  font-weight: 800;
}

.fp-sidebar-hint,
.fp-sidebar-empty,
.floor-hint {
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
  color: var(--text-muted);
}

.fp-sidebar-hint {
  margin-bottom: 16px;
}

.floor-sidebar-chip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  margin-bottom: 8px;
  padding: 10px 12px;
  border: none;
  border-radius: 12px;
  background: var(--bg-tertiary, #f8fafc);
  text-align: start;
  cursor: pointer;
  color: var(--text-primary);
}

.floor-sidebar-chip:hover {
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
}

.fp-sidebar-chip-num {
  font-size: 15px;
  font-weight: 800;
}

.fp-sidebar-chip-cap {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-muted);
}

.floor-canvas-outer {
  min-width: 0;
}

.floor-canvas-wrap {
  position: relative;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
  background: var(--bg-tertiary);
}

.floor-canvas-wrap--grid .floor-canvas {
  background-image:
    linear-gradient(to right, color-mix(in srgb, var(--text-primary) 6%, transparent) 1px, transparent 1px),
    linear-gradient(to bottom, color-mix(in srgb, var(--text-primary) 6%, transparent) 1px, transparent 1px);
  background-size: 32px 32px;
}

.floor-canvas {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 9;
  min-height: 360px;
  overflow: hidden;
}

.fp-legend {
  position: absolute;
  inset-inline-start: 12px;
  bottom: 12px;
  z-index: 5;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 8px 10px;
  border-radius: 12px;
  background: color-mix(in srgb, var(--bg-primary) 88%, transparent);
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.12);
  pointer-events: none;
}

.fp-legend-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 700;
  color: var(--text-primary);
}

.fp-legend-item i {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  display: inline-block;
}

.fp-canvas-empty {
  position: absolute;
  inset: 0;
  display: grid;
  place-items: center;
  font-size: 15px;
  font-weight: 700;
  color: var(--text-muted);
  pointer-events: none;
  z-index: 1;
}

.floor-hint {
  margin-top: 12px;
}

.floor-zone-rect {
  position: absolute;
  border: 2px dashed;
  border-radius: 12px;
  pointer-events: none;
  box-sizing: border-box;
  z-index: 1;
}

.floor-zone-rect--editable {
  pointer-events: auto;
  cursor: move;
}

.floor-zone-rect--selected {
  border-style: solid;
  z-index: 3;
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--primary-color) 35%, transparent);
}

.floor-zone-resize-handle {
  position: absolute;
  width: 10px;
  height: 10px;
  background: var(--bg-primary, #fff);
  border: 2px solid var(--primary-color);
  border-radius: 2px;
  box-sizing: border-box;
  z-index: 4;
}

.floor-zone-resize-handle--nw { top: -6px; left: -6px; cursor: nwse-resize; }
.floor-zone-resize-handle--ne { top: -6px; right: -6px; cursor: nesw-resize; }
.floor-zone-resize-handle--sw { bottom: -6px; left: -6px; cursor: nesw-resize; }
.floor-zone-resize-handle--se { bottom: -6px; right: -6px; cursor: nwse-resize; }

.floor-zone-label {
  position: absolute;
  top: 6px;
  left: 8px;
  font-size: 12px;
  font-weight: 700;
  color: var(--text-primary);
}

.floor-zone-draw-preview {
  position: absolute;
  border: 2px dashed var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 15%, transparent);
  pointer-events: none;
  border-radius: 12px;
}

.floor-table-chip {
  position: absolute;
  transform: translate(-50%, -50%);
  box-sizing: border-box;
  min-width: var(--floor-table-chip-size, 3.5rem);
  width: var(--floor-table-chip-size, 3.5rem);
  height: var(--floor-table-chip-size, 3.5rem);
  padding: 0;
  border-radius: 14px;
  border: 2px solid rgba(255, 255, 255, 0.85);
  font-weight: 800;
  font-size: var(--floor-table-chip-font, 0.9375rem);
  line-height: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: grab;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.16);
  z-index: 2;
}

.floor-table-chip:active {
  cursor: grabbing;
}

.floor-table-chip--readonly {
  cursor: default;
}

.floor-table-chip--readonly:active {
  cursor: default;
}

.chip-avail {
  background: #16a34a;
  color: #fff;
}

.chip-occ {
  background: #dc2626;
  color: #fff;
}

.chip-res {
  background: var(--primary-color, #0e7490);
  color: #fff;
}

.chip-out {
  background: #64748b;
  color: #fff;
}

@media (max-width: 600px) {
  .fp-title {
    font-size: 22px;
  }

  .floor-canvas {
    min-height: 280px;
  }
}
</style>
