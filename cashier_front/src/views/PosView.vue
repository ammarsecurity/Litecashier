<template>
  <div>
    <b-overlay
      :show="show"
      spinner-variant="danger"
      spinner-type="grow"
      spinner-large
      rounded="sm"
    >
      <SidebarView />
      <div class="main-content-wrapper">
        <b-container fluid class="pos-container-fluid">
          <div class="pos-page-container">
            <!-- Left Side: Products -->
            <div class="pos-main-section">
            <!-- Header Section -->
            <div class="pos-header-section">
              <div class="pos-header-top">
                <div class="pos-logo-section">
                  <img src="../assets/logoarabic.png" alt="logo" class="pos-logo" />
                </div>
                <div class="pos-employee-info">
                  <b-icon icon="person-circle" class="me-2"></b-icon>
                  <span class="pos-employee-label">{{ $t("employeeLabel") }}</span>
                  <span class="pos-employee-name">{{ userInfo.name }}</span>
                </div>
              </div>
            </div>

            <!-- Quick Actions Bar -->
            <div class="pos-quick-actions">
              <div class="pos-quick-search">
                <b-icon icon="search" class="pos-quick-search-icon"></b-icon>
                <input
                  v-model="search.info"
                  type="search"
                  :placeholder="$t('searchPlaceholder')"
                  class="pos-quick-search-input"
                />
              </div>
              <div class="pos-quick-barcode">
                <b-icon icon="upc-scan" class="me-2"></b-icon>
                <input
                  v-model="searchCode"
                  ref="codeNumber"
                  type="text"
                  :placeholder="$t('itemCodeLabel') || 'مسح الباركود أو QR'"
                  class="pos-quick-barcode-input"
                  autofocus
                  @keyup.enter="handleBarcodeSearch"
                  @input="handleBarcodeInput"
                />
              </div>
            </div>

            <!-- Categories Section -->
            <div class="pos-categories-scroll">
                <div class="pos-categories-list">
                  <button
                    v-for="tag in tags"
                    :key="tag.id"
                    class="pos-category-btn"
                    :class="{ 'pos-category-btn-active': search.info === tag.name }"
                    @click="search.info = tag.name"
                  >
                    {{ tag.name }}
                  </button>
                  <button
                    class="pos-category-btn"
                    :class="{ 'pos-category-btn-active': search.info === '' }"
                    @click="search.info = ''"
                  >
                    {{ $t("all") }}
                  </button>
              </div>
            </div>

            <!-- Products Grid -->
            <div class="pos-products-grid-section">
              <div class="pos-products-grid">
                <div
                  class="pos-product-card"
                  :class="{ 'pos-product-card-disabled': !item.quantity || item.quantity <= 0 }"
                  v-for="item in Items"
                  :key="item.id"
                  @click="item.quantity > 0 ? addToCartList(item) : null"
                >
                  <!-- Discount Badge -->
                  <div
                    v-if="item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
                    class="pos-product-discount-badge"
                  >
                    <b-icon icon="tag-fill" class="me-1"></b-icon>
                    {{ $t("discountLabel") }}
                  </div>

                  <!-- Product Image/Barcode -->
                  <div class="pos-product-media">
                    <vue-barcode
                      v-if="showbarCode"
                      ref="BarImg"
                      tag="img"
                      class="pos-product-barcode"
                      :value="item.code.toString()"
                      :options="{
                        displayValue: true,
                        lineColor: '#2B2B2C',
                        width: 1.5,
                        height: 60,
                      }"
                    />
                    <div v-else class="pos-product-image-container">
                      <img
                        v-if="item.image && !item.imageError"
                        :src="item.image"
                        :alt="item.name"
                        class="pos-product-image"
                        @error="item.imageError = true"
                      />
                      <div v-else class="pos-product-image-placeholder">
                        <b-icon icon="box-fill" class="pos-product-placeholder-icon"></b-icon>
                      </div>
                    </div>
                  </div>

                  <!-- Product Info -->
                  <div class="pos-product-info">
                    <h4 class="pos-product-name">{{ item.name }}</h4>
                    <div class="pos-product-meta">
                      <div class="pos-product-category">
                        <b-icon icon="tags" class="me-1"></b-icon>
                        {{ item.tags }}
                      </div>
                      <div class="pos-product-price">
                        <div
                          v-if="item.disCountPrice !== 0 && item.disCountPrice !== item.sellingPrice"
                          class="pos-product-price-discounted"
                        >
                          <span class="pos-product-price-current">
                            {{ formatPrice(item.disCountPrice) }} {{ $t("currency") }}
                          </span>
                          <span class="pos-product-price-old">
                            {{ formatPrice(item.sellingPrice) }} {{ $t("currency") }}
                          </span>
                        </div>
                        <div v-else class="pos-product-price-regular">
                          {{ formatPrice(item.sellingPrice) }} {{ $t("currency") }}
                        </div>
                      </div>
                    </div>
                    <div class="pos-product-add-badge" v-if="item.quantity && item.quantity > 0">
                      <b-icon icon="plus-circle-fill" class="me-1"></b-icon>
                      {{ $t("addButton") || "أضف" }}
                    </div>
                    <div class="pos-product-out-of-stock-badge" v-if="!item.quantity || item.quantity <= 0">
                      <b-icon icon="x-circle-fill" class="me-1"></b-icon>
                      {{ $t("itemOutOfStock") || "غير متوفر" }}
                    </div>
                  </div>
                </div>
              </div>

              <!-- Pagination -->
              <div class="pos-pagination-section">
                <b-pagination
                  v-model="pageNumber"
                  :total-rows="totalItems"
                  :per-page="pageSize"
                  aria-controls="pos-products"
                  class="pos-pagination"
                >
                </b-pagination>
              </div>
            </div>
            </div>

            <!-- Empty Cart Modal -->
            <b-modal id="modal-empty" :title="$t('confirmClearCartTitle')" hide-header hide-footer class="users-modal">
              <div class="modal-content-wrapper">
                <div class="delete-confirmation-content">
                  <div class="delete-icon-wrapper">
                    <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
                  </div>
                  <h3 class="delete-confirmation-title">{{ $t("confirmClearCartTitle") }}</h3>
                  <p class="delete-confirmation-text">{{ $t("confirmClearCartMessage") }}</p>
                  <div class="delete-confirmation-actions">
                    <button class="delete-confirm-button" @click="EmptycardList('modal-empty')">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      {{ $t("confirmButton") }}
                    </button>
                    <button class="delete-cancel-button" @click="closeModel('modal-empty')">
                      <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                      {{ $t("cancelButton") }}
                    </button>
                  </div>
                </div>
              </div>
            </b-modal>

            <!-- Cart Section -->
            <div class="pos-cart-section">
              <div class="pos-cart-container">
                <!-- Cart Items List -->
                <div class="pos-cart-items-section">
                  <div class="pos-cart-header">
                    <h3 class="pos-cart-title">
                      <b-icon icon="cart-fill" class="me-2"></b-icon>
                      {{ $t("cart") || 'السلة' }}
                    </h3>
                    <span class="pos-cart-count-badge" v-if="carditems.length > 0">
                      {{ carditems.length }}
                    </span>
                  </div>
                  <div class="pos-cart-items-list" v-if="carditems.length > 0">
                    <div
                      class="pos-cart-item"
                      v-for="(item, index) in carditems"
                      :key="index"
                    >
                      <!-- Item Name and Price -->
                      <div class="pos-cart-item-info">
                        <h4 class="pos-cart-item-name">{{ item.name }}</h4>
                        <div class="pos-cart-item-price-row">
                          <span class="pos-cart-item-price">
                            {{ formatPrice(item.price !== item.disCountPrice ? item.disCountPrice : item.price) }} {{ $t("currency") }}
                          </span>
                          <span class="pos-cart-item-total">
                            {{ formatPrice(item.total) }} {{ $t("currency") }}
                          </span>
                        </div>
                      </div>
                      
                      <!-- Quantity Controls and Delete -->
                      <div class="pos-cart-item-controls">
                        <div class="pos-cart-item-quantity">
                          <button
                            class="pos-quantity-btn pos-quantity-decrease"
                            @click.stop="decreaseQuantity(index)"
                            :title="$t('decrease') || 'تقليل'"
                          >
                            <b-icon icon="dash-lg"></b-icon>
                          </button>
                          <input
                            type="number"
                            :value="item.quantity"
                            @input="updateQuantity(index, $event.target.value)"
                            @click.stop
                            class="pos-quantity-input"
                            min="1"
                          />
                          <button
                            class="pos-quantity-btn pos-quantity-increase"
                            @click.stop="increaseQuantity(index)"
                            :title="$t('increase') || 'زيادة'"
                          >
                            <b-icon icon="plus-lg"></b-icon>
                          </button>
                        </div>
                        <button
                          class="pos-cart-item-delete"
                          @click.stop="deleteItem(index)"
                          :title="$t('delete') || 'حذف'"
                        >
                          <b-icon icon="x-lg"></b-icon>
                        </button>
                      </div>
                    </div>
                  </div>
                  <div class="pos-cart-empty" v-else>
                    <b-icon icon="cart-x" class="pos-cart-empty-icon"></b-icon>
                    <p class="pos-cart-empty-text">{{ $t("emptyCart") || 'السلة فارغة' }}</p>
                  </div>
                </div>

                <!-- Cart Summary -->
                <div class="pos-cart-summary" v-if="carditems.length > 0">
                  <div class="pos-cart-summary-row">
                    <span class="pos-cart-summary-label">
                      <b-icon icon="box-seam" class="me-2"></b-icon>
                      {{ $t("countLabel") }}:
                    </span>
                    <span class="pos-cart-summary-value">{{ totalCardItems }} {{ $t("itemLabel") }}</span>
                  </div>
                  <div class="pos-cart-summary-row pos-cart-total-row">
                    <span class="pos-cart-summary-label">
                      <b-icon icon="currency-dollar" class="me-2"></b-icon>
                      {{ $t("totalLabel") }}:
                    </span>
                    <span class="pos-cart-summary-value pos-cart-total-value">
                      {{ formattedNumber }} {{ $t("currency") }}
                    </span>
                  </div>
                </div>

                <!-- Order Type Selection -->
                <div class="pos-printer-section" v-if="carditems.length > 0">
                  <div class="pos-printer-header">
                    <b-icon icon="shop" class="me-2"></b-icon>
                    <span>{{ $t("orderType") || "نوع الطلب" }}</span>
                  </div>
                  <div class="pos-order-types-grid">
                    <button
                      class="pos-order-type-btn"
                      :class="{ 'pos-order-type-active': orderForSend.orderType === 'Takeaway' }"
                      @click="orderForSend.orderType = 'Takeaway'"
                    >
                      <b-icon icon="bag" class="pos-order-type-icon"></b-icon>
                      <span class="pos-order-type-label">{{ $t("takeaway") || "طلب خارجي" }}</span>
                    </button>
                      <button
                      class="pos-order-type-btn"
                        :class="{ 'pos-order-type-active': orderForSend.orderType === 'Delivery' }"
                        @click="orderForSend.orderType = 'Delivery'"
                      >
                        <b-icon icon="truck" class="pos-order-type-icon"></b-icon>
                        <span class="pos-order-type-label">{{ $t("delivery") || "توصيل" }}</span>
                      </button>
                  </div>
                </div>

                <!-- Payment Method Selection -->
                <div class="pos-printer-section" v-if="carditems.length > 0">
                  <div class="pos-printer-header">
                    <b-icon icon="credit-card-fill" class="me-2"></b-icon>
                    <span>{{ $t("paymentMethod") || "طريقة الدفع" }}</span>
                  </div>
                  <div class="pos-payment-methods-grid">
                    <button
                      class="pos-payment-method-btn"
                      :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Cash' }"
                      @click="orderForSend.paymentMethod = 'Cash'"
                    >
                      <b-icon icon="cash-stack" class="pos-payment-icon"></b-icon>
                      <span class="pos-payment-label">{{ $t("cash") || "نقد" }}</span>
                    </button>
                    <button
                      class="pos-payment-method-btn"
                      :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Card' }"
                      @click="orderForSend.paymentMethod = 'Card'"
                    >
                      <b-icon icon="credit-card" class="pos-payment-icon"></b-icon>
                      <span class="pos-payment-label">{{ $t("card") || "بطاقة" }}</span>
                    </button>
                    <button
                      class="pos-payment-method-btn"
                      :class="{ 'pos-payment-method-active': orderForSend.paymentMethod === 'Credit' }"
                      @click="orderForSend.paymentMethod = 'Credit'"
                    >
                      <b-icon icon="clock-history" class="pos-payment-icon"></b-icon>
                      <span class="pos-payment-label">{{ $t("credit") || "دفع لاحق" }}</span>
                    </button>
                  </div>
                </div>

                <!-- Printer Selection Section -->
                <div class="pos-printer-section" v-if="availablePrinters.length > 0 || webPrintAPISupported">
                  <div class="pos-printer-header">
                    <b-icon icon="printer-fill" class="me-2"></b-icon>
                    <span>{{ $t("printerSettings") || "إعدادات الطابعة" }}</span>
                  </div>
                  
                  <!-- Web Print API Support Status -->
                  <div class="pos-printer-status" v-if="webPrintAPISupported">
                    <div class="pos-printer-status-badge pos-printer-status-supported">
                      <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                      <span>{{ $t("webPrintAPISupported") || "المتصفح يدعم الطباعة المباشرة" }}</span>
                    </div>
                  </div>
                  <div class="pos-printer-status" v-else>
                    <div class="pos-printer-status-badge pos-printer-status-not-supported">
                      <b-icon icon="info-circle-fill" class="me-2"></b-icon>
                      <span>{{ $t("webPrintAPINotSupported") || "سيتم استخدام نافذة الطباعة العادية" }}</span>
                    </div>
                  </div>

                  <!-- Printer Selection Dropdown -->
                  <div class="pos-printer-select-wrapper" v-if="availablePrinters.length > 0">
                    <label class="pos-printer-select-label">
                      {{ $t("selectPrinter") || "اختر الطابعة" }}
                    </label>
                    <select 
                      v-model="selectedPrinterId" 
                      @change="onPrinterChange"
                      class="pos-printer-select"
                    >
                      <option 
                        v-for="printer in availablePrinters" 
                        :key="printer.id" 
                        :value="printer.id"
                      >
                        {{ printer.name }} {{ printer.isDefault ? ' (افتراضي)' : '' }}
                      </option>
                    </select>
                  </div>
                  <div class="pos-printer-select-wrapper" v-else-if="webPrintAPISupported">
                    <label class="pos-printer-select-label">
                      {{ $t("loadingPrinters") || "جاري تحميل الطابعات..." }}
                    </label>
                  </div>
                </div>

                <!-- Cart Actions -->
                <div class="pos-cart-actions">
                  <button
                    class="pos-action-btn pos-action-btn-primary"
                    @click="addOrderAndClear"
                    :disabled="totalCardItems <= 0"
                  >
                    <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                    {{ $t("saveAndClear") || "حفظ وافراغ" }}
                  </button>
                  <button
                    class="pos-action-btn pos-action-btn-danger"
                    v-b-modal.modal-empty
                    :disabled="totalCardItems <= 0"
                  >
                    <b-icon icon="trash-fill" class="me-2"></b-icon>
                    {{ $t("emptyButton") || "افراغ فقط" }}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </b-container>
      </div>
      <b-sidebar id="sidebar-right" title="Sidebar" no-header right shadow>
        <div class="px-3 py-2">
          <CalculatorComp />
        </div>
      </b-sidebar>
    </b-overlay>

    <!-- Print Section (Hidden) -->
    <div class="print_hide" id="print" style="display: none;">
      <div class="bill-container">
        <!-- Header Section -->
        <div class="bill-header">
          <div class="bill-logo-section">
            <img
              v-if="commercialUserInfo.logo"
              :src="commercialUserInfo.logo"
              alt="logo"
              class="bill-logo-img"
            />
            <img
              v-else
              src="../assets/logoarabic.png"
              alt="logo"
              class="bill-logo-img"
            />
          </div>
          <div class="bill-store-info">
            <h2 class="bill-store-name">{{ commercialUserInfo.storeName || 'LiteCashier' }}</h2>
            <p class="bill-store-subtitle">{{ $t("app-name") }}</p>
          </div>
        </div>

        <!-- Invoice Info Section -->
        <div class="bill-info-section">
          <div class="bill-info-row">
            <span class="bill-info-label">{{ $t("invoice_number") }}:</span>
            <span class="bill-info-value">{{ orderForSend.orderCode || '---' }}</span>
          </div>
          image.png          <!-- Barcode for Order Number -->
          <div class="bill-barcode-section" v-if="orderForSend.orderCode">
            <vue-barcode
              tag="img"
              class="bill-barcode-img"
              :value="orderForSend.orderCode.toString()"
              :options="{
                displayValue: true,
                fontSize: 12,
                height: 40,
                width: 1.5,
                margin: 0
              }"
            />
          </div>
          <div class="bill-info-row">
            <span class="bill-info-label">{{ $t("employeeLabel") }}:</span>
            <span class="bill-info-value">{{ userInfo.name || userInfo.fullName || '---' }}</span>
          </div>
          <div class="bill-info-row" v-if="orderForSend.orderType">
            <span class="bill-info-label">{{ $t("orderType") }}:</span>
            <span class="bill-info-value">{{ getOrderTypeText(orderForSend.orderType) }}</span>
          </div>
          <div class="bill-info-row" v-if="orderForSend.paymentMethod">
            <span class="bill-info-label">{{ $t("paymentMethod") }}:</span>
            <span class="bill-info-value">{{ getPaymentMethodText(orderForSend.paymentMethod) }}</span>
          </div>
          <div class="bill-info-row">
            <span class="bill-info-label">{{ $t("from_date") }}:</span>
            <span class="bill-info-value">{{ getCurrentDateTime() }}</span>
          </div>
        </div>

        <div class="bill-divider"></div>

        <!-- Items Table -->
        <table class="bill-table">
          <thead>
            <tr class="bill-table-header">
              <th class="bill-table-cell bill-col-item">{{ $t("itemLabel") }}</th>
              <th class="bill-table-cell bill-col-qty">{{ $t("countLabel") }}</th>
              <th class="bill-table-cell bill-col-price">{{ $t("price") }}</th>
              <th class="bill-table-cell bill-col-total">{{ $t("totalLabel") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(item, index) in carditems" :key="index" class="bill-table-row">
              <td class="bill-table-cell bill-col-item">{{ item.name }}</td>
              <td class="bill-table-cell bill-col-qty">{{ item.quantity }}</td>
              <td class="bill-table-cell bill-col-price">
                {{ formatPrice(item.price !== item.disCountPrice ? item.disCountPrice : item.price) }}
              </td>
              <td class="bill-table-cell bill-col-total">
                {{ formatPrice((item.price !== item.disCountPrice ? item.disCountPrice : item.price) * item.quantity) }}
              </td>
            </tr>
          </tbody>
        </table>

        <!-- Summary Section -->
        <div class="bill-summary-section">
          <div class="bill-summary-row">
            <span class="bill-summary-label">{{ $t("countLabel") }}:</span>
            <span class="bill-summary-value">{{ totalCardItems }} {{ $t("itemLabel") }}</span>
          </div>
          <div class="bill-summary-row bill-total-row">
            <span class="bill-summary-label">{{ $t("totalLabel") }}:</span>
            <span class="bill-summary-value bill-total-amount">
              {{ formattedNumber }} {{ $t("currency") }}
            </span>
          </div>
        </div>

        <!-- Footer Section -->
        <div class="bill-footer">
          <p class="bill-footer-text">{{ $t("thankYouMessage") || "شكراً لزيارتك" }}</p>
          <p class="bill-footer-date">{{ getCurrentDate() }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import SidebarView from "@/components/Layout/SidebarView.vue";
import CalculatorComp from "@/components/CalculatorComp.vue";
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";
import { HTTP } from "../http/api.js";
import { htmlToPaper } from 'vue-html-to-paper';
// import store from '../store/store'; // Adjust the path based on your actual folder structure

export default {
  name: "PosView",
  components: {
    SidebarView,
    ClockVue,
    "vue-barcode": VueBarcode,
    CalculatorComp,
  },
  data() {
    return {
      showbarCode: false,
      show: false,
      totaPrice: 0,
      carditems: [],
      typingTimer: null,
      doneTypingInterval: 500,
      isSearching: false,
      searchAbortController: null,
      lastAddedItem: null,
      itemsAddedCount: 0,
      addItemTimer: null,
      selectedPrinter: null,
      selectedPrinterId: null,
      availablePrinters: [],
      webPrintAPISupported: false,
      Items: [],
      tags: [],
      pageNumber: 1,
      totalItems: 0,
      pageSize: 12,
      search: {
        info: "",
      },
      searchCode: "",
      SearchItems: [],

      totalCardItems: 0,
      userInfo: {},
      commercialUserInfo: {
        storeName: 'LiteCashier',
        logo: null
      },
      orderForSend: {
        orderCode: "",
        paymentMethod: "Cash",
        customerOrderItem: [],
        orderType: "Takeaway"
      },
    };
  },

  computed: {
    formattedNumber() {
      return this.totaPrice.toLocaleString();
    },
    cardfields() {
      const lang = this.$i18n.locale;
      if (!lang) {
        return [];
      }
      return [
        {
          key: "name",
          label: this.$i18n.t("itemLabel"),
        },
        {
          key: "quantity",
          label: this.$i18n.t("countLabel"),
        },
        {
          key: "price",
          label: this.$i18n.t("price"),
        },
        {
          key: "total",
          label: this.$i18n.t("total"),
        },
        {
          key: "actions",
          label: this.$i18n.t("actions"),
        },
      ];
    },
    posCardFields() {
      const lang = this.$i18n.locale;
      if (!lang) {
        return [];
      }
      return [
        {
          key: "name",
          label: this.$i18n.t("itemLabel"),
        },
        {
          key: "quantity",
          label: this.$i18n.t("countLabel"),
        },
        {
          key: "price",
          label: this.$i18n.t("price"),
        },
        {
          key: "total",
          label: this.$i18n.t("total"),
        },
      ];
    },
  },
  watch: {
    carditems: {
      handler() {
        this.totaPrice = 0;
        this.carditems.forEach((item) => {
          // Ensure total is calculated if missing
          if (item.total === undefined || isNaN(item.total)) {
            const finalPrice = item.price !== item.disCountPrice ? item.disCountPrice : item.price;
            item.total = finalPrice * (item.quantity || 1);
          }
          this.totaPrice += item.total || 0;
        });
        this.totalCardItems = this.carditems.length;
      },
      deep: true,
    },
    search: {
      handler() {
        this.GetAllItems();
      },
      deep: true,
    },
    pageNumber() {
      this.GetAllItems();
    },

  },

  mounted() {
    try {
      this.getTags();
      this.$nextTick(() => {
        if (this.$refs.codeNumber) {
          this.$refs.codeNumber.focus();
        }
      });
      this.GetAllItems();
      
      const userInfoStr = localStorage.getItem("info");
      if (userInfoStr) {
        this.userInfo = JSON.parse(userInfoStr);
      }

      // Load commercial user info for printing
      this.loadCommercialUserInfo();

      // Initialize printers on mount
      this.initializePrinters();
      
      // Add keyboard shortcut listener
      this.handleKeyup = (e) => {
        if (e.ctrlKey && e.keyCode === 38) {
          this.$root.$emit("bv::toggle::collapse", "sidebar-right");
        }
      };
      window.addEventListener("keyup", this.handleKeyup);
    } catch (error) {
      this.$toast.error(this.$i18n.t("error") || "An error occurred", {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    }
  },
  
  beforeDestroy() {
    // Cleanup: Remove event listener
    if (this.handleKeyup) {
      window.removeEventListener("keyup", this.handleKeyup);
    }
  },

  methods: {
    loadCommercialUserInfo() {
      // CommercialUserInfo endpoint is not available in cashier_back
      // Using default values instead
      this.commercialUserInfo = {
        storeName: 'LiteCashier',
        logo: null
      };
    },
    getTags() {
      HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`)
        .then((response) => {
          this.tags = response.data.data.items;
        })
        .catch((error) => {
          this.$toast.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          });
        });
    },
    formatPrice(price) {
      if (price) {
        return price.toLocaleString("en-EG");
      }
      return "";
    },
    addOrderAndClear() {
      const textDirection = document.documentElement.dir;
      const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      this.show = true;
      this.orderForSend.orderCode = "";
      this.orderForSend.paymentMethod = this.orderForSend.paymentMethod || "Cash";
      this.orderForSend.customerOrderItem = [];
      for (const item of this.carditems) {
        this.orderForSend.customerOrderItem.push({
          itemId: item.id,
          quantity: item.quantity,
        });
      }
      this.orderForSend.orderCode = Math.floor(
        Math.random() * 1000000000
      ).toString().padStart(9, '0');
      
      HTTP.post(`Admin/AddOrder`, this.orderForSend)
        .then((response) => {
          if (response) {
            this.show = false;
            // Save a copy of carditems for printing before clearing
            const itemsForPrint = JSON.parse(JSON.stringify(this.carditems));
            // Clear cart after successful save
            this.carditems = [];
            this.orderForSend.orderType = 'Takeaway'; // Reset to default when clearing
            
            this.$toast.success(this.$i18n.t("orderSavedAndCleared") || "تم حفظ الطلب وافراغ السلة بنجاح", {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            });
            
            // Print automatically after saving
            setTimeout(() => {
              try {
                this.printCard(itemsForPrint);
              } catch (printError) {
                console.error('Print error:', printError);
                // Don't show error to user, printing is optional
                // The order was saved successfully
              }
            }, 100);
          }
        })
        .catch((error) => {
          this.show = false;
          console.error('Order save error:', error);
          let errorMessage = this.$i18n.t("error") || "حدث خطأ ما";
          
          if (error.response) {
            if (error.response.data && error.response.data.message) {
              errorMessage = error.response.data.message;
            } else if (error.response.status === 400) {
              errorMessage = this.$i18n.t("badRequest") || "طلب غير صحيح";
            } else if (error.response.status === 401) {
              errorMessage = this.$i18n.t("unauthorized") || "غير مصرح";
            } else if (error.response.status === 500) {
              errorMessage = this.$i18n.t("serverError") || "خطأ في الخادم";
            }
          } else if (error.request) {
            errorMessage = this.$i18n.t("networkError") || "خطأ في الاتصال بالخادم";
          }
          
          this.$toast.error(errorMessage, {
            position: "top-right",
            timeout: 3000,
            maxToasts: 1,
          });
        });
    },
    addOrder(isPrint) {
      const textDirection = document.documentElement.dir;
      const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }
      this.show = true;
      this.orderForSend.orderCode = "";
      this.orderForSend.paymentMethod = this.orderForSend.paymentMethod || "Cash";
      this.orderForSend.customerOrderItem = [];
      for (const item of this.carditems) {
        this.orderForSend.customerOrderItem.push({
          itemId: item.id,
          quantity: item.quantity,
        });
      }
      this.orderForSend.orderCode = Math.floor(
        Math.random() * 1000000000
      ).toString().padStart(9, '0');
      
      HTTP.post(`Admin/AddOrder`, this.orderForSend)
        .then((response) => {
          if (response) {
            this.show = false;
            this.$toast.warning(this.$i18n.t("addOrderSucsses"), {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            });
            // Save a copy of carditems for printing before clearing
            const itemsForPrint = JSON.parse(JSON.stringify(this.carditems));
            this.carditems = [];
            this.$refs.codeNumber.focus();
            
            
            if (isPrint) {
              // Use setTimeout to ensure print happens after UI updates
              setTimeout(() => {
                try {
                  this.printCard(itemsForPrint);
                } catch (printError) {
                  console.error('Print error:', printError);
                  // Don't show error to user, printing is optional
                  // The order was saved successfully
                }
              }, 100);
            }
          }
        })
        .catch((error) => {
          this.show = false;
          console.error('Order save error:', error);
          let errorMessage = this.$i18n.t("error") || "حدث خطأ ما";
          
          if (error.response) {
            // Server responded with error status
            if (error.response.data && error.response.data.message) {
              errorMessage = error.response.data.message;
            } else if (error.response.status === 400) {
              errorMessage = this.$i18n.t("badRequest") || "طلب غير صحيح";
            } else if (error.response.status === 401) {
              errorMessage = this.$i18n.t("unauthorized") || "غير مصرح";
            } else if (error.response.status === 500) {
              errorMessage = this.$i18n.t("serverError") || "خطأ في الخادم";
            }
          } else if (error.request) {
            // Request was made but no response received
            errorMessage = this.$i18n.t("networkError") || "خطأ في الاتصال بالخادم";
          }
          
          this.$toast.error(errorMessage, {
            position: "top-right",
            timeout: 3000,
            maxToasts: 1,
          });
        });
    },

    
    getCurrentDateTime() {
      const now = new Date();
      const date = now.toLocaleDateString('ar-IQ', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit' 
      });
      const time = now.toLocaleTimeString('ar-IQ', { 
        hour: '2-digit', 
        minute: '2-digit' 
      });
      return `${date} ${time}`;
    },
    getCurrentDate() {
      const now = new Date();
      return now.toLocaleDateString('ar-IQ', { 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric',
        weekday: 'long'
      });
    },
    getOrderTypeText(type) {
      if (!type) return '-';
      const types = {
        'DineIn': this.$t('dineIn') || 'داخلي',
        'Takeaway': this.$t('takeaway') || 'طلب خارجي',
        'Delivery': this.$t('delivery') || 'توصيل'
      };
      return types[type] || type;
    },
    getPaymentMethodText(method) {
      if (!method) return '-';
      const methods = {
        'Cash': this.$t('cash') || 'نقدي',
        'Card': this.$t('card') || 'بطاقة',
        'Credit': this.$t('credit') || 'آجل',
        'BankTransfer': this.$t('bankTransfer') || 'تحويل بنكي'
      };
      return methods[method] || method;
    },
    async initializePrinters() {
      // Check if Web Print API is supported (experimental)
      if ('navigator' in window && 'printer' in navigator) {
        this.webPrintAPISupported = true;
        try {
          // Get available printers
          const printers = await navigator.printer.getPrinters();
          this.availablePrinters = printers;
          
          // Try to get saved printer preference
          const savedPrinterId = localStorage.getItem('selectedPrinter');
          if (savedPrinterId) {
            const printer = printers.find(p => p.id === savedPrinterId);
            if (printer) {
              this.selectedPrinter = printer;
              this.selectedPrinterId = printer.id;
            }
          }
          
          // If no saved printer, use default
          if (!this.selectedPrinter && printers.length > 0) {
            const defaultPrinter = printers.find(p => p.isDefault) || printers[0];
            this.selectedPrinter = defaultPrinter;
            this.selectedPrinterId = defaultPrinter.id;
            localStorage.setItem('selectedPrinter', defaultPrinter.id);
          }
        } catch (error) {
          console.warn('Web Print API not fully supported:', error);
          this.webPrintAPISupported = false;
          // Web Print API not available, will use standard print
        }
      } else {
        // Web Print API not supported, use standard print
        this.webPrintAPISupported = false;
        console.log('Web Print API not supported, using standard print dialog');
      }
    },
    onPrinterChange() {
      const printer = this.availablePrinters.find(p => p.id === this.selectedPrinterId);
      if (printer) {
        this.selectedPrinter = printer;
        localStorage.setItem('selectedPrinter', printer.id);
      }
    },
    async printWithWebPrintAPI(printContent, stylesHtml) {
      try {
        // Check if Web Print API is supported
        if (!('navigator' in window && 'printer' in navigator)) {
          throw new Error('Web Print API not supported');
        }

        // Get printer (use selected or default)
        let printer = this.selectedPrinter;
        if (!printer && this.selectedPrinterId) {
          const printers = await navigator.printer.getPrinters();
          printer = printers.find(p => p.id === this.selectedPrinterId);
        }
        
        if (!printer) {
          const printers = await navigator.printer.getPrinters();
          printer = printers.find(p => p.isDefault) || printers[0];
          if (!printer) {
            throw new Error('No printer available');
          }
        }

        // Create print job
        const printJob = await navigator.printer.print({
          printer: printer.id,
          pages: [{
            html: printContent,
            css: stylesHtml
          }]
        });

        // Wait for print job to complete
        await printJob.complete;
        return true;
      } catch (error) {
        console.error('Web Print API error:', error);
        throw error;
      }
    },
    async checkPythonServerHealth() {
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 3000); // 3 seconds timeout for health check
        
        const response = await fetch('http://localhost:5000/health', {
          method: 'GET',
          signal: controller.signal
        });
        
        clearTimeout(timeoutId);
        
        if (response.ok) {
          const health = await response.json();
          return health.status === 'ok' && health.printer?.available;
        }
        return false;
      } catch (error) {
        console.warn('Python print server health check failed:', error);
        return false;
      }
    },
    async printWithPythonServer(itemsToPrint = null) {
      try {
        const printItems = itemsToPrint || this.carditems;
        
        if (!printItems || printItems.length === 0) {
          console.warn('No items to print');
          return;
        }
        
        // Check if Python server is available
        const serverAvailable = await this.checkPythonServerHealth();
        if (!serverAvailable) {
          console.warn('Python print server is not available, skipping...');
          return false; // Return false to fallback to other print methods
        }
        
        // Prepare print data
        const printData = {
          storeName: this.commercialUserInfo.storeName || 'متجر',
          storeAddress: '',
          storePhone: '',
          orderCode: this.orderForSend.orderCode || '',
          date: new Date().toLocaleDateString('ar-EG'),
          time: new Date().toLocaleTimeString('ar-EG'),
          employeeName: this.userInfo.name || '',
          items: printItems.map(item => ({
            name: item.name || '',
            quantity: item.quantity || 0,
            price: item.price ? item.price.toLocaleString() : '0',
            total: item.total ? item.total.toLocaleString() : '0',
            discount: item.discount || null
          })),
          subtotal: this.totaPrice.toLocaleString(),
          discount: '0',
          tax: '0',
          total: this.totaPrice.toLocaleString(),
          paymentMethod: this.orderForSend.paymentMethod === 'Cash' ? 'نقدي' : 
                        this.orderForSend.paymentMethod === 'Card' ? 'بطاقة' : 
                        this.orderForSend.paymentMethod || 'نقدي'
        };
        
        // Get HTML content if needed
        await this.$nextTick();
        const printElement = document.getElementById("print");
        if (printElement) {
          printData.htmlContent = printElement.innerHTML;
        }
        
        // Send to Python print server with timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 seconds timeout
        
        try {
          const response = await fetch('http://localhost:5000/print', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(printData),
            signal: controller.signal
          });
          
          clearTimeout(timeoutId);
          
          if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
          }
          
          const result = await response.json();
          console.log("Print result:", result);  
          
          if (result.success) {
            this.$toast.success(this.$i18n.t("printSuccess") || 'تم الطباعة بنجاح', {
              position: "top-right",
              timeout: 2000,
              maxToasts: 1,
            });
            return true;
          } else {
            throw new Error(result.message || 'فشلت الطباعة');
          }
        } catch (fetchError) {
          clearTimeout(timeoutId);
          
          // Don't show error toast, just return false to allow fallback
          if (fetchError.name === 'AbortError') {
            console.warn('Python print server timeout - falling back to other methods');
          } else if (fetchError.message.includes('Failed to fetch') || fetchError.message.includes('NetworkError')) {
            console.warn('Python print server not available - falling back to other methods');
          } else {
            console.warn('Python print server error - falling back to other methods:', fetchError);
          }
          return false; // Return false to allow fallback to other print methods
        }
      } catch (error) {
        console.warn('Python print server error - falling back to other methods:', error);
        return false; // Return false to allow fallback to other print methods
      }
    },
    async printCard(itemsToPrint = null) {
      try {
        // Use provided items or fallback to current carditems
        const printItems = itemsToPrint || this.carditems;
        
        // Temporarily replace carditems for printing if needed
        const originalCarditems = this.carditems;
        if (itemsToPrint) {
          this.carditems = itemsToPrint;
        }
        
        // Wait for Vue to update the DOM
        await this.$nextTick();
        
        // Get the print content
        const printElement = document.getElementById("print");
        if (!printElement) {
          console.error("Print element not found");
          // Restore original carditems if we changed it
          if (itemsToPrint) {
            this.carditems = originalCarditems;
          }
          return;
        }

        // Professional print styles optimized for POS printers (58mm/80mm)
        const stylesHtml = `
    <style>
      @page {
        size: 80mm auto;
        margin: 0;
      }
      
      * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
      }
      
      body {
        font-family: 'Cairo', 'Arial', sans-serif;
        direction: rtl;
        font-size: 12px;
        line-height: 1.4;
        color: #000;
        background: #fff;
        padding: 8mm;
        width: 80mm;
      }
      
      .bill-container {
        width: 100%;
        max-width: 80mm;
        margin: 0 auto;
      }
      
      .bill-header {
        text-align: center;
        margin-bottom: 8px;
        padding-bottom: 8px;
        border-bottom: 1px dashed #000;
      }
      
      .bill-logo-img {
        max-width: 60px;
        height: auto;
        margin-bottom: 4px;
      }
      
      .bill-store-name {
        font-size: 16px;
        font-weight: 800;
        margin: 4px 0 2px 0;
        color: #000;
      }
      
      .bill-store-subtitle {
        font-size: 10px;
        color: #666;
        margin: 0;
      }
      
      .bill-info-section {
        margin: 8px 0;
        font-size: 10px;
      }
      
      .bill-info-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 4px;
      }
      
      .bill-info-label {
        font-weight: 600;
      }
      
      .bill-info-value {
        font-weight: 400;
      }
      
      .bill-barcode-section {
        text-align: center;
        margin: 8px 0;
        padding: 4px 0;
      }
      
      .bill-barcode-img {
        max-width: 100%;
        height: auto;
        display: block;
        margin: 0 auto;
      }
      
      .bill-divider {
        border: none;
        border-top: 1px dashed #000;
        margin: 8px 0;
      }
      
      .bill-table {
        width: 100%;
        border-collapse: collapse;
        margin: 8px 0;
        font-size: 10px;
      }
      
      .bill-table-header {
        background: #f5f5f5;
        border-bottom: 2px solid #000;
      }
      
      .bill-table-cell {
        padding: 4px 2px;
        text-align: right;
        border-bottom: 1px dotted #ccc;
      }
      
      .bill-table-header .bill-table-cell {
        font-weight: 700;
        font-size: 10px;
        padding: 6px 2px;
      }
      
      .bill-col-item {
        width: 40%;
        text-align: right;
      }
      
      .bill-col-qty {
        width: 15%;
        text-align: center;
      }
      
      .bill-col-price {
        width: 20%;
        text-align: left;
      }
      
      .bill-col-total {
        width: 25%;
        text-align: left;
        font-weight: 600;
      }
      
      .bill-summary-section {
        margin-top: 12px;
        padding-top: 8px;
        border-top: 2px solid #000;
        font-size: 11px;
      }
      
      .bill-summary-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 6px;
      }
      
      .bill-summary-label {
        font-weight: 600;
      }
      
      .bill-summary-value {
        font-weight: 400;
      }
      
      .bill-total-row {
        margin-top: 8px;
        padding-top: 8px;
        border-top: 1px dashed #000;
        font-size: 14px;
      }
      
      .bill-total-amount {
        font-weight: 800;
        font-size: 16px;
      }
      
      .bill-footer {
        text-align: center;
        margin-top: 16px;
        padding-top: 12px;
        border-top: 1px dashed #000;
        font-size: 10px;
      }
      
      .bill-footer-text {
        margin: 4px 0;
        font-weight: 600;
      }
      
      .bill-footer-date {
        margin: 4px 0;
        color: #666;
        font-size: 9px;
      }
      
      @media print {
        body {
          padding: 0;
        }
        
        .bill-container {
          width: 80mm;
        }
        
        .bill-table-cell {
          padding: 3px 2px;
        }
      }
    </style>
  `;

        // Try Python print server first (if available)
        try {
          const pythonPrintSuccess = await this.printWithPythonServer(itemsToPrint);
          if (pythonPrintSuccess) {
            // Restore original carditems if we changed it
            if (itemsToPrint) {
              this.carditems = originalCarditems;
            }
            return; // Success - exit early
          }
        } catch (pythonError) {
          console.warn('Python print server not available, trying other methods:', pythonError);
          // Fall through to other print methods
        }

        // Check if Web Print API is truly supported and printer is selected
        const isWebPrintAPISupported = 'navigator' in window && 
                                       'printer' in navigator && 
                                       typeof navigator.printer !== 'undefined' &&
                                       this.selectedPrinter &&
                                       this.webPrintAPISupported;

        // Try Web Print API (if truly supported)
        if (isWebPrintAPISupported) {
          try {
            const printContent = printElement.innerHTML;
            await this.printWithWebPrintAPI(printContent, stylesHtml);
            // Restore original carditems if we changed it
            if (itemsToPrint) {
              this.carditems = originalCarditems;
            }
            return; // Success - exit early
          } catch (webPrintError) {
            console.warn('Web Print API failed, falling back to standard print:', webPrintError);
            // Fall through to standard print methods
          }
        }

        // Use standard browser print dialog (works in Chrome, Firefox, Edge, etc.)
        // Create a new window for printing
        const printWindow = window.open('', '_blank', 'width=800,height=600');
        if (printWindow) {
          // Build HTML content
          const invoiceTitle = (this.$t("invoice_number") || "فاتورة") + ' - ' + (this.orderForSend.orderCode || 'Invoice');
          const htmlContent = '<!DOCTYPE html><html><head><title>' + invoiceTitle +
            '</title><meta charset="UTF-8">' + stylesHtml +
            '</head><body>' + printElement.innerHTML + '</body></html>';
          
          printWindow.document.write(htmlContent);
          printWindow.document.close();
          
          // Wait for content to load, then print
          setTimeout(() => {
            printWindow.focus();
            printWindow.print();
            // Close window after printing
            setTimeout(() => {
              printWindow.close();
              // Restore original carditems if we changed it
              if (itemsToPrint) {
                this.carditems = originalCarditems;
              }
            }, 500);
          }, 500);
        } else {
          // If popup blocked, use fallback method with iframe
          console.warn('Popup blocked, using fallback print method');
          this.fallbackPrint(itemsToPrint);
        }
      } catch (error) {
        console.error('Print card error:', error);
        // Restore original carditems if we changed it
        if (itemsToPrint) {
          this.carditems = originalCarditems;
        }
        // Silently fail - order was saved successfully, printing is optional
      }
    },
    async fallbackPrint(itemsToPrint = null) {
      // Use provided items or fallback to current carditems
      const printItems = itemsToPrint || this.carditems;
      
      // Temporarily replace carditems for printing if needed
      const originalCarditems = this.carditems;
      if (itemsToPrint) {
        this.carditems = itemsToPrint;
      }
      
      // Wait for Vue to update the DOM
      await this.$nextTick();
      
      // Fallback method using iframe (original method)
      const prtHtml = document.getElementById("print").innerHTML;
      const stylesHtml = `
    <style>
      @page {
        size: 80mm auto;
        margin: 0;
      }
      
      * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
      }
      
      body {
        font-family: 'Cairo', 'Arial', sans-serif;
        direction: rtl;
        font-size: 12px;
        line-height: 1.4;
        color: #000;
        background: #fff;
        padding: 8mm;
        width: 80mm;
      }
      
      .bill-container {
        width: 100%;
        max-width: 80mm;
        margin: 0 auto;
      }
      
      .bill-header {
        text-align: center;
        margin-bottom: 8px;
        padding-bottom: 8px;
        border-bottom: 1px dashed #000;
      }
      
      .bill-logo-img {
        max-width: 60px;
        height: auto;
        margin-bottom: 4px;
      }
      
      .bill-store-name {
        font-size: 16px;
        font-weight: 800;
        margin: 4px 0 2px 0;
        color: #000;
      }
      
      .bill-store-subtitle {
        font-size: 10px;
        color: #666;
        margin: 0;
      }
      
      .bill-info-section {
        margin: 8px 0;
        font-size: 10px;
      }
      
      .bill-info-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 4px;
      }
      
      .bill-info-label {
        font-weight: 600;
      }
      
      .bill-info-value {
        font-weight: 400;
      }
      
      .bill-barcode-section {
        text-align: center;
        margin: 8px 0;
        padding: 4px 0;
      }
      
      .bill-barcode-img {
        max-width: 100%;
        height: auto;
        display: block;
        margin: 0 auto;
      }
      
      .bill-divider {
        border: none;
        border-top: 1px dashed #000;
        margin: 8px 0;
      }
      
      .bill-table {
        width: 100%;
        border-collapse: collapse;
        margin: 8px 0;
        font-size: 10px;
      }
      
      .bill-table-header {
        background: #f5f5f5;
        border-bottom: 2px solid #000;
      }
      
      .bill-table-cell {
        padding: 4px 2px;
        text-align: right;
        border-bottom: 1px dotted #ccc;
      }
      
      .bill-table-header .bill-table-cell {
        font-weight: 700;
        font-size: 10px;
        padding: 6px 2px;
      }
      
      .bill-col-item {
        width: 40%;
        text-align: right;
      }
      
      .bill-col-qty {
        width: 15%;
        text-align: center;
      }
      
      .bill-col-price {
        width: 20%;
        text-align: left;
      }
      
      .bill-col-total {
        width: 25%;
        text-align: left;
        font-weight: 600;
      }
      
      .bill-summary-section {
        margin-top: 12px;
        padding-top: 8px;
        border-top: 2px solid #000;
        font-size: 11px;
      }
      
      .bill-summary-row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 6px;
      }
      
      .bill-summary-label {
        font-weight: 600;
      }
      
      .bill-summary-value {
        font-weight: 400;
      }
      
      .bill-total-row {
        margin-top: 8px;
        padding-top: 8px;
        border-top: 1px dashed #000;
        font-size: 14px;
      }
      
      .bill-total-amount {
        font-weight: 800;
        font-size: 16px;
      }
      
      .bill-footer {
        text-align: center;
        margin-top: 16px;
        padding-top: 12px;
        border-top: 1px dashed #000;
        font-size: 10px;
      }
      
      .bill-footer-text {
        margin: 4px 0;
        font-weight: 600;
      }
      
      .bill-footer-date {
        margin: 4px 0;
        color: #666;
        font-size: 9px;
      }
      
      @media print {
        body {
          padding: 0;
        }
        
        .bill-container {
          width: 80mm;
        }
        
        .bill-table-cell {
          padding: 3px 2px;
        }
      }
    </style>
  `;

      const content = `
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="UTF-8">
      <title>فاتورة - ${this.orderForSend.orderCode || 'Invoice'}</title>
      ${stylesHtml}
    </head>
    <body>
      ${prtHtml}
    </body>
    </html>
  `;

      const iframe = document.createElement("iframe");
      iframe.style.position = "absolute";
      iframe.style.top = "-10000px";
      iframe.style.width = "80mm";
      iframe.style.height = "1000px";
      document.body.appendChild(iframe);

      const doc = iframe.contentWindow.document;
      doc.open();
      doc.write(content);
      doc.close();

      setTimeout(() => {
        iframe.contentWindow.focus();
        iframe.contentWindow.print();
        
        setTimeout(() => {
          if (document.body.contains(iframe)) {
            document.body.removeChild(iframe);
          }
          // Restore original carditems if we changed it
          if (itemsToPrint) {
            this.carditems = originalCarditems;
          }
        }, 1000);
      }, 250);
    },

    EmptycardList(id) {
      this.carditems = [];
      this.$bvModal.hide(id);
      // Reset order type when clearing cart
      this.orderForSend.orderType = 'Takeaway';
      this.$refs.codeNumber.focus();
    },
    closeModel(id) {
      this.$bvModal.hide(id);
    },
    addToCartList(item) {
      try {
        const bodyElement = document.querySelector("body");
        const textDirection = bodyElement.getAttribute("dir");
        const toastPosition = textDirection === "rtl" ? "top-right" : "top-left";
        
        // Check if item has available quantity
        if (!item.quantity || item.quantity <= 0) {
          this.$toast.error(
            this.$i18n.t("itemOutOfStock") || "المنتج غير متوفر في المخزون",
            {
              position: toastPosition,
              timeout: 2000,
              maxToasts: 1,
            }
          );
          return;
        }
        
        // Check if item already exists in cart
        const existingItemIndex = this.carditems.findIndex(cartItem => cartItem.id === item.id);
        
        if (existingItemIndex !== -1) {
          // Item exists, increment quantity
          this.carditems[existingItemIndex].quantity += 1;
          this.carditems[existingItemIndex].total = 
            (this.carditems[existingItemIndex].price !== this.carditems[existingItemIndex].disCountPrice
              ? this.carditems[existingItemIndex].disCountPrice
              : this.carditems[existingItemIndex].price) * this.carditems[existingItemIndex].quantity;
        } else {
          // New item, add to cart
          const cartItem = {
            name: item.name,
            quantity: 1,
            price: item.sellingPrice,
            disCountPrice: item.disCountPrice,
            total:
              item.sellingPrice !== item.disCountPrice
                ? item.disCountPrice
                : item.sellingPrice,
            id: item.id,
          };

          this.carditems.push(cartItem);
        }

        if (this.$refs.codeNumber) {
          this.$refs.codeNumber.focus();
        }

        // Show compact notification
        this.showItemAddedNotification(item.name);
      } catch (error) {
        console.error("Error adding item to cart:", error);
        this.$toast.error(this.$i18n.t("error"), {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
          newestOnTop: true,
        });
      }
    },

    deleteItem(index) {
      this.carditems.splice(index, 1);
      this.$toast.error(this.$i18n.t("deleteItemFromOrderSucsses"), {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    },
    increaseQuantity(index) {
      if (this.carditems[index]) {
        this.carditems[index].quantity += 1;
        this.updateItemTotal(index);
      }
    },
    decreaseQuantity(index) {
      if (this.carditems[index] && this.carditems[index].quantity > 1) {
        this.carditems[index].quantity -= 1;
        this.updateItemTotal(index);
      }
    },
    updateQuantity(index, value) {
      const quantity = parseInt(value) || 1;
      if (quantity > 0 && this.carditems[index]) {
          this.carditems[index].quantity = quantity;
        this.updateItemTotal(index);
      }
    },
    updateItemTotal(index) {
      if (this.carditems[index]) {
        const item = this.carditems[index];
        const finalPrice = item.price !== item.disCountPrice ? item.disCountPrice : item.price;
        this.carditems[index].total = finalPrice * item.quantity;
      }
    },
    GetAllItems() {
      this.show = true;
      HTTP.get(
        `Admin/GetItems?pageNumber=${this.pageNumber - 1}&pageSize=${
          this.pageSize
        }&info=${this.search.info}`
      )
        .then((response) => {
          this.Items = response.data.data.items.map(item => ({
            ...item,
            imageError: false
          }));
          this.totalItems = response.data.data.totalItems;
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
        });
    },
    handleBarcodeSearch() {
      // Immediate search when Enter is pressed (barcode scanner)
      if (this.searchCode && this.searchCode.trim() !== "") {
        clearTimeout(this.typingTimer);
        // Cancel any pending debounced search
        this.typingTimer = null;
        this.SearchByCode();
      }
    },
    handleBarcodeInput() {
      // Cancel any pending search
      clearTimeout(this.typingTimer);
      
      if (this.searchCode.trim() === "") {
        return;
      }
      
      // Use debounce for all searches to prevent multiple requests
      // Barcode scanners send codes quickly, so we wait a bit to ensure complete code
      this.typingTimer = setTimeout(() => {
        // Only search if code is long enough (likely complete)
        // Minimum 3 chars for manual typing, but prefer longer codes
        if (this.searchCode.length >= 3) {
          this.SearchByCode();
        }
      }, this.doneTypingInterval);
    },
    SearchByCode() {
      // Prevent multiple simultaneous searches
      if (this.isSearching) {
        return;
      }
      
      if (!this.searchCode || this.searchCode.trim() === "") {
        return;
      }
      
      // Cancel any previous request
      if (this.searchAbortController) {
        this.searchAbortController.abort();
      }
      
      // Create new abort controller for this request
      this.searchAbortController = new AbortController();
      this.isSearching = true;
      
      HTTP.get(`Admin/GetItemsByCode?code=${this.searchCode}`, {
        signal: this.searchAbortController.signal
      })
        .then((response) => {
          this.isSearching = false;
          
          if (response.data && response.data.data) {
            this.SearchItems = response.data.data;
            
            // Check if item already exists in cart
            const existingItemIndex = this.carditems.findIndex(cartItem => cartItem.id === this.SearchItems.id);
            
            if (existingItemIndex !== -1) {
              // Item exists, increment quantity
              this.carditems[existingItemIndex].quantity += 1;
              this.carditems[existingItemIndex].total = 
                (this.carditems[existingItemIndex].price !== this.carditems[existingItemIndex].disCountPrice
                  ? this.carditems[existingItemIndex].disCountPrice
                  : this.carditems[existingItemIndex].price) * this.carditems[existingItemIndex].quantity;
            } else {
              // Check if item has available quantity
              if (!this.SearchItems.quantity || this.SearchItems.quantity <= 0) {
                const toastPosition = document.documentElement.dir === "rtl" ? "top-right" : "top-left";
                this.$toast.error(
                  this.$i18n.t("itemOutOfStock") || "المنتج غير متوفر في المخزون",
                  {
                    position: toastPosition,
                    timeout: 2000,
                    maxToasts: 1,
                    newestOnTop: true,
                  }
                );
                this.searchCode = "";
                if (this.$refs.codeNumber) {
                  this.$refs.codeNumber.focus();
                }
                return;
              }
              
              // New item, add to cart
              const finalPrice = this.SearchItems.disCountPrice > 0 && this.SearchItems.disCountPrice !== this.SearchItems.sellingPrice
                ? this.SearchItems.disCountPrice
                : this.SearchItems.sellingPrice;
                
              var item = {
                name: this.SearchItems.name,
                quantity: 1,
                price: this.SearchItems.sellingPrice,
                disCountPrice: this.SearchItems.disCountPrice,
                total: finalPrice * 1,
                id: this.SearchItems.id,
              };
              this.carditems.push(item);
            }
            
            // Show compact notification for quick additions
            this.showItemAddedNotification(this.SearchItems.name);
            
            this.searchCode = "";
            if (this.$refs.codeNumber) {
              this.$refs.codeNumber.focus();
            }
          }
        })
        .catch((error) => {
          this.isSearching = false;
          
          // Don't show error if request was aborted
          if (error.name === 'AbortError' || error.code === 'ERR_CANCELED') {
            return;
          }
          
          this.searchCode = "";
          // Show error notification (only one at a time)
          this.$toast.error(this.$i18n.t("itemNotFound") || "Item not found", {
            position: "top-right",
            timeout: 2000,
            closeOnClick: true,
            pauseOnFocusLoss: false,
            pauseOnHover: false,
            draggable: false,
            hideProgressBar: false,
            maxToasts: 1,
            newestOnTop: true,
          });
        });
    },
    showItemAddedNotification(itemName) {
      // Clear any existing timer
      if (this.addItemTimer) {
        clearTimeout(this.addItemTimer);
      }
      
      // Increment counter
      this.itemsAddedCount++;
      this.lastAddedItem = itemName;
      
      // Clear previous success toasts
      this.$toast.clear();
      
      // Show aggregated notification
      const message = this.itemsAddedCount > 1 
        ? `${this.itemsAddedCount} ${this.$i18n.t("itemsAdded") || "مواد مضافة"}`
        : `${itemName} : ${this.$i18n.t("itemToCard")}`;
      
      this.$toast.success(message, {
        position: "top-right",
        timeout: 1500,
        closeOnClick: true,
        pauseOnFocusLoss: false,
        pauseOnHover: false,
        draggable: false,
        hideProgressBar: true,
        maxToasts: 1,
        newestOnTop: true,
        icon: true,
      });
      
      // Reset counter after 2 seconds of inactivity
      this.addItemTimer = setTimeout(() => {
        this.itemsAddedCount = 0;
        this.lastAddedItem = null;
      }, 2000);
    },
  },
};
</script>
