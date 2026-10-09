using System.Diagnostics;
using KH2.ManagementSystem.Api.Contracts.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Presensis;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("FaceAttendanceDevice")]
[Route("api/attendance/face-recognition")]
public sealed partial class DeviceFaceAttendanceController(
    AppDbContext dbContext,
    IAttendanceDeviceAuthenticator deviceAuthenticator,
    RecognizeFaceImage recognition,
    IClock clock,
    ILogger<DeviceFaceAttendanceController> logger) : ControllerBase
{
    private const long MaximumImageBytes = 5 * 1024 * 1024;
    private const string DeviceIdHeader = "X-Attendance-Device-Id";
    private const string DeviceKeyHeader = "X-Attendance-Device-Key";

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<DeviceFaceAttendanceResponse>> Record(
        [FromForm] DeviceFaceAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await deviceAuthenticator.AuthenticateAsync(
            Request.Headers[DeviceIdHeader].FirstOrDefault(),
            Request.Headers[DeviceKeyHeader].FirstOrDefault(),
            cancellationToken);
        if (!authentication.IsAuthenticated || authentication.DeviceId is null)
            return Unauthorized();

        if (!TryValidateImage(request.Image, out var validationError))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: validationError);

        var session = await (
            from sesi in dbContext.Sesis.AsNoTracking()
            join kegiatan in dbContext.Kegiatans.AsNoTracking() on sesi.KegiatanId equals kegiatan.Id
            where sesi.Id == request.SesiId
            select new AttendanceSession(sesi.Id, sesi.KegiatanId, kegiatan.Waktu))
            .SingleOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            await SaveEventAsync(authentication.DeviceId.Value, null, null, null, null,
                RecognitionOutcome.NotAttempted, AttendanceOutcome.InvalidSession, null, "InvalidSession", 0, cancellationToken);
            return NotFound();
        }

        var stopwatch = Stopwatch.StartNew();
        await using var imageStream = request.Image!.OpenReadStream();
        var result = await recognition.HandleAsync(
            new FaceImage("attendance-capture", request.Image.ContentType, imageStream), cancellationToken);
        stopwatch.Stop();
        var elapsed = ToMilliseconds(stopwatch.ElapsedMilliseconds);

        if (!result.IsSuccess)
        {
            var reason = MapRecognitionFailure(result.Error.Code);
            await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, null, null, null,
                RecognitionOutcome.Error, AttendanceOutcome.Failed, null, reason, elapsed, cancellationToken);
            return Problem(statusCode: ToStatusCode(result.Error.Code), title: reason);
        }

        var recognitionResult = result.Value!;
        if (!recognitionResult.IsRecognized || recognitionResult.SantriId is null)
        {
            var reason = recognitionResult.Reason is "AmbiguousMatch" ? "AmbiguousMatch" : "UnknownFace";
            var recognitionOutcome = reason == "AmbiguousMatch" ? RecognitionOutcome.Rejected : RecognitionOutcome.Unknown;
            await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, null, null, null,
                recognitionOutcome, AttendanceOutcome.NotAttempted, null, reason, elapsed, cancellationToken);
            return Ok(new DeviceFaceAttendanceResponse(false, "not-recognized", null, reason));
        }

        var profile = await dbContext.FaceProfiles.SingleOrDefaultAsync(profile =>
            profile.SantriId == recognitionResult.SantriId &&
            profile.Status == FaceProfileStatus.Active,
            cancellationToken);
        if (profile is null)
        {
            await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, null, null, recognitionResult.SantriId,
                RecognitionOutcome.Rejected, AttendanceOutcome.NotAttempted, recognitionResult.Similarity, "InactiveFaceProfile", elapsed, cancellationToken);
            return Ok(new DeviceFaceAttendanceResponse(false, "not-recognized", null, "UnknownFace"));
        }

        var enrollmentId = await dbContext.FaceEnrollments.AsNoTracking()
            .Where(item => item.FaceProfileId == profile.Id && item.Status == FaceEnrollmentStatus.Active)
            .Select(item => (Guid?)item.Id).SingleOrDefaultAsync(cancellationToken);
        if (enrollmentId is null)
        {
            await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, profile.Id, null, recognitionResult.SantriId,
                RecognitionOutcome.Rejected, AttendanceOutcome.NotAttempted, recognitionResult.Similarity, "InactiveFaceEnrollment", elapsed, cancellationToken);
            return Ok(new DeviceFaceAttendanceResponse(false, "not-recognized", null, "UnknownFace"));
        }

        var existingPresensiId = await FindPresensiIdAsync(recognitionResult.SantriId.Value, request.SesiId, cancellationToken);
        if (existingPresensiId is not null)
        {
            await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, profile.Id, enrollmentId,
                recognitionResult.SantriId, RecognitionOutcome.Recognized, AttendanceOutcome.Duplicate,
                recognitionResult.Similarity, null, elapsed, cancellationToken, existingPresensiId);
            return Conflict(new ProblemDetails { Title = "DuplicateAttendance", Status = StatusCodes.Status409Conflict });
        }

        var santri = await dbContext.Santris.SingleOrDefaultAsync(item => item.Id == recognitionResult.SantriId, cancellationToken);
        if (santri is null)
        {
            await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, profile.Id, enrollmentId,
                recognitionResult.SantriId, RecognitionOutcome.Rejected, AttendanceOutcome.NotAttempted,
                recognitionResult.Similarity, "SantriNotFound", elapsed, cancellationToken);
            return Ok(new DeviceFaceAttendanceResponse(false, "not-recognized", null, "UnknownFace"));
        }

        var presensi = new Presensi(Guid.NewGuid(), santri.Id, santri.FullName, "hadir", session.KegiatanId,
            session.SesiId, "Face recognition", session.Waktu, PresensiSource.FaceRecognition);
        var recognitionEvent = CreateEvent(authentication.DeviceId.Value, request.SesiId, profile.Id,
            enrollmentId, santri.Id, presensi.Id, RecognitionOutcome.Recognized, AttendanceOutcome.Recorded,
            recognitionResult.Similarity, null, elapsed);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Presensis.Add(presensi);
            dbContext.FaceRecognitionEvents.Add(recognitionEvent);
            profile.MarkVerified(clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (AttendanceDuplicateViolationDetector.IsExpectedSesiSantriDuplicate(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var racePresensiId = await FindPresensiIdAsync(santri.Id, request.SesiId, cancellationToken);
            await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, profile.Id, enrollmentId, santri.Id,
                RecognitionOutcome.Recognized, AttendanceOutcome.Duplicate, recognitionResult.Similarity, null,
                elapsed, cancellationToken, racePresensiId);
            return Conflict(new ProblemDetails { Title = "DuplicateAttendance", Status = StatusCodes.Status409Conflict });
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            try
            {
                await SaveEventAsync(authentication.DeviceId.Value, request.SesiId, profile.Id, enrollmentId, santri.Id,
                    RecognitionOutcome.Recognized, AttendanceOutcome.Failed, recognitionResult.Similarity,
                    "AttendancePersistenceFailed", elapsed, cancellationToken);
            }
            catch (DbUpdateException auditException)
            {
                LogFailureAuditPersistenceError(logger, auditException);
            }

            LogAttendancePersistenceError(logger, exception);
            throw;
        }

        return Created(string.Empty, new DeviceFaceAttendanceResponse(true, "recorded", presensi.Id, null));
    }

    private async Task SaveEventAsync(Guid deviceId, Guid? sesiId, Guid? profileId, Guid? enrollmentId, Guid? santriId,
        RecognitionOutcome recognitionOutcome, AttendanceOutcome attendanceOutcome, double? similarity, string? reason,
        int elapsed, CancellationToken cancellationToken, Guid? presensiId = null)
    {
        dbContext.FaceRecognitionEvents.Add(CreateEvent(deviceId, sesiId, profileId, enrollmentId, santriId,
            presensiId, recognitionOutcome, attendanceOutcome, similarity, reason, elapsed));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private FaceRecognitionEvent CreateEvent(Guid deviceId, Guid? sesiId, Guid? profileId, Guid? enrollmentId,
        Guid? santriId, Guid? presensiId, RecognitionOutcome recognitionOutcome, AttendanceOutcome attendanceOutcome,
        double? similarity, string? failureReason, int elapsed) =>
        new(Guid.NewGuid(), deviceId, profileId, enrollmentId, santriId, sesiId, presensiId,
            FaceRecognitionEventSource.AttendanceDevice, recognitionOutcome, attendanceOutcome,
            similarity, similarity is null ? null : 1d - similarity.Value, failureReason, elapsed, clock.UtcNow);

    private Task<Guid?> FindPresensiIdAsync(Guid santriId, Guid sesiId, CancellationToken cancellationToken) =>
        dbContext.Presensis.AsNoTracking()
            .Where(item => item.SantriId == santriId && item.SesiId == sesiId)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private static bool TryValidateImage(IFormFile? image, out string error)
    {
        if (image is null || image.Length == 0) { error = "InvalidImage"; return false; }
        if (image.Length > MaximumImageBytes) { error = "ImageTooLarge"; return false; }
        if (image.ContentType is not ("image/jpeg" or "image/png")) { error = "InvalidImage"; return false; }
        error = string.Empty;
        return true;
    }

    private static string MapRecognitionFailure(string code) => code switch
    {
        "FaceServiceUnavailable" or "FaceServiceTimeout" or "InvalidFaceServiceResponse" or "InvalidFaceMatchResult" or "RecognitionNotConfigured" => code,
        _ => "RecognitionFailed"
    };

    private static int ToStatusCode(string code) => code switch
    {
        "RecognitionNotConfigured" or "FaceServiceUnavailable" => StatusCodes.Status503ServiceUnavailable,
        "FaceServiceTimeout" => StatusCodes.Status504GatewayTimeout,
        "InvalidFaceServiceResponse" => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status400BadRequest
    };

    private static int ToMilliseconds(long milliseconds) => milliseconds > int.MaxValue ? int.MaxValue : (int)Math.Max(0, milliseconds);

    [LoggerMessage(EventId = 4101, Level = LogLevel.Error, Message = "Unable to persist face-attendance failure audit event.")]
    private static partial void LogFailureAuditPersistenceError(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 4102, Level = LogLevel.Error, Message = "Face attendance persistence failed after successful recognition.")]
    private static partial void LogAttendancePersistenceError(ILogger logger, Exception exception);

    private sealed record AttendanceSession(Guid SesiId, Guid KegiatanId, string Waktu);
}
