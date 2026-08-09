<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content stock-returns-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="arrow-return-left" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">
                    {{ $t("stockReturnsTitle") || "إرجاع مخزني" }}
                  </h1>
                  <p class="header-subtitle">
                    {{
                      $t("stockReturnsSubtitle") ||
                      "مرتجع مبيعات أو إرجاع يدوي لكمية كتالوج البيع"
                    }}
                  </p>
                </div>
              </div>
              <div class="app-header-actions">
                <button
                  type="button"
                  class="btn-refresh"
                  @click="loadHistory"
                  :disabled="historyLoading"
                >
                  <b-icon
                    icon="arrow-clockwise"
                    class="button-icon"
                    :class="{ spinning: historyLoading }"
                  ></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="journal-text"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="historyLoading"></b-spinner>
                  <template v-else>{{ historyTotal }}</template>
                </div>
                <div class="app-overview-stat-label">
                  {{ $t("stockReturnsOverviewTotal") || "إجمالي المرتجعات" }}
                </div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="receipt"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="historyLoading"></b-spinner>
                  <template v-else>{{ orderReturnCount }}</template>
                </div>
                <div class="app-overview-stat-label">
                  {{ $t("stockReturnsOverviewOrder") || "من فاتورة" }}
                </div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="box-arrow-in-down"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="historyLoading"></b-spinner>
                  <template v-else>{{ manualReturnCount }}</template>
                </div>
                <div class="app-overview-stat-label">
                  {{ $t("stockReturnsOverviewManual") || "يدوي" }}
                </div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="arrow-return-left"></b-icon>
                </div>
                <div>
                  <h2 class="app-section-title">
                    {{ $t("stockReturnsActionTitle") || "تنفيذ إرجاع" }}
                  </h2>
                  <p class="app-section-subtitle">
                    {{
                      $t("stockReturnsActionHint") ||
                      "اختر مرتجع فاتورة أو إرجاع يدوي للمخزون"
                    }}
                  </p>
                </div>
              </div>
            </div>

            <div class="reports-tabs-section stock-returns-tabs" role="tablist">
              <div class="reports-tabs">
                <button
                  type="button"
                  class="report-tab stock-returns-tab"
                  :class="{ 'report-tab-active stock-returns-tab--active': activeTab === 'order' }"
                  role="tab"
                  @click="activeTab = 'order'"
                >
                  <b-icon icon="receipt" class="me-1"></b-icon>
                  {{ $t("stockReturnsTabOrder") || "مرتجع فاتورة" }}
                </button>
                <button
                  type="button"
                  class="report-tab stock-returns-tab"
                  :class="{ 'report-tab-active stock-returns-tab--active': activeTab === 'manual' }"
                  role="tab"
                  @click="activeTab = 'manual'"
                >
                  <b-icon icon="box-arrow-in-down" class="me-1"></b-icon>
                  {{ $t("stockReturnsTabManual") || "إرجاع يدوي" }}
                </button>
              </div>
            </div>

            <div class="app-section-body">
              <!-- Order return -->
              <div v-show="activeTab === 'order'" class="stock-returns-panel">
                <div class="stock-returns-form-row">
                  <div class="users-search-container stock-returns-grow">
                    <b-icon icon="search" class="search-icon"></b-icon>
                    <input
                      v-model="orderCode"
                      type="search"
                      class="users-search-input"
                      :placeholder="
                        $t('stockReturnsOrderCodePlaceholder') ||
                        'رقم الفاتورة...'
                      "
                      autocomplete="off"
                      @keyup.enter="lookupOrder"
                    />
                  </div>
                  <button
                    type="button"
                    class="btn-refresh"
                    :disabled="orderLoading || !(orderCode || '').trim()"
                    @click="lookupOrder"
                  >
                    <b-icon
                      icon="search"
                      class="button-icon"
                      :class="{ spinning: orderLoading }"
                    ></b-icon>
                    <span class="button-text">{{ $t("search") || "بحث" }}</span>
                  </button>
                </div>

                <div v-if="orderLoading" class="stock-returns-loading">
                  <b-spinner></b-spinner>
                </div>
                <div v-else-if="orderForReturn" class="stock-returns-order-block">
                  <div class="stock-returns-order-meta">
                    <span>
                      <strong>{{ $t("orderCode") || "رقم الفاتورة" }}:</strong>
                      {{ orderForReturn.orderCode }}
                    </span>
                    <span>
                      <strong>{{ $t("date") || "التاريخ" }}:</strong>
                      {{ formatDate(orderForReturn.insertDate) }}
                    </span>
                    <span>
                      <strong>{{ $t("paymentMethod") || "الدفع" }}:</strong>
                      {{ orderForReturn.paymentMethod }}
                    </span>
                    <span>
                      <strong>{{ $t("warehouseName") || "المخزن" }}:</strong>
                      {{ orderForReturn.warehouseName || "—" }}
                    </span>
                  </div>

                  <div v-if="warehouses.length" class="stock-returns-form-row">
                    <div class="stock-returns-field stock-returns-grow">
                      <label class="stock-returns-label">
                        {{ $t("stockReturnsTargetWarehouse") || "مخزن الإرجاع" }}
                      </label>
                      <div class="users-search-container">
                        <b-icon icon="building" class="search-icon"></b-icon>
                        <select
                          v-model.number="orderReturnWarehouseId"
                          class="users-search-input reports-filter-select"
                        >
                          <option
                            v-for="wh in warehouses"
                            :key="'order-wh-' + wh.id"
                            :value="wh.id"
                          >
                            {{ wh.name }}
                            {{ wh.isDefault ? ($t("defaultWarehouse") || "(افتراضي)") : "" }}
                          </option>
                        </select>
                      </div>
                    </div>
                  </div>

                  <div class="table-responsive report-table-container">
                    <table class="table reports-table stock-returns-table">
                      <thead>
                        <tr>
                          <th>{{ $t("itemNamePlaceholder") || "اسم المنتج" }}</th>
                          <th>{{ $t("codePlaceholder") || "الكود" }}</th>
                          <th>{{ $t("stockReturnsSoldQty") || "مباع" }}</th>
                          <th>{{ $t("stockReturnsAlreadyReturned") || "مرتجع سابقاً" }}</th>
                          <th>{{ $t("stockReturnsReturnable") || "قابل للإرجاع" }}</th>
                          <th>{{ $t("stockReturnsReturnQty") || "كمية الإرجاع" }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="line in orderForReturn.lines" :key="line.itemId">
                          <td>{{ line.itemName }}</td>
                          <td>{{ line.itemCode || "—" }}</td>
                          <td>{{ line.soldQty }}</td>
                          <td>{{ line.alreadyReturnedQty }}</td>
                          <td>{{ line.returnableQty }}</td>
                          <td>
                            <input
                              v-model.number="returnQtyByItem[line.itemId]"
                              type="number"
                              min="0"
                              :max="line.returnableQty"
                              class="stock-returns-qty-input"
                              :disabled="line.returnableQty <= 0"
                            />
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>

                  <div class="stock-returns-notes-row">
                    <label class="stock-returns-label">
                      {{ $t("notesLabel") || "ملاحظات" }}
                    </label>
                    <input
                      v-model="orderReturnNotes"
                      type="text"
                      class="users-search-input"
                      :placeholder="
                        $t('stockReturnsNotesPlaceholder') || 'ملاحظات اختيارية'
                      "
                    />
                  </div>

                  <div class="stock-returns-actions">
                    <button
                      type="button"
                      class="btn-refresh stock-returns-submit"
                      :disabled="orderSubmitting || !hasOrderReturnLines"
                      @click="submitOrderReturn"
                    >
                      <b-icon
                        icon="check-circle-fill"
                        class="button-icon"
                        :class="{ spinning: orderSubmitting }"
                      ></b-icon>
                      <span class="button-text">
                        {{ $t("stockReturnsConfirmOrder") || "تأكيد مرتجع الفاتورة" }}
                      </span>
                    </button>
                  </div>
                </div>
                <div
                  v-else-if="orderLookupTried"
                  class="stock-returns-empty"
                >
                  <b-icon icon="receipt" class="stock-returns-empty-icon"></b-icon>
                  <p>
                    {{
                      $t("stockReturnsOrderNotFound") ||
                      "لم يتم العثور على الفاتورة"
                    }}
                  </p>
                </div>
              </div>

              <!-- Manual restock -->
              <div v-show="activeTab === 'manual'" class="stock-returns-panel">
                <div class="stock-returns-form-row">
                  <div v-if="warehouses.length" class="stock-returns-field">
                    <label class="stock-returns-label">
                      {{ $t("stockReturnsTargetWarehouse") || "مخزن الإرجاع" }}
                    </label>
                    <div class="users-search-container">
                      <b-icon icon="building" class="search-icon"></b-icon>
                      <select
                        v-model.number="manualWarehouseId"
                        class="users-search-input reports-filter-select"
                        @change="onManualWarehouseChange"
                      >
                        <option
                          v-for="wh in warehouses"
                          :key="'manual-wh-' + wh.id"
                          :value="wh.id"
                        >
                          {{ wh.name }}
                          {{ wh.isDefault ? ($t("defaultWarehouse") || "(افتراضي)") : "" }}
                        </option>
                      </select>
                    </div>
                  </div>
                  <div class="users-search-container stock-returns-grow">
                    <b-icon icon="search" class="search-icon"></b-icon>
                    <input
                      v-model="itemSearch"
                      type="search"
                      class="users-search-input"
                      :placeholder="
                        $t('stockReturnsItemSearchPlaceholder') ||
                        'ابحث بالاسم أو الكود...'
                      "
                      autocomplete="off"
                      @input="onItemSearchInput"
                    />
                  </div>
                </div>

                <div v-if="itemSearchLoading" class="stock-returns-loading">
                  <b-spinner small></b-spinner>
                </div>
                <div
                  v-else-if="itemResults.length"
                  class="stock-returns-item-results"
                >
                  <button
                    v-for="item in itemResults"
                    :key="item.id"
                    type="button"
                    class="stock-returns-item-option"
                    :class="{
                      'stock-returns-item-option--active':
                        selectedItem && selectedItem.id === item.id,
                    }"
                    @click="selectItem(item)"
                  >
                    <span class="stock-returns-item-name">{{ item.name }}</span>
                    <span class="stock-returns-item-meta">
                      {{ item.code || "—" }} ·
                      {{ $t("quantityLabel") || "الكمية" }}:
                      {{ item.quantity }}
                    </span>
                  </button>
                </div>

                <div v-if="selectedItem" class="stock-returns-manual-form">
                  <div class="stock-returns-order-meta">
                    <span>
                      <strong>{{ selectedItem.name }}</strong>
                    </span>
                    <span>{{ selectedItem.code || "—" }}</span>
                    <span>
                      {{ $t("stockReturnsCurrentQty") || "الكمية الحالية" }}:
                      {{ selectedItem.quantity }}
                    </span>
                  </div>
                  <div class="stock-returns-form-row">
                    <div class="stock-returns-field">
                      <label class="stock-returns-label">
                        {{ $t("stockReturnsReturnQty") || "كمية الإرجاع" }}
                      </label>
                      <input
                        v-model.number="manualQty"
                        type="number"
                        min="1"
                        class="users-search-input stock-returns-qty-input--wide"
                      />
                    </div>
                    <div class="stock-returns-field stock-returns-grow">
                      <label class="stock-returns-label">
                        {{ $t("notesLabel") || "ملاحظات" }}
                      </label>
                      <input
                        v-model="manualNotes"
                        type="text"
                        class="users-search-input"
                        :placeholder="
                          $t('stockReturnsNotesPlaceholder') ||
                          'ملاحظات اختيارية'
                        "
                      />
                    </div>
                  </div>
                  <div class="stock-returns-actions">
                    <button
                      type="button"
                      class="btn-refresh stock-returns-submit"
                      :disabled="manualSubmitting || !(manualQty > 0)"
                      @click="submitManualRestock"
                    >
                      <b-icon
                        icon="check-circle-fill"
                        class="button-icon"
                        :class="{ spinning: manualSubmitting }"
                      ></b-icon>
                      <span class="button-text">
                        {{ $t("stockReturnsConfirmManual") || "تأكيد الإرجاع اليدوي" }}
                      </span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="clock-history"></b-icon>
                </div>
                <div>
                  <h2 class="app-section-title">
                    {{ $t("stockReturnsHistoryTitle") || "سجل المرتجعات" }}
                  </h2>
                  <p class="app-section-subtitle">
                    {{
                      $t("stockReturnsHistoryHint") ||
                      "آخر عمليات الإرجاع المسجّلة"
                    }}
                  </p>
                </div>
              </div>
            </div>

            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                    <p>{{ $t("stockReturnsFiltersHint") || "تصفية سجل المرتجعات حسب النوع أو البحث" }}</p>
                  </div>
                </div>
                <div class="app-filters-panel-actions">
                  <button type="button" class="btn-refresh" @click="loadHistory">
                    <b-icon icon="search" class="button-icon"></b-icon>
                    <span class="button-text">{{ $t("search") || "بحث" }}</span>
                  </button>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--3">
                <label class="app-filter-field">
                  <span class="app-filter-label">{{ $t("stockReturnsType") || "النوع" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="funnel" class="search-icon"></b-icon>
                    <select
                      v-model="historyType"
                      class="users-search-input reports-filter-select"
                      @change="loadHistory"
                    >
                      <option value="">{{ $t("stockReturnsAllTypes") || "كل الأنواع" }}</option>
                      <option value="Order">{{ $t("stockReturnsTabOrder") || "مرتجع فاتورة" }}</option>
                      <option value="Manual">{{ $t("stockReturnsTabManual") || "إرجاع يدوي" }}</option>
                    </select>
                  </div>
                </label>
                <label class="app-filter-field" v-if="warehouses.length">
                  <span class="app-filter-label">{{ $t("warehouseName") || "المخزن" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="building" class="search-icon"></b-icon>
                    <select
                      v-model="historyWarehouseId"
                      class="users-search-input reports-filter-select"
                      @change="loadHistory"
                    >
                      <option value="">{{ $t("allWarehouses") || "كل المخازن" }}</option>
                      <option
                        v-for="wh in warehouses"
                        :key="'hist-wh-' + wh.id"
                        :value="String(wh.id)"
                      >
                        {{ wh.name }}
                      </option>
                    </select>
                  </div>
                </label>
                <label class="app-filter-field app-filter-field--grow">
                  <span class="app-filter-label">{{ $t("search") || "بحث" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="search" class="search-icon"></b-icon>
                    <input
                      v-model="historySearch"
                      type="search"
                      class="users-search-input"
                      :placeholder="$t('searchPlaceholder') || 'بحث'"
                      autocomplete="off"
                      @keyup.enter="loadHistory"
                    />
                  </div>
                </label>
              </div>
            </div>

            <div class="app-section-body app-section-body--no-padding">
              <div v-if="historyLoading" class="stock-returns-loading">
                <b-spinner></b-spinner>
              </div>
              <div v-else-if="!historyRows.length" class="stock-returns-empty">
                <b-icon icon="inbox" class="stock-returns-empty-icon"></b-icon>
                <p>
                  {{ $t("stockReturnsHistoryEmpty") || "لا توجد مرتجعات بعد" }}
                </p>
              </div>
              <div v-else class="table-responsive report-table-container">
                <table class="table reports-table stock-returns-table">
                  <thead>
                    <tr>
                      <th>{{ $t("date") || "التاريخ" }}</th>
                      <th>{{ $t("stockReturnsType") || "النوع" }}</th>
                      <th>{{ $t("warehouseName") || "المخزن" }}</th>
                      <th>{{ $t("itemNamePlaceholder") || "المنتج" }}</th>
                      <th>{{ $t("codePlaceholder") || "الكود" }}</th>
                      <th>{{ $t("quantityLabel") || "الكمية" }}</th>
                      <th>{{ $t("orderCode") || "الفاتورة" }}</th>
                      <th>{{ $t("notesLabel") || "ملاحظات" }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="row in historyRows" :key="row.id">
                      <td>{{ formatDate(row.insertDate) }}</td>
                      <td>
                        <span
                          class="stock-returns-badge"
                          :class="
                            row.returnType === 'Order'
                              ? 'stock-returns-badge--order'
                              : 'stock-returns-badge--manual'
                          "
                        >
                          {{ returnTypeLabel(row.returnType) }}
                        </span>
                      </td>
                      <td>{{ row.warehouseName || "—" }}</td>
                      <td>{{ row.itemName }}</td>
                      <td>{{ row.itemCode || "—" }}</td>
                      <td>{{ row.quantity }}</td>
                      <td>{{ row.orderCode || "—" }}</td>
                      <td>{{ row.notes || "—" }}</td>
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
  name: "StockReturnsView",
  components: { AppHeader },
  data() {
    return {
      activeTab: "order",
      orderCode: "",
      orderLoading: false,
      orderLookupTried: false,
      orderForReturn: null,
      returnQtyByItem: {},
      orderReturnNotes: "",
      orderReturnWarehouseId: null,
      orderSubmitting: false,
      warehouses: [],
      itemSearch: "",
      itemSearchTimer: null,
      itemSearchLoading: false,
      itemResults: [],
      selectedItem: null,
      manualWarehouseId: null,
      manualQty: 1,
      manualNotes: "",
      manualSubmitting: false,
      historyLoading: false,
      historyRows: [],
      historyTotal: 0,
      historyType: "",
      historyWarehouseId: "",
      historySearch: "",
      orderReturnCount: 0,
      manualReturnCount: 0,
    };
  },
  computed: {
    hasOrderReturnLines() {
      if (!this.orderForReturn?.lines) return false;
      return this.orderForReturn.lines.some((line) => {
        const qty = Number(this.returnQtyByItem[line.itemId]) || 0;
        return qty > 0 && qty <= line.returnableQty;
      });
    },
  },
  mounted() {
    this.loadWarehouses();
    this.loadHistory();
  },
  beforeDestroy() {
    clearTimeout(this.itemSearchTimer);
  },
  methods: {
    formatDate(value) {
      if (!value) return "—";
      try {
        return new Date(value).toLocaleString();
      } catch (_) {
        return String(value);
      }
    },
    returnTypeLabel(type) {
      if (type === "Order") {
        return this.$t("stockReturnsTabOrder") || "مرتجع فاتورة";
      }
      return this.$t("stockReturnsTabManual") || "إرجاع يدوي";
    },
    async loadWarehouses() {
      try {
        const res = await HTTP.get("Warehouses/ForPos");
        this.warehouses = (res.data?.data || []).map((w) => ({
          id: w.id ?? w.Id,
          name: w.name ?? w.Name,
          isDefault: !!(w.isDefault ?? w.IsDefault),
        }));
        const def = this.warehouses.find((w) => w.isDefault) || this.warehouses[0];
        if (!this.manualWarehouseId && def) {
          this.manualWarehouseId = def.id;
        }
      } catch (_) {
        this.warehouses = [];
      }
    },
    async lookupOrder() {
      const code = (this.orderCode || "").trim();
      if (!code) return;
      this.orderLoading = true;
      this.orderLookupTried = true;
      this.orderForReturn = null;
      this.returnQtyByItem = {};
      try {
        const response = await HTTP.get("Admin/GetOrderForReturn", {
          params: { orderCode: code },
        });
        if (response.data?.errorStatus) {
          this.$notify.error(
            response.data?.message || this.$t("error") || "حدث خطأ",
            { position: "top-right", timeout: 3000, maxToasts: 1 }
          );
          return;
        }
        const data = response.data?.data;
        this.orderForReturn = data || null;
        const qtyMap = {};
        (data?.lines || []).forEach((line) => {
          qtyMap[line.itemId] = 0;
        });
        this.returnQtyByItem = qtyMap;
        const whId = data?.warehouseId ?? data?.WarehouseId;
        if (whId) {
          this.orderReturnWarehouseId = Number(whId);
        } else {
          const def = this.warehouses.find((w) => w.isDefault) || this.warehouses[0];
          this.orderReturnWarehouseId = def?.id || null;
        }
      } catch (err) {
        const msg =
          err?.response?.data?.message ||
          this.$t("stockReturnsOrderNotFound") ||
          "لم يتم العثور على الفاتورة";
        this.$notify.error(msg, {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } finally {
        this.orderLoading = false;
      }
    },
    async submitOrderReturn() {
      if (!this.orderForReturn || !this.hasOrderReturnLines) return;
      const lines = this.orderForReturn.lines
        .map((line) => ({
          itemId: line.itemId,
          quantity: Number(this.returnQtyByItem[line.itemId]) || 0,
        }))
        .filter((l) => l.quantity > 0);

      for (const line of lines) {
        const src = this.orderForReturn.lines.find((x) => x.itemId === line.itemId);
        if (!src || line.quantity > src.returnableQty) {
          this.$notify.error(
            this.$t("stockReturnsQtyExceeds") ||
              "كمية الإرجاع تتجاوز المتاح",
            { position: "top-right", timeout: 3000, maxToasts: 1 }
          );
          return;
        }
      }

      this.orderSubmitting = true;
      try {
        const response = await HTTP.post("Admin/ReturnFromOrder", {
          orderId: this.orderForReturn.orderId,
          notes: this.orderReturnNotes || null,
          warehouseId: this.orderReturnWarehouseId || null,
          lines,
        });
        if (response.data?.errorStatus) {
          this.$notify.error(
            response.data?.message || this.$t("error") || "حدث خطأ",
            { position: "top-right", timeout: 3000, maxToasts: 1 }
          );
          return;
        }
        this.$notify.success(
          response.data?.message ||
            this.$t("stockReturnsSuccess") ||
            "تم تسجيل المرتجع بنجاح",
          { position: "top-right", timeout: 2500, maxToasts: 1 }
        );
        this.orderReturnNotes = "";
        await this.lookupOrder();
        await this.loadHistory();
      } catch (err) {
        this.$notify.error(
          err?.response?.data?.message || this.$t("error") || "حدث خطأ",
          { position: "top-right", timeout: 3000, maxToasts: 1 }
        );
      } finally {
        this.orderSubmitting = false;
      }
    },
    onItemSearchInput() {
      clearTimeout(this.itemSearchTimer);
      this.itemSearchTimer = setTimeout(() => this.searchItems(), 350);
    },
    onManualWarehouseChange() {
      this.selectedItem = null;
      if ((this.itemSearch || "").trim()) {
        this.searchItems();
      }
    },
    async searchItems() {
      const q = (this.itemSearch || "").trim();
      if (q.length < 1) {
        this.itemResults = [];
        return;
      }
      this.itemSearchLoading = true;
      try {
        const params = new URLSearchParams({
          pageNumber: "0",
          pageSize: "20",
          info: q,
        });
        if (this.manualWarehouseId) {
          params.set("warehouseId", String(this.manualWarehouseId));
        }
        const response = await HTTP.get(`Admin/GetItems?${params.toString()}`);
        this.itemResults = response.data?.data?.items || [];
      } catch (_) {
        this.itemResults = [];
      } finally {
        this.itemSearchLoading = false;
      }
    },
    selectItem(item) {
      this.selectedItem = item;
      this.manualQty = 1;
      this.manualNotes = "";
    },
    async submitManualRestock() {
      if (!this.selectedItem || !(this.manualQty > 0)) return;
      if (this.warehouses.length && !this.manualWarehouseId) {
        this.$notify.error(
          this.$t("warehouseRequired") || "اختر المخزن",
          { position: "top-right", timeout: 3000, maxToasts: 1 }
        );
        return;
      }
      this.manualSubmitting = true;
      try {
        const response = await HTTP.post("Admin/RestockItem", {
          itemId: this.selectedItem.id,
          quantity: Number(this.manualQty),
          notes: this.manualNotes || null,
          warehouseId: this.manualWarehouseId || null,
        });
        if (response.data?.errorStatus) {
          this.$notify.error(
            response.data?.message || this.$t("error") || "حدث خطأ",
            { position: "top-right", timeout: 3000, maxToasts: 1 }
          );
          return;
        }
        this.$notify.success(
          response.data?.message ||
            this.$t("stockReturnsSuccess") ||
            "تم الإرجاع بنجاح",
          { position: "top-right", timeout: 2500, maxToasts: 1 }
        );
        const newQty = response.data?.data?.newQuantity;
        if (typeof newQty === "number") {
          this.selectedItem = { ...this.selectedItem, quantity: newQty };
        }
        this.manualQty = 1;
        this.manualNotes = "";
        await this.loadHistory();
        if ((this.itemSearch || "").trim()) {
          await this.searchItems();
        }
      } catch (err) {
        this.$notify.error(
          err?.response?.data?.message || this.$t("error") || "حدث خطأ",
          { position: "top-right", timeout: 3000, maxToasts: 1 }
        );
      } finally {
        this.manualSubmitting = false;
      }
    },
    async loadHistory() {
      this.historyLoading = true;
      try {
        const params = {
          pageNumber: 0,
          pageSize: 50,
        };
        if ((this.historySearch || "").trim()) {
          params.info = this.historySearch.trim();
        }
        if (this.historyType) {
          params.returnType = this.historyType;
        }
        if (this.historyWarehouseId) {
          params.warehouseId = Number(this.historyWarehouseId);
        }
        const response = await HTTP.get("Admin/GetStockReturns", { params });
        const data = response.data?.data;
        this.historyRows = data?.items || [];
        this.historyTotal = data?.totalItems ?? this.historyRows.length;

        const countsRes = await HTTP.get("Admin/GetStockReturns", {
          params: { pageNumber: 0, pageSize: 1 },
        });
        this.historyTotal = countsRes.data?.data?.totalItems ?? this.historyTotal;

        const [orderRes, manualRes] = await Promise.all([
          HTTP.get("Admin/GetStockReturns", {
            params: { pageNumber: 0, pageSize: 1, returnType: "Order" },
          }),
          HTTP.get("Admin/GetStockReturns", {
            params: { pageNumber: 0, pageSize: 1, returnType: "Manual" },
          }),
        ]);
        this.orderReturnCount = orderRes.data?.data?.totalItems ?? 0;
        this.manualReturnCount = manualRes.data?.data?.totalItems ?? 0;
      } catch (_) {
        this.historyRows = [];
        this.historyTotal = 0;
        this.orderReturnCount = 0;
        this.manualReturnCount = 0;
      } finally {
        this.historyLoading = false;
      }
    },
  },
};
</script>

<style scoped>
.stock-returns-tabs.reports-tabs-section {
  margin: 0.85rem 1.25rem 0.5rem;
}

.stock-returns-panel {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.stock-returns-form-row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: flex-end;
}

.stock-returns-grow {
  flex: 1 1 16rem;
  min-width: 0;
}

.stock-returns-field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 8rem;
}

.stock-returns-label {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.stock-returns-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem 1rem;
  color: var(--text-muted);
}

.stock-returns-order-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 0.75rem;
  font-size: 0.9rem;
  color: var(--text-secondary);
}

.stock-returns-qty-input {
  width: 5.5rem;
  padding: 0.35rem 0.5rem;
  border: 1px solid var(--border-color);
  border-radius: 0.45rem;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.stock-returns-qty-input--wide {
  width: 8rem;
}

.stock-returns-notes-row {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-top: 0.75rem;
}

.stock-returns-actions {
  display: flex;
  justify-content: flex-start;
  margin-top: 0.5rem;
}

.stock-returns-submit {
  min-width: 12rem;
}

.stock-returns-item-results {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  max-height: 16rem;
  overflow: auto;
}

.stock-returns-item-option {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.15rem;
  width: 100%;
  padding: 0.65rem 0.85rem;
  border: 1px solid var(--border-color);
  border-radius: 0.65rem;
  background: var(--bg-secondary);
  color: var(--text-primary);
  text-align: start;
  cursor: pointer;
}

.stock-returns-item-option--active {
  border-color: color-mix(in srgb, var(--primary-color) 55%, var(--border-color));
  background: color-mix(in srgb, var(--primary-color) 12%, var(--bg-primary));
}

.stock-returns-item-name {
  font-weight: 700;
}

.stock-returns-item-meta {
  font-size: 0.8rem;
  color: var(--text-muted);
}

.stock-returns-manual-form {
  margin-top: 0.5rem;
  padding-top: 0.75rem;
  border-top: 1px solid var(--border-color);
}

.stock-returns-badge {
  display: inline-flex;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
}

.stock-returns-badge--order {
  background: color-mix(in srgb, #f59e0b 18%, transparent);
  color: #d97706;
}

.stock-returns-badge--manual {
  background: color-mix(in srgb, #10b981 18%, transparent);
  color: #059669;
}

.stock-returns-filters {
  margin-top: 1rem;
  padding: 0 1.25rem 0.75rem;
}

.spinning {
  animation: stock-returns-spin 0.8s linear infinite;
}

@keyframes stock-returns-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
