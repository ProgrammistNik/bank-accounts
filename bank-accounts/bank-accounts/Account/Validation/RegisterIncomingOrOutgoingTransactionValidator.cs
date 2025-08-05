using bank_accounts.Account.Commands;
using FluentValidation;
using JetBrains.Annotations;

namespace bank_accounts.Account.Validation;

// Resharper жалуется на неиспользование, но валидатор обрабатывается через middleware.
[UsedImplicitly]
public class RegisterIncomingOrOutgoingTransactionValidator : AbstractValidator<RegisterIncomingOrOutgoingTransactionsCommand>
{
   public RegisterIncomingOrOutgoingTransactionValidator()
   {
      ClassLevelCascadeMode = CascadeMode.Continue;
      RuleLevelCascadeMode = CascadeMode.Stop;
      
      RuleFor(x => x.AccountId)
         .NotEmpty()
         .Must(id => id != Guid.Empty)
         .WithMessage("AccountId не может быть пустым GUID.");
      RuleFor(x => x.Amount)
         .NotEmpty()
         .GreaterThan(0)
         .WithMessage("Amount должен быть больше нуля.");
      RuleFor(x => x.Currency)
         .NotEmpty();
   }
}
