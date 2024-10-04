using Microsoft.Data.SqlClient;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient
{
    public class SqlConnectionStringBuilderWrapper : ISqlConnectionStringBuilderWrapper
    {
        private readonly SqlConnectionStringBuilder _sqlConnectionStringBuilder;

        public SqlConnectionStringBuilderWrapper(string connectionString, bool enlist)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                Enlist = enlist
            };
            //_sqlConnectionStringBuilder
        }

        public string ConnectionString => _sqlConnectionStringBuilder.ConnectionString;

        public string InitialCatalog
        {
            get => _sqlConnectionStringBuilder.InitialCatalog;
            set => _sqlConnectionStringBuilder.InitialCatalog = value;
        }
    }
}
