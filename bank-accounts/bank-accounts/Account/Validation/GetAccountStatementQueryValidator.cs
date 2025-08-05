using bank_accounts.Account.Queries;
using FluentValidation;

namespace bank_accounts.Account.Validation;

public class GetAccountStatementQueryValidator : AbstractValidator<GetAccountStatementQuery>
{
    public GetAccountStatementQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
    }
}
