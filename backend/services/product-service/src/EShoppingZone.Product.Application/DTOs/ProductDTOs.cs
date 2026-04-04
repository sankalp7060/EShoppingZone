using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EShoppingZone.Product.Application.DTOs
{
    public class CreateProductRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ProductType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, 999999)]
        public int StockQuantity { get; set; }

        public Dictionary<string, string> Specifications { get; set; } = new();

        public List<string> Images { get; set; } = new();
        public int? MerchantId { get; set; }
    }

    public class UpdateProductRequest
    {
        [StringLength(200, MinimumLength = 3)]
        public string? ProductName { get; set; }

        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [Range(0.01, 999999.99)]
        public decimal? Price { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Range(0, 999999)]
        public int? StockQuantity { get; set; }

        public Dictionary<string, string>? Specifications { get; set; }

        public List<string>? Images { get; set; }

        public List<int>? ImagesToRemove { get; set; }
    }

    public class AddProductImageRequest
    {
        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public int? Position { get; set; }
    }

    public class ProductResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public Dictionary<int, double> Ratings { get; set; } = new();
        public Dictionary<int, string> Reviews { get; set; } = new();
        public List<string> Images { get; set; } = new();
        public Dictionary<string, string> Specifications { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductListResponse
    {
        public List<ProductResponse> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProductFilterRequest
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? ProductType { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MerchantId { get; set; }
        public bool? InStock { get; set; }
        public string? SortBy { get; set; } // price_asc, price_desc, name_asc, name_desc, newest, rating
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
