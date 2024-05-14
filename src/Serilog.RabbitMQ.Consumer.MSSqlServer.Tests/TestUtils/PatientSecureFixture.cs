namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils
{
    [CollectionDefinition("DatabaseTests", DisableParallelization = true)]
    public class PatientSecureFixture : ICollectionFixture<DatabaseFixture> { }
}
