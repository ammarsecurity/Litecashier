import { HTTP } from "@/http/api.js";
import { resolvePrintServerUrl, resolveAbsoluteAssetUrl } from "@/utils/apiBase.js";
import {
  PRINT_API_TIMEOUT_MS,
  PRINT_SERVER_FETCH_TIMEOUT_MS,
  buildReceiptPrintDocument,
  ensurePrintOrderCodeInHtml,
} from "@/utils/receiptPrint.js";
import { buildA4InvoicePrintDocument } from "@/utils/a4InvoicePrint.js";

export default {
  data() {
    return {
      managedPrinters: [],
      selectedManagedPrinterId: null,
      loadingManagedPrinters: false,
      accountDefaultPrinterId: null,
      /** True after cashier changes the POS dropdown this session (temporary override). */
      printerManuallyOverridden: false,
    };
  },
  computed: {
    mainPrinter() {
      return (
        (this.managedPrinters || []).find(
          (p) =>
            (p.isMain ?? p.IsMain) && (p.isActive ?? p.IsActive) !== false
        ) || null
      );
    },
    activeCheckoutPrinters() {
      return (this.managedPrinters || []).filter(
        (p) => (p.isActive ?? p.IsActive) !== false
      );
    },
    /** Normalized print format from commercial settings: "Pos" | "A4" */
    printInvoiceFormat() {
      const fromInfo =
        this.commercialUserInfo?.printInvoiceFormat ??
        this.commercialUserInfo?.PrintInvoiceFormat;
      const raw =
        fromInfo ||
        localStorage.getItem("printInvoiceFormat") ||
        "Pos";
      return String(raw).toUpperCase() === "A4" ? "A4" : "Pos";
    },
  },
  methods: {
    getPosUserInfo() {
      try {
        if (this.userInfo && Object.keys(this.userInfo).length) {
          return this.userInfo;
        }
        return JSON.parse(localStorage.getItem("info") || "{}");
      } catch {
        return {};
      }
    },
    getPosPrinterUserId() {
      const info = this.getPosUserInfo();
      return info?.id ?? info?.Id ?? info?.userId ?? info?.UserId ?? null;
    },
    getPosSelectedPrinterStorageKey() {
      const userId = this.getPosPrinterUserId();
      return userId != null
        ? `posSelectedPrinterId_${userId}`
        : "posSelectedPrinterId";
    },
    seedAccountDefaultFromLoginInfo() {
      const info = this.getPosUserInfo();
      const fromLogin = info?.defaultPrinterId ?? info?.DefaultPrinterId ?? null;
      if (fromLogin != null && this.accountDefaultPrinterId == null) {
        this.accountDefaultPrinterId = fromLogin;
      }
    },
    async loadAccountDefaultPrinter() {
      this.seedAccountDefaultFromLoginInfo();
      try {
        const response = await HTTP.get("Printers/my-default");
        if (response.data && !response.data.errorStatus) {
          const data = response.data.data || {};
          const printerId = data.printerId ?? data.PrinterId ?? null;
          // API is source of truth (null clears a stale login-info value).
          this.accountDefaultPrinterId = printerId;
        }
      } catch (error) {
        console.error("Error loading account default printer:", error);
        // Keep seed from login info if API fails.
        this.seedAccountDefaultFromLoginInfo();
      }
    },
    async loadManagedPrinters() {
      this.loadingManagedPrinters = true;
      try {
        const [printersResponse] = await Promise.all([
          HTTP.get("Printers"),
          this.loadAccountDefaultPrinter(),
        ]);
        if (printersResponse.data && !printersResponse.data.errorStatus) {
          this.managedPrinters = printersResponse.data.data || [];
        } else {
          this.managedPrinters = [];
        }
      } catch (error) {
        console.error("Error loading managed printers:", error);
        this.managedPrinters = [];
      } finally {
        this.loadingManagedPrinters = false;
        this.printerManuallyOverridden = false;
        this.syncSelectedManagedPrinter();
      }
    },
    syncSelectedManagedPrinter() {
      const active = this.activeCheckoutPrinters;
      if (!active.length) {
        this.selectedManagedPrinterId = null;
        return;
      }

      const storageKey = this.getPosSelectedPrinterStorageKey();
      const savedId = localStorage.getItem(storageKey);
      const saved = savedId
        ? active.find((p) => String(p.id ?? p.Id) === String(savedId))
        : null;

      const accountDefault =
        this.accountDefaultPrinterId != null
          ? active.find(
              (p) =>
                String(p.id ?? p.Id) === String(this.accountDefaultPrinterId)
            )
          : null;

      const main = this.mainPrinter;
      // Account assignment wins so remote POS logins use the assigned printer.
      const pick = accountDefault || saved || main || active[0];
      this.selectedManagedPrinterId = pick?.id ?? pick?.Id ?? null;
      if (this.selectedManagedPrinterId != null) {
        localStorage.setItem(
          storageKey,
          String(this.selectedManagedPrinterId)
        );
      }
    },
    onManagedPrinterChange() {
      this.printerManuallyOverridden = true;
      if (this.selectedManagedPrinterId != null) {
        localStorage.setItem(
          this.getPosSelectedPrinterStorageKey(),
          String(this.selectedManagedPrinterId)
        );
      }
    },
    async ensurePrintPrintersReady() {
      if (!this.managedPrinters?.length) {
        await this.loadManagedPrinters();
        return;
      }
      // Re-fetch assignment before pay/print so a newly assigned printer is used.
      await this.loadAccountDefaultPrinter();
      if (!this.printerManuallyOverridden) {
        this.syncSelectedManagedPrinter();
      }
    },
    resolvePrintPrinterId() {
      const selected = this.selectedManagedPrinterId;
      if (selected != null && this.findManagedPrinter(selected)) {
        return selected;
      }
      if (
        !this.printerManuallyOverridden &&
        this.accountDefaultPrinterId != null &&
        this.findManagedPrinter(this.accountDefaultPrinterId)
      ) {
        return this.accountDefaultPrinterId;
      }
      const main = this.mainPrinter;
      return main?.id ?? main?.Id ?? null;
    },
    findManagedPrinter(printerId) {
      if (printerId == null) return null;
      return (this.managedPrinters || []).find(
        (p) => String(p.id ?? p.Id) === String(printerId)
      );
    },
    escapeHtml(text) {
      const div = document.createElement("div");
      div.textContent = text == null ? "" : String(text);
      return div.innerHTML;
    },
    resolveCreditCustomerName() {
      const id =
        this.orderForSend?.creditCustomerId ??
        this.order?.creditCustomerId ??
        this.order?.customerId;
      if (id == null) {
        return (
          this.order?.customerName ||
          this.order?.creditCustomerName ||
          ""
        );
      }
      const list = this.creditCustomers || [];
      const match = list.find(
        (c) => String(c.id ?? c.Id) === String(id)
      );
      return (
        match?.name ||
        match?.Name ||
        this.order?.customerName ||
        this.order?.creditCustomerName ||
        ""
      );
    },
    resolveWarehouseNameForPrint() {
      const id =
        this.orderForSend?.warehouseId ??
        this.selectedWarehouseId ??
        this.order?.warehouseId;
      if (id == null) return this.order?.warehouseName || "";
      const wh = (this.warehouses || []).find(
        (w) => String(w.id ?? w.Id) === String(id)
      );
      return wh?.name || wh?.Name || this.order?.warehouseName || "";
    },
    buildA4InvoicePayload() {
      const t = (key) => {
        try {
          return this.$t(key);
        } catch {
          return key;
        }
      };
      const payText =
        typeof this.getPaymentMethodText === "function"
          ? this.getPaymentMethodText.bind(this)
          : (m) => m;

      // Reports invoice modal: `order` + `customerOrderItem`
      if (this.order && Array.isArray(this.customerOrderItem)) {
        const items =
          this.customerOrderItemsWithTotalPrice || this.customerOrderItem || [];
        const wholesale = !!(this.order?.isWholesale ?? this.order?.IsWholesale);
        return {
          t,
          storeName: this.commercialUserInfo?.storeName || "LiteCashier",
          logoUrl: resolveAbsoluteAssetUrl(this.commercialUserInfo?.logo) || null,
          footerCreditText: this.commercialUserInfo?.footerCreditText || null,
          footerCreditPhone: this.commercialUserInfo?.footerCreditPhone || null,
          appName: t("app-name"),
          orderCode: this.order?.orderCode || this.orderForSend?.orderCode || "",
          dateTime:
            typeof this.formatDate === "function"
              ? this.formatDate(this.order?.insertDate)
              : this.order?.insertDate || "",
          employeeName:
            this.orderEmployeeName ||
            this.userInfo?.name ||
            this.userInfo?.fullName ||
            "---",
          paymentMethod: this.order?.paymentMethod || "Cash",
          paymentMethodLabel: payText(this.order?.paymentMethod || "Cash"),
          paymentStatus:
            this.order?.paymentStatus || this.order?.PaymentStatus || "",
          isCheckout: this.order?.isCheckout ?? this.order?.IsCheckout ?? true,
          priceModeLabel: wholesale
            ? t("wholesalePriceMode") || "جملة"
            : t("retailPriceMode") || "مفرد",
          creditCustomerName: this.resolveCreditCustomerName(),
          warehouseName: this.resolveWarehouseNameForPrint(),
          discountAmount: Number(this.order?.discountAmount || 0),
          grandTotal: (() => {
            const discount = Number(this.order?.discountAmount || 0);
            if (typeof this.totaPrice === "number") {
              return Math.max(this.totaPrice - discount, 0);
            }
            return Number(
              this.order?.finalTotal ??
                this.order?.totalPrice ??
                0
            );
          })(),
          itemsCount:
            this.reportInvoiceItemCount ??
            items.reduce((s, i) => s + (Number(i.quantity) || 0), 0),
          currency: t("currency") || "",
          notes: this.order?.notes || "",
          lines: items.map((item) => {
            const unit =
              typeof this.getSellingPrice === "function"
                ? this.getSellingPrice(item)
                : Number(item.price || item.sellingPrice || 0);
            const hasDisc =
              typeof this.hasDiscount === "function"
                ? this.hasDiscount(item)
                : false;
            return {
              name: item.item?.name || item.name || "—",
              quantity: Number(item.quantity) || 0,
              unitPrice: unit,
              hasDiscount: hasDisc,
            };
          }),
        };
      }

      // POS cart
      const wholesale = !!this.isWholesale;
      const lines = (this.carditems || []).map((item) => {
        const unit =
          typeof this.cartLineUnitPrice === "function"
            ? this.cartLineUnitPrice(item)
            : Number(item.price || item.sellingPrice || 0);
        const hasDisc =
          typeof this.cartLineHasDiscount === "function"
            ? this.cartLineHasDiscount(item)
            : false;
        return {
          name: item.name || "—",
          quantity: Number(item.quantity) || 0,
          unitPrice: unit,
          hasDiscount: hasDisc,
        };
      });

      const discountAmount = Number(this.orderDiscountAmount || 0);
      const grandTotal =
        typeof this.finalOrderTotal === "number"
          ? this.finalOrderTotal
          : Math.max(
              Number(this.totaPrice || 0) - discountAmount,
              0
            ) ||
            lines.reduce((s, l) => s + l.unitPrice * l.quantity, 0);

      return {
        t,
        storeName: this.commercialUserInfo?.storeName || "LiteCashier",
        logoUrl: resolveAbsoluteAssetUrl(this.commercialUserInfo?.logo) || null,
        footerCreditText: this.commercialUserInfo?.footerCreditText || null,
        footerCreditPhone: this.commercialUserInfo?.footerCreditPhone || null,
        appName: t("app-name"),
        orderCode:
          (typeof this.ensureOrderCodeForPrint === "function"
            ? this.ensureOrderCodeForPrint()
            : this.orderForSend?.orderCode) || "",
        dateTime:
          typeof this.getCurrentDateTime === "function"
            ? this.getCurrentDateTime()
            : new Date().toLocaleString("en-GB"),
        employeeName: this.userInfo?.name || this.userInfo?.fullName || "---",
        paymentMethod: this.orderForSend?.paymentMethod || "Cash",
        paymentMethodLabel: payText(this.orderForSend?.paymentMethod || "Cash"),
        paymentStatus: "",
        isCheckout: true,
        priceModeLabel: wholesale
          ? t("wholesalePriceMode") || "جملة"
          : t("retailPriceMode") || "مفرد",
        creditCustomerName: this.resolveCreditCustomerName(),
        warehouseName: this.resolveWarehouseNameForPrint(),
        discountAmount,
        grandTotal,
        itemsCount:
          this.totalCardItems ??
          lines.reduce((s, l) => s + l.quantity, 0),
        currency: t("currency") || "",
        notes: this.orderForSend?.notes || "",
        lines,
      };
    },
    async getReceiptHtmlContent() {
      await this.$nextTick();

      if (this.printInvoiceFormat === "A4") {
        return buildA4InvoicePrintDocument(this.buildA4InvoicePayload());
      }

      const printElement = document.getElementById("print");
      if (!printElement) return "";

      const orderCode =
        typeof this.ensureOrderCodeForPrint === "function"
          ? this.ensureOrderCodeForPrint()
          : this.orderForSend?.orderCode;

      let inner = printElement.innerHTML;
      inner = ensurePrintOrderCodeInHtml(inner, orderCode, (t) =>
        this.escapeHtml(t)
      );

      const title =
        `${this.$t("invoice_number") || "فاتورة"} - ${orderCode || ""}`.trim();
      return buildReceiptPrintDocument(inner, title);
    },
    async checkPrintServerHealth() {
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 4000);
        const response = await fetch(`${resolvePrintServerUrl()}/health`, {
          method: "GET",
          signal: controller.signal,
        });
        clearTimeout(timeoutId);
        if (!response.ok) return false;
        const health = await response.json();
        return health.status === "ok";
      } catch (error) {
        console.warn("Print server health check failed:", error);
        return false;
      }
    },
    async printViaPrintServer(htmlContent, printer) {
      if (!htmlContent || !printer) return false;

      const serverOk = await this.checkPrintServerHealth();
      if (!serverOk) return false;

      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(
          () => controller.abort(),
          PRINT_SERVER_FETCH_TIMEOUT_MS
        );

        const response = await fetch(`${resolvePrintServerUrl()}/print`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            printerName: printer.printerName ?? printer.PrinterName,
            printerType: printer.printerType ?? printer.PrinterType ?? "windows",
            htmlContent,
          }),
          signal: controller.signal,
        });

        clearTimeout(timeoutId);
        if (!response.ok) return false;

        const result = await response.json();
        return !!result.success;
      } catch (error) {
        console.warn("Direct print server error:", error);
        return false;
      }
    },
    async printViaApi(printerId, htmlContent) {
      const response = await HTTP.post(
        `Printers/${printerId}/print`,
        { htmlContent, copies: 1 },
        { timeout: PRINT_API_TIMEOUT_MS }
      );
      return !!(response.data && !response.data.errorStatus);
    },
    browserPrintReceipt(htmlContent) {
      return new Promise((resolve) => {
        const isA4 = this.printInvoiceFormat === "A4";
        const features = isA4
          ? "width=900,height=1100"
          : "width=420,height=720";
        const printWindow = window.open("", "_blank", features);
        if (!printWindow) {
          this.fallbackPrintIframe(htmlContent);
          resolve(true);
          return;
        }

        printWindow.document.write(htmlContent);
        printWindow.document.close();
        setTimeout(() => {
          printWindow.focus();
          printWindow.print();
          setTimeout(() => {
            printWindow.close();
            resolve(true);
          }, isA4 ? 600 : 400);
        }, isA4 ? 450 : 350);
      });
    },
    fallbackPrintIframe(htmlContent) {
      const iframe = document.createElement("iframe");
      iframe.style.position = "fixed";
      iframe.style.right = "0";
      iframe.style.bottom = "0";
      iframe.style.width = "0";
      iframe.style.height = "0";
      iframe.style.border = "0";
      document.body.appendChild(iframe);

      const doc = iframe.contentWindow?.document;
      if (!doc) {
        document.body.removeChild(iframe);
        return;
      }
      doc.open();
      doc.write(htmlContent);
      doc.close();
      setTimeout(() => {
        iframe.contentWindow?.focus();
        iframe.contentWindow?.print();
        setTimeout(() => document.body.removeChild(iframe), 500);
      }, 300);
    },
    notifyPrintSuccess(silent = false) {
      if (silent) return;
      this.$notify.success(this.$t("printSuccess") || "تم الطباعة بنجاح", {
        position: "top-right",
        timeout: 2000,
        maxToasts: 1,
      });
    },
    notifyPrintError(message) {
      this.$notify.error(
        message || this.$t("printError") || "حدث خطأ أثناء الطباعة",
        { position: "top-right", timeout: 3000, maxToasts: 1 }
      );
    },
    async printCard(itemsToPrint = null, printOptions = {}) {
      const raiseOnError = !!(printOptions && printOptions.raiseOnError);
      const silent = !!(printOptions && printOptions.silent);
      let originalCarditems = null;

      try {
        if (typeof this.ensureOrderCodeForPrint === "function") {
          this.ensureOrderCodeForPrint();
        }
        await this.ensurePrintPrintersReady();

        const printItems = itemsToPrint || this.carditems;
        if (!printItems || printItems.length === 0) {
          if (raiseOnError) throw new Error("empty cart");
          return { ok: false, reason: "emptyCart" };
        }

        originalCarditems = this.carditems;
        if (itemsToPrint) {
          this.carditems = printItems;
        }
        await this.$nextTick();

        const htmlContent = await this.getReceiptHtmlContent();
        if (!htmlContent) {
          if (raiseOnError) throw new Error("Print element not found");
          return { ok: false, reason: "noPrintElement" };
        }

        const printerId = this.resolvePrintPrinterId();
        const printer = this.findManagedPrinter(printerId);
        if (printerId != null) {
          this.selectedManagedPrinterId = printerId;
        }

        if (printerId && printer) {
          console.info(
            "[print] using printer",
            printerId,
            printer.name ?? printer.Name,
            "accountDefault=",
            this.accountDefaultPrinterId
          );
          try {
            const apiOk = await this.printViaApi(printerId, htmlContent);
            if (apiOk) {
              this.notifyPrintSuccess(silent);
              return { ok: true, method: "api", printerId };
            }
          } catch (apiError) {
            console.warn("[print] API queue failed, trying print server:", apiError);
          }

          const directOk = await this.printViaPrintServer(htmlContent, printer);
          if (directOk) {
            this.notifyPrintSuccess(silent);
            return { ok: true, method: "printServer", printerId };
          }

          console.warn(
            "[print] assigned/managed printer failed; falling back to browser dialog"
          );
        }

        await this.browserPrintReceipt(htmlContent);
        this.notifyPrintSuccess(silent);
        return { ok: true, method: "browser", printerId: printerId || null };
      } catch (error) {
        console.error("printCard error:", error);
        if (raiseOnError) throw error;
        this.notifyPrintError(error.message);
        return { ok: false, reason: "error" };
      } finally {
        if (itemsToPrint && originalCarditems !== null) {
          this.carditems = originalCarditems;
          await this.$nextTick();
        }
      }
    },
  },
};
