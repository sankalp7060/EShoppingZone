using System.Text.Json;
using EShoppingZone.Cart.Application.DTOs;
using EShoppingZone.Cart.Domain.Entities;
using EShoppingZone.Cart.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EShoppingZone.Cart.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CartService> _logger;

        public CartService(
            ICartRepository cartRepository,
            IDistributedCache cache,
            ILogger<CartService> logger
        )
        {
            _cartRepository = cartRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CartResponse> GetCartAsync(int userId)
        {
            // Try Redis cache first
            var cacheKey = $"cart_{userId}";
            var cachedCart = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedCart))
            {
                return JsonSerializer.Deserialize<CartResponse>(cachedCart)!;
            }

            var cart = await _cartRepository.GetByUserIdAsync(userId);

            if (cart == null)
            {
                return new CartResponse
                {
                    UserId = userId,
                    Items = new List<CartItemResponse>(),
                    TotalPrice = 0,
                };
            }

            var response = MapToCartResponse(cart);

            // Cache for 10 minutes
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                }
            );

            return response;
        }

        public async Task<CartResponse> AddToCartAsync(int userId, AddToCartRequest request)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);

            if (cart == null)
            {
                cart = new CartEntity
                {
                    UserId = userId,
                    Items = new List<CartItemEntity>(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };
                await _cartRepository.CreateAsync(cart);
            }

            // Check if product already exists in cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                // Update quantity
                existingItem.Quantity += request.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new item
                cart.Items.Add(
                    new CartItemEntity
                    {
                        ProductId = request.ProductId,
                        ProductName = request.ProductName,
                        Price = request.Price,
                        Quantity = request.Quantity,
                        ImageUrl = request.ImageUrl,
                        CartId = cart.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                    }
                );
            }

            // Calculate total
            cart.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
            cart.LastUpdatedAt = DateTime.UtcNow;

            await _cartRepository.UpdateAsync(cart);

            // Invalidate cache
            await _cache.RemoveAsync($"cart_{userId}");

            _logger.LogInformation(
                "Item added to cart for user {UserId}, Product: {ProductName}",
                userId,
                request.ProductName
            );

            return await GetCartAsync(userId);
        }

        public async Task<CartResponse> UpdateCartItemAsync(
            int userId,
            UpdateCartItemRequest request
        )
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);

            if (cart == null)
                throw new InvalidOperationException("Cart not found");

            var cartItem = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);

            if (cartItem == null)
                throw new InvalidOperationException("Cart item not found");

            cartItem.Quantity = request.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            // Recalculate total
            cart.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
            cart.LastUpdatedAt = DateTime.UtcNow;

            await _cartRepository.UpdateAsync(cart);

            // Invalidate cache
            await _cache.RemoveAsync($"cart_{userId}");

            _logger.LogInformation(
                "Cart item updated for user {UserId}, ItemId: {CartItemId}, Quantity: {Quantity}",
                userId,
                request.CartItemId,
                request.Quantity
            );

            return await GetCartAsync(userId);
        }

        public async Task<CartResponse> RemoveFromCartAsync(int userId, int cartItemId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);

            if (cart == null)
                throw new InvalidOperationException("Cart not found");

            var cartItem = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (cartItem == null)
                throw new InvalidOperationException("Cart item not found");

            cart.Items.Remove(cartItem);

            // Recalculate total
            cart.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
            cart.LastUpdatedAt = DateTime.UtcNow;

            await _cartRepository.UpdateAsync(cart);

            // Invalidate cache
            await _cache.RemoveAsync($"cart_{userId}");

            _logger.LogInformation(
                "Item removed from cart for user {UserId}, ItemId: {CartItemId}",
                userId,
                cartItemId
            );

            return await GetCartAsync(userId);
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);

            if (cart == null)
                return false;

            cart.Items.Clear();
            cart.TotalPrice = 0;
            cart.LastUpdatedAt = DateTime.UtcNow;

            await _cartRepository.UpdateAsync(cart);

            // Invalidate cache
            await _cache.RemoveAsync($"cart_{userId}");

            _logger.LogInformation("Cart cleared for user {UserId}", userId);

            return true;
        }

        public async Task<CartSummaryResponse> GetCartSummaryAsync(int userId)
        {
            var cart = await GetCartAsync(userId);

            return new CartSummaryResponse
            {
                TotalItems = cart.Items.Sum(i => i.Quantity),
                TotalPrice = cart.TotalPrice,
            };
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            var cart = await GetCartAsync(userId);
            return cart.Items.Sum(i => i.Quantity);
        }

        private CartResponse MapToCartResponse(CartEntity cart)
        {
            return new CartResponse
            {
                Id = cart.Id,
                UserId = cart.UserId,
                TotalPrice = cart.TotalPrice,
                TotalItems = cart.Items.Sum(i => i.Quantity),
                LastUpdatedAt = cart.LastUpdatedAt,
                Items = cart
                    .Items.Select(i => new CartItemResponse
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        Subtotal = i.Price * i.Quantity,
                        ImageUrl = i.ImageUrl,
                    })
                    .ToList(),
            };
        }
    }
}
