using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class FaceProfile : AuditableEntity<Guid>
{
    private FaceProfile() : base(Guid.Empty) { }

    public FaceProfile(
        Guid id,
        Guid santriId,
        DateTimeOffset now,
        string? referenceImagePath = null)
        : base(id)
    {
        SantriId = santriId != Guid.Empty
            ? santriId
            : throw new ArgumentException("Santri id is required.", nameof(santriId));
        ReferenceImagePath = NormalizeOptional(referenceImagePath);
        Status = FaceProfileStatus.Pending;
        Touch(now);
    }

    public FaceProfile(Guid id, Guid santriId, string modelName, string modelVersion, DateTimeOffset now, string? referenceImagePath = null)
        : this(id, santriId, now, referenceImagePath) { }

    public Guid SantriId { get; private set; }
    public FaceProfileStatus Status { get; private set; }
    public Guid? CurrentEnrollmentId { get; private set; }
    public string? ReferenceImagePath { get; private set; }
    public DateTimeOffset? LastVerifiedAtUtc { get; private set; }

    public void Activate(Guid enrollmentId, DateTimeOffset now)
    {
        if (enrollmentId == Guid.Empty) throw new ArgumentException("Enrollment id is required.", nameof(enrollmentId));
        CurrentEnrollmentId = enrollmentId;
        Status = FaceProfileStatus.Active;
        Touch(now);
    }

    public void Activate(DateTimeOffset now) => Activate(Guid.NewGuid(), now);

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
        if (Status is not FaceProfileStatus.Active || CurrentEnrollmentId is null)
        {
            throw new InvalidOperationException("Only an active face profile can be verified.");
        }

        LastVerifiedAtUtc = verifiedAtUtc;
        Touch(verifiedAtUtc);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
