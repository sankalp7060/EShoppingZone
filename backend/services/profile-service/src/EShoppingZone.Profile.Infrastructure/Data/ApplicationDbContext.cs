using EShoppingZone.Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Profile.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<AddressEntity> Addresses { get; set; }
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("profile");
            base.OnModelCreating(modelBuilder);

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
                entity.Property(e => e.ProfileImage).HasMaxLength(2000); // Increased for Google URLs
                entity.Property(e => e.About).HasMaxLength(2000);
                entity.Property(e => e.Gender).HasMaxLength(20);
                entity.Property(e => e.OAuthId).HasMaxLength(200);

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
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                    }
                );
            });

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
