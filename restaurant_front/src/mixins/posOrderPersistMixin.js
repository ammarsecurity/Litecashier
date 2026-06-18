import { HTTP } from "@/http/api.js";
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
      printedCartBaseline: [],
      orderPersisting: false,
    };
  },
  methods: {
    getOrderPersistToastPosition() {
      const textDirection = document.documentElement.dir;
      return textDirection === "rtl" ? "top-right" : "top-left";
    },
    resolveActiveOrderId() {
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
      if (this.selectedTableId) {
        const table = this.allTables.find((t) => t.id === this.selectedTableId);
        const raw =
          table?.currentOrderId ??
          table?.currentorderid ??
          table?.current_order_id;
        const n = Number(raw || 0);
        if (Number.isFinite(n) && n > 0) {
          return n;
        }
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
        this.carditems = [];
        this.selectedTableId = null;
        this.selectedTableIds = [];
        this.orderForSend.tableId = null;
        this.orderForSend.tableIds = null;
        this.orderForSend.orderType = "Takeaway";
        this.orderForSend.numberOfGuests = 0;
        this.orderForSend.notes = "";
        this.orderForSend.pagerNumber = "";
        this.clearOrderDiscount();
        this.activeOrderId = null;
        this.tableOrders = [];
        this.resetPrintedCartBaseline();
        const quickSearchRef = this.$refs.posQuickSearchInput;
        if (quickSearchRef) {
          quickSearchRef.focus();
        }
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

      this.carditems = [];
      this.selectedTableId = null;
      this.selectedTableIds = [];
      this.orderForSend.tableId = null;
      this.orderForSend.tableIds = null;
      this.orderForSend.orderType = "Takeaway";
      this.orderForSend.numberOfGuests = 0;
      this.orderForSend.notes = "";
      this.orderForSend.pagerNumber = "";
      this.clearOrderDiscount();
      this.tableOrders = [];
      this.activeOrderId = null;
      this.resetPrintedCartBaseline();
    },
    getOrderPersistSuccessMessage({ isUpdate, isCheckout, isPrint, isDineInTableOrder }) {
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
        });
        this.$toast.success(successMessage, {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      } catch (error) {
        console.error("Order save error:", error);
        this.$toast.error(this.mapOrderPersistErrorMessage(error), {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } finally {
        this.show = false;
        this.orderPersisting = false;
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
      this.carditems = [];
      this.selectedTableId = null;
      this.selectedTableIds = [];
      this.orderForSend.tableId = null;
      this.orderForSend.tableIds = null;
      this.orderForSend.orderType = "Takeaway";
      this.orderForSend.numberOfGuests = 0;
      this.orderForSend.notes = "";
      this.orderForSend.pagerNumber = "";
      this.orderForSend.orderCode = "";
      this.clearOrderDiscount();
      this.activeOrderId = null;
      this.tableOrders = [];
      this.resetPrintedCartBaseline();
      this.clearMergedTableIdsCache();
      const quickSearchRef = this.$refs.posQuickSearchInput;
      if (quickSearchRef) {
        quickSearchRef.focus();
      }
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
