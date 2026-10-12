using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.FaceRecognition;

public sealed record SantriFaceVerificationDto(bool Verified, double? Similarity, string? Reason);

public sealed class VerifySantriFace(IFaceEnrollmentStore store, RecognizeFaceImage recognition)
{
    public async Task<Result<SantriFaceVerificationDto>> HandleAsync(
        Guid userId, FaceImage image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();

        var santriId = await store.FindSantriIdAsync(userId, cancellationToken);
        if (santriId is null)
            return Result.Failure<SantriFaceVerificationDto>(new AppError("SantriNotFound", "A Santri account is required."));

        var result = await recognition.HandleAsync(image, cancellationToken);
        if (!result.IsSuccess)
            return Result.Failure<SantriFaceVerificationDto>(result.Error);

        var match = result.Value!;
        if (!match.IsRecognized)
            return Result.Success(new SantriFaceVerificationDto(false, null, match.Reason));

        if (match.SantriId != santriId)
            return Result.Success(new SantriFaceVerificationDto(false, null, "FaceDoesNotBelongToAccount"));

        return Result.Success(new SantriFaceVerificationDto(true, match.Similarity, null));
    }
}
