using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.Authorization;

public sealed class AppDbSantriAccessReader(
    AppDbContext dbContext,
    IOptions<DevelopmentAuthorizationOptions> options)
    : ISantriAccessReader
{
    public async Task<bool> IsSantriOwnerAsync(
        Guid userId,
        Guid santriId,
        CancellationToken cancellationToken = default)
    {
        var isOwner = await dbContext.Santris
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.Id == santriId, cancellationToken);

        if (isOwner)
        {
            return true;
        }

        var authzOptions = options.Value;

        if (!authzOptions.Enabled)
        {
            return false;
        }

        return authzOptions.SantriOwnerships
            .Any(x => x.UserId == userId && x.SantriId == santriId);
    }

    public async Task<bool> IsWaliOfSantriAsync(
        Guid waliUserId,
        Guid santriId,
        CancellationToken cancellationToken = default)
    {
        var isWali = await dbContext.WaliSantriRelations
            .AsNoTracking()
            .AnyAsync(x => x.WaliUserId == waliUserId && x.SantriId == santriId, cancellationToken);

        if (isWali)
        {
            return true;
        }

        var authzOptions = options.Value;

        if (!authzOptions.Enabled)
        {
            return false;
        }

        return authzOptions.WaliSantriRelations
            .Any(x => x.WaliUserId == waliUserId && x.SantriIds.Contains(santriId));
    }
}
