<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
            <div class="app-page-container">
                <div class="app-page-content brands-page">
                    <div class="users-header-section">
                        <div class="users-header-content app-header-row">
                            <div class="header-title-wrapper">
                                <div class="header-icon-wrapper">
                                    <b-icon icon="award-fill" class="header-icon"></b-icon>
                                </div>
                                <div>
                                    <h1 class="users-page-title">{{ $t('all_brands') }}</h1>
                                    <p class="header-subtitle">{{ $t('brandsPageDescription') || 'إدارة براندات المنتجات' }}</p>
                                </div>
                            </div>
                            <div class="app-header-actions">
                                <button type="button" class="btn-refresh" @click="refreshPage" :disabled="show">
                                    <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                                    <span class="button-text">{{ $t('refresh') || 'تحديث' }}</span>
                                </button>
                                <button type="button" class="users-add-button" v-b-modal.modal-addBrands>
                                    <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                                    <span class="button-text">{{ $t('add_brand') }}</span>
                                </button>
                            </div>
                        </div>
                    </div>

                    <div class="app-overview-grid">
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                                <b-icon icon="award-fill"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ totalBrands }}</div>
                                <div class="app-overview-stat-label">{{ $t('brandsOverviewTotal') || 'إجمالي البراندات' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                                <b-icon icon="list-ul"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ Brands.length }}</div>
                                <div class="app-overview-stat-label">{{ $t('brandsOverviewOnPage') || 'في الصفحة الحالية' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--info">
                                <b-icon icon="layers-fill"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ totalPages }}</div>
                                <div class="app-overview-stat-label">{{ $t('brandsOverviewPages') || 'عدد الصفحات' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--success">
                                <b-icon icon="search"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ searchActive ? Brands.length : '—' }}</div>
                                <div class="app-overview-stat-label">{{ $t('brandsOverviewSearch') || 'نتائج البحث' }}</div>
                            </div>
                        </div>
                    </div>

                    <div class="app-section-card">
                        <div class="app-section-header app-section-header--toolbar">
                            <div class="app-section-title-wrap">
                                <div class="app-section-icon-wrap">
                                    <b-icon icon="award-fill"></b-icon>
                                </div>
                                <div>
                                    <h3 class="app-section-title">{{ $t('all_brands') }}</h3>
                                    <p class="app-section-subtitle">{{ $t('brandsListHint') || 'قائمة البراندات مع التعديل والحذف' }}</p>
                                </div>
                            </div>
                        </div>
                        <div class="app-filters-panel app-filters-panel--inset">
                            <div class="app-filters-panel-head">
                                <div class="app-filters-panel-title">
                                    <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                                    <div>
                                        <h3>{{ $t('filters') || 'الفلاتر' }}</h3>
                                        <p>{{ $t('brandsFiltersHint') || 'بحث في الأقسام بالاسم' }}</p>
                                    </div>
                                </div>
                                <div class="app-filters-panel-actions" v-if="search.info">
                                    <button
                                        type="button"
                                        class="users-filter-clear-btn app-filters-clear-btn"
                                        @click="search.info = ''"
                                    >
                                        <b-icon icon="x-circle" class="me-1"></b-icon>
                                        {{ $t('clearFilters') || 'مسح الفلاتر' }}
                                    </button>
                                </div>
                            </div>
                            <div class="app-filters-fields app-filters-fields--2">
                                <label class="app-filter-field app-filter-field--grow">
                                    <span class="app-filter-label">{{ $t('search') || 'بحث' }}</span>
                                    <div class="users-search-container">
                                        <b-icon icon="search" class="search-icon"></b-icon>
                                        <input
                                            v-model="search.info"
                                            type="search"
                                            :placeholder="$t('search')"
                                            class="users-search-input"
                                            autocomplete="off"
                                        />
                                    </div>
                                </label>
                            </div>
                        </div>
                        <div class="app-section-body app-section-body--no-padding">
                    <div class="brands-table-container report-table-container">
                        <b-table
                            :items="Brands"
                            :fields="brandFields"
                            striped
                            hover
                            responsive
                            class="brands-table reports-table"
                        >
                            <template #cell(name)="row">
                                <div class="brand-name-cell">
                                    <b-icon icon="award-fill" class="brand-icon"></b-icon>
                                    <span class="brand-name-text">{{ row.item.name }}</span>
                                </div>
                            </template>

                            <template #cell(actions)="row">
                                <div class="actions-cell" role="group" :aria-label="$t('actions') || 'العمليات'">
                                    <button
                                        type="button"
                                        class="action-btn action-btn--icon action-btn--edit"
                                        @click="getBrandInfo(row.item)"
                                        :title="$t('edit')"
                                        :aria-label="$t('edit')"
                                    >
                                        <b-icon icon="pencil-square" class="action-icon"></b-icon>
                                    </button>
                                    <button
                                        type="button"
                                        class="action-btn action-btn--icon action-btn--delete"
                                        @click="deleteBrandModel(row.item.id)"
                                        :title="$t('delete')"
                                        :aria-label="$t('delete')"
                                    >
                                        <b-icon icon="trash" class="action-icon"></b-icon>
                                    </button>
                                </div>
                            </template>
                        </b-table>

                        <!-- Pagination -->
                        <div class="pagination-container" v-if="totalPages > 1">
                            <b-pagination
                                v-model="pageNumber"
                                :total-rows="totalBrands"
                                :per-page="pageSize"
                                :limit="7"
                                first-number
                                last-number
                                @change="onPageChange"
                                class="categories-pagination"
                            ></b-pagination>
                            <div class="pagination-info">
                                <span>{{ $t('showing') || 'عرض' }} {{ ((pageNumber - 1) * pageSize) + 1 }} - {{ Math.min(pageNumber * pageSize, totalBrands) }} {{ $t('of') || 'من' }} {{ totalBrands }}</span>
                            </div>
                        </div>
                    </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Add Category Modal -->
            <b-modal id="modal-addBrands" :title="$t('add_new_brand')" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <h2 class="modal-title">{{ $t('add_new_brand') }}</h2>
                    <form @submit.prevent="addBrand" class="users-form">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="award-fill" class="form-label-icon"></b-icon>
                                {{ $t('brand_name') }}
                            </label>
                            <input 
                                id="inputName" 
                                v-model="addForm.name" 
                                type="text"
                                :placeholder="$t('brand_name')" 
                                required 
                                class="users-form-input"
                            />
                        </div>
                        <div class="users-form-actions">
                            <button type="submit" class="users-form-submit-button" :disabled="show == true">
                                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                                {{ $t('add') }}
                            </button>
                            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addBrands')">
                                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                                {{ $t('close') }}
                            </button>
                        </div>
                    </form>
                </div>
            </b-modal>

            <!-- Edit Category Modal -->
            <b-modal id="modal-editBrands" :title="$t('edit_brand')" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <h2 class="modal-title">{{ $t('edit_brand') }}</h2>
                    <form @submit.prevent="EditBrand" class="users-form">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="award-fill" class="form-label-icon"></b-icon>
                                {{ $t('brand_name') }}
                            </label>
                            <input 
                                id="editInputName" 
                                v-model="editForm.name" 
                                type="text" 
                                :placeholder="$t('brand_name')"
                                required 
                                class="users-form-input"
                            />
                        </div>
                        <div class="users-form-actions">
                            <button type="submit" class="users-form-submit-button" :disabled="show == true">
                                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                                {{ $t('edit') }}
                            </button>
                            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editBrands')">
                                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                                {{ $t('close') }}
                            </button>
                        </div>
                    </form>
                </div>
            </b-modal>

            <!-- Delete Confirmation Modal -->
            <b-modal id="modal-delete" :title="$t('confirm_delete')" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <div class="delete-confirmation-content">
                        <div class="delete-icon-wrapper">
                            <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
                        </div>
                        <h3 class="delete-confirmation-title">{{ $t('confirm_delete') }}</h3>
                        <p class="delete-confirmation-text">{{ $t('areYouSureDeleteBrand') || 'هل أنت متأكد من حذف هذا البراند؟' }}</p>
                        <div class="delete-confirmation-actions">
                            <button class="delete-confirm-button" @click="deleteBrand('modal-delete')">
                                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                                {{ $t('delete') }}
                            </button>
                            <button class="delete-cancel-button" @click="closeModel('modal-delete')">
                                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                                {{ $t('cancel') }}
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
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";
import { HTTP } from '../http/api.js';
export default {
    name: "BrandsView",
    components: {
        AppHeader,
        ClockVue,
        "vue-barcode": VueBarcode,

    },
    data() {
        return {
            show: false,
            search: "",
            Brands: [],
            pageNumber: 1,
            totalBrands: 0,
            pageSize: 10,
            search: {
                info: "",
            },
            SearchBrands: [],
            totalCardBrands: 0,
            BrandInfo: {},
            editForm: {
                name: "",
                id: "",
            },
            addForm: {
                name: "",
            },
            BrandId: '',
        };
    },

    watch: {

        search: {
            handler() {
                this.GetAllBrands();
            },
            deep: true,
        },

        pageNumber() {
            this.GetAllBrands();
        },
    },

    mounted() {
        this.GetAllBrands();
    },

    computed: {
        role() {
            return localStorage.getItem("role");
        },
        brandFields() {
            return [
                {
                    key: 'name',
                    label: this.$t('brand_name') || 'اسم البراند',
                    sortable: true,
                    thClass: 'brand-header-cell'
                },
                {
                    key: 'actions',
                    label: this.$t('actions') || 'الإجراءات',
                    sortable: false,
                    thClass: 'brand-header-cell'
                }
            ];
        },
        totalPages() {
            return Math.ceil(this.totalBrands / this.pageSize);
        },
        searchActive() {
            return !!(this.search.info || '').trim();
        },
    },

    methods: {
        refreshPage() {
            this.GetAllBrands();
        },
        deleteBrandModel(id) {
            this.BrandId = id;
            this.$bvModal.show("modal-delete");
        },
        getBrandInfo(brand) {
            this.editForm = {
                id: brand.id,
                name: brand.name || "",
            };
            this.$bvModal.show("modal-editBrands");
        },
        brandApiError(error) {
            const apiMsg = error?.response?.data?.message;
            if (apiMsg && this.$te(apiMsg)) return this.$t(apiMsg);
            return this.$t("somethingWrong");
        },
        addBrand() {

            this.show = true;
            HTTP.post(`Admin/AddBrand`, { name: (this.addForm.name || "").trim() })
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('BrandHasbeenAddedSuccessfully'), {
                        position: "top-right",
                        timeout: 4000,
                        closeOnClick: true,
                        pauseOnFocusLoss: true,
                        pauseOnHover: true,
                        draggable: true,
                        draggablePercent: 0.6,
                        showCloseButtonOnHover: false,
                        hideProgressBar: true,
                        closeButton: "button",
                        icon: true,
                        
                    });
                    this.addForm.name = '';
                    this.GetAllBrands();
                    this.$bvModal.hide('modal-addBrands');
                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(this.brandApiError(error), {
                        position: "top-right",
                        timeout: 4000,
                        closeOnClick: true,
                        pauseOnFocusLoss: true,
                        pauseOnHover: true,
                        draggable: true,
                        draggablePercent: 0.6,
                        showCloseButtonOnHover: false,
                        hideProgressBar: true,
                        closeButton: "button",
                        icon: true,
                        
                    });
                });
        },
        EditBrand() {
            this.show = true;
            HTTP.put(`Admin/UpdateBrand?id=${this.editForm.id}`, { name: (this.editForm.name || "").trim() })
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('BrandHadbeenEditSuccessfully'), {
                        position: "top-right",
                        timeout: 4000,
                        closeOnClick: true,
                        pauseOnFocusLoss: true,
                        pauseOnHover: true,
                        draggable: true,
                        draggablePercent: 0.6,
                        showCloseButtonOnHover: false,
                        hideProgressBar: true,
                        closeButton: "button",
                        icon: true,
                        
                    });
                    this.GetAllBrands();
                    this.$bvModal.hide('modal-editBrands');
                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(this.brandApiError(error), {
                        position: "top-right",
                        timeout: 4000,
                        closeOnClick: true,
                        pauseOnFocusLoss: true,
                        pauseOnHover: true,
                        draggable: true,
                        draggablePercent: 0.6,
                        showCloseButtonOnHover: false,
                        hideProgressBar: true,
                        closeButton: "button",
                        icon: true,
                        
                    });
                });
        },

        deleteBrand(modelId) {
            this.show = true;
            HTTP.delete(`Admin/DeleteBrand?id=${this.BrandId}`)
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('BrandHadbeenDeleteSuccessfully'), {
                        position: "top-right",
                        timeout: 4000,
                        closeOnClick: true,
                        pauseOnFocusLoss: true,
                        pauseOnHover: true,
                        draggable: true,
                        draggablePercent: 0.6,
                        showCloseButtonOnHover: false,
                        hideProgressBar: true,
                        closeButton: "button",
                        icon: true,
                        
                    });
                    this.GetAllBrands();
                    this.$bvModal.hide(modelId);

                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(this.brandApiError(error), {
                        position: "top-right",
                        timeout: 4000,
                        closeOnClick: true,
                        pauseOnFocusLoss: true,
                        pauseOnHover: true,
                        draggable: true,
                        draggablePercent: 0.6,
                        showCloseButtonOnHover: false,
                        hideProgressBar: true,
                        closeButton: "button",
                        icon: true,
                        
                    });
                });
        },


        closeModel(id) {
            this.$bvModal.hide(id);
        },


        GetAllBrands() {
            this.show = true;
            HTTP.get(`Admin/GetBrands?pageNumber=${this.pageNumber - 1}&pageSize=${this.pageSize}&info=${this.search.info}`)
                .then((response) => {
                    this.Brands = response.data.data.items;
                    this.totalBrands = response.data.data.totalItems;
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                });
        },
        onPageChange(page) {
            this.pageNumber = page;
            this.GetAllBrands();
        },

    },


};
</script>

<style scoped>
.brands-table-container {
  margin-top: 1.5rem;
}

.brands-table {
  margin: 0;
}

.brands-table >>> thead th .sr-only,
.brands-table >>> thead th .visually-hidden {
  display: none !important;
}

.brand-name-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.brand-icon {
  color: var(--primary-color);
  font-size: 1.25rem;
}

.brand-name-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: #111827;
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

.categories-pagination >>> .page-link {
  color: var(--text-primary);
  border-color: var(--border-color);
  background-color: var(--bg-tertiary);
}

.categories-pagination >>> .page-item.active .page-link {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
}

.categories-pagination >>> .page-link:hover {
  background-color: color-mix(in srgb, var(--primary-color) 10%, transparent);
  border-color: var(--border-dark);
  color: var(--primary-color);
}
</style>