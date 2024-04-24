using System.Data;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform.SqlClient
{
    internal interface ISqlCommandWrapper : IDisposable
    {
        CommandType CommandType { get; set; }
        string CommandText { get; set; }

        void AddParameter(string parameterName, object value);
        Task<int> ExecuteNonQueryAsync();
        int ExecuteNonQuery();
    }
}
