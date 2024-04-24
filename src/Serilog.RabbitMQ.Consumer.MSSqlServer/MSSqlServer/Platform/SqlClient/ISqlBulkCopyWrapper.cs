using System.Data;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient
{
    internal interface ISqlBulkCopyWrapper : IDisposable
    {
        void AddSqlBulkCopyColumnMapping(string sourceColumn, string destinationColumn);
        Task WriteToServerAsync(DataTable table);
    }
}
