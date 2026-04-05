using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Order.Domain.Entities
{
    public class OrderStatusHistoryEntity : BaseEntity
    {
        public int OrderId { get; set; }
        public OrderEntity Order { get; set; } = null!;
        public string Status { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
        public string? Remarks { get; set; }
    }
}
