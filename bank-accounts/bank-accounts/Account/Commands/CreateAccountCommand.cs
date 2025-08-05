using bank_accounts.Account.Dto;
using bank_accounts.Account.Enums;
using MediatR;

namespace bank_accounts.Account.Commands;

public record CreateAccountCommand : IRequest<ReturnAccountDto>
{
    public Guid OwnerId { get; init; }
    public AccountType AccountType { get; init; }
    public Currency Currency { get; init; }
    public decimal Balance { get; init; }
    public decimal? InterestRate { get; init; }
}
