using System.Net;
using System.Text;
using System.Text.Json;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class FacePhaseThreeTests
{
    [Fact]
    public async Task HttpAnalysisAcceptsValidEmbeddingAndDoesNotCloseCallerStream()
    {
        using var handler = new ResponseHandler((request, _) =>
        {
            Assert.Equal("/v1/analyze", request.RequestUri!.AbsolutePath);
            Assert.Equal(new string('k', 32), request.Headers.GetValues("X-Face-Service-Key").Single());
            Assert.IsType<MultipartFormDataContent>(request.Content);
            return Task.FromResult(JsonResponse(new { isAccepted = true, faceCount = 1, embedding = Vector(), modelName = "arcface", modelVersion = "1" }));
        });
        using var client = new HttpClient(handler);
        var service = CreateService(client);
        using var stream = new MemoryStream([1, 2, 3]);
        var result = await service.AnalyzeImageAsync(new FaceImage("test.jpg", "image/jpeg", stream), CancellationToken.None);
        Assert.True(result.IsAccepted);
        Assert.Equal(512, result.Embedding!.Embedding.Count);
        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not JSON")]
    [InlineData("{\"isAccepted\":true,\"faceCount\":2,\"embedding\":[1],\"modelName\":\"arcface\",\"modelVersion\":\"1\"}")]
    [InlineData("{\"isAccepted\":false,\"faceCount\":0,\"reason\":\"private traceback\"}")]
    public async Task MalformedResponseNeverExposesInternalDetails(string payload)
    {
        using var handler = new ResponseHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(payload, Encoding.UTF8, "application/json") }));
        using var client = new HttpClient(handler);
        using var stream = new MemoryStream([1]);
        var result = await CreateService(client).AnalyzeImageAsync(new FaceImage("a.jpg", "image/jpeg", stream), CancellationToken.None);
        Assert.Equal(FaceServiceFailureReason.InvalidFaceServiceResponse, result.Reason);
        Assert.Null(result.Embedding);
    }

    [Fact]
    public async Task ResponseSizeIsBounded()
    {
        using var handler = new ResponseHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(new string('x', 65537)) }));
        using var client = new HttpClient(handler);
        var result = await CreateService(client).HealthCheckAsync(CancellationToken.None);
        Assert.Equal(FaceServiceFailureReason.InvalidFaceServiceResponse, result.Reason);
    }

    [Fact]
    public async Task TimeoutAndCallerCancellationAreDifferent()
    {
        using var handler = new ResponseHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return JsonResponse(new { isHealthy = true });
        });
        using var client = new HttpClient(handler);
        var service = CreateService(client);
        Assert.Equal(FaceServiceFailureReason.FaceServiceTimeout, (await service.HealthCheckAsync(CancellationToken.None)).Reason);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.HealthCheckAsync(cancellation.Token));
    }

    [Fact]
    public async Task HealthAndTransportFailuresAreMapped()
    {
        using var handler = new ResponseHandler((_, _) => Task.FromResult(JsonResponse(new { isHealthy = true })));
        using var client = new HttpClient(handler);
        Assert.True((await CreateService(client).HealthCheckAsync(CancellationToken.None)).IsHealthy);
        using var failedHandler = new ResponseHandler((_, _) => throw new HttpRequestException("private details"));
        using var failedClient = new HttpClient(failedHandler);
        Assert.Equal(FaceServiceFailureReason.FaceServiceUnavailable,
            (await CreateService(failedClient).HealthCheckAsync(CancellationToken.None)).Reason);
    }

    [Fact]
    public async Task EnrollmentSavesFiveEmbeddingsAndReturnsNoBiometrics()
    {
        var store = new TestStore();
        var handler = new EnrollFaceProfile(new TestService(), store, new TestClock(), 5);
        var result = await handler.HandleAsync(Guid.NewGuid(), Images(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(FaceProfileStatus.Active, store.Profile!.Status);
        Assert.Equal(store.SantriId, store.Profile.SantriId);
        Assert.Equal([1, 2, 3, 4, 5], store.Embeddings!.Select(x => x.CaptureIndex));
        Assert.All(store.Embeddings!, x => Assert.Equal(store.Enrollment!.Id, x.FaceEnrollmentId));
        Assert.DoesNotContain("embedding", JsonSerializer.Serialize(result.Value), StringComparison.OrdinalIgnoreCase);
        Assert.Null(store.Profile.ReferenceImagePath);
    }

    [Theory]
    [InlineData("samples")]
    [InlineData("owner")]
    [InlineData("duplicate")]
    [InlineData("service")]
    [InlineData("model")]
    public async Task EnrollmentFailuresDoNotWrite(string failure)
    {
        var store = new TestStore { MissingOwner = failure == "owner", SaveConflict = failure == "duplicate" };
        var service = new TestService { RejectAt = failure == "service" ? 3 : 0, ChangeModel = failure == "model" };
        var handler = new EnrollFaceProfile(service, store, new TestClock(), 5);
        var images = failure == "samples" ? Images().Take(4).ToArray() : Images();
        var result = await handler.HandleAsync(Guid.NewGuid(), images, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Null(store.Profile);
        Assert.Null(store.Embeddings);
    }

    [Fact]
    public async Task ConcurrentDuplicateIsReportedWithoutReplacingProfile()
    {
        var store = new TestStore { SaveConflict = true };
        var result = await new EnrollFaceProfile(new TestService(), store, new TestClock(), 5)
            .HandleAsync(Guid.NewGuid(), Images());
        Assert.Equal("AlreadyEnrolled", result.Error.Code);
        Assert.Null(store.Profile);
    }

    private static FaceImage[] Images() => Enumerable.Range(1, 5)
        .Select(_ => new FaceImage("image.jpg", "image/jpeg", Stream.Null)).ToArray();
    private static float[] Vector() { var values = new float[512]; values[0] = 1; return values; }
    private static HttpResponseMessage JsonResponse(object payload) => new(HttpStatusCode.OK)
        { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
    private static HttpFaceRecognitionService CreateService(HttpClient client) => new(client,
        Options.Create(new FaceRecognitionServiceOptions { BaseUrl = "http://face.internal/", ApiKey = new string('k', 32), TimeoutSeconds = 1 }),
        NullLogger<HttpFaceRecognitionService>.Instance);
    private sealed class ResponseHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response(request, cancellationToken);
    }
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
    private sealed class TestService : IFaceRecognitionService
    {
        private int calls;
        public int RejectAt { get; init; }
        public bool ChangeModel { get; init; }
        public Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken)
        {
            calls++;
            return Task.FromResult(calls == RejectAt
                ? FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.PoorImageQuality, new FaceDetectionResult(1))
                : FaceImageAnalysisResult.Accepted(new FaceDetectionResult(1), new FaceEmbeddingResult(Vector(), "arcface", ChangeModel ? calls.ToString(System.Globalization.CultureInfo.InvariantCulture) : "1")));
        }
        public Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken) => Task.FromResult(FaceServiceHealthResult.Healthy());
    }
    private sealed class TestStore : IFaceEnrollmentStore
    {
        public Guid SantriId { get; } = Guid.NewGuid();
        public bool MissingOwner { get; init; }
        public bool Existing { get; init; }
        public bool SaveConflict { get; init; }
        public FaceProfile? Profile { get; private set; }
        public FaceEnrollment? Enrollment { get; private set; }
        public IReadOnlyList<FaceEmbedding>? Embeddings { get; private set; }
        public Task<Guid?> FindSantriIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(MissingOwner ? null : SantriId);
        public Task<FaceProfile?> FindProfileAsync(Guid santriId, CancellationToken cancellationToken) => Task.FromResult<FaceProfile?>(Existing ? new FaceProfile(Guid.NewGuid(), santriId, DateTimeOffset.UtcNow) : null);
        public Task<bool> SaveAsync(FaceProfile profile, bool isNewProfile, FaceEnrollment enrollment, IReadOnlyList<FaceEmbedding> embeddings, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (SaveConflict) return Task.FromResult(false);
            enrollment.Activate(now);
            profile.Activate(enrollment.Id, now);
            Profile = profile;
            Enrollment = enrollment;
            Embeddings = embeddings;
            return Task.FromResult(true);
        }
    }
}
