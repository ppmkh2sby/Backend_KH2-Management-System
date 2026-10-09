namespace KH2.ManagementSystem.Application.Abstractions.FaceRecognition;

public interface IFaceCaptureStorage
{
    Task<StoredFaceCapture> SaveAsync(Guid enrollmentId, int sequence, string fileName, string contentType, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed record StoredFaceCapture(string StorageKey, string ContentType, string FileName);
