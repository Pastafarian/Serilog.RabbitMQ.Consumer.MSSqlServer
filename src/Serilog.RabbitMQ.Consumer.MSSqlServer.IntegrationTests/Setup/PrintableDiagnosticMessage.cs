using Xunit.Sdk;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

/// <summary>
/// See https://github.com/xunit/xunit/pull/2148#issuecomment-839838421.
/// </summary>
internal sealed class PrintableDiagnosticMessage(string message) : DiagnosticMessage(message)
{
    public override string ToString() => Message;
}