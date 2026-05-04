<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
            <div class="users-page-container">
                <div class="users-page-content">
                    <!-- Header Section -->
                    <div class="users-header-section">
                        <div class="users-header-content">
                            <h1 class="users-page-title">{{ $t('all_categories') }}</h1>
                            <div class="header-buttons-group">
                                <button class="users-add-button ai-generate-button" v-b-modal.modal-ai-generate>
                                    <b-icon icon="cpu-fill" class="button-icon"></b-icon>
                                    <span class="button-text">{{ $t('aiGenerateCategories') }}</span>
                                </button>
                            <button class="users-add-button" v-b-modal.modal-addTags>
                                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                                <span class="button-text">{{ $t('add_category') }}</span>
                            </button>
                            </div>
                        </div>
                    </div>

                    <!-- Search Section -->
                    <div class="users-search-section">
                        <div class="users-search-container">
                            <b-icon icon="search" class="search-icon"></b-icon>
                            <input 
                                v-model="search.info" 
                                type="search" 
                                :placeholder="$t('search')"
                                class="users-search-input"
                            />
                        </div>
                    </div>

                    <!-- Categories Table -->
                    <div class="categories-table-container">
                        <b-table
                            :items="Tagss"
                            :fields="categoryFields"
                            striped
                            hover
                            responsive
                            class="categories-table"
                        >
                            <template #cell(name)="row">
                                <div class="category-name-cell">
                                    <b-icon icon="tags-fill" class="category-icon"></b-icon>
                                    <span class="category-name-text">{{ formatCategoryDisplay(row.item) }}</span>
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

            <!-- Add Category Modal -->
            <b-modal id="modal-addTags" :title="$t('add_new_category')" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <h2 class="modal-title">{{ $t('add_new_category') }}</h2>
                    <form @submit.prevent="addTags" class="users-form">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="diagram-3-fill" class="form-label-icon"></b-icon>
                                {{ $t('parentCategory') }}
                            </label>
                            <select
                                class="users-form-select"
                                :value="addForm.parentTagId == null ? '' : String(addForm.parentTagId)"
                                @change="onAddParentTagChange"
                            >
                                <option value="">{{ $t('categoryMainLevel') }}</option>
                                <option v-for="t in rootTagsForParentSelect" :key="t.id" :value="String(t.id)">{{ t.name }}</option>
                            </select>
                            <p class="users-form-hint text-muted small mb-0 mt-1">{{ $t('subCategoryHint') }}</p>
                        </div>
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
                                <b-icon icon="diagram-3-fill" class="form-label-icon"></b-icon>
                                {{ $t('parentCategory') }}
                            </label>
                            <select
                                class="users-form-select"
                                :value="editForm.parentTagId == null ? '' : String(editForm.parentTagId)"
                                @change="onEditParentTagChange"
                            >
                                <option value="">{{ $t('categoryMainLevel') }}</option>
                                <option v-for="t in rootTagsForEditSelect" :key="t.id" :value="String(t.id)">{{ t.name }}</option>
                            </select>
                        </div>
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

            <!-- AI Generate Categories Modal -->
            <b-modal id="modal-ai-generate" :title="$t('aiGenerateCategories')" hide-header hide-footer class="users-modal">
                <div class="modal-content-wrapper">
                    <h2 class="modal-title">{{ $t('aiGenerateCategories') }}</h2>
                    <form @submit.prevent="generateCategoriesWithAI" class="users-form">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="diagram-3-fill" class="form-label-icon"></b-icon>
                                {{ $t('aiParentCategoryLabel') }}
                            </label>
                            <select
                                class="users-form-select"
                                :value="aiParentTagId == null ? '' : String(aiParentTagId)"
                                @change="onAiParentTagChange"
                            >
                                <option value="">{{ $t('aiRootCategoriesMode') }}</option>
                                <option v-for="t in rootTagsForParentSelect" :key="'ai-root-' + t.id" :value="String(t.id)">{{ t.name }}</option>
                            </select>
                            <p class="users-form-hint text-muted small mb-0 mt-1">{{ $t('aiParentCategoryHint') }}</p>
                        </div>
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="file-text" class="form-label-icon"></b-icon>
                                {{ $t('enterDescription') }}
                            </label>
                            <textarea 
                                v-model="aiDescription" 
                                :placeholder="$t('enterDescription')"
                                rows="6"
                                required 
                                class="users-form-textarea"
                            ></textarea>
                        </div>
                        <div class="users-form-actions">
                            <button type="submit" class="users-form-submit-button" :disabled="generatingCategories">
                                <b-spinner small v-if="generatingCategories" class="me-2"></b-spinner>
                                <b-icon icon="magic" class="me-2" v-if="!generatingCategories"></b-icon>
                                {{ generatingCategories ? $t('generatingCategories') : $t('generateCategories') }}
                            </button>
                            <button type="button" class="users-form-cancel-button" @click="closeModel('modal-ai-generate')">
                                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                                {{ $t('close') }}
                            </button>
                        </div>
                    </form>
                </div>
            </b-modal>

            <!-- Generated Categories Modal -->
            <b-modal id="modal-ai-categories" :title="$t('generatedCategories')" hide-header hide-footer class="users-modal" size="lg">
                <div class="modal-content-wrapper">
                    <h2 class="modal-title">{{ $t('generatedCategories') }}</h2>
                    <div class="generated-categories-container">
                        <div class="categories-actions-header">
                            <button type="button" class="select-all-button" @click="selectAllCategories">
                                <b-icon icon="check-square" class="me-2"></b-icon>
                                {{ $t('selectAll') }}
                            </button>
                            <button type="button" class="deselect-all-button" @click="deselectAllCategories">
                                <b-icon icon="square" class="me-2"></b-icon>
                                {{ $t('deselectAll') }}
                            </button>
                            <button type="button" class="add-more-ai-button" @click="addMoreCategoriesWithAI" :disabled="generatingMoreCategories">
                                <b-spinner small v-if="generatingMoreCategories" class="me-2"></b-spinner>
                                <b-icon icon="arrow-repeat" class="me-2" v-if="!generatingMoreCategories"></b-icon>
                                {{ generatingMoreCategories ? $t('generatingCategories') : $t('addMoreWithAI') }}
                            </button>
                            <button type="button" class="add-category-button" @click="addManualCategory">
                                <b-icon icon="plus-circle" class="me-2"></b-icon>
                                {{ $t('addCategory') }}
                            </button>
                        </div>
                        <div class="categories-list">
                            <div 
                                v-for="(category, index) in generatedCategories" 
                                :key="index"
                                class="category-item"
                            >
                                <input 
                                    type="checkbox" 
                                    v-model="category.selected"
                                    class="category-checkbox"
                                />
                                <input 
                                    type="text" 
                                    v-model="category.name"
                                    :placeholder="$t('editCategoryName')"
                                    class="category-name-input"
                                />
                                <button 
                                    type="button" 
                                    class="remove-category-btn" 
                                    @click="removeCategory(index)"
                                    :title="$t('delete')"
                                >
                                    <b-icon icon="trash-fill"></b-icon>
                                </button>
                            </div>
                        </div>
                        <div v-if="generatedCategories.length === 0" class="no-categories-message">
                            {{ $t('noCategoriesGenerated') }}
                        </div>
                    </div>
                    <div class="users-form-actions">
                        <button 
                            type="button" 
                            class="users-form-submit-button" 
                            @click="saveGeneratedCategories"
                            :disabled="savingCategories || selectedCategoriesCount === 0"
                        >
                            <b-spinner small v-if="savingCategories" class="me-2"></b-spinner>
                            <b-icon icon="check-circle-fill" class="me-2" v-if="!savingCategories"></b-icon>
                            {{ savingCategories ? $t('savingCategories') : $t('saveSelectedCategories') }}
                        </button>
                        <button type="button" class="users-form-cancel-button" @click="closeModel('modal-ai-categories')">
                            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                            {{ $t('close') }}
                        </button>
                    </div>
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
import { tagDisplayName as hierarchyTagLabel } from '@/utils/tagHierarchy.js';

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
                parentTagId: null,
            },
            addForm: {
                name: "",
                isForAll: false,
                parentTagId: null,
            },
            allTagsFlat: [],
            TagsId: '',
            // AI Generate Categories
            aiParentTagId: null,
            aiDescription: '',
            savedAiDescription: '', // حفظ الوصف الأصلي
            savedAiParentTagId: null,
            generatedCategories: [],
            generatingCategories: false,
            generatingMoreCategories: false,
            savingCategories: false,
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
        this.fetchAllTagsFlat();
        this.GetAllTagss();
    },

    computed: {
        rootTagsForParentSelect() {
            return this.allTagsFlat.filter((t) => t.parentTagId == null);
        },
        rootTagsForEditSelect() {
            const id = this.editForm.id;
            return this.allTagsFlat.filter(
                (t) => t.parentTagId == null && String(t.id) !== String(id)
            );
        },
        role() {
            return localStorage.getItem("role");
        },
        selectedCategoriesCount() {
            return this.generatedCategories.filter(cat => cat.selected && cat.name && cat.name.trim() !== '').length;
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
        }
    },

    methods: {
        formatCategoryDisplay(tag) {
            return hierarchyTagLabel(tag, this.allTagsFlat);
        },
        fetchAllTagsFlat() {
            HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`)
                .then((response) => {
                    this.allTagsFlat = response.data.data.items || [];
                })
                .catch(() => {
                    this.allTagsFlat = [];
                });
        },
        onAddParentTagChange(e) {
            const v = e.target.value;
            this.addForm.parentTagId = v === "" ? null : Number(v);
        },
        onEditParentTagChange(e) {
            const v = e.target.value;
            this.editForm.parentTagId = v === "" ? null : Number(v);
        },
        onAiParentTagChange(e) {
            const v = e.target.value;
            this.aiParentTagId = v === "" ? null : Number(v);
        },
        deleteTagsModel(id) {
            this.TagsId = id;
            this.$bvModal.show("modal-delete");
        },
        getTagsInfo(Tags) {
            this.editForm = { ...Tags };
            if (this.editForm.parentTagId === undefined) {
                this.editForm.parentTagId = null;
            }
            this.$bvModal.show("modal-editTags");
        },
        addTags() {

            this.show = true;
            HTTP.post(`Admin/AddTag`, {
                name: this.addForm.name,
                isForAll: this.addForm.isForAll,
                parentTagId: this.addForm.parentTagId,
            })
                .then((response) => {
                    this.show = false;
                    this.$toast.success(this.$i18n.t('TagsHasbeenAddedSuccessfully'), {
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
                    this.addForm.parentTagId = null;
                    this.addForm.password = '';
                    this.addForm.phoneNumber = 0;
                    this.addForm.Tagsname = 0;
                    this.addForm.role = '';
                    this.fetchAllTagsFlat();
                    this.GetAllTagss();
                    this.$bvModal.hide('modal-addTags');
                })
                .catch((error) => {
                    this.show = false;
                    this.$toast.error(this.$i18n.t('somethingWrong'), {
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
            HTTP.put(`Admin/UpdateTag?id=${this.editForm.id}`, {
                name: this.editForm.name,
                isForAll: this.editForm.isForAll || false,
                parentTagId: this.editForm.parentTagId,
            })
                .then((response) => {
                    this.show = false;
                    this.$toast.success(this.$i18n.t('TagsHadbeenEditSuccessfully'), {
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
                    this.fetchAllTagsFlat();
                    this.GetAllTagss();
                    this.$bvModal.hide('modal-editTags');
                })
                .catch((error) => {
                    this.show = false;
                    this.$toast.error(this.$i18n.t('somethingWrong'), {
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
                    this.$toast.success(this.$i18n.t('TagsHadbeenDeleteSuccessfully'), {
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
                    this.fetchAllTagsFlat();
                    this.GetAllTagss();
                    this.$bvModal.hide(modelId);

                })
                .catch((error) => {
                    this.show = false;
                    this.$toast.error(this.$i18n.t('somethingWrong'), {
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

        // AI Generate Categories Methods
        async generateCategoriesWithAI() {
            if (!this.aiDescription || this.aiDescription.trim() === '') {
                this.$toast.error(this.$i18n.t('enterDescription'), {
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
                return;
            }

            this.generatingCategories = true;
            try {
                const payload = {
                    description: this.aiDescription,
                    maxCategories: 15
                };
                if (this.aiParentTagId != null && !Number.isNaN(this.aiParentTagId)) {
                    payload.parentTagId = this.aiParentTagId;
                }
                const response = await HTTP.post('Admin/GenerateCategoriesWithAI', payload);

                if (response.data.errorStatus) {
                    this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
                } else {
                    this.generatedCategories = response.data.data.map(name => ({
                        name: name,
                        selected: true
                    }));
                    // حفظ الوصف الأصلي للاستخدام لاحقاً
                    this.savedAiDescription = this.aiDescription;
                    this.savedAiParentTagId = this.aiParentTagId;
                    this.$bvModal.hide('modal-ai-generate');
                    this.$bvModal.show('modal-ai-categories');
                    this.aiDescription = '';
                }
            } catch (error) {
                this.$toast.error(this.$i18n.t('somethingWrong'), {
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
            } finally {
                this.generatingCategories = false;
            }
        },

        async saveGeneratedCategories() {
            const parentId = this.savedAiParentTagId != null && !Number.isNaN(this.savedAiParentTagId)
                ? this.savedAiParentTagId
                : null;
            const selectedCategories = this.generatedCategories
                .filter(cat => cat.selected && cat.name && cat.name.trim() !== '')
                .map(cat => {
                    const row = {
                        name: cat.name.trim(),
                        isForAll: false
                    };
                    if (parentId != null) {
                        row.parentTagId = parentId;
                    }
                    return row;
                });

            if (selectedCategories.length === 0) {
                this.$toast.error(this.$i18n.t('noCategoriesSelected') || 'لم يتم تحديد أي أقسام', {
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
                return;
            }

            this.savingCategories = true;
            try {
                const response = await HTTP.post('Admin/AddMultipleTags', selectedCategories);

                if (response.data.errorStatus) {
                    this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
                } else {
                    this.$toast.success(response.data.message || this.$i18n.t('categoriesSavedSuccessfully') || 'تم حفظ الأقسام بنجاح', {
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
                    this.generatedCategories = [];
                    this.savedAiDescription = '';
                    this.savedAiParentTagId = null;
                    this.aiParentTagId = null;
                    this.$bvModal.hide('modal-ai-categories');
                    this.fetchAllTagsFlat();
                    this.GetAllTagss();
                }
            } catch (error) {
                this.$toast.error(this.$i18n.t('somethingWrong'), {
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
            } finally {
                this.savingCategories = false;
            }
        },

        addManualCategory() {
            this.generatedCategories.push({
                name: '',
                selected: true
            });
        },

        removeCategory(index) {
            this.generatedCategories.splice(index, 1);
        },

        selectAllCategories() {
            this.generatedCategories.forEach(cat => {
                cat.selected = true;
            });
        },

        deselectAllCategories() {
            this.generatedCategories.forEach(cat => {
                cat.selected = false;
            });
        },

        async addMoreCategoriesWithAI() {
            if (!this.savedAiDescription || this.savedAiDescription.trim() === '') {
                this.$toast.error(this.$i18n.t('noOriginalDescription') || 'الوصف الأصلي غير موجود', {
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
                return;
            }

            this.generatingMoreCategories = true;
            try {
                // إرسال الأقسام الحالية كـ context لتجنب التكرار
                const existingCategories = this.generatedCategories
                    .map(cat => cat.name.trim())
                    .filter(name => name !== '');

                const morePayload = {
                    description: this.savedAiDescription,
                    maxCategories: 15,
                    existingCategories: existingCategories
                };
                if (this.savedAiParentTagId != null && !Number.isNaN(this.savedAiParentTagId)) {
                    morePayload.parentTagId = this.savedAiParentTagId;
                }
                const response = await HTTP.post('Admin/GenerateCategoriesWithAI', morePayload);

                if (response.data.errorStatus) {
                    this.$toast.error(response.data.message || this.$i18n.t('somethingWrong'), {
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
                } else {
                    const newCategories = response.data.data || [];
                    const existingNames = this.generatedCategories.map(cat => cat.name.toLowerCase().trim());
                    
                    // Filter out duplicates (double check on frontend as well)
                    const uniqueNewCategories = newCategories
                        .filter(name => {
                            const normalizedName = name.toLowerCase().trim();
                            return !existingNames.includes(normalizedName);
                        })
                        .map(name => ({
                            name: name,
                            selected: true
                        }));

                    if (uniqueNewCategories.length === 0) {
                        this.$toast.info(this.$i18n.t('noNewCategoriesFound') || 'لم يتم العثور على أقسام جديدة', {
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
                    } else {
                        this.generatedCategories.push(...uniqueNewCategories);
                        this.$toast.success(`${uniqueNewCategories.length} ${this.$i18n.t('newCategoriesAdded') || 'قسم جديد تم إضافته'}`, {
                            position: "top-right",
                            timeout: 3000,
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
                    }
                }
            } catch (error) {
                this.$toast.error(this.$i18n.t('somethingWrong'), {
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
            } finally {
                this.generatingMoreCategories = false;
            }
        },

    },
};
</script>

<style scoped>
.categories-table-container {
  background: #ffffff;
  border-radius: 0.75rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  margin-top: 1.5rem;
}

.categories-table {
  margin: 0;
}

.categories-table >>> thead th {
  background-color: #f9fafb;
  color: #374151;
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 1rem;
  border-bottom: 2px solid #e5e7eb;
}

.categories-table >>> tbody td {
  padding: 1rem;
  vertical-align: middle;
  border-bottom: 1px solid #f3f4f6;
}

.categories-table >>> tbody tr:hover {
  background-color: #f9fafb;
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

/* AI Generate Categories Styles */
.header-buttons-group {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.ai-generate-button {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
}

.ai-generate-button:hover {
  background: linear-gradient(135deg, #5568d3 0%, #653a8f 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.users-form-textarea {
  width: 100%;
  padding: 0.75rem;
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  font-size: 0.9375rem;
  font-family: inherit;
  background-color: var(--bg-secondary);
  color: var(--text-primary);
  resize: vertical;
  transition: border-color 0.2s ease;
}

.users-form-textarea:focus {
  outline: none;
  border-color: var(--primary-color);
}

.users-form-textarea::placeholder {
  color: var(--text-muted);
}

.generated-categories-container {
  margin: 1.5rem 0;
}

.categories-actions-header {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.select-all-button,
.deselect-all-button {
  padding: 0.5rem 1rem;
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  background-color: var(--bg-secondary);
  color: var(--text-primary);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
}

.select-all-button:hover,
.deselect-all-button:hover {
  background-color: var(--bg-primary);
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.add-more-ai-button,
.add-category-button {
  padding: 0.5rem 1rem;
  border: 2px solid var(--primary-color);
  border-radius: 0.5rem;
  background-color: var(--primary-color);
  color: white;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
}

.add-more-ai-button:hover:not(:disabled),
.add-category-button:hover {
  background-color: var(--primary-color-dark);
  border-color: var(--primary-color-dark);
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(99, 102, 241, 0.3);
}

.add-more-ai-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.categories-list {
  max-height: 400px;
  overflow-y: auto;
  border: 2px solid var(--border-color);
  border-radius: 0.5rem;
  padding: 1rem;
  background-color: var(--bg-secondary);
}

.category-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem;
  margin-bottom: 0.5rem;
  background-color: var(--bg-primary);
  border-radius: 0.5rem;
  border: 1px solid var(--border-color);
  transition: all 0.2s ease;
}

.category-item:hover {
  border-color: var(--primary-color);
  box-shadow: 0 2px 8px rgba(99, 102, 241, 0.1);
}

.category-checkbox {
  width: 20px;
  height: 20px;
  cursor: pointer;
  accent-color: var(--primary-color);
}

.category-name-input {
  flex: 1;
  padding: 0.5rem 0.75rem;
  border: 2px solid var(--border-color);
  border-radius: 0.375rem;
  font-size: 0.9375rem;
  background-color: var(--bg-secondary);
  color: var(--text-primary);
  transition: border-color 0.2s ease;
}

.category-name-input:focus {
  outline: none;
  border-color: var(--primary-color);
}

.remove-category-btn {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 0.375rem;
  background-color: #fee2e2;
  color: #991b1b;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
}

.remove-category-btn:hover {
  background-color: #991b1b;
  color: white;
  transform: scale(1.05);
}

.no-categories-message {
  text-align: center;
  padding: 2rem;
  color: var(--text-muted);
  font-size: 0.9375rem;
}
</style>