extern alias ConsumerAlias;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Program = ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.Program;


namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

public class ConsumerWebApplicationFactory(string connectionString, Func<IServiceCollection, bool>? registerCustomIoc) : WebApplicationFactory<Program>
{
    public Func<IServiceCollection, bool>? RegisterCustomIoc { get; } = registerCustomIoc;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(ConnectionString));
                services.AddScoped(_ => new ConnectionString(connectionString, "Logging"));
                if (RegisterCustomIoc != null)
                {
                    RegisterCustomIoc(services);
                }
            }
        );

        builder.ConfigureLogging(logging =>
        {
            using var loggerFactory = new LoggerFactory();
            logging.AddSerilog();
            logging.ClearProviders(); // Remove other loggers
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .CreateLogger()
                .ForContext<ConsumerWebApplicationFactory>();
        });

        builder.UseEnvironment("consumer");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.consumer.json")
                .Build();

            config.AddConfiguration(configuration);
        });

        return base.CreateHost(builder);
    }

}