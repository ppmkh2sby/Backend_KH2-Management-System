using System.Security.Claims;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.Authorization;

public sealed class CanOperateFaceAttendanceHandler(AppDbContext dbContext)
    : AuthorizationHandler<CanOperateFaceAttendanceRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanOperateFaceAttendanceRequirement requirement)
    {
        if (context.User.IsInRole(UserRole.Admin.ToString()) ||
            context.User.IsInRole(UserRole.DewanGuru.ToString()) ||
            context.User.IsInRole(UserRole.Pengurus.ToString()))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole(UserRole.Santri.ToString()) ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        var team = await dbContext.Santris.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Tim)
            .FirstOrDefaultAsync();

        if (string.Equals(team, "KTB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(team, "Ketertiban", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
    }
}
