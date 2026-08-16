using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Santri))]
[Route("api/v1/face-enrollment/me")]
public sealed class FaceEnrollmentController(
    AppDbContext dbContext,
    IFaceRecognitionClient faceRecognitionClient,
    IFaceCaptureStorage captureStorage,
    IClock clock) : ControllerBase
{
    private static readonly string[] Poses = ["lurus", "sedikit-kiri", "sedikit-kanan", "menengadah", "menunduk"];
    private const long MaximumPhotoBytes = 5 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<FaceEnrollmentResponse>> GetMe(CancellationToken cancellationToken)
    {
        var santriId = await GetCurrentSantriIdAsync(cancellationToken);
        if (santriId is null) return Unauthorized();

        var enrollment = await dbContext.FaceEnrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SantriId == santriId, cancellationToken);
        var captured = enrollment is null
            ? []
            : await dbContext.FaceEnrollmentCaptures.AsNoTracking()
                .Where(x => x.EnrollmentId == enrollment.Id)
                .Select(x => x.Sequence)
                .ToArrayAsync(cancellationToken);

        return Ok(ToResponse(enrollment, captured));
    }

    [HttpPost("captures")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FaceEnrollmentCaptureResponse>> Capture(
        [FromForm] FaceEnrollmentCaptureFormRequest request,
        CancellationToken cancellationToken)
    {
        var santriId = await GetCurrentSantriIdAsync(cancellationToken);
        if (santriId is null) return Unauthorized();
        if (!TryValidatePhoto(request.Photo, out var error)) return BadRequestProblem(error!);
        if (request.CaptureOrder is < 1 or > 5) return BadRequestProblem("captureOrder harus bernilai 1 sampai 5.");

        var expectedPose = Poses[request.CaptureOrder - 1];
        var photo = request.Photo!;
        try
        {
            await using var validationStream = photo.OpenReadStream();
            var validation = await faceRecognitionClient.ValidateEnrollmentCaptureAsync(
                new FaceImage(photo.FileName, photo.ContentType, validationStream), expectedPose, cancellationToken);
            if (!validation.IsValid) return BadRequestProblem(validation.Reason ?? "Capture wajah tidak valid.");
        }
        catch (FaceRecognitionUnavailableException)
        {
            return AiUnavailable();
        }

        var enrollment = await dbContext.FaceEnrollments
            .FirstOrDefaultAsync(x => x.SantriId == santriId, cancellationToken);
        if (enrollment?.Status == FaceEnrollmentStatus.Registered)
        {
            return ConflictProblem("Wajah sudah terdaftar. Reset profil wajah sendiri sebelum mendaftar ulang.");
        }

        enrollment ??= new FaceEnrollment(Guid.NewGuid(), santriId.Value);
        var exists = await dbContext.FaceEnrollmentCaptures.AnyAsync(
            x => x.EnrollmentId == enrollment.Id && x.Sequence == request.CaptureOrder, cancellationToken);
        if (exists) return ConflictProblem($"Capture ke-{request.CaptureOrder} sudah tersimpan.");

        StoredFaceCapture stored;
        try
        {
            await using var content = photo.OpenReadStream();
            stored = await captureStorage.SaveAsync(enrollment.Id, request.CaptureOrder, photo.FileName, photo.ContentType, content, cancellationToken);
        }
        catch (IOException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Penyimpanan capture wajah tidak tersedia.");
        }

        try
        {
            if (dbContext.Entry(enrollment).State == EntityState.Detached) dbContext.FaceEnrollments.Add(enrollment);
            dbContext.FaceEnrollmentCaptures.Add(new FaceEnrollmentCapture(Guid.NewGuid(), enrollment.Id, request.CaptureOrder, expectedPose, stored.StorageKey, stored.ContentType));
            var total = await dbContext.FaceEnrollmentCaptures.CountAsync(x => x.EnrollmentId == enrollment.Id, cancellationToken) + 1;
            enrollment.SetCaptureCount(total, clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new FaceEnrollmentCaptureResponse(request.CaptureOrder, expectedPose, total, "proses"));
        }
        catch (DbUpdateException)
        {
            await captureStorage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            return ConflictProblem($"Capture ke-{request.CaptureOrder} sudah tersimpan.");
        }
        catch
        {
            await captureStorage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }
    }

    [HttpPost("complete")]
    public async Task<ActionResult<FaceEnrollmentResponse>> Complete(CancellationToken cancellationToken)
    {
        var santriId = await GetCurrentSantriIdAsync(cancellationToken);
        if (santriId is null) return Unauthorized();
        var enrollment = await dbContext.FaceEnrollments.FirstOrDefaultAsync(x => x.SantriId == santriId, cancellationToken);
        if (enrollment is null) return ConflictProblem("Lima capture wajah belum tersedia.");
        if (enrollment.Status == FaceEnrollmentStatus.Registered)
        {
            var existingCaptures = await GetCaptureSequencesAsync(enrollment.Id, cancellationToken);
            return Ok(ToResponse(enrollment, existingCaptures));
        }

        var captures = await dbContext.FaceEnrollmentCaptures
            .Where(x => x.EnrollmentId == enrollment.Id && x.IsValid)
            .OrderBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
        if (captures.Count != 5 || !captures.Select(x => x.Sequence).SequenceEqual([1, 2, 3, 4, 5]))
        {
            return ConflictProblem("Lima capture valid dengan urutan pose 1 sampai 5 wajib tersedia.");
        }

        var streams = new List<Stream>(captures.Count);
        try
        {
            var images = new List<FaceImage>(captures.Count);
            foreach (var capture in captures)
            {
                var stream = await captureStorage.OpenReadAsync(capture.StorageKey, cancellationToken);
                streams.Add(stream);
                images.Add(new FaceImage($"{capture.Sequence}.jpg", capture.ContentType, stream));
            }

            var aiResult = await faceRecognitionClient.EnrollAsync(santriId.Value, images, cancellationToken);
            if (!aiResult.IsAccepted || string.IsNullOrWhiteSpace(aiResult.ProviderProfileId))
            {
                enrollment.Reject(aiResult.Reason ?? "AI menolak profil wajah.", clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                return BadRequestProblem(aiResult.Reason ?? "AI menolak profil wajah.");
            }

            var profile = await dbContext.FaceProfiles.FirstOrDefaultAsync(x => x.SantriId == santriId, cancellationToken);
            if (profile is null) dbContext.FaceProfiles.Add(new FaceProfile(Guid.NewGuid(), santriId.Value, aiResult.ProviderProfileId, clock.UtcNow));
            else profile.UpdateProviderProfile(aiResult.ProviderProfileId, clock.UtcNow);
            enrollment.Register(clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(ToResponse(enrollment, captures.Select(x => x.Sequence).ToArray()));
        }
        catch (FaceRecognitionUnavailableException)
        {
            return AiUnavailable();
        }
        finally
        {
            foreach (var stream in streams) await stream.DisposeAsync();
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        var santriId = await GetCurrentSantriIdAsync(cancellationToken);
        if (santriId is null) return Unauthorized();
        var enrollment = await dbContext.FaceEnrollments.FirstOrDefaultAsync(x => x.SantriId == santriId, cancellationToken);
        if (enrollment is null) return NoContent();
        var profile = await dbContext.FaceProfiles.FirstOrDefaultAsync(x => x.SantriId == santriId, cancellationToken);
        try
        {
            if (profile is not null) await faceRecognitionClient.DeleteProfileAsync(profile.ProviderProfileId, cancellationToken);
        }
        catch (FaceRecognitionUnavailableException)
        {
            return AiUnavailable();
        }

        var captures = await dbContext.FaceEnrollmentCaptures.Where(x => x.EnrollmentId == enrollment.Id).ToListAsync(cancellationToken);
        foreach (var capture in captures) await captureStorage.DeleteAsync(capture.StorageKey, cancellationToken);
        dbContext.FaceEnrollmentCaptures.RemoveRange(captures);
        if (profile is not null) dbContext.FaceProfiles.Remove(profile);
        dbContext.FaceEnrollments.Remove(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<Guid?> GetCurrentSantriIdAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await dbContext.Santris.AsNoTracking().Where(x => x.UserId == userId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int[]> GetCaptureSequencesAsync(Guid enrollmentId, CancellationToken cancellationToken) =>
        await dbContext.FaceEnrollmentCaptures.AsNoTracking().Where(x => x.EnrollmentId == enrollmentId).Select(x => x.Sequence).ToArrayAsync(cancellationToken);

    private static FaceEnrollmentResponse ToResponse(FaceEnrollment? enrollment, IEnumerable<int> captured) => new(
        enrollment is null ? "belum-terdaftar" : enrollment.Status switch { FaceEnrollmentStatus.InProgress => "proses", FaceEnrollmentStatus.Registered => "terdaftar", _ => "ditolak" },
        enrollment?.CaptureCount ?? 0, 5,
        Poses.Select((pose, index) => new FaceCaptureGuideResponse(index + 1, pose, captured.Contains(index + 1))).ToArray(),
        enrollment?.RegisteredAtUtc, enrollment?.EmbeddingUpdatedAtUtc, enrollment?.RejectionReason);

    private static bool TryValidatePhoto(IFormFile? photo, out string? error)
    {
        if (photo is null || photo.Length == 0) { error = "Photo wajib diisi."; return false; }
        if (photo.Length > MaximumPhotoBytes) { error = "Ukuran photo maksimal 5 MB."; return false; }
        if (photo.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) { error = "Format photo harus JPEG, PNG, atau WebP."; return false; }
        error = null; return true;
    }

    private static ObjectResult AiUnavailable() => new ObjectResult(new ProblemDetails { Status = StatusCodes.Status503ServiceUnavailable, Title = "Layanan AI pengenalan wajah tidak tersedia.", Detail = "Tidak ada presensi atau perubahan profil yang dicatat. Coba kembali nanti." }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
    private static BadRequestObjectResult BadRequestProblem(string detail) => new(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Permintaan face enrollment tidak valid.", Detail = detail });
    private static ObjectResult ConflictProblem(string detail) => new(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Status face enrollment tidak memungkinkan.", Detail = detail }) { StatusCode = StatusCodes.Status409Conflict };
}
