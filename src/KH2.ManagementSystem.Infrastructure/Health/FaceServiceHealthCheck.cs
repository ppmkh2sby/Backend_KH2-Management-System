using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KH2.ManagementSystem.Infrastructure.Health;

public sealed class FaceServiceHealthCheck(IFaceRecognitionService faceRecognitionService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await faceRecognitionService.HealthCheckAsync(cancellationToken);
        return result.IsHealthy
            ? HealthCheckResult.Healthy("Face service is reachable.")
            : HealthCheckResult.Unhealthy("Face service is unavailable.");
    }
}
