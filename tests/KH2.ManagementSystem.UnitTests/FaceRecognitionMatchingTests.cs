using System.Text.Json;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class FaceRecognitionMatchingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(1.01)]
    [InlineData(-1.01)]
    public async Task UncalibratedOrInvalidThresholdFailsBeforeAnyAnalysis(double? threshold)
    {
        var service = new TestService();
        var reader = new TestReader();
        var result = await new RecognizeFaceImage(service, reader, threshold).HandleAsync(Image());
        Assert.Equal("RecognitionNotConfigured", result.Error.Code);
        Assert.Equal(0, service.Calls);
        Assert.Equal(0, reader.Calls);
    }

    [Theory]
    [InlineData(0.75, true)]
    [InlineData(0.7499, false)]
    [InlineData(1.0, true)]
    public async Task ThresholdBoundaryIsInclusive(double score, bool recognized)
    {
        var id = Guid.NewGuid();
        var reader = new TestReader { Candidates = [new(id, score)] };
        var result = await new RecognizeFaceImage(new TestService(), reader, 0.75).HandleAsync(Image());
        Assert.True(result.IsSuccess);
        Assert.Equal(recognized, result.Value!.IsRecognized);
        Assert.Equal(recognized ? id : null, result.Value.SantriId);
        Assert.Equal(recognized ? score : null, result.Value.Similarity);
        Assert.Equal(recognized ? null : "UnknownFace", result.Value.Reason);
    }

    [Fact]
    public async Task EmptyGalleryDoesNotReturnAnIdentity()
    {
        var result = await new RecognizeFaceImage(new TestService(), new TestReader(), 0.75).HandleAsync(Image());
        Assert.True(result.IsSuccess);
        Assert.Equal("UnknownFace", result.Value!.Reason);
        Assert.Null(result.Value.SantriId);
        Assert.Null(result.Value.Similarity);
    }

    [Theory]
    [InlineData(0.75)]
    [InlineData(0.9)]
    public async Task MultipleIdentitiesAboveThresholdAreAmbiguous(double secondScore)
    {
        var reader = new TestReader { Candidates = [new(Guid.NewGuid(), secondScore), new(Guid.NewGuid(), 0.95)] };
        var result = await new RecognizeFaceImage(new TestService(), reader, 0.75).HandleAsync(Image());
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsRecognized);
        Assert.Equal("AmbiguousMatch", result.Value.Reason);
        Assert.Null(result.Value.SantriId);
        Assert.Null(result.Value.Similarity);
    }

    [Fact]
    public async Task WinningCandidateUsesExactProbeModelAndExcludesBiometricsFromDto()
    {
        var winner = Guid.NewGuid();
        var reader = new TestReader { Candidates = [new(Guid.NewGuid(), 0.5), new(winner, 0.9)] };
        var result = await new RecognizeFaceImage(new TestService(), reader, 0.75).HandleAsync(Image());
        Assert.Equal(winner, result.Value!.SantriId);
        Assert.Equal("arcface", reader.Probe!.ModelName);
        Assert.Equal("pipeline-v1", reader.Probe.ModelVersion);
        var json = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("embedding", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("image", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(FaceServiceFailureReason.NoFaceDetected)]
    [InlineData(FaceServiceFailureReason.MultipleFacesDetected)]
    [InlineData(FaceServiceFailureReason.PoorImageQuality)]
    [InlineData(FaceServiceFailureReason.InvalidImage)]
    [InlineData(FaceServiceFailureReason.FaceServiceUnavailable)]
    [InlineData(FaceServiceFailureReason.FaceServiceTimeout)]
    [InlineData(FaceServiceFailureReason.InvalidFaceServiceResponse)]
    public async Task AnalysisFailureDoesNotQueryGallery(FaceServiceFailureReason failure)
    {
        var reader = new TestReader();
        var result = await new RecognizeFaceImage(new TestService { Failure = failure }, reader, 0.75).HandleAsync(Image());
        Assert.Equal(failure.ToString(), result.Error.Code);
        Assert.Equal(0, reader.Calls);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(1.1)]
    [InlineData(-1.1)]
    public async Task InvalidSimilarityNeverRecognizesAnIdentity(double score)
    {
        var reader = new TestReader { Candidates = [new(Guid.NewGuid(), score)] };
        var result = await new RecognizeFaceImage(new TestService(), reader, 0.75).HandleAsync(Image());
        Assert.Equal("InvalidFaceMatchResult", result.Error.Code);
    }

    [Fact]
    public async Task DuplicateCandidateIdentitiesAreAnInvalidReaderResult()
    {
        var id = Guid.NewGuid();
        var reader = new TestReader { Candidates = [new(id, 0.95), new(id, 0.9)] };
        var result = await new RecognizeFaceImage(new TestService(), reader, 0.75).HandleAsync(Image());
        Assert.Equal("InvalidFaceMatchResult", result.Error.Code);
    }

    [Fact]
    public async Task ZeroVectorIsRejectedBeforeCosineSearch()
    {
        var reader = new TestReader();
        var result = await new RecognizeFaceImage(new TestService { ZeroVector = true }, reader, 0.75).HandleAsync(Image());
        Assert.Equal("InvalidFaceServiceResponse", result.Error.Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task CallerCancellationIsPreserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new TestService();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new RecognizeFaceImage(service, new TestReader(), 0.75).HandleAsync(Image(), cancellation.Token));
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void PostgreSqlSearchAggregatesPerSantriAndUsesParameterizedActiveModelFilters()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_tests;Username=postgres;Password=postgres", options => options.UseVector()).Options);
        var values = new float[512];
        values[0] = 1;
        var query = new FaceMatchReader(context).CreateSearchQuery(new FaceEmbeddingResult(values, "arcface", "pipeline-v1"));
        var sql = query.ToQueryString();
        Assert.Contains("<=> @probe", sql, StringComparison.Ordinal);
        Assert.Contains("GROUP BY eligible.\"SantriId\"", sql, StringComparison.Ordinal);
        Assert.Contains("MAX(1.0 - eligible.\"Distance\")", sql, StringComparison.Ordinal);
        Assert.Contains("LIMIT 2", sql, StringComparison.Ordinal);
        Assert.Contains("embedding.\"IsActive\" = TRUE", sql, StringComparison.Ordinal);
        Assert.Contains("profile.\"Status\" = @activeStatus", sql, StringComparison.Ordinal);
        Assert.Contains("profile.\"CurrentEnrollmentId\" = enrollment.\"Id\"", sql, StringComparison.Ordinal);
        Assert.Contains("enrollment.\"ModelName\" = @modelName", sql, StringComparison.Ordinal);
        Assert.Contains("enrollment.\"ModelVersion\" = @modelVersion", sql, StringComparison.Ordinal);
        Assert.Contains("account.\"IsActive\" = TRUE", sql, StringComparison.Ordinal);
        Assert.Contains("account.\"Role\" = @santriRole", sql, StringComparison.Ordinal);
        Assert.Contains("BETWEEN 0.0 AND 2.0", sql, StringComparison.Ordinal);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    private static FaceImage Image() => new("probe.jpg", "image/jpeg", Stream.Null);
    private sealed class TestService : IFaceRecognitionService
    {
        public int Calls { get; private set; }
        public FaceServiceFailureReason? Failure { get; init; }
        public bool ZeroVector { get; init; }
        public Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken)
        {
            Calls++;
            var values = new float[512];
            if (!ZeroVector) values[0] = 1;
            FaceDetectionResult? detection = Failure switch
            {
                FaceServiceFailureReason.NoFaceDetected => new(0),
                FaceServiceFailureReason.MultipleFacesDetected => new(2),
                _ => null
            };
            return Task.FromResult(Failure.HasValue ? FaceImageAnalysisResult.Rejected(Failure.Value, detection)
                : FaceImageAnalysisResult.Accepted(new FaceDetectionResult(1), new FaceEmbeddingResult(values, "arcface", "pipeline-v1")));
        }
        public Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken) => Task.FromResult(FaceServiceHealthResult.Healthy());
    }
    private sealed class TestReader : IFaceMatchReader
    {
        public int Calls { get; private set; }
        public IReadOnlyList<FaceMatchCandidate> Candidates { get; init; } = [];
        public FaceEmbeddingResult? Probe { get; private set; }
        public Task<IReadOnlyList<FaceMatchCandidate>> FindNearestAsync(FaceEmbeddingResult probe, CancellationToken cancellationToken)
        {
            Calls++;
            Probe = probe;
            return Task.FromResult(Candidates);
        }
    }
}
