using Serilog.Events;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    internal class ColumnSimplePropertyValueResolver : IColumnSimplePropertyValueResolver
    {
        public KeyValuePair<string, LogEventPropertyValue> GetPropertyValueForColumn(SqlColumn additionalColumn, IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            return properties.FirstOrDefault(p => p.Key == additionalColumn.PropertyName);
        }
    }
}
