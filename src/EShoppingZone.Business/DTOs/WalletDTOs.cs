using System.ComponentModel.DataAnnotations;

namespace EShoppingZone.Business.DTOs
{
    public class CreateWalletRequest
    {
        [Required]
        public int UserId { get; set; }
    }

    public class AddMoneyRequest
    {
        [Required]
        [Range(1, 100000)]
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
    }

    public class TransactionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal NewBalance { get; set; }
        public int? TransactionId { get; set; }
    }
}
