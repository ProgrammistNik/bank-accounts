using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class RegisterAccountTransactionCommandHandler(IAccountService accountService)
    : IRequestHandler<RegisterAccountTransactionCommand, ReturnTransactionDto>
{
    public async Task<ReturnTransactionDto> Handle(RegisterAccountTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await accountService.RegisterAccountTransaction(request.AccountId,
            request.CounterpartyId,
            request.Amount,
            request.Currency,
            request.Description);
        
        return transaction.ToDto();
    }
}
