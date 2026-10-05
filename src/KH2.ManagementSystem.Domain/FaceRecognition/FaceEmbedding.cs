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
        float? qualityScore = null,
        bool isActive = true)
        : base(id)
    {
        FaceEnrollmentId = faceEnrollmentId != Guid.Empty
            ? faceEnrollmentId
            : throw new ArgumentException("Face enrollment id is required.", nameof(faceEnrollmentId));

        if (embedding.Length != RequiredDimensions)
        {
            throw new ArgumentException($"Embedding must contain exactly {RequiredDimensions} dimensions.", nameof(embedding));
        }

        if (captureIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(captureIndex), "Capture index must be positive.");
        }

        if (qualityScore is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(qualityScore), "Quality score must be between 0 and 1.");
        }

        Embedding = embedding.ToArray();
        CaptureIndex = captureIndex;
        QualityScore = qualityScore;
        IsActive = isActive;
    }

    public Guid FaceEnrollmentId { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Guid FaceProfileId => FaceEnrollmentId;
    public float? QualityScore { get; private set; }
    public int CaptureIndex { get; private set; }
    public bool IsActive { get; private set; }

}
