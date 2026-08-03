<template>

  <div>

    <AppHeader />

    <div class="main-content-wrapper">

      <div class="app-page-container">

        <div class="app-page-content database-sync-page">

          <div class="users-header-section">

            <div class="users-header-content app-header-row">

              <div class="header-title-wrapper">

                <div class="header-icon-wrapper">

                  <b-icon icon="cloud-upload-fill" class="header-icon"></b-icon>

                </div>

                <div>

                  <h1 class="users-page-title">{{ $t("databaseSyncTitle") || "نسخ احتياطي سحابي" }}</h1>

                  <p class="header-subtitle">{{ $t("databaseSyncDescription") || "رفع ملف ZIP احتياطي إلى FTP" }}</p>

                </div>

              </div>

              <div class="app-header-actions">

                <button type="button" class="btn-refresh" @click="loadAll" :disabled="loading">

                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>

                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>

                </button>

                <button type="button" class="users-add-button" @click="testConnection" :disabled="testingConnection">

                  <b-icon icon="plug-fill" class="button-icon"></b-icon>

                  <span class="button-text">{{ $t("testConnection") || "اختبار الاتصال" }}</span>

                </button>

                <button type="button" class="users-add-button sync-now-btn" @click="pushSync" :disabled="pushing || status.isSyncInProgress">

                  <b-spinner small v-if="pushing || status.isSyncInProgress" class="me-2"></b-spinner>

                  <b-icon v-else icon="cloud-upload-fill" class="button-icon"></b-icon>

                  <span class="button-text">{{ $t("uploadBackupNow") || "رفع نسخة احتياطية الآن" }}</span>

                </button>

              </div>

            </div>

          </div>



          <div class="app-overview-grid">

            <div class="app-overview-stat">

              <span class="app-overview-stat-icon" :class="status.ftpConnected ? 'app-overview-stat-icon--success' : 'app-overview-stat-icon--warning'">

                <b-icon icon="hdd-network-fill"></b-icon>

              </span>

              <div>

                <div class="app-overview-stat-value">{{ status.ftpConnected ? ($t("connected") || "متصل") : ($t("disconnected") || "غير متصل") }}</div>

                <div class="app-overview-stat-label">{{ $t("ftpBackupStorage") || "تخزين FTP" }}</div>

              </div>

            </div>

            <div class="app-overview-stat">

              <span class="app-overview-stat-icon app-overview-stat-icon--info">

                <b-icon icon="clock-history"></b-icon>

              </span>

              <div>

                <div class="app-overview-stat-value">{{ lastSyncLabel }}</div>

                <div class="app-overview-stat-label">{{ $t("lastSuccessfulSync") || "آخر رفع ناجح" }}</div>

              </div>

            </div>

            <div class="app-overview-stat">

              <span class="app-overview-stat-icon app-overview-stat-icon--primary">

                <b-icon icon="file-earmark-zip-fill"></b-icon>

              </span>

              <div>

                <div class="app-overview-stat-value">{{ formatArchiveSize(status.lastArchiveSizeBytes) }}</div>

                <div class="app-overview-stat-label">{{ $t("lastArchiveSize") || "حجم آخر نسخة" }}</div>

              </div>

            </div>

            <div class="app-overview-stat">

              <span class="app-overview-stat-icon app-overview-stat-icon--success">

                <b-icon icon="file-earmark-text-fill"></b-icon>

              </span>

              <div>

                <div class="app-overview-stat-value sync-archive-name">{{ status.lastArchiveFileName || "—" }}</div>

                <div class="app-overview-stat-label">{{ $t("lastArchiveFileName") || "اسم آخر ملف" }}</div>

              </div>

            </div>

          </div>



          <div class="app-section-card sync-settings-card">

            <div class="app-section-header">

              <div class="app-section-title-wrap">

                <div class="app-section-icon-wrap">

                  <b-icon icon="gear-fill"></b-icon>

                </div>

                <div>

                  <h3 class="app-section-title">{{ $t("autoSyncSettings") || "الرفع التلقائي" }}</h3>

                  <p class="app-section-subtitle">{{ $t("autoSyncSettingsHint") || "رفع ZIP احتياطي إلى FTP بشكل دوري" }}</p>

                </div>

              </div>

            </div>

            <div class="app-section-body">

              <div class="sync-settings-row">

                <label class="sync-toggle-label">

                  <input type="checkbox" v-model="settings.autoSyncEnabled" @change="saveSettings" />

                  <span>{{ $t("enableAutoSync") || "تفعيل الرفع التلقائي" }}</span>

                </label>

                <div class="sync-interval-wrap">

                  <label>{{ $t("syncInterval") || "الفترة" }}</label>

                  <select v-model.number="settings.intervalMinutes" @change="saveSettings" class="sync-interval-select">

                    <option :value="5">5 {{ $t("minutes") || "دقائق" }}</option>

                    <option :value="10">10 {{ $t("minutes") || "دقائق" }}</option>

                    <option :value="15">15 {{ $t("minutes") || "دقائق" }}</option>

                    <option :value="30">30 {{ $t("minutes") || "دقائق" }}</option>

                  </select>

                </div>

              </div>

              <p v-if="status.lastSyncError" class="sync-error-text">{{ status.lastSyncError }}</p>

            </div>

          </div>



          <div class="app-section-card">

            <div class="app-section-header app-section-header--toolbar">

              <div class="app-section-title-wrap">

                <div class="app-section-icon-wrap">

                  <b-icon icon="journal-text"></b-icon>

                </div>

                <div>

                  <h3 class="app-section-title">{{ $t("syncHistory") || "سجل الرفع" }}</h3>

                  <p class="app-section-subtitle">{{ $t("syncHistoryHint") || "آخر عمليات رفع النسخ الاحتياطية" }}</p>

                </div>

              </div>

              <button
                type="button"
                class="sync-clear-history-btn"
                @click="confirmClearHistory"
                :disabled="clearingHistory || loading || !history.length || status.isSyncInProgress"
              >
                <b-spinner small v-if="clearingHistory" class="me-1"></b-spinner>
                <b-icon v-else icon="trash-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("clearSyncHistory") || "حذف السجل" }}</span>
              </button>

            </div>

            <div class="app-section-body app-section-body--no-padding">

              <div class="sync-history-table-wrap">

                <table class="sync-history-table">

                  <thead>

                    <tr>

                      <th>{{ $t("date") || "التاريخ" }}</th>

                      <th>{{ $t("status") || "الحالة" }}</th>

                      <th>{{ $t("trigger") || "النوع" }}</th>

                      <th>{{ $t("archiveFileName") || "اسم الملف" }}</th>

                      <th>{{ $t("archiveSize") || "الحجم" }}</th>

                      <th>{{ $t("message") || "رسالة" }}</th>

                    </tr>

                  </thead>

                  <tbody>

                    <tr v-if="loading && !history.length">

                      <td colspan="6" class="text-center py-4">

                        <b-spinner small></b-spinner>

                      </td>

                    </tr>

                    <tr v-else-if="!history.length">

                      <td colspan="6" class="text-center py-4 text-muted">{{ $t("noSyncHistory") || "لا يوجد سجل بعد" }}</td>

                    </tr>

                    <tr v-for="row in history" :key="row.id">

                      <td>{{ formatDate(row.startedAt) }}</td>

                      <td>

                        <span class="sync-status-badge" :class="'sync-status-badge--' + (row.status || '').toLowerCase()">

                          {{ row.status }}

                        </span>

                      </td>

                      <td>{{ row.trigger }}</td>

                      <td class="sync-history-message">{{ row.archiveFileName || "—" }}</td>

                      <td>{{ formatArchiveSize(row.archiveSizeBytes) }}</td>

                      <td class="sync-history-message">{{ row.errorMessage || "—" }}</td>

                    </tr>

                  </tbody>

                </table>

              </div>

            </div>

          </div>

        </div>

      </div>

    </div>

  </div>

