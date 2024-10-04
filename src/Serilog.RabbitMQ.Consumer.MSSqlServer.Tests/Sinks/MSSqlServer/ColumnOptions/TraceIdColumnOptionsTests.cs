using System.Data;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.ColumnOptions
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class TraceIdColumnOptionsTests
    {
        [Fact]
        public void CanSetDataTypeNVarChar()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act - should not throw
            options.TraceId.DataType = SqlDbType.NVarChar;
        }

        [Fact]
        public void CanSetDataTypeVarChar()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act - should not throw
            options.TraceId.DataType = SqlDbType.VarChar;
        }

        [Fact]
        public void CannotSetDataTypeBigInt()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act and assert - should throw
            Assert.Throws<ArgumentException>(() => options.TraceId.DataType = SqlDbType.BigInt);
        }

        [Fact]
        public void CannotSetDataTypeNChar()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act and assert - should throw
            Assert.Throws<ArgumentException>(() => options.TraceId.DataType = SqlDbType.NChar);
        }
    }
}
