<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <div class="login-page">
            <div class="login-page-bg" aria-hidden="true"></div>

            <header class="login-auth-topbar">
                <div class="login-auth-topbar-inner">
                    <div class="login-auth-brand">
                        <img src="../../assets/logo.png" alt="Litecashier" class="login-auth-logo" />
                    </div>
                    <div class="login-auth-actions">
                        <button
                            type="button"
                            class="login-auth-icon-btn"
                            @click="toggleTheme"
                            :title="currentTheme === 'dark' ? ($t('switchToLightMode') || 'الوضع الفاتح') : ($t('switchToDarkMode') || 'الوضع الداكن')"
                        >
                            <b-icon :icon="currentTheme === 'dark' ? 'sun-fill' : 'moon-fill'"></b-icon>
                        </button>
                        <button
                            type="button"
                            class="login-auth-lang-btn"
                            @click="toggleLanguage"
                            :title="$t('changeLanguage') || 'تغيير اللغة'"
                        >
                            <b-icon icon="translate"></b-icon>
                            <span>{{ $i18n.locale === 'ar' ? 'EN' : 'ع' }}</span>
                        </button>
                    </div>
                </div>
            </header>

            <main class="login-page-main">
                <div class="login-card">
                    <div class="login-card-header">
                        <div class="login-card-icon-wrap">
                            <b-icon icon="shield-lock-fill" class="login-card-icon"></b-icon>
                        </div>
                        <h1 class="login-card-title">{{ $t('loginTitle') }}</h1>
                        <p class="login-card-subtitle">{{ $t('loginWelcomeSubtitle') }}</p>
                    </div>

                    <div class="login-mode-toggle" role="group" :aria-label="$t('loginModeToggleAria') || 'طريقة الدخول'">
                        <button
                            type="button"
                            class="login-mode-btn"
                            :class="{ active: loginMode === 'phone' }"
                            @click="loginMode = 'phone'"
                        >
                            <b-icon icon="telephone-fill" class="login-mode-btn-icon"></b-icon>
                            {{ $t('loginWithPhonePassword') || 'هاتف وكلمة مرور' }}
                        </button>
                        <button
                            type="button"
                            class="login-mode-btn"
                            :class="{ active: loginMode === 'code' }"
                            @click="loginMode = 'code'"
                        >
                            <b-icon icon="key-fill" class="login-mode-btn-icon"></b-icon>
                            {{ $t('loginWithAccountCode') || 'رمز الحساب' }}
                        </button>
                    </div>

                    <form @submit.prevent="onSubmit" class="login-form">
                        <template v-if="loginMode === 'phone'">
                            <div class="form-field-group">
                                <label class="form-field-label" for="inputNumber">
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
                                <label class="form-field-label" for="inputPassword">
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
                        </template>

                        <template v-else>
                            <div class="form-field-group">
                                <label class="form-field-label" for="inputLoginCode">
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
                                        class="form-input-field login-code-input"
                                    />
                                </div>
                                <p class="login-form-hint">{{ $t('accountLoginCodeHint') || 'أدخل الرقم الذي عيّنه المدير للحساب التجاري (4–12 رقماً).' }}</p>
                            </div>
                        </template>

                        <button type="submit" class="login-submit-button">
                            <span class="button-content-wrapper">
                                <b-icon icon="box-arrow-in-right" class="button-icon-element"></b-icon>
                                <span class="button-text-element">{{ $t('loginButton') }}</span>
                            </span>
                        </button>

                        <div class="login-card-footer">
                            <p>{{ $t('noAccountMessage') }}</p>
                            <router-link to="/register" class="login-auth-text-link">
                                {{ $t('registerLink') }}
                            </router-link>
                        </div>
                    </form>
                </div>
            </main>
        </div>
    </b-overlay>
</template>

<script>
import { HTTP } from '../../http/api.js';
import { setAllowedSections } from '@/navigation/sectionRegistry.js';
import { syncNotifyLocale } from '@/plugins/notifyPlugin';

