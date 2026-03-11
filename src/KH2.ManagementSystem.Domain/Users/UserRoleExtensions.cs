namespace KH2.ManagementSystem.Domain.Users;

public static class UserRoleExtensions
{
    public static bool CanReadAllSantri(this UserRole role) => 
        role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;

    public static bool CanApprove(this UserRole role) => 
        role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;
    
    public static bool CanIsInternalManagement(this UserRole role) =>
        role is UserRole.Admin or UserRole.DewanGuru or UserRole.Pengurus;

    public static bool IsSantriSide(this UserRole role) =>
        role is UserRole.Santri or UserRole.WaltiSantri;
}