using System.Text;
using AutoFixture;
using AutoFixture.AutoMoq;
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
    public interface IFakeMessageProcessor
    {
        Task ProcessMessage(LogEventWithExceptionAsJsonString message, BasicDeliverEventArgs basicDeliverEventArgs);
    }
    public class FakeWorkerService : WorkerService<FakeWorkerService>
    {
        private readonly IFakeMessageProcessor _fakeMessageProcessor;

        internal FakeWorkerService(ILogger<FakeWorkerService> logger, IRabbitConnectionFactory connectionFactory, MSSqlServerSinkOptions sinkOptions, ISinkDependencies sinkDependencies, string queueName, RabbitMqClientConsumerConfiguration rabbitMqConfiguration, IAsyncEventingBasicConsumerFactory asyncEventingBasicConsumerFactory, IFakeMessageProcessor fakeMessageProcessor) : base(logger, connectionFactory, sinkOptions, sinkDependencies, queueName, rabbitMqConfiguration, asyncEventingBasicConsumerFactory)
        {
            _fakeMessageProcessor = fakeMessageProcessor;
        }

        public override Task ProcessMessage(LogEventWithExceptionAsJsonString message, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            return _fakeMessageProcessor.ProcessMessage(message, basicDeliverEventArgs);
        }

        public async Task Execute()
        {
            await base.ExecuteAsync(new CancellationToken());
        }
    }


    public class WorkerServiceTests
    {
        private readonly Mock<ILogger<FakeWorkerService>> _logger = new();
        private readonly Mock<IRabbitConnectionFactory> _connectionFactory = new();
        private readonly MSSqlServerSinkOptions _sinkOptions = new();
        private readonly Mock<ISinkDependencies> _sinkDependencies = new();
        private RabbitMqClientConsumerConfiguration _rabbitMqConfiguration = new();
        private readonly Mock<IAsyncEventingBasicConsumerFactory> _asyncEventingBasicConsumerFactory = new();
        private FakeWorkerService? _sut;
        private readonly Mock<IFakeMessageProcessor> _fakeMessageProcessor = new();
        private void CreateFakeWorkerService()
        {
            _sinkDependencies.Setup(x
                => x.SqlDatabaseCreator).Returns(new Mock<ISqlCommandExecutor>().Object);
            _sinkDependencies.Setup(x
                => x.DataTableCreator).Returns(new Mock<IDataTableCreator>().Object);
            _sinkDependencies.Setup(x
                => x.SqlLogEventWriter).Returns(new Mock<ISqlLogEventWriter>().Object);
            _sinkDependencies.Setup(x
                => x.SqlTableCreator).Returns(new Mock<ISqlCommandExecutor>().Object);
            _sut = new FakeWorkerService(_logger.Object, _connectionFactory.Object, _sinkOptions, _sinkDependencies.Object, "fake-queue-name", _rabbitMqConfiguration, _asyncEventingBasicConsumerFactory.Object, _fakeMessageProcessor.Object);
        }

        [Fact]
        public void WorkerService_Constructor_Calls_SqlDatabaseCreator_When_AutoCreateSqlDatabase_Is_True()
        {
            // Arrange
            _sinkOptions.AutoCreateSqlDatabase = true;

            // Act
            CreateFakeWorkerService();

            // Assert
            _sinkDependencies.Verify(x => x.SqlDatabaseCreator.Execute(), Times.Once);
        }

        [Fact]
        public void WorkerService_Constructor_DoesNotCall_SqlDatabaseCreator_When_AutoCreateSqlDatabase_Is_False()
        {
            // Arrange
            _sinkOptions.AutoCreateSqlDatabase = false;

            // Act
            CreateFakeWorkerService();

            // Assert
            _sinkDependencies.Verify(x => x.SqlDatabaseCreator.Execute(), Times.Never);
        }

        [Fact]
        public void WorkerService_Constructor_Calls_SqlTableCreator_When_AutoCreateSqlTable_Is_True()
        {
            // Arrange
            _sinkOptions.AutoCreateSqlTable = true;

            // Act
            CreateFakeWorkerService();

            // Assert
            _sinkDependencies.Verify(x => x.SqlTableCreator.Execute(), Times.Once);
        }

        [Fact]
        public void WorkerService_Constructor_DoesNotCall_SqlTableCreator_When_AutoCreateSqlTable_Is_False()
        {
            // Arrange
            _sinkOptions.AutoCreateSqlTable = false;

            // Act
            CreateFakeWorkerService();

            // Assert
            _sinkDependencies.Verify(x => x.SqlTableCreator.Execute(), Times.Never);
        }

        [Fact]
        public async Task WorkerService_Execute_CreatesQueuesAndExchanges_WhenAutoCreateExchange_IsTrue()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            _rabbitMqConfiguration = fixture.Build<RabbitMqClientConsumerConfiguration>()
                .Create();
            _rabbitMqConfiguration.AutoCreateExchange = true;
            CreateFakeWorkerService();
            var mockConnection = new Mock<IConnection>();
            var mockModel = new Mock<IModel>();
            mockConnection.Setup(x => x.CreateModel()).Returns(mockModel.Object);
            _connectionFactory.Setup(x => x.GetConnectionAsync()).ReturnsAsync(mockConnection.Object);
            var fakeConsumer = new AsyncEventingBasicConsumer(mockModel.Object);
            _asyncEventingBasicConsumerFactory.Setup(x => x.CreateConsumer(mockModel.Object)).Returns(fakeConsumer);

            // Act
            await _sut!.Execute();

            // Assert
            _connectionFactory.Verify(x => x.GetConnectionAsync(), Times.Once);
            mockModel.Verify(x => x.ExchangeDeclare(_rabbitMqConfiguration.LoggingExchangeName, _rabbitMqConfiguration.ExchangeType, true, false, null), Times.Once);
            mockModel.Verify(x => x.ExchangeDeclare(_rabbitMqConfiguration.AuditExchangeName, _rabbitMqConfiguration.ExchangeType, true, false, null), Times.Once);
            mockModel.Verify(x => x.QueueDeclare(_rabbitMqConfiguration.LoggingQueueName, true, false, false, null), Times.Once);
            mockModel.Verify(x => x.QueueDeclare(_rabbitMqConfiguration.AuditQueueName, true, false, false, null), Times.Once);
            mockModel.Verify(x => x.QueueBind(_rabbitMqConfiguration.LoggingQueueName, _rabbitMqConfiguration.LoggingExchangeName, _rabbitMqConfiguration.RouteKey, null), Times.Once);
            mockModel.Verify(x => x.QueueBind(_rabbitMqConfiguration.AuditQueueName, _rabbitMqConfiguration.AuditExchangeName, _rabbitMqConfiguration.RouteKey, null), Times.Once);
        }

        [Fact]
        public async Task WorkerService_Execute_DoesNotCreateQueuesAndExchanges_WhenAutoCreateExchange_IsFalse()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            _rabbitMqConfiguration = fixture.Build<RabbitMqClientConsumerConfiguration>()
                .Create();
            _rabbitMqConfiguration.AutoCreateExchange = false;
            CreateFakeWorkerService();
            var mockConnection = new Mock<IConnection>();
            var mockModel = new Mock<IModel>();
            mockConnection.Setup(x => x.CreateModel()).Returns(mockModel.Object);
            _connectionFactory.Setup(x => x.GetConnectionAsync()).ReturnsAsync(mockConnection.Object);
            var fakeConsumer = new AsyncEventingBasicConsumer(mockModel.Object);
            _asyncEventingBasicConsumerFactory.Setup(x => x.CreateConsumer(mockModel.Object)).Returns(fakeConsumer);

            // Act
            await _sut!.Execute();

            // Assert
            _connectionFactory.Verify(x => x.GetConnectionAsync(), Times.Once);
            mockModel.Verify(x => x.ExchangeDeclare(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
            mockModel.Verify(x => x.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), false, It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
            mockModel.Verify(x => x.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task WorkerService_Execute_WhenReceivesMessage_ThenSuccessfullyDeserializesMessage()
        {
            // Arrange
            var uniqueGuid = Guid.NewGuid().ToString();
            var messageJson =
                "{\"Timestamp\":\"2024-04-14T01:08:28.2776075+01:00\",\"Level\":\"Error\",\"MessageTemplate\":\"Audit Error " +
                uniqueGuid + "\",\"RenderedMessage\":\"Audit Error " + uniqueGuid + "\"," +
                "\"TraceId\":\"ff046c23e17602d5443db6b414f598bf\",\"SpanId\":\"bc52863049fc271a\"," +
                "\"Exception\":\"Serilog.Producer.RabbitMq.Example.Exceptions.AuditLoggingException: Test exception audit logging - unique-message-" + uniqueGuid + "\\r\\n   at Serilog.Producer.RabbitMq.Example.Pages.AuditExceptionModel.OnGet(String message) in C:\\\\Git\\\\serilog-consumer-mssqlserver\\\\src\\\\Serilog.RabbitMq.Producer.Example\\\\Pages\\\\AuditException.cshtml.cs:line 19\",\"Properties\":{\"ExceptionDetail\":{\"HResult\":-2146233088,\"Message\":\"Test exception audit logging - unique-message-" + uniqueGuid + "\",\"Source\":\"Serilog.Producer.RabbitMq.Example\",\"StackTrace\":\"   at Serilog.Producer.RabbitMq.Example.Pages.AuditExceptionModel.OnGet(String message) in C:\\\\Git\\\\serilog-consumer-mssqlserver\\\\src\\\\Serilog.RabbitMq.Producer.Example\\\\Pages\\\\AuditException.cshtml.cs:line 19" + uniqueGuid + "\",\"TargetSite\":\"Void OnGet(System.String)\",\"CustomAuditMessage\":\"My custom audit message - unique-message-" + uniqueGuid + "\",\"Type\":\"Serilog.Producer.RabbitMq.Example.Exceptions.AuditLoggingException\"},\"MachineName\":\"STEVE-DESKTOP\",\"ActionId\":\"9eaf49fa-2cb2-4e68-83f7-e80e987583bf\",\"ActionName\":\"/AuditException\",\"RequestId\":\"0HN2S2RRSGIFC\",\"RequestPath\":\"/AuditException\",\"ThreadId\":25,\"CorrelationId\":\"e8863042-d23c-4b54-a0f0-4fa895ede2eb\",\"PublishTimestamp\":\"2024-04-14T00:08:28.3019024+00:00\"}}";
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            _rabbitMqConfiguration = fixture.Build<RabbitMqClientConsumerConfiguration>()
                .Create();
            CreateFakeWorkerService();
            var mockConnection = new Mock<IConnection>();
            var mockModel = new Mock<IModel>();
            mockConnection.Setup(x => x.CreateModel()).Returns(mockModel.Object);
            _connectionFactory.Setup(x => x.GetConnectionAsync()).ReturnsAsync(mockConnection.Object);
            var fakeConsumer = new AsyncEventingBasicConsumer(mockModel.Object);
            _asyncEventingBasicConsumerFactory.Setup(x => x.CreateConsumer(mockModel.Object)).Returns(fakeConsumer);
            var mockBasicProperties = new Mock<IBasicProperties>();

            // Act
            await _sut!.Execute();
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await fakeConsumer.HandleBasicDeliver("", 2, false, "", "", mockBasicProperties.Object, messageBytes);

            // Assert
            _fakeMessageProcessor.Verify(x =>
                x.ProcessMessage(
                    It.Is<LogEventWithExceptionAsJsonString>(
                        y => y.MessageTemplate.Text == "Audit Error " + uniqueGuid &&
                             y.Exception!.Message.StartsWith("Test exception audit logging - unique-message-" + uniqueGuid) &&
                                                             y.Level == LogEventLevel.Error),
                    It.IsAny<BasicDeliverEventArgs>()), Times.Once);
        }

    }
}