</template>



<script>

import AppHeader from "@/components/Layout/AppHeader.vue";

import { HTTP } from "@/http/api.js";



const BACKUP_UPLOAD_TIMEOUT_MS = 600000;



export default {

  name: "DatabaseSyncView",

  components: { AppHeader },

  data() {

    return {

      loading: false,

      pushing: false,

      testingConnection: false,

      savingSettings: false,

      clearingHistory: false,

      status: {

        syncEnabled: false,

        ftpConnected: false,

        isSyncInProgress: false,

        autoSyncEnabled: false,

        intervalMinutes: 10,

        lastSuccessfulSyncAt: null,

        lastSyncStatus: null,

        lastSyncError: null,

        lastArchiveFileName: null,

        lastArchiveSizeBytes: 0,

      },

      settings: {

        autoSyncEnabled: false,

        intervalMinutes: 10,

      },

      history: [],

      pollTimer: null,

    };

  },

  computed: {

    lastSyncLabel() {

      if (!this.status.lastSuccessfulSyncAt) {

        return "—";

      }

      return this.formatDate(this.status.lastSuccessfulSyncAt);

    },

  },

  mounted() {

    this.loadAll();

    this.pollTimer = setInterval(() => {

      if (this.status.isSyncInProgress) {

        this.loadStatus();

      }

    }, 3000);

  },

  beforeDestroy() {

    if (this.pollTimer) {

      clearInterval(this.pollTimer);

    }

  },

  methods: {

    formatDate(value) {

      if (!value) return "—";

      try {

        return new Date(value).toLocaleString(this.$i18n?.locale === "en" ? "en" : "ar-IQ");

      } catch (e) {

        return String(value);

      }

    },

    formatArchiveSize(bytes) {

      const value = Number(bytes || 0);

      if (!value) return "—";

      if (value < 1024) return `${value} B`;

      if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;

      if (value < 1024 * 1024 * 1024) return `${(value / (1024 * 1024)).toFixed(2)} MB`;

      return `${(value / (1024 * 1024 * 1024)).toFixed(2)} GB`;

    },

    isAuthError(error) {

      const status = error?.response?.status;

      return status === 401 || status === 403;

    },

    handleSyncApiError(error, fallbackMessage) {

      if (this.isAuthError(error)) {

        return;

      }

      console.error(error);

      this.$toast.error(fallbackMessage, {

        position: "top-right",

        timeout: 4000,

        maxToasts: 1,

      });

    },

    async loadAll() {

      this.loading = true;

      try {

        await Promise.allSettled([

          this.loadStatus(),

          this.loadSettings(),

          this.loadHistory(),

        ]);

      } finally {

        this.loading = false;

      }

    },

    async loadStatus() {

      try {

        const res = await HTTP.get("Sync/status");

        this.status = { ...this.status, ...(res?.data?.data || {}) };

      } catch (error) {

        this.handleSyncApiError(

          error,

          this.$t("syncLoadFailed") || "تعذر تحميل حالة المزامنة"

        );

      }

    },

    async loadSettings() {

      try {

        const res = await HTTP.get("Sync/settings");

        const data = res?.data?.data || {};

        this.settings = {

          autoSyncEnabled: !!data.autoSyncEnabled,

          intervalMinutes: data.intervalMinutes || 10,

        };

      } catch (error) {

        this.handleSyncApiError(

          error,

          this.$t("syncLoadFailed") || "تعذر تحميل إعدادات المزامنة"

        );

      }

    },

    async loadHistory() {

      try {

        const res = await HTTP.get("Sync/history");

        this.history = res?.data?.data || [];

      } catch (error) {

        this.handleSyncApiError(

          error,

          this.$t("syncLoadFailed") || "تعذر تحميل سجل المزامنة"

        );

      }

    },

    async confirmClearHistory() {

      const ok = await this.$confirm({

        message: this.$t("confirmClearSyncHistory") || "هل تريد حذف سجل عمليات الرفع؟ لا يؤثر على الملفات المرفوعة على FTP.",

      });

      if (!ok) {

        return;

      }

      this.clearingHistory = true;

      try {

        const res = await HTTP.delete("Sync/history");

        if (res?.data?.errorStatus) {

          const msg = res?.data?.message;

          if (msg === "syncInProgress") {

            this.$toast.warning(this.$t("syncInProgressCannotClear") || "لا يمكن الحذف أثناء الرفع");

          } else {

            this.$toast.error(msg || this.$t("clearSyncHistoryFailed") || "فشل حذف السجل");

          }

          return;

        }

        this.history = [];

        this.$toast.success(this.$t("clearSyncHistorySuccess") || "تم حذف السجل");

        await this.loadStatus();

      } catch (e) {

        console.error(e);

        const msg = e?.response?.data?.message;

        if (msg === "syncInProgress") {

          this.$toast.warning(this.$t("syncInProgressCannotClear") || "لا يمكن الحذف أثناء الرفع");

        } else {

          this.$toast.error(msg || this.$t("clearSyncHistoryFailed") || "فشل حذف السجل");

        }

      } finally {

        this.clearingHistory = false;

      }

    },

    async saveSettings() {

      if (this.savingSettings) return;

      this.savingSettings = true;

      try {

        await HTTP.put("Sync/settings", this.settings);

        this.$toast.success(this.$t("settingsSaved") || "تم حفظ الإعدادات");

        await this.loadStatus();

      } catch (e) {

        console.error(e);

        this.$toast.error(this.$t("saveFailed") || "فشل الحفظ");

      } finally {

        this.savingSettings = false;

      }

    },

    async testConnection() {

      this.testingConnection = true;

      try {

        const res = await HTTP.post("Sync/test-connection");

        const data = res?.data?.data || {};

        if (data.ftpOk) {

          this.$toast.success(this.$t("connectionTestOk") || "الاتصال ناجح");

        } else {

          this.$toast.warning(data.ftpMessage || this.$t("connectionTestFailed") || "فشل الاتصال");

        }

        await this.loadStatus();

      } catch (e) {

        console.error(e);

        this.$toast.error(this.$t("connectionTestFailed") || "فشل الاتصال");

      } finally {

        this.testingConnection = false;

      }

    },

    async pushSync() {

      this.pushing = true;

      try {

        const res = await HTTP.post("Sync/push", null, { timeout: BACKUP_UPLOAD_TIMEOUT_MS });

        const data = res?.data?.data || {};

        if (data.success) {

          this.$toast.success(

            `${this.$t("backupUploadCompleted") || "اكتمل رفع النسخة"}: ${data.archiveFileName || ""} (${this.formatArchiveSize(data.archiveSizeBytes)})`

          );

        } else {

          this.$toast.error(data.message || this.$t("syncFailed") || "فشل الرفع");

        }

        await this.loadAll();

      } catch (e) {

        console.error(e);

        this.$toast.error(e?.response?.data?.message || this.$t("syncFailed") || "فشل الرفع");

      } finally {

        this.pushing = false;

      }

    },

  },

};

