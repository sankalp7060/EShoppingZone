using System.Net.Http.Json;
using EShoppingZone.Order.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Order.Application.Services
{
    public interface ICartServiceClient
    {
        Task<CartResponseDto?> GetCartAsync(int userId, string token);
        Task<bool> ClearCartAsync(int userId, string token);
    }

    public class CartServiceClient : ICartServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CartServiceClient> _logger;

        public CartServiceClient(HttpClient httpClient, ILogger<CartServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CartResponseDto?> GetCartAsync(int userId, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"http://localhost:5003/api/cart");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<
                        CartApiResponse<CartResponseDto>
                    >();
                    return result?.Data;
                }

                _logger.LogWarning("Failed to get cart for user {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling cart service for user {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> ClearCartAsync(int userId, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync(
                    $"http://localhost:5003/api/cart/clear"
                );

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return false;
            }
        }
    }

    public class CartApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
