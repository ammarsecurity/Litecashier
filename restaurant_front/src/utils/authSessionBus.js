const AUTH_EVENT = "litecashier:auth";

/** Notify App (and others) that localStorage session changed. */
export function notifyAuthSessionChanged() {
  if (typeof window === "undefined") return;
  window.dispatchEvent(new CustomEvent(AUTH_EVENT));
}

export function onAuthSessionChanged(handler) {
  if (typeof window === "undefined" || typeof handler !== "function") {
    return () => {};
  }
  window.addEventListener(AUTH_EVENT, handler);
  window.addEventListener("storage", handler);
  return () => {
    window.removeEventListener(AUTH_EVENT, handler);
    window.removeEventListener("storage", handler);
  };
}
