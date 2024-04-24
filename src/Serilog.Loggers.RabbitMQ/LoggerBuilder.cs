using Microsoft.Extensions.Configuration;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Filters;

namespace Serilog.Loggers.RabbitMQ;

public static class LoggerBuilder
{
    public static ILogger BuildLogger(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder())
            .Enrich.WithMachineName()
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithCorrelationId()
            .Enrich.With<PublishTimestampEnricher>()
            .Filter.ByExcluding(logEvent => Matching.FromSource("Microsoft").Invoke(logEvent))

            .CreateLogger();
    }

    public static ILogger BuildAuditLogger(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder())
            .Enrich.WithMachineName()
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithCorrelationId()
            .Enrich.With<PublishTimestampEnricher>()
            .Filter.ByExcluding(logEvent => Matching.FromSource("Microsoft").Invoke(logEvent))

            .CreateLogger();
    }
}
