namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

/// <summary>Image analysis shared by future enrollment and recognition flows.</summary>
public interface IFaceRecognitionService
{
    // Caller owns the image stream and must keep it open until completion.
    // YOLO detects faces; alignment and ArcFace/InsightFace produce the embedding.
    // This operation does not identify a Santri or persist anything.
    Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken);
    Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken);
}
