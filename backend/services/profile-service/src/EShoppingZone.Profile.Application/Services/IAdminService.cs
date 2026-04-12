using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Domain.Entities;

namespace EShoppingZone.Profile.Application.Services
{
    public interface IAdminService
    {
        // User Management
        Task<AuthResponse> CreateUserByAdminAsync(AdminCreateUserRequest request);
        Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole);
        Task<UserListResponse> GetAllUsersAsync(UserFilterRequest filter);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<bool> UpdateUserAsync(int userId, AdminUpdateUserRequest request);
        Task<bool> SuspendUserAsync(int userId, int requestingAdminId, string? reason = null);
        Task<bool> ReactivateUserAsync(int userId);
        Task<bool> DeleteUserAsync(int userId, int requestingAdminId);

        // Dashboard Analytics
        Task<DashboardAnalyticsResponse> GetDashboardAnalyticsAsync();
        Task<List<UserActivityResponse>> GetRecentUserActivityAsync(int days = 7);
        Task<RevenueAnalyticsResponse> GetRevenueAnalyticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
    }
}
