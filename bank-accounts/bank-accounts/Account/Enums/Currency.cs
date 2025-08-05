using System.ComponentModel;
using System.Reflection;

namespace bank_accounts.Account.Enums;

public enum Currency
{
    [Description("USD")] Usd = 840,
    [Description("EUR")] Eur = 978,
    [Description("GBP")] Gbp = 826,
    [Description("RUB")] Rub = 643
}
    
public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        return value.GetType()
                   .GetMember(value.ToString())
                   .FirstOrDefault()
                   ?.GetCustomAttribute<DescriptionAttribute>()
                   ?.Description
               ?? value.ToString();
    }
}