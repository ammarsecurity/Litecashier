/** Matches backend BusinessSettings:TimeZoneId (Asia/Baghdad). */
export const BUSINESS_TIME_ZONE =
  process.env.VUE_APP_TIME_ZONE || "Asia/Baghdad";

/** Today's calendar date in business TZ as YYYY-MM-DD (for API date filters). */
export function todayBusinessDateString() {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: BUSINESS_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date());

  const get = (type) => parts.find((p) => p.type === type)?.value || "";
  return `${get("year")}-${get("month")}-${get("day")}`;
}

/** YYYY-MM-DD in business TZ for a given timestamp (date-filter comparisons). */
export function businessDateStringFrom(dateTime) {
  if (dateTime == null || dateTime === "") return "";

  const d = new Date(dateTime);
  if (Number.isNaN(d.getTime())) {
    const s = String(dateTime);
    return (s.split("T")[0] || s.split(" ")[0] || s).trim();
  }

  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: BUSINESS_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(d);

  const get = (type) => parts.find((p) => p.type === type)?.value || "";
  return `${get("year")}-${get("month")}-${get("day")}`;
}

/** Display time only (12-hour) in business TZ. */
export function formatBusinessTime(dateTime) {
  if (dateTime == null || dateTime === "") return "";

  const timeFormatOptions = {
    timeZone: BUSINESS_TIME_ZONE,
    hour: "2-digit",
    minute: "2-digit",
    hour12: true,
  };

  const d = new Date(dateTime);
  if (Number.isNaN(d.getTime())) {
    const s = String(dateTime);
    const timePart = s.includes("T") ? s.split("T")[1] : s.split(" ")[1];
    if (!timePart) return "";
    const [hours, minutes] = timePart.split(":");
    const parsed = new Date();
    parsed.setHours(Number(hours) || 0, Number(minutes) || 0, 0, 0);
    return new Intl.DateTimeFormat("ar-IQ", timeFormatOptions).format(parsed);
  }

  return new Intl.DateTimeFormat("ar-IQ", timeFormatOptions).format(d);
}

/** Display date only (no time) in business TZ. */
export function formatBusinessDate(dateTime) {
  if (dateTime == null || dateTime === "") return "";

  const d = new Date(dateTime);
  if (Number.isNaN(d.getTime())) {
    return businessDateStringFrom(dateTime);
  }

  return new Intl.DateTimeFormat("ar-IQ", {
    timeZone: BUSINESS_TIME_ZONE,
    year: "numeric",
    month: "long",
    day: "numeric",
  }).format(d);
}

/**
 * Format a UTC (or ISO) timestamp in the business timezone for display.
 * Returns "YYYY-MM-DD HH:mm:ss" so report dates align with date-filter inputs.
 */
export function formatBusinessDateTime(dateTime) {
  if (dateTime == null || dateTime === "") return "";

  const d = new Date(dateTime);
  if (Number.isNaN(d.getTime())) {
    const s = String(dateTime);
    if (s.includes("T")) {
      const [date, timePart] = s.split("T");
      const time = timePart ? timePart.split(".")[0] : "";
      return time ? `${date} ${time}` : date;
    }
    return (s.split(" ")[0] || s).trim();
  }

  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: BUSINESS_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  }).formatToParts(d);

  const get = (type) => parts.find((p) => p.type === type)?.value || "";
  return `${get("year")}-${get("month")}-${get("day")} ${get("hour")}:${get("minute")}:${get("second")}`;
}