</script>



<style scoped>

.sync-now-btn {

  background: linear-gradient(135deg, var(--primary-color), var(--primary-color)) !important;

}



.sync-settings-card {

  margin-bottom: 1rem;

}



.sync-settings-row {

  display: flex;

  flex-wrap: wrap;

  align-items: center;

  gap: 1.5rem;

}



.sync-toggle-label {

  display: inline-flex;

  align-items: center;

  gap: 0.5rem;

  font-weight: 600;

  cursor: pointer;

}



.sync-interval-wrap {

  display: flex;

  align-items: center;

  gap: 0.5rem;

}



.sync-interval-select {

  min-width: 140px;

  padding: 0.4rem 0.6rem;

  border-radius: 0.5rem;

  border: 1px solid var(--border-color);

  background: var(--bg-primary);

  color: var(--text-primary);

}



.sync-error-text {

  margin: 0.75rem 0 0;

  color: var(--danger-color);

  font-size: 0.88rem;

}



.sync-archive-name {

  font-size: 0.82rem;

  word-break: break-all;

  max-width: 220px;

}



.sync-history-table-wrap {

  overflow-x: auto;

}



.sync-history-table {

  width: 100%;

  border-collapse: collapse;

  font-size: 0.88rem;

}



.sync-history-table th,

