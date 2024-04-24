using System.Data;
using System.Text;
using Serilog.Debugging;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;
using static System.FormattableString;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    internal class SqlLogEventWriter : ISqlLogEventWriter
    {
        private readonly string _tableName;
        private readonly string _schemaName;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly ILogEventDataGenerator _logEventDataGenerator;

        public SqlLogEventWriter(
            string tableName,
            string schemaName,
            ISqlConnectionFactory sqlConnectionFactory,
            ILogEventDataGenerator logEventDataGenerator)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _logEventDataGenerator = logEventDataGenerator ?? throw new ArgumentNullException(nameof(logEventDataGenerator));
        }

        public async Task WriteEvent(LogEventWithExceptionAsJsonString logEvent)
        {
            try
            {
                using var connection = _sqlConnectionFactory.Create();
                //using var tran = connection.BeginTransaction();
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;

                    var fieldList = new StringBuilder(Invariant($"INSERT INTO [{_schemaName}].[{_tableName}] ("));
                    var parameterList = new StringBuilder(") VALUES (");

                    var index = 0;
                    foreach (var field in _logEventDataGenerator.GetColumnsAndValues(logEvent))
                    {
                        if (index != 0)
                        {
                            fieldList.Append(',');
                            parameterList.Append(',');
                        }

                        fieldList.Append(Invariant($"[{field.Key}]"));
                        parameterList.Append("@P");
                        parameterList.Append(index);

                        command.AddParameter(Invariant($"@P{index}"), field.Value);

                        index++;
                    }

                    parameterList.Append(')');
                    fieldList.Append(parameterList);

                    command.CommandText = fieldList.ToString();

                    await command.ExecuteNonQueryAsync();
                }
                //tran.Commit();
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to write log event to the database due to following error: {0}", ex);
                throw;
            }
        }
    }
}
