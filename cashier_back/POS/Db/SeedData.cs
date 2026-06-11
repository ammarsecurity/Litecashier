using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POS.Authorization;
using POS.Models;

namespace POS.Db
{
    /// <summary>
    /// بيانات تجريبية للمتجر (سوبر ماركت).
    /// حسابات العرض: كلمة المرور لجميع الحسابات = 12345678
    /// الأدمن: 07830200030 | التجاري: 07800000001 (رمز: 123456) | كاشير: 07800000002 | قارئ: 07800000003 | مدير: 07800000004
    /// </summary>
    public static class SeedData
    {
        private const string DemoPassword = "12345678";
        private const string DemoCommercialPhone = "07800000001";
        private const string DemoCommercialLoginCode = "123456";
        private const string DemoStoreName = "متجر لايت كاشير التجريبي";

        public sealed class SeedSummary
        {
            public int CommercialUserId { get; set; }
            public int Tags { get; set; }
            public int Items { get; set; }
            public int Suppliers { get; set; }
            public int Employees { get; set; }
            public int ExpenseCategories { get; set; }
            public int Expenses { get; set; }
            public int Customers { get; set; }
            public int StockMovements { get; set; }
            public int Printers { get; set; }
            public int Orders { get; set; }
            public int DemoUsersCreated { get; set; }
            public int AdditionalCatalogsSeeded { get; set; }

            public string ToMessage()
            {
                var msg =
                    $"تمت إضافة البيانات للمستخدم التجاري #{CommercialUserId}: " +
                    $"{Tags} أقسام، {Items} منتجات، {Suppliers} موردين، {Employees} موظفين، " +
                    $"{ExpenseCategories} فئات مصروفات، {Expenses} مصروفات، {Customers} زبائن، " +
                    $"{StockMovements} حركات مخزون، {Printers} طابعات، {Orders} فواتير" +
                    (DemoUsersCreated > 0 ? $"، {DemoUsersCreated} حسابات تجريبية" : "");

                if (AdditionalCatalogsSeeded > 0)
                    msg += $"؛ تم ملء المواد والأقسام لـ {AdditionalCatalogsSeeded} حساب تجاري إضافي فارغ";

                return msg;
            }
        }

        /// <summary>
        /// يملأ الأقسام (Tags) والمواد (Items) فقط — للاستخدام عند الحاجة لكتالوج بدون باقي البيانات.
        /// </summary>
        public static (int Tags, int Items) SeedCatalogOnly(DbConfig context, int commercialUserId)
        {
            var commercialUser = context.Users.FirstOrDefault(u =>
                u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);
            if (commercialUser == null)
                throw new Exception($"Commercial user with ID {commercialUserId} not found.");

            var tagsAdded = SeedTags(context, commercialUserId);
            var dbTags = context.Tags
                .Where(t => t.InsertByUserId == commercialUserId && !t.IsDeleted)
                .ToList();
            var itemsAdded = SeedItems(context, commercialUserId, dbTags);
            return (tagsAdded, itemsAdded);
        }

        /// <summary>
        /// يملأ المواد والأقسام لكل حساب تجاري لا يملك منتجات بعد.
        /// </summary>
        public static int SeedCatalogForEmptyCommercialAccounts(DbConfig context, int? skipCommercialUserId = null)
        {
            var commercialIds = context.Users
                .Where(u => u.Role == "Commercial" && !u.IsDeleted)
                .Select(u => u.Id)
                .ToList();

            var seeded = 0;
            foreach (var commercialId in commercialIds)
            {
                if (skipCommercialUserId.HasValue && commercialId == skipCommercialUserId.Value)
                    continue;

                var hasItems = context.Items.Any(i => i.InsertByUserId == commercialId && !i.IsDeleted);
                if (hasItems)
                    continue;

                SeedCatalogOnly(context, commercialId);
                seeded++;
            }

            return seeded;
        }

