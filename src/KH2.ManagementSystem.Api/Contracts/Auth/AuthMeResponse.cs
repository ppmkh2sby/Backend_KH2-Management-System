namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed record AuthMeResponse(
    string UserId,
    string Username,
    string FullName,
    string? Email,
    string Role,
    bool EmailConfirmed,
    bool MustChangePassword,
    bool IsActive);