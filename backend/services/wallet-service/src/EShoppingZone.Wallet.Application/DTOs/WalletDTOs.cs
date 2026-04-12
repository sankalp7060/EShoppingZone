using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EShoppingZone.Wallet.Application.DTOs
{
    public class CreateWalletRequest
    {
        [Required]
        public int UserId { get; set; }
    }

    public class AddMoneyRequest
    {
        [Required]
        [Range(1, 1000000)]
        public decimal Amount { get; set; }

        public string? Remarks { get; set; }
    }

    public class WithdrawRequest
    {
        [Required]
        [Range(1, 1000000)]
        public decimal Amount { get; set; }

        public string? Remarks { get; set; }
    }

    public class PayMoneyRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Amount { get; set; }

        public string? Remarks { get; set; }
    }

    public class WalletResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime? LastTransactionAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StatementResponse
    {
        public int Id { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public int? OrderId { get; set; }
        public string TransactionRemarks { get; set; } = string.Empty;
        public decimal BalanceAfterTransaction { get; set; }
    }

    public class WalletBalanceResponse
    {
        public decimal CurrentBalance { get; set; }
        public int UserId { get; set; }
        public DateTime? LastTransactionAt { get; set; }
    }

    public class TransactionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal NewBalance { get; set; }
        public int? TransactionId { get; set; }
    }

    public class RazorpayOrderRequest
    {
        public decimal Amount { get; set; }
        public string Receipt { get; set; } = string.Empty;
    }

    public class RazorpayOrderResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Key { get; set; } = string.Empty;
    }

    public class RazorpayVerifyRequest
    {
        [JsonPropertyName("razorpayPaymentId")]
        public string RazorpayPaymentId { get; set; } = string.Empty;

        [JsonPropertyName("razorpayOrderId")]
        public string RazorpayOrderId { get; set; } = string.Empty;

        [JsonPropertyName("razorpaySignature")]
        public string RazorpaySignature { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}
