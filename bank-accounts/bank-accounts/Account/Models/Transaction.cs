using bank_accounts.Account.Enums;
using JetBrains.Annotations;

namespace bank_accounts.Account.Models;

// Resharper жалуется на неиспользование значение, но они используются через рефлексию.
public class Transaction
{
    public Guid Id { [UsedImplicitly] get; set; }
    public Guid AccountId { [UsedImplicitly] get; set; }
    public Guid CounterpartyId { [UsedImplicitly] get; set; } // вторая сторона транзакции
    public decimal Amount { [UsedImplicitly] get; set; }
    public Currency Currency { get; init; }
    public TransactionType TransactionType { [UsedImplicitly] get; set; }
    public string Description { [UsedImplicitly] get; set; } = string.Empty;
    public DateTime CommitedAt { get; init; }
}