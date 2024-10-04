using System.Data;
using System.Diagnostics;
using RabbitMQ.Client.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies;
using Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

public class LoggingService : WorkerService<LoggingService>
{
    private readonly ILogger<LoggingService> _logger;
    private readonly ISinkDependencies _sinkDependencies;
    private System.Timers.Timer? _timer = null;
    private BasicDeliverEventArgs _basicDeliverEventArgs;
    public LoggingService(ILogger<LoggingService> logger,
        IRabbitConnectionFactory connectionFactory,
        MSSqlServerSinkOptions sinkOptions,
        ISinkDependencies sinkDependencies,
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

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        return base.StopAsync(stoppingToken);
    }

    public override async Task ProcessMessage(LogEventWithExceptionAsJsonString logEvent, BasicDeliverEventArgs basicDeliverEventArgs)
    {
        if (logEvent == null)
            throw new ArgumentNullException(nameof(logEvent));

        if (SinkOptions.EagerlyEmitFirstEvent)
        {

            await SinkDependencies.SqlLogEventWriter.WriteEvent(logEvent);

            Channel?.BasicAck(basicDeliverEventArgs.DeliveryTag, true);

            SinkOptions.EagerlyEmitFirstEvent = false;
            return;
        }

        _logEvents.Add(logEvent);
        _basicDeliverEventArgs = basicDeliverEventArgs;
        if (_logEvents.Count >= SinkOptions.BatchPostingLimit)
        {
            await SinkDependencies.SqlBulkBatchWriter.WriteBatch(_logEvents, _eventTable);
            Debug.Print("Written Batch");
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

            if (SinkOptions.EagerlyEmitFirstEvent)
            {
                foreach (var logEvent in _logEvents)
                {

                    await _sinkDependencies.SqlLogEventWriter.WriteEvent(logEvent);
                    Debug.Print("Written Batch eager");
                }
            }
            else
            {
                await SinkDependencies.SqlBulkBatchWriter.WriteBatch(_logEvents, _eventTable);
                Debug.Print("Written Batch standard");
            }


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