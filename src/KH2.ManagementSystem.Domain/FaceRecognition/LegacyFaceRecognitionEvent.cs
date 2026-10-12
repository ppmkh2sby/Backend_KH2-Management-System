using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

/// <summary>Audit record retained for the pre-existing user-operated face-attendance sessions.</summary>
public sealed class LegacyFaceRecognitionEvent : AuditableEntity<Guid>
{
    public LegacyFaceRecognitionEvent(Guid id, Guid sessionId, Guid? santriId, decimal? confidence, DateTimeOffset capturedAtUtc, FaceRecognitionEventStatus status, string? rejectionReason)
        : base(id)
    {
        SessionId = sessionId;
        SantriId = santriId;
        Confidence = confidence;
        CapturedAtUtc = capturedAtUtc;
        Status = status;
        RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? null : rejectionReason.Trim();
    }

    public Guid SessionId { get; private set; }
    public Guid? SantriId { get; private set; }
    public decimal? Confidence { get; private set; }
    public DateTimeOffset CapturedAtUtc { get; private set; }
    public FaceRecognitionEventStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? PresensiId { get; private set; }

    public void Accept(Guid presensiId, DateTimeOffset now)
    {
        Status = FaceRecognitionEventStatus.Accepted;
        PresensiId = presensiId;
        RejectionReason = null;
        Touch(now);
    }
}
