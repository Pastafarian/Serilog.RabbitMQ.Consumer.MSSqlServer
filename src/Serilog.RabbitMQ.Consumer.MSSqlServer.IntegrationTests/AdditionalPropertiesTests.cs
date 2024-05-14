extern alias ConsumerAlias;
using System.Data;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.ColumnOptions;
using ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Loggers.RabbitMQ;
using Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;
using Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.TestUtils;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;
using Xunit.Abstractions;
using MsSqlServerSinkOptionsProvider = ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.Setup.MsSqlServerSinkOptionsProvider;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests
{
    [Trait(TestCategory.TraitName, TestCategory.Integration)]
    public class AdditionalPropertiesTests : DatabaseTestsBase
    {
        private readonly ProducerAndConsumerFixture _fixture;

        public AdditionalPropertiesTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task WritesLogEventWithCustomNamedProperties()
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

            var messageTemplate = $"Hello {{{additionalProperty1Name}}} from thread {{{additionalProperty2Name}}}";
            var property1Value = "PropertyValue1";
            var property2Value = 2;
            var expectedMessage = $"Hello \"{property1Value}\" from thread {property2Value}";

            string connectionString;
            await InitializeAsync((service) =>
            {
                service.RemoveAll(typeof(ColumnOptions));
                service.TryAddTransient<ColumnOptions>((_) => columnOptions);

                service.RemoveAll(typeof(ConnectionString));
                connectionString = MsSqlContainer.GetConnectionString();
                DatabaseFixture.LogEventsConnectionString = connectionString;
                service.TryAddTransient<ConnectionString>((_) => new ConnectionString(MsSqlContainer.GetConnectionString(), DatabaseFixture.Database));

                service.RemoveAll(typeof(MSSqlServerSinkOptions));

                var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;

                sinkOptions.TableName = DatabaseFixture.LogTableName;
                sinkOptions.AutoCreateSqlTable = true;

                service.TryAddTransient((_) => sinkOptions);
                return true;
            });

            // Act
            IConfiguration loggerConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.int-test.json", true, true)
                .Build();

            var logger = LoggerBuilder.BuildLogger(loggerConfiguration);
            logger.Information(messageTemplate, property1Value, property2Value);

            // Assert
            VerifyDatabaseColumnsWereCreated(columnOptions.AdditionalColumns);
            VerifyLogMessageWasWritten(expectedMessage);
            VerifyStringColumnWritten(additionalColumn1Name, property1Value);
            VerifyIntegerColumnWritten(additionalColumn2Name, property2Value);
            await DisposeAsync();
        }

        [Fact]
        public async Task WritesLogEventWithColumnNamedProperties()
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
            string connectionString;
            await InitializeAsync((service) =>
            {
                service.RemoveAll(typeof(ColumnOptions));
                service.TryAddTransient<ColumnOptions>((_) => columnOptions);

                service.RemoveAll(typeof(ConnectionString));
                connectionString = MsSqlContainer.GetConnectionString();
                DatabaseFixture.LogEventsConnectionString = connectionString;
                service.TryAddTransient<ConnectionString>((_) => new ConnectionString(MsSqlContainer.GetConnectionString(), DatabaseFixture.Database));

                service.RemoveAll(typeof(MSSqlServerSinkOptions));

                var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;

                sinkOptions.TableName = DatabaseFixture.LogTableName;
                sinkOptions.AutoCreateSqlTable = true;

                service.TryAddTransient((_) => sinkOptions);
                return true;
            });



            const string messageTemplate = $"Hello {{{additionalColumnName1}}} from thread {{{additionalColumnName2}}}";
            const string property1Value = "PropertyValue1";
            const int property2Value = 2;
            var expectedMessage = $"Hello \"{property1Value}\" from thread \"{property2Value}\"";
            IConfiguration loggerConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.int-test.json", true, true)
                .Build();

            var logger = LoggerBuilder.BuildLogger(loggerConfiguration);

            logger.Error(messageTemplate, property1Value, property2Value);
            await Task.Delay(5000);

            // Assert
            VerifyDatabaseColumnsWereCreated(columnOptions.AdditionalColumns);
            VerifyLogMessageWasWritten(expectedMessage);
            VerifyStringColumnWritten(additionalColumnName1, property1Value);
            VerifyIntegerColumnWritten(additionalColumnName2, property2Value);

            await DisposeAsync();
        }

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

            string connectionString;
            await InitializeAsync((service) =>
            {
                service.RemoveAll(typeof(ColumnOptions));
                service.TryAddTransient<ColumnOptions>((_) => columnOptions);

                service.RemoveAll(typeof(ConnectionString));
                connectionString = MsSqlContainer.GetConnectionString();
                DatabaseFixture.LogEventsConnectionString = connectionString;
                service.TryAddTransient<ConnectionString>((_) => new ConnectionString(MsSqlContainer.GetConnectionString(), DatabaseFixture.Database));

                service.RemoveAll(typeof(MSSqlServerSinkOptions));

                var sinkOptions = new MsSqlServerSinkOptionsProvider().MsSqlServerSinkOptions;

                sinkOptions.TableName = DatabaseFixture.LogTableName;
                sinkOptions.AutoCreateSqlTable = true;

                service.TryAddTransient((_) => sinkOptions);
                return true;
            });

            var messageTemplate = $"Hello {{{additionalColumnName1}}} from thread {{{additionalColumnName2}}}";
            var property1Value = "PropertyValue1";
            var expectedMessage = $"Hello \"{property1Value}\" from thread null";

            // Act
            IConfiguration loggerConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.int-test.json", true, true)
                .Build();

            var logger = LoggerBuilder.BuildLogger(loggerConfiguration);

            logger.Information(messageTemplate, property1Value, null);
            await Task.Delay(5000);

            // Assert
            VerifyDatabaseColumnsWereCreated(columnOptions.AdditionalColumns);
            VerifyLogMessageWasWritten(expectedMessage);
            VerifyStringColumnWritten(additionalColumnName1, property1Value);
            await DisposeAsync();
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
    }
}
