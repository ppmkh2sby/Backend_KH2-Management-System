using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using KH2.ManagementSystem.Api;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection("LegacyFaceApi host")]
public sealed class LegacyFaceApiHostStartupTests
{
    private static readonly Guid SessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void DisabledLegacyApiStartsWithoutLegacyProviderAndDoesNotRegisterLegacyServices()
    {
        using var environment = new TestEnvironment("false");
        using var factory = new LegacyFaceApiFactory();
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IFaceRecognitionService>());
        Assert.NotNull(scope.ServiceProvider.GetService<IFaceEnrollmentStore>());
        Assert.NotNull(scope.ServiceProvider.GetService<IFaceMatchReader>());
        Assert.Null(scope.ServiceProvider.GetService<IFaceRecognitionClient>());
        Assert.Null(scope.ServiceProvider.GetService<IFaceCaptureStorage>());
    }

    [Fact]
    public async Task AuthenticationAndAuthorizationRunBeforeTheDisabledLegacyGate()
    {
        using var environment = new TestEnvironment("false");
        using var factory = new LegacyFaceApiFactory();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/face-enrollment/me")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("WaliSantri"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/face-enrollment/me")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Santri"));
        await AssertLegacyGoneAsync(client, new HttpRequestMessage(HttpMethod.Get, "/api/v1/face-enrollment/me"));
    }

    [Fact]
    public async Task DisabledLegacyApiGatesEveryEnrollmentRouteBeforeModelBindingOrControllerActivation()
    {
        using var environment = new TestEnvironment("false");
        using var factory = new LegacyFaceApiFactory();
        using var client = CreateAuthorizedClient(factory, "Santri");

        foreach (var request in new[]
        {
            new HttpRequestMessage(HttpMethod.Get, "/api/v1/face-enrollment/me"),
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/face-enrollment/me/captures"),
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/face-enrollment/me/complete"),
            new HttpRequestMessage(HttpMethod.Delete, "/api/v1/face-enrollment/me/captures/1"),
            new HttpRequestMessage(HttpMethod.Delete, "/api/v1/face-enrollment/me")
        })
        {
            await AssertLegacyGoneAsync(client, request);
        }
    }

    [Fact]
    public async Task DisabledLegacyApiGatesEveryAttendanceRouteBeforeControllerActivation()
    {
        using var environment = new TestEnvironment("false");
        using var factory = new LegacyFaceApiFactory();
        using var admin = CreateAuthorizedClient(factory, "Admin");
        using var santri = CreateAuthorizedClient(factory, "Santri");

        foreach (var request in new[]
        {
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/face-attendance/sessions"),
            new HttpRequestMessage(HttpMethod.Post, $"/api/v1/face-attendance/sessions/{SessionId}/verify-opener"),
            new HttpRequestMessage(HttpMethod.Post, $"/api/v1/face-attendance/sessions/{SessionId}/close"),
            new HttpRequestMessage(HttpMethod.Get, "/api/v1/face-attendance/sessions"),
            new HttpRequestMessage(HttpMethod.Get, $"/api/v1/face-attendance/sessions/{SessionId}"),
            new HttpRequestMessage(HttpMethod.Get, $"/api/v1/face-attendance/sessions/{SessionId}/records")
        })
        {
            await AssertLegacyGoneAsync(admin, request);
        }

        await AssertLegacyGoneAsync(santri, new HttpRequestMessage(HttpMethod.Post, $"/api/v1/face-attendance/sessions/{SessionId}/check-in"));
        await AssertLegacyGoneAsync(santri, new HttpRequestMessage(HttpMethod.Get, "/api/v1/face-attendance/sessions/active"));
    }

    [Fact]
    public async Task DisabledLegacyApiLogsExactlyOneSanitizedUsageEvent()
    {
        using var environment = new TestEnvironment("false");
        var logs = new RecordingLoggerProvider();
        using var factory = new LegacyFaceApiFactory(logs);
        using var client = CreateAuthorizedClient(factory, "Santri");

        await AssertLegacyGoneAsync(client, new HttpRequestMessage(HttpMethod.Get, "/api/v1/face-enrollment/me"));

        var entries = logs.Entries.Where(entry => entry.EventId == 4201 && entry.Category == "KH2.ManagementSystem.Api.Infrastructure.LegacyFaceApiMiddleware").ToArray();
        var entry = Assert.Single(entries);
        Assert.Contains("StatusCode=410", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiKey", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("embedding", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CanonicalRoutesAreNotGatedWhenLegacyApiIsDisabled()
    {
        using var environment = new TestEnvironment("false");
        using var factory = new LegacyFaceApiFactory();
        using var client = factory.CreateClient();

        foreach (var route in new[]
        {
            "/api/v1/face-profiles/me/enrollment",
            "/api/v1/face-recognition",
            "/api/attendance/face-recognition"
        })
        {
            var response = await client.PostAsync(route, content: null);
            Assert.NotEqual(HttpStatusCode.Gone, response.StatusCode);
        }
    }

    [Fact]
    public async Task EnabledLegacyApiStartsRegistersLegacyServicesAndPassesRequestsThrough()
    {
        using var environment = new TestEnvironment("true", includeLegacyProvider: true);
        using var factory = new LegacyFaceApiFactory();
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IFaceRecognitionClient>());
        Assert.NotNull(scope.ServiceProvider.GetService<IFaceCaptureStorage>());

        using var client = CreateAuthorizedClient(factory, "Santri");
        using var response = await client.GetAsync("/api/v1/face-enrollment/me");
        Assert.NotEqual(HttpStatusCode.Gone, response.StatusCode);
    }

    [Fact]
    public void EnabledLegacyApiWithoutProviderConfigurationFailsStartupValidation()
    {
        using var environment = new TestEnvironment("true");
        using var factory = new LegacyFaceApiFactory();

        Assert.Throws<OptionsValidationException>(() => _ = factory.Services);
    }

    private static HttpClient CreateAuthorizedClient(WebApplicationFactory<Program> factory, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return client;
    }

    private static async Task AssertLegacyGoneAsync(HttpClient client, HttpRequestMessage request)
    {
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Equal("true", response.Headers.GetValues("Deprecation").Single());
    }

    private static string CreateToken(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestEnvironment.JwtSecret));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: TestEnvironment.JwtIssuer,
            audience: TestEnvironment.JwtAudience,
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, role)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
    }

    private sealed class LegacyFaceApiFactory(RecordingLoggerProvider? logs = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (logs is not null)
            {
                builder.ConfigureLogging(logging => logging.AddProvider(logs));
            }
        }
    }

    private sealed class TestEnvironment : IDisposable
    {
        public const string JwtIssuer = "https://testing.example";
        public const string JwtAudience = "https://testing-client.example";
        public const string JwtSecret = "testing-jwt-secret-with-at-least-32-characters";
        private readonly Dictionary<string, string?> previous = [];

        public TestEnvironment(string legacyEnabled, bool includeLegacyProvider = false)
        {
            Set("ASPNETCORE_ENVIRONMENT", "Testing");
            Set("LegacyFaceApi__Enabled", legacyEnabled);
            Set("Database__MigrateOnStartup", "false");
            Set("Database__SeedOnStartup", "false");
            Set("ConnectionStrings__DefaultConnection", "Host=127.0.0.1;Port=5432;Database=kh2_test;Username=test;Password=test");
            Set("Jwt__Issuer", JwtIssuer); Set("Jwt__Audience", JwtAudience); Set("Jwt__SecretKey", JwtSecret);
            Set("FaceRecognition__BaseUrl", "http://face-recognition.test/");
            Set("FaceRecognition__ApiKey", "testing-canonical-face-key-with-at-least-32-chars");
            Set("FaceRecognition__TimeoutSeconds", "15"); Set("FaceRecognition__ExpectedEmbeddingDimension", "512"); Set("FaceRecognition__RequiredEnrollmentSamples", "5");

            if (includeLegacyProvider)
            {
                Set("LegacyFaceProvider__BaseUrl", "http://legacy-face-provider.test/");
                Set("LegacyFaceProvider__ApiKey", "testing-legacy-face-key-with-at-least-32-chars");
                Set("LegacyFaceProvider__ConfidenceThreshold", "0.60");
                Set("LegacyFaceProvider__TimeoutSeconds", "15");
                Set("LegacyFaceProvider__CaptureStoragePath", "App_Data/test-private-face-captures");
            }
        }

        private void Set(string key, string value)
        {
            previous[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        public void Dispose()
        {
            foreach (var (key, value) in previous)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Entries);
        public void Dispose() { }
    }

    private sealed class RecordingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new LogEntry(category, eventId.Id, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(string Category, int EventId, string Message);
}

[CollectionDefinition("LegacyFaceApi host", DisableParallelization = true)]
public sealed class LegacyFaceApiHostTestGroup;
