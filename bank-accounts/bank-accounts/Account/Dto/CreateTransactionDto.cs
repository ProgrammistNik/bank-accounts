using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Dto;

public class CreateTransactionDto
{
    public Guid AccountId { get; set; }
    public Guid CounterpartyId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public string Description { get; set; } = string.Empty;
}
