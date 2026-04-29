<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <div class="register-page-wrapper">
            <div class="register-background-decoration"></div>
            <div class="register-page-row">
                <!-- Left Side - Branding Section -->
                <div class="register-brand-panel">
                    <div class="brand-content-wrapper">
                        <div class="brand-logo-container">
                            <img src="../../assets/logoarabicdark.png" alt="logo" class="brand-logo-image" />
                        </div>
                        <h1 class="brand-main-title">{{ $t('registerTitle') }}</h1>
                        <p class="brand-secondary-text">انضم إلينا وابدأ رحلتك مع نظام إدارة نقاط البيع المتطور</p>
                        
                        <div class="brand-features-list">
                            <div class="brand-feature-card">
                                <div class="feature-icon-container">
                                    <b-icon icon="shield-check" class="feature-icon-element"></b-icon>
                                </div>
                                <div class="feature-text-container">
                                    <h4 class="feature-card-title">حساب آمن</h4>
                                    <p class="feature-card-text">حماية كاملة لبياناتك ومعلوماتك</p>
                                </div>
                            </div>
                            
                            <div class="brand-feature-card">
                                <div class="feature-icon-container">
                                    <b-icon icon="lightning-charge" class="feature-icon-element"></b-icon>
                                </div>
                                <div class="feature-text-container">
                                    <h4 class="feature-card-title">سهولة الاستخدام</h4>
                                    <p class="feature-card-text">واجهة بسيطة وسهلة للتعامل</p>
                                </div>
                            </div>
                            
                            <div class="brand-feature-card">
                                <div class="feature-icon-container">
                                    <b-icon icon="people" class="feature-icon-element"></b-icon>
                                </div>
                                <div class="feature-text-container">
                                    <h4 class="feature-card-title">دعم فني</h4>
                                    <p class="feature-card-text">فريق دعم متاح لمساعدتك دائماً</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- Right Side - Register Form -->
                <div class="register-form-panel">
                    <div class="form-content-wrapper">
                        <div class="form-logo-mobile-only">
                            <img src="../../assets/logoarabicdark.png" alt="logo" class="mobile-logo-image" />
                        </div>
                        
                        <div class="form-header-section">
                            <h1 class="form-main-title">{{ $t('registerTitle') }}</h1>
                            <p class="form-secondary-text">أنشئ حسابك الجديد وابدأ الآن</p>
                        </div>
                        
                        <form @submit.prevent="register" class="register-form-element">
                            <div class="form-field-group">
                                <label class="form-field-label">
                                    <b-icon icon="person-fill" class="form-field-icon"></b-icon>
                                    {{ $t('fullNamePlaceholder') }}
                                </label>
                                <div class="form-input-container">
                                    <input 
                                        id="inputName" 
                                        v-model="form.name" 
                                        type="text"
                                        minlength="8"
                                        :placeholder="$t('fullNamePlaceholder')" 
                                        required 
                                        autofocus
                                        class="form-input-field"
                                    />
                                </div>
                            </div>
                            
                            <div class="form-field-group">
                                <label class="form-field-label">
                                    <b-icon icon="telephone-fill" class="form-field-icon"></b-icon>
                                    {{ $t('phoneNumberPlaceholder') }}
                                </label>
                                <div class="form-input-container">
                                    <input 
                                        id="inputNumber" 
                                        v-model="form.phoneNumber" 
                                        type="tel"
                                        pattern="07\d{9}"
                                        minlength="11"
                                        :placeholder="$t('phoneNumberPlaceholder')" 
                                        required
                                        class="form-input-field"
                                    />
                                </div>
                            </div>
                            
                            <div class="form-field-group">
                                <label class="form-field-label">
                                    <b-icon icon="lock-fill" class="form-field-icon"></b-icon>
                                    {{ $t('passwordPlaceholder') }}
                                </label>
                                <div class="form-input-container">
                                    <input 
                                        id="inputPassword" 
                                        v-model="form.password" 
                                        minlength="8" 
                                        type="password"
                                        :placeholder="$t('passwordPlaceholder')" 
                                        required
                                        class="form-input-field"
                                    />
                                </div>
                            </div>
                            
                            <!-- Restaurant Name Field -->
                            <div class="form-field-group">
                                <label class="form-field-label">
                                    <b-icon icon="shop" class="form-field-icon"></b-icon>
                                    {{ $t('restaurantName') || 'اسم المطعم' }}
                                </label>
                                <div class="form-input-container">
                                    <input 
                                        id="inputRestaurantName" 
                                        v-model="form.restaurantName" 
                                        type="text"
                                        :placeholder="$t('restaurantName') || 'اسم المطعم'" 
                                        class="form-input-field"
                                    />
                                </div>
                            </div>
                            
                            <!-- Logo Upload Field -->
                            <div class="form-field-group">
                                <label class="form-field-label">
                                    <b-icon icon="image" class="form-field-icon"></b-icon>
                                    {{ $t('logo') || 'الشعار' }}
                                </label>
                                <div class="logo-upload-section">
                                    <div v-if="form.logoPreview" class="logo-preview">
                                        <img :src="form.logoPreview" alt="Logo Preview" class="logo-preview-img" />
                                        <button type="button" class="logo-remove-btn" @click="removeLogoPreview">
                                            <b-icon icon="x-circle-fill"></b-icon>
                                        </button>
                                    </div>
                                    <input 
                                        ref="logoInput"
                                        type="file" 
                                        accept="image/*"
                                        @change="handleLogoChange"
                                        class="form-input-field"
                                        style="display: none;"
                                    />
                                    <button 
                                        type="button" 
                                        class="logo-upload-btn"
                                        @click="$refs.logoInput.click()"
                                    >
                                        <b-icon icon="upload" class="me-2"></b-icon>
                                        {{ form.logoPreview ? ($t('changeLogo') || 'تغيير الشعار') : ($t('uploadLogo') || 'رفع شعار') }}
                                    </button>
                                </div>
                            </div>
                            
                            <button type="submit" class="register-submit-button">
                                <span class="button-content-wrapper">
                                    <b-icon icon="person-plus-fill" class="button-icon-element"></b-icon>
                                    <span class="button-text-element">{{ $t('registerButton') }}</span>
                                </span>
                            </button>
                            
                            <div class="form-footer-section">
                                <p class="footer-main-text">{{ $t('alreadyHaveAccountMessage') }}</p>
                                <router-link to="/login" class="login-link-button">
                                    <b-icon icon="box-arrow-in-right" class="link-icon-element"></b-icon>
                                    <span>{{ $t('loginLink') }}</span>
                                </router-link>
                            </div>
                            
                            <div class="form-developer-section">
                                <p class="developer-main-text">
                                    {{ $t('developedBy') }}
                                    <a :href="$t('companyWebsite')" class="developer-link-button" target="_blank">
                                        {{ $t('companyName') }}
                                    </a>
                                </p>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </b-overlay>
