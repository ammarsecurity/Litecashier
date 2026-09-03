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
