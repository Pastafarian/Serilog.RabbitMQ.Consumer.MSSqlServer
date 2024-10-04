using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform;
using Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.BackgroundWorkers
{
    public class LoggingServiceTests
    {
        private readonly Mock<IRabbitConnectionFactory> _connectionFactory = new();
        private readonly Mock<ILogger<LoggingService>> _logger = new();
        private readonly MSSqlServerSinkOptions _sinkOptions = new();
        private readonly Mock<ISinkDependencies> _sinkDependencies = new();
        private readonly Mock<IAsyncEventingBasicConsumerFactory> _asyncEventingBasicConsumerFactory = new();
        private readonly RabbitMqClientConsumerConfiguration _rabbitMqConfiguration = new();
        private readonly Mock<IModel> _mockChannel;
        private readonly LoggingService _sut;

        public LoggingServiceTests()
        {
            var mockDataTableCreator = new Mock<IDataTableCreator>();
            var mockSqlTableCreator = new Mock<ISqlCommandExecutor>();
            var mockSqlBulkBatchWriter = new Mock<ISqlBulkBatchWriter>();
            var mockSqlDatabaseCreator = new Mock<ISqlCommandExecutor>();
            var mockSqlLogEventWriter = new Mock<ISqlLogEventWriter>();
            _sinkDependencies.Setup(x => x.DataTableCreator).Returns(mockDataTableCreator.Object);
            _sinkDependencies.Setup(x => x.SqlTableCreator).Returns(mockSqlTableCreator.Object);
            _sinkDependencies.Setup(x => x.SqlDatabaseCreator).Returns(mockSqlDatabaseCreator.Object);
            _sinkDependencies.Setup(x => x.SqlBulkBatchWriter).Returns(mockSqlBulkBatchWriter.Object);
            _sinkDependencies.Setup(x => x.SqlLogEventWriter).Returns(mockSqlLogEventWriter.Object);

            var mockConnection = new Mock<IConnection>();
            _mockChannel = new Mock<IModel>();
            mockConnection.Setup(x => x.CreateModel()).Returns(_mockChannel.Object);
            _connectionFactory.Setup(x => x.GetConnectionAsync()).Returns(Task.FromResult(mockConnection.Object));

            _asyncEventingBasicConsumerFactory.Setup(x => x.CreateConsumer(It.IsAny<IModel>()))
                .Returns(new AsyncEventingBasicConsumer(_mockChannel.Object));

            _sut = new LoggingService(_logger.Object, _connectionFactory.Object, _sinkOptions, _sinkDependencies.Object,
                _rabbitMqConfiguration, _asyncEventingBasicConsumerFactory.Object);
        }

        [Fact]
        public async Task ProcessMessage_WhenLogEventIsNull_ThrowsArgumentNullException()
        {
            // Arrange + Act
            var act = () => _sut.ProcessMessage(null!, new BasicDeliverEventArgs());

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ProcessMessage_WhenEagerlyEmitFirstEventIsTrue_WritesEventAndReturns()
        {
            // Arrange
            LogEvent logEvent = new(DateTimeOffset.Now, LogEventLevel.Error, null, MessageTemplate.Empty, Array.Empty<LogEventProperty>());
            _sinkOptions.EagerlyEmitFirstEvent = true;
            var log = new LogEventWithExceptionAsJsonString(logEvent, "");
            var basicDeliverEventArgs = new BasicDeliverEventArgs();

            // Act
            await _sut.ExecuteAsync(new CancellationToken());
            await _sut.ProcessMessage(log, basicDeliverEventArgs);

            // Assert
            _sinkDependencies.Verify(x => x.SqlLogEventWriter.WriteEvent(log), Times.Once);
            _mockChannel.Verify(x => x.BasicAck(basicDeliverEventArgs.DeliveryTag, true), Times.Once);
        }


        [Theory, MemberData(
             nameof(ProcessMessage_WhenEagerlyEmitFirstEventIsFalseAndBatchSize2_ThenWritesEventAfterTimeoutData))]
        public async Task ProcessMessage_WhenEagerlyEmitFirstEventIsFalseAndBatchSize2_ThenWritesEventAfterTimeout(TimeSpan timeSpan, bool eventSent)
        {
            // Arrange
            LogEvent logEvent = new(DateTimeOffset.Now, LogEventLevel.Error, null, MessageTemplate.Empty, Array.Empty<LogEventProperty>());
            _sinkOptions.EagerlyEmitFirstEvent = false;
            var log = new LogEventWithExceptionAsJsonString(logEvent, "");
            var basicDeliverEventArgs = new BasicDeliverEventArgs();
            _sinkOptions.BatchPeriod = TimeSpan.FromSeconds(2);
            // Act
            await _sut.ExecuteAsync(new CancellationToken());
            await _sut.ProcessMessage(log, basicDeliverEventArgs);
            await Task.Delay(timeSpan);

            // Assert
            if (eventSent)
            {
                _sinkDependencies.Verify(x => x.SqlBulkBatchWriter.WriteBatch(It.IsAny<List<LogEventWithExceptionAsJsonString>>(), It.IsAny<DataTable>()), Times.Once);
                _mockChannel.Verify(x => x.BasicAck(basicDeliverEventArgs.DeliveryTag, true), Times.Once);
            }
            else
            {
                _sinkDependencies.Verify(x => x.SqlBulkBatchWriter.WriteBatch(It.IsAny<List<LogEventWithExceptionAsJsonString>>(), It.IsAny<DataTable>()), Times.Never);
                _mockChannel.Verify(x => x.BasicAck(basicDeliverEventArgs.DeliveryTag, true), Times.Never);
            }
        }
        public static IEnumerable<object[]>
            ProcessMessage_WhenEagerlyEmitFirstEventIsFalseAndBatchSize2_ThenWritesEventAfterTimeoutData()
        {
            yield return [new TimeSpan(0, 0, 0, 2, 300), true];
            yield return [new TimeSpan(0, 0, 0, 4, 200), true];
            yield return [new TimeSpan(0, 0, 0, 1, 0), false];
            yield return [new TimeSpan(0, 0, 0, 0, 100), false];
            yield return [new TimeSpan(0, 0, 0, 0, 200), false];
            yield return [new TimeSpan(0, 0, 0, 0, 100), false];
        }

        [Theory, MemberData(
             nameof(ProcessMessage_WhenEagerlyEmitFirstEventIsFalseAndBatchSize2_ThenWritesEventWithoutTimeout_WhenMessagesGreaterThanOrEqualToBatchPostingLimitData))]
        public async Task ProcessMessage_WhenEagerlyEmitFirstEventIsFalseAndBatchSize2_ThenWritesEventWithoutTimeout_WhenMessagesGreaterThanOrEqualToBatchPostingLimit(TimeSpan timeSpan, bool eventSent)
        {
            // Arrange
            LogEvent logEvent1 = new(DateTimeOffset.Now, LogEventLevel.Error, new Exception("Exception 1"), MessageTemplate.Empty, Array.Empty<LogEventProperty>());
            LogEvent logEvent2 = new(DateTimeOffset.Now, LogEventLevel.Error, new Exception("Exception 2"), MessageTemplate.Empty, Array.Empty<LogEventProperty>());
            _sinkOptions.EagerlyEmitFirstEvent = false;
            var log1 = new LogEventWithExceptionAsJsonString(logEvent1, "");
            var log2 = new LogEventWithExceptionAsJsonString(logEvent2, "");
            var basicDeliverEventArgs = new BasicDeliverEventArgs();
            _sinkOptions.BatchPeriod = TimeSpan.FromSeconds(2);
            _sinkOptions.BatchPostingLimit = 2;
            // Act
            await _sut.ExecuteAsync(new CancellationToken());
            await _sut.ProcessMessage(log1, basicDeliverEventArgs);
            await _sut.ProcessMessage(log2, basicDeliverEventArgs);
            await Task.Delay(timeSpan);

            // Assert
            if (eventSent)
            {
                _sinkDependencies.Verify(x => x.SqlBulkBatchWriter.WriteBatch(It.IsAny<List<LogEventWithExceptionAsJsonString>>(), It.IsAny<DataTable>()), Times.Once);
                _mockChannel.Verify(x => x.BasicAck(basicDeliverEventArgs.DeliveryTag, true), Times.Once);
            }
            else
            {
                _sinkDependencies.Verify(x => x.SqlBulkBatchWriter.WriteBatch(It.IsAny<List<LogEventWithExceptionAsJsonString>>(), It.IsAny<DataTable>()), Times.Never);
                _mockChannel.Verify(x => x.BasicAck(basicDeliverEventArgs.DeliveryTag, true), Times.Never);
            }
        }
        public static IEnumerable<object[]>
            ProcessMessage_WhenEagerlyEmitFirstEventIsFalseAndBatchSize2_ThenWritesEventWithoutTimeout_WhenMessagesGreaterThanOrEqualToBatchPostingLimitData()
        {
            yield return [new TimeSpan(0, 0, 0, 2, 300), true];
            yield return [new TimeSpan(0, 0, 0, 4, 200), true];
            yield return [new TimeSpan(0, 0, 0, 1, 0), true];
            yield return [new TimeSpan(0, 0, 0, 0, 100), true];
            yield return [new TimeSpan(0, 0, 0, 0, 200), true];
            yield return [new TimeSpan(0, 0, 0, 0, 100), true];
        }
    }
}
