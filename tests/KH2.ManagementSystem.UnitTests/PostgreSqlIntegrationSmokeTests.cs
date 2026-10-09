using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class PostgreSqlIntegrationSmokeTests(PostgreSqlIntegrationFixture fixture)
{
    [Fact]
    public async Task ContainerSupportsPostgreSqlAndPgvectorFromTheCurrentEfModel()
    {
        await using var context = await fixture.CreateContextAsync();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var version = new NpgsqlCommand("SELECT version();", connection);
        Assert.Contains("PostgreSQL 16", (string)(await version.ExecuteScalarAsync())!);
        await using var extension = new NpgsqlCommand("SELECT extversion FROM pg_extension WHERE extname = 'vector';", connection);
        Assert.False(string.IsNullOrWhiteSpace((string?)await extension.ExecuteScalarAsync()));
        Assert.True(await context.Database.CanConnectAsync());
    }
}
