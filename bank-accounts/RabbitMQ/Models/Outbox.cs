namespace bank_accounts.RabbitMQ.Models;

public class Outbox
{
    public Guid Id { get; init; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Type { get; init; } = null!;
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Payload { get; init; } = null!;
    public DateTime OccurredAt { get; init; }
    public DateTime? PublishedAt { get; set; }
    public Meta Meta { get; init; } = new();
}