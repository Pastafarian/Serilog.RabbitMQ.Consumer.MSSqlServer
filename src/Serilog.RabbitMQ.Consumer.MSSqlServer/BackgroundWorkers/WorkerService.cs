using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies;
using Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

public class AsyncEventingBasicConsumerFactory : IAsyncEventingBasicConsumerFactory
{
    public AsyncEventingBasicConsumer CreateConsumer(IModel channel)
    {
        return new AsyncEventingBasicConsumer(channel);
    }
}

public interface IAsyncEventingBasicConsumerFactory
{
    AsyncEventingBasicConsumer CreateConsumer(IModel channel);
}

public abstract class WorkerService<T> : BackgroundService
{
    protected readonly ILogger<T> _logger;
    private readonly IRabbitConnectionFactory _connectionFactory;
    private readonly string _queueName;
    protected SinkDependencies SinkDependencies;
    private readonly RabbitMqClientConsumerConfiguration _rabbitMqConfiguration;
    private readonly IAsyncEventingBasicConsumerFactory _asyncEventingBasicConsumerFactory;
    protected IModel? Channel;
    protected MSSqlServerSinkOptions SinkOptions;

    protected WorkerService(ILogger<T> logger,
        IRabbitConnectionFactory connectionFactory,
        MSSqlServerSinkOptions sinkOptions,
        SinkDependencies sinkDependencies,
        string queueName,
        RabbitMqClientConsumerConfiguration rabbitMqConfiguration,
        IAsyncEventingBasicConsumerFactory asyncEventingBasicConsumerFactory)
    {
        _connectionFactory = connectionFactory;
        SinkOptions = sinkOptions;
        SinkDependencies = sinkDependencies;
        _queueName = queueName;
        _rabbitMqConfiguration = rabbitMqConfiguration;
        _asyncEventingBasicConsumerFactory = asyncEventingBasicConsumerFactory;
        _logger = logger;
        CreateDatabaseAndTable(SinkOptions, SinkDependencies);
        CheckSinkDependencies(sinkDependencies);
    }

    private static void CreateDatabaseAndTable(MSSqlServerSinkOptions sinkOptions, SinkDependencies sinkDependencies)
    {
        if (sinkOptions.AutoCreateSqlDatabase)
        {
            sinkDependencies.SqlDatabaseCreator?.Execute();
        }

        if (sinkOptions.AutoCreateSqlTable)
        {
            sinkDependencies.SqlTableCreator?.Execute();
        }
    }

    private static void CheckSinkDependencies(SinkDependencies sinkDependencies)
    {
        if (sinkDependencies == null)
        {
            throw new ArgumentNullException(nameof(sinkDependencies));
        }

        if (sinkDependencies.DataTableCreator == null)
        {
            throw new InvalidOperationException("DataTableCreator is not initialized!");
        }

        if (sinkDependencies.SqlTableCreator == null)
        {
            throw new InvalidOperationException("SqlTableCreator is not initialized!");
        }

        if (sinkDependencies.SqlLogEventWriter == null)
        {
            throw new InvalidOperationException("SqlLogEventWriter is not initialized!");
        }
    }

    public abstract Task ProcessMessage(LogEventWithExceptionAsJsonString message, BasicDeliverEventArgs basicDeliverEventArgs);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AsyncEventingBasicConsumer consumer;
        try
        {
            var connection = await _connectionFactory.GetConnectionAsync();
            Channel = connection.CreateModel();
            CreateExchangesAndQueues(Channel);
            consumer = _asyncEventingBasicConsumerFactory.CreateConsumer(Channel);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


        consumer.Received += async (_, ea) =>
        {
            var messageString = string.Empty;
            try
            {
                var body = ea.Body.ToArray();
                messageString = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<LogEventWithExceptionAsJsonString>(body, ProjectConstants.JsonSerializerOptions);

                if (message == null)
                {
                    _logger.LogError("Failed to Deserialize logging message. Raw JsonValue: {RawJson}", messageString);
                    return;
                }

                await Policy
                    .Handle<SqlException>(
                        SqlServerTransientExceptionDetector.ShouldRetryOn)
                    .Or<TimeoutException>()
                    .WaitAndRetry(5, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .Execute(async () =>
                    {
                        Debug.WriteLine($"Processing message {nameof(T)}");

                        await ProcessMessage(message, ea);
                    });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing batch to SQL Server. Message String:{MessageString}", messageString);
            }
        };

        Channel.BasicConsume(_queueName, autoAck: false, consumer);
    }

    private void CreateExchangesAndQueues(IModel model)
    {
        if (!_rabbitMqConfiguration.AutoCreateExchange) return;
        model.ExchangeDeclare(_rabbitMqConfiguration.LoggingExchangeName, _rabbitMqConfiguration.ExchangeType, true);
        model.QueueDeclare(_rabbitMqConfiguration.LoggingQueueName, true, false, false);
        model.QueueBind(_rabbitMqConfiguration.LoggingQueueName, _rabbitMqConfiguration.LoggingExchangeName, _rabbitMqConfiguration.RouteKey);
        model.ExchangeDeclare(_rabbitMqConfiguration.AuditExchangeName, _rabbitMqConfiguration.ExchangeType, true);
        model.QueueDeclare(_rabbitMqConfiguration.AuditQueueName, true, false, false);
        model.QueueBind(_rabbitMqConfiguration.AuditQueueName, _rabbitMqConfiguration.AuditExchangeName, _rabbitMqConfiguration.RouteKey);
    }


    public override Task StopAsync(CancellationToken stoppingToken)
    {
        _connectionFactory.Close();
        _connectionFactory.Dispose();
        return base.StopAsync(stoppingToken);
    }
}