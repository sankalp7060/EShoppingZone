using EShoppingZone.Wallet.Domain.Entities;
using EShoppingZone.Wallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Wallet.Infrastructure.Repositories
{
    public interface IWalletRepository
    {
        Task<WalletEntity?> GetByUserIdAsync(int userId);
        Task<WalletEntity> CreateAsync(WalletEntity wallet);
        Task<WalletEntity> UpdateAsync(WalletEntity wallet);
        Task<StatementEntity> AddStatementAsync(StatementEntity statement);
        Task<List<StatementEntity>> GetStatementsByWalletIdAsync(int walletId);
        Task<StatementEntity?> GetStatementByIdAsync(int statementId);
        Task<bool> WalletExistsAsync(int userId);
    }

    public class WalletRepository : IWalletRepository
    {
        private readonly ApplicationDbContext _context;

        public WalletRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WalletEntity?> GetByUserIdAsync(int userId)
        {
            return await _context
                .Wallets.Include(w => w.Statements)
                .FirstOrDefaultAsync(w => w.UserId == userId && w.IsActive);
        }

        public async Task<WalletEntity> CreateAsync(WalletEntity wallet)
        {
            await _context.Wallets.AddAsync(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        public async Task<WalletEntity> UpdateAsync(WalletEntity wallet)
        {
            _context.Entry(wallet).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return wallet;
        }

        public async Task<StatementEntity> AddStatementAsync(StatementEntity statement)
        {
            await _context.Statements.AddAsync(statement);
            await _context.SaveChangesAsync();
            return statement;
        }

        public async Task<List<StatementEntity>> GetStatementsByWalletIdAsync(int walletId)
        {
            return await _context
                .Statements.Where(s => s.WalletId == walletId)
                .OrderByDescending(s => s.TransactionDate)
                .ToListAsync();
        }

        public async Task<StatementEntity?> GetStatementByIdAsync(int statementId)
        {
            return await _context.Statements.FirstOrDefaultAsync(s => s.Id == statementId);
        }

        public async Task<bool> WalletExistsAsync(int userId)
        {
            return await _context.Wallets.AnyAsync(w => w.UserId == userId && w.IsActive);
        }
    }
}
