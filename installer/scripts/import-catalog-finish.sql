SET NAMES utf8mb4;
SET @target_user := 2;

INSERT INTO pos111.ItemCodes (ItemId, Code, InsertByUserId, InsertDate, UpdateDate, IsDeleted)
SELECT dst.Id, ic.Code, @target_user, ic.InsertDate, ic.UpdateDate, ic.IsDeleted
FROM pos.ItemCodes ic
INNER JOIN pos.Items src ON src.Id = ic.ItemId AND src.IsDeleted = 0 AND src.InsertByUserId = @target_user
INNER JOIN pos111.Items dst ON dst.Code = src.Code AND dst.InsertByUserId = @target_user AND dst.IsDeleted = 0
WHERE ic.IsDeleted = 0
  AND NOT EXISTS (
    SELECT 1 FROM pos111.ItemCodes x
    WHERE x.ItemId = dst.Id AND x.Code = ic.Code AND x.IsDeleted = 0
  );

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

SELECT 'Done' AS Status,
  (SELECT COUNT(*) FROM pos111.Tags WHERE InsertByUserId=@target_user AND IsDeleted=0) AS tags,
  (SELECT COUNT(*) FROM pos111.Items WHERE InsertByUserId=@target_user AND IsDeleted=0) AS items,
  (SELECT COUNT(*) FROM pos111.ItemWarehouseStocks s JOIN pos111.Items i ON i.Id=s.ItemId WHERE i.InsertByUserId=@target_user AND s.IsDeleted=0) AS warehouse_rows,
  (SELECT COUNT(*) FROM pos111.ItemCodes ic JOIN pos111.Items i ON i.Id=ic.ItemId WHERE i.InsertByUserId=@target_user AND ic.IsDeleted=0) AS barcodes;
