using System.ComponentModel;
using System.Reflection;
using JetBrains.Annotations;

namespace bank_accounts.Account.Enums;

public enum Currency
{
    // Resharper жалуется на неиспользование Currency типов, но они используются через рефлексию.
    [Description("USD")] [UsedImplicitly] Usd = 840,
    [Description("EUR")] [UsedImplicitly] Eur = 978,
    [Description("GBP")] [UsedImplicitly] Gbp = 826,
    [Description("RUB")] [UsedImplicitly] Rub = 643
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