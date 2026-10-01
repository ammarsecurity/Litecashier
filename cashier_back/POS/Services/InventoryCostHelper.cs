using POS.Models.Dtos;

namespace POS.Services;

public static class InventoryCostHelper
{
    /// <summary>
    /// Weighted average when restocking: oldQty×oldCost + addedQty×newUnitCost over total qty.
    /// </summary>
    public static decimal ComputeWeightedAverageCost(
        int oldQty,
        decimal oldCost,
        int addedQty,
        decimal newUnitCost)
    {
        if (addedQty <= 0)
            return oldCost;

        var totalQty = oldQty + addedQty;
        if (totalQty <= 0)
            return newUnitCost;

        return Math.Round(
            (oldQty * oldCost + addedQty * newUnitCost) / totalQty,
            0,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Undo a weighted-average In that used <paramref name="batchUnitCost"/> for <paramref name="removedQty"/>.
    /// </summary>
    public static decimal ReverseWeightedAverageCost(
        int currentQty,
        decimal currentCost,
        int removedQty,
        decimal batchUnitCost)
    {
        if (removedQty <= 0)
            return currentCost;

        var qtyBefore = currentQty - removedQty;
        if (qtyBefore <= 0)
            return batchUnitCost;

        return Math.Round(
            (currentQty * currentCost - removedQty * batchUnitCost) / qtyBefore,
            0,
            MidpointRounding.AwayFromZero);
    }

    public static int ComputeIncomingTotalQuantity(
        IReadOnlyList<WarehouseStockInputDto>? stocks,
        int? fallbackTotalQuantity)
    {
        if (stocks == null || stocks.Count == 0)
            return Math.Max(0, fallbackTotalQuantity ?? 0);

        return stocks
            .GroupBy(s => s.WarehouseId)
            .Sum(g => Math.Max(0, g.Sum(x => x.Quantity)));
    }
}
