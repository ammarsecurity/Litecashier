let openHandler = null;

export function registerDevicePausedHandler(fn) {
  openHandler = typeof fn === "function" ? fn : null;
}

export function openDevicePausedGate(payload) {
  if (openHandler) openHandler(payload || {});
}
