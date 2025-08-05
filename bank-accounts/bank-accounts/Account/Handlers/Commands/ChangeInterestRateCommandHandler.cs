using AutoMapper;
using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class ChangeInterestRateCommandHandler(IAccountService accountService, IMapper mapper)
    : IRequestHandler<ChangeInterestRateCommand, ReturnAccountDto>
{
    public Task<ReturnAccountDto> Handle(ChangeInterestRateCommand request, CancellationToken cancellationToken)
    {
        var account = accountService.ChangeInterestRate(request.AccountId, request.InterestRate);
        return Task.FromResult(mapper.Map<ReturnAccountDto>(account));
    }
}
