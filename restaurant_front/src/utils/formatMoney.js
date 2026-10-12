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

export function formatPrice(value, options = {}) {
  return formatMoney(value, options);
}
