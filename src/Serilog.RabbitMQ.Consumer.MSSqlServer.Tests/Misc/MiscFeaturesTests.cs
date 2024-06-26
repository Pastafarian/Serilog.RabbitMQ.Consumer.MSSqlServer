﻿//using System.Data;
//using System.Globalization;
//using FluentAssertions;
//using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
//using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.ColumnOptions;
//using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;
//using Xunit.Abstractions;
//using static System.FormattableString;

//namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Misc
//{
//    [Trait(TestCategory.TraitName, TestCategory.Integration)]
//    public class MiscFeaturesTests : DatabaseTestsBase
//    {
//        public MiscFeaturesTests(ITestOutputHelper output) : base(output)
//        {
//        }

//        [Fact]
//        public void LogEventExcludeAdditionalProperties()
//        {
//            // This test literally checks for the exclusion of *additional* properties,
//            // meaning custom properties which have their own column. This was the original
//            // meaning of the flag. Contrast with LogEventExcludeStandardProperties below.

//            // Arrange
//            var columnOptions = new ColumnOptions()
//            {
//                AdditionalColumns = new List<SqlColumn>
//                {
//                    new SqlColumn { DataType = SqlDbType.NVarChar, DataLength = 20, ColumnName = "B" }
//                }
//            };
//            columnOptions.Store.Remove(StandardColumn.Properties);
//            columnOptions.Store.Add(StandardColumn.LogEvent);
//            columnOptions.LogEvent.ExcludeAdditionalProperties = true;

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    new MSSqlServerSinkOptions
//                    {
//                        TableName = DatabaseFixture.LogTableName,
//                        AutoCreateSqlTable = true,
//                        BatchPostingLimit = 1,
//                        BatchPeriod = TimeSpan.FromSeconds(10)
//                    },
//                    columnOptions: columnOptions,
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();

//            // Act
//            Log.Logger
//                .ForContext("A", "AValue")
//                .ForContext("B", "BValue")
//                .Information("Logging message");

//            Log.CloseAndFlush();

//            // Assert
//            VerifyCustomQuery<LogEventColumn>($"SELECT LogEvent from {DatabaseFixture.LogTableName}",
//                e => e.Should().Contain(l => l.LogEvent.Contains("AValue")));
//            VerifyCustomQuery<LogEventColumn>($"SELECT LogEvent from {DatabaseFixture.LogTableName}",
//                e => e.Should().NotContain(l => l.LogEvent.Contains("BValue")));
//        }

//        [Trait("Bugfix", "#90")]
//        [Fact]
//        public void LogEventExcludeStandardColumns()
//        {
//            // Arrange
//            var columnOptions = new MSSqlServer.ColumnOptions();
//            columnOptions.Store.Remove(StandardColumn.Properties);
//            columnOptions.Store.Add(StandardColumn.LogEvent);
//            columnOptions.LogEvent.ExcludeStandardColumns = true;

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    new MSSqlServerSinkOptions
//                    {
//                        TableName = DatabaseFixture.LogTableName,
//                        AutoCreateSqlTable = true,
//                        BatchPostingLimit = 1,
//                        BatchPeriod = TimeSpan.FromSeconds(10)
//                    },
//                    columnOptions: columnOptions,
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();

//            // Act
//            Log.Logger
//                .ForContext("A", "AValue")
//                .Information("Logging message");

//            Log.CloseAndFlush();

//            // Assert
//            VerifyCustomQuery<LogEventColumn>($"SELECT LogEvent from {DatabaseFixture.LogTableName}",
//                e => e.Should().Contain(l => l.LogEvent.Contains("AValue")));
//            VerifyCustomQuery<LogEventColumn>($"SELECT LogEvent from {DatabaseFixture.LogTableName}",
//                e => e.Should().NotContain(l => l.LogEvent.Contains("TimeStamp")));
//        }

//        [Fact]
//        public void ExcludeIdColumn()
//        {
//            // Arrange
//            var columnOptions = new MSSqlServer.ColumnOptions();
//            columnOptions.Store.Remove(StandardColumn.Id);

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    new MSSqlServerSinkOptions
//                    {
//                        TableName = DatabaseFixture.LogTableName,
//                        AutoCreateSqlTable = true,
//                        BatchPostingLimit = 1,
//                        BatchPeriod = TimeSpan.FromSeconds(10)
//                    },
//                    columnOptions: columnOptions,
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();
//            Log.CloseAndFlush();

//            // Assert
//            var query = Invariant($@"SELECT COLUMN_NAME AS ColumnName FROM {DatabaseFixture.Database}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{DatabaseFixture.LogTableName}'");
//            VerifyCustomQuery<InfoSchema>(query, e => e.Should().Contain(x => x.ColumnName == columnOptions.Properties.ColumnName));
//            VerifyCustomQuery<InfoSchema>(query, e => e.Should().NotContain(x => x.ColumnName == columnOptions.Id.ColumnName));
//        }

