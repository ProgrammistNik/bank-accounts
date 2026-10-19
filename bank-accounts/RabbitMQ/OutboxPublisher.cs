using System.Text;
using System.Collections.Concurrent;
using bank_accounts.Account.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Serilog;

namespace bank_accounts.RabbitMQ;

public sealed class OutboxPublisher(IServiceScopeFactory scopeFactory, IConfiguration configuration)
{
    public async Task PublishPendingAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var messages = await db.Outboxes
            .FromSqlRaw("""
                SELECT *
                FROM "Outboxes"
                WHERE "PublishedAt" IS NULL
                ORDER BY "OccurredAt"
                LIMIT 100
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync();

        if (messages.Count == 0)
        {
            await transaction.CommitAsync();
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"],
            UserName = configuration["RabbitMq:UserName"],
            Password = configuration["RabbitMq:Password"],
            Port = configuration.GetValue<int>("RabbitMq:Port")
        };

        using var connection = await CreateConnectionAsync(factory);
        using var channel = connection.CreateModel();
        channel.ConfirmSelect();

        var returnedMessages = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        channel.BasicReturn += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.BasicProperties.MessageId))
            {
                returnedMessages.TryAdd(eventArgs.BasicProperties.MessageId, 0);
            }
        };

        foreach (var message in messages)
        {
            try
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.Id.ToString();
                properties.ContentType = "application/json";
                properties.Headers = new Dictionary<string, object>
                {
                    ["X-Correlation-Id"] = message.Meta.CorrelationId.ToString(),
                    ["X-Causation-Id"] = message.Meta.CausationId.ToString()
                };

                channel.BasicPublish(
                    exchange: "account.events",
                    routingKey: GetRoutingKey(message.Type),
                    mandatory: true,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(message.Payload));

                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
                if (returnedMessages.ContainsKey(properties.MessageId))
                {
                    throw new InvalidOperationException($"Message {properties.MessageId} was returned by RabbitMQ");
                }

                message.PublishedAt = DateTime.UtcNow;
                Log.Information(
                    "Сообщение опубликовано: {EventId}, {Type}, {CorrelationId}, {Latency}",
                    message.Id,
                    message.Type,
                    message.Meta.CorrelationId,
                    message.PublishedAt - message.OccurredAt);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Ошибка публикации сообщения {MessageId}, {Type}", message.Id, message.Type);
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static string GetRoutingKey(string eventType) => eventType switch
    {
        "AccountOpened" => "account.opened",
        "MoneyCredited" => "money.credited",
        "MoneyDebited" => "money.debited",
        "TransferCompleted" => "money.transfer.completed",
        "InterestAccrued" => "account.audit",
        _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
    };

    private static async Task<IConnection> CreateConnectionAsync(ConnectionFactory factory)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return factory.CreateConnection();
            }
            catch (BrokerUnreachableException) when (attempt < 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
