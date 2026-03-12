using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Route("api/v1/test/authorization")]
public sealed class AuthorizationTestController(
    IAuthorizationService authorizationService)
    : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            Message = "Admin policy passed."
        });
    }

    [Authorize(Policy = AuthorizationPolicies.InternalManagement)]
    [HttpGet("internal-management")]
    public IActionResult InternalManagement()
    {
        return Ok(new
        {
            Message = "Internal management policy passed."
        });
    }

    [Authorize(Policy = AuthorizationPolicies.CanReadAllSantri)]
    [HttpGet("santri/all")]
    public IActionResult CanReadAllSantri()
    {
        return Ok(new
        {
            Message = "Read all santri policy passed."
        });
    }

    [Authorize]
    [HttpGet("santri/{santriId:guid}")]
    public async Task<IActionResult> CanAccessSantri(
        Guid santriId,
        CancellationToken cancellationToken)
    {
        var resource = new SantriAccessResource(santriId);

        var authorizationResult = await authorizationService.AuthorizeAsync(
            User,
            resource,
            AuthorizationPolicies.CanAccessSantri);

        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        return Ok(new
        {
            Message = "Santri resource access policy passed.",
            SantriId = santriId
        });
    }
}