/**
 * Display helpers: always use Western/Latin digits (0-9) for money and counts.
 */

export function formatMoney(value, options = {}) {
  const n = Number(value);
  if (!Number.isFinite(n)) return "0";
  return new Intl.NumberFormat("en-US", {
    numberingSystem: "latn",
    maximumFractionDigits: options.maximumFractionDigits ?? 2,
    minimumFractionDigits: options.minimumFractionDigits ?? 0,
    ...options,
  }).format(n);
}

/** Alias used across many views. */
export function formatPrice(value, options = {}) {
  return formatMoney(value, options);
}

/**
 * Date/time with Latin digits. Keeps Arabic month names when locale is ar.
 */
export function formatDateTimeLatn(value, locale = "ar", options = {}) {
  if (!value) return "—";
  try {
    const resolved = locale === "en" || locale === "en-GB" ? "en-GB" : "ar-IQ";
    return new Intl.DateTimeFormat(resolved, {
      numberingSystem: "latn",
      dateStyle: "medium",
      timeStyle: "short",
      ...options,
    }).format(new Date(value));
  } catch (_) {
    return String(value);
  }
}
