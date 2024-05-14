using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Platform.SqlClient
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SqlConnectionWrapperTests
    {
        [Fact]
        public void CreateCommandReturnsSqlCommandWrapper()
        {
            // Arrange
            using (var sut = new SqlConnectionWrapper(DatabaseFixture.LogEventsConnectionString))
            {
                // Act
                var result = sut.CreateCommand();

                // Assert
                Assert.NotNull(result);
            }
        }

        [Fact]
        public void CreateCommandWithParameterReturnsSqlCommandWrapper()
        {
            // Arrange
            using (var sut = new SqlConnectionWrapper(DatabaseFixture.LogEventsConnectionString))
            {
                // Act
                var result = sut.CreateCommand("CommandText");

                // Assert
                Assert.NotNull(result);
                Assert.Equal("CommandText", result.CommandText);
            }
        }

        [Fact]
        public void CreateSqlBulkCopyReturnsSqlBulkCopyWrapper()
        {
            // Arrange
            using (var sut = new SqlConnectionWrapper(DatabaseFixture.LogEventsConnectionString))
            {
                // Act
                var result = sut.CreateSqlBulkCopy(false, "DestinationTableName");

                // Assert
                Assert.NotNull(result);
            }
        }
    }
}
