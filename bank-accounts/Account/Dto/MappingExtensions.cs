using bank_accounts.Account.Enums;
using Transaction = bank_accounts.Account.Models.Transaction;

namespace bank_accounts.Account.Dto;

public static class MappingExtensions
{
    public static ReturnAccountDto ToDto(this Models.Account account) => new()
    {
        Id = account.Id,
        OwnerId = account.OwnerId,
        AccountType = account.AccountType,
        Currency = account.Currency.GetDescription(),
        Balance = account.Balance,
        InterestRate = account.InterestRate,
        CreatedAt = account.CreatedAt,
        ClosedAt = account.ClosedAt
    };

    public static ReturnTransactionDto ToDto(this Transaction transaction) => new()
    {
        AccountId = transaction.AccountId,
        CounterpartyId = transaction.CounterpartyId,
        Amount = transaction.Amount,
        Currency = transaction.Currency.GetDescription(),
        TransactionType = transaction.TransactionType,
        Description = transaction.Description
    };
}
