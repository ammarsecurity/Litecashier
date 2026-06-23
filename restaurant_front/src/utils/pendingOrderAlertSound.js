/**
 * Soft repeating alert for pending public orders (orderStatus === Pending).
 * Uses Web Audio API — no external asset required.
 */

const STORAGE_KEY = "publicOrderPendingSound";
const REPEAT_INTERVAL_MS = 32000;
const MASTER_VOLUME = 0.25;

let audioContext = null;
let repeatTimer = null;
let loopActive = false;
let unlocked = false;
let unlockHandlersBound = false;
let tabHidden = false;

function isEnabled() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw === null) return true;
    return raw !== "false" && raw !== "0";
  } catch {
    return true;
  }
}

function setEnabled(enabled) {
  try {
    localStorage.setItem(STORAGE_KEY, enabled ? "true" : "false");
  } catch {
    /* ignore */
  }
  if (!enabled) {
    stopLoop();
  }
}

function getAudioContext() {
  if (typeof window === "undefined") return null;
  const Ctx = window.AudioContext || window.webkitAudioContext;
  if (!Ctx) return null;
  if (!audioContext) {
    audioContext = new Ctx();
  }
  return audioContext;
}

async function resumeContext() {
  const ctx = getAudioContext();
  if (!ctx) return false;
  if (ctx.state === "suspended") {
    try {
      await ctx.resume();
    } catch {
      return false;
    }
  }
  return ctx.state === "running";
}

function playTone(ctx, frequency, startAt, duration, peakGain) {
  const oscillator = ctx.createOscillator();
  const gain = ctx.createGain();

  oscillator.type = "sine";
  oscillator.frequency.setValueAtTime(frequency, startAt);

  gain.gain.setValueAtTime(0.0001, startAt);
  gain.gain.exponentialRampToValueAtTime(peakGain, startAt + 0.02);
  gain.gain.exponentialRampToValueAtTime(0.0001, startAt + duration);

  oscillator.connect(gain);
  gain.connect(ctx.destination);

  oscillator.start(startAt);
  oscillator.stop(startAt + duration + 0.02);
}

async function playOnce() {
  if (!isEnabled() || tabHidden) return;

  const ready = await resumeContext();
  if (!ready) return;

  const ctx = getAudioContext();
  if (!ctx) return;

  const now = ctx.currentTime;
  const peak = MASTER_VOLUME;
  playTone(ctx, 523.25, now, 0.14, peak);
  playTone(ctx, 659.25, now + 0.16, 0.16, peak * 0.82);
}

function clearRepeatTimer() {
  if (repeatTimer != null) {
    clearInterval(repeatTimer);
    repeatTimer = null;
  }
}

function startLoop() {
  if (!isEnabled()) return;

  loopActive = true;
  if (repeatTimer != null) return;

  if (!tabHidden) {
    playOnce();
  }

  repeatTimer = setInterval(() => {
    if (!loopActive || !isEnabled() || tabHidden) return;
    playOnce();
  }, REPEAT_INTERVAL_MS);
}

function stopLoop() {
  loopActive = false;
  clearRepeatTimer();
}

function onUserGesture() {
  unlocked = true;
  resumeContext().then(() => {
    if (loopActive && !tabHidden && isEnabled()) {
      playOnce();
    }
  });
  document.removeEventListener("click", onUserGesture, true);
  document.removeEventListener("keydown", onUserGesture, true);
}

function unlock() {
  if (unlockHandlersBound || typeof document === "undefined") return;
  unlockHandlersBound = true;
  document.addEventListener("click", onUserGesture, true);
  document.addEventListener("keydown", onUserGesture, true);
}

function setTabHidden(hidden) {
  tabHidden = Boolean(hidden);
  if (tabHidden) {
    clearRepeatTimer();
    return;
  }
  if (loopActive && isEnabled()) {
    startLoop();
  }
}

function destroy() {
  stopLoop();
  document.removeEventListener("click", onUserGesture, true);
  document.removeEventListener("keydown", onUserGesture, true);
  unlockHandlersBound = false;
  unlocked = false;
  if (audioContext) {
    audioContext.close().catch(() => {});
    audioContext = null;
  }
}

export default {
  unlock,
  playOnce,
  startLoop,
  stopLoop,
  setTabHidden,
  isEnabled,
  setEnabled,
  destroy,
  get isUnlocked() {
    return unlocked;
  },
  get isLooping() {
    return loopActive;
  },
};
