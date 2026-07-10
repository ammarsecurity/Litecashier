/**
 * Resolve API base URL for axios / SignalR.
 * Installer builds use "/" (same origin) so LAN clients work via http://SERVER_IP:5189
 */
export function resolveApiBaseUrl() {
  const raw = process.env.VUE_APP_API_URL;
  if (raw != null && String(raw).trim() !== "") {
    const trimmed = String(raw).trim();
    // Same-origin relative base (installer / LAN)
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

  return process.env.NODE_ENV === "production"
    ? "https://pos-api.tanfeeth-iq.tech/"
    : "https://pos-api.tanfeeth-iq.tech/";
}

/** Print Server on the same host as the POS page (LAN-safe). */
export function resolvePrintServerUrl() {
  if (typeof window !== "undefined" && window.location?.hostname) {
    return `http://${window.location.hostname}:5000`;
  }
  return "http://localhost:5000";
}
