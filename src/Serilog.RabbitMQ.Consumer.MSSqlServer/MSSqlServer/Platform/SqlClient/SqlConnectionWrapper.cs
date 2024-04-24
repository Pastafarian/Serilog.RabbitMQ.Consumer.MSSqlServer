using Microsoft.Data.SqlClient;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient
{
    internal class SqlConnectionWrapper : ISqlConnectionWrapper
    {
        private readonly SqlConnection _sqlConnection;
        private bool _disposedValue;

        public SqlConnectionWrapper(string connectionString)
        {
            _sqlConnection = new SqlConnection(connectionString);
        }

        public string ConnectionString => _sqlConnection.ConnectionString;

        public void Open()
        {
            try
            {
                _sqlConnection.Open();
            }
            catch (Exception ex)
            {
                throw new Exception($"Connection string {_sqlConnection.ConnectionString}", ex);
            }

        }

        public async Task OpenAsync()
        {
            await _sqlConnection.OpenAsync().ConfigureAwait(false);
        }

        public void Close()
        {
            _sqlConnection.Close();
        }

        public async Task CloseAsync()
        {
            await _sqlConnection.CloseAsync().ConfigureAwait(false);
        }

        public ISqlCommandWrapper CreateCommand()
        {
            var sqlCommand = _sqlConnection.CreateCommand();
            return new SqlCommandWrapper(sqlCommand);
        }

        public SqlTransaction BeginTransaction()
        {
            return _sqlConnection.BeginTransaction();
        }

        public ISqlCommandWrapper CreateCommand(string cmdText)
        {
            var sqlCommand = new SqlCommand(cmdText, _sqlConnection);

            return new SqlCommandWrapper(sqlCommand);
        }

        public ISqlBulkCopyWrapper CreateSqlBulkCopy(bool disableTriggers, string destinationTableName)
        {
            var sqlBulkCopy = disableTriggers
                ? new SqlBulkCopy(_sqlConnection)
                : new SqlBulkCopy(_sqlConnection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, null);
            sqlBulkCopy.DestinationTableName = destinationTableName;

            return new SqlBulkCopyWrapper(sqlBulkCopy);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _sqlConnection.Dispose();

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
