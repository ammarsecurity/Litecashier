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
                <div class="login-card register-card">
                    <div class="login-card-header">
                        <div class="login-card-icon-wrap">
                            <b-icon icon="person-plus-fill" class="login-card-icon"></b-icon>
                        </div>
                        <h1 class="login-card-title">{{ $t('registerTitle') }}</h1>
                        <p class="login-card-subtitle">{{ $t('registerSubtitle') }}</p>
                    </div>

                    <form @submit.prevent="register" class="login-form">
                        <div class="form-field-group">
                            <label class="form-field-label" for="inputName">
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
                            <label class="form-field-label" for="inputNumber">
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

                        <div class="form-field-group">
                            <label class="form-field-label" for="inputRestaurantName">
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

                        <div class="form-field-group">
                            <label class="form-field-label" for="inputLoginCodeRegister">
                                <b-icon icon="key-fill" class="form-field-icon"></b-icon>
                                {{ $t('accountLoginCodeLabel') || 'رمز دخول سريع' }}
                            </label>
                            <div class="form-input-container">
                                <input
                                    id="inputLoginCodeRegister"
                                    v-model="form.loginCode"
                                    type="text"
                                    inputmode="numeric"
                                    maxlength="12"
                                    :placeholder="$t('accountLoginCodeAdminPlaceholder') || 'اختياري: 4–12 رقماً'"
                                    class="form-input-field"
                                />
                            </div>
                            <p class="form-hint-text">{{ $t('accountLoginCodeRegisterHint') }}</p>
                        </div>

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

                        <button type="submit" class="login-submit-button">
                            <span class="button-content-wrapper">
                                <b-icon icon="person-plus-fill" class="button-icon-element"></b-icon>
                                <span class="button-text-element">{{ $t('registerButton') }}</span>
                            </span>
                        </button>

                        <div class="login-card-footer">
                            <p>{{ $t('alreadyHaveAccountMessage') }}</p>
                            <router-link to="/login" class="login-auth-text-link">
                                {{ $t('loginLink') }}
                            </router-link>
                        </div>

                        <p class="register-developer">
                            {{ $t('developedBy') }}
                            <a :href="$t('companyWebsite')" class="login-auth-text-link" target="_blank" rel="noopener">
                                {{ $t('companyName') }}
                            </a>
                        </p>
                    </form>
                </div>
            </main>
        </div>
    </b-overlay>
</template>
<script>
import { HTTP } from '../../http/api.js';
import { syncNotifyLocale } from '@/plugins/notifyPlugin';

export default {
    name: 'RegisterView',
    data() {
        return {
            show: false,
            currentTheme: 'dark',
            form: {
                phoneNumber: '',
                password: '',
                name: '',
                username: 'formWeb',
                role: 'Commercial',
                restaurantName: '',
                loginCode: '',
                logoFile: null,
                logoPreview: null
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
        register() {
            if (!this.form.name || !this.form.phoneNumber || !this.form.password) {
                this.$toast.error(this.$i18n.t('pleaseFillAllFields') || 'Please fill all fields', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            if (this.form.phoneNumber.length < 10 || this.form.phoneNumber.length > 11) {
                this.$toast.error(this.$i18n.t('invalidPhoneNumber') || 'Invalid phone number', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            if (this.form.password.length < 8) {
                this.$toast.error(this.$i18n.t('passwordTooShort') || 'Password must be at least 8 characters', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            const rawCode = (this.form.loginCode || '').trim();
            if (rawCode && !/^\d{4,12}$/.test(rawCode)) {
                this.$toast.error(this.$i18n.t('invalidAccountCode') || 'رمز الدخول: 4 إلى 12 رقماً', {
                    position: "top-right",
                    timeout: 4000,
                });
                return;
            }

            this.show = true;

            const formData = new FormData();
            formData.append('name', this.form.name);
            formData.append('phoneNumber', this.form.phoneNumber);
            formData.append('password', this.form.password);
            formData.append('username', this.form.username);
            formData.append('role', this.form.role);

            if (this.form.logoFile) {
                formData.append('logo', this.form.logoFile);
            }

            if (this.form.restaurantName) {
                formData.append('restaurantName', this.form.restaurantName);
            }

            if (rawCode) {
                formData.append('loginCode', rawCode);
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
                const validImageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
                if (!validImageTypes.includes(file.type)) {
                    this.$toast.error(this.$i18n.t('invalidImageType') || 'Invalid image type. Please upload a JPEG, PNG, or GIF image.', {
                        position: "top-right",
                        timeout: 4000,
                    });
                    return;
                }

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
    background: color-mix(in srgb, var(--primary-color) 12%, transparent);
    color: var(--primary-color);
}

.login-page-main {
    position: relative;
    z-index: 1;
    flex: 1;
    display: flex;
    align-items: flex-start;
    justify-content: center;
    padding: 32px 16px 48px;
}

.login-card {
    width: 100%;
    max-width: 440px;
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

.login-card-icon {
    font-size: 1.45rem;
    color: var(--primary-color);
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

.login-form .form-field-group {
    margin-bottom: 16px;
}

.form-hint-text {
    font-size: 13px;
    color: var(--text-muted);
    margin: 8px 0 0;
    line-height: 1.45;
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
    filter: brightness(1.05);
}

.button-content-wrapper {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
}

.logo-upload-section {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.logo-preview {
    position: relative;
    width: 88px;
    height: 88px;
    border-radius: 14px;
    overflow: hidden;
    box-shadow: var(--shadow-card);
}

.logo-preview-img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.logo-remove-btn {
    position: absolute;
    top: 6px;
    inset-inline-end: 6px;
    width: 28px;
    height: 28px;
    border: none;
    border-radius: 8px;
    background: color-mix(in srgb, var(--bg-primary) 80%, transparent);
    color: var(--danger-color);
    display: grid;
    place-items: center;
    cursor: pointer;
}

.logo-upload-btn {
    min-height: 44px;
    border: none;
    border-radius: 12px;
    background: var(--bg-tertiary);
    color: var(--text-primary);
    font-weight: 700;
    box-shadow: none;
}

.logo-upload-btn:hover {
    background: color-mix(in srgb, var(--primary-color) 12%, transparent);
    color: var(--primary-color);
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

.register-developer {
    margin: 16px 0 0;
    text-align: center;
    font-size: 13px;
    color: var(--text-muted);
}
</style>
