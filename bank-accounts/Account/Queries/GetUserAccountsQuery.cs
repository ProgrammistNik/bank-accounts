using bank_accounts.Account.Dto;
using MediatR;

namespace bank_accounts.Account.Queries;

public record GetUserAccountsQuery : IRequest<IList<ReturnAccountDto>>
{
    public Guid OwnerId { get; init; }
}
