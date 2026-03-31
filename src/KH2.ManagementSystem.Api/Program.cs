using System.Net;
using KH2.ManagementSystem.Api.Options;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriAttendance;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriDashboard;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriLog;
using KH2.ManagementSystem.Application.Features.Dashboard.GetMySantriProgress;
using KH2.ManagementSystem.Application.Features.System.GetSystemOverview;
using KH2.ManagementSystem.Infrastructure.Persistence.Seed;
using KH2.ManagementSystem.BuildingBlocks.Results;
using KH2.ManagementSystem.Infrastructure;
using KH2.ManagementSystem.Infrastructure.Authorization;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddHealthChecks();
ConfigureForwardedHeaders(builder.Services, builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .SetIsOriginAllowed(origin =>
                {
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    {
                        return false;
                    }

                    return uri.Host is "localhost" or "127.0.0.1" or "::1";
                })
                .AllowAnyHeader()
                .AllowAnyMethod();

            return;
        }

        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<ApplicationMetadataOptions>(
    builder.Configuration.GetSection(ApplicationMetadataOptions.SectionName));

builder.Services.AddScoped<IQueryHandler<GetSystemOverviewQuery, Result<SystemOverviewDto>>, GetSystemOverviewQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMySantriDashboardQuery, Result<SantriDashboardDto>>, GetMySantriDashboardQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMySantriAttendanceQuery, Result<SantriDashboardAttendancePageDto>>, GetMySantriAttendanceQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMySantriProgressQuery, Result<SantriDashboardProgressPageDto>>, GetMySantriProgressQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMySantriLogQuery, Result<SantriDashboardLogPageDto>>, GetMySantriLogQueryHandler>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(UserRole.Admin.ToString()));

    options.AddPolicy(AuthorizationPolicies.InternalManagement, policy =>
        policy.RequireRole(
            UserRole.Admin.ToString(),
            UserRole.DewanGuru.ToString(),
            UserRole.Pengurus.ToString()));

    options.AddPolicy(AuthorizationPolicies.CanApprove, policy =>
        policy.RequireRole(
            UserRole.Admin.ToString(),
            UserRole.DewanGuru.ToString(),
            UserRole.Pengurus.ToString()));

    options.AddPolicy(AuthorizationPolicies.CanReadAllSantri, policy =>
        policy.RequireRole(
            UserRole.Admin.ToString(),
            UserRole.DewanGuru.ToString(),
            UserRole.Pengurus.ToString()));

    options.AddPolicy(AuthorizationPolicies.CanAccessSantri, policy =>
        policy.Requirements.Add(new CanAccessSantriRequirement()));
});

var app = builder.Build();

await InitializeDatabaseAsync(app);

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    status = "running",
    application = "KH2 Management System API",
    endpoints = Program.RootEndpoints
}));

app.MapGet("/scalar", () => Results.Redirect("/"));
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var migrateOnStartup = app.Configuration.GetValue<bool?>("Database:MigrateOnStartup") ?? true;
    if (migrateOnStartup)
    {
        await dbContext.Database.MigrateAsync();
    }

    var seedOnStartup = app.Configuration.GetValue<bool?>("Database:SeedOnStartup")
        ?? app.Environment.IsDevelopment();
    if (!seedOnStartup)
    {
        return;
    }

    var seedSampleDataOnStartup = app.Configuration.GetValue<bool?>("Database:SeedSampleDataOnStartup") ?? false;
    var seeder = scope.ServiceProvider.GetRequiredService<MasterAccountSeeder>();
    await seeder.SeedAsync(seedSampleDataOnStartup);
}

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    return configuration.GetSection("Cors:AllowedOrigins")
        .Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(static origin => origin.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray()
        ?? [];
}

static void ConfigureForwardedHeaders(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;

        var forwardLimit = configuration.GetValue<int?>("ReverseProxy:ForwardLimit") switch
        {
            null => 1,
            > 0 => configuration.GetValue<int>("ReverseProxy:ForwardLimit"),
            _ => throw new InvalidOperationException("ReverseProxy:ForwardLimit must be greater than 0.")
        };
        options.ForwardLimit = forwardLimit;

        foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        {
            if (!IPAddress.TryParse(proxy, out var ipAddress))
            {
                throw new InvalidOperationException($"ReverseProxy:KnownProxies contains an invalid IP address: '{proxy}'.");
            }

            options.KnownProxies.Add(ipAddress);
        }

        foreach (var network in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
        {
            if (!TryParseCidr(network, out var ipNetwork))
            {
                throw new InvalidOperationException($"ReverseProxy:KnownNetworks contains an invalid CIDR value: '{network}'.");
            }

            options.KnownIPNetworks.Add(ipNetwork);
        }

        foreach (var host in GetAllowedForwardedHosts(configuration))
        {
            options.AllowedHosts.Add(host);
        }
    });
}

static string[] GetAllowedForwardedHosts(IConfiguration configuration)
{
    var allowedHosts = configuration["AllowedHosts"]?
        .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        ?? [];

    return allowedHosts.Length == 1 && allowedHosts[0] == "*"
        ? []
        : allowedHosts;
}

static bool TryParseCidr(string value, out System.Net.IPNetwork network)
{
    network = default;

    if (string.IsNullOrWhiteSpace(value))
    {
        return false;
    }

    var parts = value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var prefix) || !int.TryParse(parts[1], out var prefixLength))
    {
        return false;
    }

    var maxPrefixLength = prefix.AddressFamily switch
    {
        System.Net.Sockets.AddressFamily.InterNetwork => 32,
        System.Net.Sockets.AddressFamily.InterNetworkV6 => 128,
        _ => 0
    };

    if (maxPrefixLength == 0 || prefixLength < 0 || prefixLength > maxPrefixLength)
    {
        return false;
    }

    network = new System.Net.IPNetwork(prefix, prefixLength);
    return true;
}

public partial class Program
{
    internal static readonly string[] RootEndpoints =
    [
        "/health",
        "/api/v1/system/info",
        "/api/v1/presensi",
        "/api/v1/kehadiran",
        "/api/v1/presensi/rekap",
        "/api/v1/presensi/bulk",
        "/api/v1/ketertiban/presensi",
        "/api/v1/kafarah",
        "/api/v1/kafarah/bulk",
        "/api/v1/progress-keilmuan",
        "/api/v1/progress-keilmuan/staff",
        "/api/v1/progress-keilmuan/{santriCode}/detail",
        "/api/v1/progress-keilmuan/sync",
        "/api/v1/log-keluar-masuk",
        "/api/v1/santri",
        "/api/v1/dashboard/santri/me",
        "/api/v1/dashboard/santri/me/presensi",
        "/api/v1/dashboard/santri/me/progres-keilmuan",
        "/api/v1/dashboard/santri/me/log-keluar-masuk",
        "/api/v1/auth/login",
        "/api/v1/auth/me",
        "/api/v1/auth/change-password",
        "/api/v1/auth/set-email",
        "/api/v1/auth/verify-email",
        "/api/v1/auth/logout"
    ];
}
