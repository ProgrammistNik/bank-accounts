using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Interfaces;

public interface IAccountService
{
   Task AddAccountToList(Models.Account account); 
   Task<Models.Account> CloseAccount(Guid accountId);
   Task<IList<Models.Account>> GetUserAccounts(Guid ownerId);
   Task<Models.Account> ChangeInterestRate(Guid accountId, decimal interestRate);
   Task<Models.Account> GetAccountById(Guid accountId);
   Task<Models.Transaction> RegisterAccountTransaction(
       Guid accountId, 
       Guid counterpartyId, 
       decimal amount, 
       Currency currency,
       string description = "");

   Task<Models.Transaction> RegisterIncomingOrOutgoingTransactionsCommand(
       Guid accountId,
       decimal amount,
       Currency currency,
       TransactionType transactionType,
       string description = "");
   Task<List<Models.Transaction>> GetAccountTransactions(Guid accountId, DateTime? startDate, DateTime? endDate);
   // ReSharper disable once UnusedMemberInSuper.Global
   Task AccrueInterest(Guid accountId);
   // ReSharper disable once UnusedMemberInSuper.Global
   public Task AccrueInterestForAllAccounts();
}
    