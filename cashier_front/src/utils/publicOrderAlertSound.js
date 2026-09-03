/**
 * Loud repeating chime for new public-menu orders.
 * Web Audio API — no asset file required.
 */

const REPEAT_INTERVAL_MS = 4000;
const MASTER_VOLUME = 0.78;

let audioContext = null;
let repeatTimer = null;
let loopActive = false;
let unlocked = false;
let unlockHandlersBound = false;

function getAudioContext() {
  if (typeof window === "undefined") return null;
  const Ctx = window.AudioContext || window.webkitAudioContext;
  if (!Ctx) return null;
  if (!audioContext) audioContext = new Ctx();
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

function playTone(ctx, type, frequency, startAt, duration, peakGain) {
  const oscillator = ctx.createOscillator();
  const gain = ctx.createGain();
  oscillator.type = type;
  oscillator.frequency.setValueAtTime(frequency, startAt);
  gain.gain.setValueAtTime(0.0001, startAt);
  gain.gain.exponentialRampToValueAtTime(peakGain, startAt + 0.015);
  gain.gain.exponentialRampToValueAtTime(0.0001, startAt + duration);
  oscillator.connect(gain);
  gain.connect(ctx.destination);
  oscillator.start(startAt);
  oscillator.stop(startAt + duration + 0.03);
}

async function playOnce() {
  const ready = await resumeContext();
  if (!ready) return;
  const ctx = getAudioContext();
  if (!ctx) return;
  const now = ctx.currentTime;
  const peak = MASTER_VOLUME;
  playTone(ctx, "square", 880, now, 0.16, peak);
  playTone(ctx, "square", 1175, now + 0.18, 0.16, peak);
  playTone(ctx, "square", 1480, now + 0.36, 0.28, peak * 0.9);
}

function startLoop() {
  loopActive = true;
  if (repeatTimer != null) {
    playOnce();
    return;
  }
  playOnce();
  repeatTimer = setInterval(() => {
    if (!loopActive) return;
    playOnce();
  }, REPEAT_INTERVAL_MS);
}

function stopLoop() {
  loopActive = false;
  if (repeatTimer != null) {
    clearInterval(repeatTimer);
    repeatTimer = null;
  }
}

function onUserGesture() {
  unlocked = true;
  resumeContext();
  document.removeEventListener("click", onUserGesture, true);
  document.removeEventListener("keydown", onUserGesture, true);
  document.removeEventListener("touchstart", onUserGesture, true);
}

function unlock() {
  if (unlockHandlersBound || typeof document === "undefined") return;
  unlockHandlersBound = true;
  document.addEventListener("click", onUserGesture, true);
  document.addEventListener("keydown", onUserGesture, true);
  document.addEventListener("touchstart", onUserGesture, true);
}

export default {
  unlock,
  playOnce,
  startLoop,
  stopLoop,
  get isLooping() {
    return loopActive;
  },
  get isUnlocked() {
    return unlocked;
  },
};
