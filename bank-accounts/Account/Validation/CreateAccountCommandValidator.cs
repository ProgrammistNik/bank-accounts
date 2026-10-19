using bank_accounts.Account.Commands;
using bank_accounts.Account.Enums;
using FluentValidation;
using JetBrains.Annotations;

namespace bank_accounts.Account.Validation;

// Resharper жалуется на неиспользование, но валидатор обрабатывается через middleware.
[UsedImplicitly]
public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
        RuleLevelCascadeMode = CascadeMode.Stop;
        
        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .Must(id => id != Guid.Empty)
            .WithMessage("OwnerId не может быть пустым GUID.");

        RuleFor(x => x.AccountType)
            .IsInEnum();

        RuleFor(x => x.Currency)
            .IsInEnum();

        RuleFor(x => x.Balance)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.InterestRate)
            .NotNull()
            .When(x => x.AccountType != AccountType.Checking);

        RuleFor(x => x.InterestRate)
            .InclusiveBetween(0, 100)
            .When(x => x.InterestRate.HasValue);
    }
}
