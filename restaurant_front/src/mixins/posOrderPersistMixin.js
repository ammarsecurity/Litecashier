import {
  HTTP,
  CARD_PAYMENT_REQUEST_TIMEOUT_MS,
  CARD_PAYMENT_STATUS_POLL_MS,
  startCardPaymentSale,
  getCardPaymentStatus,
  cancelCardPayment,
} from "@/http/api.js";
import signalRService from "@/services/signalr.js";
import {
  cloneCartBaseline,
  computeKitchenPrintDelta,
} from "@/utils/cartPrintDelta.js";
import { mergeCartLinesForOrderPayload } from "@/utils/mergeCartLines.js";

/**
 * Shared POS/Waiter order save vs update (AddOrder / UpdateOrder).
 */
export default {
  data() {
    return {
      activeOrderId: null,
      _orderSessionGen: 0,
      printedCartBaseline: [],
      orderPersisting: false,
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
    this.setupCardPaymentSignalRListener();
  },
  beforeDestroy() {
    this.teardownCardPaymentSignalRListener();
    this.stopCardPaymentStatusWatch();
  },
  methods: {
    getOrderPersistToastPosition() {
      const textDirection = document.documentElement.dir;
      return textDirection === "rtl" ? "top-right" : "top-left";
    },
    bumpOrderSession() {
      this._orderSessionGen = (Number(this._orderSessionGen) || 0) + 1;
      return this._orderSessionGen;
    },
    isOrderSessionCurrent(sessionGen) {
      if (sessionGen == null) {
        return true;
      }
      return Number(sessionGen) === Number(this._orderSessionGen);
    },
    canModifyCart() {
      if (this.loadingTableOrders) {
        return false;
      }
      return true;
    },
    guardCartModification() {
      if (this.canModifyCart()) {
        return true;
      }
      const toastPosition = this.getOrderPersistToastPosition();
      this.$toast.info(
        this.$i18n.t("orderSessionLoadingWait") ||
          "جاري تحميل الطلب، انتظر قليلاً...",
        {
          position: toastPosition,
          timeout: 2000,
          maxToasts: 1,
        }
      );
      return false;
    },
    resetOrderSession(options = {}) {
      const {
        orderType = "Takeaway",
        clearNotes = true,
        resetPayment = false,
        focusQuickSearch = false,
        silent = true,
      } = options;

      this.bumpOrderSession();

      this.carditems = [];
      this.activeOrderId = null;
      this.tableOrders = [];
      this.selectedTableId = null;
      this.selectedTableIds = [];

      if (this.orderForSend) {
        this.orderForSend.tableId = null;
        this.orderForSend.tableIds = null;
        this.orderForSend.orderType = orderType;
        this.orderForSend.numberOfGuests = 0;
        this.orderForSend.orderCode = "";
        this.orderForSend.reservationId = null;
        if (clearNotes) {
          this.orderForSend.notes = "";
          this.orderForSend.pagerNumber = "";
        }
      }

      if (typeof this.clearOrderDiscount === "function") {
        this.clearOrderDiscount();
      }
      if (typeof this.resetPrintedCartBaseline === "function") {
        this.resetPrintedCartBaseline();
      }
      if (typeof this.clearMergedTableIdsCache === "function") {
        this.clearMergedTableIdsCache();
      }
      if (typeof this.clearActiveTableReservation === "function") {
        this.clearActiveTableReservation();
      }
      if (resetPayment && typeof this.setPosPaymentMethod === "function") {
        this.setPosPaymentMethod("Cash");
      }

      if (focusQuickSearch) {
        this.$nextTick(() => {
          const quickSearchRef = this.$refs.posQuickSearchInput;
          if (quickSearchRef) {
            quickSearchRef.focus();
          }
        });
      }

      if (!silent) {
        const msg =
          orderType === "Delivery"
            ? this.$i18n.t("newDeliveryOrderStarted") ||
              "تم بدء طلب توصيل جديد"
            : this.$i18n.t("newOffTableOrderStarted") ||
              "تم بدء طلب جديد بدون طاولة";
        this.$toast.info(msg, {
          position: this.getOrderPersistToastPosition(),
          timeout: 2000,
          maxToasts: 1,
        });
      }
    },
    startOffTableOrderSession(orderType = "Takeaway") {
      const hadContext =
        !!this.selectedTableId ||
        (Array.isArray(this.carditems) && this.carditems.length > 0) ||
        !!this.activeOrderId;
      this.resetOrderSession({
        orderType,
        silent: !hadContext,
      });
      if (orderType === "Delivery" && this.orderForSend) {
        this.orderForSend.deliveryStatus = this.orderForSend.deliveryStatus || "Pending";
      }
    },
    resolveActiveOrderId() {
      const isDineIn =
        this.orderForSend?.orderType === "DineIn" &&
        !!this.orderForSend?.tableId &&
        !!this.selectedTableId;

      if (!isDineIn) {
        return null;
      }

      if (this.activeOrderId) {
        const fromActive = Number(this.activeOrderId);
        if (Number.isFinite(fromActive) && fromActive > 0) {
          return fromActive;
        }
      }
      const fromOrders = this.tableOrders?.[0];
      const orderId = fromOrders?.id ?? fromOrders?.Id;
      if (orderId) {
        const n = Number(orderId);
        if (Number.isFinite(n) && n > 0) {
          return n;
        }
      }
      const table = this.allTables.find((t) => t.id === this.selectedTableId);
      const raw =
        table?.currentOrderId ??
        table?.currentorderid ??
        table?.current_order_id;
      const n = Number(raw || 0);
      if (Number.isFinite(n) && n > 0) {
        return n;
      }
      return null;
    },
    clearMergedTableIdsCache(tableId = null) {
      if (!this.mergedTableIdsCache) {
        this.mergedTableIdsCache = {};
        return;
      }
      if (tableId == null) {
        this.mergedTableIdsCache = {};
        return;
      }
      delete this.mergedTableIdsCache[tableId];
    },
    syncPrintedCartBaselineFromCart() {
      this.printedCartBaseline = cloneCartBaseline(this.carditems);
    },
    resetPrintedCartBaseline() {
      this.printedCartBaseline = [];
    },
    syncActiveOrderIdFromTable(table, activeOrder) {
      const fromOrder = activeOrder?.id ?? activeOrder?.Id;
      if (fromOrder) {
        this.activeOrderId = Number(fromOrder);
        return;
      }
      const raw =
        table?.currentOrderId ??
        table?.currentorderid ??
        table?.current_order_id;
      const n = Number(raw || 0);
      this.activeOrderId = Number.isFinite(n) && n > 0 ? n : null;
    },
    getTableIdsForOrderPayload() {
      if (this.selectedTableIds.length > 1) {
        return [...this.selectedTableIds];
      }
      if (this.mergedTableIds.length > 1) {
        return [...this.mergedTableIds];
      }
      if (this.selectedTableId) {
        return [this.selectedTableId];
      }
      return [];
    },
    applyTableIdsToOrderPayload(tableIdsToUse) {
      if (tableIdsToUse.length > 1) {
        this.orderForSend.tableIds = [...tableIdsToUse];
        this.orderForSend.tableId = tableIdsToUse[0];
      } else if (tableIdsToUse.length === 1) {
        this.orderForSend.tableId = tableIdsToUse[0];
        this.orderForSend.tableIds = null;
      } else {
        this.orderForSend.tableId = null;
        this.orderForSend.tableIds = null;
      }
    },
    applyDeliveryFieldsToOrderPayload() {
      if (this.orderForSend.orderType !== "Delivery") {
        this.orderForSend.deliveryDriverId = null;
        this.orderForSend.deliveryStatus = null;
        this.orderForSend.deliveryAddress = null;
        this.orderForSend.deliveryPhoneNumber = null;
        this.orderForSend.deliveryCustomerName = null;
        this.orderForSend.deliveryFee = null;
        this.orderForSend.newDriverName = null;
        this.orderForSend.newDriverPhone = null;
        this.orderForSend.newDriverAddress = null;
        this.orderForSend.newDriverVehicleType = null;
        this.orderForSend.newDriverVehicleNumber = null;
      } else {
        if (!this.orderForSend.deliveryStatus) {
          this.orderForSend.deliveryStatus = "Pending";
        }
        this.orderForSend.newDriverName = null;
        this.orderForSend.newDriverPhone = null;
        this.orderForSend.newDriverAddress = null;
        this.orderForSend.newDriverVehicleType = null;
        this.orderForSend.newDriverVehicleNumber = null;
      }
    },
    prepareOrderPayload(isNewOrder) {
      this.orderForSend.paymentMethod =
        this.orderForSend.paymentMethod || "Cash";
      this.orderForSend.customerOrderItem = mergeCartLinesForOrderPayload(
        this.carditems
      );

      if (isNewOrder) {
        const existing = String(this.orderForSend.orderCode || "").trim();
        if (!existing || existing === "---") {
          this.orderForSend.orderCode = Math.floor(
            Math.random() * 1000000000
          )
            .toString()
            .padStart(9, "0");
        }
      }

      const tableIdsToUse = this.getTableIdsForOrderPayload();
      this.applyTableIdsToOrderPayload(tableIdsToUse);

      if (!this.orderForSend.reservationId) {
        this.orderForSend.reservationId = null;
      }

      this.applyDeliveryFieldsToOrderPayload();

      if (
        this.orderForSend.orderType === "DineIn" &&
        this.orderForSend.tableId
      ) {
        this.orderForSend.numberOfGuests = Math.max(
          1,
          Number(this.orderForSend.numberOfGuests || 1)
        );
      } else {
        this.orderForSend.numberOfGuests = 0;
      }

      const discountPayload = this.buildOrderDiscountPayload();
      Object.assign(this.orderForSend, discountPayload);

      this.orderForSend.isCheckout = !!this.isCheckoutPersist;
      this.orderForSend.cardPaymentTransactionId =
        this.cardPaymentTransactionIdForCheckout || null;
    },
    validateDeliveryForPersist(toastPosition) {
      if (this.orderForSend.orderType !== "Delivery") {
        return true;
      }
      if (this.useExistingCustomer && !this.selectedDeliveryCustomerId) {
        this.$toast.error(
          this.$i18n.t("pleaseSelectCustomer") || "يرجى اختيار عميل من القائمة",
          { position: toastPosition, timeout: 2500, maxToasts: 1 }
        );
        return false;
      }
      if (
        !this.orderForSend.deliveryCustomerName ||
        !this.orderForSend.deliveryCustomerName.trim()
      ) {
        this.$toast.error(
          this.$i18n.t("pleaseEnterCustomerName") || "يرجى إدخال اسم المستلم",
          { position: toastPosition, timeout: 2500, maxToasts: 1 }
        );
        return false;
      }
      if (
        !this.orderForSend.deliveryPhoneNumber ||
        !this.orderForSend.deliveryPhoneNumber.trim()
      ) {
        this.$toast.error(
          this.$i18n.t("pleaseEnterPhoneNumber") ||
            "يرجى إدخال رقم هاتف المستلم",
          { position: toastPosition, timeout: 2500, maxToasts: 1 }
        );
        return false;
      }
      if (
        !this.orderForSend.deliveryAddress ||
        !this.orderForSend.deliveryAddress.trim()
      ) {
        this.$toast.error(
          this.$i18n.t("pleaseEnterDeliveryAddress") ||
            "يرجى إدخال عنوان التوصيل",
          { position: toastPosition, timeout: 2500, maxToasts: 1 }
        );
        return false;
      }
      if (!this.orderForSend.deliveryDriverId) {
        this.$toast.error(
          this.$i18n.t("pleaseSelectDriver") || "يرجى اختيار سائق",
          { position: toastPosition, timeout: 2500, maxToasts: 1 }
        );
        return false;
      }
      return true;
    },
    mapOrderPersistErrorMessage(error) {
      let errorMessage = this.$i18n.t("error") || "حدث خطأ ما";
      if (error.response) {
        const apiMessage = error.response.data?.message;
        if (apiMessage === "activeTableOrderExists") {
          return (
            this.$t("activeTableOrderExists") ||
            "يوجد طلب نشط على الطاولة"
          );
        }
        if (apiMessage) {
          return apiMessage;
        }
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
    setActiveOrderIdFromResponse(savedOrder) {
      const id = savedOrder?.id ?? savedOrder?.Id;
      if (id) {
        this.activeOrderId = Number(id);
      }
    },
    async closeTablesForCheckout() {
      let tableIdsToUse = this.getTableIdsForOrderPayload();
      if (tableIdsToUse.length === 0 && this.selectedTableId) {
        tableIdsToUse = [this.selectedTableId];
      }
      if (tableIdsToUse.length === 0) {
        return;
      }
      if (tableIdsToUse.length > 1) {
        await HTTP.put("Admin/CloseTableOrder", tableIdsToUse);
      } else {
        await HTTP.put(`Admin/CloseTableOrder?tableId=${tableIdsToUse[0]}`);
      }
      this.activeOrderId = null;
      this.tableOrders = [];
      this.resetPrintedCartBaseline();
    },
    async refreshAfterOrderSave({
      isCheckout,
      isDineInTableOrder,
      tableIdToReload,
    }) {
      this.orderForSend.creditEmployeeId = null;
      this.orderForSend.creditCustomerId = null;

      if (isCheckout) {
        if (isDineInTableOrder) {
          try {
            await this.closeTablesForCheckout();
          } catch (closeError) {
            console.error("Failed to close table order after checkout:", closeError);
          }
        }
        await this.getTables();
        this.resetOrderSession({
          orderType: "Takeaway",
          clearNotes: true,
          focusQuickSearch: true,
          silent: true,
        });
        return;
      }

      await this.getTables();
      if (isDineInTableOrder && tableIdToReload) {
        const savedTable = this.allTables.find((t) => t.id === tableIdToReload);
        if (savedTable) {
          await this.selectTable(savedTable, null);
        }
        return;
      }

      this.resetOrderSession({
        orderType: "Takeaway",
        clearNotes: true,
        silent: true,
      });
    },
    getOrderPersistSuccessMessage({ isUpdate, isCheckout, isPrint, isDineInTableOrder, isCreditCheckout }) {
      if (isCheckout && isCreditCheckout) {
        return this.$t("creditCheckoutSuccess") || "تم تسجيل الطلب على الحساب الآجل";
      }
      if (isCheckout) {
        return isPrint
          ? this.$t("payAndPrint") || "دفع وطباعة"
          : this.$t("payNow") || "دفع";
      }
      if (isUpdate) {
        return (
          this.$t("orderUpdatedSuccessfully") || "تم تحديث الطلب بنجاح"
        );
      }
      if (isDineInTableOrder) {
        return this.$t("addOrderSucsses") || "تم حفظ الطلب بنجاح";
      }
      return (
        this.$i18n.t("orderSavedAndCleared") ||
        "تم حفظ الطلب وافراغ السلة بنجاح"
      );
    },
    async persistOrder({ skipPrint = false, isCheckout = false } = {}) {
      if (this.orderPersisting) {
        return;
      }

      const toastPosition = this.getOrderPersistToastPosition();

      if (this.carditems.length <= 0) {
        this.$toast.error(this.$i18n.t("emptyCartMessage"), {
          position: toastPosition,
          timeout: 2500,
          maxToasts: 1,
        });
        return;
      }

      if (!this.validateCreditForOrder(toastPosition)) {
        return;
      }

      if (!this.validateDeliveryForPersist(toastPosition)) {
        return;
      }

      const orderId = this.resolveActiveOrderId();
      const isUpdate = !!orderId;
      const shouldPrint = !skipPrint;
      const isDineInTableOrder =
        this.orderForSend.orderType === "DineIn" && !!this.orderForSend.tableId;
      const tableIdToReload = this.selectedTableId;

      const fullCartSnapshot = shouldPrint
        ? JSON.parse(JSON.stringify(this.carditems))
        : null;
      const kitchenPrintItems =
        shouldPrint && !isCheckout && fullCartSnapshot
          ? computeKitchenPrintDelta(
              fullCartSnapshot,
              this.printedCartBaseline
            )
          : null;
      const itemsForPrintSnapshot =
        shouldPrint && isCheckout
          ? fullCartSnapshot
          : kitchenPrintItems;

      this.orderPersisting = true;
      this.isCheckoutPersist = isCheckout;
      this.show = true;
      this.prepareOrderPayload(!isUpdate);

      try {
        let response;
        if (isUpdate) {
          response = await HTTP.put(
            `Admin/UpdateOrder/${orderId}`,
            this.orderForSend
          );
        } else {
          response = await HTTP.post("Admin/AddOrder", this.orderForSend);
        }

        if (!response) {
          return;
        }

        this.setActiveOrderIdFromResponse(response.data?.data);

        if (shouldPrint && !isCheckout && fullCartSnapshot?.length > 0) {
          if (!kitchenPrintItems || kitchenPrintItems.length === 0) {
            this.$toast.info(
              this.$t("noNewItemsToPrint") ||
                "لا توجد أصناف جديدة للطباعة",
              {
                position: "top-right",
                timeout: 2500,
                maxToasts: 1,
              }
            );
          }
        }

        if (shouldPrint && itemsForPrintSnapshot?.length > 0) {
          if (isCheckout) {
            await this.$nextTick();
            try {
              await this.printCard(itemsForPrintSnapshot, {
                raiseOnError: true,
                cashierReceiptOnly: true,
              });
            } catch (printError) {
              console.error("Print error:", printError);
              const persisted =
                this.$i18n.t("addOrderSucsses") ||
                this.$i18n.t("orderSavedAndCleared") ||
                "";
              const printErr = this.$t("printError") || "خطأ بالطباعة";
              const warnText = persisted ? `${printErr}. ${persisted}` : printErr;
              this.$toast.warning(warnText, {
                position: "top-right",
                timeout: 3500,
                maxToasts: 1,
              });
            }
          } else {
            try {
              await this.ensurePrintPrintersReady();
              const printResult = await this.printCard(itemsForPrintSnapshot, {
                departmentPrintersOnly: true,
              });
              if (printResult && !printResult.ok) {
                this.$toast.warning(
                  this.$t("printError") ||
                    "تم حفظ الطلب لكن فشلت الطباعة",
                  {
                    position: "top-right",
                    timeout: 3500,
                    maxToasts: 1,
                  }
                );
              }
            } catch (printError) {
              console.error("Print error:", printError);
              this.$toast.warning(
                this.$t("printError") || "تم حفظ الطلب لكن فشلت الطباعة",
                {
                  position: "top-right",
                  timeout: 3500,
                  maxToasts: 1,
                }
              );
            }
          }
        }

        if (!isCheckout && fullCartSnapshot?.length > 0) {
          this.syncPrintedCartBaselineFromCart();
        }

        const wasCreditCheckout =
          isCheckout && this.orderForSend.paymentMethod === "Credit";

        await this.refreshAfterOrderSave({
          isCheckout,
          isDineInTableOrder,
          tableIdToReload,
        });

        const successMessage = this.getOrderPersistSuccessMessage({
          isUpdate,
          isCheckout,
          isPrint: shouldPrint,
          isDineInTableOrder,
          isCreditCheckout: wasCreditCheckout,
        });
        this.$toast.success(successMessage, {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      } catch (error) {
        const apiMessage = error.response?.data?.message;
        if (
          !isUpdate &&
          apiMessage === "activeTableOrderExists" &&
          this.selectedTableId &&
          typeof this.loadExistingTableOrders === "function"
        ) {
          const table =
            (Array.isArray(this.allTables)
              ? this.allTables.find((t) => t.id === this.selectedTableId)
              : null) || { id: this.selectedTableId };
          const loaded = await this.loadExistingTableOrders(table);
          if (loaded && this.resolveActiveOrderId()) {
            this.show = false;
            this.orderPersisting = false;
            this.isCheckoutPersist = false;
            return this.persistOrder({ skipPrint, isCheckout });
          }
        }

        console.error("Order save error:", error);
        this.$toast.error(this.mapOrderPersistErrorMessage(error), {
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
        this.$toast.error(
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
            this.$toast.success(this.$t("cardPaymentRecovered") || "تم تأكيد الدفع بالبطاقة", {
              position: toastPosition,
              timeout: 3000,
              maxToasts: 1,
            });
            return recoveredTxId;
          }

          this.$toast.error(this.mapCardPaymentErrorMessage({ response: { data: payload } }), {
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
          this.$toast.success(this.$t("cardPaymentRecovered") || "تم تأكيد الدفع بالبطاقة", {
            position: toastPosition,
            timeout: 3000,
            maxToasts: 1,
          });
          return recoveredTxId;
        }

        this.$toast.error(this.mapCardPaymentErrorMessage(error), {
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
    addOrderAndClear(skipPrint = false) {
      return this.persistOrder({ skipPrint, isCheckout: false });
    },
    addOrder(isPrint) {
      return this.persistOrder({ skipPrint: !isPrint, isCheckout: true });
    },
    canCancelDineInOrder() {
      if (this.orderForSend?.orderType !== "DineIn" || !this.selectedTableId) {
        return false;
      }
      if (this.resolveActiveOrderId()) {
        return true;
      }
      const table = this.allTables?.find((t) => t.id === this.selectedTableId);
      const status = String(table?.status || "").trim().toLowerCase();
      const raw =
        table?.currentOrderId ??
        table?.currentorderid ??
        table?.current_order_id;
      const hasOrderId = Number(raw || 0) > 0;
      return status === "occupied" || status === "reserved" || hasOrderId;
    },
    mapCancelOrderErrorMessage(error) {
      const apiMessage = error?.response?.data?.message;
      if (apiMessage === "cannotCancelPaidOrder") {
        return (
          this.$t("cannotCancelPaidOrder") ||
          "لا يمكن إلغاء طلب مدفوع"
        );
      }
      if (apiMessage) {
        return apiMessage;
      }
      return (
        this.$t("errorCancellingOrder") ||
        "حدث خطأ أثناء إلغاء الطلب"
      );
    },
    openCancelDineInOrderModal() {
      this.$nextTick(() => {
        this.$bvModal.show("modal-cancel-order");
      });
    },
    resetLocalStateAfterDineInCancel() {
      this.resetOrderSession({
        orderType: "Takeaway",
        clearNotes: true,
        focusQuickSearch: true,
        silent: true,
      });
    },
    async cancelDineInTableOrderAfterAuth() {
      const toastPosition = this.getOrderPersistToastPosition();
      let tableIdsToCancel = this.getTableIdsForOrderPayload();
      if (tableIdsToCancel.length === 0 && this.selectedTableId) {
        tableIdsToCancel.push(this.selectedTableId);
      }
      if (tableIdsToCancel.length === 0) {
        this.$toast.error(
          this.$t("errorCancellingOrder") || "حدث خطأ أثناء إلغاء الطلب",
          { position: toastPosition, timeout: 2500, maxToasts: 1 }
        );
        return;
      }

      try {
        if (tableIdsToCancel.length > 1) {
          await HTTP.put("Admin/CancelTableOrder", tableIdsToCancel);
        } else {
          await HTTP.put(`Admin/CancelTableOrder?tableId=${tableIdsToCancel[0]}`);
        }

        this.resetLocalStateAfterDineInCancel();
        await this.getTables();

        this.$toast.success(
          this.$t("orderCancelledSuccessfully") || "تم إلغاء الطلب بنجاح",
          { position: toastPosition, timeout: 2000, maxToasts: 1 }
        );
      } catch (error) {
        console.error("Cancel table order error:", error);
        this.$toast.error(this.mapCancelOrderErrorMessage(error), {
          position: toastPosition,
          timeout: 3000,
          maxToasts: 1,
        });
      }
    },
  },
};
