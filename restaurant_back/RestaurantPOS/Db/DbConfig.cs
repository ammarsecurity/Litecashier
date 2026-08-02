using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Restaurant;
using RestaurantPOS.Models.Sync;
using System.Security.Claims;
using System.Text.Json;

namespace RestaurantPOS.Db
{
    public class DbConfig : DbContext
    {
      ///  private readonly IHttpContextAccessor _httpContextAccessor;

        public DbConfig(DbContextOptions<DbConfig> options) : base(options)
        {
       //     _httpContextAccessor = httpContextAccessor;
        }

        // Helper method to log audit actions
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
                {
                    auditLog.OldValues = JsonSerializer.Serialize(oldValues);
                }

                if (newValues != null)
                {
                    auditLog.NewValues = JsonSerializer.Serialize(newValues);
                }

                this.AuditLogs.Add(auditLog);
                await this.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - audit logging should not break the main operation
                Console.WriteLine($"Error logging audit: {ex.Message}");
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<CustomerOrder> CustomerOrders { get; set; }
        public DbSet<CustomerOrderItem> CustomerOrderItems { get; set; }
        public DbSet<Item> Items { get; set; }

        // Restaurant Models
        public DbSet<Table> Tables { get; set; }
        public DbSet<RestaurantLayoutSettings> RestaurantLayoutSettings { get; set; }
        public DbSet<TableLayoutPlacement> TableLayoutPlacements { get; set; }
        public DbSet<OrderTable> OrderTables { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Printer> Printers { get; set; }
        public DbSet<TagPrinter> TagPrinters { get; set; }
        public DbSet<DeliveryDriver> DeliveryDrivers { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeAdvance> EmployeeAdvances { get; set; }
        public DbSet<SalaryAdjustment> SalaryAdjustments { get; set; }
        public DbSet<PayrollRun> PayrollRuns { get; set; }
        public DbSet<PayrollLine> PayrollLines { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ReturnedOrderItem> ReturnedOrderItems { get; set; }
        public DbSet<PaymentDevice> PaymentDevices { get; set; }
        public DbSet<CardPaymentTransaction> CardPaymentTransactions { get; set; }

        public DbSet<SyncRun> SyncRuns { get; set; }
        public DbSet<SyncWatermark> SyncWatermarks { get; set; }
        public DbSet<SyncFileWatermark> SyncFileWatermarks { get; set; }
        public DbSet<TenantSyncSettings> TenantSyncSettings { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<CustomerOrder>().HasMany(o => o.CustomerOrderItem);
            modelBuilder.Entity<CustomerOrderItem>().HasOne(r => r.CustomerOrder).WithMany(r => r.CustomerOrderItem).HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.User).WithMany(r => r.CustomerOrders).HasForeignKey(x => x.InsertByUserId);
            modelBuilder.Entity<CustomerOrderItem>().HasOne(r => r.User).WithMany(r => r.CustomerOrderItem).HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Item>().HasOne(r => r.User).WithMany(r => r.Items).HasForeignKey(x => x.InsertByUserId);
            modelBuilder.Entity<Tag>().HasOne(r => r.User).WithMany(r => r.Tags).HasForeignKey(x => x.InsertByUserId);
            modelBuilder.Entity<User>().HasMany(r => r.Tags);
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.Parent)
                .WithMany(t => t.Children)
                .HasForeignKey(t => t.ParentTagId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restaurant Relationships
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.Table).WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.Reservation).WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Table>().HasOne(r => r.CurrentOrder).WithMany().HasForeignKey(x => x.CurrentOrderId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Table>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<RestaurantLayoutSettings>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<RestaurantLayoutSettings>().HasIndex(x => new { x.InsertByUserId, x.PlanKey }).IsUnique();
            modelBuilder.Entity<TableLayoutPlacement>().HasOne(r => r.Table).WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TableLayoutPlacement>().HasIndex(x => new { x.TableId, x.PlanKey }).IsUnique();
            modelBuilder.Entity<Reservation>().HasOne(r => r.Table).WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Reservation>().HasOne(r => r.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Reservation>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Printer>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<DeliveryDriver>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Employee>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Employee>().HasOne(r => r.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Expense>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Expense>().HasOne(r => r.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Expense>().HasOne(r => r.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ExpenseCategory>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.DeliveryDriver).WithMany().HasForeignKey(x => x.DeliveryDriverId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.CreditEmployee).WithMany().HasForeignKey(x => x.CreditEmployeeId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CustomerOrder>().HasOne(r => r.CreditCustomer).WithMany().HasForeignKey(x => x.CreditCustomerId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<AuditLog>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<AuditLog>().HasOne(r => r.CommercialUser).WithMany().HasForeignKey(x => x.CommercialUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<StockMovement>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Supplier>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Customer>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PaymentDevice>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CardPaymentTransaction>().HasOne(r => r.PaymentDevice).WithMany().HasForeignKey(x => x.PaymentDeviceId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CardPaymentTransaction>().HasOne(r => r.CustomerOrder).WithMany().HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CardPaymentTransaction>().HasOne(r => r.CommercialUser).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CardPaymentTransaction>().HasOne(r => r.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.NoAction);

            // OrderTable relationships (many-to-many between Orders and Tables)
            modelBuilder.Entity<OrderTable>().HasOne(r => r.Order).WithMany(r => r.OrderTables).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderTable>().HasOne(r => r.Table).WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderTable>().HasOne(r => r.User).WithMany().HasForeignKey(x => x.InsertByUserId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CustomerOrderItem>()
                   .HasOne(r => r.Item)
                   .WithMany(r => r.CustomerOrderItems)
                   .HasForeignKey(x => x.ItemId)
                   .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnedOrderItem>()
                .HasOne(r => r.CustomerOrder)
                .WithMany()
                .HasForeignKey(x => x.CustomerOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnedOrderItem>()
                .HasOne(r => r.CustomerOrderItem)
                .WithMany()
                .HasForeignKey(x => x.CustomerOrderItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnedOrderItem>()
                .HasOne(r => r.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnedOrderItem>()
                .HasOne(r => r.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ReturnedOrderItem>()
                .HasOne(r => r.DeletedByUser)
                .WithMany()
                .HasForeignKey(x => x.DeletedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnedOrderItem>()
                .HasOne(r => r.InsertByUser)
                .WithMany()
                .HasForeignKey(x => x.InsertByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.LoginCode)
                .IsUnique();

            modelBuilder.Entity<TenantSyncSettings>()
                .ToTable("SyncSettings")
                .HasIndex(x => x.CommercialUserId)
                .IsUnique();

            modelBuilder.Entity<SyncWatermark>()
                .HasIndex(x => new { x.CommercialUserId, x.TableName })
                .IsUnique();

            modelBuilder.Entity<SyncFileWatermark>()
                .HasIndex(x => new { x.CommercialUserId, x.RelativePath })
                .IsUnique();

            modelBuilder.Entity<SyncRun>()
                .HasIndex(x => new { x.CommercialUserId, x.StartedAt });

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

       //     string? currentUserId = _httpContextAccessor?.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    ((BaseEntity)entity.Entity).InsertDate = DateTime.UtcNow;
                    ((BaseEntity)entity.Entity).IsDeleted = false;
                }

                else
                {
                    // Only update the UpdateDate property in the Modified state
                    ((BaseEntity)entity.Entity).UpdateDate = DateTime.UtcNow;
                }
            }
        }
    }
}
