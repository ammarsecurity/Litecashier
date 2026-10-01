import { HTTP } from "@/http/api.js";
import { resolveCommercialUserId } from "@/utils/publicMenu.js";
import {
  POS_IDB_STORES,
  codeCacheKey,
  createClientOrderId,
  idbDelete,
  idbGet,
  idbGetAllByIndex,
  idbPut,
  idbReplaceByIndex,
  itemCacheKey,
  scopedRecordKey,
} from "@/utils/posIdb.js";
import { hasPosCatalog } from "@/utils/posCatalogQuery.js";

const CATALOG_TIMEOUT_MS = 60000;
const ORDER_TIMEOUT_MS = 25000;

const listeners = new Set();
let flushTimer = null;
let flushing = false;
let catalogSyncing = false;
let lookupsSyncing = false;
let started = false;
let lastError = null;
let lastWarehouseId = null;
let catalogSyncQueued = null;
let lookupsQueued = false;
let ioChain = Promise.resolve();
let catalogEpoch = 0;

export function markPosSaleAccepted() {
  catalogEpoch += 1;
}

const status = {
  online: typeof navigator === "undefined" ? true : navigator.onLine,
  syncing: false,
  pendingCount: 0,
  failedCount: 0,
  lastError: null,
  lastCatalogAt: null,
};

function unwrap(response) {
  return response?.data?.data || response?.data?.Data || null;
}

function isErrorStatus(response) {
  return !!(response?.data?.errorStatus || response?.data?.ErrorStatus);
}

function notify() {
  status.syncing = catalogSyncing || lookupsSyncing || flushing;
  status.lastError = lastError;
  listeners.forEach((fn) => {
    try {
      fn({ ...status });
    } catch (err) {
      console.warn("posSync listener failed", err);
    }
  });
}

async function refreshQueueCounts(commercialUserId) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) {
    status.pendingCount = 0;
    status.failedCount = 0;
    return;
  }
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.pendingOrders,
    "commercialUserId",
    cid
  );
  status.pendingCount = rows.filter(
    (row) => row.status === "pending" || row.status === "syncing"
  ).length;
  status.failedCount = rows.filter((row) => row.status === "failed").length;
}

function normalizeItem(raw, commercialUserId, warehouseId) {
  const id = raw?.id ?? raw?.Id;
  if (id == null) return null;
  const extra = raw.extraCodes || raw.ExtraCodes || [];
  return {
    cacheKey: itemCacheKey(commercialUserId, warehouseId, id),
    commercialUserId: Number(commercialUserId),
    warehouseId: Number(warehouseId),
    id: Number(id),
    name: raw.name ?? raw.Name ?? "—",
    description: (raw.description ?? raw.Description) || "",
    image: (raw.image ?? raw.Image) || null,
    code: (raw.code ?? raw.Code) || "",
    extraCodes: Array.isArray(extra) ? extra.map((c) => String(c)) : [],
    sellingPrice: Number(raw.sellingPrice ?? raw.SellingPrice) || 0,
    disCountPrice: Number(raw.disCountPrice ?? raw.DisCountPrice) || 0,
    wholesalePrice: Number(raw.wholesalePrice ?? raw.WholesalePrice) || 0,
    quantity: Number(raw.quantity ?? raw.Quantity) || 0,
    tags: (raw.tags ?? raw.Tags) || "",
    isNonInventory: !!(raw.isNonInventory ?? raw.IsNonInventory),
  };
}

function mapScopedList(list, commercialUserId, mapFn) {
  return (Array.isArray(list) ? list : [])
    .map((row) => mapFn(row, commercialUserId))
    .filter(Boolean);
}

async function replaceScopedList(storeName, commercialUserId, records) {
  await idbReplaceByIndex(
    storeName,
    "commercialUserId",
    Number(commercialUserId),
    records
  );
}

export function getPosSyncStatus() {
  return { ...status };
}

export function subscribePosSync(listener) {
  if (typeof listener === "function") listeners.add(listener);
  listener({ ...status });
  return () => listeners.delete(listener);
}

export async function loadCachedWarehouses(commercialUserId) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return [];
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.warehouses,
    "commercialUserId",
    cid
  );
  return rows
    .map((w) => ({
      id: w.id,
      name: w.name,
      isDefault: !!w.isDefault,
    }))
    .sort((a, b) => Number(b.isDefault) - Number(a.isDefault) || a.id - b.id);
}

