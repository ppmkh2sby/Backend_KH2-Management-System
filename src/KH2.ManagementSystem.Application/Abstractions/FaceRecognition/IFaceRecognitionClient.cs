namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

public interface IFaceRecognitionClient
{
    Task<FaceCaptureValidationResult> ValidateEnrollmentCaptureAsync(FaceImage image, string expectedPose, CancellationToken cancellationToken);
    Task<FaceEnrollmentResult> EnrollAsync(Guid santriId, IReadOnlyCollection<FaceImage> images, CancellationToken cancellationToken);
    Task<FaceOpenerVerificationResult> VerifyOpenerAsync(Guid openerUserId, FaceImage image, CancellationToken cancellationToken);
    Task<FaceRecognitionResult> RecognizeAsync(FaceImage image, CancellationToken cancellationToken);
    Task DeleteProfileAsync(string providerProfileId, CancellationToken cancellationToken);
}

public sealed record FaceImage(string FileName, string ContentType, Stream Content);
public sealed record FaceCaptureValidationResult(bool IsValid, string? Reason);
public sealed record FaceEnrollmentResult(bool IsAccepted, string? ProviderProfileId, string? Reason);
public sealed record FaceOpenerVerificationResult(bool IsVerified, string? Reason);
public sealed record FaceRecognitionResult(Guid? SantriId, decimal? Confidence, int FaceCount, string? Reason);
