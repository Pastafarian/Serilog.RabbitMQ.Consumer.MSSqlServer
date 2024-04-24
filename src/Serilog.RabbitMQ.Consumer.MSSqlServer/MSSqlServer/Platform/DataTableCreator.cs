using System.Data;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    internal class DataTableCreator : IDataTableCreator
    {
        private readonly string _tableName;
        private readonly ColumnOptions.ColumnOptions _columnOptions;

        public DataTableCreator(string tableName, ColumnOptions.ColumnOptions columnOptions)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _columnOptions = columnOptions ?? throw new ArgumentNullException(nameof(columnOptions));
        }

        public DataTable CreateDataTable()
        {
            var eventsTable = new DataTable(_tableName);
            eventsTable.BeginLoadData();
            foreach (var standardColumn in _columnOptions.Store)
            {
                var standardOpts = _columnOptions.GetStandardColumnOptions(standardColumn);
                var dataColumn = standardOpts.AsDataColumn();
                eventsTable.Columns.Add(dataColumn);
                if (standardOpts == _columnOptions.PrimaryKey)
                    eventsTable.PrimaryKey = [dataColumn];
            }

            foreach (var addCol in _columnOptions.AdditionalColumns)
            {
                var dataColumn = addCol.AsDataColumn();
                eventsTable.Columns.Add(dataColumn);
                if (addCol == _columnOptions.PrimaryKey)
                    eventsTable.PrimaryKey = [dataColumn];
            }

            return eventsTable;
        }
    }
}
