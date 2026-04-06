using EShoppingZone.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Merchant,Admin")]
    public class ImageUploadController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ImageUploadController> _logger;

        public ImageUploadController(
            ICloudinaryService cloudinaryService,
            ILogger<ImageUploadController> logger
        )
        {
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        [HttpPost("single")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file provided" });
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(
                        new
                        {
                            success = false,
                            message = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp",
                        }
                    );
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(
                        new { success = false, message = "File size exceeds 5MB limit" }
                    );

                var imageUrl = await _cloudinaryService.UploadImageAsync(file);
                return Ok(
                    new
                    {
                        success = true,
                        data = new { url = imageUrl },
                        message = "Image uploaded successfully",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("multiple")]
        public async Task<IActionResult> UploadMultipleImages(List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
                    return BadRequest(new { success = false, message = "No files provided" });
                if (files.Count > 10)
                    return BadRequest(
                        new { success = false, message = "Maximum 10 images allowed per upload" }
                    );

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var uploadedUrls = new List<string>();

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0)
                        return BadRequest(
                            new { success = false, message = $"File '{file?.FileName}' is empty" }
                        );
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                        return BadRequest(
                            new { success = false, message = $"Invalid file: {file.FileName}" }
                        );
                    if (file.Length > 5 * 1024 * 1024)
                        return BadRequest(
                            new { success = false, message = $"File '{file.FileName}' exceeds 5MB" }
                        );

                    var imageUrl = await _cloudinaryService.UploadImageAsync(file);
                    uploadedUrls.Add(imageUrl);
                }

                return Ok(
                    new
                    {
                        success = true,
                        data = new { urls = uploadedUrls },
                        message = $"{uploadedUrls.Count} images uploaded successfully",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple images");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("from-url")]
        public async Task<IActionResult> UploadImageFromUrl([FromBody] UploadFromUrlRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                    return BadRequest(new { success = false, message = "Image URL is required" });
                var imageUrl = await _cloudinaryService.UploadImageFromUrlAsync(request.ImageUrl);
                return Ok(
                    new
                    {
                        success = true,
                        data = new { url = imageUrl },
                        message = "Image uploaded successfully",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image from URL");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
        {
            try
            {
                var publicId = _cloudinaryService.GetPublicIdFromUrl(imageUrl);
                if (string.IsNullOrEmpty(publicId))
                    return BadRequest(new { success = false, message = "Invalid Cloudinary URL" });

                var result = await _cloudinaryService.DeleteImageAsync(publicId);
                return Ok(
                    new
                    {
                        success = result,
                        message = result ? "Image deleted successfully" : "Failed to delete image",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class UploadFromUrlRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
    }
}
