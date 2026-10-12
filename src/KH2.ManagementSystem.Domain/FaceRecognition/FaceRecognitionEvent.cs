using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class FaceRecognitionEvent : AuditableEntity<Guid>
{
    private const int MaximumFailureReasonLength = 100;

    private FaceRecognitionEvent() : base(Guid.Empty) { }

    public FaceRecognitionEvent(
        Guid id,
        Guid? deviceId,
        Guid? faceProfileId,
        Guid? faceEnrollmentId,
        Guid? santriId,
        Guid? sesiId,
        Guid? presensiId,
        FaceRecognitionEventSource source,
        RecognitionOutcome recognitionOutcome,
        AttendanceOutcome attendanceOutcome,
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
        Source = RequireDefined(source, nameof(source));
        RecognitionOutcome = RequireDefined(recognitionOutcome, nameof(recognitionOutcome));
        AttendanceOutcome = RequireDefined(attendanceOutcome, nameof(attendanceOutcome));
        Similarity = RequireFinite(similarity, nameof(similarity));
        Distance = RequireFinite(distance, nameof(distance));
        FailureReason = NormalizeFailureReason(failureReason);
        ProcessingDurationMs = processingDurationMs >= 0 ? processingDurationMs : throw new ArgumentOutOfRangeException(nameof(processingDurationMs));
        CreatedAtUtc = createdAtUtc;
    }

    public Guid? DeviceId { get; private set; }
    public Guid? FaceProfileId { get; private set; }
    public Guid? FaceEnrollmentId { get; private set; }
    public Guid? SantriId { get; private set; }
    public Guid? SesiId { get; private set; }
    public Guid? PresensiId { get; private set; }
    public FaceRecognitionEventSource Source { get; private set; }
    public RecognitionOutcome RecognitionOutcome { get; private set; }
    public AttendanceOutcome AttendanceOutcome { get; private set; }
    public double? Similarity { get; private set; }
    public double? Distance { get; private set; }
    public string? FailureReason { get; private set; }
    public int ProcessingDurationMs { get; private set; }

    private static TEnum RequireDefined<TEnum>(TEnum value, string name)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(name);

    private static double? RequireFinite(double? value, string name) =>
        value is null || double.IsFinite(value.Value) ? value : throw new ArgumentOutOfRangeException(name);

    private static string? NormalizeFailureReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= MaximumFailureReasonLength
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(value));
    }
}
