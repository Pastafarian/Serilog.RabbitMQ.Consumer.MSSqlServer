extern alias ConsumerAlias;
using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;
using Testcontainers.MsSql;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.TestUtils
{
    [Collection("DatabaseTests")]
    public abstract class DatabaseTestsBase : TestContainersBase//, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly bool _disposedValue;

        protected DatabaseTestsBase(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));

            Serilog.Debugging.SelfLog.Enable(_output.WriteLine);
        }

        protected async Task InitializeAsync(Func<IServiceCollection, bool>? registerCustomIocForConsumer = null)
        {


            var producerWebApplicationFactory = new ProducerWebApplicationFactory(MsSqlContainer.GetConnectionString(), new NullMessageSink());
            ProducerHttpClient = producerWebApplicationFactory.CreateClient();
            ConsumerHttpClient?.Dispose();
            var consumerWebApplicationFactory = new ConsumerWebApplicationFactory(MsSqlContainer.GetConnectionString(), registerCustomIocForConsumer);
            ConsumerHttpClient = consumerWebApplicationFactory.CreateClient();
        }

        protected async Task DisposeAsync()
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                await conn.ExecuteAsync($@"DROP TABLE {DatabaseFixture.LogTableName}");
            }
        }
        //protected static void VerifyDatabaseColumnsWereCreated(IEnumerable<string> columnNames)
        //{
        //    if (columnNames == null)
        //    {
        //        return;
        //    }

        //    using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
        //    {
        //        var logEvents = conn.Query<InfoSchema>($@"SELECT COLUMN_NAME AS ColumnName FROM {DatabaseFixture.Database}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{DatabaseFixture.LogTableName}'");
        //        var infoSchema = logEvents as InfoSchema[] ?? logEvents.ToArray();

        //        foreach (var column in columnNames)
        //        {
        //            infoSchema.Should().Contain(columns => columns.ColumnName == column);
        //        }

        //        infoSchema.Should().Contain(columns => columns.ColumnName == "Id");
        //    }
        //}

        //protected static void VerifyDatabaseColumnsWereCreated(IEnumerable<SqlColumn> columnDefinitions)
        //{
        //    if (columnDefinitions == null)
        //    {
        //        return;
        //    }

        //    using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
        //    {
        //        var logEvents = conn.Query<InfoSchema>($@"SELECT COLUMN_NAME AS ColumnName, UPPER(DATA_TYPE) as DataType, CHARACTER_MAXIMUM_LENGTH as DataLength, IS_NULLABLE as AllowNull
        //            FROM {DatabaseFixture.Database}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{DatabaseFixture.LogTableName}'");
        //        var infoSchema = logEvents as InfoSchema[] ?? logEvents.ToArray();

        //        foreach (var definition in columnDefinitions)
        //        {
        //            var column = infoSchema.SingleOrDefault(c => c.ColumnName == definition.ColumnName);
        //            Assert.NotNull(column);
        //            var definitionDataType = definition.DataType.ToString().ToUpperInvariant();
        //            Assert.Equal(definitionDataType, column.DataType);
        //            if (definitionDataType == "NVARCHAR" || definitionDataType == "VARCHAR")
        //            {
        //                Assert.Equal(definition.DataLength.ToString(CultureInfo.InvariantCulture), column.DataLength);
        //            }
        //            if (definition.AllowNull)
        //            {
        //                Assert.Equal("YES", column.AllowNull);
        //            }
        //            else
        //            {
        //                Assert.Equal("NO", column.AllowNull);
        //            }
        //        }
        //    }
        //}

        protected static void VerifyIdColumnWasCreatedAndHasIdentity(string idColumnName = "Id")
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<InfoSchema>($@"SELECT COLUMN_NAME AS ColumnName FROM {MsSqlBuilder.DefaultDatabase}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{DatabaseFixture.LogTableName}'");
                var infoSchema = logEvents as InfoSchema[] ?? logEvents.ToArray();

                infoSchema.Should().Contain(columns => columns.ColumnName == idColumnName);

                var isIdentity = conn.Query<IdentityQuery>($"SELECT COLUMNPROPERTY(object_id('{DatabaseFixture.LogTableName}'), '{idColumnName}', 'IsIdentity') AS IsIdentity");

                isIdentity.Should().Contain(i => i.IsIdentity == 1);
            }
        }

        protected static void VerifyLogMessageWasWritten(string expectedMessage, string messageColumnName = "Message")
        {
            VerifyStringColumnWritten(messageColumnName, expectedMessage);
        }

        protected static void VerifyCustomLogMessageWasWritten(string expectedMessage)
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<CustomStandardLogColumns>($"SELECT CustomMessage FROM {DatabaseFixture.LogTableName}");

                logEvents.Should().Contain(e => e.CustomMessage == expectedMessage);
            }
        }

        protected static void VerifyStringColumnWritten(string columnName, string expectedValue)
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<string>($"SELECT {columnName} FROM {DatabaseFixture.LogTableName}");

                logEvents.Should().Contain(c => c == expectedValue);
            }
        }

        protected static void VerifyStringColumnMultipleValuesWrittenAndNotWritten(
            string columnName,
            List<string> valuesWritten,
            List<string> valuesNotWritten)
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<string>($"SELECT {columnName} FROM {DatabaseFixture.LogTableName}");

                valuesWritten?.ForEach(v => logEvents.Should().Contain(v));
                valuesNotWritten?.ForEach(v => logEvents.Should().NotContain(v));
            }
        }

        protected static void VerifyIntegerColumnWritten(string columnName, int expectedValue)
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<int>($"SELECT {columnName} FROM {DatabaseFixture.LogTableName}");

                logEvents.Should().Contain(c => c == expectedValue);
            }
        }

        //protected static void VerifyColumnStoreIndex()
        //{
        //    using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
        //    {
        //        conn.Execute($"use {DatabaseFixture.Database}");
        //        var query = conn.Query<SysIndex_CCI>("select name from sys.indexes where type = 5");
        //        var results = query as SysIndex_CCI[] ?? query.ToArray();

        //        results.Should().Contain(x => x.name == $"CCI_{DatabaseFixture.LogTableName}");
        //    }
        //}

        protected static void VerifyCustomQuery<TColumnDefinition>(string query, Action<IEnumerable<TColumnDefinition>> validationAction)
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<TColumnDefinition>(query);
                validationAction?.Invoke(logEvents);
            }
        }

        protected static void CreateTrigger(string logTriggerTableName, string logTriggerName)
        {
            using (var conn = new SqlConnection(MsSqlContainer.GetConnectionString()))
            {
                conn.Execute($"CREATE TABLE {logTriggerTableName} ([Id] [UNIQUEIDENTIFIER] NOT NULL, [Data] [NVARCHAR](50) NOT NULL)");
                conn.Execute($@"
CREATE TRIGGER {logTriggerName} ON {DatabaseFixture.LogTableName} 
AFTER INSERT 
AS
BEGIN 
INSERT INTO {logTriggerTableName} VALUES (NEWID(), 'Data') 
END");
            }
        }

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (!_disposedValue)
        //    {
        //        DatabaseFixture.DropTable();
        //        _disposedValue = true;
        //    }
        //}

        //public void Dispose()
        //{
        //    Dispose(disposing: true);
        //    GC.SuppressFinalize(this);
        //}
    }
}
