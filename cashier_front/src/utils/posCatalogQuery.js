import { normalizeBarcodeCode } from "@/utils/barcodeScan.js";
import {
  POS_IDB_STORES,
  codeCacheKey,
  idbGet,
  idbGetAllByIndex,
  itemCacheKey,
} from "@/utils/posIdb.js";

function textOf(value) {
  return String(value || "").toLowerCase();
}

function matchesSearch(item, rawSearch) {
  const search = String(rawSearch || "").trim();
  if (!search) return true;
  const exact = search.toLowerCase();
  const name = textOf(item.name);
  const description = textOf(item.description);
  const tags = textOf(item.tags);
  const code = String(item.code || "").toLowerCase();
  if (code === exact) return true;
  if (name.indexOf(exact) !== -1) return true;
  if (description.indexOf(exact) !== -1) return true;
  if (tags.indexOf(exact) !== -1) return true;
  const extra = Array.isArray(item.extraCodes) ? item.extraCodes : [];
  return extra.some((c) => String(c || "").toLowerCase() === exact);
}

function matchesCategory(item, category) {
  const tag = String(category || "").trim();
  if (!tag) return true;
  return String(item.tags || "").trim() === tag;
}

async function pendingQtyByItem(commercialUserId, warehouseId) {
  const cid = Number(commercialUserId);
  const wid = Number(warehouseId);
  const map = {};
  if (!cid || !wid) return map;
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.pendingOrders,
    "commercialUserId",
    cid
  );
  rows.forEach((row) => {
    if (
      row.status !== "pending" &&
      row.status !== "syncing" &&
      row.status !== "failed"
    ) {
      return;
    }
    if (Number(row.warehouseId) !== wid) return;
    const lines = (row.payload && row.payload.customerOrderItem) || [];
    lines.forEach((line) => {
      const id = Number(line.itemId);
      const qty = Number(line.quantity) || 0;
      if (!id || qty <= 0) return;
      map[id] = (map[id] || 0) + qty;
    });
  });
  return map;
}

function withPendingStock(item, pendingMap) {
  if (!item || item.isNonInventory) return item;
  const reserved = pendingMap[item.id] || 0;
  return {
    ...item,
    quantity: Math.max(0, (Number(item.quantity) || 0) - reserved),
  };
}

export async function queryPosItems({
  commercialUserId,
  warehouseId,
  search,
  category,
  pageNumber,
  pageSize,
} = {}) {
  const cid = Number(commercialUserId);
  const wid = Number(warehouseId);
  if (!cid || !wid) {
    return { items: [], totalItems: 0 };
  }

  const all = await idbGetAllByIndex(POS_IDB_STORES.items, "scope", [cid, wid]);
  const pendingMap = await pendingQtyByItem(cid, wid);
  const filtered = all
    .map((item) => withPendingStock(item, pendingMap))
    .filter((item) => matchesCategory(item, category) && matchesSearch(item, search))
    .sort((a, b) => Number(b.id) - Number(a.id));

  const size = Math.max(1, Number(pageSize) || 28);
  const page = Math.max(1, Number(pageNumber) || 1);
  const start = (page - 1) * size;
  const items = filtered.slice(start, start + size).map((item) => ({
    ...item,
    imageError: false,
  }));

  return { items, totalItems: filtered.length };
}

export async function findPosItemByCode(commercialUserId, warehouseId, rawCode) {
  const cid = Number(commercialUserId);
  const wid = Number(warehouseId);
  const code = normalizeBarcodeCode(rawCode);
  if (!cid || !wid || !code) return null;

  const mapped = await idbGet(POS_IDB_STORES.itemCodes, codeCacheKey(cid, wid, code));
  const pendingMap = await pendingQtyByItem(cid, wid);
  if (mapped && mapped.itemId != null) {
    const item = await idbGet(
      POS_IDB_STORES.items,
      itemCacheKey(cid, wid, mapped.itemId)
    );
    if (item) return withPendingStock(item, pendingMap);
  }

  const scoped = await idbGetAllByIndex(POS_IDB_STORES.items, "scope", [cid, wid]);
  const needle = code.toLowerCase();
  const exact = scoped.find((item) => {
    if (String(item.code || "").toLowerCase() === needle) return true;
    const extra = Array.isArray(item.extraCodes) ? item.extraCodes : [];
    return extra.some((c) => String(c || "").toLowerCase() === needle);
  });
  return exact ? withPendingStock(exact, pendingMap) : null;
}

export async function getItemAvailableQty(commercialUserId, warehouseId, itemId) {
  const cid = Number(commercialUserId);
  const wid = Number(warehouseId);
  const id = Number(itemId);
  if (!cid || !wid || !id) return null;
  const item = await idbGet(POS_IDB_STORES.items, itemCacheKey(cid, wid, id));
  if (!item) return null;
  if (item.isNonInventory) return Number.MAX_SAFE_INTEGER;
  const pendingMap = await pendingQtyByItem(cid, wid);
  const adjusted = withPendingStock(item, pendingMap);
  return Math.max(0, Number(adjusted.quantity) || 0);
}

export async function hasPosCatalog(commercialUserId, warehouseId) {
  const cid = Number(commercialUserId);
  const wid = Number(warehouseId);
  if (!cid || !wid) return false;
  const meta = await idbGet(POS_IDB_STORES.meta, `catalog:${cid}:${wid}`);
  return !!(meta && meta.lastCatalogAt);
}
