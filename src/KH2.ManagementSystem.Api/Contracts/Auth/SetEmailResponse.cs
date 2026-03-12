namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed record SetEmailResponse(
    string Email,
    bool EmailConfirmed,
    string? VerificationCode);