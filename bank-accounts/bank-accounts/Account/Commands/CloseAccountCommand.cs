using bank_accounts.Account.Dto;
using MediatR;

namespace bank_accounts.Account.Commands;

public record CloseAccountCommand : IRequest<ReturnAccountDto>
{
    public Guid AccountId { get; init; }
}
