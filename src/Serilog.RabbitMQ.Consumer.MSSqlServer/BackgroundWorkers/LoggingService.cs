using System.Data;
using RabbitMQ.Client.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies;
using Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

public class LoggingService : WorkerService<LoggingService>
{
    private readonly ILogger<LoggingService> _logger;
    private readonly SinkDependencies _sinkDependencies;
    private System.Timers.Timer? _timer = null;
    private BasicDeliverEventArgs _basicDeliverEventArgs;
    public LoggingService(ILogger<LoggingService> logger,
        IRabbitConnectionFactory connectionFactory,
        MSSqlServerSinkOptions sinkOptions,
        SinkDependencies sinkDependencies,
        RabbitMqClientConsumerConfiguration configuration,
        IAsyncEventingBasicConsumerFactory asyncEventingBasicConsumerFactory) :
        base(logger, connectionFactory, sinkOptions, sinkDependencies, configuration.LoggingQueueName, configuration, asyncEventingBasicConsumerFactory)
    {
        _logger = logger;
        _sinkDependencies = sinkDependencies;

        if (_sinkDependencies.DataTableCreator != null)
            _eventTable = _sinkDependencies.DataTableCreator.CreateDataTable();
    }

    private readonly List<LogEventWithExceptionAsJsonString> _logEvents = [];
    private readonly DataTable _eventTable;

    public override async Task ProcessMessage(LogEventWithExceptionAsJsonString logEvent, BasicDeliverEventArgs basicDeliverEventArgs)
    {
        if (logEvent == null)
            throw new ArgumentNullException(nameof(logEvent));

        if (SinkOptions.EagerlyEmitFirstEvent)
        {
            if (SinkDependencies.SqlLogEventWriter != null)
                await SinkDependencies.SqlLogEventWriter.WriteEvent(logEvent);

            Channel?.BasicAck(basicDeliverEventArgs.DeliveryTag, true);

            SinkOptions.EagerlyEmitFirstEvent = false;
            return;
        }

        _logEvents.Add(logEvent);
        _basicDeliverEventArgs = basicDeliverEventArgs;
        if (_logEvents.Count >= SinkOptions.BatchPostingLimit)
        {
            if (SinkDependencies.SqlBulkBatchWriter != null)
                await SinkDependencies.SqlBulkBatchWriter.WriteBatch(_logEvents, _eventTable);

            Channel?.BasicAck(basicDeliverEventArgs.DeliveryTag, true);
            _logEvents.Clear();

            // Reset the timer
            _timer?.Stop();
            _timer?.Start();

            return;
        }

        if (_timer == null)
            StartTimer();
    }

    private void StartTimer()
    {
        _timer = new System.Timers.Timer(SinkOptions.BatchPeriod)
        {
            AutoReset = true
        };
        _timer.Elapsed += async (_, _) =>
        {
            if (_logEvents.Count == 0)
            {
                return;
            }

            if (SinkDependencies.SqlBulkBatchWriter != null)
                await SinkDependencies.SqlBulkBatchWriter.WriteBatch(_logEvents, _eventTable);
            Channel?.BasicAck(_basicDeliverEventArgs.DeliveryTag, true);
            _logEvents.Clear();
            _timer?.Start();
        };
        _timer.Start();
    }

    internal new async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await base.ExecuteAsync(cancellationToken);
    }
}