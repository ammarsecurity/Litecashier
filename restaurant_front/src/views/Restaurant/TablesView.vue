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
              <div class="tables-header-actions">
                <router-link
                  to="/restaurant/table-layout"
                  class="users-add-button floor-plan-nav-link"
                  style="text-decoration: none;"
                >
                  <b-icon icon="columns-gap" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("tableFloorPlanTitle") }}</span>
                </router-link>
                <button class="users-add-button" v-b-modal.modal-addTableBulk>
                  <b-icon icon="layers-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addTablesBulk") || "إضافة مجموعات" }}</span>
                </button>
                <button class="users-add-button" v-b-modal.modal-addTable>
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addTable") || "إضافة طاولة" }}</span>
                </button>
                <button
                  type="button"
                  class="users-add-button tables-action-btn tables-action-btn--outline-danger"
                  :disabled="selectedIds.length === 0 || deletingTables"
                  @click="confirmDeleteSelected"
                >
                  <b-icon icon="trash" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("deleteSelectedTables") }}</span>
                </button>
                <button
                  type="button"
                  class="users-add-button tables-action-btn tables-action-btn--danger"
                  :disabled="deletingTables || totalItems === 0"
                  @click="confirmDeleteAll"
                >
                  <b-icon icon="trash-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("deleteAllTables") }}</span>
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

          <!-- جدول الطاولات — نفس أسلوب جداول الأصناف/التصنيفات -->
          <div class="items-table-container">
            <b-table
              :items="tables"
              :fields="tableFields"
              striped
              hover
              responsive
              class="items-table"
            >
              <template #head(selected)>
                <b-form-checkbox
                  :checked="allPageSelected"
                  :indeterminate="somePageSelected && !allPageSelected"
                  @change="onSelectAllPageChange"
                  :title="$t('selectAll') || 'تحديد الكل في الصفحة'"
                  class="mb-0"
                />
              </template>
              <template #cell(selected)="row">
                <b-form-checkbox
                  :checked="isTableSelected(tableRowId(row.item))"
                  @change="toggleTableSelected(tableRowId(row.item))"
                  class="mb-0"
                />
              </template>
              <template #cell(tableNumber)="row">
                <div class="table-number-cell">
                  <b-icon icon="table" class="table-list-icon"></b-icon>
                  <span class="table-number-text">{{ row.item.tableNumber }}</span>
                </div>
              </template>

              <template #cell(status)="row">
                <span class="item-status-badge" :class="getStatusBadgeClass(row.item.status)">
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
                <div class="actions-cell">
                  <button 
                    type="button"
                    class="action-btn action-btn--icon action-btn--edit" 
                    @click="editTable(row.item)"
                    :title="$t('edit')"
                  >
                    <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--delete"
                    @click="confirmDeleteOne(row.item)"
                    :title="$t('deleteTableRow')"
                    :disabled="deletingTables"
                  >
                    <b-icon icon="trash-fill" class="action-icon"></b-icon>
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
                class="items-pagination"
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

      <!-- مسح طاولة / المحدد / الكل — نفس تصميم تأكيد الحذف في النظام -->
      <b-modal
        id="modal-delete-tables"
        :title="$t('deleteConfirmationModalTitle')"
        hide-header
        hide-footer
        class="users-modal"
        @hidden="onTableDeleteModalHidden"
      >
        <div class="modal-content-wrapper">
          <div class="delete-confirmation-content">
            <div class="delete-icon-wrapper">
              <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
            </div>
            <h3 class="delete-confirmation-title">{{ deleteModalHeading }}</h3>
            <p class="delete-confirmation-text">{{ deleteModalBody }}</p>
            <div class="delete-confirmation-actions">
              <button
                type="button"
                class="delete-confirm-button"
                :disabled="deletingTables"
                @click="executeTableDeleteConfirm"
              >
                <b-spinner v-if="deletingTables" small class="me-2"></b-spinner>
                <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("deleteButtonLabel") }}
              </button>
              <button
                type="button"
                class="delete-cancel-button"
                :disabled="deletingTables"
                @click="closeTableDeleteModal"
              >
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancelButtonLabel") }}
              </button>
            </div>
          </div>
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
      selectedIds: [],
      deletingTables: false,
      deleteModalMode: null,
      pendingDeleteIds: [],
    };
  },
  computed: {
    pageTableIds() {
      return this.tables
        .map((t) => this.tableRowId(t))
        .filter((id) => id != null);
    },
    allPageSelected() {
      if (!this.pageTableIds.length) return false;
      return this.pageTableIds.every((id) => this.selectedIds.includes(id));
    },
    somePageSelected() {
      return this.pageTableIds.some((id) => this.selectedIds.includes(id));
    },
    tableFields() {
      return [
        {
          key: "selected",
          label: "",
          thClass: "item-header-cell table-col-select",
          tdClass: "table-col-select",
        },
        {
          key: 'tableNumber',
          label: this.$t('tableNumber') || 'رقم الطاولة',
          sortable: false,
          thClass: 'item-header-cell'
        },
        {
          key: 'status',
          label: this.$t('status') || 'الحالة',
          sortable: false,
          thClass: 'item-header-cell'
        },
        {
          key: 'capacity',
          label: this.$t('capacity') || 'السعة',
          sortable: false,
          thClass: 'item-header-cell'
        },
        {
          key: 'zone',
          label: this.$t('zone') || 'المنطقة',
          sortable: false,
          thClass: 'item-header-cell'
        },
        {
          key: 'currentOrderId',
          label: this.$t('currentOrder') || 'الطلب الحالي',
          sortable: false,
          thClass: 'item-header-cell'
        },
        {
          key: 'actions',
          label: this.$t('actions') || 'الإجراءات',
          sortable: false,
          thClass: 'item-header-cell'
        }
      ];
    },
    deleteModalHeading() {
      const m = this.deleteModalMode;
      if (m === "one") return this.$t("deleteTableRow") || "مسح الطاولة";
      if (m === "selected") return this.$t("deleteSelectedTables") || "مسح المحدد";
      if (m === "all") return this.$t("deleteAllTables") || "مسح الكل";
      return "";
    },
    deleteModalBody() {
      const m = this.deleteModalMode;
      if (m === "one") {
        return this.$t("confirmDeleteTable") || "";
      }
      if (m === "selected") {
        const c = this.pendingDeleteIds.length;
        return (
          this.$t("confirmDeleteSelectedTables", { count: c }) ||
          `مسح ${c} طاولة؟`
        );
      }
      if (m === "all") {
        const c = this.totalItems;
        return this.statusFilter
          ? this.$t("confirmDeleteAllTablesFiltered", { count: c }) || ""
          : this.$t("confirmDeleteAllTables", { count: c }) || "";
      }
      return "";
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
          const rawItems = pagedData.items || pagedData.Items || [];
          this.tables = rawItems.map((t) => {
            const id = t.id ?? t.Id;
            return {
              ...t,
              id,
              currentOrderId: t.currentOrderId ?? t.CurrentOrderId ?? null,
            };
          });
          this.totalItems = pagedData.totalItems ?? pagedData.TotalItems ?? 0;
          this.totalPages = pagedData.totalPages ?? pagedData.TotalPages ?? 0;
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
      this.selectedIds = [];
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
    getStatusBadgeClass(status) {
      const map = {
        Available: "table-badge-available",
        Occupied: "table-badge-occupied",
        Reserved: "table-badge-reserved",
        OutOfService: "table-badge-out",
      };
      return map[status] || "";
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
    },
    tableRowId(table) {
      if (!table) return null;
      const id = table.id ?? table.Id;
      return id != null ? Number(id) : null;
    },
    isTableSelected(id) {
      if (id == null) return false;
      return this.selectedIds.includes(Number(id));
    },
    toggleTableSelected(id) {
      if (id == null) return;
      const n = Number(id);
      const i = this.selectedIds.indexOf(n);
      if (i >= 0) {
        this.selectedIds.splice(i, 1);
      } else {
        this.selectedIds.push(n);
      }
    },
    onSelectAllPageChange(checked) {
      const take = !!checked;
      const pageIds = this.pageTableIds;
      if (take) {
        const set = new Set([...this.selectedIds, ...pageIds]);
        this.selectedIds = [...set];
      } else {
        const pageSet = new Set(pageIds);
        this.selectedIds = this.selectedIds.filter((id) => !pageSet.has(id));
      }
    },
    confirmDeleteOne(table) {
      const id = this.tableRowId(table);
      if (id == null) return;
      this.deleteModalMode = "one";
      this.pendingDeleteIds = [id];
      this.$bvModal.show("modal-delete-tables");
    },
    confirmDeleteSelected() {
      if (this.selectedIds.length === 0) {
        this.$toast.error(
          this.$i18n.t("noTablesSelectedForDelete") ||
            "لم تحدد أي طاولات",
          { position: "top-right", timeout: 4000 }
        );
        return;
      }
      this.deleteModalMode = "selected";
      this.pendingDeleteIds = [...this.selectedIds];
      this.$bvModal.show("modal-delete-tables");
    },
    confirmDeleteAll() {
      if (this.totalItems === 0) return;
      this.deleteModalMode = "all";
      this.pendingDeleteIds = [];
      this.$bvModal.show("modal-delete-tables");
    },
    closeTableDeleteModal() {
      if (this.deletingTables) return;
      this.$bvModal.hide("modal-delete-tables");
    },
    onTableDeleteModalHidden() {
      if (!this.deletingTables) {
        this.deleteModalMode = null;
        this.pendingDeleteIds = [];
      }
    },
    executeTableDeleteConfirm() {
      if (this.deletingTables) return;
      const mode = this.deleteModalMode;
      const ids = [...this.pendingDeleteIds];
      this.$bvModal.hide("modal-delete-tables");
      if (mode === "all") {
        this.deleteAllTablesApi();
      } else if (ids.length) {
        this.deleteTablesByIds(ids);
      }
    },
    async deleteTablesByIds(ids) {
      if (!ids || !ids.length) return;
      this.deletingTables = true;
      try {
        if (ids.length === 1) {
          const response = await HTTP.delete(`Tables/${ids[0]}`);
          if (response.data.errorStatus) {
            this.$toast.error(response.data.message || this.$i18n.t("error"), {
              position: "top-right",
              timeout: 4000,
            });
          } else {
            this.$toast.success(
              response.data.message ||
                this.$i18n.t("tableUpdatedSuccessfully"),
              { position: "top-right", timeout: 4000 }
            );
            this.selectedIds = this.selectedIds.filter((x) => !ids.includes(x));
            if (this.tables.length <= 1 && this.currentPage > 1) {
              this.currentPage -= 1;
            }
            this.getTables();
          }
          return;
        }
        const response = await HTTP.post("Tables/bulk-delete", ids);
        if (response.data.errorStatus) {
          this.$toast.error(response.data.message || this.$i18n.t("error"), {
            position: "top-right",
            timeout: 4000,
          });
        } else {
          this.$toast.success(
            response.data.message || this.$i18n.t("tableUpdatedSuccessfully"),
            { position: "top-right", timeout: 4000 }
          );
          this.selectedIds = this.selectedIds.filter((x) => !ids.includes(x));
          if (
            this.tables.length <= ids.length &&
            this.currentPage > 1
          ) {
            this.currentPage -= 1;
          }
          this.getTables();
        }
      } catch (error) {
        this.$toast.error(
          error.response?.data?.message || this.$i18n.t("error"),
          { position: "top-right", timeout: 4000 }
        );
      } finally {
        this.deletingTables = false;
        this.deleteModalMode = null;
        this.pendingDeleteIds = [];
      }
    },
    async deleteAllTablesApi() {
      this.deletingTables = true;
      try {
        const params = {};
        if (this.statusFilter) {
          params.status = this.statusFilter;
        }
        const response = await HTTP.post("Tables/delete-all", null, {
          params,
        });
        if (response.data.errorStatus) {
          this.$toast.error(response.data.message || this.$i18n.t("error"), {
            position: "top-right",
            timeout: 4000,
          });
        } else {
          this.$toast.success(
            response.data.message ||
              this.$i18n.t("tableUpdatedSuccessfully"),
            { position: "top-right", timeout: 4000 }
          );
          this.selectedIds = [];
          this.currentPage = 1;
          this.getTables();
        }
      } catch (error) {
        this.$toast.error(
          error.response?.data?.message || this.$i18n.t("error"),
          { position: "top-right", timeout: 4000 }
        );
      } finally {
        this.deletingTables = false;
        this.deleteModalMode = null;
        this.pendingDeleteIds = [];
      }
    },
  },
};
</script>

