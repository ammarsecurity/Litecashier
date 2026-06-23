import {
  CARD_PAYMENT_REQUEST_TIMEOUT_MS,
  CARD_PAYMENT_STATUS_POLL_MS,
  startPublicCardPaymentSale,
  getPublicCardPaymentStatus,
  cancelPublicCardPayment,
} from "@/http/api.js";

/**
 * Card payment on device for anonymous public order page (kiosk / in-store).
 */
export default {
  data() {
    return {
      cardPaymentEnabled: false,
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
      _publicCardPaymentPollTimer: null,
      _publicCardPaymentPollStartedAt: 0,
      _publicCardPaymentWaitResolve: null,
      _publicCardPaymentDismissResolve: null,
    };
  },
  beforeDestroy() {
    this.stopPublicCardPaymentWatch();
    this.resolvePublicCardPaymentDismiss();
  },
  methods: {
    resolvePublicCardPaymentDismiss() {
      if (typeof this._publicCardPaymentDismissResolve === "function") {
        const resolve = this._publicCardPaymentDismissResolve;
        this._publicCardPaymentDismissResolve = null;
        resolve();
      }
    },
    waitForPublicCardPaymentModalDismiss() {
      return new Promise((resolve) => {
        this._publicCardPaymentDismissResolve = resolve;
      });
    },
    stopPublicCardPaymentWatch() {
      if (this._publicCardPaymentPollTimer != null) {
        clearInterval(this._publicCardPaymentPollTimer);
        this._publicCardPaymentPollTimer = null;
      }
    },
    openPublicCardPaymentWaitModal(amount) {
      this.cardPaymentWait = {
        show: true,
        transactionId: null,
        status: "Starting",
        amount,
        currencyCode: "IQD",
        deviceName: "",
        message: this.$t("cardPaymentWaitHint") || "أكمل الدفع على جهاز البطاقة",
        authCode: "",
        refNo: "",
        errorMessage: "",
        cancelling: false,
      };
    },
    finishPublicCardPaymentWait(transactionId) {
      this.stopPublicCardPaymentWatch();
      this._publicCardPaymentPollStartedAt = 0;
      if (typeof this._publicCardPaymentWaitResolve === "function") {
        const resolve = this._publicCardPaymentWaitResolve;
        this._publicCardPaymentWaitResolve = null;
        resolve(transactionId || null);
      }
    },
    async applyPublicCardPaymentStatus(payload) {
      if (!payload) return;

      const transactionId = payload.transactionId ?? payload.TransactionId;
      if (transactionId != null) {
        this.cardPaymentWait.transactionId = transactionId;
      }
      this.cardPaymentWait.status = payload.status || this.cardPaymentWait.status;
      if (payload.amount != null) this.cardPaymentWait.amount = payload.amount;
      if (payload.currencyCode) this.cardPaymentWait.currencyCode = payload.currencyCode;
      if (payload.deviceName) this.cardPaymentWait.deviceName = payload.deviceName;
      if (payload.message) this.cardPaymentWait.message = payload.message;
      if (payload.authCode) this.cardPaymentWait.authCode = payload.authCode;
      if (payload.refNo) this.cardPaymentWait.refNo = payload.refNo;

      const status = String(payload.status || "").toLowerCase();
      if (status === "success") {
        this.cardPaymentWait.errorMessage = "";
        if (payload.isTerminal || payload.IsTerminal) {
          this.finishPublicCardPaymentWait(transactionId);
        }
        return;
      }

      if (
        status === "failed" ||
        ((payload.isTerminal || payload.IsTerminal) && status !== "success")
      ) {
        this.cardPaymentWait.status = "Failed";
        this.cardPaymentWait.errorMessage =
          payload.message || this.$t("cardPaymentFailed") || "فشل الدفع بالبطاقة";
        this.finishPublicCardPaymentWait(null);
      }
    },
    async pollPublicCardPaymentStatusOnce(transactionId) {
      try {
        const response = await getPublicCardPaymentStatus(this.commercialUserId, transactionId);
        const payload = response?.data?.data;
        if (payload) {
          await this.applyPublicCardPaymentStatus(payload);
        }
      } catch (pollError) {
        console.warn("Public card payment poll failed:", pollError);
      }
    },
    waitForPublicCardPaymentTerminal(transactionId) {
      return new Promise((resolve) => {
        this._publicCardPaymentWaitResolve = resolve;
        this.stopPublicCardPaymentWatch();
        this._publicCardPaymentPollStartedAt = Date.now();
        this.pollPublicCardPaymentStatusOnce(transactionId);
        this._publicCardPaymentPollTimer = setInterval(() => {
          if (!this._publicCardPaymentPollStartedAt) return;
          const elapsed = Date.now() - this._publicCardPaymentPollStartedAt;
          if (elapsed >= CARD_PAYMENT_REQUEST_TIMEOUT_MS) {
            this.stopPublicCardPaymentWatch();
            this.cardPaymentWait.status = "Failed";
            this.cardPaymentWait.errorMessage =
              this.$t("cardPaymentTimeout") ||
              "انتهت مهلة انتظار الجهاز. تحقق من نجاح الدفع قبل إعادة المحاولة.";
            this.finishPublicCardPaymentWait(null);
            return;
          }
          this.pollPublicCardPaymentStatusOnce(transactionId);
        }, CARD_PAYMENT_STATUS_POLL_MS);
      });
    },
    mapPublicCardPaymentError(error) {
      const key = error?.response?.data?.message;
      if (key && this.$te(key)) {
        return this.$t(key);
      }
      return key || this.$t("cardPaymentFailed") || "فشل الدفع بالبطاقة";
    },
    async processPublicCardPayment(amount) {
      if (!this.commercialUserId) {
        return null;
      }
      if (!amount || amount <= 0) {
        this.$bvToast.toast(this.$t("invalidPaymentAmount") || "مبلغ الدفع غير صالح", {
          title: this.$t("error") || "خطأ",
          variant: "danger",
          solid: true,
        });
        return null;
      }

      this.openPublicCardPaymentWaitModal(amount);

      try {
        const response = await startPublicCardPaymentSale(this.commercialUserId, {
          amount,
          tipAmount: 0,
          currencyCode: "IQD",
        });

        const payload = response?.data;
        const data = payload?.data;
        const txId = data?.transactionId;

        if (payload?.errorStatus || !txId) {
          this.cardPaymentWait.status = "Failed";
          this.cardPaymentWait.errorMessage = this.mapPublicCardPaymentError({
            response: { data: payload },
          });
          await this.waitForPublicCardPaymentModalDismiss();
          return null;
        }

        this.cardPaymentWait.transactionId = txId;
        if (data.deviceName) this.cardPaymentWait.deviceName = data.deviceName;
        if (data.status) this.cardPaymentWait.status = data.status;

        const terminalId = await this.waitForPublicCardPaymentTerminal(txId);

        if (!terminalId) {
          await this.waitForPublicCardPaymentModalDismiss();
          return null;
        }

        this.cardPaymentWait.show = false;
        return terminalId;
      } catch (error) {
        console.error("Public card payment failed:", error);
        this.cardPaymentWait.status = "Failed";
        this.cardPaymentWait.errorMessage = this.mapPublicCardPaymentError(error);
        await this.waitForPublicCardPaymentModalDismiss();
        return null;
      }
    },
    async onPublicCardPaymentWaitCancel() {
      const transactionId = this.cardPaymentWait.transactionId;
      if (!transactionId || this.cardPaymentWait.cancelling || !this.commercialUserId) {
        return;
      }

      this.cardPaymentWait.cancelling = true;
      try {
        await cancelPublicCardPayment(this.commercialUserId, transactionId);
        await this.pollPublicCardPaymentStatusOnce(transactionId);
      } catch (cancelError) {
        console.warn("Public card payment cancel failed:", cancelError);
        this.cardPaymentWait.status = "Failed";
        this.cardPaymentWait.errorMessage = this.mapPublicCardPaymentError(cancelError);
      } finally {
        this.cardPaymentWait.cancelling = false;
        this.stopPublicCardPaymentWatch();
        this.finishPublicCardPaymentWait(null);
        await new Promise((r) => setTimeout(r, 600));
        this.cardPaymentWait.show = false;
        this.resolvePublicCardPaymentDismiss();
      }
    },
    onPublicCardPaymentWaitClose() {
      this.cardPaymentWait.show = false;
      this.stopPublicCardPaymentWatch();
      this.resolvePublicCardPaymentDismiss();
    },
  },
};