export async function loadCachedTags(commercialUserId) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return [];
  const rows = await idbGetAllByIndex(POS_IDB_STORES.tags, "commercialUserId", cid);
  return rows
    .map((t) => ({ id: t.id, name: t.name }))
    .sort((a, b) => Number(b.id) - Number(a.id));
}

export async function loadCachedShortcuts(commercialUserId) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return [];
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.shortcuts,
    "commercialUserId",
    cid
  );
  return rows.sort((a, b) => String(a.name || "").localeCompare(String(b.name || "")));
}

export async function loadCachedCustomers(commercialUserId) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return [];
  return idbGetAllByIndex(POS_IDB_STORES.customers, "commercialUserId", cid);
}

export async function loadCachedPrinters(commercialUserId) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return [];
  return idbGetAllByIndex(POS_IDB_STORES.printers, "commercialUserId", cid);
}

export async function loadCachedCommercialInfo(commercialUserId) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return null;
  return idbGet(POS_IDB_STORES.commercialInfo, cid);
}

export async function cacheCustomers(commercialUserId, customers) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return;
  const records = mapScopedList(customers, cid, (row, ownerId) => {
    const id = row.id ?? row.Id;
    if (id == null) return null;
    return {
      ...row,
      id,
      cacheKey: scopedRecordKey(ownerId, id),
      commercialUserId: ownerId,
    };
  });
  await replaceScopedList(POS_IDB_STORES.customers, cid, records);
}

export async function cachePrinters(commercialUserId, printers) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid) return;
  const records = mapScopedList(printers, cid, (row, ownerId) => {
    const id = row.id ?? row.Id;
    if (id == null) return null;
    return {
      ...row,
      id,
      cacheKey: scopedRecordKey(ownerId, id),
      commercialUserId: ownerId,
    };
  });
  await replaceScopedList(POS_IDB_STORES.printers, cid, records);
}

export async function cacheCommercialInfo(commercialUserId, info) {
  const cid = Number(commercialUserId || resolveCommercialUserId());
  if (!cid || !info) return;
  await idbPut(POS_IDB_STORES.commercialInfo, {
    commercialUserId: cid,
    ...info,
  });
}

export async function syncPosLookups() {
  const cid = Number(resolveCommercialUserId());
  if (!cid) return;
  if (lookupsSyncing) {
    lookupsQueued = true;
    return;
  }
  lookupsSyncing = true;
  notify();
  try {
    const [tagsRes, warehousesRes, shortcutsRes, infoRes, printersRes, customersRes] =
      await Promise.allSettled([
        HTTP.get("Admin/GetTags?pageNumber=0&pageSize=10000"),
        HTTP.get("Warehouses/ForPos"),
        HTTP.get("ShortcutItems/ForPos"),
        HTTP.get("Admin/CommercialUserInfo"),
        HTTP.get("Printers"),
        HTTP.get("Customers"),
      ]);

    if (tagsRes.status === "fulfilled" && !isErrorStatus(tagsRes.value)) {
      const page = unwrap(tagsRes.value);
      const items = page?.items || page?.Items || [];
      await replaceScopedList(
        POS_IDB_STORES.tags,
        cid,
        mapScopedList(items, cid, (row, ownerId) => {
          const id = row.id ?? row.Id;
          if (id == null) return null;
          return {
            cacheKey: scopedRecordKey(ownerId, id),
            commercialUserId: ownerId,
            id,
            name: row.name ?? row.Name ?? "",
          };
        })
      );
    }

    if (warehousesRes.status === "fulfilled" && !isErrorStatus(warehousesRes.value)) {
      const raw = unwrap(warehousesRes.value) || [];
      await replaceScopedList(
        POS_IDB_STORES.warehouses,
        cid,
        mapScopedList(raw, cid, (row, ownerId) => {
          const id = row.id ?? row.Id;
          if (id == null) return null;
          return {
            cacheKey: scopedRecordKey(ownerId, id),
            commercialUserId: ownerId,
            id,
            name: row.name ?? row.Name ?? "—",
            isDefault: !!(row.isDefault ?? row.IsDefault),
          };
        })
      );
    }

    if (shortcutsRes.status === "fulfilled" && !isErrorStatus(shortcutsRes.value)) {
      const raw = unwrap(shortcutsRes.value) || [];
      await replaceScopedList(
        POS_IDB_STORES.shortcuts,
        cid,
        mapScopedList(raw, cid, (row, ownerId) => {
          const id = row.id ?? row.Id;
          if (id == null) return null;
          return {
            cacheKey: scopedRecordKey(ownerId, id),
            commercialUserId: ownerId,
            id,
            name: row.name ?? row.Name ?? "",
            description: (row.description ?? row.Description) || "",
            sellingPrice: Number(row.sellingPrice ?? row.SellingPrice) || 0,
            wholesalePrice: Number(row.wholesalePrice ?? row.WholesalePrice) || 0,
            isNonInventory: true,
            quantity: 1,
          };
        })
      );
    }

    if (infoRes.status === "fulfilled" && !isErrorStatus(infoRes.value)) {
      const info = unwrap(infoRes.value);
      if (info) await cacheCommercialInfo(cid, info);
    }

    if (printersRes.status === "fulfilled" && !isErrorStatus(printersRes.value)) {
      const raw = unwrap(printersRes.value) || [];
      await cachePrinters(cid, raw);
    }

    if (customersRes.status === "fulfilled" && !isErrorStatus(customersRes.value)) {
      const raw = unwrap(customersRes.value) || [];
      await cacheCustomers(cid, raw);
    }

    lastError = null;
  } catch (err) {
    lastError = err?.message || "lookup sync failed";
  } finally {
    lookupsSyncing = false;
    notify();
    if (lookupsQueued) {
      lookupsQueued = false;
      syncPosLookups();
    }
  }
}

