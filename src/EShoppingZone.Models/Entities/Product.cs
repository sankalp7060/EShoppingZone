using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Models.Entities
{
    public class Product : BaseEntity
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int StockQuantity { get; set; }

        // JSON stored fields (PostgreSQL JSONB)
        public Dictionary<int, double> Ratings { get; set; } = new();
        public Dictionary<int, string> Reviews { get; set; } = new();
        public List<string> Images { get; set; } = new();
        public Dictionary<string, string> Specifications { get; set; } = new();

        // Foreign keys
        public int MerchantId { get; set; }
    }
}
