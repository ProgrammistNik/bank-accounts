using bank_accounts.Account.Enums;
using bank_accounts.Account.Interfaces;
using static System.Enum;

namespace bank_accounts.Account.Services;

public class CurrencyService : ICurrencyService
{
    public bool IsCurrencySupported(Currency currency)
    {
        return IsDefined(currency);
    }
}
