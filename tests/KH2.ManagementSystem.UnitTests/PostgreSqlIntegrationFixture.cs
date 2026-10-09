using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlIntegrationFixtureDefinition : ICollectionFixture<PostgreSqlIntegrationFixture>
{
    public const string Name = "PostgreSQL integration";
}

public sealed class PostgreSqlIntegrationFixture : IAsyncLifetime
{
    private const string Image = "pgvector/pgvector:0.8.6-pg16";
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder(Image)
        .WithDatabase("kh2_c1")
        .WithUsername("postgres")
        .WithPassword("kh2_c1_test_password")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector;", connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public async Task<AppDbContext> CreateContextAsync(bool resetDatabase = true, params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString, npgsqlOptions => npgsqlOptions.UseVector())
            .AddInterceptors(interceptors)
            .Options;
        var context = new AppDbContext(options);
        if (resetDatabase)
        {
            await context.Database.EnsureDeletedAsync();
            NpgsqlConnection.ClearAllPools();
            await context.Database.EnsureCreatedAsync();
            NpgsqlConnection.ClearAllPools();
        }
        return context;
    }
}
