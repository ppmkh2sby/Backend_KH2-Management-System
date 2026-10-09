using KH2.ManagementSystem.Domain.Santris;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class SantriTeamTests
{
    [Theory]
    [InlineData("KTB")]
    [InlineData("ktb")]
    [InlineData("Ketertiban")]
    [InlineData("ke-tertiban")]
    [InlineData("Tim Ketertiban")]
    [InlineData("Tim KTB")]
    public void KetertibanAliasesResolveToTheSameTeam(string team)
    {
        Assert.True(SantriTeam.IsKetertiban(team));
    }

    [Theory]
    [InlineData("KBM")]
    [InlineData("Kebersihan")]
    [InlineData("Sekben")]
    [InlineData(null)]
    public void OtherTeamsCannotAccessKetertibanFeatures(string? team)
    {
        Assert.False(SantriTeam.IsKetertiban(team));
    }
}
