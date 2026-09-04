import { HTTP } from "@/http/api.js";
import {
  findCartLineIndex,
  getCartLineTotal,
  promoteCartLineToFront,
} from "@/utils/mergeCartLines.js";
import {
  normalizeBarcodeCode,
  buildGetItemsByCodeUrl,
  normalizeScannedItem,
  BARCODE_SCANNER_KEY_GAP_MS,
  BARCODE_SCANNER_TAIL_MS,
  BARCODE_MANUAL_DEBOUNCE_MS,
} from "@/utils/barcodeScan.js";
import { findPosItemByCode } from "@/utils/posCatalogQuery.js";
import { resolveCommercialUserId } from "@/utils/publicMenu.js";

/**
 * POS barcode field: instant on Enter, ~35ms tail for wedge scanners, no isSearching deadlocks.
 */
export default {
  data() {
    return {
      barcodeTypingTimer: null,
      barcodeLastKeyAt: 0,
      barcodeScannerBurst: false,
      barcodeSearchGeneration: 0,
    };
  },
  beforeDestroy() {
    this.clearBarcodeTypingTimer();
    if (this.searchAbortController) {
      this.searchAbortController.abort();
      this.searchAbortController = null;
    }
  },
  methods: {
    clearBarcodeTypingTimer() {
      if (this.barcodeTypingTimer) {
        clearTimeout(this.barcodeTypingTimer);
        this.barcodeTypingTimer = null;
      }
    },
    handleBarcodeKeydown(e) {
      const now = Date.now();
      const gap = this.barcodeLastKeyAt ? now - this.barcodeLastKeyAt : 999;
      this.barcodeLastKeyAt = now;

      if (gap < BARCODE_SCANNER_KEY_GAP_MS) {
        this.barcodeScannerBurst = true;
      }

      if (e.key === "Enter" || e.key === "Tab") {
        e.preventDefault();
        this.commitBarcodeScan();
      }
    },
    handleBarcodeSearch() {
      this.commitBarcodeScan();
    },
    handleBarcodeInput() {
      this.clearBarcodeTypingTimer();
      const code = normalizeBarcodeCode(this.searchCode);
      if (!code) return;

      const isScanner = this.barcodeScannerBurst;
      const tailMs = isScanner ? BARCODE_SCANNER_TAIL_MS : BARCODE_MANUAL_DEBOUNCE_MS;

      this.barcodeTypingTimer = setTimeout(() => {
        this.barcodeScannerBurst = false;
        const current = normalizeBarcodeCode(this.searchCode);
        if (!current) return;
        if (!isScanner && current.length < 3) return;
        this.commitBarcodeScan();
      }, tailMs);
    },
    handleBarcodePaste() {
      this.barcodeScannerBurst = true;
      this.$nextTick(() => this.commitBarcodeScan());
    },
    commitBarcodeScan() {
      this.clearBarcodeTypingTimer();
      this.barcodeScannerBurst = false;
      const code = normalizeBarcodeCode(this.searchCode);
      if (!code) return;

      const now = Date.now();
      if (
        this._lastBarcodeCommitCode === code &&
        now - (this._lastBarcodeCommitAt || 0) < 150
      ) {
        return;
      }
      this._lastBarcodeCommitCode = code;
      this._lastBarcodeCommitAt = now;

      this.searchCode = code;
      this.SearchByCode(code);
    },
    applyLocalBarcodeItem(item, query) {
      const normalized = normalizeScannedItem(item, query);
      if (!normalized) return false;
      this.applyBarcodeItemToCart(normalized);
      this.resetBarcodeField();
      return true;
    },
    SearchByCode(code) {
      const query = normalizeBarcodeCode(code ?? this.searchCode);
      if (!query) return;

      this.clearBarcodeTypingTimer();

      if (this.searchAbortController) {
        this.searchAbortController.abort();
      }

      const controller = new AbortController();
      this.searchAbortController = controller;
      const generation = ++this.barcodeSearchGeneration;
      this.isSearching = true;

      const commercialUserId = resolveCommercialUserId();
      findPosItemByCode(commercialUserId, this.selectedWarehouseId, query)
        .catch(() => null)
        .then((localItem) => {
          if (generation !== this.barcodeSearchGeneration) return true;
          if (localItem && this.applyLocalBarcodeItem(localItem, query)) {
            return true;
          }
          return false;
        })
        .then((handled) => {
          if (handled || generation !== this.barcodeSearchGeneration) return null;
          if (typeof navigator !== "undefined" && !navigator.onLine) {
            this.notifyBarcodeNotFound();
            this.resetBarcodeField();
            return null;
          }
          const url = buildGetItemsByCodeUrl(query, this.selectedWarehouseId);
          if (!url) return null;
          return HTTP.get(url, { signal: controller.signal, timeout: 4000 }).then((response) => {
            if (generation !== this.barcodeSearchGeneration) return;
            const item = normalizeScannedItem(
              response?.data?.data || response?.data?.Data,
              query
            );
            if (!item) {
              this.notifyBarcodeNotFound();
              this.resetBarcodeField();
              return;
            }
            this.applyBarcodeItemToCart(item);
            this.resetBarcodeField();
          });
        })
        .catch((error) => {
          if (
            error?.name === "AbortError" ||
            error?.name === "CanceledError" ||
            error?.code === "ERR_CANCELED"
          ) {
            return;
          }
          if (generation !== this.barcodeSearchGeneration) return;
          this.notifyBarcodeNotFound();
          this.resetBarcodeField();
        })
        .finally(() => {
          if (
            this.searchAbortController === controller &&
            generation === this.barcodeSearchGeneration
          ) {
            this.isSearching = false;
            this.searchAbortController = null;
          }
        });
    },
    resetBarcodeField() {
      this.searchCode = "";
      this.$nextTick(() => this.focusPosBarcode?.());
    },
    notifyBarcodeNotFound() {
      const toastPosition =
        document.documentElement.dir === "rtl" ? "top-right" : "top-left";
      this.$notify.error(this.$i18n.t("itemNotFound") || "Item not found", {
        position: toastPosition,
        timeout: 2000,
        closeOnClick: true,
        pauseOnFocusLoss: false,
        pauseOnHover: false,
        maxToasts: 1,
        newestOnTop: true,
      });
    },
    applyBarcodeItemToCart(item) {
      if (!item.isNonInventory) {
        const inCart = this.cartQuantityForItem
          ? this.cartQuantityForItem(item.id)
          : 0;
        const available = Number(item.quantity);
        if (!Number.isFinite(available) || inCart + 1 > available) {
          const toastPosition =
            document.documentElement.dir === "rtl" ? "top-right" : "top-left";
          this.$notify.error(
            this.$i18n.t("itemOutOfStock") || "المنتج غير متوفر في المخزون",
            {
              position: toastPosition,
              timeout: 2000,
              maxToasts: 1,
              newestOnTop: true,
            }
          );
          return;
        }
      }

      this.SearchItems = item;

      const existingItemIndex = findCartLineIndex(this.carditems, item.id);

      if (existingItemIndex !== -1) {
        this.carditems[existingItemIndex].quantity += 1;
        this.carditems[existingItemIndex].isWholesale = this.isWholesale;
        this.carditems[existingItemIndex].wholesalePrice =
          item.wholesalePrice ||
          this.carditems[existingItemIndex].wholesalePrice ||
          0;
        this.carditems[existingItemIndex].total = getCartLineTotal(
          this.carditems[existingItemIndex],
          this.isWholesale
        );
        promoteCartLineToFront(this.carditems, existingItemIndex);
      } else {
        const cartItem = {
          name: item.name,
          quantity: 1,
          price: item.sellingPrice,
          disCountPrice: item.disCountPrice,
          wholesalePrice: item.wholesalePrice,
          isWholesale: this.isWholesale,
          id: item.id,
        };
        cartItem.total = getCartLineTotal(cartItem, this.isWholesale);
        this.carditems.unshift(cartItem);
      }

      this.feedbackItemAdded(item.name);
    },
  },
};
