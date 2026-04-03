using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Profile.Domain.Entities
{
    public class AddressEntity : BaseEntity
    {
        public string HouseNumber { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string ColonyName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public bool IsDefault { get; set; } = false;

        // Foreign key
        public int UserId { get; set; }
        public UserEntity User { get; set; } = null!;
    }
}
