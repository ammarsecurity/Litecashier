<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
        <div class="users-page-container">
            <div class="users-page-content">
                <!-- Header Section -->
                <div class="users-header-section">
                    <div class="users-header-content">
                        <h1 class="users-page-title">{{ $t('all_accounts') }}</h1>
                        <button class="users-add-button" v-b-modal.modal-addUser>
                            <b-icon icon="person-plus-fill" class="button-icon"></b-icon>
                            <span class="button-text">{{ $t('add_account') }}</span>
                        </button>
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

                <!-- Users Grid -->
                <div class="users-grid-container">
                    <div class="users-grid">
                        <div class="user-card" v-for="User in Users" v-bind:key="User.id">
                            <div class="user-card-header">
                                <div class="user-avatar">
                                    <b-icon icon="person-circle" class="avatar-icon"></b-icon>
                                </div>
                                <h3 class="user-name">{{ User.name }}</h3>
                            </div>
                            <div class="user-card-body">
                                <div class="user-info-item">
                                    <b-icon icon="person-badge" class="info-icon"></b-icon>
                                    <span class="info-label">{{ $t('role') }}:</span>
                                    <span class="info-value user-role-badge" :class="getRoleClass(User.role)">{{ User.role }}</span>
                                </div>
                                <div class="user-info-item">
                                    <b-icon icon="person" class="info-icon"></b-icon>
                                    <span class="info-label">{{ $t('username') }}:</span>
                                    <span class="info-value">{{ User.username }}</span>
                                </div>
                            </div>
                            <div class="user-card-footer">
                                <button 
                                    class="user-action-button user-edit-button" 
                                    @click="getUserInfo(User)"
                                    :disabled="role === 'Commercial' && User.role === 'Commercial'"
                                    :title="role === 'Commercial' && User.role === 'Commercial' ? ($t('noPermissionToEditCommercial') || 'ليس لديك صلاحية لتعديل المستخدمين التجاريين') : ''"
                                >
                                    <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                                    <span>{{ $t('edit') }}</span>
                                </button>
                                <button class="user-action-button user-delete-button" @click="deleteUserModel(User.id)">
                                    <b-icon icon="trash-fill" class="action-icon"></b-icon>
                                    <span>{{ $t('delete') }}</span>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Pagination -->
                <div class="users-pagination-section">
                    <b-pagination 
                        v-model="pageNumber" 
                        :total-rows="totalUsers" 
                        :per-page="pageSize"
                        aria-controls="users-table"
                        class="users-pagination"
                    ></b-pagination>
                </div>
            </div>
        </div>

        <!-- Add User Modal -->
        <b-modal id="modal-addUser" :title="$t('add_new_account')" hide-header hide-footer class="users-modal">
            <div class="modal-content-wrapper">
                <h2 class="modal-title">{{ $t('add_new_account') }}</h2>
                <form @submit.prevent="addUser" class="users-form">
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                            {{ $t('full_name') }}
                        </label>
                        <input 
                            id="inputName" 
                            v-model="addForm.name" 
                            type="text"
                            :placeholder="$t('full_name')" 
                            required 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                            {{ $t('phone_number') }}
                        </label>
                        <input 
                            id="inputPhoneNumber" 
                            v-model="addForm.phoneNumber" 
                            type="tel"
                            :placeholder="$t('phone_number')" 
                            required 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="lock-fill" class="form-label-icon"></b-icon>
                            {{ $t('password') }}
                        </label>
                        <input 
                            id="inputPassword" 
                            v-model="addForm.password" 
                            type="password"
                            :placeholder="$t('password')" 
                            required 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="person" class="form-label-icon"></b-icon>
                            {{ $t('username') }}
                        </label>
                        <input 
                            id="inputUsername" 
                            v-model="addForm.username" 
                            type="text"
                            :placeholder="$t('username')" 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="person-badge" class="form-label-icon"></b-icon>
                            {{ $t('role') }}
                        </label>
                        <select v-model="addForm.role" class="users-form-select">
                            <option v-if="role == 'Commercial'" value="POS">{{ $t('point_of_sale') }}</option>
                            <option v-if="role == 'Commercial'" value="Reader">{{ $t('price_reader') }}</option>
                            <option v-if="role == 'Commercial'" value="Manager">{{ $t('managerRole') || 'مدير إدارة' }}</option>
                            <option v-if="role == 'Admin'" value="Commercial">{{ $t('commercial') }}</option>
                        </select>
                    </div>

                    <div
                        v-if="role == 'Commercial' && addForm.role == 'Manager'"
                        class="users-form-group users-sections-picker"
                    >
                        <label class="users-form-label">
                            <b-icon icon="grid-3x3-gap-fill" class="form-label-icon"></b-icon>
                            {{ $t('sectionsPermissions') || 'صلاحيات الأقسام' }}
                        </label>
                        <p class="text-muted small mb-2">{{ $t('selectAtLeastOneSection') || 'اختر قسماً واحداً على الأقل' }}</p>
                        <div class="users-sections-grid">
                            <label
                                v-for="key in assignableSectionKeys"
                                :key="'add-sec-' + key"
                                class="users-section-check"
                            >
                                <input
                                    type="checkbox"
                                    :value="key"
                                    v-model="addForm.allowedSections"
                                />
                                <span>{{ sectionLabel(key) }}</span>
                            </label>
                        </div>
                        <div class="users-form-group mt-3">
                            <label class="users-section-check d-flex align-items-start gap-2">
                                <input
                                    type="checkbox"
                                    v-model="addForm.canUseOwnLoginCodeForSensitiveActions"
                                />
                                <span>{{ $t('managerCanUseOwnLoginCode') || 'يمكنه استخدام رمز الدخول الخاص به لتأكيد الإجراءات الحساسة' }}</span>
                            </label>
                        </div>
                        <div
                            v-if="addForm.canUseOwnLoginCodeForSensitiveActions"
                            class="users-form-group"
                        >
                            <label class="users-form-label">
                                <b-icon icon="shield-lock" class="form-label-icon"></b-icon>
                                {{ $t('managerSensitiveLoginCodeLabel') || 'رمز تأكيد الإجراءات' }}
                            </label>
                            <input
                                v-model="addForm.loginCode"
                                type="password"
                                inputmode="numeric"
                                maxlength="12"
                                autocomplete="off"
                                :placeholder="$t('managerSensitiveLoginCodePlaceholder') || '4–12 رقماً'"
                                class="users-form-input"
                            />
                        </div>
                    </div>
                    
                    <template v-if="role == 'Admin' && addForm.role == 'Commercial'">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="shop" class="form-label-icon"></b-icon>
                                {{ $t('storeName') || 'اسم المتجر' }}
                            </label>
                            <input
                                v-model="addForm.storeName"
                                type="text"
                                class="users-form-input"
                                :placeholder="$t('storeName') || 'اسم المتجر'"
                            />
                        </div>
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="key-fill" class="form-label-icon"></b-icon>
                                {{ $t('accountLoginCodeLabel') || 'رمز الحساب' }}
                            </label>
                            <input
                                v-model="addForm.loginCode"
                                type="text"
                                inputmode="numeric"
                                maxlength="12"
                                autocomplete="off"
                                :placeholder="$t('accountLoginCodeAdminPlaceholder') || 'اختياري: 4–12 رقماً'"
                                class="users-form-input"
                            />
                            <small class="text-muted d-block mt-1">{{ $t('accountLoginCodeAdminHint') || 'يسمح لتاجر الحساب بتسجيل الدخول بهذا الرمز فقط دون هاتف وكلمة مرور' }}</small>
                        </div>
                    </template>
                    
                    <div class="users-form-actions">
                        <button type="submit" class="users-form-submit-button" :disabled="show == true">
                            <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                            <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                            {{ $t('add') }}
                        </button>
                        <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addUser')">
                            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                            {{ $t('close') }}
                        </button>
                    </div>
                </form>
            </div>
        </b-modal>

        <!-- Edit User Modal -->
        <b-modal id="modal-editUser" :title="$t('edit_account')" hide-header hide-footer class="users-modal">
            <div class="modal-content-wrapper">
                <h2 class="modal-title">{{ $t('edit_account') }}</h2>
                <form @submit.prevent="EditUser" class="users-form">
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                            {{ $t('full_name') }}
                        </label>
                        <input 
                            id="editInputName" 
                            v-model="editForm.name" 
                            type="text"
                            :placeholder="$t('full_name')" 
                            required 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                            {{ $t('phone_number') }}
                        </label>
                        <input 
                            id="editInputPhoneNumber" 
                            v-model="editForm.phoneNumber" 
                            type="tel"
                            :placeholder="$t('phone_number')" 
                            required 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="lock-fill" class="form-label-icon"></b-icon>
                            {{ $t('password') }} ({{ $t('optional') || 'اختياري' }})
                        </label>
                        <input 
                            id="editInputPassword" 
                            v-model="editForm.password" 
                            type="password"
                            :placeholder="$t('password')" 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="person" class="form-label-icon"></b-icon>
                            {{ $t('username') }}
                        </label>
                        <input 
                            id="editInputUsername" 
                            v-model="editForm.username" 
                            type="text"
                            :placeholder="$t('username')" 
                            class="users-form-input"
                        />
                    </div>
                    <div class="users-form-group">
                        <label class="users-form-label">
                            <b-icon icon="person-badge" class="form-label-icon"></b-icon>
                            {{ $t('role') }}
                        </label>
                        <select v-model="editForm.role" class="users-form-select">
                            <!-- Admin can only edit Commercial users -->
                            <template v-if="role == 'Admin'">
                                <option value="Commercial">{{ $t('commercial') }}</option>
                            </template>
                            <!-- Commercial can edit their sub-users -->
                            <template v-else-if="role == 'Commercial'">
                                <option value="POS">{{ $t('point_of_sale') }}</option>
                                <option value="Reader">{{ $t('price_reader') }}</option>
                                <option value="Manager">{{ $t('managerRole') || 'مدير إدارة' }}</option>
                            </template>
                        </select>
                    </div>

                    <div
                        v-if="role == 'Commercial' && editForm.role == 'Manager'"
                        class="users-form-group users-sections-picker"
                    >
                        <label class="users-form-label">
                            <b-icon icon="grid-3x3-gap-fill" class="form-label-icon"></b-icon>
                            {{ $t('sectionsPermissions') || 'صلاحيات الأقسام' }}
                        </label>
                        <p class="text-muted small mb-2">{{ $t('selectAtLeastOneSection') || 'اختر قسماً واحداً على الأقل' }}</p>
                        <div class="users-sections-grid">
                            <label
                                v-for="key in assignableSectionKeys"
                                :key="'edit-sec-' + key"
                                class="users-section-check"
                            >
                                <input
                                    type="checkbox"
                                    :value="key"
                                    v-model="editForm.allowedSections"
                                />
                                <span>{{ sectionLabel(key) }}</span>
                            </label>
                        </div>
                        <div class="users-form-group mt-3">
                            <label class="users-section-check d-flex align-items-start gap-2">
                                <input
                                    type="checkbox"
                                    v-model="editForm.canUseOwnLoginCodeForSensitiveActions"
                                />
                                <span>{{ $t('managerCanUseOwnLoginCode') || 'يمكنه استخدام رمز الدخول الخاص به لتأكيد الإجراءات الحساسة' }}</span>
                            </label>
                        </div>
                        <div
                            v-if="editForm.canUseOwnLoginCodeForSensitiveActions"
                            class="users-form-group"
                        >
                            <label class="users-form-label">
                                <b-icon icon="shield-lock" class="form-label-icon"></b-icon>
                                {{ $t('managerSensitiveLoginCodeLabel') || 'رمز تأكيد الإجراءات' }}
                            </label>
                            <input
                                v-model="editForm.loginCode"
                                type="password"
                                inputmode="numeric"
                                maxlength="12"
                                autocomplete="off"
                                :placeholder="$t('managerSensitiveLoginCodePlaceholder') || '4–12 رقماً'"
                                class="users-form-input"
                            />
                        </div>
                    </div>
                    
                    <!-- Commercial User Fields (Only for Admin) -->
                    <template v-if="role == 'Admin' && editForm.role == 'Commercial'">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="shop" class="form-label-icon"></b-icon>
                                {{ $t('storeName') || 'اسم المتجر' }}
                            </label>
                            <input
                                v-model="editForm.storeName"
                                type="text"
                                class="users-form-input"
                                :placeholder="$t('storeName') || 'اسم المتجر'"
                            />
                        </div>
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="key-fill" class="form-label-icon"></b-icon>
                                {{ $t('accountLoginCodeLabel') || 'رمز الحساب' }}
                            </label>
                            <input
                                v-model="editForm.loginCode"
                                type="text"
                                inputmode="numeric"
                                maxlength="12"
                                autocomplete="off"
                                :placeholder="$t('accountLoginCodeAdminEditPlaceholder') || 'اتركه فارغاً لإلغاء الرمز أو أدخل رقماً جديداً'"
                                class="users-form-input"
                            />
                            <small class="text-muted d-block mt-1">{{ $t('accountLoginCodeAdminHint') || 'يسمح لتاجر الحساب بتسجيل الدخول بهذا الرمز فقط دون هاتف وكلمة مرور' }}</small>
                        </div>
                    </template>
                    
                    <div class="users-form-actions">
                        <button type="submit" class="users-form-submit-button" :disabled="show == true">
                            <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                            <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                            {{ $t('edit') }}
                        </button>
                        <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editUser')">
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
                    <p class="delete-confirmation-text">{{ $t('areYouSureDeleteUser') || 'هل أنت متأكد من حذف هذا المستخدم؟' }}</p>
                    <div class="delete-confirmation-actions">
                        <button class="delete-confirm-button" @click="deleteUser('modal-delete')">
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
import {
    ASSIGNABLE_SECTION_KEYS,
    SECTION_I18N_KEYS,
    parseAllowedSectionsJson,
} from "@/navigation/sectionRegistry.js";

