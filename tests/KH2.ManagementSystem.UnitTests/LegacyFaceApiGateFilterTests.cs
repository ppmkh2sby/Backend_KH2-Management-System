using KH2.ManagementSystem.Api.Infrastructure;
using KH2.ManagementSystem.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class LegacyFaceApiGateFilterTests
{
    [Fact]
    public async Task DisabledGateReturnsGoneBeforeTheActionDelegateRuns()
    {
        var logger = new RecordingLogger();
        var filter = new LegacyFaceApiGateFilter(Options.Create(new LegacyFaceApiOptions { Enabled = false }), logger);
        var context = NewContext("GET", "/api/v1/face-enrollment/me");
        var invoked = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            invoked = true;
            throw new InvalidOperationException("Legacy action must not execute when disabled.");
        });

        Assert.False(invoked);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status410Gone, result.StatusCode);
        Assert.Equal("true", context.HttpContext.Response.Headers["Deprecation"].ToString());
        Assert.Contains("LegacyFaceApiUsage", logger.Messages);
    }

    [Fact]
    public async Task EnabledGateInvokesTheActionAndRecordsItsActualStatus()
    {
        var logger = new RecordingLogger();
        var filter = new LegacyFaceApiGateFilter(Options.Create(new LegacyFaceApiOptions { Enabled = true }), logger);
        var context = NewContext("POST", "/api/v1/face-attendance/sessions");
        var invoked = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            invoked = true;
            context.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });

        Assert.True(invoked);
        Assert.Equal("true", context.HttpContext.Response.Headers["Deprecation"].ToString());
        Assert.Contains("LegacyFaceApiUsage", logger.Messages);
        Assert.Contains("409", logger.Messages);
    }

    private static ActionExecutingContext NewContext(string method, string path)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.Request.Path = path;
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor { DisplayName = "Legacy route" });
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), new object());
    }

    private sealed class RecordingLogger : ILogger<LegacyFaceApiGateFilter>
    {
        public string Messages { get; private set; } = string.Empty;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages += $"{eventId.Name} {formatter(state, exception)}";
    }
}
