using System.Text.Json;
using BCrypt.Net;
using EShoppingZone.Profile.Application.Common.Exceptions;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace EShoppingZone.Profile.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminService(
            IUserRepository userRepository, 
            ILogger<AdminService> logger, 
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<JsonElement> FetchOrdersAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", token);
                }
                var response = await client.GetAsync("http://localhost:5004/api/orders/admin/all?pageSize=10000");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("orders", out var orders))
                    {
                        return orders.Clone();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch orders from order-service");
            }
            return JsonDocument.Parse("[]").RootElement;
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

        public async Task<bool> SuspendUserAsync(int userId, int requestingAdminId, string? reason = null)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (userId == requestingAdminId)
                throw new InvalidOperationException("You cannot suspend your own account.");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation(
                "User {UserId} suspended by admin {AdminId}. Reason: {Reason}",
                userId,
                requestingAdminId,
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

        public async Task<bool> DeleteUserAsync(int userId, int requestingAdminId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (userId == requestingAdminId)
                throw new InvalidOperationException("You cannot delete your own account.");

            await _userRepository.DeleteAsync(userId);
            _logger.LogInformation("User {UserId} deleted by admin {AdminId}", userId, requestingAdminId);
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

            var orders = await FetchOrdersAsync();
            int totalOrders = orders.GetArrayLength();
            decimal totalRevenue = 0;
            int pending = 0, shipped = 0, delivered = 0, cancelled = 0;

            foreach (var order in orders.EnumerateArray())
            {
                if (order.TryGetProperty("amountPaid", out var amount)) totalRevenue += amount.GetDecimal();
                if (order.TryGetProperty("orderStatus", out var statusProp))
                {
                    var status = statusProp.GetString()?.ToLower() ?? "";
                    if (status == "pending") pending++;
                    else if (status == "shipped") shipped++;
                    else if (status == "delivered") delivered++;
                    else if (status == "cancelled") cancelled++;
                }
            }

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
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                PendingOrders = pending,
                ShippedOrders = shipped,
                DeliveredOrders = delivered,
                CancelledOrders = cancelled,
                // Add Trend Logic
                IdentityTrend = Enumerable.Range(0, 7)
                    .Select(i => DateTime.UtcNow.AddDays(-6 + i).Date)
                    .Select(date => userList.Count(u => u.CreatedAt.Date == date))
                    .ToList(),
                ThroughputTrend = Enumerable.Range(0, 7)
                    .Select(i => DateTime.UtcNow.AddDays(-6 + i).Date)
                    .Select(date => {
                        int count = 0;
                        foreach(var o in orders.EnumerateArray()) {
                            if (o.TryGetProperty("orderDate", out var od) && od.TryGetDateTime(out var odt) && odt.Date == date) count++;
                        }
                        return count;
                    })
                    .ToList(),
                RevenueTrend = Enumerable.Range(0, 7)
                    .Select(i => DateTime.UtcNow.AddDays(-6 + i).Date)
                    .Select(date => {
                        decimal rev = 0;
                        foreach(var o in orders.EnumerateArray()) {
                            if (o.TryGetProperty("orderDate", out var od) && od.TryGetDateTime(out var odt) && odt.Date == date) {
                                if (o.TryGetProperty("amountPaid", out var ap)) rev += ap.GetDecimal();
                            }
                        }
                        return rev;
                    })
                    .ToList()
            };
        }

        public async Task<List<UserActivityResponse>> GetRecentUserActivityAsync(int days = 7)
        {
            var users = await _userRepository.GetAllAsync();
            var userList = users.ToList();

            var orders = await FetchOrdersAsync();

            var userActivity = userList
                .Select(u => {
                    int orderCount = 0;
                    decimal totalSpent = 0;
                    foreach(var o in orders.EnumerateArray()) {
                        if (o.TryGetProperty("customerId", out var cId) && cId.GetInt32() == u.Id) {
                            orderCount++;
                            if (o.TryGetProperty("amountPaid", out var amount)) totalSpent += amount.GetDecimal();
                        }
                    }
                    return new UserActivityResponse
                    {
                        UserId = u.Id,
                        UserName = u.FullName,
                        LastLoginAt = u.LastLoginAt ?? u.CreatedAt,
                        OrderCount = orderCount,
                        TotalSpent = totalSpent,
                        ActiveSessions = 0,
                    };
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(20)
                .ToList();

            return userActivity;
        }

        public async Task<RevenueAnalyticsResponse> GetRevenueAnalyticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            var orders = await FetchOrdersAsync();
            var start = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var end = toDate ?? DateTime.UtcNow;

            decimal totalRevenue = 0;
            int orderCount = 0;
            var dailyData = new Dictionary<DateTime, DailyRevenueDto>();

            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                dailyData[d] = new DailyRevenueDto { Date = d, Revenue = 0, OrderCount = 0 };
            }

            foreach (var order in orders.EnumerateArray())
            {
                if (!order.TryGetProperty("orderDate", out var dProp) || !dProp.TryGetDateTime(out var orderDate)) continue;
                
                var dateOnly = orderDate.Date;
                if (dateOnly >= start.Date && dateOnly <= end.Date)
                {
                    decimal amount = 0;
                    if (order.TryGetProperty("amountPaid", out var aProp)) amount = aProp.GetDecimal();

                    totalRevenue += amount;
                    orderCount++;

                    if (dailyData.TryGetValue(dateOnly, out var dayObj))
                    {
                        dayObj.Revenue += amount;
                        dayObj.OrderCount++;
                    }
                }
            }

            return new RevenueAnalyticsResponse
            {
                TotalRevenue = totalRevenue,
                OrderCount = orderCount,
                AverageOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0,
                PeriodStart = start,
                PeriodEnd = end,
                DailyRevenue = dailyData.Values.OrderBy(x => x.Date).ToList()
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
