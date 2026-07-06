<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content payment-devices-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="credit-card-2-front-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("paymentDevicesManagement") || "إدارة أجهزة الدفع" }}</h1>
                  <p class="header-subtitle">{{ $t("paymentDevicesDescription") || "ربط جهاز PAX Nebula والاتصال عبر USB أو WiFi أو Cloud" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="loadDevices" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
                <button type="button" class="users-add-button" @click="openAddModal">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addPaymentDevice") || "إضافة جهاز" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="hdd-network-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ devices.length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("paymentDevicesTotal") || "إجمالي الأجهزة" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ activeDevicesCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("active") || "نشط" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="star-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ defaultDeviceName }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("defaultDevice") || "الجهاز الافتراضي" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="plug-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ checkedOnlineCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("devicesCheckedOnline") || "متصل (بعد الفحص)" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="credit-card-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("paymentDevicesList") || "أجهزة الدفع" }}</h3>
                  <p class="app-section-subtitle">{{ $t("paymentDevicesListHint") || "إدارة الاتصال بأجهزة PAX وتعيين الجهاز الافتراضي للكاشير" }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body">
              <div v-if="loading" class="loading-state">
                <b-spinner small></b-spinner>
                <span>{{ $t("loading") || "جاري التحميل..." }}</span>
              </div>
              <div v-else-if="devices.length > 0" class="payment-devices-cards-grid">
                <div v-for="device in devices" :key="device.id" class="payment-device-card">
                  <div class="payment-device-card-header">
                    <div class="payment-device-card-title">
                      <b-icon icon="hdd-network-fill" class="payment-device-card-icon"></b-icon>
                      <h4 :title="device.name">{{ device.name }}</h4>
                    </div>
                    <div class="payment-device-card-badges">
                      <span v-if="device.isDefault" class="item-badge item-badge--main">
                        {{ $t("defaultDevice") || "افتراضي" }}
                      </span>
                      <span v-if="!device.isActive" class="item-badge item-badge--inactive">
                        {{ $t("inactive") || "غير مفعل" }}
                      </span>
                      <span
                        v-else-if="getDeviceConnection(device.id).online === true"
                        class="item-badge item-badge--online"
                      >
                        <b-icon icon="circle-fill"></b-icon>
                        {{ $t("online") || "متصل" }}
                      </span>
                      <span
                        v-else-if="getDeviceConnection(device.id).online === false"
                        class="item-badge item-badge--offline"
                      >
                        <b-icon icon="circle-fill"></b-icon>
                        {{ $t("offline") || "غير متصل" }}
                      </span>
                      <span
                        v-else
                        class="item-badge item-badge--unknown"
                      >
                        {{ $t("notChecked") || "لم يُفحص" }}
                      </span>
                    </div>
                    <div class="payment-device-card-actions">
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--edit"
                        @click="openEditModal(device)"
                        :title="$t('edit') || 'تعديل'"
                      >
                        <b-icon icon="pencil" class="action-icon"></b-icon>
                      </button>
                      <button
                        type="button"
                        class="action-btn action-btn--icon action-btn--delete"
                        @click="confirmDelete(device)"
                        :title="$t('delete') || 'حذف'"
                      >
                        <b-icon icon="trash" class="action-icon"></b-icon>
                      </button>
                    </div>
                  </div>

                  <div class="payment-device-card-body">
                    <div class="payment-device-info-row">
                      <b-icon icon="link-45deg" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("baseUrl") || "عنوان الخادم" }}</span>
                      <span class="info-value info-value--mono" :title="device.baseUrl">{{ device.baseUrl }}</span>
                    </div>
                    <div class="payment-device-info-row">
                      <b-icon icon="diagram-3-fill" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("connectionType") || "نوع الاتصال" }}</span>
                      <span class="info-value">{{ connectionTypeLabel(device.connectionType) }}</span>
                    </div>
                    <div v-if="device.comPort" class="payment-device-info-row">
                      <b-icon icon="usb-plug-fill" class="info-icon"></b-icon>
                      <span class="info-label">COM Port</span>
                      <span class="info-value info-value--mono">{{ device.comPort }}</span>
                    </div>
                    <div v-if="device.wifiHost" class="payment-device-info-row">
                      <b-icon icon="wifi" class="info-icon"></b-icon>
                      <span class="info-label">Host</span>
                      <span class="info-value info-value--mono">
                        {{ device.wifiHost }}{{ device.wifiPort ? `:${device.wifiPort}` : "" }}
                      </span>
                    </div>
                    <div
                      v-if="getDeviceConnection(device.id).label"
                      class="payment-device-info-row payment-device-info-row--status"
                    >
                      <b-icon icon="activity" class="info-icon"></b-icon>
                      <span class="info-label">{{ $t("connectionStatus") || "حالة الاتصال" }}</span>
                      <span
                        class="info-value"
                        :class="{
                          'info-value--success': getDeviceConnection(device.id).online === true,
                          'info-value--danger': getDeviceConnection(device.id).online === false,
                        }"
                      >
                        {{ getDeviceConnection(device.id).label }}
                      </span>
                    </div>
                  </div>

                  <div class="payment-device-card-footer">
                    <div class="payment-device-footer-primary">
                      <button
                        type="button"
                        class="payment-device-btn payment-device-btn--outline"
                        @click="checkStatus(device)"
                        :disabled="busyId === device.id"
                      >
                        <b-spinner small v-if="busyId === device.id && busyAction === 'check'"></b-spinner>
                        <b-icon v-else icon="arrow-repeat" class="btn-icon"></b-icon>
                        <span>{{ $t("checkConnection") || "فحص الاتصال" }}</span>
                      </button>
                      <button
                        type="button"
                        class="payment-device-btn payment-device-btn--primary"
                        @click="connectDevice(device)"
                        :disabled="busyId === device.id || !device.isActive"
                      >
                        <b-spinner small v-if="busyId === device.id && busyAction === 'connect'"></b-spinner>
                        <b-icon v-else icon="plug-fill" class="btn-icon"></b-icon>
                        <span>{{ $t("connectDevice") || "اتصال بالجهاز" }}</span>
                      </button>
                    </div>
                    <button
                      type="button"
                      class="payment-device-btn payment-device-btn--danger-outline"
                      @click="cancelTrans(device)"
                      :disabled="busyId === device.id"
                    >
                      <b-spinner small v-if="busyId === device.id && busyAction === 'cancel'"></b-spinner>
                      <b-icon v-else icon="x-circle" class="btn-icon"></b-icon>
                      <span>{{ $t("cancelTransaction") || "إلغاء العملية الجارية" }}</span>
                    </button>
                  </div>
                </div>
              </div>
              <div v-else class="empty-state">
                <b-icon icon="credit-card-2-front" class="empty-icon"></b-icon>
                <p>{{ $t("noPaymentDevices") || "لا توجد أجهزة دفع" }}</p>
                <button type="button" class="empty-state-btn" @click="openAddModal">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addFirstPaymentDevice") || "إضافة أول جهاز" }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal
      v-model="showDeviceModal"
      hide-header
      hide-footer
      class="users-modal"
      centered
      size="lg"
      @hidden="resetForm"
    >
      <div class="modal-content-wrapper">
        <h2 class="modal-title">
          {{ editingId ? ($t("editPaymentDevice") || "تعديل جهاز") : ($t("addPaymentDevice") || "إضافة جهاز") }}
        </h2>
        <form class="users-form" @submit.prevent="saveDevice">
          <div class="modal-form-grid">
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="hdd-network-fill" class="form-label-icon"></b-icon>
                {{ $t("deviceName") || "اسم الجهاز" }} <span class="required">*</span>
              </label>
              <input v-model="form.name" type="text" class="users-form-input" required />
            </div>
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="link-45deg" class="form-label-icon"></b-icon>
                {{ $t("baseUrl") || "عنوان الخادم" }} <span class="required">*</span>
              </label>
              <input v-model="form.baseUrl" type="text" class="users-form-input" placeholder="http://localhost:9092" required />
            </div>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="diagram-3-fill" class="form-label-icon"></b-icon>
              {{ $t("connectionType") || "نوع الاتصال" }}
            </label>
            <select v-model="form.connectionType" class="users-form-select">
              <option value="Usb">USB</option>
              <option value="Wifi">WiFi</option>
              <option value="Cloud">Cloud</option>
            </select>
          </div>
          <div v-if="form.connectionType === 'Usb'" class="users-form-group">
            <label class="users-form-label">
              <b-icon icon="usb-plug-fill" class="form-label-icon"></b-icon>
              COM Port
            </label>
            <input v-model="form.comPort" type="text" class="users-form-input" placeholder="COM6" />
          </div>
          <template v-if="form.connectionType === 'Wifi'">
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="wifi" class="form-label-icon"></b-icon>
                  Host
                </label>
                <input v-model="form.wifiHost" type="text" class="users-form-input" />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">Port</label>
                <input v-model.number="form.wifiPort" type="number" class="users-form-input" />
              </div>
            </div>
            <div class="users-form-group">
              <label class="users-form-label">WiFi JSON</label>
              <textarea
                v-model="form.wifiConfigJson"
                class="users-form-input"
                rows="3"
                placeholder='{"eid":"248093","sn":["1850151137"],"isWebsocket":false}'
              ></textarea>
            </div>
          </template>
          <div v-if="form.connectionType === 'Cloud'" class="users-form-group">
            <label class="users-form-label">Cloud JSON</label>
            <textarea
              v-model="form.cloudConfigJson"
              class="users-form-input"
              rows="3"
              placeholder='{"code":"68728","eid":"9100088"}'
            ></textarea>
          </div>
          <div class="form-toggle-cards">
            <label
              class="form-toggle-card"
              :class="{ 'form-toggle-card--on': form.isActive }"
            >
              <input
                v-model="form.isActive"
                type="checkbox"
                id="pd-active"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--success">
                  <b-icon icon="check-circle-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("active") || "مفعل" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("paymentDeviceActiveHint") || "الجهاز متاح للدفع من الكاشير" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
            <label
              class="form-toggle-card form-toggle-card--accent-warning"
              :class="{ 'form-toggle-card--on': form.isDefault }"
            >
              <input
                v-model="form.isDefault"
                type="checkbox"
                id="pd-default"
                class="form-toggle-card-input"
              />
              <span class="form-toggle-card-body">
                <span class="form-toggle-card-icon form-toggle-card-icon--warning">
                  <b-icon icon="star-fill"></b-icon>
                </span>
                <span class="form-toggle-card-text">
                  <span class="form-toggle-card-title">{{ $t("defaultDevice") || "جهاز افتراضي" }}</span>
                  <span class="form-toggle-card-desc">{{ $t("defaultDeviceHint") || "يُستخدم تلقائياً عند الدفع بالبطاقة" }}</span>
                </span>
              </span>
              <span class="form-toggle-switch" aria-hidden="true"></span>
            </label>
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showDeviceModal = false" :disabled="saving">
              {{ $t("cancel") || "إلغاء" }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="saving">
              <b-spinner small v-if="saving" class="me-2"></b-spinner>
              {{ saving ? ($t("saving") || "جاري الحفظ...") : ($t("save") || "حفظ") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <b-modal
      v-model="showDeleteModal"
      hide-header
      hide-footer
      class="users-modal"
      centered
      @hidden="deviceToDelete = null"
    >
      <div class="modal-content-wrapper">
        <div class="delete-confirmation-content">
          <div class="delete-icon-wrapper">
            <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
          </div>
          <h3 class="delete-confirmation-title">{{ $t("confirmDelete") || "تأكيد الحذف" }}</h3>
          <p class="delete-confirmation-text">
            {{ $t("confirmDeletePaymentDevice") || "هل أنت متأكد من حذف جهاز الدفع" }}
            <strong v-if="deviceToDelete">{{ deviceToDelete.name }}</strong>؟
          </p>
          <div class="delete-confirmation-actions">
            <button type="button" class="delete-confirm-button" :disabled="deleting" @click="executeDelete">
              <b-spinner small v-if="deleting" class="me-2"></b-spinner>
              {{ $t("confirmButton") || "تأكيد" }}
            </button>
            <button type="button" class="delete-cancel-button" @click="showDeleteModal = false">
              {{ $t("cancelButton") || "إلغاء" }}
            </button>
          </div>
        </div>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";

const emptyForm = () => ({
  name: "",
  baseUrl: "http://localhost:9092",
  connectionType: "Usb",
  comPort: "COM6",
  wifiHost: "",
  wifiPort: null,
  wifiConfigJson: "",
  cloudConfigJson: "",
  isDefault: false,
  isActive: true,
});

export default {
  name: "PaymentDevicesView",
  components: { AppHeader },
  data() {
    return {
      devices: [],
      loading: false,
      saving: false,
      deleting: false,
      busyId: null,
      busyAction: null,
      editingId: null,
      form: emptyForm(),
      connectionStatus: {},
      showDeviceModal: false,
      showDeleteModal: false,
      deviceToDelete: null,
    };
  },
  computed: {
    activeDevicesCount() {
      return this.devices.filter((d) => d.isActive !== false).length;
    },
    defaultDeviceName() {
      const d = this.devices.find((x) => x.isDefault);
      if (!d) return "—";
      return d.name.length > 18 ? `${d.name.slice(0, 18)}…` : d.name;
    },
    checkedOnlineCount() {
      return this.devices.filter((d) => this.getDeviceConnection(d.id).online === true).length;
    },
  },
  mounted() {
    this.loadDevices();
  },
  methods: {
    connectionTypeLabel(type) {
      const map = {
        Usb: "USB",
        Wifi: "WiFi",
        Cloud: "Cloud",
      };
      return map[type] || type || "—";
    },
    getDeviceConnection(deviceId) {
      return this.connectionStatus[deviceId] || { online: null, label: "" };
    },
    parseConnectionStatus(raw) {
      if (raw === null || raw === undefined || raw === "") {
        return { online: null, label: "" };
      }

      const str = String(raw).trim();
      const connectedLabel = this.$t("connected") || "متصل";
      const disconnectedLabel = this.$t("disconnected") || "غير متصل";

      try {
        const parsed = JSON.parse(str);
        const code = String(parsed.resultCode ?? parsed.ResultCode ?? "").trim();
        const message = String(parsed.message ?? parsed.Message ?? "").trim();

        if (code === "200" || message.toLowerCase().includes("connected")) {
          return { online: true, label: message || connectedLabel };
        }
        if (code && code !== "200") {
          return { online: false, label: message || `${this.$t("error") || "خطأ"} (${code})` };
        }
        return { online: false, label: message || disconnectedLabel };
      } catch (e) {
        const lower = str.toLowerCase();
        if (lower.includes("connected") || lower === "true" || lower === "200") {
          return { online: true, label: connectedLabel };
        }
        if (lower.includes("fail") || lower.includes("error") || lower === "false") {
          return { online: false, label: str };
        }
        return { online: null, label: str };
      }
    },
    async loadDevices() {
      this.loading = true;
      try {
        const res = await HTTP.get("PaymentDevices");
        this.devices = res?.data?.data || [];
        this.refreshAllDeviceStatuses();
      } catch (e) {
        console.error(e);
        this.$notify.error(this.$t("loadFailed") || "فشل التحميل");
      } finally {
        this.loading = false;
      }
    },
    openAddModal() {
      this.editingId = null;
      this.form = emptyForm();
      this.showDeviceModal = true;
    },
    openEditModal(device) {
      this.editingId = device.id;
      this.form = {
        name: device.name,
        baseUrl: device.baseUrl,
        connectionType: device.connectionType || "Usb",
        comPort: device.comPort || "",
        wifiHost: device.wifiHost || "",
        wifiPort: device.wifiPort,
        wifiConfigJson: device.wifiConfigJson || "",
        cloudConfigJson: device.cloudConfigJson || "",
        isDefault: !!device.isDefault,
        isActive: device.isActive !== false,
      };
      this.showDeviceModal = true;
    },
    resetForm() {
      this.form = emptyForm();
      this.editingId = null;
    },
    async saveDevice() {
      this.saving = true;
      try {
        if (this.editingId) {
          await HTTP.put(`PaymentDevices/${this.editingId}`, this.form);
        } else {
          await HTTP.post("PaymentDevices", this.form);
        }
        this.showDeviceModal = false;
        await this.loadDevices();
        this.$notify.success(this.$t("savedSuccessfully") || "تم الحفظ");
      } catch (e) {
        console.error(e);
        this.$notify.error(e?.response?.data?.message || this.$t("saveFailed") || "فشل الحفظ");
      } finally {
        this.saving = false;
      }
    },
    confirmDelete(device) {
      this.deviceToDelete = device;
      this.showDeleteModal = true;
    },
    async executeDelete() {
      if (!this.deviceToDelete) return;
      this.deleting = true;
      try {
        await HTTP.delete(`PaymentDevices/${this.deviceToDelete.id}`);
        this.showDeleteModal = false;
        await this.loadDevices();
        this.$notify.success(this.$t("deletedSuccessfully") || "تم الحذف");
      } catch (e) {
        this.$notify.error(this.$t("deleteFailed") || "فشل الحذف");
      } finally {
        this.deleting = false;
      }
    },
    async refreshAllDeviceStatuses() {
      if (!this.devices.length) return;
      await Promise.all(
        this.devices.map((device) => this.checkStatus(device, { silent: true }))
      );
    },
    async checkStatus(device, options = {}) {
      const silent = options.silent === true;
      if (!silent) {
        this.busyId = device.id;
        this.busyAction = "check";
      }
      try {
        const res = await HTTP.get(`PaymentDevices/${device.id}/status`);
        const raw = res?.data?.data?.raw ?? res?.data?.data?.connected ?? "";
        this.$set(this.connectionStatus, device.id, this.parseConnectionStatus(raw));
      } catch (e) {
        this.$set(this.connectionStatus, device.id, {
          online: false,
          label: this.$t("connectionFailed") || "فشل الاتصال",
        });
        if (!silent) {
          this.$notify.error(this.$t("connectionFailed") || "فشل الاتصال");
        }
      } finally {
        if (!silent) {
          this.busyId = null;
          this.busyAction = null;
        }
      }
    },
    async connectDevice(device) {
      this.busyId = device.id;
      this.busyAction = "connect";
      try {
        const body = {
          comPort: device.comPort,
          wifiHost: device.wifiHost,
          wifiPort: device.wifiPort,
          wifiConfigJson: device.wifiConfigJson,
          cloudConfigJson: device.cloudConfigJson,
        };
        const res = await HTTP.post(`PaymentDevices/${device.id}/connect`, body);
        const success = res?.data?.data?.success !== false && !res?.data?.errorStatus;
        await this.checkStatus(device, { silent: true });
        if (success) {
          this.$notify.success(this.$t("connectDeviceSuccess") || "تم الاتصال بالجهاز");
        } else {
          this.$notify.warning(res?.data?.message || this.$t("connectionFailed"));
        }
      } catch (e) {
        this.$notify.error(e?.response?.data?.message || this.$t("connectionFailed"));
      } finally {
        this.busyId = null;
        this.busyAction = null;
      }
    },
    async cancelTrans(device) {
      this.busyId = device.id;
      this.busyAction = "cancel";
      try {
        await HTTP.post(`PaymentDevices/${device.id}/cancel`);
        this.$notify.info(this.$t("cancelSent") || "تم إرسال الإلغاء");
      } catch (e) {
        this.$notify.error(this.$t("cancelFailed") || "فشل الإلغاء");
      } finally {
        this.busyId = null;
        this.busyAction = null;
      }
    },
  },
};
</script>

<style scoped>
.payment-devices-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 1rem;
}

.payment-device-card {
  border: 1.5px solid var(--border-color);
  border-radius: 0.85rem;
  background: var(--bg-primary);
  display: flex;
  flex-direction: column;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.payment-device-card:hover {
  border-color: rgba(99, 102, 241, 0.45);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.payment-device-card-header {
  display: grid;
  grid-template-columns: 1fr auto;
  grid-template-rows: auto auto;
  gap: 0.5rem 0.75rem;
  padding: 0.9rem 1rem;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
}

.payment-device-card-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  grid-column: 1;
  grid-row: 1;
}

.payment-device-card-title h4 {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.payment-device-card-icon {
  color: var(--primary-color);
  flex-shrink: 0;
  font-size: 1.2rem;
}

.payment-device-card-badges {
  grid-column: 1;
  grid-row: 2;
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}

.payment-device-card-actions {
  grid-column: 2;
  grid-row: 1 / span 2;
  display: flex;
  align-items: flex-start;
  gap: 0.35rem;
}

.item-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 700;
}

.item-badge--main {
  background: rgba(99, 102, 241, 0.15);
  color: #4f46e5;
}

.item-badge--inactive {
  background: rgba(148, 163, 184, 0.2);
  color: #64748b;
}

.item-badge--online {
  background: rgba(34, 197, 94, 0.15);
  color: #15803d;
}

.item-badge--offline {
  background: rgba(239, 68, 68, 0.12);
  color: #b91c1c;
}

.item-badge--unknown {
  background: rgba(148, 163, 184, 0.15);
  color: #64748b;
}

.payment-device-card-body {
  padding: 0.85rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  flex: 1;
}

.payment-device-info-row {
  display: grid;
  grid-template-columns: auto minmax(5.5rem, auto) 1fr;
  gap: 0.35rem 0.65rem;
  align-items: start;
  font-size: 0.82rem;
}

.payment-device-info-row .info-icon {
  color: var(--text-secondary);
  margin-top: 0.1rem;
}

.payment-device-info-row .info-label {
  color: var(--text-secondary);
  font-weight: 600;
  white-space: nowrap;
}

.payment-device-info-row .info-value {
  color: var(--text-primary);
  font-weight: 500;
  word-break: break-word;
  text-align: start;
}

.payment-device-info-row .info-value--mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 0.78rem;
  direction: ltr;
  text-align: left;
}

.payment-device-info-row--status {
  padding-top: 0.35rem;
  margin-top: 0.15rem;
  border-top: 1px dashed var(--border-color);
}

.info-value--success {
  color: #15803d;
  font-weight: 700;
}

.info-value--danger {
  color: #b91c1c;
  font-weight: 700;
}

.payment-device-card-footer {
  padding: 0.75rem 1rem 1rem;
  border-top: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.payment-device-footer-primary {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
}

.payment-device-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  padding: 0.55rem 0.75rem;
  border-radius: 0.6rem;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  border: 1px solid transparent;
  min-width: 0;
}

.payment-device-btn .btn-icon {
  flex-shrink: 0;
}

.payment-device-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.payment-device-btn--outline {
  background: var(--bg-primary);
  border-color: var(--border-color);
  color: var(--text-primary);
}

.payment-device-btn--outline:hover:not(:disabled) {
  border-color: rgba(99, 102, 241, 0.45);
  background: var(--bg-secondary);
}

.payment-device-btn--primary {
  background: linear-gradient(135deg, #818cf8 0%, #6366f1 100%);
  color: #fff;
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.25);
}

.payment-device-btn--primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(99, 102, 241, 0.35);
}

.payment-device-btn--danger-outline {
  width: 100%;
  background: rgba(239, 68, 68, 0.06);
  border-color: rgba(239, 68, 68, 0.22);
  color: #dc2626;
}

.payment-device-btn--danger-outline:hover:not(:disabled) {
  background: rgba(239, 68, 68, 0.1);
}

.loading-state,
.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.empty-icon {
  font-size: 4rem;
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.empty-state p {
  color: var(--text-secondary);
  margin-bottom: 1rem;
}

.required {
  color: var(--danger-color, #dc3545);
}

@media (max-width: 480px) {
  .payment-devices-cards-grid {
    grid-template-columns: 1fr;
  }

  .payment-device-footer-primary {
    grid-template-columns: 1fr;
  }
}
</style>
