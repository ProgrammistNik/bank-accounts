using bank_accounts.Account.Dto;
using bank_accounts.Account.Interfaces;
using bank_accounts.Account.Queries;
using MediatR;

namespace bank_accounts.Account.Handlers.Queries;

public class GetAccountStatementQueryHandler(IAccountService accountService)
    : IRequestHandler<GetAccountStatementQuery, IList<ReturnTransactionDto>>
{
    public async Task<IList<ReturnTransactionDto>> Handle(GetAccountStatementQuery request, CancellationToken cancellationToken)
    {
        var transactions = await accountService.GetAccountTransactions(request.AccountId, request.From, request.To);
        
        return transactions.Select(transaction => transaction.ToDto()).ToList();
    }
}
