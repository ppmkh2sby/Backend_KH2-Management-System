namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed class LoginRequest
{
    public string? Identity { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? Nis { get; init; }
    public string Password { get; init; } = string.Empty;

    public string ResolvedIdentity =>
        Identity
        ?? Username
        ?? Email
        ?? Nis
        ?? string.Empty;
}
