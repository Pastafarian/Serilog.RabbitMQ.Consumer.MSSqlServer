extern alias ConsumerAlias;
extern alias ProducerAlias;
using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests.Setup;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.IntegrationTests;

public sealed class EndToEndTests(ProducerAndConsumerFixture fixture) : IClassFixture<ProducerAndConsumerFixture>
{
    [Fact]
    public async Task GivenAuditLogsGenerated_WhenMessagePublished_ThenConsumedAndLogsStoredToTheDatabase()
    {
        // Arrange
        var connectionString = fixture.MsSqlContainer.GetConnectionString();
        var auditMessage1 = $"unique-message-{Guid.NewGuid()}";
        var auditMessage2 = $"unique-message-{Guid.NewGuid()}";

        // Act
        if (fixture.ProducerHttpClient != null)
        {
            await fixture.ProducerHttpClient.GetAsync("AuditException?message=" + auditMessage1);
            await fixture.ProducerHttpClient.GetAsync("AuditException?message=" + auditMessage2);
        }
        await Task.Delay(3000);

        // Assert
        var connection = new SqlConnection(connectionString);
        var rows = connection.Query<LogRow>("SELECT * FROM [master].[dbo].[ApiLogs] WHERE Exception like @n OR Exception like @y", new { n = "%" + auditMessage1 + "%", y = "%" + auditMessage2 + "%" });

        rows.Should().HaveCount(2);
    }


    [Fact]
    public async Task GivenLogsGenerated_WhenMessagePublished_ThenConsumedAndLogsStoredToTheDatabase()
    {
        // Arrange
        var connectionString = fixture.MsSqlContainer.GetConnectionString();
        var logMessage1 = $"unique-message-{Guid.NewGuid()}";
        var logMessage2 = $"unique-message-{Guid.NewGuid()}";

        // Act
        if (fixture.ProducerHttpClient != null)
        {
            await fixture.ProducerHttpClient.GetAsync("LogException?message=" + logMessage1);
            await fixture.ProducerHttpClient.GetAsync("LogException?message=" + logMessage2);
        }
        await Task.Delay(3000);

        // Assert
        var connection = new SqlConnection(connectionString);
        var rows = connection.Query<LogRow>("SELECT * FROM [master].[dbo].[ApiLogs] WHERE Exception like @n OR Exception like @y", new { n = "%" + logMessage1 + "%", y = "%" + logMessage2 + "%" });

        rows.Should().HaveCount(2);
    }


    public class LogRow
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTimeOffset DequeueTimestamp { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; }
        public string Thread { get; set; }
        public string Server { get; set; }
        public string Logger { get; set; }
        public string RequestUrl { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid ExternalCorrelationId { get; set; }
        public DateTimeOffset PublishTimestamp { get; set; }
    }
}