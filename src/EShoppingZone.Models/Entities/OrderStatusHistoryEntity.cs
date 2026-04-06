using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Models.Entities
{
    public class OrderStatusHistoryEntity : BaseEntity
    {
        public int OrderId { get; set; }
        public OrderEntity Order { get; set; } = null!;
        public string Status { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
