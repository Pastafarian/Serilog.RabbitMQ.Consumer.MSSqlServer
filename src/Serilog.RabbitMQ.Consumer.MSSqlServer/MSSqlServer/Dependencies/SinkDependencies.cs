﻿using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies
{
    public interface ISinkDependencies
    {
        ColumnOptions.ColumnOptions ColumnOptions { get; }
        IDataTableCreator DataTableCreator { get; set; }
        ISqlCommandExecutor SqlDatabaseCreator { get; set; }
        ISqlCommandExecutor SqlTableCreator { get; set; }
        ISqlBulkBatchWriter SqlBulkBatchWriter { get; set; }
        ISqlLogEventWriter SqlLogEventWriter { get; set; }
    }

    public class SinkDependencies : ISinkDependencies
    {
        public ColumnOptions.ColumnOptions ColumnOptions { get; }
        public static ILogEventDataGenerator _logEventDataGenerator;
        public SinkDependencies(ConnectionString connectionString,
            MSSqlServerSinkOptions sinkOptions,
            IFormatProvider? formatProvider,
            ColumnOptions.ColumnOptions columnOptions,
            ILogEventDataGenerator logEventDataGenerator)
        {
            _logEventDataGenerator = logEventDataGenerator;
            columnOptions.FinalizeConfigurationForSinkConstructor();
            ColumnOptions = columnOptions;
            var sqlConnectionStringBuilderWrapper = new SqlConnectionStringBuilderWrapper(
                connectionString.DefaultConnection, sinkOptions.EnlistInTransaction)
            { InitialCatalog = connectionString.DatabaseName };
            var sqlConnectionFactory = new SqlConnectionFactory(sqlConnectionStringBuilderWrapper);
            var dataTableCreator = new DataTableCreator(sinkOptions.TableName, columnOptions);
            var sqlCreateTableWriter = new SqlCreateTableWriter(sinkOptions.SchemaName,
                sinkOptions.TableName, columnOptions, dataTableCreator);

            var sqlConnectionStringBuilderWrapperNoDb = new SqlConnectionStringBuilderWrapper(
                connectionString.DefaultConnection, sinkOptions.EnlistInTransaction)
            {
                InitialCatalog = ""
            };
            var sqlConnectionFactoryNoDb =
                new SqlConnectionFactory(sqlConnectionStringBuilderWrapperNoDb);
            var sqlCreateDatabaseWriter = new SqlCreateDatabaseWriter(sqlConnectionStringBuilderWrapper.InitialCatalog);



            DataTableCreator = dataTableCreator;
            SqlDatabaseCreator = new SqlDatabaseCreator(
                sqlCreateDatabaseWriter, sqlConnectionFactoryNoDb);
            SqlTableCreator = new SqlTableCreator(
                sqlCreateTableWriter, sqlConnectionFactory);
            //SqlBulkBatchWriter = sinkOptions.UseSqlBulkCopy
            //    ? new SqlBulkBatchWriter(
            //        sinkOptions.TableName, sinkOptions.SchemaName, columnOptions.DisableTriggers,
            //        sqlConnectionFactory, logEventDataGenerator)
            //    : new SqlInsertStatementWriter(
            //        sinkOptions.TableName, sinkOptions.SchemaName,
            //        sqlConnectionFactory, logEventDataGenerator);
            SqlLogEventWriter = new SqlInsertStatementWriter(
                sinkOptions.TableName, sinkOptions.SchemaName,
                sqlConnectionFactory, logEventDataGenerator);
        }
        public IDataTableCreator DataTableCreator { get; set; }
        public ISqlCommandExecutor SqlDatabaseCreator { get; set; }
        public ISqlCommandExecutor SqlTableCreator { get; set; }
        public ISqlBulkBatchWriter SqlBulkBatchWriter { get; set; }
        public ISqlLogEventWriter SqlLogEventWriter { get; set; }
    }
}
