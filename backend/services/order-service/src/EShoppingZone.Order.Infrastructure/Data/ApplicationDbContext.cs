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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var comparer = new ValueComparer<List<OrderItemEntity>>(
                (c1, c2) =>
                    JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null)
                    == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
                c =>
                    c == null
                        ? 0
                        : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
                c =>
                    c == null
                        ? new List<OrderItemEntity>()
                        : JsonSerializer.Deserialize<List<OrderItemEntity>>(
                            JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                            (JsonSerializerOptions?)null
                        )!
            );

            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderId).ValueGeneratedOnAdd();
                entity.Property(e => e.AmountPaid).HasPrecision(18, 2);
                entity.Property(e => e.ModeOfPayment).HasMaxLength(20);
                entity.Property(e => e.OrderStatus).HasMaxLength(20);

                entity.Property(e => e.AddressHouseNumber).HasMaxLength(50);
                entity.Property(e => e.AddressStreetName).HasMaxLength(200);
                entity.Property(e => e.AddressColonyName).HasMaxLength(200);
                entity.Property(e => e.AddressCity).HasMaxLength(100);
                entity.Property(e => e.AddressState).HasMaxLength(100);
                entity.Property(e => e.AddressPincode).HasMaxLength(10);
                entity.Property(e => e.AddressLandmark).HasMaxLength(200);

                // Store OrderItems as JSONB
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
                    .Metadata.SetValueComparer(comparer);

                // Indexes
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.OrderStatus);
                entity.HasIndex(e => e.OrderDate);
            });
        }
    }
}
