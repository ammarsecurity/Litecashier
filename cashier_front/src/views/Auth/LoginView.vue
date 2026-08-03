<template>
  <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <div class="login-page-wrapper login-page-wrapper--v2">
      <div class="login-background-decoration"></div>

      <div class="login-top-bar">
        <select
          v-model="$i18n.locale"
          @change="onLanguageChange"
          class="login-lang-select"
          :aria-label="$t('language') || 'اللغة'"
        >
          <option value="ar">عربي</option>
          <option value="en">English</option>
        </select>
      </div>

      <div class="login-center-stage">
        <div class="login-card">
          <div class="login-card-brand">
            <img src="../../assets/logo.png" alt="LiteCashier" class="login-card-logo" />
            <h1 class="login-card-title">{{ $t("loginTitle") }}</h1>
            <p class="login-card-subtitle">{{ $t("loginWelcomeSubtitle") }}</p>
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

          <form v-if="loginMode === 'phone'" @submit.prevent="login" class="login-form-element">
            <div class="form-field-group">
              <label class="form-field-label" for="inputNumber">
                <b-icon icon="telephone-fill" class="form-field-icon"></b-icon>
                {{ $t("phoneNumberPlaceholder") }}
              </label>
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

          <form v-else @submit.prevent="loginByCode" class="login-form-element">
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

        <div class="login-feature-chips" aria-hidden="true">
          <div class="login-feature-chip">
            <b-icon icon="cart-check-fill"></b-icon>
            <span>{{ $t("loginFeaturePos") }}</span>
          </div>
          <div class="login-feature-chip">
            <b-icon icon="graph-up"></b-icon>
            <span>{{ $t("loginFeatureReports") }}</span>
          </div>
          <div class="login-feature-chip">
            <b-icon icon="box-seam"></b-icon>
            <span>{{ $t("loginFeatureInventory") }}</span>
          </div>
        </div>
      </div>
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