        /// <summary>
        /// ينشئ حسابات العرض (تجاري + كاشير + قارئ + مدير) إن لم تكن موجودة.
        /// </summary>
        public static (int CommercialUserId, int UsersCreated) EnsureDemoAccounts(DbConfig context)
        {
            var created = 0;
            var now = DateTime.UtcNow;
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

            var commercial = context.Users.FirstOrDefault(u =>
                u.PhoneNumber == DemoCommercialPhone && u.Role == "Commercial" && !u.IsDeleted);

            if (commercial == null)
            {
                commercial = new User
                {
                    Name = "أحمد التاجر",
                    PhoneNumber = DemoCommercialPhone,
                    Username = "demo_store",
                    Password = passwordHash,
                    Role = "Commercial",
                    InsertByUserId = 1,
                    StoreName = DemoStoreName,
                    LoginCode = DemoCommercialLoginCode,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                };
                context.Users.Add(commercial);
                context.SaveChanges();
                created++;
            }

            var commercialId = commercial.Id;
            var managerSections = JsonSerializer.Serialize(SectionDefinitions.AssignableSectionKeys);

            created += EnsureSubUser(context, commercialId, "07800000002", "سارة الكاشير", "demo_pos", "POS", passwordHash, now);
            created += EnsureSubUser(context, commercialId, "07800000003", "علي القارئ", "demo_reader", "Reader", passwordHash, now);
            created += EnsureSubUser(
                context, commercialId, "07800000004", "محمد المدير", "demo_manager", "Manager",
                passwordHash, now, managerSections, canUseOwnLoginCode: true, loginCode: "9999");

            return (commercialId, created);
        }

        private static int EnsureSubUser(
            DbConfig context,
            int commercialId,
            string phone,
            string name,
            string username,
            string role,
            string passwordHash,
            DateTime now,
            string? allowedSectionsJson = null,
            bool canUseOwnLoginCode = false,
            string? loginCode = null)
        {
            if (context.Users.Any(u => u.PhoneNumber == phone && !u.IsDeleted))
                return 0;

            context.Users.Add(new User
            {
                Name = name,
                PhoneNumber = phone,
                Username = username,
                Password = passwordHash,
                Role = role,
                InsertByUserId = commercialId,
                AllowedSectionsJson = allowedSectionsJson,
                CanUseOwnLoginCodeForSensitiveActions = canUseOwnLoginCode,
                LoginCode = loginCode,
                InsertDate = now,
                UpdateDate = now,
                IsDeleted = false
            });
            context.SaveChanges();
            return 1;
        }

        public static SeedSummary SeedDatabase(DbConfig context, int commercialUserId)
        {
            var summary = new SeedSummary { CommercialUserId = commercialUserId };

            var commercialUser = context.Users.FirstOrDefault(u =>
                u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);
            if (commercialUser == null)
                throw new Exception($"Commercial user with ID {commercialUserId} not found.");

            var posUserId = context.Users
                .Where(u => u.InsertByUserId == commercialUserId && u.Role == "POS" && !u.IsDeleted)
                .Select(u => u.Id)
                .FirstOrDefault();
            var orderUserId = posUserId > 0 ? posUserId : commercialUserId;

            var catalog = SeedCatalogOnly(context, commercialUserId);
            summary.Tags = catalog.Tags;
            summary.Items = catalog.Items;
            var dbTags = context.Tags
                .Where(t => t.InsertByUserId == commercialUserId && !t.IsDeleted)
                .ToList();
            summary.Suppliers = SeedSuppliers(context, commercialUserId);
            summary.Employees = SeedEmployees(context, commercialUserId, dbTags);
            summary.ExpenseCategories = SeedExpenseCategories(context, commercialUserId);
            summary.Expenses = SeedExpenses(context, commercialUserId);
            summary.Customers = SeedCustomers(context, commercialUserId);
            summary.StockMovements = SeedStockMovements(context, commercialUserId);
            summary.Printers = SeedPrinters(context, commercialUserId);
            summary.Orders = SeedSampleOrders(context, commercialUserId, orderUserId);

            return summary;
        }

