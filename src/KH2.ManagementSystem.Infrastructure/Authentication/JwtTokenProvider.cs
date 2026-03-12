using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Application.Abstractions.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KH2.ManagementSystem.Infrastructure.Authentication;

public sealed class JwtTokenProvider(
    IOptions<JwtOptions> options,
    IClock clock)
    : IAccessTokenProvider
{
    public AccessTokenResult Create(AuthenticatedUser user)
    {
        var jwtOptions = options.Value;

        var now = clock.UtcNow;
        var expiresAt = now.AddMinutes(jwtOptions.AccessTokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.FullName),
            new Claim("username", user.Username),

            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("email_confirmed", user.EmailConfirmed.ToString().ToLowerInvariant()),
            new Claim("must_change_password", user.MustChangePassword.ToString().ToLowerInvariant()),
            new Claim("is_active", user.IsActive.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims = claims.Append(new Claim(ClaimTypes.Email, user.Email)).ToArray();
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(
            accessToken,
            "Bearer",
            (int)TimeSpan.FromMinutes(jwtOptions.AccessTokenLifetimeMinutes).TotalSeconds);
    }
}