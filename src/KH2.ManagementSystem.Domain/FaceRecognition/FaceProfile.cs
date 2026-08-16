using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

// Only the opaque identifier held by the private AI service is persisted. Embeddings are never stored here.
public sealed class FaceProfile : AuditableEntity<Guid>
{
    public FaceProfile(Guid id, Guid santriId, string providerProfileId, DateTimeOffset embeddingUpdatedAtUtc)
        : base(id)
    {
        SantriId = santriId;
        ProviderProfileId = Require(providerProfileId);
        EmbeddingUpdatedAtUtc = embeddingUpdatedAtUtc;
    }

    public Guid SantriId { get; private set; }
    public string ProviderProfileId { get; private set; } = string.Empty;
    public DateTimeOffset EmbeddingUpdatedAtUtc { get; private set; }

    public void UpdateProviderProfile(string providerProfileId, DateTimeOffset now)
    {
        ProviderProfileId = Require(providerProfileId);
        EmbeddingUpdatedAtUtc = now;
        Touch(now);
    }

    private static string Require(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Provider profile id is required.", nameof(value)) : value.Trim();
}
