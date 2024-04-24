using RabbitMQ.Client.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies;
using Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

public class AuditService(
    ILogger<AuditService> logger,
    IRabbitConnectionFactory connectionFactory,
    MSSqlServerSinkOptions sinkOptions,
    SinkDependencies sinkDependencies,
    RabbitMqClientConsumerConfiguration configuration,
    IAsyncEventingBasicConsumerFactory asyncEventingBasicConsumerFactory)
    : WorkerService<AuditService>(logger, connectionFactory, sinkOptions, sinkDependencies,
        configuration.AuditQueueName, configuration, asyncEventingBasicConsumerFactory)
{
    public override async Task ProcessMessage(LogEventWithExceptionAsJsonString message, BasicDeliverEventArgs basicDeliverEventArgs)
    {
        try
        {
            if (SinkDependencies.SqlLogEventWriter != null)
                await SinkDependencies.SqlLogEventWriter.WriteEvent(message);

            Channel?.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audit message");
        }
    }
}