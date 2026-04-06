using EShoppingZone.Business.DTOs;
using EShoppingZone.Business.Services;
using EShoppingZone.Common.Exceptions;
using EShoppingZone.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.WebAPI.Controllers
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

        [HttpPost("users/{userId}/suspend")]
        public async Task<IActionResult> SuspendUser(
            int userId,
            [FromBody] AdminUserActionRequest? request
        )
        {
            try
            {
                var result = await _adminService.SuspendUserAsync(userId, request?.Reason);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

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

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var result = await _adminService.DeleteUserAsync(userId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

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
    }
}
