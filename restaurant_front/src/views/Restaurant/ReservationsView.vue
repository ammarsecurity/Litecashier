<template>
  <b-overlay
    :show="show"
    spinner-variant="primary"
    spinner-type="grow"
    spinner-large
    rounded="sm"
  >
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <!-- Header Section -->
          <div class="users-header-section">
            <div class="users-header-content">
              <h1 class="users-page-title">{{ $t("reservations") || "الحجوزات" }}</h1>
              <button class="users-add-button" v-b-modal.modal-addReservation>
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addReservation") || "إضافة حجز" }}</span>
              </button>
            </div>
          </div>

          <!-- Filter Section -->
          <div class="reservation-filter-section">
            <div class="reservation-filter-card">
              <div class="reservation-filter-header">
                <b-icon icon="funnel-fill" class="filter-header-icon"></b-icon>
                <span class="filter-header-text">{{ $t("filters") || "الفلاتر" }}</span>
              </div>
              <div class="reservation-filter-content">
                <div class="reservation-filter-item">
                  <label class="reservation-filter-label">
                    <b-icon icon="calendar-event" class="filter-label-icon"></b-icon>
                    {{ $t("fromDate") || "من تاريخ" }}
                  </label>
                  <div class="reservation-date-wrapper">
                    <b-icon icon="calendar-event" class="reservation-date-icon"></b-icon>
                    <input 
                      type="date" 
                      v-model="fromDate"
                      class="reservation-date-input"
                      @change="onFilterChange"
                    />
                  </div>
                </div>
                <div class="reservation-filter-item">
                  <label class="reservation-filter-label">
                    <b-icon icon="calendar-event-fill" class="filter-label-icon"></b-icon>
                    {{ $t("toDate") || "إلى تاريخ" }}
                  </label>
                  <div class="reservation-date-wrapper">
                    <b-icon icon="calendar-event-fill" class="reservation-date-icon"></b-icon>
                    <input 
                      type="date" 
                      v-model="toDate"
                      class="reservation-date-input"
                      @change="onFilterChange"
                    />
                  </div>
                </div>
                <div class="reservation-filter-item">
                  <label class="reservation-filter-label">
                    <b-icon icon="award-fill" class="filter-label-icon"></b-icon>
                    {{ $t("status") || "الحالة" }}
                  </label>
                  <div class="reservation-select-wrapper">
                    <b-icon icon="chevron-down" class="reservation-select-icon"></b-icon>
                    <select v-model="statusFilter" class="reservation-select-input" @change="onFilterChange">
                      <option value="">{{ $t("all") || "الكل" }}</option>
                      <option value="Pending">{{ $t("pending") || "قيد الانتظار" }}</option>
                      <option value="Confirmed">{{ $t("confirmed") || "مؤكد" }}</option>
                      <option value="Seated">{{ $t("seated") || "جلس" }}</option>
                      <option value="Completed">{{ $t("completed") || "مكتمل" }}</option>
                      <option value="Cancelled">{{ $t("cancelled") || "ملغي" }}</option>
                    </select>
                  </div>
                </div>
                <div class="reservation-filter-item reservation-filter-action">
                  <button class="reservation-filter-btn" @click="onFilterChange">
                    <b-icon icon="search" class="me-2"></b-icon>
                    <span>{{ $t("search") || "بحث" }}</span>
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Reservations Table -->
          <div class="reservations-table-container">
            <b-table
              :items="reservations"
              :fields="reservationFields"
              striped
              hover
              responsive
              class="reservations-table"
              :tbody-tr-class="getReservationRowClass"
            >
              <template #cell(customerName)="row">
                <div class="reservation-customer-cell">
                  <b-icon icon="person-fill" class="customer-icon"></b-icon>
                  <span class="customer-name-text">{{ row.item.customerName }}</span>
                </div>
              </template>

              <template #cell(phoneNumber)="row">
                <div class="reservation-phone-cell">
                  <b-icon icon="telephone-fill" class="phone-icon"></b-icon>
                  <span>{{ row.item.phoneNumber }}</span>
                </div>
              </template>

              <template #cell(reservationDateTime)="row">
                <div class="reservation-datetime-cell">
                  <b-icon icon="clock-fill" class="datetime-icon"></b-icon>
                  <span>{{ formatDateTime(row.item.reservationDateTime) }}</span>
                </div>
              </template>

              <template #cell(numberOfGuests)="row">
                <div class="reservation-guests-cell">
                  <b-icon icon="people-fill" class="guests-icon"></b-icon>
                  <span>{{ row.item.numberOfGuests }}</span>
                </div>
              </template>

              <template #cell(table)="row">
                <span v-if="row.item.table" class="reservation-table-cell">
                  <b-icon icon="table" class="table-icon"></b-icon>
                  {{ row.item.table.tableNumber }}
                </span>
                <span v-else class="text-muted">-</span>
              </template>

              <template #cell(status)="row">
                <span class="reservation-status-badge" :class="getReservationStatusClass(row.item.status)">
                  {{ getStatusText(row.item.status) }}
                </span>
              </template>

              <template #cell(actions)="row">
                <div class="actions-cell">
                  <button 
                    type="button"
                    class="action-btn action-btn--icon action-btn--edit" 
                    @click="editReservation(row.item)"
                    :title="$t('edit')"
                  >
                    <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                  </button>
                  <button 
                    type="button"
                    class="action-btn action-btn--icon"
                    :class="row.item.status === 'Confirmed' ? 'action-btn--delete' : 'action-btn--success'"
                    @click="updateReservationStatus(row.item)"
                    :title="row.item.status === 'Confirmed' ? $t('cancel') : $t('confirm')"
                  >
                    <b-icon :icon="row.item.status === 'Confirmed' ? 'x-circle-fill' : 'check-circle-fill'" class="action-icon"></b-icon>
                  </button>
                </div>
              </template>
            </b-table>

            <!-- Pagination -->
            <div class="pagination-container" v-if="totalPages > 1">
              <b-pagination
                v-model="currentPage"
                :total-rows="totalItems"
                :per-page="pageSize"
                :limit="7"
                first-number
                last-number
                @change="onPageChange"
                class="reservations-pagination"
              ></b-pagination>
              <div class="pagination-info">
                <span>{{ $t('showing') || 'عرض' }} {{ ((currentPage - 1) * pageSize) + 1 }} - {{ Math.min(currentPage * pageSize, totalItems) }} {{ $t('of') || 'من' }} {{ totalItems }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Add Reservation Modal -->
      <b-modal id="modal-addReservation" :title="$t('addReservation')" hide-header hide-footer class="users-modal" size="lg">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("addReservation") || "إضافة حجز" }}</h2>
          <form @submit.prevent="addReservation" class="users-form">
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                  {{ $t("customerName") || "اسم العميل" }}
                </label>
                <input 
                  id="inputCustomerName"
                  v-model="addForm.customerName" 
                  type="text"
                  :placeholder="$t('customerName') || 'اسم العميل'" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                  {{ $t("phoneNumber") || "رقم الهاتف" }}
                </label>
                <input 
                  id="inputPhoneNumber"
                  v-model="addForm.phoneNumber" 
                  type="tel"
                  :placeholder="$t('phoneNumber') || 'رقم الهاتف'" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="calendar-fill" class="form-label-icon"></b-icon>
                  {{ $t("reservationDateTime") || "تاريخ ووقت الحجز" }}
                </label>
                <input 
                  id="inputDateTime"
                  v-model="addForm.reservationDateTime" 
                  type="datetime-local"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                  {{ $t("numberOfGuests") || "عدد الضيوف" }}
                </label>
                <input 
                  id="inputNumberOfGuests"
                  v-model="addForm.numberOfGuests" 
                  type="number"
                  min="1"
                  max="50"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group users-form-group--full">
                <label class="users-form-label">
                  <b-icon icon="table" class="form-label-icon"></b-icon>
                  {{ $t("table") || "الطاولة (اختياري)" }}
                </label>
                <div class="reservation-table-picker">
                  <div class="users-form-sublabel">
                    <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                    {{ $t("zone") || "الموقع" }}
                  </div>
                  <select
                    v-model="tableZoneFilter"
                    class="users-form-input reservation-zone-select"
                    @change="onReservationTableZoneFilterChanged"
                  >
                  <option value="">{{ $t("allZones") || "جميع المواقع" }}</option>
                  <option v-for="zone in uniqueZones" :key="'add-z-' + zone" :value="zone">{{ zone }}</option>
                </select>
                <div class="table-search-wrapper">
                  <div class="table-search-input-wrapper">
                    <b-icon icon="search" class="table-search-icon"></b-icon>
                    <input
                      v-model="tableSearchQuery"
                      type="text"
                      :placeholder="$t('searchTable') || 'ابحث عن طاولة...'"
                      class="table-search-input"
                      autocomplete="off"
                    />
                  </div>
                </div>
                <select v-model="addForm.tableId" class="users-form-input reservation-table-select" size="8">
                  <option :value="null">{{ $t("selectTable") || "اختر طاولة" }}</option>
                  <option v-for="table in filteredTables" :key="table.id" :value="table.id">
                    {{ table.tableNumber }} 
                    {{ table.zone ? `- ${table.zone}` : '' }}
                    ({{ $t("capacity") || "سعة" }}: {{ table.capacity }}) 
                    - {{ getTableStatusText(table.status) }}
                  </option>
                </select>
                </div>
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="chat-left-text-fill" class="form-label-icon"></b-icon>
                {{ $t("specialRequests") || "طلبات خاصة (اختياري)" }}
              </label>
              <textarea 
                id="inputSpecialRequests"
                v-model="addForm.specialRequests" 
                :placeholder="$t('specialRequests') || 'طلبات خاصة'" 
                class="users-form-input"
                rows="3"
              ></textarea>
            </div>
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("add") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addReservation')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("close") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Edit Reservation Modal -->
      <b-modal id="modal-editReservation" :title="$t('editReservation')" hide-header hide-footer class="users-modal" size="lg">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("editReservation") || "تعديل حجز" }}</h2>
          <form @submit.prevent="updateReservation" class="users-form">
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                  {{ $t("customerName") || "اسم العميل" }}
                </label>
                <input 
                  id="inputCustomerNameEdit"
                  v-model="editForm.customerName" 
                  type="text"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                  {{ $t("phoneNumber") || "رقم الهاتف" }}
                </label>
                <input 
                  id="inputPhoneNumberEdit"
                  v-model="editForm.phoneNumber" 
                  type="tel"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="calendar-fill" class="form-label-icon"></b-icon>
                  {{ $t("reservationDateTime") || "تاريخ ووقت الحجز" }}
                </label>
                <input 
                  id="inputDateTimeEdit"
                  v-model="editForm.reservationDateTime" 
                  type="datetime-local"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                  {{ $t("numberOfGuests") || "عدد الضيوف" }}
                </label>
                <input 
                  id="inputNumberOfGuestsEdit"
                  v-model="editForm.numberOfGuests" 
                  type="number"
                  min="1"
                  max="50"
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group users-form-group--full">
                <label class="users-form-label">
                  <b-icon icon="table" class="form-label-icon"></b-icon>
                  {{ $t("table") || "الطاولة" }}
                </label>
                <div class="reservation-table-picker">
                  <div class="users-form-sublabel">
                    <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                    {{ $t("zone") || "الموقع" }}
                  </div>
                  <select
                    v-model="tableZoneFilter"
                    class="users-form-input reservation-zone-select"
                    @change="onReservationTableZoneFilterChanged"
                  >
                  <option value="">{{ $t("allZones") || "جميع المواقع" }}</option>
                  <option v-for="zone in uniqueZones" :key="'edit-z-' + zone" :value="zone">{{ zone }}</option>
                </select>
                <div class="table-search-wrapper">
                  <div class="table-search-input-wrapper">
                    <b-icon icon="search" class="table-search-icon"></b-icon>
                    <input
                      v-model="tableSearchQuery"
                      type="text"
                      :placeholder="$t('searchTable') || 'ابحث عن طاولة...'"
                      class="table-search-input"
                      autocomplete="off"
                    />
                  </div>
                </div>
                <select v-model="editForm.tableId" class="users-form-input reservation-table-select" size="8">
                  <option :value="null">{{ $t("selectTable") || "اختر طاولة" }}</option>
                  <option v-for="table in filteredTables" :key="table.id" :value="table.id">
                    {{ table.tableNumber }} 
                    {{ table.zone ? `- ${table.zone}` : '' }}
                    ({{ $t("capacity") || "سعة" }}: {{ table.capacity }}) 
                    - {{ getTableStatusText(table.status) }}
                  </option>
                </select>
                </div>
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="chat-left-text-fill" class="form-label-icon"></b-icon>
                {{ $t("specialRequests") || "طلبات خاصة" }}
              </label>
              <textarea 
                id="inputSpecialRequestsEdit"
                v-model="editForm.specialRequests" 
                class="users-form-input"
                rows="3"
              ></textarea>
            </div>
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("save") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editReservation')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("close") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>
    </div>
  </b-overlay>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "../../http/api.js";