<style scoped>
/* نفس أسلوب جدول الأصناف (ItemsView) والتصنيفات (CategoryView) */
.items-table-container {
  background: #ffffff;
  border-radius: 0.75rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  margin-top: 1.5rem;
}

.items-table {
  margin: 0;
}

.items-table >>> thead th {
  background-color: #f9fafb;
  color: #374151;
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 1rem;
  border-bottom: 2px solid #e5e7eb;
}

.items-table >>> tbody td {
  padding: 1rem;
  vertical-align: middle;
  border-bottom: 1px solid #f3f4f6;
}

.items-table >>> tbody tr:hover {
  background-color: #f9fafb;
}

.items-table >>> .table-col-select {
  width: 2.75rem;
  text-align: center;
  vertical-align: middle;
}

.table-number-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.table-list-icon {
  color: var(--primary-color);
  font-size: 1.25rem;
  flex-shrink: 0;
}

.table-number-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: #111827;
}

.item-status-badge {
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  display: inline-block;
}

.item-status-badge.table-badge-available {
  background-color: var(--success-light);
  color: var(--success-color);
}

.item-status-badge.table-badge-occupied {
  background-color: var(--danger-light);
  color: var(--danger-color);
}

.item-status-badge.table-badge-reserved {
  background-color: var(--warning-light);
  color: var(--warning-color);
}

