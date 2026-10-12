using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using KH2.ManagementSystem.Infrastructure.Persistence.Seed;
using KH2.ManagementSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class MasterAccountSeederTests(PostgreSqlIntegrationFixture fixture)
{
    [Fact]
    public async Task SeedAsyncPreservesNameUniquenessAndAppliesPutraTeamAssignments()
    {
        await using var dbContext = await fixture.CreateContextAsync();

        dbContext.Users.Add(new User(
            Guid.NewGuid(),
            "existing-azka",
            "AZKA NAFILATA RISKI",
            null,
            UserRole.Santri,
            "existing-password-hash"));
        await dbContext.SaveChangesAsync();

        var seeder = new MasterAccountSeeder(dbContext, new AspNetPasswordHasher());
        await seeder.SeedAsync(includeSampleData: false);

        Assert.Equal(1, await dbContext.Users.CountAsync(x => x.FullName == "AZKA NAFILATA RISKI"));
        Assert.False(await dbContext.Users.AnyAsync(x => x.Username == "022626005"));
        Assert.False(await dbContext.Santris.AnyAsync(x => x.Nis == "022626005"));
        dbContext.ChangeTracker.Clear();

        var alwida = await dbContext.Santris.SingleAsync(x => x.Nis == "022424001");
        Assert.Equal("PH", alwida.Tim);

        var syahdinda = await dbContext.Santris.SingleAsync(x => x.Nis == "022424016");
        Assert.Equal("UKPPT", syahdinda.Tim);

        var aiko = await dbContext.Santris.SingleAsync(x => x.Nis == "022626001");
        Assert.Equal("Umum", aiko.Tim);
    }
}
