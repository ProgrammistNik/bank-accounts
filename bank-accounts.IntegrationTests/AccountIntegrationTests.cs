using System.Net;
using System.Net.Http.Json;
using bank_accounts.Account.Data;
using bank_accounts.Account.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bank_accounts.IntegrationTests;

public class AccountIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    
    public AccountIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(); 
    }

#pragma warning disable xUnit1013
    public async Task DisposeAsync()
#pragma warning restore xUnit1013
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
    
    [Fact]
    public async Task GetAccounts_ReturnsOk()
    {

        var response = await _client.GetAsync("api/health/live");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task FiftyParallelTransfers_ShouldUpdateBalancesAndCreateTransactions()
    {
        const int transferCount = 50;
        const decimal transferAmount = 1m;
        const decimal initialBalance = 100m;
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        using (var setupScope = _factory.Services.CreateScope())
        {
            var dbSetup = setupScope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();

            var account1 = new Account.Models.Account
            {
                Id = fromAccountId,
                OwnerId = Guid.NewGuid(),
                AccountType = Account.Enums.AccountType.Checking,
                Currency = Account.Enums.Currency.Rub,
                Balance = initialBalance,
                CreatedAt = DateTime.UtcNow
            };
            var account2 = new Account.Models.Account
            {
                Id = toAccountId,
                OwnerId = Guid.NewGuid(),
                AccountType = Account.Enums.AccountType.Checking,
                Currency = Account.Enums.Currency.Rub,
                Balance = initialBalance,
                CreatedAt = DateTime.UtcNow
            };

            dbSetup.Accounts.AddRange(account1, account2);
            await dbSetup.SaveChangesAsync();
        }

        var tasks = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < transferCount; i++)
        {
            var dto = new CreateTransactionDto
            {
                AccountId = fromAccountId,
                CounterpartyId = toAccountId,
                Amount = transferAmount,
                Currency = Account.Enums.Currency.Rub,
                Description = $"Transfer {i}"
            };
            tasks.Add(_client.PostAsJsonAsync("/transaction", dto));
        }

        var responses = await Task.WhenAll(tasks);

        foreach (var response in responses)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.True(
                response.IsSuccessStatusCode,
                $"Transfer failed with {(int)response.StatusCode}: {content}");
        }

        using var assertionScope = _factory.Services.CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        var accounts = await dbContext.Accounts
            .Where(account => account.Id == fromAccountId || account.Id == toAccountId)
            .ToDictionaryAsync(account => account.Id);

        Assert.Equal(initialBalance - transferCount * transferAmount, accounts[fromAccountId].Balance);
        Assert.Equal(initialBalance + transferCount * transferAmount, accounts[toAccountId].Balance);

        var transactionCount = await dbContext.Transactions.CountAsync(transaction =>
            (transaction.AccountId == fromAccountId && transaction.CounterpartyId == toAccountId) ||
            (transaction.AccountId == toAccountId && transaction.CounterpartyId == fromAccountId));

        Assert.Equal(transferCount * 2, transactionCount);
    }
    
    
    [Fact]
    public async Task UpdateAccount_ShouldThrowConcurrencyException()
    {
        using (var setupScope = _factory.Services.CreateScope())
        {
            var dbSetup = setupScope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        
            var account = new Account.Models.Account
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                AccountType = Account.Enums.AccountType.Checking,
                Currency = Account.Enums.Currency.Rub,
                Balance = 100,
                CreatedAt = DateTime.UtcNow
            };
        
            dbSetup.Accounts.Add(account);
            await dbSetup.SaveChangesAsync();
        }
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
    
        var db1 = scope1.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
    
        var accountId = await db1.Accounts.Select(a => a.Id).FirstAsync();
    
        var acc1 = await db1.Accounts.FirstAsync(a => a.Id == accountId);
        var acc2 = await db2.Accounts.FirstAsync(a => a.Id == accountId);
    
        acc1.Balance += 10;
        acc2.Balance += 20;
    
        await db1.SaveChangesAsync();
    
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await db2.SaveChangesAsync();
        });
    }
}
