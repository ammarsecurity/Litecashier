-- Add TagId (nullable) to Expenses - run once on database restaurant_pos
ALTER TABLE `Expenses` ADD COLUMN `TagId` int NULL;
CREATE INDEX `IX_Expenses_TagId` ON `Expenses` (`TagId`);
ALTER TABLE `Expenses` ADD CONSTRAINT `FK_Expenses_Tags_TagId` FOREIGN KEY (`TagId`) REFERENCES `Tags` (`Id`) ON DELETE SET NULL;
