using System.Security.Claims;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Domain.Users;
using Microsoft.AspNetCore.Authorization;

namespace KH2.ManagementSystem.Infrastructure.Authorization;

public sealed class CanAccessSantriHandler(
    ISantriAccessReader santriAccessReader)
    : AuthorizationHandler<CanAccessSantriRequirement, SantriAccessResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanAccessSantriRequirement requirement,
        SantriAccessResource resource)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = context.User.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        if (!Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role))
        {
            return;
        }

        if (role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus)
        {
            context.Succeed(requirement);
            return;
        }

        if (role == UserRole.Santri)
        {
            var isOwner = await santriAccessReader.IsSantriOwnerAsync(userId, resource.SantriId);

            if (isOwner)
            {
                context.Succeed(requirement);
            }

            return;
        }

        if (role == UserRole.WaliSantri)
        {
            var isRelatedWali = await santriAccessReader.IsWaliOfSantriAsync(userId, resource.SantriId);

            if (isRelatedWali)
            {
                context.Succeed(requirement);
            }
        }
    }
}