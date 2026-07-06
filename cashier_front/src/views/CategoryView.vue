<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
            <div class="app-page-container">
                <div class="app-page-content category-page">
                    <div class="users-header-section">
                        <div class="users-header-content app-header-row">
                            <div class="header-title-wrapper">
                                <div class="header-icon-wrapper">
                                    <b-icon icon="tags-fill" class="header-icon"></b-icon>
                                </div>
                                <div>
                                    <h1 class="users-page-title">{{ $t('all_categories') }}</h1>
                                    <p class="header-subtitle">{{ $t('categoriesPageDescription') || 'إدارة فئات المنتجات' }}</p>
                                </div>
                            </div>
                            <div class="app-header-actions">
                                <button type="button" class="btn-refresh" @click="refreshPage" :disabled="show">
                                    <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                                    <span class="button-text">{{ $t('refresh') || 'تحديث' }}</span>
                                </button>
                                <button type="button" class="users-add-button" v-b-modal.modal-addTags>
                                    <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                                    <span class="button-text">{{ $t('add_category') }}</span>
                                </button>
                            </div>
                        </div>
                    </div>

                    <div class="app-overview-grid">
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                                <b-icon icon="tags-fill"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ totalTagss }}</div>
                                <div class="app-overview-stat-label">{{ $t('categoriesOverviewTotal') || 'إجمالي الفئات' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                                <b-icon icon="list-ul"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ Tagss.length }}</div>
                                <div class="app-overview-stat-label">{{ $t('categoriesOverviewOnPage') || 'في الصفحة الحالية' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--info">
                                <b-icon icon="layers-fill"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ totalPages }}</div>
                                <div class="app-overview-stat-label">{{ $t('categoriesOverviewPages') || 'عدد الصفحات' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--success">
                                <b-icon icon="search"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ searchActive ? Tagss.length : '—' }}</div>
                                <div class="app-overview-stat-label">{{ $t('categoriesOverviewSearch') || 'نتائج البحث' }}</div>
                            </div>
                        </div>
                    </div>

                    <div class="app-section-card">
                        <div class="app-section-header app-section-header--toolbar">
                            <div class="app-section-title-wrap">
                                <div class="app-section-icon-wrap">
                                    <b-icon icon="tags-fill"></b-icon>
                                </div>
                                <div>
                                    <h3 class="app-section-title">{{ $t('all_categories') }}</h3>
                                    <p class="app-section-subtitle">{{ $t('categoriesListHint') || 'قائمة الفئات مع التعديل والحذف' }}</p>
                                </div>
                            </div>
                            <div class="app-search-wrap app-search-wrap--wide">
                                <b-icon icon="search" class="app-search-icon"></b-icon>
                                <input
                                    v-model="search.info"
                                    type="search"
                                    :placeholder="$t('search')"
                                    class="app-search-input"
                                    autocomplete="off"
                                />
                            </div>
                        </div>
                        <div class="app-section-body app-section-body--no-padding">
                    <div class="categories-table-container report-table-container">
                        <b-table
                            :items="Tagss"
                            :fields="categoryFields"
                            striped
                            hover
                            responsive
                            class="categories-table reports-table"
                        >
                            <template #cell(name)="row">
                                <div class="category-name-cell">
                                    <b-icon icon="tags-fill" class="category-icon"></b-icon>
                                    <span class="category-name-text">{{ row.item.name }}</span>
                                </div>
                            </template>

                            <template #cell(actions)="row">
                                <div class="actions-cell">
                                    <button
                                        type="button"
                                        class="action-btn action-btn--icon action-btn--edit"
                                        @click="getTagsInfo(row.item)"
                                        :title="$t('edit')"
                                    >
                                        <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                                    </button>
                                    <button
                                        type="button"
                                        class="action-btn action-btn--icon action-btn--delete"
                                        @click="deleteTagsModel(row.item.id)"
                                        :title="$t('delete')"
                                    >
                                        <b-icon icon="trash-fill" class="action-icon"></b-icon>
                                    </button>
                                </div>
                            </template>
                        </b-table>

                        <!-- Pagination -->
                        <div class="pagination-container" v-if="totalPages > 1">
                            <b-pagination
                                v-model="pageNumber"
                                :total-rows="totalTagss"
                                :per-page="pageSize"
                                :limit="7"
                                first-number
                                last-number
                                @change="onPageChange"
                                class="categories-pagination"
                            ></b-pagination>
                            <div class="pagination-info">
                                <span>{{ $t('showing') || 'عرض' }} {{ ((pageNumber - 1) * pageSize) + 1 }} - {{ Math.min(pageNumber * pageSize, totalTagss) }} {{ $t('of') || 'من' }} {{ totalTagss }}</span>
                            </div>
                        </div>
                    </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Add Category Modal -->
            <b-modal id="modal-addTags" :title="$t('add_new_category')" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <h2 class="modal-title">{{ $t('add_new_category') }}</h2>
                    <form @submit.prevent="addTags" class="users-form">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
                                {{ $t('category_name') }}
                            </label>
                            <input 
                                id="inputName" 
                                v-model="addForm.name" 
                                type="text"
                                :placeholder="$t('category_name')" 
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
                            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addTags')">
                                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                                {{ $t('close') }}
                            </button>
                        </div>
                    </form>
                </div>
            </b-modal>

            <!-- Edit Category Modal -->
            <b-modal id="modal-editTags" :title="$t('edit_account')" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <h2 class="modal-title">{{ $t('edit_account') }}</h2>
                    <form @submit.prevent="EditTags" class="users-form">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="tags-fill" class="form-label-icon"></b-icon>
                                {{ $t('category_name') }}
                            </label>
                            <input 
                                id="editInputName" 
                                v-model="editForm.name" 
                                type="text" 
                                :placeholder="$t('category_name')"
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
                            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editTags')">
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
                        <p class="delete-confirmation-text">{{ $t('areYouSureDeleteUser') || 'هل أنت متأكد من حذف هذا التصنيف؟' }}</p>
                        <div class="delete-confirmation-actions">
                            <button class="delete-confirm-button" @click="deleteTags('modal-delete')">
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
    name: "TagssView",
    components: {
        AppHeader,
        ClockVue,
        "vue-barcode": VueBarcode,

    },
    data() {
        return {
            show: false,
            search: "",
            Tagss: [],
            pageNumber: 1,
            totalTagss: 0,
            pageSize: 10,
            search: {
                info: "",
            },
            SearchTagss: [],
            totalCardTagss: 0,
            TagsInfo: {},
            editForm: {
                name: "",
                phoneNumber: "",
                Tagsname: "",
                role: "",
                id: "",
            },
            addForm: {
                name: "",
                isForAll: false,
            },
            TagsId: '',
        };
    },

    watch: {

        search: {
            handler() {
                this.GetAllTagss();
            },
            deep: true,
        },

        pageNumber() {
            this.GetAllTagss();
        },
    },

    mounted() {
        this.GetAllTagss();
    },

    computed: {
        role() {
            return localStorage.getItem("role");
        },
        categoryFields() {
            return [
                {
                    key: 'name',
                    label: this.$t('category_name') || 'اسم التصنيف',
                    sortable: true,
                    thClass: 'category-header-cell'
                },
                {
                    key: 'actions',
                    label: this.$t('actions') || 'الإجراءات',
                    sortable: false,
                    thClass: 'category-header-cell'
                }
            ];
        },
        totalPages() {
            return Math.ceil(this.totalTagss / this.pageSize);
        },
        searchActive() {
            return !!(this.search.info || '').trim();
        },
    },

    methods: {
        refreshPage() {
            this.GetAllTagss();
        },
        deleteTagsModel(id) {
            this.TagsId = id;
            this.$bvModal.show("modal-delete");
        },
        getTagsInfo(Tags) {
            this.editForm = Tags;
            this.$bvModal.show("modal-editTags");
        },
        addTags() {

            this.show = true;
            HTTP.post(`Admin/AddTag`, this.addForm)
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('TagsHasbeenAddedSuccessfully'), {
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
                    this.addForm.password = '';
                    this.addForm.phoneNumber = 0;
                    this.addForm.Tagsname = 0;
                    this.addForm.role = '';
                    this.GetAllTagss();
                    this.$bvModal.hide('modal-addTags');
                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(this.$i18n.t('somethingWrong'), {
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
        EditTags() {
            this.show = true;
            HTTP.put(`Admin/UpdateTag?id=${this.editForm.id}`, this.editForm)
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('TagsHadbeenEditSuccessfully'), {
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
                    this.GetAllTagss();
                    this.$bvModal.hide('modal-editTags');
                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(this.$i18n.t('somethingWrong'), {
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

        deleteTags(modelId) {
            this.show = true;
            HTTP.delete(`Admin/DeleteTag?id=${this.TagsId}`)
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('TagsHadbeenDeleteSuccessfully'), {
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
                    this.GetAllTagss();
                    this.$bvModal.hide(modelId);

                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(this.$i18n.t('somethingWrong'), {
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


        GetAllTagss() {
            this.show = true;
            HTTP.get(`Admin/GetTags?pageNumber=${this.pageNumber - 1}&pageSize=${this.pageSize}&info=${this.search.info}`)
                .then((response) => {
                    this.Tagss = response.data.data.items;
                    this.totalTagss = response.data.data.totalItems;
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                });
        },
        onPageChange(page) {
            this.pageNumber = page;
            this.GetAllTagss();
        },

    },


};
</script>

<style scoped>
.categories-table-container {
  margin-top: 1.5rem;
}

.categories-table {
  margin: 0;
}

.categories-table >>> thead th .sr-only,
.categories-table >>> thead th .visually-hidden {
  display: none !important;
}

.category-name-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.category-icon {
  color: var(--primary-color);
  font-size: 1.25rem;
}

.category-name-text {
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
  background-color: rgba(99, 102, 241, 0.1);
  border-color: var(--border-dark);
  color: var(--primary-color);
}
</style>