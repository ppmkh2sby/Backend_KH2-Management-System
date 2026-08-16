namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class FaceRecognitionOptions
{
    public const string SectionName = "FaceRecognition";
    public string BaseUrl { get; init; } = "http://face-recognition.internal/";
    public decimal ConfidenceThreshold { get; init; } = 0.85m;
    public int TimeoutSeconds { get; init; } = 15;
    public string CaptureStoragePath { get; init; } = "App_Data/private-face-captures";
}
