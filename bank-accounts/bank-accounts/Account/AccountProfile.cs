using AutoMapper;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Enums;
using JetBrains.Annotations;

namespace bank_accounts.Account;

// Resharper жалуется на неиспользование, но профиль обрабатывается через рефлексию.
[UsedImplicitly]
public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Models.Account, ReturnAccountDto>()
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.GetDescription()));
        CreateMap<Models.Transaction, ReturnTransactionDto>()
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.GetDescription()));
    }
}
