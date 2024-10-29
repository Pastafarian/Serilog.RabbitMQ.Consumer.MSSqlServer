using System.Diagnostics;
using Serilog.Events;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    public class ColumnSimplePropertyValueResolver : IColumnSimplePropertyValueResolver
    {
        public KeyValuePair<string, LogEventPropertyValue> GetPropertyValueForColumn(SqlColumn additionalColumn, IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            var messghe = $"GetPropertyValueForColumn ! additionalColumn.PropertyName - {additionalColumn.PropertyName}, properties - " + string.Join(",", properties.Select(s => "Key: " + s.Key + " Value: " + s.Value));
            Debug.Print(messghe);

            var result = properties.FirstOrDefault(p => p.Key == additionalColumn.PropertyName);
            Debug.Print(" GetPropertyValueForColumn result = key - " + result.Key + " value " + result.Value);

            return result;
        }
    }
}
