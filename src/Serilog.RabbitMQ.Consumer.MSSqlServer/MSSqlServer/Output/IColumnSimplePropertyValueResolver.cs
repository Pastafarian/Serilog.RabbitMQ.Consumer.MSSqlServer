using Serilog.Events;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    public interface IColumnSimplePropertyValueResolver
    {
        KeyValuePair<string, LogEventPropertyValue> GetPropertyValueForColumn(SqlColumn additionalColumn, IReadOnlyDictionary<string, LogEventPropertyValue> properties);
    }
}
