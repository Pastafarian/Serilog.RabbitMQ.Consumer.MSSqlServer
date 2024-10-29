extern alias ConsumerAlias;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.ColumnOptions;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Loggers.RabbitMQ;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;
using LoggingService = ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers.LoggingService;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests
{
    using Program = ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.Program;

    public class BasicWebApplication : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection> _configureServices;

        public BasicWebApplication(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices((services =>
            {
                _configureServices(services);

            }));
        }
    }

    //   [Collection("Database collection")]
    [Trait(TestCategory.TraitName, TestCategory.Integration)]
    public class AdditionalPropertiesTests : IDisposable
    {
        private readonly DatabaseFixture _fixture;

        //private readonly ProducerAndConsumerFixture _fixture;

        public AdditionalPropertiesTests()
        {
            _fixture = new DatabaseFixture();
        }

        [Fact]
        public async Task WritesLogEventWithCustomNamedProperties()
        {
            // Arrange
            const string additionalColumn1Name = "AdditionalColumn1";
            const string additionalProperty1Name = "AdditionalProperty1";
            const string additionalColumn2Name = "AdditionalColumn2";
            const string additionalProperty2Name = "AdditionalProperty2";
            const string messageTemplate = $"Hello {{{additionalProperty1Name}}} from thread {{{additionalProperty2Name}}}";
            const string property1Value = "PropertyValue1";
            const int property2Value = 2;
            var expectedMessage = $"Hello \"{property1Value}\" from thread {property2Value}";

            var columnOptions = new ColumnOptions
            {
                AdditionalColumns =
                [
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
                ]
            };

            _fixture.BuildConsumerHttpClient(service =>
            {
                service.RemoveAll(typeof(LoggingService));
                service.AddHostedService<LoggingService>();
                service.TryAddTransient(_ => columnOptions);
                service.RemoveAll(typeof(ColumnOptions));
                service.TryAddTransient(_ => columnOptions);

                return true;
            });

            // Act
            IConfiguration loggerConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.int-test.json", true, true)
                .Build();

            var logger = LoggerBuilder.BuildLogger(loggerConfiguration);
            logger.Information(messageTemplate, property1Value, property2Value);
            await Task.Delay(3000);

            // Assert
            VerifyLogMessageWasWritten(expectedMessage, "Message");
            VerifyStringColumnWritten(additionalColumn1Name, property1Value);
            VerifyIntegerColumnWritten(additionalColumn2Name, property2Value);
            DatabaseFixture.DeleteDatabase();
        }

        //[Fact]
        //public async Task WritesLogEventWithColumnNamedProperties()
        //{
        //    // Arrange
        //    const string additionalColumn1Name = "AdditionalColumn1";
        //    const string additionalColumn2Name = "AdditionalColumn2";
        //    var columnOptions = new ColumnOptions
        //    {
        //        AdditionalColumns = new List<SqlColumn>
        //        {
        //            new()
        //            {
        //                ColumnName = additionalColumn1Name,
        //                PropertyName = "Foo",
        //                DataType = SqlDbType.NVarChar,
        //                AllowNull = true,
        //                DataLength = 100
        //            },
        //            new()
        //            {
        //                ColumnName = additionalColumn2Name,
        //                PropertyName = "Bar",
        //                DataType = SqlDbType.Int,
        //                AllowNull = true
        //            }
        //        }
        //    };
        //    string connectionString;
        //    await InitializeAsync((service) =>
        //    {
        //        service.RemoveAll(typeof(ColumnOptions));
        //        service.TryAddTransient<ColumnOptions>((_) => columnOptions);

        //        service.RemoveAll(typeof(ConnectionString));
        //        connectionString = MsSqlContainer.GetConnectionString();
        //        DatabaseFixture.LogEventsConnectionString = connectionString;
        //        service.TryAddTransient<ConnectionString>((_) => new ConnectionString(MsSqlContainer.GetConnectionString(), DatabaseFixture.Database));

        //        service.RemoveAll(typeof(MSSqlServerSinkOptions));

        //        var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;

        //        sinkOptions.TableName = DatabaseFixture.LogTableName;
        //        sinkOptions.AutoCreateSqlTable = true;

        //        service.TryAddTransient((_) => sinkOptions);
        //        return true;
        //    });

        //    var messageTemplate = $"Hello {{{additionalColumn1Name}}} from thread {{{additionalColumn2Name}}}";
        //    var property1Value = "PropertyValue1";
        //    var property2Value = 2;
        //    var expectedMessage = $"Hello \"{property1Value}\" from thread {property2Value}";

        //    IConfiguration loggerConfiguration = new ConfigurationBuilder()
        //        .SetBasePath(Directory.GetCurrentDirectory())
        //        .AddJsonFile("appsettings.int-test.json", true, true)
        //        .Build();

        //    var logger = LoggerBuilder.BuildLogger(loggerConfiguration);

        //    logger.Error(messageTemplate, property1Value, property2Value);
        //    await Task.Delay(9000);

        //    // Assert
        //    VerifyDatabaseColumnsWereCreated(columnOptions.AdditionalColumns);
        //    VerifyLogMessageWasWritten(expectedMessage);
        //    VerifyStringColumnWritten(additionalColumn1Name, property1Value);
        //    VerifyIntegerColumnWritten(additionalColumn2Name, property2Value);

        //    await DisposeAsync();
        //    ProducerHttpClient?.Dispose();
        //    ConsumerHttpClient?.Dispose();
        //    await Task.Delay(9000);
        //}

        [Trait("Bugfix", "#458")]
        [Fact]
        public async Task WritesLogEventWithNullValueForNullableColumn()
        {
            // Arrange
            const string additionalColumnName1 = "AdditionalColumn1";
            const string additionalColumnName2 = "AdditionalColumn2";
            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn>
                {
                    new()
                    {
                        ColumnName = additionalColumnName1,
                        DataType = SqlDbType.NVarChar,
                        AllowNull = true,
                        DataLength = 100
                    },
                    new()
                    {
                        ColumnName = additionalColumnName2,
                        DataType = SqlDbType.Int,
                        AllowNull = true
                    }
                }
            };
            var messageTemplate = $"Hello {{{additionalColumnName1}}} from thread {{{additionalColumnName2}}}";
            var property1Value = "PropertyValue1";
            var expectedMessage = $"Hello \"{property1Value}\" from thread null";
            _fixture.BuildConsumerHttpClient(service =>
            {
                service.RemoveAll(typeof(LoggingService));
                service.AddHostedService<LoggingService>();
                service.RemoveAll(typeof(ColumnOptions));
                service.TryAddTransient(_ => columnOptions);

                return true;
            });

            // Act
            IConfiguration loggerConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.int-test.json", true, true)
                .Build();

            var logger = LoggerBuilder.BuildLogger(loggerConfiguration);
            logger.Information(messageTemplate, property1Value, null);
            await Task.Delay(3000);

            VerifyDatabaseColumnsWereCreated(columnOptions.AdditionalColumns);
            VerifyLogMessageWasWritten(expectedMessage);
            VerifyStringColumnWritten(additionalColumnName1, property1Value);

            Debug.Print($"Test 1 Done");
        }

        protected void VerifyDatabaseColumnsWereCreated(IEnumerable<SqlColumn> columnDefinitions)
        {
            if (columnDefinitions == null)
            {
                return;
            }

            using (var conn = new SqlConnection(_fixture.MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<InfoSchema>(
                    $@"SELECT COLUMN_NAME AS ColumnName, UPPER(DATA_TYPE) as DataType, CHARACTER_MAXIMUM_LENGTH as DataLength, IS_NULLABLE as AllowNull
                    FROM {DatabaseFixture.Database}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{DatabaseFixture.LogTableName}'");
                var infoSchema = logEvents as InfoSchema[] ?? logEvents.ToArray();

                foreach (var definition in columnDefinitions)
                {
                    var column = infoSchema.SingleOrDefault(c => c.ColumnName == definition.ColumnName);
                    Assert.NotNull(column);
                    var definitionDataType = definition.DataType.ToString().ToUpperInvariant();
                    Assert.Equal(definitionDataType, column.DataType);
                    if (definitionDataType == "NVARCHAR" || definitionDataType == "VARCHAR")
                    {
                        Assert.Equal(definition.DataLength.ToString(CultureInfo.InvariantCulture), column.DataLength);
                    }

                    if (definition.AllowNull)
                    {
                        Assert.Equal("YES", column.AllowNull);
                    }
                    else
                    {
                        Assert.Equal("NO", column.AllowNull);
                    }
                }
            }
        }

        protected void VerifyStringColumnWritten(string columnName, string expectedValue)
        {
            var connectionString = _fixture.MsSqlContainer.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                Debug.Print($"VerifyStringColumnWritten connection string {connectionString}");
                var logEvents = conn.Query<string>($"SELECT {columnName} FROM {DatabaseFixture.LogTableName}");

                logEvents.Should().Contain(c => c == expectedValue);
            }
        }

        protected void VerifyLogMessageWasWritten(string expectedMessage, string messageColumnName = "Message")
        {
            VerifyStringColumnWritten(messageColumnName, expectedMessage);
        }

        protected void VerifyIntegerColumnWritten(string columnName, int expectedValue)
        {
            using (var conn = new SqlConnection(_fixture.MsSqlContainer.GetConnectionString()))
            {
                var logEvents = conn.Query<int>($"SELECT {columnName} FROM {DatabaseFixture.LogTableName}");

                logEvents.Should().Contain(c => c == expectedValue);
            }
        }

        //[Fact]
        //public void WritesLogEventWithColumnsFromHierarchicalNamedProperties()
        //{
        //    // Arrange
        //    const string additionalColumn1Name = "AdditionalColumn1";
        //    const string additionalProperty1Name = "AdditionalProperty1.SubProperty1";
        //    const string additionalColumn2Name = "AdditionalColumn2";
        //    const string additionalProperty2Name = "AdditionalProperty2.SubProperty2.SubSubProperty1";
        //    var columnOptions = new ColumnOptions
        //    {
        //        AdditionalColumns = new List<SqlColumn>
        //        {
        //            new SqlColumn
        //            {
        //                ColumnName = additionalColumn1Name,
        //                PropertyName = additionalProperty1Name,
        //                DataType = SqlDbType.NVarChar,
        //                AllowNull = true,
        //                DataLength = 100
        //            },
        //            new SqlColumn
        //            {
        //                ColumnName = additionalColumn2Name,
        //                PropertyName = additionalProperty2Name,
        //                DataType = SqlDbType.Int,
        //                AllowNull = true
        //            }
        //        }
        //    };
        //    var property1Value = "PropertyValue1";
        //    var property2Value = 2;

        //    // Act
        //    //Log.Logger = new LoggerConfiguration()
        //    //    .WriteTo.MSSqlServer(
        //    //        DatabaseFixture.LogEventsConnectionString,
        //    //        sinkOptions: new MSSqlServerSinkOptions
        //    //        {
        //    //            TableName = DatabaseFixture.LogTableName,
        //    //            AutoCreateSqlTable = true
        //    //        },
        //    //        columnOptions: columnOptions,
        //    //        formatProvider: CultureInfo.InvariantCulture)
        //    //    .CreateLogger();
        //    Log.Information("Hello {@AdditionalProperty1} from thread {@AdditionalProperty2}",
        //        new StructuredType
        //        {
        //            SubProperty1 = property1Value
        //        },
        //        new StructuredType
        //        {
        //            SubProperty2 = new StructuredSubType
        //            {
        //                SubSubProperty1 = property2Value
        //            }
        //        });
        //    Log.CloseAndFlush();

        //    // Assert
        //    VerifyDatabaseColumnsWereCreated(columnOptions.AdditionalColumns);
        //    VerifyStringColumnWritten(additionalColumn1Name, property1Value);
        //    VerifyIntegerColumnWritten(additionalColumn2Name, property2Value);
        //}
        //public void Dispose()
        //{
        //    _fixture.Dispose();
        //}
        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
