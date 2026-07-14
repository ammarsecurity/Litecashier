<template>
  <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <!-- Header Section -->
          <div class="users-header-section">
            <div class="users-header-content">
              <h1 class="users-page-title">{{ $t("printServerManagement") || "إدارة خادم الطباعة" }}</h1>
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

          <!-- Server Status Alert -->
          <div v-if="!serverStatus && !loading" class="server-alert-card">
            <div class="server-alert-header">
              <b-icon icon="exclamation-triangle-fill" class="alert-icon"></b-icon>
              <h2 class="alert-title">{{ $t("serverNotAvailable") || "خادم الطباعة غير متصل" }}</h2>
            </div>
            <div class="server-alert-body">
              <p class="alert-message">
                {{ $t("serverNotAvailableMessage") || "يمكنك إعداد الطابعات أدناه. لتفعيل الطباعة الفعلية، شغّل Print Server على هذا الجهاز." }}
              </p>
              <button
                class="users-add-button"
                type="button"
                @click="showInstallInstructions"
              >
                <b-icon icon="info-circle" class="button-icon"></b-icon>
                <span class="button-text">
                  {{ showInstallGuide ? ($t("hideInstallGuide") || "إخفاء التعليمات") : ($t("showInstallGuide") || "تعليمات التشغيل") }}
                </span>
              </button>

              <!-- Install Instructions Section -->
              <div v-if="showInstallGuide" class="install-instructions-section">
                <h4 class="instructions-title">
                  <b-icon icon="info-circle-fill" class="me-2"></b-icon>
                  {{ $t("installInstructions") || "تعليمات تشغيل Print Server (C#)" }}
                </h4>
                <ol class="instructions-list-detailed">
                  <li>
                    <strong>{{ $t("installStep1") || "الخطوة 1:" }}</strong>
                    {{ $t("installStep1Desc") }}
                  </li>
                  <li>
                    <strong>{{ $t("installStep2") || "الخطوة 2:" }}</strong>
                    {{ $t("installStep2Desc") }}
                  </li>
                  <li>
                    <strong>{{ $t("installStep3") || "الخطوة 3:" }}</strong>
                    {{ $t("installStep3Desc") }}
                  </li>
                  <li>
                    <strong>{{ $t("installStep4") || "الخطوة 4:" }}</strong>
                    {{ $t("installStep4Desc") }}
                  </li>
                  <li>
                    <strong>{{ $t("installStep5") || "الخطوة 5:" }}</strong>
                    {{ $t("installStep5Desc") }}
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
                    {{ $t("commandHelp") }}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- Server Status Card -->
          <div v-if="serverStatus || loading" class="status-card">
            <div class="status-card-header">
              <h3 class="status-card-title">
                <b-icon icon="activity" class="me-2"></b-icon>
                {{ $t("serverStatus") || "حالة الخادم" }}
              </h3>
            </div>
            <div class="status-card-body">
              <div v-if="loading" class="loading-state">
                <b-spinner small></b-spinner>
                <span>{{ $t("checking") || "جاري الفحص..." }}</span>
              </div>
              <div v-else-if="serverStatus" class="status-info">
                <div class="status-badge-large" :class="serverStatus.status === 'ok' ? 'status-success' : 'status-error'">
                  <b-icon 
                    :icon="serverStatus.status === 'ok' ? 'check-circle-fill' : 'x-circle-fill'"
                    class="status-icon-large"
                  ></b-icon>
                  <span class="status-text">
                    {{ serverStatus.status === 'ok' ? ($t("online") || "متصل") : ($t("offline") || "غير متصل") }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Printers Management Section -->
          <div class="printers-section">
            <!-- Section Header -->
            <div class="users-header-section">
              <div class="users-header-content">
                <h2 class="section-title">{{ $t("printersManagement") || "إدارة الطابعات" }}</h2>
                <button 
                  class="users-add-button" 
                  @click="showAddPrinterModal = true"
                >
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addPrinter") || "إضافة طابعة" }}</span>
                </button>
              </div>
            </div>

            <!-- Printers Grid -->
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
                    <div class="user-card-actions">
                      <button 
                        class="card-action-btn edit-btn"
                        @click="editPrinter(printer)"
                        :title="$t('edit') || 'تعديل'"
                      >
                        <b-icon icon="pencil-square"></b-icon>
                      </button>
                      <button 
                        class="card-action-btn delete-btn"
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
                      <span v-if="printer.isMain" class="badge badge-main">
                        {{ $t("mainPrinter") || "رئيسية" }}
                      </span>
                      <span v-if="printer.isPublicOrderPrinter" class="badge badge-public">
                        {{ $t("publicOrderPrinter") || "طلبات عامة" }}
                      </span>
                      <span v-if="!printer.isActive" class="badge badge-inactive">
                        {{ $t("inactive") || "غير مفعل" }}
                      </span>
                      <span 
                        v-else-if="getPrinterStatus(printer.id).online" 
                        class="badge badge-online"
                      >
                        <b-icon icon="circle-fill" class="badge-icon"></b-icon>
                        {{ $t("online") || "أونلاين" }}
                      </span>
                      <span 
                        v-else 
                        class="badge badge-offline"
                      >
                        <b-icon icon="circle-fill" class="badge-icon"></b-icon>
                        {{ $t("offline") || "أوفلاين" }}
                      </span>
                    </div>
                  </div>
                  <div class="user-card-body">
                    <div class="user-info-item">
                      <b-icon icon="printer" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("printerName") || "اسم الطابعة" }}:</span>
                      <span class="info-value">{{ printer.printerName || '---' }}</span>
                    </div>
                    <div class="user-info-item">
                      <b-icon icon="tag" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("type") || "النوع" }}:</span>
                      <span class="info-value">{{ printer.printerType || '---' }}</span>
                    </div>
                    <div class="user-info-item" v-if="printer.printCategory">
                      <b-icon icon="tags-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("printCategory") || "فئة الطباعة" }}:</span>
                      <span class="info-value">{{ getCategoryLabel(printer.printCategory) }}</span>
                    </div>
                    <div class="user-info-item" v-if="printer.description">
                      <b-icon icon="file-text" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("description") || "الوصف" }}:</span>
                      <span class="info-value">{{ printer.description }}</span>
                    </div>
                  </div>
                  <div class="user-card-footer">
                    <button 
                      class="user-action-button user-test-button" 
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
              <p class="empty-text">{{ $t("noPrinters") || "لا توجد طابعات" }}</p>
              <button 
                class="users-add-button" 
                @click="showAddPrinterModal = true"
              >
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addFirstPrinter") || "إضافة أول طابعة" }}</span>
              </button>
            </div>
          </div>

          <!-- Tag Printers Management Section -->
          <div class="tag-printers-section">
            <div class="users-header-section">
              <div class="users-header-content">
                <h2 class="section-title">{{ $t("tagPrintersManagement") || "إدارة طباعة الأقسام" }}</h2>
                <button 
                  class="users-add-button" 
                  @click="showAddTagPrinterModal = true"
                >
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addTagPrinter") || "إضافة ربط قسم بطابعة" }}</span>
                </button>
              </div>
            </div>

            <div v-if="loadingTagPrinters" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="tagPrinters.length > 0" class="users-grid-container">
              <div class="users-grid">
                <div 
                  v-for="tagPrinter in tagPrinters" 
                  :key="tagPrinter.id"
                  class="user-card"
                >
                  <div class="user-card-header">
                    <div class="user-card-actions">
                      <button 
                        class="card-action-btn edit-btn"
                        @click="editTagPrinter(tagPrinter)"
                        :title="$t('edit') || 'تعديل'"
                      >
                        <b-icon icon="pencil-square"></b-icon>
                      </button>
                      <button 
                        class="card-action-btn delete-btn"
                        @click="confirmDeleteTagPrinter(tagPrinter)"
                        :title="$t('delete') || 'حذف'"
                      >
                        <b-icon icon="trash"></b-icon>
                      </button>
                    </div>
                    <div class="user-avatar">
                      <b-icon icon="tags-fill" class="avatar-icon"></b-icon>
                    </div>
                    <h3 class="user-name">{{ tagPrinter.tag?.name || 'قسم غير محدد' }}</h3>
                  </div>
                  <div class="user-card-body">
                    <div class="user-info-item">
                      <b-icon icon="printer" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("printer") || "الطابعة" }}:</span>
                      <span class="info-value">{{ tagPrinter.printer?.name || '---' }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="tags" class="empty-icon"></b-icon>
              <p class="empty-text">{{ $t("noTagPrinters") || "لا توجد ربطات أقسام" }}</p>
              <button 
                class="users-add-button" 
                @click="showAddTagPrinterModal = true"
              >
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addFirstTagPrinter") || "إضافة أول ربط" }}</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Printer Modal -->
    <b-modal 
      id="modal-addPrinter" 
      :title="$t('addPrinter')" 
      hide-header 
      hide-footer 
      class="users-modal"
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
          <div class="form-toggle-cards">
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
                  <span class="form-toggle-card-desc">{{ $t("mainPrinterHint") || "تطبع كل الفواتير والإيصالات" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card form-toggle-card--accent-info"
              :class="{ 'form-toggle-card--on': printerForm.isPublicOrderPrinter }"
            >
              <input v-model="printerForm.isPublicOrderPrinter" type="checkbox" class="form-toggle-card-input" />
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
            <button type="submit" class="users-form-button users-form-button-primary" :disabled="savingPrinter">
              <b-spinner v-if="savingPrinter" small class="me-2"></b-spinner>
              {{ savingPrinter ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-button users-form-button-secondary" @click="$bvModal.hide('modal-addPrinter')">
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
          <div class="form-toggle-cards">
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
                  <span class="form-toggle-card-desc">{{ $t("mainPrinterHint") || "تطبع كل الفواتير والإيصالات" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card form-toggle-card--accent-info"
              :class="{ 'form-toggle-card--on': printerForm.isPublicOrderPrinter }"
            >
              <input v-model="printerForm.isPublicOrderPrinter" type="checkbox" class="form-toggle-card-input" />
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
            <button type="submit" class="users-form-button users-form-button-primary" :disabled="savingPrinter">
              <b-spinner v-if="savingPrinter" small class="me-2"></b-spinner>
              {{ savingPrinter ? ($t("saving") || "جاري الحفظ...") : ($t("update") || "تحديث") }}
            </button>
            <button type="button" class="users-form-button users-form-button-secondary" @click="$bvModal.hide('modal-editPrinter')">
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Add Tag Printer Modal -->
    <b-modal 
      id="modal-addTagPrinter" 
      :title="$t('addTagPrinter')" 
      hide-header 
      hide-footer 
      class="users-modal"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addTagPrinter") || "إضافة ربط قسم بطابعة" }}</h2>
        <form @submit.prevent="saveTagPrinter" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
              {{ $t("category") || "القسم" }} <span class="required">*</span>
            </label>
            <select 
              v-model="tagPrinterForm.tagId" 
              class="users-form-select"
              required
            >
              <option value="">{{ $t("selectCategory") || "اختر القسم" }}</option>
              <option v-for="tag in tags" :key="tag.id" :value="tag.id">
                {{ tag.name }}
              </option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="printer" class="form-label-icon"></b-icon>
              {{ $t("printer") || "الطابعة" }} <span class="required">*</span>
            </label>
            <select 
              v-model="tagPrinterForm.printerId" 
              class="users-form-select"
              required
            >
              <option value="">{{ $t("selectPrinter") || "اختر الطابعة" }}</option>
              <option v-for="printer in managedPrinters" :key="printer.id" :value="printer.id">
                {{ printer.name }}
              </option>
            </select>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-button users-form-button-primary" :disabled="savingTagPrinter">
              <b-spinner v-if="savingTagPrinter" small class="me-2"></b-spinner>
              {{ savingTagPrinter ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-button users-form-button-secondary" @click="$bvModal.hide('modal-addTagPrinter')">
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Edit Tag Printer Modal -->
    <b-modal 
      id="modal-editTagPrinter" 
      :title="$t('editTagPrinter')" 
      hide-header 
      hide-footer 
      class="users-modal"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("editTagPrinter") || "تعديل ربط قسم بطابعة" }}</h2>
        <form @submit.prevent="updateTagPrinter" class="users-form">
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
              {{ $t("category") || "القسم" }} <span class="required">*</span>
            </label>
            <select 
              v-model="tagPrinterForm.tagId" 
              class="users-form-select"
              required
            >
              <option value="">{{ $t("selectCategory") || "اختر القسم" }}</option>
              <option v-for="tag in tags" :key="tag.id" :value="tag.id">
                {{ tag.name }}
              </option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="printer" class="form-label-icon"></b-icon>
              {{ $t("printer") || "الطابعة" }} <span class="required">*</span>
            </label>
            <select 
              v-model="tagPrinterForm.printerId" 
              class="users-form-select"
              required
            >
              <option value="">{{ $t("selectPrinter") || "اختر الطابعة" }}</option>
              <option v-for="printer in managedPrinters" :key="printer.id" :value="printer.id">
                {{ printer.name }}
              </option>
            </select>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-button users-form-button-primary" :disabled="savingTagPrinter">
              <b-spinner v-if="savingTagPrinter" small class="me-2"></b-spinner>
              {{ savingTagPrinter ? ($t("saving") || "جاري الحفظ...") : ($t("update") || "تحديث") }}
            </button>
            <button type="button" class="users-form-button users-form-button-secondary" @click="$bvModal.hide('modal-editTagPrinter')">
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
            <button type="submit" class="users-form-button users-form-button-primary" :disabled="testingPrint">
              <b-spinner v-if="testingPrint" small class="me-2"></b-spinner>
              {{ testingPrint ? ($t("printing") || "جاري الطباعة...") : ($t("print") || "طباعة") }}
            </button>
            <button type="button" class="users-form-button users-form-button-secondary" @click="$bvModal.hide('modal-print')">
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
      tagPrinters: [],
      loadingTagPrinters: false,
      tags: [],
      loadingTags: false,
      showAddTagPrinterModal: false,
      selectedTagPrinter: null,
      savingPrinter: false,
      savingTagPrinter: false,
      testingPrint: false,
      testContent: 'اختبار الطباعة\nهذا نص تجريبي للتحقق من عمل الطابعة بشكل صحيح.',
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
        isMain: false,
        isPublicOrderPrinter: false
      },
      printForm: {
        printerId: null,
        htmlContent: '',
        copies: 1
      },
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
    printServerManualCommand() {
      return "cd restaurant_back\\PrintServer && start_print_server.bat";
    },
  },
  mounted() {
    this.checkServerHealth(true);
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
    async checkServerHealth(silent = false) {
      this.loading = true;
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/health`);
        if (response.ok) {
          this.serverStatus = await response.json();
          if (!silent) {
            this.$toast.success(this.$i18n.t("serverStatusUpdated") || 'تم تحديث حالة الخادم', {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            });
          }
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
      try {
        const response = await fetch(`${PRINT_SERVER_URL}/printers`);
        if (response.ok) {
          this.printers = await response.json();
        }
      } catch (error) {
        console.error('Error loading printers:', error);
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
        const response = await HTTP.post('Printers', this.printerForm);
        if (response.data && !response.data.errorStatus) {
          this.$bvModal.hide('modal-addPrinter');
          this.resetPrinterForm();
          await this.loadManagedPrinters();
          this.$toast.success(this.$i18n.t("printerAdded") || 'تمت إضافة الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("errorAddingPrinter") || 'حدث خطأ أثناء إضافة الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error saving printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("errorAddingPrinter") || 'حدث خطأ أثناء إضافة الطابعة', {
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
        const response = await HTTP.put(`Printers/${this.selectedPrinter.id}`, this.printerForm);
        if (response.data && !response.data.errorStatus) {
          this.$bvModal.hide('modal-editPrinter');
          this.resetPrinterForm();
          await this.loadManagedPrinters();
          this.$toast.success(this.$i18n.t("printerUpdated") || 'تم تحديث الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("errorUpdatingPrinter") || 'حدث خطأ أثناء تحديث الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error updating printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("errorUpdatingPrinter") || 'حدث خطأ أثناء تحديث الطابعة', {
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
          this.$toast.success(this.$i18n.t("printerDeleted") || 'تم حذف الطابعة بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("errorDeletingPrinter") || 'حدث خطأ أثناء حذف الطابعة', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("errorDeletingPrinter") || 'حدث خطأ أثناء حذف الطابعة', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    async saveTagPrinter() {
      this.savingTagPrinter = true;
      try {
        const response = await HTTP.post('TagPrinters', {
          tagId: parseInt(this.tagPrinterForm.tagId),
          printerId: parseInt(this.tagPrinterForm.printerId)
        });
        if (response.data && !response.data.errorStatus) {
          this.$bvModal.hide('modal-addTagPrinter');
          this.resetTagPrinterForm();
          await this.loadTagPrinters();
          this.$toast.success(this.$i18n.t("tagPrinterAdded") || 'تمت إضافة الربط بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("errorAddingTagPrinter") || 'حدث خطأ أثناء إضافة الربط', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error saving tag printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("errorAddingTagPrinter") || 'حدث خطأ أثناء إضافة الربط', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingTagPrinter = false;
      }
    },
    async updateTagPrinter() {
      this.savingTagPrinter = true;
      try {
        const response = await HTTP.put(`TagPrinters/${this.selectedTagPrinter.id}`, {
          tagId: parseInt(this.tagPrinterForm.tagId),
          printerId: parseInt(this.tagPrinterForm.printerId)
        });
        if (response.data && !response.data.errorStatus) {
          this.$bvModal.hide('modal-editTagPrinter');
          this.resetTagPrinterForm();
          await this.loadTagPrinters();
          this.$toast.success(this.$i18n.t("tagPrinterUpdated") || 'تم تحديث الربط بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("errorUpdatingTagPrinter") || 'حدث خطأ أثناء تحديث الربط', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error updating tag printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("errorUpdatingTagPrinter") || 'حدث خطأ أثناء تحديث الربط', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingTagPrinter = false;
      }
    },
    async deleteTagPrinter(tagPrinterId) {
      try {
        const response = await HTTP.delete(`TagPrinters/${tagPrinterId}`);
        if (response.data && !response.data.errorStatus) {
          await this.loadTagPrinters();
          this.$toast.success(this.$i18n.t("tagPrinterDeleted") || 'تم حذف الربط بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("errorDeletingTagPrinter") || 'حدث خطأ أثناء حذف الربط', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting tag printer:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("errorDeletingTagPrinter") || 'حدث خطأ أثناء حذف الربط', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    editPrinter(printer) {
      this.selectedPrinter = printer;
      this.printerForm = {
        name: printer.name,
        description: printer.description || '',
        printerName: printer.printerName,
        printerType: printer.printerType || 'windows',
        printCategory: printer.printCategory || '',
        isActive: printer.isActive,
        isMain: printer.isMain,
        isPublicOrderPrinter: printer.isPublicOrderPrinter || false
      };
      this.$bvModal.show('modal-editPrinter');
    },
    editTagPrinter(tagPrinter) {
      this.selectedTagPrinter = tagPrinter;
      this.tagPrinterForm = {
        tagId: tagPrinter.tagId,
        printerId: tagPrinter.printerId
      };
      this.$bvModal.show('modal-editTagPrinter');
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
    async confirmDeleteTagPrinter(tagPrinter) {
      const ok = await this.$confirm({
        title: this.$t("confirmDelete"),
        message: this.$t("confirmDeleteTagPrinter"),
      });
      if (ok) {
        this.deleteTagPrinter(tagPrinter.id);
      }
    },
    testPrintToPrinter(printerId) {
      this.printForm.printerId = printerId;
      const testContent = this.testContent || 'اختبار الطباعة\nهذا نص تجريبي للتحقق من عمل الطابعة بشكل صحيح.';
      this.printForm.htmlContent = `<div style="text-align: center; padding: 20px; direction: rtl;">
        <h2>اختبار الطباعة</h2>
        <p>${testContent.replace(/\n/g, '<br>')}</p>
        <p style="margin-top: 20px;">تاريخ: ${new Date().toLocaleDateString('ar-EG')}</p>
        <p>الوقت: ${new Date().toLocaleTimeString('ar-EG')}</p>
      </div>`;
      this.printForm.copies = 1;
      this.$bvModal.show('modal-print');
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
          this.$toast.success(this.$i18n.t("printSentSuccessfully") || `تم إرسال أمر الطباعة بنجاح (${this.printForm.copies} نسخة)`, {
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
          
          this.$toast.success(this.$i18n.t("downloadStarted") || 'تم بدء التحميل', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          
          setTimeout(() => {
            this.showInstallGuide = true;
          }, 1000);
        } else {
          this.$toast.warning(this.$i18n.t("serverNotAvailableForDownload") || 'الخادم غير متاح. يرجى تحميل الملفات يدوياً من مجلد restaurant_back/PrintServer', {
            position: "top-right",
            timeout: 4000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.showInstallGuide = true;
        }
      } catch (error) {
        console.error('Error downloading package:', error);
        this.$toast.info(this.$i18n.t("manualDownloadInstructions") || 'يمكنك تحميل الملفات يدوياً من مجلد restaurant_back/PrintServer', {
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
        this.$toast.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
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
        this.$toast.success(this.$i18n.t("commandCopied") || 'تم نسخ الأمر', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      });
    },
    getCategoryLabel(value) {
      const category = this.availablePrintCategories.find(cat => cat.value === value);
      return category ? category.label : value;
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
    resetTagPrinterForm() {
      this.tagPrinterForm = {
        tagId: '',
        printerId: ''
      };
      this.selectedTagPrinter = null;
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

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem;
  color: var(--text-secondary);
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
}

.empty-icon {
  font-size: 4rem;
  color: var(--text-muted);
  margin-bottom: 1rem;
}

.empty-text {
  font-size: 1.125rem;
  color: var(--text-secondary);
  margin: 0;
}

.server-alert-card {
  background: var(--bg-secondary);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-md);
  margin-bottom: 2rem;
  overflow: hidden;
  border: 2px solid var(--warning-color);
}

.server-alert-header {
  background: linear-gradient(135deg, rgba(251, 191, 36, 0.2) 0%, rgba(245, 158, 11, 0.15) 100%);
  padding: 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  border-bottom: 2px solid var(--warning-color);
}

.alert-icon {
  font-size: 2rem;
  color: var(--warning-color);
}

.alert-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.server-alert-body {
  padding: 1.5rem;
}

.alert-message {
  font-size: 1rem;
  color: var(--text-secondary);
  margin-bottom: 1.5rem;
  line-height: 1.6;
}

.status-card {
  background: var(--bg-secondary);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-md);
  margin-bottom: 2rem;
  overflow: hidden;
}

.status-card-header {
  padding: 1.5rem;
  border-bottom: 2px solid var(--border-color);
}

.status-card-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  display: flex;
  align-items: center;
}

.status-card-body {
  padding: 1.5rem;
}

.status-info {
  display: flex;
  justify-content: center;
  align-items: center;
}

.status-badge-large {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 2rem;
  border-radius: var(--radius-lg);
  font-size: 1.125rem;
  font-weight: 600;
}

.status-badge-large.status-success {
  background: rgba(34, 197, 94, 0.1);
  color: var(--success-color);
  border: 2px solid var(--success-color);
}

.status-badge-large.status-error {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
  border: 2px solid var(--danger-color);
}

.status-icon-large {
  font-size: 1.5rem;
}

.section-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  background: linear-gradient(135deg, #818cf8 0%, #a78bfa 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.printers-section,
.tag-printers-section {
  margin-bottom: 2rem;
}

.user-card-actions {
  position: absolute;
  top: 1rem;
  right: 1rem;
  display: flex;
  gap: 0.5rem;
}

[dir="rtl"] .user-card-actions {
  right: auto;
  left: 1rem;
}

.card-action-btn {
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
  width: 2rem;
  height: 2rem;
}

.card-action-btn:hover {
  background: rgba(255, 255, 255, 0.3);
  transform: scale(1.05);
}

.card-action-btn.delete-btn:hover {
  background: rgba(239, 68, 68, 0.4);
  border-color: rgba(239, 68, 68, 0.6);
}

.printer-badges {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  margin-top: 0.5rem;
}

.badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.75rem;
  border-radius: var(--radius-sm);
  font-size: 0.75rem;
  font-weight: 600;
}

.badge-main {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
  border: 1px solid rgba(255, 255, 255, 0.4);
}

.badge-public {
  background: rgba(99, 102, 241, 0.25);
  color: #ffffff;
  border: 1px solid rgba(99, 102, 241, 0.45);
}

.badge-inactive {
  background: rgba(239, 68, 68, 0.3);
  color: #ffffff;
  border: 1px solid rgba(239, 68, 68, 0.5);
}

.badge-online {
  background: rgba(34, 197, 94, 0.3);
  color: #ffffff;
  border: 1px solid rgba(34, 197, 94, 0.5);
}

.badge-offline {
  background: rgba(239, 68, 68, 0.3);
  color: #ffffff;
  border: 1px solid rgba(239, 68, 68, 0.5);
}

.badge-icon {
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

.user-test-button {
  background: linear-gradient(135deg, #818cf8 0%, #a78bfa 100%);
  color: #ffffff;
  border: none;
  width: 100%;
}

.user-test-button:hover {
  background: linear-gradient(135deg, #a78bfa 0%, #818cf8 100%);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.user-test-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
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
</style>

