using System.Security.Claims;
using KH2.ManagementSystem.Api.Contracts.Auth;
using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IUserAuthenticator authenticator,
    IAccessTokenProvider accessTokenProvider,
    IPasswordHasher passwordHasher,
    IEmailVerificationCodeService emailVerificationCodeService,
    AppDbContext dbContext,
    IWebHostEnvironment environment)
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
        if (string.IsNullOrWhiteSpace(request.ResolvedIdentity) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Login failed.",
                Detail = "Identity and password are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var user = await authenticator.AuthenticateAsync(
            request.ResolvedIdentity,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Login failed.",
                Detail = "Identity or password is invalid.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var token = accessTokenProvider.Create(user);

        return Ok(new LoginResponse(
            token.AccessToken,
            token.TokenType,
            token.ExpiresInSeconds,
            user.Username,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.EmailConfirmed,
            user.MustChangePassword,
            user.IsActive));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var username = User.FindFirstValue("username") ?? string.Empty;
        var fullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var emailConfirmed = bool.TryParse(User.FindFirstValue("email_confirmed"), out var parsedEmailConfirmed) && parsedEmailConfirmed;
        var mustChangePassword = bool.TryParse(User.FindFirstValue("must_change_password"), out var parsedMustChangePassword) && parsedMustChangePassword;
        var isActive = bool.TryParse(User.FindFirstValue("is_active"), out var parsedIsActive) && parsedIsActive;

        return Ok(new AuthMeResponse(
            userId,
            username,
            fullName,
            email,
            role,
            emailConfirmed,
            mustChangePassword,
            isActive));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        var currentPasswordValid = passwordHasher.VerifyPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword);

        if (!currentPasswordValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Change password failed.",
                Detail = "Current password is invalid.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var newPasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.SetPasswordHash(newPasswordHash);
        user.CompletePasswordChange();

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("set-email")]
    public async Task<IActionResult> SetEmail(
        [FromBody] SetEmailRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized();
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        var emailAlreadyUsed = await dbContext.Users
            .AnyAsync(x => x.Id != parsedUserId && x.Email == normalizedEmail, cancellationToken);

        if (emailAlreadyUsed)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email already in use.",
                Detail = "Please use another email address.",
                Status = StatusCodes.Status409Conflict
            });
        }

        user.SetEmail(normalizedEmail);
        user.MarkEmailUnconfirmed();

        await dbContext.SaveChangesAsync(cancellationToken);

        var verificationCode = await emailVerificationCodeService.GenerateAsync(
            user.Id,
            normalizedEmail,
            cancellationToken);

        return Ok(new SetEmailResponse(
            normalizedEmail,
            false,
            environment.IsDevelopment() ? verificationCode : null));
    }

    [Authorize]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Email verification failed.",
                Detail = "User email is not set.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var verified = await emailVerificationCodeService.VerifyAsync(
            user.Id,
            user.Email,
            request.Code,
            cancellationToken);

        if (!verified)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Email verification failed.",
                Detail = "Verification code is invalid or expired.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        user.MarkEmailConfirmed();
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}