function enqueueIo(task) {
  const run = ioChain.then(task, task);
  ioChain = run.then(
    () => undefined,
    () => undefined
  );
  return run;
}

function soldMarkerKey(clientOrderId) {
  return `sold:${clientOrderId}`;
}

async function runCatalogSync(warehouseId) {
  const cid = Number(resolveCommercialUserId());
  const wid = Number(warehouseId);
  if (wid) lastWarehouseId = wid;
  if (!cid || !wid) return;
  const epochAtStart = catalogEpoch;
  catalogSyncing = true;
  notify();
  try {
    const response = await HTTP.get(`Admin/GetItemsForPos?warehouseId=${wid}`, {
      timeout: CATALOG_TIMEOUT_MS,
    });
    if (epochAtStart !== catalogEpoch) return;
    if (isErrorStatus(response)) {
      throw new Error(response?.data?.message || "catalog sync failed");
    }
    const payload = unwrap(response) || {};
    const rawItems = payload.items || payload.Items || [];
    const items = rawItems
      .map((row) => normalizeItem(row, cid, wid))
      .filter(Boolean);

    const codeRows = [];
    items.forEach((item) => {
      const codes = [item.code].concat(item.extraCodes || []).filter(Boolean);
      const unique = Array.from(new Set(codes.map((c) => String(c).trim()).filter(Boolean)));
      unique.forEach((code) => {
        codeRows.push({
          cacheKey: codeCacheKey(cid, wid, code),
          commercialUserId: cid,
          warehouseId: wid,
          code,
          itemId: item.id,
        });
      });
    });

    if (epochAtStart !== catalogEpoch) return;

    await idbReplaceByIndex(POS_IDB_STORES.items, "scope", [cid, wid], items);
    await idbReplaceByIndex(POS_IDB_STORES.itemCodes, "scope", [cid, wid], codeRows);

    const now = Date.now();
    await idbPut(POS_IDB_STORES.meta, {
      key: `catalog:${cid}:${wid}`,
      lastCatalogAt: now,
    });
    status.lastCatalogAt = now;
    lastError = null;
  } catch (err) {
    lastError = err?.message || "catalog sync failed";
  } finally {
    catalogSyncing = false;
    notify();
  }
}

export function syncPosCatalog(warehouseId) {
  const wid = Number(warehouseId);
  if (!wid) return Promise.resolve();
  lastWarehouseId = wid;
  catalogSyncQueued = wid;
  return enqueueIo(async () => {
    const target = catalogSyncQueued;
    catalogSyncQueued = null;
    if (target) await runCatalogSync(target);
  });
}

function isRetryableOrderError(error) {
  if (!error) return true;
  if (!error.response) return true;
  const statusCode = error.response.status;
  if (statusCode === 401 || statusCode === 403) return true;
  if (statusCode >= 500) return true;
  return false;
}

function orderApiMessage(error) {
  return (
    error?.response?.data?.message ||
    error?.response?.data?.Message ||
    error?.message ||
    "sync failed"
  );
}

