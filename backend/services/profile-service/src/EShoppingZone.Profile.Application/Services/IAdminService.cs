using System.ComponentModel.DataAnnotations;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Domain.Entities;

namespace EShoppingZone.Profile.Application.Services
{
    public interface IAdminService
    {
        Task<AuthResponse> CreateUserByAdminAsync(AdminCreateUserRequest request);
        Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
    }

    public class AdminCreateUserRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public long MobileNumber { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
