using Microsoft.Data.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Platform.SqlClient
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SqlCommandWrapperTests
    {
        [Fact]
        public void InitializeThrowsIfSqlCommandIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlCommandWrapper(null));
        }

        [Fact]
        public void AddParameterDoesNotThrow()
        {
            // Arrange
            using (var sqlCommand = new SqlCommand())
            {
                using (var sut = new SqlCommandWrapper(sqlCommand))
                {
                    // Act (should not throw)
                    sut.AddParameter("Parameter", "Value");
                }
            }
        }
    }
}
