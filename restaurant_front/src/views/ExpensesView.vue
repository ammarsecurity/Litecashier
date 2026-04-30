<template>
  <b-overlay :show="false" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <!-- Header Section -->
          <div class="users-header-section">
            <div class="users-header-content">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="wallet2" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("expensesManagement") || "إدارة الصرفيات" }}</h1>
                  <p class="header-subtitle">{{ $t("expensesManagementDescription") || "إدارة ومتابعة جميع الصرفيات" }}</p>
                </div>
              </div>
              <button 
                class="users-add-button" 
                @click="showAddExpenseModal = true"
              >
                <b-icon icon="plus-circle" class="me-1"></b-icon>
                {{ $t("addExpense") || "إضافة صرفية" }}
              </button>
            </div>
          </div>

          <!-- Statistics Card -->
          <div class="expenses-statistics-card">
            <div class="expenses-statistics-header">
              <div class="expenses-statistics-header-content">
                <div class="expenses-statistics-title-wrapper">
                  <div class="expenses-statistics-icon-wrapper">
                    <b-icon icon="graph-up" class="expenses-statistics-icon"></b-icon>
                  </div>
                  <div>
                    <h3 class="expenses-statistics-title">
                      {{ $t("expensesStatistics") || "إحصائيات الصرفيات" }}
                    </h3>
                  </div>
                </div>
                <button 
                  class="btn-refresh" 
                  @click="loadStatistics"
                  :disabled="loadingStatistics"
                >
                  <b-icon icon="arrow-clockwise" :class="{ 'spinning': loadingStatistics }"></b-icon>
                  {{ $t("refresh") || "تحديث" }}
                </button>
              </div>
            </div>
            <div class="expenses-statistics-body">
              <div v-if="loadingStatistics" class="loading-state">
                <b-spinner small></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="statistics" class="statistics-grid">
                <div class="stat-card">
                  <div class="stat-icon total">
                    <b-icon icon="wallet2"></b-icon>
                  </div>
                  <div class="stat-content">
                    <div class="stat-value">{{ formatPrice(statistics.totalExpenses || 0) }}</div>
                    <div class="stat-label">{{ $t("totalExpenses") || "إجمالي الصرفيات" }}</div>
                  </div>
                </div>
                <div class="stat-card">
                  <div class="stat-icon active">
                    <b-icon icon="calendar-month"></b-icon>
                  </div>
                  <div class="stat-content">
                    <div class="stat-value">{{ formatPrice(statistics.thisMonthExpenses || 0) }}</div>
                    <div class="stat-label">{{ $t("expensesThisMonth") || "صرفيات هذا الشهر" }}</div>
                  </div>
                </div>
                <div class="stat-card">
                  <div class="stat-icon orders">
                    <b-icon icon="calendar-week"></b-icon>
                  </div>
                  <div class="stat-content">
                    <div class="stat-value">{{ formatPrice(statistics.thisWeekExpenses || 0) }}</div>
                    <div class="stat-label">{{ $t("expensesThisWeek") || "صرفيات هذا الأسبوع" }}</div>
                  </div>
                </div>
                <div class="stat-card">
                  <div class="stat-icon delivered">
                    <b-icon icon="tag-fill"></b-icon>
                  </div>
                  <div class="stat-content">
                    <div class="stat-value">{{ statistics.topCategory || "-" }}</div>
                    <div class="stat-label">{{ $t("topCategory") || "أكبر فئة" }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Search and Filter Section -->
          <div class="users-search-section">
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem;">
              <div class="users-search-container">
                <b-icon icon="search" class="search-icon"></b-icon>
                <input 
                  v-model="searchQuery" 
                  type="text" 
                  :placeholder="$t('searchByDescription') || 'ابحث بالوصف...'"
                  class="users-search-input"
                  @input="debounceSearch"
                />
              </div>
              <div class="users-search-container">
                <b-icon icon="calendar" class="search-icon"></b-icon>
                <input 
                  v-model="startDate" 
                  type="date" 
                  :placeholder="$t('from_date') || 'من تاريخ'"
                  class="users-search-input"
                  @change="loadExpenses"
                />
              </div>
              <div class="users-search-container">
                <b-icon icon="calendar-check" class="search-icon"></b-icon>
                <input 
                  v-model="endDate" 
                  type="date" 
                  :placeholder="$t('to_date') || 'إلى تاريخ'"
                  class="users-search-input"
                  @change="loadExpenses"
                />
              </div>
              <div class="users-search-container">
                <b-icon icon="tag" class="search-icon"></b-icon>
                <select 
                  v-model="categoryFilter" 
                  class="users-search-input"
                  @change="loadExpenses"
                >
                  <option value="">{{ $t('allCategories') || 'جميع الفئات' }}</option>
                  <option v-for="cat in expenseCategories" :key="cat.id" :value="cat.name">
                    {{ cat.name }}
                  </option>
                </select>
              </div>
            </div>
          </div>

          <!-- Actions Section -->
          <div class="expenses-actions-section">
            <button class="btn-manage-categories" @click="showCategoriesModal = true">
              <b-icon icon="tags-fill" class="me-2"></b-icon>
              {{ $t("manageCategories") || "إدارة الفئات" }}
            </button>
            <button class="btn-export" @click="exportExpenses" :disabled="exportingExpenses">
              <b-spinner small v-if="exportingExpenses" class="me-2"></b-spinner>
              <b-icon v-else icon="download" class="me-2"></b-icon>
              {{ exportingExpenses ? ($t("exporting") || "جاري التصدير...") : ($t("export") || "تصدير") }}
            </button>
          </div>

          <!-- Expenses Table -->
          <div class="report-table-container">
            <div v-if="loadingExpenses" class="loading-state-full">
              <b-spinner variant="primary"></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <b-table
              v-else
              id="expenses-table"
              :items="Expenses"
              :fields="expensesTableFields"
              striped
              hover
              responsive
              class="reports-table"
              :empty-text="$t('noExpenses') || 'لا توجد صرفيات'"
            >
              <template #cell(amount)="row">
                <span class="expense-amount-text">{{ formatPrice(row.item.amount) }} {{ $t("currency") || "د.ع" }}</span>
              </template>
              <template #cell(date)="row">
                <span class="expense-date-text">{{ formatDate(row.item.date) }}</span>
              </template>
              <template #cell(category)="row">
                <span
                  class="expense-category-badge"
                  :class="getCategoryClass(row.item.category)"
                  :style="getCategoryClass(row.item.category) === '' ? {
                    backgroundColor: getCategoryColor(row.item.category) + '20',
                    color: getCategoryColor(row.item.category),
                    borderColor: getCategoryColor(row.item.category) + '50'
                  } : {}"
                >
                  {{ row.item.category }}
                </span>
              </template>
              <template #cell(description)="row">
                <span>{{ row.item.description || '-' }}</span>
              </template>
              <template #cell(employee)="row">
                <span>{{ row.item.employee?.name || '-' }}</span>
              </template>
              <template #cell(tag)="row">
                <span>{{ row.item.tag?.name || '-' }}</span>
              </template>
              <template #cell(actions)="row">
                <div class="expenses-actions-cell">
                  <button class="user-action-button expense-action-btn expense-action-btn-edit" @click="editExpense(row.item)">
                    <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                  </button>
                  <button class="user-action-button expense-action-btn expense-action-btn-delete" @click="confirmDeleteExpense(row.item)">
                    <b-icon icon="trash-fill" class="action-icon"></b-icon>
                  </button>
                </div>
              </template>
            </b-table>
          </div>

          <!-- Empty State -->
          <div v-if="Expenses.length === 0 && !loadingExpenses" class="empty-state">
            <b-icon icon="wallet2" class="empty-icon"></b-icon>
            <p class="empty-text">{{ $t("noExpenses") || "لا توجد صرفيات" }}</p>
          </div>

          <!-- Pagination -->
          <div class="users-pagination-section">
            <b-pagination 
              v-model="pageNumber" 
              :total-rows="totalExpenses" 
              :per-page="pageSize"
              aria-controls="expenses-table"
              class="users-pagination"
              @change="loadExpenses"
            ></b-pagination>
          </div>
        </div>
      </div>
    </div>

    <!-- Add/Edit Expense Modal -->
    <b-modal 
      v-model="showAddExpenseModal" 
      :title="selectedExpense ? ($t('editExpense') || 'تعديل صرفية') : ($t('addExpense') || 'إضافة صرفية')" 
      @hidden="resetExpenseForm"
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ selectedExpense ? ($t('editExpense') || 'تعديل صرفية') : ($t('addExpense') || 'إضافة صرفية') }}</h2>
        <form @submit.prevent="saveExpense" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="currency-dollar" class="form-label-icon"></b-icon>
                {{ $t("expenseAmount") || "المبلغ" }} <span class="required">*</span>
              </label>
              <input 
                v-model.number="expenseForm.amount" 
                type="number" 
                step="0.01"
                min="0.01"
                class="users-form-input"
                :placeholder="$t('enterAmount') || 'أدخل المبلغ'"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="calendar" class="form-label-icon"></b-icon>
                {{ $t("expenseDate") || "تاريخ الصرف" }} <span class="required">*</span>
              </label>
              <input 
                v-model="expenseForm.date" 
                type="date" 
                class="users-form-input"
                required
              />
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
              {{ $t("expenseCategory") || "الفئة" }} <span class="required">*</span>
            </label>
            <select 
              v-model="expenseForm.category" 
              class="users-form-input"
              required
            >
              <option value="">{{ $t('selectCategory') || 'اختر الفئة' }}</option>
              <option v-for="cat in expenseCategories" :key="cat.id" :value="cat.name">
                {{ cat.name }}
              </option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text-fill" class="form-label-icon"></b-icon>
              {{ $t("expenseDescription") || "الوصف" }}
            </label>
            <textarea 
              v-model="expenseForm.description" 
              class="users-form-input"
              rows="3"
              :placeholder="$t('enterDescription') || 'أدخل وصف الصرفية (اختياري)'"
            ></textarea>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="person-badge-fill" class="form-label-icon"></b-icon>
              {{ $t("expenseEmployee") || "الموظف" }}
            </label>
            <select v-model="expenseForm.employeeId" class="users-form-input">
              <option value="">{{ $t("noEmployee") || "بدون موظف" }}</option>
              <option v-for="emp in employees" :key="emp.id" :value="emp.id">{{ emp.name }}</option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
              {{ $t("expenseTag") || "القسم (Tag)" }}
            </label>
            <select v-model="expenseForm.tagId" class="users-form-input">
              <option value="">{{ $t("noTag") || "بدون قسم" }}</option>
              <option v-for="tag in tags" :key="tag.id" :value="tag.id">{{ tag.name }}</option>
            </select>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="savingExpense">
              <b-spinner small v-if="savingExpense" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{ savingExpense ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showAddExpenseModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Delete Confirmation Modal -->
    <b-modal 
      v-model="showDeleteModal" 
      :title="$t('confirmDelete') || 'تأكيد الحذف'"
      @ok="deleteExpense"
      @cancel="showDeleteModal = false"
      ok-variant="danger"
      cancel-variant="secondary"
      :ok-disabled="deletingExpense"
    >
      <div v-if="deletingExpense" class="loading-state">
        <b-spinner small></b-spinner>
        <span>{{ $t("deleting") || "جاري الحذف..." }}</span>
      </div>
      <p v-else>{{ $t("confirmDeleteExpense") || "هل أنت متأكد من حذف هذه الصرفية؟" }}</p>
    </b-modal>

    <!-- Categories Management Modal -->
    <b-modal 
      v-model="showCategoriesModal" 
      :title="$t('manageCategories') || 'إدارة الفئات'"
      @hidden="resetCategoryForm"
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("manageCategories") || "إدارة الفئات" }}</h2>
        
        <!-- Add Category Form -->
        <div class="category-form-section">
          <h3 class="section-title">{{ selectedCategory ? ($t('editCategory') || 'تعديل فئة') : ($t('addCategory') || 'إضافة فئة') }}</h3>
          <form @submit.prevent="saveCategory" class="users-form">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
                {{ $t("categoryName") || "اسم الفئة" }} <span class="required">*</span>
              </label>
              <input 
                v-model="categoryForm.name" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterCategoryName') || 'أدخل اسم الفئة'"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="file-text-fill" class="form-label-icon"></b-icon>
                {{ $t("description") || "الوصف" }}
              </label>
              <textarea 
                v-model="categoryForm.description" 
                class="users-form-input"
                rows="2"
                :placeholder="$t('enterDescription') || 'أدخل الوصف (اختياري)'"
              ></textarea>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="palette-fill" class="form-label-icon"></b-icon>
                {{ $t("color") || "اللون" }}
              </label>
              <input 
                v-model="categoryForm.color" 
                type="color" 
                class="users-form-input color-input"
              />
            </div>
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="savingCategory">
                <b-spinner small v-if="savingCategory" class="me-2"></b-spinner>
                <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
                {{ savingCategory ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="cancelCategoryForm" :disabled="savingCategory">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancel") || "إلغاء" }}
              </button>
            </div>
          </form>
        </div>

        <!-- Categories List -->
        <div class="categories-list-section">
          <h3 class="section-title">{{ $t("categoriesList") || "قائمة الفئات" }}</h3>
          <div v-if="loadingCategories" class="loading-state">
            <b-spinner small></b-spinner>
            <span>{{ $t("loading") || "جاري التحميل..." }}</span>
          </div>
          <div v-else-if="expenseCategories.length > 0" class="categories-grid">
            <div 
              v-for="cat in expenseCategories" 
              :key="cat.id"
              class="category-item"
            >
              <div class="category-item-content">
                <div class="category-color-indicator" :style="{ backgroundColor: cat.color || '#6b7280' }"></div>
                <div class="category-info">
                  <h4 class="category-name">{{ cat.name }}</h4>
                  <p v-if="cat.description" class="category-description">{{ cat.description }}</p>
                </div>
              </div>
              <div class="category-actions">
                <button class="category-edit-btn" @click="editCategory(cat)">
                  <b-icon icon="pencil-fill"></b-icon>
                </button>
                <button class="category-delete-btn" @click="confirmDeleteCategory(cat)">
                  <b-icon icon="trash-fill"></b-icon>
                </button>
              </div>
            </div>
          </div>
          <div v-else class="empty-state">
            <b-icon icon="tags" class="empty-icon"></b-icon>
            <p class="empty-text">{{ $t("noCategories") || "لا توجد فئات" }}</p>
          </div>
        </div>
      </div>
    </b-modal>

    <!-- Delete Category Confirmation Modal -->
    <b-modal 
      v-model="showDeleteCategoryModal" 
      :title="$t('confirmDelete') || 'تأكيد الحذف'"
      @ok="deleteCategory"
      @cancel="showDeleteCategoryModal = false"
      ok-variant="danger"
      cancel-variant="secondary"
      :ok-disabled="deletingCategory"
    >
      <div v-if="deletingCategory" class="loading-state">
        <b-spinner small></b-spinner>
        <span>{{ $t("deleting") || "جاري الحذف..." }}</span>
      </div>
      <p v-else>{{ $t("confirmDeleteCategory") || "هل أنت متأكد من حذف هذه الفئة؟" }}</p>
    </b-modal>
  </b-overlay>
</template>

<script>
import AppHeader from '../components/Layout/AppHeader.vue';
import { HTTP } from '../http/api.js';

export default {
  name: 'ExpensesView',
  components: {
    AppHeader
  },
  data() {
    return {
      show: false,
      Expenses: [],
      totalExpenses: 0,
      pageNumber: 1,
      pageSize: 10,
      searchQuery: '',
      categoryFilter: '',
      startDate: '',
      endDate: '',
      searchTimer: null,
      showAddExpenseModal: false,
      selectedExpense: null,
      expenseForm: {
        amount: 0,
        date: new Date().toISOString().split('T')[0],
        category: '',
        description: '',
        employeeId: '',
        tagId: ''
      },
      employees: [],
      tags: [],
      statistics: null,
      loadingStatistics: false,
      loadingExpenses: false,
      savingExpense: false,
      deletingExpense: false,
      exportingExpenses: false,
      showDeleteModal: false,
      expenseToDelete: null,
      commercialUserId: null,
      expenseCategories: [],
      showCategoriesModal: false,
      categoryForm: {
        name: '',
        description: '',
        color: '#6b7280'
      },
      selectedCategory: null,
      showDeleteCategoryModal: false,
      categoryToDelete: null,
      loadingCategories: false,
      savingCategory: false,
      deletingCategory: false
    };
  },
  computed: {
    expensesTableFields() {
      return [
        { key: 'amount', label: this.$t("expenseAmount") || "المبلغ" },
        { key: 'date', label: this.$t("expenseDate") || "التاريخ" },
        { key: 'category', label: this.$t("expenseCategory") || "الفئة" },
        { key: 'description', label: this.$t("expenseDescription") || "الوصف" },
        { key: 'employee', label: this.$t("employeeLabel") || "الموظف" },
        { key: 'tag', label: this.$t("expenseTag") || "القسم (Tag)" },
        { key: 'actions', label: this.$t("actions") || "الإجراءات" }
      ];
    }
  },
  mounted() {
    const userInfo = JSON.parse(localStorage.getItem('info') || '{}');
    this.commercialUserId = userInfo.id || userInfo.commercialUserId;
    
    if (!this.commercialUserId) {
      this.$bvToast.toast('معرف المطعم غير موجود', {
        title: 'خطأ',
        variant: 'danger',
        solid: true
      });
      return;
    }

    this.loadExpenses();
    this.loadStatistics();
    this.loadCategories();
    this.loadEmployees();
    this.loadTags();
  },
  beforeDestroy() {
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
  },
  methods: {
    async loadExpenses() {
      try {
        this.loadingExpenses = true;
        
        const params = new URLSearchParams({
          pageNumber: (this.pageNumber - 1).toString(),
          pageSize: this.pageSize.toString()
        });

        if (this.searchQuery) {
          params.append('search', this.searchQuery);
        }
        if (this.categoryFilter) {
          params.append('category', this.categoryFilter);
        }
        if (this.startDate) {
          params.append('startDate', this.startDate);
        }
        if (this.endDate) {
          params.append('endDate', this.endDate);
        }

        const response = await HTTP.get(`Expenses?${params.toString()}`);
        
        if (response.data && !response.data.errorStatus) {
          this.Expenses = response.data.data.items || [];
          this.totalExpenses = response.data.data.totalItems || 0;
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء جلب الصرفيات', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error loading expenses:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء جلب الصرفيات', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.loadingExpenses = false;
      }
    },
    async loadStatistics() {
      try {
        this.loadingStatistics = true;
        const params = new URLSearchParams();
        if (this.startDate) params.append('startDate', this.startDate);
        if (this.endDate) params.append('endDate', this.endDate);
        
        const response = await HTTP.get(`Expenses/Statistics?${params.toString()}`);
        
        if (response.data && !response.data.errorStatus) {
          this.statistics = response.data.data;
        }
      } catch (error) {
        console.error('Error loading statistics:', error);
      } finally {
        this.loadingStatistics = false;
      }
    },
    debounceSearch() {
      clearTimeout(this.searchTimer);
      this.searchTimer = setTimeout(() => {
        this.pageNumber = 1;
        this.loadExpenses();
      }, 500);
    },
    async saveExpense() {
      try {
        this.savingExpense = true;
        
        const request = {
          amount: parseFloat(this.expenseForm.amount),
          date: this.expenseForm.date,
          category: this.expenseForm.category,
          description: this.expenseForm.description || null,
          employeeId: (this.expenseForm.employeeId != null && this.expenseForm.employeeId !== '') ? Number(this.expenseForm.employeeId) : null,
          tagId: (this.expenseForm.tagId != null && this.expenseForm.tagId !== '') ? Number(this.expenseForm.tagId) : null
        };

        let response;
        if (this.selectedExpense) {
          response = await HTTP.put(`Expenses/${this.selectedExpense.id}`, request);
        } else {
          response = await HTTP.post('Expenses', request);
        }

        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || 'تم حفظ الصرفية بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });
          this.showAddExpenseModal = false;
          this.resetExpenseForm();
          this.loadExpenses();
          this.loadStatistics();
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء حفظ الصرفية', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error saving expense:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء حفظ الصرفية', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.savingExpense = false;
      }
    },
    editExpense(expense) {
      this.selectedExpense = expense;
      this.expenseForm = {
        amount: expense.amount,
        date: expense.date.split('T')[0],
        category: expense.category,
        description: expense.description || '',
        employeeId: (expense.employeeId != null && expense.employeeId !== '') ? expense.employeeId : '',
        tagId: (expense.tagId != null && expense.tagId !== '') ? expense.tagId : ''
      };
      this.showAddExpenseModal = true;
    },
    confirmDeleteExpense(expense) {
      this.expenseToDelete = expense;
      this.showDeleteModal = true;
    },
    async deleteExpense() {
      if (!this.expenseToDelete) return;
      
      try {
        this.deletingExpense = true;
        const response = await HTTP.delete(`Expenses/${this.expenseToDelete.id}`);
        
        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast('تم حذف الصرفية بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });
          this.showDeleteModal = false;
          this.expenseToDelete = null;
          this.loadExpenses();
          this.loadStatistics();
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء حذف الصرفية', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error deleting expense:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء حذف الصرفية', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.deletingExpense = false;
      }
    },
    resetExpenseForm() {
      this.selectedExpense = null;
      this.expenseForm = {
        amount: 0,
        date: new Date().toISOString().split('T')[0],
        category: '',
        description: '',
        employeeId: '',
        tagId: ''
      };
    },
    async loadTags() {
      try {
        const response = await HTTP.get('Admin/GetTags?pageNumber=0&pageSize=500&info=');
        if (response.data && response.data.data && response.data.data.items) {
          this.tags = response.data.data.items;
        } else {
          this.tags = [];
        }
      } catch (error) {
        console.error('Error loading tags:', error);
        this.tags = [];
      }
    },
    async loadEmployees() {
      try {
        const response = await HTTP.get('Employees');
        if (response.data && !response.data.errorStatus) {
          this.employees = response.data.data || [];
        } else {
          this.employees = [];
        }
      } catch (error) {
        console.error('Error loading employees:', error);
        this.employees = [];
      }
    },
    async exportExpenses() {
      try {
        this.exportingExpenses = true;
        const params = new URLSearchParams();
        if (this.categoryFilter) params.append('category', this.categoryFilter);
        if (this.startDate) params.append('startDate', this.startDate);
        if (this.endDate) params.append('endDate', this.endDate);
        params.append('format', 'csv');

        const response = await HTTP.get(`Expenses/Export?${params.toString()}`, {
          responseType: 'blob'
        });

        const blob = new Blob([response.data], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', `expenses_${new Date().toISOString().split('T')[0]}.csv`);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        this.$bvToast.toast('تم تصدير الصرفيات بنجاح', {
          title: 'نجاح',
          variant: 'success',
          solid: true
        });
      } catch (error) {
        console.error('Error exporting expenses:', error);
        this.$bvToast.toast('حدث خطأ أثناء تصدير الصرفيات', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.exportingExpenses = false;
      }
    },
    getCategoryClass(category) {
      // Use dynamic color from category if available
      const categoryObj = this.expenseCategories.find(c => c.name === category);
      if (categoryObj && categoryObj.color) {
        return '';
      }
      // Fallback to default classes
      const classes = {
        'رواتب': 'category-salaries',
        'إيجار': 'category-rent',
        'فواتير': 'category-utilities',
        'صيانة': 'category-maintenance',
        'مستلزمات': 'category-supplies',
        'أخرى': 'category-other'
      };
      return classes[category] || 'category-other';
    },
    formatPrice(price) {
      if (price) {
        return price.toLocaleString("en-EG");
      }
      return "0";
    },
    formatDate(date) {
      if (!date) return '';
      const d = new Date(date);
      return d.toLocaleDateString('ar-IQ', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      });
    },
    async loadCategories() {
      try {
        this.loadingCategories = true;
        const response = await HTTP.get('ExpenseCategories');
        
        if (response.data && !response.data.errorStatus) {
          this.expenseCategories = response.data.data || [];
        }
      } catch (error) {
        console.error('Error loading categories:', error);
      } finally {
        this.loadingCategories = false;
      }
    },
    async saveCategory() {
      try {
        this.savingCategory = true;
        
        const request = {
          name: this.categoryForm.name.trim(),
          description: this.categoryForm.description?.trim() || null,
          color: this.categoryForm.color || null
        };

        let response;
        if (this.selectedCategory) {
          response = await HTTP.put(`ExpenseCategories/${this.selectedCategory.id}`, request);
        } else {
          response = await HTTP.post('ExpenseCategories', request);
        }

        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast(response.data.message || 'تم حفظ الفئة بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });
          this.resetCategoryForm();
          this.loadCategories();
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء حفظ الفئة', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error saving category:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء حفظ الفئة', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.savingCategory = false;
      }
    },
    editCategory(category) {
      this.selectedCategory = category;
      this.categoryForm = {
        name: category.name,
        description: category.description || '',
        color: category.color || '#6b7280'
      };
    },
    confirmDeleteCategory(category) {
      this.categoryToDelete = category;
      this.showDeleteCategoryModal = true;
    },
    async deleteCategory() {
      if (!this.categoryToDelete) return;
      
      try {
        this.deletingCategory = true;
        const response = await HTTP.delete(`ExpenseCategories/${this.categoryToDelete.id}`);
        
        if (response.data && !response.data.errorStatus) {
          this.$bvToast.toast('تم حذف الفئة بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });
          this.showDeleteCategoryModal = false;
          this.categoryToDelete = null;
          this.loadCategories();
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء حذف الفئة', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error deleting category:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء حذف الفئة', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.deletingCategory = false;
      }
    },
    resetCategoryForm() {
      this.selectedCategory = null;
      this.categoryForm = {
        name: '',
        description: '',
        color: '#6b7280'
      };
    },
    cancelCategoryForm() {
      this.resetCategoryForm();
      this.showCategoriesModal = false;
    },
    getCategoryColor(categoryName) {
      const category = this.expenseCategories.find(c => c.name === categoryName);
      return category?.color || '#6b7280';
    }
  }
};
</script>

<style scoped>
.expenses-statistics-card {
  background: var(--bg-primary);
  border-radius: 1rem;
  padding: 0;
  margin-bottom: 2rem;
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-color);
  overflow: hidden;
}

.expenses-statistics-header {
  padding: 1.5rem;
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-color);
}

.expenses-statistics-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.expenses-statistics-title-wrapper {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.expenses-statistics-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 0.75rem;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.15) 0%, rgba(167, 139, 250, 0.15) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.expenses-statistics-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.expenses-statistics-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.expenses-statistics-body {
  padding: 1.5rem;
}

.statistics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.stat-card {
  background: var(--bg-secondary);
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  padding: 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  transition: all 0.3s ease;
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 16px rgba(129, 140, 248, 0.2), 0 4px 8px rgba(0, 0, 0, 0.3);
  border-color: var(--primary-color);
  background: var(--bg-primary);
}

.stat-icon {
  width: 50px;
  height: 50px;
  border-radius: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  flex-shrink: 0;
}

.stat-icon.total {
  background: rgba(239, 68, 68, 0.15);
  color: var(--danger-color);
}

.stat-icon.active {
  background: rgba(59, 130, 246, 0.15);
  color: var(--info-color);
}

.stat-icon.orders {
  background: rgba(251, 191, 36, 0.15);
  color: #fbbf24;
}

.stat-icon.delivered {
  background: rgba(34, 197, 94, 0.15);
  color: var(--success-color);
}

.stat-content {
  flex: 1;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 0.25rem;
}

.stat-label {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.btn-refresh {
  background: var(--bg-tertiary);
  border: 2px solid var(--border-color);
  padding: 0.625rem 1rem;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: all 0.3s ease;
  color: var(--text-primary);
  font-weight: 600;
}

.btn-refresh:hover {
  background: var(--primary-color);
  color: #ffffff;
  border-color: var(--primary-color);
}

.btn-refresh .spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.expenses-actions-section {
  margin-bottom: 1.5rem;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.btn-manage-categories {
  background: var(--primary-color);
  color: #ffffff;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.3s ease;
}

.btn-manage-categories:hover {
  background: var(--accent-dark);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.expenses-export-section {
  display: flex;
  justify-content: flex-end;
}

.btn-export {
  background: var(--success-color);
  color: #ffffff;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.3s ease;
}

.btn-export:hover {
  background: var(--accent-dark);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.expense-card {
  transition: all 0.3s ease;
}

.expense-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-md);
}

.expense-header-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  flex: 1;
}

.expense-date {
  font-size: 0.75rem;
  color: var(--text-secondary);
  font-weight: 400;
}

.expense-category-badge {
  display: inline-block;
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  white-space: nowrap;
}

.category-salaries {
  background: rgba(59, 130, 246, 0.15);
  color: var(--info-color);
  border: 1px solid rgba(59, 130, 246, 0.3);
}

.category-rent {
  background: rgba(239, 68, 68, 0.15);
  color: var(--danger-color);
  border: 1px solid rgba(239, 68, 68, 0.3);
}

.category-utilities {
  background: rgba(251, 191, 36, 0.15);
  color: #fbbf24;
  border: 1px solid rgba(251, 191, 36, 0.3);
}

.category-maintenance {
  background: rgba(139, 92, 246, 0.15);
  color: #8b5cf6;
  border: 1px solid rgba(139, 92, 246, 0.3);
}

.category-supplies {
  background: rgba(34, 197, 94, 0.15);
  color: var(--success-color);
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.category-other {
  background: rgba(107, 114, 128, 0.15);
  color: #6b7280;
  border: 1px solid rgba(107, 114, 128, 0.3);
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem;
  color: var(--text-secondary);
}

.loading-state-full {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  padding: 4rem 2rem;
  color: var(--text-secondary);
  min-height: 300px;
}

.expenses-actions-cell {
  display: flex;
  gap: 0.5rem;
  justify-content: center;
}

.reports-table .user-action-button {
  width: 34px;
  height: 34px;
  padding: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.expense-action-btn {
  border-radius: 0.55rem;
  border: 1px solid transparent;
  transition: all 0.2s ease;
}

.expense-action-btn-edit {
  background: color-mix(in srgb, var(--primary-color) 12%, var(--bg-primary));
  color: var(--primary-color);
  border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
}

.expense-action-btn-delete {
  background: color-mix(in srgb, #dc3545 12%, var(--bg-primary));
  color: #dc3545;
  border-color: color-mix(in srgb, #dc3545 40%, var(--border-color));
}

.expense-action-btn:hover {
  transform: translateY(-1px);
}

.expense-action-btn-edit:hover {
  background: color-mix(in srgb, var(--primary-color) 20%, var(--bg-primary));
  border-color: var(--primary-color);
}

.expense-action-btn-delete:hover {
  background: color-mix(in srgb, #dc3545 20%, var(--bg-primary));
  border-color: #dc3545;
}

.expense-amount-text {
  font-weight: 700;
  color: var(--text-primary);
  white-space: nowrap;
}

.expense-date-text {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
}

.reports-table ::v-deep tbody td {
  vertical-align: middle;
}

.empty-state {
  text-align: center;
  padding: 4rem 2rem;
}

.empty-icon {
  font-size: 4rem;
  color: rgba(129, 140, 248, 0.4);
  margin-bottom: 1rem;
}

.empty-text {
  font-size: 1.125rem;
  color: var(--text-secondary);
}

.category-form-section {
  margin-bottom: 2rem;
  padding-bottom: 2rem;
  border-bottom: 2px solid var(--border-color);
}

.section-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 1.5rem;
}

.categories-list-section {
  margin-top: 2rem;
}

.categories-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
}

.category-item {
  background: var(--bg-secondary);
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  padding: 1rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  transition: all 0.3s ease;
}

.category-item:hover {
  border-color: var(--primary-color);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.category-item-content {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex: 1;
}

.category-color-indicator {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  flex-shrink: 0;
  border: 2px solid var(--border-color);
}

.category-info {
  flex: 1;
}

.category-name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 0.25rem 0;
}

.category-description {
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin: 0;
}

.category-actions {
  display: flex;
  gap: 0.5rem;
}

.category-edit-btn,
.category-delete-btn {
  width: 36px;
  height: 36px;
  border-radius: 0.5rem;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s ease;
  font-size: 1rem;
}

.category-edit-btn {
  background: rgba(59, 130, 246, 0.15);
  color: var(--info-color);
}

.category-edit-btn:hover {
  background: var(--info-color);
  color: #ffffff;
  transform: scale(1.05);
}

.category-delete-btn {
  background: rgba(239, 68, 68, 0.15);
  color: var(--danger-color);
}

.category-delete-btn:hover {
  background: var(--danger-color);
  color: #ffffff;
  transform: scale(1.05);
}

.color-input {
  height: 48px;
  cursor: pointer;
}
</style>

