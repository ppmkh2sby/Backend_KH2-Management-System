using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Auth;

public sealed class RefreshToken : AuditableEntity<Guid>
{
    public RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        Id = id;
        UserId = userId;
        TokenHash = tokenHash.Trim();
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
        Touch(revokedAtUtc);
    }
}