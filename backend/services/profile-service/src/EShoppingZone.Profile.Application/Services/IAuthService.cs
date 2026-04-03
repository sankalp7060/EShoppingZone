using EShoppingZone.Profile.Application.DTOs;

namespace EShoppingZone.Profile.Application.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> GoogleLoginAsync(GoogleAuthRequest request);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<bool> RevokeAllUserTokensAsync(int userId, string? deviceInfo = null);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task LogoutAsync(int userId, string? refreshToken = null);
    }
}
