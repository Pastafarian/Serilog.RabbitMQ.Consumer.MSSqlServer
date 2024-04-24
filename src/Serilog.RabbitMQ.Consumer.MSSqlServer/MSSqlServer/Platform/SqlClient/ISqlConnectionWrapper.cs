using Microsoft.Data.SqlClient;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient
{
    internal interface ISqlConnectionWrapper : IDisposable
    {
        string ConnectionString { get; }

        void Open();
        void Close();
        Task CloseAsync();
        Task OpenAsync();
        public SqlTransaction BeginTransaction();
        ISqlCommandWrapper CreateCommand();
        ISqlCommandWrapper CreateCommand(string cmdText);
        ISqlBulkCopyWrapper CreateSqlBulkCopy(bool disableTriggers, string destinationTableName);
    }
}
