using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class FaceServiceHealthCheckTests
{
    [Fact]
    public async Task HealthyFaceServiceProducesHealthyReadinessResult()
    {
        var result = await new FaceServiceHealthCheck(new TestService(true))
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Face service is reachable.", result.Description);
    }

    [Fact]
    public async Task UnavailableFaceServiceDoesNotExposeUpstreamDetails()
    {
        var result = await new FaceServiceHealthCheck(new TestService(false))
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Face service is unavailable.", result.Description);
    }

    private sealed class TestService(bool isHealthy) : IFaceRecognitionService
    {
        public Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken) =>
            Task.FromResult(FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.FaceServiceUnavailable));

        public Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken) =>
            Task.FromResult(isHealthy ? FaceServiceHealthResult.Healthy() : FaceServiceHealthResult.Unhealthy(FaceServiceFailureReason.FaceServiceUnavailable));
    }
}
