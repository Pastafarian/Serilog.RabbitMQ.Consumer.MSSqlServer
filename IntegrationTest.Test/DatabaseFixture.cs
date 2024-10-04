using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using static System.FormattableString;
namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private static readonly string DropLogEventsDatabase = Invariant($@"
ALTER DATABASE [{Database}]
SET SINGLE_USER
WITH ROLLBACK IMMEDIATE
DROP DATABASE [{Database}]
");

        private const string CreateLogEventsDatabase = @"
EXEC ('CREATE DATABASE [{0}] ON PRIMARY 
	(NAME = [{0}], 
	FILENAME =''{1}'', 
	SIZE = 25MB, 
	MAXSIZE = 50MB, 
	FILEGROWTH = 5MB )')";
        private static readonly string _databaseFileNameQuery = Invariant($@"SELECT CONVERT(VARCHAR(255), SERVERPROPERTY('instancedefaultdatapath')) + '{Database}.mdf' AS Name");
        public static string Database => MsSqlBuilder.DefaultDatabase;
        public static string LogTableName => "LogEvents";
        public static string LogEventsConnectionString { get; set; }
        public HttpClient? ProducerHttpClient;
        public HttpClient? ConsumerHttpClient;
        public MsSqlContainer MsSqlContainer;
        public RabbitMqContainer RabbitMqContainer;


        public void BuildTestContainers()
        {
            MsSqlContainer = new MsSqlBuilder().WithName("sql-server-2022").WithPortBinding(5500, 5500)
                .WithPassword("Moo12345!@").Build();

            RabbitMqContainer = new RabbitMqBuilder().WithPassword("guest").WithUsername("guest")
                .WithPortBinding(5672, 5672).WithName("rabbitmq").Build();

            CreateDatabase();
        }

        public async Task InitializeAsync()
        {
            BuildTestContainers();
            await MsSqlContainer.StartAsync();
            await RabbitMqContainer.StartAsync();
            LogEventsConnectionString = MsSqlContainer.GetConnectionString();
            //BuildProducerHttpClient();
        }

        //public void BuildConsumerHttpClient(Func<IServiceCollection, bool>? registerCustomIocForConsumer = null)
        //{
        //    var factory = new ConsumerWebApplicationFactory(MsSqlContainer.GetConnectionString(), service =>
        //    {
        //        service.RemoveAll(typeof(ConnectionString));

        //        service.TryAddTransient(_ =>
        //            new ConnectionString(MsSqlContainer.GetConnectionString(), Database));

        //        service.RemoveAll(typeof(MSSqlServerSinkOptions));

        //        var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;

        //        sinkOptions.TableName = LogTableName;
        //        sinkOptions.AutoCreateSqlTable = true;

        //        service.TryAddTransient(_ => sinkOptions);

        //        registerCustomIocForConsumer?.Invoke(service);

        //        return true;
        //    });
        //    ConsumerHttpClient = factory.CreateClient();
        //}

        public void BuildProducerHttpClient()
        {
            //var factory =
            //    new ProducerWebApplicationFactory(MsSqlContainer.GetConnectionString(), new NullMessageSink());
            //ProducerHttpClient = factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            Debug.Print("DisposeAsync being called");
            await MsSqlContainer.DisposeAsync();
            await RabbitMqContainer.DisposeAsync();
        }

        public static void DropTable(string tableName = null)
        {
            using (var conn = new SqlConnection(LogEventsConnectionString))
            {
                var actualTableName = string.IsNullOrEmpty(tableName) ? LogTableName : tableName;
                conn.Execute(Invariant($"IF OBJECT_ID('{actualTableName}', 'U') IS NOT NULL DROP TABLE {actualTableName};"));
            }
        }

        public static void DeleteTableContents()
        {
            using (var conn = new SqlConnection(LogEventsConnectionString))
            {
                conn.Open();
                var databases = conn.Query("DELETE FROM LogEvents");

                if (databases.Any(d => d.name == Database)) conn.Execute(DropLogEventsDatabase);
            }
            Debug.Print("DeleteTableContents");
        }

        private static void CreateDatabase()
        {
            //DeleteTableContents();

            //using (var conn = new SqlConnection(LogEventsConnectionString))
            //{
            //    conn.Open();
            //    // ReSharper disable once PossibleNullReferenceException
            //    var filename = conn.Query<FileName>(_databaseFileNameQuery).FirstOrDefault().Name;
            //    var createDatabase = string.Format(CultureInfo.InvariantCulture, CreateLogEventsDatabase, Database, filename);

            //    conn.Execute(createDatabase);
            //}
        }
    }
}