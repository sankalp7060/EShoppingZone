using System.Security.Claims;
using EShoppingZone.Wallet.Application.DTOs;
using EShoppingZone.Wallet.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.Wallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IRazorpayService _razorpayService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletService walletService, IRazorpayService razorpayService, ILogger<WalletController> logger)
        {
            _walletService = walletService;
            _razorpayService = razorpayService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            // Prioritize 'UserId' claim as it's the projects dedicated integer ID
            var userIdStr = User.FindFirst("UserId")?.Value 
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
            {
                _logger.LogWarning("Identity failed: No 'UserId' or 'NameIdentifier' claim found in token.");
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            if (!int.TryParse(userIdStr, out int userId))
            {
                _logger.LogWarning("Identity failed: Claim value '{Value}' is not a valid integer.", userIdStr);
                throw new UnauthorizedAccessException("User ID claim is not a valid integer");
            }

            _logger.LogInformation("Identity resolved: User ID {UserId} found from token.", userId);
            return userId;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
        }

        /// <summary>
        /// Create a new wallet for current user
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet()
        {
            try
            {
                var userId = GetCurrentUserId();
                var wallet = await _walletService.CreateWalletAsync(userId);
                return Ok(
                    new
                    {
                        success = true,
                        data = wallet,
                        message = "Wallet created successfully",
                    }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet for user");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get current user's wallet
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            try
            {
                var userId = GetCurrentUserId();
                var wallet = await _walletService.GetWalletByUserIdAsync(userId);
                return Ok(new { success = true, data = wallet });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get wallet balance
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            try
            {
                var userId = GetCurrentUserId();
                var balance = await _walletService.GetBalanceAsync(userId);
                return Ok(new { success = true, data = balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting balance");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Add money to wallet
        /// </summary>
        [HttpPost("add-money")]
        public async Task<IActionResult> AddMoney([FromBody] AddMoneyRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _walletService.AddMoneyAsync(userId, request);
                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding money to wallet");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all wallet transactions
        /// </summary>
        [HttpGet("statements")]
        public async Task<IActionResult> GetStatements()
        {
            try
            {
                var userId = GetCurrentUserId();
                var statements = await _walletService.GetStatementsAsync(userId);
                return Ok(new { success = true, data = statements });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statements");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get transaction by ID
        /// </summary>
        [HttpGet("statements/{statementId}")]
        public async Task<IActionResult> GetStatementById(int statementId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var statement = await _walletService.GetStatementByIdAsync(userId, statementId);

                if (statement == null)
                    return NotFound(new { success = false, message = "Transaction not found" });

                return Ok(new { success = true, data = statement });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statement {StatementId}", statementId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin - Get wallet by user ID
        /// </summary>
        [HttpGet("admin/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetWalletByUserId(int userId)
        {
            try
            {
                var wallet = await _walletService.GetWalletByUserIdAsync(userId);
                return Ok(new { success = true, data = wallet });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Pay for an order using wallet (called by Order Service)
        /// </summary>
        [HttpPost("pay")]
        public async Task<IActionResult> PayForOrder([FromBody] PayMoneyRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _walletService.PayMoneyAsync(userId, request);
                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing wallet payment");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Credit a merchant for an order (called by Order Service)
        /// </summary>
        [HttpPost("credit/{merchantId}")]
        public async Task<IActionResult> CreditForOrder(int merchantId, [FromBody] PayMoneyRequest request)
        {
            try
            {
                var result = await _walletService.CreditMoneyAsync(merchantId, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing wallet credit for merchant {MerchantId}", merchantId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Refund for an order (called by Order Service)
        /// </summary>
        [HttpPost("refund")]
        public async Task<IActionResult> RefundForOrder([FromBody] PayMoneyRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _walletService.RefundMoneyAsync(userId, request);
                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing wallet refund");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _walletService.WithdrawMoneyAsync(userId, request.Amount);
                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing withdrawal");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create Razorpay Order
        /// </summary>
        [HttpPost("create-payment-order")]
        public async Task<IActionResult> CreatePaymentOrder([FromBody] RazorpayOrderRequest request)
        {
            try
            {
                var order = await _razorpayService.CreateOrderAsync(request);
                return Ok(new { success = true, data = order });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Razorpay order");
                return StatusCode(500, new { success = false, message = "Failed to create payment order" });
            }
        }

        /// <summary>
        /// Verify Razorpay Payment and Credit Wallet
        /// </summary>
        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPayment([FromBody] RazorpayVerifyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RazorpaySignature) || string.IsNullOrEmpty(request.RazorpayOrderId))
                {
                    return BadRequest(new { success = false, message = "Missing Razorpay verification data (OrderId or Signature)" });
                }

                var isValid = _razorpayService.VerifySignature(request);
                if (!isValid)
                {
                    return BadRequest(new { success = false, message = "Invalid payment signature match. Please check server logs." });
                }

                var userId = GetCurrentUserId();
                var addMoneyRequest = new AddMoneyRequest
                {
                    Amount = request.Amount,
                    Remarks = $"Razorpay Deposit (ID: {request.RazorpayPaymentId})"
                };

                var result = await _walletService.AddMoneyAsync(userId, addMoneyRequest);
                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Razorpay payment: {Message}", ex.Message);
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Payment verification failed", 
                    error = ex.Message,
                    details = ex.InnerException?.Message 
                });
            }
        }
    }
}
