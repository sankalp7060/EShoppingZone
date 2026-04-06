using System.Text.Json;
using EShoppingZone.Profile.Application.Common.Exceptions;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Profile.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAddressRepository _addressRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            IUserRepository userRepository,
            IAddressRepository addressRepository,
            IDistributedCache cache,
            ILogger<ProfileService> logger
        )
        {
            _userRepository = userRepository;
            _addressRepository = addressRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ProfileResponse> GetProfileAsync(int userId)
        {
            var cacheKey = $"user_profile_{userId}";
            var cachedProfile = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedProfile))
            {
                return JsonSerializer.Deserialize<ProfileResponse>(cachedProfile)!;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            var response = MapToProfileResponse(user);

            // Cache for 30 minutes
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                }
            );

            return response;
        }

        public async Task<ProfileResponse> UpdateProfileAsync(
            int userId,
            UpdateProfileRequest request
        )
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (request.MobileNumber.HasValue && request.MobileNumber.Value > 0)
            {
                // Check if mobile number is already taken by another user
                var existingUser = await _userRepository.GetByMobileNumberAsync(
                    request.MobileNumber.Value
                );
                if (existingUser != null && existingUser.Id != userId)
                    throw new InvalidOperationException(
                        "Mobile number already registered by another user"
                    );

                user.MobileNumber = request.MobileNumber.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.About))
                user.About = request.About;

            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = DateTime.SpecifyKind(
                    request.DateOfBirth.Value,
                    DateTimeKind.Utc
                );
            }

            if (!string.IsNullOrWhiteSpace(request.Gender))
                user.Gender = request.Gender;

            if (!string.IsNullOrWhiteSpace(request.ProfileImage))
                user.ProfileImage = request.ProfileImage;

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Profile updated for user {UserId}", userId);

            // Invalidate cache
            await _cache.RemoveAsync($"user_profile_{userId}");

            return MapToProfileResponse(user);
        }

        public async Task<AddressDto> AddAddressAsync(int userId, AddAddressRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            // If this is the first address or marked as default, update other addresses
            var existingAddresses = await _addressRepository.GetByUserIdAsync(userId);
            bool shouldBeDefault = request.IsDefault || !existingAddresses.Any();

            if (shouldBeDefault)
            {
                // Remove default flag from other addresses
                foreach (var addr in existingAddresses.Where(a => a.IsDefault))
                {
                    addr.IsDefault = false;
                    await _addressRepository.UpdateAsync(addr);
                }
            }

            var address = new AddressEntity
            {
                HouseNumber = request.HouseNumber,
                StreetName = request.StreetName,
                ColonyName = request.ColonyName ?? string.Empty,
                City = request.City,
                State = request.State,
                Pincode = request.Pincode,
                Landmark = request.Landmark,
                IsDefault = shouldBeDefault,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var created = await _addressRepository.CreateAsync(address);
            _logger.LogInformation("New address added for user {UserId}", userId);

            // Invalidate profile cache
            await _cache.RemoveAsync($"user_profile_{userId}");
            await _cache.RemoveAsync($"user_addresses_{userId}");

            return MapToAddressDto(created);
        }

        public async Task<AddressDto> UpdateAddressAsync(int userId, UpdateAddressRequest request)
        {
            var address = await _addressRepository.GetByIdAsync(request.AddressId);
            if (address == null)
                throw new NotFoundException("Address not found");

            if (address.UserId != userId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to update this address"
                );

            // Update fields
            address.HouseNumber = request.HouseNumber;
            address.StreetName = request.StreetName;
            address.ColonyName = request.ColonyName ?? string.Empty;
            address.City = request.City;
            address.State = request.State;
            address.Pincode = request.Pincode;
            address.Landmark = request.Landmark;

            // Handle default address logic
            if (request.IsDefault && !address.IsDefault)
            {
                // Remove default flag from other addresses
                var userAddresses = await _addressRepository.GetByUserIdAsync(userId);
                foreach (var addr in userAddresses.Where(a => a.IsDefault))
                {
                    addr.IsDefault = false;
                    await _addressRepository.UpdateAsync(addr);
                }
                address.IsDefault = true;
            }
            else if (!request.IsDefault && address.IsDefault)
            {
                // Can't unset default if it's the only address
                var userAddresses = await _addressRepository.GetByUserIdAsync(userId);
                if (userAddresses.Count() == 1)
                    throw new InvalidOperationException(
                        "Cannot unset default address when it's the only address"
                    );

                address.IsDefault = false;
            }

            address.UpdatedAt = DateTime.UtcNow;
            await _addressRepository.UpdateAsync(address);
            _logger.LogInformation(
                "Address updated for user {UserId}, AddressId {AddressId}",
                userId,
                request.AddressId
            );

            return MapToAddressDto(address);
        }

        public async Task<bool> DeleteAddressAsync(int userId, int addressId)
        {
            var address = await _addressRepository.GetByIdAsync(addressId);
            if (address == null)
                throw new NotFoundException("Address not found");

            if (address.UserId != userId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to delete this address"
                );

            // Check if it's the last address
            var userAddresses = await _addressRepository.GetByUserIdAsync(userId);
            if (userAddresses.Count() == 1)
                throw new InvalidOperationException(
                    "Cannot delete the last address. Please add another address first."
                );

            // If deleting default address, set another as default
            if (address.IsDefault)
            {
                var newDefault = userAddresses.FirstOrDefault(a => a.Id != addressId);
                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    await _addressRepository.UpdateAsync(newDefault);
                }
            }

            await _addressRepository.DeleteAsync(addressId);
            _logger.LogInformation(
                "Address deleted for user {UserId}, AddressId {AddressId}",
                userId,
                addressId
            );

            return true;
        }

        public async Task<AddressDto> SetDefaultAddressAsync(int userId, int addressId)
        {
            var address = await _addressRepository.GetByIdAsync(addressId);
            if (address == null)
                throw new NotFoundException("Address not found");

            if (address.UserId != userId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to modify this address"
                );

            // Remove default flag from all user addresses
            var userAddresses = await _addressRepository.GetByUserIdAsync(userId);
            foreach (var addr in userAddresses.Where(a => a.IsDefault))
            {
                addr.IsDefault = false;
                await _addressRepository.UpdateAsync(addr);
            }

            // Set new default
            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;
            await _addressRepository.UpdateAsync(address);

            _logger.LogInformation(
                "Default address set for user {UserId}, AddressId {AddressId}",
                userId,
                addressId
            );
            return MapToAddressDto(address);
        }

        public async Task<List<AddressDto>> GetAllAddressesAsync(int userId)
        {
            var cacheKey = $"user_addresses_{userId}";
            var cachedAddresses = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedAddresses))
            {
                return JsonSerializer.Deserialize<List<AddressDto>>(cachedAddresses)!;
            }
            var addresses = await _addressRepository.GetByUserIdAsync(userId);

            var addressDtos = addresses.Select(MapToAddressDto).ToList();

            // Cache for 30 minutes
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(addressDtos),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                }
            );

            return addressDtos;
        }

        public async Task<AddressDto?> GetAddressByIdAsync(int userId, int addressId)
        {
            var address = await _addressRepository.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                return null;

            return MapToAddressDto(address);
        }

        public async Task<bool> DeleteProfileImageAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            user.ProfileImage = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Profile image deleted for user {UserId}", userId);
            return true;
        }

        public async Task<ProfileResponse> UploadProfileImageAsync(int userId, string imageUrl)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            user.ProfileImage = imageUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Profile image uploaded for user {UserId}", userId);
            return MapToProfileResponse(user);
        }

        private ProfileResponse MapToProfileResponse(UserEntity user)
        {
            return new ProfileResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                MobileNumber = user.MobileNumber,
                About = user.About,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                ProfileImage = user.ProfileImage,
                Role = user.Role.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                Addresses = user.Addresses?.Select(MapToAddressDto).ToList() ?? new(),
            };
        }

        private AddressDto MapToAddressDto(AddressEntity address)
        {
            return new AddressDto
            {
                Id = address.Id,
                HouseNumber = address.HouseNumber,
                StreetName = address.StreetName,
                ColonyName = address.ColonyName,
                City = address.City,
                State = address.State,
                Pincode = address.Pincode,
                Landmark = address.Landmark,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
            };
        }
    }
}
