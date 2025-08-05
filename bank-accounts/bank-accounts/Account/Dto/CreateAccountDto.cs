using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Dto;

public class CreateAccountDto
{
    public Guid OwnerId { get; set; }
    public AccountType AccountType { get; set; }
    public Currency Currency { get; set; }
    public decimal Balance { get; set; }
    public decimal? InterestRate { get; set; }
}
