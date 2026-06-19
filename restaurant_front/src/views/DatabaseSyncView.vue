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
                  <h1 class="users-page-title">{{ $t("databaseSyncTitle") || "مزامنة البيانات" }}</h1>
                  <p class="header-subtitle">{{ $t("databaseSyncDescription") || "مزامنة محلية إلى السحابة (نسخ احتياطي)" }}</p>
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
                  <span class="button-text">{{ $t("syncNow") || "مزامنة الآن" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon" :class="status.remoteDatabaseConnected ? 'app-overview-stat-icon--success' : 'app-overview-stat-icon--warning'">
                <b-icon icon="database-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ status.remoteDatabaseConnected ? ($t("connected") || "متصل") : ($t("disconnected") || "غير متصل") }}</div>
                <div class="app-overview-stat-label">{{ $t("cloudDatabase") || "قاعدة السحابة" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon" :class="status.ftpConnected ? 'app-overview-stat-icon--success' : 'app-overview-stat-icon--warning'">
                <b-icon icon="folder-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ status.ftpConnected ? ($t("connected") || "متصل") : ($t("disconnected") || "غير متصل") }}</div>
                <div class="app-overview-stat-label">{{ $t("cloudImagesFtp") || "صور السحابة (FTP)" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="clock-history"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ lastSyncLabel }}</div>
                <div class="app-overview-stat-label">{{ $t("lastSuccessfulSync") || "آخر مزامنة ناجحة" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="layers-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ status.estimatedPendingRecords || 0 }}</div>
                <div class="app-overview-stat-label">{{ $t("pendingRecords") || "سجلات معلقة" }}</div>
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
                  <h3 class="app-section-title">{{ $t("autoSyncSettings") || "المزامنة التلقائية" }}</h3>
                  <p class="app-section-subtitle">{{ $t("autoSyncSettingsHint") || "رفع التغييرات المحلية إلى السحابة بشكل دوري" }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <div class="sync-settings-row">
                <label class="sync-toggle-label">
                  <input type="checkbox" v-model="settings.autoSyncEnabled" @change="saveSettings" />
                  <span>{{ $t("enableAutoSync") || "تفعيل المزامنة التلقائية" }}</span>
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
                  <h3 class="app-section-title">{{ $t("syncHistory") || "سجل المزامنة" }}</h3>
                  <p class="app-section-subtitle">{{ $t("syncHistoryHint") || "آخر عمليات المزامنة" }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding">
              <div class="sync-history-table-wrap">
                <table class="sync-history-table">
                  <thead>
                    <tr>
                      <th>{{ $t("date") || "التاريخ" }}</th>
                      <th>{{ $t("status") || "الحالة" }}</th>
                      <th>{{ $t("trigger") || "النوع" }}</th>
                      <th>{{ $t("records") || "سجلات" }}</th>
                      <th>{{ $t("files") || "ملفات" }}</th>
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
                      <td>{{ row.recordsPushed }}</td>
                      <td>{{ row.filesPushed }}</td>
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
import { HTTP } from "../http/api";

export default {
  name: "DatabaseSyncView",
  components: { AppHeader },
  data() {
    return {
      loading: false,
      pushing: false,
      testingConnection: false,
      savingSettings: false,
      status: {
        syncEnabled: false,
        remoteDatabaseConnected: false,
        ftpConnected: false,
        isSyncInProgress: false,
        autoSyncEnabled: false,
        intervalMinutes: 10,
        estimatedPendingRecords: 0,
        lastSuccessfulSyncAt: null,
        lastSyncStatus: null,
        lastSyncError: null,
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
    async loadAll() {
      this.loading = true;
      try {
        await Promise.all([this.loadStatus(), this.loadSettings(), this.loadHistory()]);
      } finally {
        this.loading = false;
      }
    },
    async loadStatus() {
      const res = await HTTP.get("Sync/status");
      this.status = { ...this.status, ...(res?.data?.data || {}) };
    },
    async loadSettings() {
      const res = await HTTP.get("Sync/settings");
      const data = res?.data?.data || {};
      this.settings = {
        autoSyncEnabled: !!data.autoSyncEnabled,
        intervalMinutes: data.intervalMinutes || 10,
      };
    },
    async loadHistory() {
      const res = await HTTP.get("Sync/history");
      this.history = res?.data?.data || [];
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
        const dbOk = data.remoteDatabaseOk;
        const ftpOk = data.ftpOk;
        if (dbOk && (ftpOk || !data.ftpMessage || data.ftpMessage.includes("disabled"))) {
          this.$toast.success(this.$t("connectionTestOk") || "الاتصال ناجح");
        } else {
          const msg = [data.databaseMessage, data.ftpMessage].filter(Boolean).join(" | ");
          this.$toast.warning(msg || this.$t("connectionTestFailed") || "فشل الاتصال");
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
        const res = await HTTP.post("Sync/push");
        const data = res?.data?.data || {};
        if (data.success) {
          this.$toast.success(
            `${this.$t("syncCompleted") || "اكتملت المزامنة"}: ${data.recordsPushed} ${this.$t("records") || "سجلات"}, ${data.filesPushed} ${this.$t("files") || "ملفات"}`
          );
        } else {
          this.$toast.error(data.message || this.$t("syncFailed") || "فشلت المزامنة");
        }
        await this.loadAll();
      } catch (e) {
        console.error(e);
        this.$toast.error(e?.response?.data?.message || this.$t("syncFailed") || "فشلت المزامنة");
      } finally {
        this.pushing = false;
      }
    },
  },
};
</script>

<style scoped>
.sync-now-btn {
  background: linear-gradient(135deg, #6366f1, #4f46e5) !important;
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
  background: rgba(99, 102, 241, 0.15);
  color: #6366f1;
}
</style>
