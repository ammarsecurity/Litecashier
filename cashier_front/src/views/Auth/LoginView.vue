<template>
  <b-overlay :show="show" spinner-variant="primary" spinner-type="border" rounded="sm">
    <div class="login-shell">
      <aside class="login-brand">
        <img src="../../assets/logo.png" alt="" class="login-brand-logo" />
        <p class="login-brand-kicker">{{ $t("app-name") || "نظام الكاشير" }}</p>
        <h1 class="login-brand-title">{{ $t("welcomeMessage") }}</h1>
        <p class="login-brand-text">{{ $t("loginSubtitle") }}</p>
        <ul class="login-brand-list">
          <li>
            <span class="login-brand-icon"><b-icon icon="cart-check-fill"></b-icon></span>
            <span>{{ $t("loginFeaturePos") }}</span>
          </li>
          <li>
            <span class="login-brand-icon"><b-icon icon="graph-up"></b-icon></span>
            <span>{{ $t("loginFeatureReports") }}</span>
          </li>
          <li>
            <span class="login-brand-icon"><b-icon icon="box-seam"></b-icon></span>
            <span>{{ $t("loginFeatureInventory") }}</span>
          </li>
        </ul>
      </aside>

      <main class="login-main">
        <div class="login-toolbar">
          <label class="login-lang">
            <b-icon icon="translate"></b-icon>
            <select
              v-model="$i18n.locale"
              :aria-label="$t('language') || 'اللغة'"
              @change="onLanguageChange"
            >
              <option value="ar">عربي</option>
              <option value="en">English</option>
            </select>
          </label>
        </div>

        <div class="login-panel">
          <div class="login-panel-head">
            <img src="../../assets/logo.png" alt="LiteCashier" class="login-panel-logo" />
            <div>
              <h2 class="login-panel-title">{{ $t("loginTitle") }}</h2>
              <p class="login-panel-subtitle">{{ $t("loginWelcomeSubtitle") }}</p>
            </div>
          </div>

          <div
            class="login-mode-toggle"
            role="group"
            :aria-label="$t('loginModeToggleAria') || 'طريقة الدخول'"
          >
            <button
              type="button"
              class="login-mode-btn"
              :class="{ active: loginMode === 'code' }"
              @click="loginMode = 'code'"
            >
              <b-icon icon="key-fill" class="login-mode-btn-icon"></b-icon>
              <span>{{ $t("loginWithAccountCode") || "رمز الحساب" }}</span>
            </button>
            <button
              type="button"
              class="login-mode-btn"
              :class="{ active: loginMode === 'phone' }"
              @click="loginMode = 'phone'"
            >
              <b-icon icon="telephone-fill" class="login-mode-btn-icon"></b-icon>
              <span>{{ $t("loginWithPhonePassword") || "هاتف وكلمة مرور" }}</span>
            </button>
          </div>

          <form v-if="loginMode === 'phone'" class="login-form-element" @submit.prevent="login">
            <div class="form-field-group">
              <label class="form-field-label" for="inputNumber">
                <b-icon icon="telephone-fill" class="form-field-icon"></b-icon>
                {{ $t("phoneNumberPlaceholder") }}
              </label>
              <input
                id="inputNumber"
                v-model="form.phoneNumber"
                type="tel"
                dir="ltr"
                :placeholder="$t('phoneNumberPlaceholder')"
                required
                autofocus
                class="form-input-field"
              />
            </div>

            <div class="form-field-group">
              <label class="form-field-label" for="inputPassword">
                <b-icon icon="lock-fill" class="form-field-icon"></b-icon>
                {{ $t("passwordPlaceholder") }}
              </label>
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

            <button type="submit" class="login-submit-button">
              <span class="button-content-wrapper">
                <b-icon icon="box-arrow-in-right" class="button-icon-element"></b-icon>
                <span class="button-text-element">{{ $t("loginButton") }}</span>
              </span>
            </button>
          </form>

          <form v-else class="login-form-element" @submit.prevent="loginByCode">
            <div class="form-field-group">
              <label class="form-field-label" for="inputLoginCode">
                <b-icon icon="key-fill" class="form-field-icon"></b-icon>
                {{ $t("accountLoginCodeLabel") || "رمز الحساب" }}
              </label>
              <input
                id="inputLoginCode"
                v-model="form.loginCode"
                type="text"
                inputmode="numeric"
                autocomplete="one-time-code"
                maxlength="12"
                minlength="4"
                pattern="[0-9]*"
                dir="ltr"
                :placeholder="$t('accountLoginCodePlaceholder') || 'مثال: 45443'"
                required
                autofocus
                class="form-input-field login-code-input"
              />
              <p class="form-hint-text">{{ $t("accountLoginCodeHint") }}</p>
            </div>

            <button type="submit" class="login-submit-button">
              <span class="button-content-wrapper">
                <b-icon icon="box-arrow-in-right" class="button-icon-element"></b-icon>
                <span class="button-text-element">{{ $t("loginButton") }}</span>
              </span>
            </button>
          </form>
        </div>
      </main>
    </div>
  </b-overlay>
