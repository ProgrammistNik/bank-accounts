using bank_accounts.Account.Queries;
using FluentValidation;

namespace bank_accounts.Account.Validation;

public class CheckIfAccountExistsQueryValidator : AbstractValidator<CheckIfAccountsExistsQuery>
{
    public CheckIfAccountExistsQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
    }
}
