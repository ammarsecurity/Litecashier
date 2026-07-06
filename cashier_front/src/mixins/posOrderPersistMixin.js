import { HTTP } from "@/http/api.js";
import { mergeCartLinesForOrderPayload } from "@/utils/mergeCartLines.js";

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
    },
    mapOrderPersistErrorMessage(error) {
      let errorMessage = this.$i18n.t("error") || "حدث خطأ ما";
      if (error.response) {
        const apiMessage = error.response.data?.message;
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

      const shouldPrint = printOnSave || (!skipPrint && isCheckout);

      this.orderPersisting = true;
      this.isCheckoutPersist = isCheckout;
      this.show = true;
      this.prepareOrderPayload();

      try {
        const response = await HTTP.post("Admin/AddOrder", this.orderForSend);
        if (!response) return;

        if (shouldPrint && typeof this.printCard === "function") {
          try {
            const itemsForPrint = JSON.parse(JSON.stringify(this.carditems));
            const printResult = await this.printCard(itemsForPrint, { silent: true });
            if (!printResult?.ok) {
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
        if (typeof this.clearOrderDiscount === "function") {
          this.clearOrderDiscount();
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
      } catch (error) {
        console.error("Order save error:", error);
        this.$notify.error(this.mapOrderPersistErrorMessage(error), {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
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

      if (this.orderForSend.paymentMethod === "Card") {
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
