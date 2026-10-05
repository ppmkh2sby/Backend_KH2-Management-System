using System.Text.Json;
using KH2.ManagementSystem.Domain.FaceRecognition;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class TrustedFaceAttendanceDomainTests
{
    [Fact]
    public void AttendanceDeviceKeepsOnlyTheProvidedHashAndTracksLastSeen()
    {
        var created = DateTimeOffset.UtcNow;
        var device = new AttendanceDevice(Guid.NewGuid(), "Gate A", "hashed-device-secret", created, "Main gate");

        device.MarkSeen(created.AddMinutes(1));

        Assert.Equal("hashed-device-secret", device.ApiKeyHash);
        Assert.True(device.IsActive);
        Assert.Equal(created.AddMinutes(1), device.LastSeenAtUtc);
        Assert.Equal("Main gate", device.LocationLabel);
    }

    [Fact]
    public void DeviceRecognitionEventContainsOnlySafeAuditFields()
    {
        var evt = new FaceRecognitionEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), true, 0.91, 0.09, null, 123, DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(evt);

        Assert.True(evt.Recognized);
        Assert.Equal(0.91, evt.Similarity);
        Assert.Equal(0.09, evt.Distance);
        Assert.DoesNotContain("embedding", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("image", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apikey", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeviceRecognitionEventRejectsNegativeProcessingDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FaceRecognitionEvent(Guid.NewGuid(), null, null, null, null,
            null, null, false, null, null, "UnknownFace", -1, DateTimeOffset.UtcNow));
    }
}
