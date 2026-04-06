using System.Security.Claims;
using EShoppingZone.Business.DTOs;
using EShoppingZone.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
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

        private string GetCurrentUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";

        [HttpPost("place")]
        [Authorize]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var order = await _orderService.PlaceOrderAsync(userId, request);
                return Ok(
                    new
                    {
                        success = true,
                        data = order,
                        message = "Order placed successfully",
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order for user");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("pay")]
        [Authorize]
        public async Task<IActionResult> PlaceOrderWithWallet(
            [FromBody] WalletPaymentRequest request
        )
        {
            try
            {
                var userId = GetCurrentUserId();
                if (request.ModeOfPayment != "EWALLET")
                    return BadRequest(
                        new
                        {
                            success = false,
                            message = "Invalid payment mode. Use 'EWALLET' for wallet payment.",
                        }
                    );

                var result = await _orderService.PlaceOrderWithWalletAsync(userId, request);
                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing wallet order");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = GetCurrentUserId();
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("filter")]
        [Authorize]
        public async Task<IActionResult> GetFilteredOrders([FromQuery] OrderFilterRequest filter)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var result = await _orderService.GetFilteredOrdersAsync(userId, filter, userRole);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered orders");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var order = await _orderService.GetOrderByIdAsync(id, userId);
                if (order == null)
                    return NotFound(new { success = false, message = "Order not found" });
                return Ok(new { success = true, data = order });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("{id}/track")]
        [Authorize]
        public async Task<IActionResult> TrackOrder(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var tracking = await _orderService.GetOrderTrackingAsync(id, userId, userRole);
                return Ok(new { success = true, data = tracking });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order {OrderId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            [FromBody] UpdateOrderStatusWithRemarksRequest request
        )
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var order = await _orderService.UpdateOrderStatusAsync(
                    id,
                    request.OrderStatus,
                    userRole,
                    request.Remarks
                );
                return Ok(
                    new
                    {
                        success = true,
                        data = order,
                        message = "Order status updated",
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest? request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.CancelOrderAsync(id, userId, request?.Reason);
                return Ok(new { success = true, message = "Order cancelled successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterRequest filter)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var orders = await _orderService.GetFilteredOrdersAsync(userId, filter, userRole);
                return Ok(new { success = true, data = orders });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all orders");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }
}