        /// <summary>
        /// إنشاء الحسابات التجريبية ثم ملء كل البيانات.
        /// </summary>
        public static SeedSummary SeedDemoEnvironment(DbConfig context)
        {
            var (commercialId, usersCreated) = EnsureDemoAccounts(context);
            var summary = SeedDatabase(context, commercialId);
            summary.DemoUsersCreated = usersCreated;
            summary.AdditionalCatalogsSeeded = SeedCatalogForEmptyCommercialAccounts(context, commercialId);
            return summary;
        }

        private static int SeedTags(DbConfig context, int commercialUserId)
        {
            var categoryNames = new[]
            {
                "مواد غذائية أساسية",
                "مشروبات",
                "منتجات الألبان والبيض",
                "خضروات طازجة",
                "فواكه طازجة",
                "لحوم ودواجن",
                "مواد تنظيف",
                "مواد صحية",
                "حلويات وسكاكر",
                "مواد منزلية"
            };

            var tags = new List<Tag>();
            var now = DateTime.UtcNow;
            foreach (var name in categoryNames)
            {
                if (context.Tags.Any(t => t.Name == name && t.InsertByUserId == commercialUserId && !t.IsDeleted))
                    continue;

                tags.Add(new Tag
                {
                    Name = name,
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsForAll = false,
                    IsDeleted = false
                });
            }

            if (tags.Count == 0) return 0;
            context.Tags.AddRange(tags);
            context.SaveChanges();
            return tags.Count;
        }

        private static int SeedItems(DbConfig context, int commercialUserId, List<Tag> dbTags)
        {
            if (!dbTags.Any())
                throw new Exception("No tags found. Seed tags first.");

            var itemsToAdd = RetailCatalog.Items;
            var items = new List<Item>();
            var random = new Random(42);
            var now = DateTime.UtcNow;
            var codeCounter = 1;

            foreach (var itemData in itemsToAdd)
            {
                var code = $"ITEM{commercialUserId}{codeCounter:D4}";
                codeCounter++;

                if (context.Items.Any(i =>
                        i.InsertByUserId == commercialUserId && !i.IsDeleted &&
                        (i.Code == code || i.Name == itemData.Name)))
                    continue;

                var tag = dbTags.FirstOrDefault(t => t.Name == itemData.Category) ?? dbTags.First();
                var sellingPrice = itemData.SellingPrice;
                var purchasingPrice = Math.Round(sellingPrice * (decimal)(0.65 + random.NextDouble() * 0.1), 2);
                var discountPrice = random.Next(10) < 2
                    ? Math.Round(sellingPrice * (decimal)(0.85 + random.NextDouble() * 0.1), 2)
                    : 0m;

                items.Add(new Item
                {
                    Name = itemData.Name,
                    Description = $"منتج {itemData.Name}",
                    Code = code,
                    SellingPrice = sellingPrice,
                    PurchasingPrice = purchasingPrice,
                    DisCountPrice = discountPrice,
                    Tags = tag.Name,
                    Quantity = random.Next(15, 120),
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                });
            }

            if (items.Count == 0) return 0;
            context.Items.AddRange(items);
            context.SaveChanges();
            return items.Count;
        }

        private static int SeedSuppliers(DbConfig context, int commercialUserId)
        {
            var names = new[]
            {
                ("شركة الغذاء المتحدة", "مورد مواد غذائية وتموين"),
                ("مورد الألبان الطازجة", "حليب وأجبان"),
                ("شركة النظافة العراقية", "مواد تنظيف وصحية"),
                ("مورد الخضار المركزي", "خضروات وفواكه"),
                ("توزيع المشروبات", "مشروبات غازية وعصائر")
            };

            var added = 0;
            var now = DateTime.UtcNow;
            foreach (var (name, notes) in names)
            {
                if (context.Suppliers.Any(s => s.Name == name && s.InsertByUserId == commercialUserId && !s.IsDeleted))
                    continue;

                context.Suppliers.Add(new Supplier
                {
                    Name = name,
                    Notes = notes,
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                });
                added++;
            }

            if (added > 0) context.SaveChanges();
            return added;
        }