</template>
<script>
import { HTTP } from '../../http/api.js';

export default {
    name: 'RegisterView',
    data() {
        return {
            show: false,
            form: {
                phoneNumber: '',
                password: '',
                name: '',
                username: 'formWeb',
                role: 'Commercial',
                restaurantName: '',
                logoFile: null,
                logoPreview: null
            }
        };
    },

    methods: {
        register() {
            // Validation
            if (!this.form.name || !this.form.phoneNumber || !this.form.password) {
                this.$toast.error(this.$i18n.t('pleaseFillAllFields') || 'Please fill all fields', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            // Phone number validation (Iraqi format)
            if (this.form.phoneNumber.length < 10 || this.form.phoneNumber.length > 11) {
                this.$toast.error(this.$i18n.t('invalidPhoneNumber') || 'Invalid phone number', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            // Password validation
            if (this.form.password.length < 8) {
                this.$toast.error(this.$i18n.t('passwordTooShort') || 'Password must be at least 8 characters', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            this.show = true;
            
            // Create FormData for file upload
            const formData = new FormData();
            formData.append('name', this.form.name);
            formData.append('phoneNumber', this.form.phoneNumber);
            formData.append('password', this.form.password);
            formData.append('username', this.form.username);
            formData.append('role', this.form.role);
            
            // Add logo if provided
            if (this.form.logoFile) {
                formData.append('logo', this.form.logoFile);
            }
            
            // Add restaurant name if provided
            if (this.form.restaurantName) {
                formData.append('restaurantName', this.form.restaurantName);
            }
            
            HTTP.post('Auth/RegisterUser', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            })
                .then(response => {
                    this.show = false;
                    if (response.data && !response.data.errorStatus) {
                        this.$toast.success(this.$i18n.t('sucessRegister') || 'Registration successful', {
                            position: "top-right",
                            timeout: 5000,
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
                        this.$router.push('/login');
                    } else {
                        throw new Error(response.data?.message || 'Registration failed');
                    }
                })
                .catch(error => {
                    this.show = false;
                    const errorMessage = error.response?.data?.message || 
                                       error.response?.data?.error || 
                                       error.message ||
                                       this.$i18n.t('registrationError') || 
                                       'Registration failed. Please try again.';
                    
                    this.$toast.error(errorMessage, {
                        position: "top-right",
                        timeout: 5000,
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
        
        handleLogoChange(event) {
            const file = event.target.files[0];
            if (file) {
                // Validate file type
                const validImageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
                if (!validImageTypes.includes(file.type)) {
                    this.$toast.error(this.$i18n.t('invalidImageType') || 'Invalid image type. Please upload a JPEG, PNG, or GIF image.', {
                        position: "top-right",
                        timeout: 4000,
                    });
                    return;
                }
                
                // Validate file size (max 5MB)
                if (file.size > 5 * 1024 * 1024) {
                    this.$toast.error(this.$i18n.t('imageTooLarge') || 'Image size is too large. Maximum size is 5MB.', {
                        position: "top-right",
                        timeout: 4000,
                    });
                    return;
                }
                
                this.form.logoFile = file;
                const reader = new FileReader();
                reader.onload = (e) => {
                    this.form.logoPreview = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        },
        
        removeLogoPreview() {
            this.form.logoPreview = null;
            this.form.logoFile = null;
            if (this.$refs.logoInput) {
                this.$refs.logoInput.value = '';
            }
        }
    }

}
</script>
