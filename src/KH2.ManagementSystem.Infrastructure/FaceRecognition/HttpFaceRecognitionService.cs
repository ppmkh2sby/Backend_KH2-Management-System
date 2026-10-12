using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class HttpFaceRecognitionService(
    HttpClient client,
    IOptions<FaceRecognitionServiceOptions> options,
    ILogger<HttpFaceRecognitionService> logger)
    : IFaceRecognitionService
{
    private const int MaximumResponseBytes = 65536;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, Exception?> GatewayTimeoutLog = LoggerMessage.Define(
        LogLevel.Warning, new EventId(1001, "FaceServiceGatewayTimeout"), "Face service timed out with gateway timeout response.");
    private static readonly Action<ILogger, int, Exception?> UnsuccessfulStatusLog = LoggerMessage.Define<int>(
        LogLevel.Warning, new EventId(1002, "FaceServiceUnsuccessfulStatus"), "Face service returned unsuccessful status code {StatusCode}.");
    private static readonly Action<ILogger, Exception?> TimeoutLog = LoggerMessage.Define(
        LogLevel.Warning, new EventId(1003, "FaceServiceTimeout"), "Face service request timed out.");
    private static readonly Action<ILogger, Exception?> UnavailableLog = LoggerMessage.Define(
        LogLevel.Warning, new EventId(1004, "FaceServiceUnavailable"), "Face service request was unavailable.");

    public async Task<FaceImageAnalysisResult> AnalyzeImageAsync(FaceImage image, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();
        if (image.ContentType is not ("image/jpeg" or "image/png") || !image.Content.CanRead)
            return FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.InvalidImage);

        // Buffer at most one bounded image. Disposing the request must not close the caller's stream.
        using var bytes = new MemoryStream();
        if (!await CopyBoundedAsync(image.Content, bytes, 5 * 1024 * 1024, cancellationToken) || bytes.Length == 0)
            return FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.InvalidImage);
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        form.Add(content, "image", image.ContentType == "image/png" ? "capture.png" : "capture.jpg");
        var (payload, failure) = await SendAsync(HttpMethod.Post, "v1/analyze", form, cancellationToken);
        if (failure.HasValue) return FaceImageAnalysisResult.Rejected(failure.Value);
        try
        {
            var body = JsonSerializer.Deserialize<AnalysisResponse>(payload!, JsonOptions);
            if (body?.IsAccepted is null) throw new JsonException();
            FaceDetectionResult? detection = body.FaceCount.HasValue ? new(body.FaceCount.Value, body.QualityScore) : null;
            if (!body.IsAccepted.Value)
            {
                if (body.Embedding is not null || string.IsNullOrWhiteSpace(body.Reason)) throw new JsonException();
                return FaceImageAnalysisResult.Rejected(FaceServiceFailureReasons.FromServiceCode(body.Reason), detection);
            }

            if (detection is null || body.Embedding is null || body.Reason is not null ||
                string.IsNullOrWhiteSpace(body.ModelName) || body.ModelName.Length > 100 ||
                string.IsNullOrWhiteSpace(body.ModelVersion) || body.ModelVersion.Length > 100 ||
                !body.Embedding.Any(value => value != 0)) throw new JsonException();
            return FaceImageAnalysisResult.Accepted(detection,
                new FaceEmbeddingResult(body.Embedding, body.ModelName, body.ModelVersion, body.QualityScore));
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return FaceImageAnalysisResult.Rejected(FaceServiceFailureReason.InvalidFaceServiceResponse);
        }
    }

    public async Task<FaceServiceHealthResult> HealthCheckAsync(CancellationToken cancellationToken)
    {
        var (payload, failure) = await SendAsync(HttpMethod.Get, "v1/health", null, cancellationToken);
        if (failure.HasValue) return FaceServiceHealthResult.Unhealthy(failure.Value);
        try
        {
            var body = JsonSerializer.Deserialize<HealthResponse>(payload!, JsonOptions);
            if (body?.IsHealthy == true && body.Reason is null) return FaceServiceHealthResult.Healthy();
            if (body?.IsHealthy == false && body.Reason == nameof(FaceServiceFailureReason.FaceServiceUnavailable))
                return FaceServiceHealthResult.Unhealthy(FaceServiceFailureReason.FaceServiceUnavailable);
        }
        catch (JsonException) { }
        return FaceServiceHealthResult.Unhealthy(FaceServiceFailureReason.InvalidFaceServiceResponse);
    }

    private async Task<(byte[]? Payload, FaceServiceFailureReason? Failure)> SendAsync(
        HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try
        {
            using var request = new HttpRequestMessage(method, new Uri(new Uri(options.Value.BaseUrl.TrimEnd('/') + "/"), path))
            { Content = content };
            request.Headers.Add("X-Face-Service-Key", options.Value.ApiKey);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                GatewayTimeoutLog(logger, null);
                return (null, FaceServiceFailureReason.FaceServiceTimeout);
            }
            if (!response.IsSuccessStatusCode)
            {
                UnsuccessfulStatusLog(logger, (int)response.StatusCode, null);
                return (null, FaceServiceFailureReason.FaceServiceUnavailable);
            }
            if (response.Content.Headers.ContentLength > MaximumResponseBytes)
                return (null, FaceServiceFailureReason.InvalidFaceServiceResponse);
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            if (!await CopyBoundedAsync(stream, buffer, MaximumResponseBytes, timeout.Token))
                return (null, FaceServiceFailureReason.InvalidFaceServiceResponse);
            return (buffer.ToArray(), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TimeoutLog(logger, null);
            return (null, FaceServiceFailureReason.FaceServiceTimeout);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            UnavailableLog(logger, null);
            return (null, FaceServiceFailureReason.FaceServiceUnavailable);
        }
    }

    private static async Task<bool> CopyBoundedAsync(Stream source, Stream target, int limit, CancellationToken token)
    {
        var buffer = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(buffer, token)) > 0)
        {
            if (target.Length + count > limit) return false;
            await target.WriteAsync(buffer.AsMemory(0, count), token);
        }
        return true;
    }

    private sealed record AnalysisResponse(bool? IsAccepted, int? FaceCount, float[]? Embedding,
        float? QualityScore, string? ModelName, string? ModelVersion, string? Reason);
    private sealed record HealthResponse(bool? IsHealthy, string? Reason);
}
