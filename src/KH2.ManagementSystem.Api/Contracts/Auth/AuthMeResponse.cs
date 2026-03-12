namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed record AuthMeResponse(
    string UserId,
    string FullName,
    string Email,
    string Role);