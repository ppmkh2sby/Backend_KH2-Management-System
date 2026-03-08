using KH2.ManagementSystem.Api.Contracts;
using KH2.ManagementSystem.Api.Options;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.Application.Features.System.GetSystemOverview;
using KH2.ManagementSystem.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(
    IQueryHandler<GetSystemOverviewQuery, Result<SystemOverviewDto>> handler,
    IOptions<ApplicationMetadataOptions> metadataOptions,
    IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType<SystemInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemInfoResponse>> GetInfo(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSystemOverviewQuery(), cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return Problem(
                title: "Unable to fetch system overview.",
                detail: result.Error.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var metadata = metadataOptions.Value;

        return Ok(new SystemInfoResponse(
            metadata.Name,
            metadata.Version,
            environment.EnvironmentName,
            result.Value.ServerTimeUtc));
    }
}
