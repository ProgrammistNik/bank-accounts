using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class ChangeInterestRateCommandHandler(IAccountService accountService)
    : IRequestHandler<ChangeInterestRateCommand, ReturnAccountDto>
{
    public async Task<ReturnAccountDto> Handle(ChangeInterestRateCommand request, CancellationToken cancellationToken)
    {
        var account = await accountService.ChangeInterestRate(request.AccountId, request.InterestRate);
        return account.ToDto();
    }
}
