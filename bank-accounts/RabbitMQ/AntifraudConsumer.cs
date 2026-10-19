using System.Text;
using System.Text.Json;
using bank_accounts.Account.Data;
using bank_accounts.RabbitMQ.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace bank_accounts.RabbitMQ;

public sealed class AntifraudConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"],
            UserName = configuration["RabbitMq:UserName"],
            Password = configuration["RabbitMq:Password"],
            Port = configuration.GetValue<int>("RabbitMq:Port"),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += (_, eventArgs) => HandleMessageAsync(eventArgs, stoppingToken);
        _channel.BasicConsume("account.antifraud", false, consumer);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(eventArgs.Body);
            var root = document.RootElement;

            if (!TryReadMessage(root, out var eventId, out var occurredAt, out var payload, out var meta))
            {
                Log.Warning("Получено некорректное antifraud-сообщение");
                Reject(eventArgs.DeliveryTag, false);
                return;
            }

            var type = GetEventType(payload.Status);
            if (meta.Version != "v1" || type is null)
            {
                await SaveDeadLetterAsync(type ?? "ClientStatusChanged", payload, occurredAt, meta, cancellationToken);
                Log.Warning(
                    "Antifraud-сообщение отправлено в dead letter: {EventId}, {Status}, {Version}, {CorrelationId}, {Retry}, {Latency}",
                    eventId,
                    payload.Status,
                    meta.Version,
                    meta.CorrelationId,
                    ReadHeader(eventArgs.BasicProperties, "X-Retry") ?? "0",
                    ReadHeader(eventArgs.BasicProperties, "X-Latency"));
                Acknowledge(eventArgs.DeliveryTag);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();

            if (await db.Inboxes.AnyAsync(x => x.Id == eventId, cancellationToken))
            {
                Acknowledge(eventArgs.DeliveryTag);
                Log.Information(
                    "Antifraud-сообщение уже обработано: {EventId}, {Type}, {CorrelationId}",
                    eventId,
                    type,
                    meta.CorrelationId);
                return;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var isFrozen = type == "ClientBlocked";
            await db.Accounts
                .Where(account => account.OwnerId == payload.OwnerId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(account => account.IsFrozen, isFrozen),
                    cancellationToken);

            db.Inboxes.Add(new Inbox
            {
                Id = eventId,
                Type = type,
                Payload = payload,
                OccurredAt = occurredAt,
                Meta = meta
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            Acknowledge(eventArgs.DeliveryTag);

            Log.Information(
                "Antifraud-сообщение обработано: {EventId}, {Type}, {CorrelationId}, {Retry}, {Latency}",
                eventId,
                type,
                meta.CorrelationId,
                ReadHeader(eventArgs.BasicProperties, "X-Retry") ?? "0",
                ReadHeader(eventArgs.BasicProperties, "X-Latency"));
        }
        catch (JsonException exception)
        {
            Log.Warning(exception, "Получено antifraud-сообщение с некорректным JSON");
            Reject(eventArgs.DeliveryTag, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Ошибка обработки antifraud-сообщения {DeliveryTag}", eventArgs.DeliveryTag);
            Reject(eventArgs.DeliveryTag, true);
        }
    }

    private async Task SaveDeadLetterAsync(
        string type,
        ClientStatusChanged payload,
        DateTime occurredAt,
        Meta meta,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankAccountsDbContext>();
        db.InboxDeadLetters.Add(new InboxDeadLetter
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            OccurredAt = occurredAt,
            Meta = meta
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool TryReadMessage(
        JsonElement root,
        out Guid eventId,
        out DateTime occurredAt,
        out ClientStatusChanged payload,
        out Meta meta)
    {
        eventId = Guid.Empty;
        occurredAt = default;
        payload = null!;
        meta = null!;

        if (!root.TryGetProperty("eventId", out var eventIdElement) ||
            !eventIdElement.TryGetGuid(out eventId) ||
            !root.TryGetProperty("occurredAt", out var occurredAtElement) ||
            !occurredAtElement.TryGetDateTime(out occurredAt) ||
            !root.TryGetProperty("payload", out var payloadElement) ||
            !root.TryGetProperty("meta", out var metaElement))
        {
            return false;
        }

        payload = payloadElement.Deserialize<ClientStatusChanged>(SerializerOptions)!;
        meta = metaElement.Deserialize<Meta>(SerializerOptions)!;

        return payload is not null &&
               payload.OwnerId != Guid.Empty &&
               !string.IsNullOrWhiteSpace(payload.Status) &&
               meta is not null &&
               !string.IsNullOrWhiteSpace(meta.Version);
    }

    private static string? GetEventType(string status)
    {
        if (status.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            return "ClientBlocked";
        }

        if (status.Equals("Unblocked", StringComparison.OrdinalIgnoreCase))
        {
            return "ClientUnblocked";
        }

        return null;
    }

    private static string? ReadHeader(IBasicProperties properties, string name)
    {
        if (properties.Headers?.TryGetValue(name, out var value) != true)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => value?.ToString()
        };
    }

    private void Acknowledge(ulong deliveryTag)
    {
        _channel?.BasicAck(deliveryTag, false);
    }

    private void Reject(ulong deliveryTag, bool requeue)
    {
        _channel?.BasicNack(deliveryTag, false, requeue);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
