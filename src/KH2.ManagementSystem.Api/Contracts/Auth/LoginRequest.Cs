namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed record LoginRequest(
    string Identity,
    string Password);