import brandLogo from "@/assets/logo.png";

/** Brand logo used as fallback when a product has no image. */
export const DEFAULT_PRODUCT_IMAGE = brandLogo;
export const BRAND_LOGO = brandLogo;

export function productImageSrc(image, imageError = false) {
  if (!imageError && image) {
    return image;
  }
  return DEFAULT_PRODUCT_IMAGE;
}

export function isProductImageFallback(image, imageError = false) {
  return !!imageError || !image;
}

export function onProductImageError(item) {
  if (!item || item.imageError) return;
  item.imageError = true;
}
