using KH2.ManagementSystem.Api.Options;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.Application.Features.System.GetSystemOverview;
using KH2.ManagementSystem.BuildingBlocks.Results;
using KH2.ManagementSystem.Infrastructure;
using KH2.ManagementSystem.Infrastructure.Authorization;
using KH2.ManagementSystem.Application.Abstractions.Authorization;
using KH2.ManagementSystem.Domain.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddHealthChecks();

builder.Services.Configure<ApplicationMetadataOptions>(
    builder.Configuration.GetSection(ApplicationMetadataOptions.SectionName));

builder.Services.AddScoped<IQueryHandler<GetSystemOverviewQuery, Result<SystemOverviewDto>>, GetSystemOverviewQueryHandler>();

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

app.UseExceptionHandler();
app.UseHttpsRedirection();

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

public partial class Program
{
    internal static readonly string[] RootEndpoints =
    [
        "/health",
        "/api/v1/system/info",
        "/api/v1/auth/login",
        "/api/v1/auth/me"
    ];
}
