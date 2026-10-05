using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

// Compatibility model for the existing private face-service integration.
public sealed class ProviderFaceProfile : AuditableEntity<Guid>
{
    public ProviderFaceProfile(Guid id, Guid userId, string providerProfileId, DateTimeOffset embeddingUpdatedAtUtc)
        : base(id)
    {
        UserId = userId;
        ProviderProfileId = Require(providerProfileId);
        EmbeddingUpdatedAtUtc = embeddingUpdatedAtUtc;
    }

    public Guid UserId { get; private set; }
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
