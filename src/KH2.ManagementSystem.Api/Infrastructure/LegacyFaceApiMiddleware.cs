using KH2.ManagementSystem.Api.Options;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Api.Infrastructure;

public sealed class LegacyFaceApiMiddleware(RequestDelegate next, IOptions<LegacyFaceApiOptions> options, ILogger<LegacyFaceApiMiddleware> logger)
{
    private static readonly Action<ILogger, string, string, int, Exception?> LogUsage = LoggerMessage.Define<string, string, int>(
        LogLevel.Information, new EventId(4201, "LegacyFaceApiUsage"),
        "Legacy face API access. RouteName={RouteName} Method={Method} StatusCode={StatusCode}");

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<LegacyFaceApiAttribute>() is null)
        {
            await next(context);
            return;
        }

        var route = endpoint.DisplayName ?? context.Request.Path.Value ?? "legacy-face";
        if (!options.Value.Enabled)
        {
            context.Response.StatusCode = StatusCodes.Status410Gone;
            context.Response.Headers.Append("Deprecation", "true");
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new { status = StatusCodes.Status410Gone, title = "Legacy face API is disabled.", detail = "Use the canonical face recognition API." });
            LogUsage(logger, route, context.Request.Method, StatusCodes.Status410Gone, null);
            return;
        }

        await next(context);
        LogUsage(logger, route, context.Request.Method, context.Response.StatusCode, null);
        context.Response.Headers.Append("Deprecation", "true");
    }
}
