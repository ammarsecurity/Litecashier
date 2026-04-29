<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
    <div class="employees-page-container">
      <div class="employees-page-content">
        <div class="users-header-section">
          <div class="users-header-content">
            <div class="header-title-wrapper">
              <div class="header-icon-wrapper">
                <b-icon icon="person-badge-fill" class="header-icon"></b-icon>
              </div>
              <div>
                <h1 class="users-page-title">{{ $t("employeesManagement") || "إدارة الموظفين" }}</h1>
                <p class="header-subtitle">{{ $t("employeesManagementDescription") || "إدارة بيانات الموظفين والرواتب والأقسام" }}</p>
              </div>
            </div>
            <button
              class="users-add-button btn-add-employee-header"
              @click="openAddModal"
            >
              <b-icon icon="plus-circle" class="me-1"></b-icon>
              {{ $t("addEmployee") || "إضافة موظف" }}
            </button>
          </div>
        </div>

        <div class="employees-management-card">
          <div class="employees-management-header">
            <div class="employees-management-header-content">
              <div class="employees-management-title-wrapper">
                <div class="employees-management-icon-wrapper">
                  <b-icon icon="people-fill" class="employees-management-icon"></b-icon>
                </div>
                <div>
                  <h3 class="employees-management-title">
                    {{ $t("employees") || "الموظفون" }}
                  </h3>
                  <p class="employees-management-subtitle">
                    {{ $t("employeesListDescription") || "قائمة الموظفين مع البيانات والراتب والقسم" }}
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div class="employees-management-body">
            <div v-if="loadingEmployees" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="employees.length > 0" class="employees-grid">
              <div
                v-for="emp in employees"
                :key="emp.id"
                class="employee-card"
              >
                <div class="employee-card-header">
                  <div class="employee-card-title">
                    <b-icon icon="person-circle" class="employee-card-icon"></b-icon>
                    <h4>{{ emp.name }}</h4>
                  </div>
                  <div class="employee-card-actions">
                    <button
                      class="btn-edit-employee"
                      @click="editEmployee(emp)"
                      :title="$t('edit') || 'تعديل'"
                    >
                      <b-icon icon="pencil"></b-icon>
                    </button>
                    <button
                      class="btn-delete-employee"
                      @click="confirmDeleteEmployee(emp)"
                      :title="$t('delete') || 'حذف'"
                    >
                      <b-icon icon="trash"></b-icon>
                    </button>
                  </div>
                </div>
                <div class="employee-card-body">
                  <div class="employee-info-item">
                    <b-icon icon="telephone" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("phoneNumber") || "رقم الهاتف:" }}</span>
                    <span class="info-value">{{ emp.phoneNumber }}</span>
                  </div>
                  <div class="employee-info-item" v-if="emp.address">
                    <b-icon icon="geo-alt" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("address") || "العنوان:" }}</span>
                    <span class="info-value">{{ emp.address }}</span>
                  </div>
                  <div class="employee-info-item" v-if="emp.jobTitle">
                    <b-icon icon="briefcase-fill" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("jobTitle") || "المسمى الوظيفي:" }}</span>
                    <span class="info-value">{{ emp.jobTitle }}</span>
                  </div>
                  <div class="employee-info-item">
                    <b-icon icon="cash-stack" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("salary") || "الراتب:" }}</span>
                    <span class="info-value">{{ formatPrice(emp.salary) }} ({{ salaryTypeLabel(emp.salaryType) }})</span>
                  </div>
                  <div class="employee-info-item" v-if="emp.tag">
                    <b-icon icon="tags-fill" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("category") || "القسم:" }}</span>
                    <span class="info-value">{{ emp.tag.name }}</span>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="people" class="empty-icon"></b-icon>
              <p>{{ $t("noEmployees") || "لا يوجد موظفون" }}</p>
              <button
                class="btn-add-first-employee"
                @click="openAddModal"
              >
                <b-icon icon="plus-circle" class="me-2"></b-icon>
                {{ $t("addFirstEmployee") || "إضافة أول موظف" }}
              </button>
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
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showEmployeeModal = false" :disabled="savingEmployee">
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="savingEmployee">
              <b-spinner small v-if="savingEmployee" class="me-2"></b-spinner>
              {{ savingEmployee ? (selectedEmployee ? ($t("updating") || "جاري التحديث...") : ($t("adding") || "جاري الإضافة...")) : (selectedEmployee ? ($t("update") || "تحديث") : ($t("add") || "إضافة")) }}
            </button>
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
        tagId: ''
      },
    };
  },
  mounted() {
    this.loadEmployees();
    this.loadTags();
  },
  methods: {
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
        this.$toast.error(this.$i18n.t("failedToLoadEmployees") || 'فشل تحميل الموظفين', {
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
        tagId: emp.tagId != null && emp.tagId !== '' ? emp.tagId : ''
      };
      this.showEmployeeModal = true;
    },
    async saveEmployee() {
      if (!this.employeeForm.name || !this.employeeForm.name.trim()) {
        this.$toast.warning(this.$i18n.t("pleaseEnterEmployeeName") || 'يرجى إدخال اسم الموظف', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }
      if (!this.employeeForm.phoneNumber || !this.employeeForm.phoneNumber.trim()) {
        this.$toast.warning(this.$i18n.t("pleaseEnterPhoneNumber") || 'يرجى إدخال رقم الهاتف', {
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
          tagId: (this.employeeForm.tagId != null && this.employeeForm.tagId !== '') ? Number(this.employeeForm.tagId) : null
        };
        let response;
        if (this.selectedEmployee) {
          response = await HTTP.put(`Employees/${this.selectedEmployee.id}`, payload);
        } else {
          response = await HTTP.post('Employees', payload);
        }
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(
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
          this.$toast.error(response.data?.message || (this.selectedEmployee ? this.$i18n.t("employeeUpdateFailed") : this.$i18n.t("employeeAddFailed")) || 'فشل حفظ الموظف', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error saving employee:', error);
        this.$toast.error(error.response?.data?.message || (this.selectedEmployee ? this.$i18n.t("employeeUpdateFailed") : this.$i18n.t("employeeAddFailed")) || 'حدث خطأ أثناء حفظ الموظف', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingEmployee = false;
      }
    },
    confirmDeleteEmployee(emp) {
      if (confirm(this.$i18n.t("confirmDeleteEmployee") || `هل أنت متأكد من حذف الموظف "${emp.name}"؟`)) {
        this.deleteEmployee(emp.id);
      }
    },
    async deleteEmployee(id) {
      try {
        const response = await HTTP.delete(`Employees/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("employeeDeletedSuccess") || 'تم حذف الموظف بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.loadEmployees();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("employeeDeleteFailed") || 'فشل حذف الموظف', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting employee:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("employeeDeleteFailed") || 'حدث خطأ أثناء حذف الموظف', {
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
        tagId: ''
      };
    },
    formatPrice(price) {
      if (price != null && price !== '') {
        return Number(price).toLocaleString("en-EG");
      }
      return "0";
    },
  },
};
</script>

<style scoped>
.employees-page-container {
  padding: 2rem;
  min-height: 100vh;
  background: var(--bg-primary, #f5f5f5);
}

.employees-page-content {
  max-width: 1400px;
  margin: 0 auto;
}

.btn-add-employee-header {
  background: var(--primary-color, #007bff);
  color: #ffffff;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: var(--radius-md, 8px);
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.3s ease;
}

.btn-add-employee-header:hover {
  background: var(--primary-hover, #0056b3);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md, 0 4px 8px rgba(0,0,0,0.15));
}

.employees-management-card {
  background: var(--bg-primary);
  border-radius: 1rem;
  padding: 0;
  margin-bottom: 2rem;
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-color);
  overflow: hidden;
}

.employees-management-header {
  padding: 1.5rem;
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-color);
}

.employees-management-header-content {
  width: 100%;
}

.employees-management-title-wrapper {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.employees-management-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 0.75rem;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.15) 0%, rgba(167, 139, 250, 0.15) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.employees-management-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.employees-management-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 0.25rem 0;
  line-height: 1.2;
}

.employees-management-subtitle {
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1.4;
}

.employees-management-body {
  padding: 1.5rem;
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem;
  color: var(--text-secondary);
}

.employees-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.employee-card {
  background: var(--bg-secondary);
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  padding: 1.25rem;
  transition: all 0.3s ease;
}

.employee-card:hover {
  border-color: var(--primary-color);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.employee-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.employee-card-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.employee-card-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.employee-card-title h4 {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
}

.employee-card-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-edit-employee,
.btn-delete-employee {
  padding: 0.4rem;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.btn-edit-employee {
  background: rgba(0, 123, 255, 0.1);
  color: #007bff;
}

.btn-edit-employee:hover {
  background: rgba(0, 123, 255, 0.2);
}

.btn-delete-employee {
  background: rgba(220, 53, 69, 0.1);
  color: #dc3545;
}

.btn-delete-employee:hover {
  background: rgba(220, 53, 69, 0.2);
}

.employee-card-body {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.employee-info-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
}

.info-icon {
  color: var(--text-secondary);
  flex-shrink: 0;
}

.info-label {
  color: var(--text-secondary);
  min-width: 100px;
}

.info-value {
  color: var(--text-primary);
}

.empty-state {
  text-align: center;
  padding: 3rem 2rem;
}

.empty-icon {
  font-size: 4rem;
  color: var(--text-secondary);
  margin-bottom: 1rem;
  opacity: 0.5;
}

.empty-state p {
  color: var(--text-secondary);
  margin-bottom: 1.5rem;
}

.btn-add-first-employee {
  background: var(--primary-color, #007bff);
  color: #fff;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: var(--radius-md, 8px);
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
}

.btn-add-first-employee:hover {
  background: var(--primary-hover, #0056b3);
}

.modal-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

@media (max-width: 768px) {
  .modal-form-grid {
    grid-template-columns: 1fr;
  }
  .employees-grid {
    grid-template-columns: 1fr;
  }
}
</style>
