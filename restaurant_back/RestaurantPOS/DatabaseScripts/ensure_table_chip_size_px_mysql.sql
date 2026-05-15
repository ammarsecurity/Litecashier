-- Idempotent: إضافة عمود حجم رقاقة الطاولة إن لم يكن موجوداً (MySQL)
-- نفّذه يدوياً على نفس قاعدة البيانات المذكورة في ConnectionStrings:WebApiDatabase إذا لم تستخدم EF Migrate.

SET @dbname = DATABASE();
SET @col_exists = (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = @dbname
    AND TABLE_NAME = 'RestaurantLayoutSettings'
    AND COLUMN_NAME = 'TableChipSizePx'
);
SET @q = IF(
  @col_exists = 0,
  'ALTER TABLE `RestaurantLayoutSettings` ADD COLUMN `TableChipSizePx` INT NULL',
  'SELECT 1'
);
PREPARE stmt FROM @q;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- سجل الترحيل (اختياري — فقط إذا اعتمدت على جدول __EFMigrationsHistory ولن تشغّل dotnet ef)
-- INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
-- VALUES ('20260515120000_AddFloorPlanTableChipSize', '8.0.2');
