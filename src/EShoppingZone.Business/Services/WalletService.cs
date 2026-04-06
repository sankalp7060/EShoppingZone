using System.Text.Json;
using EShoppingZone.Business.DTOs;
using EShoppingZone.Common.Constants;
using EShoppingZone.Data.Repositories;
using EShoppingZone.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Business.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            IWalletRepository walletRepository,
            IDistributedCache cache,
            ILogger<WalletService> logger
        )
        {
            _walletRepository = walletRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<WalletResponse> CreateWalletAsync(int userId)
        {
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
            var cacheKey = $"wallet_{userId}";
            var cachedWallet = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedWallet))
                return JsonSerializer.Deserialize<WalletResponse>(cachedWallet)!;

            var wallet = await _walletRepository.GetByUserIdAsync(userId);
            if (wallet == null)
                throw new InvalidOperationException("Wallet not found for this user");

            var response = MapToWalletResponse(wallet);
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                }
            );

            return response;
        }

        public async Task<TransactionResponse> AddMoneyAsync(int userId, AddMoneyRequest request)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);
            if (wallet == null)
                throw new InvalidOperationException(
                    "Wallet not found. Please create a wallet first."
                );

            var oldBalance = wallet.CurrentBalance;
            wallet.CurrentBalance += request.Amount;
            wallet.LastTransactionAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateAsync(wallet);

            var statement = new StatementEntity
            {
                WalletId = wallet.Id,
                TransactionType = ServiceConstants.TransactionTypes.Credit,
                Amount = request.Amount,
                TransactionDate = DateTime.UtcNow,
                OrderId = null,
                TransactionRemarks = request.Remarks ?? $"Added money to wallet",
                BalanceAfterTransaction = wallet.CurrentBalance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var createdStatement = await _walletRepository.AddStatementAsync(statement);

            _logger.LogInformation(
                "Added {Amount} to wallet for user {UserId}. New balance: {Balance}",
                request.Amount,
                userId,
                wallet.CurrentBalance
            );
            await _cache.RemoveAsync($"wallet_{userId}");

            return new TransactionResponse
            {
                Success = true,
                Message = $"Successfully added {request.Amount:C} to wallet",
                NewBalance = wallet.CurrentBalance,
                TransactionId = createdStatement.Id,
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
                    $"Insufficient balance. Current balance: {wallet.CurrentBalance:C}, Required: {request.Amount:C}"
                );

            try
            {
                await _walletRepository.UpdateAsync(wallet);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Balance changed - please try again");
            }

            wallet.CurrentBalance -= request.Amount;
            wallet.LastTransactionAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateAsync(wallet);

            var statement = new StatementEntity
            {
                WalletId = wallet.Id,
                TransactionType = ServiceConstants.TransactionTypes.Debit,
                Amount = request.Amount,
                TransactionDate = DateTime.UtcNow,
                OrderId = request.OrderId,
                TransactionRemarks = request.Remarks ?? $"Payment for order #{request.OrderId}",
                BalanceAfterTransaction = wallet.CurrentBalance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var createdStatement = await _walletRepository.AddStatementAsync(statement);

            _logger.LogInformation(
                "Deducted {Amount} from wallet for user {UserId} for order {OrderId}. New balance: {Balance}",
                request.Amount,
                userId,
                request.OrderId,
                wallet.CurrentBalance
            );
            await _cache.RemoveAsync($"wallet_{userId}");

            return new TransactionResponse
            {
                Success = true,
                Message = $"Payment of {request.Amount:C} successful",
                NewBalance = wallet.CurrentBalance,
                TransactionId = createdStatement.Id,
            };
        }

        public async Task<TransactionResponse> RefundAsync(
            int userId,
            int orderId,
            int transactionId,
            string remarks
        )
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);
            if (wallet == null)
                throw new InvalidOperationException("Wallet not found");

            var originalTransaction = await _walletRepository.GetStatementByIdAsync(transactionId);
            if (originalTransaction == null)
                throw new InvalidOperationException("Original transaction not found");

            wallet.CurrentBalance += originalTransaction.Amount;
            wallet.LastTransactionAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateAsync(wallet);

            var refundStatement = new StatementEntity
            {
                WalletId = wallet.Id,
                TransactionType = ServiceConstants.TransactionTypes.Credit,
                Amount = originalTransaction.Amount,
                TransactionDate = DateTime.UtcNow,
                OrderId = orderId,
                TransactionRemarks = remarks,
                BalanceAfterTransaction = wallet.CurrentBalance,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var createdStatement = await _walletRepository.AddStatementAsync(refundStatement);

            _logger.LogInformation(
                "Refunded {Amount} to wallet for user {UserId} for order {OrderId}",
                originalTransaction.Amount,
                userId,
                orderId
            );
            await _cache.RemoveAsync($"wallet_{userId}");

            return new TransactionResponse
            {
                Success = true,
                Message = $"Refund of {originalTransaction.Amount:C} successful",
                NewBalance = wallet.CurrentBalance,
                TransactionId = createdStatement.Id,
            };
        }

        public async Task<WalletBalanceResponse> GetBalanceAsync(int userId)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);
            return new WalletBalanceResponse
            {
                CurrentBalance = wallet?.CurrentBalance ?? 0,
                UserId = userId,
            };
        }

        public async Task<List<StatementResponse>> GetStatementsAsync(int userId)
        {
            var cacheKey = $"wallet_transactions_{userId}";
            var cachedTransactions = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedTransactions))
                return JsonSerializer.Deserialize<List<StatementResponse>>(cachedTransactions)!;

            var wallet = await _walletRepository.GetByUserIdAsync(userId);
            if (wallet == null)
                return new List<StatementResponse>();

            var statements = await _walletRepository.GetStatementsByWalletIdAsync(wallet.Id);
            var response = statements.Select(MapToStatementResponse).ToList();

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                }
            );

            return response;
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
