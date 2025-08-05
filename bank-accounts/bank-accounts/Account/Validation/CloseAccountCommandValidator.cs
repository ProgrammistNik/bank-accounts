using bank_accounts.Account.Commands;
using FluentValidation;

namespace bank_accounts.Account.Validation;

public class CloseAccountCommandValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .Must(id => id != Guid.Empty)
            .WithMessage("AccountId не может быть пустым GUID.");
    }
}
