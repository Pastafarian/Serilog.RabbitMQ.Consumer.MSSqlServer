using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit.Abstractions;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

public sealed class ProducerAndConsumerFixture(IMessageSink messageSink) : IAsyncLifetime
{
    public readonly MsSqlContainer MsSqlContainer
        = new MsSqlBuilder().WithName("sql-server-2022").WithPassword("Moo12345!@").Build();

    public readonly RabbitMqContainer RabbitMqContainer = new RabbitMqBuilder().WithPassword("guest").WithUsername("guest")
        .WithPortBinding(5672)
        .WithPortBinding(15672)
        .WithExposedPort(5672)
        .WithExposedPort(15672)
        .Build();

    public HttpClient? ProducerHttpClient;
    public HttpClient? ConsumerHttpClient;

    async Task IAsyncLifetime.InitializeAsync()
    {
        await MsSqlContainer.StartAsync();
        await RabbitMqContainer.StartAsync();

        var producerWebApplicationFactory = new ProducerWebApplicationFactory(this, messageSink);
        ProducerHttpClient = producerWebApplicationFactory.CreateClient();

        var consumerWebApplicationFactory = new ConsumerWebApplicationFactory(MsSqlContainer.GetConnectionString(), messageSink);
        ConsumerHttpClient = consumerWebApplicationFactory.CreateClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await MsSqlContainer.DisposeAsync().AsTask();
        await RabbitMqContainer.DisposeAsync().AsTask();
    }
}