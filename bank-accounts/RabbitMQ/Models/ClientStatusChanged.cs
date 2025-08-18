namespace bank_accounts.RabbitMQ.Models;

public class ClientStatusChanged
{
    public Guid OwnerId { get; init; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Status { get; init; } = string.Empty;
}