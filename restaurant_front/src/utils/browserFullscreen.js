/** Browser Fullscreen API helpers (F11-like behavior). */

export function isBrowserFullscreen() {
  if (typeof document === "undefined") return false;
  return !!(
    document.fullscreenElement ||
    document.webkitFullscreenElement ||
    document.mozFullScreenElement ||
    document.msFullscreenElement
  );
}

export async function enterBrowserFullscreen(element) {
  const el = element || document.documentElement;
  const request =
    el.requestFullscreen ||
    el.webkitRequestFullscreen ||
    el.mozRequestFullScreen ||
    el.msRequestFullscreen;
  if (!request) {
    throw new Error("Fullscreen API not supported");
  }
  await request.call(el);
  return isBrowserFullscreen();
}

export async function exitBrowserFullscreen() {
  const doc = document;
  const exit =
    doc.exitFullscreen ||
    doc.webkitExitFullscreen ||
    doc.mozCancelFullScreen ||
    doc.msExitFullscreen;
  if (!exit) {
    throw new Error("Fullscreen API not supported");
  }
  await exit.call(doc);
  return !isBrowserFullscreen();
}

export async function toggleBrowserFullscreen(element) {
  if (isBrowserFullscreen()) {
    await exitBrowserFullscreen();
    return false;
  }
  await enterBrowserFullscreen(element);
  return true;
}
