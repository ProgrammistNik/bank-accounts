using AutoMapper;
using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class RegisterAccountTransactionCommandHandler(IAccountService accountService, IMapper mapper)
    : IRequestHandler<RegisterAccountTransactionCommand, ReturnTransactionDto>
{
    public Task<ReturnTransactionDto> Handle(RegisterAccountTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = accountService.RegisterAccountTransaction(request.AccountId,
            request.CounterpartyId,
            request.Amount,
            request.Currency,
            request.Description);
        
        return Task.FromResult(mapper.Map<ReturnTransactionDto>(transaction));
    }
}