async function deductCatalogForFlushedOrder(row) {
  const cid = Number(row.commercialUserId);
  const wid = Number(row.warehouseId);
  const clientOrderId = row.clientOrderId;
  const lines = (row.payload && row.payload.customerOrderItem) || [];
  if (!cid || !wid || !lines.length) return;
  if (clientOrderId) {
    const already = await idbGet(POS_IDB_STORES.meta, soldMarkerKey(clientOrderId));
    if (already) return;
  }
  for (let i = 0; i < lines.length; i += 1) {
    const line = lines[i];
    const itemId = Number(line.itemId);
    const qty = Number(line.quantity) || 0;
    if (!itemId || qty <= 0) continue;
    const item = await idbGet(POS_IDB_STORES.items, itemCacheKey(cid, wid, itemId));
    if (!item || item.isNonInventory) continue;
    const nextQty = Math.max(0, (Number(item.quantity) || 0) - qty);
    await idbPut(POS_IDB_STORES.items, { ...item, quantity: nextQty });
  }
  if (clientOrderId) {
    await idbPut(POS_IDB_STORES.meta, {
      key: soldMarkerKey(clientOrderId),
      at: Date.now(),
    });
  }
}

export function applySoldPayloadToCatalog(payload, warehouseId) {
  return enqueueIo(async () => {
    const cid = Number(resolveCommercialUserId());
    const wid = Number(warehouseId) || Number(payload && payload.warehouseId);
    await deductCatalogForFlushedOrder({
      commercialUserId: cid,
      warehouseId: wid,
      payload,
      clientOrderId: payload && payload.clientOrderId,
    });
  });
}

async function recoverStuckSyncingOrders(commercialUserId) {
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.pendingOrders,
    "commercialUserId",
    Number(commercialUserId)
  );
  for (let i = 0; i < rows.length; i += 1) {
    const row = rows[i];
    if (row.status !== "syncing") continue;
    await idbPut(POS_IDB_STORES.pendingOrders, {
      ...row,
      status: "pending",
    });
  }
}

export async function enqueuePosOrder({ payload, warehouseId, soldAt }) {
  const cid = Number(resolveCommercialUserId());
  const clientOrderId = payload.clientOrderId || createClientOrderId();
  const record = {
    clientOrderId,
    commercialUserId: cid,
    warehouseId: Number(warehouseId) || Number(payload.warehouseId) || null,
    payload: { ...payload, clientOrderId },
    status: "pending",
    attempts: 0,
    lastError: null,
    createdAt: Date.now(),
    soldAt: soldAt || new Date().toISOString(),
  };
  await idbPut(POS_IDB_STORES.pendingOrders, record);
  await refreshQueueCounts(cid);
  notify();
  return record;
}

export function flushPendingOrders() {
  return enqueueIo(() => runFlush());
}

async function runFlush() {
  if (typeof navigator !== "undefined" && !navigator.onLine) return;
  const cid = Number(resolveCommercialUserId());
  if (!cid) return;

  flushing = true;
  notify();
  try {
    await recoverStuckSyncingOrders(cid);
    const rows = await idbGetAllByIndex(
      POS_IDB_STORES.pendingOrders,
      "commercialUserId",
      cid
    );
    const queue = rows
      .filter((row) => row.status === "pending")
      .sort((a, b) => (a.createdAt || 0) - (b.createdAt || 0));

    for (const row of queue) {
      if (typeof navigator !== "undefined" && !navigator.onLine) break;
      const next = {
        ...row,
        status: "syncing",
        attempts: (Number(row.attempts) || 0) + 1,
      };
      await idbPut(POS_IDB_STORES.pendingOrders, next);
      try {
        const stillQueued = await idbGet(
          POS_IDB_STORES.pendingOrders,
          next.clientOrderId
        );
        if (!stillQueued) continue;

        const response = await HTTP.post("Admin/AddOrder", next.payload, {
          timeout: ORDER_TIMEOUT_MS,
        });
        if (isErrorStatus(response)) {
          throw Object.assign(new Error(response?.data?.message || "AddOrder failed"), {
            response,
          });
        }
        markPosSaleAccepted();
        await deductCatalogForFlushedOrder(next);
        const afterOk = await idbGet(
          POS_IDB_STORES.pendingOrders,
          next.clientOrderId
        );
        if (afterOk) {
          await idbDelete(POS_IDB_STORES.pendingOrders, next.clientOrderId);
        }
        lastError = null;
      } catch (err) {
        const statusCode = err && err.response && err.response.status;
        const message = orderApiMessage(err);
        const inventory = String(message).indexOf("insufficientInventory") === 0;
        const retryable = !inventory && isRetryableOrderError(err);
        const stillThere = await idbGet(
          POS_IDB_STORES.pendingOrders,
          next.clientOrderId
        );
        if (stillThere) {
          await idbPut(POS_IDB_STORES.pendingOrders, {
            ...next,
            status: retryable ? "pending" : "failed",
            lastError: message,
          });
        }
        lastError = message;
        if (statusCode === 401 || statusCode === 403) {
          break;
        }
      }
    }
  } finally {
    flushing = false;
    await refreshQueueCounts(cid);
    notify();
  }
}

