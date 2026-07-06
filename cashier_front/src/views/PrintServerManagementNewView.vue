<template>
  <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content print-server-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="printer-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("printServerManagement") || "إدارة خادم الطباعة" }}</h1>
                  <p class="header-subtitle">{{ $t("printServerManagementDescription") || "إدارة حالة الخادم والطابعات وإعداد طباعة الفواتير" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button
                  type="button"
                  class="btn-refresh"
                  @click="refreshPage"
                  :disabled="loading"
                >
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
                <button
                  v-if="serverStatus"
                  type="button"
                  class="users-add-button"
                  @click="openAddPrinterModal"
                >
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addPrinter") || "إضافة طابعة" }}</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Server Status Alert -->
          <div v-if="!serverStatus && !loading" class="app-section-card server-offline-card">
            <div class="server-offline-content">
              <div class="server-offline-icon-wrap">
                <b-icon icon="printer-fill" class="server-offline-icon"></b-icon>
                <span class="server-offline-status-dot"></span>
              </div>
              <h2 class="server-offline-title">{{ $t("printServerOfflineTitle") || "خادم الطباعة غير متصل" }}</h2>
              <p class="server-offline-message">
                {{ $t("serverNotAvailableMessage") || "الخدمة غير متاحة. يرجى تحميل وتشغيل نظام الطباعة (Print Server) أولاً" }}
              </p>
              <div class="server-offline-meta">
                <span class="server-offline-chip">
                  <b-icon icon="hdd-network"></b-icon>
                  localhost:5000
                </span>
                <span class="server-offline-chip server-offline-chip--warning">
                  <b-icon icon="exclamation-circle"></b-icon>
                  {{ $t("offline") || "غير متصل" }}
                </span>
              </div>
              <div class="server-offline-actions">
                <button
                  type="button"
                  class="server-offline-btn server-offline-btn--primary"
                  @click="downloadPrintServer"
                  :disabled="downloading"
                >
                  <b-spinner small v-if="downloading" class="me-1"></b-spinner>
                  <b-icon v-else icon="download" class="button-icon"></b-icon>
                  <span class="button-text">
                    {{ downloading ? ($t("downloading") || "جاري التحميل...") : ($t("downloadPrintServer") || "تحميل Print Server") }}
                  </span>
                </button>
                <button
                  type="button"
                  class="server-offline-btn server-offline-btn--secondary"
                  @click="toggleInstallGuide"
                >
                  <b-icon :icon="showInstallGuide ? 'chevron-up' : 'info-circle'" class="button-icon"></b-icon>
                  <span class="button-text">
                    {{ showInstallGuide ? ($t("hideInstallGuide") || "إخفاء التعليمات") : ($t("showInstallGuide") || "تعليمات التشغيل") }}
                  </span>
                </button>
                <button type="button" class="server-offline-btn server-offline-btn--ghost" @click="refreshPage">
                  <b-icon icon="arrow-clockwise" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>

              <div v-if="showInstallGuide" class="install-instructions-section">
                <h4 class="instructions-title">
                  <b-icon icon="list-ol" class="me-2"></b-icon>
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
                    <code class="command-text-large">dotnet run --project cashier_back/PrintServer</code>
                    <button
                      type="button"
                      class="btn-copy-large"
                      @click="copyCommand('dotnet run --project cashier_back/PrintServer')"
                      :title="$t('copyCommand') || 'نسخ الأمر'"
                    >
                      <b-icon icon="clipboard"></b-icon>
                      {{ $t("copyCommand") || "نسخ" }}
                    </button>
                  </div>
                  <p class="command-help">
                    {{ $t("commandHelp") || "افتح Terminal أو Command Prompt في مجلد المشروع وشغّل الأمر أعلاه" }}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div v-if="serverStatus || loading" class="app-overview-grid">
            <div class="app-overview-stat">
              <span
                class="app-overview-stat-icon"
                :class="serverOnline ? 'app-overview-stat-icon--success' : 'app-overview-stat-icon--warning'"
              >
                <b-icon :icon="serverOnline ? 'check-circle-fill' : 'exclamation-triangle-fill'"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner v-if="loading" small></b-spinner>
                  <template v-else>{{ serverOnline ? ($t('online') || 'متصل') : ($t('offline') || 'غير متصل') }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("serverStatus") || "حالة الخادم" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="printer-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner v-if="loadingPrinters" small></b-spinner>
                  <template v-else>{{ managedPrinters.length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("printersManagement") || "إدارة الطابعات" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="hdd-network-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner v-if="loadingSystemPrinters" small></b-spinner>
                  <template v-else>{{ printers.length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("availablePrinters") || "الطابعات المتاحة" }}</div>
              </div>
            </div>
          </div>

          <div v-if="serverStatus" class="app-section-card">
            <div class="app-section-header">
              <div>
                <h2 class="app-section-title">{{ $t("printersManagement") || "إدارة الطابعات" }}</h2>
                <p class="retail-print-hint">
                  <b-icon icon="receipt-cutoff" class="me-2"></b-icon>
                  {{ $t("retailPrintModeHint") || "طباعة فواتير كاملة فقط — بدون تقسيم حسب أقسام المنتجات" }}
                </p>
              </div>
            </div>
            <div class="app-section-body">
              <div v-if="loadingPrinters" class="loading-state-full">
                <b-spinner></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="managedPrinters.length > 0" class="app-cards-grid">
                <div v-for="printer in managedPrinters" :key="printer.id" class="app-item-card">
                  <div class="app-item-card-header">
                    <div class="app-item-card-title">
                      <b-icon icon="printer-fill" class="app-item-card-icon"></b-icon>
                      <h4>{{ printer.name }}</h4>
                    </div>
                    <div class="printer-status-badges">
                      <span v-if="printer.isMain" class="printer-chip printer-chip--main">{{ $t("mainPrinter") || "رئيسية" }}</span>
                      <span v-if="!printer.isActive" class="printer-chip printer-chip--inactive">{{ $t("inactive") || "غير مفعل" }}</span>
                      <span
                        v-else-if="getPrinterStatus(printer.id).online"
                        class="printer-chip printer-chip--online"
                      >{{ $t("online") || "متصل" }}</span>
                      <span v-else class="printer-chip printer-chip--offline">{{ $t("offline") || "غير متصل" }}</span>
                    </div>
                    <div class="app-item-card-actions">
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        @click="editPrinter(printer)"
                        :title="$t('edit') || 'تعديل'"
                      >
                        <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--delete"
                        @click="confirmDeletePrinter(printer)"
                        :title="$t('delete') || 'حذف'"
                      >
                        <b-icon icon="trash-fill" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </div>
                  <div class="app-item-card-body">
                    <div class="app-info-row">
                      <b-icon icon="printer" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("printerName") || "اسم الطابعة" }}</span>
                      <span class="info-value">{{ printer.printerName || '---' }}</span>
                    </div>
                    <div class="app-info-row">
                      <b-icon icon="tag" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("type") || "النوع" }}</span>
                      <span class="info-value">{{ printer.printerType || '---' }}</span>
                    </div>
                    <div class="app-info-row">
                      <b-icon icon="receipt-cutoff" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("printMode") || "نوع الطباعة" }}</span>
                      <span class="info-value">{{ $t("fullReceiptPrint") || "فاتورة كاملة" }}</span>
                    </div>
                    <div v-if="printer.description" class="app-info-row">
                      <b-icon icon="file-text" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("description") || "الوصف" }}</span>
                      <span class="info-value">{{ printer.description }}</span>
                    </div>
                  </div>
                  <div class="app-item-card-footer">
                    <button
                      type="button"
                      class="users-form-submit-button printer-test-btn"
                      @click="testPrintToPrinter(printer.id)"
                      :disabled="!printer.isActive || testingPrint"
                    >
                      <b-icon icon="printer-fill" class="action-icon"></b-icon>
                      <span>{{ $t("testPrint") || "اختبار الطباعة" }}</span>
                    </button>
                  </div>
                </div>
              </div>
              <div v-else class="empty-state">
                <b-icon icon="printer" class="empty-icon"></b-icon>
                <p>{{ $t("noPrinters") || "لا توجد طابعات" }}</p>
                <button type="button" class="empty-state-btn" @click="openAddPrinterModal">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addFirstPrinter") || "إضافة أول طابعة" }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Printer Modal -->
    <b-modal
      v-model="showAddPrinterModal"
      id="modal-addPrinter"
      :title="$t('addPrinter')"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
      @show="onPrinterModalShow"
      @hidden="resetPrinterForm"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addPrinter") || "إضافة طابعة" }}</h2>
        <form @submit.prevent="savePrinter" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
              {{ $t("name") || "الاسم" }} <span class="required">*</span>
            </label>
            <input 
              v-model="printerForm.name" 
              type="text"
              class="users-form-input"
              :placeholder="$t('printerName') || 'اسم الطابعة'"
              required
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text-fill" class="form-label-icon"></b-icon>
              {{ $t("description") || "الوصف" }}
            </label>
            <textarea 
              v-model="printerForm.description" 
              class="users-form-input"
              rows="2"
              :placeholder="$t('description') || 'وصف الطابعة'"
            ></textarea>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="printer" class="form-label-icon"></b-icon>
              {{ $t("systemPrinterName") || "اسم الطابعة في النظام" }} <span class="required">*</span>
            </label>
            <select
              v-model="printerForm.printerName"
              class="users-form-select"
              :disabled="loadingSystemPrinters"
              required
              @change="onSystemPrinterChange"
            >
              <option value="">
                {{ loadingSystemPrinters ? ($t('loadingPrinters') || 'جاري تحميل الطابعات...') : ($t('selectPrinter') || 'اختر الطابعة') }}
              </option>
              <option v-for="printer in printers" :key="printer.name" :value="printer.name">
                {{ printer.name }} ({{ printer.type }})
              </option>
            </select>
            <small v-if="!loadingSystemPrinters && printers.length === 0" class="printer-select-hint">
              {{ $t("noPrintersFound") || "لم يتم العثور على طابعات متاحة" }}
            </small>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="receipt-cutoff" class="form-label-icon"></b-icon>
              {{ $t("printMode") || "نوع الطباعة" }}
            </label>
            <div class="retail-print-mode-badge">
              {{ $t("fullReceiptPrint") || "فاتورة كاملة" }}
            </div>
            <small class="text-muted d-block mt-1">
              {{ $t("fullReceiptPrintHint") || "تُطبع الفاتورة كاملة بكل المنتجات دون تقسيم حسب الأقسام" }}
            </small>
          </div>
          <div class="form-toggle-cards form-toggle-cards--stack">
            <label
              class="form-toggle-card"
              :class="{ 'form-toggle-card--on': printerForm.isActive }"
            >
              <input v-model="printerForm.isActive" type="checkbox" class="form-toggle-card-input" />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--success">
                  <b-icon icon="check-circle-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("active") || "مفعل" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("printerActiveHint") || "الطابعة متاحة للاستخدام" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card form-toggle-card--accent-warning"
              :class="{ 'form-toggle-card--on': printerForm.isMain }"
            >
              <input v-model="printerForm.isMain" type="checkbox" class="form-toggle-card-input" />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--warning">
                  <b-icon icon="star-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("mainPrinter") || "طابعة رئيسية" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("retailMainPrinterHint") || "الطابعة الافتراضية لطباعة الفواتير الكاملة" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingPrinter">
              <b-spinner v-if="savingPrinter" small class="me-2"></b-spinner>
              {{ savingPrinter ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showAddPrinterModal = false">
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Edit Printer Modal -->
    <b-modal
      id="modal-editPrinter"
      :title="$t('editPrinter')"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
      @show="onPrinterModalShow"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("editPrinter") || "تعديل طابعة" }}</h2>
        <form @submit.prevent="updatePrinter" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
              {{ $t("name") || "الاسم" }} <span class="required">*</span>
            </label>
            <input 
              v-model="printerForm.name" 
              type="text"
              class="users-form-input"
              :placeholder="$t('printerName') || 'اسم الطابعة'"
              required
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text-fill" class="form-label-icon"></b-icon>
              {{ $t("description") || "الوصف" }}
            </label>
            <textarea 
              v-model="printerForm.description" 
              class="users-form-input"
              rows="2"
              :placeholder="$t('description') || 'وصف الطابعة'"
            ></textarea>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="printer" class="form-label-icon"></b-icon>
              {{ $t("systemPrinterName") || "اسم الطابعة في النظام" }} <span class="required">*</span>
            </label>
            <select
              v-model="printerForm.printerName"
              class="users-form-select"
              :disabled="loadingSystemPrinters"
              required
              @change="onSystemPrinterChange"
            >
              <option value="">
                {{ loadingSystemPrinters ? ($t('loadingPrinters') || 'جاري تحميل الطابعات...') : ($t('selectPrinter') || 'اختر الطابعة') }}
              </option>
              <option v-for="printer in printers" :key="printer.name" :value="printer.name">
                {{ printer.name }} ({{ printer.type }})
              </option>
            </select>
            <small v-if="!loadingSystemPrinters && printers.length === 0" class="printer-select-hint">
              {{ $t("noPrintersFound") || "لم يتم العثور على طابعات متاحة" }}
            </small>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="receipt-cutoff" class="form-label-icon"></b-icon>
              {{ $t("printMode") || "نوع الطباعة" }}
            </label>
            <div class="retail-print-mode-badge">
              {{ $t("fullReceiptPrint") || "فاتورة كاملة" }}
            </div>
            <small class="text-muted d-block mt-1">
              {{ $t("fullReceiptPrintHint") || "تُطبع الفاتورة كاملة بكل المنتجات دون تقسيم حسب الأقسام" }}
            </small>
          </div>
          <div class="form-toggle-cards form-toggle-cards--stack">
            <label
              class="form-toggle-card"
              :class="{ 'form-toggle-card--on': printerForm.isActive }"
            >
              <input v-model="printerForm.isActive" type="checkbox" class="form-toggle-card-input" />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--success">
                  <b-icon icon="check-circle-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("active") || "مفعل" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("printerActiveHint") || "الطابعة متاحة للاستخدام" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card form-toggle-card--accent-warning"
              :class="{ 'form-toggle-card--on': printerForm.isMain }"
            >
              <input v-model="printerForm.isMain" type="checkbox" class="form-toggle-card-input" />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--warning">
                  <b-icon icon="star-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("mainPrinter") || "طابعة رئيسية" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("retailMainPrinterHint") || "الطابعة الافتراضية لطباعة الفواتير الكاملة" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingPrinter">
              <b-spinner v-if="savingPrinter" small class="me-2"></b-spinner>
              {{ savingPrinter ? ($t("saving") || "جاري الحفظ...") : ($t("update") || "تحديث") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="$bvModal.hide('modal-editPrinter')">
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Print Modal -->
    <b-modal 
      id="modal-print" 
      :title="$t('sendPrintCommand') || 'إرسال أمر طباعة'" 
      hide-header 
      hide-footer 
      class="users-modal"
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("sendPrintCommand") || "إرسال أمر طباعة" }}</h2>
        <form @submit.prevent="sendPrint" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="123" class="form-label-icon"></b-icon>
              {{ $t("copies") || "عدد النسخ" }} <span class="required">*</span>
            </label>
            <input 
              v-model.number="printForm.copies" 
              type="number" 
              min="1"
              max="10"
              class="users-form-input"
              required
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-code" class="form-label-icon"></b-icon>
              {{ $t("printContent") || "محتوى الطباعة" }}
            </label>
            <textarea 
              v-model="printForm.htmlContent" 
              class="users-form-input"
              rows="10"
              :placeholder="$t('enterHtmlContent') || 'أدخل محتوى HTML للطباعة...'"
            ></textarea>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="testingPrint">
              <b-spinner v-if="testingPrint" small class="me-2"></b-spinner>
              {{ testingPrint ? ($t("printing") || "جاري الطباعة...") : ($t("print") || "طباعة") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="$bvModal.hide('modal-print')">
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>
  </b-overlay>
</template>

<script>
import AppHeader from '../components/Layout/AppHeader.vue';
import { HTTP } from '../http/api.js';

const PRINT_SERVER_URL = 'http://localhost:5000';

export default {
  name: "PrintServerManagementNewView",
  components: {
    AppHeader,
  },
  data() {
    return {
      show: false,
      loading: false,
      loadingPrinters: false,
      loadingSystemPrinters: false,
      downloading: false,
      showInstallGuide: false,
      serverStatus: null,
      printers: [],
      managedPrinters: [],
      printerStatuses: {},
      statusCheckInterval: null,
      showAddPrinterModal: false,
      showEditPrinterModal: false,
      showPrintModal: false,
      selectedPrinter: null,
      savingPrinter: false,
      testingPrint: false,
      testContent: 'اختبار طباعة فاتورة كاملة.هذا نص تجريبي للتحقق من عمل الطابعة بشكل صحيح.',
      printerForm: {
        name: '',
        description: '',
        printerName: '',
        printerType: 'windows',
        printCategory: 'Receipt',
        isActive: true,
        isMain: false
      },
      printForm: {
        printerId: null,
        htmlContent: '',
        copies: 1
      }
    };
  },
  computed: {
    serverOnline() {
      return !!(this.serverStatus && this.serverStatus.status === 'ok');
    },
  },
  mounted() {
    this.refreshPage();
    this.startStatusPolling();
  },
  beforeDestroy() {
    this.stopStatusPolling();
  },
  methods: {
    async refreshPage() {
      await this.checkServerHealth();
      await this.loadManagedPrinters();
      if (this.serverStatus) {
        await this.loadPrinters();
      }
    },
    openAddPrinterModal() {
      this.resetPrinterForm();
      this.showAddPrinterModal = true;
    },
    onPrinterModalShow() {
      this.loadPrinters();
    },
    onSystemPrinterChange() {
      const selected = this.printers.find((p) => p.name === this.printerForm.printerName);
      if (selected && selected.type) {
        this.printerForm.printerType = selected.type;
      }
    },
    async checkServerHealth() {
      this.loading = true;
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/health`);
        if (response.ok) {
          this.serverStatus = await response.json();
        } else {
          this.serverStatus = null;
        }
      } catch (error) {
        console.error('Error checking server health:', error);
        this.serverStatus = null;
      } finally {
        this.loading = false;
      }
    },
    async loadPrinters() {
      if (!this.serverStatus) {
        this.printers = [];
        return;
      }
      this.loadingSystemPrinters = true;
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/printers`);
        if (response.ok) {
          const data = await response.json();
          this.printers = Array.isArray(data) ? data : (data.printers || []);
        } else {
          this.printers = [];
        }
      } catch (error) {
        console.error('Error loading printers:', error);
        this.printers = [];
      } finally {
        this.loadingSystemPrinters = false;
      }
    },
    async loadManagedPrinters() {
      this.loadingPrinters = true;
      try {
        const response = await HTTP.get('Printers');
        if (response.data && !response.data.errorStatus) {
          this.managedPrinters = response.data.data || [];
          this.checkAllPrinterStatuses();
        } else {
          this.managedPrinters = [];
        }
      } catch (error) {
        console.error('Error loading managed printers:', error);
        this.managedPrinters = [];
      } finally {
        this.loadingPrinters = false;
      }
    },
    buildPrinterPayload() {
      return {
        ...this.printerForm,
        printCategory: 'Receipt',
      };
    },
    async checkAllPrinterStatuses() {
      for (const printer of this.managedPrinters) {
        await this.checkPrinterStatus(printer.id, printer.printerName, printer.printerType);
      }
    },
    async checkPrinterStatus(printerId, printerName, printerType) {
      try {
        const response = await HTTP.get(`Printers/${printerId}/status`);
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.$set(this.printerStatuses, printerId, response.data.data);
        }
      } catch (error) {
        console.error(`Error checking printer ${printerId} status:`, error);
        this.$set(this.printerStatuses, printerId, { online: false, available: false });
      }
    },
    getPrinterStatus(printerId) {
      return this.printerStatuses[printerId] || { online: false, available: false };
    },
    startStatusPolling() {
      this.statusCheckInterval = setInterval(() => {
        if (this.serverStatus) {
          this.checkAllPrinterStatuses();
        }
      }, 30000);
    },
    stopStatusPolling() {
      if (this.statusCheckInterval) {
        clearInterval(this.statusCheckInterval);
        this.statusCheckInterval = null;
      }
    },
    async savePrinter() {
      this.savingPrinter = true;
      try {
        const response = await HTTP.post('Printers', this.buildPrinterPayload());
        if (response.data && !response.data.errorStatus) {
          this.showAddPrinterModal = false;
          this.resetPrinterForm();
          await this.loadManagedPrinters();
          this.$notify.success(this.$i18n.t("printerAdded") || 'تمت إضافة الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$notify.error(response.data?.message || this.$i18n.t("errorAddingPrinter") || 'حدث خطأ أثناء إضافة الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error saving printer:', error);
        this.$notify.error(error.response?.data?.message || this.$i18n.t("errorAddingPrinter") || 'حدث خطأ أثناء إضافة الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingPrinter = false;
      }
    },
    async updatePrinter() {
      this.savingPrinter = true;
      try {
        const response = await HTTP.put(`Printers/${this.selectedPrinter.id}`, this.buildPrinterPayload());
        if (response.data && !response.data.errorStatus) {
          this.$bvModal.hide('modal-editPrinter');
          this.resetPrinterForm();
          await this.loadManagedPrinters();
          this.$notify.success(this.$i18n.t("printerUpdated") || 'تم تحديث الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$notify.error(response.data?.message || this.$i18n.t("errorUpdatingPrinter") || 'حدث خطأ أثناء تحديث الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error updating printer:', error);
        this.$notify.error(error.response?.data?.message || this.$i18n.t("errorUpdatingPrinter") || 'حدث خطأ أثناء تحديث الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingPrinter = false;
      }
    },
    async deletePrinter(printerId) {
      try {
        const response = await HTTP.delete(`Printers/${printerId}`);
        if (response.data && !response.data.errorStatus) {
          await this.loadManagedPrinters();
          this.$notify.success(this.$i18n.t("printerDeleted") || 'تم حذف الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$notify.error(response.data?.message || this.$i18n.t("errorDeletingPrinter") || 'حدث خطأ أثناء حذف الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting printer:', error);
        this.$notify.error(error.response?.data?.message || this.$i18n.t("errorDeletingPrinter") || 'حدث خطأ أثناء حذف الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    async editPrinter(printer) {
      this.selectedPrinter = printer;
      this.printerForm = {
        name: printer.name,
        description: printer.description || '',
        printerName: printer.printerName,
        printerType: printer.printerType || 'windows',
        printCategory: 'Receipt',
        isActive: printer.isActive,
        isMain: printer.isMain
      };
      await this.loadPrinters();
      this.$bvModal.show('modal-editPrinter');
    },
    async confirmDeletePrinter(printer) {
      const ok = await this.$confirm({
        title: this.$t("confirmDelete"),
        message: this.$t("confirmDeletePrinter", { name: printer.name || "" }),
      });
      if (ok) {
        this.deletePrinter(printer.id);
      }
    },
    testPrintToPrinter(printerId) {
      this.printForm.printerId = printerId;
      const testContent = this.testContent || 'اختبار الطباعة.هذا نص تجريبي للتحقق من عمل الطابعة بشكل صحيح.';
      this.printForm.htmlContent = `<div style="padding: 16px; direction: rtl; font-family: sans-serif; max-width: 300px;">
        <h2 style="text-align: center; margin: 0 0 12px;">فاتورة تجريبية كاملة</h2>
        <p style="text-align: center; margin: 0 0 16px; font-size: 12px;">${testContent}</p>
        <hr />
        <p>أرز بسمتي × 2 — 30,000</p>
        <p>حليب طازج × 1 — 2,000</p>
        <p>ماء معدني × 3 — 3,000</p>
        <hr />
        <p><strong>المجموع: 35,000</strong></p>
        <p style="margin-top: 16px; font-size: 12px;">تاريخ: ${new Date().toLocaleDateString('ar-EG')} — ${new Date().toLocaleTimeString('ar-EG')}</p>
      </div>`;
      this.printForm.copies = 1;
      this.$bvModal.show('modal-print');
    },
    async sendPrint() {
      if (!this.printForm.printerId) {
        this.$notify.warning(this.$i18n.t("pleaseSelectPrinter") || 'يرجى اختيار طابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }

      this.testingPrint = true;
      try {
        const printer = this.managedPrinters.find(p => p.id === this.printForm.printerId);
        if (!printer) {
          throw new Error('Printer not found');
        }

        const response = await HTTP.post(`Printers/${this.printForm.printerId}/print`, {
          htmlContent: this.printForm.htmlContent,
          copies: this.printForm.copies || 1,
          printerName: printer.printerName
        });
        
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(this.$i18n.t("printSentSuccessfully") || `تم إرسال أمر الطباعة بنجاح (${this.printForm.copies} نسخة)`, {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.$bvModal.hide('modal-print');
          this.printForm = {
            printerId: null,
            htmlContent: '',
            copies: 1
          };
        } else {
          this.$notify.error(response.data?.message || this.$i18n.t("printFailed") || 'فشلت الطباعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error printing:', error);
        this.$notify.error(error.response?.data?.message || this.$i18n.t("printError") || 'حدث خطأ أثناء الطباعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.testingPrint = false;
      }
    },
    toggleInstallGuide() {
      this.showInstallGuide = !this.showInstallGuide;
    },
    async downloadPrintServer() {
      this.downloading = true;
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/download`, {
          method: 'GET'
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
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          
          setTimeout(() => {
            this.showInstallGuide = true;
          }, 1000);
        } else {
          this.$notify.warning(this.$i18n.t("serverNotAvailableForDownload") || 'الخادم غير متاح. يرجى تحميل الملفات يدوياً من مجلد cashier_back/PrintServer', {
            position: "top-right",
            timeout: 4000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.showInstallGuide = true;
        }
      } catch (error) {
        console.error('Error downloading package:', error);
        this.$notify.info(this.$i18n.t("manualDownloadInstructions") || 'يمكنك تحميل الملفات يدوياً من مجلد cashier_back/PrintServer', {
          position: "top-right",
          timeout: 4000,
          rtl: this.$i18n.locale === 'ar'
        });
        this.showInstallGuide = true;
      } finally {
        this.downloading = false;
      }
    },
    copyCommand(command) {
      navigator.clipboard.writeText(command).then(() => {
        this.$notify.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }).catch(() => {
        const textArea = document.createElement('textarea');
        textArea.value = command;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        this.$notify.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      });
    },
    resetPrinterForm() {
      this.printerForm = {
        name: '',
        description: '',
        printerName: '',
        printerType: 'windows',
        printCategory: 'Receipt',
        isActive: true,
        isMain: false
      };
      this.selectedPrinter = null;
    }
  }
};
</script>

<style scoped>
.spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.print-server-page .app-overview-grid {
  margin-bottom: 1.25rem;
}

.print-server-page .app-section-card {
  margin-bottom: 1.5rem;
}

.print-server-page .app-section-title {
  margin: 0 0 0.35rem;
}

.server-offline-card {
  margin-bottom: 1.5rem;
  border: 1px dashed color-mix(in srgb, #d97706 35%, var(--border-color));
  background: color-mix(in srgb, #f59e0b 5%, var(--bg-primary));
}

.server-offline-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 2rem 1.5rem 1.75rem;
  gap: 0.75rem;
}

.server-offline-icon-wrap {
  position: relative;
  width: 4.5rem;
  height: 4.5rem;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--primary-color) 12%, var(--bg-primary));
  border: 1px solid var(--border-color);
  margin-bottom: 0.25rem;
}

.server-offline-icon {
  font-size: 1.75rem;
  color: var(--primary-color);
}

.server-offline-status-dot {
  position: absolute;
  bottom: 0.2rem;
  inset-inline-end: 0.35rem;
  width: 0.85rem;
  height: 0.85rem;
  border-radius: 50%;
  background: #ef4444;
  border: 2px solid var(--bg-primary);
  box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.25);
}

.server-offline-title {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--text-primary);
  line-height: 1.35;
  max-width: 28rem;
}

.server-offline-message {
  margin: 0;
  font-size: 0.9rem;
  color: var(--text-secondary);
  line-height: 1.55;
  max-width: 32rem;
}

.server-offline-meta {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.5rem;
  margin: 0.15rem 0 0.35rem;
}

.server-offline-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.3rem 0.65rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  color: var(--text-secondary);
  direction: ltr;
}

.server-offline-chip--warning {
  background: rgba(245, 158, 11, 0.12);
  border-color: rgba(245, 158, 11, 0.35);
  color: #b45309;
  direction: inherit;
}

.server-offline-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 0.55rem;
  margin-top: 0.35rem;
  width: 100%;
}

.server-offline-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  padding: 0.55rem 1rem;
  border-radius: 0.55rem;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  border: 1px solid transparent;
  transition: background 0.15s, border-color 0.15s, transform 0.1s;
}

.server-offline-btn--primary {
  background: var(--primary-color);
  color: #fff;
}

.server-offline-btn--primary:hover:not(:disabled) {
  filter: brightness(1.05);
  transform: translateY(-1px);
}

.server-offline-btn--primary:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.server-offline-btn--secondary {
  background: var(--bg-secondary);
  border-color: var(--border-color);
  color: var(--text-primary);
}

.server-offline-btn--secondary:hover {
  background: var(--bg-tertiary);
}

.server-offline-btn--ghost {
  background: transparent;
  border-color: var(--border-color);
  color: var(--text-secondary);
}

.server-offline-btn--ghost:hover {
  background: var(--bg-secondary);
  color: var(--text-primary);
}

.retail-print-hint {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0.35rem 0 0;
  color: var(--text-secondary, #64748b);
  font-size: 0.9rem;
}

.retail-print-mode-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.45rem 0.85rem;
  border-radius: 999px;
  background: rgba(34, 197, 94, 0.12);
  color: #15803d;
  font-weight: 600;
  font-size: 0.9rem;
}

.printer-status-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-inline-start: auto;
}

.printer-chip {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;
}

.printer-chip--main {
  background: rgba(99, 102, 241, 0.12);
  color: var(--primary-color);
}

.printer-chip--inactive,
.printer-chip--offline {
  background: rgba(239, 68, 68, 0.12);
  color: #dc2626;
}

.printer-chip--online {
  background: rgba(34, 197, 94, 0.12);
  color: #15803d;
}

.app-item-card-footer {
  padding: 0.85rem 1rem 1rem;
  border-top: 1px solid var(--border-color);
}

.printer-test-btn {
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.printer-select-hint {
  display: block;
  margin-top: 0.45rem;
  color: var(--warning-color, #d97706);
  font-size: 0.85rem;
}

.install-instructions-section {
  width: 100%;
  max-width: 40rem;
  margin-top: 1rem;
  padding: 1rem 1.1rem;
  text-align: start;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
}

.instructions-title {
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 0.85rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

[dir="rtl"] .instructions-title {
  flex-direction: row-reverse;
}

[dir="ltr"] .instructions-title {
  flex-direction: row;
}

.instructions-list-detailed {
  color: var(--text-secondary);
  margin: 0;
  padding: 0;
  line-height: 1.7;
  font-size: 0.85rem;
}

[dir="rtl"] .instructions-list-detailed {
  text-align: right;
  padding-right: 1.5rem;
}

[dir="ltr"] .instructions-list-detailed {
  text-align: left;
  padding-left: 1.5rem;
}

.instructions-list-detailed li {
  margin-bottom: 0.65rem;
}

.instructions-list-detailed li strong {
  color: var(--text-primary);
  font-weight: 600;
}

.alternative-instructions {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px dashed var(--border-color);
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

[dir="rtl"] .btn-copy-large {
  flex-direction: row-reverse;
}

[dir="ltr"] .btn-copy-large {
  flex-direction: row;
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
}

[dir="rtl"] .command-help {
  text-align: right;
}

[dir="ltr"] .command-help {
  text-align: left;
}

@media (max-width: 640px) {
  .server-offline-content {
    padding: 1.5rem 1rem;
  }

  .server-offline-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .server-offline-btn {
    width: 100%;
  }
}
</style>

