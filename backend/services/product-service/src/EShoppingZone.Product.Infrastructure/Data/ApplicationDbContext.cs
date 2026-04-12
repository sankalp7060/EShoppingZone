using EShoppingZone.Product.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EShoppingZone.Product.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ProductEntity> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductEntity>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ProductType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.StockQuantity).IsRequired();
                entity.Property(e => e.MerchantId).IsRequired();

                // Create value comparers for collections
                var stringListComparer = new ValueComparer<List<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                );

                var dictionaryComparer = new ValueComparer<Dictionary<int, double>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                    c => new Dictionary<int, double>(c)
                );

                var reviewsComparer = new ValueComparer<Dictionary<int, string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                    c => new Dictionary<int, string>(c)
                );

                var specificationsComparer = new ValueComparer<Dictionary<string, string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                    c => new Dictionary<string, string>(c)
                );

                entity
                    .Property(e => e.Ratings)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, double>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<int, double>()
                    )
                    .Metadata.SetValueComparer(dictionaryComparer);

                entity
                    .Property(e => e.Images)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                    )
                    .Metadata.SetValueComparer(stringListComparer);

                entity
                    .Property(e => e.Specifications)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                    )
                    .Metadata.SetValueComparer(specificationsComparer);

                // Indexes
                entity.HasIndex(e => e.ProductName);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.MerchantId);
                entity.HasIndex(e => e.IsActive);

                // Default values
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });
        }
    }
}
