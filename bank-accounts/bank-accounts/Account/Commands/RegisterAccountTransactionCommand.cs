using bank_accounts.Account.Dto;
using bank_accounts.Account.Enums;
using MediatR;

namespace bank_accounts.Account.Commands;

public class RegisterAccountTransactionCommand : IRequest<ReturnTransactionDto>
{
    public Guid AccountId { get; init; }
    public Guid CounterpartyId { get; init; }
    public decimal Amount { get; init; }
    public Currency Currency { get; init; }
    public TransactionType TransactionType { get; set; }
    public string Description { get; init; } = string.Empty;
}
