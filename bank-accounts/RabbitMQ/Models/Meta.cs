namespace bank_accounts.RabbitMQ.Models;

public class Meta
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Version { get; init; } = "v1";
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Source { get; init; } = "account-service";
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public Guid CausationId { get; init; } = Guid.NewGuid();
}