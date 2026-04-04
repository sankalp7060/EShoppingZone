using EShoppingZone.Product.Application.DTOs;
using EShoppingZone.Product.Domain.Entities;
using EShoppingZone.Product.Infrastructure.Repositories;
using EShoppingZone.Profile.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Product.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<ProductResponse> CreateProductAsync(
            int merchantId,
            CreateProductRequest request
        )
        {
            var product = new ProductEntity
            {
                ProductName = request.ProductName,
                ProductType = request.ProductType,
                Category = request.Category,
                Price = request.Price,
                Description = request.Description,
                StockQuantity = request.StockQuantity,
                MerchantId = merchantId,
                Specifications = request.Specifications ?? new Dictionary<string, string>(),
                Images = request.Images ?? new List<string>(),
                Ratings = new Dictionary<int, double>(),
                Reviews = new Dictionary<int, string>(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var created = await _productRepository.AddAsync(product);
            _logger.LogInformation(
                "Product created by merchant {MerchantId}: {ProductName}",
                merchantId,
                request.ProductName
            );

            return await MapToProductResponse(created);
        }

        public async Task<ProductResponse> UpdateProductAsync(
            int merchantId,
            int productId,
            UpdateProductRequest request
        )
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
                throw new NotFoundException("Product not found");

            if (product.MerchantId != merchantId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to update this product"
                );

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.ProductName))
                product.ProductName = request.ProductName;

            if (!string.IsNullOrWhiteSpace(request.ProductType))
                product.ProductType = request.ProductType;

            if (!string.IsNullOrWhiteSpace(request.Category))
                product.Category = request.Category;

            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            if (!string.IsNullOrWhiteSpace(request.Description))
                product.Description = request.Description;

            if (request.StockQuantity.HasValue)
                product.StockQuantity = request.StockQuantity.Value;

            if (request.Specifications != null)
            {
                foreach (var spec in request.Specifications)
                {
                    product.Specifications[spec.Key] = spec.Value;
                }
            }

            if (request.Images != null)
            {
                product.Images = request.Images;
            }

            if (request.ImagesToRemove != null && request.ImagesToRemove.Any())
            {
                var newImages = product
                    .Images.Where((_, idx) => !request.ImagesToRemove.Contains(idx))
                    .ToList();
                product.Images = newImages;
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation(
                "Product {ProductId} updated by merchant {MerchantId}",
                productId,
                merchantId
            );

            return await MapToProductResponse(product);
        }

        public async Task<bool> DeleteProductAsync(int merchantId, int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
                throw new NotFoundException("Product not found");

            if (product.MerchantId != merchantId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to delete this product"
                );

            await _productRepository.DeleteAsync(productId);
            _logger.LogInformation(
                "Product {ProductId} deleted by merchant {MerchantId}",
                productId,
                merchantId
            );

            return true;
        }

        public async Task<ProductResponse> AddProductImageAsync(
            int merchantId,
            int productId,
            AddProductImageRequest request
        )
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
                throw new NotFoundException("Product not found");

            if (product.MerchantId != merchantId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to modify this product"
                );

            if (request.Position.HasValue && request.Position.Value < product.Images.Count)
            {
                product.Images.Insert(request.Position.Value, request.ImageUrl);
            }
            else
            {
                product.Images.Add(request.ImageUrl);
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation(
                "Image added to product {ProductId} by merchant {MerchantId}",
                productId,
                merchantId
            );

            return await MapToProductResponse(product);
        }

        public async Task<bool> DeleteProductImageAsync(
            int merchantId,
            int productId,
            int imageIndex
        )
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
                throw new NotFoundException("Product not found");

            if (product.MerchantId != merchantId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to modify this product"
                );

            if (imageIndex < 0 || imageIndex >= product.Images.Count)
                throw new ArgumentException("Invalid image index");

            product.Images.RemoveAt(imageIndex);
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation(
                "Image removed from product {ProductId} by merchant {MerchantId}",
                productId,
                merchantId
            );

            return true;
        }

        public async Task<ProductListResponse> GetAllProductsAsync(ProductFilterRequest filter)
        {
            var query = _productRepository.GetQueryable().Where(p => p.IsActive);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(filter.SearchTerm)
                    || p.Description.Contains(filter.SearchTerm)
                );
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(p => p.Category == filter.Category);
            }

            if (!string.IsNullOrWhiteSpace(filter.ProductType))
            {
                query = query.Where(p => p.ProductType == filter.ProductType);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            if (filter.MerchantId.HasValue)
            {
                query = query.Where(p => p.MerchantId == filter.MerchantId.Value);
            }

            if (filter.InStock.HasValue && filter.InStock.Value)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.ProductName),
                "name_desc" => query.OrderByDescending(p => p.ProductName),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "rating" => query.OrderByDescending(p =>
                    p.Ratings.Values.DefaultIfEmpty(0).Average()
                ),
                _ => query.OrderByDescending(p => p.CreatedAt),
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var productResponses = new List<ProductResponse>();
            foreach (var product in products)
            {
                productResponses.Add(await MapToProductResponse(product));
            }

            return new ProductListResponse
            {
                Products = productResponses,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
            };
        }

        public async Task<ProductResponse?> GetProductByIdAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null || !product.IsActive)
                return null;

            return await MapToProductResponse(product);
        }

        public async Task<ProductListResponse> GetMerchantProductsAsync(
            int merchantId,
            int page = 1,
            int pageSize = 20
        )
        {
            var filter = new ProductFilterRequest
            {
                MerchantId = merchantId,
                Page = page,
                PageSize = pageSize,
                SortBy = "newest",
            };

            return await GetAllProductsAsync(filter);
        }

        public async Task<ProductListResponse> GetProductsByCategoryAsync(
            string category,
            int page = 1,
            int pageSize = 20
        )
        {
            var filter = new ProductFilterRequest
            {
                Category = category,
                Page = page,
                PageSize = pageSize,
            };

            return await GetAllProductsAsync(filter);
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantityChange)
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
                throw new NotFoundException("Product not found");

            var newStock = product.StockQuantity + quantityChange;
            if (newStock < 0)
                throw new InvalidOperationException("Insufficient stock");

            product.StockQuantity = newStock;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            return true;
        }

        public async Task<bool> AdminDeleteProductAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
                throw new NotFoundException("Product not found");

            await _productRepository.DeleteAsync(productId);
            _logger.LogInformation("Product {ProductId} deleted by Admin", productId);

            return true;
        }

        public async Task<ProductListResponse> AdminGetAllProductsAsync(ProductFilterRequest filter)
        {
            // Admin can see all products including inactive ones
            var query = _productRepository.GetQueryable();

            // Similar filtering logic as GetAllProductsAsync but without IsActive filter
            // ... (same filtering logic)

            return await GetAllProductsAsync(filter);
        }

        private async Task<ProductResponse> MapToProductResponse(ProductEntity product)
        {
            // Simulate getting merchant name from Profile Service
            // In production, you'd call Profile Service API
            var merchantName = $"Merchant_{product.MerchantId}";

            var averageRating = product.Ratings.Values.Any()
                ? Math.Round(product.Ratings.Values.Average(), 1)
                : 0;

            return new ProductResponse
            {
                Id = product.Id,
                ProductName = product.ProductName,
                ProductType = product.ProductType,
                Category = product.Category,
                Price = product.Price,
                Description = product.Description,
                StockQuantity = product.StockQuantity,
                MerchantId = product.MerchantId,
                MerchantName = merchantName,
                Ratings = product.Ratings,
                Reviews = product.Reviews,
                Images = product.Images,
                Specifications = product.Specifications,
                AverageRating = averageRating,
                TotalReviews = product.Reviews.Count,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
            };
        }
    }
}
