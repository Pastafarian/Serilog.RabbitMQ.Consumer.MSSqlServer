namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    internal interface ISqlCreateDatabaseWriter : ISqlWriter
    {
        string DatabaseName { get; }
    }
}
