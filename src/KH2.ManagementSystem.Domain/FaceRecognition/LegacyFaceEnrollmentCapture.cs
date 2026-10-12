using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class LegacyFaceEnrollmentCapture : AuditableEntity<Guid>
{
    public LegacyFaceEnrollmentCapture(Guid id, Guid enrollmentId, int sequence, string pose, string storageKey, string contentType) : base(id)
    { EnrollmentId = enrollmentId; Sequence = sequence; Pose = Require(pose); StorageKey = Require(storageKey); ContentType = Require(contentType); }
    public Guid EnrollmentId { get; private set; }
    public int Sequence { get; private set; }
    public string Pose { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public bool IsValid { get; private set; } = true;
    private static string Require(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.") : value.Trim();
}
