using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Order.Domain.Entities
{
    public class OrderEntity : BaseEntity
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public decimal AmountPaid { get; set; }
        public string ModeOfPayment { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = "Placed";
        public int Quantity { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string? CancellationReason { get; set; }

        // Address snapshot
        public string AddressHouseNumber { get; set; } = string.Empty;
        public string AddressStreetName { get; set; } = string.Empty;
        public string AddressColonyName { get; set; } = string.Empty;
        public string AddressCity { get; set; } = string.Empty;
        public string AddressState { get; set; } = string.Empty;
        public string AddressPincode { get; set; } = string.Empty;
        public string AddressLandmark { get; set; } = string.Empty;

        // Order items
        public List<OrderItemEntity> OrderItems { get; set; } = new();

        // Status history
        public List<OrderStatusHistoryEntity> StatusHistory { get; set; } = new();
    }

    public class OrderItemEntity
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string? ImageUrl { get; set; }
    }
}
