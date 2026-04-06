using System.ComponentModel.DataAnnotations;

namespace EShoppingZone.Business.DTOs
{
    public class AddToCartRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateCartItemRequest
    {
        [Required]
        public int CartItemId { get; set; }

        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }
    }

    public class RemoveFromCartRequest
    {
        [Required]
        public int CartItemId { get; set; }
    }

    public class CartItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CartResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public int TotalItems { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
        public DateTime? LastUpdatedAt { get; set; }
    }

    public class CartSummaryResponse
    {
        public int TotalItems { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
