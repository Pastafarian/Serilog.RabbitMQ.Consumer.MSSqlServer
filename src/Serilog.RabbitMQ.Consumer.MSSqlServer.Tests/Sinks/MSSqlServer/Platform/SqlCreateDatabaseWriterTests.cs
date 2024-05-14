using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Platform
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SqlCreateDatabaseWriterTests
    {
        [Fact]
        public void GetSqlWritesCorrectCommand()
        {
            // Arrange
            const string databaseName = "LogDatabase";
            const string expectedResult = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'LogDatabase')\r\nBEGIN\r\nCREATE DATABASE [LogDatabase]\r\nEND\r\n";
            var sut = new SqlCreateDatabaseWriter(databaseName);

            // Act
            var result = sut.GetSql();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void GetSqlWritesCorrectCommandForDatabaseNameWithSpaces()
        {
            // Arrange
            const string databaseName = "Log Data Base";
            const string expectedResult = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Log Data Base')\r\nBEGIN\r\nCREATE DATABASE [Log Data Base]\r\nEND\r\n";
            var sut = new SqlCreateDatabaseWriter(databaseName);

            // Act
            var result = sut.GetSql();

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
