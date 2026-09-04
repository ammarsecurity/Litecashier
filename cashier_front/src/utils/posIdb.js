const DB_NAME = "litecashier-pos";
const DB_VERSION = 1;

export const POS_IDB_STORES = {
  meta: "meta",
  items: "items",
  itemCodes: "itemCodes",
  tags: "tags",
  warehouses: "warehouses",
  shortcuts: "shortcuts",
  customers: "customers",
  printers: "printers",
  commercialInfo: "commercialInfo",
  pendingOrders: "pendingOrders",
};

let dbPromise = null;

function requestToPromise(request) {
  return new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error || new Error("IndexedDB request failed"));
  });
}

function transactionDone(tx) {
  return new Promise((resolve, reject) => {
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error || new Error("IndexedDB transaction failed"));
    tx.onabort = () => reject(tx.error || new Error("IndexedDB transaction aborted"));
  });
}

export function createClientOrderId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/x|y/g, (ch) => {
    const r = (Math.random() * 16) | 0;
    const v = ch === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export function itemCacheKey(commercialUserId, warehouseId, itemId) {
  return `${commercialUserId}|${warehouseId}|${itemId}`;
}

export function codeCacheKey(commercialUserId, warehouseId, code) {
  return `${commercialUserId}|${warehouseId}|${String(code || "").trim().toLowerCase()}`;
}

export function scopedRecordKey(commercialUserId, id) {
  return `${commercialUserId}|${id}`;
}

export function openPosIdb() {
  if (typeof indexedDB === "undefined") {
    return Promise.reject(new Error("IndexedDB is not available"));
  }
  if (dbPromise) return dbPromise;

  dbPromise = new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onupgradeneeded = () => {
      const db = request.result;

      if (!db.objectStoreNames.contains(POS_IDB_STORES.meta)) {
        db.createObjectStore(POS_IDB_STORES.meta, { keyPath: "key" });
      }

      if (!db.objectStoreNames.contains(POS_IDB_STORES.items)) {
        const items = db.createObjectStore(POS_IDB_STORES.items, { keyPath: "cacheKey" });
        items.createIndex("scope", ["commercialUserId", "warehouseId"], { unique: false });
        items.createIndex("itemId", "id", { unique: false });
        items.createIndex("code", "code", { unique: false });
      }

      if (!db.objectStoreNames.contains(POS_IDB_STORES.itemCodes)) {
        const codes = db.createObjectStore(POS_IDB_STORES.itemCodes, { keyPath: "cacheKey" });
        codes.createIndex("scope", ["commercialUserId", "warehouseId"], { unique: false });
        codes.createIndex("code", "code", { unique: false });
      }

      ["tags", "warehouses", "shortcuts", "customers", "printers"].forEach((name) => {
        if (!db.objectStoreNames.contains(name)) {
          const store = db.createObjectStore(name, { keyPath: "cacheKey" });
          store.createIndex("commercialUserId", "commercialUserId", { unique: false });
        }
      });

      if (!db.objectStoreNames.contains(POS_IDB_STORES.commercialInfo)) {
        db.createObjectStore(POS_IDB_STORES.commercialInfo, { keyPath: "commercialUserId" });
      }

      if (!db.objectStoreNames.contains(POS_IDB_STORES.pendingOrders)) {
        const pending = db.createObjectStore(POS_IDB_STORES.pendingOrders, {
          keyPath: "clientOrderId",
        });
        pending.createIndex("commercialUserId", "commercialUserId", { unique: false });
        pending.createIndex("status", "status", { unique: false });
      }
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => {
      dbPromise = null;
      reject(request.error || new Error("Failed to open IndexedDB"));
    };
  });

  return dbPromise;
}

export async function idbGet(storeName, key) {
  const db = await openPosIdb();
  const tx = db.transaction(storeName, "readonly");
  const result = await requestToPromise(tx.objectStore(storeName).get(key));
  await transactionDone(tx);
  return result || null;
}

export async function idbGetAll(storeName) {
  const db = await openPosIdb();
  const tx = db.transaction(storeName, "readonly");
  const result = await requestToPromise(tx.objectStore(storeName).getAll());
  await transactionDone(tx);
  return Array.isArray(result) ? result : [];
}

export async function idbGetAllByIndex(storeName, indexName, query) {
  const db = await openPosIdb();
  const tx = db.transaction(storeName, "readonly");
  const index = tx.objectStore(storeName).index(indexName);
  const result = await requestToPromise(index.getAll(query));
  await transactionDone(tx);
  return Array.isArray(result) ? result : [];
}

export async function idbPut(storeName, value) {
  const db = await openPosIdb();
  const tx = db.transaction(storeName, "readwrite");
  tx.objectStore(storeName).put(value);
  await transactionDone(tx);
}

export async function idbDelete(storeName, key) {
  const db = await openPosIdb();
  const tx = db.transaction(storeName, "readwrite");
  tx.objectStore(storeName).delete(key);
  await transactionDone(tx);
}

export async function idbReplaceByIndex(storeName, indexName, query, records) {
  const db = await openPosIdb();
  const tx = db.transaction(storeName, "readwrite");
  const store = tx.objectStore(storeName);
  const index = store.index(indexName);
  const existing = await requestToPromise(index.getAll(query));
  (existing || []).forEach((row) => {
    if (row && row.cacheKey != null) store.delete(row.cacheKey);
  });
  (records || []).forEach((row) => store.put(row));
  await transactionDone(tx);
}

export async function idbCountByIndex(storeName, indexName, query) {
  const db = await openPosIdb();
  const tx = db.transaction(storeName, "readonly");
  const count = await requestToPromise(tx.objectStore(storeName).index(indexName).count(query));
  await transactionDone(tx);
  return Number(count) || 0;
}
