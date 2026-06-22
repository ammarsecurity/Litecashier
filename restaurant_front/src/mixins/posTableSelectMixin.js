import { HTTP } from "@/http/api.js";
import { mergeCartLines } from "@/utils/mergeCartLines.js";

/**
 * Shared table selection / floor-plan status logic for POS and Waiter views.
 */
export default {
  methods: {
    getTableCurrentOrderId(table) {
      const raw =
        table?.currentOrderId ??
        table?.currentorderid ??
        table?.current_order_id ??
        null;
      const normalized = Number(raw || 0);
      return Number.isFinite(normalized) && normalized > 0 ? normalized : 0;
    },
    tableHasActiveOrder(table) {
      return this.getTableCurrentOrderId(table) > 0;
    },
    getTableOccupancyFlags(table) {
      const tableStatus = String(table?.status || "").trim().toLowerCase();
      const hasActiveOrder = this.tableHasActiveOrder(table);
      const isOutOfService =
        tableStatus === "outofservice" || tableStatus === "out_of_service";
      const isReserved = tableStatus === "reserved";
      const isOccupied = tableStatus === "occupied" || hasActiveOrder;
      const isAvailable = !isOccupied && !isReserved && !isOutOfService;
      return { tableStatus, hasActiveOrder, isOutOfService, isReserved, isOccupied, isAvailable };
    },
    posFloorTableStatusClass(status) {
      const m = {
        Available: "pos-fp-chip-avail",
        Occupied: "pos-fp-chip-occ",
        Reserved: "pos-fp-chip-res",
        OutOfService: "pos-fp-chip-out",
      };
      return m[status] || "pos-fp-chip-avail";
    },
    posFloorTableStatusClassForTable(table) {
      const { isOutOfService, isReserved, isOccupied } = this.getTableOccupancyFlags(table);
      if (isOutOfService) {
        return "pos-fp-chip-out";
      }
      if (isReserved) {
        return "pos-fp-chip-res";
      }
      if (isOccupied) {
        return "pos-fp-chip-occ";
      }
      return "pos-fp-chip-avail";
    },
    attachTableForOrderSession(table, options = {}) {
      const { silent = false } = options;
      this.activeOrderId = null;
      if (typeof this.resetPrintedCartBaseline === "function") {
        this.resetPrintedCartBaseline();
      }
      this.orderForSend.orderCode = "";
      this.selectedTableId = table.id;
      this.selectedTableIds = [table.id];
      this.orderForSend.tableId = table.id;
      this.orderForSend.tableIds = null;
      this.orderForSend.orderType = "DineIn";
      if (!this.orderForSend.numberOfGuests || this.orderForSend.numberOfGuests < 1) {
        this.orderForSend.numberOfGuests = 1;
      }
      this.carditems = [];
      this.tableOrders = [];

      if (!silent) {
        const { isReserved } = this.getTableOccupancyFlags(table);
        const msg = isReserved
          ? this.$i18n.t("reservedTableSessionStarted") || "طاولة محجوزة — يمكنك بدء الطلب"
          : this.$i18n.t("newTableOrderStarted") || "تم بدء طلب جديد للطاولة";
        this.$toast.info(msg, {
          position: "top-right",
          timeout: 2000,
          maxToasts: 1,
        });
      }
    },
    markTableOccupiedLocally(table, orderId) {
      if (!table) {
        return;
      }
      table.status = "Occupied";
      table.currentOrderId = orderId;
      const match = Array.isArray(this.allTables)
        ? this.allTables.find((t) => t.id === table.id)
        : null;
      if (match && match !== table) {
        match.status = "Occupied";
        match.currentOrderId = orderId;
      }
    },
    async loadExistingTableOrders(table) {
      if (!table?.id) {
        return false;
      }

      this.loadingTableOrders = true;
      try {
        const response = await HTTP.get(`Admin/GetTableOrders?tableId=${table.id}`);
        const orders = response.data?.data || [];
        if (!Array.isArray(orders) || orders.length === 0) {
          return false;
        }

        this.tableOrders = orders;
        const activeOrder = orders[0] || null;
        const activeOrderId = activeOrder?.id ?? activeOrder?.Id ?? null;
        this.syncActiveOrderIdFromTable(table, activeOrder);
        this.markTableOccupiedLocally(table, activeOrderId);
        this.orderForSend.numberOfGuests = Number(activeOrder?.numberOfGuests || 0);
        const loadedOrderCode = activeOrder?.orderCode ?? activeOrder?.OrderCode ?? "";
        if (loadedOrderCode) {
          this.orderForSend.orderCode = String(loadedOrderCode);
        }

        this.carditems = [];
        orders.forEach((order) => {
          if (!order.customerOrderItem) {
            return;
          }
          order.customerOrderItem.forEach((orderItem) => {
            if (orderItem.item && !orderItem.isDeleted) {
              const sellingPrice = orderItem.sellingPrice || 0;
              const discountPrice = orderItem.item.disCountPrice || 0;
              const finalPrice =
                discountPrice > 0 && discountPrice !== sellingPrice
                  ? discountPrice
                  : sellingPrice;

              this.carditems.push({
                id: orderItem.item.id,
                name: orderItem.item.name,
                price: sellingPrice,
                disCountPrice: discountPrice,
                quantity: orderItem.quantity || 1,
                code: orderItem.item.code,
                image: orderItem.item.image,
                total: finalPrice * (orderItem.quantity || 1),
                tags: orderItem.item.tags || "مواد اخرى",
                sourceOrderId: order.id,
                sourceOrderItemId: orderItem.id,
                lineNote: (orderItem.notes || orderItem.Notes || "").trim() || undefined,
              });
            }
          });
        });

        this.carditems = mergeCartLines(this.carditems);
        this.syncPrintedCartBaselineFromCart();
        this.selectedTableId = table.id;

        const mergedIds = await this.loadMergedTableIds(table.id);
        const mergedIdsArray = Array.isArray(mergedIds) ? mergedIds : [table.id];
        this.selectedTableIds = mergedIdsArray;

        if (mergedIdsArray.length > 1) {
          this.orderForSend.tableIds = [...mergedIdsArray];
          this.orderForSend.tableId = mergedIdsArray[0];
        } else {
          this.orderForSend.tableId = table.id;
          this.orderForSend.tableIds = null;
        }
        this.orderForSend.orderType = "DineIn";

        if (typeof this.getTables === "function") {
          this.getTables();
        }

        this.$toast.success(
          this.$i18n.t("tableOrdersLoaded") || "تم تحميل طلبات الطاولة",
          {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          }
        );
        return true;
      } catch (error) {
        console.error("Error loading table orders:", error);
        this.$toast.error(
          this.$i18n.t("errorLoadingTableOrders") || "خطأ في تحميل طلبات الطاولة",
          {
            position: "top-right",
            timeout: 2000,
            maxToasts: 1,
          }
        );
        return false;
      } finally {
        this.loadingTableOrders = false;
      }
    },
    async onPosFloorPlanTableClick(table, event) {
      this.posFloorPlanGateVisible = false;
      if (!table) {
        return;
      }

      const { isOutOfService, isAvailable } = this.getTableOccupancyFlags(table);
      if (isOutOfService) {
        return;
      }

      if (isAvailable) {
        const loaded = await this.loadExistingTableOrders(table);
        if (loaded) {
          this.resetPosFloorPlanGateTools();
          return;
        }

        this.floorPlanGuestModal.table = table;
        this.floorPlanGuestModal.tableNumber = table.tableNumber || "";
        this.floorPlanGuestModal.count = 1;
        this.$bvModal.show("modal-floor-table-guests");
        return;
      }

      await this.selectTable(table, event || null);
      this.resetPosFloorPlanGateTools();
    },
    async selectTable(table, event) {
      this.clearMergedTableIdsCache(table?.id);
      const { isOutOfService, isOccupied, isAvailable } = this.getTableOccupancyFlags(table);
      if (isOutOfService) {
        return;
      }

      const isMultiSelect = event && (event.ctrlKey || event.metaKey);
      if (isMultiSelect && (isOccupied || isAvailable)) {
        if (!this.selectedTableIds.includes(table.id)) {
          this.selectedTableIds.push(table.id);
        } else {
          this.selectedTableIds = this.selectedTableIds.filter((id) => id !== table.id);
        }
        return;
      }

      const loaded = await this.loadExistingTableOrders(table);
      if (loaded) {
        return;
      }

      this.attachTableForOrderSession(table);
    },
  },
};
