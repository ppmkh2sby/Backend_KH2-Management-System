using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.BuildingBlocks.Results;
using KH2.ManagementSystem.Domain.FaceRecognition;

namespace KH2.ManagementSystem.Application.Features.FaceRecognition;

public sealed record FaceProfileEnrollmentDto(Guid Id, string Status, int SampleCount, string ModelName,
    string ModelVersion, DateTimeOffset EnrolledAtUtc);

public sealed class EnrollFaceProfile(IFaceRecognitionService service, IFaceEnrollmentStore store, IClock clock, int requiredSamples)
{
    public async Task<Result<FaceProfileEnrollmentDto>> HandleAsync(Guid userId, IReadOnlyList<FaceImage> images,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(images);
        cancellationToken.ThrowIfCancellationRequested();
        if (images.Count != requiredSamples || images.Count == 0)
            return Failure("InvalidSampleCount", "The required enrollment sample count was not supplied.");
        var santriId = await store.FindSantriIdAsync(userId, cancellationToken);
        if (santriId is null) return Failure("SantriNotFound", "A Santri account is required.");
        var profile = await store.FindProfileAsync(santriId.Value, cancellationToken);
        if (profile?.Status is FaceProfileStatus.Disabled)
            return Failure("FaceProfileDisabled", "The face profile is disabled and must be restored through an authorized workflow.");

        var results = new List<FaceEmbeddingResult>(images.Count);
        foreach (var image in images)
        {
            var analysis = await service.AnalyzeImageAsync(image, cancellationToken);
            if (!analysis.IsAccepted)
                return Failure(analysis.Reason!.Value.ToString(), "Face analysis rejected an enrollment sample.");
            var embedding = analysis.Embedding!;
            if (embedding.ModelName.Length > 100 || embedding.ModelVersion.Length > 100 ||
                (results.Count > 0 && (embedding.ModelName != results[0].ModelName || embedding.ModelVersion != results[0].ModelVersion)))
                return Failure("InvalidFaceServiceResponse", "Enrollment samples must use the same model.");
            results.Add(embedding);
        }
        var now = clock.UtcNow;
        var isNewProfile = profile is null;
        profile ??= new FaceProfile(Guid.NewGuid(), santriId.Value, now);
        var enrollment = new FaceEnrollment(Guid.NewGuid(), profile.Id, results[0].ModelName, results[0].ModelVersion, now);
        var embeddings = results.Select((result, index) => new FaceEmbedding(Guid.NewGuid(), enrollment.Id,
            result.Embedding.ToArray(), index + 1, result.QualityScore)).ToArray();
        if (!await store.SaveAsync(profile, isNewProfile, enrollment, embeddings, now, cancellationToken))
            return Failure("AlreadyEnrolled", "A face profile already exists.");
        return Result.Success(new FaceProfileEnrollmentDto(profile.Id, profile.Status.ToString(), embeddings.Length,
            enrollment.ModelName, enrollment.ModelVersion, enrollment.EnrolledAtUtc));
    }

    private static Result<FaceProfileEnrollmentDto> Failure(string code, string message) =>
        Result.Failure<FaceProfileEnrollmentDto>(new AppError(code, message));
}
