using KH2.ManagementSystem.Domain.Users;

namespace KH2.ManagementSystem.Application.Abstractions.Authentication;

public sealed record AuthenticatedUser(
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role);