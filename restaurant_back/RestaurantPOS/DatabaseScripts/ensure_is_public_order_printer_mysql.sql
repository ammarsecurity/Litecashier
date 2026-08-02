-- Idempotent: إضافة IsPublicOrderPrinter لجدول Printers إن لم يكن موجوداً (MySQL)
-- يُنفَّذ أيضاً تلقائياً عند تشغيل الـ API (انظر Program.cs)

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND LOWER(TABLE_NAME) = 'printers'
    AND LOWER(COLUMN_NAME) = 'ispublicorderprinter');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `Printers` ADD COLUMN `IsPublicOrderPrinter` tinyint(1) NOT NULL DEFAULT 0',
  'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
