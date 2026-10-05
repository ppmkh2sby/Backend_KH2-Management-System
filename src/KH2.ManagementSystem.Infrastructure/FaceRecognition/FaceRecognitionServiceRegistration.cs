using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KH2.ManagementSystem.Application.Features.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Time;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public static class FaceRecognitionServiceRegistration
{
    public static IServiceCollection AddFaceRecognitionServiceContracts(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<FaceRecognitionServiceOptions>()
            .Bind(configuration.GetSection(FaceRecognitionServiceOptions.SectionName))
            .PostConfigure(options =>
            {
                // The embedding service can run alongside the legacy provider service.
                options.BaseUrl = options.AnalysisBaseUrl ?? options.BaseUrl;
                // Environment-provider configuration takes priority; retain the existing key as a fallback.
                options.ApiKey = configuration["FACE_SERVICE_API_KEY"]
                    ?? configuration["FaceRecognition:ApiKey"]
                    ?? configuration["FaceRecognition:ServiceApiKey"]
                    ?? string.Empty;
            })
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && string.IsNullOrEmpty(uri.UserInfo)
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment), "FaceRecognition:BaseUrl must be an HTTP(S) URI without credentials, query, or fragment.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey) && options.ApiKey.Trim().Length >= 32,
                "FaceRecognition:ApiKey must be at least 32 characters.")
            .Validate(options => options.TimeoutSeconds is > 0 and <= 60,
                "FaceRecognition:TimeoutSeconds must be between 1 and 60.")
            .Validate(options => options.SimilarityThreshold is null ||
                (double.IsFinite(options.SimilarityThreshold.Value) && options.SimilarityThreshold is >= -1 and <= 1),
                "FaceRecognition:SimilarityThreshold must be a finite cosine similarity between -1 and 1 when configured.")
            .Validate(options => options.ExpectedEmbeddingDimension == FaceEmbeddingResult.RequiredDimension,
                "FaceRecognition:ExpectedEmbeddingDimension must be 512 for the current model and database.")
            .Validate(options => options.RequiredEnrollmentSamples == 5,
                "FaceRecognition:RequiredEnrollmentSamples must be 5 for the Phase 3 enrollment flow.")
            .ValidateOnStart();
        services.AddHttpClient<IFaceRecognitionService, HttpFaceRecognitionService>(client =>
            client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .RemoveAllLoggers();
        services.AddScoped<IFaceEnrollmentStore, FaceEnrollmentStore>();
        services.AddScoped<IFaceMatchReader, FaceMatchReader>();
        services.AddScoped<IAttendanceDeviceAuthenticator, AttendanceDeviceAuthenticator>();
        services.AddScoped(provider => new RecognizeFaceImage(
            provider.GetRequiredService<IFaceRecognitionService>(), provider.GetRequiredService<IFaceMatchReader>(),
            provider.GetRequiredService<IOptions<FaceRecognitionServiceOptions>>().Value.SimilarityThreshold));
        services.AddScoped(provider => new EnrollFaceProfile(
            provider.GetRequiredService<IFaceRecognitionService>(), provider.GetRequiredService<IFaceEnrollmentStore>(),
            provider.GetRequiredService<IClock>(), provider.GetRequiredService<IOptions<FaceRecognitionServiceOptions>>().Value.RequiredEnrollmentSamples));
        return services;
    }
}
