namespace KH2.ManagementSystem.Application.Abstractions.Authorization;

public interface ISantriAccessReader
{
    Task<bool> IsSantriOwnerAsync(
        Guid userId,
        Guid santriId,
        CancellationToken cancellationToken = default);

    Task<bool> IsWaliOfSantriAsync(
        Guid waliUserId,
        Guid santriId,
        CancellationToken cancellationToken = default);
}