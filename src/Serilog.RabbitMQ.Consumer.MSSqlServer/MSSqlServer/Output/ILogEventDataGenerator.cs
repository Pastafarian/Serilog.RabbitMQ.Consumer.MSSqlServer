using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    public interface ILogEventDataGenerator
    {
        IEnumerable<KeyValuePair<string, object>> GetColumnsAndValues(LogEventWithExceptionAsJsonString logEvent);
    }
}
