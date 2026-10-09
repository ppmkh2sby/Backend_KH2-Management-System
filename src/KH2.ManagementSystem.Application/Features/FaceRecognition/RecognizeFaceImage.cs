using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.BuildingBlocks.Results;

namespace KH2.ManagementSystem.Application.Features.FaceRecognition;

public sealed record FaceRecognitionDto(bool IsRecognized, Guid? SantriId, double? Similarity, string? Reason);

public sealed class RecognizeFaceImage(IFaceRecognitionService service, IFaceMatchReader reader, double? similarityThreshold)
{
    public async Task<Result<FaceRecognitionDto>> HandleAsync(FaceImage image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();
        // Enrollment may run before calibration, but recognition must never invent a threshold.
        if (!similarityThreshold.HasValue || !double.IsFinite(similarityThreshold.Value) || similarityThreshold is < -1 or > 1)
            return Failure("RecognitionNotConfigured", "A calibrated similarity threshold is required.");
        var analysis = await service.AnalyzeImageAsync(image, cancellationToken);
        if (!analysis.IsAccepted)
            return Failure(analysis.Reason!.Value.ToString(), "Face analysis rejected the image.");
        var probe = analysis.Embedding!;
        if (probe.ModelName.Length > 100 || probe.ModelVersion.Length > 100 || !probe.Embedding.Any(value => value != 0))
            return Failure("InvalidFaceServiceResponse", "Face analysis returned an invalid embedding.");
        var candidates = await reader.FindNearestAsync(probe, cancellationToken);
        if (candidates.Count > 2 || candidates.Any(candidate => candidate.SantriId == Guid.Empty ||
            !double.IsFinite(candidate.Similarity) || candidate.Similarity is < -1 or > 1) ||
            candidates.Select(candidate => candidate.SantriId).Distinct().Count() != candidates.Count)
            return Failure("InvalidFaceMatchResult", "Face matching returned an invalid result.");
        var sorted = candidates.OrderByDescending(candidate => candidate.Similarity).ToArray();
        if (sorted.Length == 0 || sorted[0].Similarity < similarityThreshold.Value)
            return Result.Success(new FaceRecognitionDto(false, null, null, "UnknownFace"));
        if (sorted.Length > 1 && sorted[1].Similarity >= similarityThreshold.Value)
            return Result.Success(new FaceRecognitionDto(false, null, null, "AmbiguousMatch"));
        return Result.Success(new FaceRecognitionDto(true, sorted[0].SantriId, sorted[0].Similarity, null));
    }

    private static Result<FaceRecognitionDto> Failure(string code, string message) =>
        Result.Failure<FaceRecognitionDto>(new AppError(code, message));
}
