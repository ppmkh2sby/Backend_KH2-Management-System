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
    public void AttendanceDeviceRotationReplacesHashAndAdvancesVersionOnce()
    {
        var created = DateTimeOffset.UtcNow;
        var device = new AttendanceDevice(Guid.NewGuid(), "Gate A", "old-hash", created);
        var rotated = created.AddDays(1);

        device.RotateKey("new-hash", rotated);

        Assert.Equal("new-hash", device.ApiKeyHash);
        Assert.Equal(2, device.KeyVersion);
        Assert.Equal(rotated, device.KeyRotatedAtUtc);
        Assert.Equal(rotated, device.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AttendanceDeviceRotationRejectsBlankHashes(string newApiKeyHash)
    {
        var device = new AttendanceDevice(Guid.NewGuid(), "Gate A", "old-hash", DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => device.RotateKey(newApiKeyHash, DateTimeOffset.UtcNow));
        Assert.Equal("old-hash", device.ApiKeyHash);
        Assert.Equal(1, device.KeyVersion);
    }

    [Fact]
    public void DeviceRecognitionEventRecognizedAndRecordedIsValid()
    {
        var evt = CreateEvent(RecognitionOutcome.Recognized, AttendanceOutcome.Recorded, 0.91, 0.09);

        Assert.Equal(RecognitionOutcome.Recognized, evt.RecognitionOutcome);
        Assert.Equal(AttendanceOutcome.Recorded, evt.AttendanceOutcome);
    }

    [Fact]
    public void DeviceRecognitionEventRecognizedAndDuplicateIsValid()
    {
        var evt = CreateEvent(RecognitionOutcome.Recognized, AttendanceOutcome.Duplicate, 0.91, 0.09);

        Assert.Equal(RecognitionOutcome.Recognized, evt.RecognitionOutcome);
        Assert.Equal(AttendanceOutcome.Duplicate, evt.AttendanceOutcome);
        Assert.Null(evt.FailureReason);
    }

    [Fact]
    public void DeviceRecognitionEventNotAttemptedAndInvalidSessionIsValid()
    {
        var evt = CreateEvent(RecognitionOutcome.NotAttempted, AttendanceOutcome.InvalidSession, null, null, "InvalidSession");

        Assert.Equal(RecognitionOutcome.NotAttempted, evt.RecognitionOutcome);
        Assert.Equal(AttendanceOutcome.InvalidSession, evt.AttendanceOutcome);
        Assert.Equal("InvalidSession", evt.FailureReason);
    }

    [Theory]
    [InlineData(RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted)]
    [InlineData(RecognitionOutcome.Rejected, AttendanceOutcome.NotAttempted)]
    [InlineData(RecognitionOutcome.Error, AttendanceOutcome.Failed)]
    public void DeviceRecognitionEventSupportsCanonicalNonRecordedOutcomes(
        RecognitionOutcome recognitionOutcome,
        AttendanceOutcome attendanceOutcome)
    {
        var evt = CreateEvent(recognitionOutcome, attendanceOutcome, null, null, "SafeDiagnosticCode");

        Assert.Equal(recognitionOutcome, evt.RecognitionOutcome);
        Assert.Equal(attendanceOutcome, evt.AttendanceOutcome);
        Assert.Equal("SafeDiagnosticCode", evt.FailureReason);
    }

    [Fact]
    public void DeviceRecognitionEventContainsOnlySafeAuditFields()
    {
        var evt = CreateEvent(RecognitionOutcome.Recognized, AttendanceOutcome.Recorded, 0.91, 0.09);
        var json = JsonSerializer.Serialize(evt);

        Assert.Equal(0.91, evt.Similarity);
        Assert.Equal(0.09, evt.Distance);
        Assert.Null(typeof(FaceRecognitionEvent).GetProperty("Recognized"));
        Assert.Null(typeof(FaceRecognitionEvent).GetProperty("Embedding"));
        Assert.Null(typeof(FaceRecognitionEvent).GetProperty("Image"));
        Assert.Null(typeof(FaceRecognitionEvent).GetProperty("ApiKey"));
        Assert.DoesNotContain("embedding", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("image", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apikey", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeviceRecognitionEventRejectsNegativeProcessingDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEvent(
            RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted, null, null, processingDurationMs: -1));
    }

    [Fact]
    public void DeviceRecognitionEventAcceptsZeroProcessingDuration() =>
        Assert.Equal(0, CreateEvent(RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted, null, null, processingDurationMs: 0).ProcessingDurationMs);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DeviceRecognitionEventRejectsNonFiniteSimilarity(double similarity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEvent(
            RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted, similarity, null));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DeviceRecognitionEventRejectsNonFiniteDistance(double distance) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEvent(
            RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted, null, distance));

    [Fact]
    public void DeviceRecognitionEventAllowsNullFailureReason()
    {
        var evt = CreateEvent(RecognitionOutcome.Unknown, AttendanceOutcome.NotAttempted, null, null);

        Assert.Null(evt.FailureReason);
    }

    private static FaceRecognitionEvent CreateEvent(
        RecognitionOutcome recognitionOutcome,
        AttendanceOutcome attendanceOutcome,
        double? similarity,
        double? distance,
        string? failureReason = null,
        int processingDurationMs = 123) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            FaceRecognitionEventSource.AttendanceDevice, recognitionOutcome, attendanceOutcome, similarity, distance,
            failureReason, processingDurationMs, DateTimeOffset.UtcNow);
}
