namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

// Internal projection only. Embeddings never leave the persistence boundary.
public sealed record FaceMatchCandidate(Guid SantriId, double Similarity);

public interface IFaceMatchReader
{
    // Returns at most two distinct Santris, highest similarity first.
    // Only active profiles/embeddings/accounts with the supplied model identity qualify.
    Task<IReadOnlyList<FaceMatchCandidate>> FindNearestAsync(FaceEmbeddingResult probe, CancellationToken cancellationToken);
}
