using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

/// <summary>A complete, versioned biometric enrollment for one face profile.</summary>
public sealed class FaceEnrollment : AuditableEntity<Guid>
{
    public FaceEnrollment(Guid id, Guid faceProfileId) : this(id, faceProfileId, "legacy", "legacy", DateTimeOffset.UtcNow) { }
    public FaceEnrollment(Guid id, Guid faceProfileId, string modelName, string modelVersion,
        DateTimeOffset enrolledAtUtc, string? referenceImagePath = null) : base(id)
    {
        FaceProfileId = faceProfileId != Guid.Empty ? faceProfileId : throw new ArgumentException("Face profile id is required.", nameof(faceProfileId));
        ModelName = Require(modelName, nameof(modelName));
        ModelVersion = Require(modelVersion, nameof(modelVersion));
        ReferenceImagePath = NormalizeOptional(referenceImagePath);
        EnrolledAtUtc = enrolledAtUtc;
        Status = FaceEnrollmentStatus.Pending;
    }

    public Guid FaceProfileId { get; private set; }
    public FaceEnrollmentStatus Status { get; private set; }
    public string ModelName { get; private set; } = string.Empty;
    public string ModelVersion { get; private set; } = string.Empty;
    public string? ReferenceImagePath { get; private set; }
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public DateTimeOffset? SupersededAtUtc { get; private set; }

    public void Activate(DateTimeOffset now)
    {
        if (Status is not FaceEnrollmentStatus.Pending) throw new InvalidOperationException("Only a pending enrollment can be activated.");
        Status = FaceEnrollmentStatus.Active; ActivatedAtUtc = now; Touch(now);
    }

    public void Supersede(DateTimeOffset now)
    {
        if (Status is not FaceEnrollmentStatus.Active) throw new InvalidOperationException("Only an active enrollment can be superseded.");
        Status = FaceEnrollmentStatus.Superseded; SupersededAtUtc = now; Touch(now);
    }

    public void Fail(DateTimeOffset now)
    {
        if (Status is not FaceEnrollmentStatus.Pending) throw new InvalidOperationException("Only a pending enrollment can fail.");
        Status = FaceEnrollmentStatus.Failed; Touch(now);
    }

    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
