using KH2.ManagementSystem.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.Authentication;

public sealed class DevelopmentUserAuthenticator(
    IOptions<DevelopmentAuthOptions> options)
    : IUserAuthenticator
{
    public Task<AuthenticatedUser?> AuthenticateAsync(
        string identity,
        string password,
        CancellationToken cancellationToken = default)
    {
        var authOptions = options.Value;

        if (!authOptions.Enabled)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var matchedUser = authOptions.Users.FirstOrDefault(user =>
            (
                string.Equals(user.Email, identity.Trim(), StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(user.Username) &&
                 string.Equals(user.Username, identity.Trim(), StringComparison.OrdinalIgnoreCase))
            ) &&
            user.Password == password);

        if (matchedUser is null)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var username = string.IsNullOrWhiteSpace(matchedUser.Username)
            ? matchedUser.Email
            : matchedUser.Username;

        var authenticatedUser = new AuthenticatedUser(
            matchedUser.UserId,
            username,
            matchedUser.FullName,
            matchedUser.Email,
            matchedUser.Role,
            EmailConfirmed: true,
            MustChangePassword: false,
            IsActive: true);

        return Task.FromResult<AuthenticatedUser?>(authenticatedUser);
    }
}
