/**
 * Resolve API base URL for axios / SignalR.
 * Prefer same-origin in browser so custom Commercial domains hit the correct host.
 * Installer builds can set VUE_APP_API_URL="/" explicitly.
 */
export function resolveApiBaseUrl() {
  const raw = process.env.VUE_APP_API_URL;
  if (raw != null && String(raw).trim() !== "") {
    const trimmed = String(raw).trim();
    // Same-origin relative base (installer / LAN / cloud SPA behind nginx)
    if (trimmed === "/" || trimmed === "./") {
      if (typeof window !== "undefined" && window.location?.origin) {
        return `${window.location.origin}/`;
      }
      return "/";
    }
    return trimmed.endsWith("/") ? trimmed : `${trimmed}/`;
  }

  if (typeof window !== "undefined" && window.location?.origin) {
    return `${window.location.origin}/`;
  }

  return "https://litecashier.smartstick-iq.com/";
}

/** Print Server on the same host as the POS page (LAN-safe). */
export function resolvePrintServerUrl() {
  if (typeof window !== "undefined" && window.location?.hostname) {
    return `http://${window.location.hostname}:5000`;
  }
  return "http://localhost:5000";
}

/**
 * Turn a relative asset path (/Images/foo.png) into an absolute URL so Print Server
 * WebView2 can load images (it has no same-origin base as the POS SPA).
 */
export function resolveAbsoluteAssetUrl(url) {
  if (url == null || url === "") return null;
  const raw = String(url).trim();
  if (raw.startsWith("data:")) return raw;

  if (raw.startsWith("http://") || raw.startsWith("https://")) {
    try {
      const parsed = new URL(raw);
      const api = new URL(resolveApiBaseUrl());
      const isLocal =
        parsed.hostname === "localhost" || parsed.hostname === "127.0.0.1";
      if (isLocal && parsed.port && parsed.port !== api.port.replace(/^$/, "80")) {
        const fileName = parsed.pathname.split("/").filter(Boolean).pop();
        if (fileName) return `${api.origin}/Images/${fileName}`;
      }
    } catch (_) {
      /* keep original */
    }
    return raw;
  }
  const base = resolveApiBaseUrl().replace(/\/$/, "");
  if (raw.startsWith("/")) {
    return `${base}${raw}`;
  }
  return `${base}/${raw}`;
}
