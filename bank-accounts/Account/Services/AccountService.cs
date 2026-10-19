using System.Data;
using bank_accounts.Account.Data;
using bank_accounts.Account.Enums;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Interfaces;
using bank_accounts.RabbitMQ.Models;
using Microsoft.EntityFrameworkCore;
using Transaction = bank_accounts.Account.Models.Transaction;

namespace bank_accounts.Account.Services;

public class AccountService(BankAccountsDbContext context) : IAccountService
{
    private const string InterestAccrualDescription = "Daily interest accrual";

    public async Task AddAccountToList(Models.Account account)
    {
        await context.Accounts.AddAsync(account);
        
        var @event = new
        {
            eventId = Guid.NewGuid(),
            occurredAt = DateTime.UtcNow,
            accountId = account.Id,
            ownerId = account.OwnerId,
            currency = account.Currency.ToString(),
            type = account.AccountType.ToString()
        };

        await context.Outboxes.AddAsync(new Outbox
        {
            Id = Guid.NewGuid(),
            Type = "AccountOpened",
            Payload = System.Text.Json.JsonSerializer.Serialize(@event),
            OccurredAt = DateTime.UtcNow
        });
        
        await context.SaveChangesAsync();
    }

    public async Task<Models.Account> CloseAccount(Guid accountId)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }

        if (account.IsClosed)
        {
            return account;
        }

        if (account.Balance != 0)
        {
            throw new CustomExceptions.AccountBalanceNotZeroException(accountId);
        }
        
        account.ClosedAt = DateTime.UtcNow;
        
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CustomExceptions.ConcurrencyConflictException(accountId);
        }
        
        return account;
    }

    public async Task<IList<Models.Account>> GetUserAccounts(Guid ownerId)
    {
        var accounts = await context.Accounts.Where(a => a.OwnerId == ownerId).ToListAsync();
        
        return accounts;
    }

    public async Task<Models.Account> ChangeInterestRate(Guid accountId, decimal interestRate)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
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
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CustomExceptions.ConcurrencyConflictException(accountId);
        }
        
        return account;
    }

    public async Task<Models.Account> GetAccountById(Guid accountId)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }
        
        return account;
    }
    
    public async Task<Transaction> RegisterAccountTransaction(Guid accountId, Guid counterpartyId, decimal amount, Currency currency,
        string description = "")
    {
        if (accountId == counterpartyId)
        {
            throw new CustomExceptions.InvalidTransferException("Account и counterparty не могут совпадать");
        }

        if (amount <= 0)
        {
            throw new CustomExceptions.InvalidTransferException("Сумма должна быть больше нуля");
        }

        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            if (context.Database.IsNpgsql())
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $@"SELECT 1 FROM ""Accounts"" WHERE ""Id"" = {accountId} OR ""Id"" = {counterpartyId} ORDER BY ""Id"" FOR UPDATE");
            }

            var accounts = await context.Accounts
                .Where(a => a.Id == accountId || a.Id == counterpartyId)
                .OrderBy(a => a.Id)
                .ToListAsync();

            var account = accounts.SingleOrDefault(a => a.Id == accountId);
            if (account == null)
            {
                throw new CustomExceptions.AccountNotFoundException(accountId);
            }
            if (account.IsClosed || account.IsFrozen)
            {
                throw new CustomExceptions.AccountClosedException(accountId);
            }
            
            var counterpartyAccount = accounts.SingleOrDefault(a => a.Id == counterpartyId);
            if (counterpartyAccount == null)
            {
                throw new CustomExceptions.AccountNotFoundException(counterpartyId);
            }
            if (counterpartyAccount.IsClosed || counterpartyAccount.IsFrozen)
            {
                throw new CustomExceptions.AccountClosedException(counterpartyId);
            }

            if (account.Balance < amount)
            {
                throw new CustomExceptions.InsufficientBalanceException(accountId);
            }

            if (account.Currency != counterpartyAccount.Currency || account.Currency != currency)
            {
                throw new CustomExceptions.CurrenciesDoNotMatchException();
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
        
            await context.Transactions.AddRangeAsync(transaction, counterpartyTransaction);
            
            var moneyDebitedEvent = new
            {
                eventId = Guid.NewGuid(),
                occurredAt = DateTime.UtcNow,
                accountId = account.Id,
                amount,
                operationId = transaction.Id,
                reason = description
            };

            await context.Outboxes.AddAsync(new Outbox
            {
                Id = Guid.NewGuid(),
                Type = "MoneyDebited",
                Payload = System.Text.Json.JsonSerializer.Serialize(moneyDebitedEvent),
                OccurredAt = DateTime.UtcNow
            });
            
            var moneyCreditedEvent = new
            {
                eventId = Guid.NewGuid(),
                occurredAt = DateTime.UtcNow,
                accountId = counterpartyAccount.Id,
                amount,
                operationId = counterpartyTransaction.Id
            };

            await context.Outboxes.AddAsync(new Outbox
            {
                Id = Guid.NewGuid(),
                Type = "MoneyCredited",
                Payload = System.Text.Json.JsonSerializer.Serialize(moneyCreditedEvent),
                OccurredAt = DateTime.UtcNow
            });
            
            var transferCompletedEvent = new
            {
                eventId = Guid.NewGuid(),
                occurredAt = DateTime.UtcNow,
                sourceAccountId = accountId,
                destinationAccountId = counterpartyId,
                amount,
                currency = currency.ToString(),
                transferId = transaction.Id
            };
            
            await context.Outboxes.AddAsync(new Outbox
            {
                Id = Guid.NewGuid(),
                Type = "TransferCompleted",
                Payload = System.Text.Json.JsonSerializer.Serialize(transferCompletedEvent),
                OccurredAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await dbTransaction.CommitAsync(); 
        
            return transaction;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            throw new CustomExceptions.ConcurrencyConflictException(accountId);
        }
        catch (DbUpdateException e)
        {
            await dbTransaction.RollbackAsync();
            throw new CustomExceptions.InvalidTransferException(e.InnerException?.Message ?? e.Message);
        }
        
    }

    public async Task<Transaction> RegisterIncomingOrOutgoingTransactionsCommand(Guid accountId, decimal amount, Currency currency,
        TransactionType transactionType, string description = "")
    {
        if (amount <= 0)
        {
            throw new CustomExceptions.InvalidTransferException("Сумма должна быть больше нуля");
        }

        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            if (context.Database.IsNpgsql())
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $@"SELECT 1 FROM ""Accounts"" WHERE ""Id"" = {accountId} FOR UPDATE");
            }

            var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
            if (account == null)
            {
                throw new CustomExceptions.AccountNotFoundException(accountId);
            }

            if (account.IsClosed || account.IsFrozen)
            {
                throw new CustomExceptions.AccountClosedException(accountId);
            }

            if (account.Currency != currency)
            {
                throw new CustomExceptions.CurrenciesDoNotMatchException();
            }

            switch (transactionType)
            {
                case TransactionType.Debit when account.Balance < amount:
                    throw new CustomExceptions.InsufficientBalanceException(accountId);
                case TransactionType.Debit:
                    account.Balance -= amount;
                    break;
                case TransactionType.Credit:
                    account.Balance += amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, null);
            }

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Amount = amount,
                Currency = currency,
                TransactionType = transactionType,
                Description = description,
                CommitedAt = DateTime.UtcNow
            };

            await context.Transactions.AddAsync(transaction);
            
            var isDebit = transactionType == TransactionType.Debit;
            
            var @event = new
            {
                eventId = Guid.NewGuid(),
                occurredAt = DateTime.UtcNow,
                accountId = account.Id,
                amount,
                operationId = transaction.Id,
                reason = isDebit ? description : null
            };

            await context.Outboxes.AddAsync(new Outbox
            {
                Id = Guid.NewGuid(),
                Type = isDebit ? "MoneyDebited" : "MoneyCredited",
                Payload = System.Text.Json.JsonSerializer.Serialize(@event),
                OccurredAt = DateTime.UtcNow
            });
            
            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync(); 
            
            return transaction;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            throw new CustomExceptions.ConcurrencyConflictException(accountId);
        }
        catch (DbUpdateException e)
        {
            await dbTransaction.RollbackAsync();
            throw new CustomExceptions.InvalidTransferException(e.InnerException?.Message ?? e.Message);
        }
    }


    public async Task<List<Transaction>> GetAccountTransactions(Guid accountId, DateTime? startDate, DateTime? endDate)
    {
        var accountExists = await context.Accounts.AnyAsync(a => a.Id == accountId);
        if (!accountExists)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }

        var query = context.Transactions
            .Where(t => t.AccountId == accountId);

        if (startDate.HasValue)
        {
            var utcStartDate = startDate.Value.ToUniversalTime();
            query = query.Where(t => t.CommitedAt >= utcStartDate);
        }

        if (!endDate.HasValue)
            return await query
                .OrderByDescending(t => t.CommitedAt)
                .ToListAsync();
        {
            var utcEndDate = endDate.Value.ToUniversalTime();
            query = query.Where(t => t.CommitedAt <= utcEndDate);
        }

        return await query
                    .OrderByDescending(t => t.CommitedAt)
                    .ToListAsync();
    }
    
    public async Task AccrueInterest(Guid accountId)
    {
        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        if (context.Database.IsNpgsql())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $@"SELECT 1 FROM ""Accounts"" WHERE ""Id"" = {accountId} FOR UPDATE");
        }

        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null)
        {
            throw new CustomExceptions.AccountNotFoundException(accountId);
        }

        if (account.IsClosed || account.AccountType == AccountType.Checking || !account.InterestRate.HasValue)
        {
            await dbTransaction.CommitAsync();
            return;
        }

        var periodFrom = DateTime.UtcNow.Date;
        var alreadyAccrued = await context.Transactions.AnyAsync(t =>
            t.AccountId == accountId &&
            t.Description == InterestAccrualDescription &&
            t.CommitedAt >= periodFrom);
        if (alreadyAccrued)
        {
            await dbTransaction.CommitAsync();
            return;
        }

        var amount = decimal.Round(
            account.Balance * (account.InterestRate.Value / 100) / 365,
            10,
            MidpointRounding.ToEven);
        if (amount <= 0)
        {
            await dbTransaction.CommitAsync();
            return;
        }

        var occurredAt = DateTime.UtcNow;
        account.Balance += amount;
        await context.Transactions.AddAsync(new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Amount = amount,
            Currency = account.Currency,
            TransactionType = TransactionType.Credit,
            Description = InterestAccrualDescription,
            CommitedAt = occurredAt
        });
        
        var @event = new
        {
            eventId = Guid.NewGuid(),
            occurredAt,
            accountId = account.Id,
            periodFrom,
            periodTo = occurredAt,
            amount
        };

        await context.Outboxes.AddAsync(new Outbox
        {
            Id = Guid.NewGuid(),
            Type = "InterestAccrued",
            Payload = System.Text.Json.JsonSerializer.Serialize(@event),
            OccurredAt = occurredAt
        });
        await context.SaveChangesAsync();
        await dbTransaction.CommitAsync();
    }


    public async Task AccrueInterestForAllAccounts()
    {
        var accountsId = await context.Accounts
            .Where(a => !a.IsClosed && a.AccountType != AccountType.Checking && a.InterestRate.HasValue)
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToListAsync();

        foreach (var accountId in accountsId)
        {
            await AccrueInterest(accountId);
        }
    }
}
