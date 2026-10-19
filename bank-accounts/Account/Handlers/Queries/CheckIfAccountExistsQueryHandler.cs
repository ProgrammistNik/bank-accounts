using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using bank_accounts.Account.Queries;
using MediatR;

namespace bank_accounts.Account.Handlers.Queries;

public class CheckIfAccountExistsQueryHandler(IAccountService accountService)
    : IRequestHandler<CheckIfAccountsExistsQuery, ReturnAccountDto>
{
    public async Task<ReturnAccountDto> Handle(CheckIfAccountsExistsQuery request, CancellationToken cancellationToken)
    {
        var account = await accountService.GetAccountById(request.AccountId);
        return account.ToDto();
    }
}
