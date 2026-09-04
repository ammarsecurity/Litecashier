<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content settings-page">

          <!-- Header -->
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="person-circle" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t('myProfile') || 'الحساب الشخصي' }}</h1>
                  <p class="header-subtitle">{{ $t('myProfileSubtitle') || 'تعديل بيانات حسابك ومتجرك' }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Loading -->
          <div v-if="loading" class="profile-loading">
            <b-spinner></b-spinner>
          </div>

          <template v-else>
            <!-- Basic Info -->
            <div class="app-section-card profile-info-zone">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap profile-info-zone__icon">
                    <b-icon icon="person-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t('basicInfo') || 'المعلومات الأساسية' }}</h3>
                    <p class="app-section-subtitle">{{ $t('basicInfoSubtitle') || 'الاسم ورقم الهاتف واسم المتجر' }}</p>
                  </div>
                </div>
              </div>
              <div class="app-section-body">
                <form @submit.prevent="saveProfile" class="profile-form">
                  <div class="profile-form-grid">
                    <div class="users-form-group">
                      <label class="users-form-label">
                        <b-icon icon="person-fill" class="form-label-icon"></b-icon>
                        {{ $t('full_name') || 'الاسم الكامل' }}
                      </label>
                      <input
                        v-model="form.name"
                        type="text"
                        class="users-form-input"
                        :placeholder="$t('full_name') || 'الاسم الكامل'"
                        required
                      />
                    </div>
                    <div class="users-form-group">
                      <label class="users-form-label">
                        <b-icon icon="telephone-fill" class="form-label-icon"></b-icon>
                        {{ $t('phone_number') || 'رقم الهاتف' }}
                      </label>
                      <input
                        v-model="form.phoneNumber"
                        type="tel"
                        class="users-form-input"
                        :placeholder="$t('phone_number') || 'رقم الهاتف'"
                        required
                      />
                    </div>
                    <div class="users-form-group">
                      <label class="users-form-label">
                        <b-icon icon="person" class="form-label-icon"></b-icon>
                        {{ $t('username') || 'اسم المستخدم' }}
                      </label>
                      <input
                        v-model="form.username"
                        type="text"
                        class="users-form-input"
                        :placeholder="$t('username') || 'اسم المستخدم'"
                      />
                    </div>
                    <div class="users-form-group">
                      <label class="users-form-label">
                        <b-icon icon="shop" class="form-label-icon"></b-icon>
                        {{ $t('storeName') || 'اسم المتجر' }}
                      </label>
                      <input
                        v-model="form.storeName"
                        type="text"
                        class="users-form-input"
                        :placeholder="$t('storeName') || 'اسم المتجر'"
                      />
                    </div>
                  </div>

                  <div class="settings-danger-zone__actions">
                    <button
                      type="submit"
                      class="users-add-button"
                      :disabled="saving"
                    >
                      <b-spinner small v-if="saving" class="button-icon"></b-spinner>
                      <b-icon v-else icon="check2-circle" class="button-icon"></b-icon>
                      <span class="button-text">
                        {{ saving ? ($t('saving') || 'جاري الحفظ...') : ($t('saveChanges') || 'حفظ التغييرات') }}
                      </span>
                    </button>
                  </div>
                </form>
              </div>
            </div>

            <!-- Change Password -->
            <div class="app-section-card profile-password-zone">
              <div class="app-section-header">
                <div class="app-section-title-wrap">
                  <div class="app-section-icon-wrap profile-password-zone__icon">
                    <b-icon icon="lock-fill"></b-icon>
                  </div>
                  <div>
                    <h3 class="app-section-title">{{ $t('changePassword') || 'تغيير كلمة المرور' }}</h3>
                    <p class="app-section-subtitle">{{ $t('changePasswordSubtitle') || 'اترك الحقول فارغة إذا لا تريد التغيير' }}</p>
                  </div>
                </div>
              </div>
              <div class="app-section-body">
                <form @submit.prevent="savePassword" class="profile-form">
                  <div class="profile-form-grid">
                    <div class="users-form-group">
                      <label class="users-form-label">
                        <b-icon icon="lock" class="form-label-icon"></b-icon>
                        {{ $t('newPassword') || 'كلمة المرور الجديدة' }}
                      </label>
                      <input
                        v-model="passwordForm.newPassword"
                        type="password"
                        class="users-form-input"
                        :placeholder="$t('newPassword') || 'كلمة المرور الجديدة'"
                        autocomplete="new-password"
                      />
                    </div>
                    <div class="users-form-group">
                      <label class="users-form-label">
                        <b-icon icon="lock-fill" class="form-label-icon"></b-icon>
                        {{ $t('confirmNewPassword') || 'تأكيد كلمة المرور' }}
                      </label>
                      <input
                        v-model="passwordForm.confirmPassword"
                        type="password"
                        class="users-form-input"
                        :placeholder="$t('confirmNewPassword') || 'تأكيد كلمة المرور الجديدة'"
                        autocomplete="new-password"
                      />
                    </div>
                  </div>
                  <div class="settings-danger-zone__actions">
                    <button
                      type="submit"
                      class="users-add-button"
                      :disabled="savingPassword || !passwordForm.newPassword"
                    >
                      <b-spinner small v-if="savingPassword" class="button-icon"></b-spinner>
                      <b-icon v-else icon="shield-lock-fill" class="button-icon"></b-icon>
                      <span class="button-text">
                        {{ savingPassword ? ($t('saving') || 'جاري الحفظ...') : ($t('changePassword') || 'تغيير كلمة المرور') }}
                      </span>
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </template>

        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";

export default {
  name: "ProfileView",
  components: { AppHeader },
  data() {
    return {
      loading: true,
      saving: false,
      savingPassword: false,
      form: {
        name: "",
        phoneNumber: "",
        username: "",
        storeName: "",
      },
      passwordForm: {
        newPassword: "",
        confirmPassword: "",
      },
    };
  },
  mounted() {
    this.loadProfile();
  },
  methods: {
    async loadProfile() {
      this.loading = true;
      try {
        const res = await HTTP.get("Admin/CommercialUserInfo");
        const d = res?.data?.data;
        if (d) {
          this.form.name = d.storeName || d.StoreName || "";
          this.form.storeName = d.storeName || d.StoreName || "";
        }
        // Also fetch user's own info from localStorage
        try {
          const info = JSON.parse(localStorage.getItem("info") || "{}");
          this.form.name = info.name || info.Name || this.form.name;
          this.form.phoneNumber = info.phoneNumber || info.PhoneNumber || "";
          this.form.username = info.username || info.Username || "";
        } catch (_) { /* ignore */ }
      } catch (err) {
        console.error("Error loading profile:", err);
      } finally {
        this.loading = false;
      }
    },
    async saveProfile() {
      if (this.saving) return;
      this.saving = true;
      try {
        const formData = new FormData();
        if (this.form.name) formData.append("name", this.form.name);
        if (this.form.phoneNumber) formData.append("phoneNumber", this.form.phoneNumber);
        if (this.form.username) formData.append("username", this.form.username);
        if (this.form.storeName) formData.append("storeName", this.form.storeName);

        const res = await HTTP.post("Admin/UpdateMyProfile", formData, {
          headers: { "Content-Type": "multipart/form-data" },
        });

        if (res?.data?.errorStatus) {
          throw new Error(res.data.message || "saveFailed");
        }

        // Update localStorage info
        try {
          const info = JSON.parse(localStorage.getItem("info") || "{}");
          info.name = this.form.name;
          info.phoneNumber = this.form.phoneNumber;
          info.username = this.form.username;
          localStorage.setItem("info", JSON.stringify(info));
        } catch (_) { /* ignore */ }

        this.$notify.success(this.$t("profileSaveSuccess") || "تم حفظ البيانات بنجاح", {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } catch (err) {
        const msg = err?.response?.data?.message || err?.message;
        this.$notify.error(
          msg || this.$t("saveFailed") || "حدث خطأ أثناء الحفظ",
          { position: "top-right", timeout: 4000, maxToasts: 1 }
        );
      } finally {
        this.saving = false;
      }
    },
    async savePassword() {
      if (this.savingPassword || !this.passwordForm.newPassword) return;
      if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
        this.$notify.error(
          this.$t("passwordsMismatch") || "كلمتا المرور غير متطابقتين",
          { position: "top-right", timeout: 3500, maxToasts: 1 }
        );
        return;
      }
      this.savingPassword = true;
      try {
        const formData = new FormData();
        formData.append("password", this.passwordForm.newPassword);

        const res = await HTTP.post("Admin/UpdateMyProfile", formData, {
          headers: { "Content-Type": "multipart/form-data" },
        });

        if (res?.data?.errorStatus) {
          throw new Error(res.data.message || "saveFailed");
        }

        this.passwordForm.newPassword = "";
        this.passwordForm.confirmPassword = "";
        this.$notify.success(this.$t("passwordChangeSuccess") || "تم تغيير كلمة المرور بنجاح", {
          position: "top-right",
          timeout: 3000,
          maxToasts: 1,
        });
      } catch (err) {
        const msg = err?.response?.data?.message || err?.message;
        this.$notify.error(
          msg || this.$t("saveFailed") || "حدث خطأ أثناء تغيير كلمة المرور",
          { position: "top-right", timeout: 4000, maxToasts: 1 }
        );
      } finally {
        this.savingPassword = false;
      }
    },
  },
};
</script>

<style scoped>
.profile-loading {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 200px;
}

.profile-info-zone {
  margin-bottom: 1.25rem;
}

.profile-info-zone__icon {
  background: rgba(15, 110, 110, 0.15);
  color: #0f6e6e;
}

.profile-password-zone {
  margin-bottom: 1.25rem;
}

.profile-password-zone__icon {
  background: rgba(239, 68, 68, 0.15);
  color: #ef4444;
}

.profile-form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 0.75rem;
  margin-bottom: 1.25rem;
}

.mt-3 {
  margin-top: 0.75rem;
}
</style>
