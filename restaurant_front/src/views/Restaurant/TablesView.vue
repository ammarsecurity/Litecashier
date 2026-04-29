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
              <h1 class="users-page-title">{{ $t("tables") || "الطاولات" }}</h1>
              <div style="display: flex; gap: 10px;">
                <button class="users-add-button" v-b-modal.modal-addTableBulk>
                  <b-icon icon="layers-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addTablesBulk") || "إضافة مجموعات" }}</span>
                </button>
                <button class="users-add-button" v-b-modal.modal-addTable>
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addTable") || "إضافة طاولة" }}</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Filter Section -->
          <div class="users-search-section">
            <div class="users-search-container">
              <b-icon icon="filter" class="search-icon"></b-icon>
              <select v-model="statusFilter" @change="onFilterChange" class="users-search-input" style="padding-left: 2.5rem;">
                <option value="">{{ $t("all") || "الكل" }}</option>
                <option value="Available">{{ $t("available") || "متاحة" }}</option>
                <option value="Occupied">{{ $t("occupied") || "مشغولة" }}</option>
                <option value="Reserved">{{ $t("reserved") || "محجوزة" }}</option>
                <option value="OutOfService">{{ $t("outOfService") || "خارج الخدمة" }}</option>
              </select>
            </div>
          </div>

          <!-- Tables Table -->
          <div class="tables-table-container">
            <b-table
              :items="tables"
              :fields="tableFields"
              striped
              hover
              responsive
              class="tables-table"
              :tbody-tr-class="getTableRowClass"
            >
              <template #cell(tableNumber)="row">
                <div class="table-number-cell">
                  <b-icon icon="table" class="table-icon" :class="getTableStatusClass(row.item.status)"></b-icon>
                  <span class="table-number-text">{{ row.item.tableNumber }}</span>
                </div>
              </template>

              <template #cell(status)="row">
                <span class="table-status-badge" :class="getTableStatusClass(row.item.status)">
                  {{ getStatusText(row.item.status) }}
                </span>
              </template>

              <template #cell(capacity)="row">
                <div class="table-capacity-cell">
                  <b-icon icon="people-fill" class="capacity-icon"></b-icon>
                  <span>{{ row.item.capacity }}</span>
                </div>
              </template>

              <template #cell(zone)="row">
                <span v-if="row.item.zone">{{ row.item.zone }}</span>
                <span v-else class="text-muted">-</span>
              </template>

              <template #cell(currentOrderId)="row">
                <span v-if="row.item.currentOrderId" class="order-id-cell">
                  <b-icon icon="receipt" class="order-icon"></b-icon>
                  #{{ row.item.currentOrderId }}
                </span>
                <span v-else class="text-muted">-</span>
              </template>

              <template #cell(actions)="row">
                <div class="table-actions-cell">
                  <button 
                    class="table-action-btn edit-btn" 
                    @click="editTable(row.item)"
                    :title="$t('edit')"
                  >
                    <b-icon icon="pencil-fill"></b-icon>
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
                class="tables-pagination"
              ></b-pagination>
              <div class="pagination-info">
                <span>{{ $t('showing') || 'عرض' }} {{ ((currentPage - 1) * pageSize) + 1 }} - {{ Math.min(currentPage * pageSize, totalItems) }} {{ $t('of') || 'من' }} {{ totalItems }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Add Tables Bulk Modal -->
      <b-modal id="modal-addTableBulk" :title="$t('addTablesBulk')" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("addTablesBulk") || "إضافة مجموعات الطاولات" }}</h2>
          <form @submit.prevent="addTablesBulk" class="users-form">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                {{ $t("zone") || "المنطقة" }}
              </label>
              <input 
                id="inputZoneBulk"
                v-model="bulkForm.zone" 
                type="text"
                :placeholder="$t('zone') || 'المنطقة (مثلاً: الشرفة، داخلية، خارجية)'" 
                required 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="hash" class="form-label-icon"></b-icon>
                {{ $t("numberOfTables") || "عدد الطاولات" }}
              </label>
              <input 
                id="inputNumberOfTables"
                v-model="bulkForm.numberOfTables" 
                type="number"
                min="1"
                max="100"
                :placeholder="$t('numberOfTables') || 'عدد الطاولات'" 
                required 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                {{ $t("capacity") || "عدد الكراسي لكل طاولة" }}
              </label>
              <input 
                id="inputCapacityBulk"
                v-model="bulkForm.capacity" 
                type="number"
                min="1"
                max="50"
                :placeholder="$t('capacity') || 'عدد الكراسي لكل طاولة'" 
                required 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="info-circle-fill" class="form-label-icon"></b-icon>
                {{ $t("notes") || "ملاحظات" }}
              </label>
              <textarea 
                id="inputNotesBulk"
                v-model="bulkForm.notes" 
                :placeholder="$t('notes') || 'ملاحظات (اختياري)'" 
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
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addTableBulk')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("close") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Add Table Modal -->
      <b-modal id="modal-addTable" :title="$t('addTable')" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("addTable") || "إضافة طاولة" }}</h2>
          <form @submit.prevent="addTable" class="users-form">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="hash" class="form-label-icon"></b-icon>
                {{ $t("tableNumber") || "رقم الطاولة" }}
              </label>
              <input 
                id="inputTableNumber"
                v-model="addForm.tableNumber" 
                type="text"
                :placeholder="$t('tableNumber') || 'رقم الطاولة'" 
                required 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                {{ $t("capacity") || "السعة" }}
              </label>
              <input 
                id="inputCapacity"
                v-model="addForm.capacity" 
                type="number"
                min="1"
                max="50"
                :placeholder="$t('capacity') || 'السعة'" 
                required 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                {{ $t("zone") || "المنطقة" }}
              </label>
              <input 
                id="inputZone"
                v-model="addForm.zone" 
                type="text"
                :placeholder="$t('zone') || 'المنطقة (اختياري)'" 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="info-circle-fill" class="form-label-icon"></b-icon>
                {{ $t("notes") || "ملاحظات" }}
              </label>
              <textarea 
                id="inputNotes"
                v-model="addForm.notes" 
                :placeholder="$t('notes') || 'ملاحظات (اختياري)'" 
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
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addTable')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("close") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Edit Table Modal -->
      <b-modal id="modal-editTable" :title="$t('editTable')" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("editTable") || "تعديل طاولة" }}</h2>
          <form @submit.prevent="updateTable" class="users-form">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="hash" class="form-label-icon"></b-icon>
                {{ $t("tableNumber") || "رقم الطاولة" }}
              </label>
              <input 
                id="inputTableNumberEdit"
                v-model="editForm.tableNumber" 
                type="text"
                :placeholder="$t('tableNumber') || 'رقم الطاولة'" 
                required 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="people-fill" class="form-label-icon"></b-icon>
                {{ $t("capacity") || "السعة" }}
              </label>
              <input 
                id="inputCapacityEdit"
                v-model="editForm.capacity" 
                type="number"
                min="1"
                max="50"
                :placeholder="$t('capacity') || 'السعة'" 
                required 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                {{ $t("zone") || "المنطقة" }}
              </label>
              <input 
                id="inputZoneEdit"
                v-model="editForm.zone" 
                type="text"
                :placeholder="$t('zone') || 'المنطقة (اختياري)'" 
                class="users-form-input"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="info-circle-fill" class="form-label-icon"></b-icon>
                {{ $t("notes") || "ملاحظات" }}
              </label>
              <textarea 
                id="inputNotesEdit"
                v-model="editForm.notes" 
                :placeholder="$t('notes') || 'ملاحظات (اختياري)'" 
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
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editTable')">
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
  name: "TablesView",
  components: {
    AppHeader,
  },
  data() {
    return {
      show: false,
      tables: [],
      statusFilter: "",
      currentPage: 1,
      pageSize: 10,
      totalItems: 0,
      totalPages: 0,
      addForm: {
        tableNumber: "",
        capacity: 4,
        zone: "",
        notes: "",
        status: "Available"
      },
      editForm: {
        id: null,
        tableNumber: "",
        capacity: 4,
        zone: "",
        notes: "",
        status: "Available"
      },
      bulkForm: {
        zone: "",
        numberOfTables: 10,
        capacity: 4,
        notes: ""
      },
    };
  },
  computed: {
    tableFields() {
      return [
        {
          key: 'tableNumber',
          label: this.$t('tableNumber') || 'رقم الطاولة',
          sortable: true,
          thClass: 'table-header-cell'
        },
        {
          key: 'status',
          label: this.$t('status') || 'الحالة',
          sortable: true,
          thClass: 'table-header-cell'
        },
        {
          key: 'capacity',
          label: this.$t('capacity') || 'السعة',
          sortable: true,
          thClass: 'table-header-cell'
        },
        {
          key: 'zone',
          label: this.$t('zone') || 'المنطقة',
          sortable: true,
          thClass: 'table-header-cell'
        },
        {
          key: 'currentOrderId',
          label: this.$t('currentOrder') || 'الطلب الحالي',
          sortable: false,
          thClass: 'table-header-cell'
        },
        {
          key: 'actions',
          label: this.$t('actions') || 'الإجراءات',
          sortable: false,
          thClass: 'table-header-cell'
        }
      ];
    },
  },
  mounted() {
    this.getTables();
  },
  methods: {
    getTables() {
      this.show = true;
      const params = {
        pageNumber: this.currentPage - 1, // Backend uses 0-based index
        pageSize: this.pageSize
      };

      if (this.statusFilter) {
        params.status = this.statusFilter;
      }

      HTTP.get("Tables", { params })
        .then((response) => {
          const pagedData = response.data.data;
          this.tables = pagedData.items || [];
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
      this.getTables();
    },
    onFilterChange() {
      this.currentPage = 1; // Reset to first page when filter changes
      this.getTables();
    },
    addTable() {
      this.show = true;
      HTTP.post("Tables", this.addForm)
        .then((response) => {
          this.show = false;
          this.$toast.success(this.$i18n.t("tableAddedSuccessfully") || "تم إضافة الطاولة بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.addForm = {
            tableNumber: "",
            capacity: 4,
            zone: "",
            notes: "",
            status: "Available"
          };
          this.$bvModal.hide("modal-addTable");
          this.getTables();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    addTablesBulk() {
      this.show = true;
      HTTP.post("Tables/bulk", this.bulkForm)
        .then((response) => {
          this.show = false;
          const count = response.data.data?.length || this.bulkForm.numberOfTables;
          this.$toast.success(
            this.$i18n.t("tablesAddedSuccessfully", { count }) || `تم إضافة ${count} طاولة بنجاح`, 
            {
              position: "top-right",
              timeout: 4000,
            }
          );
          this.bulkForm = {
            zone: "",
            numberOfTables: 10,
            capacity: 4,
            notes: ""
          };
          this.$bvModal.hide("modal-addTableBulk");
          this.getTables();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    editTable(table) {
      this.editForm = {
        id: table.id,
        tableNumber: table.tableNumber,
        capacity: table.capacity,
        zone: table.zone || "",
        notes: table.notes || "",
        status: table.status
      };
      this.$bvModal.show("modal-editTable");
    },
    updateTable() {
      this.show = true;
      HTTP.put(`Tables/${this.editForm.id}`, {
        tableNumber: this.editForm.tableNumber,
        capacity: this.editForm.capacity,
        zone: this.editForm.zone,
        notes: this.editForm.notes,
        status: this.editForm.status
      })
        .then((response) => {
          this.show = false;
          this.$toast.success(this.$i18n.t("tableUpdatedSuccessfully") || "تم تحديث الطاولة بنجاح", {
            position: "top-right",
            timeout: 4000,
          });
          this.$bvModal.hide("modal-editTable");
          this.getTables();
        })
        .catch((error) => {
          this.show = false;
          this.$toast.error(error.response?.data?.message || this.$i18n.t("error") || "حدث خطأ", {
            position: "top-right",
            timeout: 4000,
          });
        });
    },
    selectTable(table) {
      // يمكن استخدامها للانتقال إلى POS مع الطاولة المحددة
      if (table.status === "Available") {
        this.$router.push({ path: '/pos', query: { tableId: table.id } });
      }
    },
    getTableRowClass(item, type) {
      if (!item || type !== 'row') return '';
      return `table-row-${this.getTableStatusClass(item.status)}`;
    },
    getTableStatusClass(status) {
      const statusClasses = {
        Available: "table-available",
        Occupied: "table-occupied",
        Reserved: "table-reserved",
        OutOfService: "table-out-of-service"
      };
      return statusClasses[status] || "";
    },
    getStatusText(status) {
      const statusTexts = {
        Available: this.$t("available") || "متاحة",
        Occupied: this.$t("occupied") || "مشغولة",
        Reserved: this.$t("reserved") || "محجوزة",
        OutOfService: this.$t("outOfService") || "خارج الخدمة"
      };
      return statusTexts[status] || status;
    },
    closeModel(modalId) {
      this.$bvModal.hide(modalId);
    }
  },
};
</script>

<style scoped>
.tables-table-container {
  background: #ffffff;
  border-radius: 0.75rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  margin-top: 1.5rem;
}

.tables-table {
  margin: 0;
}

.tables-table >>> thead th {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.9) 0%, rgba(129, 140, 248, 0.9) 100%);
  color: #ffffff;
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 1rem;
  border-bottom: 2px solid var(--border-color);
}

.tables-table >>> tbody td {
  padding: 1rem;
  vertical-align: middle;
  background-color: var(--bg-primary);
  color: var(--text-primary);
  border-bottom: 1px solid var(--border-light);
}

.tables-table >>> tbody tr:hover {
  background-color: rgba(99, 102, 241, 0.1);
}

.table-row-table-available {
  border-left: 4px solid var(--success-color);
}

.table-row-table-occupied {
  border-left: 4px solid var(--danger-color);
}

.table-row-table-reserved {
  border-left: 4px solid var(--warning-color);
}

.table-row-table-out-of-service {
  border-left: 4px solid var(--text-muted);
}

.table-number-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.table-icon {
  font-size: 1.25rem;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.5rem;
}

.table-icon.table-available {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
}

.table-icon.table-occupied {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: white;
}

.table-icon.table-reserved {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: white;
}

.table-icon.table-out-of-service {
  background: linear-gradient(135deg, #94a3b8 0%, #64748b 100%);
  color: white;
}

.table-number-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: var(--text-primary);
}

.table-status-badge {
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  display: inline-block;
}

.table-status-badge.table-available {
  background-color: var(--success-light);
  color: var(--success-color);
  border: 1px solid rgba(34, 197, 94, 0.4);
}

.table-status-badge.table-occupied {
  background-color: var(--danger-light);
  color: var(--danger-color);
  border: 1px solid rgba(239, 68, 68, 0.4);
}

.table-status-badge.table-reserved {
  background-color: var(--warning-light);
  color: var(--warning-color);
  border: 1px solid rgba(192, 132, 252, 0.4);
}

.table-status-badge.table-out-of-service {
  background-color: rgba(30, 41, 59, 0.5);
  color: var(--text-muted);
  border: 1px solid var(--border-color);
}

.table-capacity-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--text-primary);
  font-weight: 500;
}

.capacity-icon {
  color: var(--text-muted);
  font-size: 1rem;
}

.order-id-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--text-primary);
  font-weight: 500;
}

.order-icon {
  color: var(--text-muted);
  font-size: 1rem;
}

.table-actions-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.table-action-btn {
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 0.875rem;
}

.table-action-btn.edit-btn {
  background-color: rgba(129, 140, 248, 0.2);
  color: var(--primary-color);
  border: 1px solid rgba(129, 140, 248, 0.4);
}

.table-action-btn.edit-btn:hover {
  background-color: var(--primary-color);
  color: #ffffff;
  border-color: var(--primary-color);
  transform: scale(1.05);
  box-shadow: 0 4px 12px rgba(129, 140, 248, 0.4);
}

.table-action-btn:active {
  transform: scale(0.95);
}

.text-muted {
  color: var(--text-muted);
  font-style: italic;
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

.tables-pagination >>> .page-link {
  color: var(--text-primary);
  border-color: var(--border-color);
  background-color: var(--bg-tertiary);
}

.tables-pagination >>> .page-item.active .page-link {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
}

.tables-pagination >>> .page-link:hover {
  background-color: rgba(99, 102, 241, 0.1);
  border-color: var(--border-dark);
  color: var(--primary-color);
}
</style>

