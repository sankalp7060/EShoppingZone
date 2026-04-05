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
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletService walletService, ILogger<WalletController> logger)
        {
            _walletService = walletService;
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
    }
}
