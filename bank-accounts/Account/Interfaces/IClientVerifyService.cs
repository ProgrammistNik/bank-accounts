namespace bank_accounts.Account.Interfaces;

public interface IClientVerifyService
{
    bool VerifyClient(Guid ownerId);
}
