using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EShoppingZone.Profile.Application.Common.Exceptions;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.Profile.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found in token");

            return int.Parse(userIdClaim);
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var profile = await _profileService.GetProfileAsync(userId);
                return Ok(new { success = true, data = profile });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get profile by ID (Admin only can access others)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfileById(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Only allow users to view their own profile unless they're admin
                if (currentUserId != id && userRole != "Admin")
                    return Forbid();

                var profile = await _profileService.GetProfileAsync(id);
                return Ok(new { success = true, data = profile });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update current user's profile
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var updatedProfile = await _profileService.UpdateProfileAsync(userId, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = updatedProfile,
                        message = "Profile updated successfully",
                    }
                );
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
                _logger.LogError(
                    ex,
                    "Error updating profile for user {UserId}",
                    GetCurrentUserId()
                );
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload profile image
        /// </summary>
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadProfileImage([FromBody] UploadImageRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var updatedProfile = await _profileService.UploadProfileImageAsync(
                    userId,
                    request.ImageUrl
                );
                return Ok(
                    new
                    {
                        success = true,
                        data = updatedProfile,
                        message = "Profile image updated successfully",
                    }
                );
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete profile image
        /// </summary>
        [HttpDelete("image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _profileService.DeleteProfileImageAsync(userId);
                return Ok(new { success = true, message = "Profile image deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile image");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== ADDRESS MANAGEMENT ====================

        /// <summary>
        /// Get all addresses for current user
        /// </summary>
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAllAddresses()
        {
            try
            {
                var userId = GetCurrentUserId();
                var addresses = await _profileService.GetAllAddressesAsync(userId);
                return Ok(new { success = true, data = addresses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addresses");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get specific address by ID
        /// </summary>
        [HttpGet("addresses/{addressId}")]
        public async Task<IActionResult> GetAddressById(int addressId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var address = await _profileService.GetAddressByIdAsync(userId, addressId);

                if (address == null)
                    return NotFound(new { success = false, message = "Address not found" });

                return Ok(new { success = true, data = address });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting address {AddressId}", addressId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Add new address
        /// </summary>
        [HttpPost("address")]
        public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var address = await _profileService.AddAddressAsync(userId, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = address,
                        message = "Address added successfully",
                    }
                );
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update existing address
        /// </summary>
        [HttpPut("address")]
        public async Task<IActionResult> UpdateAddress([FromBody] UpdateAddressRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var address = await _profileService.UpdateAddressAsync(userId, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = address,
                        message = "Address updated successfully",
                    }
                );
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {AddressId}", request.AddressId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete address
        /// </summary>
        [HttpDelete("address/{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _profileService.DeleteAddressAsync(userId, addressId);
                return Ok(new { success = true, message = "Address deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {AddressId}", addressId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Set address as default
        /// </summary>
        [HttpPatch("address/{addressId}/default")]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var address = await _profileService.SetDefaultAddressAsync(userId, addressId);
                return Ok(
                    new
                    {
                        success = true,
                        data = address,
                        message = "Default address set successfully",
                    }
                );
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default address {AddressId}", addressId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    public class UploadImageRequest
    {
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
