using System.Data;
using System.Globalization;
using System.Text;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Extensions;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    internal class StandardColumnDataGenerator : IStandardColumnDataGenerator
    {
        private readonly ColumnOptions.ColumnOptions _columnOptions;
        private readonly IFormatProvider _formatProvider;
        private readonly IXmlPropertyFormatter _xmlPropertyFormatter;
        private readonly ITextFormatterLogEventWithExceptionAsJsonString _logEventFormatter;
        private readonly ISet<string> _additionalColumnPropertyNames;

        public StandardColumnDataGenerator(
            ColumnOptions.ColumnOptions columnOptions,
            IXmlPropertyFormatter xmlPropertyFormatter,
            ITextFormatterLogEventWithExceptionAsJsonString logEventFormatter)
        {
            _columnOptions = columnOptions ?? throw new ArgumentNullException(nameof(columnOptions));


            _xmlPropertyFormatter = xmlPropertyFormatter ?? throw new ArgumentNullException(nameof(xmlPropertyFormatter));

            if (_columnOptions.Store.Contains(StandardColumn.LogEvent))
                _logEventFormatter = logEventFormatter ?? new JsonLogEventFormatter(_columnOptions, this);

            _additionalColumnPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_columnOptions.AdditionalColumns != null)
                foreach (var col in _columnOptions.AdditionalColumns)
                    _additionalColumnPropertyNames.Add(col.PropertyName);
        }

        public KeyValuePair<string, object?> GetStandardColumnNameAndValue(StandardColumn column,
            LogEventWithExceptionAsJsonString logEvent)
        {
            switch (column)
            {
                case StandardColumn.Message:
                    return new KeyValuePair<string, object?>(_columnOptions.Message.ColumnName, logEvent.RenderMessage(_formatProvider).TruncateOutput(_columnOptions.Message.DataLength));
                case StandardColumn.MessageTemplate:
                    return new KeyValuePair<string, object?>(_columnOptions.MessageTemplate.ColumnName, logEvent.MessageTemplate.Text.TruncateOutput(_columnOptions.MessageTemplate.DataLength));
                case StandardColumn.Level:
                    return new KeyValuePair<string, object?>(_columnOptions.Level.ColumnName, _columnOptions.Level.StoreAsEnum ? (object)logEvent.Level : logEvent.Level.ToString());
                case StandardColumn.TraceId:
                    return new KeyValuePair<string, object?>(_columnOptions.TraceId.ColumnName, logEvent.TraceId);
                case StandardColumn.SpanId:
                    return new KeyValuePair<string, object?>(_columnOptions.SpanId.ColumnName, logEvent.SpanId);
                case StandardColumn.TimeStamp:
                    return GetTimeStampStandardColumnNameAndValue(logEvent);
                case StandardColumn.Exception:
                    return new KeyValuePair<string, object?>(_columnOptions.Exception.ColumnName, logEvent.JsonException?.TruncateOutput(_columnOptions.Exception.DataLength));
                case StandardColumn.Properties:
                    return new KeyValuePair<string, object?>(_columnOptions.Properties.ColumnName, ConvertPropertiesToXmlStructure(logEvent.Properties));
                case StandardColumn.LogEvent:
                    return new KeyValuePair<string, object?>(_columnOptions.LogEvent.ColumnName, RenderLogEventColumn(logEvent));
                default:
                    throw new ArgumentOutOfRangeException(nameof(column));
            }
        }

        private KeyValuePair<string, object?> GetTimeStampStandardColumnNameAndValue(LogEventWithExceptionAsJsonString logEvent)
        {
            var dateTimeOffset = _columnOptions.TimeStamp.ConvertToUtc ? logEvent.Timestamp.ToUniversalTime() : logEvent.Timestamp;

            if (_columnOptions.TimeStamp.DataType == SqlDbType.DateTimeOffset)
                return new KeyValuePair<string, object?>(_columnOptions.TimeStamp.ColumnName, dateTimeOffset);

            return new KeyValuePair<string, object?>(_columnOptions.TimeStamp.ColumnName, dateTimeOffset.DateTime);
        }

        private string RenderLogEventColumn(LogEventWithExceptionAsJsonString logEvent)
        {
            LogEventWithExceptionAsJsonString preparedLogEvent;
            if (_columnOptions.LogEvent.ExcludeAdditionalProperties)
            {
                var filteredProperties = logEvent.Properties.Where(p => !_additionalColumnPropertyNames.Contains(p.Key));
                preparedLogEvent = new LogEventWithExceptionAsJsonString(new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.Exception, logEvent.MessageTemplate,
                    filteredProperties.Select(x => new LogEventProperty(x.Key, x.Value))), logEvent.JsonException);
            }
            else
            {
                preparedLogEvent = logEvent;
            }

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                _logEventFormatter.Format(preparedLogEvent, writer);
            return sb.ToString();
        }

        private string ConvertPropertiesToXmlStructure(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties)
        {
            var options = _columnOptions.Properties;

            if (options.ExcludeAdditionalProperties)
                properties = properties.Where(p => !_additionalColumnPropertyNames.Contains(p.Key));

            if (options.PropertiesFilter != null)
            {
                try
                {
                    properties = properties.Where(p => options.PropertiesFilter(p.Key));
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Unable to filter properties to store in {0} due to following error: {1}", this, ex);
                }
            }

            var sb = new StringBuilder();

            sb.AppendFormat(CultureInfo.InvariantCulture, "<{0}>", options.RootElementName);

            foreach (var property in properties)
            {
                var value = _xmlPropertyFormatter.Simplify(property.Value, options);
                if (options.OmitElementIfEmpty && string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (options.UsePropertyKeyAsElementName)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "<{0}>{1}</{0}>", _xmlPropertyFormatter.GetValidElementName(property.Key), value);
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "<{0} key='{1}'>{2}</{0}>", options.PropertyElementName, property.Key, value);
                }
            }

            sb.AppendFormat(CultureInfo.InvariantCulture, "</{0}>", options.RootElementName);

            return sb.ToString();
        }
    }
}
