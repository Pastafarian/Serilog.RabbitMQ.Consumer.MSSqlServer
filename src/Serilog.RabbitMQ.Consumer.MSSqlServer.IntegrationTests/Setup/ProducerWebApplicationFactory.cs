extern alias ConsumerAlias;
extern alias ProducerAlias;
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
using ConnectionString = ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.Setup.ConnectionString;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

public class ProducerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _sqlConnectionString;
    public IMessageSink Output;

    public ProducerWebApplicationFactory(string sqlConnectionString, IMessageSink output)
    {
        _sqlConnectionString = sqlConnectionString;
        Output = output;
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(ConnectionString));
                services.AddScoped(_ => new ConnectionString(_sqlConnectionString, "Logs"));
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

        builder.UseEnvironment("producer");
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.producer.json")
                .Build();

            config.AddConfiguration(configuration);
        });

        return base.CreateHost(builder);
    }
}