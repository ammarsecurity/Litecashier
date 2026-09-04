using Microsoft.EntityFrameworkCore;
using POS.Models;
using System.Text.Json;

namespace POS.Db
{
    public class DbConfig : DbContext
    {
        public DbConfig(DbContextOptions<DbConfig> options) : base(options)
        {
        }

        public async Task LogAuditAsync(string action, string entityType, int entityId, string? entityName, int userId, int commercialUserId, object? oldValues = null, object? newValues = null, string? description = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    EntityName = entityName,
                    UserId = userId,
                    CommercialUserId = commercialUserId,
                    Description = description,
                    InsertDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                if (oldValues != null)
                    auditLog.OldValues = JsonSerializer.Serialize(oldValues);

                if (newValues != null)
                    auditLog.NewValues = JsonSerializer.Serialize(newValues);

                AuditLogs.Add(auditLog);
                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging audit: {ex.Message}");
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<CustomerOrder> CustomerOrders { get; set; }
        public DbSet<CustomerOrderItem> CustomerOrderItems { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemCode> ItemCodes { get; set; }
        public DbSet<Printer> Printers { get; set; }
        public DbSet<TagPrinter> TagPrinters { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeAdvance> EmployeeAdvances { get; set; }
        public DbSet<SalaryAdjustment> SalaryAdjustments { get; set; }
        public DbSet<PayrollRun> PayrollRuns { get; set; }
        public DbSet<PayrollLine> PayrollLines { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<CatalogStockReturn> CatalogStockReturns { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<PaymentDevice> PaymentDevices { get; set; }
        public DbSet<CardPaymentTransaction> CardPaymentTransactions { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<ItemWarehouseStock> ItemWarehouseStocks { get; set; }
        public DbSet<PublicMenuAd> PublicMenuAds { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerOrder>().HasMany(o => o.CustomerOrderItem);
            modelBuilder.Entity<CustomerOrderItem>().HasOne(r => r.CustomerOrder).WithMany(r => r.CustomerOrderItem).HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.User).WithMany(r => r.CustomerOrders).HasForeignKey(x => x.InsertByUserId);
            modelBuilder.Entity<CustomerOrderItem>().HasOne(r => r.User).WithMany(r => r.CustomerOrderItem).HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Item>().HasOne(r => r.User).WithMany(r => r.Items).HasForeignKey(x => x.InsertByUserId);
            modelBuilder.Entity<Tag>().HasOne(r => r.User).WithMany(r => r.Tags).HasForeignKey(x => x.InsertByUserId);
            modelBuilder.Entity<User>().HasMany(r => r.Tags);

            modelBuilder.Entity<CustomerOrderItem>()
                   .HasOne(r => r.Item)
                   .WithMany(r => r.CustomerOrderItems)
                   .HasForeignKey(x => x.ItemId)
                   .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ItemCode>()
                .HasOne(c => c.Item)
                .WithMany(i => i.ItemCodes)
                .HasForeignKey(c => c.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemCode>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.InsertByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ItemCode>()
                .HasIndex(c => c.Code);

            modelBuilder.Entity<Printer>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<TagPrinter>().HasOne(r => r.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<TagPrinter>().HasOne(r => r.Printer).WithMany().HasForeignKey(x => x.PrinterId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<TagPrinter>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Employee>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Employee>().HasOne(r => r.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Expense>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Expense>().HasOne(r => r.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Expense>().HasOne(r => r.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ExpenseCategory>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<AuditLog>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<AuditLog>().HasOne(r => r.CommercialUser).WithMany().HasForeignKey(x => x.CommercialUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<StockMovement>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CatalogStockReturn>().HasOne(r => r.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CatalogStockReturn>().HasOne(r => r.CustomerOrder).WithMany().HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CatalogStockReturn>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CatalogStockReturn>().HasIndex(r => r.CustomerOrderId);
            modelBuilder.Entity<CatalogStockReturn>().HasIndex(r => r.ItemId);
            modelBuilder.Entity<CatalogStockReturn>().HasIndex(r => r.ReturnType);
            modelBuilder.Entity<Supplier>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Customer>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.CreditCustomer).WithMany().HasForeignKey(x => x.CreditCustomerId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.InsertByUserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Warehouse>()
                .HasIndex(w => new { w.InsertByUserId, w.Name });

            modelBuilder.Entity<PublicMenuAd>()
                .HasOne(a => a.CommercialUser)
                .WithMany()
                .HasForeignKey(a => a.CommercialUserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PublicMenuAd>()
                .HasIndex(a => new { a.CommercialUserId, a.SortOrder });

            modelBuilder.Entity<ItemWarehouseStock>()
                .HasOne(s => s.Item)
                .WithMany(i => i.WarehouseStocksNav)
                .HasForeignKey(s => s.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ItemWarehouseStock>()
                .HasOne(s => s.Warehouse)
                .WithMany(w => w.Stocks)
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ItemWarehouseStock>()
                .HasIndex(s => new { s.ItemId, s.WarehouseId })
                .IsUnique();

            modelBuilder.Entity<CustomerOrder>()
                .HasOne(o => o.Warehouse)
                .WithMany()
                .HasForeignKey(o => o.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CatalogStockReturn>()
                .HasOne(r => r.Warehouse)
                .WithMany()
                .HasForeignKey(r => r.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.LoginCode)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.DefaultPrinter)
                .WithMany()
                .HasForeignKey(u => u.DefaultPrinterId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>().HasData(
                               new User
                               {
                    Id = 1,
                    Name = "Admin",
                    PhoneNumber = "07830200030",
                    Password = "$2a$11$7SBTLUns2M8qHvo8kz3L7ujqU2dd/BlfMOggeU/.ipSVRWGC4AH.2",
                    Username = "admin",
                    Role = "Admin",
                    InsertByUserId = 0
                         }
                     );
        }

        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void AddTimestamps()
        {
            var entities = ChangeTracker.Entries().Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    ((BaseEntity)entity.Entity).InsertDate = DateTime.UtcNow;
                    ((BaseEntity)entity.Entity).IsDeleted = false;
                }
                else
                {
                    ((BaseEntity)entity.Entity).UpdateDate = DateTime.UtcNow;
                }
            }
        }
    }
}
