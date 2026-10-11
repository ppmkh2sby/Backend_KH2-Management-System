using System.Text.Json;
using KH2.ManagementSystem.Api.Controllers;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class VerifySantriFaceTests
{
    [Fact]
    public async Task OwnActiveEnrollmentMatchVerifiesWithoutReturningIdentity()
    {
        var ownId = Guid.NewGuid();
        var result = await Verify(ownId, [new(ownId, 0.91)]);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Verified);
        Assert.Equal(0.91, result.Value.Similarity);
        var json = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain(ownId.ToString(), json);
        Assert.DoesNotContain("embedding", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnotherSantriMatchDoesNotLeakIdentityOrSimilarity()
    {
        var otherId = Guid.NewGuid();
        var result = await Verify(Guid.NewGuid(), [new(otherId, 0.93)]);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Verified);
        Assert.Equal("FaceDoesNotBelongToAccount", result.Value.Reason);
        Assert.Null(result.Value.Similarity);
        Assert.DoesNotContain(otherId.ToString(), JsonSerializer.Serialize(result.Value));
    }

    [Fact]
    public async Task EmptyActiveGalleryReturnsUnknownFace()
    {
        var result = await Verify(Guid.NewGuid(), []);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Verified);
        Assert.Equal("UnknownFace", result.Value.Reason);
    }

    [Fact]
    public async Task CompetingActiveIdentitiesReturnAmbiguousMatch()
    {
        var result = await Verify(Guid.NewGuid(), [new(Guid.NewGuid(), 0.94), new(Guid.NewGuid(), 0.91)]);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Verified);
        Assert.Equal("AmbiguousMatch", result.Value.Reason);
        Assert.Null(result.Value.Similarity);
    }

    [Fact]
    public async Task MissingSantriDoesNotInvokeFaceService()
    {
        var service = new FakeService();
        var command = new VerifySantriFace(new FakeStore(null), new RecognizeFaceImage(service, new FakeReader([]), 0.75));
        var result = await command.HandleAsync(Guid.NewGuid(), Image());
        Assert.Equal("SantriNotFound", result.Error.Code);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(FaceServiceFailureReason.FaceServiceUnavailable, "FaceServiceUnavailable")]
    [InlineData(FaceServiceFailureReason.InvalidImage, "InvalidImage")]
    public async Task AnalysisFailuresRemainErrors(FaceServiceFailureReason failure, string code)
    {
        var service = new FakeService { Failure = failure };
        var result = await new VerifySantriFace(new FakeStore(Guid.NewGuid()),
            new RecognizeFaceImage(service, new FakeReader([]), 0.75)).HandleAsync(Guid.NewGuid(), Image());
        Assert.Equal(code, result.Error.Code);
    }

    [Fact]
    public async Task MissingThresholdIsAServiceErrorNotAnUnknownFace()
    {
        var service = new FakeService();
        var result = await new VerifySantriFace(new FakeStore(Guid.NewGuid()),
            new RecognizeFaceImage(service, new FakeReader([]), null)).HandleAsync(Guid.NewGuid(), Image());
        Assert.Equal("RecognitionNotConfigured", result.Error.Code);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void VerifyEndpointRequiresSantriAndMultipartPhoto()
    {
        var controller = typeof(SantriFaceProfileController);
        Assert.Equal("Santri", Assert.Single(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);
        Assert.Equal("api/v1/face-profiles/me", Assert.Single(controller.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>()).Template);
        var action = controller.GetMethod(nameof(SantriFaceProfileController.Verify))!;
        Assert.Equal("verify", Assert.Single(action.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>()).Template);
        Assert.Equal("multipart/form-data", Assert.Single(action.GetCustomAttributes(typeof(ConsumesAttribute), true).Cast<ConsumesAttribute>()).ContentTypes.Single());
    }

    private static async Task<KH2.ManagementSystem.BuildingBlocks.Results.Result<SantriFaceVerificationDto>> Verify(
        Guid owner, IReadOnlyList<FaceMatchCandidate> candidates) =>
        await new VerifySantriFace(new FakeStore(owner),
            new RecognizeFaceImage(new FakeService(), new FakeReader(candidates), 0.75))
            .HandleAsync(Guid.NewGuid(), Image());

    private static FaceImage Image() => new("probe.jpg", "image/jpeg", Stream.Null);

    private sealed class FakeStore(Guid? owner) : IFaceEnrollmentStore
    {
        public Task<Guid?> FindSantriIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(owner);
        public Task<FaceProfile?> FindProfileAsync(Guid santriId, CancellationToken cancellationToken) => Task.FromResult<FaceProfile?>(null);
        public Task<bool> SaveAsync(FaceProfile profile, bool isNewProfile, FaceEnrollment enrollment,
            IReadOnlyList<FaceEmbedding> embeddings, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeReader(IReadOnlyList<FaceMatchCandidate> candidates) : IFaceMatchReader
    {
        public Task<IReadOnlyList<FaceMatchCandidate>> FindNearestAsync(FaceEmbeddingResult probe, CancellationToken cancellationToken) =>
            Task.FromResult(candidates);
    }

    private sealed class FakeService : IFaceRecognitionService
    {
        public FaceServiceFailureReason? Failure { get; init; }
        public int Calls { get; private set; }
        public Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken)
        {
            Calls++;
            var embedding = new float[512];
            embedding[0] = 1;
            return Task.FromResult(Failure.HasValue
                ? FaceImageAnalysisResult.Rejected(Failure.Value)
                : FaceImageAnalysisResult.Accepted(new FaceDetectionResult(1),
                    new FaceEmbeddingResult(embedding, "arcface", "test-v1")));
        }
        public Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken) =>
            Task.FromResult(FaceServiceHealthResult.Healthy());
    }
}
