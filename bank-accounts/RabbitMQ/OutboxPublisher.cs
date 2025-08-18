using System.Text;
using bank_accounts.Account.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;

namespace bank_accounts.RabbitMQ;

public class OutboxPublisher(IServiceScopeFactory scopeFactory, IConfiguration configuration)
{
    public async Task PublishPendingAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();

        var messages = await db.Outboxes
            .Where(x => x.PublishedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Include(outbox => outbox.Meta)
            .ToListAsync();

        if (messages.Count == 0)
            return;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"],
            UserName = configuration["RabbitMq:UserName"],
            Password = configuration["RabbitMq:Password"],
            Port = 5672
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        
        foreach (var msg in messages)
        {
            try
            {
                var props = channel.CreateBasicProperties();
                props.Persistent = true;
                props.Headers = new Dictionary<string, object>
                {
                    ["X-Correlation-Id"] = msg.Meta.CorrelationId.ToString(),
                    ["X-Causation-Id"] = msg.Meta.CausationId.ToString()
                };
                
                var body = Encoding.UTF8.GetBytes(msg.Payload);

                channel.BasicPublish(
                    exchange: "account.events",
                    routingKey: GetRoutingKey(msg.Type),
                    basicProperties: props,
                    body: body
                );
                var latency = DateTime.UtcNow - msg.OccurredAt;
                msg.PublishedAt = DateTime.UtcNow;
                Log.Information("Сообщение успешно опубликовано: {EventId}, {Type}, {CorrelationId} {Latency}", msg.Id, msg.Type, msg.Meta.CorrelationId, latency);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при публикации сообщения {MessageId} {Type}", msg.Id, msg.Type);
            }
        }

        await db.SaveChangesAsync();
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
}
