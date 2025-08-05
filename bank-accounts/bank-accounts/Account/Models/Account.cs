using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Models;

public class Account
{
    private decimal? _interestRate;
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public AccountType AccountType { get; init; }
    public Currency Currency { get; init; }
    public decimal Balance { get; set; }
    public decimal? InterestRate
    {
        get => _interestRate;
        set => _interestRate = AccountType == AccountType.Checking ? null : value;
    } // процентная ставка
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsClosed => ClosedAt.HasValue;
    public ICollection<Transaction> Transactions { get; } = new List<Transaction>();
}