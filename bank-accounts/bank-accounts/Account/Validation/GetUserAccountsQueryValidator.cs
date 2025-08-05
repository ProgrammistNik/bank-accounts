using bank_accounts.Account.Queries;
using FluentValidation;

namespace bank_accounts.Account.Validation;

public class GetUserAccountsQueryValidator  : AbstractValidator<GetUserAccountsQuery>
{
    public GetUserAccountsQueryValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .Must(id => id != Guid.Empty)
            .WithMessage("OwnerId не может быть пустым GUID.");
    }
}