        private static int SeedEmployees(DbConfig context, int commercialUserId, List<Tag> dbTags)
        {
            var foodTag = dbTags.FirstOrDefault(t => t.Name == "مواد غذائية أساسية");
            var cleaningTag = dbTags.FirstOrDefault(t => t.Name == "مواد تنظيف");

            var employees = new[]
            {
                ("حسين كريم", "07801111001", "مدير المتجر", 750000m, SalaryType.Monthly, (int?)null),
                ("زينب محمد", "07801111002", "كاشير", 450000m, SalaryType.Monthly, foodTag?.Id),
                ("عمر سالم", "07801111003", "مخزن", 400000m, SalaryType.Monthly, foodTag?.Id),
                ("نور علي", "07801111004", "تنظيف", 25000m, SalaryType.Daily, cleaningTag?.Id),
                ("ياسر فاضل", "07801111005", "مندوب توصيل", 30000m, SalaryType.Daily, (int?)null)
            };

            var added = 0;
            var now = DateTime.UtcNow;
            foreach (var (name, phone, job, salary, salaryType, tagId) in employees)
            {
                if (context.Employees.Any(e => e.PhoneNumber == phone && e.InsertByUserId == commercialUserId && !e.IsDeleted))
                    continue;

                context.Employees.Add(new Employee
                {
                    Name = name,
                    PhoneNumber = phone,
                    JobTitle = job,
                    Salary = salary,
                    SalaryType = salaryType,
                    TagId = tagId,
                    Address = "بغداد",
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                });
                added++;
            }

            if (added > 0) context.SaveChanges();
            return added;
        }

        private static int SeedExpenseCategories(DbConfig context, int commercialUserId)
        {
            var categories = new[]
            {
                ("إيجار", "إيجار المحل الشهري", "#6366f1"),
                ("رواتب", "رواتب الموظفين", "#22c55e"),
                ("كهرباء وماء", "فواتير الخدمات", "#f59e0b"),
                ("صيانة", "صيانة معدات وأجهزة", "#ef4444"),
                ("تسويق", "إعلانات وعروض", "#8b5cf6"),
                ("نقل وتوصيل", "مصاريف النقل", "#06b6d4"),
                ("مشتريات طارئة", "مشتريات غير مخططة", "#64748b")
            };

            var added = 0;
            var now = DateTime.UtcNow;
            foreach (var (name, description, color) in categories)
            {
                if (context.ExpenseCategories.Any(c => c.Name == name && c.InsertByUserId == commercialUserId && !c.IsDeleted))
                    continue;

                context.ExpenseCategories.Add(new ExpenseCategory
                {
                    Name = name,
                    Description = description,
                    Color = color,
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                });
                added++;
            }

            if (added > 0) context.SaveChanges();
            return added;
        }

        private static int SeedExpenses(DbConfig context, int commercialUserId)
        {
            if (context.Expenses.Any(e => e.InsertByUserId == commercialUserId && !e.IsDeleted))
                return 0;

            var manager = context.Employees
                .FirstOrDefault(e => e.InsertByUserId == commercialUserId && e.JobTitle == "مدير المتجر" && !e.IsDeleted);

            var expenses = new[]
            {
                (850000m, -2, "إيجار", "إيجار شهر الحالي"),
                (120000m, -5, "كهرباء وماء", "فاتورة كهرباء"),
                (45000m, -1, "صيانة", "صيانة مكيف"),
                (75000m, -7, "تسويق", "منشورات إعلانية"),
                (200000m, -3, "رواتب", "سلفة موظف")
            };

            var now = DateTime.UtcNow;
            foreach (var (amount, daysAgo, category, description) in expenses)
            {
                context.Expenses.Add(new Expense
                {
                    Amount = amount,
                    Date = now.AddDays(daysAgo),
                    Category = category,
                    Description = description,
                    EmployeeId = category == "رواتب" ? manager?.Id : null,
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                });
            }

            context.SaveChanges();
            return expenses.Length;
        }

