<template>
  <div class="main-content-wrapper" :dir="direction">
    <AppHeader />
    <div class="print-server-page-container">
      <div class="print-server-page-content">
        <!-- Header -->
        <div class="users-header-section">
          <div class="users-header-content print-server-header-row">
            <div class="header-title-wrapper">
              <div class="header-icon-wrapper">
                <b-icon icon="server" class="header-icon"></b-icon>
              </div>
              <div>
                <h1 class="users-page-title">{{ $t("printServerManagement") || "إدارة خادم الطباعة" }}</h1>
                <p class="header-subtitle">{{ $t("printServerManagementDescription") || "إدارة حالة الخادم والطابعات وربط الأقسام بالطابعات" }}</p>
              </div>
            </div>
            <div class="print-server-header-actions">
              <button
                type="button"
                class="btn-refresh"
                @click="checkServerHealth()"
                :disabled="loading"
              >
                <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
              </button>
              <button
                type="button"
                class="users-add-button"
                @click="showAddPrinterModal = true"
              >
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addPrinter") || "إضافة طابعة" }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Print Server offline notice -->
        <div v-if="!serverStatus && !loading" class="server-offline-banner">
          <div class="server-offline-banner-main">
            <b-icon icon="exclamation-triangle-fill" class="server-offline-icon"></b-icon>
            <div>
              <h2 class="server-offline-title">
                {{ $t("serverNotAvailable") || "خادم الطباعة غير متصل" }}
              </h2>
              <p class="server-offline-message">
                {{ $t("serverNotAvailableMessage") || "يمكنك إعداد الطابعات أدناه. لتفعيل الطباعة الفعلية، شغّل Print Server على هذا الجهاز." }}
              </p>
            </div>
          </div>
          <button
            type="button"
            class="btn-install-guide"
            @click="showInstallInstructions"
          >
            <b-icon icon="info-circle"></b-icon>
            {{ showInstallGuide ? ($t("hideInstallGuide") || "إخفاء التعليمات") : ($t("showInstallGuide") || "تعليمات التشغيل") }}
          </button>
        </div>

        <div v-if="!serverStatus && !loading && showInstallGuide" class="server-not-available-card server-not-available-card--compact">
          <div class="server-not-available-body">
            <div class="install-instructions-section">
              <h4 class="instructions-title">
                <b-icon icon="info-circle-fill" class="me-2"></b-icon>
                {{ $t("installInstructions") || "تعليمات تشغيل Print Server (C#)" }}
              </h4>
              <ol class="instructions-list-detailed">
                <li>
                  <strong>{{ $t("installStep1") || "الخطوة 1:" }}</strong>
                  {{ $t("installStep1Desc") || "تأكد من تثبيت .NET 8 SDK على جهاز الكاشير" }}
                </li>
                <li>
                  <strong>{{ $t("installStep2") || "الخطوة 2:" }}</strong>
                  {{ $t("installStep2Desc") || "افتح مجلد restaurant_back/PrintServer في مشروع النظام" }}
                </li>
                <li>
                  <strong>{{ $t("installStep3") || "الخطوة 3:" }}</strong>
                  {{ $t("installStep3Desc") || "انقر نقراً مزدوجاً على start_print_server.bat" }}
                </li>
                <li>
                  <strong>{{ $t("installStep4") || "الخطوة 4:" }}</strong>
                  {{ $t("installStep4Desc") || "انتظر حتى تظهر: Server will run on http://localhost:5000" }}
                </li>
                <li>
                  <strong>{{ $t("installStep5") || "الخطوة 5:" }}</strong>
                  {{ $t("installStep5Desc") || "ارجع إلى هذه الصفحة واضغط «تحديث» للتحقق من الاتصال" }}
                </li>
              </ol>

              <div class="alternative-instructions">
                <h5>{{ $t("alternativeMethod") || "طريقة بديلة (سطر الأوامر):" }}</h5>
                <div class="command-box-large">
                  <code class="command-text-large">{{ printServerManualCommand }}</code>
                  <button
                    class="btn-copy-large"
                    @click="copyCommand(printServerManualCommand)"
                    :title="$t('copyCommand') || 'نسخ'"
                  >
                    <b-icon icon="clipboard"></b-icon>
                    {{ $t("copyCommand") || "نسخ" }}
                  </button>
                </div>
                <p class="command-help">
                  {{ $t("commandHelp") || "من مجلد PrintServer في المشروع" }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Overview -->
        <div v-if="loading && !serverStatus" class="print-server-section-card">
          <div class="loading-state">
            <b-spinner small></b-spinner>
            <span>{{ $t("checking") || "جاري الفحص..." }}</span>
          </div>
        </div>

        <div v-if="!loading" class="print-server-overview">
          <div class="overview-stat-card">
            <div
              class="overview-stat-icon"
              :class="serverStatus ? 'overview-stat-icon--success' : 'overview-stat-icon--danger'"
            >
              <b-icon :icon="serverStatus ? 'hdd-network-fill' : 'hdd-network'"></b-icon>
            </div>
            <div class="overview-stat-content">
              <div class="overview-stat-value">
                {{ serverStatus ? ($t("online") || "متصل") : ($t("offline") || "غير متصل") }}
              </div>
              <div class="overview-stat-label">{{ $t("serverStatus") || "حالة الخادم" }}</div>
            </div>
          </div>
          <div class="overview-stat-card">
            <div class="overview-stat-icon overview-stat-icon--primary">
              <b-icon icon="printer-fill"></b-icon>
            </div>
            <div class="overview-stat-content">
              <div class="overview-stat-value">{{ managedPrinters.length }}</div>
              <div class="overview-stat-label">{{ $t("printersManagement") || "الطابعات" }}</div>
            </div>
          </div>
          <div class="overview-stat-card">
            <div class="overview-stat-icon overview-stat-icon--info">
              <b-icon icon="wifi"></b-icon>
            </div>
            <div class="overview-stat-content">
              <div class="overview-stat-value">{{ printersOnlineCount }}</div>
              <div class="overview-stat-label">{{ $t("printerOnline") || "طابعات متصلة" }}</div>
            </div>
          </div>
          <div class="overview-stat-card">
            <div class="overview-stat-icon overview-stat-icon--warning">
              <b-icon icon="tags-fill"></b-icon>
            </div>
            <div class="overview-stat-content">
              <div class="overview-stat-value">{{ tagPrinters.length }}</div>
              <div class="overview-stat-label">{{ $t("tagPrintersManagement") || "ربط الأقسام" }}</div>
            </div>
          </div>
        </div>

        <!-- Printers -->
        <div class="print-server-section-card">
          <div class="print-server-section-header">
            <div class="print-server-section-title-wrap">
              <div class="print-server-section-icon-wrap">
                <b-icon icon="printer-fill" class="print-server-section-icon"></b-icon>
              </div>
              <div>
                <h3 class="print-server-section-title">{{ $t("printersManagement") || "إدارة الطابعات" }}</h3>
                <p class="print-server-section-subtitle">{{ $t("printersManagementHint") || "إعداد الطابعات وتجربة الطباعة" }}</p>
              </div>
            </div>
          </div>
          <div class="print-server-section-body">
            <div v-if="loadingPrinters" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="managedPrinters.length > 0" class="print-server-cards-grid">
              <div
                v-for="printer in managedPrinters"
                :key="printer.id"
                class="print-server-item-card"
              >
                <div class="print-server-item-card-header">
                  <div class="print-server-item-card-title">
                    <b-icon icon="printer-fill" class="print-server-item-card-icon"></b-icon>
                    <h4>{{ printer.name }}</h4>
                  </div>
                  <div class="print-server-item-badges">
                    <span v-if="printer.isMain" class="item-badge item-badge--main">
                      {{ $t("mainPrinter") || "رئيسية" }}
                    </span>
                    <span v-if="printer.isPublicOrderPrinter" class="item-badge item-badge--public">
                      {{ $t("publicOrderPrinter") || "طلبات عامة" }}
                    </span>
                    <span v-if="!printer.isActive" class="item-badge item-badge--inactive">
                      {{ $t("inactive") || "غير مفعل" }}
                    </span>
                    <span
                      v-else-if="getPrinterStatus(printer.id).online"
                      class="item-badge item-badge--online"
                    >
                      <b-icon icon="circle-fill"></b-icon>
                      {{ $t("online") || "متصل" }}
                    </span>
                    <span v-else class="item-badge item-badge--offline">
                      <b-icon icon="circle-fill"></b-icon>
                      {{ $t("offline") || "غير متصل" }}
                    </span>
                  </div>
                  <div class="print-server-item-card-actions">
                    <button
                      type="button"
                      class="action-btn action-btn--icon action-btn--edit"
                      @click="editPrinter(printer)"
                      :title="$t('edit') || 'تعديل'"
                    >
                      <b-icon icon="pencil" class="action-icon"></b-icon>
                    </button>
                    <button
                      type="button"
                      class="action-btn action-btn--icon action-btn--delete"
                      @click="confirmDeletePrinter(printer)"
                      :title="$t('delete') || 'حذف'"
                    >
                      <b-icon icon="trash" class="action-icon"></b-icon>
                    </button>
                  </div>
                </div>
                <div class="print-server-item-card-body">
                  <div class="print-server-info-row">
                    <b-icon icon="pc-display" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("systemPrinterName") || "في النظام" }}</span>
                    <span class="info-value">{{ printer.printerName }}</span>
                  </div>
                  <div class="print-server-info-row">
                    <b-icon icon="gear-fill" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("type") || "النوع" }}</span>
                    <span class="info-value">{{ printer.printerType }}</span>
                  </div>
                  <div v-if="printer.printCategory" class="print-server-info-row">
                    <b-icon icon="folder-fill" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("printCategory") || "الفئة" }}</span>
                    <span class="info-value">{{ getCategoryLabel(printer.printCategory) }}</span>
                  </div>
                  <div v-if="printer.description" class="print-server-info-row">
                    <b-icon icon="file-text" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("description") || "الوصف" }}</span>
                    <span class="info-value">{{ printer.description }}</span>
                  </div>
                </div>
                <div class="print-server-item-card-footer">
                  <button
                    type="button"
                    class="print-server-test-btn"
                    @click="testPrintToPrinter(printer.id)"
                    :disabled="!printer.isActive || testingPrint || !serverStatus"
                  >
                    <b-icon icon="printer-fill"></b-icon>
                    {{ $t("testPrint") || "اختبار الطباعة" }}
                  </button>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="printer" class="empty-icon"></b-icon>
              <p>{{ $t("noPrintersConfigured") || "لم يتم إعداد أي طابعات" }}</p>
              <button type="button" class="empty-state-btn" @click="showAddPrinterModal = true">
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addFirstPrinter") || "إضافة أول طابعة" }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Tag ↔ Printer links -->
        <div class="print-server-section-card">
          <div class="print-server-section-header print-server-section-header--with-action">
            <div class="print-server-section-title-wrap">
              <div class="print-server-section-icon-wrap print-server-section-icon-wrap--tags">
                <b-icon icon="tags-fill" class="print-server-section-icon"></b-icon>
              </div>
              <div>
                <h3 class="print-server-section-title">{{ $t("tagPrintersManagement") || "إدارة طباعة الأقسام" }}</h3>
                <p class="print-server-section-subtitle">{{ $t("tagPrintersManagementHint") || "ربط الأقسام الرئيسية أو الفرعية بطابعة المطبخ المناسبة" }}</p>
              </div>
            </div>
            <button type="button" class="users-add-button users-add-button--compact" @click="openAddTagPrinterModal">
              <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
              <span class="button-text">{{ $t("addTagPrinter") || "إضافة ربط" }}</span>
            </button>
          </div>
          <div class="print-server-section-body">
            <div v-if="loadingTagPrinters" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="tagPrinters.length > 0" class="print-server-cards-grid print-server-cards-grid--compact">
              <div
                v-for="tagPrinter in tagPrinters"
                :key="tagPrinter.id"
                class="print-server-item-card print-server-item-card--link"
              >
                <div class="print-server-item-card-header">
                  <div class="print-server-item-card-title">
                    <b-icon icon="tag-fill" class="print-server-item-card-icon"></b-icon>
                    <h4>{{ tagPrinterLabel(tagPrinter) || ($t("undefinedTag") || "قسم غير محدد") }}</h4>
                  </div>
                  <div class="print-server-item-card-actions">
                    <button
                      type="button"
                      class="action-btn action-btn--icon action-btn--edit"
                      @click="editTagPrinter(tagPrinter)"
                      :title="$t('edit') || 'تعديل'"
                    >
                      <b-icon icon="pencil" class="action-icon"></b-icon>
                    </button>
                    <button
                      type="button"
                      class="action-btn action-btn--icon action-btn--delete"
                      @click="confirmDeleteTagPrinter(tagPrinter)"
                      :title="$t('delete') || 'حذف'"
                    >
                      <b-icon icon="trash" class="action-icon"></b-icon>
                    </button>
                  </div>
                </div>
                <div class="print-server-item-card-body">
                  <div class="print-server-link-arrow">
                    <span class="print-server-link-from">{{ tagPrinterLabel(tagPrinter) }}</span>
                    <b-icon icon="arrow-left" class="print-server-link-arrow-icon"></b-icon>
                    <span class="print-server-link-to">{{ tagPrinter.printer?.name || "—" }}</span>
                  </div>
                  <div class="print-server-info-row">
                    <b-icon icon="printer-fill" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("printer") || "الطابعة" }}</span>
                    <span class="info-value">{{ tagPrinter.printer?.name || "—" }}</span>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="tags" class="empty-icon"></b-icon>
              <p>{{ $t("noTagPrintersConfigured") || "لم يتم إعداد أي ربط بين الأقسام والطابعات" }}</p>
              <button type="button" class="empty-state-btn" @click="openAddTagPrinterModal">
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addFirstTagPrinter") || "إضافة أول ربط" }}</span>
              </button>
            </div>
          </div>
        </div>

      </div>
    </div>

    <!-- Add Tag Printer Modal -->
    <b-modal
      v-model="showAddTagPrinterModal"
      @hidden="resetTagPrinterForm"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">
          {{
            selectedTagPrinter
              ? $t("editTagPrinter") || "تعديل ربط قسم بطابعة"
              : $t("addTagPrinter") || "إضافة ربط قسم بطابعة"
          }}
        </h2>
        <form @submit.prevent="saveTagPrinter" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
                {{
                  selectedTagPrinter
                    ? ($t("category") || "القسم")
                    : ($t("selectCategoriesForPrinter") || "الأقسام (رئيسي أو فرعي)")
                }}
                <span class="required">*</span>
              </label>
              <select
                v-if="selectedTagPrinter"
                v-model="tagPrinterForm.tagId"
                class="users-form-select"
                :disabled="loadingTags"
                required
              >
                <option value="">{{ $t("selectCategory") || "اختر القسم" }}</option>
                <option
                  v-for="tag in tagsForPrinterSelectList"
                  :key="tag.id"
                  :value="tag.id"
                >
                  {{ tag.label }}
                </option>
              </select>
              <div v-else class="tag-printer-multi-select" :class="{ 'is-disabled': loadingTags }">
                <label
                  v-for="tag in tagsForPrinterSelectList"
                  :key="tag.id"
                  class="tag-printer-multi-option"
                  :class="{ 'tag-printer-multi-option--sub': !tag.isRoot }"
                >
                  <input
                    type="checkbox"
                    :value="String(tag.id)"
                    v-model="tagPrinterForm.tagIds"
                  />
                  <span>{{ tag.label }}</span>
                </label>
                <p v-if="!tagsForPrinterSelectList.length" class="tag-printer-multi-empty">
                  {{ $t("noCategoriesAvailable") || "لا توجد أقسام" }}
                </p>
              </div>
              <p v-if="!selectedTagPrinter" class="users-form-hint">
                {{ $t("tagPrinterMultiSelectHint") || "يمكن اختيار قسم فرعي واحد أو أكثر مع الطابعة نفسها" }}
              </p>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="printer-fill" class="form-label-icon"></b-icon>
                {{ $t("printer") || "الطابعة" }}
                <span class="required">*</span>
              </label>
              <select
                v-model="tagPrinterForm.printerId"
                class="users-form-select"
                :disabled="loadingPrinters"
                required
              >
                <option value="">{{ $t("selectPrinter") || "اختر الطابعة" }}</option>
                <option
                  v-for="printer in managedPrinters"
                  :key="printer.id"
                  :value="printer.id"
                >
                  {{ printer.name }}
                </option>
              </select>
            </div>
          </div>
          <div class="users-form-actions">
            <button
              type="button"
              class="users-form-cancel-button"
              @click="showAddTagPrinterModal = false"
              :disabled="savingTagPrinter"
            >
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button
              type="submit"
              class="users-form-submit-button"
              :disabled="!canSaveTagPrinter || savingTagPrinter"
            >
              <b-spinner small v-if="savingTagPrinter" class="me-2"></b-spinner>
              {{
                savingTagPrinter
                  ? selectedTagPrinter
                    ? $t("updating") || "جاري التحديث..."
                    : $t("adding") || "جاري الإضافة..."
                  : selectedTagPrinter
                    ? $t("update") || "تحديث"
                    : $t("add") || "إضافة"
              }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Add Printer Modal -->
    <b-modal 
      v-model="showAddPrinterModal" 
      @hidden="resetPrinterForm"
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addPrinter") || "إضافة طابعة" }}</h2>
        <form @submit.prevent="addPrinter" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="printer-fill" class="form-label-icon"></b-icon>
                {{ $t("printerName") || "اسم الطابعة" }} <span class="required">*</span>
              </label>
              <input 
                v-model="printerForm.name" 
                type="text" 
                class="users-form-input"
                placeholder="مثال: طابعة الكاشير الرئيسية"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="list-ul" class="form-label-icon"></b-icon>
                {{ $t("printerType") || "نوع الطابعة" }} <span class="required">*</span>
              </label>
              <select v-model="printerForm.printerType" class="users-form-select" required>
                <option value="windows">Windows</option>
                <option value="usb">USB</option>
                <option value="serial">Serial</option>
                <option value="network">Network</option>
                <option value="file">File</option>
              </select>
            </div>
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
              placeholder="وصف الطابعة..."
            ></textarea>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="printer" class="form-label-icon"></b-icon>
              {{ $t("systemPrinterName") || "اسم الطابعة في النظام" }} <span class="required">*</span>
            </label>
            <select
              v-if="printers.length"
              v-model="printerForm.printerName"
              class="users-form-select"
              @change="printerForm.printerName = $event.target.value"
              required
            >
              <option value="">{{ $t("selectPrinter") || "اختر الطابعة" }}</option>
              <option v-for="printer in printers" :key="printer.name" :value="printer.name">
                {{ printer.name }} ({{ printer.type }})
              </option>
            </select>
            <input
              v-else
              v-model="printerForm.printerName"
              type="text"
              class="users-form-input"
              :placeholder="$t('manualPrinterNameHint') || 'اسم الطابعة في Windows (مثال: EPSON TM-T20)'"
              required
            />
            <p v-if="!serverStatus && !printers.length" class="form-field-hint">
              {{ $t("manualPrinterNameOfflineHint") || "خادم الطباعة غير متصل — أدخل اسم الطابعة يدوياً كما يظهر في Windows" }}
            </p>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
              {{ $t("printCategory") || "فئة الطباعة" }}
            </label>
            <select v-model="printerForm.printCategory" class="users-form-select">
              <option value="">{{ $t("selectCategory") || "اختر الفئة" }}</option>
              <option v-for="cat in availablePrintCategories" :key="cat.value" :value="cat.value">
                {{ cat.label }}
              </option>
            </select>
          </div>
          <div class="form-toggle-cards">
            <label
              class="form-toggle-card"
              :class="{ 'form-toggle-card--on': printerForm.isActive }"
            >
              <input
                v-model="printerForm.isActive"
                type="checkbox"
                id="printer-active"
                class="form-toggle-card-input"
              />
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
              <input
                v-model="printerForm.isMain"
                type="checkbox"
                id="printer-main"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--warning">
                  <b-icon icon="star-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("mainPrinter") || "طابعة رئيسية" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("mainPrinterHint") || "تطبع كل الفواتير والإيصالات" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card form-toggle-card--accent-info"
              :class="{ 'form-toggle-card--on': printerForm.isPublicOrderPrinter }"
            >
              <input
                v-model="printerForm.isPublicOrderPrinter"
                type="checkbox"
                id="printer-public-order"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--info">
                  <b-icon icon="bag-check-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("publicOrderPrinter") || "طابعة الطلبات العامة" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("publicOrderPrinterHint") || "تطبع إيصال طلبات الصفحة العامة للزبون" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showAddPrinterModal = false" :disabled="savingPrinter">
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="savingPrinter">
              <b-spinner small v-if="savingPrinter" class="me-2"></b-spinner>
              {{ savingPrinter ? ($t("adding") || "جاري الإضافة...") : ($t("add") || "إضافة") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Edit Printer Modal -->
    <b-modal
      v-model="showEditPrinterModal"
      @hidden="resetPrinterForm"
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("editPrinter") || "تعديل طابعة" }}</h2>
        <form @submit.prevent="updatePrinter" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="printer-fill" class="form-label-icon"></b-icon>
                {{ $t("printerName") || "اسم الطابعة" }} <span class="required">*</span>
              </label>
              <input 
                v-model="printerForm.name" 
                type="text" 
                class="users-form-input"
                placeholder="مثال: طابعة الكاشير الرئيسية"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="list-ul" class="form-label-icon"></b-icon>
                {{ $t("printerType") || "نوع الطابعة" }} <span class="required">*</span>
              </label>
              <select v-model="printerForm.printerType" class="users-form-select" required>
                <option value="windows">Windows</option>
                <option value="usb">USB</option>
                <option value="serial">Serial</option>
                <option value="network">Network</option>
                <option value="file">File</option>
              </select>
            </div>
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
              placeholder="وصف الطابعة..."
            ></textarea>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="printer" class="form-label-icon"></b-icon>
              {{ $t("systemPrinterName") || "اسم الطابعة في النظام" }} <span class="required">*</span>
            </label>
            <select
              v-if="printers.length"
              v-model="printerForm.printerName"
              class="users-form-select"
              @change="printerForm.printerName = $event.target.value"
              required
            >
              <option value="">{{ $t("selectPrinter") || "اختر الطابعة" }}</option>
              <option v-for="printer in printers" :key="printer.name" :value="printer.name">
                {{ printer.name }} ({{ printer.type }})
              </option>
            </select>
            <input
              v-else
              v-model="printerForm.printerName"
              type="text"
              class="users-form-input"
              :placeholder="$t('manualPrinterNameHint') || 'اسم الطابعة في Windows (مثال: EPSON TM-T20)'"
              required
            />
            <p v-if="!serverStatus && !printers.length" class="form-field-hint">
              {{ $t("manualPrinterNameOfflineHint") || "خادم الطباعة غير متصل — أدخل اسم الطابعة يدوياً كما يظهر في Windows" }}
            </p>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
              {{ $t("printCategory") || "فئة الطباعة" }}
            </label>
            <select v-model="printerForm.printCategory" class="users-form-select">
              <option value="">{{ $t("selectCategory") || "اختر الفئة" }}</option>
              <option v-for="cat in availablePrintCategories" :key="cat.value" :value="cat.value">
                {{ cat.label }}
              </option>
            </select>
          </div>
          <div class="form-toggle-cards">
            <label
              class="form-toggle-card"
              :class="{ 'form-toggle-card--on': printerForm.isActive }"
            >
              <input
                v-model="printerForm.isActive"
                type="checkbox"
                id="edit-printer-active"
                class="form-toggle-card-input"
              />
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
              <input
                v-model="printerForm.isMain"
                type="checkbox"
                id="edit-printer-main"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--warning">
                  <b-icon icon="star-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("mainPrinter") || "طابعة رئيسية" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("mainPrinterHint") || "تطبع كل الفواتير والإيصالات" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card form-toggle-card--accent-info"
              :class="{ 'form-toggle-card--on': printerForm.isPublicOrderPrinter }"
            >
              <input
                v-model="printerForm.isPublicOrderPrinter"
                type="checkbox"
                id="edit-printer-public-order"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--info">
                  <b-icon icon="bag-check-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("publicOrderPrinter") || "طابعة الطلبات العامة" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("publicOrderPrinterHint") || "تطبع إيصال طلبات الصفحة العامة للزبون" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showEditPrinterModal = false" :disabled="savingPrinter">
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="savingPrinter">
              <b-spinner small v-if="savingPrinter" class="me-2"></b-spinner>
              {{ savingPrinter ? ($t("updating") || "جاري التحديث...") : ($t("update") || "تحديث") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>


    <!-- Print Modal -->
    <b-modal
      v-model="showPrintModal"
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
              <b-icon icon="files" class="form-label-icon"></b-icon>
              {{ $t("copies") || "عدد النسخ" }}
              <span class="required">*</span>
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
              <b-icon icon="code-slash" class="form-label-icon"></b-icon>
              {{ $t("printContent") || "محتوى الطباعة" }}
            </label>
            <textarea
              v-model="printForm.htmlContent"
              class="users-form-input"
              rows="10"
              placeholder="أدخل محتوى HTML للطباعة..."
            ></textarea>
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showPrintModal = false">
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="testingPrint">
              <b-spinner small v-if="testingPrint" class="me-2"></b-spinner>
              {{ testingPrint ? ($t("printing") || "جاري الطباعة...") : ($t("print") || "طباعة") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../http/api.js";
import {
  tagsForPrinterSelect,
  tagPrinterDisplayLabel,
} from "@/utils/tagHierarchy.js";

const PRINT_SERVER_URL = 'http://localhost:5000';

export default {
  name: "PrintServerManagementView",
  components: {
    AppHeader,
  },
  data() {
    return {
      loading: false,
      loadingPrinters: false,
      downloading: false,
      showInstallGuide: false,
      serverStatus: null,
      printers: [],
      managedPrinters: [],
      printerStatuses: {}, // { printerId: { online, available, error } }
      statusCheckInterval: null,
      showAddPrinterModal: false,
      showEditPrinterModal: false,
      showPrintModal: false,
      selectedPrinter: null,
      tagPrinters: [],
      loadingTagPrinters: false,
      tags: [],
      loadingTags: false,
      showAddTagPrinterModal: false,
      selectedTagPrinter: null,
      savingPrinter: false,
      savingTagPrinter: false,
      tagPrinterForm: {
        tagId: '',
        tagIds: [],
        printerId: ''
      },
      printerForm: {
        name: '',
        description: '',
        printerName: '',
        printerType: 'windows',
        printCategory: '',
        isActive: true,
        isMain: false,
        isPublicOrderPrinter: false
      },
      printForm: {
        printerId: null,
        htmlContent: '',
        copies: 1
      },
      testingPrint: false,
      testContent: 'اختبار الطباعة\nهذا نص تجريبي للتحقق من عمل الطابعة بشكل صحيح.',
      availablePrintCategories: [
        { value: 'Receipt', label: 'كاشير رئيسية' },
        { value: 'Kitchen', label: 'مطبخ' },
        { value: 'CustomerOrder', label: 'الطلبات العامة' },
        { value: 'Report', label: 'تقارير' },
        { value: 'Other', label: 'أخرى' }
      ]
    };
  },
  computed: {
    direction() {
      return this.$i18n.locale === "ar" ? "rtl" : "ltr";
    },
    tagsForPrinterSelectList() {
      return tagsForPrinterSelect(this.tags);
    },
    canSaveTagPrinter() {
      if (!this.tagPrinterForm.printerId) return false;
      if (this.selectedTagPrinter) {
        return !!this.tagPrinterForm.tagId;
      }
      return Array.isArray(this.tagPrinterForm.tagIds) && this.tagPrinterForm.tagIds.length > 0;
    },
    printersOnlineCount() {
      if (!this.serverStatus) return 0;
      return this.managedPrinters.filter(
        (p) => p.isActive && this.getPrinterStatus(p.id).online
      ).length;
    },
    printServerManualCommand() {
      return "cd restaurant_back\\PrintServer && start_print_server.bat";
    },
  },
  mounted() {
    this.checkServerHealth(true);
    this.loadManagedPrinters();
    this.loadPrinters(true);
    this.loadTagPrinters();
    this.loadTags();
    this.startStatusPolling();
  },
  beforeDestroy() {
    this.stopStatusPolling();
  },
  methods: {
    async checkServerHealth(silent = false) {
      this.loading = true;
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/health`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });
        
        if (response.ok) {
          this.serverStatus = await response.json();
          if (this.serverStatus.config) {
            this.currentDefaultPrinter = this.serverStatus.config.windows_printer_name || null;
          }
          if (!silent) {
            this.$toast.success(this.$i18n.t("serverStatusUpdated") || 'تم تحديث حالة الخادم', {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            });
          }
          await this.loadPrinters(true);
        } else {
          this.serverStatus = null;
          if (!silent) {
            this.$toast.error(this.$i18n.t("serverNotAvailable") || 'خادم الطباعة غير متصل', {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            });
          }
        }
      } catch (error) {
        console.error('Error checking server health:', error);
        this.serverStatus = null;
        if (!silent) {
          this.$toast.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بخادم الطباعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } finally {
        this.loading = false;
      }
    },
    async loadPrinters(silent = false) {
      this.loadingPrinters = true;
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/printers`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });
        
        if (response.ok) {
          const data = await response.json();
          this.printers = data.printers || [];
        } else {
          this.printers = [];
        }
      } catch (error) {
        console.error('Error loading printers:', error);
        this.printers = [];
        if (!silent) {
          this.$toast.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بخادم الطباعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } finally {
        this.loadingPrinters = false;
      }
    },
    async testPrint() {
      if (!this.testContent || !this.testContent.trim()) {
          this.$toast.warning(this.$i18n.t("pleaseEnterTestContent") || 'يرجى إدخال محتوى للاختبار', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        return;
      }

      this.testing = true;
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/print`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            htmlContent: `<div style="text-align: center; padding: 20px;">
              <h2>اختبار الطباعة</h2>
              <p>${(this.testContent || '').replace(/\n/g, '<br>')}</p>
              <p style="margin-top: 20px;">تاريخ: ${new Date().toLocaleDateString('ar-EG')}</p>
              <p>الوقت: ${new Date().toLocaleTimeString('ar-EG')}</p>
            </div>`
          }),
        });
        
        const result = await response.json();
        
        if (response.ok && result.success) {
          this.$toast.success(this.$i18n.t("printTestSuccess") || 'تم إرسال أمر الطباعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(result.message || this.$i18n.t("printTestFailed") || 'فشلت الطباعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error testing print:', error);
        this.$toast.error(this.$i18n.t("printTestError") || 'حدث خطأ أثناء الطباعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
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

        const response = await fetch(`${PRINT_SERVER_URL}/print`, {
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
          this.$toast.success(this.$i18n.t("printTestSuccess") || 'تم إرسال أمر الطباعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(result.message || this.$i18n.t("printTestFailed") || 'فشلت الطباعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error testing print receipt:', error);
        this.$toast.error(this.$i18n.t("printTestError") || 'حدث خطأ أثناء الطباعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.testing = false;
      }
    },
    async loadManagedPrinters() {
      try {
        const response = await HTTP.get('Printers');
        if (response.data && !response.data.errorStatus) {
          this.managedPrinters = response.data.data || [];
          // Check status for all printers
          this.checkAllPrinterStatuses();
        }
      } catch (error) {
        console.error('Error loading managed printers:', error);
        this.managedPrinters = [];
      }
    },
    async checkPrinterStatus(printerId) {
      try {
        const response = await HTTP.get(`Printers/${printerId}/status`);
        if (response.data && !response.data.errorStatus && response.data.data) {
          this.$set(this.printerStatuses, printerId, {
            online: response.data.data.online,
            available: response.data.data.available,
            error: response.data.data.error
          });
        }
      } catch (error) {
        console.error(`Error checking printer status for ${printerId}:`, error);
        this.$set(this.printerStatuses, printerId, {
          online: false,
          available: false,
          error: 'Cannot check status'
        });
      }
    },
    async checkAllPrinterStatuses() {
      for (const printer of this.managedPrinters) {
        if (printer.isActive) {
          await this.checkPrinterStatus(printer.id);
        }
      }
    },
    startStatusPolling() {
      this.statusCheckInterval = setInterval(() => {
        if (this.managedPrinters.length > 0 && this.serverStatus) {
          this.checkAllPrinterStatuses();
        }
      }, 5000);
    },
    stopStatusPolling() {
      if (this.statusCheckInterval) {
        clearInterval(this.statusCheckInterval);
        this.statusCheckInterval = null;
      }
    },
    getPrinterStatus(printerId) {
      return this.printerStatuses[printerId] || { online: false, available: false, error: null };
    },
    getCategoryLabel(category) {
      const cat = this.availablePrintCategories.find(c => c.value === category);
      return cat ? cat.label : category;
    },
    async addPrinter() {
      try {
        const response = await HTTP.post('Printers', this.printerForm);
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("printerAddedSuccess") || 'تم إضافة الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.showAddPrinterModal = false;
          this.resetPrinterForm();
          this.loadManagedPrinters();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("printerAddFailed") || 'فشل إضافة الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error adding printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("printerAddError") || 'حدث خطأ أثناء إضافة الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingPrinter = false;
      }
    },
    editPrinter(printer) {
      this.selectedPrinter = printer;
      this.printerForm = {
        name: printer.name,
        description: printer.description || '',
        printerName: printer.printerName,
        printerType: printer.printerType,
        printCategory: printer.printCategory || '',
        isActive: printer.isActive,
        isMain: printer.isMain || false,
        isPublicOrderPrinter: printer.isPublicOrderPrinter || false
      };
      this.showEditPrinterModal = true;
    },
    async updatePrinter() {
      try {
        const response = await HTTP.put(`Printers/${this.selectedPrinter.id}`, this.printerForm);
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("printerUpdatedSuccess") || 'تم تحديث الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.showEditPrinterModal = false;
          this.resetPrinterForm();
          this.loadManagedPrinters();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("printerUpdateFailed") || 'فشل تحديث الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error updating printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("printerUpdateError") || 'حدث خطأ أثناء تحديث الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingPrinter = false;
      }
    },
    async confirmDeletePrinter(printer) {
      const ok = await this.$confirm({
        message: this.deletePrinterMessageFor(printer),
      });
      if (ok) {
        await this.deletePrinter(printer.id);
      }
    },
    deletePrinterMessageFor(printer) {
      const name = printer?.name || "";
      return (
        this.$t("confirmDeletePrinter", { name }) ||
        `هل أنت متأكد من حذف الطابعة "${name}"؟`
      );
    },
    async deletePrinter(id) {
      try {
        const response = await HTTP.delete(`Printers/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("printerDeletedSuccess") || 'تم حذف الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.loadManagedPrinters();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("printerDeleteFailed") || 'فشل حذف الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("printerDeleteError") || 'حدث خطأ أثناء حذف الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    resetPrinterForm() {
      this.printerForm = {
        name: '',
        description: '',
        printerName: '',
        printerType: 'windows',
        printCategory: '',
        isActive: true,
        isMain: false,
        isPublicOrderPrinter: false
      };
      this.selectedPrinter = null;
    },
    async testPrintToPrinter(printerId) {
      this.printForm.printerId = printerId;
      const testContent = this.testContent || 'اختبار الطباعة\nهذا نص تجريبي للتحقق من عمل الطابعة بشكل صحيح.';
      this.printForm.htmlContent = `<div style="text-align: center; padding: 20px;">
        <h2>اختبار الطباعة</h2>
        <p>${testContent.replace(/\n/g, '<br>')}</p>
        <p style="margin-top: 20px;">تاريخ: ${new Date().toLocaleDateString('ar-EG')}</p>
        <p>الوقت: ${new Date().toLocaleTimeString('ar-EG')}</p>
      </div>`;
      this.printForm.copies = 1;
      this.showPrintModal = true;
    },
    async sendPrint() {
      if (!this.printForm.printerId) {
        this.$toast.warning(this.$i18n.t("pleaseSelectPrinter") || 'يرجى اختيار طابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }

      this.testingPrint = true;
      try {
        const response = await HTTP.post(`Printers/${this.printForm.printerId}/print`, {
          htmlContent: this.printForm.htmlContent,
          copies: this.printForm.copies || 1
        });
        
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("printSentSuccessfully") || `تم إرسال أمر الطباعة بنجاح (${this.printForm.copies} نسخة)`, {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.showPrintModal = false;
          this.printForm = {
            printerId: null,
            htmlContent: '',
            copies: 1
          };
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("printFailed") || 'فشلت الطباعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error printing:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("printError") || 'حدث خطأ أثناء الطباعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.testingPrint = false;
      }
    },
    copyCommand(command) {
      navigator.clipboard.writeText(command).then(() => {
          this.$toast.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
      }).catch(() => {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = command;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
          this.$toast.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
      });
    },
    async downloadPrintServer() {
      this.downloading = true;
      try {
        // Download from backend
        const response = await fetch(`${PRINT_SERVER_URL}/download`, {
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
          
          this.$toast.success(this.$i18n.t("downloadStarted") || 'تم بدء التحميل', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          
          // Show install guide after download
          setTimeout(() => {
            this.showInstallGuide = true;
          }, 1000);
        } else {
          // If server is not available, show instructions to download manually
          this.$toast.warning(this.$i18n.t("serverNotAvailableForDownload") || 'الخادم غير متاح. يرجى تحميل الملفات يدوياً من مجلد restaurant_back', {
            position: "top-right",
            timeout: 4000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.showInstallGuide = true;
        }
      } catch (error) {
        console.error('Error downloading package:', error);
        // Show manual download instructions
        this.$toast.info(this.$i18n.t("manualDownloadInstructions") || 'يمكنك تحميل الملفات يدوياً من مجلد restaurant_back', {
          position: "top-right",
          timeout: 4000,
          rtl: this.$i18n.locale === 'ar'
        });
        this.showInstallGuide = true;
      } finally {
        this.downloading = false;
      }
    },
    showInstallInstructions() {
      this.showInstallGuide = !this.showInstallGuide;
    },
    async loadTagPrinters() {
      this.loadingTagPrinters = true;
      try {
        const response = await HTTP.get('TagPrinters');
        if (response.data && !response.data.errorStatus) {
          this.tagPrinters = response.data.data || [];
        } else {
          this.tagPrinters = [];
        }
      } catch (error) {
        console.error('Error loading tag printers:', error);
        this.tagPrinters = [];
      } finally {
        this.loadingTagPrinters = false;
      }
    },
    async loadTags() {
      this.loadingTags = true;
      try {
        const response = await HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`);
        if (response.data && response.data.data) {
          this.tags = response.data.data.items || [];
        } else {
          this.tags = [];
        }
      } catch (error) {
        console.error('Error loading tags:', error);
        this.tags = [];
      } finally {
        this.loadingTags = false;
      }
    },
    tagPrinterLabel(tagPrinter) {
      return tagPrinterDisplayLabel(tagPrinter, this.tags);
    },
    async saveTagPrinter() {
      if (!this.canSaveTagPrinter) {
        this.$toast.warning(this.$i18n.t("pleaseFillAllFields") || 'يرجى ملء جميع الحقول المطلوبة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }

      try {
        this.savingTagPrinter = true;
        const printerId = parseInt(this.tagPrinterForm.printerId, 10);

        if (this.selectedTagPrinter) {
          const response = await HTTP.put(`TagPrinters/${this.selectedTagPrinter.id}`, {
            tagId: parseInt(this.tagPrinterForm.tagId, 10),
            printerId
          });
          if (response.data && !response.data.errorStatus) {
            this.$toast.success(
              this.$i18n.t("tagPrinterUpdatedSuccess") || 'تم تحديث ربط القسم بالطابعة بنجاح',
              {
                position: "top-right",
                timeout: 3000,
                rtl: this.$i18n.locale === 'ar'
              }
            );
            this.showAddTagPrinterModal = false;
            this.resetTagPrinterForm();
            await this.loadTagPrinters();
          } else {
            this.$toast.error(response.data?.message || this.$i18n.t("tagPrinterSaveFailed") || 'فشل حفظ ربط القسم بالطابعة', {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            });
          }
          return;
        }

        const tagIds = [...new Set((this.tagPrinterForm.tagIds || []).map((id) => parseInt(id, 10)))].filter(
          (id) => !Number.isNaN(id)
        );
        let ok = 0;
        let fail = 0;
        let lastError = "";
        for (const tagId of tagIds) {
          try {
            const response = await HTTP.post('TagPrinters', { tagId, printerId });
            if (response.data && !response.data.errorStatus) {
              ok += 1;
            } else {
              fail += 1;
              lastError = response.data?.message || lastError;
            }
          } catch (error) {
            fail += 1;
            lastError = error.response?.data?.message || lastError;
            console.error('Error creating tag-printer link:', error);
          }
        }

        if (ok > 0) {
          this.$toast.success(
            fail > 0
              ? (this.$i18n.t("tagPrintersPartiallyAdded", { ok, fail }) ||
                  `تم ربط ${ok} قسم(أقسام)، وفشل ${fail}`)
              : (this.$i18n.t("tagPrinterAddedSuccess") || 'تم إضافة ربط القسم بالطابعة بنجاح'),
            {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            }
          );
          this.showAddTagPrinterModal = false;
          this.resetTagPrinterForm();
          await this.loadTagPrinters();
        } else {
          this.$toast.error(
            lastError || this.$i18n.t("tagPrinterSaveFailed") || 'فشل حفظ ربط القسم بالطابعة',
            {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            }
          );
        }
      } catch (error) {
        console.error('Error saving tag printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("tagPrinterSaveError") || 'حدث خطأ أثناء حفظ ربط القسم بالطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingTagPrinter = false;
      }
    },
    editTagPrinter(tagPrinter) {
      this.selectedTagPrinter = tagPrinter;
      this.tagPrinterForm = {
        tagId: tagPrinter.tagId,
        tagIds: [],
        printerId: tagPrinter.printerId
      };
      this.showAddTagPrinterModal = true;
    },
    openAddTagPrinterModal() {
      this.selectedTagPrinter = null;
      this.resetTagPrinterForm();
      this.showAddTagPrinterModal = true;
    },
    async confirmDeleteTagPrinter(tagPrinter) {
      const ok = await this.$confirm({
        message: this.deleteTagPrinterMessageFor(tagPrinter),
      });
      if (ok) {
        await this.deleteTagPrinter(tagPrinter.id);
      }
    },
    deleteTagPrinterMessageFor(tagPrinter) {
      const tagName = this.tagPrinterLabel(tagPrinter) || tagPrinter?.tag?.name || "";
      const printerName = tagPrinter?.printer?.name || tagPrinter?.Printer?.name || "";
      return (
        this.$t("confirmDeleteTagPrinterDetailed", { tagName, printerName }) ||
        `هل أنت متأكد من حذف ربط القسم "${tagName}" بالطابعة "${printerName}"؟`
      );
    },
    async deleteTagPrinter(id) {
      try {
        const response = await HTTP.delete(`TagPrinters/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("tagPrinterDeletedSuccess") || 'تم حذف ربط القسم بالطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          await this.loadTagPrinters();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("tagPrinterDeleteFailed") || 'فشل حذف ربط القسم بالطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting tag printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("tagPrinterDeleteError") || 'حدث خطأ أثناء حذف ربط القسم بالطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    resetTagPrinterForm() {
      this.tagPrinterForm = {
        tagId: '',
        tagIds: [],
        printerId: ''
      };
      this.selectedTagPrinter = null;
    },
    async removeDefaultPrinter() {
      const ok = await this.$confirm({
        message: this.$t("confirmRemovePrinter"),
        variant: "warning",
      });
      if (!ok) {
        return;
      }

      try {
        const response = await fetch(`${PRINT_SERVER_URL}/config`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            windows_printer_name: null
          }),
        });

        if (response.ok) {
          const result = await response.json();
          this.$toast.success(this.$i18n.t("printerRemovedSuccess") || 'تم إزالة الطابعة المحددة بنجاح. سيتم استخدام الطابعة الافتراضية.', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          // Refresh server status to show updated config
          await this.checkServerHealth();
        } else {
          const errorData = await response.json().catch(() => ({}));
          this.$toast.error(errorData.message || this.$i18n.t("printerRemoveFailed") || 'فشلت إزالة الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error removing default printer:', error);
        this.$toast.error(this.$i18n.t("printerRemoveError") || 'حدث خطأ أثناء إزالة الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    }
  }
};
</script>

<style scoped>
.print-server-page-container {
  padding: 2rem;
  min-height: 100vh;
  background: var(--bg-primary);
}

.print-server-page-content {
  max-width: 1400px;
  margin: 0 auto;
}

.print-server-header-row {
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.print-server-overview {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

@media (max-width: 992px) {
  .print-server-overview {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 576px) {
  .print-server-overview {
    grid-template-columns: 1fr;
  }
}

.overview-stat-card {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 1rem 1.1rem;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  box-shadow: var(--shadow-sm);
}

.overview-stat-icon {
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.65rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 1.15rem;
  flex-shrink: 0;
}

.overview-stat-icon--success {
  background: rgba(16, 185, 129, 0.14);
  color: #059669;
}

.overview-stat-icon--primary {
  background: rgba(99, 102, 241, 0.14);
  color: #4f46e5;
}

.overview-stat-icon--info {
  background: rgba(59, 130, 246, 0.14);
  color: #2563eb;
}

.overview-stat-icon--warning {
  background: rgba(245, 158, 11, 0.16);
  color: #d97706;
}

.overview-stat-icon--danger {
  background: rgba(239, 68, 68, 0.14);
  color: #dc2626;
}

.server-offline-banner {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
  padding: 1rem 1.25rem;
  border-radius: var(--radius-lg);
  border: 1px solid rgba(245, 158, 11, 0.45);
  background: rgba(245, 158, 11, 0.08);
}

.server-offline-banner-main {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  flex: 1;
  min-width: 220px;
}

.server-offline-icon {
  font-size: 1.5rem;
  color: #d97706;
  flex-shrink: 0;
  margin-top: 0.15rem;
}

.server-offline-title {
  margin: 0 0 0.35rem;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text-primary);
}

.server-offline-message {
  margin: 0;
  font-size: 0.9rem;
  line-height: 1.5;
  color: var(--text-secondary);
}

.btn-install-guide {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.55rem 1rem;
  border-radius: var(--radius-md);
  border: 1px solid rgba(245, 158, 11, 0.5);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
}

.btn-install-guide:hover {
  background: rgba(245, 158, 11, 0.12);
}

.server-not-available-card--compact {
  border-width: 1px;
  margin-bottom: 1rem;
}

.server-not-available-card--compact .server-not-available-body {
  padding: 1.25rem 1.5rem;
}

.form-field-hint {
  margin: 0.4rem 0 0;
  font-size: 0.8rem;
  color: var(--text-muted);
  line-height: 1.4;
}

.overview-stat-value {
  font-size: 1.35rem;
  font-weight: 800;
  color: var(--text-primary);
  line-height: 1.2;
}

.overview-stat-label {
  font-size: 0.78rem;
  color: var(--text-secondary);
}

.print-server-section-card {
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 1rem;
  margin-bottom: 1.5rem;
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.print-server-section-header {
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-secondary);
}

.print-server-section-header--with-action {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.print-server-section-title-wrap {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.print-server-section-icon-wrap {
  width: 2.75rem;
  height: 2.75rem;
  border-radius: 0.7rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.16) 0%, rgba(79, 70, 229, 0.08) 100%);
  color: var(--primary-color);
  flex-shrink: 0;
}

.print-server-section-icon-wrap--tags {
  background: linear-gradient(135deg, rgba(245, 158, 11, 0.18) 0%, rgba(217, 119, 6, 0.08) 100%);
  color: #d97706;
}

.print-server-section-icon {
  font-size: 1.25rem;
}

.print-server-section-title {
  margin: 0 0 0.2rem;
  font-size: 1.2rem;
  font-weight: 700;
  color: var(--text-primary);
}

.print-server-section-subtitle {
  margin: 0;
  font-size: 0.82rem;
  color: var(--text-secondary);
}

.print-server-section-body {
  padding: 1.25rem 1.5rem 1.5rem;
}

.users-add-button--compact {
  padding: 0.5rem 1rem;
  font-size: 0.88rem;
}

.print-server-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1rem;
}

.print-server-cards-grid--compact {
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
}

.print-server-item-card {
  border: 1.5px solid var(--border-color);
  border-radius: 0.85rem;
  background: var(--bg-primary);
  display: flex;
  flex-direction: column;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.print-server-item-card:hover {
  border-color: rgba(99, 102, 241, 0.45);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.print-server-item-card-header {
  display: grid;
  grid-template-columns: 1fr auto;
  grid-template-rows: auto auto;
  gap: 0.5rem 0.65rem;
  padding: 0.9rem 1rem;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
}

.print-server-item-card-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  grid-column: 1;
  grid-row: 1;
}

.print-server-item-card-title h4 {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.print-server-item-card-icon {
  color: var(--primary-color);
  flex-shrink: 0;
}

.print-server-item-badges {
  grid-column: 1;
  grid-row: 2;
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}

.print-server-item-card-actions {
  grid-column: 2;
  grid-row: 1 / span 2;
  display: flex;
  align-items: flex-start;
  gap: 0.35rem;
}

.item-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 700;
}

.item-badge--main {
  background: rgba(245, 158, 11, 0.16);
  color: #b45309;
}

.item-badge--public {
  background: rgba(99, 102, 241, 0.14);
  color: #4f46e5;
}

.item-badge--inactive {
  background: rgba(239, 68, 68, 0.12);
  color: #dc2626;
}

.item-badge--online {
  background: rgba(16, 185, 129, 0.14);
  color: #059669;
}

.item-badge--offline {
  background: rgba(148, 163, 184, 0.2);
  color: var(--text-secondary);
}

.print-server-item-card-body {
  padding: 0.85rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  flex: 1;
}

.print-server-info-row {
  display: grid;
  grid-template-columns: auto auto 1fr;
  gap: 0.35rem 0.5rem;
  align-items: start;
  font-size: 0.82rem;
}

.print-server-info-row .info-icon {
  color: var(--text-secondary);
  margin-top: 0.1rem;
}

.print-server-info-row .info-label {
  color: var(--text-secondary);
  font-weight: 600;
  white-space: nowrap;
}

.print-server-info-row .info-value {
  color: var(--text-primary);
  font-weight: 500;
  word-break: break-word;
}

.print-server-item-card-footer {
  padding: 0.75rem 1rem 1rem;
  border-top: 1px solid var(--border-color);
}

.print-server-test-btn {
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  padding: 0.55rem 0.75rem;
  border: 1px solid rgba(99, 102, 241, 0.35);
  border-radius: 0.6rem;
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.12) 0%, rgba(79, 70, 229, 0.06) 100%);
  color: var(--primary-color);
  font-weight: 700;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.print-server-test-btn:hover:not(:disabled) {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.2) 0%, rgba(79, 70, 229, 0.12) 100%);
  transform: translateY(-1px);
}

.print-server-test-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.print-server-link-arrow {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.55rem 0.65rem;
  border-radius: 0.55rem;
  background: var(--bg-secondary);
  border: 1px dashed var(--border-color);
  font-size: 0.82rem;
  font-weight: 600;
}

.print-server-link-from {
  color: #d97706;
}

.print-server-link-to {
  color: var(--primary-color);
}

.print-server-link-arrow-icon {
  color: var(--text-secondary);
  flex-shrink: 0;
}

[dir="rtl"] .print-server-link-arrow-icon {
  transform: scaleX(-1);
}

@media (max-width: 768px) {
  .print-server-page-container {
    padding: 1rem;
  }

  .print-server-cards-grid,
  .print-server-cards-grid--compact {
    grid-template-columns: 1fr;
  }
}

/* Using users-page-container and users-page-content from main.css */
/* Using printers-management-card styles from below */

.users-page-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.users-header-section {
  margin-bottom: 0.25rem;
}

.header-title-wrapper {
  display: flex;
  align-items: center;
  gap: 0.9rem;
}

.header-icon-wrapper {
  width: 2.75rem;
  height: 2.75rem;
  border-radius: 0.8rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--primary-color) 16%, var(--bg-secondary));
  border: 1px solid color-mix(in srgb, var(--primary-color) 34%, var(--border-color));
  flex-shrink: 0;
}

.header-icon {
  font-size: 1.3rem;
  color: var(--primary-color);
}

.header-subtitle {
  margin: 0.2rem 0 0;
  color: var(--text-secondary);
  font-size: 0.92rem;
}

.spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.loading-state {
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
  color: var(--danger-color);
}

.error-message {
  max-width: 600px;
}

.error-message h4 {
  font-size: 1.25rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: var(--danger-color);
}

.error-message p {
  font-size: 1rem;
  color: var(--text-secondary);
  margin-bottom: 1.5rem;
}

.server-instructions {
  background: var(--warning-light);
  border: 2px solid var(--warning-color);
  border-radius: var(--radius-lg);
  padding: 1.5rem;
  margin-top: 1rem;
}

.server-instructions h5 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--warning-color);
  margin-bottom: 1rem;
}

.instructions-list {
  color: var(--text-secondary);
  margin: 1rem 0;
  padding: 0;
}

.instructions-list li {
  margin-bottom: 0.5rem;
  line-height: 1.6;
  color: var(--text-secondary);
}

.command-box {
  background: var(--bg-dark);
  color: var(--text-primary);
  padding: 1rem;
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

.command-text {
  font-family: 'Courier New', monospace;
  font-size: 0.875rem;
  flex: 1;
  word-break: break-all;
  color: var(--text-primary);
}

.btn-copy {
  background: var(--primary-color);
  color: white;
  border: none;
  padding: 0.5rem;
  border-radius: var(--radius-md);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--transition-base);
  flex-shrink: 0;
  box-shadow: var(--shadow-xs);
}

.btn-copy:hover {
  background: var(--primary-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-sm);
}

.alternative-method {
  margin-top: 1rem;
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.download-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;
  flex-wrap: wrap;
}

.btn-download {
  background: var(--success-color);
  color: white;
  box-shadow: var(--shadow-sm);
}

.btn-download:hover:not(:disabled) {
  background: var(--accent-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.btn-install {
  background: var(--primary-color);
  color: white;
  box-shadow: var(--shadow-sm);
}

.btn-install:hover:not(:disabled) {
  background: var(--primary-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.install-guide {
  margin-top: 1.5rem;
  padding: 1rem;
  background: var(--success-light);
  border: 2px solid var(--success-color);
  border-radius: var(--radius-md);
}

.install-guide h6 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--success-color);
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

[dir="rtl"] .server-not-available-header {
  direction: rtl;
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

[dir="rtl"] .server-not-available-message {
  direction: rtl;
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

[dir="rtl"] .btn-download-large {
  flex-direction: row-reverse;
}

[dir="ltr"] .btn-download-large {
  flex-direction: row;
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

[dir="rtl"] .instructions-title {
  flex-direction: row-reverse;
}

[dir="ltr"] .instructions-title {
  flex-direction: row;
}

.instructions-list-detailed {
  color: var(--text-secondary);
  margin: 1rem 0;
  padding: 0;
  line-height: 2;
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

[dir="rtl"] .btn-copy-large {
  flex-direction: row-reverse;
}

[dir="ltr"] .btn-copy-large {
  flex-direction: row;
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

.status-info {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.status-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  width: 100%;
}

[dir="rtl"] .status-item {
  flex-direction: row;
  justify-content: flex-end;
}

[dir="ltr"] .status-item {
  flex-direction: row;
  justify-content: flex-start;
}

.status-label {
  font-weight: 500;
  color: var(--text-secondary);
  min-width: 0;
}

[dir="rtl"] .status-label {
  text-align: right;
}

[dir="ltr"] .status-label {
  text-align: left;
}

.status-value {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--text-primary);
}

.btn-remove-printer-config {
  background: transparent;
  border: none;
  padding: 0.25rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--danger-color);
  transition: all var(--transition-base);
  border-radius: var(--radius-sm);
}

.btn-remove-printer-config:hover {
  background: var(--danger-light);
  color: var(--danger-color);
  transform: scale(1.1);
}

.btn-remove-printer-config .remove-icon {
  font-size: 1rem;
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

/* RTL Support - Using main.css styles */
[dir="rtl"] .printers-management-title {
  direction: rtl;
}

[dir="ltr"] .printers-management-title {
  direction: ltr;
}

/* Printers Management Styles */
.printers-management-card {
  background: var(--bg-primary);
  border-radius: var(--radius-lg);
  padding: 1.2rem;
  margin-bottom: 0;
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-color);
}

.printers-management-header {
  margin-bottom: 1rem;
  padding-bottom: 0.85rem;
  border-bottom: 1px solid var(--border-color);
}

.printers-management-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

[dir="rtl"] .printers-management-header-content {
  flex-direction: row;
}

[dir="ltr"] .printers-management-header-content {
  flex-direction: row;
}

.printers-management-title {
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  text-align: start;
}

[dir="rtl"] .printers-management-title {
  text-align: right;
}

/* Using users-add-button styles from main.css */

/* Using users-grid and user-card styles from main.css */

.printer-badges {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  margin-top: 0.5rem;
}

.printer-status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.5rem;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 600;
}

.printer-status-badge.online {
  background: rgba(255, 255, 255, 0.2);
  color: #ffffff;
  border: 1px solid rgba(255, 255, 255, 0.3);
}

.printer-status-badge.offline {
  background: rgba(239, 68, 68, 0.3);
  color: #ffffff;
  border: 1px solid rgba(239, 68, 68, 0.5);
}

.printer-status-badge .status-icon {
  font-size: 0.5rem;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.main-printer-badge {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
  border: 1px solid rgba(255, 255, 255, 0.4);
  padding: 0.25rem 0.5rem;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 600;
}

.form-help-text {
  font-size: 0.75rem;
  color: var(--text-secondary);
  font-style: italic;
  margin-right: 0.5rem;
}

.inactive-badge {
  background: rgba(239, 68, 68, 0.3);
  color: #ffffff;
  border: 1px solid rgba(239, 68, 68, 0.5);
  padding: 0.25rem 0.5rem;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 600;
}

.printer-card-actions {
  display: flex;
  gap: 0.5rem;
  position: absolute;
  top: 1rem;
  right: 1rem;
}

[dir="rtl"] .printer-card-actions {
  flex-direction: row-reverse;
  right: auto;
  left: 1rem;
}

[dir="ltr"] .printer-card-actions {
  flex-direction: row;
  left: auto;
  right: 1rem;
}

.btn-edit-printer,
.btn-delete-printer {
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: var(--radius-md);
  padding: 0.5rem;
  cursor: pointer;
  color: #ffffff;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-edit-printer:hover {
  background: rgba(255, 255, 255, 0.3);
  border-color: rgba(255, 255, 255, 0.5);
  transform: scale(1.05);
}

.btn-delete-printer:hover {
  background: rgba(239, 68, 68, 0.4);
  border-color: rgba(239, 68, 68, 0.6);
  transform: scale(1.05);
}

/* Using user-card-body, user-info-item, and user-card-footer styles from main.css */

/* Tag Printers Management Styles */
.tag-printers-management-card {
  background: var(--bg-primary);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  margin-bottom: 0;
  overflow: hidden;
  border: 1px solid var(--border-color);
}

.tag-printers-management-header {
  background: color-mix(in srgb, var(--primary-color) 10%, var(--bg-secondary));
  padding: 1rem 1.2rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  color: var(--text-primary);
  border-bottom: 1px solid var(--border-color);
}

[dir="rtl"] .tag-printers-management-header {
  flex-direction: row-reverse;
}

[dir="ltr"] .tag-printers-management-header {
  flex-direction: row;
}

.tag-printers-management-title {
  flex: 1;
  margin: 0;
  font-size: 1.12rem;
  font-weight: 700;
  color: var(--text-primary);
}

.btn-add-tag-printer {
  background: var(--bg-primary);
  color: var(--primary-color);
  border: 1px solid color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
  padding: 0.42rem 0.85rem;
  border-radius: var(--radius-md);
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: all var(--transition-base);
  font-weight: 500;
}

.btn-add-tag-printer:hover {
  background: color-mix(in srgb, var(--primary-color) 12%, var(--bg-primary));
  transform: translateY(-1px);
  box-shadow: var(--shadow-sm);
}

.tag-printers-management-body {
  padding: 1.5rem;
}

.tag-printers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

.tag-printer-card {
  border: 2px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: 1rem;
  transition: all var(--transition-base);
  background: var(--bg-tertiary);
}

.tag-printer-card:hover {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.tag-printer-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

[dir="rtl"] .tag-printer-card-header {
  flex-direction: row-reverse;
}

[dir="ltr"] .tag-printer-card-header {
  flex-direction: row;
}

.tag-printer-card-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
}

[dir="rtl"] .tag-printer-card-title {
  flex-direction: row-reverse;
}

[dir="ltr"] .tag-printer-card-title {
  flex-direction: row;
}

.tag-printer-card-title h4 {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
}

.tag-printer-card-icon {
  font-size: 1.25rem;
  color: var(--primary-color);
}

.tag-printer-card-actions {
  display: flex;
  gap: 0.5rem;
}

[dir="rtl"] .tag-printer-card-actions {
  flex-direction: row-reverse;
}

[dir="ltr"] .tag-printer-card-actions {
  flex-direction: row;
}

.btn-edit-tag-printer,
.btn-delete-tag-printer {
  background: transparent;
  border: 1px solid var(--border-color);
  padding: 0.375rem;
  border-radius: var(--radius-sm);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--transition-base);
  color: var(--text-secondary);
}

.btn-edit-tag-printer:hover {
  background: var(--primary-light);
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.btn-delete-tag-printer:hover {
  background: var(--danger-light);
  border-color: var(--danger-color);
  color: var(--danger-color);
}

.tag-printer-card-body {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.tag-printer-info-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
}

.tag-printer-info-item .info-icon {
  font-size: 1rem;
  color: var(--text-secondary);
}

.tag-printer-info-item .info-label {
  font-weight: 500;
  color: var(--text-secondary);
  min-width: 60px;
}

.tag-printer-info-item .info-value {
  color: var(--text-primary);
  font-weight: 500;
}

.btn-add-first-tag-printer {
  background: var(--primary-color);
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: var(--radius-md);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  transition: all var(--transition-base);
  font-weight: 500;
  margin-top: 1rem;
}

.btn-add-first-tag-printer:hover {
  background: var(--accent-dark);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.tag-printer-form {
  padding: 0.5rem 0;
}

.tag-printer-form .form-group {
  margin-bottom: 1.5rem;
}

.tag-printer-form label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: var(--text-primary);
}

.tag-printer-form .required {
  color: var(--danger-color);
}

.btn-add-first-printer {
  background: var(--primary-color);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  padding: 1rem 2rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  margin-top: 1rem;
  transition: all 0.3s ease;
}

.btn-add-first-printer:hover {
  background: var(--primary-dark);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  width: 100%;
  min-height: 140px;
  gap: 0.75rem;
}

.empty-state p {
  margin: 0;
  text-align: center;
}

.printer-form,
.print-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 600;
  color: var(--text-primary);
}

.required {
  color: var(--danger-color);
}

.form-control {
  padding: 0.75rem;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  color: var(--text-primary);
  font-size: 1rem;
}

.form-control:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(129, 140, 248, 0.1);
}

.tag-printer-multi-select {
  max-height: 220px;
  overflow-y: auto;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  padding: 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.tag-printer-multi-select.is-disabled {
  opacity: 0.6;
  pointer-events: none;
}

.tag-printer-multi-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.4rem 0.55rem;
  border-radius: var(--radius-sm);
  cursor: pointer;
  color: var(--text-primary);
  margin: 0;
  font-weight: 500;
}

.tag-printer-multi-option:hover {
  background: color-mix(in srgb, var(--primary-color) 10%, transparent);
}

.tag-printer-multi-option--sub {
  padding-inline-start: 1.35rem;
  font-weight: 400;
  color: var(--text-secondary);
}

.tag-printer-multi-empty,
.users-form-hint {
  margin: 0.35rem 0 0;
  font-size: 0.85rem;
  color: var(--text-secondary);
}

</style>


