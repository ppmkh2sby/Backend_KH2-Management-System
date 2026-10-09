namespace KH2.ManagementSystem.Api.Contracts.FaceRecognition;

public sealed class DeviceFaceAttendanceRequest
{
    public Guid SesiId { get; init; }
    public IFormFile? Image { get; init; }
}

public sealed record DeviceFaceAttendanceResponse(
    bool Recognized,
    string Outcome,
    Guid? PresensiId,
    string? Reason);
