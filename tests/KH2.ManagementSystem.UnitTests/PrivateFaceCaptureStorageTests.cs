using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using Microsoft.Extensions.Options;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

public sealed class PrivateFaceCaptureStorageTests
{
    [Fact]
    public async Task SavesUnderPrivateRootUsingMimeDerivedExtensionAndDeletesCapture()
    {
        var root = Path.Combine(Path.GetTempPath(), $"kh2-face-test-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalPrivateFaceCaptureStorage(Options.Create(new FaceRecognitionOptions { CaptureStoragePath = root }));
            await using var content = new MemoryStream([1, 2, 3]);

            var stored = await storage.SaveAsync(Guid.NewGuid(), 1, "untrusted-name.exe", "image/png", content, CancellationToken.None);

            Assert.EndsWith(".png", stored.StorageKey, StringComparison.Ordinal);
            Assert.Equal("capture-1.png", stored.FileName);
            await storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            Assert.False(File.Exists(Path.Combine(root, stored.StorageKey.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RejectsUnsupportedMimeTypeBeforeWritingAFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"kh2-face-test-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalPrivateFaceCaptureStorage(Options.Create(new FaceRecognitionOptions { CaptureStoragePath = root }));
            await using var content = new MemoryStream([1]);

            await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(Guid.NewGuid(), 1, "capture.gif", "image/gif", content, CancellationToken.None));
            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
