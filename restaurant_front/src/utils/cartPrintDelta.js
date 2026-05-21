/**
 * Kitchen delta print: new cart lines or quantity increases since last print baseline.
 */

export function cartLineKey(line) {
  const orderItemId = Number(line?.sourceOrderItemId || 0);
  if (Number.isFinite(orderItemId) && orderItemId > 0) {
    return `oi:${orderItemId}`;
  }
  const itemId = line?.id ?? line?.itemId;
  if (itemId != null && itemId !== "") {
    return `id:${itemId}`;
  }
  return `line:${line?.name || ""}:${line?.price || 0}`;
}

export function lineUnitPrice(line) {
  const price = Number(line?.price || 0);
  const discount = Number(line?.disCountPrice || 0);
  return discount > 0 && discount !== price ? discount : price;
}

/**
 * @param {Array} current - current cart lines
 * @param {Array} baseline - last printed/saved baseline
 * @returns {Array} lines to send to printCard (delta only)
 */
export function computeKitchenPrintDelta(current, baseline) {
  const currentList = Array.isArray(current) ? current : [];
  const baselineList = Array.isArray(baseline) ? baseline : [];

  if (baselineList.length === 0) {
    return currentList.map((line) => ({ ...line }));
  }

  const baselineMap = new Map();
  for (const b of baselineList) {
    baselineMap.set(cartLineKey(b), b);
  }

  const delta = [];
  for (const line of currentList) {
    const key = cartLineKey(line);
    const prev = baselineMap.get(key);
    const curQty = Math.max(0, Number(line.quantity || 0));

    if (!prev) {
      delta.push({ ...line });
      continue;
    }

    const prevQty = Math.max(0, Number(prev.quantity || 0));
    if (curQty > prevQty) {
      const addQty = curQty - prevQty;
      const unit = lineUnitPrice(line);
      delta.push({
        ...line,
        quantity: addQty,
        total: unit * addQty,
      });
    }
  }

  return delta;
}

/**
 * Clone cart lines for baseline storage.
 */
export function cloneCartBaseline(items) {
  if (!Array.isArray(items)) {
    return [];
  }
  return items.map((line) => ({
    id: line.id,
    name: line.name,
    quantity: line.quantity,
    price: line.price,
    disCountPrice: line.disCountPrice,
    total: line.total,
    tags: line.tags,
    code: line.code,
    image: line.image,
    sourceOrderId: line.sourceOrderId,
    sourceOrderItemId: line.sourceOrderItemId,
  }));
}
