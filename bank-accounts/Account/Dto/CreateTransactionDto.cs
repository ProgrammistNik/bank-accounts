using bank_accounts.Account.Enums;
using JetBrains.Annotations;

namespace bank_accounts.Account.Dto;

// Resharper жалуется на неиспользование некоторых типов, но они используются через рефлексию.
public class CreateTransactionDto
{
    public Guid AccountId { get; [UsedImplicitly] set; }
    public Guid CounterpartyId { get; [UsedImplicitly] set; }
    public decimal Amount { get; [UsedImplicitly] set; }
    public Currency Currency { get; [UsedImplicitly] set; }
    public string Description { get; [UsedImplicitly] set; } = string.Empty;
}
