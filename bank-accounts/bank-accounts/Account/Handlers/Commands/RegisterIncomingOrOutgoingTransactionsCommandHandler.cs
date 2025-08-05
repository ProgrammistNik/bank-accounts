using AutoMapper;
using bank_accounts.Account.Commands;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using MediatR;

namespace bank_accounts.Account.Handlers.Commands;

public class RegisterIncomingOrOutgoingTransactionsCommandHandler(IAccountService accountService, IMapper mapper)
: IRequestHandler<RegisterIncomingOrOutgoingTransactionsCommand, ReturnTransactionDto>
{
    public Task<ReturnTransactionDto> Handle(RegisterIncomingOrOutgoingTransactionsCommand request, CancellationToken cancellationToken)
    {
        var transaction = accountService.RegisterIncomingOrOutgoingTransactionsCommand(
            request.AccountId,
            request.Amount,
            request.Currency,
            request.TransactionType,
            request.Description);
        return Task.FromResult(mapper.Map<ReturnTransactionDto>(transaction));
    }
}
