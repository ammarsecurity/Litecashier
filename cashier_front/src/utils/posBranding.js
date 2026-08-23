import { resolveAbsoluteAssetUrl } from "@/utils/apiBase.js";
import { applyDefaultProductImage } from "@/utils/productImage.js";

const WATERMARK_KEY = "posCartWatermarkLogo";
const WATERMARK_OPACITY_KEY = "posCartWatermarkOpacity";

export function clampWatermarkOpacity(value) {
  const n = Number(value);
  if (!Number.isFinite(n) || n <= 0) return 70;
  return Math.min(100, Math.max(20, Math.round(n)));
}

export function getStoredCartWatermark() {
  try {
    return localStorage.getItem(WATERMARK_KEY) || "";
  } catch (_) {
    return "";
  }
}

export function getStoredCartWatermarkOpacity() {
  try {
    return clampWatermarkOpacity(localStorage.getItem(WATERMARK_OPACITY_KEY));
  } catch (_) {
    return 18;
  }
}

export function parseCommercialBranding(d) {
  if (!d) {
    return {
      cartWatermarkLogo: null,
      cartWatermarkOpacity: 18,
      defaultProductImage: null,
    };
  }
  return {
    cartWatermarkLogo:
      resolveAbsoluteAssetUrl(d.cartWatermarkLogo || d.CartWatermarkLogo) || null,
    cartWatermarkOpacity: clampWatermarkOpacity(
      d.cartWatermarkOpacity ?? d.CartWatermarkOpacity
    ),
    defaultProductImage:
      resolveAbsoluteAssetUrl(d.defaultProductImage || d.DefaultProductImage) || null,
  };
}

export function applyCommercialBranding(d) {
  const branding = parseCommercialBranding(d);
  applyDefaultProductImage(branding.defaultProductImage);
  try {
    if (branding.cartWatermarkLogo) {
      localStorage.setItem(WATERMARK_KEY, branding.cartWatermarkLogo);
    } else {
      localStorage.removeItem(WATERMARK_KEY);
    }
    localStorage.setItem(WATERMARK_OPACITY_KEY, String(branding.cartWatermarkOpacity));
  } catch (_) {
    /* ignore */
  }
  return branding;
}
