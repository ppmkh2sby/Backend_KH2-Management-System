namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    string FullName,
    string Email,
    string Role);