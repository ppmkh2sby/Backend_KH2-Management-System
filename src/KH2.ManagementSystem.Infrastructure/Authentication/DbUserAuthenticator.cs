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
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Email == normalizedEmail,
                cancellationToken);

        if (user is null)
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
            user.FullName,
            user.Email,
            user.Role);
    }
}