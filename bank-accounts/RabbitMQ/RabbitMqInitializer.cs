using RabbitMQ.Client;

namespace bank_accounts.RabbitMQ;

public class RabbitMqInitializer(IConfiguration configuration)
{

    public void Initialize()
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"],
            UserName = configuration["RabbitMq:UserName"],
            Password = configuration["RabbitMq:Password"],
            Port = configuration.GetValue<int>("RabbitMq:Port")
        };

        var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        
        channel.ExchangeDeclare(
            exchange: "account.events",
            type: ExchangeType.Topic,
            durable: true
        );

        channel.QueueDeclare("account.crm", durable: true);
        channel.QueueDeclare("account.notifications", durable: true);
        channel.QueueDeclare("account.antifraud", durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare("account.audit", durable: true);

        channel.QueueBind("account.crm", "account.events", "account.*");
        channel.QueueBind("account.notifications", "account.events", "money.*");
        channel.QueueBind("account.antifraud", "account.events", "client.*");
        channel.QueueBind("account.audit", "account.events", "#");
    }
}