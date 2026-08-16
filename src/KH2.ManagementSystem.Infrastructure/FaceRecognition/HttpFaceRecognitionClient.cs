using System.Net;
using System.Net.Http.Json;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class HttpFaceRecognitionClient(HttpClient httpClient) : IFaceRecognitionClient
{
    public async Task<FaceCaptureValidationResult> ValidateEnrollmentCaptureAsync(FaceImage image, string expectedPose, CancellationToken cancellationToken)
    {
        using var form = CreateImageForm(image);
        form.Add(new StringContent(expectedPose), "expectedPose");
        var response = await SendAsync(HttpMethod.Post, "v1/enrollment/validate-capture", form, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<CaptureResponse>(cancellationToken: cancellationToken);
        return new FaceCaptureValidationResult(body?.IsValid == true, body?.Reason ?? "Capture tidak valid.");
    }

    public async Task<FaceEnrollmentResult> EnrollAsync(Guid userId, IReadOnlyCollection<FaceImage> images, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(userId.ToString()), "userId");
        foreach (var image in images)
        {
            form.Add(CreateImageContent(image), "images", image.FileName);
        }

        var response = await SendAsync(HttpMethod.Post, "v1/enrollment", form, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<EnrollmentResponse>(cancellationToken: cancellationToken);
        return new FaceEnrollmentResult(body?.IsAccepted == true, body?.ProviderProfileId, body?.Reason ?? "Profil wajah ditolak.");
    }

    public async Task<FaceOpenerVerificationResult> VerifyOpenerAsync(string providerProfileId, FaceImage image, CancellationToken cancellationToken)
    {
        using var form = CreateImageForm(image);
        form.Add(new StringContent(providerProfileId), "providerProfileId");
        var response = await SendAsync(HttpMethod.Post, "v1/attendance/verify-opener", form, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<VerificationResponse>(cancellationToken: cancellationToken);
        return new FaceOpenerVerificationResult(body?.IsVerified == true, body?.Reason ?? "Wajah petugas tidak terverifikasi.");
    }

    public async Task<FaceRecognitionResult> RecognizeAsync(FaceImage image, CancellationToken cancellationToken)
    {
        using var form = CreateImageForm(image);
        var response = await SendAsync(HttpMethod.Post, "v1/attendance/recognize", form, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<RecognitionResponse>(cancellationToken: cancellationToken);
        return new FaceRecognitionResult(body?.ProviderProfileId, body?.Confidence, body?.FaceCount ?? 0, body?.Reason);
    }

    public async Task DeleteProfileAsync(string providerProfileId, CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Delete, $"v1/enrollment/{Uri.EscapeDataString(providerProfileId)}", null, cancellationToken);
        response.Dispose();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                response.Dispose();
                throw new FaceRecognitionUnavailableException();
            }

            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                throw new FaceRecognitionUnavailableException();
            }

            return response;
        }
        catch (HttpRequestException exception)
        {
            throw new FaceRecognitionUnavailableException(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FaceRecognitionUnavailableException(exception);
        }
    }

    private static MultipartFormDataContent CreateImageForm(FaceImage image)
    {
        var form = new MultipartFormDataContent();
        form.Add(CreateImageContent(image), "image", image.FileName);
        return form;
    }

    private static StreamContent CreateImageContent(FaceImage image)
    {
        var content = new StreamContent(image.Content);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
        return content;
    }

    private sealed record CaptureResponse(bool IsValid, string? Reason);
    private sealed record EnrollmentResponse(bool IsAccepted, string? ProviderProfileId, string? Reason);
    private sealed record VerificationResponse(bool IsVerified, string? Reason);
    private sealed record RecognitionResponse(string? ProviderProfileId, decimal? Confidence, int? FaceCount, string? Reason);
}

public sealed class FaceRecognitionUnavailableException : Exception
{
    public FaceRecognitionUnavailableException(Exception? innerException = null)
        : base("Layanan AI pengenalan wajah tidak tersedia.", innerException)
    {
    }
}