//        [Fact]
//        public void BigIntIdColumn()
//        {
//            // Arrange
//            var columnOptions = new MSSqlServer.ColumnOptions();
//            columnOptions.Id.DataType = SqlDbType.BigInt;

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    new MSSqlServerSinkOptions
//                    {
//                        TableName = DatabaseFixture.LogTableName,
//                        AutoCreateSqlTable = true,
//                        BatchPostingLimit = 1,
//                        BatchPeriod = TimeSpan.FromSeconds(10)
//                    },
//                    columnOptions: columnOptions,
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();
//            Log.CloseAndFlush();

//            // Assert
//            VerifyCustomQuery<InfoSchema>($@"SELECT DATA_TYPE AS DataType FROM {DatabaseFixture.Database}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{DatabaseFixture.LogTableName}' AND COLUMN_NAME = '{columnOptions.Id.ColumnName}'",
//                e => e.Should().Contain(x => x.DataType == "bigint"));
//        }

//        [Trait("Bugfix", "#130")]
//        [Fact]
//        public void XmlPropertyColumn()
//        {
//            // Arrange
//            var columnOptions = new MSSqlServer.ColumnOptions();
//            columnOptions.Properties.DataType = SqlDbType.Xml;

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    new MSSqlServerSinkOptions
//                    {
//                        TableName = DatabaseFixture.LogTableName,
//                        AutoCreateSqlTable = true,
//                        BatchPostingLimit = 1,
//                        BatchPeriod = TimeSpan.FromSeconds(10)
//                    },
//                    columnOptions: columnOptions,
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();
//            Log.CloseAndFlush();

//            // Assert
//            VerifyCustomQuery<InfoSchema>($@"SELECT DATA_TYPE AS DataType FROM {DatabaseFixture.Database}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{DatabaseFixture.LogTableName}' AND COLUMN_NAME = '{columnOptions.Properties.ColumnName}'",
//                e => e.Should().Contain(x => x.DataType == "xml"));
//        }

//        [Trait("Bugfix", "#107")]
//        [Fact]
//        [Obsolete("Testing an inteface marked as obsolete", error: false)]
//        public void AutoCreateSchemaLegacyInterface()
//        {
//            // Use a custom table name because DROP SCHEMA can
//            // require permissions higher than the test-runner
//            // needs, and we don't want this left-over table
//            // to create misleading results in other tests.

//            // Arrange
//            var schemaName = "CustomTestSchema";
//            var tableName = "CustomSchemaLogTable";
//            var columnOptions = new MSSqlServer.ColumnOptions();

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    schemaName: schemaName,
//                    tableName: tableName,
//                    columnOptions: columnOptions,
//                    autoCreateSqlTable: true,
//                    batchPostingLimit: 1,
//                    period: TimeSpan.FromSeconds(10),
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();
//            Log.CloseAndFlush();

//            // Assert
//            VerifyCustomQuery<InfoSchema>("SELECT SCHEMA_NAME AS SchemaName FROM INFORMATION_SCHEMA.SCHEMATA",
//                e => e.Should().Contain(x => x.SchemaName == schemaName));
//        }

//        [Trait("Bugfix", "#107")]
//        [Fact]
//        public void AutoCreateSchemaSinkOptionsInterface()
//        {
//            // Use a custom table name because DROP SCHEMA can
//            // require permissions higher than the test-runner
//            // needs, and we don't want this left-over table
//            // to create misleading results in other tests.

//            // Arrange
//            var schemaName = "CustomTestSchema";
//            var tableName = "CustomSchemaLogTable";
//            var columnOptions = new MSSqlServer.ColumnOptions();

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    sinkOptions: new MSSqlServerSinkOptions
//                    {
//                        SchemaName = schemaName,
//                        TableName = tableName,
//                        AutoCreateSqlTable = true,
//                        BatchPostingLimit = 1,
//                        BatchPeriod = TimeSpan.FromSeconds(10)
//                    },
//                    columnOptions: columnOptions,
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();
//            Log.CloseAndFlush();

//            // Assert
//            VerifyCustomQuery<InfoSchema>("SELECT SCHEMA_NAME AS SchemaName FROM INFORMATION_SCHEMA.SCHEMATA",
//                e => e.Should().Contain(x => x.SchemaName == schemaName));
//        }

//        [Trait("Bugfix", "#171")]
//        [Fact]
//        public void LogEventStoreAsEnum()
//        {
//            // Arrange
//            var columnOptions = new MSSqlServer.ColumnOptions();
//            columnOptions.Level.StoreAsEnum = true;
//            columnOptions.Store.Add(StandardColumn.LogEvent);

//            Log.Logger = new LoggerConfiguration()
//                .WriteTo.MSSqlServer
//                (
//                    connectionString: DatabaseFixture.LogEventsConnectionString,
//                    new MSSqlServerSinkOptions
//                    {
//                        TableName = DatabaseFixture.LogTableName,
//                        AutoCreateSqlTable = true
//                    },
//                    columnOptions: columnOptions,
//                    formatProvider: CultureInfo.InvariantCulture
//                )
//                .CreateLogger();

//            // Act
//            Log.Logger
//                .Information("Logging message");

//            Log.CloseAndFlush();

//            // Assert
//            VerifyCustomQuery<LogEventColumn>($"SELECT Id from {DatabaseFixture.LogTableName}",
//                e => e.Should().HaveCount(1));
//        }
//    }
//}
