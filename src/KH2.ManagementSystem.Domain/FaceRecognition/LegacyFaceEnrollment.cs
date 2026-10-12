using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

/// <summary>Provider-ID enrollment retained solely for the pre-existing API flow.</summary>
public sealed class LegacyFaceEnrollment : AuditableEntity<Guid>
{
    public LegacyFaceEnrollment(Guid id, Guid userId) : base(id) { UserId = userId; Status = LegacyFaceEnrollmentStatus.InProgress; }
    public Guid UserId { get; private set; }
    public LegacyFaceEnrollmentStatus Status { get; private set; }
    public int CaptureCount { get; private set; }
    public DateTimeOffset? RegisteredAtUtc { get; private set; }
    public DateTimeOffset? EmbeddingUpdatedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public void SetCaptureCount(int count, DateTimeOffset now) { CaptureCount = Math.Clamp(count, 0, 5); Status = LegacyFaceEnrollmentStatus.InProgress; RejectionReason = null; Touch(now); }
    public void Register(DateTimeOffset now) { CaptureCount = 5; Status = LegacyFaceEnrollmentStatus.Registered; RegisteredAtUtc = now; EmbeddingUpdatedAtUtc = now; RejectionReason = null; Touch(now); }
    public void Reject(string reason, DateTimeOffset now) { Status = LegacyFaceEnrollmentStatus.Rejected; RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Profil wajah ditolak." : reason.Trim(); Touch(now); }
}

public enum LegacyFaceEnrollmentStatus { InProgress = 1, Registered = 2, Rejected = 3 }
