using bank_accounts.Account.Enums;

namespace bank_accounts.Account.Interfaces;

public interface IAccountService
{
   void AddAccountToList(Models.Account account); 
   Models.Account CloseAccount(Guid accountId);
   IList<Models.Account> GetUserAccounts(Guid ownerId);
   Models.Account ChangeInterestRate(Guid accountId, decimal interestRate);
   Models.Account GetAccountById(Guid accountId);
   Models.Transaction RegisterAccountTransaction(
       Guid accountId, 
       Guid counterpartyId, 
       decimal amount, 
       Currency currency,  
       string description = "");
   List<Models.Transaction> GetAccountTransactions(Guid accountId, DateTime? startDate, DateTime? endDate);
}
    