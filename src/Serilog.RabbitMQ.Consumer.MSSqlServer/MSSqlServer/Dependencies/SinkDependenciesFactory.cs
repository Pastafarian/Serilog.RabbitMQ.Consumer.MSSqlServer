using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies
{
    internal static class SinkDependenciesFactory
    {
        internal static SinkDependencies Create(
            string connectionString,
            MSSqlServerSinkOptions sinkOptions,
            IFormatProvider formatProvider,
            ColumnOptions.ColumnOptions columnOptions,
            ITextFormatterLogEventWithExceptionAsJsonString logEventFormatter)
        {
            columnOptions = columnOptions ?? new ColumnOptions.ColumnOptions();
            columnOptions.FinalizeConfigurationForSinkConstructor();

            // Add 'Enlist=false', so that ambient transactions (TransactionScope) will not affect/rollback logging
            // unless sink option EnlistInTransaction is set to true.
            var sqlConnectionStringBuilderWrapper = new SqlConnectionStringBuilderWrapper(
                connectionString, sinkOptions.EnlistInTransaction);
            var sqlConnectionFactory = new SqlConnectionFactory(sqlConnectionStringBuilderWrapper);
            var dataTableCreator = new DataTableCreator(sinkOptions.TableName, columnOptions);
            var sqlCreateTableWriter = new SqlCreateTableWriter(sinkOptions.SchemaName,
                sinkOptions.TableName, columnOptions, dataTableCreator);

            var sqlConnectionStringBuilderWrapperNoDb = new SqlConnectionStringBuilderWrapper(
                connectionString, sinkOptions.EnlistInTransaction)
            {
                InitialCatalog = ""
            };
            var sqlConnectionFactoryNoDb =
                new SqlConnectionFactory(sqlConnectionStringBuilderWrapperNoDb);
            var sqlCreateDatabaseWriter = new SqlCreateDatabaseWriter(sqlConnectionStringBuilderWrapper.InitialCatalog);

            var logEventDataGenerator =
                new LogEventDataGenerator(columnOptions,
                    new StandardColumnDataGenerator(columnOptions,
                        new XmlPropertyFormatter(),
                        logEventFormatter),
                    new AdditionalColumnDataGenerator(
                        new ColumnSimplePropertyValueResolver(),
                        new ColumnHierarchicalPropertyValueResolver()));

            var sinkDependencies = new SinkDependencies
            {
                DataTableCreator = dataTableCreator,
                SqlDatabaseCreator = new SqlDatabaseCreator(
                    sqlCreateDatabaseWriter, sqlConnectionFactoryNoDb),
                SqlTableCreator = new SqlTableCreator(
                    sqlCreateTableWriter, sqlConnectionFactory),
                SqlBulkBatchWriter = sinkOptions.UseSqlBulkCopy
                    ? (ISqlBulkBatchWriter)new SqlBulkBatchWriter(
                        sinkOptions.TableName, sinkOptions.SchemaName, columnOptions.DisableTriggers,
                        sqlConnectionFactory, logEventDataGenerator)
                    : (ISqlBulkBatchWriter)new SqlInsertStatementWriter(
                        sinkOptions.TableName, sinkOptions.SchemaName,
                        sqlConnectionFactory, logEventDataGenerator),
                SqlLogEventWriter = new SqlInsertStatementWriter(
                    sinkOptions.TableName, sinkOptions.SchemaName,
                    sqlConnectionFactory, logEventDataGenerator)
            };

            return sinkDependencies;
        }
    }
}
