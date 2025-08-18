using bank_accounts.Account.Dto;
using MediatR;

namespace bank_accounts.Account.Queries;

public class GetAccountStatementQuery : IRequest<IList<ReturnTransactionDto>>
{
    public Guid AccountId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
