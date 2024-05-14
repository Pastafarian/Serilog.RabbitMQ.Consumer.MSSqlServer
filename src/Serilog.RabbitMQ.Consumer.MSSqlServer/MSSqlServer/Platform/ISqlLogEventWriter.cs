using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    public interface ISqlLogEventWriter
    {
        Task WriteEvent(LogEventWithExceptionAsJsonString logEvent);
    }
}
