using System.Collections.ObjectModel;

namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

public sealed class FaceDetectionResult
{
    public FaceDetectionResult(int faceCount, float? qualityScore = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(faceCount);
        FaceEmbeddingResult.ValidateQualityScore(qualityScore);
        FaceCount = faceCount;
        QualityScore = qualityScore;
    }

    public int FaceCount { get; }
    public float? QualityScore { get; }
    public bool IsValid => FaceCount == 1;
    public FaceServiceFailureReason? Reason => FaceCount switch
    {
        0 => FaceServiceFailureReason.NoFaceDetected,
        1 => null,
        _ => FaceServiceFailureReason.MultipleFacesDetected
    };
}

// A class, rather than a positional record, avoids printing embedding values in ToString().
public sealed class FaceEmbeddingResult
{
    public const int RequiredDimension = 512;

    public FaceEmbeddingResult(
        ReadOnlySpan<float> embedding,
        string modelName,
        string modelVersion,
        float? qualityScore = null)
    {
        if (embedding.Length != RequiredDimension)
        {
            throw new ArgumentException("Embedding must contain exactly 512 dimensions.", nameof(embedding));
        }

        foreach (var value in embedding)
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Embedding values must be finite.", nameof(embedding));
            }
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        ValidateQualityScore(qualityScore);
        Embedding = Array.AsReadOnly(embedding.ToArray());
        ModelName = modelName.Trim();
        ModelVersion = modelVersion.Trim();
        QualityScore = qualityScore;
    }

    public ReadOnlyCollection<float> Embedding { get; }
    public string ModelName { get; }
    public string ModelVersion { get; }
    public float? QualityScore { get; }

    internal static void ValidateQualityScore(float? score)
    {
        if (score.HasValue && (!float.IsFinite(score.Value) || score.Value is < 0 or > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Quality score must be finite and between 0 and 1.");
        }
    }
}

public sealed class FaceImageAnalysisResult
{
    private FaceImageAnalysisResult(
        FaceDetectionResult? detection,
        FaceEmbeddingResult? embedding,
        FaceServiceFailureReason? reason)
    {
        Detection = detection;
        Embedding = embedding;
        Reason = reason;
    }

    public bool IsAccepted => Reason is null;
    // Null means detection could not run (for example, a timeout), not zero faces.
    public FaceDetectionResult? Detection { get; }
    public FaceEmbeddingResult? Embedding { get; }
    public FaceServiceFailureReason? Reason { get; }

    public static FaceImageAnalysisResult Accepted(FaceDetectionResult detection, FaceEmbeddingResult embedding)
    {
        ArgumentNullException.ThrowIfNull(detection);
        ArgumentNullException.ThrowIfNull(embedding);
        if (!detection.IsValid)
        {
            throw new ArgumentException("An accepted analysis requires exactly one face.", nameof(detection));
        }

        return new FaceImageAnalysisResult(detection, embedding, null);
    }

    public static FaceImageAnalysisResult Rejected(FaceServiceFailureReason reason, FaceDetectionResult? detection = null)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        if ((reason == FaceServiceFailureReason.NoFaceDetected && detection?.FaceCount != 0) ||
            (reason == FaceServiceFailureReason.MultipleFacesDetected && detection?.FaceCount is not > 1) ||
            (detection is { IsValid: false } && detection.Reason != reason))
        {
            throw new ArgumentException("Detection and failure reason must agree.", nameof(detection));
        }

        return new FaceImageAnalysisResult(detection, null, reason);
    }
}

public sealed class FaceServiceHealthResult
{
    private FaceServiceHealthResult(FaceServiceFailureReason? reason) => Reason = reason;

    public bool IsHealthy => Reason is null;
    public FaceServiceFailureReason? Reason { get; }
    public static FaceServiceHealthResult Healthy() => new(null);

    public static FaceServiceHealthResult Unhealthy(FaceServiceFailureReason reason)
    {
        if (reason is not (FaceServiceFailureReason.FaceServiceUnavailable or
            FaceServiceFailureReason.FaceServiceTimeout or FaceServiceFailureReason.InvalidFaceServiceResponse))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new FaceServiceHealthResult(reason);
    }
}
