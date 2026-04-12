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
            var strategy = _walletRepository.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _walletRepository.BeginTransactionAsync();
                try
                {
                    var wallet = await _walletRepository.GetByUserIdAsync(userId);

                    if (wallet == null)
                    {
                        _logger.LogInformation("Auto-Healing: Creating missing wallet for user {UserId} during deposit.", userId);
                        var createdWallet = new WalletEntity
                        {
                            UserId = userId,
                            CurrentBalance = 0,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true,
                        };
                        wallet = await _walletRepository.CreateAsync(createdWallet);
                    }

                    // Idempotency: Check if this Razorpay Order has already been processed
                    if (!string.IsNullOrEmpty(request.Remarks) && request.Remarks.StartsWith("order_"))
                    {
                        var allStatements = await _walletRepository.GetStatementsByWalletIdAsync(wallet.Id);
                        var duplicate = allStatements.FirstOrDefault(s => s.TransactionRemarks == request.Remarks);
                        if (duplicate != null)
                        {
                            _logger.LogWarning("Idempotency Triggered: Razorpay Order {OrderId} already processed for User {UserId}.", request.Remarks, userId);
                            return new TransactionResponse
                            {
                                Success = true,
                                Message = "Transaction already processed successfully.",
                                NewBalance = wallet.CurrentBalance,
                                TransactionId = duplicate.Id,
                            };
                        }
                    }

                    _logger.LogInformation("Processing deposit for User {UserId}. Current Balance: {Balance}, Deposit: {Amount}", userId, wallet.CurrentBalance, request.Amount);

                    // Add money to wallet
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
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Deposit SUCCESS for user {UserId}. Final balance: {Balance}",
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
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Deposit FAILED for user {UserId}. Rolling back transaction.", userId);
                    throw;
                }
            });
        }

        public async Task<TransactionResponse> PayMoneyAsync(int userId, PayMoneyRequest request)
        {
            var strategy = _walletRepository.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _walletRepository.BeginTransactionAsync();
                try
                {
                    var wallet = await _walletRepository.GetByUserIdAsync(userId);

                    if (wallet == null)
                        throw new InvalidOperationException("Wallet not found. Please create a wallet first.");

                    if (wallet.CurrentBalance < request.Amount)
                        throw new InvalidOperationException($"Insufficient balance. Current balance: {wallet.CurrentBalance:C}, Required: {request.Amount:C}");

                    // Deduct money from wallet
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

                    var createdStatement = await _walletRepository.AddStatementAsync(statement);
                    await transaction.CommitAsync();

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
                        TransactionId = createdStatement.Id,
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<TransactionResponse> CreditMoneyAsync(int userId, PayMoneyRequest request)
        {
            var strategy = _walletRepository.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _walletRepository.BeginTransactionAsync();
                try
                {
                    var wallet = await _walletRepository.GetByUserIdAsync(userId);

                    if (wallet == null)
                    {
                        // Auto-create wallet for merchant if not exists
                        var createdWallet = new WalletEntity
                        {
                            UserId = userId,
                            CurrentBalance = 0,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true,
                        };
                        wallet = await _walletRepository.CreateAsync(createdWallet);
                    }

                    // Credit money to wallet
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
                        OrderId = request.OrderId,
                        TransactionRemarks = request.Remarks ?? $"Earnings from order #{request.OrderId}",
                        BalanceAfterTransaction = wallet.CurrentBalance,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                    };

                    var createdStatement = await _walletRepository.AddStatementAsync(statement);
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Credited {Amount} to wallet for merchant {UserId} for order {OrderId}. New balance: {Balance}",
                        request.Amount,
                        userId,
                        request.OrderId,
                        wallet.CurrentBalance
                    );

                    return new TransactionResponse
                    {
                        Success = true,
                        Message = $"Credit of {request.Amount:C} successful",
                        NewBalance = wallet.CurrentBalance,
                        TransactionId = createdStatement.Id,
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<TransactionResponse> WithdrawMoneyAsync(int userId, decimal amount)
        {
            var strategy = _walletRepository.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _walletRepository.BeginTransactionAsync();
                try
                {
                    var wallet = await _walletRepository.GetByUserIdAsync(userId);
                    if (wallet == null)
                        throw new InvalidOperationException("Wallet not found.");

                    if (wallet.CurrentBalance < amount)
                        throw new InvalidOperationException("Insufficient balance for payout.");

                    // Deduct money
                    wallet.CurrentBalance -= amount;
                    wallet.LastTransactionAt = DateTime.UtcNow;
                    wallet.UpdatedAt = DateTime.UtcNow;

                    await _walletRepository.UpdateAsync(wallet);

                    // Create statement
                    var statement = new StatementEntity
                    {
                        WalletId = wallet.Id,
                        TransactionType = "DEBIT",
                        Amount = amount,
                        TransactionDate = DateTime.UtcNow,
                        TransactionRemarks = "Instant Payout (Merchant)",
                        BalanceAfterTransaction = wallet.CurrentBalance,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                    };

                    await _walletRepository.AddStatementAsync(statement);
                    await transaction.CommitAsync();

                    return new TransactionResponse
                    {
                        Success = true,
                        Message = $"Payout of {amount:C} successful",
                        NewBalance = wallet.CurrentBalance,
                        TransactionId = statement.Id
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<TransactionResponse> RefundMoneyAsync(int userId, PayMoneyRequest request)
        {
            var strategy = _walletRepository.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _walletRepository.BeginTransactionAsync();
                try
                {
                    var wallet = await _walletRepository.GetByUserIdAsync(userId);
                    if (wallet == null)
                        throw new InvalidOperationException("Wallet not found.");

                    // Add money back
                    wallet.CurrentBalance += request.Amount;
                    wallet.LastTransactionAt = DateTime.UtcNow;
                    wallet.UpdatedAt = DateTime.UtcNow;

                    await _walletRepository.UpdateAsync(wallet);

                    // Create statement
                    var statement = new StatementEntity
                    {
                        WalletId = wallet.Id,
                        TransactionType = "CREDIT",
                        Amount = request.Amount,
                        TransactionDate = DateTime.UtcNow,
                        OrderId = request.OrderId,
                        TransactionRemarks = request.Remarks ?? $"Refund for cancelled order #{request.OrderId}",
                        BalanceAfterTransaction = wallet.CurrentBalance,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                    };

                    await _walletRepository.AddStatementAsync(statement);
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Refunded {Amount} to wallet for user {UserId} for order {OrderId}. New balance: {Balance}",
                        request.Amount,
                        userId,
                        request.OrderId,
                        wallet.CurrentBalance
                    );

                    return new TransactionResponse
                    {
                        Success = true,
                        Message = $"Refund of {request.Amount:C} successful",
                        NewBalance = wallet.CurrentBalance,
                        TransactionId = statement.Id
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<WalletBalanceResponse> GetBalanceAsync(int userId)
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId);

            if (wallet == null)
                return new WalletBalanceResponse { CurrentBalance = 0, UserId = userId };

            // Reconciliation: Ensure CurrentBalance matches the sum of transactions
            var reconciledBalance = await ReconcileBalanceAsync(wallet.Id);
            if (reconciledBalance != wallet.CurrentBalance)
            {
                _logger.LogWarning("Balance drift detected for User {UserId}. Fixed: {Old} -> {New}", userId, wallet.CurrentBalance, reconciledBalance);
                wallet.CurrentBalance = reconciledBalance;
                await _walletRepository.UpdateAsync(wallet);
            }

            return new WalletBalanceResponse
            {
                CurrentBalance = wallet.CurrentBalance,
                UserId = userId,
                LastTransactionAt = wallet.LastTransactionAt
            };
        }

        private async Task<decimal> ReconcileBalanceAsync(int walletId)
        {
            var statements = await _walletRepository.GetStatementsByWalletIdAsync(walletId);
            decimal balance = 0;
            foreach (var s in statements)
            {
                if (s.TransactionType == "CREDIT") balance += s.Amount;
                else if (s.TransactionType == "DEBIT") balance -= s.Amount;
            }
            return balance;
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
