/** Matches backend BusinessSettings:TimeZoneId (Asia/Baghdad). */
export const BUSINESS_TIME_ZONE =
  process.env.VUE_APP_TIME_ZONE || "Asia/Baghdad";

/**
 * Format a UTC (or ISO) timestamp in the business timezone for display.
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

  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: BUSINESS_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: true,
  }).formatToParts(d);

  const get = (type) => parts.find((p) => p.type === type)?.value || "";
  return `${get("year")}-${get("month")}-${get("day")} ${get("hour")}:${get("minute")}:${get("second")} ${get("dayPeriod")}`;
}
