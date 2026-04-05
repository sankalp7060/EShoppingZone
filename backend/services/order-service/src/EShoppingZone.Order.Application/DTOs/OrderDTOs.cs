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

    public class WalletPaymentRequest
    {
        [Required]
        public int AddressId { get; set; }

        [Required]
        public string ModeOfPayment { get; set; } = "EWALLET";
    }

    public class WalletPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal WalletBalanceAfter { get; set; }
        public int TransactionId { get; set; }
    }

    public class OrderStatusHistoryResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateOrderStatusWithRemarksRequest
    {
        [Required]
        public string OrderStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    public class OrderTrackingResponse
    {
        public int OrderId { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = new();
        public List<string> AvailableActions { get; set; } = new();
    }

    public class OrderFilterRequest
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "newest";
    }

    public class OrderListResponse
    {
        public List<OrderResponse> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
