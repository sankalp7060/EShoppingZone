using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<OrderEntity> Orders { get; set; }
        public DbSet<OrderStatusHistoryEntity> OrderStatusHistories { get; set; }
        public DbSet<CartEntity> Carts { get; set; }
        public DbSet<CartItemEntity> CartItems { get; set; }
        public DbSet<WalletEntity> Wallets { get; set; }
        public DbSet<StatementEntity> WalletStatements { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<AddressEntity> Addresses { get; set; }
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product Configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ProductType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).IsRequired().HasPrecision(18, 2);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.StockQuantity).IsRequired();
                entity.Property(e => e.MerchantId).IsRequired();

                entity.Property(e => e.Ratings).HasColumnType("jsonb");
                entity.Property(e => e.Reviews).HasColumnType("jsonb");
                entity.Property(e => e.Images).HasColumnType("jsonb");
                entity.Property(e => e.Specifications).HasColumnType("jsonb");

                entity.HasIndex(e => e.ProductName);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.MerchantId);
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Order Configuration
            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderId).ValueGeneratedOnAdd();
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

                entity.Property(e => e.OrderItems).HasColumnType("jsonb");
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.OrderStatus);
                entity.HasIndex(e => e.OrderDate);
            });

            // OrderStatusHistory Configuration
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
            });

            // Cart Configuration
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
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CartId);
            });

            // Wallet Configuration
            modelBuilder.Entity<WalletEntity>(entity =>
            {
                entity.ToTable("Wallets");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);

                entity
                    .HasMany(e => e.Statements)
                    .WithOne(e => e.Wallet)
                    .HasForeignKey(e => e.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StatementEntity>(entity =>
            {
                entity.ToTable("WalletStatements");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.BalanceAfterTransaction).HasPrecision(18, 2);
                entity.Property(e => e.TransactionRemarks).HasMaxLength(500);

                entity.HasIndex(e => e.WalletId);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.TransactionDate);
            });

            // User Configuration
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.MobileNumber);
                entity.HasIndex(e => e.OAuthProvider);
                entity.HasIndex(e => e.Role);

                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PasswordHash).HasMaxLength(500);
                entity.Property(e => e.ProfileImage).HasMaxLength(500);
                entity.Property(e => e.About).HasMaxLength(1000);
                entity.Property(e => e.Gender).HasMaxLength(10);

                // Seed admin user
                entity.HasData(
                    new UserEntity
                    {
                        Id = 1,
                        FullName = "System Admin",
                        Email = "admin@eshoppingzone.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        Role = UserRole.Admin,
                        IsEmailVerified = true,
                        CreatedAt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc),
                        IsActive = true,
                    }
                );
            });

            // Address Configuration
            modelBuilder.Entity<AddressEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HouseNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.StreetName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.State).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Pincode).IsRequired().HasMaxLength(10);

                entity
                    .HasOne(e => e.User)
                    .WithMany(u => u.Addresses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RefreshToken Configuration
            modelBuilder.Entity<RefreshTokenEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.JwtId);

                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.JwtId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeviceInfo).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(50);

                entity
                    .HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
