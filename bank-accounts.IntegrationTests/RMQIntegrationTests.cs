using System.Net.Http.Json;
using bank_accounts.Account.Data;
using bank_accounts.Account.Dto;
using bank_accounts.Account.Enums;
using bank_accounts.Account.Exceptions;
using bank_accounts.Account.Services;
using bank_accounts.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace bank_accounts.IntegrationTests;

public class RmqIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    
    public RmqIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(); 
    }

    [Fact]
    public async Task OutboxPublishesAfterFailure()
    {
        await _factory.StopRabbitAsync();
        
        var newAccount = new CreateAccountDto
        {
            OwnerId = Guid.NewGuid(),
            AccountType = AccountType.Checking,
            Currency = Currency.Rub,
            Balance = 1000,
            InterestRate = 5
        };
        var response = await _client.PostAsJsonAsync("/account", newAccount);
        response.EnsureSuccessStatusCode();

        Guid messageId;
        using (var messageScope = _factory.Services.CreateScope())
        {
            var messageDb = messageScope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
            messageId = await messageDb.Outboxes
                .Where(message => message.Type == "AccountOpened")
                .OrderByDescending(message => message.OccurredAt)
                .Select(message => message.Id)
                .FirstAsync();
        }
        
        await _factory.StartRabbitAsync();
        
        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var published = await dbContext.Outboxes
                .AsNoTracking()
                .AnyAsync(message => message.Id == messageId && message.PublishedAt != null);
            if (published)
            {
                break;
            }

            await publisher.PublishPendingAsync();
        }

        var publishedAt = await dbContext.Outboxes
            .AsNoTracking()
            .Where(message => message.Id == messageId)
            .Select(message => message.PublishedAt)
            .SingleAsync();

        Assert.NotNull(publishedAt);
    }

    [Fact]
    public async Task ClientBlockedPreventsDebit()
    {
        var accountId = Guid.NewGuid();
        using (var setupScope = _factory.Services.CreateScope())
        {
            var dbSetup = setupScope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        
            var account = new Account.Models.Account
            {
                Id = accountId,
                OwnerId = Guid.NewGuid(),
                AccountType = AccountType.Checking,
                Currency = Currency.Rub,
                Balance = 100,
                CreatedAt = DateTime.UtcNow,
                IsFrozen = true
            };
        
            dbSetup.Accounts.Add(account);
            await dbSetup.SaveChangesAsync();
        }
        var context = _factory.Services.GetRequiredService<BankAccountsDbContext>();
        var service = new AccountService(context);
        await Assert.ThrowsAsync<CustomExceptions.AccountClosedException>(async () =>
        {
            await service.RegisterIncomingOrOutgoingTransactionsCommand(
                accountId, 10, Currency.Rub, TransactionType.Debit, "MoneyDebited"
            );
        });
        
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        var inboxesMsg = await dbContext.Outboxes.Where(o => o.Type == "MoneyDebited")
            .ToListAsync();
        Assert.Empty(inboxesMsg);
        
    }
}
