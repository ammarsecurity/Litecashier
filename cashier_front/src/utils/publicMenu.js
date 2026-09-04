export function resolveCommercialUserId() {
  try {
    const info = JSON.parse(localStorage.getItem("info") || "{}");
    const role = localStorage.getItem("role");
    if (role === "Commercial") {
      return Number(info.id ?? info.Id) || null;
    }
    const parent = info.insertByUserId ?? info.InsertByUserId ?? info.commercialUserId;
    return Number(parent ?? info.id ?? info.Id) || null;
  } catch {
    return null;
  }
}

export function publicMenuUrl(commercialUserId) {
  const origin = typeof window !== "undefined" ? window.location.origin : "";
  const base = (process.env.BASE_URL || "/").replace(/\/?$/, "/");
  return `${origin}${base}menu/${commercialUserId}`;
}

export function formatMenuPrice(value) {
  const n = Number(value) || 0;
  return n.toLocaleString("en-US", { minimumFractionDigits: 0, maximumFractionDigits: 2 });
}

export function itemUnitPrice(item) {
  const sell = Number(item?.sellingPrice ?? item?.SellingPrice ?? 0);
  const disc = Number(item?.discountPrice ?? item?.DiscountPrice ?? 0);
  if (disc > 0 && disc < sell) return disc;
  return sell;
}

export function itemDiscountPercent(item) {
  const sell = Number(item?.sellingPrice ?? item?.SellingPrice ?? 0);
  const disc = Number(item?.discountPrice ?? item?.DiscountPrice ?? 0);
  if (disc <= 0 || disc >= sell) return 0;
  return Math.round(((sell - disc) / sell) * 100);
}

const IRAQI_PHONE_RE = /^07[4578]\d{8}$/;

export function normalizeIraqiPhone(raw) {
  let digits = String(raw || "").replace(/\D/g, "");
  if (digits.startsWith("9640")) digits = digits.slice(3);
  else if (digits.startsWith("964")) {
    digits = digits.slice(3);
    if (digits.length === 10 && /^7[4578]/.test(digits)) digits = `0${digits}`;
  } else if (digits.length === 10 && /^7[4578]/.test(digits)) {
    digits = `0${digits}`;
  }
  return digits;
}

export function isValidIraqiPhone(raw) {
  return IRAQI_PHONE_RE.test(normalizeIraqiPhone(raw));
}

export function normalizeCustomerName(raw) {
  return String(raw || "").trim().replace(/\s+/g, " ");
}

export function isValidCustomerName(raw) {
  const name = normalizeCustomerName(raw);
  if (name.length < 2 || name.length > 120) return false;
  if (/\d/.test(name)) return false;
  const letters = name.match(/[\u0600-\u06FFa-zA-Z\u0750-\u077F]/g) || [];
  return letters.length >= 2;
}

const LAST_ORDER_KEY = "publicMenuLastOrder";

export function saveLastPublicOrder({ commercialUserId, orderCode, phone }) {
  try {
    localStorage.setItem(
      LAST_ORDER_KEY,
      JSON.stringify({
        commercialUserId: Number(commercialUserId),
        orderCode: String(orderCode || ""),
        phone: normalizeIraqiPhone(phone),
      })
    );
  } catch {
    /* ignore */
  }
}

export function loadLastPublicOrder(commercialUserId) {
  try {
    const raw = JSON.parse(localStorage.getItem(LAST_ORDER_KEY) || "null");
    if (!raw || Number(raw.commercialUserId) !== Number(commercialUserId)) return null;
    if (!raw.orderCode || !raw.phone) return null;
    return raw;
  } catch {
    return null;
  }
}
