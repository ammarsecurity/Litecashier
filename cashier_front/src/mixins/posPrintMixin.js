import { HTTP } from "@/http/api.js";
import { resolvePrintServerUrl } from "@/utils/apiBase.js";
import {
  PRINT_API_TIMEOUT_MS,
  PRINT_SERVER_FETCH_TIMEOUT_MS,
  buildReceiptPrintDocument,
  ensurePrintOrderCodeInHtml,
} from "@/utils/receiptPrint.js";

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
    async getReceiptHtmlContent() {
      await this.$nextTick();
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
        const printWindow = window.open("", "_blank", "width=420,height=720");
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
          }, 400);
        }, 350);
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
