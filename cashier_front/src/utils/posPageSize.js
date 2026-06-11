/**
 * يحسب عدد المنتجات المعروضة في شبكة POS حسب المساحة المتاحة.
 */
export function computePosPageSize({
  sectionWidth = 0,
  sectionHeight = 0,
  windowInnerWidth = 1024,
  windowInnerHeight = 768,
  isWide = false,
  hasCheckoutBar = false,
  currentPageSize = 36,
}) {
  const minCardW = isWide ? 118 : 104;
  const gap = 9;
  const cardH = 148;
  const paginationH = 56;

  const width = sectionWidth > 0 ? sectionWidth : windowInnerWidth - 480;
  let height = sectionHeight;

  if (height < 120) {
    const headerH = 64;
    const checkoutH = hasCheckoutBar ? 92 : 0;
    const chromeH = 200;
    height = windowInnerHeight - headerH - checkoutH - chromeH;
  }

  const columns = Math.max(3, Math.floor((width + gap) / (minCardW + gap)));
  const rows = Math.max(3, Math.floor((height - paginationH + gap) / (cardH + gap)));
  const nextSize = Math.min(120, Math.max(24, columns * rows));

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

  const section = vm.$refs.posProductsGridSection;
  const isWide = window.matchMedia("(min-width: 768px)").matches;
  const { nextSize, changed } = computePosPageSize({
    sectionWidth: section?.clientWidth || 0,
    sectionHeight: section?.clientHeight || 0,
    windowInnerWidth: window.innerWidth,
    windowInnerHeight: window.innerHeight,
    isWide,
    hasCheckoutBar: Boolean(vm.showPosCheckoutBar),
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
