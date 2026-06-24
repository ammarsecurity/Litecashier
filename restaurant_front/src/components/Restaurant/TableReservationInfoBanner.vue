<template>
  <div
    v-if="hasData"
    class="res-info-card"
    :class="{ 'res-info-card--embedded': embedded }"
  >
    <div class="res-info-card-accent" aria-hidden="true"></div>
    <div class="res-info-card-inner">
      <div class="res-info-card-top">
        <div class="res-info-card-badge" aria-hidden="true">
          <b-icon icon="calendar-check-fill"></b-icon>
        </div>
        <div class="res-info-card-title-wrap">
          <span class="res-info-card-title">{{ $t("posTableReservationInfo") || "بيانات الحجز" }}</span>
          <span v-if="statusLabel" class="res-info-card-status">{{ statusLabel }}</span>
        </div>
      </div>

      <div class="res-info-card-stats">
        <div v-if="customerName" class="res-info-stat res-info-stat--name">
          <span class="res-info-stat-icon-wrap" aria-hidden="true">
            <b-icon icon="person-fill" class="res-info-stat-icon"></b-icon>
          </span>
          <div class="res-info-stat-body">
            <span class="res-info-stat-label">{{ $t("customerName") || "اسم العميل" }}</span>
            <span class="res-info-stat-value" :title="customerName">{{ customerName }}</span>
          </div>
        </div>

        <div v-if="phoneNumber" class="res-info-stat res-info-stat--phone">
          <span class="res-info-stat-icon-wrap" aria-hidden="true">
            <b-icon icon="telephone-fill" class="res-info-stat-icon"></b-icon>
          </span>
          <div class="res-info-stat-body">
            <span class="res-info-stat-label">{{ $t("phoneNumber") || "رقم الهاتف" }}</span>
            <a
              :href="`tel:${phoneNumber}`"
              class="res-info-stat-value res-info-stat-value--ltr res-info-stat-link"
              :title="phoneNumber"
            >{{ phoneNumber }}</a>
          </div>
        </div>

        <div v-if="numberOfGuests" class="res-info-stat res-info-stat--guests">
          <span class="res-info-stat-icon-wrap" aria-hidden="true">
            <b-icon icon="people-fill" class="res-info-stat-icon"></b-icon>
          </span>
          <div class="res-info-stat-body">
            <span class="res-info-stat-label">{{ $t("numberOfGuests") || "عدد الضيوف" }}</span>
            <span class="res-info-stat-value res-info-stat-value--guests">{{ numberOfGuests }}</span>
          </div>
        </div>

        <div v-if="reservationDateTime" class="res-info-stat res-info-stat--datetime">
          <span class="res-info-stat-icon-wrap" aria-hidden="true">
            <b-icon icon="calendar-event-fill" class="res-info-stat-icon"></b-icon>
          </span>
          <div class="res-info-stat-body">
            <span class="res-info-stat-label">{{ $t("reservationDateTime") || "تاريخ ووقت الحجز" }}</span>
            <span class="res-info-stat-value" :title="formattedReservationDateTime">
              <span class="res-info-datetime-date">{{ formattedReservationDate }}</span>
              <span v-if="formattedReservationTime" class="res-info-datetime-sep" aria-hidden="true">·</span>
              <span v-if="formattedReservationTime" class="res-info-stat-value--ltr res-info-datetime-time">{{ formattedReservationTime }}</span>
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import {
  formatBusinessDate,
  formatBusinessTime,
} from "@/utils/formatBusinessDateTime.js";

export default {
  name: "TableReservationInfoBanner",
  props: {
    reservation: { type: Object, default: null },
    embedded: { type: Boolean, default: false },
  },
  computed: {
    customerName() {
      return this.reservation?.customerName ?? this.reservation?.CustomerName ?? "";
    },
    phoneNumber() {
      return this.reservation?.phoneNumber ?? this.reservation?.PhoneNumber ?? "";
    },
    numberOfGuests() {
      return this.reservation?.numberOfGuests ?? this.reservation?.NumberOfGuests ?? "";
    },
    reservationDateTime() {
      return (
        this.reservation?.reservationDateTime ??
        this.reservation?.ReservationDateTime ??
        null
      );
    },
    formattedReservationDate() {
      return formatBusinessDate(this.reservationDateTime);
    },
    formattedReservationTime() {
      return (
        this.reservation?.reservationTime ||
        formatBusinessTime(this.reservationDateTime)
      );
    },
    formattedReservationDateTime() {
      const parts = [this.formattedReservationDate, this.formattedReservationTime].filter(Boolean);
      return parts.join(" · ");
    },
    status() {
      return String(this.reservation?.status ?? this.reservation?.Status ?? "").trim();
    },
    statusLabel() {
      const map = {
        Pending: this.$t("reservationStatusPending") || "قيد الانتظار",
        Confirmed: this.$t("reservationStatusConfirmed") || "مؤكد",
        Seated: this.$t("reservationStatusSeated") || "جالس",
        Completed: this.$t("reservationStatusCompleted") || "مكتمل",
        Cancelled: this.$t("reservationStatusCancelled") || "ملغي",
      };
      return map[this.status] || "";
    },
    hasData() {
      return !!(
        this.customerName ||
        this.phoneNumber ||
        this.numberOfGuests ||
        this.reservationDateTime
      );
    },
  },
};
</script>

