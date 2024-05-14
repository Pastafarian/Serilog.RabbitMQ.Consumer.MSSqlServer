using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Platform.SqlClient
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SqlConnectionStringBuilderWrapperTests
    {
        [Fact]
        public void ChangeEnlistFalseToTrueIfEnlistPropertyIsSetToTrue()
        {
            // Arrange + act
            var sut = new SqlConnectionStringBuilderWrapper(DatabaseFixture.LogEventsConnectionString + ";Enlist=False", true);

            // Assert
            Assert.Equal(DatabaseFixture.LogEventsConnectionString + ";Enlist=True", sut.ConnectionString);
        }

        [Fact]
        public void ChangeEnlistTrueToFalseIfEnlistPropertyIsSetToFalse()
        {
            // Arrange
            var sut = new SqlConnectionStringBuilderWrapper(DatabaseFixture.LogEventsConnectionString + ";Enlist=True", false);

            // Assert
            Assert.Equal(DatabaseFixture.LogEventsConnectionString + ";Enlist=False", sut.ConnectionString);
        }

        [Fact]
        public void AddsEnlistFalseIfEnlistPropertySetToFalse()
        {
            // Arrange
            var sut = new SqlConnectionStringBuilderWrapper(DatabaseFixture.LogEventsConnectionString, false);

            // Assert
            Assert.Equal(DatabaseFixture.LogEventsConnectionString + ";Enlist=False", sut.ConnectionString);
        }

        [Fact]
        public void AddsEnlistTrueIfEnlistPropertySetToTrue()
        {
            // Arrange
            var sut = new SqlConnectionStringBuilderWrapper(DatabaseFixture.LogEventsConnectionString, true);

            // Assert
            Assert.Equal(DatabaseFixture.LogEventsConnectionString + ";Enlist=True", sut.ConnectionString);
        }

        [Fact]
        public void DoesNotDuplicateEnlistIfEnlistFalseIsPresentAndEnlistPropertySetToFalse()
        {
            // Arrange
            var sut = new SqlConnectionStringBuilderWrapper("Enlist = false ; " + DatabaseFixture.LogEventsConnectionString, false);

            // Assert
            Assert.Equal(DatabaseFixture.LogEventsConnectionString + ";Enlist=False", sut.ConnectionString);
        }

    }
}
