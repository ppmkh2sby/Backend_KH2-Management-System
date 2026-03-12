using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Santris;

public sealed class Santri : AuditableEntity<Guid>
{
    public Santri(
        Guid id,
        Guid userId,
        string fullName,
        string nis)
        : base(id)
    {
        UserId = userId;
        Rename(fullName);
        ChangeNis(nis);
    }

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Nis { get; private set; } = string.Empty;

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Santri full name is required.", nameof(fullName));
        }

        FullName = fullName.Trim();
        Touch(DateTimeOffset.UtcNow);
    }

    public void ChangeNis(string nis)
    {
        if (string.IsNullOrWhiteSpace(nis))
        {
            throw new ArgumentException("Santri NIS is required.", nameof(nis));
        }

        Nis = nis.Trim();
        Touch(DateTimeOffset.UtcNow);
    }
}