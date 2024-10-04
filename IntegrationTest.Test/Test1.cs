using System.Data;
using System.Diagnostics;
using Dapper;
using FluentAssertions;
using Marten;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Loggers.RabbitMQ;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.ColumnOptions;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace IntegrationTest.Test
{
    //[Collection(nameof(DatabaseCollection))]
    public class Test1 : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _databaseFixture;

        public Test1(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
        }

        [Fact]
        public async Task TheTest1()
        {
            // Arrange
            var connectionString = _databaseFixture.MsSqlContainer.GetConnectionString();
            DatabaseFixture.LogEventsConnectionString = connectionString;
            const string additionalColumnName1 = "AdditionalColumn1";
            const string additionalColumnName2 = "AdditionalColumn2";
            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn>
            {
                new()
                {
                    ColumnName = additionalColumnName1,
                    PropertyName = additionalColumnName1,
                    DataType = SqlDbType.NVarChar,
                    AllowNull = true,
                    DataLength = 100
                },
                new()
                {
                    ColumnName = additionalColumnName2,
                    PropertyName = additionalColumnName2,
                    DataType = SqlDbType.Int,
                    AllowNull = true
                },
                new()
                {
                    ColumnName = "etupOne",
                    PropertyName = "Message",
                    DataType = SqlDbType.NVarChar,
                    AllowNull = true,
                    DataLength = 100
                }
            }
            };
            var messageTemplate = $"Hello {{{additionalColumnName1}}} from thread {{{additionalColumnName2}}}";
            var property1Value = "PropertyValue1";
            var expectedMessage = $"Hello \"{property1Value}\" from thread null";
            LoggingService? loggingService = null;
            var factory = new IntegrationTestWebApplicationFactory(service =>
            {
                service.RemoveAll(typeof(ColumnOptions));
                service.TryAddTransient(_ => columnOptions);
                service.RemoveAll(typeof(ConnectionString));

                service.TryAddTransient(_ =>
                    new ConnectionString(_databaseFixture.MsSqlContainer.GetConnectionString(), DatabaseFixture.Database));

                service.RemoveAll(typeof(MSSqlServerSinkOptions));

                var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;
                service.RemoveAll<ISinkDependencies>();
                service.TryAddTransient<ISinkDependencies>(_ =>
                    new SinkDependencies(
                        new ConnectionString(_databaseFixture.MsSqlContainer.GetConnectionString(),
                            DatabaseFixture.Database), sinkOptions, null, columnOptions));
                sinkOptions.TableName = DatabaseFixture.LogTableName;
                sinkOptions.AutoCreateSqlTable = true;
                sinkOptions.BatchPeriod = TimeSpan.FromSeconds(1);
                sinkOptions.EagerlyEmitFirstEvent = true;
                service.TryAddTransient(_ => sinkOptions);

                var serviceProvider = service.BuildServiceProvider();

                loggingService = serviceProvider
                    .GetServices<IHostedService>()
                    .OfType<LoggingService>()
                    .Single();

                return true;
            });
            // Act

            var client = factory.CreateClient();
            var result = await client.GetAsync("SetupTwo");
            IConfiguration loggerConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.producer.json", true, true)
                .Build();

            var logger = LoggerBuilder.BuildLogger(loggerConfiguration);
            logger.Information(messageTemplate, property1Value, null);
            await Task.Delay(5000);
            Debug.Print("Checkout log messages for TheTest1");
            VerifyLogMessageWasWritten(expectedMessage, "Message");
            VerifyStringColumnWritten(additionalColumnName1, property1Value);
            Debug.Print("DeleteTableContents TheTest1");
            DatabaseFixture.DeleteTableContents();
            Debug.Print("DeleteTableContents TheTest1 Finish");
        }

        [Fact]
        public async Task TheTest2()
        {
            // Arrange
            const string additionalColumn1Name = "AdditionalColumn1";
            const string additionalProperty1Name = "AdditionalProperty1";
            const string additionalColumn2Name = "AdditionalColumn2";
            const string additionalProperty2Name = "AdditionalProperty2";
            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn>
            {
                new()
                {
                    ColumnName = additionalColumn1Name,
                    PropertyName = additionalProperty1Name,
                    DataType = SqlDbType.NVarChar,
                    AllowNull = true,
                    DataLength = 100
                },
                new()
                {
                    ColumnName = additionalColumn2Name,
                    PropertyName = additionalProperty2Name,
                    DataType = SqlDbType.Int,
                    AllowNull = true
                }
            }
            };
            const string messageTemplate = $"Hello {{{additionalProperty1Name}}} from thread {{{additionalProperty2Name}}}";
            const string property1Value = "PropertyValue1";
            const int property2Value = 2;
            var expectedMessage = $"Hello \"{property1Value}\" from thread {property2Value}";
            string connectionString;
            var factory = new IntegrationTestWebApplicationFactory(service =>
            {

                service.RemoveAll(typeof(ILogEventDataGenerator));
                service.TryAddTransient<ILogEventDataGenerator>(services =>
                {
                    return new LogEventDataGenerator(columnOptions,
                        new StandardColumnDataGenerator(columnOptions, null, new XmlPropertyFormatter()),
                        new AdditionalColumnDataGenerator(new ColumnSimplePropertyValueResolver(),
                            new ColumnHierarchicalPropertyValueResolver()));
                });
                service.RemoveAll(typeof(ColumnOptions));
                service.TryAddTransient(_ => columnOptions);
                service.RemoveAll(typeof(ConnectionString));
                connectionString = _databaseFixture.MsSqlContainer.GetConnectionString();
                //DatabaseFixture.LogEventsConnectionString = connectionString;
                service.TryAddTransient(_ =>
                    new ConnectionString(_databaseFixture.MsSqlContainer.GetConnectionString(), DatabaseFixture.Database));

                service.RemoveAll(typeof(MSSqlServerSinkOptions));

                var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;
                service.RemoveAll<ISinkDependencies>();
                service.TryAddTransient<ISinkDependencies>(_ =>
                    new SinkDependencies(
                        new ConnectionString(_databaseFixture.MsSqlContainer.GetConnectionString(),
                            DatabaseFixture.Database), sinkOptions, null, columnOptions));
                sinkOptions.TableName = DatabaseFixture.LogTableName;
                sinkOptions.AutoCreateSqlTable = true;
                sinkOptions.BatchPeriod = TimeSpan.FromSeconds(1);
                sinkOptions.EagerlyEmitFirstEvent = true;
                service.TryAddTransient(_ => sinkOptions);

                return true;
            });
            // Act

            var client = factory.CreateClient();
            var result = await client.GetAsync("SetupOne");
            IConfiguration loggerConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.producer.json", true, true)
                .Build();

            var logger = LoggerBuilder.BuildLogger(loggerConfiguration);
            logger.Information(messageTemplate, property1Value, property2Value);
            await Task.Delay(5000);
            // Assert
            Debug.Print("Checkout log messages for TheTest2");
            VerifyLogMessageWasWritten(expectedMessage, "Message");
            VerifyStringColumnWritten(additionalColumn1Name, property1Value);
            VerifyIntegerColumnWritten(additionalColumn2Name, property2Value);
            Debug.Print("DeleteTableContents TheTest2");
            DatabaseFixture.DeleteTableContents();
            Debug.Print("DeleteTableContents TheTest2 Finish");

        }

        protected void VerifyStringColumnWritten(string columnName, string expectedValue)
        {
            using (var conn = new SqlConnection(_databaseFixture.MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<string>($"SELECT {columnName} FROM {DatabaseFixture.LogTableName}");

                if (!logEvents.Any())
                {
                    Debug.Print("No log messages found");
                    throw new InvalidOperationException(
                        $"No log events found in table {DatabaseFixture.LogTableName}, Column name: {columnName}, expectedValue: {expectedValue}. Actual: {(logEvents.IsEmpty() ? "IsEmpty" : "NotEmpty")}");
                }

                if (logEvents.Contains(expectedValue) == false)
                {

                    throw new InvalidOperationException(
                        $"No matching log events found in table {DatabaseFixture.LogTableName}, Column name: {columnName}, expectedValue: {expectedValue}. Actual:{logEvents.Count()} - '{logEvents.First()}' {string.Join(",", logEvents)}");
                }

                logEvents.Should().Contain(c => c == expectedValue);
            }
        }

        protected void VerifyLogMessageWasWritten(string expectedMessage, string messageColumnName = "Name")
        {
            VerifyStringColumnWritten(messageColumnName, expectedMessage);
        }

        protected void VerifyIntegerColumnWritten(string columnName, int expectedValue)
        {
            using (var conn = new SqlConnection(_databaseFixture.MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<int>($"SELECT {columnName} FROM {DatabaseFixture.LogTableName}");

                logEvents.Should().Contain(c => c == expectedValue);
            }
        }
    }
}
