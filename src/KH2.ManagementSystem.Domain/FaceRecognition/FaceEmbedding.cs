using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.FaceRecognition;

public sealed class FaceEmbedding : AuditableEntity<Guid>
{
    public const int RequiredDimensions = 512;

    private float[] Embedding { get; set; } = [];

    private FaceEmbedding()
        : base(Guid.Empty)
    {
    }

    public FaceEmbedding(
        Guid id,
        Guid faceEnrollmentId,
        ReadOnlySpan<float> embedding,
        int captureIndex,
        float? qualityScore = null)
        : base(id)
    {
        FaceEnrollmentId = faceEnrollmentId != Guid.Empty
            ? faceEnrollmentId
            : throw new ArgumentException("Face enrollment id is required.", nameof(faceEnrollmentId));

        if (embedding.Length != RequiredDimensions)
        {
            throw new ArgumentException($"Embedding must contain exactly {RequiredDimensions} dimensions.", nameof(embedding));
        }

        if (captureIndex is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(captureIndex), "Capture index must be positive.");
        }

        if (qualityScore is not null && (!float.IsFinite(qualityScore.Value) || qualityScore is < 0f or > 1f))
        {
            throw new ArgumentOutOfRangeException(nameof(qualityScore), "Quality score must be between 0 and 1.");
        }

        Embedding = embedding.ToArray();
        CaptureIndex = captureIndex;
        QualityScore = qualityScore;
    }

    public Guid FaceEnrollmentId { get; private set; }
    public float? QualityScore { get; private set; }
    public int CaptureIndex { get; private set; }
}
