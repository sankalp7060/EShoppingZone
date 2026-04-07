using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BCrypt.Net;
using EShoppingZone.Business.DTOs;
using EShoppingZone.Common.Exceptions;
using EShoppingZone.Data.Repositories;
using EShoppingZone.Models.Entities;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EShoppingZone.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration,
            IDistributedCache cache,
            ILogger<AuthService> logger
        )
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new InvalidOperationException("Email already registered");

            if (await _userRepository.MobileExistsAsync(request.MobileNumber))
                throw new InvalidOperationException("Mobile number already registered");

            if (request.Role == UserRole.Admin)
                throw new InvalidOperationException(
                    "Admin accounts can only be created by existing Admins"
                );

            var user = new UserEntity
            {
                FullName = request.FullName,
                Email = request.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber,
                Gender = request.Gender ?? string.Empty,
                DateOfBirth = request.DateOfBirth,
                Role = request.Role,
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

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User logged in: {Email}", user.Email);

            await _cache.SetStringAsync(
                $"user_session_{user.Id}",
                JsonSerializer.Serialize(
                    new
                    {
                        user.Id,
                        user.Email,
                        LoginTime = DateTime.UtcNow,
                    }
                ),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                }
            );

            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponse> GoogleLoginAsync(GoogleAuthRequest request)
        {
            try
            {
                string email, name, picture;
                bool emailVerified;

                // Detect if this is a Google ID token (JWT) or an OAuth2 access token
                // JWT tokens have 3 dot-separated base64 parts; access tokens don't
                bool isJwt = request.IdToken.Split('.').Length == 3;

                if (isJwt)
                {
                    // Validate as a proper Google ID token (JWT)
                    var settings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["Google:ClientId"] },
                    };
                    var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                    email = payload.Email;
                    name = payload.Name;
                    picture = payload.Picture;
                    emailVerified = payload.EmailVerified;
                }
                else
                {
                    // It's an OAuth2 access token — call Google's userinfo endpoint
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.IdToken);

                    var userInfoResponse = await httpClient.GetAsync(
                        "https://www.googleapis.com/oauth2/v3/userinfo"
                    );

                    if (!userInfoResponse.IsSuccessStatusCode)
                        throw new UnauthorizedAccessException("Failed to verify Google access token");

                    var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
                    var userInfo = System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(
                        userInfoJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                        throw new UnauthorizedAccessException("Could not retrieve Google user info");

                    email = userInfo.Email;
                    name = userInfo.Name ?? userInfo.Email;
                    picture = userInfo.Picture ?? string.Empty;
                    emailVerified = userInfo.Email_Verified;
                }

                // Find or create the user
                var user = await _userRepository.GetByOAuthAsync("Google", email);

                if (user == null)
                {
                    user = await _userRepository.GetByEmailAsync(email);

                    if (user == null)
                    {
                        user = new UserEntity
                        {
                            FullName = name,
                            Email = email,
                            ProfileImage = picture,
                            OAuthProvider = "Google",
                            OAuthId = email,
                            Role = UserRole.Customer,
                            IsEmailVerified = emailVerified,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                        };
                        await _userRepository.CreateAsync(user);
                        _logger.LogInformation("New Google user registered: {Email}", user.Email);
                    }
                    else
                    {
                        user.OAuthProvider = "Google";
                        user.OAuthId = email;
                        user.IsEmailVerified = emailVerified;
                        user.ProfileImage ??= picture;
                        await _userRepository.UpdateAsync(user);
                        _logger.LogInformation(
                            "Google linked to existing account: {Email}",
                            user.Email
                        );
                    }
                }
                else
                {
                    user.FullName = name;
                    user.ProfileImage = picture;
                    user.IsEmailVerified = emailVerified;
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                return await GenerateAuthResponse(user);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogError(ex, "Invalid Google ID token");
                throw new UnauthorizedAccessException("Invalid Google token");
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed");
                throw new UnauthorizedAccessException("Google authentication failed");
            }
        }

        // Helper class for Google userinfo API response
        private class GoogleUserInfo
        {
            public string? Sub { get; set; }
            public string? Email { get; set; }
            public bool Email_Verified { get; set; }
            public string? Name { get; set; }
            public string? Picture { get; set; }
            public string? Given_Name { get; set; }
            public string? Family_Name { get; set; }
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

            refreshToken.IsUsed = true;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

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
                    token.IsRevoked = true;
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
                await RevokeRefreshTokenAsync(refreshToken);

            await _cache.RemoveAsync($"user_session_{userId}");
            _logger.LogInformation("User logged out: {UserId}", userId);
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
                ExpiryDate = DateTime.UtcNow.AddDays(7),
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
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
            };
        }
    }
}
