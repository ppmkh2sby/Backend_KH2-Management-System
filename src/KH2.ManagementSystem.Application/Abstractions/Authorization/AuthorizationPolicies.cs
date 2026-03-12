namespace KH2.ManagementSystem.Application.Abstractions.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string InternalManagement = nameof(InternalManagement);
    public const string CanApprove = nameof(CanApprove);
    public const string CanReadAllSantri = nameof(CanReadAllSantri);
    public const string CanAccessSantri = nameof(CanAccessSantri);
}