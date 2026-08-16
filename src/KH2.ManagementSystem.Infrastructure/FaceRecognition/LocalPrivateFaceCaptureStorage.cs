using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class LocalPrivateFaceCaptureStorage(IOptions<FaceRecognitionOptions> options) : IFaceCaptureStorage
{
    private readonly string rootPath = Path.GetFullPath(options.Value.CaptureStoragePath);

    public async Task<StoredFaceCapture> SaveAsync(Guid enrollmentId, int sequence, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        extension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension.ToLowerInvariant();
        var relativeKey = Path.Combine(enrollmentId.ToString("N"), $"{sequence}{extension}");
        var destination = GetPath(relativeKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, cancellationToken);
        return new StoredFaceCapture(relativeKey.Replace('\\', '/'), contentType, Path.GetFileName(fileName));
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream result = new FileStream(GetPath(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(result);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetPath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(string storageKey)
    {
        var path = Path.GetFullPath(Path.Combine(rootPath, storageKey));
        if (!path.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage key tidak valid.");
        }

        return path;
    }
}
