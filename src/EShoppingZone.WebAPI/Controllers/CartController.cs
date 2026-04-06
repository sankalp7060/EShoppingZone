using System.Security.Claims;
using EShoppingZone.Business.DTOs;
using EShoppingZone.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found in token");
            return int.Parse(userIdClaim);
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.GetCartAsync(userId);
                return Ok(new { success = true, data = cart });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetCartSummary()
        {
            try
            {
                var userId = GetCurrentUserId();
                var summary = await _cartService.GetCartSummaryAsync(userId);
                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartItemCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _cartService.GetCartItemCountAsync(userId);
                return Ok(new { success = true, data = new { count } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.AddToCartAsync(userId, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = cart,
                        message = "Item added to cart successfully",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.UpdateCartItemAsync(userId, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = cart,
                        message = "Cart updated successfully",
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await _cartService.RemoveFromCartAsync(userId, cartItemId);
                return Ok(
                    new
                    {
                        success = true,
                        data = cart,
                        message = "Item removed from cart",
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _cartService.ClearCartAsync(userId);
                return Ok(new { success = true, message = "Cart cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