.sync-history-table td {

  padding: 0.65rem 0.85rem;

  border-bottom: 1px solid var(--border-color);

  text-align: right;

}



.sync-history-message {

  max-width: 220px;

  word-break: break-word;

}



.sync-status-badge {

  display: inline-block;

  padding: 0.2rem 0.55rem;

  border-radius: 999px;

  font-size: 0.75rem;

  font-weight: 700;

}



.sync-status-badge--success {

  background: rgba(34, 197, 94, 0.15);

  color: #16a34a;

}



.sync-status-badge--failed {

  background: rgba(239, 68, 68, 0.15);

  color: #dc2626;

}



.sync-status-badge--running {

  background: color-mix(in srgb, var(--primary-color) 15%, transparent);

  color: var(--primary-color);

}



.sync-clear-history-btn {

  display: inline-flex;

  align-items: center;

  gap: 0.35rem;

  padding: 0.45rem 0.85rem;

  border-radius: 0.5rem;

  border: 1px solid rgba(239, 68, 68, 0.35);

  background: rgba(239, 68, 68, 0.08);

  color: var(--danger-color, #dc2626);

  font-weight: 600;

  font-size: 0.88rem;

  cursor: pointer;

}



.sync-clear-history-btn:hover:not(:disabled) {

  background: rgba(239, 68, 68, 0.15);

}



.sync-clear-history-btn:disabled {

  opacity: 0.5;

  cursor: not-allowed;

}

</style>


