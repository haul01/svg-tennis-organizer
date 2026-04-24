using Testcontainers.MsSql;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Starts a shared SQL Server container for the duration of the test run.
/// Individual test classes create their own database inside this container
/// so tests don't interfere with each other.
/// </summary>
public sealed class MsSqlFixture : IAsyncLifetime
{
    public MsSqlContainer Container { get; } =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition("Sql")]
public class SqlTestCollection : ICollectionFixture<MsSqlFixture>;
