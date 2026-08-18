namespace KH2.ManagementSystem.Api.Contracts.FaceRecognition;

public sealed record FaceEnrollmentResponse(
    string Status,
    int CaptureCount,
    int RequiredCaptureCount,
    IReadOnlyList<FaceCaptureGuideResponse> Guides,
    DateTimeOffset? RegisteredAtUtc,
    DateTimeOffset? EmbeddingUpdatedAtUtc,
    string? RejectionReason);

public sealed record FaceCaptureGuideResponse(int Sequence, string Pose, bool Captured);
public sealed record FaceEnrollmentCaptureResponse(int Sequence, string Pose, int CaptureCount, string Status);
