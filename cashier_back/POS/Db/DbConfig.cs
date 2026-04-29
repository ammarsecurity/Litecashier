using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using POS.Models;
using System.Security.Claims;

namespace POS.Db
{
    public class DbConfig : DbContext
    {
      ///  private readonly IHttpContextAccessor _httpContextAccessor;

        public DbConfig(DbContextOptions<DbConfig> options) : base(options)
        {
       //     _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<CustomerOrder> CustomerOrders { get; set; }
        public DbSet<CustomerOrderItem> CustomerOrderItems { get; set; }
        public DbSet<Item> Items { get; set; }



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


            modelBuilder.Entity<Tag>().HasOne(r => r.User).WithMany(r => r.Tags).HasForeignKey(x => x.InsertByUserId);

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