<style scoped>
.res-info-card {
  position: relative;
  margin: 0 0 0.75rem;
  border-radius: 0.8rem;
  overflow: hidden;
  border: 1px solid rgba(167, 139, 250, 0.28);
  background:
    linear-gradient(145deg, rgba(124, 58, 237, 0.12) 0%, rgba(30, 27, 75, 0.55) 55%, rgba(15, 23, 42, 0.9) 100%);
  box-shadow:
    0 1px 0 rgba(255, 255, 255, 0.05) inset,
    0 8px 22px rgba(15, 23, 42, 0.28);
}

.res-info-card--embedded {
  margin: 0;
  border-radius: 0;
  border-inline: none;
  border-bottom: none;
  box-shadow: none;
}

.res-info-card-accent {
  position: absolute;
  inset-inline-start: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: linear-gradient(180deg, #c4b5fd, #7c3aed 55%, #5b21b6);
}

.res-info-card-inner {
  padding: 0.7rem 0.8rem 0.75rem 0.95rem;
}

.res-info-card-top {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  margin-bottom: 0.65rem;
}

.res-info-card-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border-radius: 0.55rem;
  flex-shrink: 0;
  color: #ede9fe;
  background: linear-gradient(135deg, rgba(167, 139, 250, 0.35), rgba(124, 58, 237, 0.22));
  border: 1px solid rgba(196, 181, 253, 0.35);
  font-size: 1rem;
}

.res-info-card-title-wrap {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.35rem 0.5rem;
  min-width: 0;
}

.res-info-card-title {
  font-size: 0.8125rem;
  font-weight: 800;
  color: #ede9fe;
  letter-spacing: 0.01em;
}

.res-info-card-status {
  display: inline-flex;
  align-items: center;
  padding: 0.12rem 0.5rem;
  border-radius: 999px;
  font-size: 0.6875rem;
  font-weight: 700;
  color: #ddd6fe;
  background: rgba(124, 58, 237, 0.28);
  border: 1px solid rgba(167, 139, 250, 0.35);
}

.res-info-card-stats {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.45rem;
}

.res-info-stat {
  display: flex;
  align-items: flex-start;
  gap: 0.45rem;
  min-width: 0;
  padding: 0.5rem 0.55rem;
  border-radius: 0.62rem;
  background: rgba(15, 23, 42, 0.42);
  border: 1px solid rgba(148, 163, 184, 0.14);
}

.res-info-stat-icon-wrap {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.65rem;
  height: 1.65rem;
  border-radius: 0.45rem;
  flex-shrink: 0;
  background: rgba(124, 58, 237, 0.16);
  color: #c4b5fd;
}

.res-info-stat-icon {
  font-size: 0.82rem;
}

.res-info-stat-body {
  display: flex;
  flex-direction: column;
  gap: 0.12rem;
  min-width: 0;
  flex: 1;
}

.res-info-stat-label {
  font-size: 0.625rem;
  font-weight: 650;
  color: var(--text-muted, #94a3b8);
  line-height: 1.2;
}

.res-info-stat-value {
  font-size: 0.8125rem;
  font-weight: 800;
  color: var(--text-primary, #f8fafc);
  line-height: 1.25;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.res-info-stat-value--ltr {
  direction: ltr;
  text-align: start;
  unicode-bidi: plaintext;
}

.res-info-stat-value--guests {
  font-variant-numeric: tabular-nums;
  font-size: 0.9375rem;
}

.res-info-datetime-date,
.res-info-datetime-time {
  display: inline;
}

.res-info-datetime-sep {
  margin-inline: 0.2rem;
  opacity: 0.65;
}

.res-info-stat-link {
  color: #c4b5fd;
  text-decoration: none;
}

.res-info-stat-link:hover {
  color: #ede9fe;
  text-decoration: underline;
}

@media (max-width: 900px) {
  .res-info-card-stats {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 520px) {
  .res-info-card-inner {
    padding: 0.65rem 0.7rem 0.7rem 0.85rem;
  }

  .res-info-stat {
    padding: 0.45rem 0.5rem;
  }
}
</style>

<style>
/* Light mode — soft lavender card matching POS light theme */
:root.light-theme .res-info-card {
  border-color: rgba(124, 58, 237, 0.18);
  background: linear-gradient(145deg, #faf5ff 0%, #f5f3ff 45%, #ffffff 100%);
  box-shadow:
    0 1px 0 rgba(255, 255, 255, 0.95) inset,
    0 4px 16px rgba(124, 58, 237, 0.08);
}

:root.light-theme .res-info-card--embedded {
  border-top: 1px solid rgba(124, 58, 237, 0.12);
  background: linear-gradient(180deg, #faf5ff 0%, #ffffff 100%);
}

:root.light-theme .res-info-card-accent {
  background: linear-gradient(180deg, #a78bfa, #7c3aed 60%, #6d28d9);
}

:root.light-theme .res-info-card-badge {
  color: #6d28d9;
  background: linear-gradient(135deg, #ede9fe 0%, #ddd6fe 100%);
  border-color: rgba(124, 58, 237, 0.18);
}

:root.light-theme .res-info-card-title {
  color: #4c1d95;
}

:root.light-theme .res-info-card-status {
  color: #6d28d9;
  background: rgba(167, 139, 250, 0.22);
  border-color: rgba(124, 58, 237, 0.16);
}

:root.light-theme .res-info-stat {
  background: #ffffff;
  border-color: rgba(124, 58, 237, 0.1);
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
}

:root.light-theme .res-info-stat-icon-wrap {
  background: #f3e8ff;
  color: #7c3aed;
}

:root.light-theme .res-info-stat-label {
  color: #64748b;
}

:root.light-theme .res-info-stat-value {
  color: #0f172a;
}

:root.light-theme .res-info-stat-link {
  color: #6d28d9;
}

:root.light-theme .res-info-stat-link:hover {
  color: #5b21b6;
}
</style>