export default {
    name: 'LoginView',
    data() {
        return {
            show: false,
            currentTheme: 'dark',
            loginMode: 'code',
            form: {
                phoneNumber: '',
                password: '',
                loginCode: ''
            }
        };
    },
    mounted() {
        const savedTheme = localStorage.getItem('theme') || 'dark';
        this.currentTheme = savedTheme;
        this.applyTheme(savedTheme);
    },
    methods: {
        toggleTheme() {
            this.currentTheme = this.currentTheme === 'dark' ? 'light' : 'dark';
            this.applyTheme(this.currentTheme);
            localStorage.setItem('theme', this.currentTheme);
        },
        applyTheme(theme) {
            const root = document.documentElement;
            root.classList.remove('light-theme', 'dark-theme');
            root.classList.add(`${theme}-theme`);
        },
        toggleLanguage() {
            const currentLang = this.$i18n.locale || localStorage.getItem('language') || 'ar';
            const nextLang = currentLang === 'ar' ? 'en' : 'ar';
            localStorage.setItem('language', nextLang);
            this.$i18n.locale = nextLang;
            document.body.dir = nextLang === 'en' ? 'ltr' : 'rtl';
            syncNotifyLocale(nextLang);
        },
        onSubmit() {
            if (this.loginMode === 'phone') {
                this.login();
            } else {
                this.loginByCode();
            }
        },
        resolveLoginApiMessage(raw) {
            const s = raw != null ? String(raw).trim() : '';
            if (!s) return this.$i18n.t('errorInLoginInfo');
            if (this.$te(s)) return this.$i18n.t(s);
            if (/^error in login info$/i.test(s)) return this.$i18n.t('errorInLoginInfo');
            return s;
        },
        persistSession(responseData) {
            localStorage.setItem('token', responseData.token);
            localStorage.setItem('role', responseData.role);
            localStorage.setItem('info', JSON.stringify(responseData.info || {}));
            const sections = responseData.allowedSections || [];
            setAllowedSections(sections);
        },
        redirectAfterLogin(role) {
            if (role === 'Admin') {
                this.$router.push('/users');
            } else if (role === 'POS') {
                this.$router.push('/pos');
            } else if (role === 'Manager') {
                this.$router.push('/sections');
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
                        this.persistSession(response.data);
                        this.redirectAfterLogin(response.data.role);
                    } else {
                        throw new Error('Invalid response from server');
                    }
                    this.show = false;
                })
                .catch(error => {
                    this.show = false;
                    const errorMessage = this.resolveLoginApiMessage(
                        error.response?.data?.message || error.response?.data?.error
                    );
                    this.$toast.error(errorMessage, {
                        position: 'top-right',
                        timeout: 5000,
                        closeOnClick: true
                    });
                });
        },
        login() {
            if (!this.form.phoneNumber || !this.form.password) {
                this.$toast.error(this.$i18n.t('pleaseFillAllFields') || 'Please fill all fields', {
                    position: 'top-right',
                    timeout: 4000
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
                        this.persistSession(response.data);
                        this.redirectAfterLogin(response.data.role);
                    } else {
                        throw new Error('Invalid response from server');
                    }
                    this.show = false;
                })
                .catch(error => {
                    this.show = false;
                    const errorMessage = this.resolveLoginApiMessage(
                        error.response?.data?.message || error.response?.data?.error
                    );

                    this.$toast.error(errorMessage, {
                        position: 'top-right',
                        timeout: 5000,
                        closeOnClick: true,
                        pauseOnFocusLoss: true,
                        pauseOnHover: true,
                        draggable: true,
                        draggablePercent: 0.6,
                        showCloseButtonOnHover: false,
                        hideProgressBar: true,
                        closeButton: 'button',
                        icon: true
                    });
                });
        }
    }
};
</script>

<style scoped>
.login-page {
    min-height: 100vh;
    position: relative;
    display: flex;
    flex-direction: column;
    background: var(--bg-secondary);
    color: var(--text-primary);
}

.login-page-bg {
    position: fixed;
    inset: 0;
    pointer-events: none;
    z-index: 0;
    background:
        radial-gradient(ellipse 80% 50% at 50% -10%, color-mix(in srgb, var(--primary-color) 22%, transparent), transparent 55%),
        radial-gradient(circle at 85% 75%, color-mix(in srgb, var(--primary-color) 10%, transparent), transparent 40%),
        radial-gradient(circle at 10% 60%, color-mix(in srgb, var(--accent-color) 8%, transparent), transparent 35%);
}

