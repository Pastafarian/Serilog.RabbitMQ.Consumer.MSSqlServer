using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

extern alias ConsumerAlias;

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

//public sealed class ProducerAndConsumerFixture(IMessageSink messageSink) : IAsyncLifetime
//{
//    async Task IAsyncLifetime.InitializeAsync()
//    {
//        await MsSqlContainer.StartAsync();
//        await RabbitMqContainer.StartAsync();
//    }

//    async Task IAsyncLifetime.DisposeAsync()
//    {
//        await MsSqlContainer.DisposeAsync().AsTask();
//        await RabbitMqContainer.DisposeAsync().AsTask();
//    }
//}