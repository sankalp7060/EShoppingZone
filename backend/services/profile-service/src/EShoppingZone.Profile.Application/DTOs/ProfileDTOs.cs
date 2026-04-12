using System.ComponentModel.DataAnnotations;

namespace EShoppingZone.Profile.Application.DTOs
{
    public class UpdateProfileRequest
    {
        [StringLength(200, MinimumLength = 2)]
        public string? FullName { get; set; }

        [Range(1000000000, 9999999999, ErrorMessage = "Mobile number must be 10 digits")]
        public long? MobileNumber { get; set; }

        [StringLength(1000)]
        public string? About { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public string? ProfileImage { get; set; }
    }

    public class AddAddressRequest
    {
        [Required]
        [StringLength(50)]
        public string HouseNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string StreetName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ColonyName { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^[1-9][0-9]{5}$", ErrorMessage = "Invalid pincode")]
        public string Pincode { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Landmark { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateAddressRequest : AddAddressRequest
    {
        [Required]
        public int AddressId { get; set; }
    }

    public class AddressDto
    {
        public int Id { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string? ColonyName { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProfileResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long MobileNumber { get; set; }
        public string? About { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? ProfileImage { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AddressDto> Addresses { get; set; } = new();
    }
}
