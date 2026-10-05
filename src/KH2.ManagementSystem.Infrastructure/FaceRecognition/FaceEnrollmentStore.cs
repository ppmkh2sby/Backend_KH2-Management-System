using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class FaceEnrollmentStore(AppDbContext context) : IFaceEnrollmentStore
{
    public Task<Guid?> FindSantriIdAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Santris.Where(santri => santri.UserId == userId).Select(santri => (Guid?)santri.Id)
            .SingleOrDefaultAsync(cancellationToken);
    public Task<FaceProfile?> FindProfileAsync(Guid santriId, CancellationToken cancellationToken) =>
        context.FaceProfiles.SingleOrDefaultAsync(profile => profile.SantriId == santriId, cancellationToken);

    public async Task<bool> SaveAsync(FaceProfile profile, bool isNewProfile, FaceEnrollment enrollment, IReadOnlyList<FaceEmbedding> embeddings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (isNewProfile) context.FaceProfiles.Add(profile);
        var old = profile.CurrentEnrollmentId is Guid oldId
            ? await context.FaceEnrollments.SingleOrDefaultAsync(item => item.Id == oldId, cancellationToken) : null;
        context.FaceEnrollments.Add(enrollment);
        context.FaceEmbeddings.AddRange(embeddings);
        try
        {
            enrollment.Activate(now);
            profile.Activate(enrollment.Id, now);
            old?.Supersede(now);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_FaceProfiles_SantriId" })
        {
            await transaction.RollbackAsync(cancellationToken);
            if (isNewProfile) context.Entry(profile).State = EntityState.Detached;
            context.Entry(enrollment).State = EntityState.Detached;
            foreach (var embedding in embeddings) context.Entry(embedding).State = EntityState.Detached;
            return false;
        }
    }
}
