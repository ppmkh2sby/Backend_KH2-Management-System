namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

/// <summary>
/// Transitional configuration for the legacy provider-backed attendance and
/// staged-enrollment routes. This is deliberately separate from the canonical
/// embedding Face Service configuration.
/// </summary>
public sealed class LegacyFaceProviderOptions
{
    public const string SectionName = "LegacyFaceProvider";

    public string BaseUrl { get; init; } = "http://face-recognition.internal/";
    public string ApiKey { get; init; } = string.Empty;
    public decimal ConfidenceThreshold { get; init; } = 0.60m;
    public int TimeoutSeconds { get; init; } = 15;
    public string CaptureStoragePath { get; init; } = "App_Data/private-face-captures";
}
