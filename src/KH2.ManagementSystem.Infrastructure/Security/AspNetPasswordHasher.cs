using KH2.ManagementSystem.Application.Abstractions.Security;
using KH2.ManagementSystem.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace KH2.ManagementSystem.Infrastructure.Security;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string passwordHash, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, passwordHash, providedPassword);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}