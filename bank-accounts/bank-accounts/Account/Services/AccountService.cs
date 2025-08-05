using bank_accounts.Account.Enums;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Interfaces;
using Transaction = bank_accounts.Account.Models.Transaction;

namespace bank_accounts.Account.Services;

public class AccountService : IAccountService
{
    private readonly List<Models.Account> _accounts = [];

    public void AddAccountToList(Models.Account account)
    {
        _accounts.Add(account);
    }

    public Models.Account CloseAccount(Guid accountId)
    {
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }

        if (account.IsClosed)
        {
            return account;
        }
        
        account.ClosedAt = DateTime.Now;
        return account;
    }

    public IList<Models.Account> GetUserAccounts(Guid ownerId)
    {
        var accounts = _accounts.Where(a => a.OwnerId == ownerId).ToList();
        
        return accounts;
    }

    public Models.Account ChangeInterestRate(Guid accountId, decimal interestRate)
    {
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }

        if (account.IsClosed)
        {
            throw new CustomExceptions.AccountClosedException(accountId);
        }

        if (account.AccountType == AccountType.Checking)
        {
            throw new CustomExceptions.CheckingAccountNotSupportInterestRateException();
        }
        account.InterestRate = interestRate;
        return account;
    }

    public Models.Account GetAccountById(Guid accountId)
    {
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }
        
        return account;
    }


    public Transaction RegisterAccountTransaction(Guid accountId, Guid counterpartyId, decimal amount, Currency currency,
        string description = "")
    {
        if(accountId == counterpartyId)
        {
            throw new CustomExceptions.InvalidTransferException();
        }
        
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }
        if (account.IsClosed)
        {
            throw new CustomExceptions.AccountClosedException(accountId);
        }
        
        var counterpartyAccount = _accounts.FirstOrDefault(a => a.Id == counterpartyId);
        if (counterpartyAccount == null)
        {
            throw new CustomExceptions.AccountNotFoundException(counterpartyId);
        }
        if (counterpartyAccount.IsClosed)
        {
            throw new CustomExceptions.AccountClosedException(counterpartyId);
        }

        if (account.Balance < amount)
        {
            throw new CustomExceptions.InsufficientBalanceException(accountId);
        }

        if (account.Currency != counterpartyAccount.Currency)
        {
            throw new CustomExceptions.CurriesDontMatchException();
        }
        
        account.Balance -= amount;
        counterpartyAccount.Balance += amount;
        
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CounterpartyId = counterpartyId,
            Amount = amount,
            Currency = currency,
            TransactionType = TransactionType.Debit,
            Description = description,
            CommitedAt = DateTime.UtcNow
        };
        
        var counterpartyTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = counterpartyId,
            CounterpartyId = accountId,
            Amount = amount,
            Currency = currency,
            TransactionType = TransactionType.Credit,
            Description = description,
            CommitedAt = DateTime.UtcNow
        };
        
        account.Transactions.Add(transaction);
        counterpartyAccount.Transactions.Add(counterpartyTransaction);
        
        return transaction;
        
    }

    public List<Transaction> GetAccountTransactions(Guid accountId, DateTime? startDate, DateTime? endDate)
    {
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }

        var transactions = account.Transactions.AsQueryable();

        if (startDate.HasValue)
        {
            transactions = transactions.Where(t => t.CommitedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            transactions = transactions.Where(t => t.CommitedAt <= endDate.Value);
        }

        return transactions.ToList();
    }
}
