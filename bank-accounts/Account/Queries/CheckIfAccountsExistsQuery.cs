using bank_accounts.Account.Dto;
using MediatR;

namespace bank_accounts.Account.Queries;

public class CheckIfAccountsExistsQuery : IRequest<ReturnAccountDto>
{
    public Guid AccountId { get; init; }
}
