using AutoMapper;
using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class CloseAccountCommandHandler(IAccountService accountService, IMapper mapper)
    : IRequestHandler<CloseAccountCommand, ReturnAccountDto>
{
    public async Task<ReturnAccountDto> Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountService.CloseAccount(request.AccountId);
        return mapper.Map<ReturnAccountDto>(account);
    }
}
