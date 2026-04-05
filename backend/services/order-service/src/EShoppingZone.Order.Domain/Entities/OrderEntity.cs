using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Order.Domain.Entities
{
    public class OrderEntity : BaseEntity
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public decimal AmountPaid { get; set; }
        public string ModeOfPayment { get; set; } = string.Empty; // "COD", "EWALLET"
        public string OrderStatus { get; set; } = "Placed"; // Placed, Shipped, Delivered, Cancelled
        public int Quantity { get; set; }

        // Address snapshot (stored at order time)
        public string AddressHouseNumber { get; set; } = string.Empty;
        public string AddressStreetName { get; set; } = string.Empty;
        public string AddressColonyName { get; set; } = string.Empty;
        public string AddressCity { get; set; } = string.Empty;
        public string AddressState { get; set; } = string.Empty;
        public string AddressPincode { get; set; } = string.Empty;
        public string AddressLandmark { get; set; } = string.Empty;

        // Order items (JSON stored)
        public List<OrderItemEntity> OrderItems { get; set; } = new();
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
