using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace IntegrationTest.Test;

[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;