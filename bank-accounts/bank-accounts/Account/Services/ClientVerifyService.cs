using bank_accounts.Account.Interfaces;

namespace bank_accounts.Account.Services;

public class ClientVerifyService : IClientVerifyService
{
    public bool VerifyClient(Guid ownerId)
    {
        return ownerId != Guid.Empty;
    }
}
