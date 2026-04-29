<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
    <div class="delivery-drivers-page-container">
      <div class="delivery-drivers-page-content">
        <!-- Header Section -->
        <div class="users-header-section">
          <div class="users-header-content">
            <div class="header-title-wrapper">
              <div class="header-icon-wrapper">
                <b-icon icon="truck" class="header-icon"></b-icon>
              </div>
              <div>
                <h1 class="users-page-title">{{ $t("deliveryDriversManagement") || "إدارة سائقي التوصيل" }}</h1>
                <p class="header-subtitle">{{ $t("deliveryDriversDescription") || "إدارة سائقي التوصيل ومتابعة الطلبات" }}</p>
              </div>
            </div>
            <button 
              class="users-add-button btn-add-driver-header" 
              @click="showAddDriverModal = true"
            >
              <b-icon icon="plus-circle" class="me-1"></b-icon>
              {{ $t("addDeliveryDriver") || "إضافة سائق" }}
            </button>
          </div>
        </div>

        <!-- Statistics Card -->
        <div class="delivery-statistics-card">
          <div class="delivery-statistics-header">
            <div class="delivery-statistics-header-content">
              <div class="delivery-statistics-title-wrapper">
                <div class="delivery-statistics-icon-wrapper">
                  <b-icon icon="graph-up" class="delivery-statistics-icon"></b-icon>
                </div>
                <div>
                  <h3 class="delivery-statistics-title">
                    {{ $t("deliveryStatistics") || "إحصائيات التوصيل" }}
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
          <div class="delivery-statistics-body">
            <div v-if="loadingStatistics" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="statistics" class="statistics-grid">
              <div class="stat-card">
                <div class="stat-icon total">
                  <b-icon icon="truck"></b-icon>
                </div>
                <div class="stat-content">
                  <div class="stat-value">{{ statistics.totalDrivers || 0 }}</div>
                  <div class="stat-label">{{ $t("totalDrivers") || "إجمالي السائقين" }}</div>
                </div>
              </div>
              <div class="stat-card">
                <div class="stat-icon active">
                  <b-icon icon="check-circle"></b-icon>
                </div>
                <div class="stat-content">
                  <div class="stat-value">{{ statistics.activeDrivers || 0 }}</div>
                  <div class="stat-label">{{ $t("activeDrivers") || "السائقين النشطين" }}</div>
                </div>
              </div>
              <div class="stat-card">
                <div class="stat-icon orders">
                  <b-icon icon="clipboard-check"></b-icon>
                </div>
                <div class="stat-content">
                  <div class="stat-value">{{ statistics.totalOrders || 0 }}</div>
                  <div class="stat-label">{{ $t("totalDeliveries") || "إجمالي التوصيلات" }}</div>
                </div>
              </div>
              <div class="stat-card">
                <div class="stat-icon delivered">
                  <b-icon icon="check2-circle"></b-icon>
                </div>
                <div class="stat-content">
                  <div class="stat-value">{{ statistics.deliveredOrders || 0 }}</div>
                  <div class="stat-label">{{ $t("deliveredOrders") || "الطلبات الواصلة" }}</div>
                </div>
              </div>
              <div class="stat-card">
                <div class="stat-icon pending">
                  <b-icon icon="clock-history"></b-icon>
                </div>
                <div class="stat-content">
                  <div class="stat-value">{{ statistics.pendingOrders || 0 }}</div>
                  <div class="stat-label">{{ $t("pendingDeliveries") || "التوصيلات المعلقة" }}</div>
                </div>
              </div>
              <div class="stat-card">
                <div class="stat-icon failed">
                  <b-icon icon="x-circle"></b-icon>
                </div>
                <div class="stat-content">
                  <div class="stat-value">{{ statistics.failedOrders || 0 }}</div>
                  <div class="stat-label">{{ $t("failedDeliveries") || "التوصيلات الفاشلة" }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Drivers Management Card -->
        <div class="drivers-management-card">
          <div class="drivers-management-header">
            <div class="drivers-management-header-content">
              <div class="drivers-management-title-wrapper">
                <div class="drivers-management-icon-wrapper">
                  <b-icon icon="people-fill" class="drivers-management-icon"></b-icon>
                </div>
                <div>
                  <h3 class="drivers-management-title">
                    {{ $t("deliveryDrivers") || "سائقي التوصيل" }}
                  </h3>
                  <p class="drivers-management-subtitle">
                    {{ $t("deliveryDriversDescription") || "إدارة ومتابعة سائقي التوصيل" }}
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div class="drivers-management-body">
            <div v-if="loadingDrivers" class="loading-state">
              <b-spinner small></b-spinner>
              <span>{{ $t("loading") || "جاري التحميل..." }}</span>
            </div>
            <div v-else-if="drivers.length > 0" class="drivers-grid">
              <div 
                v-for="driver in drivers" 
                :key="driver.id"
                class="driver-card"
              >
                <div class="driver-card-header">
                  <div class="driver-card-title">
                    <b-icon icon="person-circle" class="driver-card-icon"></b-icon>
                    <h4>{{ driver.name }}</h4>
                  
                  </div>
                  <div v-if="!driver.isActive" class="status-icon-badge inactive-badge-icon m-2" :title="$t('inactive') || 'غير مفعل'">
                      <b-icon icon="x-circle-fill"></b-icon>
                    </div>
                    <div v-else class="status-icon-badge active-badge-icon m-2" :title="$t('active') || 'نشط'">
                      <b-icon icon="check-circle-fill"></b-icon>
                    </div>
                  <div class="driver-card-actions">
                    <button 
                      class="btn-view-stats"
                      @click="viewDriverStatistics(driver.id)"
                      :title="$t('viewStatistics') || 'عرض الإحصائيات'"
                    >
                      <b-icon icon="graph-up"></b-icon>
                    </button>
                    <button 
                      class="btn-edit-driver"
                      @click="editDriver(driver)"
                      :title="$t('edit') || 'تعديل'"
                    >
                      <b-icon icon="pencil"></b-icon>
                    </button>
                    <button 
                      class="btn-delete-driver"
                      @click="confirmDeleteDriver(driver)"
                      :title="$t('delete') || 'حذف'"
                    >
                      <b-icon icon="trash"></b-icon>
                    </button>
                  </div>
                </div>
                <div class="driver-card-body">
                  <div class="driver-info-item">
                    <b-icon icon="telephone" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("phoneNumber") || "رقم الهاتف:" }}</span>
                    <span class="info-value">{{ driver.phoneNumber }}</span>
                  </div>
                  <div class="driver-info-item" v-if="driver.address">
                    <b-icon icon="geo-alt" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("address") || "العنوان:" }}</span>
                    <span class="info-value">{{ driver.address }}</span>
                  </div>
                  <div class="driver-info-item" v-if="driver.vehicleType">
                    <b-icon icon="car-front" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("vehicleType") || "نوع المركبة:" }}</span>
                    <span class="info-value">{{ driver.vehicleType }}</span>
                  </div>
                  <div class="driver-info-item" v-if="driver.vehicleNumber">
                    <b-icon icon="123" class="info-icon"></b-icon>
                    <span class="info-label">{{ $t("vehicleNumber") || "رقم المركبة:" }}</span>
                    <span class="info-value">{{ driver.vehicleNumber }}</span>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="empty-state">
              <b-icon icon="truck" class="empty-icon"></b-icon>
              <p>{{ $t("noDriversConfigured") || "لم يتم إعداد أي سائقين" }}</p>
              <button 
                class="btn-add-first-driver"
                @click="showAddDriverModal = true"
              >
                <b-icon icon="plus-circle" class="me-2"></b-icon>
                {{ $t("addFirstDriver") || "إضافة أول سائق" }}
              </button>
            </div>
          </div>
        </div>

      </div>
    </div>

    <!-- Add/Edit Driver Modal -->
    <b-modal 
      v-model="showAddDriverModal" 
      :title="selectedDriver ? ($t('editDeliveryDriver') || 'تعديل سائق') : ($t('addDeliveryDriver') || 'إضافة سائق')" 
      @hidden="resetDriverForm"
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ selectedDriver ? ($t('editDeliveryDriver') || 'تعديل سائق') : ($t('addDeliveryDriver') || 'إضافة سائق') }}</h2>
        <form @submit.prevent="saveDriver" class="users-form">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                {{ $t("driverName") || "اسم السائق" }} <span class="required">*</span>
              </label>
              <input 
                v-model="driverForm.name" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterDriverName') || 'أدخل اسم السائق'"
                required
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                {{ $t("driverPhone") || "رقم الهاتف" }} <span class="required">*</span>
              </label>
              <input 
                v-model="driverForm.phoneNumber" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterDriverPhone') || 'أدخل رقم الهاتف'"
                required
              />
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
              {{ $t("driverAddress") || "العنوان" }}
            </label>
            <textarea 
              v-model="driverForm.address" 
              class="users-form-input"
              rows="2"
              :placeholder="$t('enterDriverAddress') || 'أدخل عنوان السائق (اختياري)'"
            ></textarea>
          </div>
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="car-front-fill" class="form-label-icon"></b-icon>
                {{ $t("vehicleType") || "نوع المركبة" }}
              </label>
              <input 
                v-model="driverForm.vehicleType" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterVehicleType') || 'مثال: دراجة، سيارة'"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="123" class="form-label-icon"></b-icon>
                {{ $t("vehicleNumber") || "رقم المركبة" }}
              </label>
              <input 
                v-model="driverForm.vehicleNumber" 
                type="text" 
                class="users-form-input"
                :placeholder="$t('enterVehicleNumber') || 'أدخل رقم المركبة'"
              />
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="file-text-fill" class="form-label-icon"></b-icon>
              {{ $t("notes") || "ملاحظات" }}
            </label>
            <textarea 
              v-model="driverForm.notes" 
              class="users-form-input"
              rows="2"
              :placeholder="$t('enterNotes') || 'أدخل ملاحظات (اختياري)'"
            ></textarea>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="check-circle-fill" class="form-label-icon"></b-icon>
              {{ $t("active") || "مفعل" }}
            </label>
            <div class="users-form-checkbox">
              <input 
                type="checkbox" 
                v-model="driverForm.isActive"
                id="driver-active"
                class="users-form-checkbox-input"
              />
              <label for="driver-active" class="users-form-checkbox-label">
                {{ $t("active") || "مفعل" }}
              </label>
            </div>
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showAddDriverModal = false" :disabled="savingDriver">
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="savingDriver">
              <b-spinner small v-if="savingDriver" class="me-2"></b-spinner>
              {{ savingDriver ? (selectedDriver ? ($t("updating") || "جاري التحديث...") : ($t("adding") || "جاري الإضافة...")) : (selectedDriver ? ($t("update") || "تحديث") : ($t("add") || "إضافة")) }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <!-- Driver Statistics Modal -->
    <b-modal 
      v-model="showStatisticsModal" 
      :title="selectedDriverStats ? ($t('driverStatistics') || 'إحصائيات السائق') + ': ' + selectedDriverStats.driverName : ($t('driverStatistics') || 'إحصائيات السائق')" 
      hide-header 
      hide-footer 
      class="users-modal" 
      centered
      size="lg"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ selectedDriverStats ? ($t('driverStatistics') || 'إحصائيات السائق') + ': ' + selectedDriverStats.driverName : ($t('driverStatistics') || 'إحصائيات السائق') }}</h2>
        <div v-if="selectedDriverStats" class="driver-statistics-content">
          <div class="statistics-detail-grid">
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("totalOrders") || "إجمالي الطلبات" }}</div>
              <div class="stat-detail-value">{{ selectedDriverStats.totalOrders || 0 }}</div>
            </div>
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("deliveredOrders") || "الطلبات الواصلة" }}</div>
              <div class="stat-detail-value success">{{ selectedDriverStats.deliveredOrders || 0 }}</div>
            </div>
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("pendingDeliveries") || "التوصيلات المعلقة" }}</div>
              <div class="stat-detail-value warning">{{ selectedDriverStats.pendingOrders || 0 }}</div>
            </div>
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("failedDeliveries") || "التوصيلات الفاشلة" }}</div>
              <div class="stat-detail-value danger">{{ selectedDriverStats.failedOrders || 0 }}</div>
            </div>
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("completedDeliveries") || "التوصيلات المكتملة" }}</div>
              <div class="stat-detail-value success">{{ selectedDriverStats.completedOrders || 0 }}</div>
            </div>
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("totalAmount") || "إجمالي المبلغ" }}</div>
              <div class="stat-detail-value">{{ formatPrice(selectedDriverStats.totalAmount || 0) }} د.ع</div>
            </div>
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("paidAmount") || "المبلغ المدفوع" }}</div>
              <div class="stat-detail-value success">{{ formatPrice(selectedDriverStats.paidAmount || 0) }} د.ع</div>
            </div>
            <div class="stat-detail-card">
              <div class="stat-detail-label">{{ $t("remainingAmount") || "المبلغ المتبقي" }}</div>
              <div class="stat-detail-value warning">{{ formatPrice(selectedDriverStats.remainingAmount || 0) }} د.ع</div>
            </div>
          </div>
        </div>
        <div class="users-form-actions">
          <button type="button" class="users-form-cancel-button" @click="showStatisticsModal = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </div>
      </div>
    </b-modal>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from '../http/api.js';

