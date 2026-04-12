using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BCrypt.Net;
using EShoppingZone.Profile.Application.Common.Exceptions;
using EShoppingZone.Profile.Application.DTOs;
using EShoppingZone.Profile.Domain.Entities;
using EShoppingZone.Profile.Infrastructure.Repositories;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EShoppingZone.Profile.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger
        )
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already registered");

            // Check if mobile already exists
            if (await _userRepository.MobileExistsAsync(request.MobileNumber))
                throw new InvalidOperationException("Mobile number already registered");

            // Role validation - Only Admin can create Admin accounts
            var isAdminRequest = request.Role == UserRole.Admin;
            if (isAdminRequest)
            {
                // Check if current user is Admin (when called from admin panel)
                // For public registration, prevent Admin role
                throw new InvalidOperationException(
                    "Admin accounts can only be created by existing Admins"
                );
            }

            // Create new user
            var user = new UserEntity
            {
                FullName = request.FullName,
                Email = request.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber,
                Gender = request.Gender ?? string.Empty,
                DateOfBirth = request.DateOfBirth,
                Role = request.Role, // Use the role from request
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            await _userRepository.CreateAsync(user);
            _logger.LogInformation(
                "New user registered: {Email} with role {Role}",
                user.Email,
                user.Role
            );

            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated");

            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new UnauthorizedAccessException("Please login using Google OAuth");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password");

            // Role verification
            if (!string.IsNullOrEmpty(request.Role))
            {
                if (user.Role.ToString().ToLower() != request.Role.ToLower())
                {
                    _logger.LogWarning("Role mismatch login attempt for {Email}: Expected {Selected}, actual {Actual}", 
                        user.Email, request.Role, user.Role);
                    throw new UnauthorizedAccessException($"This account is registered as a {user.Role}. Please log in with the correct role.");
                }
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User logged in: {Email}", user.Email);

            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponse> GoogleLoginAsync(GoogleAuthRequest request)
        {
            try
            {
                string email;
                string name;
                string? pictureUrl = null;
                string googleId = null;
                bool emailVerified = false;

                // Parse the selected role from request
                UserRole selectedRole = UserRole.Customer;
                if (!string.IsNullOrEmpty(request.Role))
                {
                    selectedRole = request.Role.ToLower() switch
                    {
                        "merchant" => UserRole.Merchant,
                        "deliveryagent" => UserRole.DeliveryAgent,
                        "customer" => UserRole.Customer,
                        _ => UserRole.Customer
                    };
                }

                _logger.LogInformation("Google Login Attempt with Role: {Role}", selectedRole);

                // Check if it's an ID Token (JWT format containing dots) or an Access Token
                if (request.IdToken.Contains('.') && request.IdToken.Split('.').Length == 3)
                {
                    _logger.LogInformation("Verifying Google ID Token (JWT)");
                    // Verify Google ID token
                    var settings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["Google:ClientId"] },
                    };

                    var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                    email = payload.Email;
                    name = payload.Name;
                    pictureUrl = payload.Picture;
                    googleId = payload.Subject;
                    emailVerified = payload.EmailVerified;
                }
                else
                {
                    _logger.LogInformation("Verifying Google Access Token via UserInfo API");
                    // Verify Google Access Token via Google's userinfo API
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        request.IdToken
                    );
                    var response = await httpClient.GetAsync(
                        "https://www.googleapis.com/oauth2/v3/userinfo"
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Invalid Google Access Token: {Status}", response.StatusCode);
                        throw new UnauthorizedAccessException("Invalid Google access token");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<JsonElement>(content);

                    email = userInfo.GetProperty("email").GetString()!;
                    name = userInfo.GetProperty("name").GetString()!;
                    googleId = userInfo.GetProperty("sub").GetString()!;
                    if (userInfo.TryGetProperty("picture", out var pic))
                        pictureUrl = pic.GetString();
                    if (userInfo.TryGetProperty("email_verified", out var ev))
                        emailVerified = ev.GetBoolean();
                }

                _logger.LogInformation(
                    "Google verified: Email={Email}, GoogleId={GoogleId}, SelectedRole={Role}",
                    email,
                    googleId,
                    selectedRole
                );

                // 1. Check if user already exists
                var existingUserByOAuth = await _userRepository.GetByOAuthAsync("Google", googleId);
                var existingUserByEmail = await _userRepository.GetByEmailAsync(email);
                var user = existingUserByOAuth ?? existingUserByEmail;

                // 2. Logic to handle user creation vs login
                if (user == null)
                {
                    // REGISTRATION PATH
                    if (string.IsNullOrEmpty(request.Role))
                    {
                        _logger.LogWarning("Login attempt for non-existent user: {Email}", email);
                        throw new UnauthorizedAccessException("Account not found. Please register first.");
                    }

                    // Create new user with selected role
                    var newUser = new UserEntity
                    {
                        FullName = name,
                        Email = email.ToLower(),
                        ProfileImage = pictureUrl,
                        OAuthProvider = "Google",
                        OAuthId = googleId,
                        Role = selectedRole,
                        IsEmailVerified = emailVerified,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        MobileNumber = 0,
                        Gender = "Other",
                    };

                    try
                    {
                        var createdUser = await _userRepository.CreateAsync(newUser);
                        _logger.LogInformation("New Google user registered successfully: {UserId}", createdUser.Id);
                        return await GenerateAuthResponse(createdUser);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save new Google user: {Email}", email);
                        throw new Exception("Failed to create Google user account.");
                    }
                }
                else
                {
                    // LOGIN PATH (for existing users)
                    
                    // If role was explicitly provided for an existing user, verify it
                    if (!string.IsNullOrEmpty(request.Role))
                    {
                        if (user.Role.ToString().ToLower() != request.Role.ToLower())
                        {
                            _logger.LogWarning("Google Role mismatch for {Email}: Expected {Selected}, actual {Actual}", 
                                user.Email, request.Role, user.Role);
                            throw new UnauthorizedAccessException($"This account is already registered as a {user.Role}. Please select the correct role to log in.");
                        }
                    }

                    // If found by email but not linked to Google
                    if (string.IsNullOrEmpty(user.OAuthProvider))
                    {
                        user.OAuthProvider = "Google";
                        user.OAuthId = googleId;
                        user.IsEmailVerified = emailVerified;
                        user.ProfileImage ??= pictureUrl;
                        await _userRepository.UpdateAsync(user);
                        _logger.LogInformation("Google linked to existing email account: {Email}", user.Email);
                    }
                    else
                    {
                        // Existing OAuth user - Update profile info
                        user.FullName = name;
                        user.ProfileImage = pictureUrl;
                        user.IsEmailVerified = emailVerified;
                        await _userRepository.UpdateAsync(user);
                    }

                    return await GenerateAuthResponse(user);
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                return await GenerateAuthResponse(user);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogError(ex, "Invalid Google token validation failed");
                throw new UnauthorizedAccessException("Invalid Google token signature");
            }
            catch (InvalidOperationException ex)
            {
                throw; // Re-throw planned registration errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Google login/register error");
                throw;
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

            if (refreshToken == null)
                throw new UnauthorizedAccessException("Invalid refresh token");

            if (refreshToken.IsRevoked)
                throw new UnauthorizedAccessException("Refresh token has been revoked");

            if (refreshToken.IsUsed)
                throw new UnauthorizedAccessException("Refresh token has already been used");

            if (refreshToken.ExpiryDate < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired");

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("User not found or deactivated");

            // Mark the old token as used
            refreshToken.IsUsed = true;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            // Generate new tokens
            var accessToken = GenerateJwtToken(user);
            var newRefreshToken = await GenerateRefreshToken(
                user.Id,
                refreshToken.DeviceInfo,
                refreshToken.IpAddress
            );

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                AccessTokenExpiresAt = DateTime.UtcNow.AddHours(24),
                RefreshTokenExpiresAt = newRefreshToken.ExpiryDate,
                User = MapToUserDto(user),
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token == null)
                return false;

            token.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(token);

            _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);
            return true;
        }

        public async Task<bool> RevokeAllUserTokensAsync(int userId, string? deviceInfo = null)
        {
            var tokens = await _refreshTokenRepository.GetAllByUserIdAsync(userId);

            foreach (var token in tokens)
            {
                if (deviceInfo == null || token.DeviceInfo == deviceInfo)
                {
                    token.IsRevoked = true;
                }
            }

            await _refreshTokenRepository.UpdateRangeAsync(tokens);
            _logger.LogInformation("All tokens revoked for user {UserId}", userId);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found");

            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new InvalidOperationException(
                    "Google OAuth users cannot change password. Use Google to login."
                );

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);

            // Revoke all refresh tokens on password change
            await RevokeAllUserTokensAsync(userId);

            return true;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

                tokenHandler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = _configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = _configuration["Jwt:Audience"],
                        ClockSkew = TimeSpan.Zero,
                    },
                    out _
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user == null ? null : MapToUserDto(user);
        }

        public async Task LogoutAsync(int userId, string? refreshToken = null)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await RevokeRefreshTokenAsync(refreshToken);
            }

            _logger.LogInformation("User logged out: {UserId}", userId);
            await Task.CompletedTask;
        }

        private async Task<AuthResponse> GenerateAuthResponse(
            UserEntity user,
            string? deviceInfo = null,
            string? ipAddress = null
        )
        {
            var accessToken = GenerateJwtToken(user);
            var refreshToken = await GenerateRefreshToken(user.Id, deviceInfo, ipAddress);

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                RefreshTokenExpiresAt = refreshToken.ExpiryDate,
                User = MapToUserDto(user),
            };
        }

        private string GenerateJwtToken(UserEntity user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            var tokenId = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, tokenId),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Role", user.Role.ToString()),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<RefreshTokenEntity> GenerateRefreshToken(
            int userId,
            string? deviceInfo = null,
            string? ipAddress = null
        )
        {
            var refreshToken = new RefreshTokenEntity
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                JwtId = Guid.NewGuid().ToString(),
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(7), // 7 days expiry
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            await _refreshTokenRepository.CreateAsync(refreshToken);
            return refreshToken;
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
        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
                return null;

            return MapToUserDto(user);
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
                throw new NotFoundException("User not found");

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await _userRepository.UpdateAsync(user);

            // Revoke all refresh tokens after password reset
            await RevokeAllUserTokensAsync(user.Id);

            return true;
        }
    }
}
