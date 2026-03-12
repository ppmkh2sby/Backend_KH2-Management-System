namespace KH2.ManagementSystem.Api.Contracts.Auth;

public sealed record VerifyEmailRequest(
    string Code);