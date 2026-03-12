using KH2.ManagementSystem.Application.Abstractions.Authorization;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.Authorization;

public sealed class DevelopmentSantriAccessReader(
    IOptions<DevelopmentAuthorizationOptions> options)
    : ISantriAccessReader
{
    public Task<bool> IsSantriOwnerAsync(
        Guid userId,
        Guid santriId,
        CancellationToken cancellationToken = default)
    {
        var authzOptions = options.Value;

        if (!authzOptions.Enabled)
        {
            return Task.FromResult(false);
        }

        var ownedSantri = authzOptions.SantriOwnerships
            .FirstOrDefault(x => x.UserId == userId);

        return Task.FromResult(
            ownedSantri is not null &&
            ownedSantri.SantriId == santriId);
    }

    public Task<bool> IsWaliOfSantriAsync(
        Guid waliUserId,
        Guid santriId,
        CancellationToken cancellationToken = default)
    {
        var authzOptions = options.Value;

        if (!authzOptions.Enabled)
        {
            return Task.FromResult(false);
        }

        var waliRelation = authzOptions.WaliSantriRelations
            .FirstOrDefault(x => x.WaliUserId == waliUserId);

        return Task.FromResult(
            waliRelation is not null &&
            waliRelation.SantriIds.Contains(santriId));
    }
}