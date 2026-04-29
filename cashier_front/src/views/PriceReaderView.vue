<template>
  <div class="price-reader-fullscreen">
    <!-- Header Section -->
    <div class="price-reader-header-fullscreen">
      <div class="price-reader-header-content">
        <h1 class="price-reader-title">
          <b-icon icon="upc-scan" class="me-2"></b-icon>
          {{ $t("PriceReader") || "قارئ الأسعار" }}
        </h1>
      </div>
    </div>

    <!-- Main Content Area -->
    <div class="price-reader-content-fullscreen">
      <!-- Scanner Input Section -->
      <div class="price-reader-scanner-section-fullscreen" v-if="!price && !showNotFound">
        <div class="scanner-input-wrapper-fullscreen">
          <label class="scanner-input-label-fullscreen">
            <b-icon icon="upc-scan" class="scanner-icon"></b-icon>
            {{ $t("barcode") || "الباركود" }}
          </label>
          <div class="scanner-input-container-fullscreen">
            <input
              v-model="searchCode"
              ref="codeNumber"
              type="search"
              :placeholder="$t('place_barcode_on_reader') || 'امسح الباركود أو أدخل الكود'"
              class="scanner-input-fullscreen"
              autofocus
              @keyup.enter="handleBarcodeSearch"
              @input="handleBarcodeInput"
              :disabled="isSearching"
            />
            <div class="scanner-input-indicator-fullscreen" v-if="isSearching">
              <b-icon icon="arrow-repeat" animation="spin" class="scanner-loading-icon"></b-icon>
            </div>
          </div>
        </div>

        <!-- Barcode Animation -->
        <div class="barcode-animation-wrapper-fullscreen">
          <lottie-animation 
            path="./barcode.json" 
            :width="200" 
            :height="200"
            v-if="!isSearching"
          />
          <div class="barcode-placeholder-fullscreen" v-else-if="isSearching">
            <b-icon icon="arrow-repeat" animation="spin" class="barcode-loading-icon-fullscreen"></b-icon>
            <p class="barcode-loading-text-fullscreen">{{ $t("searching") || "جاري البحث..." }}</p>
          </div>
        </div>
      </div>

      <!-- Price Display Section -->
      <div class="price-display-section-fullscreen" v-if="price !== '' && price !== null">
        <div class="price-display-card-fullscreen">
          <div class="price-display-header-fullscreen">
            <b-icon icon="check-circle-fill" class="price-success-icon-fullscreen"></b-icon>
            <h2 class="price-display-title-fullscreen">
              {{ $t("priceFound") || "تم العثور على السعر" }}
            </h2>
          </div>
          <div class="price-display-body-fullscreen">
            <div class="price-display-content-fullscreen">
              <div class="price-label-wrapper-fullscreen">
                <span class="price-label-fullscreen">{{ $t("price") || "السعر" }}</span>
              </div>
              <div class="price-value-wrapper-fullscreen">
                <span class="price-value-fullscreen">{{ formattedNumber(price) }}</span>
                <span class="price-currency-fullscreen">{{ $t("currency") || "دينار" }}</span>
              </div>
            </div>
            <div class="price-display-footer-fullscreen">
              <button class="price-reset-btn-fullscreen" @click="resetSearch">
                <b-icon icon="arrow-repeat" class="me-2"></b-icon>
                {{ $t("scanAgain") || "مسح جديد" }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- No Price Found Section -->
      <div class="price-not-found-section-fullscreen" v-if="showNotFound">
        <div class="price-not-found-card-fullscreen">
          <b-icon icon="x-circle-fill" class="price-error-icon-fullscreen"></b-icon>
          <h3 class="price-not-found-title-fullscreen">
            {{ $t("priceNotFound") || "لم يتم العثور على السعر" }}
          </h3>
          <p class="price-not-found-text-fullscreen">
            {{ $t("priceNotFoundDescription") || "تأكد من صحة الباركود وحاول مرة أخرى" }}
          </p>
          <button class="price-reset-btn-fullscreen" @click="resetSearch">
            <b-icon icon="arrow-repeat" class="me-2"></b-icon>
            {{ $t("tryAgain") || "حاول مرة أخرى" }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import LottieAnimation from "lottie-vuejs/src/LottieAnimation.vue";
import { HTTP } from '../http/api.js';

export default {
  name: "PriceReaderView",
  components: {
    LottieAnimation,
  },
    data() {
        return {
            searchCode: '',
            price: "",
            typingTimer: null,
      doneTypingInterval: 500,
      isSearching: false,
      searchAbortController: null,
      showNotFound: false,
        };
    },
    mounted() {
    // Focus on input when component mounts
    this.$nextTick(() => {
      if (this.$refs.codeNumber) {
        this.$refs.codeNumber.focus();
      }
    });

    // Focus on input when clicking anywhere on the page
    document.addEventListener('click', this.handlePageClick);
    
    // Prevent scrolling
    document.body.style.overflow = 'hidden';
  },
  beforeDestroy() {
    // Cleanup
    if (this.typingTimer) {
      clearTimeout(this.typingTimer);
    }
    if (this.searchAbortController) {
      this.searchAbortController.abort();
    }
    document.removeEventListener('click', this.handlePageClick);
    document.body.style.overflow = '';
  },
  methods: {
    handlePageClick() {
      // Focus on input when clicking anywhere on the page
      if (this.$refs.codeNumber) {
        this.$refs.codeNumber.focus();
      }
    },
    formattedNumber(info) {
      if (!info || isNaN(info)) {
        return "0";
      }
      return info.toLocaleString('ar-SA');
    },
    handleBarcodeSearch() {
      // Immediate search when Enter is pressed (barcode scanner)
      if (this.searchCode && this.searchCode.trim() !== "") {
        clearTimeout(this.typingTimer);
        this.typingTimer = null;
        this.SearchByCode();
      }
    },
    handleBarcodeInput() {
      // Cancel any pending search
                clearTimeout(this.typingTimer);

      // Reset states
      this.showNotFound = false;
      this.price = "";
      
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
      this.showNotFound = false;
      this.price = "";
      
      HTTP.get(`Admin/GetItemsByCode?code=${this.searchCode}`, {
        signal: this.searchAbortController.signal
      })
                .then((response) => {
          this.isSearching = false;
          
          if (response.data && response.data.data && response.data.data.sellingPrice) {
                    this.price = response.data.data.sellingPrice;
            this.showNotFound = false;
            
            // Clear search code and focus for next scan
            setTimeout(() => {
                    this.searchCode = '';
              if (this.$refs.codeNumber) {
                    this.$refs.codeNumber.focus();
              }
            }, 100);
          } else {
            this.showNotFound = true;
            this.price = "";
          }
                })
                .catch((error) => {
          this.isSearching = false;
          
          // Only show error if it's not an abort error
          if (error.name !== 'AbortError' && error.name !== 'CanceledError') {
            this.showNotFound = true;
            this.price = "";
          }
        });
    },
    resetSearch() {
      // Cancel any pending search
      if (this.typingTimer) {
        clearTimeout(this.typingTimer);
        this.typingTimer = null;
      }
      
      if (this.searchAbortController) {
        this.searchAbortController.abort();
      }
      
      // Reset all states
                    this.searchCode = '';
      this.price = "";
      this.showNotFound = false;
      this.isSearching = false;
      
      // Focus on input
      this.$nextTick(() => {
        if (this.$refs.codeNumber) {
          this.$refs.codeNumber.focus();
        }
                });
        },
    },
};
</script>
