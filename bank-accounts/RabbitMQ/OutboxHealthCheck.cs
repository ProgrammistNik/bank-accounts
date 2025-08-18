using bank_accounts.Account.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace bank_accounts.RabbitMQ;

public class OutboxHealthCheck(BankAccountsDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var pendingMessages = await dbContext.Outboxes.CountAsync(m => m.PublishedAt == null, cancellationToken);
        
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (pendingMessages > 1000)
        {
            return HealthCheckResult.Unhealthy(
                $"Too many pending outbox messages: {pendingMessages}");
        }
        if (pendingMessages > 100)
        {
            return HealthCheckResult.Degraded(
                $"Outbox lag detected: {pendingMessages} messages not published yet");
        }

        return HealthCheckResult.Healthy(
            $"Outbox pending messages: {pendingMessages}");
    }
}