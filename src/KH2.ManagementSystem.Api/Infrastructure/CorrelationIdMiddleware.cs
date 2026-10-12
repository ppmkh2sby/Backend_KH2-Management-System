using System.Diagnostics;

namespace KH2.ManagementSystem.Api.Infrastructure;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault())
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string? GetCorrelationId(string? value) =>
        value is { Length: > 0 and <= 128 } && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            ? value
            : null;
}