export default {
  name: "DeliveryDriversView",
  components: {
    AppHeader,
  },
  data() {
    return {
      loadingDrivers: false,
      loadingStatistics: false,
      drivers: [],
      statistics: null,
      showAddDriverModal: false,
      showStatisticsModal: false,
      savingDriver: false,
      selectedDriver: null,
      selectedDriverStats: null,
      driverForm: {
        name: '',
        phoneNumber: '',
        address: '',
        vehicleType: '',
        vehicleNumber: '',
        notes: '',
        isActive: true
      },
    };
  },
  mounted() {
    this.loadDrivers();
    this.loadStatistics();
  },
  methods: {
    async loadDrivers() {
      try {
        this.loadingDrivers = true;
        const response = await HTTP.get('DeliveryDrivers');
        if (response.data && !response.data.errorStatus) {
          this.drivers = response.data.data || [];
        } else {
          this.drivers = [];
        }
      } catch (error) {
        console.error('Error loading drivers:', error);
        this.drivers = [];
        this.$toast.error(this.$i18n.t("failedToLoadDrivers") || 'فشل تحميل السائقين', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.loadingDrivers = false;
      }
    },
    async loadStatistics() {
      try {
        this.loadingStatistics = true;
        const response = await HTTP.get('DeliveryDrivers/Statistics/All');
        if (response.data && !response.data.errorStatus) {
          this.statistics = response.data.data || null;
        } else {
          this.statistics = null;
        }
      } catch (error) {
        console.error('Error loading statistics:', error);
        this.statistics = null;
      } finally {
        this.loadingStatistics = false;
      }
    },
    async viewDriverStatistics(driverId) {
      try {
        const response = await HTTP.get(`DeliveryDrivers/${driverId}/Statistics`);
        if (response.data && !response.data.errorStatus) {
          this.selectedDriverStats = response.data.data;
          this.showStatisticsModal = true;
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("failedToLoadStatistics") || 'فشل تحميل الإحصائيات', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error loading driver statistics:', error);
        this.$toast.error(this.$i18n.t("failedToLoadStatistics") || 'فشل تحميل الإحصائيات', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    async saveDriver() {
      if (!this.driverForm.name || !this.driverForm.name.trim()) {
        this.$toast.warning(this.$i18n.t("pleaseEnterDriverName") || 'يرجى إدخال اسم السائق', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }
      if (!this.driverForm.phoneNumber || !this.driverForm.phoneNumber.trim()) {
        this.$toast.warning(this.$i18n.t("pleaseEnterDriverPhone") || 'يرجى إدخال رقم هاتف السائق', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
        return;
      }

      try {
        this.savingDriver = true;
        let response;
        if (this.selectedDriver) {
          response = await HTTP.put(`DeliveryDrivers/${this.selectedDriver.id}`, this.driverForm);
        } else {
          response = await HTTP.post('DeliveryDrivers', this.driverForm);
        }

        if (response.data && !response.data.errorStatus) {
          this.$toast.success(
            this.selectedDriver 
              ? (this.$i18n.t("driverUpdatedSuccess") || 'تم تحديث السائق بنجاح')
              : (this.$i18n.t("driverAddedSuccess") || 'تم إضافة السائق بنجاح'),
            {
              position: "top-right",
              timeout: 3000,
              rtl: this.$i18n.locale === 'ar'
            }
          );
          this.showAddDriverModal = false;
          this.resetDriverForm();
          this.loadDrivers();
          this.loadStatistics();
        } else {
          this.$toast.error(response.data?.message || (this.selectedDriver ? this.$i18n.t("driverUpdateFailed") : this.$i18n.t("driverAddFailed")) || 'فشل حفظ السائق', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error saving driver:', error);
        this.$toast.error(error.response?.data?.message || (this.selectedDriver ? this.$i18n.t("driverUpdateFailed") : this.$i18n.t("driverAddFailed")) || 'حدث خطأ أثناء حفظ السائق', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      } finally {
        this.savingDriver = false;
      }
    },
    editDriver(driver) {
      this.selectedDriver = driver;
      this.driverForm = {
        name: driver.name || '',
        phoneNumber: driver.phoneNumber || '',
        address: driver.address || '',
        vehicleType: driver.vehicleType || '',
        vehicleNumber: driver.vehicleNumber || '',
        notes: driver.notes || '',
        isActive: driver.isActive !== undefined ? driver.isActive : true
      };
      this.showAddDriverModal = true;
    },
    confirmDeleteDriver(driver) {
      if (confirm(this.$i18n.t("confirmDeleteDriver") || `هل أنت متأكد من حذف السائق "${driver.name}"؟`)) {
        this.deleteDriver(driver.id);
      }
    },
    async deleteDriver(id) {
      try {
        const response = await HTTP.delete(`DeliveryDrivers/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$i18n.t("driverDeletedSuccess") || 'تم حذف السائق بنجاح', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
          this.loadDrivers();
          this.loadStatistics();
        } else {
          this.$toast.error(response.data?.message || this.$i18n.t("driverDeleteFailed") || 'فشل حذف السائق', {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === 'ar'
          });
        }
      } catch (error) {
        console.error('Error deleting driver:', error);
        this.$toast.error(error.response?.data?.message || this.$i18n.t("driverDeleteFailed") || 'حدث خطأ أثناء حذف السائق', {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === 'ar'
        });
      }
    },
    resetDriverForm() {
      this.selectedDriver = null;
      this.driverForm = {
        name: '',
        phoneNumber: '',
        address: '',
        vehicleType: '',
        vehicleNumber: '',
        notes: '',
        isActive: true
      };
    },
    formatPrice(price) {
      if (price) {
        return price.toLocaleString("en-EG");
      }
      return "0";
    },
  },
};
</script>

<style scoped>
.delivery-drivers-page-container {
  padding: 2rem;
  min-height: 100vh;
  background: var(--bg-primary, #f5f5f5);
}

.delivery-drivers-page-content {
  max-width: 1400px;
  margin: 0 auto;
}

.btn-add-driver-header {
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

.btn-add-driver-header:hover {
  background: var(--primary-hover, #0056b3);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md, 0 4px 8px rgba(0,0,0,0.15));
}

.delivery-statistics-card,
.drivers-management-card {
  background: var(--bg-primary);
  border-radius: 1rem;
  padding: 0;
  margin-bottom: 2rem;
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-color);
  overflow: hidden;
}

.delivery-statistics-header {
  padding: 1.5rem;
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-color);
}

.delivery-statistics-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.delivery-statistics-title-wrapper {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.delivery-statistics-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 0.75rem;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.15) 0%, rgba(167, 139, 250, 0.15) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.delivery-statistics-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.drivers-management-header {
  padding: 1.5rem;
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-color);
}

.drivers-management-header-content {
  width: 100%;
}

.drivers-management-title-wrapper {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.drivers-management-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 0.75rem;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.15) 0%, rgba(167, 139, 250, 0.15) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.drivers-management-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.delivery-statistics-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.delivery-statistics-body {
  padding: 1.5rem;
}

.drivers-management-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 0.25rem 0;
  line-height: 1.2;
}

.drivers-management-subtitle {
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1.4;
}

.drivers-management-body {
  padding: 1.5rem;
}

.btn-refresh {
  background: var(--bg-tertiary, #f8f9fa);
  border: 1px solid var(--border-color, #dee2e6);
  padding: 0.5rem 1rem;
  border-radius: var(--radius-sm, 4px);
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: all 0.3s ease;
}

.btn-refresh:hover {
  background: var(--bg-secondary, #e9ecef);
}

.btn-refresh .spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
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
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
}

.stat-icon.total {
  background: rgba(0, 123, 255, 0.1);
  color: #007bff;
}

.stat-icon.active {
  background: rgba(40, 167, 69, 0.1);
  color: #28a745;
}

.stat-icon.orders {
  background: rgba(108, 117, 125, 0.1);
  color: #6c757d;
}

.stat-icon.delivered {
  background: rgba(40, 167, 69, 0.1);
  color: #28a745;
}

.stat-icon.pending {
  background: rgba(255, 193, 7, 0.1);
  color: #ffc107;
}

.stat-icon.failed {
  background: rgba(220, 53, 69, 0.1);
  color: #dc3545;
}

.stat-content {
  flex: 1;
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 700;
  color: var(--text-primary, #212529);
  margin-bottom: 0.25rem;
}

.stat-label {
  font-size: 0.875rem;
  color: var(--text-secondary, #6c757d);
}

.drivers-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

@media (max-width: 768px) {
  .drivers-grid {
    grid-template-columns: 1fr;
  }
}

.driver-card {
  background: var(--bg-primary);
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  overflow: hidden;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  height: 100%;
}

.driver-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-md);
  border-color: var(--primary-color);
}

.driver-card-header {
  background: var(--bg-secondary);
  padding: 1.25rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid var(--border-color);
}

.driver-card-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  flex-wrap: wrap;
}

.driver-card-icon {
  font-size: 1.75rem;
  color: var(--primary-color);
  flex-shrink: 0;
}

.driver-card-title h4 {
  margin: 0;
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
  flex: 1;
  min-width: 120px;
}

.status-icon-badge {
  width: 40px;
  height: 40px;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid;
  font-size: 1.125rem;
  flex-shrink: 0;
  transition: all 0.3s ease;
}

.active-badge-icon {
  background: rgba(34, 197, 94, 0.1);
  color: var(--success-color);
  border-color: rgba(34, 197, 94, 0.3);
}

.active-badge-icon:hover {
  background: rgba(34, 197, 94, 0.2);
  border-color: var(--success-color);
  transform: scale(1.05);
}

.inactive-badge-icon {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
  border-color: rgba(239, 68, 68, 0.3);
}

.inactive-badge-icon:hover {
  background: rgba(239, 68, 68, 0.2);
  border-color: var(--danger-color);
  transform: scale(1.05);
}

.driver-card-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-view-stats,
.btn-edit-driver,
.btn-delete-driver {
  width: 40px;
  height: 40px;
  border: 2px solid;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s ease;
  font-size: 1rem;
}

.btn-view-stats {
  background: rgba(59, 130, 246, 0.1);
  color: var(--info-color);
  border-color: rgba(59, 130, 246, 0.3);
}

.btn-view-stats:hover {
  background: rgba(59, 130, 246, 0.2);
  border-color: var(--info-color);
  transform: scale(1.05);
}

.btn-edit-driver {
  background: rgba(251, 191, 36, 0.1);
  color: #fbbf24;
  border-color: rgba(251, 191, 36, 0.3);
}

.btn-edit-driver:hover {
  background: rgba(251, 191, 36, 0.2);
  border-color: #fbbf24;
  transform: scale(1.05);
}

.btn-delete-driver {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
  border-color: rgba(239, 68, 68, 0.3);
}

.btn-delete-driver:hover {
  background: rgba(239, 68, 68, 0.2);
  border-color: var(--danger-color);
  transform: scale(1.05);
}

.driver-card-body {
  padding: 1.25rem;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.driver-info-item {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.75rem;
  background: var(--bg-secondary);
  border-radius: 0.5rem;
  border: 1px solid var(--border-color);
  transition: all 0.2s ease;
}

.driver-info-item:hover {
  background: var(--bg-tertiary);
  border-color: var(--primary-color);
}

.info-icon {
  color: var(--primary-color);
  font-size: 1.125rem;
  flex-shrink: 0;
  margin-top: 0.125rem;
}

.info-label {
  font-weight: 600;
  color: var(--text-secondary);
  min-width: 110px;
  font-size: 0.875rem;
}

.info-value {
  color: var(--text-primary);
  flex: 1;
  font-weight: 500;
  font-size: 0.9375rem;
  word-break: break-word;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.empty-icon {
  font-size: 4rem;
  color: var(--text-secondary, #6c757d);
  margin-bottom: 1rem;
}

.empty-state p {
  color: var(--text-secondary, #6c757d);
  margin-bottom: 1.5rem;
}

.btn-add-first-driver {
  background: var(--primary-color, #007bff);
  color: #ffffff;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: var(--radius-md, 8px);
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  transition: all 0.3s ease;
}

.btn-add-first-driver:hover {
  background: var(--primary-hover, #0056b3);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md, 0 4px 8px rgba(0,0,0,0.15));
}

.driver-form .form-group {
  margin-bottom: 1rem;
}

.driver-form label {
  display: block;
  font-weight: 500;
  margin-bottom: 0.5rem;
  color: var(--text-primary, #212529);
}

.driver-form .required {
  color: var(--danger-color, #dc3545);
}

.driver-form .form-control {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--border-color, #dee2e6);
  border-radius: var(--radius-sm, 4px);
  font-size: 0.95rem;
}

.driver-form .form-control:focus {
  outline: none;
  border-color: var(--primary-color, #007bff);
  box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
}

.checkbox-input {
  margin-right: 0.5rem;
}

.statistics-detail-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.stat-detail-card {
  background: var(--bg-tertiary, #f8f9fa);
  border: 1px solid var(--border-color, #dee2e6);
  border-radius: var(--radius-md, 8px);
  padding: 1.5rem;
  text-align: center;
}

.stat-detail-label {
  font-size: 0.875rem;
  color: var(--text-secondary, #6c757d);
  margin-bottom: 0.5rem;
}

.stat-detail-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary, #212529);
}

.stat-detail-value.success {
  color: #28a745;
}

.stat-detail-value.warning {
  color: #ffc107;
}

.stat-detail-value.danger {
  color: #dc3545;
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 2rem;
  color: var(--text-secondary, #6c757d);
}

[dir="rtl"] .delivery-drivers-page-container {
  direction: rtl;
}

[dir="rtl"] .delivery-statistics-header,
[dir="rtl"] .drivers-management-header {
  flex-direction: row-reverse;
}

[dir="rtl"] .driver-card-actions {
  flex-direction: row-reverse;
}
</style>
