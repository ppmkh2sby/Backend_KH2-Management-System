using KH2.ManagementSystem.Application.Abstractions.FaceRecognition;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.FaceRecognition;

public sealed class AttendanceDeviceAuthenticator(AppDbContext dbContext, IClock clock) : IAttendanceDeviceAuthenticator
{
    private readonly PasswordHasher<AttendanceDevice> _hasher = new();

    public async Task<AttendanceDeviceAuthenticationResult> AuthenticateAsync(
        string? deviceId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(deviceId, out var parsedDeviceId) || string.IsNullOrWhiteSpace(apiKey))
            return AttendanceDeviceAuthenticationResult.Denied();

        var device = await dbContext.AttendanceDevices
            .SingleOrDefaultAsync(item => item.Id == parsedDeviceId && item.IsActive, cancellationToken);
        if (device is null)
            return AttendanceDeviceAuthenticationResult.Denied();

        var verification = _hasher.VerifyHashedPassword(device, device.ApiKeyHash, apiKey);
        if (verification is not (PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded))
            return AttendanceDeviceAuthenticationResult.Denied();

        device.MarkSeen(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AttendanceDeviceAuthenticationResult.Authenticated(device.Id);
    }
}
