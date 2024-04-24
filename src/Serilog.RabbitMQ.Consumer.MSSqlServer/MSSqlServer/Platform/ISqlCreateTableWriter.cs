namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    internal interface ISqlCreateTableWriter : ISqlWriter
    {
        string TableName { get; }
    }
}
