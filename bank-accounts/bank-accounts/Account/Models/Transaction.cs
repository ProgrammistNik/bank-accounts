using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid CounterpartyId { get; set; } // вторая сторона транзакции
    public decimal Amount { get; set; }
    public Currency Currency { get; init; }
    public TransactionType TransactionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CommitedAt { get; init; }
}