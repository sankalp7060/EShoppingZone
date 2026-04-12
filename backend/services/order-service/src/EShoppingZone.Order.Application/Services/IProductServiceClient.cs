using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Order.Application.Services
{
    public interface IProductServiceClient
    {
        Task<bool> UpdateStockAsync(int productId, int quantityChange, string token);
        Task<ProductResponseDto?> GetProductAsync(int productId, string token);
    }

    public class ProductServiceClient : IProductServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductServiceClient> _logger;

        public ProductServiceClient(HttpClient httpClient, ILogger<ProductServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantityChange, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var request = new { quantityChange };
                var response = await _httpClient.PatchAsJsonAsync(
                    $"http://localhost:5002/api/products/{productId}/stock",
                    request
                );

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<ProductResponseDto?> GetProductAsync(int productId, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"http://localhost:5002/api/products/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ProductApiResponse<ProductResponseDto>>();
                    return result?.Data;
                }

                _logger.LogWarning("Failed to get product {ProductId}", productId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling product service for product {ProductId}", productId);
                return null;
            }
        }
    }

    public class ProductApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }

    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int MerchantId { get; set; }
        public string? ImageUrl { get; set; }
    }
}
