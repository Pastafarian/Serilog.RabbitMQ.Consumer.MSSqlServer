using Dapper;
using JasperFx.Core;
using Marten;
using Npgsql;
using Testcontainers.PostgreSql;
using Weasel.Core;
using Xunit.Abstractions;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils
{
    public class Person(string name = "")
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Id { get; set; }
        // ReSharper disable once UnusedMember.Global
        public string Name { get; set; } = name;
    };
    public class FileName
    {
        public string Name { get; set; }
    }

    [CollectionDefinition(nameof(PostgresCollection))]
    public class PostgresCollection : ICollectionFixture<PostgresFixture>;
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer container =
            new PostgreSqlBuilder()
                .Build();

        public string ConnectionString => container.GetConnectionString();
        public string ContainerId => $"{container.Id}";

        public async Task InitializeAsync()
        {
            await container.StartAsync();
        }


        public async Task DisposeAsync()
        {
            await container.DisposeAsync().AsTask();
        }


        public static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }
    public static class DatabaseContainerPerCollection
    {
        [Collection(nameof(PostgresCollection))]
        public class First(PostgresFixture fixture, ITestOutputHelper output) : IDisposable
        {

            [Fact]
            public async Task Database_Can_Run_Query()
            {

                await using NpgsqlConnection connection = new(fixture.ConnectionString);
                await connection.OpenAsync();

                const int expected = 1;
                var actual = await connection.QueryFirstAsync<int>("SELECT 1");

                Assert.Equal(expected, actual);
            }

            [Fact]
            public async Task Database_Can_Select_DateTime()
            {
                await using NpgsqlConnection connection = new(fixture.ConnectionString);
                await connection.OpenAsync();

                var actual = await connection.QueryFirstAsync<DateTime>("SELECT NOW()");
                Assert.IsType<DateTime>(actual);
            }

            [Fact]
            public async Task Can_Store_Document_With_Marten()
            {
                await using NpgsqlConnection connection = new(fixture.ConnectionString);
                var store = DocumentStore.For(options =>
                {
                    options.Connection(fixture.ConnectionString);
                    options.AutoCreateSchemaObjects = AutoCreate.All;
                });

                int id;
                {
                    await using var session = store.IdentitySession();
                    var person = new Person("Khalid");
                    session.Store(person);
                    await session.SaveChangesAsync();

                    id = person.Id;
                }

                {
                    await using var session = store.QuerySession();
                    var person = session.Query<Person>().FindFirst(p => p.Id == id);
                    Assert.NotNull(person);
                }
            }

            public void Dispose() => output.WriteLine(fixture.ContainerId);
        }

        [Collection(nameof(PostgresCollection))]
        public class Second(PostgresFixture fixture, ITestOutputHelper output) : IDisposable
        {
            [Fact]
            public async Task Database_Can_Run_Query()
            {
                await using NpgsqlConnection connection = new(fixture.ConnectionString);
                await connection.OpenAsync();

                output.WriteLine("Hi! 👋");

                const int expected = 1;
                var actual = await connection.QueryFirstAsync<int>("SELECT 1");

                Assert.Equal(expected, actual);
            }

            [Fact]
            public async Task Database_Can_Select_DateTime()
            {
                await using NpgsqlConnection connection = new(fixture.ConnectionString);
                await connection.OpenAsync();

                var actual = await connection.QueryFirstAsync<DateTime>("SELECT NOW()");
                Assert.IsType<DateTime>(actual);
            }

            [Fact]
            public async Task Can_Store_Document_With_Marten()
            {
                await using NpgsqlConnection connection = new(fixture.ConnectionString);
                var store = DocumentStore.For(options =>
                {
                    options.Connection(fixture.ConnectionString);
                    options.AutoCreateSchemaObjects = AutoCreate.All;
                });

                int id;
                {
                    await using var session = store.IdentitySession();
                    var person = new Person("Khalid");
                    session.Store(person);
                    await session.SaveChangesAsync();

                    id = person.Id;
                }

                {
                    await using var session = store.QuerySession();
                    var person = session.Query<Person>().FindFirst(p => p.Id == id);
                    Assert.NotNull(person);
                }
            }

            public void Dispose() => output.WriteLine(fixture.ContainerId);
        }
    }
}
