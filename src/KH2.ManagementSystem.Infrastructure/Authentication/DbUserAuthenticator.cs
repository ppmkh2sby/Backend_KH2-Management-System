using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KH2.ManagementSystem.Infrastructure.Authentication;

public sealed class DbUserAuthenticator(
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
}