using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.InternalManagement)]
[EnableRateLimiting("FaceRecognition")]
[Route("api/v1/face-recognition")]
public sealed class FaceRecognitionController(RecognizeFaceImage recognition) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<FaceRecognitionDto>> Recognize([FromForm] IFormFile photo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(photo);
        if (photo.Length is <= 0 or > 5 * 1024 * 1024 || photo.ContentType is not ("image/jpeg" or "image/png"))
            return Problem(statusCode: 400, title: "InvalidImage");
        await using var stream = photo.OpenReadStream();
        var result = await recognition.HandleAsync(new FaceImage("capture", photo.ContentType, stream), cancellationToken);
        if (result.IsSuccess) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "RecognitionNotConfigured" or "FaceServiceUnavailable" => 503,
            "FaceServiceTimeout" => 504,
            "InvalidFaceServiceResponse" => 502,
            "InvalidFaceMatchResult" => 500,
            _ => 400
        };
        return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
    }
}
