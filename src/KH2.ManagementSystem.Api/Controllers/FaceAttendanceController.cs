using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Kegiatans;
using KH2.ManagementSystem.Domain.Presensis;
using KH2.ManagementSystem.Domain.Sesis;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("FaceRecognition")]
[Route("api/v1/face-attendance/sessions")]
public sealed class FaceAttendanceController(
    AppDbContext dbContext,
    IFaceRecognitionClient faceRecognitionClient,
    IClock clock,
    IOptions<FaceRecognitionOptions> faceOptions) : ControllerBase
{
    private const long MaximumPhotoBytes = 5 * 1024 * 1024;

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanOperateFaceAttendance)]
    public async Task<ActionResult<FaceAttendanceSessionResponse>> Create(
        [FromBody] CreateFaceAttendanceSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Kelas) || string.IsNullOrWhiteSpace(request.Kegiatan)) return BadRequestProblem("Kelas dan kegiatan wajib diisi.");
        var waktu = NormalizeWaktu(request.Waktu);
        if (waktu is null) return BadRequestProblem("Waktu kegiatan tidak valid.");

        var sessionId = Guid.NewGuid();
        var kegiatan = new Kegiatan(Guid.NewGuid(), $"face-{sessionId:N}"[..30], waktu, request.Kegiatan.Trim());
        var sesi = new Sesi(Guid.NewGuid(), kegiatan.Id, request.Tanggal, $"Sesi face attendance {sessionId:N}");
        var session = new FaceAttendanceSession(sessionId, request.Kelas, request.Kegiatan, waktu, request.Tanggal, userId, kegiatan.Id, sesi.Id);
        dbContext.AddRange(kegiatan, sesi, session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, ToResponse(session));
    }

    [HttpPost("{id:guid}/verify-opener")]
    [Authorize(Policy = AuthorizationPolicies.CanOperateFaceAttendance)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FaceAttendanceSessionResponse>> VerifyOpener(Guid id, [FromForm] FacePhotoFormRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (!TryValidatePhoto(request.Photo, out var error)) return BadRequestProblem(error!);
        var photo = request.Photo!;
        var session = await dbContext.FaceAttendanceSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (session is null) return NotFound();
        if (session.OpenerUserId != userId) return Forbid();
        if (session.Status == FaceAttendanceSessionStatus.Closed) return ConflictProblem("Sesi telah ditutup.");

        var profile = await dbContext.FaceProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is null) return BadRequestProblem("Daftarkan wajah Anda terlebih dahulu sebelum membuka presensi wajah.");

        try
        {
            await using var stream = photo.OpenReadStream();
            var result = await faceRecognitionClient.VerifyOpenerAsync(profile.ProviderProfileId, new FaceImage(photo.FileName, photo.ContentType, stream), cancellationToken);
            if (!result.IsVerified) return BadRequestProblem(result.Reason ?? "Wajah petugas tidak terverifikasi.");
            session.Open(clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(ToResponse(session));
        }
        catch (FaceRecognitionUnavailableException)
        {
            return AiUnavailable();
        }
    }

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Roles = nameof(UserRole.Santri))]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FaceCheckInResponse>> CheckIn(Guid id, [FromForm] FacePhotoFormRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (!TryValidatePhoto(request.Photo, out var error)) return BadRequestProblem(error!);
        var photo = request.Photo!;
        var santri = await dbContext.Santris.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (santri is null) return Unauthorized();
        var session = await dbContext.FaceAttendanceSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (session is null) return NotFound();
        if (session.Status != FaceAttendanceSessionStatus.Open) return ConflictProblem("Check-in hanya tersedia saat sesi berstatus open.");

        FaceRecognitionResult recognition;
        try
        {
            await using var stream = photo.OpenReadStream();
            recognition = await faceRecognitionClient.RecognizeAsync(new FaceImage(photo.FileName, photo.ContentType, stream), cancellationToken);
        }
        catch (FaceRecognitionUnavailableException)
        {
            await SaveReviewEventAsync(session.Id, null, null, "Layanan AI pengenalan wajah tidak tersedia.", cancellationToken);
            return AiUnavailable();
        }

        Guid? recognizedUserId = null;
        if (!string.IsNullOrWhiteSpace(recognition.ProviderProfileId))
        {
            recognizedUserId = await dbContext.FaceProfiles.AsNoTracking()
                .Where(x => x.ProviderProfileId == recognition.ProviderProfileId)
                .Select(x => (Guid?)x.UserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var rejection = GetRecognitionRejection(recognition, recognizedUserId, userId, faceOptions.Value.ConfidenceThreshold);
        if (rejection is not null)
        {
            Guid? recognizedSantriId = recognizedUserId == userId ? santri.Id : null;
            await SaveReviewEventAsync(session.Id, recognizedSantriId, recognition.Confidence, rejection, cancellationToken);
            return Ok(new FaceCheckInResponse("review", recognizedSantriId, recognition.Confidence, rejection, null));
        }

        var exists = await dbContext.Presensis.AnyAsync(x => x.FaceAttendanceSessionId == session.Id && x.SantriId == santri.Id, cancellationToken);
        if (exists) return ConflictProblem("Santri sudah tercatat pada sesi presensi wajah ini.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var presensi = new Presensi(Guid.NewGuid(), santri.Id, santri.FullName, "hadir", session.KegiatanId, session.SesiId, "Face recognition", session.Waktu, PresensiSource.FaceRecognition, session.Id);
        var acceptedEvent = new FaceRecognitionEvent(Guid.NewGuid(), session.Id, santri.Id, recognition.Confidence, clock.UtcNow, FaceRecognitionEventStatus.Accepted, null);
        dbContext.Presensis.Add(presensi);
        dbContext.FaceRecognitionEvents.Add(acceptedEvent);
        acceptedEvent.Accept(presensi.Id, clock.UtcNow);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ConflictProblem("Santri sudah tercatat pada sesi presensi wajah ini.");
        }

        return Ok(new FaceCheckInResponse("accepted", santri.Id, recognition.Confidence, null, presensi.Id));
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = AuthorizationPolicies.CanOperateFaceAttendance)]
    public async Task<ActionResult<FaceAttendanceSessionResponse>> Close(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        var session = await dbContext.FaceAttendanceSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (session is null) return NotFound();
        if (session.OpenerUserId != userId && !User.IsInRole(UserRole.Admin.ToString())) return Forbid();
        try { session.Close(clock.UtcNow); }
        catch (InvalidOperationException exception) { return ConflictProblem(exception.Message); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(session));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanOperateFaceAttendance)]
    public async Task<ActionResult<IReadOnlyList<FaceAttendanceSessionResponse>>> GetList([FromQuery] DateOnly? tanggal, CancellationToken cancellationToken)
    {
        var query = dbContext.FaceAttendanceSessions.AsNoTracking();
        if (tanggal.HasValue) query = query.Where(x => x.Tanggal == tanggal.Value);
        var sessions = await query.OrderByDescending(x => x.Tanggal).ThenByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
        return Ok(sessions.Select(ToResponse).ToArray());
    }

    [HttpGet("active")]
    [Authorize(Roles = nameof(UserRole.Santri))]
    public async Task<ActionResult<FaceAttendanceSessionResponse?>> GetActiveForCurrentSantri(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        var kelas = await dbContext.Santris.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Kelas)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(kelas)) return Unauthorized();

        var session = await dbContext.FaceAttendanceSessions.AsNoTracking()
            .Where(x => x.Kelas == kelas && x.Status == FaceAttendanceSessionStatus.Open)
            .OrderByDescending(x => x.VerifiedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return Ok(session is null ? null : ToResponse(session));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanOperateFaceAttendance)]
    public async Task<ActionResult<FaceAttendanceSessionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var session = await dbContext.FaceAttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return session is null ? NotFound() : Ok(ToResponse(session));
    }

    [HttpGet("{id:guid}/records")]
    [Authorize(Policy = AuthorizationPolicies.CanOperateFaceAttendance)]
    public async Task<ActionResult<IReadOnlyList<FaceAttendanceRecordResponse>>> GetRecords(Guid id, CancellationToken cancellationToken)
    {
        if (!await dbContext.FaceAttendanceSessions.AnyAsync(x => x.Id == id, cancellationToken)) return NotFound();
        var rows = await (
            from evt in dbContext.FaceRecognitionEvents.AsNoTracking()
            join santri in dbContext.Santris.AsNoTracking() on evt.SantriId equals santri.Id into santriRows
            from santri in santriRows.DefaultIfEmpty()
            where evt.SessionId == id
            orderby evt.CapturedAtUtc descending
            select new FaceAttendanceRecordResponse(evt.Id, evt.SantriId, santri == null ? null : santri.FullName, evt.Confidence, evt.CapturedAtUtc, ToStatus(evt.Status), evt.RejectionReason, evt.PresensiId))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    private async Task SaveReviewEventAsync(Guid sessionId, Guid? recognizedSantriId, decimal? confidence, string reason, CancellationToken cancellationToken)
    {
        dbContext.FaceRecognitionEvents.Add(new FaceRecognitionEvent(Guid.NewGuid(), sessionId, recognizedSantriId, confidence, clock.UtcNow, FaceRecognitionEventStatus.Review, reason));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? GetRecognitionRejection(FaceRecognitionResult result, Guid? recognizedUserId, Guid authenticatedUserId, decimal threshold)
    {
        if (result.FaceCount != 1) return result.FaceCount > 1 ? "Terdeteksi lebih dari satu wajah; perlu review manual." : "Wajah tidak terdeteksi; perlu review manual.";
        if (string.IsNullOrWhiteSpace(result.ProviderProfileId) || recognizedUserId is null) return "Wajah tidak dikenali; perlu review manual.";
        if (recognizedUserId != authenticatedUserId) return "Identitas wajah tidak sesuai dengan akun yang login; perlu review manual.";
        if (result.Confidence is null || result.Confidence < threshold) return "Confidence pengenalan wajah di bawah ambang aman; perlu review manual.";
        return null;
    }

    private static FaceAttendanceSessionResponse ToResponse(FaceAttendanceSession session) => new(session.Id, session.Kelas, session.Kegiatan, session.Waktu, session.Tanggal, session.OpenerUserId, ToStatus(session.Status), session.VerifiedAtUtc, session.ClosedAtUtc, session.CreatedAtUtc);
    private static string ToStatus(FaceAttendanceSessionStatus status) => status switch { FaceAttendanceSessionStatus.Draft => "draft", FaceAttendanceSessionStatus.AwaitingVerification => "menunggu-verifikasi", FaceAttendanceSessionStatus.Open => "open", _ => "closed" };
    private static string ToStatus(FaceRecognitionEventStatus status) => status switch { FaceRecognitionEventStatus.Accepted => "accepted", FaceRecognitionEventStatus.Rejected => "rejected", _ => "review" };
    private static string? NormalizeWaktu(string? value) => string.IsNullOrWhiteSpace(value) ? null : Presensi.Times.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase) ? value.Trim().ToLowerInvariant() : null;
    private bool TryGetCurrentUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    private static bool TryValidatePhoto(IFormFile? photo, out string? error) { if (photo is null || photo.Length == 0) { error = "Photo wajib diisi."; return false; } if (photo.Length > MaximumPhotoBytes) { error = "Ukuran photo maksimal 5 MB."; return false; } if (photo.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) { error = "Format photo harus JPEG, PNG, atau WebP."; return false; } error = null; return true; }
    private static BadRequestObjectResult BadRequestProblem(string detail) => new(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Permintaan face attendance tidak valid.", Detail = detail });
    private static ObjectResult ConflictProblem(string detail) => new(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Status sesi tidak memungkinkan.", Detail = detail }) { StatusCode = StatusCodes.Status409Conflict };
    private static ObjectResult AiUnavailable() => new(new ProblemDetails { Status = StatusCodes.Status503ServiceUnavailable, Title = "Layanan AI pengenalan wajah tidak tersedia.", Detail = "Tidak ada presensi otomatis yang dicatat. Hasil dapat direview manual." }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
}
