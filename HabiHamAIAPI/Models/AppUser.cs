namespace HabiHamAIAPI.Models;

public sealed class AppUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AppUserRole Role { get; set; } = AppUserRole.User;
    public DateTime CreatedAtUtc { get; set; }
}
