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
    public async Task FiftyParallelTransfers_ShouldKeepTotalBalance_AndAllowConflicts()
    {
        using (var setupScope = _factory.Services.CreateScope())
        {
            var dbSetup = setupScope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        
            var account1 = new Account.Models.Account
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                AccountType = Account.Enums.AccountType.Checking,
                Currency = Account.Enums.Currency.Rub,
                Balance = 100,
                CreatedAt = DateTime.UtcNow
            };
            var account2 = new Account.Models.Account
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                AccountType = Account.Enums.AccountType.Checking,
                Currency = Account.Enums.Currency.Rub,
                Balance = 100,
                CreatedAt = DateTime.UtcNow
            };
        
            dbSetup.Accounts.AddRange(account1, account2);
            await dbSetup.SaveChangesAsync();
        }
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
    
        var accounts = await dbContext.Accounts.Where(a => a.ClosedAt == null && a.Balance > 50).Take(2).ToListAsync();
        var fromAccount = accounts[0];
        var toAccount = accounts[1];
    
        var initialTotalBalance = await dbContext.Accounts.SumAsync(a => a.Balance);
    
        var tasks = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < 50; i++)
        {
            var dto = new CreateTransactionDto
            {
                AccountId = fromAccount.Id,
                CounterpartyId = toAccount.Id,
                Amount = 1,
                Currency = fromAccount.Currency,
                Description = $"Transfer {i}"
            };
            tasks.Add(_client.PostAsJsonAsync("/transaction", dto));
        }
    
        var responses = await Task.WhenAll(tasks);
    
        Assert.Contains(responses, r => r.IsSuccessStatusCode);
    
        foreach (var response in responses)
        {
            if (response.IsSuccessStatusCode) continue;
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (response.StatusCode)
            {
                case HttpStatusCode.Conflict:
                    
                    continue;
                case HttpStatusCode.BadRequest:
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!content.Contains("transient failure", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Fail($"Unexpected 400 failure without transient failure message: {content}");
                    }
    
                    break;
                }
                default:
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.Fail($"Unexpected failure: {(int)response.StatusCode} - {content}");
                    break;
                }
            }
        }
    
        var finalTotalBalance = await dbContext.Accounts.SumAsync(a => a.Balance);
        Assert.Equal(initialTotalBalance, finalTotalBalance);
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