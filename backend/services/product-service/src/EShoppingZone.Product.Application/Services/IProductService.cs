using EShoppingZone.Product.Application.DTOs;

namespace EShoppingZone.Product.Application.Services
{
    public interface IProductService
    {
        // Merchant operations
        Task<ProductResponse> CreateProductAsync(int merchantId, CreateProductRequest request);
        Task<ProductResponse> UpdateProductAsync(
            int merchantId,
            int productId,
            UpdateProductRequest request
        );
        Task<bool> DeleteProductAsync(int merchantId, int productId);
        Task<ProductResponse> AddProductImageAsync(
            int merchantId,
            int productId,
            AddProductImageRequest request
        );
        Task<bool> DeleteProductImageAsync(int merchantId, int productId, int imageIndex);

        // Public operations
        Task<ProductListResponse> GetAllProductsAsync(ProductFilterRequest filter);
        Task<ProductResponse?> GetProductByIdAsync(int productId);
        Task<ProductListResponse> GetMerchantProductsAsync(
            int merchantId,
            int page = 1,
            int pageSize = 20
        );
        Task<ProductListResponse> GetProductsByCategoryAsync(
            string category,
            int page = 1,
            int pageSize = 20
        );
        Task<bool> UpdateStockAsync(int productId, int quantityChange);

        // Admin operations
        Task<bool> AdminDeleteProductAsync(int productId);
        Task<ProductListResponse> AdminGetAllProductsAsync(ProductFilterRequest filter);
    }
}
