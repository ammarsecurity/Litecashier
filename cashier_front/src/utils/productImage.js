import defaultProductImage from "@/assets/JSGOWBameP9oHllllPBZ0O838AJflZHVMMw5wzx7.jpg";

export const DEFAULT_PRODUCT_IMAGE = defaultProductImage;

export function productImageSrc(image, imageError = false) {
  if (!imageError && image) {
    return image;
  }
  return DEFAULT_PRODUCT_IMAGE;
}

export function onProductImageError(item) {
  if (!item || item.imageError) return;
  item.imageError = true;
}
