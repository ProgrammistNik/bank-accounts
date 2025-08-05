using bank_accounts.Account.Queries;
using FluentValidation;
using JetBrains.Annotations;

namespace bank_accounts.Account.Validation;

// Resharper жалуется на неиспользование, но валидатор обрабатывается через middleware.
[UsedImplicitly]
public class CheckIfAccountExistsQueryValidator : AbstractValidator<CheckIfAccountsExistsQuery>
{
    public CheckIfAccountExistsQueryValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
        RuleLevelCascadeMode = CascadeMode.Stop;
        
        RuleFor(x => x.AccountId).NotEmpty();
    }
}
