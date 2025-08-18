using AutoMapper;
using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class ChangeInterestRateCommandHandler(IAccountService accountService, IMapper mapper)
    : IRequestHandler<ChangeInterestRateCommand, ReturnAccountDto>
{
    public async Task<ReturnAccountDto> Handle(ChangeInterestRateCommand request, CancellationToken cancellationToken)
    {
        var account = await accountService.ChangeInterestRate(request.AccountId, request.InterestRate);
        return mapper.Map<ReturnAccountDto>(account);
    }
}
