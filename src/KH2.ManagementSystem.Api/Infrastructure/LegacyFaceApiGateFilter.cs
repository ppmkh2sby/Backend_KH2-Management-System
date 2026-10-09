using KH2.ManagementSystem.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Api.Infrastructure;

public sealed class LegacyFaceApiGateFilter(IOptions<LegacyFaceApiOptions> options, ILogger<LegacyFaceApiGateFilter> logger) : IAsyncActionFilter
{
    private static readonly Action<ILogger, string, string, int, Exception?> LogUsage = LoggerMessage.Define<string, string, int>(
        LogLevel.Information, new EventId(4201, "LegacyFaceApiUsage"),
        "Legacy face API access. RouteName={RouteName} Method={Method} StatusCode={StatusCode}");

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var route = context.ActionDescriptor.DisplayName ?? request.Path.Value ?? "legacy-face";
        if (!options.Value.Enabled)
        {
            LogUsage(logger, route, request.Method, StatusCodes.Status410Gone, null);
            context.HttpContext.Response.Headers.Append("Deprecation", "true");
            context.Result = new ObjectResult(new ProblemDetails { Status = StatusCodes.Status410Gone, Title = "Legacy face API is disabled.", Detail = "Use the canonical face recognition API." }) { StatusCode = StatusCodes.Status410Gone };
            return;
        }

        var executed = await next();
        LogUsage(logger, route, request.Method, executed.HttpContext.Response.StatusCode, null);
        executed.HttpContext.Response.Headers.Append("Deprecation", "true");
    }
}
