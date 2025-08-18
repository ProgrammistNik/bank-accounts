using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Interfaces;

public interface ICurrencyService
{
    bool IsCurrencySupported(Currency currency);
}
