<template>
  <div class="main-content-wrapper" :dir="direction">
    <AppHeader />
    <div class="users-page-container">
      <div class="users-page-content">
        <!-- Header Section -->
        <div class="users-header-section">
          <div class="users-header-content">
            <div class="header-title-wrapper">
              <div class="header-icon-wrapper">
                <b-icon icon="server" class="header-icon"></b-icon>
              </div>
              <div>
                <h1 class="users-page-title">{{ $t("printServerManagement") || "إدارة خادم الطباعة" }}</h1>
                <p class="header-subtitle">{{ $t("printServerManagementDescription") || "إدارة حالة الخادم والطابعات وربط الأقسام بالطابعات" }}</p>
              </div>
            </div>
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
        <div class="printers-management-card" v-if="serverStatus || loading">
          <div class="printers-management-header">
            <div class="printers-management-header-content">
              <h3 class="printers-management-title">
                {{ $t("serverStatus") || "حالة الخادم" }}
              </h3>
              <button 
                class="users-add-button" 
                @click="checkServerHealth"
                :disabled="loading"
              >
                <b-icon icon="arrow-clockwise" :class="{ 'spinning': loading }" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
              </button>
            </div>
          </div>
          <div class="printers-management-body">
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
            </div>
          </div>
        </div>

        <!-- Printers Management Card -->
        <div class="printers-management-card" v-if="serverStatus">
          <div class="printers-management-header">
            <div class="printers-management-header-content">
              <h3 class="printers-management-title">
                {{ $t("printersManagement") || "إدارة الطابعات" }}
              </h3>
              <button 
                class="users-add-button" 
                @click="showAddPrinterModal = true"
              >
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addPrinter") || "إضافة طابعة" }}</span>
              </button>
            </div>
          </div>
          <div class="printers-management-body">
            <div v-if="loadingPrinters" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="managedPrinters.length > 0" class="users-grid-container">
              <div class="users-grid">
                <div 
                  v-for="printer in managedPrinters" 
                  :key="printer.id"
                  class="user-card"
                >
                  <div class="user-card-header">
                    <div class="printer-card-actions">
                      <button 
                        class="btn-edit-printer"
                        @click="editPrinter(printer)"
                        :title="$t('edit') || 'تعديل'"
                      >
                        <b-icon icon="pencil"></b-icon>
                      </button>
                      <button 
                        class="btn-delete-printer"
                        @click="confirmDeletePrinter(printer)"
                        :title="$t('delete') || 'حذف'"
                      >
                        <b-icon icon="trash"></b-icon>
                      </button>
                    </div>
                    <div class="user-avatar">
                      <b-icon icon="printer-fill" class="avatar-icon"></b-icon>
                    </div>
                    <h3 class="user-name">{{ printer.name }}</h3>
                    <div class="printer-badges">
                      <span v-if="printer.isMain" class="main-printer-badge">
                        {{ $t("mainPrinter") || "رئيسية" }}
                      </span>
                      <span v-if="!printer.isActive" class="inactive-badge">
                        {{ $t("inactive") || "غير مفعل" }}
                      </span>
                      <span 
                        v-else-if="getPrinterStatus(printer.id).online" 
                        class="printer-status-badge online"
                        :title="$t('printerOnline') || 'الطابعة متصلة'"
                      >
                        <b-icon icon="circle-fill" class="status-icon"></b-icon>
                        {{ $t("online") || "أونلاين" }}
                      </span>
                      <span 
                        v-else 
                        class="printer-status-badge offline"
                        :title="getPrinterStatus(printer.id).error || ($t('printerOffline') || 'الطابعة غير متصلة')"
                      >
                        <b-icon icon="circle-fill" class="status-icon"></b-icon>
                        {{ $t("offline") || "أوفلاين" }}
                      </span>
                    </div>
                  </div>
                  <div class="user-card-body">
                    <div class="user-info-item">
                      <b-icon icon="printer-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("printerName") || "اسم الطابعة:" }}</span>
                      <span class="info-value">{{ printer.printerName }}</span>
                    </div>
                    <div class="user-info-item">
                      <b-icon icon="tag" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("type") || "النوع:" }}</span>
                      <span class="info-value">{{ printer.printerType }}</span>
                    </div>
                    <div class="user-info-item" v-if="printer.printCategory">
                      <b-icon icon="folder" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("category") || "الفئة:" }}</span>
                      <span class="info-value">{{ getCategoryLabel(printer.printCategory) }}</span>
                    </div>
                    <div class="user-info-item" v-if="printer.description">
                      <b-icon icon="file-text" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("description") || "الوصف:" }}</span>
                      <span class="info-value">{{ printer.description }}</span>
                    </div>
                  </div>
                  <div class="user-card-footer">
                    <button 
                      class="user-action-button user-edit-button"
                      @click="testPrintToPrinter(printer.id)"
                      :disabled="!printer.isActive || testingPrint"
                    >
                      <b-icon icon="printer-fill" class="action-icon"></b-icon>
                      <span>{{ $t("testPrint") || "اختبار الطباعة" }}</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="printer" class="empty-icon"></b-icon>
              <p>{{ $t("noPrintersConfigured") || "لم يتم إعداد أي طابعات" }}</p>
              <button 
                class="btn-add-first-printer"
                @click="showAddPrinterModal = true"
              >
                <b-icon icon="plus-circle" class="me-2"></b-icon>
                {{ $t("addFirstPrinter") || "إضافة أول طابعة" }}
              </button>
            </div>
          </div>
        </div>

        <!-- Tag Printers Management Card -->
        <div class="tag-printers-management-card" v-if="serverStatus">
          <div class="tag-printers-management-header">
            <b-icon icon="tags-fill" class="me-2"></b-icon>
            <h3 class="tag-printers-management-title">
              {{ $t("tagPrintersManagement") || "إدارة طباعة الأقسام" }}
            </h3>
            <button 
              class="btn-add-tag-printer" 
              @click="showAddTagPrinterModal = true"
            >
              <b-icon icon="plus-circle" class="me-1"></b-icon>
              {{ $t("addTagPrinter") || "إضافة ربط قسم بطابعة" }}
            </button>
          </div>
          <div class="tag-printers-management-body">
            <div v-if="loadingTagPrinters" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="tagPrinters.length > 0" class="tag-printers-grid">
              <div 
                v-for="tagPrinter in tagPrinters" 
                :key="tagPrinter.id"
                class="tag-printer-card"
              >
                <div class="tag-printer-card-header">
                  <div class="tag-printer-card-title">
                    <b-icon icon="tag" class="tag-printer-card-icon"></b-icon>
                    <h4>{{ tagPrinter.tag?.name || 'قسم غير محدد' }}</h4>
                  </div>
                  <div class="tag-printer-card-actions">
                    <button 
                      class="btn-edit-tag-printer"
                      @click="editTagPrinter(tagPrinter)"
                      :title="$t('edit') || 'تعديل'"
                    >
                      <b-icon icon="pencil"></b-icon>
                    </button>
                    <button 
                      class="btn-delete-tag-printer"
                      @click="confirmDeleteTagPrinter(tagPrinter)"
                      :title="$t('delete') || 'حذف'"
                    >
                      <b-icon icon="trash"></b-icon>
                    </button>
                  </div>
                </div>
                <div class="tag-printer-card-body">
                  <div class="tag-printer-info-item">
                    <b-icon icon="tag" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("tag") || "القسم:" }}</span>
                    <span class="info-value">{{ tagPrinter.tag?.name || 'N/A' }}</span>
                  </div>
                  <div class="tag-printer-info-item">
                    <b-icon icon="printer" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("printer") || "الطابعة:" }}</span>
                    <span class="info-value">{{ tagPrinter.printer?.name || 'N/A' }}</span>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="tags" class="empty-icon"></b-icon>
              <p>{{ $t("noTagPrintersConfigured") || "لم يتم إعداد أي ربط بين الأقسام والطابعات" }}</p>
              <button 
                class="btn-add-first-tag-printer"
                @click="showAddTagPrinterModal = true"
              >
                <b-icon icon="plus-circle" class="me-2"></b-icon>
                {{ $t("addFirstTagPrinter") || "إضافة أول ربط" }}
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
                {{ $t("mainCategory") || $t("tag") || "القسم الرئيسي" }}
                <span class="required">*</span>
              </label>
              <select
                v-model="tagPrinterForm.tagId"
                class="users-form-select"
                :disabled="loadingTags"
                required
              >
            <option value="">{{ $t("selectMainCategory") || $t("selectTag") || "اختر القسم الرئيسي" }}</option>
            <option
              v-for="tag in rootTagsForSelect"
              :key="tag.id ?? tag.Id"
              :value="tag.id ?? tag.Id"
            >
              {{ tag.name ?? tag.Name }}
            </option>
              </select>
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
              :disabled="!tagPrinterForm.tagId || !tagPrinterForm.printerId || savingTagPrinter"
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
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="check-circle-fill" class="form-label-icon"></b-icon>
                {{ $t("active") || "مفعل" }}
              </label>
              <div class="users-form-checkbox">
                <input 
                  type="checkbox" 
                  v-model="printerForm.isActive"
                  id="printer-active"
                  class="users-form-checkbox-input"
                />
                <label for="printer-active" class="users-form-checkbox-label">
                  {{ $t("active") || "مفعل" }}
                </label>
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="star-fill" class="form-label-icon"></b-icon>
                {{ $t("mainPrinter") || "طابعة رئيسية" }}
              </label>
              <div class="users-form-checkbox">
                <input 
                  type="checkbox" 
                  v-model="printerForm.isMain"
                  id="printer-main"
                  class="users-form-checkbox-input"
                />
                <label for="printer-main" class="users-form-checkbox-label">
                  {{ $t("mainPrinter") || "طابعة رئيسية" }}
                  <small class="form-help-text">(تطبع كل الفواتير)</small>
                </label>
              </div>
            </div>
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
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="check-circle-fill" class="form-label-icon"></b-icon>
                {{ $t("active") || "مفعل" }}
              </label>
              <div class="users-form-checkbox">
                <input 
                  type="checkbox" 
                  v-model="printerForm.isActive"
                  id="edit-printer-active"
                  class="users-form-checkbox-input"
                />
                <label for="edit-printer-active" class="users-form-checkbox-label">
                  {{ $t("active") || "مفعل" }}
                </label>
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="star-fill" class="form-label-icon"></b-icon>
                {{ $t("mainPrinter") || "طابعة رئيسية" }}
              </label>
              <div class="users-form-checkbox">
                <input 
                  type="checkbox" 
                  v-model="printerForm.isMain"
                  id="edit-printer-main"
                  class="users-form-checkbox-input"
                />
                <label for="edit-printer-main" class="users-form-checkbox-label">
                  {{ $t("mainPrinter") || "طابعة رئيسية" }}
                  <small class="form-help-text">(تطبع كل الفواتير)</small>
                </label>
              </div>
            </div>
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

    <!-- Delete Printer Modal -->
    <b-modal
      v-model="showDeletePrinterModal"
      hide-header
      hide-footer
      class="users-modal"
      centered
    >
      <div class="modal-content-wrapper">
        <div class="delete-confirmation-content">
          <div class="delete-icon-wrapper">
            <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
          </div>
          <h3 class="delete-confirmation-title">{{ $t("confirm_delete") || "تأكيد الحذف" }}</h3>
          <p class="delete-confirmation-text">{{ deletePrinterMessage }}</p>
          <div class="delete-confirmation-actions">
            <button type="button" class="delete-confirm-button" @click="executeDeletePrinter">
              <b-icon icon="check-circle-fill" class="me-2"></b-icon>
              {{ $t("delete") || "حذف" }}
            </button>
            <button type="button" class="delete-cancel-button" @click="showDeletePrinterModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </div>
      </div>
    </b-modal>

    <!-- Delete Tag Printer Modal -->
    <b-modal
      v-model="showDeleteTagPrinterModal"
      hide-header
      hide-footer
      class="users-modal"
      centered
    >
      <div class="modal-content-wrapper">
        <div class="delete-confirmation-content">
          <div class="delete-icon-wrapper">
            <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
          </div>
          <h3 class="delete-confirmation-title">{{ $t("confirm_delete") || "تأكيد الحذف" }}</h3>
          <p class="delete-confirmation-text">{{ deleteTagPrinterMessage }}</p>
          <div class="delete-confirmation-actions">
            <button type="button" class="delete-confirm-button" @click="executeDeleteTagPrinter">
              <b-icon icon="check-circle-fill" class="me-2"></b-icon>
              {{ $t("delete") || "حذف" }}
            </button>
            <button type="button" class="delete-cancel-button" @click="showDeleteTagPrinterModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </div>
      </div>
    </b-modal>

  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../http/api.js";