export async function retryFailedOrders() {
  const cid = Number(resolveCommercialUserId());
  if (!cid) return;
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.pendingOrders,
    "commercialUserId",
    cid
  );
  for (const row of rows) {
    if (row.status !== "failed") continue;
    await idbPut(POS_IDB_STORES.pendingOrders, {
      ...row,
      status: "pending",
      attempts: 0,
      lastError: null,
    });
  }
  await refreshQueueCounts(cid);
  notify();
  await flushPendingOrders();
}

function matchesQueueFilter(row, filter) {
  const status = String(row?.status || "");
  if (filter === "all") return true;
  if (filter === "failed") return status === "failed";
  // pending = waiting to sync (pending + syncing)
  return status === "pending" || status === "syncing";
}

async function enrichQueuedOrder(row) {
  const payload = row?.payload || {};
  const lines = Array.isArray(payload.customerOrderItem)
    ? payload.customerOrderItem
    : [];
  const cid = Number(row.commercialUserId);
  const wid = Number(row.warehouseId || payload.warehouseId) || null;
  const isWholesale = !!payload.isWholesale;
  const enrichedLines = [];
  let shortcutsCache = null;

  async function resolveItem(itemId) {
    if (!cid || !itemId) return null;
    if (wid) {
      try {
        const fromCatalog = await idbGet(
          POS_IDB_STORES.items,
          itemCacheKey(cid, wid, itemId)
        );
        if (fromCatalog) return fromCatalog;
      } catch (_) {
        /* ignore */
      }
    }
    if (!shortcutsCache) {
      try {
        shortcutsCache = await idbGetAllByIndex(
          POS_IDB_STORES.shortcuts,
          "commercialUserId",
          cid
        );
      } catch (_) {
        shortcutsCache = [];
      }
    }
    return shortcutsCache.find((s) => Number(s.id) === itemId) || null;
  }

  for (let i = 0; i < lines.length; i += 1) {
    const line = lines[i] || {};
    const itemId = Number(line.itemId) || 0;
    const qty = Math.max(0, Number(line.quantity) || 0);
    const item = await resolveItem(itemId);
    const unitPrice = item
      ? isWholesale
        ? Number(item.wholesalePrice) > 0
          ? Number(item.wholesalePrice)
          : Number(item.sellingPrice) || 0
        : Number(item.disCountPrice) > 0 &&
          Number(item.disCountPrice) < Number(item.sellingPrice)
        ? Number(item.disCountPrice)
        : Number(item.sellingPrice) || 0
      : 0;
    enrichedLines.push({
      itemId,
      quantity: qty,
      notes: line.notes || null,
      name: item?.name || `#${itemId || "—"}`,
      code: item?.code || "",
      unitPrice,
      lineTotal: unitPrice * qty,
      fromCache: !!item,
    });
  }

  const cachedSubtotal = enrichedLines.reduce((sum, l) => sum + l.lineTotal, 0);
  const payloadSubtotal = Number(payload.orderSubTotal);
  const payloadTotal = Number(payload.orderTotalAfterDiscount);
  const discountAmount = Number(payload.discountAmount) || 0;

  return {
    clientOrderId: row.clientOrderId,
    commercialUserId: cid,
    warehouseId: wid,
    status: row.status || "pending",
    attempts: Number(row.attempts) || 0,
    lastError: row.lastError || null,
    createdAt: row.createdAt || null,
    soldAt: row.soldAt || payload.soldAt || null,
    orderCode: payload.orderCode || "—",
    paymentMethod: payload.paymentMethod || "Cash",
    isWholesale,
    notes: payload.notes || "",
    discountAmount,
    discountType: payload.discountType || null,
    discountValue: payload.discountValue ?? null,
    orderSubTotal: Number.isFinite(payloadSubtotal)
      ? payloadSubtotal
      : cachedSubtotal,
    orderTotal: Number.isFinite(payloadTotal)
      ? payloadTotal
      : Math.max(0, cachedSubtotal - discountAmount),
    lineCount: enrichedLines.length,
    itemQty: enrichedLines.reduce((sum, l) => sum + l.quantity, 0),
    lines: enrichedLines,
  };
}

