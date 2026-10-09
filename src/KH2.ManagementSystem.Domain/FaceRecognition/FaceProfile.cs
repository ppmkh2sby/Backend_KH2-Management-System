using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class FaceProfile : AuditableEntity<Guid>
{
    private FaceProfile() : base(Guid.Empty) { }

    public FaceProfile(
        Guid id,
        Guid santriId,
        DateTimeOffset now)
        : base(id)
    {
        SantriId = santriId != Guid.Empty
            ? santriId
            : throw new ArgumentException("Santri id is required.", nameof(santriId));
        Status = FaceProfileStatus.Pending;
        Touch(now);
    }

    public FaceProfile(Guid id, Guid santriId, string modelName, string modelVersion, DateTimeOffset now)
        : this(id, santriId, now) { }

    public Guid SantriId { get; private set; }
    public FaceProfileStatus Status { get; private set; }
    public DateTimeOffset? LastVerifiedAtUtc { get; private set; }

    public void ActivateProfile(DateTimeOffset now)
    {
        if (Status is FaceProfileStatus.Disabled)
        {
            throw new InvalidOperationException("A disabled face profile must be restored through an authorized workflow before enrollment.");
        }

        Status = FaceProfileStatus.Active;
        Touch(now);
    }

    public void UpdateModelVersion(string modelVersion, DateTimeOffset now) => RequireReEnrollment(now);

    public void Disable(DateTimeOffset now)
    {
        Status = FaceProfileStatus.Disabled;
        Touch(now);
    }

    public void RequireReEnrollment(DateTimeOffset now)
    {
        Status = FaceProfileStatus.NeedsReEnrollment;
        Touch(now);
    }

    public void MarkVerified(DateTimeOffset verifiedAtUtc)
    {
        if (Status is not FaceProfileStatus.Active)
        {
            throw new InvalidOperationException("Only an active face profile can be verified.");
        }

        LastVerifiedAtUtc = verifiedAtUtc;
        Touch(verifiedAtUtc);
    }
}
