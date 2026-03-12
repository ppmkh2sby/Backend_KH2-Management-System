using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Infrastructure.Authentication;
using KH2.ManagementSystem.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}