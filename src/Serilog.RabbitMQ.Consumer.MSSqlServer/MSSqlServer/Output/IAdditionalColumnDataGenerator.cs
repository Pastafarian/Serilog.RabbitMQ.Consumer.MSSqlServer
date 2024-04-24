using Serilog.Events;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    internal interface IAdditionalColumnDataGenerator
    {
        KeyValuePair<string, object> GetAdditionalColumnNameAndValue(SqlColumn additionalColumn, IReadOnlyDictionary<string, LogEventPropertyValue> properties);
    }
}
