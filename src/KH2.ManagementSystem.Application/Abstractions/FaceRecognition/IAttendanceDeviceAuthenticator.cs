namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

public sealed record AttendanceDeviceAuthenticationResult(bool IsAuthenticated, Guid? DeviceId)
{
    public static AttendanceDeviceAuthenticationResult Denied() => new(false, null);
    public static AttendanceDeviceAuthenticationResult Authenticated(Guid deviceId) => new(true, deviceId);
}

public interface IAttendanceDeviceAuthenticator
{
    Task<AttendanceDeviceAuthenticationResult> AuthenticateAsync(
        string? deviceId,
        string? apiKey,
        CancellationToken cancellationToken = default);
}
