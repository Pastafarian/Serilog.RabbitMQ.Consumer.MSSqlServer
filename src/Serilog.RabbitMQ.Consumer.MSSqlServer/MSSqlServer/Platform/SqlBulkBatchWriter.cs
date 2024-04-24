using System.Data;
using System.Globalization;
using Serilog.Debugging;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    internal class SqlBulkBatchWriter : ISqlBulkBatchWriter
    {
        private readonly string _tableName;
        private readonly string _schemaName;
        private readonly bool _disableTriggers;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly ILogEventDataGenerator _logEventDataGenerator;

        public SqlBulkBatchWriter(
            string tableName,
            string schemaName,
            bool disableTriggers,
            ISqlConnectionFactory sqlConnectionFactory,
            ILogEventDataGenerator logEventDataGenerator)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
            _disableTriggers = disableTriggers;
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _logEventDataGenerator = logEventDataGenerator ?? throw new ArgumentNullException(nameof(logEventDataGenerator));
        }
        private readonly object balanceLock = new object();
        public async Task WriteBatch(List<LogEventWithExceptionAsJsonString> events, DataTable dataTable)
        {

            try
            {

                FillDataTable(events, dataTable);


                using var cn = _sqlConnectionFactory.Create();
                await cn.OpenAsync();

                using var copy = cn.CreateSqlBulkCopy(_disableTriggers,
                    string.Format(CultureInfo.InvariantCulture, "[{0}].[{1}]", _schemaName, _tableName));
                foreach (var column in dataTable.Columns)
                {
                    var columnName = ((DataColumn)column).ColumnName;
                    copy.AddSqlBulkCopyColumnMapping(columnName, columnName);
                }

                await copy.WriteToServerAsync(dataTable).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to write batch of {0} log events to the database due to following error: {1}",
                    events.Count(), ex);
                throw;
            }
            finally
            {
                dataTable.Clear();
            }
        }

        private void FillDataTable(List<LogEventWithExceptionAsJsonString> events, DataTable dataTable)
        {
            var ff = dataTable;
            try
            {
                lock (balanceLock)
                {
                    // Add the new rows to the collection. 
                    for (var i = 0; i < events.Count; i++)
                    {
                        var row = dataTable.NewRow();

                        foreach (var field in _logEventDataGenerator.GetColumnsAndValues(events[i]))
                        {
                            row[field.Key] = field.Value;
                        }

                        dataTable.Rows.Add(row);
                    }

                    dataTable.EndLoadData();
                    dataTable.AcceptChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
