/** Shared queue filtering + commercial user id for order-queue / public-queue. */

import { todayBusinessDateString } from "./formatBusinessDateTime.js";

export function normalizeOrderStatus(status) {
  const s = status != null ? String(status).trim() : "";
  return s || "Pending";
}

export function filterQueuePending(orders) {
  return (orders || []).filter(
    (o) => normalizeOrderStatus(o.orderStatus) === "Pending"
  );
}

export function filterQueueProcessing(orders) {
  return (orders || []).filter(
    (o) => normalizeOrderStatus(o.orderStatus) === "Processing"
  );
}

export function filterQueueReady(orders) {
  return (orders || []).filter(
    (o) => normalizeOrderStatus(o.orderStatus) === "Ready"
  );
}

/** Public display «قيد الانتظار»: Pending + Processing + Ready */
export function filterQueueActive(orders) {
  return (orders || []).filter((o) => {
    const status = normalizeOrderStatus(o.orderStatus);
    return status === "Pending" || status === "Processing" || status === "Ready";
  });
}

export function filterQueueCompleted(orders) {
  return (orders || []).filter(
    (o) => normalizeOrderStatus(o.orderStatus) === "Completed"
  );
}

export function filterQueueForAdminBoard(orders) {
  return (orders || []).filter((o) => {
    const status = normalizeOrderStatus(o.orderStatus);
    return (
      status === "Pending" ||
      status === "Processing" ||
      status === "Ready" ||
      status === "Completed"
    );
  });
}

export function todayDateParams() {
  const today = todayBusinessDateString();
  return { startDate: today, endDate: today };
}

export function buildTodayOrdersQueryParams(extra = {}) {
  const { startDate, endDate } = todayDateParams();
  const merged = {
    pageNumber: "0",
    pageSize: "100",
    startDate,
    endDate,
    ...extra,
  };
  const params = new URLSearchParams();
  Object.entries(merged).forEach(([key, value]) => {
    if (value != null && value !== "") {
      params.append(key, String(value));
    }
  });
  return params;
}

export function buildQueueDisplayQueryParams() {
  const { startDate, endDate } = todayDateParams();
  return new URLSearchParams({ startDate, endDate });
}

/**
 * Commercial account id for PublicMenu APIs and /public-queue/{id} links.
 * Commercial → own id; POS/Waiter/Manager → insertByUserId (restaurant owner).
 */
export function resolveCommercialUserIdFromStorage() {
  try {
    const role = localStorage.getItem("role");
    const info = JSON.parse(localStorage.getItem("info") || "{}");
    const id = Number(info.id ?? info.Id);
    const insertByUserId = Number(info.insertByUserId ?? info.InsertByUserId);

    if (role === "Commercial" && Number.isFinite(id) && id > 0) {
      return id;
    }
    if (Number.isFinite(insertByUserId) && insertByUserId > 0) {
      return insertByUserId;
    }
    if (Number.isFinite(id) && id > 0) {
      return id;
    }
    const fallback = Number(info.commercialUserId ?? info.CommercialUserId);
    if (Number.isFinite(fallback) && fallback > 0) {
      return fallback;
    }
  } catch {
    /* ignore */
  }
  return null;
}

/** Section keys that show pending public-order badge on /sections */
export const PUBLIC_ORDER_BADGE_SECTIONS = new Set([
  "orderQueue",
  "publicOrders",
]);

export async function fetchTodayPublicOrders(http, commercialUserId, extra = {}) {
  if (!http || !commercialUserId) return [];
  const params = buildTodayOrdersQueryParams(extra);
  const response = await http.get(
    `PublicMenu/${commercialUserId}/orders?${params.toString()}`
  );
  if (response.data?.errorStatus || response.data?.ErrorStatus) return [];
  return response.data?.data?.items || response.data?.data?.Items || [];
}

export async function fetchPendingPublicOrderCount(http, commercialUserId) {
  try {
    const orders = await fetchTodayPublicOrders(http, commercialUserId, {
      pageSize: 500,
    });
    return filterQueuePending(orders).length;
  } catch {
    return 0;
  }
}
