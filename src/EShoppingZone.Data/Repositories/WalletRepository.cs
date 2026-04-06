using EShoppingZone.Data.Context;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShoppingZone.Data.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AppDbContext _context;

        public WalletRepository(AppDbContext context)
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
            await _context.WalletStatements.AddAsync(statement);
            await _context.SaveChangesAsync();
            return statement;
        }

        public async Task<List<StatementEntity>> GetStatementsByWalletIdAsync(int walletId)
        {
            return await _context
                .WalletStatements.Where(s => s.WalletId == walletId)
                .OrderByDescending(s => s.TransactionDate)
                .ToListAsync();
        }

        public async Task<StatementEntity?> GetStatementByIdAsync(int statementId)
        {
            return await _context.WalletStatements.FirstOrDefaultAsync(s => s.Id == statementId);
        }

        public async Task<bool> WalletExistsAsync(int userId)
        {
            return await _context.Wallets.AnyAsync(w => w.UserId == userId && w.IsActive);
        }
    }
}
