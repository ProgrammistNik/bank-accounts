using AutoMapper;
using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Enums;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class CreateAccountCommandHandler(
    IAccountService accountService,
    IClientVerifyService verifyService,
    ICurrencyService currencyService,
    IMapper mapper)
    : IRequestHandler<CreateAccountCommand, ReturnAccountDto>
{
    public Task<ReturnAccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var isInterestAccount = request.AccountType is AccountType.Credit or AccountType.Deposit;
        
        if(!verifyService.VerifyClient(request.OwnerId))
        {
            throw new CustomExceptions.OwnerNotFoundException(request.OwnerId);

        }

        if (!currencyService.IsCurrencySupported(request.Currency))
        {
            throw new CustomExceptions.CurrencyDoesNotSupportedException(request.Currency);
        }
        
        var account = new Models.Account
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            AccountType = request.AccountType,
            Currency = request.Currency,
            Balance = request.Balance,
            InterestRate = isInterestAccount ? request.InterestRate : null,
            CreatedAt = DateTime.Now
        };
        
        accountService.AddAccountToList(account);

        return Task.FromResult(mapper.Map<ReturnAccountDto>(account));
    }
}
