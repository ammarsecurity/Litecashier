export function normalizeCartLineNote(note) {
  return note ? String(note).trim() : "";
}

/**
 * Effective unit price for a cart line.
 * Retail: discount when valid and below selling price.
 * Wholesale: wholesalePrice when > 0, else selling price (no retail discount).
 */
export function getCartLineUnitPrice(line, isWholesale) {
  const wholesaleMode = isWholesale ?? !!line?.isWholesale;
  const price = Number(line?.price ?? line?.sellingPrice ?? 0);

  if (wholesaleMode) {
    const wholesale = Number(line?.wholesalePrice ?? 0);
    return wholesale > 0 ? wholesale : price;
  }

  const discount = Number(line?.disCountPrice ?? 0);
  if (discount > 0 && discount < price) return discount;
  return price;
}

export function hasCartLineDiscount(line, isWholesale) {
  const wholesaleMode = isWholesale ?? !!line?.isWholesale;
  if (wholesaleMode) return false;
  const price = Number(line?.price ?? line?.sellingPrice ?? 0);
  const discount = Number(line?.disCountPrice ?? 0);
  return discount > 0 && discount < price;
}

export function getCartLineTotal(line, isWholesale) {
  const qty = Math.max(0, Number(line?.quantity) || 0);
  return getCartLineUnitPrice(line, isWholesale) * qty;
}

export function cartLineMergeKey(line) {
  const itemId = Number(line?.id ?? line?.itemId ?? 0);
  const note = normalizeCartLineNote(line?.lineNote ?? line?.notes);
  return `${itemId}|${note}`;
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
      target.total = getCartLineTotal(target);
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
