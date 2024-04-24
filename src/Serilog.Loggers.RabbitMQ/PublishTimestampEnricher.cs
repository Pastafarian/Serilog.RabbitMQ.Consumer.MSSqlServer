using Serilog.Core;
using Serilog.Events;

namespace Serilog.Loggers.RabbitMQ;

public class PublishTimestampEnricher : ILogEventEnricher
{
    /// <summary>The property name added to enriched log events.</summary>
    public const string PublishTimestampPropertyName = "PublishTimestamp";

    /// <summary>Enrich the log event.</summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) => logEvent.AddPropertyIfAbsent(new LogEventProperty(PublishTimestampPropertyName, new ScalarValue((object)DateTimeOffset.UtcNow)));
}