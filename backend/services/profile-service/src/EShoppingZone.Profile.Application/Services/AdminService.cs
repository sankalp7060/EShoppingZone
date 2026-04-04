using BCrypt.Net;
using EShoppingZone.Profile.Application.Common.Exceptions;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Profile.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IUserRepository userRepository,
            IAuthService authService,
            ILogger<AdminService> logger
        )
        {
            _userRepository = userRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<AuthResponse> CreateUserByAdminAsync(AdminCreateUserRequest request)
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already registered");

            // Check if mobile already exists
            if (await _userRepository.MobileExistsAsync(request.MobileNumber))
                throw new InvalidOperationException("Mobile number already registered");

            // Create new user
            var user = new UserEntity
            {
                FullName = request.FullName,
                Email = request.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber,
                Gender = request.Gender ?? string.Empty,
                DateOfBirth = request.DateOfBirth,
                Role = request.Role,
                IsEmailVerified = true, // Admin-created users are verified
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            await _userRepository.CreateAsync(user);
            _logger.LogInformation(
                "Admin created user: {Email} with role {Role}",
                user.Email,
                user.Role
            );

            // Generate auth response
            return await GenerateAuthResponse(user);
        }

        public async Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found");

            var oldRole = user.Role;
            user.Role = newRole;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation(
                "User {UserId} role changed from {OldRole} to {NewRole}",
                userId,
                oldRole,
                newRole
            );

            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToUserDto);
        }

        private async Task<AuthResponse> GenerateAuthResponse(UserEntity user)
        {
            // This is a simplified version - in real scenario, you might not want to return token
            // when admin creates user. Adjust based on your needs.
            return new AuthResponse
            {
                Token = "", // Or generate token if needed
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = MapToUserDto(user),
            };
        }

        private UserDto MapToUserDto(UserEntity user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                MobileNumber = user.MobileNumber,
                Role = user.Role.ToString(),
                ProfileImage = user.ProfileImage,
                IsEmailVerified = user.IsEmailVerified,
            };
        }
    }
}
