using System.Text.Json;
using EShoppingZone.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EShoppingZone.Order.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<OrderEntity> Orders { get; set; }
        public DbSet<OrderStatusHistoryEntity> OrderStatusHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("ordering");
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderId).ValueGeneratedOnAdd();
                entity.Property(e => e.CustomerName).HasMaxLength(200);
                entity.Property(e => e.AmountPaid).HasPrecision(18, 2);
                entity.Property(e => e.ModeOfPayment).HasMaxLength(20);
                entity.Property(e => e.OrderStatus).HasMaxLength(20);
                entity.Property(e => e.CancellationReason).HasMaxLength(500);

                entity.Property(e => e.AddressHouseNumber).HasMaxLength(50);
                entity.Property(e => e.AddressStreetName).HasMaxLength(200);
                entity.Property(e => e.AddressColonyName).HasMaxLength(200);
                entity.Property(e => e.AddressCity).HasMaxLength(100);
                entity.Property(e => e.AddressState).HasMaxLength(100);
                entity.Property(e => e.AddressPincode).HasMaxLength(10);
                entity.Property(e => e.AddressLandmark).HasMaxLength(200);

                var orderItemsComparer = new ValueComparer<List<OrderItemEntity>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                );

                entity
                    .Property(e => e.OrderItems)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v =>
                            JsonSerializer.Deserialize<List<OrderItemEntity>>(
                                v,
                                (JsonSerializerOptions?)null
                            ) ?? new()
                    )
                    .Metadata.SetValueComparer(orderItemsComparer);

                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.MerchantId);
                entity.HasIndex(e => e.OrderStatus);
                entity.HasIndex(e => e.OrderDate);
                entity.HasIndex(e => e.EstimatedDeliveryDate);
            });

            modelBuilder.Entity<OrderStatusHistoryEntity>(entity =>
            {
                entity.ToTable("OrderStatusHistories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                entity.Property(e => e.Remarks).HasMaxLength(500);

                entity
                    .HasOne(e => e.Order)
                    .WithMany(o => o.StatusHistory)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
