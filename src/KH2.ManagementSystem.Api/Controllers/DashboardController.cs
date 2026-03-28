using System.Security.Claims;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriAttendance;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriLog;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriProgress;
using KH2.ManagementSystem.BuildingBlocks.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public sealed class DashboardController(
    IQueryHandler<GetMySantriDashboardQuery, Result<SantriDashboardDto>> dashboardHandler,
    IQueryHandler<GetMySantriAttendanceQuery, Result<SantriDashboardAttendancePageDto>> attendanceHandler,
    IQueryHandler<GetMySantriProgressQuery, Result<SantriDashboardProgressPageDto>> progressHandler,
    IQueryHandler<GetMySantriLogQuery, Result<SantriDashboardLogPageDto>> logHandler)
    : ControllerBase
{
    [HttpGet("santri/me")]
    [HttpGet("santri/me/home")]
    [ProducesResponseType(typeof(SantriDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySantriDashboard(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await dashboardHandler.HandleAsync(new GetMySantriDashboardQuery(userId), cancellationToken);
        return ToActionResult(result, "Santri dashboard unavailable.");
    }

    [HttpGet("santri/me/presensi")]
    [ProducesResponseType(typeof(SantriDashboardAttendancePageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySantriAttendance(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await attendanceHandler.HandleAsync(new GetMySantriAttendanceQuery(userId), cancellationToken);
        return ToActionResult(result, "Santri attendance dashboard unavailable.");
    }

    [HttpGet("santri/me/progres-keilmuan")]
    [ProducesResponseType(typeof(SantriDashboardProgressPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySantriProgress(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await progressHandler.HandleAsync(new GetMySantriProgressQuery(userId), cancellationToken);
        return ToActionResult(result, "Santri progress dashboard unavailable.");
    }

    [HttpGet("santri/me/log-keluar-masuk")]
    [ProducesResponseType(typeof(SantriDashboardLogPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySantriLogs(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await logHandler.HandleAsync(new GetMySantriLogQuery(userId), cancellationToken);
        return ToActionResult(result, "Santri log dashboard unavailable.");
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }

    private IActionResult ToActionResult<T>(Result<T> result, string title)
    {
        if (result.IsFailure || result.Value is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = title,
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }
}