</template>

<script>
import { HTTP } from "../../http/api.js";
import { setAllowedSections } from "@/navigation/sectionRegistry.js";
import { getDefaultPathForRole } from "@/router/index.js";
import { syncNotifyLocale } from "@/plugins/notifyPlugin.js";

export default {
  name: "LoginView",
  data() {
    return {
      show: false,
      loginMode: "code",
      form: {
        phoneNumber: "",
        password: "",
        loginCode: "",
      },
    };
  },
  mounted() {
    document.documentElement.classList.add("login-page");
    document.body.classList.add("login-page");
  },
  beforeDestroy() {
    document.documentElement.classList.remove("login-page");
    document.body.classList.remove("login-page");
  },
  methods: {
    onLanguageChange(event) {
      const lang = event.target.value;
      localStorage.setItem("language", lang);
      this.$i18n.locale = lang;
      document.body.dir = lang === "en" ? "ltr" : "rtl";
      syncNotifyLocale(lang);
    },
    resolveLoginApiMessage(raw) {
      const s = raw != null ? String(raw).trim() : "";
      if (!s) return this.$i18n.t("errorInLoginInfo");
      if (this.$te(s)) return this.$i18n.t(s);
      if (/^error in login info$/i.test(s)) return this.$i18n.t("errorInLoginInfo");
      return s;
    },
    persistSession(responseData) {
      localStorage.setItem("token", responseData.token);
      localStorage.setItem("role", responseData.role);
      localStorage.setItem("info", JSON.stringify(responseData.info || {}));
      setAllowedSections(responseData.allowedSections || []);
    },
    redirectAfterLogin(role) {
      this.$router.push(getDefaultPathForRole(role));
    },
    loginByCode() {
      const code = (this.form.loginCode || "").trim();
      if (!/^.{4,12}$/.test(code)) {
        this.$notify.error(this.$i18n.t("invalidAccountCode") || "أدخل رقماً بين 4 و 12 خانة");
        return;
      }
      this.show = true;
      HTTP.post("Auth/LoginByCode", { loginCode: code })
        .then((response) => {
          if (response.data && response.data.token) {
            this.persistSession(response.data);
            this.redirectAfterLogin(response.data.role);
          } else {
            throw new Error("Invalid response from server");
          }
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(
            this.resolveLoginApiMessage(
              error.response?.data?.message || error.response?.data?.error
            )
          );
        });
    },
    login() {
      if (!this.form.phoneNumber || !this.form.password) {
        this.$notify.error(this.$t("pleaseFillAllFields") || "يرجى ملء جميع الحقول");
        return;
      }
      this.show = true;
      HTTP.post("Auth/Login", {
        phoneNumber: this.form.phoneNumber,
        password: this.form.password,
      })
        .then((response) => {
          if (response.data && response.data.token) {
            this.persistSession(response.data);
            this.redirectAfterLogin(response.data.role);
          } else {
            throw new Error("Invalid response from server");
          }
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(
            this.resolveLoginApiMessage(
              error.response?.data?.message || error.response?.data?.error
            )
          );
        });
    },
  },
};
</script>

<style>
html.login-page,
html.login-page.dark-theme,
html.login-page.light-theme,
body.login-page {
  background: var(--bg-secondary) !important;
  min-height: 100%;
  height: 100%;
}
body.login-page #app {
  background: var(--bg-secondary);
  min-height: 100%;
  min-height: 100dvh;
}
</style>

