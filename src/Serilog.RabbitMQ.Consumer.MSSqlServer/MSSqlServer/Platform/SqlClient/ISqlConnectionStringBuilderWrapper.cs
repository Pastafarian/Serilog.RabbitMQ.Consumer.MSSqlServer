namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient
{
    internal interface ISqlConnectionStringBuilderWrapper
    {
        string ConnectionString { get; }
        string InitialCatalog { get; }
    }
}
