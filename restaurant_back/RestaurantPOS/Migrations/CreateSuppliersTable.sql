-- جدول الموردين (إضافة مسبقاً، تحكم: إضافة / تعديل / حذف)
CREATE TABLE IF NOT EXISTS `Suppliers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Notes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `InsertByUserId` int NOT NULL,
    `InsertDate` datetime(6) NOT NULL,
    `UpdateDate` datetime(6) NOT NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Suppliers` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Suppliers_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE NO ACTION
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_Suppliers_InsertByUserId` ON `Suppliers` (`InsertByUserId`);
