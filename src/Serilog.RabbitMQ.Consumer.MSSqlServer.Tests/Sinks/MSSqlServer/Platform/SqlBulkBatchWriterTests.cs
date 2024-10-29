using System.Data;
using System.Globalization;
using Moq;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Platform
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SqlBulkBatchWriterTests : IDisposable
    {
        private const string TableName = "TestTableName";
        private const string SchemaName = "TestSchemaName";
        private readonly Mock<ISqlConnectionFactory> _sqlConnectionFactoryMock;
        private readonly Mock<ILogEventDataGenerator> _logEventDataGeneratorMock;
        private readonly Mock<ISqlConnectionWrapper> _sqlConnectionWrapperMock;
        private readonly Mock<ISqlBulkCopyWrapper> _sqlBulkCopyWrapper;
        private readonly DataTable _dataTable;
        private readonly SqlBulkBatchWriter _sut;
        private bool _disposedValue;

        public SqlBulkBatchWriterTests()
        {
            _sqlConnectionFactoryMock = new Mock<ISqlConnectionFactory>();
            _logEventDataGeneratorMock = new Mock<ILogEventDataGenerator>();
            _sqlConnectionWrapperMock = new Mock<ISqlConnectionWrapper>();
            _sqlBulkCopyWrapper = new Mock<ISqlBulkCopyWrapper>();

            _sqlConnectionFactoryMock.Setup(f => f.Create()).Returns(_sqlConnectionWrapperMock.Object);
            _sqlConnectionWrapperMock.Setup(c => c.CreateSqlBulkCopy(It.IsAny<bool>(), It.IsAny<string>())).Returns(_sqlBulkCopyWrapper.Object);

            _dataTable = new DataTable(TableName);

            _sut = new SqlBulkBatchWriter(TableName, SchemaName, false, _sqlConnectionFactoryMock.Object, _logEventDataGeneratorMock.Object);
        }

        [Fact]
        public void InitializeWithoutTableNameThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlBulkBatchWriter(null!, SchemaName, false, _sqlConnectionFactoryMock.Object, _logEventDataGeneratorMock.Object));
        }

        [Fact]
        public void InitializeWithoutSchemaNameThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlBulkBatchWriter(TableName, null!, false, _sqlConnectionFactoryMock.Object, _logEventDataGeneratorMock.Object));
        }

        [Fact]
        public void InitializeWithoutSqlConnectionFactoryThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlBulkBatchWriter(TableName, SchemaName, false, null!, _logEventDataGeneratorMock.Object));
        }

        [Fact]
        public void InitializeWithoutLogEventDataGeneratorThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlBulkBatchWriter(TableName, SchemaName, false, _sqlConnectionFactoryMock.Object, null!));
        }

        [Fact]
        public async Task WriteBatchCallsLogEventDataGeneratorGetColumnsAndValuesForEachLogEvent()
        {
            // Arrange
            var logEvents = CreateLogEvents();

            // Act
            await _sut.WriteBatch(logEvents, _dataTable);

            // Assert
            _logEventDataGeneratorMock.Verify(c => c.GetColumnsAndValues(logEvents[0]), Times.Once);
            _logEventDataGeneratorMock.Verify(c => c.GetColumnsAndValues(logEvents[1]), Times.Once);
        }

        [Fact]
        public async Task WriteBatchCallsSqlConnectionFactoryCreate()
        {
            // Arrange
            var logEvents = CreateLogEvents();

            // Act
            await _sut.WriteBatch(logEvents, _dataTable);

            // Assert
            _sqlConnectionFactoryMock.Verify(f => f.Create(), Times.Once);
        }

        [Fact]
        public async Task WriteBatchCallsSqlConnectionWrapperOpenAsync()
        {
            // Arrange
            var logEvents = CreateLogEvents();

            // Act
            await _sut.WriteBatch(logEvents, _dataTable);

            // Assert
            _sqlConnectionWrapperMock.Verify(c => c.OpenAsync(), Times.Once);
        }

        [Fact]
        public async Task WriteBatchCallsSqlConnectionWrapperCreateSqlBulkCopy()
        {
            // Arrange
            var logEvents = CreateLogEvents();
            var expectedDestinationTableName = string.Format(CultureInfo.InvariantCulture, "[{0}].[{1}]", SchemaName, TableName);

            // Act
            await _sut.WriteBatch(logEvents, _dataTable);

            // Assert
            _sqlConnectionWrapperMock.Verify(c => c.CreateSqlBulkCopy(false, expectedDestinationTableName), Times.Once);
        }

        [Fact]
        public async Task WriteBatchCallsSqlConnectionWrapperCreateSqlBulkCopyWithDisableTriggersTrue()
        {
            // Arrange
            var logEvents = CreateLogEvents();
            var expectedDestinationTableName = string.Format(CultureInfo.InvariantCulture, "[{0}].[{1}]", SchemaName, TableName);
            var sut = new SqlBulkBatchWriter(TableName, SchemaName, true, _sqlConnectionFactoryMock.Object, _logEventDataGeneratorMock.Object);

            // Act
            await sut.WriteBatch(logEvents, _dataTable);

            // Assert
            _sqlConnectionWrapperMock.Verify(c => c.CreateSqlBulkCopy(true, expectedDestinationTableName), Times.Once);
        }

        [Fact]
        public async Task WriteBatchCallsSqlBulkCopyWrapperAddSqlBulkCopyColumnMappingForEachColumn()
        {
            // Arrange
            const string column1Name = "Colum1";
            const string column2Name = "Colum2";
            var logEvents = CreateLogEvents();
            _dataTable.Columns.Add(new DataColumn(column1Name));
            _dataTable.Columns.Add(new DataColumn(column2Name));

            // Act
            await _sut.WriteBatch(logEvents, _dataTable);

            // Assert
            _sqlBulkCopyWrapper.Verify(c => c.AddSqlBulkCopyColumnMapping(column1Name, column1Name), Times.Once);
            _sqlBulkCopyWrapper.Verify(c => c.AddSqlBulkCopyColumnMapping(column2Name, column2Name), Times.Once);
        }

        [Fact]
        public async Task WriteBatchCallsSqlBulkCopyWrapperWriteToServerAsync()
        {
            // Arrange
            var logEvents = CreateLogEvents();

            // Act
            await _sut.WriteBatch(logEvents, _dataTable);

            // Assert
            _sqlBulkCopyWrapper.Verify(c => c.WriteToServerAsync(_dataTable), Times.Once);
        }

        [Fact]
        public async Task WriteBatchClearsDataTable()
        {
            // Arrange
            var logEvents = CreateLogEvents();

            // Act
            await _sut.WriteBatch(logEvents, _dataTable);

            // Assert
            Assert.Empty(_dataTable.Rows);
        }

        [Fact]
        public async Task WriteBatchRethrowsIfLogEventDataGeneratorMockGetColumnsAndValuesThrows()
        {
            // Arrange
            _logEventDataGeneratorMock.Setup(d => d.GetColumnsAndValues(It.IsAny<LogEventWithExceptionAsJsonString>()))
                .Callback(() => throw new InvalidOperationException());
            var logEvents = CreateLogEvents();

            // Act + assert
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.WriteBatch(logEvents, _dataTable));
        }

        [Fact]
        public async Task WriteBatchRethrowsIfSqlConnectionFactoryCreateThrows()
        {
            // Arrange
            _sqlConnectionFactoryMock.Setup(f => f.Create()).Callback(() => throw new InvalidOperationException());
            var logEvents = CreateLogEvents();

            // Act + assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.WriteBatch(logEvents, _dataTable));
        }

        [Fact]
        public async Task WriteBatchRethrowsIfSqlConnectionOpenAsyncThrows()
        {
            // Arrange
            _sqlConnectionWrapperMock.Setup(c => c.OpenAsync()).Callback(() => throw new InvalidOperationException());
            var logEvents = CreateLogEvents();

            // Act + assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.WriteBatch(logEvents, _dataTable));
        }

        [Fact]
        public async Task WriteBatchRethrowsIfSqlBulkCopyWriterAddSqlBulkCopyColumnMappingThrows()
        {
            // Arrange
            _sqlBulkCopyWrapper.Setup(c => c.AddSqlBulkCopyColumnMapping(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => throw new InvalidOperationException());
            var logEvents = CreateLogEvents();
            _dataTable.Columns.Add(new DataColumn("ColumnName"));

            // Act + assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.WriteBatch(logEvents, _dataTable));
        }

        [Fact]
        public async Task WriteBatchRethrowsIfSqlBulkCopyWriterWriteToServerAsyncThrows()
        {
            // Arrange
            _sqlBulkCopyWrapper.Setup(c => c.WriteToServerAsync(It.IsAny<DataTable>()))
                .Callback(() => throw new InvalidOperationException());
            var logEvents = CreateLogEvents();

            // Act + assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.WriteBatch(logEvents, _dataTable));
        }

        private static List<LogEventWithExceptionAsJsonString> CreateLogEvents()
        {
            var logEvents = new List<LogEventWithExceptionAsJsonString>
            {
                TestLogEventHelper.CreateLogEvent(),
                TestLogEventHelper.CreateLogEvent()
            };
            return logEvents;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _dataTable.Dispose();
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
