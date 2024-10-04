using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Serilog;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Serilog.RabbitMQ.Consumer.MSSqlServer.Program>
{
    private readonly Func<IServiceCollection, bool>? _registerCustomIoc;
    public IntegrationTestWebApplicationFactory(Func<IServiceCollection, bool>? registerCustomIoc)
    {
        _registerCustomIoc = registerCustomIoc;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            {
                if (_registerCustomIoc != null)
                {

                    _registerCustomIoc(services);
                }
            }
        );

        builder.ConfigureLogging(logging =>
        {
            using var loggerFactory = new LoggerFactory();
            loggerFactory
                .CreateLogger<IntegrationTestWebApplicationFactory>();
            logging.AddSerilog();
            logging.ClearProviders(); // Remove other loggers
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .CreateLogger()
                .ForContext<IntegrationTestWebApplicationFactory>();
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