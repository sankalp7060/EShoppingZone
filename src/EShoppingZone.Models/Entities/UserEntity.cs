using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Models.Entities
{
    public class UserEntity : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public string? ProfileImage { get; set; }
        public long MobileNumber { get; set; }
        public string? About { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;
        public bool IsEmailVerified { get; set; } = false;
        public string? OAuthProvider { get; set; }
        public string? OAuthId { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public IList<AddressEntity> Addresses { get; set; } = new List<AddressEntity>();
        public IList<RefreshTokenEntity> RefreshTokens { get; set; } =
            new List<RefreshTokenEntity>();
    }

    public enum UserRole
    {
        Customer = 1,
        Merchant = 2,
        Admin = 3,
        DeliveryAgent = 4,
    }
}
