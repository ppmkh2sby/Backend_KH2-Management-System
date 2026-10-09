using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using KH2.ManagementSystem.Api;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class CanonicalFaceHostE2ETests(PostgreSqlIntegrationFixture fixture)
{
    private const string Key = "kh2-local-face-service-test-key-2026";
    private const string Issuer = "https://testing.example";
    private const string Audience = "https://testing-client.example";
    private const string Secret = "testing-jwt-secret-with-at-least-32-characters";

    [Fact]
    public async Task HostEnrollmentPersistsFiveRealEmbeddingsRollsBackInvalidCaptureAndMatchesProbe()
    {
        var fixturePath = Environment.GetEnvironmentVariable("KH2_FACE_E2E_FIXTURES_DIR");
        if (string.IsNullOrWhiteSpace(fixturePath)) return;
        var image = Directory.EnumerateFiles(fixturePath, "*.*").FirstOrDefault(path => Path.GetExtension(path) is ".png" or ".jpg" or ".jpeg");
        if (image is null) return;

        var userId = Guid.NewGuid();
        var santriId = Guid.NewGuid();
        await using (var setup = await fixture.CreateContextAsync())
        {
            setup.Add(new User(userId, $"e2e-{userId:N}", "E2E", null, UserRole.Santri, "test-hash"));
            setup.Add(new Santri(santriId, userId, "E2E", userId.ToString("N")[..12], "A", "B", "M", "A", "A"));
            await setup.SaveChangesAsync();
        }

        using var environment = new EnvironmentScope(fixture.ConnectionString);
        using var host = new Factory();
        using var anonymous = host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/v1/face-profiles/me/enrollment", null)).StatusCode);
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(userId, UserRole.Santri));

        using var success = Photos(Enumerable.Repeat(image, 5));
        using var response = await client.PostAsync("/api/v1/face-profiles/me/enrollment", success);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Guid activeId;
        await using (var verify = await fixture.CreateContextAsync(false))
        {
            var profile = await verify.FaceProfiles.SingleAsync(value => value.SantriId == santriId);
            Assert.Equal(FaceProfileStatus.Active, profile.Status);
            var enrollment = await verify.FaceEnrollments.SingleAsync(value => value.FaceProfileId == profile.Id && value.Status == FaceEnrollmentStatus.Active);
            activeId = enrollment.Id;
            Assert.Equal("arcface-insightface", enrollment.ModelName); Assert.Equal("buffalo_l-v1", enrollment.ModelVersion);
            var embeddings = await verify.FaceEmbeddings.Where(value => value.FaceEnrollmentId == activeId).OrderBy(value => value.CaptureIndex).ToArrayAsync();
            Assert.Equal([1,2,3,4,5], embeddings.Select(value => value.CaptureIndex));
            Assert.All(embeddings, value => Assert.InRange(value.QualityScore ?? 0f, 0f, 1f));
        }

        using var invalid = Photos([image, image, image, image, "invalid"]);
        using var invalidResponse = await client.PostAsync("/api/v1/face-profiles/me/enrollment", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        await using (var rollback = await fixture.CreateContextAsync(false))
        {
            Assert.Equal(1, await rollback.FaceEnrollments.CountAsync());
            Assert.Equal(activeId, (await rollback.FaceEnrollments.SingleAsync()).Id);
            Assert.Equal(5, await rollback.FaceEmbeddings.CountAsync());
        }

        await using var matchContext = await fixture.CreateContextAsync(false);
        using var probeClient = new HttpClient();
        await using var probeStream = File.OpenRead(image);
        var service = new HttpFaceRecognitionService(probeClient, Options.Create(new FaceRecognitionServiceOptions { BaseUrl = "http://127.0.0.1:8000", ApiKey = Key, TimeoutSeconds = 30 }), Microsoft.Extensions.Logging.Abstractions.NullLogger<HttpFaceRecognitionService>.Instance);
        var probe = await service.AnalyzeImageAsync(new FaceImage("probe.png", "image/png", probeStream), CancellationToken.None);
        Assert.True(probe.IsAccepted);
        var candidate = Assert.Single(await new FaceMatchReader(matchContext).FindNearestAsync(probe.Embedding!, CancellationToken.None));
        Assert.Equal(santriId, candidate.SantriId);
    }

    private static MultipartFormDataContent Photos(IEnumerable<string> paths)
    {
        var form = new MultipartFormDataContent();
        foreach (var path in paths)
        {
            var bytes = path == "invalid" ? [1, 2, 3] : File.ReadAllBytes(path);
            var content = new ByteArrayContent(bytes); content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png"); form.Add(content, "photos", "capture.png");
        }
        return form;
    }
    private static string Token(Guid userId, UserRole role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(Issuer, Audience, [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role.ToString())], expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256)));
    private sealed class Factory : WebApplicationFactory<Program> { protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing"); }
    private sealed class EnvironmentScope : IDisposable { private readonly Dictionary<string,string?> old = []; public EnvironmentScope(string connection) { Set("ConnectionStrings__DefaultConnection", connection); Set("Database__MigrateOnStartup", "false"); Set("Database__SeedOnStartup", "false"); Set("LegacyFaceApi__Enabled", "false"); Set("FaceRecognition__BaseUrl", "http://127.0.0.1:8000"); Set("FaceRecognition__ApiKey", Key); Set("FaceRecognition__TimeoutSeconds", "30"); Set("FaceRecognition__ExpectedEmbeddingDimension", "512"); Set("FaceRecognition__RequiredEnrollmentSamples", "5"); Set("Jwt__Issuer", Issuer); Set("Jwt__Audience", Audience); Set("Jwt__SecretKey", Secret); } private void Set(string key,string value) { old[key]=Environment.GetEnvironmentVariable(key); Environment.SetEnvironmentVariable(key,value); } public void Dispose(){foreach(var item in old) Environment.SetEnvironmentVariable(item.Key,item.Value);} }
}
