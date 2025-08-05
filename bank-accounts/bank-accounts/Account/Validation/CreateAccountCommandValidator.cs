using bank_accounts.Account.Commands;
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
    }
}
