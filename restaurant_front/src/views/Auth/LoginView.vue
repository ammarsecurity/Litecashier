<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <div class="login-page-wrapper">
            <div class="login-background-decoration"></div>
            <div class="login-page-row">
                <!-- Right Side - Login Form -->
                <div class="login-form-panel">
                    <div class="form-content-wrapper">
                        <div class="form-logo-mobile-only">
                            <img src="../../assets/logoarabicdark.png" alt="logo" class="mobile-logo-image" />
                        </div>
                        
                        <div class="form-header-section">
                            <h1 class="form-main-title">{{ $t('loginTitle') }}</h1>
                            <p class="form-secondary-text">مرحباً بك، يرجى تسجيل الدخول للمتابعة</p>
                        </div>

                        <div class="login-mode-toggle" role="group" :aria-label="$t('loginModeToggleAria') || 'طريقة الدخول'">
                            <button
                                type="button"
                                class="login-mode-btn"
                                :class="{ active: loginMode === 'phone' }"
                                @click="loginMode = 'phone'"
                            >
                                {{ $t('loginWithPhonePassword') || 'هاتف وكلمة مرور' }}
                            </button>
                            <button
                                type="button"
                                class="login-mode-btn"
                                :class="{ active: loginMode === 'code' }"
                                @click="loginMode = 'code'"
                            >
                                {{ $t('loginWithAccountCode') || 'رمز الحساب' }}
                            </button>
                        </div>
                        
                        <form v-if="loginMode === 'phone'" @submit.prevent="login" class="login-form-element">
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
                            
                            <div class="form-developer-section">
                                <p class="developer-main-text">
                                    {{ $t('developedBy') }}
                                    <a :href="$t('companyWebsite')" class="developer-link-button" target="_blank">
                                        {{ $t('companyName') }}
                                    </a>
                                </p>
                            </div>
                        </form>

                        <form v-else @submit.prevent="loginByCode" class="login-form-element">
                            <div class="form-field-group">
                                <label class="form-field-label">
                                    <b-icon icon="key-fill" class="form-field-icon"></b-icon>
                                    {{ $t('accountLoginCodeLabel') || 'رمز الحساب' }}
                                </label>
                                <div class="form-input-container">
                                    <input
                                        id="inputLoginCode"
                                        v-model="form.loginCode"
                                        type="text"
                                        inputmode="numeric"
                                        autocomplete="one-time-code"
                                        maxlength="12"
                                        minlength="4"
                                        pattern="[0-9]*"
                                        :placeholder="$t('accountLoginCodePlaceholder') || 'مثال: 45443'"
                                        required
                                        autofocus
                                        class="form-input-field"
                                    />
                                </div>
                                <p class="form-hint-text">{{ $t('accountLoginCodeHint') || 'أدخل الرقم الذي عيّنه المدير للحساب التجاري (4–12 رقماً).' }}</p>
                            </div>
                            <button type="submit" class="login-submit-button">
                                <span class="button-content-wrapper">
                                    <b-icon icon="box-arrow-in-right" class="button-icon-element"></b-icon>
                                    <span class="button-text-element">{{ $t('loginButton') }}</span>
                                </span>
                            </button>
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
            loginMode: 'code',
            form: {
                phoneNumber: '',
                password: '',
                loginCode: ''
            }
        };
    },

    methods: {
        redirectAfterLogin(role) {
            if (role === 'Admin') {
                this.$router.push('/users');
            } else if (role === 'POS') {
                this.$router.push('/pos');
            } else if (role === 'TablesManager') {
                this.$router.push('/restaurant/tables');
            } else if (role === 'ReservationsManager') {
                this.$router.push('/restaurant/reservations');
            } else if (role === 'KitchenManager') {
                this.$router.push('/restaurant/kitchen');
            } else if (role === 'Waiter') {
                this.$router.push('/restaurant/waiter');
            } else {
                this.$router.push('/dashboard');
            }
        },
        loginByCode() {
            const code = (this.form.loginCode || '').trim();
            if (!/^\d{4,12}$/.test(code)) {
                this.$toast.error(this.$i18n.t('invalidAccountCode') || 'أدخل رقماً بين 4 و 12 خانة', {
                    position: 'top-right',
                    timeout: 4000
                });
                return;
            }
            this.show = true;
            HTTP.post('Auth/LoginByCode', { loginCode: code })
                .then(response => {
                    if (response.data && response.data.token) {
                        localStorage.setItem('token', response.data.token);
                        localStorage.setItem('role', response.data.role);
                        localStorage.setItem('info', JSON.stringify(response.data.info || {}));
                        this.redirectAfterLogin(response.data.role);
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
                        position: 'top-right',
                        timeout: 5000,
                        closeOnClick: true
                    });
                });
        },
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
            HTTP.post('Auth/Login', {
                phoneNumber: this.form.phoneNumber,
                password: this.form.password
            })
                .then(response => {
                    if (response.data && response.data.token) {
                        localStorage.setItem('token', response.data.token);
                        localStorage.setItem('role', response.data.role);
                        localStorage.setItem('info', JSON.stringify(response.data.info || {}));
                        this.redirectAfterLogin(response.data.role);
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

<style scoped>
.login-form-panel {
    width: 100% !important;
}

.login-mode-toggle {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1.25rem;
    padding: 0.25rem;
    background: rgba(0, 0, 0, 0.04);
    border-radius: 10px;
}
.login-mode-btn {
    flex: 1;
    border: none;
    padding: 0.55rem 0.75rem;
    border-radius: 8px;
    font-size: 0.9rem;
    cursor: pointer;
    background: transparent;
    color: #444;
    transition: background 0.15s, color 0.15s;
}
.login-mode-btn.active {
    background: #fff;
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
    color: #0d6efd;
    font-weight: 600;
}
.form-hint-text {
    font-size: 0.8rem;
    color: #6c757d;
    margin: 0.35rem 0 0;
    line-height: 1.4;
}
</style>