.login-auth-topbar {
    position: sticky;
    top: 0;
    z-index: 2;
    background: color-mix(in srgb, var(--bg-primary) 78%, transparent);
    border-bottom: 1px solid var(--border-light);
    backdrop-filter: blur(16px);
    box-shadow: none;
}

.login-auth-icon-btn,
.login-auth-lang-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.35rem;
    height: 40px;
    min-width: 40px;
    padding: 0 0.65rem;
    border-radius: 12px;
    border: none;
    background: var(--bg-tertiary);
    color: var(--text-secondary);
    font-size: 13px;
    font-weight: 700;
    cursor: pointer;
}

.login-auth-icon-btn:hover,
.login-auth-lang-btn:hover {
    border-color: transparent;
    background: color-mix(in srgb, var(--primary-color) 12%, transparent);
    color: var(--primary-color);
    transform: none;
}

.login-page-main {
    position: relative;
    z-index: 1;
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 32px 16px 48px;
}

.login-card {
    width: 100%;
    max-width: 420px;
    padding: 32px 24px 24px;
    background: var(--bg-primary);
    border: none;
    border-radius: 16px;
    box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
}

.login-card-header {
    text-align: center;
    margin-bottom: 24px;
}

.login-card-icon-wrap {
    width: 52px;
    height: 52px;
    margin: 0 auto 16px;
    border-radius: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: color-mix(in srgb, var(--primary-color) 12%, transparent);
    border: none;
}

.login-card-title {
    font-size: 28px;
    font-weight: 800;
    color: var(--text-primary);
    margin: 0 0 8px;
    letter-spacing: -0.03em;
    line-height: 1.2;
}

.login-card-subtitle {
    margin: 0;
    font-size: 15px;
    font-weight: 500;
    color: var(--text-secondary);
    line-height: 1.5;
}

.login-mode-toggle {
    display: flex;
    gap: 4px;
    margin-bottom: 24px;
    padding: 4px;
    background: var(--bg-tertiary);
    border: none;
    border-radius: 14px;
}

.login-mode-btn.active {
    background: var(--bg-primary);
    color: var(--primary-color);
    box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
}

.login-submit-button {
    margin-top: 8px;
    padding: 0;
    min-height: 48px;
    width: 100%;
    border: none;
    border-radius: 12px;
    font-size: 16px;
    font-weight: 700;
    color: #fff;
    cursor: pointer;
    background: var(--primary-color);
    box-shadow: none;
}

.login-submit-button:hover {
    transform: none;
    filter: brightness(1.05);
    box-shadow: none;
}

.button-content-wrapper {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
}

.login-auth-topbar-inner {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    min-height: 64px;
    padding: 0.75rem 1.25rem;
    max-width: 520px;
    margin: 0 auto;
    width: 100%;
}

.login-auth-logo {
    height: 36px;
    width: auto;
    display: block;
}

.login-auth-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.login-card-icon {
    font-size: 1.45rem;
    color: var(--primary-color);
}

.login-mode-btn {
    flex: 1;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.35rem;
    border: none;
    padding: 0.6rem 0.5rem;
    border-radius: 12px;
    font-size: 13px;
    font-weight: 700;
    cursor: pointer;
    background: transparent;
    color: var(--text-muted);
}

.login-mode-btn-icon {
    font-size: 0.95rem;
    flex-shrink: 0;
}

.login-form {
    margin-top: 0.25rem;
}

.login-form .form-field-group {
    margin-bottom: 16px;
}

.login-form-hint {
    font-size: 13px;
    color: var(--text-muted);
    margin: 8px 0 0;
    line-height: 1.45;
}

.login-code-input {
    letter-spacing: 0.08em;
    font-weight: 700;
    text-align: center;
    font-size: 1.05rem;
}

.login-card-footer {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    margin-top: 20px;
    font-size: 14px;
    color: var(--text-secondary);
}

.login-card-footer p {
    margin: 0;
}

.login-auth-text-link {
    color: var(--primary-color);
    font-weight: 700;
    text-decoration: none;
}

.login-auth-text-link:hover {
    text-decoration: underline;
}

@media (max-width: 480px) {
    .login-card {
        padding: 1.35rem 1rem 1rem;
    }

    .login-mode-btn {
        font-size: 0.75rem;
        padding-inline: 0.35rem;
    }

    .login-mode-btn-icon {
        display: none;
    }

    .login-card-title {
        font-size: 1.3rem;
    }
}
</style>
