<template>
  <b-overlay :show="false" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="lightning-charge-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("shortcutItemsTitle") }}</h1>
                  <p class="header-subtitle">{{ $t("shortcutItemsSubtitle") }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button
                  type="button"
                  class="btn-refresh"
                  @click="loadItems"
                  :disabled="loading"
                >
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
                <button type="button" class="users-add-button" @click="openCreate">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addShortcutItem") }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="lightning-charge-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ totalItems }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("shortcutItemsCount") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="receipt"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value app-overview-stat-value--text">
                  {{ $t("shortcutItemsNoStock") }}
                </div>
                <div class="app-overview-stat-label">{{ $t("shortcutItemsNoStockHint") }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="list-ul"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("shortcutItemsList") }}</h3>
                  <p class="app-section-subtitle">{{ $t("shortcutItemsListHint") }}</p>
                </div>
              </div>
            </div>

            <div class="app-filters-panel app-filters-panel--inset">
              <label class="app-filter-field app-filter-field--grow">
                <span class="app-filter-label">{{ $t("search") || "بحث" }}</span>
                <div class="users-search-container">
                  <b-icon icon="search" class="search-icon"></b-icon>
                  <input
                    v-model="searchQuery"
                    type="search"
                    class="users-search-input"
                    :placeholder="$t('shortcutItemsSearchPlaceholder')"
                    autocomplete="off"
                    @input="debounceSearch"
                  />
                </div>
              </label>
            </div>

            <div class="app-section-body app-section-body--no-padding">
              <div v-if="loading" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else class="report-table-container">
                <b-table
                  id="shortcut-items-table"
                  :items="items"
                  :fields="tableFields"
                  striped
                  hover
                  responsive
                  class="reports-table"
                  :empty-text="$t('noShortcutItems')"
                >
                  <template #cell(name)="row">
                    <div class="shortcut-item-name-cell">
                      <strong>{{ row.item.name }}</strong>
                      <span v-if="row.item.description" class="shortcut-item-desc">{{ row.item.description }}</span>
                    </div>
                  </template>
                  <template #cell(sellingPrice)="row">
                    {{ formatPrice(row.item.sellingPrice) }} {{ $t("currency") }}
                  </template>
                  <template #cell(wholesalePrice)="row">
                    <template v-if="Number(row.item.wholesalePrice) > 0">
                      {{ formatPrice(row.item.wholesalePrice) }} {{ $t("currency") }}
                    </template>
                    <template v-else>—</template>
                  </template>
                  <template #cell(actions)="row">
                    <div class="actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        @click="openEdit(row.item)"
                        :title="$t('edit') || 'تعديل'"
                      >
                        <b-icon icon="pencil-square" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--delete"
                        @click="confirmDelete(row.item)"
                        :title="$t('delete') || 'حذف'"
                      >
                        <b-icon icon="trash" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </template>
                </b-table>
              </div>
            </div>

            <div v-if="!loading" class="app-section-body">
              <div class="users-pagination-section">
                <b-pagination
                  v-model="pageNumber"
                  :total-rows="totalItems"
                  :per-page="pageSize"
                  aria-controls="shortcut-items-table"
                  class="users-pagination"
                  @change="loadItems"
                ></b-pagination>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal
      v-model="showFormModal"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="md"
      @hidden="resetForm"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">
          {{ selectedItem ? $t("editShortcutItem") : $t("addShortcutItem") }}
        </h2>
        <form class="users-form" @submit.prevent="saveItem">
          <div class="users-form-group">
            <label class="users-form-label">
              {{ $t("shortcutItemName") }} <span class="required">*</span>
            </label>
            <input
              v-model.trim="form.name"
              type="text"
              class="users-form-input"
              :placeholder="$t('shortcutItemNamePlaceholder')"
              required
              maxlength="200"
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              {{ $t("shortcutItemPrice") }} <span class="required">*</span>
            </label>
            <input
              v-model.number="form.sellingPrice"
              type="number"
              min="0"
              step="1"
              class="users-form-input"
              :placeholder="$t('enterAmount') || 'أدخل المبلغ'"
              required
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("shortcutItemWholesale") }}</label>
            <input
              v-model.number="form.wholesalePrice"
              type="number"
              min="0"
              step="1"
              class="users-form-input"
              :placeholder="$t('shortcutItemWholesaleHint')"
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("shortcutItemDescription") }}</label>
            <textarea
              v-model.trim="form.description"
              class="users-form-input"
              rows="3"
              maxlength="500"
              :placeholder="$t('shortcutItemDescriptionPlaceholder')"
            ></textarea>
          </div>
          <div class="users-form-actions">
            <button type="submit" class="users-form-submit-button" :disabled="saving">
              <b-spinner small v-if="saving" class="me-2"></b-spinner>
              <b-icon v-else icon="check-circle-fill" class="me-2"></b-icon>
              {{ saving ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
            <button type="button" class="users-form-cancel-button" @click="showFormModal = false">
              <b-icon icon="x-circle-fill" class="me-2"></b-icon>
              {{ $t("cancel") || "إلغاء" }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <b-modal
      v-model="showDeleteModal"
      :title="$t('confirmDelete') || 'تأكيد الحذف'"
      @ok="deleteItem"
      @cancel="showDeleteModal = false"
      ok-variant="danger"
      cancel-variant="secondary"
      :ok-disabled="deleting"
    >
      <p>{{ $t("confirmDeleteShortcutItem") }}</p>
    </b-modal>
  </b-overlay>
</template>

<script>
import AppHeader from "../components/Layout/AppHeader.vue";
import { HTTP } from "../http/api.js";

export default {
  name: "ShortcutItemsView",
  components: { AppHeader },
  data() {
    return {
      items: [],
      totalItems: 0,
      pageNumber: 1,
      pageSize: 12,
      searchQuery: "",
      searchTimer: null,
      loading: false,
      saving: false,
      deleting: false,
      showFormModal: false,
      showDeleteModal: false,
      selectedItem: null,
      itemToDelete: null,
      form: {
        name: "",
        description: "",
        sellingPrice: 0,
        wholesalePrice: 0,
      },
    };
  },
  computed: {
    tableFields() {
      return [
        { key: "name", label: this.$t("shortcutItemName") },
        { key: "sellingPrice", label: this.$t("shortcutItemPrice") },
        { key: "wholesalePrice", label: this.$t("shortcutItemWholesale") },
        { key: "actions", label: this.$t("actions") || "العمليات" },
      ];
    },
  },
  mounted() {
    this.loadItems();
  },
  beforeDestroy() {
    clearTimeout(this.searchTimer);
  },
  methods: {
    formatPrice(price) {
      return Number(price || 0).toLocaleString("en-EG");
    },
    toastError(message) {
      this.$bvToast.toast(message, {
        title: this.$t("error") || "خطأ",
        variant: "danger",
        solid: true,
      });
    },
    toastSuccess(message) {
      this.$bvToast.toast(message, {
        title: this.$t("success") || "تم",
        variant: "success",
        solid: true,
      });
    },
    mapApiError(message) {
      if (message === "shortcutItemExists") return this.$t("shortcutItemExists");
      if (message === "shortcutItemNotFound") return this.$t("shortcutItemNotFound");
      return message || this.$t("error");
    },
    debounceSearch() {
      clearTimeout(this.searchTimer);
      this.searchTimer = setTimeout(() => {
        this.pageNumber = 1;
        this.loadItems();
      }, 300);
    },
    async loadItems() {
      this.loading = true;
      try {
        const params = new URLSearchParams({
          pageNumber: String(this.pageNumber - 1),
          pageSize: String(this.pageSize),
        });
        if (this.searchQuery.trim()) params.append("search", this.searchQuery.trim());
        const response = await HTTP.get(`ShortcutItems?${params.toString()}`);
        if (response.data && !response.data.errorStatus) {
          this.items = response.data.data.items || [];
          this.totalItems = response.data.data.totalItems || 0;
        } else {
          this.toastError(this.mapApiError(response.data?.message));
        }
      } catch (error) {
        this.toastError(this.mapApiError(error.response?.data?.message));
      } finally {
        this.loading = false;
      }
    },
    openCreate() {
      this.selectedItem = null;
      this.resetForm();
      this.showFormModal = true;
    },
    openEdit(item) {
      this.selectedItem = item;
      this.form = {
        name: item.name || "",
        description: item.description || "",
        sellingPrice: Number(item.sellingPrice) || 0,
        wholesalePrice: Number(item.wholesalePrice) || 0,
      };
      this.showFormModal = true;
    },
    resetForm() {
      if (this.showFormModal) return;
      this.selectedItem = null;
      this.form = {
        name: "",
        description: "",
        sellingPrice: 0,
        wholesalePrice: 0,
      };
    },
    async saveItem() {
      if (!this.form.name || this.form.sellingPrice < 0) return;
      this.saving = true;
      try {
        const payload = {
          name: this.form.name,
          description: this.form.description || null,
          sellingPrice: Number(this.form.sellingPrice) || 0,
          wholesalePrice: Number(this.form.wholesalePrice) || 0,
        };
        const response = this.selectedItem
          ? await HTTP.put(`ShortcutItems/${this.selectedItem.id}`, payload)
          : await HTTP.post("ShortcutItems", payload);
        if (response.data && !response.data.errorStatus) {
          this.showFormModal = false;
          this.toastSuccess(
            this.selectedItem ? this.$t("shortcutItemUpdated") : this.$t("shortcutItemAdded")
          );
          this.loadItems();
        } else {
          this.toastError(this.mapApiError(response.data?.message));
        }
      } catch (error) {
        this.toastError(this.mapApiError(error.response?.data?.message));
      } finally {
        this.saving = false;
      }
    },
    confirmDelete(item) {
      this.itemToDelete = item;
      this.showDeleteModal = true;
    },
    async deleteItem() {
      if (!this.itemToDelete) return;
      this.deleting = true;
      try {
        const response = await HTTP.delete(`ShortcutItems/${this.itemToDelete.id}`);
        if (response.data && !response.data.errorStatus) {
          this.showDeleteModal = false;
          this.toastSuccess(this.$t("shortcutItemDeleted"));
          if (this.items.length === 1 && this.pageNumber > 1) this.pageNumber -= 1;
          this.loadItems();
        } else {
          this.toastError(this.mapApiError(response.data?.message));
        }
      } catch (error) {
        this.toastError(this.mapApiError(error.response?.data?.message));
      } finally {
        this.deleting = false;
        this.itemToDelete = null;
      }
    },
  },
};
</script>

<style scoped>
.shortcut-item-name-cell {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}
.shortcut-item-desc {
  font-size: 0.8rem;
  color: #64748b;
}
</style>
