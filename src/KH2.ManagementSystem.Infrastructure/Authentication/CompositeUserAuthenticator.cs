using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KH2.ManagementSystem.Infrastructure.Authentication;

public sealed class CompositeUserAuthenticator(
    IOptions<DevelopmentAuthOptions> options,
    AppDbContext dbContext,
    IPasswordHasher passwordHasher)
    : IUserAuthenticator
{
    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string identity,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedIdentity = identity.Trim();

        var developmentUser = AuthenticateDevelopmentUser(
            options.Value,
            normalizedIdentity,
            password);

        if (developmentUser is not null)
        {
            return developmentUser;
        }

        var normalizedEmail = normalizedIdentity.ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Username == normalizedIdentity ||
                     (x.Email != null && x.Email == normalizedEmail),
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var isPasswordValid = passwordHasher.VerifyPassword(
            user,
            user.PasswordHash,
            password);

        if (!isPasswordValid)
        {
            return null;
        }

        return new AuthenticatedUser(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Role,
            user.EmailConfirmed,
            user.MustChangePassword,
            user.IsActive);
    }

    private static AuthenticatedUser? AuthenticateDevelopmentUser(
        DevelopmentAuthOptions authOptions,
        string identity,
        string password)
    {
        if (!authOptions.Enabled)
        {
            return null;
        }

        var matchedUser = authOptions.Users.FirstOrDefault(user =>
            (
                string.Equals(user.Email, identity, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(user.Username) &&
                 string.Equals(user.Username, identity, StringComparison.OrdinalIgnoreCase))
            ) &&
            user.Password == password);

        if (matchedUser is null)
        {
            return null;
        }

        var username = string.IsNullOrWhiteSpace(matchedUser.Username)
            ? matchedUser.Email
            : matchedUser.Username;

        return new AuthenticatedUser(
            matchedUser.UserId,
            username,
            matchedUser.FullName,
            matchedUser.Email,
            matchedUser.Role,
            EmailConfirmed: true,
            MustChangePassword: false,
            IsActive: true);
    }
}
