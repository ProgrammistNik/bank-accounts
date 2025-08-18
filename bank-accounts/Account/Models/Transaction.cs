using bank_accounts.Account.Enums;
using JetBrains.Annotations;

namespace bank_accounts.Account.Models;

// Resharper жалуется на неиспользование значение, но они используются через рефлексию.
public class Transaction
{
    public Guid Id { [UsedImplicitly] get; init; }
    public Guid AccountId { [UsedImplicitly] get; init; }
    public Guid CounterpartyId { [UsedImplicitly] get; init; } // вторая сторона транзакции
    public decimal Amount { [UsedImplicitly] get; init; }
    public Currency Currency { get; init; }
    public TransactionType TransactionType { [UsedImplicitly] get; init; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Description { [UsedImplicitly] get; init; } = string.Empty;
    public DateTime CommitedAt { get; init; }
}