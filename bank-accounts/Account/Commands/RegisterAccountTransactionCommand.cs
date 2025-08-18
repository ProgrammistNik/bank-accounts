using bank_accounts.Account.Dto;
using bank_accounts.Account.Enums;
using JetBrains.Annotations;
using MediatR;

namespace bank_accounts.Account.Commands;

public class RegisterAccountTransactionCommand : IRequest<ReturnTransactionDto>
{
    public Guid AccountId { get; init; }
    public Guid CounterpartyId { get; init; }
    public decimal Amount { get; init; }
    public Currency Currency { get; init; }
    // Resharper жалуется на неиспользование TransactionType, но он нужен.
    [UsedImplicitly] 
    public TransactionType TransactionType { get; set; }
    public string Description { get; init; } = string.Empty;
}
