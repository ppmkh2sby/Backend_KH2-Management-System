namespace KH2.ManagementSystem.Application.Abstractions.Authentication;

public interface IUserAuthenticator
{
    Task<AuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}