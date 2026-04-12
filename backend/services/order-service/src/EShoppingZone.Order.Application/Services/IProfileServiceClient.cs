using System.Net.Http.Json;
using EShoppingZone.Order.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Order.Application.Services
{
    public interface IProfileServiceClient
    {
        Task<AddressDto?> GetAddressByIdAsync(int addressId, int userId, string token);
        Task<ProfileResponseDto?> GetProfileAsync(string token);
    }

    public class ProfileServiceClient : IProfileServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProfileServiceClient> _logger;
        private readonly IConfiguration _configuration;

        public ProfileServiceClient(
            HttpClient httpClient,
            ILogger<ProfileServiceClient> logger,
            IConfiguration configuration
        )
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<AddressDto?> GetAddressByIdAsync(int addressId, int userId, string token)
        {
            try
            {
                var profileServiceUrl =
                    _configuration["ServiceUrls:ProfileService"] ?? "http://localhost:5001";

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync(
                    $"{profileServiceUrl}/api/profile/addresses/{addressId}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<AddressDto>>();
                    return result?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling profile service for address {AddressId}", addressId);
                return null;
            }
        }

        public async Task<ProfileResponseDto?> GetProfileAsync(string token)
        {
            try
            {
                var profileServiceUrl =
                    _configuration["ServiceUrls:ProfileService"] ?? "http://localhost:5001";

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{profileServiceUrl}/api/profile/me");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProfileResponseDto>>();
                    return result?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling profile service for profile");
                return null;
            }
        }
    }

    public class ProfileResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
