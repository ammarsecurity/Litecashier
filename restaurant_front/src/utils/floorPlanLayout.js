/** Reference canvas size aligned with POS floor-plan CSS (16:10, max-height ~420px). */
const ASPECT_W = 16;
const ASPECT_H = 10;
const REF_HEIGHT_PX = 420;
const PADDING_NORM = 0.01;

/**
 * Minimum normalized separation between table chip centers for a given chip size.
 */
export function floorPlanChipSeparationNorm(chipSizePx) {
  const chip = Math.max(32, Math.min(96, Number(chipSizePx) || 56));
  const refH = REF_HEIGHT_PX;
  const refW = (refH * ASPECT_W) / ASPECT_H;
  const minDy = chip / refH + PADDING_NORM;
  const minDx = chip / refW + PADDING_NORM;
  const minDist = Math.max(minDx, minDy);
  const margin = minDist * 0.52;
  return { minDx, minDy, minDist, margin };
}

/**
 * Spread overlapping table positions so chips do not stack on the floor plan.
 * @param {Record<string, { x: number, y: number }>} positions
 * @param {number} chipSizePx
 * @returns {Record<string, { x: number, y: number }>}
 */
export function resolveFloorPlanOverlaps(positions, chipSizePx) {
  const ids = Object.keys(positions || {});
  if (ids.length < 2) {
    const out = {};
    ids.forEach((id) => {
      out[id] = { x: Number(positions[id].x), y: Number(positions[id].y) };
    });
    return out;
  }

  const { minDist, margin } = floorPlanChipSeparationNorm(chipSizePx);
  const pts = ids.map((id) => ({
    id,
    x: Number(positions[id].x),
    y: Number(positions[id].y),
  }));

  const clamp = (p) => {
    p.x = Math.max(margin, Math.min(1 - margin, p.x));
    p.y = Math.max(margin, Math.min(1 - margin, p.y));
  };

  for (let iter = 0; iter < 120; iter++) {
    let moved = false;
    for (let i = 0; i < pts.length; i++) {
      for (let j = i + 1; j < pts.length; j++) {
        const a = pts[i];
        const b = pts[j];
        let dx = b.x - a.x;
        let dy = b.y - a.y;
        let dist = Math.hypot(dx, dy);
        if (dist < 1e-8) {
          const angle = ((i + j) % 8) * (Math.PI / 4);
          dx = Math.cos(angle) * 1e-4;
          dy = Math.sin(angle) * 1e-4;
          dist = Math.hypot(dx, dy);
        }
        if (dist >= minDist) continue;
        moved = true;
        const push = ((minDist - dist) / dist) * 0.52;
        a.x -= dx * push;
        a.y -= dy * push;
        b.x += dx * push;
        b.y += dy * push;
        clamp(a);
        clamp(b);
      }
    }
    if (!moved) break;
  }

  const out = {};
  pts.forEach((p) => {
    out[p.id] = { x: p.x, y: p.y };
  });
  return out;
}

/**
 * Find a non-overlapping slot when placing a new table on the canvas.
 */
export function findOpenFloorPlanSlot(positions, chipSizePx) {
  const { minDist, margin } = floorPlanChipSeparationNorm(chipSizePx);
  const collides = (x, y) => {
    for (const id of Object.keys(positions || {})) {
      const p = positions[id];
      if (p == null) continue;
      const dx = Number(p.x) - x;
      const dy = Number(p.y) - y;
      if (Math.hypot(dx, dy) < minDist) return true;
    }
    return false;
  };

  const stepY = minDist * 0.92;
  const stepX = minDist * 0.92;
  for (let y = margin; y <= 1 - margin + 1e-6; y += stepY) {
    for (let x = margin; x <= 1 - margin + 1e-6; x += stepX) {
      if (!collides(x, y)) return { x, y };
    }
  }

  const fallback = { x: 0.5, y: 0.5 };
  const withNew = { ...(positions || {}), __slot__: fallback };
  const resolved = resolveFloorPlanOverlaps(withNew, chipSizePx);
  return resolved.__slot__ || fallback;
}
