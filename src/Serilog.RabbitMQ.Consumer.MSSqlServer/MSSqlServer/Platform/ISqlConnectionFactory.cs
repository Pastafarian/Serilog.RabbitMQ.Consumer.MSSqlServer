using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    internal interface ISqlConnectionFactory
    {
        ISqlConnectionWrapper Create();
    }
}
