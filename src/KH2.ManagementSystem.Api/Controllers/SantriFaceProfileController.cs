using System.Security.Claims;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Api.Controllers;

public sealed record MyFaceEnrollmentStatusDto(bool IsEnrolled, string? Status = null, int SampleCount = 0,
    string? ModelName = null, string? ModelVersion = null, DateTimeOffset? EnrolledAtUtc = null);

[ApiController]
[Authorize(Roles = "Santri")]
[Route("api/v1/face-profiles/me")]
public sealed class SantriFaceProfileController(
    VerifySantriFace verification,
    AppDbContext dbContext,
    IOptions<FaceRecognitionServiceOptions> faceRecognitionOptions) : ControllerBase
{
    [HttpPost("verify")]
    [EnableRateLimiting("FaceRecognition")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<SantriFaceVerificationDto>> Verify(
        [FromForm] IFormFile? photo, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        if (photo is null || photo.Length is <= 0 or > 5 * 1024 * 1024 ||
            photo.ContentType is not ("image/jpeg" or "image/png"))
            return Problem(statusCode: 400, title: "InvalidImage");

        await using var stream = photo.OpenReadStream();
        var result = await verification.HandleAsync(userId,
            new FaceImage("capture", photo.ContentType, stream), cancellationToken);
        if (result.IsSuccess) return Ok(result.Value);

        var status = result.Error.Code switch
        {
            "SantriNotFound" => 403,
            "RecognitionNotConfigured" or "FaceServiceUnavailable" => 503,
            "FaceServiceTimeout" => 504,
            "InvalidFaceServiceResponse" => 502,
            "InvalidFaceMatchResult" => 500,
            _ => 400
        };
        return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
    }

    [HttpGet("enrollment")]
    public async Task<ActionResult<MyFaceEnrollmentStatusDto>> GetEnrollment(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var santriId = await dbContext.Santris.AsNoTracking()
            .Where(santri => santri.UserId == userId)
            .Select(santri => (Guid?)santri.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (santriId is null) return Forbid();

        var profile = await dbContext.FaceProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.SantriId == santriId, cancellationToken);
        if (profile is null) return Ok(new MyFaceEnrollmentStatusDto(false));

        var enrollment = await dbContext.FaceEnrollments.AsNoTracking()
            .Where(item => item.FaceProfileId == profile.Id && item.Status == FaceEnrollmentStatus.Active)
            .Select(item => new
            {
                item.Id,
                item.ModelName,
                item.ModelVersion,
                item.EnrolledAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (enrollment is null || profile.Status != FaceProfileStatus.Active)
            return Ok(new MyFaceEnrollmentStatusDto(false, profile.Status.ToString()));

        var sampleCount = await dbContext.FaceEmbeddings.AsNoTracking()
            .CountAsync(item => item.FaceEnrollmentId == enrollment.Id, cancellationToken);
        var isEnrolled = sampleCount == faceRecognitionOptions.Value.RequiredEnrollmentSamples;
        return Ok(new MyFaceEnrollmentStatusDto(isEnrolled, profile.Status.ToString(), sampleCount,
            enrollment.ModelName, enrollment.ModelVersion, enrollment.EnrolledAtUtc));
    }
}
