/** Strip scanner suffix chars and whitespace from a product code. */
export function normalizeBarcodeCode(raw) {
  return String(raw ?? "")
    .replace(/[\r\n\t\u0000-\u001F]+/g, "")
    .trim();
}

/** Gap between wedge keys — faster means hardware scanner, not manual typing. */
export const BARCODE_SCANNER_KEY_GAP_MS = 80;

/** Minimal tail after last scanner character (no Enter suffix). */
export const BARCODE_SCANNER_TAIL_MS = 35;

/** Manual typing debounce — only when gaps look like human input. */
export const BARCODE_MANUAL_DEBOUNCE_MS = 450;

export function buildGetItemsByCodeUrl(code, warehouseId) {
  const q = encodeURIComponent(normalizeBarcodeCode(code));
  if (!q) return "";
  const wh =
    warehouseId != null && warehouseId !== ""
      ? `&warehouseId=${encodeURIComponent(String(warehouseId))}`
      : "";
  return `Admin/GetItemsByCode?code=${q}${wh}`;
}

/** Normalize API item payload (camelCase / PascalCase). */
export function normalizeScannedItem(item, fallbackCode) {
  if (!item || typeof item !== "object") return null;
  const id = item.id ?? item.Id;
  if (id == null) return null;
  return {
    id,
    name: item.name ?? item.Name ?? "—",
    code: item.code ?? item.Code ?? fallbackCode,
    quantity: Number(item.quantity ?? item.Quantity) || 0,
    sellingPrice: Number(item.sellingPrice ?? item.SellingPrice) || 0,
    disCountPrice: Number(item.disCountPrice ?? item.DisCountPrice) || 0,
    wholesalePrice: Number(item.wholesalePrice ?? item.WholesalePrice) || 0,
  };
}
