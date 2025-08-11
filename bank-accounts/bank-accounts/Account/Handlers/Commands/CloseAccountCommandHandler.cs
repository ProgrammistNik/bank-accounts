using AutoMapper;
using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class CloseAccountCommandHandler(IAccountService accountService, IMapper mapper)
    : IRequestHandler<CloseAccountCommand, ReturnAccountDto>
{
    public Task<ReturnAccountDto> Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account =  accountService.CloseAccount(request.AccountId);
        return Task.FromResult(mapper.Map<ReturnAccountDto>(account));
    }
}
