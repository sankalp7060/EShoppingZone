using EShoppingZone.Business.DTOs;
using EShoppingZone.Models.Entities;

namespace EShoppingZone.Business.Services
{
    public interface IAdminService
    {
        // User Management
        Task<AuthResponse> CreateUserByAdminAsync(AdminCreateUserRequest request);
        Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole);
        Task<UserListResponse> GetAllUsersAsync(UserFilterRequest filter);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<bool> UpdateUserAsync(int userId, AdminUpdateUserRequest request);
        Task<bool> SuspendUserAsync(int userId, string? reason = null);
        Task<bool> ReactivateUserAsync(int userId);
        Task<bool> DeleteUserAsync(int userId);

        // Dashboard Analytics
        Task<DashboardAnalyticsResponse> GetDashboardAnalyticsAsync();
        Task<List<UserActivityResponse>> GetRecentUserActivityAsync(int days = 7);
        Task<RevenueAnalyticsResponse> GetRevenueAnalyticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
    }
}
