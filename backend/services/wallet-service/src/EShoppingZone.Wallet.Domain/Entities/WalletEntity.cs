using EShoppingZone.Common.BaseEntities;

namespace EShoppingZone.Wallet.Domain.Entities
{
    public class WalletEntity : BaseEntity
    {
        public int UserId { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime? LastTransactionAt { get; set; }
        public ICollection<StatementEntity> Statements { get; set; } = new List<StatementEntity>();
    }

    public class StatementEntity : BaseEntity
    {
        public int WalletId { get; set; }
        public WalletEntity Wallet { get; set; } = null!;
        public string TransactionType { get; set; } = string.Empty; // CREDIT, DEBIT
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public int? OrderId { get; set; }
        public string TransactionRemarks { get; set; } = string.Empty;
        public decimal BalanceAfterTransaction { get; set; }
    }
}
