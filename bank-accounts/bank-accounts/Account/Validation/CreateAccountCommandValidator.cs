using bank_accounts.Account.Commands;
using FluentValidation;

namespace bank_accounts.Account.Validation;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .Must(id => id != Guid.Empty)
            .WithMessage("OwnerId не может быть пустым GUID.");
    }
}
