using System.Data;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform
{
    public interface IDataTableCreator
    {
        DataTable CreateDataTable();
    }
}
