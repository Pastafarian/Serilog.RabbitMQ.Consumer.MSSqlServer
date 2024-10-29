using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    public interface IStandardColumnDataGenerator
    {
        KeyValuePair<string, object?> GetStandardColumnNameAndValue(StandardColumn column,
            LogEventWithExceptionAsJsonString logEvent);
    }
}
