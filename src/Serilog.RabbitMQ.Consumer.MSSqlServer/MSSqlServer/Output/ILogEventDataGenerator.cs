using Serilog.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    internal interface ILogEventDataGenerator
    {
        IEnumerable<KeyValuePair<string, object>> GetColumnsAndValues(LogEventWithExceptionAsJsonString logEvent);
    }
}