import { rootTags } from "@/utils/tagHierarchy.js";

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
      showDeletePrinterModal: false,
      printerToDelete: null,
      showDeleteTagPrinterModal: false,
      tagPrinterToDelete: null,
      savingPrinter: false,
      savingTagPrinter: false,
      tagPrinterForm: {
        tagId: '',
        printerId: ''
      },
      printerForm: {
        name: '',
        description: '',
        printerName: '',
        printerType: 'windows',
        printCategory: '',
        isActive: true,
        isMain: false
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
        { value: 'CustomerOrder', label: 'طلبات الزبائن' },
        { value: 'Report', label: 'تقارير' },
        { value: 'Other', label: 'أخرى' }
      ]
    };
  },
  computed: {
    direction() {
      return this.$i18n.locale === "ar" ? "rtl" : "ltr";
    },
    /** أقسام رئيسية فقط (بدون الأقسام الفرعية) لربط الطابعات */
    rootTagsForSelect() {
      return rootTags(this.tags);
    },
    deletePrinterMessage() {
      if (!this.printerToDelete) return "";
      const name = this.printerToDelete.name || "";
      return (
        this.$t("confirmDeletePrinter") ||
        `هل أنت متأكد من حذف الطابعة "${name}"؟`
      );
    },
    deleteTagPrinterMessage() {
      if (!this.tagPrinterToDelete) return "";
      const tagName =
        this.tagPrinterToDelete.tag?.name ||
        this.tagPrinterToDelete.Tag?.name ||
        "";
      const printerName =
        this.tagPrinterToDelete.printer?.name ||
        this.tagPrinterToDelete.Printer?.name ||
        "";
      return (
        this.$t("confirmDeleteTagPrinter") ||
        `هل أنت متأكد من حذف ربط القسم "${tagName}" بالطابعة "${printerName}"؟`
      );
    },
  },
  mounted() {
    this.checkServerHealth();
    this.loadManagedPrinters();
    this.loadPrinters();
    this.loadTagPrinters();
    this.loadTags();
    this.startStatusPolling();
  },
  beforeDestroy() {
    this.stopStatusPolling();
  },
  methods: {
    async checkServerHealth() {
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
          // Get current default printer from config
          if (this.serverStatus.config) {
            this.currentDefaultPrinter = this.serverStatus.config.windows_printer_name || null;
          }
          this.$toast.success(this.$i18n.t("serverStatusUpdated") || 'تم تحديث حالة الخادم', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.serverStatus = null;
          this.$toast.error(this.$i18n.t("serverNotAvailable") || 'الخادم غير متاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error checking server health:', error);
        this.serverStatus = null;
        this.$toast.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بالخادم', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.loading = false;
      }
    },
    async loadPrinters() {
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
          this.$toast.error(this.$i18n.t("failedToLoadPrinters") || 'فشل تحميل الطابعات', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error loading printers:', error);
        this.printers = [];
        this.$toast.error(this.$i18n.t("serverConnectionError") || 'خطأ في الاتصال بالخادم', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
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
      // Check status every 5 seconds
      this.statusCheckInterval = setInterval(() => {
        if (this.managedPrinters.length > 0) {
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
        isMain: printer.isMain || false
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
    confirmDeletePrinter(printer) {
      this.printerToDelete = printer;
      this.showDeletePrinterModal = true;
    },
    async executeDeletePrinter() {
      if (!this.printerToDelete) return;
      const id = this.printerToDelete.id;
      this.showDeletePrinterModal = false;
      this.printerToDelete = null;
      await this.deletePrinter(id);
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
        isMain: false
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
    async saveTagPrinter() {
      if (!this.tagPrinterForm.tagId || !this.tagPrinterForm.printerId) {
        this.$toast.warning(this.$i18n.t("pleaseFillAllFields") || 'يرجى ملء جميع الحقول المطلوبة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }

      try {
        this.savingTagPrinter = true;
        let response;
        if (this.selectedTagPrinter) {
          // Update existing
          response = await HTTP.put(`TagPrinters/${this.selectedTagPrinter.id}`, {
            tagId: parseInt(this.tagPrinterForm.tagId),
            printerId: parseInt(this.tagPrinterForm.printerId)
          });
        } else {
          // Add new
          response = await HTTP.post('TagPrinters', {
            tagId: parseInt(this.tagPrinterForm.tagId),
            printerId: parseInt(this.tagPrinterForm.printerId)
          });
        }

        if (response.data && !response.data.errorStatus) {
          this.$toast.success(
            this.selectedTagPrinter 
              ? (this.$i18n.t("tagPrinterUpdatedSuccess") || 'تم تحديث ربط القسم بالطابعة بنجاح')
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
          this.$toast.error(response.data?.message || this.$i18n.t("tagPrinterSaveFailed") || 'فشل حفظ ربط القسم بالطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
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
        printerId: tagPrinter.printerId
      };
      this.showAddTagPrinterModal = true;
    },
    confirmDeleteTagPrinter(tagPrinter) {
      this.tagPrinterToDelete = tagPrinter;
      this.showDeleteTagPrinterModal = true;
    },
    async executeDeleteTagPrinter() {
      if (!this.tagPrinterToDelete) return;
      const id = this.tagPrinterToDelete.id;
      this.showDeleteTagPrinterModal = false;
      this.tagPrinterToDelete = null;
      await this.deleteTagPrinter(id);
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
        printerId: ''
      };
      this.selectedTagPrinter = null;
    },
    async removeDefaultPrinter() {
      if (!confirm(this.$i18n.t("confirmRemovePrinter") || 'هل أنت متأكد من إزالة الطابعة المحددة؟ سيتم استخدام الطابعة الافتراضية للنظام.')) {
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

</style>


