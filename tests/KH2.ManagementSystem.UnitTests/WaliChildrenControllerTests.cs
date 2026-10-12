using System.Security.Claims;
using KH2.ManagementSystem.Api.Controllers;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Domain.Walis;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class WaliChildrenControllerTests(PostgreSqlIntegrationFixture fixture)
{
    [Fact]
    public async Task GetChildrenReturnsOnlyRelationsValidForAuthenticatedWaliUsername()
    {
        await using var db = await fixture.CreateContextAsync();
        var waliId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var rejectedChildId = Guid.NewGuid();
        var childUserId = Guid.NewGuid();
        var rejectedChildUserId = Guid.NewGuid();
        db.Add(new User(waliId, "wali-valid", "Wali", null, UserRole.WaliSantri, "hash"));
        db.Add(new User(childUserId, "santri-valid", "Anak Valid", null, UserRole.Santri, "hash"));
        db.Add(new User(rejectedChildUserId, "santri-invalid", "Anak Invalid", null, UserRole.Santri, "hash"));
        db.Add(new Santri(childId, childUserId, "Anak Valid", "NIS-VALID", "K", "J", "L", "KTB", "A"));
        db.Add(new Santri(rejectedChildId, rejectedChildUserId, "Anak Invalid", "NIS-INVALID", "K", "J", "L", "KTB", "A"));
        db.Add(new WaliSantriRelation(Guid.NewGuid(), waliId, childId, "Orang Tua", "wali-valid"));
        db.Add(new WaliSantriRelation(Guid.NewGuid(), waliId, rejectedChildId, "Orang Tua", "stale-code"));
        await db.SaveChangesAsync();

        var controller = new WaliController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, waliId.ToString())], "test"))
                }
            }
        };

        var action = await controller.GetChildren(CancellationToken.None);
        var result = Assert.IsType<OkObjectResult>(action.Result);
        var payload = Assert.IsType<WaliChildrenResponse>(result.Value);
        var child = Assert.Single(payload.Items);
        Assert.Equal(childId, child.SantriId);
        Assert.Equal("NIS-VALID", child.Nis);
    }
}
