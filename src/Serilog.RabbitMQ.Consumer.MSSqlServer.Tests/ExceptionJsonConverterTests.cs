using FluentAssertions;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Xunit.Abstractions;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests
{
    public class ExceptionJsonConverterTests
    {

        private readonly ExceptionJsonConverter _sut = new();

        [Fact]
        public void Can_read_string_value_as_datetime()
        {
            // Arrange
            const string serializedException =
                "{\"HResult\":-2146233088,\"Message\":\"Test exception audit logging - unique-message-01c7914b-ecc8-4413-a73d-8041aae2f234\"," +
                "\"Source\":\"Serilog.Producer.RabbitMq.Example\",\"StackTrace\":\"   at Serilog.Producer.RabbitMq.Example.Pages.AuditExceptionModel.OnGet(String message) in " +
                "C:\\\\Git\\\\serilog-consumer-mssqlserver\\\\src\\\\Serilog.RabbitMq.Producer.Example\\\\Pages\\\\AuditException.cshtml.cs:line 19\"" +
                ",\"TargetSite\":\"Void OnGet(System.String)\",\"CustomAuditMessage\":\"My custom audit message - unique-message-01c7914b-ecc8-4413-a73d-8041aae2f234\"," +
                "\"Type\":\"Serilog.Producer.RabbitMq.Example.Exceptions.AuditLoggingException\"}";

            // Act
            var exception = _sut.Read(serializedException);

            // Assert
            exception.Should().NotBeNull();
            exception!.Message.Should().Be("Test exception audit logging - unique-message-01c7914b-ecc8-4413-a73d-8041aae2f234");
            exception.StackTrace.Should().Be("   at Serilog.Producer.RabbitMq.Example.Pages.AuditExceptionModel.OnGet(String message) in C:\\Git\\serilog-consumer-mssqlserver\\src\\Serilog.RabbitMq.Producer.Example\\Pages\\AuditException.cshtml.cs:line 19");
        }

        public class Samples
        {
            readonly ILogger _output;

            public Samples(ITestOutputHelper output)
            {
                // Pass the ITestOutputHelper object to the TestOutput sink
                _output = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.TestOutput(output, Events.LogEventLevel.Verbose)
                    .CreateLogger();
            }

            [Fact]
            public void ExampleUsage()
            {
                // Use ILogger as you normally would. These messages will show up in the test output
                _output.Information("Test output to Serilog!");

                Action sketchy = () => throw new Exception("I threw up.");
                var exception = Record.Exception(sketchy);

                _output.Error(exception, "Here is an error.");
                Assert.NotNull(exception);
            }
        }
    }
}
