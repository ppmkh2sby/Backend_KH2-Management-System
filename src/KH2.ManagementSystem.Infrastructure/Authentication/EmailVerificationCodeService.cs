using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Application.Abstractions.Time;
using KH2.ManagementSystem.Domain.Auth;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.Authentication;

public sealed class EmailVerificationCodeService(
    AppDbContext dbContext,
    IClock clock)
    : IEmailVerificationCodeService
{
    public async Task<string> GenerateAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = clock.UtcNow;

        var existingCodes = await dbContext.EmailVerificationCodes
            .Where(x => x.UserId == userId && x.Email == normalizedEmail && x.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        dbContext.EmailVerificationCodes.RemoveRange(existingCodes);

        var plainCode = RandomNumberGenerator
            .GetInt32(100000, 999999)
            .ToString(CultureInfo.InvariantCulture);
        var codeHash = ComputeSha256(plainCode);

        var entity = new EmailVerificationCode(
            Guid.NewGuid(),
            userId,
            normalizedEmail,
            codeHash,
            now.AddMinutes(10));

        await dbContext.EmailVerificationCodes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return plainCode;
    }

    public async Task<bool> VerifyAsync(
        Guid userId,
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var codeHash = ComputeSha256(code.Trim());
        var now = clock.UtcNow;

        var record = await dbContext.EmailVerificationCodes
            .Where(x =>
                x.UserId == userId &&
                x.Email == normalizedEmail &&
                x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return false;
        }

        if (record.IsExpired(now))
        {
            return false;
        }

        if (!string.Equals(record.CodeHash, codeHash, StringComparison.Ordinal))
        {
            return false;
        }

        record.MarkUsed(now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