export default {
  name: "ReservationsView",
  components: {
    AppHeader,
  },
  data() {
    return {
      show: false,
      reservations: [],
      availableTables: [],
      allTables: [],
      tableSearchQuery: "",
      tableZoneFilter: "",
      fromDate: "",
      toDate: "",
      statusFilter: "",
      currentPage: 1,
      pageSize: 10,
      totalItems: 0,
      totalPages: 0,
      addForm: {
        customerName: "",
        phoneNumber: "",
        reservationDateTime: "",
        numberOfGuests: 2,
        tableId: null,
        specialRequests: "",
        status: "Pending"
      },
      editForm: {
        id: null,
        customerName: "",
        phoneNumber: "",
        reservationDateTime: "",
        numberOfGuests: 2,
        tableId: null,
        specialRequests: "",
        status: "Pending"
      },
    };
  },
  computed: {
    uniqueZones() {
      if (!Array.isArray(this.allTables)) return [];
      const zones = this.allTables
        .map((table) => table.zone)
        .filter((zone) => zone && String(zone).trim() !== "");
      return [...new Set(zones)].sort();
    },
    filteredTables() {
      let tables = Array.isArray(this.allTables) ? [...this.allTables] : [];
      const zf = (this.tableZoneFilter ?? "").trim();
      if (zf) {
        tables = tables.filter((t) => (t.zone && String(t.zone).trim()) === zf);
      }
      if (!this.tableSearchQuery) {
        return tables;
      }
      const query = this.tableSearchQuery.toLowerCase().trim();
      return tables.filter((table) => {
        const tableNumber = String(table.tableNumber || "").toLowerCase();
        const zone = (table.zone || "").toLowerCase();
        const status = (table.status || "").toLowerCase();
        return (
          tableNumber.includes(query) ||
          zone.includes(query) ||
          status.includes(query)
        );
      });
    },
    reservationFields() {
      return [
        {
          key: 'customerName',
          label: this.$t('customerName') || 'اسم العميل',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'phoneNumber',
          label: this.$t('phoneNumber') || 'رقم الهاتف',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'reservationDateTime',
          label: this.$t('reservationDateTime') || 'تاريخ ووقت الحجز',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'numberOfGuests',
          label: this.$t('numberOfGuests') || 'عدد الضيوف',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'table',
          label: this.$t('table') || 'الطاولة',
          sortable: false,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'status',
          label: this.$t('status') || 'الحالة',
          sortable: true,
          thClass: 'reservation-header-cell'
        },
        {
          key: 'actions',
          label: this.$t('actions') || 'الإجراءات',
          sortable: false,
          thClass: 'reservation-header-cell'
        }
      ];
    }
  },
  mounted() {
    this.getReservations();
    this.getTables();
    // Set default dates (today and next week)
    const today = new Date();
    const nextWeek = new Date();
    nextWeek.setDate(today.getDate() + 7);
    this.fromDate = today.toISOString().split('T')[0];
    this.toDate = nextWeek.toISOString().split('T')[0];
  },
  methods: {
    getReservations() {
      this.show = true;
      const params = {
        pageNumber: this.currentPage - 1, // Backend uses 0-based index
        pageSize: this.pageSize
      };

      if (this.fromDate) {
        params.fromDate = `${this.fromDate}T00:00:00`;
      }
      if (this.toDate) {
        params.toDate = `${this.toDate}T23:59:59`;
      }
      if (this.statusFilter) {
        params.status = this.statusFilter;
      }
      
      HTTP.get("Reservations", { params })
        .then((response) => {
          const pagedData = response.data.data;
          this.reservations = pagedData.items || [];
          this.totalItems = pagedData.totalItems || 0;
          this.totalPages = pagedData.totalPages || 0;
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    onPageChange(page) {
      this.currentPage = page;
      this.getReservations();
    },
    onFilterChange() {
      this.currentPage = 1; // Reset to first page when filter changes
      this.getReservations();
    },
    getTables() {
      HTTP.get("Tables", { 
        params: { 
          pageNumber: 0, 
          pageSize: 1000 // Get all tables for dropdown
        } 
      })
        .then((response) => {
          const pagedData = response.data.data;
          this.allTables = pagedData.items || [];
          this.availableTables = this.allTables; // Keep for backward compatibility
        })
        .catch(() => {
          // Ignore errors
        });
    },
    getTableStatusText(status) {
      const statusTexts = {
        Available: this.$t("available") || "متاحة",
        Occupied: this.$t("occupied") || "مشغولة",
        Reserved: this.$t("reserved") || "محجوزة",
        OutOfService: this.$t("outOfService") || "خارج الخدمة"
      };
      return statusTexts[status] || status;
    },
    addReservation() {
      this.show = true;
      // Convert datetime-local to ISO format
      const formData = {
        customerName: this.addForm.customerName,
        phoneNumber: this.addForm.phoneNumber,
        reservationDateTime: new Date(this.addForm.reservationDateTime).toISOString(),
        numberOfGuests: this.addForm.numberOfGuests,
        status: this.addForm.status || "Pending"
      };
      
      // Only include tableId if it's not null
      if (this.addForm.tableId !== null && this.addForm.tableId !== undefined) {
        formData.tableId = this.addForm.tableId;
      }
      
      // Only include specialRequests if it's not empty
      if (this.addForm.specialRequests && this.addForm.specialRequests.trim() !== "") {
        formData.specialRequests = this.addForm.specialRequests.trim();
      }
      
      HTTP.post("Reservations", formData)
        .then((response) => {
          this.show = false;
          this.$toast.success(this.$i18n.t("reservationAddedSuccessfully") || "تم إضافة الحجز بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.addForm = {
            customerName: "",
            phoneNumber: "",
            reservationDateTime: "",
            numberOfGuests: 2,
            tableId: null,
            specialRequests: "",
            status: "Pending"
          };
          this.$bvModal.hide("modal-addReservation");
          this.getReservations();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    editReservation(reservation) {
      this.editForm = {
        id: reservation.id,
        customerName: reservation.customerName,
        phoneNumber: reservation.phoneNumber,
        reservationDateTime: new Date(reservation.reservationDateTime).toISOString().slice(0, 16),
        numberOfGuests: reservation.numberOfGuests,
        tableId: reservation.tableId,
        specialRequests: reservation.specialRequests || "",
        status: reservation.status
      };
      this.$bvModal.show("modal-editReservation");
    },
    updateReservation() {
      this.show = true;
      const formData = {
        ...this.editForm,
        reservationDateTime: new Date(this.editForm.reservationDateTime).toISOString()
      };
      
      HTTP.put(`Reservations/${this.editForm.id}`, formData)
        .then((response) => {
          this.show = false;
          this.$toast.success(this.$i18n.t("reservationUpdatedSuccessfully") || "تم تحديث الحجز بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.$bvModal.hide("modal-editReservation");
          this.getReservations();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    updateReservationStatus(reservation) {
      const newStatus = reservation.status === "Confirmed" ? "Cancelled" : "Confirmed";
      this.show = true;
      HTTP.put(`Reservations/${reservation.id}/status`, newStatus, {
        headers: {
          'Content-Type': 'application/json'
        }
      })
        .then((response) => {
          this.show = false;
          this.$toast.success(this.$i18n.t("reservationStatusUpdated") || "تم تحديث حالة الحجز بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.getReservations();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    formatDateTime(dateTime) {
      if (!dateTime) return "";
      const date = new Date(dateTime);
      return date.toLocaleString('ar-IQ', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    },
    getReservationRowClass(item, type) {
      if (!item || type !== 'row') return '';
      return `reservation-row-${this.getReservationStatusClass(item.status)}`;
    },
    getReservationStatusClass(status) {
      const statusClasses = {
        Pending: "reservation-pending",
        Confirmed: "reservation-confirmed",
        Seated: "reservation-seated",
        Completed: "reservation-completed",
        Cancelled: "reservation-cancelled"
      };
      return statusClasses[status] || "";
    },
    getStatusText(status) {
      const statusTexts = {
        Pending: this.$t("pending") || "قيد الانتظار",
        Confirmed: this.$t("confirmed") || "مؤكد",
        Seated: this.$t("seated") || "جلس",
        Completed: this.$t("completed") || "مكتمل",
        Cancelled: this.$t("cancelled") || "ملغي"
      };
      return statusTexts[status] || status;
    },
    onReservationTableZoneFilterChanged() {
      const ids = new Set(this.filteredTables.map((t) => t.id));
      if (this.addForm.tableId != null && !ids.has(this.addForm.tableId)) {
        this.addForm.tableId = null;
      }
      if (this.editForm.tableId != null && !ids.has(this.editForm.tableId)) {
        this.editForm.tableId = null;
      }
    },
    closeModel(modalId) {
      this.$bvModal.hide(modalId);
      // Reset table search when closing modal
      if (modalId === 'modal-addReservation' || modalId === 'modal-editReservation') {
        this.tableSearchQuery = "";
        this.tableZoneFilter = "";
      }
    }
  },
};
</script>

<style scoped>
/* Filter Section Styles */
.reservation-filter-section {
  margin-bottom: 2rem;
}

.reservation-filter-card {
  background: var(--bg-primary);
  border-radius: 1rem;
  padding: 1.5rem;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-md);
  transition: all 0.3s ease;
}

.reservation-filter-card:hover {
  box-shadow: var(--shadow-lg);
  border-color: var(--border-dark);
}

.reservation-filter-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 2px solid var(--border-color);
}

.filter-header-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.filter-header-text {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--text-primary);
}

.reservation-filter-content {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1.5rem;
  align-items: end;
}

.reservation-filter-item {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.reservation-filter-item.reservation-filter-action {
  align-items: stretch;
}

.reservation-filter-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  font-size: 0.9375rem;
  color: var(--text-secondary);
}

.filter-label-icon {
  font-size: 1rem;
  color: var(--primary-color);
}

.reservation-date-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.reservation-date-icon {
  position: absolute;
  right: 1rem;
  color: var(--text-muted);
  font-size: 1.125rem;
  pointer-events: none;
  z-index: 1;
}

.reservation-date-input {
  width: 100%;
  padding: 0.875rem 1rem 0.875rem 3rem;
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  font-size: 1rem;
  transition: all 0.3s ease;
  background: var(--bg-tertiary);
  color: var(--text-primary);
  font-weight: 500;
}

.reservation-date-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.1);
  outline: none;
  background: var(--bg-tertiary);
}

.reservation-date-input::-webkit-calendar-picker-indicator {
  filter: invert(0.8);
  cursor: pointer;
}

.reservation-select-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.reservation-select-icon {
  position: absolute;
  left: 1rem;
  color: var(--text-muted);
  font-size: 1rem;
  pointer-events: none;
  z-index: 1;
}

.reservation-select-input {
  width: 100%;
  padding: 0.875rem 1rem 0.875rem 3rem;
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  font-size: 1rem;
  transition: all 0.3s ease;
  background: var(--bg-tertiary);
  color: var(--text-primary);
  font-weight: 500;
  cursor: pointer;
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
}

.reservation-select-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.1);
  outline: none;
  background: var(--bg-tertiary);
}

.reservation-select-input option {
  background: var(--bg-primary);
  color: var(--text-primary);
  padding: 0.5rem;
}

.reservation-filter-btn {
  width: 100%;
  padding: 0.875rem 1.5rem;
  border: none;
  border-radius: 0.75rem;
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
  color: #ffffff;
  font-weight: 600;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  box-shadow: var(--shadow-sm);
}

.reservation-filter-btn:hover {
  background: linear-gradient(135deg, var(--primary-dark) 0%, var(--primary-color) 100%);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.reservation-filter-btn:active {
  transform: translateY(0);
}

@media (max-width: 768px) {
  .reservation-filter-content {
    grid-template-columns: 1fr;
  }
  
  .reservation-filter-card {
    padding: 1rem;
  }
  
  .reservation-filter-item.reservation-filter-action {
    grid-column: 1;
  }
}

.reservation-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.reservation-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
}

.reservation-pending {
  border-left: 4px solid #d97706;
}

.reservation-confirmed {
  border-left: 4px solid #0284c7;
}

.reservation-seated {
  border-left: 4px solid #059669;
}

.reservation-completed {
  border-left: 4px solid #64748b;
}

.reservation-cancelled {
  border-left: 4px solid #dc2626;
  opacity: 0.7;
}

.reservation-status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  margin-top: 0.5rem;
  display: inline-block;
}

.reservation-status-badge.reservation-pending {
  background-color: var(--warning-light);
  color: var(--warning-color);
}

.reservation-status-badge.reservation-confirmed {
  background-color: var(--info-light);
  color: var(--info-color);
}

.reservation-status-badge.reservation-seated {
  background-color: var(--success-light);
  color: var(--success-color);
}

.reservation-status-badge.reservation-completed {
  background-color: rgba(30, 41, 59, 0.5);
  color: var(--text-muted);
}

.reservation-status-badge.reservation-cancelled {
  background-color: var(--danger-light);
  color: var(--danger-color);
}

.reservation-avatar {
  width: 60px;
  height: 60px;
  border-radius: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
}

.reservation-avatar.reservation-pending {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: white;
}

.reservation-avatar.reservation-confirmed {
  background: linear-gradient(135deg, #0284c7 0%, #0369a1 100%);
  color: white;
}

.reservation-avatar.reservation-seated {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
}

.reservation-avatar.reservation-completed {
  background: linear-gradient(135deg, #94a3b8 0%, #64748b 100%);
  color: white;
}

.reservation-avatar.reservation-cancelled {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: white;
}

.reservations-table-container {
  background: #ffffff;
  border-radius: 0.75rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  margin-top: 1.5rem;
}

.reservations-table {
  margin: 0;
}

.reservations-table >>> thead th {
  background-color: #f9fafb;
  color: #374151;
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 1rem;
  border-bottom: 2px solid #e5e7eb;
}

.reservations-table >>> tbody td {
  padding: 1rem;
  vertical-align: middle;
  border-bottom: 1px solid #f3f4f6;
}

.reservations-table >>> tbody tr:hover {
  background-color: #f9fafb;
}

.reservation-row-reservation-pending {
  border-left: 4px solid #d97706;
}

.reservation-row-reservation-confirmed {
  border-left: 4px solid #0284c7;
}

.reservation-row-reservation-seated {
  border-left: 4px solid #059669;
}

.reservation-row-reservation-completed {
  border-left: 4px solid #64748b;
}

.reservation-row-reservation-cancelled {
  border-left: 4px solid #dc2626;
}

.reservation-customer-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.customer-icon {
  color: #6b7280;
  font-size: 1rem;
}

.customer-name-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: #111827;
}

.reservation-phone-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #374151;
  font-weight: 500;
}

.phone-icon {
  color: #6b7280;
  font-size: 1rem;
}

.reservation-datetime-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #374151;
  font-weight: 500;
}

.datetime-icon {
  color: #6b7280;
  font-size: 1rem;
}

.reservation-guests-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #374151;
  font-weight: 500;
}

.guests-icon {
  color: #6b7280;
  font-size: 1rem;
}

.reservation-table-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #374151;
  font-weight: 500;
}

.table-icon {
  color: #6b7280;
  font-size: 1rem;
}

.reservation-status-badge {
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  display: inline-block;
}

.reservation-status-badge.reservation-pending {
  background-color: var(--warning-light);
  color: var(--warning-color);
}

.reservation-status-badge.reservation-confirmed {
  background-color: var(--info-light);
  color: var(--info-color);
}

.reservation-status-badge.reservation-seated {
  background-color: var(--success-light);
  color: var(--success-color);
}

.reservation-status-badge.reservation-completed {
  background-color: rgba(30, 41, 59, 0.5);
  color: var(--text-muted);
}

.reservation-status-badge.reservation-cancelled {
  background-color: var(--danger-light);
  color: var(--danger-color);
}

.pagination-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background-color: var(--bg-primary);
  border-top: 1px solid var(--border-color);
}

.pagination-info {
  color: var(--text-muted);
  font-size: 0.875rem;
}

.reservations-pagination >>> .page-link {
  color: var(--text-primary);
  border-color: var(--border-color);
  background-color: var(--bg-tertiary);
}

.reservations-pagination >>> .page-item.active .page-link {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
}

.reservations-pagination >>> .page-link:hover {
  background-color: rgba(99, 102, 241, 0.1);
  border-color: var(--border-dark);
  color: var(--primary-color);
}

.text-muted {
  color: #9ca3af;
  font-style: italic;
}

/* اختيار الطاولة في نموذج الحجز */
.reservation-table-picker {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  width: 100%;
  box-sizing: border-box;
  padding: 1rem 1.1rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--bg-secondary) 92%, var(--border-color) 8%);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
}

