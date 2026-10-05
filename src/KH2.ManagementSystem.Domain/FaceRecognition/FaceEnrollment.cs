using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

/// <summary>A complete, versioned biometric enrollment for one face profile.</summary>
public sealed class FaceEnrollment : AuditableEntity<Guid>
{
    public FaceEnrollment(Guid id, Guid faceProfileId) : this(id, faceProfileId, "legacy", "legacy", 0, DateTimeOffset.UtcNow) { }
    public FaceEnrollment(Guid id, Guid faceProfileId, string modelName, string modelVersion, int acceptedSampleCount,
        DateTimeOffset enrolledAtUtc) : base(id)
    {
        FaceProfileId = faceProfileId != Guid.Empty ? faceProfileId : throw new ArgumentException("Face profile id is required.", nameof(faceProfileId));
        ModelName = Require(modelName, nameof(modelName));
        ModelVersion = Require(modelVersion, nameof(modelVersion));
        AcceptedSampleCount = acceptedSampleCount is >= 0 and <= 5 ? acceptedSampleCount : throw new ArgumentOutOfRangeException(nameof(acceptedSampleCount));
        EnrolledAtUtc = enrolledAtUtc;
        Status = FaceEnrollmentStatus.Pending;
    }

    public Guid FaceProfileId { get; private set; }
    public FaceEnrollmentStatus Status { get; private set; }
    public string ModelName { get; private set; } = string.Empty;
    public string ModelVersion { get; private set; } = string.Empty;
    public int AcceptedSampleCount { get; private set; }
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public DateTimeOffset? SupersededAtUtc { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public int CaptureCount => AcceptedSampleCount;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public DateTimeOffset? EmbeddingUpdatedAtUtc => ActivatedAtUtc;
    public void SetCaptureCount(int count, DateTimeOffset now) { AcceptedSampleCount = Math.Clamp(count, 0, 5); Touch(now); }
    public void Register(DateTimeOffset now) => Activate(now);

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
}
