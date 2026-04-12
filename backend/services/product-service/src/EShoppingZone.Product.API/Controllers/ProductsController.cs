using System.Security.Claims;
using EShoppingZone.Product.Application.DTOs;
using EShoppingZone.Product.Application.Services;
using EShoppingZone.Profile.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.Product.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ILogger<ProductsController> logger
        )
        {
            _productService = productService;
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

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
        }

        // ==================== PUBLIC ENDPOINTS (No Auth Required) ====================

        /// <summary>
        /// Get all products with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductFilterRequest filter)
        {
            try
            {
                var result = await _productService.GetAllProductsAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }


        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);

                if (product == null)
                    return NotFound(new { success = false, message = "Product not found" });

                return Ok(new { success = true, data = product });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get products by category with pagination
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetProductsByCategory(
            string category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "newest"
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    return BadRequest(new { success = false, message = "Category is required" });

                var filter = new ProductFilterRequest
                {
                    Category = category,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                };

                var result = await _productService.GetAllProductsAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {Category}", category);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all unique categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _productService.GetAllCategoriesAsync();
                return Ok(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get featured products (top rated or recently added)
        /// </summary>
        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10)
        {
            try
            {
                var filter = new ProductFilterRequest
                {
                    Page = 1,
                    PageSize = limit,
                    SortBy = "newest",
                    InStock = true,
                };

                var result = await _productService.GetAllProductsAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured products: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get all unique product types
        /// </summary>
        [HttpGet("types")]
        public async Task<IActionResult> GetAllProductTypes()
        {
            try
            {
                var types = await _productService.GetAllProductTypesAsync();
                return Ok(new { success = true, data = types });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all product types");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get product distribution by category
        /// </summary>
        [HttpGet("stats/category-distribution")]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            try
            {
                var stats = await _productService.GetCategoryDistributionAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category distribution");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== MERCHANT ONLY ENDPOINTS ====================

        /// <summary>
        /// Create a new product (Merchant only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Merchant,Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                // Determine merchant ID
                int merchantId;
                if (userRole == "Admin" && request.MerchantId.HasValue)
                {
                    // Admin can create product for any merchant
                    merchantId = request.MerchantId.Value;
                }
                else
                {
                    // Merchant creates product for themselves
                    merchantId = userId;
                }

                var product = await _productService.CreateProductAsync(merchantId, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = product,
                        message = "Product created successfully",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update product (Merchant only - own products)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Merchant,Admin")]
        public async Task<IActionResult> UpdateProduct(
            int id,
            [FromBody] UpdateProductRequest request
        )
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                // For admin, we need to get the product first to check merchantId
                // For now, admin can update any product by passing merchantId in request?
                // Let's simplify - admin can update any product
                if (userRole == "Admin")
                {
                    var product = await _productService.UpdateProductAsync(userId, id, request);
                    return Ok(
                        new
                        {
                            success = true,
                            data = product,
                            message = "Product updated successfully",
                        }
                    );
                }
                else
                {
                    var product = await _productService.UpdateProductAsync(userId, id, request);
                    return Ok(
                        new
                        {
                            success = true,
                            data = product,
                            message = "Product updated successfully",
                        }
                    );
                }
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete product (Merchant only - own products)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Merchant,Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userRole == "Admin")
                {
                    await _productService.AdminDeleteProductAsync(id);
                }
                else
                {
                    await _productService.DeleteProductAsync(userId, id);
                }

                return Ok(new { success = true, message = "Product deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Add image to product (Merchant only)
        /// </summary>
        [HttpPost("{id}/images")]
        [Authorize(Roles = "Merchant,Admin")]
        public async Task<IActionResult> AddProductImage(
            int id,
            [FromBody] AddProductImageRequest request
        )
        {
            try
            {
                var userId = GetCurrentUserId();
                var product = await _productService.AddProductImageAsync(userId, id, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = product,
                        message = "Image added successfully",
                    }
                );
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding image to product {ProductId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete image from product (Merchant only)
        /// </summary>
        [HttpDelete("{id}/images/{imageIndex}")]
        [Authorize(Roles = "Merchant,Admin")]
        public async Task<IActionResult> DeleteProductImage(int id, int imageIndex)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _productService.DeleteProductImageAsync(userId, id, imageIndex);
                return Ok(new { success = true, message = "Image deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from product {ProductId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get merchant's own products
        /// </summary>
        [HttpGet("merchant/products")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> GetMyProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _productService.GetMerchantProductsAsync(userId, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting merchant products");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== ADMIN ONLY ENDPOINTS ====================

        /// <summary>
        /// Admin delete any product
        /// </summary>
        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteProduct(int id)
        {
            try
            {
                await _productService.AdminDeleteProductAsync(id);
                return Ok(
                    new { success = true, message = "Product deleted by admin successfully" }
                );
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin deleting product {ProductId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Admin get all products (including inactive)
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAllProducts(
            [FromQuery] ProductFilterRequest filter
        )
        {
            try
            {
                var result = await _productService.AdminGetAllProductsAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin getting all products");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update stock quantity (for order service)
        /// </summary>
        [HttpPatch("{id}/stock")]
        [Authorize]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
        {
            try
            {
                await _productService.UpdateStockAsync(id, request.QuantityChange);
                return Ok(new { success = true, message = "Stock updated successfully" });
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
                _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Search products by name (Case-insensitive partial match)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts(
            [FromQuery] string name,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { success = false, message = "Search term is required" });

                var filter = new ProductFilterRequest
                {
                    SearchTerm = name,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = "name_asc",
                };

                var result = await _productService.GetAllProductsAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with name: {Name}", name);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
        /// <summary>
        /// Rate a product (Customers only)
        /// </summary>
        [HttpPost("{id}/rate")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RateProduct(int id, [FromBody] RateProductRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId))
                    return Unauthorized(new { success = false, message = "Invalid user to rate" });

                var result = await _productService.RateProductAsync(id, userId, request.Rating);
                return Ok(new { success = true, data = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating product {ProductId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Instantly buy a product and apply stock modification
        /// </summary>
        [HttpPost("{id}/buy")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> BuyProduct(int id, [FromBody] BuyProductRequest request)
        {
            try
            {
                var result = await _productService.BuyProductAsync(id, request.Quantity);
                return Ok(new { success = true, data = result });
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
                _logger.LogError(ex, "Error buying product {ProductId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    public class UpdateStockRequest
    {
        public int QuantityChange { get; set; } // Positive for add, negative for remove
    }
}
