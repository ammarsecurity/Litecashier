-- Idempotent: عمود TagId لجدول Expenses (مطلوب لـ Admin/GetSalesByCategory وغيره)
-- نفّذه على نفس قاعدة البيانات في ConnectionStrings:WebApiDatabase

SET @dbname = DATABASE();
SET @col_exists = (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'TagId'
);
SET @q = IF(@col_exists = 0, 'ALTER TABLE `Expenses` ADD COLUMN `TagId` INT NULL', 'SELECT 1');
PREPARE stmt FROM @q;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- فهرس (تجاهل الخطأ إن وُجد مسبقاً)
SET @idx_exists = (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = 'Expenses' AND INDEX_NAME = 'IX_Expenses_TagId'
);
SET @q2 = IF(@idx_exists = 0, 'CREATE INDEX `IX_Expenses_TagId` ON `Expenses` (`TagId`)', 'SELECT 1');
PREPARE stmt2 FROM @q2;
EXECUTE stmt2;
DEALLOCATE PREPARE stmt2;

-- مفتاح أجنبي (MySQL قد يحتاج NULL إن وُجد مفتاح بنفس الاسم)
SET @fk_exists = (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA = @dbname AND TABLE_NAME = 'Expenses' AND CONSTRAINT_NAME = 'FK_Expenses_Tags_TagId'
);
SET @q3 = IF(@fk_exists = 0,
  'ALTER TABLE `Expenses` ADD CONSTRAINT `FK_Expenses_Tags_TagId` FOREIGN KEY (`TagId`) REFERENCES `Tags` (`Id`) ON DELETE SET NULL',
  'SELECT 1');
PREPARE stmt3 FROM @q3;
EXECUTE stmt3;
DEALLOCATE PREPARE stmt3;
