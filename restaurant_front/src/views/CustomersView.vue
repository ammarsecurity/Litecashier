<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="customers-page-container">
        <div class="customers-page-content">
          <div class="users-header-section">
            <div class="users-header-content">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="person-lines-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("customersManagement") }}</h1>
                  <p class="header-subtitle">{{ $t("customersManagementDescription") }}</p>
                </div>
              </div>
              <button type="button" class="users-add-button btn-add-customer-header" @click="openAddModal">
                <b-icon icon="plus-circle" class="me-1"></b-icon>
                {{ $t("addCustomer") }}
              </button>
            </div>
          </div>

          <div class="customers-toolbar">
            <div class="customers-search-wrap">
              <b-icon icon="search" class="customers-search-icon"></b-icon>
              <input
                v-model="searchQuery"
                type="search"
                class="customers-search-input"
                :placeholder="$t('searchCustomersPlaceholder')"
                autocomplete="off"
              />
            </div>
          </div>

          <div class="customers-management-card">
            <div class="customers-management-header">
              <div class="customers-management-header-content">
                <div class="customers-management-title-wrapper">
                  <div class="customers-management-icon-wrapper">
                    <b-icon icon="people-fill" class="customers-management-icon"></b-icon>
                  </div>
                  <div>
                    <h3 class="customers-management-title">{{ $t("customers") }}</h3>
                    <p class="customers-management-subtitle">{{ $t("customersListDescription") }}</p>
                  </div>
                </div>
              </div>
            </div>
            <div class="customers-management-body">
              <div v-if="loadingCustomers" class="loading-state">
                <b-spinner small></b-spinner>
                <span>{{ $t("loading") }}</span>
              </div>
              <div v-else-if="customers.length > 0" class="users-grid-container customers-cards-wrap">
                <div class="users-grid">
                  <div v-for="c in customers" :key="c.id" class="user-card">
                    <div class="user-card-header">
                      <div class="user-avatar">
                        <b-icon icon="person-circle" class="avatar-icon"></b-icon>
                      </div>
                      <h3 class="user-name">{{ c.name }}</h3>
                      <div
                        v-if="!c.isActive"
                        class="status-icon-badge inactive-badge-icon customer-status-badge"
                        :title="$t('inactive')"
                      >
                        <b-icon icon="x-circle-fill"></b-icon>
                      </div>
                      <div v-else class="status-icon-badge active-badge-icon customer-status-badge" :title="$t('active')">
                        <b-icon icon="check-circle-fill"></b-icon>
                      </div>
                    </div>
                    <div class="user-card-body">
                      <div class="user-info-item">
                        <b-icon icon="telephone-fill" class="info-icon"></b-icon>
                        <span class="info-label">{{ $t("phoneNumber") }}</span>
                        <span class="info-value">{{ c.phoneNumber }}</span>
                      </div>
                      <div v-if="c.address" class="user-info-item">
                        <b-icon icon="geo-alt-fill" class="info-icon"></b-icon>
                        <span class="info-label">{{ $t("address") }}</span>
                        <span class="info-value">{{ c.address }}</span>
                      </div>
                      <div v-if="c.notes" class="user-info-item user-info-item--notes">
                        <b-icon icon="chat-left-text-fill" class="info-icon"></b-icon>
                        <span class="info-label">{{ $t("notes") }}</span>
                        <span class="info-value">{{ c.notes }}</span>
                      </div>
                    </div>
                    <div class="user-card-footer customers-card-footer">
                      <button
                        type="button"
                        class="user-action-button action-btn action-btn--edit"
                        @click="editCustomer(c)"
                      >
                        <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                        <span>{{ $t("edit") }}</span>
                      </button>
                      <button
                        type="button"
                        class="user-action-button action-btn action-btn--delete"
                        @click="confirmDeleteCustomer(c)"
                      >
                        <b-icon icon="trash-fill" class="action-icon"></b-icon>
                        <span>{{ $t("delete") }}</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="empty-state">
                <b-icon icon="people" class="empty-icon"></b-icon>
                <p>{{ $t("noCustomers") }}</p>
                <button type="button" class="btn-add-first-customer" @click="openAddModal">
                  <b-icon icon="plus-circle" class="me-2"></b-icon>
                  {{ $t("addFirstCustomer") }}
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
          <h2 class="modal-title">
            {{ selectedCustomer ? $t("editCustomer") : $t("addCustomer") }}
          </h2>
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
              <input v-model="customerForm.address" type="text" class="users-form-input" :placeholder="$t('enterAddress')" />
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
            <div class="users-form-group customers-active-row">
              <label class="customers-checkbox-label">
                <input v-model="customerForm.isActive" type="checkbox" />
                <span>{{ $t("customerActive") }}</span>
              </label>
            </div>
            <div class="users-form-actions">
              <button type="button" class="users-form-cancel-button" :disabled="savingCustomer" @click="showCustomerModal = false">
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
        this.$toast.error(this.$t("failedToLoadCustomers"), {
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
        this.$toast.warning(this.$t("pleaseEnterCustomerName"), { position: "top-right", rtl: this.$i18n.locale === "ar" });
        return;
      }
      if (!phone) {
        this.$toast.warning(this.$t("pleaseEnterPhoneNumber"), { position: "top-right", rtl: this.$i18n.locale === "ar" });
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
          this.$toast.success(
            this.selectedCustomer ? this.$t("customerUpdatedSuccess") : this.$t("customerAddedSuccess"),
            { position: "top-right", timeout: 3000, rtl: this.$i18n.locale === "ar" }
          );
          this.showCustomerModal = false;
          this.resetCustomerForm();
          this.loadCustomers();
        } else {
          this.$toast.error(response.data?.message || this.$t("customerSaveFailed"), {
            position: "top-right",
            timeout: 4000,
            rtl: this.$i18n.locale === "ar",
          });
        }
      } catch (err) {
        this.$toast.error(err.response?.data?.message || this.$t("customerSaveFailed"), {
          position: "top-right",
          timeout: 4000,
          rtl: this.$i18n.locale === "ar",
        });
      } finally {
        this.savingCustomer = false;
      }
    },
    confirmDeleteCustomer(c) {
      const msg = this.$t("confirmDeleteCustomer", { name: c.name || "" });
      if (typeof msg === "string" && window.confirm(msg)) {
        this.deleteCustomer(c.id);
      }
    },
    async deleteCustomer(id) {
      try {
        const response = await HTTP.delete(`Customers/${id}`);
        if (response.data && !response.data.errorStatus) {
          this.$toast.success(this.$t("customerDeletedSuccess"), {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === "ar",
          });
          this.loadCustomers();
        } else {
          this.$toast.error(response.data?.message || this.$t("customerDeleteFailed"), {
            position: "top-right",
            timeout: 3000,
            rtl: this.$i18n.locale === "ar",
          });
        }
      } catch (err) {
        this.$toast.error(err.response?.data?.message || this.$t("customerDeleteFailed"), {
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
.customers-page-container {
  padding: 2rem;
  min-height: 100vh;
  background: var(--bg-primary, #f5f5f5);
}

.customers-page-content {
  max-width: 1400px;
  margin: 0 auto;
}

.btn-add-customer-header {
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

.btn-add-customer-header:hover {
  background: var(--primary-hover, #0056b3);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md, 0 4px 8px rgba(0, 0, 0, 0.15));
}

.customers-toolbar {
  margin-bottom: 1rem;
}

.customers-search-wrap {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  max-width: 420px;
  padding: 0.5rem 0.85rem;
  border-radius: 0.75rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
}

.customers-search-icon {
  color: var(--text-secondary);
  flex-shrink: 0;
}

.customers-search-input {
  flex: 1;
  border: none;
  background: transparent;
  color: var(--text-primary);
  font-size: 0.9375rem;
  outline: none;
  min-width: 0;
}

.customers-management-card {
  background: var(--bg-primary);
  border-radius: 1rem;
  padding: 0;
  margin-bottom: 2rem;
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-color);
  overflow: hidden;
}

.customers-management-header {
  padding: 1.5rem;
  background: var(--bg-primary);
  border-bottom: 1px solid var(--border-color);
}

.customers-management-title-wrapper {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.customers-management-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 0.75rem;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.15) 0%, rgba(167, 139, 250, 0.15) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.customers-management-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.customers-management-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 0.25rem 0;
  line-height: 1.2;
}

.customers-management-subtitle {
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin: 0;
  line-height: 1.4;
}

.customers-management-body {
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

.customers-cards-wrap .customers-card-footer {
  grid-template-columns: 1fr 1fr;
}

.customer-status-badge {
  margin-inline-start: auto;
}

.user-info-item--notes .info-value {
  white-space: pre-wrap;
  word-break: break-word;
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

.btn-add-first-customer {
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

.btn-add-first-customer:hover {
  background: var(--primary-hover, #0056b3);
}

.modal-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.customers-active-row {
  margin-top: 0.25rem;
}

.customers-checkbox-label {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  font-weight: 600;
  color: var(--text-primary);
}

.customers-checkbox-label input {
  width: 1.1rem;
  height: 1.1rem;
  accent-color: var(--primary-color);
}

@media (max-width: 768px) {
  .modal-form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
