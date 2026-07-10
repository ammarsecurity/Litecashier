<template>
  <div class="main-content-wrapper">
    <AppHeader />
    <div class="print-server-page-container">
      <div class="print-server-page-content">
        <!-- Header Section -->
        <div class="print-server-header-section">
          <div class="print-server-header-content">
            <h1 class="print-server-page-title">
              <b-icon icon="server" class="me-2"></b-icon>
              {{ $t("printServerManagement") || "إدارة خادم الطباعة" }}
            </h1>
          </div>
        </div>

        <!-- Server Not Available - Download Section -->
        <div v-if="!serverStatus && !loading" class="server-not-available-card">
          <div class="server-not-available-header">
            <b-icon icon="exclamation-triangle-fill" class="error-icon-large"></b-icon>
            <h2 class="server-not-available-title">
              {{ $t("serverNotAvailable") || "الخادم غير متاح" }}
            </h2>
          </div>
          <div class="server-not-available-body">
            <p class="server-not-available-message">
              {{ $t("serverNotAvailableMessage") || "الخدمة غير متاحة. يرجى تحميل وتشغيل نظام الطباعة (Print Server) أولاً" }}
            </p>
            
            <div class="download-section">
              <button 
                class="btn btn-download-large" 
                @click="downloadPrintServer"
                :disabled="downloading"
              >
                <b-icon icon="download" class="me-2"></b-icon>
                {{ downloading ? ($t("downloading") || "جاري التحميل...") : ($t("downloadPrintServer") || "تحميل Print Server") }}
              </button>
            </div>

            <div class="install-instructions-section">
              <h4 class="instructions-title">
                <b-icon icon="info-circle-fill" class="me-2"></b-icon>
                {{ $t("installInstructions") || "تعليمات التثبيت والتشغيل" }}
              </h4>
              <ol class="instructions-list-detailed">
                <li>
                  <strong>{{ $t("installStep1") || "الخطوة 1:" }}</strong>
                  {{ $t("installStep1Desc") || "حمّل ملف Print Server من الزر أعلاه" }}
                </li>
                <li>
                  <strong>{{ $t("installStep2") || "الخطوة 2:" }}</strong>
                  {{ $t("installStep2Desc") || "استخرج الملف المضغوط (ZIP) في أي مجلد على جهازك" }}
                </li>
                <li>
                  <strong>{{ $t("installStep3") || "الخطوة 3:" }}</strong>
                  {{ $t("installStep3Desc") || "انقر نقراً مزدوجاً على ملف start_print_server.bat (في Windows)" }}
                </li>
                <li>
                  <strong>{{ $t("installStep4") || "الخطوة 4:" }}</strong>
                  {{ $t("installStep4Desc") || "سيتم تثبيت المتطلبات تلقائياً وتشغيل الخادم. انتظر حتى تظهر رسالة 'Starting server on http://localhost:5000'" }}
                </li>
                <li>
                  <strong>{{ $t("installStep5") || "الخطوة 5:" }}</strong>
                  {{ $t("installStep5Desc") || "ارجع إلى هذه الصفحة واضغط زر 'تحديث' لفحص حالة الخادم" }}
                </li>
              </ol>
              
              <div class="alternative-instructions">
                <h5>{{ $t("alternativeMethod") || "طريقة بديلة (يدوية):" }}</h5>
                <div class="command-box-large">
                  <code class="command-text-large">python print_server.py</code>
                  <button 
                    class="btn-copy-large" 
                    @click="copyCommand('python print_server.py')"
                    :title="$t('copyCommand') || 'نسخ الأمر'"
                  >
                    <b-icon icon="clipboard"></b-icon>
                    {{ $t("copyCommand") || "نسخ" }}
                  </button>
                </div>
                <p class="command-help">
                  {{ $t("commandHelp") || "افتح Terminal أو Command Prompt في مجلد Print Server وشغّل الأمر أعلاه" }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Server Status Card -->
        <div class="print-server-status-card" v-if="serverStatus || loading">
          <div class="print-server-status-header">
            <b-icon icon="activity" class="me-2"></b-icon>
            <h3 class="print-server-status-title">
              {{ $t("serverStatus") || "حالة الخادم" }}
            </h3>
            <button 
              class="btn-refresh" 
              @click="checkServerHealth"
              :disabled="loading"
            >
              <b-icon icon="arrow-clockwise" :class="{ 'spinning': loading }"></b-icon>
              {{ $t("refresh") || "تحديث" }}
            </button>
          </div>
          <div class="print-server-status-body">
            <div v-if="loading" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("checking") || "جاري الفحص..." }}</span>
            </div>
            <div v-else-if="serverStatus" class="status-info">
              <div class="status-item">
                <span class="status-label">{{ $t("serverStatus") || "حالة الخادم" }}:</span>
                <span 
                  class="status-badge" 
                  :class="serverStatus.status === 'ok' ? 'status-success' : 'status-error'"
                >
                  <b-icon 
                    :icon="serverStatus.status === 'ok' ? 'check-circle-fill' : 'x-circle-fill'"
                    class="me-1"
                  ></b-icon>
                  {{ serverStatus.status === 'ok' ? ($t("online") || "متصل") : ($t("offline") || "غير متصل") }}
                </span>
              </div>
              <div class="status-item" v-if="serverStatus.printer">
                <span class="status-label">{{ $t("printerAvailable") || "الطابعة متاحة" }}:</span>
                <span 
                  class="status-badge" 
                  :class="serverStatus.printer.available ? 'status-success' : 'status-error'"
                >
                  <b-icon 
                    :icon="serverStatus.printer.available ? 'check-circle-fill' : 'x-circle-fill'"
                    class="me-1"
                  ></b-icon>
                  {{ serverStatus.printer.available ? ($t("yes") || "نعم") : ($t("no") || "لا") }}
                </span>
              </div>
              <div class="status-item" v-if="serverStatus.config">
                <span class="status-label">{{ $t("printerType") || "نوع الطابعة" }}:</span>
                <span class="status-value">{{ serverStatus.config.printer_type || 'N/A' }}</span>
              </div>
              <div class="status-item" v-if="serverStatus.config">
                <span class="status-label">{{ $t("printerName") || "اسم الطابعة" }}:</span>
                <span class="status-value">{{ serverStatus.config.windows_printer_name || serverStatus.printer?.windows_default_printer || 'N/A' }}</span>
              </div>
              <div class="status-item">
                <span class="status-label">{{ $t("win32Available") || "Windows Print API" }}:</span>
                <span 
                  class="status-badge" 
                  :class="serverStatus.win32_available ? 'status-success' : 'status-error'"
                >
                  {{ serverStatus.win32_available ? ($t("yes") || "نعم") : ($t("no") || "لا") }}
                </span>
              </div>
              <div class="status-item">
                <span class="status-label">{{ $t("esposAvailable") || "ESC/POS Library" }}:</span>
                <span 
                  class="status-badge" 
                  :class="serverStatus.espos_available ? 'status-success' : 'status-error'"
                >
                  {{ serverStatus.espos_available ? ($t("yes") || "نعم") : ($t("no") || "لا") }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Available Printers Card -->
        <div class="printers-list-card" v-if="serverStatus">
          <div class="printers-list-header">
            <b-icon icon="printer-fill" class="me-2"></b-icon>
            <h3 class="printers-list-title">
              {{ $t("availablePrinters") || "الطابعات المتاحة" }}
            </h3>
            <button 
              class="btn-refresh" 
              @click="loadPrinters"
              :disabled="loadingPrinters"
            >
              <b-icon icon="arrow-clockwise" :class="{ 'spinning': loadingPrinters }"></b-icon>
              {{ $t("refresh") || "تحديث" }}
            </button>
          </div>
          <div class="printers-list-body">
            <p class="printers-help-text">
              <b-icon icon="info-circle-fill" class="me-2"></b-icon>
              {{ $t("changeDefaultPrinter") || "هنا يمكن تعديل الطابعة الافتراضية" }}
            </p>
            <div v-if="loadingPrinters" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="printers.length > 0" class="printers-grid">
              <div 
                v-for="printer in printers" 
                :key="printer.name"
                class="printer-item"
                :class="{ 'is-default': isDefaultPrinter(printer.name) }"
              >
                <div class="printer-item-header">
                  <b-icon icon="printer" class="printer-icon"></b-icon>
                  <span class="printer-name">{{ printer.name }}</span>
                  <span v-if="isDefaultPrinter(printer.name)" class="default-badge">
                    {{ $t("default") || "افتراضي" }}
                  </span>
                </div>
                <div class="printer-item-footer">
                  <span class="printer-type">{{ printer.type }}</span>
                  <button 
                    v-if="!isDefaultPrinter(printer.name)"
                    class="btn-set-default"
                    @click="setDefaultPrinter(printer.name)"
                    :disabled="settingDefault"
                  >
                    <b-icon icon="check-circle" class="me-1"></b-icon>
                    {{ $t("setAsDefault") || "تعيين كافتراضي" }}
                  </button>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="printer" class="empty-icon"></b-icon>
              <span>{{ $t("noPrintersFound") || "لم يتم العثور على طابعات" }}</span>
            </div>
          </div>
        </div>

        <!-- Test Print Card -->
        <div class="test-print-card" v-if="serverStatus">
          <div class="test-print-header">
            <b-icon icon="file-earmark-text" class="me-2"></b-icon>
            <h3 class="test-print-title">
              {{ $t("testPrint") || "اختبار الطباعة" }}
            </h3>
          </div>
          <div class="test-print-body">
            <div class="test-print-form">
              <div class="form-group">
                <label>{{ $t("testContent") || "محتوى الاختبار" }}:</label>
                <textarea 
                  v-model="testContent" 
                  class="form-control"
                  rows="5"
                  :placeholder="$t('testContentPlaceholder') || 'أدخل نص للاختبار...'"
                ></textarea>
              </div>
              <div class="form-actions">
                <button 
                  class="btn btn-primary" 
                  @click="testPrint"
                  :disabled="testing || !serverStatus || !serverStatus.printer?.available"
                >
                  <b-icon icon="printer-fill" class="me-2"></b-icon>
                  {{ testing ? ($t("printing") || "جاري الطباعة...") : ($t("printTest") || "طباعة اختبار") }}
                </button>
                <button 
                  class="btn btn-secondary" 
                  @click="testPrintReceipt"
                  :disabled="testing || !serverStatus || !serverStatus.printer?.available"
                >
                  <b-icon icon="receipt" class="me-2"></b-icon>
                  {{ testing ? ($t("printing") || "جاري الطباعة...") : ($t("printTestReceipt") || "طباعة فاتورة اختبار") }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Configuration Card -->
        <div class="config-card" v-if="serverStatus">
          <div class="config-header">
            <b-icon icon="gear-fill" class="me-2"></b-icon>
            <h3 class="config-title">
              {{ $t("configuration") || "الإعدادات" }}
            </h3>
            <button 
              class="btn-refresh" 
              @click="loadConfig"
              :disabled="loadingConfig"
            >
              <b-icon icon="arrow-clockwise" :class="{ 'spinning': loadingConfig }"></b-icon>
              {{ $t("refresh") || "تحديث" }}
            </button>
          </div>
          <div class="config-body">
            <div v-if="loadingConfig" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="config" class="config-form">
              <div class="form-group">
                <label>{{ $t("printerType") || "نوع الطابعة" }}:</label>
                <select v-model="config.type" class="form-control">
                  <option value="windows">Windows</option>
                  <option value="usb">USB</option>
                  <option value="serial">Serial</option>
                  <option value="network">Network</option>
                  <option value="file">File</option>
                </select>
              </div>

              <div class="form-group" v-if="config.type === 'windows'">
                <label>{{ $t("printerName") || "اسم الطابعة" }}:</label>
                <select v-model="config.windows_printer_name" class="form-control">
                  <option :value="null">{{ $t("useDefaultPrinter") || "استخدام الطابعة الافتراضية" }}</option>
                  <option v-for="printer in printers" :key="printer.name" :value="printer.name">
                    {{ printer.name }}
                  </option>
                </select>
              </div>

              <div class="form-group">
                <label>
                  <input 
                    type="checkbox" 
                    v-model="config.use_esc_pos_commands"
                    class="checkbox-input"
                  />
                  {{ $t("useEscPosCommands") || "استخدام أوامر ESC/POS" }}
                </label>
              </div>

              <div class="form-group">
                <label>{{ $t("encoding") || "الترميز" }}:</label>
                <select v-model="config.encoding" class="form-control">
                  <option value="utf-8">UTF-8</option>
                  <option value="windows-1256">Windows-1256 (Arabic)</option>
                  <option value="cp1256">CP1256 (Arabic)</option>
                  <option value="latin1">Latin1</option>
                </select>
              </div>

              <div class="form-group">
                <label>{{ $t("escPosEncoding") || "ترميز ESC/POS" }}:</label>
                <select v-model.number="config.esc_pos_encoding" class="form-control">
                  <option :value="16">16 - UTF-8</option>
                  <option :value="17">17 - Windows-1256 (Arabic)</option>
                  <option :value="0">0 - PC437</option>
                </select>
                <small class="form-help">{{ $t("escPosEncodingHelp") || "16 للعربية UTF-8، 17 للعربية Windows-1256" }}</small>
              </div>

              <div class="form-group" v-if="config.type === 'serial'">
                <label>{{ $t("serialPort") || "منفذ Serial" }}:</label>
                <input type="text" v-model="config.serial_port" class="form-control" placeholder="COM3" />
              </div>

              <div class="form-group" v-if="config.type === 'network'">
                <label>{{ $t("networkHost") || "عنوان الشبكة" }}:</label>
                <input type="text" v-model="config.network_host" class="form-control" placeholder="192.168.1.100" />
              </div>

              <div class="form-group" v-if="config.type === 'network'">
                <label>{{ $t("networkPort") || "منفذ الشبكة" }}:</label>
                <input type="number" v-model.number="config.network_port" class="form-control" placeholder="9100" />
              </div>

              <div class="form-actions">
                <button 
                  class="btn btn-primary" 
                  @click="saveConfig"
                  :disabled="savingConfig"
                >
                  <b-icon icon="save-fill" class="me-2"></b-icon>
                  {{ savingConfig ? ($t("saving") || "جاري الحفظ...") : ($t("saveConfig") || "حفظ الإعدادات") }}
                </button>
                <button 
                  class="btn btn-secondary" 
                  @click="resetConfig"
                  :disabled="savingConfig"
                >
                  <b-icon icon="arrow-counterclockwise" class="me-2"></b-icon>
                  {{ $t("reset") || "إعادة تعيين" }}
                </button>
              </div>
            </div>
            <div v-else class="error-state">
              <b-icon icon="exclamation-triangle-fill" class="error-icon"></b-icon>
              <span>{{ $t("failedToLoadConfig") || "فشل تحميل الإعدادات" }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { resolvePrintServerUrl } from "@/utils/apiBase.js";

export default {
  name: "PrintServerManagementView",
  components: {
    AppHeader,
  },
  data() {
    return {
      loading: false,
      loadingPrinters: false,
      loadingConfig: false,
      testing: false,
      settingDefault: false,
      savingConfig: false,
      downloading: false,
      showInstallGuide: false,
      serverStatus: null,
      printers: [],
      defaultPrinter: null,
      currentDefaultPrinter: null,
      config: null,
      originalConfig: null,
      testContent: `اختبار الطباعة
Test Print
الخادم يعمل بشكل صحيح
Server is working correctly
تاريخ: ${new Date().toLocaleDateString('ar-EG')}
الوقت: ${new Date().toLocaleTimeString('ar-EG')}`
    };
  },
  mounted() {
    this.checkServerHealth();
    this.loadPrinters();
    this.loadConfig();
  },
  computed: {
    defaultPrinterName() {
      // Use configured printer if available, otherwise use Windows default
      return this.currentDefaultPrinter || this.defaultPrinter;
    }
  },
  methods: {
    isDefaultPrinter(printerName) {
      // Check if this printer is the default (prioritize configured printer)
      return printerName === this.currentDefaultPrinter || 
             (printerName === this.defaultPrinter && !this.currentDefaultPrinter);
    },
    async checkServerHealth() {
      this.loading = true;
      try {
        const response = await fetch(`${resolvePrintServerUrl()}/health`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });
        
        if (response.ok) {
          this.serverStatus = await response.json();
          // Get current default printer from config
          if (this.serverStatus.config) {
            this.currentDefaultPrinter = this.serverStatus.config.windows_printer_name || null;
          }
          this.$notify.success(this.$i18n.t("serverStatusUpdated") || 'تم تحديث حالة الخادم', {
            position: "top-right",
            timeout: 2000,
          });
        } else {
          this.serverStatus = null;
          this.$notify.error(this.$i18n.t("serverNotAvailable") || 'الخادم غير متاح', {
            position: "top-right",
            timeout: 3000,
          });
        }
      } catch (error) {
        console.error('Error checking server health:', error);
        this.serverStatus = null;
        this.$notify.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بالخادم', {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.loading = false;
      }
    },
    async loadPrinters() {
      this.loadingPrinters = true;
      try {
        const response = await fetch(`${resolvePrintServerUrl()}/printers`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });
        
        if (response.ok) {
          const data = await response.json();
          this.printers = data.printers || [];
          this.defaultPrinter = data.default || null;
        } else {
          this.printers = [];
          this.$notify.error(this.$i18n.t("failedToLoadPrinters") || 'فشل تحميل الطابعات', {
            position: "top-right",
            timeout: 3000,
          });
        }
      } catch (error) {
        console.error('Error loading printers:', error);
        this.printers = [];
        this.$notify.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بالخادم', {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.loadingPrinters = false;
      }
    },
    async setDefaultPrinter(printerName) {
      this.settingDefault = true;
      try {
        const response = await fetch(`${resolvePrintServerUrl()}/config/printer`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            printer_name: printerName
          }),
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
          this.currentDefaultPrinter = printerName;
          // Update config if loaded
          if (this.config) {
            this.config.windows_printer_name = printerName;
          }
          this.$notify.success(this.$i18n.t("defaultPrinterSet") || `تم تعيين "${printerName}" كطابعة افتراضية`, {
            position: "top-right",
            timeout: 3000,
          });
          // Refresh server status and config to update
          await this.checkServerHealth();
          await this.loadConfig();
        } else {
          this.$notify.error(result.message || this.$i18n.t("failedToSetDefaultPrinter") || 'فشل تعيين الطابعة الافتراضية', {
            position: "top-right",
            timeout: 3000,
          });
        }
      } catch (error) {
        console.error('Error setting default printer:', error);
        this.$notify.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بالخادم', {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.settingDefault = false;
      }
    },
    async testPrint() {
      if (!this.testContent.trim()) {
        this.$notify.warning(this.$i18n.t("pleaseEnterTestContent") || 'يرجى إدخال محتوى للاختبار', {
          position: "top-right",
          timeout: 2000,
        });
        return;
      }

      this.testing = true;
      try {
        const response = await fetch(`${resolvePrintServerUrl()}/print`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            htmlContent: `<div style="text-align: center; padding: 20px;">
              <h2>اختبار الطباعة</h2>
              <p>${this.testContent.replace(/./g, '<br>')}</p>
              <p style="margin-top: 20px;">تاريخ: ${new Date().toLocaleDateString('ar-EG')}</p>
              <p>الوقت: ${new Date().toLocaleTimeString('ar-EG')}</p>
            </div>`
          }),
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
          this.$notify.success(this.$i18n.t("printTestSuccess") || 'تم إرسال أمر الطباعة بنجاح', {
            position: "top-right",
            timeout: 3000,
          });
        } else {
          this.$notify.error(result.message || this.$i18n.t("printTestFailed") || 'فشلت الطباعة', {
            position: "top-right",
            timeout: 3000,
          });
        }
      } catch (error) {
        console.error('Error testing print:', error);
        this.$notify.error(this.$i18n.t("printTestError") || 'حدث خطأ أثناء الطباعة', {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.testing = false;
      }
    },
    async testPrintReceipt() {
      this.testing = true;
      try {
        const testReceipt = {
          storeName: 'متجر الاختبار',
          storeAddress: 'عنوان المتجر',
          storePhone: '1234567890',
          orderCode: 'TEST-' + Date.now(),
          date: new Date().toLocaleDateString('ar-EG'),
          time: new Date().toLocaleTimeString('ar-EG'),
          tableNumber: '1',
          employeeName: 'موظف الاختبار',
          items: [
            { name: 'منتج اختبار 1', quantity: 2, price: '10.00', total: '20.00' },
            { name: 'منتج اختبار 2', quantity: 1, price: '15.00', total: '15.00' }
          ],
          subtotal: '35.00',
          discount: '0',
          tax: '0',
          total: '35.00',
          paymentMethod: 'نقدي'
        };

        const response = await fetch(`${resolvePrintServerUrl()}/print`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            htmlContent: `<div style="text-align: center; padding: 20px; direction: rtl;">
              <h2>فاتورة اختبار</h2>
              <hr>
              <p><strong>رقم الفاتورة:</strong> ${testReceipt.orderCode}</p>
              <p><strong>التاريخ:</strong> ${testReceipt.date}</p>
              <p><strong>الوقت:</strong> ${testReceipt.time}</p>
              <p><strong>الطاولة:</strong> ${testReceipt.tableNumber}</p>
              <p><strong>الموظف:</strong> ${testReceipt.employeeName}</p>
              <hr>
              <table style="width: 100%; border-collapse: collapse;">
                <thead>
                  <tr>
                    <th style="text-align: right;">المنتج</th>
                    <th style="text-align: center;">الكمية</th>
                    <th style="text-align: left;">السعر</th>
                    <th style="text-align: left;">الإجمالي</th>
                  </tr>
                </thead>
                <tbody>
                  ${testReceipt.items.map(item => `
                    <tr>
                      <td>${item.name}</td>
                      <td style="text-align: center;">${item.quantity}</td>
                      <td>${item.price}</td>
                      <td>${item.total}</td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
              <hr>
              <p><strong>الإجمالي:</strong> ${testReceipt.total}</p>
              <p><strong>طريقة الدفع:</strong> ${testReceipt.paymentMethod}</p>
              <hr>
              <p>شكراً لزيارتك</p>
            </div>`
          }),
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
          this.$notify.success(this.$i18n.t("printTestSuccess") || 'تم إرسال أمر الطباعة بنجاح', {
            position: "top-right",
            timeout: 3000,
          });
        } else {
          this.$notify.error(result.message || this.$i18n.t("printTestFailed") || 'فشلت الطباعة', {
            position: "top-right",
            timeout: 3000,
          });
        }
      } catch (error) {
        console.error('Error testing print receipt:', error);
        this.$notify.error(this.$i18n.t("printTestError") || 'حدث خطأ أثناء الطباعة', {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.testing = false;
      }
    },
    async loadConfig() {
      this.loadingConfig = true;
      try {
        const response = await fetch(`${resolvePrintServerUrl()}/config`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });
        
        if (response.ok) {
          const data = await response.json();
          this.config = { ...data.config };
          this.originalConfig = JSON.parse(JSON.stringify(data.config)); // Deep copy
        } else {
          this.config = null;
          this.$notify.error(this.$i18n.t("failedToLoadConfig") || 'فشل تحميل الإعدادات', {
            position: "top-right",
            timeout: 3000,
          });
        }
      } catch (error) {
        console.error('Error loading config:', error);
        this.config = null;
        this.$notify.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بالخادم', {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.loadingConfig = false;
      }
    },
    async saveConfig() {
      if (!this.config) return;
      
      this.savingConfig = true;
      try {
        const response = await fetch(`${resolvePrintServerUrl()}/config`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(this.config),
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
          this.originalConfig = JSON.parse(JSON.stringify(this.config)); // Update original
          this.$notify.success(this.$i18n.t("configSaved") || 'تم حفظ الإعدادات بنجاح', {
            position: "top-right",
            timeout: 3000,
          });
          // Refresh server status
          await this.checkServerHealth();
          // Update current default printer if changed
          if (this.config.windows_printer_name) {
            this.currentDefaultPrinter = this.config.windows_printer_name;
          }
        } else {
          this.$notify.error(result.message || this.$i18n.t("failedToSaveConfig") || 'فشل حفظ الإعدادات', {
            position: "top-right",
            timeout: 3000,
          });
        }
      } catch (error) {
        console.error('Error saving config:', error);
        this.$notify.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بالخادم', {
          position: "top-right",
          timeout: 3000,
        });
      } finally {
        this.savingConfig = false;
      }
    },
    resetConfig() {
      if (this.originalConfig) {
        this.config = JSON.parse(JSON.stringify(this.originalConfig)); // Reset to original
        this.$notify.info(this.$i18n.t("configReset") || 'تم إعادة تعيين الإعدادات', {
          position: "top-right",
          timeout: 2000,
        });
      }
    },
    copyCommand(command) {
      navigator.clipboard.writeText(command).then(() => {
        this.$notify.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
          position: "top-right",
          timeout: 2000,
        });
      }).catch(() => {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = command;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        this.$notify.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
          position: "top-right",
          timeout: 2000,
        });
      });
    },
    async downloadPrintServer() {
      this.downloading = true;
      try {
        // Download from backend
        const response = await fetch(`${resolvePrintServerUrl()}/download`, {
          method: 'GET',
        });
        
        if (response.ok) {
          const blob = await response.blob();
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = 'PrintServer.zip';
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
          window.URL.revokeObjectURL(url);
          
          this.$notify.success(this.$i18n.t("downloadStarted") || 'تم بدء التحميل', {
            position: "top-right",
            timeout: 2000,
          });
          
          // Show install guide after download
          setTimeout(() => {
            this.showInstallGuide = true;
          }, 1000);
        } else {
          // If server is not available, show instructions to download manually
          this.$notify.warning(this.$i18n.t("serverNotAvailableForDownload") || 'الخادم غير متاح. يرجى تحميل الملفات يدوياً من مجلد cashier_back', {
            position: "top-right",
            timeout: 4000,
          });
          this.showInstallGuide = true;
        }
      } catch (error) {
        console.error('Error downloading package:', error);
        // Show manual download instructions
        this.$notify.info(this.$i18n.t("manualDownloadInstructions") || 'يمكنك تحميل الملفات يدوياً من مجلد cashier_back', {
          position: "top-right",
          timeout: 4000,
        });
        this.showInstallGuide = true;
      } finally {
        this.downloading = false;
      }
    },
    showInstallInstructions() {
      this.showInstallGuide = !this.showInstallGuide;
    }
  }
};
</script>

<style scoped>
.print-server-page-container {
  padding: 2rem;
  min-height: 100vh;
  background: var(--bg-secondary);
}

.print-server-page-content {
  max-width: 1200px;
  margin: 0 auto;
}

.print-server-header-section {
  margin-bottom: 2rem;
}

.print-server-header-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.print-server-page-title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
}

.print-server-status-card,
.printers-list-card,
.test-print-card,
.config-card {
  background: var(--bg-primary);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-md);
  margin-bottom: 1.5rem;
  overflow: hidden;
  border: 1px solid var(--border-color);
}

.print-server-status-header,
.printers-list-header,
.test-print-header,
.config-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.1) 0%, rgba(99, 102, 241, 0.05) 100%);
}

.print-server-status-title,
.printers-list-title,
.test-print-title,
.config-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--text-primary);
  flex: 1;
}

.btn-refresh {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  padding: 0.5rem 1rem;
  border-radius: var(--radius-md);
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: all var(--transition-base);
  color: var(--text-primary);
}

.btn-refresh:hover:not(:disabled) {
  background: var(--primary-color);
  border-color: var(--primary-color);
  color: white;
  transform: translateY(-1px);
  box-shadow: var(--shadow-sm);
}

.btn-refresh:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.print-server-status-body,
.printers-list-body,
.test-print-body,
.config-body {
  padding: 1.5rem;
}

.loading-state,
.empty-state {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem;
  justify-content: center;
  color: var(--text-secondary);
}

.error-state {
  padding: 2rem;
  color: var(--danger-color);
  display: flex;
  align-items: center;
  gap: 0.5rem;
  justify-content: center;
}

.error-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  text-align: center;
}

.error-icon {
  font-size: 3rem;
  color: #dc2626;
}

.error-message {
  max-width: 600px;
}

.error-message h4 {
  font-size: 1.25rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: #dc2626;
}

.error-message p {
  font-size: 1rem;
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.server-instructions {
  background: #fef3c7;
  border: 2px solid #f59e0b;
  border-radius: 0.75rem;
  padding: 1.5rem;
  margin-top: 1rem;
  text-align: right;
}

.server-instructions h5 {
  font-size: 1rem;
  font-weight: 600;
  color: #92400e;
  margin-bottom: 1rem;
}

.instructions-list {
  text-align: right;
  color: #78350f;
  margin: 1rem 0;
  padding-right: 1.5rem;
}

.instructions-list li {
  margin-bottom: 0.5rem;
  line-height: 1.6;
}

.command-box {
  background: #1f2937;
  color: #f9fafb;
  padding: 1rem;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin: 1rem 0;
  direction: ltr;
  text-align: left;
}

.command-text {
  font-family: 'Courier New', monospace;
  font-size: 0.875rem;
  flex: 1;
  word-break: break-all;
}

.btn-copy {
  background: #3b82f6;
  color: white;
  border: none;
  padding: 0.5rem;
  border-radius: 0.375rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  flex-shrink: 0;
}

.btn-copy:hover {
  background: #2563eb;
}

.alternative-method {
  margin-top: 1rem;
  color: #78350f;
  font-size: 0.875rem;
}

.download-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;
  flex-wrap: wrap;
}

.btn-download {
  background: #10b981;
  color: white;
}

.btn-download:hover:not(:disabled) {
  background: #059669;
}

.btn-install {
  background: #6366f1;
  color: white;
}

.btn-install:hover:not(:disabled) {
  background: #4f46e5;
}

.install-guide {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #f0fdf4;
  border: 2px solid #10b981;
  border-radius: 0.5rem;
}

.install-guide h6 {
  font-size: 1rem;
  font-weight: 600;
  color: #065f46;
  margin-bottom: 0.75rem;
}

.server-not-available-card {
  background: var(--bg-primary);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
  margin-bottom: 1.5rem;
  overflow: hidden;
  border: 2px solid var(--warning-color);
}

.server-not-available-header {
  background: linear-gradient(135deg, rgba(192, 132, 252, 0.2) 0%, rgba(168, 85, 247, 0.15) 100%);
  padding: 2rem;
  text-align: center;
  border-bottom: 2px solid var(--warning-color);
}

.error-icon-large {
  font-size: 4rem;
  color: var(--warning-color);
  margin-bottom: 1rem;
}

.server-not-available-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
}

.server-not-available-body {
  padding: 2rem;
}

.server-not-available-message {
  font-size: 1.125rem;
  color: var(--text-secondary);
  text-align: center;
  margin-bottom: 2rem;
  line-height: 1.6;
}

.download-section {
  text-align: center;
  margin-bottom: 2rem;
}

.btn-download-large {
  background: linear-gradient(135deg, var(--success-color) 0%, var(--accent-dark) 100%);
  color: white;
  border: none;
  padding: 1rem 2rem;
  border-radius: var(--radius-lg);
  font-size: 1.125rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  transition: all var(--transition-slow);
  box-shadow: var(--shadow-md);
}

.btn-download-large:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
  background: linear-gradient(135deg, var(--accent-light) 0%, var(--success-color) 100%);
}

.btn-download-large:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.install-instructions-section {
  background: var(--bg-tertiary);
  border: 2px solid var(--border-color);
  border-radius: var(--radius-lg);
  padding: 1.5rem;
  margin-top: 2rem;
}

.instructions-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 1.5rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.instructions-list-detailed {
  text-align: right;
  color: var(--text-secondary);
  margin: 1rem 0;
  padding-right: 1.5rem;
  line-height: 2;
}

.instructions-list-detailed li {
  margin-bottom: 1rem;
}

.instructions-list-detailed li strong {
  color: var(--text-primary);
  font-weight: 600;
}

.alternative-instructions {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 2px solid var(--border-color);
}

.alternative-instructions h5 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 1rem;
}

.command-box-large {
  background: var(--bg-dark);
  color: var(--text-primary);
  padding: 1.25rem;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin: 1rem 0;
  direction: ltr;
  text-align: left;
  border: 1px solid var(--border-color);
}

.command-text-large {
  font-family: 'Courier New', monospace;
  font-size: 1rem;
  flex: 1;
  word-break: break-all;
  color: var(--text-primary);
}

.btn-copy-large {
  background: var(--primary-color);
  color: white;
  border: none;
  padding: 0.75rem 1.25rem;
  border-radius: var(--radius-md);
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: all var(--transition-base);
  flex-shrink: 0;
  font-weight: 500;
  box-shadow: var(--shadow-xs);
}

.btn-copy-large:hover {
  background: var(--primary-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-sm);
}

.command-help {
  margin-top: 0.75rem;
  color: var(--text-secondary);
  font-size: 0.875rem;
  text-align: right;
}

.empty-icon {
  font-size: 2rem;
}

.status-info {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.status-item {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.status-label {
  font-weight: 500;
  color: var(--text-secondary);
  min-width: 150px;
}

.status-value {
  color: var(--text-primary);
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.status-success {
  background: var(--success-light);
  color: var(--success-color);
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.status-error {
  background: var(--danger-light);
  color: var(--danger-color);
  border: 1px solid rgba(239, 68, 68, 0.3);
}

.printers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
}

.printer-item {
  border: 2px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: 1rem;
  transition: all var(--transition-base);
  background: var(--bg-tertiary);
}

.printer-item:hover {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.printer-item.is-default {
  border-color: var(--success-color);
  background: var(--success-light);
}

.printer-item-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.printer-icon {
  color: var(--primary-color);
}

.printer-name {
  font-weight: 500;
  color: var(--text-primary);
  flex: 1;
}

.default-badge {
  background: var(--success-color);
  color: white;
  padding: 0.125rem 0.5rem;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 600;
  box-shadow: var(--shadow-xs);
}

.printer-item-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.printer-type {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.printers-help-text {
  padding: 0.75rem 1rem;
  background: rgba(59, 130, 246, 0.1);
  border-left: 4px solid var(--info-color);
  border-radius: var(--radius-md);
  color: var(--info-color);
  margin-bottom: 1rem;
  display: flex;
  align-items: center;
  font-size: 0.875rem;
}

.btn-set-default {
  background: var(--primary-color);
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.25rem;
  transition: all var(--transition-base);
  box-shadow: var(--shadow-xs);
}

.btn-set-default:hover:not(:disabled) {
  background: var(--primary-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-sm);
}

.btn-set-default:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.test-print-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 500;
  color: var(--text-primary);
}

.form-control {
  padding: 0.75rem;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  font-size: 1rem;
  resize: vertical;
  background: var(--bg-tertiary);
  color: var(--text-primary);
}

.form-control:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.2);
  background: var(--bg-primary);
}

.form-actions {
  display: flex;
  gap: 1rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.5rem;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.btn-primary {
  background: var(--primary-color);
  color: white;
  box-shadow: var(--shadow-sm);
}

.btn-primary:hover:not(:disabled) {
  background: var(--primary-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.btn-secondary {
  background: var(--secondary-color);
  color: white;
  box-shadow: var(--shadow-sm);
}

.btn-secondary:hover:not(:disabled) {
  background: var(--secondary-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.config-info {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.config-note {
  padding: 1rem;
  background: var(--warning-light);
  border-left: 4px solid var(--warning-color);
  border-radius: var(--radius-md);
  color: var(--warning-color);
  display: flex;
  align-items: start;
  gap: 0.5rem;
}

.config-details {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.config-detail-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-color);
}

.config-detail-label {
  font-weight: 500;
  color: var(--text-secondary);
  min-width: 150px;
}

.config-detail-value {
  color: var(--text-primary);
  font-family: monospace;
}

.config-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.checkbox-input {
  margin-left: 0.5rem;
  margin-right: 0.5rem;
  width: 1.25rem;
  height: 1.25rem;
  cursor: pointer;
}

.form-help {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
  color: var(--text-secondary);
}

select.form-control {
  padding: 0.75rem;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  font-size: 1rem;
  background: var(--bg-tertiary);
  cursor: pointer;
  color: var(--text-primary);
}

select.form-control:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.2);
  background: var(--bg-primary);
}

[dir="rtl"] .print-server-page-title,
[dir="rtl"] .print-server-status-title,
[dir="rtl"] .printers-list-title,
[dir="rtl"] .test-print-title,
[dir="rtl"] .config-title {
  direction: rtl;
}

[dir="rtl"] .status-item,
[dir="rtl"] .config-detail-item {
  flex-direction: row-reverse;
}

[dir="rtl"] .form-actions {
  flex-direction: row-reverse;
}
</style>

