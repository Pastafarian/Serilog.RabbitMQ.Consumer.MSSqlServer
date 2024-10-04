using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.ColumnOptions
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class ColumnOptionsTests
    {
        [Fact]
        public void GetStandardColumnOptionsReturnsTraceIdOptions()
        {
            // Arrange
            var sut = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act
            var result = sut.GetStandardColumnOptions(StandardColumn.TraceId);

            // Assert
            Assert.Same(sut.TraceId, result);
        }

        [Fact]
        public void GetStandardColumnOptionsReturnsSpanIdOptions()
        {
            // Arrange
            var sut = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act
            var result = sut.GetStandardColumnOptions(StandardColumn.SpanId);

            // Assert
            Assert.Same(sut.SpanId, result);
        }
    }
}
