using EShoppingZone.Wallet.Application.DTOs;

namespace EShoppingZone.Wallet.Application.Services
{
    public interface IWalletService
    {
        Task<WalletResponse> CreateWalletAsync(int userId);
        Task<WalletResponse> GetWalletByUserIdAsync(int userId);
        Task<TransactionResponse> AddMoneyAsync(int userId, AddMoneyRequest request);
        Task<TransactionResponse> PayMoneyAsync(int userId, PayMoneyRequest request);
        Task<WalletBalanceResponse> GetBalanceAsync(int userId);
        Task<List<StatementResponse>> GetStatementsAsync(int userId);
        Task<StatementResponse?> GetStatementByIdAsync(int userId, int statementId);
    }
}
