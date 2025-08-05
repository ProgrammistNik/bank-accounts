using bank_accounts.Account.Enums;
using JetBrains.Annotations;

namespace bank_accounts.Account.Dto;

// Resharper жалуется на неиспользование некоторых типов, но они используются через рефлексию.
public class CreateAccountDto
{
    public Guid OwnerId { get; [UsedImplicitly] set; }
    public AccountType AccountType { get; [UsedImplicitly] set; }
    public Currency Currency { get; [UsedImplicitly] set; }
    public decimal Balance { get; [UsedImplicitly] set; }
    public decimal? InterestRate { get; [UsedImplicitly] set; }
}
