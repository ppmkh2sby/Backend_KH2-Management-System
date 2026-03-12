namespace KH2.ManagementSystem.Application.Abstractions.Authentication;

public interface IAccessTokenProvider
{
    AccessTokenResult Create(AuthenticatedUser user);
}