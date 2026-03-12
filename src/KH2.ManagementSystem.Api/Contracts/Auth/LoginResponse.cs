namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    string Username,
    string FullName,
    string? Email,
    string Role,
    bool EmailConfirmed,
    bool MustChangePassword,
    bool IsActive);