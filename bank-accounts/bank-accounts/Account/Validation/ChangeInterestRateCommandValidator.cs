using bank_accounts.Account.Commands;
using FluentValidation;

namespace bank_accounts.Account.Validation;

public class ChangeInterestRateCommandValidator : AbstractValidator<ChangeInterestRateCommand>
{
    public ChangeInterestRateCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
    }
}
