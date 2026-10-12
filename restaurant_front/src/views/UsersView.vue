<template>
    <b-overlay :show="show && Users.length > 0" spinner-variant="primary" spinner-type="border" rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
        <div class="app-page-container">
            <div class="app-page-content users-page">
                <div class="users-header-section">
                    <div class="users-header-content app-header-row">
                        <div class="header-title-wrapper">
                            <div class="header-icon-wrapper">
                                <b-icon icon="people-fill" class="header-icon"></b-icon>
                            </div>
                            <div>
                                <h1 class="users-page-title">{{ $t('all_accounts') }}</h1>
                                <p class="header-subtitle">{{ $t('usersPageDescription') || 'إدارة حسابات النظام والصلاحيات' }}</p>
                            </div>
                        </div>
                        <div class="app-header-actions">
                            <button type="button" class="btn-refresh" @click="refreshPage" :disabled="show">
                                <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: show }"></b-icon>
                                <span class="button-text">{{ $t('refresh') || 'تحديث' }}</span>
                            </button>
                            <button type="button" class="users-add-button" v-b-modal.modal-addUser>
                                <b-icon icon="person-plus-fill" class="button-icon"></b-icon>
                                <span class="button-text">{{ $t('add_account') }}</span>
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
                                <b-spinner small v-if="show && !Users.length"></b-spinner>
                                <template v-else>{{ totalUsers }}</template>
                            </div>
                            <div class="app-overview-stat-label">{{ $t('usersOverviewTotal') || 'إجمالي الحسابات' }}</div>
                        </div>
                    </div>
                    <div class="app-overview-stat">
                        <span class="app-overview-stat-icon app-overview-stat-icon--info">
                            <b-icon icon="person-badge-fill"></b-icon>
                        </span>
                        <div>
                            <div class="app-overview-stat-value">{{ usersOnPageCount }}</div>
                            <div class="app-overview-stat-label">{{ $t('usersOverviewOnPage') || 'في الصفحة الحالية' }}</div>
                        </div>
                    </div>
                    <div class="app-overview-stat">
                        <span class="app-overview-stat-icon app-overview-stat-icon--success">
                            <b-icon :icon="role === 'Admin' ? 'shop' : 'shield-check'"></b-icon>
                        </span>
                        <div>
                            <div class="app-overview-stat-value">{{ overviewStatThreeCount }}</div>
                            <div class="app-overview-stat-label">{{ overviewStatThreeLabel }}</div>
                        </div>
                    </div>
                    <div class="app-overview-stat">
                        <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                            <b-icon icon="cash-stack"></b-icon>
                        </span>
                        <div>
                            <div class="app-overview-stat-value">{{ posCountOnPage }}</div>
                            <div class="app-overview-stat-label">{{ $t('point_of_sale') || 'نقطة بيع' }}</div>
                        </div>
                    </div>
                </div>

                <div class="app-section-card">
                    <div class="app-section-header app-section-header--toolbar">
                        <div class="app-section-title-wrap">
                            <div class="app-section-icon-wrap">
                                <b-icon icon="person-lines-fill"></b-icon>
                            </div>
                            <div>
                                <h3 class="app-section-title">{{ $t('all_accounts') }}</h3>
                                <p class="app-section-subtitle">{{ $t('usersListHint') || 'قائمة الحسابات مع الأدوار والصلاحيات' }}</p>
                            </div>
                        </div>
                    </div>
                    <div class="app-filters-panel app-filters-panel--inset">
                        <div class="app-filters-panel-head">
                            <div class="app-filters-panel-title">
                                <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                                <div>
                                    <h3>{{ $t('filters') || 'الفلاتر' }}</h3>
                                    <p>{{ $t('usersFiltersHint') || 'بحث في الحسابات بالاسم أو اسم المستخدم وتصفية حسب الدور' }}</p>
                                </div>
                            </div>
                            <div class="app-filters-panel-actions" v-if="hasActiveFilters">
                                <button type="button" class="users-filter-clear-btn app-filters-clear-btn" @click="clearFilters">
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
                                        :placeholder="$t('usersSearchPlaceholder') || 'بحث بالاسم أو الهاتف أو اسم المستخدم...'"
                                        class="users-search-input"
                                        autocomplete="off"
                                    />
                                </div>
                            </label>
                            <label class="app-filter-field">
                                <span class="app-filter-label">{{ $t('role') || 'الدور' }}</span>
                                <div class="users-search-container">
                                    <b-icon icon="person-badge" class="search-icon"></b-icon>
                                    <select v-model="roleFilter" class="users-search-input reports-filter-select">
                                        <option value="">{{ $t('allRoles') || 'كل الأدوار' }}</option>
                                        <option v-for="opt in roleFilterOptions" :key="opt.value" :value="opt.value">
                                            {{ opt.label }}
                                        </option>
                                    </select>
                                </div>
                            </label>
                        </div>
                    </div>
                    <div class="app-section-body">
                        <div v-if="show && !Users.length" class="loading-state-full">
                            <b-spinner small></b-spinner>
                            <span>{{ $t('loading') || 'جاري التحميل...' }}</span>
                        </div>
                        <div v-else-if="Users.length > 0" class="app-cards-grid">
                            <div
                                v-for="User in Users"
                                :key="User.id"
                                class="app-item-card users-account-card"
                            >
                                <div class="app-item-card-header">
                                    <div class="app-item-card-title">
                                        <span class="users-avatar-chip" :class="getRoleClass(User.role)">
                                            <b-icon icon="person-fill"></b-icon>
                                        </span>
                                        <div class="users-account-title-text">
                                            <h4>{{ User.name }}</h4>
                                            <span class="user-role-badge" :class="getRoleClass(User.role)">{{ roleLabel(User.role) }}</span>
                                        </div>
                                    </div>
                                    <div class="app-item-card-actions" role="group" :aria-label="$t('actions') || 'العمليات'">
                                        <button
                                            type="button"
                                            class="action-btn action-btn--icon action-btn--edit"
                                            @click="getUserInfo(User)"
                                            :disabled="role === 'Commercial' && User.role === 'Commercial'"
                                            :title="role === 'Commercial' && User.role === 'Commercial' ? ($t('noPermissionToEditCommercial') || 'ليس لديك صلاحية لتعديل المستخدمين التجاريين') : ($t('edit') || 'تعديل')"
                                            :aria-label="$t('edit') || 'تعديل'"
                                        >
                                            <b-icon icon="pencil-square" class="action-icon"></b-icon>
                                        </button>
                                        <button
                                            type="button"
                                            class="action-btn action-btn--icon action-btn--delete"
                                            @click="deleteUserModel(User)"
                                            :title="$t('delete') || 'حذف'"
                                            :aria-label="$t('delete') || 'حذف'"
                                        >
                                            <b-icon icon="trash" class="action-icon"></b-icon>
                                        </button>
                                    </div>
                                </div>
                                <div class="app-item-card-body">
                                    <div class="app-info-row">
                                        <b-icon icon="person" class="info-icon"></b-icon>
                                        <span class="info-label">{{ $t('username') }}</span>
                                        <span class="info-value">{{ User.username || '—' }}</span>
                                    </div>
                                    <div v-if="User.phoneNumber || User.PhoneNumber" class="app-info-row">
                                        <b-icon icon="telephone-fill" class="info-icon"></b-icon>
                                        <span class="info-label">{{ $t('phone_number') }}</span>
                                        <a class="info-value users-phone-link" :href="'tel:' + (User.phoneNumber || User.PhoneNumber)">{{ User.phoneNumber || User.PhoneNumber }}</a>
                                    </div>
                                    <div v-if="isSubordinateRole(User.role)" class="app-info-row">
                                        <b-icon icon="shop" class="info-icon"></b-icon>
                                        <span class="info-label">{{ $t('belongsToCommercial') || 'تابع لـ' }}</span>
                                        <span class="info-value">{{ commercialOwnerLabel(User) }}</span>
                                    </div>
                                    <div v-if="User.role === 'Commercial' && (User.restaurantName || User.RestaurantName)" class="app-info-row">
                                        <b-icon icon="shop" class="info-icon"></b-icon>
                                        <span class="info-label">{{ $t('restaurantName') || 'اسم المطعم' }}</span>
                                        <span class="info-value">{{ User.restaurantName || User.RestaurantName }}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div v-else class="empty-state">
                            <b-icon icon="people" class="empty-icon"></b-icon>
                            <p>{{ emptyStateMessage }}</p>
                            <button v-if="!hasActiveFilters" type="button" class="empty-state-btn" v-b-modal.modal-addUser>
                                <b-icon icon="person-plus-fill" class="button-icon"></b-icon>
                                <span class="button-text">{{ $t('add_account') }}</span>
                            </button>
                        </div>

                        <div v-if="Users.length > 0" class="users-pagination-section">
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
                            <option v-if="role == 'Commercial'" value="Waiter">{{ $t('waiter') || 'ويتر' }}</option>
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
                            <small class="text-muted d-block mt-1">{{ $t('managerCanUseOwnLoginCodeHint') || 'نقل الفواتير، الدمج، الخصم وغيرها في نقطة البيع' }}</small>
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
                            <small class="text-muted d-block mt-1">{{ $t('managerSensitiveLoginCodeHint') || 'يُطلب عند تفعيل الخيار. يحتاج المدير إعادة تسجيل الدخول بعد التعديل.' }}</small>
                        </div>
                    </div>
                    
                    <!-- Commercial User Fields (Only for Admin) -->
                    <template v-if="role == 'Admin' && addForm.role == 'Commercial'">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="shop" class="form-label-icon"></b-icon>
                                {{ $t('restaurantName') || 'اسم المطعم' }}
                            </label>
                            <input 
                                v-model="addForm.restaurantName" 
                                type="text"
                                :placeholder="$t('restaurantName') || 'اسم المطعم'" 
                                class="users-form-input"
                            />
                        </div>
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="key-fill" class="form-label-icon"></b-icon>
                                {{ $t('accountLoginCodeLabel') || 'رمز دخول سريع' }}
                            </label>
                            <input
                                v-model="addForm.loginCode"
                                type="text"
                                inputmode="numeric"
                                maxlength="12"
                                :placeholder="$t('accountLoginCodeAdminPlaceholder') || 'اختياري: 4–12 رقماً'"
                                class="users-form-input"
                            />
                            <small class="text-muted d-block mt-1">{{ $t('accountLoginCodeAdminHint') || 'يسمح بتسجيل الدخول برمز فقط دون هاتف وكلمة مرور' }}</small>
                        </div>
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="image" class="form-label-icon"></b-icon>
                                {{ $t('logo') || 'الشعار' }}
                            </label>
                            <div class="logo-upload-section">
                                <div v-if="addForm.logoPreview" class="logo-preview">
                                    <img :src="addForm.logoPreview" alt="Logo Preview" class="logo-preview-img" />
                                    <button type="button" class="logo-remove-btn" @click="removeAddLogoPreview">
                                        <b-icon icon="x-circle-fill"></b-icon>
                                    </button>
                                </div>
                                <input 
                                    ref="addLogoInput"
                                    type="file" 
                                    accept="image/*"
                                    @change="handleAddLogoChange"
                                    class="users-form-input"
                                    style="display: none;"
                                />
                                <button 
                                    type="button" 
                                    class="logo-upload-btn"
                                    @click="$refs.addLogoInput.click()"
                                >
                                    <b-icon icon="upload" class="me-2"></b-icon>
                                    {{ addForm.logoPreview ? ($t('changeLogo') || 'تغيير الشعار') : ($t('uploadLogo') || 'رفع شعار') }}
                                </button>
                            </div>
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
                                <option value="Waiter">{{ $t('waiter') || 'ويتر' }}</option>
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
                            <small class="text-muted d-block mt-1">{{ $t('managerCanUseOwnLoginCodeHint') || 'نقل الفواتير، الدمج، الخصم وغيرها في نقطة البيع' }}</small>
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
                            <small class="text-muted d-block mt-1">{{ $t('managerSensitiveLoginCodeHint') || 'اتركه فارغاً للإبقاء على الرمز الحالي' }}</small>
                        </div>
                    </div>
                    
                    <!-- Commercial User Fields (Only for Admin) -->
                    <template v-if="role == 'Admin' && editForm.role == 'Commercial'">
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="shop" class="form-label-icon"></b-icon>
                                {{ $t('restaurantName') || 'اسم المطعم' }}
                            </label>
                            <input 
                                v-model="editForm.restaurantName" 
                                type="text"
                                :placeholder="$t('restaurantName') || 'اسم المطعم'" 
                                class="users-form-input"
                            />
                        </div>
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="key-fill" class="form-label-icon"></b-icon>
                                {{ $t('accountLoginCodeLabel') || 'رمز دخول سريع' }}
                            </label>
                            <input
                                v-model="editForm.loginCode"
                                type="text"
                                inputmode="numeric"
                                maxlength="12"
                                :placeholder="$t('accountLoginCodeAdminPlaceholder') || 'اتركه فارغاً لإلغاء الرمز'"
                                class="users-form-input"
                            />
                            <small class="text-muted d-block mt-1">{{ $t('accountLoginCodeAdminHint') || 'يسمح بتسجيل الدخول برمز فقط دون هاتف وكلمة مرور' }}</small>
                        </div>
                        <div class="users-form-group">
                            <label class="users-form-label">
                                <b-icon icon="image" class="form-label-icon"></b-icon>
                                {{ $t('logo') || 'الشعار' }}
                            </label>
                            <div class="logo-upload-section">
                                <div v-if="editForm.logoPreview" class="logo-preview">
                                    <img :src="editForm.logoPreview" alt="Logo Preview" class="logo-preview-img" />
                                    <button type="button" class="logo-remove-btn" @click="removeLogoPreview">
                                        <b-icon icon="x-circle-fill"></b-icon>
                                    </button>
                                </div>
                                <input 
                                    ref="logoInput"
                                    type="file" 
                                    accept="image/*"
                                    @change="handleLogoChange"
                                    class="users-form-input"
                                    style="display: none;"
                                />
                                <button 
                                    type="button" 
                                    class="logo-upload-btn"
                                    @click="$refs.logoInput.click()"
                                >
                                    <b-icon icon="upload" class="me-2"></b-icon>
                                    {{ editForm.logoPreview ? ($t('changeLogo') || 'تغيير الشعار') : ($t('uploadLogo') || 'رفع شعار') }}
                                </button>
                            </div>
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
                    <p class="delete-confirmation-text">
                        {{ deleteTargetUser && deleteTargetUser.role === 'Commercial'
                            ? ($t('deleteCommercialAccountWarning') || 'سيتم حذف الحساب التجاري وجميع بياناته.')
                            : ($t('areYouSureDeleteUser') || 'هل أنت متأكد من حذف هذا المستخدم؟') }}
                    </p>
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
    },
    data() {
        return {
            show: false,
            Users: [],
            pageNumber: 1,
            totalUsers: 0,
            pageSize: 12,
            roleFilter: "",
            search: {
                info: "",
            },
            editForm: {
                name: "",
                phoneNumber: "",
                username: "",
                role: "",
                id: "",
                restaurantName: "",
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
                restaurantName: "",
                loginCode: "",
                logoFile: null,
                logoPreview: null,
                allowedSections: [],
                canUseOwnLoginCodeForSensitiveActions: false
            },
            UserId: '',
            deleteTargetUser: null,
        };
    },

    watch: {

        search: {
            handler() {
                this.pageNumber = 1;
                this.GetAllUsers();
            },
            deep: true,
        },
        roleFilter() {
            this.pageNumber = 1;
            this.GetAllUsers();
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
        usersOnPageCount() {
            return (this.Users || []).length;
        },
        commercialCountOnPage() {
            return (this.Users || []).filter((u) => u.role === "Commercial").length;
        },
        managerCountOnPage() {
            return (this.Users || []).filter((u) => u.role === "Manager").length;
        },
        posCountOnPage() {
            return (this.Users || []).filter((u) => u.role === "POS").length;
        },
        overviewStatThreeCount() {
            return this.role === "Admin" ? this.commercialCountOnPage : this.managerCountOnPage;
        },
        overviewStatThreeLabel() {
            if (this.role === "Admin") {
                return this.$t("commercial") || "تجاري";
            }
            return this.$t("managerRole") || "مدير إدارة";
        },
        hasActiveFilters() {
            return !!(this.search.info || this.roleFilter);
        },
        emptyStateMessage() {
            if (this.hasActiveFilters) {
                return this.$t("noResults") || "لا توجد نتائج";
            }
            return this.$t("noUsersYet") || "لا توجد حسابات بعد";
        },
        roleFilterOptions() {
            if (this.role === "Admin") {
                return [
                    { value: "Commercial", label: this.roleLabel("Commercial") },
                    { value: "Admin", label: this.roleLabel("Admin") },
                    { value: "POS", label: this.roleLabel("POS") },
                    { value: "Waiter", label: this.roleLabel("Waiter") },
                    { value: "Manager", label: this.roleLabel("Manager") },
                    { value: "Reader", label: this.roleLabel("Reader") },
                ];
            }
            return [
                { value: "POS", label: this.roleLabel("POS") },
                { value: "Waiter", label: this.roleLabel("Waiter") },
                { value: "Manager", label: this.roleLabel("Manager") },
                { value: "Reader", label: this.roleLabel("Reader") },
            ];
        },
    },

    methods: {
        refreshPage() {
            this.GetAllUsers();
        },
        clearFilters() {
            this.search.info = "";
            this.roleFilter = "";
        },
        roleLabel(role) {
            const map = {
                Admin: this.$t("adminRole") || "مدير النظام",
                Commercial: this.$t("commercial") || "تجاري",
                POS: this.$t("point_of_sale") || "نقطة بيع",
                Reader: this.$t("price_reader") || "قارئ أسعار",
                Waiter: this.$t("waiter") || "ويتر",
                Manager: this.$t("managerRole") || "مدير إدارة",
            };
            return map[role] || role;
        },
        isSubordinateRole(role) {
            return role === "POS" || role === "Reader" || role === "Manager" || role === "Waiter";
        },
        commercialOwnerLabel(user) {
            const store =
                user.commercialOwnerStoreName ||
                user.CommercialOwnerStoreName ||
                user.commercialOwnerRestaurantName ||
                user.CommercialOwnerRestaurantName ||
                "";
            const name =
                user.commercialOwnerName ||
                user.CommercialOwnerName ||
                "";
            if (store && name && store !== name) return `${store} · ${name}`;
            return store || name || "—";
        },
        getRoleClass(role) {
            const roleClasses = {
                'Admin': 'role-admin',
                'Commercial': 'role-commercial',
                'POS': 'role-pos',
                'Reader': 'role-reader',
                'Waiter': 'role-waiter',
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
        deleteUserModel(user) {
            this.deleteTargetUser = user;
            this.UserId = user.id;
            this.$bvModal.show("modal-delete");
        },
        getUserInfo(User) {
            // Check if Commercial user is trying to edit a Commercial user
            if (this.role === 'Commercial' && User.role === 'Commercial') {
                this.$notify.error(this.$i18n.t('noPermissionToEditCommercial') || 'ليس لديك صلاحية لتعديل المستخدمين التجاريين. فقط المدير الرئيسي يمكنه ذلك');
                return;
            }
            
            const imageBaseUrl = HTTP.defaults.baseURL?.replace('/api', '') || 'https://pos-api.tatwer.tech';
            this.editForm = {
                ...User,
                password: '',
                restaurantName: User.restaurantName || '',
                loginCode: User.loginCode || User.LoginCode || '',
                logo: null,
                logoPreview: User.logo ? `${imageBaseUrl}/Images/${User.logo}` : null,
                logoFile: null,
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
        handleLogoChange(event) {
            const file = event.target.files[0];
            if (file) {
                this.editForm.logoFile = file;
                const reader = new FileReader();
                reader.onload = (e) => {
                    this.editForm.logoPreview = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        },
        removeLogoPreview() {
            this.editForm.logoPreview = null;
            this.editForm.logoFile = null;
            if (this.$refs.logoInput) {
                this.$refs.logoInput.value = '';
            }
        },
        handleAddLogoChange(event) {
            const file = event.target.files[0];
            if (file) {
                this.addForm.logoFile = file;
                const reader = new FileReader();
                reader.onload = (e) => {
                    this.addForm.logoPreview = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        },
        removeAddLogoPreview() {
            this.addForm.logoPreview = null;
            this.addForm.logoFile = null;
            if (this.$refs.addLogoInput) {
                this.$refs.addLogoInput.value = '';
            }
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
            
            // Create FormData for file upload (if Admin adding Commercial user)
            const formData = new FormData();
            formData.append('name', this.addForm.name);
            formData.append('phoneNumber', this.addForm.phoneNumber);
            formData.append('password', this.addForm.password);
            formData.append('username', this.addForm.username);
            formData.append('role', this.addForm.role);
            this.appendManagerSections(
                formData,
                this.addForm.role,
                this.addForm.allowedSections,
                this.addForm.canUseOwnLoginCodeForSensitiveActions,
                this.addForm.loginCode
            );
            
            // Add logo and restaurant name if Admin adding Commercial user
            if (this.role === 'Admin' && this.addForm.role === 'Commercial') {
                if (this.addForm.logoFile) {
                    formData.append('logo', this.addForm.logoFile);
                }
                if (this.addForm.restaurantName) {
                    formData.append('restaurantName', this.addForm.restaurantName);
                }
                if (this.addForm.loginCode) {
                    formData.append('loginCode', this.addForm.loginCode);
                }
            }
            
            HTTP.post(`Admin/AddUser`, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            })
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('userHasbeenAddedSuccessfully'));
                    // Reset form
                    this.addForm = {
                        name: "",
                        phoneNumber: "",
                        password: "",
                        username: "",
                        role: "",
                        restaurantName: "",
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
                    const raw = error.response?.data?.message;
                    const msg = raw && this.$te(raw) ? this.$i18n.t(raw) : (raw || this.$i18n.t('somethingWrong'));
                    this.$notify.error(msg);
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
            
            // Create FormData for file upload
            const formData = new FormData();
            formData.append('name', this.editForm.name);
            formData.append('phoneNumber', this.editForm.phoneNumber);
            formData.append('username', this.editForm.username);
            formData.append('role', this.editForm.role);
            this.appendManagerSections(
                formData,
                this.editForm.role,
                this.editForm.allowedSections,
                this.editForm.canUseOwnLoginCodeForSensitiveActions,
                this.editForm.loginCode
            );
            
            // Add password only if provided
            if (this.editForm.password) {
                formData.append('password', this.editForm.password);
            }
            
            // Add logo and restaurant name if Admin editing Commercial user
            if (this.role === 'Admin' && this.editForm.role === 'Commercial') {
                if (this.editForm.logoFile) {
                    formData.append('logo', this.editForm.logoFile);
                }
                if (this.editForm.restaurantName) {
                    formData.append('restaurantName', this.editForm.restaurantName);
                }
                formData.append('loginCode', this.editForm.loginCode || '');
            }
            
            HTTP.put(`Admin/UpdateUser?id=${this.editForm.id}`, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            })
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('userHadbeenEditSuccessfully'));
                    this.GetAllUsers();
                    this.$bvModal.hide('modal-editUser');
                    // Reset form
                    this.editForm = {
                        name: "",
                        phoneNumber: "",
                        username: "",
                        role: "",
                        id: "",
                        restaurantName: "",
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
                    const raw = error.response?.data?.message;
                    const msg = raw && this.$te(raw) ? this.$i18n.t(raw) : (raw || this.$i18n.t('somethingWrong'));
                    this.$notify.error(msg);
                });
        },

        deleteUser(modelId) {
            this.show = true;
            HTTP.delete(`Admin/DeleteUser?id=${this.UserId}`)
                .then((response) => {
                    this.show = false;
                    this.$notify.success(this.$i18n.t('userHadbeenDeleteSuccessfully'));
                    this.GetAllUsers();
                    this.$bvModal.hide(modelId);
                })
                .catch((error) => {
                    this.show = false;
                    const raw = error.response?.data?.message;
                    const msg = raw && this.$te(raw) ? this.$i18n.t(raw) : (raw || this.$i18n.t('somethingWrong'));
                    this.$notify.error(msg);
                });
        },

        closeModel(id) {
            this.$bvModal.hide(id);
        },


        GetAllUsers() {
            this.show = true;
            const params = new URLSearchParams({
                pageNumber: String(this.pageNumber - 1),
                pageSize: String(this.pageSize),
                info: this.search.info || "",
            });
            if (this.roleFilter) {
                params.set("role", this.roleFilter);
            }
            HTTP.get(`Admin/GetUsers?${params.toString()}`)
                .then((response) => {
                    this.Users = response.data.data.items;
                    this.totalUsers = response.data.data.totalItems;
                    this.show = false;
                })
                .catch(() => {
                    this.show = false;
                });
        },

    },


};
</script>

<style scoped>
.users-account-card .app-item-card-title {
  align-items: flex-start;
  gap: 12px;
}

.users-avatar-chip {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
  font-size: 1.15rem;
}

.users-account-title-text {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  min-width: 0;
}

.users-account-title-text h4 {
  margin: 0;
  line-height: 1.3;
  word-break: break-word;
}

.users-account-card .user-role-badge {
  text-transform: none;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  letter-spacing: 0;
}

.users-phone-link {
  color: var(--primary-color);
  text-decoration: none;
  font-weight: 700;
}

.users-phone-link:hover {
  text-decoration: underline;
}

.users-pagination-section {
  margin-top: 1.25rem;
  display: flex;
  justify-content: center;
}
</style>
