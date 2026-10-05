using System.Security.Claims;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = "Santri")]
[EnableRateLimiting("FaceRecognition")]
[Route("api/v1/face-profiles/me/enrollment")]
public sealed class FaceProfileEnrollmentController(EnrollFaceProfile enrollment) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(27 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 27 * 1024 * 1024)]
    public async Task<ActionResult<FaceProfileEnrollmentDto>> Enroll(
        [FromForm] List<IFormFile> photos, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        if (photos.Count > 5 || photos.Any(photo => photo.Length is <= 0 or > 5 * 1024 * 1024 ||
            photo.ContentType is not ("image/jpeg" or "image/png")))
            return Problem(statusCode: 400, title: "InvalidImage");
        var streams = new List<Stream>(photos.Count);
        try
        {
            var images = photos.Select(photo =>
            {
                var stream = photo.OpenReadStream();
                streams.Add(stream);
                return new FaceImage("capture", photo.ContentType, stream);
            }).ToArray();
            var result = await enrollment.HandleAsync(userId, images, cancellationToken);
            if (result.IsSuccess) return StatusCode(StatusCodes.Status201Created, result.Value);
            var status = result.Error.Code switch
            {
                "SantriNotFound" => 403,
                "AlreadyEnrolled" => 409,
                "FaceServiceUnavailable" => 503,
                "FaceServiceTimeout" => 504,
                "InvalidFaceServiceResponse" => 502,
                _ => 400
            };
            return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
        }
        finally
        {
            foreach (var stream in streams) await stream.DisposeAsync();
        }
    }
}
