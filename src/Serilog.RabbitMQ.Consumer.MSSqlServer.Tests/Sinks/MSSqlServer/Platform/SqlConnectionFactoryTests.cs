using Moq;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Platform
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SqlConnectionFactoryTests
    {
        private readonly Mock<ISqlConnectionStringBuilderWrapper> _sqlConnectionStringBuilderWrapperMock;

        public SqlConnectionFactoryTests()
        {
            _sqlConnectionStringBuilderWrapperMock = new Mock<ISqlConnectionStringBuilderWrapper>();
            _sqlConnectionStringBuilderWrapperMock.SetupAllProperties();
        }

        [Fact]
        public void IntializeThrowsIfSqlConnectionStringBuilderWrapperIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlConnectionFactory(null));
        }

        [Fact]
        public void CreateConnectionReturnsConnectionWrapper()
        {
            // Arrange
            var sut = new SqlConnectionFactory(_sqlConnectionStringBuilderWrapperMock.Object);

            // Act
            using (var connection = sut.Create())
            {
                // Assert
                Assert.NotNull(connection);
                Assert.IsAssignableFrom<ISqlConnectionWrapper>(connection);
            }
        }
    }
}
