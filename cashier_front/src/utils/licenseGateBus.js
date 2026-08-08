let openHandler = null;

export function registerLicenseGateHandler(fn) {
  openHandler = typeof fn === "function" ? fn : null;
}

export function openLicenseGate(payload) {
  if (openHandler) openHandler(payload || {});
}
