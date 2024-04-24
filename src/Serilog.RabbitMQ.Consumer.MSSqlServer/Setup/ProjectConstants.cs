using System.Text.Json;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;

public static class ProjectConstants
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new LogEventJsonConverter(),
            new ExceptionJsonConverter()
        }
    };
}