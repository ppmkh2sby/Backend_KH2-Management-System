using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class FaceEnrollment : AuditableEntity<Guid>
{
    public FaceEnrollment(Guid id, Guid santriId)
        : base(id)
    {
        SantriId = santriId;
        Status = FaceEnrollmentStatus.InProgress;
    }

    public Guid SantriId { get; private set; }
    public FaceEnrollmentStatus Status { get; private set; }
    public int CaptureCount { get; private set; }
    public DateTimeOffset? RegisteredAtUtc { get; private set; }
    public DateTimeOffset? EmbeddingUpdatedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    public void SetCaptureCount(int captureCount, DateTimeOffset now)
    {
        CaptureCount = Math.Clamp(captureCount, 0, 5);
        Status = FaceEnrollmentStatus.InProgress;
        RejectionReason = null;
        Touch(now);
    }

    public void Register(DateTimeOffset now)
    {
        CaptureCount = 5;
        Status = FaceEnrollmentStatus.Registered;
        RegisteredAtUtc = now;
        EmbeddingUpdatedAtUtc = now;
        RejectionReason = null;
        Touch(now);
    }

    public void Reject(string reason, DateTimeOffset now)
    {
        Status = FaceEnrollmentStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Profil wajah ditolak." : reason.Trim();
        Touch(now);
    }
}
