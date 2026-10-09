using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class FaceRecognitionServiceContractTests
{
    [Fact]
    public void ConfigurationDefaultsDoNotInventASimilarityThreshold()
    {
        using var provider = CreateProvider();
        var options = provider.GetRequiredService<IOptions<FaceRecognitionServiceOptions>>().Value;
        Assert.Equal(512, options.ExpectedEmbeddingDimension);
        Assert.Equal(5, options.RequiredEnrollmentSamples);
        Assert.Null(options.SimilarityThreshold);
        Assert.IsType<HttpFaceRecognitionService>(provider.GetRequiredService<IFaceRecognitionService>());
    }

    [Fact]
    public void CanonicalServiceUsesOnlyTheCanonicalBaseUrl()
    {
        using var provider = CreateProvider("LegacyFaceProvider:BaseUrl", "http://legacy.internal/");
        Assert.Equal("http://face.internal/", provider.GetRequiredService<IOptions<FaceRecognitionServiceOptions>>().Value.BaseUrl);
    }

    [Theory]
    [InlineData("BaseUrl", "file:///tmp/face")]
    [InlineData("BaseUrl", "https://user:password@face.internal")]
    [InlineData("BaseUrl", "invalid")]
    [InlineData("ApiKey", "short")]
    [InlineData("TimeoutSeconds", "0")]
    [InlineData("TimeoutSeconds", "61")]
    [InlineData("SimilarityThreshold", "NaN")]
    [InlineData("SimilarityThreshold", "1.1")]
    [InlineData("ExpectedEmbeddingDimension", "256")]
    [InlineData("RequiredEnrollmentSamples", "0")]
    public void InvalidConfigurationIsRejected(string key, string value)
    {
        using var provider = CreateProvider($"FaceRecognition:{key}", value);
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<FaceRecognitionServiceOptions>>().Value);
    }

    [Fact]
    public void CanonicalOptionsDoNotReadLegacyProviderSettings()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FaceRecognition:BaseUrl"] = "http://face.internal/",
            ["LegacyFaceProvider:ApiKey"] = new string('l', 32)
        }).Build();
        using var provider = new ServiceCollection().AddFaceRecognitionServiceContracts(config).BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<FaceRecognitionServiceOptions>>().Value);
    }

    [Fact]
    public void LegacyProviderOptionsBindOnlyFromLegacyFaceProvider()
    {
        var apiKey = new string('l', 32);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LegacyFaceProvider:BaseUrl"] = "http://legacy.internal/",
            ["LegacyFaceProvider:ApiKey"] = apiKey,
            ["LegacyFaceProvider:ConfidenceThreshold"] = "0.75",
            ["LegacyFaceProvider:TimeoutSeconds"] = "12",
            ["LegacyFaceProvider:CaptureStoragePath"] = "private-captures"
        }).Build();
        using var provider = new ServiceCollection().AddLegacyFaceProviderContracts(config).BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LegacyFaceProviderOptions>>().Value;

        Assert.Equal("http://legacy.internal/", options.BaseUrl);
        Assert.Equal(apiKey, options.ApiKey);
        Assert.Equal(0.75m, options.ConfidenceThreshold);
        Assert.Equal(12, options.TimeoutSeconds);
        Assert.Equal("private-captures", options.CaptureStoragePath);
        Assert.IsType<LocalPrivateFaceCaptureStorage>(provider.GetRequiredService<IFaceCaptureStorage>());
    }

    [Fact]
    public void OptionModelsHaveNonOverlappingResponsibilities()
    {
        var canonicalFields = typeof(FaceRecognitionServiceOptions).GetProperties().Select(property => property.Name).ToArray();
        var legacyFields = typeof(LegacyFaceProviderOptions).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("ConfidenceThreshold", canonicalFields);
        Assert.DoesNotContain("CaptureStoragePath", canonicalFields);
        Assert.DoesNotContain("SimilarityThreshold", legacyFields);
        Assert.DoesNotContain("ExpectedEmbeddingDimension", legacyFields);
        Assert.DoesNotContain("RequiredEnrollmentSamples", legacyFields);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(511)]
    [InlineData(513)]
    public void EmbeddingDimensionMustMatchFoundation(int dimension)
    {
        Assert.Throws<ArgumentException>(() => new FaceEmbeddingResult(new float[dimension], "arcface", "1"));
    }

    [Fact]
    public void EmbeddingRejectsNonFiniteValuesAndCopiesInput()
    {
        var values = new float[512];
        values[0] = float.NaN;
        Assert.Throws<ArgumentException>(() => new FaceEmbeddingResult(values, "arcface", "1"));
        values[0] = 0.125f;
        var result = new FaceEmbeddingResult(values, "arcface", "1");
        values[0] = 1;
        Assert.Equal(0.125f, result.Embedding[0]);
        Assert.DoesNotContain("0.125", result.ToString()!);
    }

    [Theory]
    [InlineData(0, FaceServiceFailureReason.NoFaceDetected)]
    [InlineData(2, FaceServiceFailureReason.MultipleFacesDetected)]
    public void DetectionFailuresCannotBeAccepted(int count, FaceServiceFailureReason reason)
    {
        var detection = new FaceDetectionResult(count);
        Assert.False(detection.IsValid);
        Assert.Equal(reason, detection.Reason);
        var rejected = FaceImageAnalysisResult.Rejected(reason, detection);
        Assert.False(rejected.IsAccepted);
        Assert.Null(rejected.Embedding);
        Assert.Throws<ArgumentException>(() => FaceImageAnalysisResult.Accepted(detection, NewEmbedding()));
    }

    [Fact]
    public void AcceptedAnalysisIncludesModelMetadataAndOneFace()
    {
        var result = FaceImageAnalysisResult.Accepted(new FaceDetectionResult(1, 0.9f), NewEmbedding());
        Assert.True(result.IsAccepted);
        Assert.Null(result.Reason);
        Assert.Equal("arcface", result.Embedding!.ModelName);
        Assert.Equal("1", result.Embedding.ModelVersion);
        Assert.Equal(512, result.Embedding.Embedding.Count);
    }

    [Fact]
    public void ContradictoryOrUnknownFailureReasonsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => FaceImageAnalysisResult.Rejected(
            FaceServiceFailureReason.NoFaceDetected, new FaceDetectionResult(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => FaceImageAnalysisResult.Rejected((FaceServiceFailureReason)999));
    }

    [Theory]
    [InlineData("NoFaceDetected", FaceServiceFailureReason.NoFaceDetected)]
    [InlineData("MultipleFacesDetected", FaceServiceFailureReason.MultipleFacesDetected)]
    [InlineData("PoorImageQuality", FaceServiceFailureReason.PoorImageQuality)]
    [InlineData("InvalidImage", FaceServiceFailureReason.InvalidImage)]
    [InlineData("FaceServiceUnavailable", FaceServiceFailureReason.FaceServiceUnavailable)]
    [InlineData("FaceServiceTimeout", FaceServiceFailureReason.FaceServiceTimeout)]
    [InlineData("InvalidFaceServiceResponse", FaceServiceFailureReason.InvalidFaceServiceResponse)]
    [InlineData("Internal Python exception details", FaceServiceFailureReason.InvalidFaceServiceResponse)]
    [InlineData("1", FaceServiceFailureReason.InvalidFaceServiceResponse)]
    [InlineData(null, FaceServiceFailureReason.InvalidFaceServiceResponse)]
    public void WireFailureCodesAreRestrictedToKnownReasons(string? code, FaceServiceFailureReason expected)
    {
        Assert.Equal(expected, FaceServiceFailureReasons.FromServiceCode(code));
    }

    [Fact]
    public async Task StubReportsUnavailableWithoutReadingImage()
    {
        var service = new UnavailableFaceRecognitionService();
        using var stream = new MemoryStream();
        stream.Close();
        var result = await service.AnalyzeImageAsync(new FaceImage("test.jpg", "image/jpeg", stream), CancellationToken.None);
        var health = await service.HealthCheckAsync(CancellationToken.None);
        Assert.False(result.IsAccepted);
        Assert.Null(result.Detection);
        Assert.Equal(FaceServiceFailureReason.FaceServiceUnavailable, result.Reason);
        Assert.False(health.IsHealthy);
        Assert.Equal(FaceServiceFailureReason.FaceServiceUnavailable, health.Reason);
        Assert.True(FaceServiceHealthResult.Healthy().IsHealthy);
    }

    [Fact]
    public async Task StubHonorsCallerCancellation()
    {
        var service = new UnavailableFaceRecognitionService();
        using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.AnalyzeImageAsync(
            new FaceImage("test.jpg", "image/jpeg", stream), cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.HealthCheckAsync(cancellation.Token));
    }

    private static FaceEmbeddingResult NewEmbedding() => new(new float[512], "arcface", "1", 0.9f);

    private static ServiceProvider CreateProvider(string? key = null, string? value = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["FaceRecognition:BaseUrl"] = "http://face.internal/",
            ["FaceRecognition:ApiKey"] = new string('t', 32)
        };
        if (key is not null) settings[key] = value;
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddFaceRecognitionServiceContracts(config).BuildServiceProvider();
    }
}
