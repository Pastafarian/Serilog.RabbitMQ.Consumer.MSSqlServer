using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Platform;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Dependencies
{
    public class SinkDependencies
    {
        public virtual IDataTableCreator? DataTableCreator { get; set; }
        public virtual ISqlCommandExecutor? SqlDatabaseCreator { get; set; }
        public virtual ISqlCommandExecutor? SqlTableCreator { get; set; }
        public virtual ISqlBulkBatchWriter? SqlBulkBatchWriter { get; set; }
        public virtual ISqlLogEventWriter? SqlLogEventWriter { get; set; }
    }
}
