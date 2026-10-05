using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

/// <summary>A fixed, trusted device permitted to submit face-attendance captures.</summary>
public sealed class AttendanceDevice : AuditableEntity<Guid>
{
    private AttendanceDevice() : base(Guid.Empty) { }

    public AttendanceDevice(Guid id, string name, string apiKeyHash, DateTimeOffset createdAtUtc, string? locationLabel = null)
        : base(id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Device id is required.", nameof(id));
        Name = Require(name, nameof(name));
        ApiKeyHash = Require(apiKeyHash, nameof(apiKeyHash));
        LocationLabel = Normalize(locationLabel);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;
    public string? LocationLabel { get; private set; }
    public string ApiKeyHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastSeenAtUtc { get; private set; }

    public void MarkSeen(DateTimeOffset now)
    {
        LastSeenAtUtc = now;
        Touch(now);
    }

    public void Disable(DateTimeOffset now)
    {
        IsActive = false;
        Touch(now);
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
