using System.Net.Http.Json;
using EShoppingZone.Order.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Order.Application.Services
{
    public interface IWalletServiceClient
    {
        Task<WalletPaymentResult> ProcessWalletPaymentAsync(
            int userId,
            int orderId,
            decimal amount,
            string token
        );
        Task<WalletBalanceResponse> GetWalletBalanceAsync(int userId, string token);
    }

    public class WalletPaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal NewBalance { get; set; }
        public int TransactionId { get; set; }
    }

    public class WalletBalanceResponse
    {
        public decimal CurrentBalance { get; set; }
        public int UserId { get; set; }
    }

    public class WalletServiceClient : IWalletServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WalletServiceClient> _logger;

        public WalletServiceClient(HttpClient httpClient, ILogger<WalletServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<WalletPaymentResult> ProcessWalletPaymentAsync(
            int userId,
            int orderId,
            decimal amount,
            string token
        )
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var request = new
                {
                    orderId = orderId,
                    amount = amount,
                    remarks = $"Payment for order #{orderId}",
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"http://localhost:5005/api/wallet/pay",
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<
                        WalletApiResponse<WalletPaymentResult>
                    >();
                    return result?.Data
                        ?? new WalletPaymentResult { Success = false, Message = "Payment failed" };
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Wallet payment failed: {Error}", error);
                return new WalletPaymentResult
                {
                    Success = false,
                    Message = "Insufficient wallet balance",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling wallet service for payment");
                return new WalletPaymentResult
                {
                    Success = false,
                    Message = "Payment service unavailable",
                };
            }
        }

        public async Task<WalletBalanceResponse> GetWalletBalanceAsync(int userId, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync(
                    $"http://localhost:5005/api/wallet/balance"
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<
                        WalletApiResponse<WalletBalanceResponse>
                    >();
                    return result?.Data
                        ?? new WalletBalanceResponse { CurrentBalance = 0, UserId = userId };
                }

                return new WalletBalanceResponse { CurrentBalance = 0, UserId = userId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet balance");
                return new WalletBalanceResponse { CurrentBalance = 0, UserId = userId };
            }
        }
    }

    public class WalletApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
