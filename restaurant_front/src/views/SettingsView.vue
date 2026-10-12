<template>
  <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
    <div>
      <AppHeader />
      <div class="main-content-wrapper">
        <div class="app-page-container">
          <div class="app-page-content settings-page">
            <div class="users-header-section">
              <div class="users-header-content app-header-row">
                <div class="header-title-wrapper">
                  <div class="header-icon-wrapper">
                    <b-icon icon="gear-fill" class="header-icon"></b-icon>
                  </div>
                  <div>
                    <h1 class="users-page-title">{{ $t("settingsTitle") || "الإعدادات" }}</h1>
                    <p class="header-subtitle">
                      {{ $t("settingsSubtitle") || "إدارة إعدادات النظام" }}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div v-if="isPrimaryAdmin" class="users-backup-zone settings-admin-zone">
              <div class="users-danger-zone-header">
                <b-icon icon="archive-fill" class="users-backup-zone-icon"></b-icon>
                <div>
                  <h2 class="users-danger-zone-title">{{ $t("systemBackupTitle") }}</h2>
                  <p class="users-danger-zone-text">{{ $t("systemBackupDescription") }}</p>
                </div>
              </div>
              <div class="users-backup-actions">
                <button
                  type="button"
                  class="users-backup-download-button"
                  :disabled="show || downloadingBackup"
                  @click="downloadSystemBackup"
                >
                  <b-spinner small v-if="downloadingBackup" class="me-2"></b-spinner>
                  <b-icon v-else icon="download" class="me-2"></b-icon>
                  {{ $t("downloadSystemBackupButton") }}
                </button>
                <button
                  type="button"
                  class="users-backup-restore-button"
                  @click="openRestoreBackupModal"
                >
                  <b-icon icon="upload" class="me-2"></b-icon>
                  {{ $t("restoreSystemBackupButton") }}
                </button>
              </div>
            </div>

            <div v-if="isPrimaryAdmin" class="users-danger-zone settings-admin-zone">
              <div class="users-danger-zone-header">
                <b-icon icon="exclamation-octagon-fill" class="users-danger-zone-icon"></b-icon>
                <div>
                  <h2 class="users-danger-zone-title">{{ $t("purgeAllSystemDataTitle") }}</h2>
                  <p class="users-danger-zone-text">{{ $t("purgeAllSystemDataDescription") }}</p>
                </div>
              </div>
              <button
                type="button"
                class="users-danger-zone-button"
                @click="openPurgeSystemDataModal"
              >
                <b-icon icon="trash-fill" class="me-2"></b-icon>
                {{ $t("purgeAllSystemDataButton") }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <b-modal id="modal-purge-system-data" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <div class="delete-confirmation-content">
            <div class="delete-icon-wrapper">
              <b-icon icon="exclamation-octagon-fill" class="delete-warning-icon"></b-icon>
            </div>
            <h3 class="delete-confirmation-title">{{ $t("purgeAllSystemDataTitle") }}</h3>
            <p class="delete-confirmation-text">{{ $t("purgeAllSystemDataWarning") }}</p>
            <div class="users-form-group">
              <label class="users-form-label">{{ $t("password") }}</label>
              <input
                v-model="purgeSystemDataPassword"
                type="password"
                class="users-form-input"
                :placeholder="$t('purgeSystemDataConfirmPassword')"
                autocomplete="current-password"
              />
            </div>
            <div class="delete-confirmation-actions">
              <button
                class="delete-confirm-button"
                :disabled="show || !purgeSystemDataPassword"
                @click="purgeAllSystemData"
              >
                <b-spinner small v-if="show" class="me-2"></b-spinner>
                <b-icon v-else icon="trash-fill" class="me-2"></b-icon>
                {{ $t("purgeAllSystemDataButton") }}
              </button>
              <button class="delete-cancel-button" @click="closePurgeSystemDataModal">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancel") }}
              </button>
            </div>
          </div>
        </div>
      </b-modal>

      <b-modal id="modal-restore-backup" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <div class="delete-confirmation-content">
            <div class="delete-icon-wrapper">
              <b-icon icon="upload" class="delete-warning-icon"></b-icon>
            </div>
            <h3 class="delete-confirmation-title">{{ $t("restoreSystemBackupButton") }}</h3>
            <p class="delete-confirmation-text">{{ $t("restoreSystemBackupWarning") }}</p>
            <div class="users-form-group">
              <label class="users-form-label">{{ $t("backupZipFile") }}</label>
              <input
                ref="backupFileInput"
                type="file"
                accept=".zip,application/zip"
                class="users-form-input"
                @change="onBackupFileSelected"
              />
              <p v-if="restoreBackupFileName" class="users-backup-file-name">{{ restoreBackupFileName }}</p>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">{{ $t("password") }}</label>
              <input
                v-model="restoreBackupPassword"
                type="password"
                class="users-form-input"
                :placeholder="$t('purgeSystemDataConfirmPassword')"
                autocomplete="current-password"
              />
            </div>
            <div class="delete-confirmation-actions">
              <button
                class="delete-confirm-button"
                :disabled="show || !restoreBackupFile || !restoreBackupPassword"
                @click="restoreSystemBackup"
              >
                <b-spinner small v-if="show" class="me-2"></b-spinner>
                <b-icon v-else icon="upload" class="me-2"></b-icon>
                {{ $t("restoreSystemBackupButton") }}
              </button>
              <button class="delete-cancel-button" @click="closeRestoreBackupModal">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancel") }}
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
import { HTTP } from "@/http/api.js";

export default {
  name: "SettingsView",
  components: { AppHeader },
  data() {
    return {
      show: false,
      purgeSystemDataPassword: "",
      downloadingBackup: false,
      restoreBackupPassword: "",
      restoreBackupFile: null,
      restoreBackupFileName: "",
    };
  },
  computed: {
    isPrimaryAdmin() {
      if (localStorage.getItem("role") !== "Admin") {
        return false;
      }
      try {
        const info = JSON.parse(localStorage.getItem("info") || "{}");
        return Number(info.id || info.Id) === 1;
      } catch {
        return false;
      }
    },
  },
  methods: {
    openPurgeSystemDataModal() {
      this.purgeSystemDataPassword = "";
      this.$bvModal.show("modal-purge-system-data");
    },
    closePurgeSystemDataModal() {
      this.purgeSystemDataPassword = "";
      this.$bvModal.hide("modal-purge-system-data");
    },
    purgeAllSystemData() {
      if (!this.purgeSystemDataPassword) {
        return;
      }
      this.show = true;
      HTTP.post("Admin/PurgeAllSystemData", { password: this.purgeSystemDataPassword })
        .then(() => {
          this.show = false;
          this.purgeSystemDataPassword = "";
          this.$notify.success(this.$t("purgeAllSystemDataSuccess"));
          this.$bvModal.hide("modal-purge-system-data");
        })
        .catch((error) => {
          this.show = false;
          const raw = error.response?.data?.message;
          const msg =
            raw && this.$te(raw) ? this.$i18n.t(raw) : raw || this.$i18n.t("somethingWrong");
          this.$notify.error(msg);
        });
    },
    downloadSystemBackup() {
      this.downloadingBackup = true;
      HTTP.get("Admin/DownloadSystemBackup", {
        responseType: "blob",
        timeout: 600000,
      })
        .then((response) => {
          const blob = new Blob([response.data], { type: "application/zip" });
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement("a");
          const stamp = new Date().toISOString().replace(/[:.]/g, "-").slice(0, 19);
          link.href = url;
          link.setAttribute("download", `litecashier-backup-${stamp}.zip`);
          document.body.appendChild(link);
          link.click();
          link.remove();
          window.URL.revokeObjectURL(url);
          this.$notify.success(this.$t("backupDownloadSuccess"));
        })
        .catch(async (error) => {
          let msg = this.$t("backupDownloadFailed");
          const data = error.response?.data;
          if (data instanceof Blob) {
            try {
              const text = await data.text();
              const parsed = JSON.parse(text);
              if (parsed.message && this.$te(parsed.message)) {
                msg = this.$i18n.t(parsed.message);
              } else if (parsed.message) {
                msg = parsed.message;
              }
            } catch {
              // keep default message
            }
          } else {
            const raw = data?.message;
            msg = raw && this.$te(raw) ? this.$i18n.t(raw) : raw || msg;
          }
          this.$notify.error(msg);
        })
        .finally(() => {
          this.downloadingBackup = false;
        });
    },
    openRestoreBackupModal() {
      this.restoreBackupPassword = "";
      this.restoreBackupFile = null;
      this.restoreBackupFileName = "";
      if (this.$refs.backupFileInput) {
        this.$refs.backupFileInput.value = "";
      }
      this.$bvModal.show("modal-restore-backup");
    },
    closeRestoreBackupModal() {
      this.restoreBackupPassword = "";
      this.restoreBackupFile = null;
      this.restoreBackupFileName = "";
      this.$bvModal.hide("modal-restore-backup");
    },
    onBackupFileSelected(event) {
      const file = event.target.files && event.target.files[0];
      this.restoreBackupFile = file || null;
      this.restoreBackupFileName = file ? file.name : "";
    },
    restoreSystemBackup() {
      if (!this.restoreBackupFile || !this.restoreBackupPassword) {
        return;
      }
      const formData = new FormData();
      formData.append("file", this.restoreBackupFile);
      formData.append("password", this.restoreBackupPassword);
      this.show = true;
      HTTP.post("Admin/RestoreSystemBackup", formData, {
        headers: { "Content-Type": "multipart/form-data" },
        timeout: 600000,
      })
        .then(() => {
          this.show = false;
          this.closeRestoreBackupModal();
          this.$notify.success(this.$t("backupRestoreSuccess"));
        })
        .catch((error) => {
          this.show = false;
          const raw = error.response?.data?.message;
          const msg =
            raw && this.$te(raw)
              ? this.$i18n.t(raw)
              : raw || this.$i18n.t("backupRestoreFailed");
          this.$notify.error(msg);
        });
    },
  },
};
</script>

<style scoped>
.settings-admin-zone {
  margin-top: 0;
  margin-bottom: 1.25rem;
}
</style>