        private static int SeedCustomers(DbConfig context, int commercialUserId)
        {
            var customers = new[]
            {
                ("فاطمة أحمد", "07901234567", "بغداد - الكرادة", "زبونة دائمة"),
                ("كريم جواد", "07709876543", "بغداد - المنصور", null),
                ("مريم حسن", "07805551234", "بغداد - الشعب", "تفضل الدفع نقداً"),
                ("سامر عبد الله", "07501112233", null, "طلبات جملة"),
                ("رنا محمود", "07803334455", "بغداد - زيونة", null),
                ("باسم طارق", "07706667788", null, "زبون جديد")
            };

            var added = 0;
            var now = DateTime.UtcNow;
            foreach (var (name, phone, address, notes) in customers)
            {
                if (context.Customers.Any(c => c.PhoneNumber == phone && c.InsertByUserId == commercialUserId && !c.IsDeleted))
                    continue;

                context.Customers.Add(new Customer
                {
                    Name = name,
                    PhoneNumber = phone,
                    Address = address,
                    Notes = notes,
                    IsActive = true,
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                });
                added++;
            }

            if (added > 0) context.SaveChanges();
            return added;
        }

        private static int SeedStockMovements(DbConfig context, int commercialUserId)
        {
            if (context.StockMovements.Any(m => m.InsertByUserId == commercialUserId && !m.IsDeleted))
                return 0;

            var now = DateTime.UtcNow;
            var movements = new (string Material, string Type, decimal Qty, string Unit, string? Supplier, decimal? Amount, string? Receipt, string? ReceivedBy)[]
            {
                ("أرز بسمتي", "Add", 500m, "كيلو", "شركة الغذاء المتحدة", 12000000m, "RCP-1001", null),
                ("زيت نباتي", "Add", 200m, "لتر", "شركة الغذاء المتحدة", 7000000m, "RCP-1002", null),
                ("حليب طازج", "Add", 150m, "علبة", "مورد الألبان الطازجة", 3000000m, "RCP-1003", null),
                ("صابون غسيل", "Add", 80m, "كيس", "شركة النظافة العراقية", 2400000m, "RCP-1004", null),
                ("أرز بسمتي", "Withdraw", 25m, "كيلو", null, null, null, "زينب محمد"),
                ("حليب طازج", "Withdraw", 10m, "علبة", null, null, null, "عمر سالم")
            };

            foreach (var (material, type, qty, unit, supplier, amount, receipt, receivedBy) in movements)
            {
                context.StockMovements.Add(new StockMovement
                {
                    MaterialName = material,
                    MovementType = type,
                    Quantity = qty,
                    UnitType = unit,
                    SupplierName = supplier,
                    Amount = amount,
                    ReceiptNumber = receipt,
                    ReceivedByEmployeeName = receivedBy,
                    Notes = type == "Add" ? "توريد مخزون تجريبي" : "سحب للاستخدام اليومي",
                    InsertByUserId = commercialUserId,
                    InsertDate = now.AddDays(type == "Add" ? -10 : -1),
                    UpdateDate = now,
                    IsDeleted = false
                });
            }

            context.SaveChanges();
            return movements.Length;
        }

        private static int SeedPrinters(DbConfig context, int commercialUserId)
        {
            var printers = new[]
            {
                ("طابعة الكاشير الرئيسية", "طابعة فواتير نقطة البيع", "POS-Printer", "windows", true),
                ("طابعة المخزن", "طباعة تقارير المخزون", "Stock-Printer", "windows", false)
            };

            var added = 0;
            var now = DateTime.UtcNow;
            foreach (var (name, description, printerName, printerType, isMain) in printers)
            {
                if (context.Printers.Any(p => p.Name == name && p.InsertByUserId == commercialUserId && !p.IsDeleted))
                    continue;

                context.Printers.Add(new Printer
                {
                    Name = name,
                    Description = description,
                    PrinterName = printerName,
                    PrinterType = printerType,
                    PrintCategory = "Receipt",
                    IsActive = true,
                    IsMain = isMain,
                    InsertByUserId = commercialUserId,
                    InsertDate = now,
                    UpdateDate = now,
                    IsDeleted = false
                });
                added++;
            }

            if (added > 0) context.SaveChanges();
            return added;
        }