.item-status-badge.table-badge-out {
  background-color: rgba(30, 41, 59, 0.08);
  color: var(--text-muted);
}

.table-capacity-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #374151;
  font-weight: 500;
  font-size: 0.9375rem;
}

.capacity-icon {
  color: #9ca3af;
  font-size: 1rem;
}

.order-id-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #374151;
  font-weight: 500;
  font-size: 0.9375rem;
}

.order-icon {
  color: #9ca3af;
  font-size: 1rem;
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

.items-pagination >>> .page-link {
  color: var(--text-primary);
  border-color: var(--border-color);
  background-color: var(--bg-tertiary);
}

.items-pagination >>> .page-item.active .page-link {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
}

.items-pagination >>> .page-link:hover {
  background-color: rgba(99, 102, 241, 0.1);
  border-color: var(--border-dark);
  color: var(--primary-color);
}

.tables-header-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.tables-action-btn.tables-action-btn--danger {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%) !important;
  color: #fff !important;
  border: none;
}

.tables-action-btn.tables-action-btn--danger:hover:not(:disabled) {
  filter: brightness(1.05);
}

.tables-action-btn.tables-action-btn--outline-danger {
  background: transparent !important;
  color: var(--danger-color, #dc2626) !important;
  border: 1px solid var(--danger-color, #dc2626) !important;
}

.tables-action-btn.tables-action-btn--outline-danger:hover:not(:disabled) {
  background: rgba(239, 68, 68, 0.08) !important;
}

.actions-cell {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  flex-wrap: wrap;
}
</style>

