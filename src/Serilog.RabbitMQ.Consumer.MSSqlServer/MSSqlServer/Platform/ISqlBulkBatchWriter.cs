using System.Data;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    public interface ISqlBulkBatchWriter
    {
        Task WriteBatch(List<LogEventWithExceptionAsJsonString> events, DataTable dataTable);
    }
}
