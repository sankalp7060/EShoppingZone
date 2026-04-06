using System.ComponentModel.DataAnnotations;
using EShoppingZone.Models.Entities;

namespace EShoppingZone.Business.DTOs
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
        public string? Action { get; set; }
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

    public class DashboardAnalyticsResponse
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int Merchants { get; set; }
        public int Customers { get; set; }
        public int DeliveryAgents { get; set; }
        public int Admins { get; set; }
        public List<UserDto> RecentUsers { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueAnalyticsResponse
    {
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class UserFilterRequest
    {
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class UserListResponse
    {
        public List<UserDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AdminUpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public long? MobileNumber { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}
