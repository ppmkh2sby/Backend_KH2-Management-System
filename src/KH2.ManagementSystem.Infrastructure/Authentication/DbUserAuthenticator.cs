using KH2.ManagementSystem.Application.Abstractions.Authentication;
using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Domain.Users;
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

        if (user is null)
        {
            var waliUserId = await dbContext.WaliSantriRelations
                .Where(x => x.WaliSantriCode == normalizedIdentity)
                .Select(x => (Guid?)x.WaliUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (waliUserId.HasValue)
            {
                user = await dbContext.Users
                    .FirstOrDefaultAsync(
                        x => x.Id == waliUserId.Value && x.Role == UserRole.WaliSantri,
                        cancellationToken);
            }
        }

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
