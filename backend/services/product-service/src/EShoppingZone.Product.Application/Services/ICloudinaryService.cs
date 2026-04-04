using Microsoft.AspNetCore.Http;

namespace EShoppingZone.Product.Application.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "products");
        Task<string> UploadImageFromUrlAsync(string imageUrl, string folder = "products");
        Task<bool> DeleteImageAsync(string publicId);
        string GetPublicIdFromUrl(string imageUrl);
    }
}
