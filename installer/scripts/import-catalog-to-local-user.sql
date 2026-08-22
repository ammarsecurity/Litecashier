-- Import tags + items from backup DB (pos) into pos111 for commercial user 07830200031 (Id=2)
SET NAMES utf8mb4;
SET @target_user := 2;
SET @source_user := 2;

-- Tags (skip duplicate names for same owner)
INSERT INTO pos111.Tags (Name, IsForAll, InsertByUserId, InsertDate, UpdateDate, IsDeleted)
SELECT src.Name, src.IsForAll, @target_user, src.InsertDate, src.UpdateDate, src.IsDeleted
FROM pos.Tags src
WHERE src.IsDeleted = 0
  AND src.InsertByUserId = @source_user
  AND NOT EXISTS (
    SELECT 1 FROM pos111.Tags dst
    WHERE dst.InsertByUserId = @target_user
      AND dst.IsDeleted = 0
      AND dst.Name = src.Name
  );

-- Items (skip duplicate product codes for same owner)
INSERT INTO pos111.Items (
  Name, Description, Image, DisCountPrice, SellingPrice, PurchasingPrice,
  Quantity, InsertByUserId, Tags, Code, InsertDate, UpdateDate, IsDeleted,
  LowStockAlertQuantity, WholesalePrice
)
SELECT
  src.Name, src.Description, src.Image, src.DisCountPrice, src.SellingPrice, src.PurchasingPrice,
  src.Quantity, @target_user, src.Tags, src.Code, src.InsertDate, src.UpdateDate, src.IsDeleted,
  src.LowStockAlertQuantity, src.WholesalePrice
FROM pos.Items src
WHERE src.IsDeleted = 0
  AND src.InsertByUserId = @source_user
  AND NOT EXISTS (
    SELECT 1 FROM pos111.Items dst
    WHERE dst.InsertByUserId = @target_user
      AND dst.IsDeleted = 0
      AND dst.Code = src.Code
  );

-- Extra barcodes (match by product code)
INSERT INTO pos111.ItemCodes (ItemId, Code, InsertByUserId, InsertDate, UpdateDate, IsDeleted)
SELECT dst.Id, ic.Code, @target_user, ic.InsertDate, ic.UpdateDate, ic.IsDeleted
FROM pos.ItemCodes ic
INNER JOIN pos.Items src ON src.Id = ic.ItemId AND src.IsDeleted = 0 AND src.InsertByUserId = @source_user
INNER JOIN pos111.Items dst ON dst.Code = src.Code AND dst.InsertByUserId = @target_user AND dst.IsDeleted = 0
WHERE ic.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM pos111.ItemCodes x
    WHERE x.ItemId = dst.Id AND x.Code = ic.Code AND x.IsDeleted = 0
  );

-- Warehouse stock rows for default warehouse
INSERT INTO pos111.ItemWarehouseStocks (ItemId, WarehouseId, Quantity, InsertDate, UpdateDate, IsDeleted)
SELECT i.Id, w.Id, i.Quantity, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 0
FROM pos111.Items i
INNER JOIN pos111.Warehouses w ON w.InsertByUserId = @target_user AND w.IsDefault = 1 AND w.IsDeleted = 0
WHERE i.InsertByUserId = @target_user
  AND i.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM pos111.ItemWarehouseStocks s
    WHERE s.ItemId = i.Id AND s.WarehouseId = w.Id AND s.IsDeleted = 0
  );

SELECT 'Import summary' AS Info,
  (SELECT COUNT(*) FROM pos111.Tags WHERE InsertByUserId=@target_user AND IsDeleted=0) AS tags_total,
  (SELECT COUNT(*) FROM pos111.Items WHERE InsertByUserId=@target_user AND IsDeleted=0) AS items_total,
  (SELECT COUNT(*) FROM pos111.ItemCodes ic INNER JOIN pos111.Items i ON i.Id=ic.ItemId WHERE i.InsertByUserId=@target_user AND ic.IsDeleted=0) AS item_codes_total;
