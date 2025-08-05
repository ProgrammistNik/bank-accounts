using bank_accounts.Account.Dto;
using MediatR;

namespace bank_accounts.Account.Commands;

public class ChangeInterestRateCommand : IRequest<ReturnAccountDto>
{
    public Guid AccountId { get; init; }
    public decimal InterestRate { get; init; }
}
