using bank_accounts.Account.Commands;
using FluentValidation;
using JetBrains.Annotations;

namespace bank_accounts.Account.Validation;

// Resharper жалуется на неиспользование, но валидатор обрабатывается через middleware.
[UsedImplicitly]
public class CloseAccountCommandValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
        RuleLevelCascadeMode = CascadeMode.Stop;
        
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .Must(id => id != Guid.Empty)
            .WithMessage("AccountId не может быть пустым GUID.");
    }
}
