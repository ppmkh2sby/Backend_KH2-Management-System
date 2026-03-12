using System.Text;
using System.Security.Claims;
using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Infrastructure.Authentication;
using KH2.ManagementSystem.Infrastructure.Time;
using KH2.ManagementSystem.Application.Abstractions.Authorization;

using KH2.ManagementSystem.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace KH2.ManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                $"{JwtOptions.SectionName}:Issuer is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                $"{JwtOptions.SectionName}:Audience is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SecretKey),
                $"{JwtOptions.SectionName}:SecretKey is required.")
            .Validate(
                options => options.SecretKey.Trim().Length >= 32,
                $"{JwtOptions.SectionName}:SecretKey must be at least 32 characters.")
            .Validate(
                options => options.AccessTokenLifetimeMinutes > 0,
                $"{JwtOptions.SectionName}:AccessTokenLifetimeMinutes must be greater than 0.")
            .Validate(
                options => options.RefreshTokenLifetimeDays > 0,
                $"{JwtOptions.SectionName}:RefreshTokenLifetimeDays must be greater than 0.")
            .ValidateOnStart();

        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        services.AddOptions<DevelopmentAuthOptions>()
            .Bind(configuration.GetSection(DevelopmentAuthOptions.SectionName));

        services.AddOptions<DevelopmentAuthorizationOptions>()
            .Bind(configuration.GetSection(DevelopmentAuthorizationOptions.SectionName));

        services.AddSingleton<IAuthorizationHandler, CanAccessSantriHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddScoped<IUserAuthenticator, DevelopmentUserAuthenticator>();
        services.AddScoped<IAccessTokenProvider, JwtTokenProvider>();
        services.AddScoped<ISantriAccessReader, DevelopmentSantriAccessReader>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
