using Serilog.Events;
using Serilog.Parsing;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils
{
    internal static class TestLogEventHelper
    {
        public static LogEventWithExceptionAsJsonString CreateLogEvent()
        {
            return new LogEventWithExceptionAsJsonString(new LogEvent(
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken>()),
                new List<LogEventProperty>()), "");
        }
    }
}
