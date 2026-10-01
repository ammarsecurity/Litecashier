/**
 * Weighted average purchase cost when restocking existing inventory.
 */
export function computeWeightedAverageCost(oldQty, oldCost, addedQty, newUnitCost) {
  const oldQ = Math.max(0, Number(oldQty) || 0);
  const addQ = Math.max(0, Number(addedQty) || 0);
  const oldC = Number(oldCost) || 0;
  const newC = Number(newUnitCost) || 0;

  if (addQ <= 0) return oldC;
  const totalQty = oldQ + addQ;
  if (totalQty <= 0) return newC;

  const avg = (oldQ * oldC + addQ * newC) / totalQty;
  return Math.round(avg);
}
