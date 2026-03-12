namespace KH2.ManagementSystem.Application.Abstractions.Authentication;

public sealed record AccessTokenResult(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds);