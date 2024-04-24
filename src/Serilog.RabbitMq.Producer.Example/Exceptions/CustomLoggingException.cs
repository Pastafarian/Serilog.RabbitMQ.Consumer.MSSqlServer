namespace Serilog.Producer.RabbitMq.Example.Exceptions
{
    public class CustomLoggingException(string message) : Exception(message)
    {
        public string? CustomLoggingMessage { get; set; }
    }
}
