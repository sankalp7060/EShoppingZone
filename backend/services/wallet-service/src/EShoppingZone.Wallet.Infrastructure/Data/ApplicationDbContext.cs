using EShoppingZone.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Wallet.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<WalletEntity> Wallets { get; set; }
        public DbSet<StatementEntity> Statements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("wallet");
            base.OnModelCreating(modelBuilder);

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
                entity.HasIndex(e => e.TransactionType);
            });
        }
    }
}
