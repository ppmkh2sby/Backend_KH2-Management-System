using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Auth;

public sealed class EmailVerificationCode : AuditableEntity<Guid>
{
    public EmailVerificationCode(
        Guid id,
        Guid userId,
        string email,
        string codeHash,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new ArgumentException("Code hash is required.", nameof(codeHash));
        }

        UserId = userId;
        Email = email.Trim().ToLowerInvariant();
        CodeHash = codeHash.Trim();
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsed => UsedAtUtc.HasValue;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public void MarkUsed(DateTimeOffset usedAtUtc)
    {
        if (IsUsed)
        {
            return;
        }

        UsedAtUtc = usedAtUtc;
        Touch(usedAtUtc);
    }
}