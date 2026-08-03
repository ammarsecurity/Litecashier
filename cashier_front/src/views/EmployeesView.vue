<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content employees-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="person-badge-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("employeesManagement") || "إدارة الموظفين" }}</h1>
                  <p class="header-subtitle">{{ $t("employeesManagementDescription") || "إدارة بيانات الموظفين والرواتب والأقسام" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button
                  type="button"
                  class="btn-refresh"
                  @click="refreshPage"
                  :disabled="loadingEmployees"
                >
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loadingEmployees }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
                <button type="button" class="users-add-button" @click="openAddModal">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addEmployee") || "إضافة موظف" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="people-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingEmployees"></b-spinner>
                  <template v-else>{{ employees.length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("employees") || "الموظفون" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="tags-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingEmployees"></b-spinner>
                  <template v-else>{{ employeesWithDepartmentCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("employeesOverviewWithDepartment") || "مرتبطون بقسم" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="calendar-month"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingEmployees"></b-spinner>
                  <template v-else>{{ monthlySalaryEmployeesCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("salaryTypeMonthly") || "راتب شهري" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="briefcase-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingEmployees"></b-spinner>
                  <template v-else>{{ employeesWithJobTitleCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("employeesOverviewWithJobTitle") || "لديهم مسمى وظيفي" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="people-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("employees") || "الموظفون" }}</h3>
                  <p class="app-section-subtitle">{{ $t("employeesListDescription") || "قائمة الموظفين مع البيانات والراتب والقسم" }}</p>
                </div>
              </div>
            </div>
            <div class="app-filters-panel app-filters-panel--inset">
              <div class="app-filters-panel-head">
                <div class="app-filters-panel-title">
                  <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                  <div>
                    <h3>{{ $t("filters") || "الفلاتر" }}</h3>
                    <p>{{ $t("employeesFiltersHint") || "بحث في الموظفين بالاسم أو الهاتف" }}</p>
                  </div>
                </div>
                <div class="app-filters-panel-actions" v-if="searchQuery">
                  <button
                    type="button"
                    class="users-filter-clear-btn app-filters-clear-btn"
                    @click="searchQuery = ''"
                  >
                    <b-icon icon="x-circle" class="me-1"></b-icon>
                    {{ $t("clearFilters") || "مسح الفلاتر" }}
                  </button>
                </div>
              </div>
              <div class="app-filters-fields app-filters-fields--2">
                <label class="app-filter-field app-filter-field--grow">
                  <span class="app-filter-label">{{ $t("search") || "بحث" }}</span>
                  <div class="users-search-container">
                    <b-icon icon="search" class="search-icon"></b-icon>
                    <input
                      v-model="searchQuery"
                      type="search"
                      class="users-search-input"
                      :placeholder="$t('searchEmployeesPlaceholder') || 'بحث بالاسم أو الهاتف...'"
                      autocomplete="off"
                    />
                  </div>
                </label>
              </div>
            </div>
            <div class="app-section-body">
              <div v-if="loadingEmployees" class="loading-state-full">
                <b-spinner small></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="filteredEmployees.length > 0" class="app-cards-grid">
                <div v-for="emp in filteredEmployees" :key="emp.id" class="app-item-card">
                  <div class="app-item-card-header">
                    <div class="app-item-card-title">
                      <b-icon icon="person-circle" class="app-item-card-icon"></b-icon>
                      <h4>{{ emp.name }}</h4>
                    </div>
                    <div class="app-item-card-actions" role="group" :aria-label="$t('actions') || 'العمليات'">
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        @click="editEmployee(emp)"
                        :title="$t('edit') || 'تعديل'"
                        :aria-label="$t('edit') || 'تعديل'"
                      >
                        <b-icon icon="pencil-square" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--delete"
                        @click="confirmDeleteEmployee(emp)"
                        :title="$t('delete') || 'حذف'"
                        :aria-label="$t('delete') || 'حذف'"
                      >
                        <b-icon icon="trash" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </div>
                  <div class="app-item-card-body">
                    <div class="app-info-row">
                      <b-icon icon="telephone-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("phoneNumber") || "رقم الهاتف" }}</span>
                      <span class="info-value">{{ emp.phoneNumber }}</span>
                    </div>
                    <div v-if="emp.address" class="app-info-row">
                      <b-icon icon="geo-alt-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("address") || "العنوان" }}</span>
                      <span class="info-value">{{ emp.address }}</span>
                    </div>
                    <div v-if="emp.jobTitle" class="app-info-row">
                      <b-icon icon="briefcase-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("jobTitle") || "المسمى الوظيفي" }}</span>
                      <span class="info-value">{{ emp.jobTitle }}</span>
                    </div>
                    <div class="app-info-row">
                      <b-icon icon="cash-stack" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("salary") || "الراتب" }}</span>
                      <span class="info-value">{{ formatPrice(emp.salary) }} ({{ salaryTypeLabel(emp.salaryType) }})</span>
                    </div>
                    <div v-if="emp.tag" class="app-info-row">
                      <b-icon icon="tags-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("category") || "القسم" }}</span>
                      <span class="info-value">{{ emp.tag.name }}</span>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="empty-state">
                <b-icon icon="people" class="empty-icon"></b-icon>
                <p>{{ searchQuery ? ($t("noResults") || "لا توجد نتائج") : ($t("noEmployees") || "لا يوجد موظفون") }}</p>
                <button v-if="!searchQuery" type="button" class="empty-state-btn" @click="openAddModal">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addFirstEmployee") || "إضافة أول موظف" }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal
      v-model="showEmployeeModal"
      :title="selectedEmployee ? ($t('editEmployee') || 'تعديل موظف') : ($t('addEmployee') || 'إضافة موظف')"
      @hidden="resetEmployeeForm"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ selectedEmployee ? ($t('editEmployee') || 'تعديل موظف') : ($t('addEmployee') || 'إضافة موظف') }}</h2>
        <form @submit.prevent="saveEmployee" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                {{ $t("employeeName") || "اسم الموظف" }} <span class="required">*</span>
              </label>
              <input
                v-model="employeeForm.name"
                type="text"
                class="users-form-input"
                :placeholder="$t('enterEmployeeName') || 'أدخل اسم الموظف'"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                {{ $t("phoneNumber") || "رقم الهاتف" }} <span class="required">*</span>
              </label>
              <input
                v-model="employeeForm.phoneNumber"
                type="text"
                class="users-form-input"
                :placeholder="$t('enterPhoneNumber') || 'أدخل رقم الهاتف'"
                required
              />
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
              {{ $t("address") || "العنوان" }}
            </label>
            <input
              v-model="employeeForm.address"
              type="text"
              class="users-form-input"
              :placeholder="$t('enterAddress') || 'أدخل العنوان'"
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="briefcase-fill" class="form-label-icon"></b-icon>
              {{ $t("jobTitle") || "المسمى الوظيفي" }}
            </label>
            <input
              v-model="employeeForm.jobTitle"
              type="text"
              class="users-form-input"
              :placeholder="$t('enterJobTitle') || 'أدخل المسمى الوظيفي'"
            />
          </div>
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="cash-stack" class="form-label-icon"></b-icon>
                {{ $t("salary") || "الراتب" }} <span class="required">*</span>
              </label>
              <input
                v-model.number="employeeForm.salary"
                type="number"
                step="0.01"
                min="0"
                class="users-form-input"
                :placeholder="$t('enterSalary') || 'أدخل الراتب'"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="calendar-check" class="form-label-icon"></b-icon>
                {{ $t("salaryType") || "نوع الراتب" }} <span class="required">*</span>
              </label>
              <select v-model.number="employeeForm.salaryType" class="users-form-input" required>
                <option :value="0">{{ $t("salaryTypeDaily") || "يومي" }}</option>
                <option :value="1">{{ $t("salaryTypeWeekly") || "أسبوعي" }}</option>
                <option :value="2">{{ $t("salaryTypeMonthly") || "شهري" }}</option>
              </select>
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
              {{ $t("category") || "القسم" }}
            </label>
            <select v-model="employeeForm.tagId" class="users-form-input">
              <option value="">{{ $t("noDepartment") || "بدون قسم" }}</option>
              <option v-for="tag in tags" :key="tag.id" :value="tag.id">{{ tag.name }}</option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="toggle-on" class="form-label-icon"></b-icon>
              {{ $t("active") || "نشط" }}
            </label>
            <select v-model="employeeForm.isActive" class="users-form-input">
              <option :value="true">{{ $t("active") || "نشط" }}</option>
              <option :value="false">{{ $t("inactive") || "غير نشط" }}</option>
            </select>
          </div>
          <div class="users-form-actions employees-form-actions">
            <button
              type="button"
              class="users-form-cancel-button employees-payroll-btn"
              @click="$router.push('/payroll')"
            >
              <b-icon icon="wallet2" class="me-2"></b-icon>
              {{ $t("payrollAndAdvances") || "الرواتب والسلف" }}
            </button>
            <div class="employees-form-actions-main">
              <button
                type="button"
                class="users-form-cancel-button"
                @click="showEmployeeModal = false"
                :disabled="savingEmployee"
              >
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancel") || "إلغاء" }}
              </button>
              <button type="submit" class="users-form-submit-button" :disabled="savingEmployee">
                <b-spinner small v-if="savingEmployee" class="me-2"></b-spinner>
                <b-icon
                  v-else
                  :icon="selectedEmployee ? 'check-circle-fill' : 'plus-circle-fill'"
                  class="me-2"
                ></b-icon>
                {{
                  savingEmployee
                    ? (selectedEmployee ? ($t("updating") || "جاري التحديث...") : ($t("adding") || "جاري الإضافة..."))
                    : (selectedEmployee ? ($t("update") || "تحديث") : ($t("add") || "إضافة"))
                }}
              </button>
            </div>
          </div>
        </form>
      </div>
    </b-modal>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from '../http/api.js';

export default {
  name: "EmployeesView",
  components: {
    AppHeader,
  },
  data() {
    return {
      loadingEmployees: false,
      loadingTags: false,
      searchQuery: '',
      employees: [],
      tags: [],
      showEmployeeModal: false,
      savingEmployee: false,
      selectedEmployee: null,
      employeeForm: {
        name: '',
        phoneNumber: '',
        address: '',
        jobTitle: '',
        salary: 0,
        salaryType: 0,
        tagId: '',
        isActive: true
      },
    };
  },
  computed: {
    filteredEmployees() {
      const q = (this.searchQuery || '').trim().toLowerCase();
      if (!q) return this.employees;
      return this.employees.filter((emp) => {
        const name = (emp.name || '').toLowerCase();
        const phone = (emp.phoneNumber || '').toLowerCase();
        const job = (emp.jobTitle || '').toLowerCase();
        const dept = (emp.tag?.name || '').toLowerCase();
        return name.includes(q) || phone.includes(q) || job.includes(q) || dept.includes(q);
      });
    },
    employeesWithDepartmentCount() {
      return this.employees.filter((e) => e.tag || e.tagId).length;
    },
    monthlySalaryEmployeesCount() {
      return this.employees.filter((e) => Number(e.salaryType) === 2).length;
    },
    employeesWithJobTitleCount() {
      return this.employees.filter((e) => (e.jobTitle || '').trim()).length;
    },
  },
  mounted() {
    this.loadEmployees();
    this.loadTags();
  },
  methods: {
    refreshPage() {
      this.loadEmployees();
      this.loadTags();
    },
    salaryTypeLabel(type) {
      if (type === 0) return this.$t("salaryTypeDaily") || "يومي";
      if (type === 1) return this.$t("salaryTypeWeekly") || "أسبوعي";
      if (type === 2) return this.$t("salaryTypeMonthly") || "شهري";
      return "";
    },
    async loadEmployees() {
      try {
        this.loadingEmployees = true;
        const response = await HTTP.get('Employees');
        if (response.data && !response.data.errorStatus) {
          this.employees = response.data.data || [];
        } else {
          this.employees = [];
        }
      } catch (error) {
        console.error('Error loading employees:', error);
        this.employees = [];
        this.$notify.error(this.$i18n.t("failedToLoadEmployees") || 'فشل تحميل الموظفين', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.loadingEmployees = false;
      }
    },
    async loadTags() {
      try {
        this.loadingTags = true;
        const response = await HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=500&info=`);
        if (response.data && response.data.data && response.data.data.items) {
          this.tags = response.data.data.items;
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
    openAddModal() {
      this.selectedEmployee = null;
      this.resetEmployeeForm();
      this.showEmployeeModal = true;
    },
    editEmployee(emp) {
      this.selectedEmployee = emp;
      this.employeeForm = {
        name: emp.name || '',
        phoneNumber: emp.phoneNumber || '',
        address: emp.address || '',
        jobTitle: emp.jobTitle || '',
        salary: emp.salary != null ? Number(emp.salary) : 0,
        salaryType: emp.salaryType != null ? Number(emp.salaryType) : 0,
        tagId: emp.tagId != null && emp.tagId !== '' ? emp.tagId : '',
        isActive: emp.isActive !== false
      };
      this.showEmployeeModal = true;
    },
    async saveEmployee() {
      if (!this.employeeForm.name || !this.employeeForm.name.trim()) {
        this.$notify.warning(this.$i18n.t("pleaseEnterEmployeeName") || 'يرجى إدخال اسم الموظف', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }
      if (!this.employeeForm.phoneNumber || !this.employeeForm.phoneNumber.trim()) {
        this.$notify.warning(this.$i18n.t("pleaseEnterPhoneNumber") || 'يرجى إدخال رقم الهاتف', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }
      try {
        this.savingEmployee = true;
        const payload = {
          name: this.employeeForm.name.trim(),
          phoneNumber: this.employeeForm.phoneNumber.trim(),
          address: this.employeeForm.address?.trim() || null,
          jobTitle: this.employeeForm.jobTitle?.trim() || null,
          salary: Number(this.employeeForm.salary),
          salaryType: Number(this.employeeForm.salaryType),
          tagId: (this.employeeForm.tagId != null && this.employeeForm.tagId !== '') ? Number(this.employeeForm.tagId) : null,
          isActive: this.employeeForm.isActive !== false
        };
        let response;
        if (this.selectedEmployee) {
          response = await HTTP.put(`Employees/${this.selectedEmployee.id}`, payload);
        } else {
          response = await HTTP.post('Employees', payload);
        }
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(
            this.selectedEmployee
              ? (this.$i18n.t("employeeUpdatedSuccess") || 'تم تحديث الموظف بنجاح')
              : (this.$i18n.t("employeeAddedSuccess") || 'تم إضافة الموظف بنجاح'),
            {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            }
          );
          this.showEmployeeModal = false;
          this.resetEmployeeForm();
          this.loadEmployees();
        } else {
          this.$notify.error(response.data?.message || (this.selectedEmployee ? this.$i18n.t("employeeUpdateFailed") : this.$i18n.t("employeeAddFailed")) || 'فشل حفظ الموظف', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error saving employee:', error);
        this.$notify.error(error.response?.data?.message || (this.selectedEmployee ? this.$i18n.t("employeeUpdateFailed") : this.$i18n.t("employeeAddFailed")) || 'حدث خطأ أثناء حفظ الموظف', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingEmployee = false;
      }
    },
    async confirmDeleteEmployee(emp) {
      const ok = await this.$confirm({
        message: this.$t("confirmDeleteEmployee", { name: emp.name || "" }),
      });
      if (ok) {
        this.deleteEmployee(emp.id);
      }
    },
    async deleteEmployee(id) {
      try {
        const response = await HTTP.delete(`Employees/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(this.$i18n.t("employeeDeletedSuccess") || 'تم حذف الموظف بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.loadEmployees();
        } else {
          this.$notify.error(response.data?.message || this.$i18n.t("employeeDeleteFailed") || 'فشل حذف الموظف', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting employee:', error);
        this.$notify.error(error.response?.data?.message || this.$i18n.t("employeeDeleteFailed") || 'حدث خطأ أثناء حذف الموظف', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    resetEmployeeForm() {
      this.selectedEmployee = null;
      this.employeeForm = {
        name: '',
        phoneNumber: '',
        address: '',
        jobTitle: '',
        salary: 0,
        salaryType: 0,
        tagId: '',
        isActive: true
      };
    },
    formatPrice(price) {
      if (price != null && price !== '') {
        const n = Number(price);
        if (Number.isNaN(n)) return '0';
        return n.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
      }
      return '0';
    },
  },
};
</script>

<style scoped>
.modal-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.employees-form-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.employees-form-actions-main {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  flex: 1 1 auto;
  justify-content: flex-end;
  min-width: min(100%, 18rem);
}

.employees-form-actions-main .users-form-cancel-button,
.employees-form-actions-main .users-form-submit-button {
  flex: 1 1 8rem;
  min-width: 8rem;
}

.employees-payroll-btn {
  flex: 0 0 auto;
  width: auto;
  min-width: 0;
  padding-inline: 1.1rem;
  white-space: nowrap;
  border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
  color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 8%, var(--bg-primary));
}

.employees-payroll-btn:hover {
  background: color-mix(in srgb, var(--primary-color) 16%, var(--bg-primary));
  border-color: var(--primary-color);
  color: var(--primary-color);
}

@media (max-width: 768px) {
  .modal-form-grid {
    grid-template-columns: 1fr;
  }

  .employees-form-actions {
    flex-direction: column-reverse;
    align-items: stretch;
  }

  .employees-form-actions-main {
    width: 100%;
    justify-content: stretch;
  }

  .employees-payroll-btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
