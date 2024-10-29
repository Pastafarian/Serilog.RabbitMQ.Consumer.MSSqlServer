using System.Diagnostics;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output
{
    public class LogEventDataGenerator : ILogEventDataGenerator
    {
        private readonly ColumnOptions.ColumnOptions _columnOptions;
        private readonly IStandardColumnDataGenerator _standardColumnDataGenerator;
        private readonly IAdditionalColumnDataGenerator _additionalColumnDataGenerator;
        private readonly string _identity = "" + Guid.NewGuid();
        public LogEventDataGenerator(
            ColumnOptions.ColumnOptions columnOptions,
            IStandardColumnDataGenerator standardColumnDataGenerator,
            IAdditionalColumnDataGenerator additionalColumnDataGenerator)
        {
            Debug.Print("LogEventDataGenerator Created: " + _identity);
            _columnOptions = columnOptions ?? throw new ArgumentNullException(nameof(columnOptions));
            _standardColumnDataGenerator = standardColumnDataGenerator ?? throw new ArgumentNullException(nameof(standardColumnDataGenerator));
            _additionalColumnDataGenerator = additionalColumnDataGenerator ?? throw new ArgumentNullException(nameof(additionalColumnDataGenerator));
        }

        public IEnumerable<KeyValuePair<string, object>> GetColumnsAndValues(LogEventWithExceptionAsJsonString logEvent)
        {
            Debug.Print("LogEventDataGenerator - GetColumnsAndValues " + _identity);
            // skip Id (auto-incrementing identity)
            foreach (var column in _columnOptions.Store.Where(c => c != StandardColumn.Id))
            {
                yield return _standardColumnDataGenerator.GetStandardColumnNameAndValue(column, logEvent);
            }

            if (_columnOptions.AdditionalColumns != null)
            {
                foreach (var additionalColumn in _columnOptions.AdditionalColumns)
                {
                    yield return _additionalColumnDataGenerator.GetAdditionalColumnNameAndValue(additionalColumn, logEvent.Properties);
                }
            }
        }
    }
}
