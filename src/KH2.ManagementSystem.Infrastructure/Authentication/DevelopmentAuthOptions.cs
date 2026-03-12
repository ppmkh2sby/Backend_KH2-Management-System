using KH2.ManagementSystem.Domain.Users;

namespace KH2.ManagementSystem.Infrastructure.Authentication;

public sealed class DevelopmentAuthOptions
{
    public const string SectionName = "DevelopmentAuth";

    public bool Enabled { get; init; }
    public List<DevelopmentUserOptions> Users { get; init; } = [];
}

public sealed class DevelopmentUserOptions
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}
