using System.Data;
using bank_accounts.Account.Data;
using bank_accounts.Account.Enums;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Interfaces;
using bank_accounts.RabbitMQ.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Transaction = bank_accounts.Account.Models.Transaction;

namespace bank_accounts.Account.Services;

public class AccountService(BankAccountsDbContext context) : IAccountService
{
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
        if(accountId == counterpartyId)
        {
            throw new CustomExceptions.InvalidTransferException("Account не counterparty могут быть одинаковыми");
        }
        
        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var totalBalanceBeforeTransaction = await context.Accounts.SumAsync(a => a.Balance);
            var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
            if (account == null)
            {
                throw new CustomExceptions.AccountNotFoundException(accountId);
            }
            if (account.IsClosed || account.IsFrozen)
            {
                throw new CustomExceptions.AccountClosedException(accountId);
            }
            
            var counterpartyAccount = await context.Accounts.FirstOrDefaultAsync(a => a.Id == counterpartyId);
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
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new CustomExceptions.ConcurrencyConflictException(accountId);
            }
            
            var totalBalanceAfterTransaction = await context.Accounts.SumAsync(a => a.Balance);
            if (!totalBalanceBeforeTransaction.Equals(totalBalanceAfterTransaction))
            {
                await dbTransaction.RollbackAsync();
                throw new CustomExceptions.InvalidBalanceStateException(totalBalanceBeforeTransaction, totalBalanceAfterTransaction);
            }

            await dbTransaction.CommitAsync(); 
        
            return transaction;
        }
        catch (Exception e)
        {
            await dbTransaction.RollbackAsync();
            throw new CustomExceptions.InvalidTransferException(e.Message);
        }
        
    }

    public async Task<Transaction> RegisterIncomingOrOutgoingTransactionsCommand(Guid accountId, decimal amount, Currency currency,
        TransactionType transactionType, string description = "")
    {
        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
            if (account == null)
            {
                throw new CustomExceptions.AccountNotFoundException(accountId);
            }

            if (account.IsClosed || account.IsFrozen)
            {
                throw new CustomExceptions.AccountClosedException(accountId);
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
            
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new CustomExceptions.ConcurrencyConflictException(accountId);
            }
            await dbTransaction.CommitAsync(); 
            
            return transaction;
        }
        catch (Exception e)
        {
            await dbTransaction.RollbackAsync();
            throw new CustomExceptions.InvalidTransferException(e.Message);
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
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        var amount = account!.Balance * (account.InterestRate / 100) / 365;
        await using var connection = new NpgsqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand("CALL accrue_interest(@account_id)", connection);
        cmd.Parameters.AddWithValue("account_id", accountId);
        await cmd.ExecuteNonQueryAsync();
        
        var @event = new
        {
            eventId = Guid.NewGuid(),
            occurredAt = DateTime.UtcNow,
            accountId = account.Id,
            periodFrom = DateTime.UtcNow.AddDays(-1),
            periodTo = DateTime.UtcNow,
            amount
        };

        await context.Outboxes.AddAsync(new Outbox
        {
            Id = Guid.NewGuid(),
            Type = "InterestAccrued",
            Payload = System.Text.Json.JsonSerializer.Serialize(@event),
            OccurredAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }


    public async Task AccrueInterestForAllAccounts()
    {
        var accountsId = await context.Accounts.Select(a => a.Id).ToListAsync();

        foreach (var accountId in accountsId)
        {
            await AccrueInterest(accountId);
        }
    }
}
