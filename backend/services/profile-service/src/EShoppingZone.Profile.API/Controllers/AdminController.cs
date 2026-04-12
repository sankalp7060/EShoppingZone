using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EShoppingZone.Profile.Application.Common.Exceptions;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Application.Services;
using EShoppingZone.Profile.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.Profile.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        // ==================== USER MANAGEMENT ====================

        /// <summary>
        /// Create a new user with any role
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
        {
            try
            {
                var response = await _adminService.CreateUserByAdminAsync(request);
                return Ok(
                    new
                    {
                        success = true,
                        data = response,
                        message = $"User created with role {request.Role}",
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user by admin");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all users with filtering and pagination
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterRequest filter)
        {
            try
            {
                var users = await _adminService.GetAllUsersAsync(filter);
                return Ok(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user details
        /// </summary>
        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(
            int userId,
            [FromBody] AdminUpdateUserRequest request
        )
        {
            try
            {
                var result = await _adminService.UpdateUserAsync(userId, request);
                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Change user role
        /// </summary>
        [HttpPatch("users/{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(
            int userId,
            [FromBody] ChangeRoleRequest request
        )
        {
            try
            {
                var result = await _adminService.ChangeUserRoleAsync(userId, request.Role);
                return Ok(new { success = true, message = $"User role changed to {request.Role}" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user role");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Suspend a user account
        /// </summary>
        [HttpPost("users/{userId}/suspend")]
        public async Task<IActionResult> SuspendUser(
            int userId,
            [FromBody] AdminUserActionRequest? request
        )
        {
            try
            {
                var adminId = GetCurrentAdminId();
                var result = await _adminService.SuspendUserAsync(userId, adminId, request?.Reason);
                return Ok(new { success = true, message = "User suspended successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Reactivate a suspended user account
        /// </summary>
        [HttpPost("users/{userId}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int userId)
        {
            try
            {
                var result = await _adminService.ReactivateUserAsync(userId);
                return Ok(new { success = true, message = "User reactivated successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a user account
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var adminId = GetCurrentAdminId();
                var result = await _adminService.DeleteUserAsync(userId, adminId);
                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== DASHBOARD ANALYTICS ====================

        /// <summary>
        /// Get dashboard analytics
        /// </summary>
        [HttpGet("dashboard/analytics")]
        public async Task<IActionResult> GetDashboardAnalytics()
        {
            try
            {
                var analytics = await _adminService.GetDashboardAnalyticsAsync();
                return Ok(new { success = true, data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard analytics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get recent user activity
        /// </summary>
        [HttpGet("dashboard/user-activity")]
        public async Task<IActionResult> GetUserActivity([FromQuery] int days = 7)
        {
            try
            {
                var activity = await _adminService.GetRecentUserActivityAsync(days);
                return Ok(new { success = true, data = activity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get revenue analytics
        /// </summary>
        [HttpGet("dashboard/revenue")]
        public async Task<IActionResult> GetRevenueAnalytics(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate
        )
        {
            try
            {
                var revenue = await _adminService.GetRevenueAnalyticsAsync(fromDate, toDate);
                return Ok(new { success = true, data = revenue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue analytics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
        // ==================== PRIVATE HELPERS ====================

        /// <summary>
        /// Extracts the numeric user ID of the currently authenticated admin from the JWT 'sub' claim.
        /// </summary>
        private int GetCurrentAdminId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(sub) || !int.TryParse(sub, out var adminId))
                throw new UnauthorizedAccessException("Unable to determine caller identity from token.");

            return adminId;
        }
    }
}
