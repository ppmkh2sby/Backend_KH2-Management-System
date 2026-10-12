using System.Security.Claims;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

public sealed record WaliChildResponse(Guid SantriId, string Nis, string FullName, string Tim);
public sealed record WaliChildrenResponse(IReadOnlyList<WaliChildResponse> Items);

[ApiController]
[Authorize(Roles = nameof(UserRole.WaliSantri))]
[Route("api/v1/wali/me")]
public sealed class WaliController(AppDbContext dbContext) : ControllerBase
{
    /// <summary>Returns only children whose persisted relation is valid for the authenticated Wali account.</summary>
    [HttpGet("children")]
    [ProducesResponseType(typeof(WaliChildrenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WaliChildrenResponse>> GetChildren(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var waliUserId))
        {
            return Unauthorized();
        }

        var items = await (
                from relation in dbContext.WaliSantriRelations.AsNoTracking()
                join wali in dbContext.Users.AsNoTracking() on relation.WaliUserId equals wali.Id
                join santri in dbContext.Santris.AsNoTracking() on relation.SantriId equals santri.Id
                where relation.WaliUserId == waliUserId &&
                      relation.WaliSantriCode == wali.Username
                orderby santri.FullName, santri.Nis
                select new WaliChildResponse(santri.Id, santri.Nis, santri.FullName, santri.Tim))
            .ToArrayAsync(cancellationToken);

        return Ok(new WaliChildrenResponse(items));
    }
}
