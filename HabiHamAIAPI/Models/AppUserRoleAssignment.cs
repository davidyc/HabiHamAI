namespace HabiHamAIAPI.Models;

public sealed class AppUserRoleAssignment
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public AppRole? Role { get; set; }
}
