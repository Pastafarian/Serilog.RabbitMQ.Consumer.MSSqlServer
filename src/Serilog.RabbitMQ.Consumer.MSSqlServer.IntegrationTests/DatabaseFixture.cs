﻿extern alias ConsumerAlias;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit.Sdk;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests;
using static System.FormattableString;

public class DatabaseFixture : IDisposable
{
    //private static readonly string DropLogEventsDatabase = Invariant($"""

    //                                                                  ALTER DATABASE [{Database}]
    //                                                                  SET SINGLE_USER
    //                                                                  WITH ROLLBACK IMMEDIATE
    //                                                                  DROP DATABASE [{Database}]

    //                                                                  """);

    //private const string CreateLogEventsDatabase = """

    //                                               EXEC ('CREATE DATABASE [{0}] ON PRIMARY
    //                                               	(NAME = [{0}],
    //                                               	FILENAME =''{1}'',
    //                                               	SIZE = 25MB,
    //                                               	MAXSIZE = 50MB,
    //                                               	FILEGROWTH = 5MB )')
    //                                               """;
    private static readonly string DatabaseFileNameQuery = Invariant($"SELECT CONVERT(VARCHAR(255), SERVERPROPERTY('instancedefaultdatapath')) + '{Database}.mdf' AS Name");
    public static string Database => MsSqlBuilder.DefaultDatabase;
    public static string LogTableName => "LogEvents";
    public HttpClient? ProducerHttpClient;
    public HttpClient? ConsumerHttpClient;
    public MsSqlContainer MsSqlContainer;
    public RabbitMqContainer RabbitMqContainer;
    private ConsumerWebApplicationFactory factory;
    private static string LogEventsConnectionString { get; set; }
    public DatabaseFixture()
    {
        MsSqlContainer = new MsSqlBuilder().WithName("sql-server-2022").WithPortBinding(5500, 5500).WithPassword("Moo12345!@").Build();
        RabbitMqContainer = new RabbitMqBuilder().WithPassword("guest").WithUsername("guest").WithName("RabbitMqContainer").WithPortBinding(5672, 5672).WithCleanUp(true).Build();
        MsSqlContainer.StartAsync().GetAwaiter().GetResult();
        RabbitMqContainer.StartAsync().GetAwaiter().GetResult();
        LogEventsConnectionString = MsSqlContainer.GetConnectionString();
        //
        CreateDatabase();
        BuildProducerHttpClient();
    }

    public void BuildConsumerHttpClient(Func<IServiceCollection, bool>? registerCustomIocForConsumer = null)
    {
        factory = new ConsumerWebApplicationFactory(MsSqlContainer.GetConnectionString(), service =>
        {
            service.RemoveAll(typeof(ConnectionString));

            service.TryAddTransient(_ => new ConnectionString(MsSqlContainer.GetConnectionString(), Database));

            service.RemoveAll(typeof(MSSqlServerSinkOptions));

            var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;

            sinkOptions.TableName = LogTableName;
            sinkOptions.AutoCreateSqlTable = true;

            service.TryAddTransient<IFormatProvider>(_ => null!);

            service.TryAddTransient(_ => sinkOptions);

            registerCustomIocForConsumer?.Invoke(service);

            return true;
        });
        ConsumerHttpClient = factory.CreateClient();
    }
    public void KillApplication()
    {
        factory.Dispose();
    }

    public void BuildProducerHttpClient()
    {
        var factory = new ProducerWebApplicationFactory(MsSqlContainer.GetConnectionString(), new NullMessageSink());
        ProducerHttpClient = factory.CreateClient();
    }

    public static void DeleteDatabase()
    {
        using (var conn = new SqlConnection(LogEventsConnectionString))
        {
            conn.Open();
            var databases = conn.Query("select name from sys.databases");
            conn.Query("DELETE FROM LogEvents");
            //if (databases.Any(d => d.name == Database)) conn.Execute(DropLogEventsDatabase);
        }
    }

    private static void CreateDatabase()
    {
        //using (var conn = new SqlConnection(LogEventsConnectionString))
        //{
        //    conn.Open();
        //    // ReSharper disable once PossibleNullReferenceException
        //    var filename = conn.Query<FileName>(DatabaseFileNameQuery).FirstOrDefault().Name;
        //    var createDatabase = string.Format(CultureInfo.InvariantCulture, CreateLogEventsDatabase, Database, filename);

        //    conn.Execute(createDatabase);
        //}
    }

    public void Dispose()
    {
        DeleteDatabase();
        MsSqlContainer.DisposeAsync().GetAwaiter().GetResult();
        RabbitMqContainer.DisposeAsync().GetAwaiter().GetResult();
    }
}