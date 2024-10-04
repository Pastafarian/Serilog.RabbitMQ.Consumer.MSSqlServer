using System.Data;
using Microsoft.OpenApi.Models;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.ColumnOptions;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;
using Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Setup
{
    interface IColumnOptionsProvider
    {
        ColumnOptions ColumnOptions { get; }
    }

    public static class WebApplicationBuilderExtensions
    {


        public static WebApplicationBuilder SetupServices(this WebApplicationBuilder builder)
        {
            var rabbitMqConfig = builder.Configuration.GetSection("RabbitMq") ?? throw new InvalidOperationException("RabbitMq Configuration Null");

            var rabbitMqClientConsumerConfiguration = new RabbitMqClientConsumerConfiguration();

            rabbitMqConfig.Bind(rabbitMqClientConsumerConfiguration);
            builder.Services.Configure<List<string>>(_ =>
                rabbitMqClientConsumerConfiguration.Hostnames = rabbitMqConfig.GetSection("Hostnames").Get<List<string>>() ?? throw new InvalidOperationException("RabbitMq Configuration Null"));


            // Customise TimeStamp column name


            builder.Services.AddSingleton(rabbitMqClientConsumerConfiguration);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "API Name", Version = "v1" });
                options.DocumentFilter<HealthChecksFilter>();
            });

            var conStr = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(conStr))
            {
                throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'.");
            }

            builder.Services
                .AddHealthChecks()
                .AddRabbitMQ()
                .AddSqlServer(conStr);

            builder.Services.AddTransient(_ => new ConnectionString(conStr, "Logs"));


            builder.Services.AddTransient<IRabbitConnectionFactory, RabbitConnectionFactory>();
            builder.Services.AddTransient(s => s.GetService<IRabbitConnectionFactory>()!.GetConnectionFactory());
            builder.Services.AddTransient<IAsyncEventingBasicConsumerFactory, AsyncEventingBasicConsumerFactory>();
            builder.Services.AddTransient(_ => new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions);
            builder.Services.AddTransient<IStandardColumnDataGenerator, StandardColumnDataGenerator>();
            builder.Services.AddTransient<IXmlPropertyFormatter, XmlPropertyFormatter>();
            builder.Services.AddTransient<ISinkDependencies, SinkDependencies>();
            //builder.Services.AddTransient<SinkDependencies>();

            builder.Services.AddTransient(_ => new ColumnOptionsProvider().ColumnOptions);

            builder.Services.AddHostedService<LoggingService>();
            builder.Services.AddHostedService<AuditService>();

            return builder;
        }
    }

    public class ConnectionString
    {
        public string DefaultConnection { get; }
        public string DatabaseName { get; }

        public ConnectionString(string connectionString, string databaseName)
        {
            DatabaseName = databaseName;
            DefaultConnection = connectionString;
        }
    }



}

public class ColumnOptionsProvider
{
    public ColumnOptions ColumnOptions => new()
    {
        AdditionalColumns =
        [
            new ()
                {
                    ColumnName = "Thread", PropertyName = "ThreadId", DataType = SqlDbType.Int, AllowNull = true
                },
                new ()
                {
                    ColumnName = "Server", PropertyName = "MachineName", DataType = SqlDbType.NVarChar,
                    DataLength = 64,
                    AllowNull = true
                },
                new()
                {
                    ColumnName = "Logger",
                    PropertyName = "SourceContext",
                    DataType = SqlDbType.NVarChar,
                    DataLength = 64,
                    AllowNull = true
                },
                new()
                {
                    ColumnName = "RequestUrl",
                    PropertyName = "RequestUrl",
                    DataType = SqlDbType.NVarChar,
                    DataLength = 64,
                    AllowNull = true
                },
                new()
                {
                    ColumnName = "CorrelationId",
                    PropertyName = "CorrelationId",
                    DataType = SqlDbType.UniqueIdentifier,
                    AllowNull = true
                },
                new()
                {
                    ColumnName = "ExternalCorrelationId",
                    PropertyName = "ExternalCorrelationId",
                    DataType = SqlDbType.UniqueIdentifier,
                    DataLength = 64,
                    AllowNull = true
                },
                new()
                {
                    ColumnName = "PublishTimestamp",
                    PropertyName = "PublishTimestamp",
                    DataType = SqlDbType.DateTimeOffset,
                    AllowNull = true
                }
        ],
        TimeStamp = new ColumnOptions.TimeStampColumnOptions
        {
            ColumnName = "DequeueTimestamp",
            DataType = SqlDbType.DateTimeOffset

        }
    };
}


