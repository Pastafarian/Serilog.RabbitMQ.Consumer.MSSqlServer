extern alias ConsumerAlias;
extern alias ProducerAlias;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProducerAlias::Serilog.Producer.RabbitMq.Example;
using Xunit.Abstractions;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

public class ProducerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly ProducerAndConsumerFixture _rabbitMqConnectionString;
    public IMessageSink Output;

    public ProducerWebApplicationFactory(ProducerAndConsumerFixture fixture, IMessageSink output)
    {
        _rabbitMqConnectionString = fixture;
        Output = output;
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(ISqlConnectionStringBuilderWrapper));
                services.AddScoped<ISqlConnectionStringBuilderWrapper>(_ => new SqlConnectionStringBuilderWrapper(_rabbitMqConnectionString.MsSqlContainer.GetConnectionString(), false));
            }
        );

        builder.ConfigureLogging(logging =>
        {
            using var loggerFactory = new LoggerFactory();
            loggerFactory.AddXUnit(Output, c => c.MessageSinkMessageFactory = m => new PrintableDiagnosticMessage(m))
                .CreateLogger<EndToEndTests>();
            logging.AddXUnit(Output);
            logging.AddSerilog();
            logging.ClearProviders(); // Remove other loggers
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(Output)
                .CreateLogger()
                .ForContext<ProducerWebApplicationFactory>();
        });

        builder.ConfigureAppConfiguration(config =>
        {
            //config.Sources.Clear(); <-- removed this line right here
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.producer.json")
                .Build();
            config.AddConfiguration(configuration);

        });

        builder.UseEnvironment("producer");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            ////config.Sources.Clear(); <-- removed this line right here
            //var configuration = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.producer.json")
            //    .Build();
            //config.AddConfiguration(configuration);

        });

        return base.CreateHost(builder);
    }

}