        private static int SeedSampleOrders(DbConfig context, int commercialUserId, int orderUserId)
        {
            if (context.CustomerOrders.Any(o => o.InsertByUserId == orderUserId && !o.IsDeleted))
                return 0;

            var dbItems = context.Items
                .Where(i => i.InsertByUserId == commercialUserId && !i.IsDeleted)
                .OrderBy(i => i.Id)
                .ToList();

            if (dbItems.Count < 8)
                return 0;

            var now = DateTime.UtcNow;
            var templates = new[]
            {
                new OrderTemplate("SEED-001", -1, "Cash", null, new[] { (0, 2), (1, 1), (16, 3) }),
                new OrderTemplate("SEED-002", -1, "Card", null, new[] { (5, 1), (6, 2), (25, 1) }),
                new OrderTemplate("SEED-003", -2, "Cash", null, new[] { (10, 2), (11, 1), (12, 1) }),
                new OrderTemplate("SEED-004", -3, "Cash", ("amount", 5000m), new[] { (20, 3), (21, 2), (22, 1) }),
                new OrderTemplate("SEED-005", -5, "BankTransfer", ("percentage", 10m), new[] { (30, 1), (31, 2), (32, 1), (33, 1) }),
                new OrderTemplate("SEED-006", -7, "Cash", null, new[] { (40, 2), (41, 1) }),
                new OrderTemplate("SEED-007", 0, "Cash", null, new[] { (2, 1), (3, 2), (4, 1), (7, 2) })
            };

            var ordersCreated = 0;
            foreach (var template in templates)
            {
                if (context.CustomerOrders.Any(o => o.OrderCode == template.Code && !o.IsDeleted))
                    continue;

                var lineItems = template.Lines
                    .Select(l => (Item: dbItems[l.ItemIndex], Qty: l.Qty))
                    .ToList();

                var subTotal = lineItems.Sum(x => x.Item.SellingPrice * x.Qty);
                decimal? discountAmount = null;
                decimal? discountPercent = null;
                decimal? discountValue = null;
                string? discountType = null;
                decimal total = subTotal;

                if (template.Discount is { } d)
                {
                    discountType = d.Type;
                    discountValue = d.Value;
                    if (d.Type == "amount")
                    {
                        discountAmount = Math.Min(d.Value, subTotal);
                        total = subTotal - discountAmount.Value;
                    }
                    else
                    {
                        discountPercent = d.Value;
                        discountAmount = Math.Round(subTotal * d.Value / 100m, 2);
                        total = subTotal - discountAmount.Value;
                    }
                }

                var orderDate = now.AddDays(template.DaysAgo);
                var order = new CustomerOrder
                {
                    OrderCode = template.Code,
                    PaymentMethod = template.PaymentMethod,
                    InsertByUserId = orderUserId,
                    DiscountType = discountType,
                    DiscountValue = discountValue,
                    DiscountAmount = discountAmount,
                    DiscountPercent = discountPercent,
                    OrderSubTotal = subTotal,
                    OrderTotalAfterDiscount = total,
                    InsertDate = orderDate,
                    UpdateDate = orderDate,
                    IsDeleted = false
                };
                context.CustomerOrders.Add(order);
                context.SaveChanges();

                foreach (var line in lineItems)
                {
                    context.CustomerOrderItems.Add(new CustomerOrderItem
                    {
                        ItemId = line.Item.Id,
                        CustomerOrderId = order.Id,
                        Quantity = line.Qty,
                        SellingPrice = line.Item.SellingPrice,
                        PurchasingPrice = line.Item.PurchasingPrice,
                        InsertByUserId = orderUserId,
                        InsertDate = orderDate,
                        UpdateDate = orderDate,
                        IsDeleted = false
                    });

                    line.Item.Quantity = Math.Max(0, line.Item.Quantity - line.Qty);
                }

                context.SaveChanges();
                ordersCreated++;
            }

            return ordersCreated;
        }

        private sealed record OrderTemplate(
            string Code,
            int DaysAgo,
            string PaymentMethod,
            (string Type, decimal Value)? Discount,
            (int ItemIndex, int Qty)[] Lines);

