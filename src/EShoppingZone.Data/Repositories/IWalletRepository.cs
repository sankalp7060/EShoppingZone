using EShoppingZone.Models.Entities;

namespace EShoppingZone.Data.Repositories
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
}
