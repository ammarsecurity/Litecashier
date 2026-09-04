import Vue from "vue";

/**
 * Stable public URL (copied to dist root via /public).
 * Avoids hashed webpack asset paths that 404 when the SPA folder layout differs.
 */
function resolvePublicAsset(fileName) {
  const base = (process.env.BASE_URL || "/").replace(/\/?$/, "/");
  return `${base}${fileName}`;
}

const DEFAULT_PRODUCT_IMAGE_KEY = "posDefaultProductImage";

/** Built-in fallback used when the store has not uploaded a custom default. */
export const BUILTIN_DEFAULT_PRODUCT_IMAGE = resolvePublicAsset("default-product.png");

/** Brand logo used as fallback when a product has no (usable) image. */
export const DEFAULT_PRODUCT_IMAGE = BUILTIN_DEFAULT_PRODUCT_IMAGE;
export const BRAND_LOGO = resolvePublicAsset("logo.png");

export function getDefaultProductImage() {
  try {
    const stored = localStorage.getItem(DEFAULT_PRODUCT_IMAGE_KEY);
    if (stored && stored.trim()) return stored.trim();
  } catch (_) {
    /* ignore */
  }
  return BUILTIN_DEFAULT_PRODUCT_IMAGE;
}

export function applyDefaultProductImage(url) {
  try {
    const value = url == null ? "" : String(url).trim();
    if (value) localStorage.setItem(DEFAULT_PRODUCT_IMAGE_KEY, value);
    else localStorage.removeItem(DEFAULT_PRODUCT_IMAGE_KEY);
  } catch (_) {
    /* ignore */
  }
}

export function hasRealProductImage(image) {
  const value = String(image || "").trim();
  if (!value) return false;
  if (value === "-" || value === "null" || value === "undefined") return false;
  // Legacy cashier placeholder path from older builds
  if (/JSGOWBame/i.test(value)) return false;
  if (/default-product(?:b)?\.png(?:\?|$)/i.test(value)) return false;
  return true;
}

export function productImageSrc(image, imageError = false) {
  if (!imageError && hasRealProductImage(image)) {
    return String(image).trim();
  }
  return getDefaultProductImage();
}

export function isProductImageFallback(image, imageError = false) {
  return !!imageError || !hasRealProductImage(image);
}

export function onProductImageError(item) {
  if (!item || item.imageError) return;
  // Vue 2: ensure reactivity when imageError was not predefined
  Vue.set(item, "imageError", true);
}
