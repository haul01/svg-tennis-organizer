using Testcontainers.PostgreSql;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Starts a shared Postgres container for the duration of the test run.
/// Individual test classes create their own database inside this container
/// so tests don't interfere with each other.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition("Sql")]
public class SqlTestCollection : ICollectionFixture<PostgresFixture>;
