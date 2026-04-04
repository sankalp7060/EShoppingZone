using EShoppingZone.Product.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
                entity.ToTable("Products"); // Explicit table name
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);

                entity.Property(e => e.ProductType).IsRequired().HasMaxLength(100);

                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);

                entity.Property(e => e.Price).IsRequired().HasPrecision(18, 2);

                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);

                entity.Property(e => e.StockQuantity).IsRequired();

                entity.Property(e => e.MerchantId).IsRequired();

                // Store JSON as string
                entity
                    .Property(e => e.Ratings)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v =>
                            System.Text.Json.JsonSerializer.Serialize(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ),
                        v =>
                            System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, double>>(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ) ?? new Dictionary<int, double>()
                    );

                entity
                    .Property(e => e.Reviews)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v =>
                            System.Text.Json.JsonSerializer.Serialize(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ),
                        v =>
                            System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, string>>(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ) ?? new Dictionary<int, string>()
                    );

                entity
                    .Property(e => e.Images)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v =>
                            System.Text.Json.JsonSerializer.Serialize(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ),
                        v =>
                            System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ) ?? new List<string>()
                    );

                entity
                    .Property(e => e.Specifications)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v =>
                            System.Text.Json.JsonSerializer.Serialize(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ),
                        v =>
                            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                                v,
                                (System.Text.Json.JsonSerializerOptions?)null
                            ) ?? new Dictionary<string, string>()
                    );

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
