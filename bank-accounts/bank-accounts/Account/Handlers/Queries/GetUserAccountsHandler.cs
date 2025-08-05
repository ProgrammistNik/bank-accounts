using AutoMapper;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Interfaces;
using bank_accounts.Account.Queries;
using MediatR;

namespace bank_accounts.Account.Handlers.Queries;

public class GetUserAccountsHandler(IAccountService accountService, IClientVerifyService verifyService, IMapper mapper)
    : IRequestHandler<GetUserAccountsQuery, IList<ReturnAccountDto>>
{
    public Task<IList<ReturnAccountDto>> Handle(GetUserAccountsQuery request, CancellationToken cancellationToken)
    {
        if (!verifyService.VerifyClient(request.OwnerId))
        {
            throw new CustomExceptions.OwnerNotFoundException(request.OwnerId);
        }
        
        var accounts = accountService.GetUserAccounts(request.OwnerId);
        
        return Task.FromResult(mapper.Map<IList<ReturnAccountDto>>(accounts));
    }
}
