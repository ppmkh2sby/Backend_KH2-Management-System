using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

// Phase 2 placeholder: no network calls, image reads, persistence, or logging.
public sealed class UnavailableFaceRecognitionService : IFaceRecognitionService
{
    public Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.FaceServiceUnavailable));
    }

    public Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FaceServiceHealthResult.Unhealthy(FaceServiceFailureReason.FaceServiceUnavailable));
    }
}
