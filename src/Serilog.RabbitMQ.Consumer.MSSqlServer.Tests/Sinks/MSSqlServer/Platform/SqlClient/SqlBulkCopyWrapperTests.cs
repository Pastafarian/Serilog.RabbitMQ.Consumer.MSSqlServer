using Microsoft.Data.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Platform.SqlClient
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SqlBulkCopyWrapperTests
    {
        [Fact]
        public void InitializeThrowsIfSqlBulkCopyIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlBulkCopyWrapper(null));
        }

        [Fact]
        public void AddSqlBulkCopyColumnMappingDoesNotThrow()
        {
            // Arrange
            using (var connection = new SqlConnection())
            {
                using (var sqlBulkCopy = new SqlBulkCopy(connection))
                {
                    using (var sut = new SqlBulkCopyWrapper(sqlBulkCopy))
                    {
                        // Act (should not throw)
                        sut.AddSqlBulkCopyColumnMapping("Column", "Column");
                    }
                }
            }
        }
    }
}
