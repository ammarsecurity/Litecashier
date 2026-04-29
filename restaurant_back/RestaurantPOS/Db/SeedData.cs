using RestaurantPOS.Models;
using RestaurantPOS.Models.Restaurant;
using Microsoft.EntityFrameworkCore;

namespace RestaurantPOS.Db
{
    public static class SeedData
    {
        public static void SeedDatabase(DbConfig context, int commercialUserId)
        {
            try
            {
                context.Database.EnsureCreated();

                // Check commercial user
                var commercialUser = context.Users.FirstOrDefault(u => u.Id == commercialUserId && u.Role == "Commercial" && !u.IsDeleted);
                if (commercialUser == null)
                    throw new Exception($"Commercial user with ID {commercialUserId} not found.");

                // ---------------------------
                // 1) Seed Tags (Categories)
                // ---------------------------

                var categoryNames = new List<string>
                {
                    "مقبلات",
                    "سلطات",
                    "شوربات",
                    "مشويات",
                    "أكلات عراقية",
                    "أطباق رئيسية",
                    "ساندويتشات",
                    "مشروبات ساخنة",
                    "مشروبات باردة",
                    "حلويات",
                    "عصائر طبيعية"
                };

                var tags = new List<Tag>();
                foreach (var name in categoryNames)
                {
                    if (!context.Tags.Any(t => t.Name == name && t.InsertByUserId == commercialUserId && !t.IsDeleted))
                    {
                        tags.Add(new Tag
                        {
                            Name = name,
                            InsertByUserId = commercialUserId,
                            InsertDate = DateTime.UtcNow,
                            UpdateDate = DateTime.UtcNow,
                            IsForAll = false,
                            IsDeleted = false
                        });
                    }
                }

                if (tags.Any())
                {
                    context.Tags.AddRange(tags);
                    context.SaveChanges();
                }

                // Load tags
                var dbTags = context.Tags.Where(t => t.InsertByUserId == commercialUserId && !t.IsDeleted).ToList();

                // ---------------------------
                // 2) Seed Items (Real Iraqi Dishes)
                // ---------------------------

                var itemsToAdd = new List<(string Name, string Tag, decimal Price)>
                {
                    // مقبلات
                    ("حمص", "مقبلات", 3000),
                    ("متبل", "مقبلات", 3000),
                    ("بابا غنوج", "مقبلات", 3500),
                    ("لبنة بالزيت", "مقبلات", 2500),
                    ("لبن وخيار", "مقبلات", 2500),

                    // سلطات
                    ("سلطة خضار", "سلطات", 2500),
                    ("سلطة فتوش", "سلطات", 3500),
                    ("سلطة تبولة", "سلطات", 4000),

                    // شوربات
                    ("شوربة عدس", "شوربات", 2000),
                    ("شوربة دجاج", "شوربات", 2500),

                    // مشويات
                    ("كباب عراقي", "مشويات", 8000),
                    ("تكة دجاج", "مشويات", 7000),
                    ("تكة لحم", "مشويات", 9000),
                    ("ريش مشوية", "مشويات", 12000),

                    // أكلات عراقية
                    ("برياني دجاج", "أكلات عراقية", 6000),
                    ("برياني لحم", "أكلات عراقية", 7000),
                    ("قوزي لحم", "أكلات عراقية", 10000),
                    ("تشريب دجاج", "أكلات عراقية", 5000),
                    ("تمن ومرق بامية", "أكلات عراقية", 4000),

                    // أطباق رئيسية
                    ("مندي دجاج", "أطباق رئيسية", 7000),
                    ("مندي لحم", "أطباق رئيسية", 9000),
                    ("كباب صينية", "أطباق رئيسية", 8500),

                    // ساندويتشات
                    ("شاورما دجاج", "ساندويتشات", 2500),
                    ("شاورما لحم", "ساندويتشات", 3000),
                    ("برگر دجاج", "ساندويتشات", 4000),
                    ("برگر لحم", "ساندويتشات", 4500),

                    // مشروبات ساخنة
                    ("شاي", "مشروبات ساخنة", 500),
                    ("قهوة تركية", "مشروبات ساخنة", 1500),
                    ("نسكافيه", "مشروبات ساخنة", 2000),

                    // مشروبات باردة
                    ("بيبسي", "مشروبات باردة", 1000),
                    ("سفن اب", "مشروبات باردة", 1000),
                    ("ميرندا", "مشروبات باردة", 1000),

                    // عصائر طبيعية
                    ("عصير برتقال", "عصائر طبيعية", 3000),
                    ("عصير رمان", "عصائر طبيعية", 4000),
                    ("عصير مانجو", "عصائر طبيعية", 3500),

                    // حلويات
                    ("كنافة", "حلويات", 4000),
                    ("مهلبية", "حلويات", 2500),
                    ("بقلاوة", "حلويات", 3000)
                };

                var items = new List<Item>();
                int codeCounter = 1;

                foreach (var item in itemsToAdd)
                {
                    var tagName = item.Tag;
                    var tag = dbTags.First(t => t.Name == tagName);

                    string code = $"ITEM{commercialUserId}{codeCounter:D4}";
                    codeCounter++;

                    if (!context.Items.Any(i => i.Code == code && i.InsertByUserId == commercialUserId))
                    {
                        items.Add(new Item
                        {
                            Name = item.Name,
                            Code = code,
                            Description = $"طبق {item.Name} من المطبخ العراقي",
                            SellingPrice = item.Price,
                            PurchasingPrice = item.Price * 0.65m,
                            DisCountPrice = 0,
                            IsAvailable = true,
                            Tags = tag.Name,
                            InsertByUserId = commercialUserId,
                            InsertDate = DateTime.UtcNow,
                            UpdateDate = DateTime.UtcNow,
                            IsDeleted = false
                        });
                    }
                }

                if (items.Any())
                {
                    context.Items.AddRange(items);
                    context.SaveChanges();
                }

                // ---------------------------
                // 3) Seed Tables
                // ---------------------------

                var tableList = new List<(string number, int capacity, string zone)>
                {
                    ("T1", 4, "داخلية"),
                    ("T2", 4, "داخلية"),
                    ("T3", 2, "شرفة"),
                    ("T4", 6, "خارجية"),
                    ("VIP1", 8, "VIP")
                };

                var tables = new List<Table>();
                foreach (var t in tableList)
                {
                    if (!context.Tables.Any(tb => tb.TableNumber == t.number && tb.InsertByUserId == commercialUserId))
                    {
                        tables.Add(new Table
                        {
                            TableNumber = t.number,
                            Capacity = t.capacity,
                            Zone = t.zone,
                            Status = "Available",
                            InsertByUserId = commercialUserId,
                            InsertDate = DateTime.UtcNow,
                            UpdateDate = DateTime.UtcNow,
                            IsDeleted = false
                        });
                    }
                }

                if (tables.Any())
                {
                    context.Tables.AddRange(tables);
                    context.SaveChanges();
                }

                // ---------------------------
                // 4) Reservation Sample
                // ---------------------------

                if (!context.Reservations.Any(r => r.InsertByUserId == commercialUserId))
                {
                    var tableId = context.Tables.First(t => t.InsertByUserId == commercialUserId).Id;

                    context.Reservations.Add(new Reservation
                    {
                        CustomerName = "حسن إبراهيم",
                        PhoneNumber = "07903332211",
                        NumberOfGuests = 4,
                        TableId = tableId,
                        Status = "Confirmed",
                        ReservationDateTime = DateTime.UtcNow.AddDays(1).AddHours(7),
                        InsertByUserId = commercialUserId,
                        InsertDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow,
                        IsDeleted = false
                    });

                    context.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding database: {ex.Message}", ex);
            }
        }
    }
}
