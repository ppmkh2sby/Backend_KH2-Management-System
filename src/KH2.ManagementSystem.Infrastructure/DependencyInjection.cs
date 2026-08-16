using System.Text;
using System.Security.Claims;
using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Infrastructure.Persistence.Seed;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Infrastructure.Authentication;
using KH2.ManagementSystem.Infrastructure.Time;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Application.Abstractions.Dashboard;
using KH2.ManagementSystem.Infrastructure.Authorization;
using KH2.ManagementSystem.Infrastructure.Dashboard;
using KH2.ManagementSystem.Infrastructure.Persistence;
using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Infrastructure.Security;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using Microsoft.EntityFrameworkCore;
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

        services.AddOptions<FaceRecognitionOptions>()
            .Bind(configuration.GetSection(FaceRecognitionOptions.SectionName))
            .Validate(x => Uri.TryCreate(x.BaseUrl, UriKind.Absolute, out var uri) && !uri.IsLoopback, "FaceRecognition:BaseUrl must be a private service URI.")
            .Validate(x => x.ServiceApiKey.Trim().Length >= 32, "FaceRecognition:ServiceApiKey must be at least 32 characters.")
            .Validate(x => x.ConfidenceThreshold is > 0m and <= 1m, "FaceRecognition:ConfidenceThreshold must be between 0 and 1.")
            .Validate(x => x.TimeoutSeconds is > 0 and <= 60, "FaceRecognition:TimeoutSeconds must be between 1 and 60.")
            .ValidateOnStart();

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

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ISantriAccessReader, AppDbSantriAccessReader>();
        services.AddScoped<ISantriDashboardReader, SantriDashboardReader>();
        services.AddScoped<IAuthorizationHandler, CanAccessSantriHandler>();
        services.AddScoped<IAuthorizationHandler, CanOperateFaceAttendanceHandler>();
        services.AddScoped<IUserAuthenticator, CompositeUserAuthenticator>();

        services.AddScoped<IAccessTokenProvider, JwtTokenProvider>();
        services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<IEmailVerificationCodeService, EmailVerificationCodeService>();
        services.AddScoped<MasterAccountSeeder>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IFaceCaptureStorage, LocalPrivateFaceCaptureStorage>();
        services.AddHttpClient<IFaceRecognitionClient, HttpFaceRecognitionClient>((serviceProvider, client) =>
        {
            var faceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FaceRecognitionOptions>>().Value;
            client.BaseAddress = new Uri(faceOptions.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(faceOptions.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("X-Face-Service-Key", faceOptions.ServiceApiKey);
        });

        return services;
    }
}
