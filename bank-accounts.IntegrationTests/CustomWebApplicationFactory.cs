using bank_accounts.RabbitMQ;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace bank_accounts.IntegrationTests;

// ReSharper disable once ClassNeverInstantiated.Global
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15")
        .WithDatabase("db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilMessageIsLogged("Server startup complete"))
        .Build();
    
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
    
    public async Task StopRabbitAsync() => await _rabbitMqContainer.StopAsync();
    public async Task StartRabbitAsync() => await _rabbitMqContainer.StartAsync();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var rabbitMqPort = _rabbitMqContainer.GetMappedPublicPort(5672);

            var rabbitHostAndPort = $"localhost:{rabbitMqPort}";

            var inMemorySettings = new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                ["RabbitMQ:HostName"] = "localhost",
                ["RabbitMQ:Port"] = rabbitMqPort.ToString(),
                ["RabbitMQ:UserName"] = "guest",
                ["RabbitMQ:Password"] = "guest",
                ["RabbitMQ:HostAndPort"] = rabbitHostAndPort
            };

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            config.AddInMemoryCollection(inMemorySettings);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                   .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            var hostedServiceDescriptor = services.SingleOrDefault(d => d.ImplementationType == typeof(AntifraudConsumer));
            if (hostedServiceDescriptor != null)
            {
                services.Remove(hostedServiceDescriptor);
            }
            
        });
    }
}