export default {
    name: "UsersView",
    components: {
        AppHeader,
        ClockVue,
        "vue-barcode": VueBarcode,

    },
    data() {
        return {
            show: false,
            Users: [],
            pageNumber: 1,
            totalUsers: 0,
            pageSize: 10,
            search: {
                info: "",
            },
            SearchUsers: [],
            totalCardUsers: 0,
            userInfo: {},
            editForm: {
                name: "",
                phoneNumber: "",
                username: "",
                role: "",
                id: "",
                storeName: "",
                loginCode: "",
                logo: null,
                logoPreview: null,
                logoFile: null,
                allowedSections: [],
                canUseOwnLoginCodeForSensitiveActions: false
            },
            addForm: {
                name: "",
                phoneNumber: "",
                password: "",
                username: "",
                role: "",
                storeName: "",
                loginCode: "",
                logoFile: null,
                logoPreview: null,
                allowedSections: [],
                canUseOwnLoginCodeForSensitiveActions: false
            },
            UserId: '',
        };
    },

    watch: {

        search: {
            handler() {
                this.GetAllUsers();
            },
            deep: true,
        },

        pageNumber() {
            this.GetAllUsers();
        },
    },

    mounted() {
        this.GetAllUsers();
    },

    computed: {
        role() {
            return localStorage.getItem("role");
        },
        assignableSectionKeys() {
            return ASSIGNABLE_SECTION_KEYS;
        },
    },

    methods: {
        getRoleClass(role) {
            const roleClasses = {
                'Admin': 'role-admin',
                'Commercial': 'role-commercial',
                'POS': 'role-pos',
                'Reader': 'role-reader',
                'Manager': 'role-manager'
            };
            return roleClasses[role] || 'role-default';
        },
        sectionLabel(key) {
            const i18nKey = SECTION_I18N_KEYS[key];
            if (i18nKey && this.$te(i18nKey)) return this.$t(i18nKey);
            return key;
        },
        appendManagerSections(formData, role, allowedSections, canUseOwnLoginCode, loginCode) {
            if (role !== 'Manager') return;
            formData.append(
                'allowedSectionsJson',
                JSON.stringify(Array.isArray(allowedSections) ? allowedSections : [])
            );
            formData.append(
                'canUseOwnLoginCodeForSensitiveActions',
                canUseOwnLoginCode ? 'true' : 'false'
            );
            if (loginCode && String(loginCode).trim()) {
                formData.append('loginCode', String(loginCode).trim());
            }
        },
        deleteUserModel(id) {
            this.UserId = id;
            this.$bvModal.show("modal-delete");
        },
        getUserInfo(User) {
            // Check if Commercial user is trying to edit a Commercial user
            if (this.role === 'Commercial' && User.role === 'Commercial') {
                this.$notify.error(this.$i18n.t('noPermissionToEditCommercial') || 'ليس لديك صلاحية لتعديل المستخدمين التجاريين. فقط المدير الرئيسي يمكنه ذلك', {
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
            
            this.editForm = {
                ...User,
                storeName: User.storeName || User.StoreName || '',
                password: '',
                loginCode: '',
                allowedSections: parseAllowedSectionsJson(
                    User.allowedSectionsJson || User.AllowedSectionsJson
                ),
                canUseOwnLoginCodeForSensitiveActions: !!(
                    User.canUseOwnLoginCodeForSensitiveActions ||
                    User.CanUseOwnLoginCodeForSensitiveActions
                )
            };
            this.$bvModal.show("modal-editUser");
        },
        addUser() {
            if (
                this.addForm.role === 'Manager' &&
                (!this.addForm.allowedSections || !this.addForm.allowedSections.length)
            ) {
                this.$notify.error(this.$t('selectAtLeastOneSection') || 'اختر قسماً واحداً على الأقل');
                return;
            }
            if (
                this.addForm.role === 'Manager' &&
                this.addForm.canUseOwnLoginCodeForSensitiveActions &&
                !String(this.addForm.loginCode || '').trim()
            ) {
                this.$notify.error(this.$t('managerLoginCodeRequiredForSensitiveActions') || 'رمز التأكيد مطلوب عند تفعيل الخيار');
                return;
            }
            this.show = true;

            const formData = new FormData();
            formData.append('name', this.addForm.name);
            formData.append('phoneNumber', this.addForm.phoneNumber);
            formData.append('password', this.addForm.password);
            formData.append('username', this.addForm.username);
            formData.append('role', this.addForm.role);
            if (this.role === 'Admin' && this.addForm.role === 'Commercial') {
                if (this.addForm.storeName) {
                    formData.append('storeName', this.addForm.storeName);
                }
                if (this.addForm.loginCode && String(this.addForm.loginCode).trim()) {
                    formData.append('loginCode', String(this.addForm.loginCode).trim());
                }
            }
            this.appendManagerSections(
                formData,
                this.addForm.role,
                this.addForm.allowedSections,
                this.addForm.canUseOwnLoginCodeForSensitiveActions,
                this.addForm.loginCode
            );

            HTTP.post(`Admin/AddUser`, formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            })
                .then(() => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('userHasbeenAddedSuccessfully'));
                    this.addForm = {
                        name: "",
                        phoneNumber: "",
                        password: "",
                        username: "",
                        role: "",
                        storeName: "",
                        loginCode: "",
                        logoFile: null,
                        logoPreview: null,
                        allowedSections: [],
                        canUseOwnLoginCodeForSensitiveActions: false
                    };
                    this.GetAllUsers();
                    this.$bvModal.hide('modal-addUser');
                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(error.response?.data?.message || this.$i18n.t('somethingWrong'));
                });
        },
        EditUser() {
            if (
                this.editForm.role === 'Manager' &&
                (!this.editForm.allowedSections || !this.editForm.allowedSections.length)
            ) {
                this.$notify.error(this.$t('selectAtLeastOneSection') || 'اختر قسماً واحداً على الأقل');
                return;
            }
            this.show = true;

            const formData = new FormData();
            formData.append('name', this.editForm.name);
            formData.append('phoneNumber', this.editForm.phoneNumber);
            formData.append('username', this.editForm.username);
            formData.append('role', this.editForm.role);
            if (this.role === 'Admin' && this.editForm.role === 'Commercial') {
                formData.append('storeName', this.editForm.storeName || this.editForm.StoreName || '');
                formData.append('loginCode', this.editForm.loginCode || '');
            }
            this.appendManagerSections(
                formData,
                this.editForm.role,
                this.editForm.allowedSections,
                this.editForm.canUseOwnLoginCodeForSensitiveActions,
                this.editForm.role === 'Manager' ? this.editForm.loginCode : ''
            );
            if (this.editForm.password) {
                formData.append('password', this.editForm.password);
            }

            HTTP.put(`Admin/UpdateUser?id=${this.editForm.id}`, formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            })
                .then(() => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('userHadbeenEditSuccessfully'));
                    this.GetAllUsers();
                    this.$bvModal.hide('modal-editUser');
                    this.editForm = {
                        name: "",
                        phoneNumber: "",
                        username: "",
                        role: "",
                        id: "",
                        storeName: "",
                        loginCode: "",
                        logo: null,
                        logoPreview: null,
                        logoFile: null,
                        allowedSections: [],
                        canUseOwnLoginCodeForSensitiveActions: false
                    };
                })
                .catch((error) => {
                    this.show = false;
                    this.$notify.error(error.response?.data?.message || this.$i18n.t('somethingWrong'));
                });
        },

        deleteUser(modelId) {
            this.show = true;
            HTTP.delete(`Admin/DeleteUser?id=${this.UserId}`)
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('userHadbeenDeleteSuccessfully'), {
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
                    this.GetAllUsers();
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


        GetAllUsers() {
            this.show = true;
            HTTP.get(`Admin/GetUsers?pageNumber=${this.pageNumber - 1}&pageSize=${this.pageSize}&info=${this.search.info}`)
                .then((response) => {
                    this.Users = response.data.data.items;
                    this.totalUsers = response.data.data.totalItems;
                    this.show = false;
                })
                .catch((error) => {
                    this.show = false;
                });
        },

    },


};
</script>