using System.ComponentModel.DataAnnotations;

namespace EShoppingZone.Order.Application.DTOs
{
    public class PlaceOrderRequest
    {
        [Required]
        public int AddressId { get; set; }

        [Required]
        public string ModeOfPayment { get; set; } = "COD"; // COD or EWALLET
    }

    public class OrderResponse
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public decimal AmountPaid { get; set; }
        public string ModeOfPayment { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public AddressSnapshot Address { get; set; } = null!;
        public List<OrderItemResponse> Items { get; set; } = new();
    }

    public class AddressSnapshot
    {
        public string HouseNumber { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string ColonyName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Landmark { get; set; } = string.Empty;
    }

    public class OrderItemResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required]
        public string OrderStatus { get; set; } = string.Empty;
    }

    // DTOs for inter-service communication
    public class CartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CartResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public int TotalItems { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }

    public class AddressDto
    {
        public int Id { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string ColonyName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Landmark { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
