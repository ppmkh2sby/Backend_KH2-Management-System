using KH2.ManagementSystem.Domain.Common;

namespace KH2.ManagementSystem.Domain.Users;

public sealed class User : AuditableEntity<Guid>
{
    public User(
        Guid id,
        string username,
        string fullName,
        string? email,
        UserRole role,
        string passwordHash,
        bool emailConfirmed = false,
        bool isActive = true,
        bool mustChangePassword = true)
        : base(id)
    {
        SetUsername(username);
        Rename(fullName);

        if (!string.IsNullOrWhiteSpace(email))
        {
            SetEmail(email);
        }

        ChangeRole(role);
        SetPasswordHash(passwordHash);

        EmailConfirmed = emailConfirmed;
        IsActive = isActive;
        MustChangePassword = mustChangePassword;
    }

    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public UserRole Role { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool EmailConfirmed { get; private set; }
    public bool IsActive { get; private set; }
    public bool MustChangePassword { get; private set; }

    public void SetUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        Username = username.Trim();
        Touch(DateTimeOffset.UtcNow);
    }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        FullName = fullName.Trim();
        Touch(DateTimeOffset.UtcNow);
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Email = email.Trim().ToLowerInvariant();
        Touch(DateTimeOffset.UtcNow);
    }

    public void ClearEmail()
    {
        Email = null;
        EmailConfirmed = false;
        Touch(DateTimeOffset.UtcNow);
    }

    public void MarkEmailConfirmed()
    {
        EmailConfirmed = true;
        Touch(DateTimeOffset.UtcNow);
    }

    public void MarkEmailUnconfirmed()
    {
        EmailConfirmed = false;
        Touch(DateTimeOffset.UtcNow);
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        Touch(DateTimeOffset.UtcNow);
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash.Trim();
        Touch(DateTimeOffset.UtcNow);
    }

    public void RequirePasswordChange()
    {
        MustChangePassword = true;
        Touch(DateTimeOffset.UtcNow);
    }

    public void CompletePasswordChange()
    {
        MustChangePassword = false;
        Touch(DateTimeOffset.UtcNow);
    }

    public void Activate()
    {
        IsActive = true;
        Touch(DateTimeOffset.UtcNow);
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch(DateTimeOffset.UtcNow);
    }
}