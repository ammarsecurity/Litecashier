import {
  HTTP,
  CARD_PAYMENT_REQUEST_TIMEOUT_MS,
  CARD_PAYMENT_STATUS_POLL_MS,
  startCardPaymentSale,
  getCardPaymentStatus,
  cancelCardPayment,
} from "@/http/api.js";
import signalRService from "@/services/signalr.js";

export default {
  data() {
    return {
      cardPaymentTransactionIdForCheckout: null,
      isCheckoutPersist: false,
      cardPaymentWait: {
        show: false,
        transactionId: null,
        status: "Starting",
        amount: 0,
        currencyCode: "IQD",
        deviceName: "",
        message: "",
        authCode: "",
        refNo: "",
        errorMessage: "",
        cancelling: false,
      },
      _cardPaymentPollTimer: null,
      _cardPaymentPollStartedAt: 0,
      _cardPaymentWaitResolve: null,
      _cardPaymentSignalRHandler: null,
    };
  },
  mounted() {
    signalRService.startConnection().then(() => {
      this.setupCardPaymentSignalRListener();
    });
  },
  beforeDestroy() {
    this.teardownCardPaymentSignalRListener();
    this.stopCardPaymentStatusWatch();
  },
  methods: {
    resolveCheckoutPaymentAmount() {
      if (typeof this.finalOrderTotal === "number" && !Number.isNaN(this.finalOrderTotal)) {
        return Math.round(this.finalOrderTotal);
      }
      const sub = Number(this.totaPrice) || 0;
      const discount =
        typeof this.orderDiscountAmount === "number"
          ? this.orderDiscountAmount
          : Number(this.orderDiscountAmount || 0);
      return Math.round(Math.max(sub - discount, 0));
    },
    mapCardPaymentErrorMessage(error) {
      const key = error?.response?.data?.message;
      if (key && this.$te(key)) {
        return this.$t(key);
      }
      if (key) {
        return key;
      }
      return this.$t("cardPaymentFailed") || "فشل الدفع بالبطاقة";
    },
    isCardPaymentRequestTimeout(error) {
      return (
        error?.code === "ECONNABORTED" ||
        String(error?.message || "").toLowerCase().includes("timeout")
      );
    },
    async recoverCardPaymentTransaction(amount, transactionId = null) {
      try {
        const params = { amount };
        if (transactionId != null) {
          params.transactionId = transactionId;
        }
        const response = await HTTP.get("CardPayments/recover", {
          params,
          timeout: 15000,
        });
        const payload = response?.data;
        const txId = payload?.data?.transactionId;
        if (!payload?.errorStatus && txId) {
          return txId;
        }
      } catch (recoverError) {
        console.warn("Card payment recover failed:", recoverError);
      }
      return null;
    },
    async reconcileCardPaymentTransaction(transactionId) {
      if (!transactionId) return null;
      try {
        const response = await getCardPaymentStatus(transactionId);
        const payload = response?.data?.data;
        if (
          payload &&
          String(payload.status || "").toLowerCase() === "success"
        ) {
          return payload.transactionId;
        }
      } catch (statusError) {
        console.warn("Card payment status reconcile failed:", statusError);
      }
      return null;
    },
    async verifyCardPaymentTransaction(transactionId) {
      if (!transactionId) return null;
      try {
        const response = await HTTP.get(`CardPayments/verify/${transactionId}`, {
          timeout: 15000,
        });
        const payload = response?.data;
        const txId = payload?.data?.transactionId;
        if (!payload?.errorStatus && txId) {
          return txId;
        }
      } catch (verifyError) {
        console.warn("Card payment verify failed:", verifyError);
      }
      return null;
    },
    async tryRecoverCardPayment(amount, error, transactionId = null) {
      const txIdFromError =
        transactionId ?? error?.response?.data?.data?.transactionId;
      if (txIdFromError) {
        const reconciledId = await this.reconcileCardPaymentTransaction(txIdFromError);
        if (reconciledId) {
          return reconciledId;
        }
        const verifiedId = await this.verifyCardPaymentTransaction(txIdFromError);
        if (verifiedId) {
          return verifiedId;
        }
      }
      return this.recoverCardPaymentTransaction(amount, txIdFromError);
    },
    resetCardPaymentWaitState() {
      this.cardPaymentWait = {
        show: false,
        transactionId: null,
        status: "Starting",
        amount: 0,
        currencyCode: "IQD",
        deviceName: "",
        message: "",
        authCode: "",
        refNo: "",
        errorMessage: "",
        cancelling: false,
      };
    },
    openCardPaymentWaitModal(amount) {
      this.cardPaymentWait = {
        show: true,
        transactionId: null,
        status: "Starting",
        amount,
        currencyCode: "IQD",
        deviceName: "",
        message: "",
        authCode: "",
        refNo: "",
        errorMessage: "",
        cancelling: false,
      };
    },
    async applyCardPaymentStatusPayload(payload, options = {}) {
      const { skipReconcile = false } = options;
      if (!payload || payload.transactionId == null) return;
      if (
        this.cardPaymentWait.transactionId != null &&
        Number(this.cardPaymentWait.transactionId) !== Number(payload.transactionId)
      ) {
        return;
      }

      this.cardPaymentWait.transactionId = payload.transactionId;
      this.cardPaymentWait.status = payload.status || this.cardPaymentWait.status;
      if (payload.amount != null) {
        this.cardPaymentWait.amount = payload.amount;
      }
      if (payload.currencyCode) {
        this.cardPaymentWait.currencyCode = payload.currencyCode;
      }
      if (payload.deviceName) {
        this.cardPaymentWait.deviceName = payload.deviceName;
      }
      if (payload.authCode) {
        this.cardPaymentWait.authCode = payload.authCode;
      }
      if (payload.refNo) {
        this.cardPaymentWait.refNo = payload.refNo;
      }
      if (payload.message) {
        const key = payload.message;
        this.cardPaymentWait.message = this.$te(key) ? this.$t(key) : key;
      }

      if (payload.isTerminal) {
        if (
          !skipReconcile &&
          String(payload.status || "").toLowerCase() === "failed"
        ) {
          const reconciledId = await this.reconcileCardPaymentTransaction(
            payload.transactionId
          );
          if (reconciledId) {
            try {
              const response = await getCardPaymentStatus(reconciledId);
              const fresh = response?.data?.data;
              if (
                fresh &&
                String(fresh.status || "").toLowerCase() === "success"
              ) {
                await this.applyCardPaymentStatusPayload(
                  { ...fresh, isTerminal: true },
                  { skipReconcile: true }
                );
                return;
              }
            } catch (refreshError) {
              console.warn("Card payment refresh after reconcile failed:", refreshError);
            }
          }
        }

        if (String(payload.status || "").toLowerCase() === "failed") {
          this.cardPaymentWait.errorMessage =
            this.cardPaymentWait.message ||
            this.$t("cardPaymentFailed") ||
            "فشل الدفع بالبطاقة";
        }
        this.finishCardPaymentWait(payload);
      }
    },
    async pollCardPaymentStatusOnce(transactionId) {
      try {
        const response = await getCardPaymentStatus(transactionId);
        const payload = response?.data?.data;
        if (payload) {
          await this.applyCardPaymentStatusPayload(payload);
        }
      } catch (pollError) {
        console.warn("Card payment status poll failed:", pollError);
      }
    },
    stopCardPaymentStatusWatch() {
      if (this._cardPaymentPollTimer != null) {
        clearInterval(this._cardPaymentPollTimer);
        this._cardPaymentPollTimer = null;
      }
    },
    finishCardPaymentWait(payload) {
      this.stopCardPaymentStatusWatch();
      this._cardPaymentPollStartedAt = 0;
      if (typeof this._cardPaymentWaitResolve !== "function") {
        return;
      }
      const resolve = this._cardPaymentWaitResolve;
      this._cardPaymentWaitResolve = null;
      const isSuccess =
        String(payload?.status || "").toLowerCase() === "success";
      resolve(isSuccess ? payload.transactionId : null);
    },
    waitForCardPaymentTerminal(transactionId, amount) {
      return new Promise((resolve) => {
        this._cardPaymentWaitResolve = resolve;
        this.stopCardPaymentStatusWatch();
        this._cardPaymentPollStartedAt = Date.now();
        this.pollCardPaymentStatusOnce(transactionId);
        this._cardPaymentPollTimer = setInterval(() => {
          if (!this._cardPaymentPollStartedAt) return;
          const elapsed = Date.now() - this._cardPaymentPollStartedAt;
          if (elapsed >= CARD_PAYMENT_REQUEST_TIMEOUT_MS) {
            this.stopCardPaymentStatusWatch();
            if (typeof this._cardPaymentWaitResolve !== "function") {
              return;
            }
            this.handleCardPaymentWatchTimeout(amount, transactionId).then(resolve);
            return;
          }
          this.pollCardPaymentStatusOnce(transactionId);
        }, CARD_PAYMENT_STATUS_POLL_MS);
      });
    },
    async handleCardPaymentWatchTimeout(amount, transactionId) {
      const reconciledId = await this.reconcileCardPaymentTransaction(transactionId);
      if (reconciledId) {
        await this.applyCardPaymentStatusPayload(
          {
            transactionId: reconciledId,
            status: "Success",
            isTerminal: true,
            message: this.$t("cardPaymentRecovered") || "تم تأكيد الدفع بالبطاقة",
          },
          { skipReconcile: true }
        );
        return reconciledId;
      }

      const verifiedId = await this.verifyCardPaymentTransaction(transactionId);
      if (verifiedId) {
        await this.applyCardPaymentStatusPayload(
          {
            transactionId: verifiedId,
            status: "Success",
            isTerminal: true,
            message: this.$t("cardPaymentRecovered") || "تم تأكيد الدفع بالبطاقة",
          },
          { skipReconcile: true }
        );
        return verifiedId;
      }

      const recoveredId = await this.recoverCardPaymentTransaction(
        amount,
        transactionId
      );
      if (recoveredId) {
        await this.applyCardPaymentStatusPayload(
          {
            transactionId: recoveredId,
            status: "Success",
            isTerminal: true,
            message: this.$t("cardPaymentRecovered") || "تم تأكيد الدفع بالبطاقة",
          },
          { skipReconcile: true }
        );
        return recoveredId;
      }

      this.cardPaymentWait.status = "Failed";
      this.cardPaymentWait.errorMessage =
        this.$t("cardPaymentTimeout") ||
        "انتهت مهلة انتظار الجهاز. تحقق من نجاح الدفع قبل إعادة المحاولة.";
      this.cardPaymentWait.message = this.cardPaymentWait.errorMessage;
      await this.delayMs(1500);
      this.cardPaymentWait.show = false;
      this._cardPaymentPollStartedAt = 0;
      return null;
    },
    delayMs(ms) {
      return new Promise((resolve) => setTimeout(resolve, ms));
    },
    setupCardPaymentSignalRListener() {
      if (this._cardPaymentSignalRHandler) return;
      this._cardPaymentSignalRHandler = async (payload) => {
        await this.applyCardPaymentStatusPayload(payload);
      };
      signalRService.on("CardPaymentStatusChanged", this._cardPaymentSignalRHandler);
    },
    teardownCardPaymentSignalRListener() {
      if (this._cardPaymentSignalRHandler) {
        signalRService.off("CardPaymentStatusChanged", this._cardPaymentSignalRHandler);
        this._cardPaymentSignalRHandler = null;
      }
    },
    async onCardPaymentWaitCancel() {
      const transactionId = this.cardPaymentWait.transactionId;
      if (!transactionId || this.cardPaymentWait.cancelling) return;

      this.cardPaymentWait.cancelling = true;
      try {
        await cancelCardPayment(transactionId);
        await this.pollCardPaymentStatusOnce(transactionId);
      } catch (cancelError) {
        console.warn("Card payment cancel failed:", cancelError);
        this.cardPaymentWait.status = "Failed";
        this.cardPaymentWait.errorMessage =
          this.mapCardPaymentErrorMessage(cancelError) ||
          this.$t("cardPaymentFailed") ||
          "فشل الدفع بالبطاقة";
      } finally {
        this.cardPaymentWait.cancelling = false;
        this.stopCardPaymentStatusWatch();
        if (typeof this._cardPaymentWaitResolve === "function") {
          const resolve = this._cardPaymentWaitResolve;
          this._cardPaymentWaitResolve = null;
          resolve(null);
        }
        await this.delayMs(800);
        this.cardPaymentWait.show = false;
      }
    },
    onCardPaymentWaitClose() {
      this.cardPaymentWait.show = false;
      this.stopCardPaymentStatusWatch();
    },
    async processCardPaymentBeforeCheckout() {
      const toastPosition = this.getOrderPersistToastPosition();
      const amount = this.resolveCheckoutPaymentAmount();
      if (amount <= 0) {
        this.$notify.error(
          this.$t("invalidPaymentAmount") || "مبلغ الدفع غير صالح",
          { position: toastPosition, timeout: 3000, maxToasts: 1 }
        );
        return null;
      }

      this.openCardPaymentWaitModal(amount);

      try {
        const response = await startCardPaymentSale({
          amount,
          tipAmount: 0,
          currencyCode: "IQD",
        });

        const payload = response?.data;
        const data = payload?.data;
        const txId = data?.transactionId;

        if (payload?.errorStatus || !txId) {
          const recoveredTxId = await this.tryRecoverCardPayment(
            amount,
            { response: { data: payload } },
            txId
          );
          this.cardPaymentWait.show = false;
          if (recoveredTxId) {
            this.$notify.success(this.$t("cardPaymentRecovered") || "تم تأكيد الدفع بالبطاقة", {
              position: toastPosition,
              timeout: 3000,
              maxToasts: 1,
            });
            return recoveredTxId;
          }

          this.$notify.error(this.mapCardPaymentErrorMessage({ response: { data: payload } }), {
            position: toastPosition,
            timeout: 4000,
            maxToasts: 1,
          });
          return null;
        }

        this.applyCardPaymentStatusPayload({
          transactionId: txId,
          status: data.status || "Pending",
          amount: data.amount ?? amount,
          currencyCode: data.currencyCode || "IQD",
          deviceName: data.deviceName || "",
          isTerminal: false,
        });

        const resultTxId = await this.waitForCardPaymentTerminal(txId, amount);
        if (resultTxId) {
          await this.delayMs(1500);
          this.cardPaymentWait.show = false;
          return resultTxId;
        }

        if (this.cardPaymentWait.show) {
          await this.delayMs(1500);
          this.cardPaymentWait.show = false;
        }
        return null;
      } catch (error) {
        console.error("Card payment error:", error);
        this.stopCardPaymentStatusWatch();

        const recoveredTxId = await this.tryRecoverCardPayment(
          amount,
          error,
          this.cardPaymentWait.transactionId
        );
        this.cardPaymentWait.show = false;

        if (recoveredTxId) {
          this.$notify.success(this.$t("cardPaymentRecovered") || "تم تأكيد الدفع بالبطاقة", {
            position: toastPosition,
            timeout: 3000,
            maxToasts: 1,
          });
          return recoveredTxId;
        }

        this.$notify.error(this.mapCardPaymentErrorMessage(error), {
          position: toastPosition,
          timeout: 4000,
          maxToasts: 1,
        });
        return null;
      } finally {
        this.stopCardPaymentStatusWatch();
        if (typeof this._cardPaymentWaitResolve === "function") {
          this._cardPaymentWaitResolve = null;
        }
      }
    },
  },
};