        private static class RetailCatalog
        {
            public static readonly (string Name, string Category, decimal SellingPrice)[] Items =
            {
                ("أرز بسمتي 5 كيلو", "مواد غذائية أساسية", 15000),
                ("سكر أبيض 1 كيلو", "مواد غذائية أساسية", 2000),
                ("زيت نباتي 1 لتر", "مواد غذائية أساسية", 3500),
                ("دقيق أبيض 1 كيلو", "مواد غذائية أساسية", 1500),
                ("معكرونة 500 جرام", "مواد غذائية أساسية", 1500),
                ("شاي أحمر 250 جرام", "مواد غذائية أساسية", 2500),
                ("قهوة عربية 250 جرام", "مواد غذائية أساسية", 5000),
                ("عدس 1 كيلو", "مواد غذائية أساسية", 3000),
                ("فاصوليا بيضاء 1 كيلو", "مواد غذائية أساسية", 4000),
                ("حمص 1 كيلو", "مواد غذائية أساسية", 3500),
                ("فول 1 كيلو", "مواد غذائية أساسية", 2500),
                ("زيت زيتون 500 مل", "مواد غذائية أساسية", 8000),
                ("خل 500 مل", "مواد غذائية أساسية", 1500),
                ("ملح 1 كيلو", "مواد غذائية أساسية", 500),
                ("فلفل أسود 100 جرام", "مواد غذائية أساسية", 2000),
                ("بهارات مشكلة 200 جرام", "مواد غذائية أساسية", 3000),
                ("ماء معدني 1.5 لتر", "مشروبات", 1000),
                ("عصير برتقال 1 لتر", "مشروبات", 2500),
                ("عصير تفاح 1 لتر", "مشروبات", 2500),
                ("مشروب غازي 1.5 لتر", "مشروبات", 2000),
                ("شاي مثلج 330 مل", "مشروبات", 1500),
                ("قهوة سريعة 50 جرام", "مشروبات", 3000),
                ("نسكافيه 50 جرام", "مشروبات", 4000),
                ("شاي أخضر 100 جرام", "مشروبات", 2000),
                ("عصير ليمون 1 لتر", "مشروبات", 3000),
                ("مشروب طاقة 250 مل", "مشروبات", 2000),
                ("حليب طازج 1 لتر", "منتجات الألبان والبيض", 2000),
                ("جبنة بيضاء 500 جرام", "منتجات الألبان والبيض", 4000),
                ("زبدة 250 جرام", "منتجات الألبان والبيض", 3000),
                ("بيض 30 حبة", "منتجات الألبان والبيض", 5000),
                ("جبنة شيدر 250 جرام", "منتجات الألبان والبيض", 5000),
                ("جبنة موتزاريلا 250 جرام", "منتجات الألبان والبيض", 4500),
                ("جبنة فيتا 250 جرام", "منتجات الألبان والبيض", 4000),
                ("لبنة 500 جرام", "منتجات الألبان والبيض", 3000),
                ("قشطة 250 جرام", "منتجات الألبان والبيض", 2500),
                ("زبادي 500 جرام", "منتجات الألبان والبيض", 2000),
                ("لبن 1 لتر", "منتجات الألبان والبيض", 2500),
                ("كريمة 250 مل", "منتجات الألبان والبيض", 3000),
                ("طماطم 1 كيلو", "خضروات طازجة", 2000),
                ("بصل 1 كيلو", "خضروات طازجة", 1500),
                ("ثوم 250 جرام", "خضروات طازجة", 2000),
                ("بطاطس 1 كيلو", "خضروات طازجة", 1500),
                ("جزر 1 كيلو", "خضروات طازجة", 2000),
                ("خيار 1 كيلو", "خضروات طازجة", 2500),
                ("فلفل 1 كيلو", "خضروات طازجة", 3000),
                ("باذنجان 1 كيلو", "خضروات طازجة", 2500),
                ("كوسا 1 كيلو", "خضروات طازجة", 2000),
                ("ملفوف 1 كيلو", "خضروات طازجة", 1500),
                ("خس 1 حبة", "خضروات طازجة", 1000),
                ("بقدونس 1 حزمة", "خضروات طازجة", 500),
                ("كزبرة 1 حزمة", "خضروات طازجة", 500),
                ("نعناع 1 حزمة", "خضروات طازجة", 1000),
                ("تفاح 1 كيلو", "فواكه طازجة", 3000),
                ("موز 1 كيلو", "فواكه طازجة", 2500),
                ("برتقال 1 كيلو", "فواكه طازجة", 2000),
                ("عنب 1 كيلو", "فواكه طازجة", 4000),
                ("فراولة 500 جرام", "فواكه طازجة", 3000),
                ("ليمون 1 كيلو", "فواكه طازجة", 2000),
                ("رمان 1 كيلو", "فواكه طازجة", 5000),
                ("مانجو 1 كيلو", "فواكه طازجة", 4000),
                ("بطيخ 1 حبة", "فواكه طازجة", 3000),
                ("شمام 1 حبة", "فواكه طازجة", 2500),
                ("لحم بقري 1 كيلو", "لحوم ودواجن", 25000),
                ("لحم غنم 1 كيلو", "لحوم ودواجن", 30000),
                ("دجاج كامل 1 كيلو", "لحوم ودواجن", 8000),
                ("صدور دجاج 1 كيلو", "لحوم ودواجن", 12000),
                ("سمك طازج 1 كيلو", "لحوم ودواجن", 15000),
                ("جمبري 500 جرام", "لحوم ودواجن", 20000),
                ("صابون 4 قطع", "مواد تنظيف", 3000),
                ("شامبو 400 مل", "مواد تنظيف", 5000),
                ("معجون أسنان 100 مل", "مواد تنظيف", 3000),
                ("مناديل 4 علب", "مواد تنظيف", 4000),
                ("منظف أرضيات 1 لتر", "مواد تنظيف", 2500),
                ("منظف زجاج 750 مل", "مواد تنظيف", 2000),
                ("مبيض 1 لتر", "مواد تنظيف", 2000),
                ("صابون غسيل 1 كيلو", "مواد تنظيف", 3000),
                ("منعم أقمشة 1 لتر", "مواد تنظيف", 4000),
                ("مطهر 1 لتر", "مواد تنظيف", 3000),
                ("إسفنجة تنظيف", "مواد تنظيف", 1000),
                ("قفازات مطبخ", "مواد تنظيف", 2000),
                ("صابون الوجه 100 جرام", "مواد صحية", 3000),
                ("كريم مرطب 200 مل", "مواد صحية", 6000),
                ("فرشاة أسنان", "مواد صحية", 2000),
                ("شامبو أطفال 400 مل", "مواد صحية", 4500),
                ("معجون أسنان أطفال", "مواد صحية", 2500),
                ("كريم واقي شمس", "مواد صحية", 8000),
                ("شوكولاتة 100 جرام", "حلويات وسكاكر", 2000),
                ("بسكويت 200 جرام", "حلويات وسكاكر", 1500),
                ("كيك 500 جرام", "حلويات وسكاكر", 3000),
                ("حلاوة 250 جرام", "حلويات وسكاكر", 2000),
                ("نوغا 200 جرام", "حلويات وسكاكر", 2500),
                ("شوكولاتة أطفال 100 جرام", "حلويات وسكاكر", 1500),
                ("حلوى جيلي", "حلويات وسكاكر", 1000),
                ("بونبون", "حلويات وسكاكر", 500),
                ("أكياس بلاستيك", "مواد منزلية", 2000),
                ("ورق المطبخ", "مواد منزلية", 1500),
                ("أكياس قمامة", "مواد منزلية", 3000),
                ("شمع", "مواد منزلية", 1000),
                ("أعواد ثقاب", "مواد منزلية", 500),
                ("شريط لاصق", "مواد منزلية", 1000),
                ("أكياس حفظ الطعام", "مواد منزلية", 2500)
            };
        }
    }
}
