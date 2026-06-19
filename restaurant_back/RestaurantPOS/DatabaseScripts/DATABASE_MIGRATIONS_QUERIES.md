# استعلامات قاعدة البيانات — RestaurantPOS (MySQL)

هذا الملف يجمع **كويريات SQL يدوية** للجداول الجديدة والأعمدة المُعدَّلة في مشروع `RestaurantPOS`.

> **ملاحظة:** الطريقة المفضلة للتطبيق هي عبر Entity Framework:
> ```bash
> dotnet ef database update --project RestaurantPOS
> ```
> استخدم الكويريات أدناه فقط عند التطبيق اليدوي على سيرفر قاعدة بيانات موجود مسبقاً.

**قاعدة البيانات:** MySQL 8+  
**الترميز:** `utf8mb4`  
**المحرك:** `InnoDB`

---

## فهرس سريع

| الميزة | نوع التغيير | الجداول |
|--------|-------------|---------|
| [دفع البطاقة](#1-دفع-البطاقة-paymentdevices--cardpaymenttransactions) | جداول جديدة | `PaymentDevices`, `CardPaymentTransactions` |
| [خصم الطلب](#2-خصم-الطلب-customerorders) | تعديل | `CustomerOrders` |
| [عدد الضيوف وشاشة الطابور](#3-حقول-إضافية-على-customerorders) | تعديل | `CustomerOrders` |
| [ملاحظات بند الطلب](#4-ملاحظات-بند-الطلب) | تعديل | `CustomerOrderItems` |
| [العملاء والآجل](#5-العملاء-والآجل) | جدول + تعديل | `Customers`, `CustomerOrders` |
| [مرتجعات الأصناف](#6-مرتجعات-الأصناف-returnedorderitems) | جدول جديد | `ReturnedOrderItems` |
| [الأقسام الفرعية](#7-الأقسام-الفرعية-tags) | تعديل | `Tags` |
| [مخطط الطوابق](#8-مخطط-الطوابق) | جداول + تعديل | `RestaurantLayoutSettings`, `TableLayoutPlacements`, `Tables` |
| [المخزون](#9-المخزون-stockmovements) | جدول + تعديل | `StockMovements` |
| [الموظفين والصرفيات](#10-الموظفين-والصرفيات) | جداول + تعديل | `Employees`, `Expenses`, `ExpenseCategories` |
| [دمج الطاولات بالطلب](#11-دمج-الطاولات-بالطلب) | جدول جديد | `OrderTables` |
| [سجل التدقيق](#12-سجل-التدقيق-auditlogs) | جدول جديد | `AuditLogs` |
| [صلاحيات المستخدم](#13-صلاحيات-المستخدم-users) | تعديل | `Users` |

---

## 1. دفع البطاقة (`PaymentDevices` + `CardPaymentTransactions`)

**Migration:** `20260618111254_AddPaymentDeviceAndCardTransactions`

### إنشاء جدول أجهزة الدفع

```sql
CREATE TABLE IF NOT EXISTS `PaymentDevices` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
  `BaseUrl` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
  `ConnectionType` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
  `ComPort` varchar(20) CHARACTER SET utf8mb4 NULL,
  `WifiHost` varchar(200) CHARACTER SET utf8mb4 NULL,
  `WifiPort` int NULL,
  `WifiConfigJson` longtext CHARACTER SET utf8mb4 NULL,
  `CloudConfigJson` longtext CHARACTER SET utf8mb4 NULL,
  `IsDefault` tinyint(1) NOT NULL DEFAULT 0,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_PaymentDevices_InsertByUserId` (`InsertByUserId`),
  CONSTRAINT `FK_PaymentDevices_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### إنشاء جدول معاملات الدفع بالبطاقة

```sql
CREATE TABLE IF NOT EXISTS `CardPaymentTransactions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `PaymentDeviceId` int NOT NULL,
  `CustomerOrderId` int NULL,
  `Amount` decimal(18,2) NOT NULL,
  `TipAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `CurrencyCode` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
  `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
  `ResultCode` varchar(20) CHARACTER SET utf8mb4 NULL,
  `Message` varchar(500) CHARACTER SET utf8mb4 NULL,
  `RawResponse` longtext CHARACTER SET utf8mb4 NULL,
  `AuthCode` varchar(50) CHARACTER SET utf8mb4 NULL,
  `RefNo` varchar(100) CHARACTER SET utf8mb4 NULL,
  `CardNo` varchar(50) CHARACTER SET utf8mb4 NULL,
  `CardType` varchar(30) CHARACTER SET utf8mb4 NULL,
  `IssuerName` varchar(100) CHARACTER SET utf8mb4 NULL,
  `AcquirerName` varchar(100) CHARACTER SET utf8mb4 NULL,
  `TerminalId` varchar(50) CHARACTER SET utf8mb4 NULL,
  `MerchantId` varchar(50) CHARACTER SET utf8mb4 NULL,
  `MerchantName` varchar(200) CHARACTER SET utf8mb4 NULL,
  `VoucherNo` bigint NULL,
  `BatchNo` bigint NULL,
  `TransTime` varchar(30) CHARACTER SET utf8mb4 NULL,
  `TotalAmount` varchar(50) CHARACTER SET utf8mb4 NULL,
  `InsertByUserId` int NOT NULL,
  `RequestedByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_CardPaymentTransactions_CustomerOrderId` (`CustomerOrderId`),
  KEY `IX_CardPaymentTransactions_PaymentDeviceId` (`PaymentDeviceId`),
  KEY `IX_CardPaymentTransactions_InsertByUserId` (`InsertByUserId`),
  KEY `IX_CardPaymentTransactions_RequestedByUserId` (`RequestedByUserId`),
  CONSTRAINT `FK_CardPaymentTransactions_CustomerOrders_CustomerOrderId`
    FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrders` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_CardPaymentTransactions_PaymentDevices_PaymentDeviceId`
    FOREIGN KEY (`PaymentDeviceId`) REFERENCES `PaymentDevices` (`Id`),
  CONSTRAINT `FK_CardPaymentTransactions_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`),
  CONSTRAINT `FK_CardPaymentTransactions_Users_RequestedByUserId`
    FOREIGN KEY (`RequestedByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

**قيم `Status` الشائعة:** `Pending`, `Processing`, `Success`, `Failed`, `Cancelled`

---

## 2. خصم الطلب (`CustomerOrders`)

**Migration:** `20260430081722_AddOrderDiscountFields`

```sql
ALTER TABLE `CustomerOrders`
  ADD COLUMN `DiscountAmount` decimal(65,30) NULL AFTER `PaymentMethod`,
  ADD COLUMN `DiscountPercent` decimal(65,30) NULL,
  ADD COLUMN `DiscountType` longtext CHARACTER SET utf8mb4 NULL,
  ADD COLUMN `DiscountValue` decimal(65,30) NULL,
  ADD COLUMN `OrderSubTotal` decimal(65,30) NULL,
  ADD COLUMN `OrderTotalAfterDiscount` decimal(65,30) NULL;
```

> إذا ظهر خطأ «العمود موجود»، تجاهل السطر المكرر أو استخدم:
> ```sql
> SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
> WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CustomerOrders'
>   AND COLUMN_NAME IN ('DiscountAmount','DiscountType','OrderTotalAfterDiscount');
> ```

---

## 3. حقول إضافية على `CustomerOrders`

### عدد الضيوف
**Migration:** `20260508143523_AddNumberOfGuestsToCustomerOrder`

```sql
ALTER TABLE `CustomerOrders`
  ADD COLUMN `NumberOfGuests` int NULL;
```

### إخفاء من شاشة الطابور
**Migration:** `20260519120000_AddHiddenFromQueueDisplayToCustomerOrder`

```sql
ALTER TABLE `CustomerOrders`
  ADD COLUMN `HiddenFromQueueDisplay` tinyint(1) NOT NULL DEFAULT 0;
```

---

## 4. ملاحظات بند الطلب

**Migration:** `20260521100000_AddNotesToCustomerOrderItem`

```sql
ALTER TABLE `CustomerOrderItems`
  ADD COLUMN `Notes` varchar(500) CHARACTER SET utf8mb4 NULL;
```

---

## 5. العملاء والآجل

### جدول العملاء
**Migration:** `20260509113845_AddCustomersTable`

```sql
CREATE TABLE IF NOT EXISTS `Customers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
  `PhoneNumber` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
  `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
  `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_Customers_InsertByUserId` (`InsertByUserId`),
  CONSTRAINT `FK_Customers_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### إزالة البريد من العملاء
**Migration:** `20260509150000_RemoveEmailFromCustomer`

```sql
ALTER TABLE `Customers` DROP COLUMN `Email`;
```

### ربط الطلب بحساب آجل (عميل / موظف)
**Migration:** `20260509170000_AddCreditAccountToCustomerOrder`

```sql
ALTER TABLE `CustomerOrders`
  ADD COLUMN `CreditCustomerId` int NULL,
  ADD COLUMN `CreditEmployeeId` int NULL;

CREATE INDEX `IX_CustomerOrders_CreditCustomerId` ON `CustomerOrders` (`CreditCustomerId`);
CREATE INDEX `IX_CustomerOrders_CreditEmployeeId` ON `CustomerOrders` (`CreditEmployeeId`);

ALTER TABLE `CustomerOrders`
  ADD CONSTRAINT `FK_CustomerOrders_Customers_CreditCustomerId`
    FOREIGN KEY (`CreditCustomerId`) REFERENCES `Customers` (`Id`) ON DELETE SET NULL,
  ADD CONSTRAINT `FK_CustomerOrders_Employees_CreditEmployeeId`
    FOREIGN KEY (`CreditEmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE SET NULL;
```

---

## 6. مرتجعات الأصناف (`ReturnedOrderItems`)

**Migration:** `20260512073534_AddReturnedOrderItems`

```sql
CREATE TABLE IF NOT EXISTS `ReturnedOrderItems` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CustomerOrderId` int NOT NULL,
  `CustomerOrderItemId` int NOT NULL,
  `TableId` int NULL,
  `ItemId` int NOT NULL,
  `ItemName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
  `OrderCode` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
  `TableNumber` varchar(80) CHARACTER SET utf8mb4 NULL,
  `MergedTableNumbers` varchar(250) CHARACTER SET utf8mb4 NULL,
  `OrderType` varchar(50) CHARACTER SET utf8mb4 NULL,
  `PaymentMethod` varchar(50) CHARACTER SET utf8mb4 NULL,
  `Quantity` int NOT NULL,
  `UnitPrice` decimal(18,2) NOT NULL,
  `LineTotal` decimal(18,2) NOT NULL,
  `Reason` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
  `DeletedByUserId` int NOT NULL,
  `DeletedByUsername` varchar(120) CHARACTER SET utf8mb4 NULL,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_ReturnedOrderItems_CustomerOrderId` (`CustomerOrderId`),
  KEY `IX_ReturnedOrderItems_CustomerOrderItemId` (`CustomerOrderItemId`),
  KEY `IX_ReturnedOrderItems_DeletedByUserId` (`DeletedByUserId`),
  KEY `IX_ReturnedOrderItems_InsertByUserId` (`InsertByUserId`),
  KEY `IX_ReturnedOrderItems_ItemId` (`ItemId`),
  KEY `IX_ReturnedOrderItems_TableId` (`TableId`),
  CONSTRAINT `FK_ReturnedOrderItems_CustomerOrders_CustomerOrderId`
    FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrders` (`Id`),
  CONSTRAINT `FK_ReturnedOrderItems_CustomerOrderItems_CustomerOrderItemId`
    FOREIGN KEY (`CustomerOrderItemId`) REFERENCES `CustomerOrderItems` (`Id`),
  CONSTRAINT `FK_ReturnedOrderItems_Items_ItemId`
    FOREIGN KEY (`ItemId`) REFERENCES `Items` (`Id`),
  CONSTRAINT `FK_ReturnedOrderItems_Tables_TableId`
    FOREIGN KEY (`TableId`) REFERENCES `Tables` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ReturnedOrderItems_Users_DeletedByUserId`
    FOREIGN KEY (`DeletedByUserId`) REFERENCES `Users` (`Id`),
  CONSTRAINT `FK_ReturnedOrderItems_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 7. الأقسام الفرعية (`Tags`)

**Migration:** `20260429195841_EnsureTagParentTagId`

```sql
ALTER TABLE `Tags`
  ADD COLUMN `ParentTagId` int NULL;

CREATE INDEX `IX_Tags_ParentTagId` ON `Tags` (`ParentTagId`);

ALTER TABLE `Tags`
  ADD CONSTRAINT `FK_Tags_Tags_ParentTagId`
    FOREIGN KEY (`ParentTagId`) REFERENCES `Tags` (`Id`);
```

---

## 8. مخطط الطوابق

### إعدادات المخطط + موضع الطاولة
**Migration:** `20260504163532_AddTableLayoutAndFloorPlanSettings`

```sql
ALTER TABLE `Tables`
  ADD COLUMN `LayoutPosX` double NULL,
  ADD COLUMN `LayoutPosY` double NULL;

CREATE TABLE IF NOT EXISTS `RestaurantLayoutSettings` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `InsertByUserId` int NOT NULL,
  `FloorPlanImageFileName` longtext CHARACTER SET utf8mb4 NULL,
  `BackgroundColor` longtext CHARACTER SET utf8mb4 NULL,
  `ZonesJson` longtext CHARACTER SET utf8mb4 NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_RestaurantLayoutSettings_InsertByUserId` (`InsertByUserId`),
  CONSTRAINT `FK_RestaurantLayoutSettings_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### مخططات متعددة (طوابق)
**Migration:** `20260504170547_MultiFloorFloorPlans`

```sql
ALTER TABLE `RestaurantLayoutSettings`
  ADD COLUMN `PlanKey` varchar(128) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

CREATE UNIQUE INDEX `IX_RestaurantLayoutSettings_InsertByUserId_PlanKey`
  ON `RestaurantLayoutSettings` (`InsertByUserId`, `PlanKey`);

ALTER TABLE `RestaurantLayoutSettings`
  DROP INDEX `IX_RestaurantLayoutSettings_InsertByUserId`;

CREATE TABLE IF NOT EXISTS `TableLayoutPlacements` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `TableId` int NOT NULL,
  `PlanKey` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
  `LayoutPosX` double NOT NULL,
  `LayoutPosY` double NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_TableLayoutPlacements_TableId_PlanKey` (`TableId`, `PlanKey`),
  CONSTRAINT `FK_TableLayoutPlacements_Tables_TableId`
    FOREIGN KEY (`TableId`) REFERENCES `Tables` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- نقل المواضع القديمة من Tables
INSERT INTO `TableLayoutPlacements` (`TableId`, `PlanKey`, `LayoutPosX`, `LayoutPosY`, `InsertDate`, `UpdateDate`, `IsDeleted`)
SELECT
  `Id`,
  IF(IFNULL(TRIM(`Zone`), '') = '', '', TRIM(`Zone`)),
  `LayoutPosX`,
  `LayoutPosY`,
  UTC_TIMESTAMP(6),
  UTC_TIMESTAMP(6),
  0
FROM `Tables`
WHERE `LayoutPosX` IS NOT NULL
  AND `LayoutPosY` IS NOT NULL
  AND `IsDeleted` = 0;
```

### حجم شريحة الطاولة على المخطط
**Migration:** `20260515120000_AddFloorPlanTableChipSize`

```sql
ALTER TABLE `RestaurantLayoutSettings`
  ADD COLUMN `TableChipSizePx` int NULL;
```

---

## 9. المخزون (`StockMovements`)

> **ملاحظة:** ملفات `20260219*` موجودة في المشروع لكنها **خارج سلسلة EF الرسمية**. الإنشاء الفعلي للجداول يتم عبر `MigrationBootstrapSql` داخل migration `20260501184146_AddStockMovementReceiptNumber` (Employees + StockMovements + Suppliers + Expenses.EmployeeId) باستخدام `CREATE TABLE IF NOT EXISTS`.

**Migration:** `20260219120000_AddInventoryAndStockMovements` *(مرجع يدوي — ليس في سلسلة EF)*

```sql
CREATE TABLE IF NOT EXISTS `StockMovements` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MaterialName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
  `MovementType` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
  `Quantity` decimal(18,2) NOT NULL,
  `SupplierName` varchar(200) CHARACTER SET utf8mb4 NULL,
  `Amount` decimal(18,2) NULL,
  `UnitType` varchar(50) CHARACTER SET utf8mb4 NULL,
  `ReceiptAttachmentPath` varchar(500) CHARACTER SET utf8mb4 NULL,
  `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_StockMovements_InsertByUserId` (`InsertByUserId`),
  CONSTRAINT `FK_StockMovements_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

**Migration:** `20260501184146_AddStockMovementReceiptNumber`

```sql
ALTER TABLE `StockMovements`
  ADD COLUMN `ReceiptNumber` varchar(200) CHARACTER SET utf8mb4 NULL;
```

**Migration:** `20260501201510_AddStockMovementReceivedByEmployeeName`

```sql
ALTER TABLE `StockMovements`
  ADD COLUMN `ReceivedByEmployeeName` varchar(200) CHARACTER SET utf8mb4 NULL;
```

---

## 10. الموظفين والصرفيات

### جدول الموظفين
**Migration:** `20260219000000_AddEmployeesTable`

```sql
CREATE TABLE IF NOT EXISTS `Employees` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
  `PhoneNumber` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
  `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
  `JobTitle` varchar(200) CHARACTER SET utf8mb4 NULL,
  `Salary` decimal(18,2) NOT NULL DEFAULT 0,
  `SalaryType` int NOT NULL DEFAULT 0,
  `TagId` int NULL,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_Employees_InsertByUserId` (`InsertByUserId`),
  KEY `IX_Employees_TagId` (`TagId`),
  CONSTRAINT `FK_Employees_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`),
  CONSTRAINT `FK_Employees_Tags_TagId`
    FOREIGN KEY (`TagId`) REFERENCES `Tags` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### جداول الصرفيات
**Migration:** `20260112194004_AddExspinsev`

```sql
CREATE TABLE IF NOT EXISTS `ExpenseCategories` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
  `Color` varchar(50) CHARACTER SET utf8mb4 NULL,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_ExpenseCategories_InsertByUserId` (`InsertByUserId`),
  CONSTRAINT `FK_ExpenseCategories_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Expenses` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Amount` decimal(18,2) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `Category` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 NULL,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_Expenses_InsertByUserId` (`InsertByUserId`),
  CONSTRAINT `FK_Expenses_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### ربط الصرفية بالموظف
**Migration:** `20260219100000_AddEmployeeIdToExpenses`

```sql
ALTER TABLE `Expenses`
  ADD COLUMN `EmployeeId` int NULL;

CREATE INDEX `IX_Expenses_EmployeeId` ON `Expenses` (`EmployeeId`);

ALTER TABLE `Expenses`
  ADD CONSTRAINT `FK_Expenses_Employees_EmployeeId`
    FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE SET NULL;
```

### ربط الصرفية بقسم (`Tags`)
**Migration:** `20260515133000_AddExpenseTagId`

```sql
ALTER TABLE `Expenses`
  ADD COLUMN `TagId` int NULL;

CREATE INDEX `IX_Expenses_TagId` ON `Expenses` (`TagId`);

ALTER TABLE `Expenses`
  ADD CONSTRAINT `FK_Expenses_Tags_TagId`
    FOREIGN KEY (`TagId`) REFERENCES `Tags` (`Id`) ON DELETE SET NULL;
```

---

## 11. دمج الطاولات بالطلب (`OrderTables`)

**Migration:** `20260117181249_AddOrderTablesTable`

```sql
CREATE TABLE IF NOT EXISTS `OrderTables` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `TableId` int NOT NULL,
  `IsPrimary` tinyint(1) NOT NULL DEFAULT 0,
  `InsertByUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_OrderTables_InsertByUserId` (`InsertByUserId`),
  KEY `IX_OrderTables_OrderId` (`OrderId`),
  KEY `IX_OrderTables_TableId` (`TableId`),
  CONSTRAINT `FK_OrderTables_CustomerOrders_OrderId`
    FOREIGN KEY (`OrderId`) REFERENCES `CustomerOrders` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_OrderTables_Tables_TableId`
    FOREIGN KEY (`TableId`) REFERENCES `Tables` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_OrderTables_Users_InsertByUserId`
    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 12. سجل التدقيق (`AuditLogs`)

**Migration:** `20260114081020_AddAuditLogTable`

```sql
CREATE TABLE IF NOT EXISTS `AuditLogs` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Action` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
  `EntityType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
  `EntityId` int NOT NULL,
  `EntityName` varchar(500) CHARACTER SET utf8mb4 NULL,
  `OldValues` text CHARACTER SET utf8mb4 NULL,
  `NewValues` text CHARACTER SET utf8mb4 NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 NULL,
  `UserId` int NOT NULL,
  `CommercialUserId` int NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_AuditLogs_UserId` (`UserId`),
  KEY `IX_AuditLogs_CommercialUserId` (`CommercialUserId`),
  CONSTRAINT `FK_AuditLogs_Users_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`),
  CONSTRAINT `FK_AuditLogs_Users_CommercialUserId`
    FOREIGN KEY (`CommercialUserId`) REFERENCES `Users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 13. صلاحيات المستخدم (`Users`)

### رمز دخول سريع (PIN)
**Migration:** `20260504155614_AddUserLoginCode`

```sql
ALTER TABLE `Users`
  ADD COLUMN `LoginCode` varchar(20) CHARACTER SET utf8mb4 NULL;

CREATE UNIQUE INDEX `IX_Users_LoginCode` ON `Users` (`LoginCode`);
```

### أقسام مسموحة للمستخدم (JSON)
**Migration:** `20260516120000_AddUserAllowedSectionsJson`

```sql
ALTER TABLE `Users`
  ADD COLUMN `AllowedSectionsJson` varchar(2000) CHARACTER SET utf8mb4 NULL;
```

### السماح باستخدام رمز المدير للعمليات الحساسة
**Migration:** `20260516130000_AddManagerSensitiveLoginCodeOption`

```sql
ALTER TABLE `Users`
  ADD COLUMN `CanUseOwnLoginCodeForSensitiveActions` tinyint(1) NOT NULL DEFAULT 0;
```

---

## كويريات تحقق

### التحقق من وجود الجداول الجديدة

```sql
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN (
    'PaymentDevices',
    'CardPaymentTransactions',
    'Customers',
    'ReturnedOrderItems',
    'RestaurantLayoutSettings',
    'TableLayoutPlacements',
    'StockMovements',
    'Employees',
    'Expenses',
    'ExpenseCategories',
    'OrderTables',
    'AuditLogs'
  )
ORDER BY TABLE_NAME;
```

### التحقق من أعمدة `CustomerOrders` المُضافة

```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'CustomerOrders'
  AND COLUMN_NAME IN (
    'DiscountAmount', 'DiscountPercent', 'DiscountType', 'DiscountValue',
    'OrderSubTotal', 'OrderTotalAfterDiscount',
    'NumberOfGuests', 'HiddenFromQueueDisplay',
    'CreditCustomerId', 'CreditEmployeeId'
  )
ORDER BY COLUMN_NAME;
```

### التحقق من تسجيل الـ migrations في EF

```sql
SELECT `MigrationId`, `ProductVersion`
FROM `__EFMigrationsHistory`
ORDER BY `MigrationId` DESC
LIMIT 30;
```

---

## ترتيب التطبيق اليدوي (مقترح)

إذا كانت القاعدة قديمة وتحتاج كل التحديثات دفعة واحدة، طبّق بالترتيب:

1. `Tags` → `ParentTagId`
2. `AuditLogs`, `OrderTables`, `ExpenseCategories`, `Expenses`
3. `Employees` → ثم `Expenses.EmployeeId`
4. `StockMovements` + أعمدة الإيصال
5. `RestaurantLayoutSettings` → `TableLayoutPlacements` → `TableChipSizePx`
6. `CustomerOrders` (خصم، ضيوف، آجل، شاشة طابور)
7. `CustomerOrderItems.Notes`
8. `Customers` → ربط الآجل
9. `ReturnedOrderItems`
10. `Users` (LoginCode, AllowedSectionsJson, CanUseOwnLoginCode...)
11. `Expenses.TagId`
12. **`PaymentDevices` + `CardPaymentTransactions`** (الأحدث)

---

## ملفات SQL إضافية في المشروع

| الملف | الغرض |
|-------|--------|
| `Migrations/Apply_Employees_And_ExpenseEmployeeId.sql` | تطبيق يدوي للموظفين |
| `Migrations/Apply_ExpenseTagId.sql` | `TagId` على الصرفيات |
| `DatabaseScripts/ensure_expense_tag_id_mysql.sql` | التحقق من `TagId` |
| `DatabaseScripts/ensure_table_chip_size_px_mysql.sql` | التحقق من حجم الشريحة |
| `Scripts/AddTagParentTagId_Manual.sql` | `ParentTagId` يدوياً |
| `Migrations/fix_missing_feb2026_migrations.sql` | إصلاح migrations ناقصة |

---

---

## جداول المزامنة (محلية فقط — لا تُرفع للسحابة)

**Migration:** `20260620120000_AddSyncTables`

```sql
-- SyncRuns, SyncWatermarks, SyncFileWatermarks, SyncSettings
-- راجع ملف Migration: 20260620120000_AddSyncTables.cs
```

إعدادات التطبيق في `appsettings.json`:

- `ConnectionStrings:SyncDatabase` — اتصال السحابة (يُفضّل User Secrets)
- `SyncSettings` — Enabled, BatchSize, FTP, ImagesLocalPath

