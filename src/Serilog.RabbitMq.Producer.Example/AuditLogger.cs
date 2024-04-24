using Serilog.Events;

namespace Serilog.Producer.RabbitMq.Example;

public class AuditLogger : IAuditLogger
{
    private readonly ILogger _logger;

    public AuditLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void Write(LogEvent logEvent)
    {
        _logger.Write(logEvent);
    }
}