using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Dto;

public class ReturnAccountDto
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public AccountType AccountType { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal? InterestRate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public bool IsClosed => ClosedAt.HasValue;
}
