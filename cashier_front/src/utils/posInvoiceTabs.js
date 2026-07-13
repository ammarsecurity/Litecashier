/**
 * Multi-invoice tab helpers for retail POS.
 * Each tab holds a cart snapshot; catalog/search/printers stay global.
 */

export const POS_INVOICE_TABS_MAX = 8;
export const POS_INVOICE_TABS_STORAGE_PREFIX = "posInvoiceTabs_v1";

export function getPosInvoiceTabsStorageKey(userInfo) {
  const userId =
    userInfo?.id ?? userInfo?.Id ?? userInfo?.userId ?? userInfo?.UserId ?? "anon";
  return `${POS_INVOICE_TABS_STORAGE_PREFIX}_${userId}`;
}

export function createEmptyInvoiceTab(index = 1, defaults = {}) {
  const paymentMethod =
    defaults.paymentMethod &&
    ["Cash", "Card", "Credit"].includes(defaults.paymentMethod)
      ? defaults.paymentMethod
      : "Cash";

  return {
    id: `inv_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
    index,
    carditems: [],
    isWholesale: false,
    orderForSend: {
      orderCode: "",
      paymentMethod,
      customerOrderItem: [],
      orderType: "Takeaway",
      notes: "",
      creditCustomerId: null,
    },
    orderDiscountType: "amount",
    orderDiscountValue: null,
    changeCalcOpen: false,
    customerPaidAmount: null,
  };
}

export function cloneCartItems(items) {
  try {
    return JSON.parse(JSON.stringify(Array.isArray(items) ? items : []));
  } catch (error) {
    return [];
  }
}

export function snapshotFromPos(vm) {
  if (!vm) return createEmptyInvoiceTab(1);
  return {
    id: vm.activeInvoiceTabId || createEmptyInvoiceTab(1).id,
    index: Number(vm.activeInvoiceTabIndex) || 1,
    carditems: cloneCartItems(vm.carditems),
    isWholesale: !!vm.isWholesale,
    orderForSend: {
      orderCode: vm.orderForSend?.orderCode || "",
      paymentMethod: vm.orderForSend?.paymentMethod || "Cash",
      customerOrderItem: [],
      orderType: vm.orderForSend?.orderType || "Takeaway",
      notes: vm.orderForSend?.notes || "",
      creditCustomerId:
        vm.orderForSend?.creditCustomerId != null
          ? vm.orderForSend.creditCustomerId
          : null,
    },
    orderDiscountType: vm.orderDiscountType || "amount",
    orderDiscountValue:
      vm.orderDiscountValue == null || vm.orderDiscountValue === ""
        ? null
        : Number(vm.orderDiscountValue),
    changeCalcOpen: false,
    customerPaidAmount: null,
  };
}

export function applySnapshotToPos(vm, snap, options = {}) {
  if (!vm || !snap) return;
  const keepPaymentPreference = !!options.keepPaymentPreference;
  const preferredPayment =
    keepPaymentPreference && vm.orderForSend?.paymentMethod
      ? vm.orderForSend.paymentMethod
      : snap.orderForSend?.paymentMethod || "Cash";

  vm.carditems = cloneCartItems(snap.carditems);
  vm.isWholesale = !!snap.isWholesale;
  vm.orderForSend = {
    ...vm.orderForSend,
    orderCode: snap.orderForSend?.orderCode || "",
    paymentMethod: preferredPayment,
    customerOrderItem: [],
    orderType: snap.orderForSend?.orderType || "Takeaway",
    notes: snap.orderForSend?.notes || "",
    creditCustomerId:
      snap.orderForSend?.creditCustomerId != null
        ? snap.orderForSend.creditCustomerId
        : null,
    isWholesale: !!snap.isWholesale,
  };
  vm.orderDiscountType = snap.orderDiscountType || "amount";
  vm.orderDiscountValue =
    snap.orderDiscountValue == null || snap.orderDiscountValue === ""
      ? null
      : Number(snap.orderDiscountValue);
  vm.changeCalcOpen = false;
  vm.customerPaidAmount = null;
}

export function tabItemCount(tab) {
  return (tab?.carditems || []).reduce(
    (sum, item) => sum + (Number(item.quantity) || 0),
    0
  );
}

export function tabHasItems(tab) {
  return tabItemCount(tab) > 0;
}

export function normalizeLoadedTabs(payload, defaults = {}) {
  const tabs = Array.isArray(payload?.tabs)
    ? payload.tabs
        .filter((t) => t && t.id)
        .slice(0, POS_INVOICE_TABS_MAX)
        .map((t, i) => ({
          ...createEmptyInvoiceTab(i + 1, defaults),
          ...t,
          index: Number(t.index) || i + 1,
          carditems: cloneCartItems(t.carditems),
          orderForSend: {
            ...createEmptyInvoiceTab(1, defaults).orderForSend,
            ...(t.orderForSend || {}),
            customerOrderItem: [],
          },
          changeCalcOpen: false,
          customerPaidAmount: null,
        }))
    : [];

  if (!tabs.length) {
    tabs.push(createEmptyInvoiceTab(1, defaults));
  }

  let activeId = payload?.activeId;
  if (!tabs.some((t) => t.id === activeId)) {
    activeId = tabs[0].id;
  }

  return { tabs, activeId };
}

export function loadPosInvoiceTabs(userInfo, defaults = {}) {
  try {
    const raw = localStorage.getItem(getPosInvoiceTabsStorageKey(userInfo));
    if (!raw) {
      const tab = createEmptyInvoiceTab(1, defaults);
      return { tabs: [tab], activeId: tab.id };
    }
    return normalizeLoadedTabs(JSON.parse(raw), defaults);
  } catch (error) {
    console.warn("Failed to load POS invoice tabs:", error);
    const tab = createEmptyInvoiceTab(1, defaults);
    return { tabs: [tab], activeId: tab.id };
  }
}

export function savePosInvoiceTabs(userInfo, tabs, activeId) {
  try {
    const payload = {
      activeId,
      tabs: (tabs || []).map((t, i) => ({
        id: t.id,
        index: Number(t.index) || i + 1,
        carditems: cloneCartItems(t.carditems),
        isWholesale: !!t.isWholesale,
        orderForSend: {
          orderCode: t.orderForSend?.orderCode || "",
          paymentMethod: t.orderForSend?.paymentMethod || "Cash",
          customerOrderItem: [],
          orderType: t.orderForSend?.orderType || "Takeaway",
          notes: t.orderForSend?.notes || "",
          creditCustomerId:
            t.orderForSend?.creditCustomerId != null
              ? t.orderForSend.creditCustomerId
              : null,
        },
        orderDiscountType: t.orderDiscountType || "amount",
        orderDiscountValue:
          t.orderDiscountValue == null || t.orderDiscountValue === ""
            ? null
            : Number(t.orderDiscountValue),
      })),
    };
    localStorage.setItem(
      getPosInvoiceTabsStorageKey(userInfo),
      JSON.stringify(payload)
    );
  } catch (error) {
    console.warn("Failed to save POS invoice tabs:", error);
  }
}

export function nextInvoiceTabIndex(tabs) {
  const used = new Set((tabs || []).map((t) => Number(t.index) || 0));
  let n = 1;
  while (used.has(n)) n += 1;
  return n;
}
