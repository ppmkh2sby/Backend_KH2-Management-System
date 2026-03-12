namespace KH2.ManagementSystem.Application.Abstractions.Authentication;

public interface IEmailVerificationCodeService
{
    Task<string> GenerateAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(
        Guid userId,
        string email,
        string code,
        CancellationToken cancellationToken = default);
}