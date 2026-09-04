import { HTTP } from "@/http/api.js";
import { mergeCartLinesForOrderPayload } from "@/utils/mergeCartLines.js";
import {
  createClientOrderId,
  enqueuePosOrder,
  flushPendingOrders,
  applySoldPayloadToCatalog,
  markPosSaleAccepted,
} from "@/utils/posSync.js";
import { getItemAvailableQty } from "@/utils/posCatalogQuery.js";
import { resolveCommercialUserId } from "@/utils/publicMenu.js";

/**
 * Retail POS order save (AddOrder only).
 */
export default {
  data() {
    return {
      orderPersisting: false,
    };
  },
  methods: {
    getOrderPersistToastPosition() {
      const textDirection = document.documentElement.dir;
      return textDirection === "rtl" ? "top-right" : "top-left";
    },
    prepareOrderPayload() {
      this.orderForSend.paymentMethod =
        this.orderForSend.paymentMethod || "Cash";
      this.orderForSend.customerOrderItem = mergeCartLinesForOrderPayload(
        this.carditems
      );

      const existing = String(this.orderForSend.orderCode || "").trim();
      if (!existing || existing === "---") {
        this.orderForSend.orderCode = Math.floor(
          Math.random() * 1000000000
        )
          .toString()
          .padStart(9, "0");
      }

      const discountPayload = this.buildOrderDiscountPayload
        ? this.buildOrderDiscountPayload()
        : {};
      Object.assign(this.orderForSend, discountPayload);

      this.orderForSend.isCheckout = !!this.isCheckoutPersist;
      this.orderForSend.cardPaymentTransactionId =
        this.cardPaymentTransactionIdForCheckout || null;
      this.orderForSend.isWholesale = !!this.isWholesale;
      if (this.selectedWarehouseId) {
        this.orderForSend.warehouseId = this.selectedWarehouseId;
      }
      if (!this.orderForSend.clientOrderId) {
        this.orderForSend.clientOrderId = createClientOrderId();
      }
      this.orderForSend.soldAt = new Date().toISOString();
    },
    cartQuantityForItem(itemId) {
      const id = Number(itemId);
      const qtyFrom = (lines) =>
        (lines || []).reduce((sum, line) => {
          if (line.isNonInventory) return sum;
          if (Number(line.id) !== id) return sum;
          return sum + (Number(line.quantity) || 0);
        }, 0);
      let total = qtyFrom(this.carditems);
      const activeId = this.activeInvoiceTabId;
      (this.invoiceTabs || []).forEach((tab) => {
        if (!tab || tab.id === activeId) return;
        total += qtyFrom(tab.carditems);
      });
      return total;
    },
    async validateLocalStockForOrder() {
      const grouped = {};
      (this.carditems || []).forEach((line) => {
        if (line.isNonInventory) return;
        const id = Number(line.id);
        const qty = Number(line.quantity) || 0;
        if (!id || qty <= 0) return;
        grouped[id] = (grouped[id] || 0) + qty;
      });
      const ids = Object.keys(grouped);
      const cid = resolveCommercialUserId();
      const wid = this.selectedWarehouseId;
      for (let i = 0; i < ids.length; i += 1) {
        const itemId = Number(ids[i]);
        const available = await getItemAvailableQty(cid, wid, itemId);
        if (available == null) continue;
        const reservedAcrossTabs = this.cartQuantityForItem(itemId);
        if (reservedAcrossTabs > available) {
          const named = (this.carditems || []).find((line) => Number(line.id) === itemId);
          return {
            name: (named && named.name) || String(itemId),
            available,
            required: reservedAcrossTabs,
          };
        }
      }
      return null;
    },
    async finishPosSaleUi({ shouldPrint, isCheckout }) {
      if (shouldPrint && typeof this.printCard === "function") {
        try {
          const itemsForPrint = JSON.parse(JSON.stringify(this.carditems));
          const printResult = await this.printCard(itemsForPrint, { silent: true });
          if (!printResult || !printResult.ok) {
            this.$notify.warning(
              this.$t("printError") || "تم حفظ الطلب لكن فشلت الطباعة",
              { position: "top-right", timeout: 3500, maxToasts: 1 }
            );
          }
        } catch (printError) {
          console.error("Print error:", printError);
          this.$notify.warning(
            this.$t("printError") || "تم حفظ الطلب لكن فشلت الطباعة",
            { position: "top-right", timeout: 3500, maxToasts: 1 }
          );
        }
      }

      this.carditems = [];
      this.orderForSend.notes = "";
      this.orderForSend.creditCustomerId = null;
      this.orderForSend.clientOrderId = null;
      this.orderForSend.soldAt = null;
      this.orderForSend.isWholesale = false;
      if (typeof this.isWholesale !== "undefined") {
        this.isWholesale = false;
      }
      if (typeof this.clearOrderDiscount === "function") {
        this.clearOrderDiscount();
      }
      if (typeof this.onActiveInvoiceTabClearedAfterSale === "function") {
        this.onActiveInvoiceTabClearedAfterSale();
      }
      if (typeof this.GetAllItems === "function") {
        this.GetAllItems();
      }

      const successMessage = isCheckout
        ? shouldPrint
          ? this.$t("payAndPrint") || "دفع وطباعة"
          : this.$t("payNow") || "دفع"
        : this.$i18n.t("orderSavedAndCleared") ||
          "تم حفظ الطلب وافراغ السلة بنجاح";

      this.$notify.success(successMessage, {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    },
    parseInsufficientInventoryMessage(apiMessage) {
      if (!apiMessage || typeof apiMessage !== "string") return null;

      // New structured message: insufficientInventory|name|available|required
      const structured = apiMessage.match(
        /^insufficientInventory\|(.+)\|(-?\d+(?:\.\d+)?)\|(-?\d+(?:\.\d+)?)$/
      );
      if (structured) {
        return {
          name: structured[1],
          available: structured[2],
          required: structured[3],
        };
      }

      // Legacy English message from older API builds
      const legacy = apiMessage.match(
        /^Insufficient inventory for item '(.+)'\. Available:\s*(-?\d+(?:\.\d+)?),\s*Required:\s*(-?\d+(?:\.\d+)?)/i
      );
      if (legacy) {
        return {
          name: legacy[1],
          available: legacy[2],
          required: legacy[3],
        };
      }

      return null;
    },
    formatInsufficientInventoryMessage(parsed) {
      const key = "insufficientInventoryDetail";
      if (this.$te && this.$te(key)) {
        return this.$t(key, parsed);
      }
      return (
        `المخزون غير كافٍ للمنتج «${parsed.name}». ` +
        `المتوفر حالياً: ${parsed.available}، المطلوب في الطلب: ${parsed.required}. ` +
        `قلّل الكمية أو زوّد المخزون ثم أعد المحاولة.`
      );
    },
    mapOrderPersistErrorMessage(error) {
      let errorMessage = this.$i18n.t("error") || "حدث خطأ ما";
      if (error.response) {
        const apiMessage =
          error.response.data?.message || error.response.data?.Message;
        const inventory = this.parseInsufficientInventoryMessage(apiMessage);
        if (inventory) {
          return this.formatInsufficientInventoryMessage(inventory);
        }
        if (apiMessage && this.$te(apiMessage)) {
          return this.$t(apiMessage);
        }
        if (apiMessage) return apiMessage;
        if (error.response.status === 400) {
          return this.$i18n.t("badRequest") || "طلب غير صحيح";
        }
        if (error.response.status === 401) {
          return this.$i18n.t("unauthorized") || "غير مصرح";
        }
        if (error.response.status === 500) {
          return this.$i18n.t("serverError") || "خطأ في الخادم";
        }
      } else if (error.request) {
        errorMessage =
          this.$i18n.t("networkError") || "خطأ في الاتصال بالخادم";
      }
      return errorMessage;
    },
    async persistOrder({ skipPrint = false, isCheckout = false, printOnSave = false } = {}) {
      if (this.orderPersisting) return;

      const toastPosition = this.getOrderPersistToastPosition();

      if (this.carditems.length <= 0) {
        this.$notify.error(this.$i18n.t("emptyCartMessage"), {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }

      const stockIssue = await this.validateLocalStockForOrder();
      if (stockIssue) {
        this.$notify.error(this.formatInsufficientInventoryMessage(stockIssue), {
          position: toastPosition,
          timeout: 6500,
          maxToasts: 1,
        });
        return;
      }

      const shouldPrint = printOnSave || (!skipPrint && isCheckout);

      this.orderPersisting = true;
      this.isCheckoutPersist = isCheckout;
      this.prepareOrderPayload();

      const isCard = this.orderForSend.paymentMethod === "Card";
      const waitForServer = isCard;

      if (waitForServer) {
        this.show = true;
      }

      try {
        if (waitForServer) {
          const response = await HTTP.post("Admin/AddOrder", this.orderForSend);
          if (response?.data?.errorStatus || response?.data?.ErrorStatus) {
            throw Object.assign(
              new Error(response?.data?.message || response?.data?.Message || "AddOrder failed"),
              { response }
            );
          }
          markPosSaleAccepted();
          await applySoldPayloadToCatalog(
            this.orderForSend,
            this.selectedWarehouseId
          );
        } else {
          await enqueuePosOrder({
            payload: { ...this.orderForSend },
            warehouseId: this.selectedWarehouseId,
            soldAt: this.orderForSend.soldAt,
          });
          flushPendingOrders();
        }

        await this.finishPosSaleUi({ shouldPrint, isCheckout });
      } catch (error) {
        console.error("Order save error:", error);
        const networkFail = !error || !error.response;
        if (waitForServer && networkFail) {
          try {
            await enqueuePosOrder({
              payload: { ...this.orderForSend },
              warehouseId: this.selectedWarehouseId,
              soldAt: this.orderForSend.soldAt,
            });
            flushPendingOrders();
            await this.finishPosSaleUi({ shouldPrint, isCheckout });
            return;
          } catch (queueError) {
            console.error("Order queue error:", queueError);
          }
        }
        const apiMessage =
          error?.response?.data?.message || error?.response?.data?.Message;
        const isInventory =
          !!this.parseInsufficientInventoryMessage(apiMessage);
        this.$notify.error(this.mapOrderPersistErrorMessage(error), {
          position: "top-right",
          timeout: isInventory ? 6500 : 3000,
          maxToasts: 1,
        });
        if (error && error.response) {
          this.orderForSend.clientOrderId = null;
        }
      } finally {
        this.show = false;
        this.orderPersisting = false;
        this.isCheckoutPersist = false;
        this.cardPaymentTransactionIdForCheckout = null;
      }
    },
    addOrderAndClear(skipPrint = false) {
      return this.persistOrder({ skipPrint, isCheckout: false, printOnSave: !skipPrint });
    },
    addOrder(isPrint) {
      return this.persistOrder({ skipPrint: !isPrint, isCheckout: true });
    },
    validateCreditForOrder(toastPosition) {
      if (this.orderForSend.paymentMethod !== "Credit") return true;
      const c = this.orderForSend.creditCustomerId;
      const hasC = c != null && c !== "";
      if (hasC) return true;
      const notify = this.$notify?.error || this.$toast?.error;
      const msg =
        this.$i18n.t("pleaseSelectCreditAccount") || "اختر حساباً للدفع الآجل";
      if (notify) {
        notify(msg, {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
      }
      return false;
    },
    async checkoutWithPayment(withPrint = false) {
      if (this.carditems.length <= 0) {
        this.$notify.error(this.$i18n.t("emptyCartMessage"), {
          position: this.getOrderPersistToastPosition(),
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }

      const stockIssue = await this.validateLocalStockForOrder();
      if (stockIssue) {
        this.$notify.error(this.formatInsufficientInventoryMessage(stockIssue), {
          position: this.getOrderPersistToastPosition(),
          timeout: 6500,
          maxToasts: 1,
        });
        return;
      }

      if (this.orderForSend.paymentMethod === "Card") {
        const online = typeof navigator === "undefined" || navigator.onLine;
        if (!online) {
          this.$notify.error(
            this.$i18n.t("posCardRequiresOnline") ||
              "الدفع بالبطاقة يحتاج اتصال بالإنترنت",
            {
              position: this.getOrderPersistToastPosition(),
              timeout: 3000,
              maxToasts: 1,
            }
          );
          return;
        }
        const txId = await this.processCardPaymentBeforeCheckout();
        if (!txId) return;
        this.cardPaymentTransactionIdForCheckout = txId;
      }

      if (!this.validateCreditForOrder(this.getOrderPersistToastPosition())) {
        return;
      }

      await this.addOrder(withPrint);
    },
  },
};
