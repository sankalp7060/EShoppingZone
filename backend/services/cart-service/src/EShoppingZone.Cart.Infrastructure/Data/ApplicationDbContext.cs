using EShoppingZone.Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Cart.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<CartEntity> Carts { get; set; }
        public DbSet<CartItemEntity> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("cart");
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CartEntity>(entity =>
            {
                entity.ToTable("Carts");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

                entity
                    .HasMany(e => e.Items)
                    .WithOne(e => e.Cart)
                    .HasForeignKey(e => e.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItemEntity>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.Quantity).IsRequired();
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CartId);
            });
        }
    }
}
