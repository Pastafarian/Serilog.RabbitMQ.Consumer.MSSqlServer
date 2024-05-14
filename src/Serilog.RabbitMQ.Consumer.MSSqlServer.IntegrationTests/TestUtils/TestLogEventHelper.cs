using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.TestUtils
{
    extern alias ConsumerAlias;

    internal static class TestLogEventHelper
    {
        public static ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers.LogEventWithExceptionAsJsonString CreateLogEvent()
        {
            return new ConsumerAlias::Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers.LogEventWithExceptionAsJsonString(new LogEvent(
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken>()),
                new List<LogEventProperty>()), "");
        }
    }
}
