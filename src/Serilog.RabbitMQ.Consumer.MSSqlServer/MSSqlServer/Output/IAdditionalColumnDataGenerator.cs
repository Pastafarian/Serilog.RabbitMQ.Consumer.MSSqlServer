using Serilog.Events;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    public interface IAdditionalColumnDataGenerator
    {
        KeyValuePair<string, object> GetAdditionalColumnNameAndValue(SqlColumn additionalColumn, IReadOnlyDictionary<string, LogEventPropertyValue> properties);
    }
}
