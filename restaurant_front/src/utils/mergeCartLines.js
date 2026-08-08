export function normalizeCartLineNote(note) {
  return note ? String(note).trim() : "";
}

export function cartLineMergeKey(line) {
  const itemId = Number(line?.id ?? line?.itemId ?? 0);
  const note = normalizeCartLineNote(line?.lineNote ?? line?.notes);
  return `${itemId}|${note}`;
}

/** Move a cart line to index 0 (newest / last touched first). */
export function promoteCartLineToFront(carditems, index) {
  if (!Array.isArray(carditems) || index <= 0 || index >= carditems.length) return;
  const [line] = carditems.splice(index, 1);
  carditems.unshift(line);
}

export function mergeCartLines(lines) {
  const merged = [];
  const indexByKey = new Map();

  for (const line of lines || []) {
    const key = cartLineMergeKey(line);
    const qty = Math.max(0, Number(line?.quantity) || 0);
    if (qty <= 0) continue;

    if (indexByKey.has(key)) {
      const idx = indexByKey.get(key);
      const target = merged[idx];
      target.quantity = (Number(target.quantity) || 0) + qty;
      const price = target.price || 0;
      const disCountPrice = target.disCountPrice || 0;
      const finalPrice =
        disCountPrice > 0 && disCountPrice !== price ? disCountPrice : price;
      target.total = finalPrice * target.quantity;
    } else {
      indexByKey.set(key, merged.length);
      merged.push({ ...line, quantity: qty });
    }
  }

  return merged;
}

export function findCartLineIndex(cartitems, itemId, lineNote) {
  const key = cartLineMergeKey({ id: itemId, lineNote });
  return (cartitems || []).findIndex((line) => cartLineMergeKey(line) === key);
}

export function mergeCartLinesForOrderPayload(cartitems) {
  const merged = [];
  const indexByKey = new Map();

  for (const item of cartitems || []) {
    const itemId = Number(item?.id ?? 0);
    const quantity = Math.max(0, Number(item?.quantity) || 0);
    if (!itemId || quantity <= 0) continue;

    const notes = normalizeCartLineNote(item?.lineNote);
    const key = `${itemId}|${notes}`;

    if (indexByKey.has(key)) {
      const idx = indexByKey.get(key);
      merged[idx].quantity += quantity;
    } else {
      indexByKey.set(key, merged.length);
      merged.push({
        itemId,
        quantity,
        notes: notes || null,
      });
    }
  }

  return merged;
}
