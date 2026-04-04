using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Product.Application.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "products")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
                UseFilename = true,
                UniqueFilename = true,
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

            _logger.LogInformation(
                "Image uploaded to Cloudinary: {PublicId}",
                uploadResult.PublicId
            );
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadImageFromUrlAsync(
            string imageUrl,
            string folder = "products"
        )
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imageUrl),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

            _logger.LogInformation(
                "Image uploaded to Cloudinary from URL: {PublicId}",
                uploadResult.PublicId
            );
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            _logger.LogInformation(
                "Image deleted from Cloudinary: {PublicId}, Result: {Result}",
                publicId,
                result.Result
            );
            return result.Result == "ok";
        }

        public string GetPublicIdFromUrl(string imageUrl)
        {
            // Extract public ID from Cloudinary URL
            var uri = new Uri(imageUrl);
            var segments = uri.Segments;
            var uploadIndex = Array.IndexOf(segments, "upload/");

            if (uploadIndex >= 0 && uploadIndex + 2 < segments.Length)
            {
                var publicIdWithExtension = segments[uploadIndex + 2];
                return System.IO.Path.GetFileNameWithoutExtension(publicIdWithExtension);
            }

            return string.Empty;
        }
    }
}
