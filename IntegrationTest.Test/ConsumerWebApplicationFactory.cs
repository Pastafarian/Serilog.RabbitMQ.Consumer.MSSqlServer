using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Serilog;

namespace IntegrationTest.Test
{
    internal class ConsumerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly Func<IServiceCollection, bool>? _registerCustomIoc;
        public ConsumerWebApplicationFactory(Func<IServiceCollection, bool>? registerCustomIoc)
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
                    .CreateLogger<ConsumerWebApplicationFactory>();
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
}