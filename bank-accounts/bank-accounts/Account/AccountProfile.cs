using AutoMapper;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Enums;

namespace bank_accounts.Account;

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
