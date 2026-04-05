using System.ComponentModel.DataAnnotations;
using EShoppingZone.Profile.Domain.Entities;

namespace EShoppingZone.Profile.Application.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public long MobileNumber { get; set; }

        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class OAuthLoginRequest
    {
        [Required]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string IdToken { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long MobileNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class GoogleAuthRequest
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class LogoutRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class RevokeAllTokensRequest
    {
        public string? DeviceInfo { get; set; }
    }

    // Admin specific DTOs
    public class AdminUpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public long? MobileNumber { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UserListResponse
    {
        public List<UserDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class UserFilterRequest
    {
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
