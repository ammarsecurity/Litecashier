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
                <div class="app-overview-stat-value">{{ customers.length }}</div>
                <div class="app-overview-stat-label">{{ $t("customers") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ activeCustomersCount }}</div>
                <div class="app-overview-stat-label">{{ $t("active") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                <b-icon icon="x-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ inactiveCustomersCount }}</div>
                <div class="app-overview-stat-label">{{ $t("inactive") }}</div>
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
            </div>
            <div class="app-section-body">
              <div v-if="loadingCustomers" class="loading-state">
                <b-spinner small></b-spinner>
                <span>{{ $t("loading") }}</span>
              </div>
              <div v-else-if="customers.length > 0" class="app-cards-grid">
                <div v-for="c in customers" :key="c.id" class="app-item-card">
                  <div class="app-item-card-header">
                    <div class="app-item-card-title">
                      <b-icon icon="person-circle" class="app-item-card-icon"></b-icon>
                      <h4>{{ c.name }}</h4>
                    </div>
                    <div
                      v-if="!c.isActive"
                      class="status-icon-badge inactive-badge-icon"
                      :title="$t('inactive')"
                    >
                      <b-icon icon="x-circle-fill"></b-icon>
                    </div>
                    <div v-else class="status-icon-badge active-badge-icon" :title="$t('active')">
                      <b-icon icon="check-circle-fill"></b-icon>
                    </div>
                    <div class="app-item-card-actions">
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--view"
                        @click="viewDeferredPayments(c)"
                        :title="$t('viewDeferredPayments') || 'عرض الدفع اللاحق'"
                      >
                        <b-icon icon="wallet2" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        @click="editCustomer(c)"
                        :title="$t('edit')"
                      >
                        <b-icon icon="pencil" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--delete"
                        @click="confirmDeleteCustomer(c)"
                        :title="$t('delete')"
                      >
                        <b-icon icon="trash" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </div>
                  <div class="app-item-card-body">
                    <div class="app-info-row">
                      <b-icon icon="telephone-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("phoneNumber") }}</span>
                      <span class="info-value">{{ c.phoneNumber }}</span>
                    </div>
                    <div v-if="c.address" class="app-info-row">
                      <b-icon icon="geo-alt-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("address") }}</span>
                      <span class="info-value">{{ c.address }}</span>
                    </div>
                    <div v-if="c.notes" class="app-info-row">
                      <b-icon icon="chat-left-text-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("notes") }}</span>
                      <span class="info-value">{{ c.notes }}</span>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="empty-state">
                <b-icon icon="people" class="empty-icon"></b-icon>
                <p>{{ $t("noCustomers") }}</p>
                <button type="button" class="empty-state-btn" @click="openAddModal">
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
            <div class="form-toggle-cards">
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
                    <span class="form-toggle-card-desc">{{ $t("customerActiveHint") || "العميل متاح للطلبات والتوصيل" }}</span>
                  </span>
                </span>
                <span class="form-toggle-switch" aria-hidden="true"></span>
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
  computed: {
    activeCustomersCount() {
      return this.customers.filter((c) => c.isActive !== false).length;
    },
    inactiveCustomersCount() {
      return this.customers.filter((c) => c.isActive === false).length;
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
    async confirmDeleteCustomer(c) {
      const ok = await this.$confirm({
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
.customers-page .app-item-card-header {
  display: grid;
  grid-template-columns: 1fr auto;
  grid-template-rows: auto auto;
  gap: 0.45rem 0.5rem;
  align-items: start;
}

.customers-page .app-item-card-title {
  grid-column: 1;
  grid-row: 1;
}

.customers-page .status-icon-badge {
  grid-column: 1;
  grid-row: 2;
  justify-self: start;
}

.customers-page .app-item-card-actions {
  grid-column: 2;
  grid-row: 1 / span 2;
}

@media (max-width: 768px) {
  .app-section-header--toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .app-search-wrap--wide {
    max-width: 100%;
  }
}
</style>
