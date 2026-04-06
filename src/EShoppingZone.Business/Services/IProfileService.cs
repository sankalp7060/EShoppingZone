using EShoppingZone.Business.DTOs;

namespace EShoppingZone.Business.Services
{
    public interface IProfileService
    {
        Task<ProfileResponse> GetProfileAsync(int userId);
        Task<ProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task<AddressDto> AddAddressAsync(int userId, AddAddressRequest request);
        Task<AddressDto> UpdateAddressAsync(int userId, UpdateAddressRequest request);
        Task<bool> DeleteAddressAsync(int userId, int addressId);
        Task<AddressDto> SetDefaultAddressAsync(int userId, int addressId);
        Task<List<AddressDto>> GetAllAddressesAsync(int userId);
        Task<AddressDto?> GetAddressByIdAsync(int userId, int addressId);
        Task<bool> DeleteProfileImageAsync(int userId);
        Task<ProfileResponse> UploadProfileImageAsync(int userId, string imageUrl);
    }
}
