using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Cart.Domain.Entities
{
    public class CartEntity : BaseEntity
    {
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public ICollection<CartItemEntity> Items { get; set; } = new List<CartItemEntity>();
    }

    public class CartItemEntity : BaseEntity
    {
        public int CartId { get; set; }
        public CartEntity Cart { get; set; } = null!;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
    }
}
