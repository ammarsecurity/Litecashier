/**
 * Short error buzz for POS barcode miss / out-of-stock scans.
 * Web Audio API — no asset file required.
 */

const MASTER_VOLUME = 0.55;

let audioContext = null;

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

function playTone(ctx, frequency, startAt, duration, peakGain) {
  const oscillator = ctx.createOscillator();
  const gain = ctx.createGain();
  oscillator.type = "square";
  oscillator.frequency.setValueAtTime(frequency, startAt);
  gain.gain.setValueAtTime(0.0001, startAt);
  gain.gain.exponentialRampToValueAtTime(peakGain, startAt + 0.012);
  gain.gain.exponentialRampToValueAtTime(0.0001, startAt + duration);
  oscillator.connect(gain);
  gain.connect(ctx.destination);
  oscillator.start(startAt);
  oscillator.stop(startAt + duration + 0.02);
}

/** Double low buzz — distinct from success / public-order chime. */
export async function playPosScanErrorSound() {
  const ready = await resumeContext();
  if (!ready) return;
  const ctx = getAudioContext();
  if (!ctx) return;
  const now = ctx.currentTime;
  const peak = MASTER_VOLUME;
  playTone(ctx, 420, now, 0.14, peak);
  playTone(ctx, 280, now + 0.16, 0.22, peak * 0.92);
}

export default {
  playPosScanErrorSound,
};
