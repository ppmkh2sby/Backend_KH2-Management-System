using Microsoft.AspNetCore.Http;

namespace KH2.ManagementSystem.Api.Contracts.FaceRecognition;

public sealed record CreateFaceAttendanceSessionRequest(string Kelas, string Kegiatan, string Waktu, DateOnly Tanggal);

public sealed record FaceAttendanceSessionResponse(
    Guid Id,
    string Kelas,
    string Kegiatan,
    string Waktu,
    DateOnly Tanggal,
    Guid OpenerUserId,
    string Status,
    DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record FaceAttendanceRecordResponse(
    Guid EventId,
    Guid? SantriId,
    string? SantriName,
    decimal? Confidence,
    DateTimeOffset CapturedAtUtc,
    string Status,
    string? RejectionReason,
    Guid? PresensiId);

public sealed record FaceCheckInResponse(
    string Status,
    Guid? SantriId,
    decimal? Confidence,
    string? Reason,
    Guid? PresensiId);

public class FacePhotoFormRequest
{
    public IFormFile? Photo { get; init; }
}

public sealed class FaceEnrollmentCaptureFormRequest : FacePhotoFormRequest
{
    public int CaptureOrder { get; init; }
}
