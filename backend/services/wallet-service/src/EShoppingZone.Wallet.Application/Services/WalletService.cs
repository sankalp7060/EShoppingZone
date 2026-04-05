using EShoppingZone.Wallet.Application.DTOs;
using EShoppingZone.Wallet.Domain.Entities;
using EShoppingZone.Wallet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Wallet.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IWalletRepository walletRepository, ILogger<WalletService> logger)
        {
            _walletRepository = walletRepository;
            _logger = logger;
        }

        public async Task<WalletResponse> CreateWalletAsync(int userId)
        {
            // Check if wallet already exists
            var existingWallet = await _walletRepository.GetByUserIdAsync(userId);
            if (existingWallet != null)
                throw new InvalidOperationException("Wallet already exists for this user");

            var wallet = new WalletEntity
            {
                UserId = userId,
                CurrentBalance = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var created = await _walletRepository.CreateAsync(wallet);

            _logger.LogInformation("Wallet created for user {UserId}", userId);

            return MapToWalletResponse(created);
        }

        public async Task<WalletResponse> GetWalletByUserIdAsync(int userId)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);

            if (wallet == null)
                throw new InvalidOperationException("Wallet not found for this user");

            return MapToWalletResponse(wallet);
        }

        public async Task<TransactionResponse> AddMoneyAsync(int userId, AddMoneyRequest request)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);

            if (wallet == null)
                throw new InvalidOperationException(
                    "Wallet not found. Please create a wallet first."
                );

            // Add money to wallet
            var oldBalance = wallet.CurrentBalance;
            wallet.CurrentBalance += request.Amount;
            wallet.LastTransactionAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateAsync(wallet);

            // Create transaction statement
            var statement = new StatementEntity
            {
                WalletId = wallet.Id,
                TransactionType = "CREDIT",
                Amount = request.Amount,
                TransactionDate = DateTime.UtcNow,
                OrderId = null,
                TransactionRemarks = request.Remarks ?? $"Added money to wallet",
                BalanceAfterTransaction = wallet.CurrentBalance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            await _walletRepository.AddStatementAsync(statement);

            _logger.LogInformation(
                "Added {Amount} to wallet for user {UserId}. New balance: {Balance}",
                request.Amount,
                userId,
                wallet.CurrentBalance
            );

            return new TransactionResponse
            {
                Success = true,
                Message = $"Successfully added {request.Amount:C} to wallet",
                NewBalance = wallet.CurrentBalance,
                TransactionId = statement.Id,
            };
        }

        public async Task<TransactionResponse> PayMoneyAsync(int userId, PayMoneyRequest request)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);

            if (wallet == null)
                throw new InvalidOperationException(
                    "Wallet not found. Please create a wallet first."
                );

            if (wallet.CurrentBalance < request.Amount)
                throw new InvalidOperationException(
                    $"Insufficient balance. Current balance: {wallet.CurrentBalance:C}"
                );

            // Deduct money from wallet
            var oldBalance = wallet.CurrentBalance;
            wallet.CurrentBalance -= request.Amount;
            wallet.LastTransactionAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateAsync(wallet);

            // Create transaction statement
            var statement = new StatementEntity
            {
                WalletId = wallet.Id,
                TransactionType = "DEBIT",
                Amount = request.Amount,
                TransactionDate = DateTime.UtcNow,
                OrderId = request.OrderId,
                TransactionRemarks = request.Remarks ?? $"Payment for order #{request.OrderId}",
                BalanceAfterTransaction = wallet.CurrentBalance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            await _walletRepository.AddStatementAsync(statement);

            _logger.LogInformation(
                "Deducted {Amount} from wallet for user {UserId} for order {OrderId}. New balance: {Balance}",
                request.Amount,
                userId,
                request.OrderId,
                wallet.CurrentBalance
            );

            return new TransactionResponse
            {
                Success = true,
                Message = $"Payment of {request.Amount:C} successful",
                NewBalance = wallet.CurrentBalance,
                TransactionId = statement.Id,
            };
        }

        public async Task<WalletBalanceResponse> GetBalanceAsync(int userId)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);

            if (wallet == null)
                return new WalletBalanceResponse { CurrentBalance = 0, UserId = userId };

            return new WalletBalanceResponse
            {
                CurrentBalance = wallet.CurrentBalance,
                UserId = userId,
            };
        }

        public async Task<List<StatementResponse>> GetStatementsAsync(int userId)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);

            if (wallet == null)
                return new List<StatementResponse>();

            var statements = await _walletRepository.GetStatementsByWalletIdAsync(wallet.Id);

            return statements.Select(MapToStatementResponse).ToList();
        }

        public async Task<StatementResponse?> GetStatementByIdAsync(int userId, int statementId)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);

            if (wallet == null)
                return null;

            var statement = await _walletRepository.GetStatementByIdAsync(statementId);

            if (statement == null || statement.WalletId != wallet.Id)
                return null;

            return MapToStatementResponse(statement);
        }

        private WalletResponse MapToWalletResponse(WalletEntity wallet)
        {
            return new WalletResponse
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                CurrentBalance = wallet.CurrentBalance,
                LastTransactionAt = wallet.LastTransactionAt,
                CreatedAt = wallet.CreatedAt,
            };
        }

        private StatementResponse MapToStatementResponse(StatementEntity statement)
        {
            return new StatementResponse
            {
                Id = statement.Id,
                TransactionType = statement.TransactionType,
                Amount = statement.Amount,
                TransactionDate = statement.TransactionDate,
                OrderId = statement.OrderId,
                TransactionRemarks = statement.TransactionRemarks,
                BalanceAfterTransaction = statement.BalanceAfterTransaction,
            };
        }
    }
}