<style scoped>
.login-shell {
  min-height: 100dvh;
  display: grid;
  grid-template-columns: minmax(280px, 1.05fr) minmax(320px, 0.95fr);
  font-family: Cairo, "IBM Plex Sans Arabic", system-ui, sans-serif;
}

.login-brand {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 48px 40px;
  background: var(--primary-gradient);
  color: #fff;
}

.login-brand-logo {
  width: 72px;
  height: auto;
  margin-bottom: 24px;
  filter: drop-shadow(0 8px 16px rgba(0, 0, 0, 0.2));
}

.login-brand-kicker {
  margin: 0 0 8px;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.04em;
  color: rgba(255, 255, 255, 0.72);
}

.login-brand-title {
  margin: 0 0 12px;
  font-size: 28px;
  font-weight: 800;
  line-height: 1.35;
  color: #fff !important;
  -webkit-text-fill-color: #fff !important;
  background: none !important;
}

.login-brand-text {
  margin: 0 0 32px;
  max-width: 420px;
  font-size: 15px;
  line-height: 1.7;
  color: rgba(255, 255, 255, 0.86);
}

.login-brand-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.login-brand-list li {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.12);
  font-size: 15px;
  font-weight: 700;
}

.login-brand-icon {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  background: rgba(61, 180, 208, 0.22);
  color: #7ad4e8;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
}

.login-main {
  display: flex;
  flex-direction: column;
  background: var(--bg-secondary);
  padding: 24px 32px 40px;
}

.login-toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 24px;
}

.login-lang {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  height: 44px;
  padding: 0 12px;
  border-radius: 12px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  box-shadow: var(--shadow-xs);
}

.login-lang select {
  border: 0;
  background: transparent;
  color: inherit;
  font-weight: 700;
  font-size: 14px;
  min-height: 44px;
}

.login-lang select:focus {
  outline: 2px solid color-mix(in srgb, var(--primary-color) 45%, transparent);
  outline-offset: 2px;
}

.login-panel {
  width: 100%;
  max-width: 440px;
  margin: auto;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 16px;
  box-shadow: var(--shadow-sm);
  padding: 32px 24px;
}

.login-panel-head {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 24px;
}

.login-panel-logo {
  width: 56px;
  height: auto;
  flex: 0 0 auto;
}

.login-panel-title {
  margin: 0 0 4px;
  font-size: 22px;
  font-weight: 800;
  color: var(--text-primary) !important;
  -webkit-text-fill-color: var(--text-primary) !important;
  background: none !important;
}

.login-panel-subtitle {
  margin: 0;
  font-size: 14px;
  line-height: 1.5;
  color: var(--text-secondary);
}

.login-form-element {
  margin-top: 0;
}

.login-panel .form-field-group {
  margin-bottom: 16px;
}

.login-panel .form-input-field {
  min-height: 48px;
  background: var(--bg-secondary);
}

.login-panel .form-input-field:focus {
  transform: none;
}

.login-panel .login-submit-button {
  margin-top: 8px;
  min-height: 48px;
}

.login-panel .login-submit-button:hover {
  transform: none;
}

.login-panel .login-mode-btn:focus,
.login-panel .login-submit-button:focus,
.login-panel .form-input-field:focus {
  outline: 2px solid color-mix(in srgb, var(--primary-color) 45%, transparent);
  outline-offset: 2px;
}

@media (max-width: 900px) {
  .login-shell {
    grid-template-columns: 1fr;
  }

  .login-brand {
    padding: 32px 20px 24px;
    padding-top: max(32px, env(safe-area-inset-top));
  }

  .login-brand-title {
    font-size: 22px;
  }

  .login-brand-text {
    margin-bottom: 16px;
  }

  .login-brand-list {
    flex-direction: row;
    flex-wrap: wrap;
  }

  .login-brand-list li {
    flex: 1 1 140px;
    font-size: 13px;
  }

  .login-main {
    padding: 16px 16px max(24px, env(safe-area-inset-bottom));
  }

  .login-toolbar {
    margin-bottom: 16px;
  }

  .login-panel {
    margin: 0 auto;
    padding: 24px 16px;
  }

  .login-panel-head {
    flex-direction: column;
    text-align: center;
  }
}

@media (max-width: 600px) {
  .login-brand-list {
    display: none;
  }

  .login-brand-logo {
    width: 56px;
    margin-bottom: 16px;
  }
}
</style>
