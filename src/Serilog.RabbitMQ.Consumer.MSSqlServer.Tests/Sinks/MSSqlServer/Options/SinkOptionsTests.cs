using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Options;
using Serilog.Sinks.MSSqlServer.Tests.TestUtils;

namespace Serilog.Sinks.MSSqlServer.Tests.Options
{
    [Obsolete("Backwards compatibility tests for old SinkOptions class", error: false)]
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class SinkOptionsTests
    {
        [Fact]
        public void InitializesDefaultedPropertiesWithDefaultsWhenCalledWithoutParameters()
        {
            // Act
            var sut = new SinkOptions();

            // Assert
            //Assert.Equal(MSSqlServerSink.DefaultSchemaName, sut.SchemaName);
            //Assert.Equal(MSSqlServerSink.DefaultBatchPostingLimit, sut.BatchPostingLimit);
            //Assert.Equal(MSSqlServerSink.DefaultPeriod, sut.BatchPeriod);
        }
    }
}
