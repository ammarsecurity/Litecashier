<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content customers-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="person-lines-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("customersManagement") }}</h1>
                  <p class="header-subtitle">{{ $t("customersManagementDescription") }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button
                  type="button"
                  class="btn-refresh"
                  @click="loadCustomers"
                  :disabled="loadingCustomers"
                >
                  <b-icon
                    icon="arrow-clockwise"
                    class="button-icon"
                    :class="{ spinning: loadingCustomers }"
                  ></b-icon>
                  <span class="button-text">{{ $t("refresh") }}</span>
                </button>
                <button type="button" class="users-add-button" @click="openAddModal">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addCustomer") }}</span>
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
                  <b-spinner small v-if="loadingCustomers"></b-spinner>
                  <template v-else>{{ customers.length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("customers") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingCustomers"></b-spinner>
                  <template v-else>{{ activeCustomersCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("active") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                <b-icon icon="x-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingCustomers"></b-spinner>
                  <template v-else>{{ inactiveCustomersCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("inactive") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="geo-alt-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loadingCustomers"></b-spinner>
                  <template v-else>{{ customersWithAddressCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("customersOverviewWithAddress") }}</div>
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
                  <h3 class="app-section-title">{{ $t("customers") }}</h3>
                  <p class="app-section-subtitle">{{ $t("customersListDescription") }}</p>
                </div>
              </div>
            </div>

            <div class="app-section-body customers-filters-body">
              <div class="customers-filters-grid">
                <div class="app-search-wrap app-search-wrap--wide">
                  <b-icon icon="search" class="app-search-icon"></b-icon>
                  <input
                    v-model="searchQuery"
                    type="search"
                    class="app-search-input"
                    :placeholder="$t('searchCustomersPlaceholder')"
                    autocomplete="off"
                  />
                </div>
                <div class="customers-filter-group">
                  <label class="customers-filter-label">
                    <b-icon icon="filter" class="me-1"></b-icon>
                    {{ $t("status") }}
                  </label>
                  <select v-model="statusFilter" class="customers-filter-select">
                    <option value="all">{{ $t("allStatuses") }}</option>
                    <option value="active">{{ $t("active") }}</option>
                    <option value="inactive">{{ $t("inactive") }}</option>
                  </select>
                </div>
              </div>
            </div>

            <div class="app-section-body app-section-body--no-padding">
              <div v-if="loadingCustomers" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") }}</span>
              </div>
              <div v-else-if="filteredCustomers.length > 0" class="report-table-container customers-table-wrap">
                <b-table
                  id="customers-table"
                  :items="filteredCustomers"
                  :fields="tableFields"
                  striped
                  hover
                  responsive
                  class="reports-table users-table"
                  :empty-text="$t('noCustomers')"
                >
                  <template #cell(name)="row">
                    <div class="customers-name-cell">
                      <b-icon icon="person-circle" class="customers-name-icon"></b-icon>
                      <span class="customers-name-text">{{ row.item.name }}</span>
                    </div>
                  </template>
                  <template #cell(phoneNumber)="row">
                    <a
                      v-if="row.item.phoneNumber"
                      :href="'tel:' + row.item.phoneNumber"
                      class="customers-phone-link"
                    >{{ row.item.phoneNumber }}</a>
                    <span v-else>—</span>
                  </template>
                  <template #cell(address)="row">
                    <span class="customers-cell-muted">{{ row.item.address || "—" }}</span>
                  </template>
                  <template #cell(notes)="row">
                    <span
                      v-if="row.item.notes"
                      class="customers-notes-preview"
                      :title="row.item.notes"
                    >{{ row.item.notes }}</span>
                    <span v-else class="customers-cell-muted">—</span>
                  </template>
                  <template #cell(isActive)="row">
                    <span
                      class="customers-status-pill"
                      :class="row.item.isActive !== false ? 'customers-status-pill--active' : 'customers-status-pill--inactive'"
                    >
                      <b-icon :icon="row.item.isActive !== false ? 'check-circle-fill' : 'x-circle-fill'"></b-icon>
                      {{ row.item.isActive !== false ? $t("active") : $t("inactive") }}
                    </span>
                  </template>
                  <template #cell(actions)="row">
                    <div class="actions-cell">
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--view"
                        @click="viewDeferredPayments(row.item)"
                        :title="$t('viewDeferredPayments') || 'عرض الدفع اللاحق'"
                      >
                        <b-icon icon="wallet2" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        @click="editCustomer(row.item)"
                        :title="$t('edit')"
                      >
                        <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--delete"
                        @click="confirmDeleteCustomer(row.item)"
                        :title="$t('delete')"
                      >
                        <b-icon icon="trash-fill" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </template>
                </b-table>
              </div>
              <div v-else class="empty-state customers-empty-state">
                <b-icon icon="people" class="empty-icon"></b-icon>
                <p>{{ emptyStateMessage }}</p>
                <button
                  v-if="!hasActiveFilters"
                  type="button"
                  class="empty-state-btn"
                  @click="openAddModal"
                >
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addFirstCustomer") }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <b-modal
        v-model="showCustomerModal"
        hide-header
        hide-footer
        class="users-modal"
        centered
        size="lg"
        @hidden="resetCustomerForm"
      >
        <div class="modal-content-wrapper">
          <div class="modal-title-row">
            <span class="modal-title-icon">
              <b-icon :icon="selectedCustomer ? 'pencil-square' : 'person-plus-fill'"></b-icon>
            </span>
            <h2 class="modal-title">
              {{ selectedCustomer ? $t("editCustomer") : $t("addCustomer") }}
            </h2>
          </div>
          <form class="users-form" @submit.prevent="saveCustomer">
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                  {{ $t("customerNameField") }} <span class="required">*</span>
                </label>
                <input
                  v-model="customerForm.name"
                  type="text"
                  class="users-form-input"
                  required
                  :placeholder="$t('enterCustomerNamePlaceholder')"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                  {{ $t("phoneNumber") }} <span class="required">*</span>
                </label>
                <input
                  v-model="customerForm.phoneNumber"
                  type="text"
                  class="users-form-input"
                  required
                  :placeholder="$t('enterPhoneNumber')"
                />
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="geo-alt-fill" class="form-label-icon"></b-icon>
                {{ $t("address") }}
              </label>
              <input
                v-model="customerForm.address"
                type="text"
                class="users-form-input"
                :placeholder="$t('enterAddress')"
              />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="chat-left-text-fill" class="form-label-icon"></b-icon>
                {{ $t("notes") }}
              </label>
              <textarea
                v-model="customerForm.notes"
                class="users-form-input"
                rows="3"
                :placeholder="$t('customerNotesPlaceholder')"
              ></textarea>
            </div>
            <div class="form-toggle-cards form-toggle-cards--stack">
              <label
                class="form-toggle-card"
                :class="{ 'form-toggle-card--on': customerForm.isActive }"
              >
                <input v-model="customerForm.isActive" type="checkbox" class="form-toggle-card-input" />
                <span class="form-toggle-card-body">
                  <span class="form-toggle-card-icon form-toggle-card-icon--success">
                    <b-icon icon="check-circle-fill"></b-icon>
                  </span>
                  <span class="form-toggle-card-text">
                    <span class="form-toggle-card-title">{{ $t("customerActive") }}</span>
                    <span class="form-toggle-card-desc">{{ $t("customerActiveHint") }}</span>
                  </span>
                </span>
                <span class="form-toggle-switch" aria-hidden="true"></span>
              </label>
            </div>
            <div class="users-form-actions">
              <button
                type="button"
                class="users-form-cancel-button"
                :disabled="savingCustomer"
                @click="showCustomerModal = false"
              >
                {{ $t("cancel") }}
              </button>
              <button type="submit" class="users-form-submit-button" :disabled="savingCustomer">
                <b-spinner v-if="savingCustomer" small class="me-2"></b-spinner>
                {{
                  savingCustomer
                    ? selectedCustomer
                      ? $t("updating")
                      : $t("adding")
                    : selectedCustomer
                      ? $t("update")
                      : $t("add")
                }}
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
import { HTTP } from "../http/api.js";

export default {
  name: "CustomersView",
  components: { AppHeader },
  data() {
    return {
      loadingCustomers: false,
      customers: [],
      searchQuery: "",
      statusFilter: "all",
      searchDebounce: null,
      showCustomerModal: false,
      savingCustomer: false,
      selectedCustomer: null,
      customerForm: {
        name: "",
        phoneNumber: "",
        address: "",
        notes: "",
        isActive: true,
      },
    };
  },
  computed: {
    tableFields() {
      return [
        { key: "name", label: this.$t("customerNameField"), sortable: true },
        { key: "phoneNumber", label: this.$t("phoneNumber"), sortable: true },
        { key: "address", label: this.$t("address") },
        { key: "notes", label: this.$t("notes") },
        { key: "isActive", label: this.$t("status"), class: "text-center" },
        { key: "actions", label: this.$t("actions"), class: "text-center" },
      ];
    },
    filteredCustomers() {
      if (this.statusFilter === "active") {
        return this.customers.filter((c) => c.isActive !== false);
      }
      if (this.statusFilter === "inactive") {
        return this.customers.filter((c) => c.isActive === false);
      }
      return this.customers;
    },
    activeCustomersCount() {
      return this.customers.filter((c) => c.isActive !== false).length;
    },
    inactiveCustomersCount() {
      return this.customers.filter((c) => c.isActive === false).length;
    },
    customersWithAddressCount() {
      return this.customers.filter((c) => (c.address || "").trim()).length;
    },
    hasActiveFilters() {
      return !!(this.searchQuery || "").trim() || this.statusFilter !== "all";
    },
    emptyStateMessage() {
      if (this.hasActiveFilters) {
        return this.$t("noResults");
      }
      return this.$t("noCustomers");
    },
  },
  watch: {
    searchQuery() {
      if (this.searchDebounce) {
        clearTimeout(this.searchDebounce);
      }
      this.searchDebounce = setTimeout(() => {
        this.loadCustomers();
      }, 350);
    },
  },
  mounted() {
    this.loadCustomers();
  },
  beforeDestroy() {
    if (this.searchDebounce) {
      clearTimeout(this.searchDebounce);
    }
  },
  methods: {
    async loadCustomers() {
      try {
        this.loadingCustomers = true;
        const params = {};
        const q = (this.searchQuery || "").trim();
        if (q) {
          params.search = q;
        }
        const response = await HTTP.get("Customers", { params });
        if (response.data && !response.data.errorStatus) {
          this.customers = response.data.data || [];
        } else {
          this.customers = [];
        }
      } catch (e) {
        console.error(e);
        this.customers = [];
        this.$notify.error(this.$t("failedToLoadCustomers"), {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === "ar",
        });
      } finally {
        this.loadingCustomers = false;
      }
    },
    openAddModal() {
      this.selectedCustomer = null;
      this.resetCustomerForm();
      this.showCustomerModal = true;
    },
    viewDeferredPayments(c) {
      this.$router.push({ path: "/deferred-payments", query: { customerId: c.id } });
    },
    editCustomer(c) {
      this.selectedCustomer = c;
      this.customerForm = {
        name: c.name || "",
        phoneNumber: c.phoneNumber || "",
        address: c.address || "",
        notes: c.notes || "",
        isActive: c.isActive !== false,
      };
      this.showCustomerModal = true;
    },
    async saveCustomer() {
      const name = (this.customerForm.name || "").trim();
      const phone = (this.customerForm.phoneNumber || "").trim();
      if (!name) {
        this.$notify.warning(this.$t("pleaseEnterCustomerName"), {
          position: "top-right",
          rtl: this.$i18n.locale === "ar",
        });
        return;
      }
      if (!phone) {
        this.$notify.warning(this.$t("pleaseEnterPhoneNumber"), {
          position: "top-right",
          rtl: this.$i18n.locale === "ar",
        });
        return;
      }
      const payload = {
        name,
        phoneNumber: phone,
        address: (this.customerForm.address || "").trim() || null,
        notes: (this.customerForm.notes || "").trim() || null,
        isActive: this.customerForm.isActive,
      };
      try {
        this.savingCustomer = true;
        let response;
        if (this.selectedCustomer) {
          response = await HTTP.put(`Customers/${this.selectedCustomer.id}`, payload);
        } else {
          response = await HTTP.post("Customers", payload);
        }
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(
            this.selectedCustomer ? this.$t("customerUpdatedSuccess") : this.$t("customerAddedSuccess"),
            { position: "top-right", timeout: 3000, rtl: this.$i18n.locale === "ar" }
          );
          this.showCustomerModal = false;
          this.resetCustomerForm();
          this.loadCustomers();
        } else {
          this.$notify.error(response.data?.message || this.$t("customerSaveFailed"), {
            position: "top-right",
            timeout: 4000,
            rtl: this.$i18n.locale === "ar",
          });
        }
      } catch (err) {
        this.$notify.error(err.response?.data?.message || this.$t("customerSaveFailed"), {
          position: "top-right",
          timeout: 4000,
          rtl: this.$i18n.locale === "ar",
        });
      } finally {
        this.savingCustomer = false;
      }
    },
    async confirmDeleteCustomer(c) {
      const ok = await this.$confirm({
        title: this.$t("delete"),
        message: this.$t("confirmDeleteCustomer", { name: c.name || "" }),
      });
      if (ok) {
        this.deleteCustomer(c.id);
      }
    },
    async deleteCustomer(id) {
      try {
        const response = await HTTP.delete(`Customers/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$notify.success(this.$t("customerDeletedSuccess"), {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === "ar",
          });
          this.loadCustomers();
        } else {
          this.$notify.error(response.data?.message || this.$t("customerDeleteFailed"), {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === "ar",
          });
        }
      } catch (err) {
        this.$notify.error(err.response?.data?.message || this.$t("customerDeleteFailed"), {
          position: "top-right",
          timeout: 3000,
          rtl: this.$i18n.locale === "ar",
        });
      }
    },
    resetCustomerForm() {
      this.selectedCustomer = null;
      this.customerForm = {
        name: "",
        phoneNumber: "",
        address: "",
        notes: "",
        isActive: true,
      };
    },
  },
};
</script>

<style scoped>
.customers-filters-body {
  padding-bottom: 0;
  border-bottom: 1px solid var(--border-color);
}

.customers-filters-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(180px, 220px);
  gap: 1rem;
  align-items: end;
}

.customers-filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.customers-filter-label {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.customers-filter-select {
  width: 100%;
  height: 42px;
  padding: 0 0.75rem;
  border-radius: 0.6rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.9rem;
}

.customers-name-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
}

.customers-name-icon {
  flex-shrink: 0;
  color: var(--primary-color);
  font-size: 1.25rem;
}

.customers-name-text {
  font-weight: 600;
  color: var(--text-primary);
}

.customers-phone-link {
  color: var(--primary-color);
  text-decoration: none;
  font-weight: 500;
}

.customers-phone-link:hover {
  text-decoration: underline;
}

.customers-cell-muted {
  color: var(--text-secondary);
}

.customers-notes-preview {
  display: inline-block;
  max-width: 220px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--text-secondary);
}

.customers-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.25rem 0.65rem;
  border-radius: 999px;
  font-size: 0.8125rem;
  font-weight: 600;
  white-space: nowrap;
}

.customers-status-pill--active {
  background: color-mix(in srgb, #16a34a 14%, var(--bg-primary));
  color: #16a34a;
  border: 1px solid color-mix(in srgb, #16a34a 35%, var(--border-color));
}

.customers-status-pill--inactive {
  background: color-mix(in srgb, #dc3545 12%, var(--bg-primary));
  color: #dc3545;
  border: 1px solid color-mix(in srgb, #dc3545 35%, var(--border-color));
}

.customers-empty-state {
  padding: 2.5rem 1rem;
}

@media (max-width: 768px) {
  .customers-filters-grid {
    grid-template-columns: 1fr;
  }

  .app-section-header--toolbar {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
