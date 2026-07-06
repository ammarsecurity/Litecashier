/** عدد المنتجات المعروضة في كل صفحة نقطة البيع */
export const POS_ITEMS_PER_PAGE = 28;

/**
 * يحسب عدد المنتجات المعروضة في شبكة POS حسب المساحة المتاحة.
 */
export function computePosPageSize({
  currentPageSize = POS_ITEMS_PER_PAGE,
} = {}) {
  const nextSize = POS_ITEMS_PER_PAGE;

  return {
    nextSize,
    changed: nextSize !== currentPageSize,
  };
}

/**
 * يطبّق حجم الصفحة على مكوّن POS ويعيد تحميل المنتجات عند الحاجة.
 */
export function applyPosPageSize(vm, reload = true) {
  if (!vm || vm._isDestroyed || typeof window === "undefined") return;

  const { nextSize, changed } = computePosPageSize({
    currentPageSize: vm.pageSize,
  });

  if (!changed) return;

  vm.pageSize = nextSize;
  if (vm.pageNumber !== 1) {
    vm.pageNumber = 1;
  } else if (reload && typeof vm.GetAllItems === "function") {
    vm.GetAllItems();
  }
}
