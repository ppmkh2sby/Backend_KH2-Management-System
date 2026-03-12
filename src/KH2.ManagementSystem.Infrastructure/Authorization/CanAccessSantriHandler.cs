using System.Security.Claims;
using KH2.ManagementSystem.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.Authorization;

public sealed class CanAccessSantriHandler(
    IOptions<DevelopmentAuthorizationOptions> options)
    : AuthorizationHandler<CanAccessSantriRequirement, SantriAccessResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanAccessSantriRequirement requirement,
        SantriAccessResource resource)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = context.User.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Task.CompletedTask;
        }

        if (!Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role))
        {
            return Task.CompletedTask;
        }

        if (role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var authzOptions = options.Value;

        if (!authzOptions.Enabled)
        {
            return Task.CompletedTask;
        }

        if (role == UserRole.Santri)
        {
            var ownedSantri = authzOptions.SantriOwnerships
                .FirstOrDefault(x => x.UserId == userId);

            if (ownedSantri is not null && ownedSantri.SantriId == resource.SantriId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        if (role == UserRole.WaliSantri)
        {
            var waliRelation = authzOptions.WaliSantriRelations
                .FirstOrDefault(x => x.WaliUserId == userId);

            if (waliRelation is not null && waliRelation.SantriIds.Contains(resource.SantriId))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}