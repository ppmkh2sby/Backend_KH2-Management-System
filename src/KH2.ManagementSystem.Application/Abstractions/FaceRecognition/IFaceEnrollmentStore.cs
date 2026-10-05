using KH2.ManagementSystem.Domain.FaceRecognition;

namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

public interface IFaceEnrollmentStore
{
    Task<Guid?> FindSantriIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<FaceProfile?> FindProfileAsync(Guid santriId, CancellationToken cancellationToken);
    // Atomically persists samples, activates the replacement, and supersedes the old enrollment.
    Task<bool> SaveAsync(FaceProfile profile, bool isNewProfile, FaceEnrollment enrollment, IReadOnlyList<FaceEmbedding> embeddings, DateTimeOffset now, CancellationToken cancellationToken);
}
