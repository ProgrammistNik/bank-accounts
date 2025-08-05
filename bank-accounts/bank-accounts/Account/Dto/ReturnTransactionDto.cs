using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Dto;

public class ReturnTransactionDto
{
    public Guid AccountId { get; init; }
    public Guid CounterpartyId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public TransactionType TransactionType { get; init; }
    public string Description { get; init; } = string.Empty;
}
