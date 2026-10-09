namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class FaceRecognitionServiceOptions
{
    public const string SectionName = "FaceRecognition";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
    // No production default. Calibrate cosine similarity using KH2 validation data.
    public double? SimilarityThreshold { get; set; }
    public int ExpectedEmbeddingDimension { get; set; } = 512;
    public int RequiredEnrollmentSamples { get; set; } = 5;
}
