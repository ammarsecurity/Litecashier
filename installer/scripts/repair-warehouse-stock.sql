-- Litecashier: repair catalog warehouse stock (run once on updated DB if POS shows out-of-stock
-- while Items page still shows quantities). Same logic as migration 20260822170000_RepairWarehouseStockBackfill.
-- Usage (adjust database name / credentials):
--   mysql -u root -p litecashier < repair-warehouse-stock.sql

INSERT INTO `Warehouses` (`Name`, `IsDefault`, `IsActive`, `InsertByUserId`, `InsertDate`, `UpdateDate`, `IsDeleted`)
SELECT N'المخزن الرئيسي', 1, 1, u.`Id`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 0
FROM `Users` u
WHERE u.`IsDeleted` = 0
  AND u.`Role` = 'Commercial'
  AND NOT EXISTS (
    SELECT 1 FROM `Warehouses` w
    WHERE w.`InsertByUserId` = u.`Id` AND w.`IsDeleted` = 0);

INSERT INTO `ItemWarehouseStocks` (`ItemId`, `WarehouseId`, `Quantity`, `InsertDate`, `UpdateDate`, `IsDeleted`)
SELECT i.`Id`, w.`Id`, i.`Quantity`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 0
FROM `Items` i
INNER JOIN `Users` u ON u.`Id` = i.`InsertByUserId` AND u.`IsDeleted` = 0
INNER JOIN `Warehouses` w
  ON w.`IsDeleted` = 0
 AND w.`IsDefault` = 1
 AND w.`InsertByUserId` = CASE
   WHEN u.`Role` = 'Commercial' THEN u.`Id`
   ELSE u.`InsertByUserId`
 END
WHERE i.`IsDeleted` = 0
  AND i.`Quantity` > 0
  AND NOT EXISTS (
    SELECT 1 FROM `ItemWarehouseStocks` s
    WHERE s.`ItemId` = i.`Id`
      AND s.`WarehouseId` = w.`Id`
      AND s.`IsDeleted` = 0);

UPDATE `ItemWarehouseStocks` s
INNER JOIN `Items` i ON i.`Id` = s.`ItemId` AND i.`IsDeleted` = 0
INNER JOIN `Warehouses` w ON w.`Id` = s.`WarehouseId` AND w.`IsDeleted` = 0 AND w.`IsDefault` = 1
SET s.`Quantity` = i.`Quantity`,
    s.`UpdateDate` = UTC_TIMESTAMP(6)
WHERE s.`IsDeleted` = 0
  AND s.`Quantity` = 0
  AND i.`Quantity` > 0
  AND NOT EXISTS (
    SELECT 1 FROM `ItemWarehouseStocks` s2
    WHERE s2.`ItemId` = i.`Id`
      AND s2.`IsDeleted` = 0
      AND s2.`Quantity` > 0);

UPDATE `Items` i
SET i.`Quantity` = COALESCE((
      SELECT SUM(s.`Quantity`)
      FROM `ItemWarehouseStocks` s
      WHERE s.`ItemId` = i.`Id` AND s.`IsDeleted` = 0
    ), 0),
    i.`UpdateDate` = UTC_TIMESTAMP(6)
WHERE i.`IsDeleted` = 0
  AND i.`Quantity` <> COALESCE((
      SELECT SUM(s.`Quantity`)
      FROM `ItemWarehouseStocks` s
      WHERE s.`ItemId` = i.`Id` AND s.`IsDeleted` = 0
    ), 0);

SELECT 'Repair complete' AS Status,
  (SELECT COUNT(*) FROM ItemWarehouseStocks WHERE IsDeleted = 0 AND Quantity > 0) AS warehouse_rows_with_stock,
  (SELECT COUNT(*) FROM Items WHERE IsDeleted = 0 AND Quantity > 0) AS items_with_stock;
