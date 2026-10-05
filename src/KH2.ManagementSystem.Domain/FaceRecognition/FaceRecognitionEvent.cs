using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class FaceRecognitionEvent : AuditableEntity<Guid>
{
    private FaceRecognitionEvent() : base(Guid.Empty) { }

    public FaceRecognitionEvent(
        Guid id,
        Guid? deviceId,
        Guid? faceProfileId,
        Guid? faceEnrollmentId,
        Guid? santriId,
        Guid? sesiId,
        Guid? presensiId,
        bool recognized,
        double? similarity,
        double? distance,
        string? failureReason,
        int processingDurationMs,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        DeviceId = deviceId;
        FaceProfileId = faceProfileId;
        FaceEnrollmentId = faceEnrollmentId;
        SantriId = santriId;
        SesiId = sesiId;
        PresensiId = presensiId;
        Recognized = recognized;
        Similarity = similarity;
        Distance = distance;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        ProcessingDurationMs = processingDurationMs >= 0 ? processingDurationMs : throw new ArgumentOutOfRangeException(nameof(processingDurationMs));
        CreatedAtUtc = createdAtUtc;
    }

    public Guid? DeviceId { get; private set; }
    public Guid? FaceProfileId { get; private set; }
    public Guid? FaceEnrollmentId { get; private set; }
    public Guid? SantriId { get; private set; }
    public Guid? SesiId { get; private set; }
    public Guid? PresensiId { get; private set; }
    public bool Recognized { get; private set; }
    public double? Similarity { get; private set; }
    public double? Distance { get; private set; }
    public string? FailureReason { get; private set; }
    public int ProcessingDurationMs { get; private set; }
}
