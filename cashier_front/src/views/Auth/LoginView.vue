<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <div class="login-page-wrapper">
            <div class="login-background-decoration"></div>
            <div class="login-page-row">
                <!-- Left Side - Branding Section -->
                <div class="login-brand-panel">
                    <div class="brand-content-wrapper">
                        <div class="brand-logo-container">
                            <img src="../../assets/logoarabic.png" alt="logo" class="brand-logo-image" />
                        </div>
                        <h1 class="brand-main-title">{{ $t('welcomeMessage') }}</h1>
                        <p class="brand-secondary-text">{{ $t('loginSubtitle') || 'نظام إدارة نقاط البيع المتطور والاحترافي' }}</p>
                        
                        <div class="brand-features-list">
                            <div class="brand-feature-card">
                                <div class="feature-icon-container">
                                    <b-icon icon="inbox-fill" class="feature-icon-element"></b-icon>
                                </div>
                                <div class="feature-text-container">
                                    <h4 class="feature-card-title">إدارة المنتجات</h4>
                                    <p class="feature-card-text">إدارة سهلة وسريعة للمنتجات والمخزون</p>
                                </div>
                            </div>
                            
                            <div class="brand-feature-card">
                                <div class="feature-icon-container">
                                    <b-icon icon="file-earmark-bar-graph-fill" class="feature-icon-element"></b-icon>
                                </div>
                                <div class="feature-text-container">
                                    <h4 class="feature-card-title">تقارير مفصلة</h4>
                                    <p class="feature-card-text">تقارير شاملة عن المبيعات والأرباح</p>
                                </div>
                            </div>
                            
                            <div class="brand-feature-card">
                                <div class="feature-icon-container">
                                    <b-icon icon="speedometer" class="feature-icon-element"></b-icon>
                                </div>
                                <div class="feature-text-container">
                                    <h4 class="feature-card-title">واجهة عصرية</h4>
                                    <p class="feature-card-text">تصميم عصري وسهل الاستخدام</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- Right Side - Login Form -->
                <div class="login-form-panel">
                    <div class="form-content-wrapper">
                        <div class="form-logo-mobile-only">
                            <img src="../../assets/logoarabic.png" alt="logo" class="mobile-logo-image" />
                        </div>
                        
                        <div class="form-header-section">
                            <h1 class="form-main-title">{{ $t('loginTitle') }}</h1>
                            <p class="form-secondary-text">مرحباً بك، يرجى تسجيل الدخول للمتابعة</p>
                        </div>
                        
                        <form @submit.prevent="login" class="login-form-element">
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
                                        :placeholder="$t('phoneNumberPlaceholder')" 
                                        required 
                                        autofocus
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
                            
                            <button type="submit" class="login-submit-button">
                                <span class="button-content-wrapper">
                                    <b-icon icon="box-arrow-in-right" class="button-icon-element"></b-icon>
                                    <span class="button-text-element">{{ $t('loginButton') }}</span>
                                </span>
                            </button>
                            
                            <div class="form-footer-section">
                                <p class="footer-main-text">{{ $t('noAccountMessage') }}</p>
                                <router-link to="/register" class="register-link-button">
                                    <b-icon icon="person-plus-fill" class="link-icon-element"></b-icon>
                                    <span>{{ $t('registerLink') }}</span>
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
    name: 'LoginView',
    data() {
        return {
            show: false,
            form: {
                phoneNumber: '',
                password: ''
            }
        };
    },

    methods: {
        login() {
            // Validation
            if (!this.form.phoneNumber || !this.form.password) {
                this.$toast.error(this.$i18n.t('pleaseFillAllFields') || 'Please fill all fields', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            this.show = true;
            HTTP.post('Auth/Login', this.form)
                .then(response => {
                    if (response.data && response.data.token) {
                        localStorage.setItem('token', response.data.token);
                        localStorage.setItem('role', response.data.role);
                        localStorage.setItem('info', JSON.stringify(response.data.info || {}));
                        
                        // Redirect based on role
                        const role = response.data.role;
                        if (role === 'Admin') {
                            this.$router.push('/users');
                        } else if (role === 'POS') {
                            this.$router.push('/pos');
                        } else if (role === 'Reader') {
                            this.$router.push('/priceReader');
                        } else if (role === 'TablesManager') {
                            this.$router.push('/restaurant/tables');
                        } else if (role === 'ReservationsManager') {
                            this.$router.push('/restaurant/reservations');
                        } else if (role === 'KitchenManager') {
                            this.$router.push('/restaurant/kitchen');
                        } else if (role === 'LoyaltyManager') {
                            this.$router.push('/restaurant/loyalty');
                        } else if (role === 'Waiter') {
                            this.$router.push('/restaurant/waiter');
                        } else {
                            this.$router.push('/dashboard');
                        }
                    } else {
                        throw new Error('Invalid response from server');
                    }
                    this.show = false;
                })
                .catch(error => {
                    this.show = false;
                    const errorMessage = error.response?.data?.message || 
                                       error.response?.data?.error || 
                                       this.$i18n.t('errorInLoginInfo') || 
                                       'Error in login information';
                    
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
        }
    }

}
</script>
