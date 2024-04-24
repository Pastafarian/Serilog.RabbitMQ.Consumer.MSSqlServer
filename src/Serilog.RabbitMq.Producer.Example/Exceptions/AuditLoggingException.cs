namespace Serilog.Producer.RabbitMq.Example.Exceptions;

public class AuditLoggingException(string message) : Exception(message)
{
    public string? CustomAuditMessage { get; set; }
}