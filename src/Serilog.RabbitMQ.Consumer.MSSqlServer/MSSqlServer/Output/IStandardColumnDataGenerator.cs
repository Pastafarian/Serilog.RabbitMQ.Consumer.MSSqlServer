using Serilog.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    internal interface IStandardColumnDataGenerator
    {
        KeyValuePair<string, object?> GetStandardColumnNameAndValue(StandardColumn column,
            LogEventWithExceptionAsJsonString logEvent);
    }
}