/**
 * List offline/queued invoices awaiting sync.
 * @param {{ filter?: 'pending'|'failed'|'all' }} [options]
 */
export async function listQueuedOrders({ filter = "all" } = {}) {
  const cid = Number(resolveCommercialUserId());
  if (!cid) return [];
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.pendingOrders,
    "commercialUserId",
    cid
  );
  const filtered = rows
    .filter((row) => matchesQueueFilter(row, filter))
    .sort((a, b) => (b.createdAt || 0) - (a.createdAt || 0));
  const out = [];
  for (let i = 0; i < filtered.length; i += 1) {
    out.push(await enrichQueuedOrder(filtered[i]));
  }
  return out;
}

/**
 * Remove queued invoices (cancel sync). Skips rows currently mid-flight
 * unless force is true.
 * @param {string[]|null} clientOrderIds null/empty = all matching filter
 * @param {{ filter?: string, force?: boolean }} [options]
 */
export async function clearQueuedOrders(
  clientOrderIds = null,
  { filter = "all", force = false } = {}
) {
  const cid = Number(resolveCommercialUserId());
  if (!cid) return { removed: 0, skipped: 0 };
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.pendingOrders,
    "commercialUserId",
    cid
  );
  const idSet =
    Array.isArray(clientOrderIds) && clientOrderIds.length
      ? new Set(clientOrderIds.map(String))
      : null;
  let removed = 0;
  let skipped = 0;
  for (const row of rows) {
    if (idSet && !idSet.has(String(row.clientOrderId))) continue;
    if (!idSet && !matchesQueueFilter(row, filter)) continue;
    if (row.status === "syncing" && !force) {
      skipped += 1;
      continue;
    }
    await idbDelete(POS_IDB_STORES.pendingOrders, row.clientOrderId);
    removed += 1;
  }
  await refreshQueueCounts(cid);
  notify();
  return { removed, skipped };
}

/**
 * Re-queue selected (or all failed) invoices and flush.
 * @param {string[]|null} clientOrderIds null = all failed/pending
 */
export async function retryQueuedOrders(clientOrderIds = null) {
  const cid = Number(resolveCommercialUserId());
  if (!cid) return { updated: 0 };
  const rows = await idbGetAllByIndex(
    POS_IDB_STORES.pendingOrders,
    "commercialUserId",
    cid
  );
  const idSet =
    Array.isArray(clientOrderIds) && clientOrderIds.length
      ? new Set(clientOrderIds.map(String))
      : null;
  let updated = 0;
  for (const row of rows) {
    if (idSet && !idSet.has(String(row.clientOrderId))) continue;
    if (!idSet && row.status !== "failed" && row.status !== "pending") continue;
    if (row.status === "syncing") continue;
    await idbPut(POS_IDB_STORES.pendingOrders, {
      ...row,
      status: "pending",
      attempts: idSet ? Number(row.attempts) || 0 : 0,
      lastError: null,
    });
    updated += 1;
  }
  await refreshQueueCounts(cid);
  notify();
  await flushPendingOrders();
  return { updated };
}

export async function syncPosNow(warehouseId) {
  const wid = Number(warehouseId || lastWarehouseId);
  await Promise.all([
    syncPosLookups(),
    wid ? syncPosCatalog(wid) : Promise.resolve(),
  ]);
  await flushPendingOrders();
}

function scheduleFlush(delayMs) {
  clearTimeout(flushTimer);
  flushTimer = setTimeout(() => {
    flushPendingOrders();
  }, delayMs);
}

export function startPosSyncRuntime() {
  if (started || typeof window === "undefined") return;
  started = true;
  status.online = navigator.onLine;
  notify();

  window.addEventListener("online", () => {
    status.online = true;
    notify();
    syncPosNow();
  });
  window.addEventListener("offline", () => {
    status.online = false;
    notify();
  });
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") {
      scheduleFlush(300);
    }
  });

  refreshQueueCounts().then(notify);
  scheduleFlush(800);
}

export { createClientOrderId, hasPosCatalog };
