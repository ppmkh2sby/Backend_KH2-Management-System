using KH2.ManagementSystem.Domain.FaceRecognition;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class FaceRecognitionDomainTests
{
    [Fact]
    public void EnrollmentRegisterRequiresItsCompleteTransition()
    {
        var enrollment = new FaceEnrollment(Guid.NewGuid(), Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        enrollment.SetCaptureCount(5, now);
        enrollment.Register(now);

        Assert.Equal(FaceEnrollmentStatus.Registered, enrollment.Status);
        Assert.Equal(5, enrollment.CaptureCount);
        Assert.Equal(now, enrollment.EmbeddingUpdatedAtUtc);
    }

    [Fact]
    public void ClosedSessionCannotBeReopened()
    {
        var session = new FaceAttendanceSession(
            Guid.NewGuid(), "A", "Kajian", "malam", DateOnly.FromDateTime(DateTime.UtcNow), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        session.Open(now);
        session.Close(now);

        Assert.Throws<InvalidOperationException>(() => session.Open(now));
    }
}