.users-form-sublabel {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.8125rem;
  font-weight: 650;
  color: var(--text-secondary);
  margin: 0;
  letter-spacing: 0.01em;
}

.users-form-sublabel .form-label-icon {
  font-size: 0.9rem;
  opacity: 0.9;
}

.reservation-zone-select {
  margin: 0 !important;
}

/* Table Search */
.table-search-wrapper {
  margin: 0;
  width: 100%;
}

.table-search-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.table-search-icon {
  position: absolute;
  inset-inline-end: 0.875rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  font-size: 1.05rem;
  pointer-events: none;
  z-index: 2;
  opacity: 0.88;
}

.table-search-input {
  width: 100%;
  box-sizing: border-box;
  min-height: 2.875rem;
  padding-block: 0.65rem;
  padding-inline: 1rem 2.75rem;
  border: 2px solid var(--border-color);
  border-radius: 0.65rem;
  font-size: 0.9375rem;
  line-height: 1.35;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, background 0.2s ease;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.table-search-input::placeholder {
  color: var(--text-muted);
  opacity: 0.85;
}

.table-search-input:hover {
  border-color: color-mix(in srgb, var(--primary-color) 35%, var(--border-color));
}

.table-search-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 22%, transparent);
  outline: none;
  background: var(--bg-primary);
}

.reservation-table-select {
  margin: 0 !important;
  width: 100%;
  min-height: 11.5rem;
  max-height: min(42vh, 280px);
  padding: 0.4rem;
  border-radius: 0.65rem;
  border: 2px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  line-height: 1.5;
  font-size: 0.875rem;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.reservation-table-select:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary-color) 18%, transparent);
  outline: none;
}

.users-form-input[multiple] {
  min-height: 200px;
  overflow-y: auto;
  padding: 0.5rem;
}

.users-form-input[multiple] option {
  padding: 0.5rem;
  margin-bottom: 0.25rem;
  border-radius: 0.25rem;
  cursor: pointer;
}

.users-form-input[multiple] option:hover {
  background-color: var(--primary-color);
  color: white;
}

.users-form-input[multiple] option:checked {
  background-color: var(--primary-color);
  color: white;
}

@media (max-width: 576px) {
  .reservation-table-picker {
    padding: 0.85rem;
    gap: 0.55rem;
  }

  .reservation-table-select {
    max-height: min(38vh, 220px);
    font-size: 0.8125rem;
  }

  .table-search-input {
    min-height: 2.75rem;
    font-size: 0.875rem;
  }
}
</style>

