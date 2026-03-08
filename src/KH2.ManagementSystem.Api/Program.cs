using KH2.ManagementSystem.Api.Options;
using KH2.ManagementSystem.Application.Abstractions.Messaging;
using KH2.ManagementSystem.Application.Features.System.GetSystemOverview;
using KH2.ManagementSystem.BuildingBlocks.Results;
using KH2.ManagementSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddHealthChecks();
builder.Services.Configure<ApplicationMetadataOptions>(
    builder.Configuration.GetSection(ApplicationMetadataOptions.SectionName));

builder.Services.AddScoped<IQueryHandler<GetSystemOverviewQuery, Result<SystemOverviewDto>>, GetSystemOverviewQueryHandler>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
