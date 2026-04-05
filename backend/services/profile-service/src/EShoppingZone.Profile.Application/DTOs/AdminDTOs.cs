using System.ComponentModel.DataAnnotations;
using EShoppingZone.Profile.Domain.Entities;

namespace EShoppingZone.Profile.Application.DTOs
{
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

    public class ChangeRoleRequest
    {
        [Required]
        public UserRole Role { get; set; }
    }

    public class AdminUserActionRequest
    {
        public string? Action { get; set; } // Suspend, Reactivate, Delete
        public string? Reason { get; set; }
    }

    public class UserActivityResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime LastLoginAt { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public int ActiveSessions { get; set; }
    }
}
