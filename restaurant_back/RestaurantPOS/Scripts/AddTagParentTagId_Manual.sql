-- إصلاح: Unknown column 'ParentTagId' في جدول Tags
-- نفّذ هذا السكربت على قاعدة MySQL إذا لم تُطبَّق المهاجرة عبر EF.

-- 1) عمود القسم الأب (اختياري)
ALTER TABLE `Tags`
    ADD COLUMN `ParentTagId` int NULL;

-- 2) فهرس للأداء
CREATE INDEX `IX_Tags_ParentTagId` ON `Tags` (`ParentTagId`);

-- 3) مفتاح أجنبي: قسم فرعي → قسم رئيسي (نفس الجدول)
ALTER TABLE `Tags`
    ADD CONSTRAINT `FK_Tags_Tags_ParentTagId`
    FOREIGN KEY (`ParentTagId`) REFERENCES `Tags` (`Id`)
    ON DELETE RESTRICT;

-- 4) تسجيل المهاجرة في EF (اختياري — بعد التنفيذ اليدوي حتى لا يعيد EF نفس التغيير)
-- INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
-- VALUES ('20260429195841_EnsureTagParentTagId', '8.0.2');
