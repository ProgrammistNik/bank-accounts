using bank_accounts.Account.Data;
using bank_accounts.Account.Enums;
using bank_accounts.Account.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;



namespace bank_accounts.UnitTests;

[TestFixture]
public class AccountServiceUnitTests
{
    [Test]
    public async Task CloseAccount_ShouldCloseAccount_WhenAccountIsOpen()
    {
        var options = new DbContextOptionsBuilder<BankAccountsDbContext>()
            .UseInMemoryDatabase(databaseName: "CloseAccountTestDb")
            .Options;

        var accountId = Guid.NewGuid();

        await using (var context = new BankAccountsDbContext(options))
        {
            var account = new Account.Models.Account
            {
                Id = accountId,
                OwnerId = Guid.NewGuid(),
                AccountType = AccountType.Checking,
                Currency = Currency.Rub,
                Balance = 1000,
                CreatedAt = DateTime.UtcNow,
                ClosedAt = null
            };

            context.Accounts.Add(account);
            await context.SaveChangesAsync();
        }

        await using (var context = new BankAccountsDbContext(options))
        {
            var service = new AccountService(context);
            var closedAccount = await service.CloseAccount(accountId);

            Assert.That(closedAccount, Is.Not.Null);
            Assert.That(closedAccount.IsClosed, Is.True);
        }
    }
    
    [Test]
    public async Task ChangeInterestRate_ShouldChangeInterestRate_WhenAccountIsNotChecking()
    {
        var options = new DbContextOptionsBuilder<BankAccountsDbContext>()
            .UseInMemoryDatabase(databaseName: "ChangeInterestRateTestDb")
            .Options;
        
        var accountCreditId = Guid.NewGuid();
        var accountDepositId = Guid.NewGuid();
        
        await using (var context = new BankAccountsDbContext(options))
        {
            var accountCredit = new Account.Models.Account
            {
                Id = accountCreditId,
                OwnerId = Guid.NewGuid(),
                AccountType = AccountType.Credit,
                Currency = Currency.Rub,
                Balance = 1000,
                InterestRate = 5,
                CreatedAt = DateTime.UtcNow,
                ClosedAt = null
            };
            
            var accountDeposit = new Account.Models.Account
            {
                Id = accountDepositId,
                OwnerId = Guid.NewGuid(),
                AccountType = AccountType.Deposit,
                Currency = Currency.Rub,
                Balance = 1000,
                InterestRate = 3,
                CreatedAt = DateTime.UtcNow,
                ClosedAt = null
            };

            context.Accounts.Add(accountCredit);
            context.Accounts.Add(accountDeposit);
            await context.SaveChangesAsync();
        }

        await using (var context = new BankAccountsDbContext(options))
        {
            var service = new AccountService(context);
            var updatedCreditAccount = await service.ChangeInterestRate(accountCreditId,10);
            var updatedDepositAccount = await service.ChangeInterestRate(accountDepositId, 4);
            
            Assert.That(updatedCreditAccount, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(updatedCreditAccount.InterestRate, Is.EqualTo(10));
                Assert.That(updatedDepositAccount, Is.Not.Null);
            });
            Assert.That(updatedDepositAccount.InterestRate, Is.EqualTo(4));
        }
        
    }

    [Test]
    public async Task RegisterAccountTransaction_ShouldTransferMoney_WhenDataIsValid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        try
        {
            var options = new DbContextOptionsBuilder<BankAccountsDbContext>()
                .UseSqlite(connection)
                .Options;
        
            var accountIdFrom = Guid.NewGuid();
            var accountIdTo = Guid.NewGuid();

            await using (var context = new BankAccountsDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                var accountFrom = new Account.Models.Account
                {
                    Id = accountIdFrom,
                    // OwnerId = Guid.NewGuid(),
                    AccountType = AccountType.Checking,
                    Currency = Currency.Rub,
                    Balance = 1000,
                    CreatedAt = DateTime.UtcNow,
                    ClosedAt = null
                };
            
                var accountTo = new Account.Models.Account
                {
                    Id = accountIdTo,
                    OwnerId = Guid.NewGuid(),
                    AccountType = AccountType.Credit,
                    Currency = Currency.Rub,
                    Balance = 0,
                    InterestRate = 5,
                    CreatedAt = DateTime.UtcNow,
                    ClosedAt = null
                };
            
                context.Accounts.Add(accountFrom);
                context.Accounts.Add(accountTo);
                await context.SaveChangesAsync();
            }

            await using (var context = new BankAccountsDbContext(options))
            {
                var service = new AccountService(context);
                var transaction = await service.RegisterAccountTransaction(accountIdFrom, accountIdTo, 500, Currency.Rub, "Перевод средств");

                Assert.That(transaction, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(transaction.Amount, Is.EqualTo(500));
                    Assert.That(transaction.Description, Is.EqualTo("Перевод средств"));
                });

                var updatedAccountFrom = await context.Accounts.FindAsync(accountIdFrom);
                var updatedAccountTo = await context.Accounts.FindAsync(accountIdTo);

                Assert.That(updatedAccountFrom, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(updatedAccountFrom.Balance, Is.EqualTo(500));
                    Assert.That(updatedAccountTo, Is.Not.Null);
                });
                Assert.That(updatedAccountTo.Balance, Is.EqualTo(500));
            }
        } 
        finally
        {
            connection.Close();
        }
    }
    
}