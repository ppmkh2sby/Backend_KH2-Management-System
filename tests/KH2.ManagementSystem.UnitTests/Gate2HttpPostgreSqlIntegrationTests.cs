using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using KH2.ManagementSystem.Api;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Kafarahs;
using KH2.ManagementSystem.Domain.Kegiatans;
using KH2.ManagementSystem.Domain.LogKeluarMasuks;
using KH2.ManagementSystem.Domain.Presensis;
using KH2.ManagementSystem.Domain.ProgressKeilmuans;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Sesis;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Domain.Walis;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class Gate2HttpPostgreSqlIntegrationTests(PostgreSqlIntegrationFixture fixture)
{
    private const string Issuer = "https://gate2.testing";
    private const string Audience = "https://gate2-client.testing";
    private const string Secret = "gate2-testing-jwt-secret-with-at-least-32-characters";

    private static async Task AssertWaliAndSantriScopeAcrossAllReadFeatures(SeedData data, Gate2Factory host)
    {
        using var waliA = Client(host, data.WaliAUserId, UserRole.WaliSantri);
        using var waliB = Client(host, data.WaliBUserId, UserRole.WaliSantri);
        using var waliWithoutRelation = Client(host, data.WaliWithoutRelationUserId, UserRole.WaliSantri);
        using var santriA = Client(host, data.SantriAUserId, UserRole.Santri);
        using var admin = Client(host, data.AdminUserId, UserRole.Admin);
        using var pengurus = Client(host, data.PengurusUserId, UserRole.Pengurus);
        using var dewanGuru = Client(host, data.DewanGuruUserId, UserRole.DewanGuru);

        using var childrenResponse = await waliA.GetAsync("/api/v1/wali/me/children");
        Assert.True(childrenResponse.StatusCode == HttpStatusCode.OK, await childrenResponse.Content.ReadAsStringAsync());
        using var childrenDocument = JsonDocument.Parse(await childrenResponse.Content.ReadAsStringAsync());
        var childIds = childrenDocument.RootElement.GetProperty("items")
            .EnumerateArray().Select(item => item.GetProperty("santriId").GetGuid()).ToArray();
        Assert.Equal(new[] { data.SantriAId }, childIds);
        Assert.DoesNotContain(data.SantriCId, childIds);

        using var emptyChildren = await waliWithoutRelation.GetAsync("/api/v1/wali/me/children");
        Assert.Equal(HttpStatusCode.OK, emptyChildren.StatusCode);
        using var emptyDocument = JsonDocument.Parse(await emptyChildren.Content.ReadAsStringAsync());
        Assert.Empty(emptyDocument.RootElement.GetProperty("items").EnumerateArray());

        foreach (var route in ReadRoutes(data.SantriAId, data.SantriA.Nis))
        {
            Assert.Equal(HttpStatusCode.OK, (await waliA.GetAsync(route)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await waliB.GetAsync(route)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await waliWithoutRelation.GetAsync(route)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await santriA.GetAsync(route)).StatusCode);
        }

        foreach (var route in ReadRoutes(data.SantriCId, data.SantriC.Nis))
        {
            var waliStatus = (await waliA.GetAsync(route)).StatusCode;
            Assert.True(waliStatus == HttpStatusCode.Forbidden, $"Wali route {route} returned {waliStatus}.");
            using var santriResponse = await santriA.GetAsync(route);
            if (route.Contains("log-keluar-masuk", StringComparison.Ordinal) || route.Contains("progress-keilmuan", StringComparison.Ordinal))
            {
                Assert.Equal(HttpStatusCode.OK, santriResponse.StatusCode);
                Assert.DoesNotContain(data.SantriC.Nis, await santriResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Forbidden, santriResponse.StatusCode);
            }
        }

        foreach (var staff in new[] { admin, pengurus, dewanGuru })
        {
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/santri?perPage=10")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/api/v1/presensi?santriId={data.SantriCId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/api/v1/kafarah?santriId={data.SantriCId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/log-keluar-masuk?scope=team")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/progress-keilmuan?perPage=10")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await waliA.PostAsync("/api/v1/presensi", JsonContent($"{{\"santriId\":\"{data.SantriAId}\",\"tanggal\":\"2026-10-12\",\"status\":\"hadir\",\"waktu\":\"malam\"}}"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await waliA.PostAsync("/api/v1/kafarah", JsonContent($"{{\"santriId\":\"{data.SantriAId}\",\"tanggal\":\"2026-10-12\",\"jenisPelanggaran\":\"tidak_apel_malam\"}}"))).StatusCode);
    }

    [Fact]
    public async Task CanonicalFaceHttpFlowPersistsFiveEmbeddingsAndNeverWritesPresensiDuringSelfVerification()
    {
        var data = await SeedAsync();
        using var environment = new EnvironmentScope(fixture.ConnectionString);
        using var host = new Gate2Factory();
        await AssertWaliAndSantriScopeAcrossAllReadFeatures(data, host);
        await AssertCurrentSchemaRejectsAdditionalChildForSameWaliAsync(data);
        using var client = Client(host, data.SantriAUserId, UserRole.Santri);
        var faceService = host.Services.GetRequiredService<ControlledFaceService>();

        using var enrollment = Photos(1, 1, 1, 1, 1);
        using var enrolled = await client.PostAsync("/api/v1/face-profiles/me/enrollment", enrollment);
        Assert.True(enrolled.StatusCode == HttpStatusCode.Created, await enrolled.Content.ReadAsStringAsync());

        await using (var verify = await fixture.CreateContextAsync(false))
        {
            var profile = await verify.FaceProfiles.SingleAsync(profile => profile.SantriId == data.SantriAId);
            var active = await verify.FaceEnrollments.SingleAsync(enrollment =>
                enrollment.FaceProfileId == profile.Id && enrollment.Status == FaceEnrollmentStatus.Active);
            Assert.Equal(5, await verify.FaceEmbeddings.CountAsync(embedding => embedding.FaceEnrollmentId == active.Id));
        }

        using var enrollmentState = await client.GetAsync("/api/v1/face-profiles/me/enrollment");
        Assert.Equal(HttpStatusCode.OK, enrollmentState.StatusCode);
        using var stateDocument = JsonDocument.Parse(await enrollmentState.Content.ReadAsStringAsync());
        Assert.True(stateDocument.RootElement.GetProperty("isEnrolled").GetBoolean());
        Assert.Equal(5, stateDocument.RootElement.GetProperty("sampleCount").GetInt32());

        var presensiCount = await CountPresensiAsync();
        faceService.EmbeddingIndex = 1;
        Assert.True((await VerifyAsync(client, 1)).GetProperty("verified").GetBoolean());
        faceService.EmbeddingIndex = 2;
        var otherIdentity = await VerifyAsync(client, 2);
        Assert.False(otherIdentity.GetProperty("verified").GetBoolean());
        Assert.Equal("FaceDoesNotBelongToAccount", otherIdentity.GetProperty("reason").GetString());
        faceService.Mode = ControlledFaceServiceMode.Unknown;
        Assert.Equal(HttpStatusCode.BadRequest, (await VerifyResponseAsync(client, 3)).StatusCode);
        faceService.Mode = ControlledFaceServiceMode.Available;
        Assert.Equal(presensiCount, await CountPresensiAsync());

        using var invalid = new MultipartFormDataContent();
        invalid.Add(new ByteArrayContent([1]), "photo", "capture.txt");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/face-profiles/me/verify", invalid)).StatusCode);

        faceService.Mode = ControlledFaceServiceMode.Timeout;
        Assert.Equal(HttpStatusCode.GatewayTimeout, (await VerifyResponseAsync(client, 1)).StatusCode);
        faceService.Mode = ControlledFaceServiceMode.Unavailable;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await VerifyResponseAsync(client, 1)).StatusCode);
    }

    private async Task<SeedData> SeedAsync()
    {
        var data = SeedData.Create();
        await using var db = await fixture.CreateContextAsync();
        db.AddRange(
            new User(data.AdminUserId, "admin-gate2", "Admin", null, UserRole.Admin, "hash"),
            new User(data.PengurusUserId, "pengurus-gate2", "Pengurus", null, UserRole.Pengurus, "hash"),
            new User(data.DewanGuruUserId, "guru-gate2", "Dewan Guru", null, UserRole.DewanGuru, "hash"),
            new User(data.SantriAUserId, "santri-a-gate2", "Santri A", null, UserRole.Santri, "hash"),
            new User(data.SantriBUserId, "santri-b-gate2", "Santri B", null, UserRole.Santri, "hash"),
            new User(data.SantriCUserId, "santri-c-gate2", "Santri C", null, UserRole.Santri, "hash"),
            new User(data.WaliAUserId, "wali-a-gate2", "Wali A", null, UserRole.WaliSantri, "hash"),
            new User(data.WaliBUserId, "wali-b-gate2", "Wali B", null, UserRole.WaliSantri, "hash"),
            new User(data.WaliWithoutRelationUserId, "wali-empty-gate2", "Wali Empty", null, UserRole.WaliSantri, "hash"),
            data.SantriA, data.SantriB, data.SantriC,
            new WaliSantriRelation(Guid.NewGuid(), data.WaliAUserId, data.SantriAId, "Orang tua", "wali-a-gate2"),
            new WaliSantriRelation(Guid.NewGuid(), data.WaliBUserId, data.SantriCId, "Orang tua", "wali-b-gate2"),
            new WaliSantriRelation(Guid.NewGuid(), data.WaliAUserId, data.SantriCId, "Stale", "incorrect-code"));

        var kegiatan = new Kegiatan(Guid.NewGuid(), "asrama", "malam", "gate2");
        var sesi = new Sesi(Guid.NewGuid(), kegiatan.Id, new DateOnly(2026, 10, 12), "Gate 2");
        db.AddRange(kegiatan, sesi,
            new Presensi(Guid.NewGuid(), data.SantriAId, data.SantriA.FullName, "hadir", kegiatan.Id, sesi.Id, null, "malam"),
            new Presensi(Guid.NewGuid(), data.SantriBId, data.SantriB.FullName, "izin", kegiatan.Id, sesi.Id, null, "malam"),
            new Presensi(Guid.NewGuid(), data.SantriCId, data.SantriC.FullName, "alpa", kegiatan.Id, sesi.Id, null, "malam"),
            new Kafarah(Guid.NewGuid(), data.SantriAId, new DateOnly(2026, 10, 12), "tidak_apel_malam", "Istigfar", 0, 250),
            new Kafarah(Guid.NewGuid(), data.SantriCId, new DateOnly(2026, 10, 12), "tidak_apel_malam", "Istigfar", 0, 250),
            new LogKeluarMasuk(Guid.NewGuid(), data.SantriAId, new DateOnly(2026, 10, 12), "izin", "08:00-09:00", LogKeluarMasuk.StatusRecorded, "gate2"),
            new LogKeluarMasuk(Guid.NewGuid(), data.SantriCId, new DateOnly(2026, 10, 12), "izin", "08:00-09:00", LogKeluarMasuk.StatusRecorded, "gate2"),
            new ProgressKeilmuan(Guid.NewGuid(), data.SantriAId, "Juz 1", 20, 10, "ayat", ProgressKeilmuan.LevelQuran, null, "gate2", DateTimeOffset.UtcNow),
            new ProgressKeilmuan(Guid.NewGuid(), data.SantriCId, "Juz 1", 20, 5, "ayat", ProgressKeilmuan.LevelQuran, null, "gate2", DateTimeOffset.UtcNow));

        var profileB = new FaceProfile(Guid.NewGuid(), data.SantriBId, DateTimeOffset.UtcNow);
        var enrollmentB = new FaceEnrollment(Guid.NewGuid(), profileB.Id, ControlledFaceService.ModelName, ControlledFaceService.ModelVersion, DateTimeOffset.UtcNow);
        enrollmentB.Activate(DateTimeOffset.UtcNow);
        profileB.ActivateProfile(DateTimeOffset.UtcNow);
        db.AddRange(profileB, enrollmentB, new FaceEmbedding(Guid.NewGuid(), enrollmentB.Id, Vector(2), 1, 0.9f));
        await db.SaveChangesAsync();
        return data;
    }

    private async Task AssertCurrentSchemaRejectsAdditionalChildForSameWaliAsync(SeedData data)
    {
        await using var context = await fixture.CreateContextAsync(false);
        context.WaliSantriRelations.Add(new WaliSantriRelation(
            Guid.NewGuid(), data.WaliAUserId, data.SantriBId, "Orang Tua", "wali-a-gate2"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Contains("IX_WaliSantriRelations_WaliSantriCode", exception.ToString(), StringComparison.Ordinal);
    }

    private static IEnumerable<string> ReadRoutes(Guid santriId, string santriCode) =>
    [
        $"/api/v1/presensi?santriId={santriId}",
        $"/api/v1/kafarah?santriId={santriId}",
        $"/api/v1/log-keluar-masuk?santriId={santriId}",
        $"/api/v1/progress-keilmuan?santriCode={santriCode}"
    ];

    private static HttpClient Client(WebApplicationFactory<Program> host, Guid userId, UserRole role)
    {
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(userId, role));
        return client;
    }

    private static string Token(Guid userId, UserRole role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        Issuer, Audience,
        [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role.ToString())],
        expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256)));

    private async Task<int> CountPresensiAsync()
    {
        await using var db = await fixture.CreateContextAsync(false);
        return await db.Presensis.CountAsync();
    }

    private static MultipartFormDataContent Photos(params byte[] values)
    {
        var form = new MultipartFormDataContent();
        foreach (var value in values)
        {
            var image = new ByteArrayContent([value]);
            image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            form.Add(image, "photos", "capture.png");
        }
        return form;
    }

    private static async Task<JsonElement> VerifyAsync(HttpClient client, byte value)
    {
        using var response = await VerifyResponseAsync(client, value);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private static Task<HttpResponseMessage> VerifyResponseAsync(HttpClient client, byte value)
    {
        var form = new MultipartFormDataContent();
        var image = new ByteArrayContent([value]);
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(image, "photo", "capture.png");
        return client.PostAsync("/api/v1/face-profiles/me/verify", form);
    }

    private static StringContent JsonContent(string value) => new(value, Encoding.UTF8, "application/json");

    private static float[] Vector(int index)
    {
        var vector = new float[512];
        vector[index] = 1;
        return vector;
    }

    private sealed class Gate2Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing")
            .ConfigureTestServices(services =>
        {
            services.RemoveAll<IFaceRecognitionService>();
            services.AddSingleton<ControlledFaceService>();
            services.AddSingleton<IFaceRecognitionService>(provider => provider.GetRequiredService<ControlledFaceService>());
        });
    }

    private enum ControlledFaceServiceMode { Available, Timeout, Unavailable, Unknown }

    private sealed class ControlledFaceService : IFaceRecognitionService
    {
        public const string ModelName = "gate2-stub-model";
        public const string ModelVersion = "v1";
        public ControlledFaceServiceMode Mode { get; set; }
        public int EmbeddingIndex { get; set; } = 1;

        public async Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken)
        {
            if (Mode == ControlledFaceServiceMode.Timeout)
                return FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.FaceServiceTimeout);
            if (Mode == ControlledFaceServiceMode.Unavailable)
                return FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.FaceServiceUnavailable);
            await Task.CompletedTask;
            return Mode == ControlledFaceServiceMode.Unknown
                ? FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.NoFaceDetected, new FaceDetectionResult(0))
                : Accepted(EmbeddingIndex);
        }

        public Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken) =>
            Task.FromResult(FaceServiceHealthResult.Healthy());

        private static FaceImageAnalysisResult Accepted(int index) => FaceImageAnalysisResult.Accepted(
            new FaceDetectionResult(1, 0.9f), new FaceEmbeddingResult(Vector(index), ModelName, ModelVersion, 0.9f));
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> previous = [];
        public EnvironmentScope(string connectionString)
        {
            Set("ASPNETCORE_ENVIRONMENT", "Testing");
            Set("ConnectionStrings__DefaultConnection", connectionString);
            Set("Database__MigrateOnStartup", "false");
            Set("Database__SeedOnStartup", "false");
            Set("LegacyFaceApi__Enabled", "false");
            Set("FaceRecognition__BaseUrl", "http://face.test/");
            Set("FaceRecognition__ApiKey", "gate2-face-service-key-with-at-least-32-characters");
            Set("FaceRecognition__TimeoutSeconds", "10");
            Set("FaceRecognition__SimilarityThreshold", "0.5");
            Set("FaceRecognition__ExpectedEmbeddingDimension", "512");
            Set("FaceRecognition__RequiredEnrollmentSamples", "5");
            Set("Jwt__Issuer", Issuer);
            Set("Jwt__Audience", Audience);
            Set("Jwt__SecretKey", Secret);
        }

        private void Set(string key, string value)
        {
            previous[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        public void Dispose()
        {
            foreach (var (key, value) in previous)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private sealed record SeedData(
        Guid AdminUserId, Guid PengurusUserId, Guid DewanGuruUserId,
        Guid SantriAUserId, Guid SantriBUserId, Guid SantriCUserId,
        Guid WaliAUserId, Guid WaliBUserId, Guid WaliWithoutRelationUserId,
        Guid SantriAId, Guid SantriBId, Guid SantriCId,
        Santri SantriA, Santri SantriB, Santri SantriC)
    {
        public static SeedData Create()
        {
            var santriAUserId = Guid.NewGuid();
            var santriBUserId = Guid.NewGuid();
            var santriCUserId = Guid.NewGuid();
            var santriAId = Guid.NewGuid();
            var santriBId = Guid.NewGuid();
            var santriCId = Guid.NewGuid();
            return new SeedData(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), santriAUserId, santriBUserId, santriCUserId,
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), santriAId, santriBId, santriCId,
                new Santri(santriAId, santriAUserId, "Santri A", "NIS-G2-A", "A", "MIPA", "putra", "KTB", "X"),
                new Santri(santriBId, santriBUserId, "Santri B", "NIS-G2-B", "A", "MIPA", "putra", "Umum", "X"),
                new Santri(santriCId, santriCUserId, "Santri C", "NIS-G2-C", "A", "MIPA", "putri", "Umum", "X"));
        }
    }
}
