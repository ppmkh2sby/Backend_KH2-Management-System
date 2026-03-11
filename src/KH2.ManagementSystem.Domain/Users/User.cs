using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Users;

public sealed class User : AuditableEntity<Guid>
{
    public User(Guid id, string fullName, string email, UserRole role)
        : base(id)
    {
        Rename(fullName);
        ChangeEmail(email);
        Role = role;
    }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        FullName = fullName.Trim();
        Touch(DateTimeOffset.UtcNow);
    }

    public void ChangeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Email = email.Trim();
        Touch(DateTimeOffset.UtcNow);
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        Touch(DateTimeOffset.UtcNow);
    }
}
