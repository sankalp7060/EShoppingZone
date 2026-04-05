using BCrypt.Net;
using EShoppingZone.Profile.Application.Common.Exceptions;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Profile.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUserRepository userRepository, ILogger<AdminService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<AuthResponse> CreateUserByAdminAsync(AdminCreateUserRequest request)
        {
            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already registered");

            if (await _userRepository.MobileExistsAsync(request.MobileNumber))
                throw new InvalidOperationException("Mobile number already registered");

            var user = new UserEntity
            {
                FullName = request.FullName,
                Email = request.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber,
                Gender = request.Gender ?? string.Empty,
                DateOfBirth = request.DateOfBirth,
                Role = request.Role,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            await _userRepository.CreateAsync(user);
            _logger.LogInformation(
                "Admin created user: {Email} with role {Role}",
                user.Email,
                user.Role
            );

            return new AuthResponse
            {
                Token = "",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                RefreshToken = "",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                User = MapToUserDto(user),
            };
        }

        public async Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            var oldRole = user.Role;
            user.Role = newRole;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation(
                "User {UserId} role changed from {OldRole} to {NewRole}",
                userId,
                oldRole,
                newRole
            );
            return true;
        }

        public async Task<UserListResponse> GetAllUsersAsync(UserFilterRequest filter)
        {
            var users = await _userRepository.GetAllAsync();
            var userList = users.ToList();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                userList = userList
                    .Where(u =>
                        u.FullName.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)
                        || u.Email.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)
                        || u.MobileNumber.ToString().Contains(filter.SearchTerm)
                    )
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                userList = userList.Where(u => u.Role.ToString() == filter.Role).ToList();
            }

            if (filter.IsActive.HasValue)
            {
                userList = userList.Where(u => u.IsActive == filter.IsActive.Value).ToList();
            }

            var totalCount = userList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);
            var pagedUsers = userList
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new UserListResponse
            {
                Users = pagedUsers.Select(MapToUserDto).ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user == null ? null : MapToUserDto(user);
        }

        public async Task<bool> UpdateUserAsync(int userId, AdminUpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (
                    request.Email != user.Email
                    && await _userRepository.EmailExistsAsync(request.Email)
                )
                    throw new InvalidOperationException("Email already exists");
                user.Email = request.Email.ToLower();
            }

            if (request.MobileNumber.HasValue)
            {
                if (
                    request.MobileNumber != user.MobileNumber
                    && await _userRepository.MobileExistsAsync(request.MobileNumber.Value)
                )
                    throw new InvalidOperationException("Mobile number already exists");
                user.MobileNumber = request.MobileNumber.Value;
            }

            if (request.Role.HasValue)
                user.Role = request.Role.Value;

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Admin updated user {UserId}", userId);
            return true;
        }

        public async Task<bool> SuspendUserAsync(int userId, string? reason = null)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (user.Role == UserRole.Admin)
                throw new InvalidOperationException("Cannot suspend an Admin user");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation(
                "User {UserId} suspended. Reason: {Reason}",
                userId,
                reason ?? "No reason provided"
            );
            return true;
        }

        public async Task<bool> ReactivateUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} reactivated", userId);
            return true;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (user.Role == UserRole.Admin)
                throw new InvalidOperationException("Cannot delete an Admin user");

            await _userRepository.DeleteAsync(userId);
            _logger.LogInformation("User {UserId} deleted by admin", userId);
            return true;
        }

        public async Task<DashboardAnalyticsResponse> GetDashboardAnalyticsAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var userList = users.ToList();

            var totalUsers = userList.Count;
            var activeUsers = userList.Count(u => u.IsActive);
            var suspendedUsers = totalUsers - activeUsers;

            var merchants = userList.Count(u => u.Role == UserRole.Merchant);
            var customers = userList.Count(u => u.Role == UserRole.Customer);
            var deliveryAgents = userList.Count(u => u.Role == UserRole.DeliveryAgent);
            var admins = userList.Count(u => u.Role == UserRole.Admin);

            var recentUsers = userList
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .Select(MapToUserDto)
                .ToList();

            return new DashboardAnalyticsResponse
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                SuspendedUsers = suspendedUsers,
                Merchants = merchants,
                Customers = customers,
                DeliveryAgents = deliveryAgents,
                Admins = admins,
                RecentUsers = recentUsers,
                LastUpdated = DateTime.UtcNow,
            };
        }

        public async Task<List<UserActivityResponse>> GetRecentUserActivityAsync(int days = 7)
        {
            // This would need Order Service integration
            // For now, return basic info
            var users = await _userRepository.GetAllAsync();
            var userList = users.ToList();

            return userList
                .Take(20)
                .Select(u => new UserActivityResponse
                {
                    UserId = u.Id,
                    UserName = u.FullName,
                    LastLoginAt = u.LastLoginAt ?? u.CreatedAt,
                    OrderCount = 0,
                    TotalSpent = 0,
                    ActiveSessions = 0,
                })
                .ToList();
        }

        public async Task<RevenueAnalyticsResponse> GetRevenueAnalyticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            // This would need Order Service integration
            // For now, return empty response
            return new RevenueAnalyticsResponse
            {
                TotalRevenue = 0,
                OrderCount = 0,
                AverageOrderValue = 0,
                PeriodStart = fromDate ?? DateTime.UtcNow.AddDays(-30),
                PeriodEnd = toDate ?? DateTime.UtcNow,
            };
        }

        private UserDto MapToUserDto(UserEntity user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                MobileNumber = user.MobileNumber,
                Role = user.Role.ToString(),
                ProfileImage = user.ProfileImage,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
            };
        }
    }
}
