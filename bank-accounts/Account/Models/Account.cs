using System.ComponentModel.DataAnnotations.Schema;
using bank_accounts.Account.Enums;
using JetBrains.Annotations;

namespace bank_accounts.Account.Models;

// Resharper жалуется на неиспользование некоторых типов, но они используются через рефлексию.
public class Account
{
    private decimal? _interestRate;
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public AccountType AccountType { get; init; }
    public Currency Currency { get; init; }
    [Column(TypeName = "decimal(38, 10)")]
    public decimal Balance { get; set; }
    public decimal? InterestRate
    {
        [UsedImplicitly] get => _interestRate;
        set => _interestRate = AccountType == AccountType.Checking ? null : value;
    } // процентная ставка
    public DateTime CreatedAt { [UsedImplicitly] get; init; }
    public DateTime? ClosedAt { get; set; }
    public bool IsClosed => ClosedAt.HasValue;
    public bool IsFrozen { get; init; }
    public ICollection<Transaction> Transactions { get; } = new List<Transaction>();
    
    public uint Xmin { get; init; }
}