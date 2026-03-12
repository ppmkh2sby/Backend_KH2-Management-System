using KH2.ManagementSystem.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.Authentication;

public sealed class DevelopmentUserAuthenticator(
    IOptions<DevelopmentAuthOptions> options)
    : IUserAuthenticator
{
    public Task<AuthenticatedUser?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var authOptions = options.Value;

        if (!authOptions.Enabled)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var matchedUser = authOptions.Users.FirstOrDefault(user =>
            string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) &&
            user.Password == password);

        if (matchedUser is null)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var authenticatedUser = new AuthenticatedUser(
            matchedUser.UserId,
            matchedUser.FullName,
            matchedUser.Email,
            matchedUser.Role);

        return Task.FromResult<AuthenticatedUser?>(authenticatedUser);
    }
}