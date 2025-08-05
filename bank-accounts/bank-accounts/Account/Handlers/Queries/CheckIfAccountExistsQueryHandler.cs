using AutoMapper;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using bank_accounts.Account.Queries;
using MediatR;

namespace bank_accounts.Account.Handlers.Queries;

public class CheckIfAccountExistsQueryHandler(IAccountService accountService, IMapper mapper)
    : IRequestHandler<CheckIfAccountsExistsQuery, ReturnAccountDto>
{
    public Task<ReturnAccountDto> Handle(CheckIfAccountsExistsQuery request, CancellationToken cancellationToken)
    {
        var account = accountService.GetAccountById(request.AccountId);
        return Task.FromResult(mapper.Map<ReturnAccountDto>(account));
    }
}
