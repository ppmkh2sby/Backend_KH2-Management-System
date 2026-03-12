using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.Auth;
using KH2.ManagementSystem.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IUserAuthenticator authenticator,
    IAccessTokenProvider accessTokenProvider)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authenticator.AuthenticateAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Login failed.",
                Detail = "Email or password is invalid.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var token = accessTokenProvider.Create(user);

        return Ok(new LoginResponse(
            token.AccessToken,
            token.TokenType,
            token.ExpiresInSeconds,
            user.FullName,
            user.Email,
            user.Role.ToString()));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var fullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        return Ok(new AuthMeResponse(
            userId,
            fullName,
            email,
            role));
    }
}