using System.Text;
using System.Text.Json;
using bank_accounts.Account.Data;
using bank_accounts.RabbitMQ.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace bank_accounts.RabbitMQ;

public class AntifraudConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"],
            UserName = configuration["RabbitMq:UserName"],
            Password = configuration["RabbitMq:Password"],
            Port = 5672
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, 1, false);

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch
            {
                _channel!.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            var root = doc.RootElement;
#pragma warning disable CA1869
            var payload = root.GetProperty("payload").Deserialize<ClientStatusChanged>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
#pragma warning restore CA1869
            var type = payload!.Status == "Blocked" ? "ClientBlocked" : "ClientUnblocked";
            var version = root.GetProperty("meta").TryGetProperty("version", out var v) ? v.GetString() : null;
            var meta = root.GetProperty("meta")
#pragma warning disable CA1869
                .Deserialize<Meta>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
#pragma warning restore CA1869
            if (version != "v1")
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();

#pragma warning disable CA1869
                var payloadObj = root.GetProperty("payload").Deserialize<ClientStatusChanged>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
#pragma warning restore CA1869

                db.InboxDeadLetters.Add(new InboxDeadLetter
                {
                    Id = Guid.NewGuid(),
                    Type = type,
                    Payload = payloadObj!,
                    OccurredAt = DateTime.UtcNow,
                    PublishedAt = null,
#pragma warning disable CS8601 // Possible null reference assignment.
                    Meta = meta
#pragma warning restore CS8601 // Possible null reference assignment.
                });
                await db.SaveChangesAsync(stoppingToken);
                
                var retry = ea.BasicProperties.Headers?["X-Retry"] is byte[] r ? Encoding.UTF8.GetString(r) : "0";
                var latency = ea.BasicProperties.Headers?["X-Latency"] is byte[] l ? Encoding.UTF8.GetString(l) : null;
                Log.Warning(
                    "Неправильная версия сообщения: {EventId}, {Type}, {CorrelationId}, {Retry}, {Latency}", 
                    root.TryGetProperty("eventId", out var eid) ? eid.GetGuid() : Guid.Empty, 
                    type, 
                    meta?.CorrelationId,
                    retry,
                    latency
                    );
                _channel!.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var eventId = root.GetProperty("eventId").GetGuid();
            var occurredAt = root.GetProperty("occurredAt").GetDateTime();

            using var scope2 = scopeFactory.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<BankAccountsDbContext>();

            var alreadyProcessed = await db2.Inboxes.AnyAsync(x => x.Id == eventId, stoppingToken);
            if (alreadyProcessed)
            {
                _channel!.BasicAck(ea.DeliveryTag, false);
                Log.Information("Сообщение уже обработано: {EventId}, {Type}, {CorrelationId}", eventId, type, meta?.CorrelationId);
                return;
            }

            await using var tx = await db2.Database.BeginTransactionAsync(stoppingToken);
            try
            {
                if (payload.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
                {
                    await db2.Accounts
                        .Where(a => a.OwnerId == payload.OwnerId)
                        .ExecuteUpdateAsync(a => a.SetProperty(x => x.IsFrozen, true), stoppingToken);
                }
                else
                {
                    await db2.Accounts
                        .Where(a => a.OwnerId == payload.OwnerId)
                        .ExecuteUpdateAsync(a => a.SetProperty(x => x.IsFrozen, false), stoppingToken);
                }

                db2.Inboxes.Add(new Inbox
                {
                    Id = eventId,
                    Type = type,
                    Payload = payload,
                    OccurredAt = occurredAt,
                    PublishedAt = null,
#pragma warning disable CS8601 // Possible null reference assignment.
                    Meta = meta
#pragma warning restore CS8601 // Possible null reference assignment.
                });

                await db2.SaveChangesAsync(stoppingToken);
                var retry = ea.BasicProperties.Headers?["X-Retry"] is byte[] r ? Encoding.UTF8.GetString(r) : "0";
                var latency = ea.BasicProperties.Headers?["X-Latency"] is byte[] l ? Encoding.UTF8.GetString(l) : null;
                Log.Information(
                    "Сообщение успешно обработано: {EventId}, {Type}, {CorrelationId}, {Retry}, {Latency}", 
                    eventId, 
                    type, 
                    meta?.CorrelationId,
                    retry,
                    latency);
                await tx.CommitAsync(stoppingToken);

                _channel!.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(stoppingToken);
                var retry = ea.BasicProperties.Headers?["X-Retry"] is byte[] r ? Encoding.UTF8.GetString(r) : "0";
                var latency = ea.BasicProperties.Headers?["X-Latency"] is byte[] l ? Encoding.UTF8.GetString(l) : null;
                Log.Error(ex, 
                    "Ошибка при получении сообщения {MessageId}, {Type}, {CorrelationId}, {Retry}, {Latency}",
                    eventId,
                    type, 
                    meta?.CorrelationId,
                    retry,
                    latency);
                _channel!.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume("account.antifraud", false, consumer);
        return Task.CompletedTask;
    }
}