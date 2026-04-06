using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Models.Entities
{
    public class RefreshTokenEntity : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; } = false;
        public bool IsUsed { get; set; } = false;
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
    }
}
