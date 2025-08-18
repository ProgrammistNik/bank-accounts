namespace bank_accounts.RabbitMQ.Models;

public class InboxDeadLetter
{
    public Guid Id { get; init; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Type { get; init; } = null!;
    public ClientStatusChanged Payload { get; init; } = null!;
    public DateTime OccurredAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public Meta Meta { get; init; } = new();
}