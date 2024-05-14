using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit.Abstractions;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

public class TestContainersBase
{

    public HttpClient? ProducerHttpClient;
    public HttpClient? ConsumerHttpClient;

    public static readonly MsSqlContainer MsSqlContainer
        = new MsSqlBuilder().WithName("sql-server-2022").WithPortBinding(5500, 5500).WithPassword("Moo12345!@").Build();

    public static readonly RabbitMqContainer RabbitMqContainer = new RabbitMqBuilder().WithPassword("guest").WithUsername("guest")
        .WithPortBinding(5672)
        .WithPortBinding(15672)
        .WithExposedPort(5672)
        .WithExposedPort(15672)
        .Build();
}

public sealed class ProducerAndConsumerFixture(IMessageSink messageSink) : TestContainersBase, IAsyncLifetime
{
    async Task IAsyncLifetime.InitializeAsync()
    {
        await MsSqlContainer.StartAsync();
        await RabbitMqContainer.StartAsync();

        var producerWebApplicationFactory = new ProducerWebApplicationFactory(MsSqlContainer.GetConnectionString(), messageSink);
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