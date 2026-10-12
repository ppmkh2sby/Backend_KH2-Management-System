namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

public enum FaceServiceFailureReason
{
    NoFaceDetected = 1,
    MultipleFacesDetected = 2,
    PoorImageQuality = 3,
    InvalidImage = 4,
    FaceServiceUnavailable = 5,
    FaceServiceTimeout = 6,
    InvalidFaceServiceResponse = 7
}

public static class FaceServiceFailureReasons
{
    // Only known wire codes are accepted. Never forward Python exception text.
    public static FaceServiceFailureReason FromServiceCode(string? code) => code switch
    {
        nameof(FaceServiceFailureReason.NoFaceDetected) => FaceServiceFailureReason.NoFaceDetected,
        nameof(FaceServiceFailureReason.MultipleFacesDetected) => FaceServiceFailureReason.MultipleFacesDetected,
        nameof(FaceServiceFailureReason.PoorImageQuality) => FaceServiceFailureReason.PoorImageQuality,
        nameof(FaceServiceFailureReason.InvalidImage) => FaceServiceFailureReason.InvalidImage,
        nameof(FaceServiceFailureReason.FaceServiceUnavailable) => FaceServiceFailureReason.FaceServiceUnavailable,
        nameof(FaceServiceFailureReason.FaceServiceTimeout) => FaceServiceFailureReason.FaceServiceTimeout,
        _ => FaceServiceFailureReason.InvalidFaceServiceResponse
    };
}
