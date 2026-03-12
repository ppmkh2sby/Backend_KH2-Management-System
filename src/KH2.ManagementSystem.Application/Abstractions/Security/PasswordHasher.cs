using KH2.ManagementSystem.Domain.Users;

namespace KH2.ManagementSystem.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string passwordHash, string providedPassword);
}